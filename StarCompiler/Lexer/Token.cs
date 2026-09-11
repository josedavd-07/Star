using Star.Compiler.Text;

namespace StarCompiler.Lexer;

public enum TokenType
{
    Keyword,    // Emit, EmitLn
    Type,       // Int, String, Double, Float, Char, Bool, Nova, Object
    String,     // "Hello"
    Number,     // 42, 3.14
    Char,       // 'A'
    Identifier, // nombres de variables
    LParen,     // (
    RParen,     // )
    Equals,     // =
    Plus,       // +
    Minus,      // -
    Multiply,   // *
    Divide,     // /
    EqualsEquals, // ==
    NotEquals,    // !=
    LessThan,     // <
    GreaterThan,  // >
    And,          // &&
    Or,           // ||
    LBrace,       // {
    RBrace,       // }
    LBracket,     // [
    RBracket,     // ]
    Comma,        // ,
    Semicolon,    // ;
    Dot,          // .
    Arrow,        // -> (for function return types)
    EOF
}

public class Token
{
    public TokenType Type { get; }
    public string Value { get; }
    public TextSpan Span { get; }

    public Token(TokenType type, string value)
        : this(type, value, new TextSpan(0, value.Length))
    {
    }

    public Token(TokenType type, string value, TextSpan span)
    {
        Type = type;
        Value = value;
        Span = span;
    }
}
