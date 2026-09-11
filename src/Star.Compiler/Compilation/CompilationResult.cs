using System.Collections.Immutable;
using Star.Compiler.Diagnostics;
using Star.Compiler.Semantics;
using Star.Compiler.Syntax;
using Star.Compiler.Text;

namespace Star.Compiler.Compilation;

/// <summary>The immutable output of Star's front-end pipeline for one source document.</summary>
public sealed record CompilationResult(
    SourceText Source,
    CompilationUnitSyntax Syntax,
    SemanticResult Semantics,
    TypeCheckResult Types,
    ImmutableArray<Diagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}
