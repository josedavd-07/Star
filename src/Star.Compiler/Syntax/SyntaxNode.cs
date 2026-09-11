using Star.Compiler.Text;

namespace Star.Compiler.Syntax;

/// <summary>Base type for immutable syntax. Syntax nodes never execute code.</summary>
public abstract record SyntaxNode(SyntaxKind Kind, TextSpan Span);
