using System.Collections.Immutable;
using Star.Compiler.Lexing;
using Star.Compiler.Text;

namespace Star.Compiler.Syntax;

/// <summary>The complete, immutable syntax representation of one Star source document.</summary>
public sealed record CompilationUnitSyntax(
    ImmutableArray<StatementSyntax> Statements,
    StarToken EndOfFileToken,
    TextSpan Span) : SyntaxNode(SyntaxKind.CompilationUnit, Span);

public abstract record StatementSyntax(SyntaxKind StatementKind, TextSpan Span) : SyntaxNode(StatementKind, Span);

/// <summary>A brace-delimited group of Star statements.</summary>
public sealed record BlockSyntax(
    ImmutableArray<StatementSyntax> Statements,
    TextSpan Span) : SyntaxNode(SyntaxKind.Block, Span);

public sealed record TypeSyntax(StarToken Identifier) : SyntaxNode(SyntaxKind.Type, Identifier.Span);
public sealed record QualifiedNameSyntax(ImmutableArray<StarToken> Parts, TextSpan Span) : SyntaxNode(SyntaxKind.QualifiedName, Span);
public sealed record ParameterSyntax(TypeSyntax Type, StarToken Identifier, TextSpan Span) : SyntaxNode(SyntaxKind.Parameter, Span);
public sealed record NamespaceDeclarationSyntax(StarToken Keyword, QualifiedNameSyntax Name, TextSpan Span) : StatementSyntax(SyntaxKind.NamespaceDeclaration, Span);
public sealed record ConstellationDeclarationSyntax(ImmutableArray<StarToken> Modifiers, StarToken Keyword, StarToken Identifier, BlockSyntax Body, TextSpan Span) : StatementSyntax(SyntaxKind.ConstellationDeclaration, Span);
public sealed record FunctionDeclarationSyntax(ImmutableArray<StarToken> Modifiers, StarToken Keyword, StarToken Identifier, ImmutableArray<ParameterSyntax> Parameters, TypeSyntax? ReturnType, BlockSyntax Body, TextSpan Span) : StatementSyntax(SyntaxKind.FunctionDeclaration, Span);
public sealed record VariableDeclarationSyntax(ImmutableArray<StarToken> Modifiers, TypeSyntax Type, StarToken Identifier, StarToken? EqualsToken, ExpressionSyntax? Initializer, TextSpan Span) : StatementSyntax(SyntaxKind.VariableDeclaration, Span);
public sealed record PropertyDeclarationSyntax(ImmutableArray<StarToken> Modifiers, StarToken Keyword, TypeSyntax Type, StarToken Identifier, TextSpan Span) : StatementSyntax(SyntaxKind.PropertyDeclaration, Span);
public sealed record EmitStatementSyntax(StarToken Keyword, ExpressionSyntax Expression, TextSpan Span) : StatementSyntax(SyntaxKind.EmitStatement, Span);
public sealed record ReturnStatementSyntax(StarToken Keyword, ExpressionSyntax? Expression, TextSpan Span) : StatementSyntax(SyntaxKind.ReturnStatement, Span);
public sealed record WhenStatementSyntax(StarToken Keyword, ExpressionSyntax Condition, BlockSyntax Body, ImmutableArray<WhenClauseSyntax> ElseWhenClauses, OtherwiseClauseSyntax? OtherwiseClause, TextSpan Span) : StatementSyntax(SyntaxKind.WhenStatement, Span);
public sealed record WhenClauseSyntax(StarToken Keyword, ExpressionSyntax Condition, BlockSyntax Body, TextSpan Span) : SyntaxNode(SyntaxKind.ElseWhenClause, Span);
public sealed record OtherwiseClauseSyntax(StarToken Keyword, BlockSyntax Body, TextSpan Span) : SyntaxNode(SyntaxKind.OtherwiseClause, Span);
public sealed record WhileStatementSyntax(StarToken Keyword, ExpressionSyntax Condition, BlockSyntax Body, TextSpan Span) : StatementSyntax(SyntaxKind.WhileStatement, Span);
public sealed record OrbitStatementSyntax(StarToken Keyword, StatementSyntax? Initializer, ExpressionSyntax? Condition, StatementSyntax? Step, BlockSyntax Body, TextSpan Span) : StatementSyntax(SyntaxKind.OrbitStatement, Span);
public sealed record SpinWhileStatementSyntax(StarToken Keyword, BlockSyntax Body, ExpressionSyntax Condition, TextSpan Span) : StatementSyntax(SyntaxKind.SpinWhileStatement, Span);
public sealed record ExploreStatementSyntax(StarToken Keyword, StarToken Identifier, StarToken InKeyword, ExpressionSyntax Collection, BlockSyntax Body, TextSpan Span) : StatementSyntax(SyntaxKind.ExploreStatement, Span);
public sealed record AssignmentStatementSyntax(ExpressionSyntax Target, StarToken EqualsToken, ExpressionSyntax Expression, TextSpan Span) : StatementSyntax(SyntaxKind.AssignmentStatement, Span);
public sealed record ExpressionStatementSyntax(ExpressionSyntax Expression, TextSpan Span) : StatementSyntax(SyntaxKind.ExpressionStatement, Span);

public abstract record ExpressionSyntax(SyntaxKind ExpressionKind, TextSpan Span) : SyntaxNode(ExpressionKind, Span);
public sealed record LiteralExpressionSyntax(StarToken LiteralToken) : ExpressionSyntax(SyntaxKind.LiteralExpression, LiteralToken.Span);
public sealed record NameExpressionSyntax(StarToken Identifier) : ExpressionSyntax(SyntaxKind.NameExpression, Identifier.Span);
public sealed record ParenthesizedExpressionSyntax(StarToken OpenParen, ExpressionSyntax Expression, StarToken CloseParen, TextSpan Span) : ExpressionSyntax(SyntaxKind.ParenthesizedExpression, Span);
public sealed record UnaryExpressionSyntax(StarToken OperatorToken, ExpressionSyntax Operand, TextSpan Span) : ExpressionSyntax(SyntaxKind.UnaryExpression, Span);
public sealed record BinaryExpressionSyntax(ExpressionSyntax Left, StarToken OperatorToken, ExpressionSyntax Right, TextSpan Span) : ExpressionSyntax(SyntaxKind.BinaryExpression, Span);
public sealed record CallExpressionSyntax(ExpressionSyntax Target, StarToken OpenParen, ImmutableArray<ExpressionSyntax> Arguments, StarToken CloseParen, TextSpan Span) : ExpressionSyntax(SyntaxKind.CallExpression, Span);
public sealed record MemberAccessExpressionSyntax(ExpressionSyntax Target, StarToken DotToken, StarToken Identifier, TextSpan Span) : ExpressionSyntax(SyntaxKind.MemberAccessExpression, Span);
public sealed record IndexExpressionSyntax(ExpressionSyntax Target, StarToken OpenBracket, ExpressionSyntax Index, StarToken CloseBracket, TextSpan Span) : ExpressionSyntax(SyntaxKind.IndexExpression, Span);
public sealed record NewExpressionSyntax(StarToken Keyword, TypeSyntax Type, StarToken? OpenParen, ImmutableArray<ExpressionSyntax> Arguments, StarToken? CloseParen, TextSpan Span) : ExpressionSyntax(SyntaxKind.NewExpression, Span);
public sealed record ListExpressionSyntax(StarToken OpenBracket, ImmutableArray<ExpressionSyntax> Elements, StarToken CloseBracket, TextSpan Span) : ExpressionSyntax(SyntaxKind.ListExpression, Span);
