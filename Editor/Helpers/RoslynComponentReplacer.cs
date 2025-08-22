#define ROSLYN_VERBOSE
#if UITK_USE_ROSLYN || UNITY_6000_0_OR_NEWER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GuiToolkit.Debugging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GuiToolkit.Editor.Roslyn
{
	public static class RoslynComponentReplacer
	{
		/// <summary>
		/// Replaces all appearances of component type TA with TB:
		/// - Fields/properties/parameters/locals
		/// - Casts, typeof(TA), nameof(TA)
		/// - Generic type args (e.g., GetComponent&lt;TA&gt;(), TryGetComponent&lt;TA&gt;())
		/// - Attribute arguments using typeof(TA) (e.g., [RequireComponent(typeof(TA))])
		/// Also inserts a using for TB's namespace if needed (optional).
		/// </summary>
		public static string ReplaceComponent<TA, TB>
		(
			string _sourceCode,
			IEnumerable<MetadataReference> _references,
			bool _addUsingForTargetNamespace = true 
		)
			where TA : UnityEngine.Component
			where TB : UnityEngine.Component
		{
			if (_sourceCode == null) 
				throw new ArgumentNullException(nameof(_sourceCode));
			if (_references == null) 
				throw new ArgumentNullException(nameof(_references));

			var taMetadataName = typeof(TA).FullName;      // e.g. "UnityEngine.UI.Text"
			var tbMetadataName = typeof(TB).FullName;      // e.g. "TMPro.TMP_Text"
			var tbNamespace = typeof(TB).Namespace;        // e.g. "TMPro"
			var tbShortName = typeof(TB).Name;             // e.g. "TMP_Text"

			var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
			var syntaxTree = CSharpSyntaxTree.ParseText(_sourceCode, parseOptions);
			var compilation = CSharpCompilation.Create(
				assemblyName: "TempRewrite",
				syntaxTrees: new[] { syntaxTree },
				references: _references,
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

			var semanticModel = compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
			var taSymbol = compilation.GetTypeByMetadataName(taMetadataName);
			var tbSymbol = compilation.GetTypeByMetadataName(tbMetadataName);

			if (taSymbol == null)
				throw new InvalidOperationException($"Could not resolve TA type '{taMetadataName}'. Add proper MetadataReferences please.");
			if (tbSymbol == null)
				throw new InvalidOperationException($"Could not resolve TB type '{tbMetadataName}'. Add proper MetadataReferences please.");

			// 1) Replace all type references bound to TA -> TB.
			var rewriter = new TypeUseRewriter(semanticModel, taSymbol, tbShortName);
			var newRoot = rewriter.Visit(syntaxTree.GetRoot());

			// 2) Optionally ensure we can use the short name (insert using for TB namespace if missing).
			if (_addUsingForTargetNamespace && rewriter.Changed && !string.IsNullOrEmpty(tbNamespace))
			{
				newRoot = EnsureUsing(newRoot as CompilationUnitSyntax, tbNamespace);
			}

			return newRoot.ToFullString();
		}

		/// <summary>
		/// Rewriter that swaps any TypeSyntax (and certain IdentifierName in typeof/nameof/generics)
		/// whose symbol resolves to 'fromType' with an IdentifierName of 'toShortName'.
		/// </summary>
		private sealed class TypeUseRewriter : CSharpSyntaxRewriter
		{
			private readonly SemanticModel m_model;
			private readonly ITypeSymbol m_fromType;
			private readonly string m_toShortName;

			public bool Changed { get; private set; }

			public TypeUseRewriter( SemanticModel _model, ITypeSymbol _fromType, string _toShortName )
			{
				m_model = _model;
				m_fromType = _fromType;
				m_toShortName = _toShortName;
			}

			public override SyntaxNode VisitIdentifierName( IdentifierNameSyntax _node )
			{
				if (IsTypeReference(_node, out var symbolIsFromType) && symbolIsFromType)
				{
					Changed = true;
					var result = WithTrivia(_node, SyntaxFactory.IdentifierName(m_toShortName));
					LogVerbose($"Changed: {_node.ToFullString()} -> {result.ToFullString()}");
					return result;
				}
				
				return base.VisitIdentifierName(_node);
			}

			public override SyntaxNode VisitQualifiedName( QualifiedNameSyntax _node )
			{
				if (IsTypeReference(_node, out var symbolIsFromType) && symbolIsFromType)
				{
					Changed = true;
					var result = WithTrivia(_node, SyntaxFactory.IdentifierName(m_toShortName));
					LogVerbose($"Changed: {_node.ToFullString()} -> {result.ToFullString()}");
					return result;
				}
				
				return base.VisitQualifiedName(_node);
			}

			public override SyntaxNode VisitGenericName( GenericNameSyntax _node )
			{
				var newTypeArgs = _node.TypeArgumentList.Arguments;
				var changedLocal = false;

				var builder = new List<TypeSyntax>(newTypeArgs.Count);
				for (int i = 0; i < newTypeArgs.Count; i++)
				{
					var arg = newTypeArgs[i];
					if (IsTypeReference(arg, out var isFrom) && isFrom)
					{
						builder.Add(SyntaxFactory.IdentifierName(m_toShortName).WithTriviaFrom(arg));
						changedLocal = true;
						continue;
					}
					builder.Add((TypeSyntax)Visit(arg));
				}

				if (changedLocal)
				{
					Changed = true;
					
					var result = _node.WithTypeArgumentList(
						_node.TypeArgumentList.WithArguments(SyntaxFactory.SeparatedList(builder)));
					LogVerbose($"Changed: {_node.ToFullString()} -> {result.ToFullString()}");
					return result;
				}
				
				return base.VisitGenericName(_node);
			}

			public override SyntaxNode VisitCastExpression( CastExpressionSyntax _node )
			{
				var type = _node.Type;
				if (IsTypeReference(type, out var isFrom) && isFrom)
				{
					Changed = true;
					return _node.WithType(SyntaxFactory.IdentifierName(m_toShortName).WithTriviaFrom(type));
				}
				return base.VisitCastExpression(_node);
			}

			public override SyntaxNode VisitTypeOfExpression( TypeOfExpressionSyntax _node )
			{
				var type = _node.Type;
				if (IsTypeReference(type, out var isFrom) && isFrom)
				{
					Changed = true;
					return _node.WithType(SyntaxFactory.IdentifierName(m_toShortName).WithTriviaFrom(type));
				}
				return base.VisitTypeOfExpression(_node);
			}

			private static TNew WithTrivia<TOld, TNew>( TOld _oldNode, TNew _newNode )
				where TOld : SyntaxNode
				where TNew : SyntaxNode
			{
				return _newNode
					.WithLeadingTrivia(_oldNode.GetLeadingTrivia())
					.WithTrailingTrivia(_oldNode.GetTrailingTrivia());
			}

			private bool IsTypeReference( SyntaxNode _node, out bool _symbolIsFromType )
			{
				_symbolIsFromType = false;

				if (_node is TypeSyntax || _node is IdentifierNameSyntax || _node is QualifiedNameSyntax)
				{
					var symbolInfo = m_model.GetSymbolInfo(_node);
					var sym = symbolInfo.Symbol;

					if (sym == null && symbolInfo.CandidateSymbols.Length > 0)
						sym = symbolInfo.CandidateSymbols[0];

					if (sym is ITypeSymbol t)
					{
						_symbolIsFromType = SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, m_fromType)
							|| SymbolEqualityComparer.Default.Equals(t, m_fromType);
						return true;
					}

					if (sym is INamedTypeSymbol nts)
					{
						_symbolIsFromType = SymbolEqualityComparer.Default.Equals(nts, m_fromType);
						return true;
					}
				}
				return false;
			}
		}

		private static CompilationUnitSyntax EnsureUsing( CompilationUnitSyntax _root, string _namespaceName )
		{
			if (_root == null) 
				return null;
			
			if (_root.Usings.Any(u => string.Equals(u.Name.ToString(), _namespaceName, StringComparison.Ordinal)))
				return _root;

			var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(_namespaceName))
				.NormalizeWhitespace()
				.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

			// Keep usings sorted-ish: place after existing usings.
			return _root.WithUsings(_root.Usings.Add(usingDirective));
		}
		
		[Conditional("ROSLYN_VERBOSE")]
		public static void LogVerbose(string _s)
		{
			UnityEngine.Debug.Log($"---::: {DebugUtility.GetCallingClassAndMethod(false, true, 1)}: {_s}");
		}
	}
}
#endif
