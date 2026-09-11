using System.Collections.Immutable;
using Star.Compiler.Diagnostics;
using Star.Compiler.Syntax;

namespace Star.Compiler.Parsing;

public sealed record ParseResult(CompilationUnitSyntax Root, ImmutableArray<Diagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}
