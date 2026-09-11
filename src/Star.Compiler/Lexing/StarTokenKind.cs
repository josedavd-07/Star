namespace Star.Compiler.Lexing;

/// <summary>The stable lexical vocabulary of Star.</summary>
public enum StarTokenKind
{
    Keyword, Type, String, Number, Char, Identifier,
    LParen, RParen, Equals, Plus, Minus, Multiply, Divide,
    EqualsEquals, NotEquals, LessThan, GreaterThan, And, Or,
    LBrace, RBrace, LBracket, RBracket, Comma, Semicolon, Dot, Arrow,
    EndOfFile
}
