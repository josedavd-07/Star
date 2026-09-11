namespace StarCompiler.Runtime;

/// <summary>
/// The explicit non-null result of a Star operation whose declared result is Nova.
/// It keeps legacy expression evaluation non-null while preserving the fact that no user value was produced.
/// </summary>
public sealed class StarUnit
{
    private StarUnit() { }

    public static StarUnit Value { get; } = new();
}
