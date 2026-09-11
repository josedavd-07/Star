using Star.Compiler.Text;

namespace Star.Compiler.Lexing;

/// <summary>A lexical unit in Star source code, including its exact original location.</summary>
public sealed record StarToken(StarTokenKind Kind, string Value, TextSpan Span);
