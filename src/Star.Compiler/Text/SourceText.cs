using System.Collections.Immutable;

namespace Star.Compiler.Text;

/// <summary>
/// Source code plus its line map. This is the single location model for the future lexer, parser and diagnostics.
/// </summary>
public sealed class SourceText
{
    private readonly ImmutableArray<TextLine> _lines;

    private SourceText(string text, string? filePath)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        FilePath = filePath;
        _lines = ParseLines(text);
    }

    public string Text { get; }

    public string? FilePath { get; }

    public int Length => Text.Length;

    public ImmutableArray<TextLine> Lines => _lines;

    public char this[int index] => Text[index];

    public static SourceText From(string text, string? filePath = null) => new(text, filePath);

    public string ToString(TextSpan span)
    {
        ValidateSpan(span);
        return Text.Substring(span.Start, span.Length);
    }

    public (int Line, int Column) GetLineColumn(int position)
    {
        if (position < 0 || position > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        var low = 0;
        var high = _lines.Length - 1;
        while (low <= high)
        {
            var middle = low + ((high - low) / 2);
            var line = _lines[middle];
            if (position < line.Start)
            {
                high = middle - 1;
            }
            else if (position >= line.EndIncludingLineBreak && middle < _lines.Length - 1)
            {
                low = middle + 1;
            }
            else
            {
                return (middle, position - line.Start);
            }
        }

        var last = _lines[^1];
        return (_lines.Length - 1, position - last.Start);
    }

    private void ValidateSpan(TextSpan span)
    {
        if (span.Start < 0 || span.Length < 0 || span.End > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(span));
        }
    }

    private static ImmutableArray<TextLine> ParseLines(string text)
    {
        var lines = ImmutableArray.CreateBuilder<TextLine>();
        var lineStart = 0;
        var position = 0;

        while (position < text.Length)
        {
            var lineBreakLength = GetLineBreakLength(text, position);
            if (lineBreakLength == 0)
            {
                position++;
                continue;
            }

            lines.Add(new TextLine(lineStart, position - lineStart, lineBreakLength));
            position += lineBreakLength;
            lineStart = position;
        }

        lines.Add(new TextLine(lineStart, position - lineStart, 0));
        return lines.ToImmutable();
    }

    private static int GetLineBreakLength(string text, int position) =>
        text[position] switch
        {
            '\r' when position + 1 < text.Length && text[position + 1] == '\n' => 2,
            '\r' or '\n' => 1,
            _ => 0
        };
}
