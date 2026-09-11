using Star.Compiler.Parsing;
using Star.Compiler.Semantics;
using Star.Compiler.Text;

namespace Star.Compiler.Compilation;

/// <summary>
/// The single front-door for Star's platform-independent compiler front end.
/// It intentionally stops before runtime execution, IR generation, or target-specific work.
/// </summary>
public static class StarCompilation
{
    public static CompilationResult Create(SourceText source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var parse = StarParser.Parse(source);
        var semantics = StarSemanticAnalyzer.Analyze(parse.Root);
        var types = StarTypeChecker.Check(parse.Root);
        return new CompilationResult(source, parse.Root, semantics, types, parse.Diagnostics.AddRange(semantics.Diagnostics).AddRange(types.Diagnostics));
    }
}
