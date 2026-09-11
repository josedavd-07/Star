using System.Collections.Immutable;
using Star.Compiler.Diagnostics;
using Star.Compiler.Syntax;

namespace Star.Compiler.Semantics;

/// <summary>Diagnostics emitted while checking Star expression and declaration types.</summary>
public sealed record TypeCheckResult(ImmutableArray<Diagnostic> Diagnostics, ImmutableDictionary<ExpressionSyntax, string> ExpressionTypes)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}
