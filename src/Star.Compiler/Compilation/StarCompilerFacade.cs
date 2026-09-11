using Star.Compiler.Diagnostics;
using Star.Compiler.Text;

namespace Star.Compiler.Compilation;

/// <summary>
/// Stable file and text entry point for clients of the Star front end, including the CLI.
/// It performs compilation only; runtime execution remains a compatibility concern outside this facade.
/// </summary>
public static class StarCompilerFacade
{
    public static CompilationResult CompileText(string text, string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return StarCompilation.Create(SourceText.From(text, filePath));
    }

    public static CompilationResult CompileFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return CompileText(File.ReadAllText(filePath), filePath);
    }

    public static IEnumerable<string> FormatDiagnostics(CompilationResult compilation)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        return compilation.Diagnostics.Select(diagnostic => DiagnosticFormatter.Format(compilation.Source, diagnostic));
    }
}
