using System.Collections.Immutable;
using Star.Compiler.Text;

namespace Star.Compiler.Diagnostics;

/// <summary>
/// Ordered diagnostic collector used during one compilation operation.
/// </summary>
public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics = [];

    public bool HasErrors => _diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    public ImmutableArray<Diagnostic> ToImmutable() => _diagnostics.ToImmutableArray();

    public void ReportError(string code, TextSpan span, string message) =>
        _diagnostics.Add(new Diagnostic(code, DiagnosticSeverity.Error, message, span));

    public void ReportWarning(string code, TextSpan span, string message) =>
        _diagnostics.Add(new Diagnostic(code, DiagnosticSeverity.Warning, message, span));
}
