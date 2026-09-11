using Star.Compiler.Text;

namespace Star.Compiler.Diagnostics;

public static class DiagnosticFormatter
{
    public static string Format(SourceText source, Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(diagnostic);
        var location = diagnostic.Span is { } span ? $"{source.FilePath ?? "<source>"}({source.GetLineColumn(span.Start).Line + 1},{source.GetLineColumn(span.Start).Column + 1})" : source.FilePath ?? "<source>";
        return $"{location}: {diagnostic.Severity.ToString().ToLowerInvariant()} {diagnostic.Code}: {diagnostic.Message}";
    }
}
