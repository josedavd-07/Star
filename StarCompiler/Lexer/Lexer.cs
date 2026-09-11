using Star.Compiler.Diagnostics;
using Star.Compiler.Lexing;
using Star.Compiler.Text;

namespace StarCompiler.Lexer;

/// <summary>Compatibility adapter for the Star 1.0 parser. New lexical logic lives in Star.Compiler.</summary>
public sealed class Lexer
{
    private readonly SourceText _source;

    public Lexer(string text) : this(SourceText.From(text)) { }
    public Lexer(SourceText source) => _source = source ?? throw new ArgumentNullException(nameof(source));
    public IReadOnlyList<Diagnostic> Diagnostics { get; private set; } = [];

    public List<Token> Tokenize()
    {
        var lexer = new StarLexer(_source);
        var tokens = lexer.Tokenize();
        Diagnostics = lexer.Diagnostics;
        return tokens.Select(token => new Token(Map(token.Kind), token.Value, token.Span)).ToList();
    }

    private static TokenType Map(StarTokenKind kind) => kind switch
    {
        StarTokenKind.Keyword => TokenType.Keyword, StarTokenKind.Type => TokenType.Type, StarTokenKind.String => TokenType.String,
        StarTokenKind.Number => TokenType.Number, StarTokenKind.Char => TokenType.Char, StarTokenKind.Identifier => TokenType.Identifier,
        StarTokenKind.LParen => TokenType.LParen, StarTokenKind.RParen => TokenType.RParen, StarTokenKind.Equals => TokenType.Equals,
        StarTokenKind.Plus => TokenType.Plus, StarTokenKind.Minus => TokenType.Minus, StarTokenKind.Multiply => TokenType.Multiply,
        StarTokenKind.Divide => TokenType.Divide, StarTokenKind.EqualsEquals => TokenType.EqualsEquals, StarTokenKind.NotEquals => TokenType.NotEquals,
        StarTokenKind.LessThan => TokenType.LessThan, StarTokenKind.GreaterThan => TokenType.GreaterThan, StarTokenKind.And => TokenType.And,
        StarTokenKind.Or => TokenType.Or, StarTokenKind.LBrace => TokenType.LBrace, StarTokenKind.RBrace => TokenType.RBrace,
        StarTokenKind.LBracket => TokenType.LBracket, StarTokenKind.RBracket => TokenType.RBracket, StarTokenKind.Comma => TokenType.Comma,
        StarTokenKind.Semicolon => TokenType.Semicolon, StarTokenKind.Dot => TokenType.Dot, StarTokenKind.Arrow => TokenType.Arrow,
        StarTokenKind.EndOfFile => TokenType.EOF, _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
