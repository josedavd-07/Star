using Star.Compiler.Text;

namespace Star.Compiler.Semantics;

/// <summary>A named declaration visible to semantic analysis.</summary>
public sealed record Symbol(string Name, SymbolKind Kind, string? TypeName, TextSpan Span);
