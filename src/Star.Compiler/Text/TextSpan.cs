namespace Star.Compiler.Text;

/// <summary>
/// An immutable half-open range within a source document.
/// </summary>
public readonly record struct TextSpan(int Start, int Length)
{
    public int End => Start + Length;

    public static TextSpan FromBounds(int start, int end)
    {
        if (start < 0 || end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        return new TextSpan(start, end - start);
    }
}
