using System.Collections.Immutable;
using System.Text;
using Star.Compiler.Diagnostics;
using Star.Compiler.Text;

namespace Star.Compiler.Lexing;

/// <summary>Converts Star source into tokens and collects recoverable lexical diagnostics.</summary>
public sealed class StarLexer
{
    private static readonly HashSet<string> Keywords = ["Emit", "EmitLn", "true", "false", "When", "ElseWhen", "Otherwise", "Orbit", "While", "SpinWhile", "Explore", "in", "StarFunction", "return", "Constellation", "new", "this", "StarName", "Public", "Protected", "Private", "Static", "Read", "Property"];
    private static readonly HashSet<string> Types = ["Int", "String", "Double", "Float", "Char", "Bool", "Nova", "Object", "Galaxy"];
    private readonly SourceText _source;
    private readonly DiagnosticBag _diagnostics = new();
    private int _position;

    public StarLexer(SourceText source) => _source = source ?? throw new ArgumentNullException(nameof(source));
    public ImmutableArray<Diagnostic> Diagnostics => _diagnostics.ToImmutable();

    public ImmutableArray<StarToken> Tokenize()
    {
        var tokens = ImmutableArray.CreateBuilder<StarToken>();
        while (_position < _source.Length)
        {
            if (char.IsWhiteSpace(Current)) { _position++; continue; }
            if (IsLineComment()) { SkipLineComment(); continue; }
            if (IsBlockComment()) { SkipBlockComment(); continue; }
            if (Current == '"') { tokens.Add(ReadString()); continue; }
            if (Current == '\'') { tokens.Add(ReadChar()); continue; }
            if (char.IsDigit(Current)) { tokens.Add(ReadNumber()); continue; }
            if (char.IsLetter(Current)) { tokens.Add(ReadWord()); continue; }
            if (TryReadPunctuation(out var punctuation)) { tokens.Add(punctuation); continue; }
            _diagnostics.ReportError("STR1001", new TextSpan(_position, 1), $"Unexpected character '{Current}'.");
            _position++;
        }
        tokens.Add(new StarToken(StarTokenKind.EndOfFile, string.Empty, new TextSpan(_position, 0)));
        return tokens.ToImmutable();
    }

    private char Current => _source[_position];
    private bool IsLineComment() => (Current == '*' && Peek(1) == '*') || (Current == '/' && Peek(1) == '/');
    private bool IsBlockComment() => Current == '/' && Peek(1) == '*';
    private char? Peek(int offset) => _position + offset < _source.Length ? _source[_position + offset] : null;
    private void SkipLineComment() { _position += 2; while (_position < _source.Length && Current != '\n') _position++; }
    private void SkipBlockComment()
    {
        var start = _position; _position += 2;
        while (_position < _source.Length) { if (Current == '*' && Peek(1) == '/') { _position += 2; return; } _position++; }
        _diagnostics.ReportError("STR1002", TextSpan.FromBounds(start, _position), "Unterminated block comment.");
    }
    private StarToken ReadString()
    {
        var start = _position++; var value = new StringBuilder();
        while (_position < _source.Length && Current != '"') { value.Append(Current); _position++; }
        if (_position == _source.Length) _diagnostics.ReportError("STR1003", TextSpan.FromBounds(start, _position), "Unterminated string literal."); else _position++;
        return new StarToken(StarTokenKind.String, value.ToString(), TextSpan.FromBounds(start, _position));
    }
    private StarToken ReadChar()
    {
        var start = _position++;
        if (_position >= _source.Length) { _diagnostics.ReportError("STR1004", new TextSpan(start, 1), "Unterminated character literal."); return new StarToken(StarTokenKind.Char, string.Empty, new TextSpan(start, 1)); }
        var value = Current.ToString(); _position++;
        if (_position >= _source.Length || Current != '\'') { _diagnostics.ReportError("STR1004", TextSpan.FromBounds(start, _position), "Character literals must contain one character and a closing quote."); return new StarToken(StarTokenKind.Char, value, TextSpan.FromBounds(start, _position)); }
        _position++; return new StarToken(StarTokenKind.Char, value, TextSpan.FromBounds(start, _position));
    }
    private StarToken ReadNumber() { var start = _position; while (_position < _source.Length && (char.IsDigit(Current) || Current == '.')) _position++; return CreateToken(StarTokenKind.Number, start); }
    private StarToken ReadWord()
    {
        var start = _position; while (_position < _source.Length && (char.IsLetterOrDigit(Current) || Current == '_')) _position++;
        var value = _source.ToString(TextSpan.FromBounds(start, _position));
        var kind = Keywords.Contains(value) ? StarTokenKind.Keyword : Types.Contains(value) ? StarTokenKind.Type : StarTokenKind.Identifier;
        return new StarToken(kind, value, TextSpan.FromBounds(start, _position));
    }
    private bool TryReadPunctuation(out StarToken token)
    {
        var start = _position;
        var kind = Current switch
        {
            '(' => StarTokenKind.LParen, ')' => StarTokenKind.RParen, '{' => StarTokenKind.LBrace, '}' => StarTokenKind.RBrace, '[' => StarTokenKind.LBracket, ']' => StarTokenKind.RBracket, ',' => StarTokenKind.Comma, ';' => StarTokenKind.Semicolon, '.' => StarTokenKind.Dot, '+' => StarTokenKind.Plus, '*' => StarTokenKind.Multiply, '/' => StarTokenKind.Divide, '<' => StarTokenKind.LessThan, '>' => StarTokenKind.GreaterThan,
            '=' when Peek(1) == '=' => StarTokenKind.EqualsEquals, '=' => StarTokenKind.Equals, '!' when Peek(1) == '=' => StarTokenKind.NotEquals, '&' when Peek(1) == '&' => StarTokenKind.And, '|' when Peek(1) == '|' => StarTokenKind.Or, '-' when Peek(1) == '>' => StarTokenKind.Arrow, '-' => StarTokenKind.Minus, _ => default
        };
        if (kind == default && Current != '(') { token = null!; return false; }
        var width = kind is StarTokenKind.EqualsEquals or StarTokenKind.NotEquals or StarTokenKind.And or StarTokenKind.Or or StarTokenKind.Arrow ? 2 : 1;
        _position += width; token = new StarToken(kind, _source.ToString(new TextSpan(start, width)), new TextSpan(start, width)); return true;
    }
    private StarToken CreateToken(StarTokenKind kind, int start) => new(kind, _source.ToString(TextSpan.FromBounds(start, _position)), TextSpan.FromBounds(start, _position));
}
