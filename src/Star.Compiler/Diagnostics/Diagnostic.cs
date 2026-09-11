using Star.Compiler.Text;

namespace Star.Compiler.Diagnostics;

/// <summary>
/// A compiler message tied to an exact source range when one is available.
/// </summary>
public sealed record Diagnostic(
    string Code,
    DiagnosticSeverity Severity,
    string Message,
    TextSpan? Span = null);
