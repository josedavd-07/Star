using System.Collections.Immutable;
using Star.Compiler.Diagnostics;
using Star.Compiler.Syntax;

namespace Star.Compiler.Semantics;

/// <summary>The symbols and diagnostics produced without executing a Star program.</summary>
public sealed record SemanticResult(Scope GlobalScope, ImmutableArray<Diagnostic> Diagnostics, ImmutableDictionary<SyntaxNode, Symbol> Symbols)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}
