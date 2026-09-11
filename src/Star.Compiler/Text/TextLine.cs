namespace Star.Compiler.Text;

/// <summary>
/// A line in a <see cref="SourceText"/> document. Line and column values are zero-based internally.
/// </summary>
public readonly record struct TextLine(int Start, int Length, int LineBreakLength)
{
    public int End => Start + Length;

    public int EndIncludingLineBreak => End + LineBreakLength;
}
