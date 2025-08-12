#if UITK_USE_ROSLYN || UNITY_6000_0_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
	public static string ReplaceComponent<TA, TB>(
		string sourceCode,
		IEnumerable<MetadataReference> references,
		bool addUsingForTargetNamespace = true )
		where TA : UnityEngine.Component
		where TB : UnityEngine.Component
	{
		if (sourceCode == null) throw new ArgumentNullException(nameof(sourceCode));
		if (references == null) throw new ArgumentNullException(nameof(references));

		var taMetadataName = typeof(TA).FullName;      // e.g. "UnityEngine.UI.Text"
		var tbMetadataName = typeof(TB).FullName;      // e.g. "TMPro.TMP_Text"
		var tbNamespace = typeof(TB).Namespace;        // e.g. "TMPro"
		var tbShortName = typeof(TB).Name;             // e.g. "TMP_Text"

		var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, parseOptions);
		var compilation = CSharpCompilation.Create(
			assemblyName: "TempRewrite",
			syntaxTrees: new[] { syntaxTree },
			references: references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var semanticModel = compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
		var taSymbol = compilation.GetTypeByMetadataName(taMetadataName);
		var tbSymbol = compilation.GetTypeByMetadataName(tbMetadataName);

		if (taSymbol == null)
			throw new InvalidOperationException($"Could not resolve TA type '{taMetadataName}'. Add proper MetadataReferences.");
		if (tbSymbol == null)
			throw new InvalidOperationException($"Could not resolve TB type '{tbMetadataName}'. Add proper MetadataReferences.");

		// 1) Replace all type references bound to TA -> TB.
		var rewriter = new TypeUseRewriter(semanticModel, taSymbol, tbShortName);
		var newRoot = rewriter.Visit(syntaxTree.GetRoot());

		// 2) Optionally ensure we can use the short name (insert using for TB namespace if missing).
		if (addUsingForTargetNamespace && rewriter.Changed && !string.IsNullOrEmpty(tbNamespace))
		{
			newRoot = EnsureUsing(newRoot as CompilationUnitSyntax, tbNamespace);
		}
		
		// 3) Return pretty-printed code.
		return newRoot.NormalizeWhitespace().ToFullString();
	}

	/// <summary>
	/// Rewriter that swaps any TypeSyntax (and certain IdentifierName in typeof/nameof/generics)
	/// whose symbol resolves to 'fromType' with an IdentifierName of 'toShortName'.
	/// </summary>
	private sealed class TypeUseRewriter : CSharpSyntaxRewriter
	{
		private readonly SemanticModel _model;
		private readonly ITypeSymbol _fromType;
		private readonly string _toShortName;

		public bool Changed { get; private set; }

		public TypeUseRewriter( SemanticModel model, ITypeSymbol fromType, string toShortName )
		{
			_model = model;
			_fromType = fromType;
			_toShortName = toShortName;
		}

		public override SyntaxNode VisitIdentifierName( IdentifierNameSyntax node )
		{
			if (IsTypeReference(node, out var symbolIsFromType) && symbolIsFromType)
			{
				Changed = true;
				return WithTrivia(node, SyntaxFactory.IdentifierName(_toShortName));
			}
			return base.VisitIdentifierName(node);
		}

		public override SyntaxNode VisitQualifiedName( QualifiedNameSyntax node )
		{
			if (IsTypeReference(node, out var symbolIsFromType) && symbolIsFromType)
			{
				Changed = true;
				return WithTrivia(node, SyntaxFactory.IdentifierName(_toShortName));
			}
			return base.VisitQualifiedName(node);
		}

		public override SyntaxNode VisitGenericName( GenericNameSyntax node )
		{
			var newTypeArgs = node.TypeArgumentList.Arguments;
			var changedLocal = false;

			var builder = new List<TypeSyntax>(newTypeArgs.Count);
			for (int i = 0; i < newTypeArgs.Count; i++)
			{
				var arg = newTypeArgs[i];
				if (IsTypeReference(arg, out var isFrom) && isFrom)
				{
					builder.Add(SyntaxFactory.IdentifierName(_toShortName).WithTriviaFrom(arg));
					changedLocal = true;
					continue;
				}
				builder.Add((TypeSyntax)Visit(arg));
			}

			if (changedLocal)
			{
				Changed = true;
				return node.WithTypeArgumentList(
					node.TypeArgumentList.WithArguments(SyntaxFactory.SeparatedList(builder)));
			}
			return base.VisitGenericName(node);
		}

		public override SyntaxNode VisitCastExpression( CastExpressionSyntax node )
		{
			var type = node.Type;
			if (IsTypeReference(type, out var isFrom) && isFrom)
			{
				Changed = true;
				return node.WithType(SyntaxFactory.IdentifierName(_toShortName).WithTriviaFrom(type));
			}
			return base.VisitCastExpression(node);
		}

		public override SyntaxNode VisitTypeOfExpression( TypeOfExpressionSyntax node )
		{
			var type = node.Type;
			if (IsTypeReference(type, out var isFrom) && isFrom)
			{
				Changed = true;
				return node.WithType(SyntaxFactory.IdentifierName(_toShortName).WithTriviaFrom(type));
			}
			return base.VisitTypeOfExpression(node);
		}

		private static TNew WithTrivia<TOld, TNew>( TOld oldNode, TNew newNode )
			where TOld : SyntaxNode
			where TNew : SyntaxNode
		{
			return newNode
				.WithLeadingTrivia(oldNode.GetLeadingTrivia())
				.WithTrailingTrivia(oldNode.GetTrailingTrivia());
		}

		private bool IsTypeReference( SyntaxNode node, out bool symbolIsFromType )
		{
			symbolIsFromType = false;

			if (node is TypeSyntax || node is IdentifierNameSyntax || node is QualifiedNameSyntax)
			{
				var symbolInfo = _model.GetSymbolInfo(node);
				var sym = symbolInfo.Symbol;

				if (sym == null && symbolInfo.CandidateSymbols.Length > 0)
					sym = symbolInfo.CandidateSymbols[0];

				if (sym is ITypeSymbol t)
				{
					symbolIsFromType = SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, _fromType)
						|| SymbolEqualityComparer.Default.Equals(t, _fromType);
					return true;
				}

				if (sym is INamedTypeSymbol nts)
				{
					symbolIsFromType = SymbolEqualityComparer.Default.Equals(nts, _fromType);
					return true;
				}
			}
			return false;
		}
	}

	private static CompilationUnitSyntax EnsureUsing( CompilationUnitSyntax root, string namespaceName )
	{
		if (root == null) return null;
		if (root.Usings.Any(u => string.Equals(u.Name.ToString(), namespaceName, StringComparison.Ordinal)))
			return root;

		var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
			.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

		// Keep usings sorted-ish: place after existing usings.
		return root.WithUsings(root.Usings.Add(usingDirective));
	}
}
#endif
