using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Compiles and runs a C# snippet inside the running Editor, so an agent can reach the parts of the
	/// toolkit that have no dedicated MCP tool. The bridge's other methods are deliberately narrow; this
	/// one is the escape hatch for everything else, and it exists so the toolkit stays fully drivable in
	/// environments that have no separate code-execution bridge installed.
	///
	/// Deliberately NOT a sandbox. The code runs with the Editor's own rights, in the Editor's own domain,
	/// on the main thread. That is the point — it has to be able to call AssetDatabase and touch assets —
	/// and it is defensible only because the bridge itself is loopback-only and opt-in. Two consequences
	/// worth stating rather than discovering: an endless loop freezes the Editor until the handler timeout
	/// gives up on the answer (the loop keeps running), and every compiled snippet stays loaded in the
	/// domain until the next reload, because .NET cannot unload an assembly on its own.
	/// </summary>
	public static class UiCodeRunner
	{
		/// <summary>
		/// Prepended when the snippet is a bare statement body. Kept on ONE line on purpose: it makes the
		/// mapping from a diagnostic's line back to the caller's own line exact instead of approximate.
		/// </summary>
		private const string DefaultUsings =
			"using System; using System.Collections; using System.Collections.Generic; using System.Linq; " +
			"using System.Text; using UnityEngine; using UnityEngine.UI; using UnityEditor; using TMPro; " +
			"using GuiToolkit; using GuiToolkit.Style; using GuiToolkit.Editor; using Newtonsoft.Json.Linq; ";

		private const string WrapperOpen = "public static class __UiCodeRunnerScript { public static object Run() {";
		private const string WrapperClose = "\n} }";

		private const int MaxLogs = 200;

		/// <summary>
		/// Metadata references are file reads; rebuilding them per call is wasted work in a session that
		/// runs many snippets. Keyed by assembly count so a domain that gained an assembly rebuilds once.
		/// </summary>
		private static MetadataReference[] s_references;
		private static int s_referenceAssemblyCount = -1;

		/// <summary>Only ever grows within one domain — reported so a caller can see the cost accumulating.</summary>
		private static int s_compileCount;

		/// <summary>
		/// Payload: <c>{ "code": "...", "validateOnly": false, "entry": "Run" }</c>.
		/// Returns <c>{ compiled, ran, result, logs, diagnostics, wrapped, entryPoint, compilationsThisDomain }</c>.
		/// </summary>
		public static JObject Execute( JObject _request )
		{
			string code = (string)_request["code"];
			if (string.IsNullOrWhiteSpace(code))
				throw new Exception("executeCode requires a 'code' string.");

			bool validateOnly = (bool?)_request["validateOnly"] ?? false;
			string entryName = (string)_request["entry"] ?? "Run";

			var prepared = Prepare(code);
			var result = new JObject
			{
				["wrapped"] = prepared.Wrapped,
				["compiled"] = false,
				["ran"] = false,
			};

			var compilation = CSharpCompilation.Create(
				assemblyName: "UiCodeRunner_" + (++s_compileCount),
				syntaxTrees: new[] { prepared.Tree },
				references: References(),
				options: new CSharpCompilationOptions(
					OutputKind.DynamicallyLinkedLibrary,
					optimizationLevel: OptimizationLevel.Release));

			using var stream = new System.IO.MemoryStream();
			var emitResult = compilation.Emit(stream);

			var diagnostics = new JArray();
			foreach (var d in emitResult.Diagnostics)
			{
				if (d.Severity == DiagnosticSeverity.Hidden || d.Severity == DiagnosticSeverity.Info)
					continue;
				diagnostics.Add(DiagnosticJson(d, prepared));
			}
			if (diagnostics.Count > 0)
				result["diagnostics"] = diagnostics;

			result["compilationsThisDomain"] = s_compileCount;

			if (!emitResult.Success)
			{
				// The generated source only helps when the caller cannot see it, i.e. when we wrapped their
				// snippet. Handing it back on every failure would just be noise they already have.
				if (prepared.Wrapped)
					result["generatedSource"] = prepared.Source;
				return result;
			}

			result["compiled"] = true;
			if (validateOnly)
				return result;

			var assembly = Assembly.Load(stream.ToArray());
			var entry = FindEntryPoint(assembly, entryName);
			result["entryPoint"] = entry.DeclaringType?.FullName + "." + entry.Name;

			var logs = new JArray();
			Application.LogCallback logHandler = ( _condition, _stackTrace, _type ) =>
			{
				if (logs.Count >= MaxLogs)
					return;
				logs.Add(new JObject { ["severity"] = _type.ToString(), ["message"] = _condition });
			};

			object returned;
			Application.logMessageReceived += logHandler;
			try
			{
				returned = entry.Invoke(null, null);
			}
			catch (TargetInvocationException e)
			{
				// The reflection wrapper carries no information the caller wants; the inner exception is the
				// one that happened in their code.
				var inner = e.InnerException ?? e;
				result["logs"] = logs;
				result["error"] = new JObject
				{
					["type"] = inner.GetType().Name,
					["message"] = inner.Message,
					["stackTrace"] = inner.StackTrace,
				};
				return result;
			}
			finally
			{
				Application.logMessageReceived -= logHandler;
			}

			if (logs.Count >= MaxLogs)
				logs.Add(new JObject
				{
					["severity"] = "Log",
					["message"] = $"(truncated at {MaxLogs} entries — use get_console for the full picture)",
				});

			result["ran"] = true;
			result["logs"] = logs;
			result["result"] = ResultToken(returned);
			return result;
		}

		private readonly struct Prepared
		{
			public readonly SyntaxTree Tree;
			public readonly string Source;
			public readonly bool Wrapped;
			/// <summary>1-based line in the ORIGINAL snippet that generated line 2 corresponds to.</summary>
			public readonly int FirstBodyLine;

			public Prepared( SyntaxTree _tree, string _source, bool _wrapped, int _firstBodyLine )
			{
				Tree = _tree;
				Source = _source;
				Wrapped = _wrapped;
				FirstBodyLine = _firstBodyLine;
			}
		}

		/// <summary>
		/// Accepts both shapes a caller naturally writes: a full compilation unit with its own class, and a
		/// bare statement body ("return UiStyleConfig.Instance.Skins.Count;"). The second is wrapped, with the
		/// caller's own using directives hoisted out of the method body — left where they were they would be
		/// parsed as using STATEMENTS and fail for reasons that have nothing to do with the caller's mistake.
		/// </summary>
		private static Prepared Prepare( string _code )
		{
			var tree = CSharpSyntaxTree.ParseText(_code);
			var root = (CompilationUnitSyntax)tree.GetRoot();

			bool hasTypes = root.Members.Any(_m =>
				_m is BaseTypeDeclarationSyntax || _m is NamespaceDeclarationSyntax ||
				_m is FileScopedNamespaceDeclarationSyntax);

			if (hasTypes)
				return new Prepared(tree, _code, false, 0);

			var userUsings = new StringBuilder();
			foreach (var u in root.Usings)
				userUsings.Append(u.ToString()).Append(' ');

			// Line of the first thing that is not a using directive — what generated line 2 will hold.
			int firstBodyLine = root.Members.Count > 0
				? root.Members[0].GetLocation().GetLineSpan().StartLinePosition.Line + 1
				: 1;

			var body = new StringBuilder();
			foreach (var member in root.Members)
				body.Append(member.ToFullString());

			string source = DefaultUsings + userUsings + WrapperOpen + "\n" + body + WrapperClose;
			return new Prepared(CSharpSyntaxTree.ParseText(source), source, true, firstBodyLine);
		}

		private static JObject DiagnosticJson( Diagnostic _diagnostic, Prepared _prepared )
		{
			var span = _diagnostic.Location.GetLineSpan();
			int line = span.StartLinePosition.Line + 1;

			// Generated line 2 is the caller's FirstBodyLine, so their line = line - 2 + FirstBodyLine.
			// Anything on the single generated header line cannot be mapped and is reported as line 0.
			if (_prepared.Wrapped)
				line = line >= 2 ? line - 2 + _prepared.FirstBodyLine : 0;

			return new JObject
			{
				["severity"] = _diagnostic.Severity.ToString(),
				["id"] = _diagnostic.Id,
				["message"] = _diagnostic.GetMessage(CultureInfo.InvariantCulture),
				["line"] = line,
				["column"] = span.StartLinePosition.Character + 1,
			};
		}

		private static MethodInfo FindEntryPoint( Assembly _assembly, string _entryName )
		{
			var candidates = new List<MethodInfo>();
			foreach (var type in _assembly.GetTypes())
			{
				var method = type.GetMethod(_entryName,
					BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
					null, Type.EmptyTypes, null);
				if (method != null)
					candidates.Add(method);
			}

			if (candidates.Count == 1)
				return candidates[0];

			if (candidates.Count == 0)
				throw new Exception($"No entry point found. Provide a parameterless 'public static' method " +
					$"named '{_entryName}' (any return type), or pass bare statements and let them be wrapped.");

			string found = string.Join(", ", candidates.Select(_c => _c.DeclaringType?.FullName + "." + _c.Name));
			throw new Exception($"Ambiguous entry point: {candidates.Count} candidates named '{_entryName}' " +
				$"({found}). Leave exactly one, or name the one you mean with 'entry'.");
		}

		/// <summary>
		/// A snippet's return value is data the caller wants to read, not a formatted report. Anything the
		/// bridge's JSON knows natively travels as itself; a Unity object as the thing that identifies it
		/// (its asset path, else its name); everything else as its invariant string form.
		/// </summary>
		private static JToken ResultToken( object _value )
		{
			switch (_value)
			{
				case null: return JValue.CreateNull();
				case JToken token: return token;
				case string s: return s;
				case bool b: return b;
				case float f: return f;
				case double d: return d;
				case int i: return i;
				case long l: return l;
			}

			if (_value is UnityEngine.Object unityObject)
			{
				string path = UnityEditor.AssetDatabase.GetAssetPath(unityObject);
				return string.IsNullOrEmpty(path) ? unityObject.name : path;
			}

			if (_value.GetType().IsPrimitive || _value is decimal)
				return Convert.ToString(_value, CultureInfo.InvariantCulture);

			return _value.ToString();
		}

		private static MetadataReference[] References()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			if (s_references != null && s_referenceAssemblyCount == assemblies.Length)
				return s_references;

			var references = new List<MetadataReference>(assemblies.Length);
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var assembly in assemblies)
			{
				if (assembly.IsDynamic)
					continue;

				string location;
				try { location = assembly.Location; }
				catch { continue; }

				if (string.IsNullOrEmpty(location) || !seen.Add(location))
					continue;

				try { references.Add(MetadataReference.CreateFromFile(location)); }
				catch { /* a reference we cannot read is one the snippet cannot use — not fatal */ }
			}

			s_references = references.ToArray();
			s_referenceAssemblyCount = assemblies.Length;
			return s_references;
		}
	}
}
