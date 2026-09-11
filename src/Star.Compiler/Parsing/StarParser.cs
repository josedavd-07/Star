using System.Collections.Immutable;
using Star.Compiler.Diagnostics;
using Star.Compiler.Lexing;
using Star.Compiler.Syntax;
using Star.Compiler.Text;

namespace Star.Compiler.Parsing;

/// <summary>Builds Star's immutable declaration and expression tree; it never executes source code.</summary>
public sealed class StarParser
{
    private readonly ImmutableArray<StarToken> _tokens;
    private readonly DiagnosticBag _diagnostics = new();
    private int _position;

    private StarParser(ImmutableArray<StarToken> tokens) => _tokens = tokens;

    public static ParseResult Parse(SourceText source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var lexer = new StarLexer(source);
        var parser = new StarParser(lexer.Tokenize());
        return new ParseResult(parser.ParseCompilationUnit(), lexer.Diagnostics.AddRange(parser._diagnostics.ToImmutable()));
    }

    private StarToken Current => _tokens[Math.Min(_position, _tokens.Length - 1)];
    private StarToken Previous => _tokens[Math.Max(0, _position - 1)];
    private StarToken Peek(int offset = 1) => _tokens[Math.Min(_position + offset, _tokens.Length - 1)];
    private StarToken Consume() => _tokens[_position++];
    private bool Is(StarTokenKind kind) => Current.Kind == kind;
    private bool IsKeyword(string value) => Current.Kind == StarTokenKind.Keyword && Current.Value == value;

    private CompilationUnitSyntax ParseCompilationUnit()
    {
        var statements = ImmutableArray.CreateBuilder<StatementSyntax>();
        while (!Is(StarTokenKind.EndOfFile))
        {
            if (Is(StarTokenKind.RBrace)) { Report("STR2001", Current, "Unexpected closing '}'."); Consume(); continue; }
            if (Is(StarTokenKind.Semicolon)) { Consume(); continue; }
            statements.Add(ParseStatement());
        }
        var eof = Consume();
        return new CompilationUnitSyntax(statements.ToImmutable(), eof, TextSpan.FromBounds(0, eof.Span.End));
    }

    private StatementSyntax ParseStatement()
    {
        var modifiers = ParseModifiers();
        if (IsKeyword("Property")) return ParseProperty(modifiers);
        if (Is(StarTokenKind.Type) || IsCustomTypeDeclaration()) return ParseVariableDeclaration(modifiers);
        if (IsKeyword("StarName")) return ParseNamespace();
        if (IsKeyword("Constellation")) return ParseConstellation(modifiers);
        if (IsKeyword("StarFunction")) return ParseFunction(modifiers);
        if (IsKeyword("Emit") || IsKeyword("EmitLn")) return ParseEmit();
        if (IsKeyword("return")) return ParseReturn();
        if (IsKeyword("When")) return ParseWhen();
        if (IsKeyword("While")) return ParseWhile();
        if (IsKeyword("Orbit")) return ParseOrbit();
        if (IsKeyword("SpinWhile")) return ParseSpinWhile();
        if (IsKeyword("Explore")) return ParseExplore();

        if (modifiers.Length > 0)
            Report("STR2003", modifiers[0], "A member modifier must precede a Constellation, StarFunction, or typed field.");

        var expression = ParseExpression();
        if (Is(StarTokenKind.Equals))
        {
            var equals = Consume();
            var value = ParseExpression();
            ConsumeStatementTerminator();
            return new AssignmentStatementSyntax(expression, equals, value, TextSpan.FromBounds(expression.Span.Start, value.Span.End));
        }
        ConsumeStatementTerminator();
        return new ExpressionStatementSyntax(expression, expression.Span);
    }

    private ImmutableArray<StarToken> ParseModifiers()
    {
        var result = ImmutableArray.CreateBuilder<StarToken>();
        while (IsKeyword("Public") || IsKeyword("Private") || IsKeyword("Protected") || IsKeyword("Static") || IsKeyword("Read")) result.Add(Consume());
        return result.ToImmutable();
    }

    // A Constellation name is lexed as an identifier. Two adjacent identifiers are therefore a typed declaration,
    // for example: Engine engine = new Engine();
    private bool IsCustomTypeDeclaration() => Current.Kind == StarTokenKind.Identifier && Peek().Kind == StarTokenKind.Identifier;

    private NamespaceDeclarationSyntax ParseNamespace()
    {
        var keyword = Consume();
        var parts = ImmutableArray.CreateBuilder<StarToken>();
        parts.Add(ExpectName("Expected a name after StarName."));
        while (Is(StarTokenKind.Dot)) { Consume(); parts.Add(ExpectName("Expected a name after '.'.")); }
        ConsumeStatementTerminator();
        var span = TextSpan.FromBounds(keyword.Span.Start, parts[^1].Span.End);
        return new NamespaceDeclarationSyntax(keyword, new QualifiedNameSyntax(parts.ToImmutable(), TextSpan.FromBounds(parts[0].Span.Start, parts[^1].Span.End)), span);
    }

    private ConstellationDeclarationSyntax ParseConstellation(ImmutableArray<StarToken> modifiers)
    {
        var keyword = Consume();
        var name = ExpectName("Expected a Constellation name.");
        var body = ParseRequiredBlock("Expected '{' after the Constellation name.");
        return new ConstellationDeclarationSyntax(modifiers, keyword, name, body, TextSpan.FromBounds((modifiers.Length > 0 ? modifiers[0] : keyword).Span.Start, body.Span.End));
    }

    private FunctionDeclarationSyntax ParseFunction(ImmutableArray<StarToken> modifiers)
    {
        var keyword = Consume();
        var name = ExpectName("Expected a StarFunction name.");
        Expect(StarTokenKind.LParen, "Expected '(' after the StarFunction name.");
        var parameters = ImmutableArray.CreateBuilder<ParameterSyntax>();
        while (!Is(StarTokenKind.RParen) && !Is(StarTokenKind.EndOfFile))
        {
            var type = ParseType("Expected parameter type.");
            var parameterName = ExpectName("Expected parameter name.");
            parameters.Add(new ParameterSyntax(type, parameterName, TextSpan.FromBounds(type.Span.Start, parameterName.Span.End)));
            if (!Is(StarTokenKind.Comma)) break;
            Consume();
        }
        Expect(StarTokenKind.RParen, "Expected ')' after parameters.");
        TypeSyntax? returnType = null;
        if (Is(StarTokenKind.Arrow)) { Consume(); returnType = ParseType("Expected a return type after '->'."); }
        var body = ParseRequiredBlock("Expected '{' after the StarFunction signature.");
        return new FunctionDeclarationSyntax(modifiers, keyword, name, parameters.ToImmutable(), returnType, body, TextSpan.FromBounds((modifiers.Length > 0 ? modifiers[0] : keyword).Span.Start, body.Span.End));
    }

    private VariableDeclarationSyntax ParseVariableDeclaration(ImmutableArray<StarToken> modifiers)
    {
        var type = ParseType("Expected a variable type.");
        var name = ExpectName("Expected a variable name.");
        StarToken? equals = null;
        ExpressionSyntax? initializer = null;
        if (Is(StarTokenKind.Equals)) { equals = Consume(); initializer = ParseExpression(); }
        ConsumeStatementTerminator();
        return new VariableDeclarationSyntax(modifiers, type, name, equals, initializer, TextSpan.FromBounds((modifiers.Length > 0 ? modifiers[0] : type.Identifier).Span.Start, (initializer?.Span.End ?? name.Span.End)));
    }

    private PropertyDeclarationSyntax ParseProperty(ImmutableArray<StarToken> modifiers)
    {
        var keyword = Consume();
        var type = ParseType("Expected a property type.");
        var name = ExpectName("Expected a property name.");
        ConsumeStatementTerminator();
        return new PropertyDeclarationSyntax(modifiers, keyword, type, name, TextSpan.FromBounds((modifiers.Length > 0 ? modifiers[0] : keyword).Span.Start, name.Span.End));
    }

    private EmitStatementSyntax ParseEmit()
    {
        var keyword = Consume();
        var parenthesized = Is(StarTokenKind.LParen);
        if (parenthesized) Consume();
        var expression = ParseExpression();
        if (parenthesized) Expect(StarTokenKind.RParen, "Expected ')' after Emit expression.");
        ConsumeStatementTerminator();
        return new EmitStatementSyntax(keyword, expression, TextSpan.FromBounds(keyword.Span.Start, expression.Span.End));
    }

    private ReturnStatementSyntax ParseReturn()
    {
        var keyword = Consume();
        ExpressionSyntax? expression = Is(StarTokenKind.Semicolon) || Is(StarTokenKind.RBrace) ? null : ParseExpression();
        ConsumeStatementTerminator();
        return new ReturnStatementSyntax(keyword, expression, TextSpan.FromBounds(keyword.Span.Start, expression?.Span.End ?? keyword.Span.End));
    }

    private WhenStatementSyntax ParseWhen()
    {
        var keyword = Consume(); var condition = ParseParenthesizedCondition("When"); var body = ParseRequiredBlock("Expected '{' after When condition.");
        var clauses = ImmutableArray.CreateBuilder<WhenClauseSyntax>();
        while (IsKeyword("ElseWhen")) { var elseKeyword = Consume(); var elseCondition = ParseParenthesizedCondition("ElseWhen"); var elseBody = ParseRequiredBlock("Expected '{' after ElseWhen condition."); clauses.Add(new WhenClauseSyntax(elseKeyword, elseCondition, elseBody, TextSpan.FromBounds(elseKeyword.Span.Start, elseBody.Span.End))); }
        OtherwiseClauseSyntax? otherwise = null;
        if (IsKeyword("Otherwise")) { var otherwiseKeyword = Consume(); var otherwiseBody = ParseRequiredBlock("Expected '{' after Otherwise."); otherwise = new OtherwiseClauseSyntax(otherwiseKeyword, otherwiseBody, TextSpan.FromBounds(otherwiseKeyword.Span.Start, otherwiseBody.Span.End)); }
        var end = otherwise?.Span.End ?? (clauses.Count > 0 ? clauses[^1].Span.End : body.Span.End);
        return new WhenStatementSyntax(keyword, condition, body, clauses.ToImmutable(), otherwise, TextSpan.FromBounds(keyword.Span.Start, end));
    }

    private WhileStatementSyntax ParseWhile()
    {
        var keyword = Consume(); var condition = ParseParenthesizedCondition("While"); var body = ParseRequiredBlock("Expected '{' after While condition.");
        return new WhileStatementSyntax(keyword, condition, body, TextSpan.FromBounds(keyword.Span.Start, body.Span.End));
    }

    private OrbitStatementSyntax ParseOrbit()
    {
        var keyword = Consume();
        Expect(StarTokenKind.LParen, "Expected '(' after Orbit.");
        StatementSyntax? initializer = null;
        if (!Is(StarTokenKind.Semicolon)) initializer = ParseForClauseStatement();
        Expect(StarTokenKind.Semicolon, "Expected ';' after Orbit initializer.");
        ExpressionSyntax? condition = null;
        if (!Is(StarTokenKind.Semicolon)) condition = ParseExpression();
        Expect(StarTokenKind.Semicolon, "Expected ';' after Orbit condition.");
        StatementSyntax? step = null;
        if (!Is(StarTokenKind.RParen)) step = ParseForClauseStatement();
        Expect(StarTokenKind.RParen, "Expected ')' after Orbit step.");
        var body = ParseRequiredBlock("Expected '{' after Orbit.");
        return new OrbitStatementSyntax(keyword, initializer, condition, step, body, TextSpan.FromBounds(keyword.Span.Start, body.Span.End));
    }

    private SpinWhileStatementSyntax ParseSpinWhile()
    {
        var keyword = Consume();
        var body = ParseRequiredBlock("Expected '{' after SpinWhile.");
        var condition = ParseParenthesizedCondition("SpinWhile block");
        ConsumeStatementTerminator();
        return new SpinWhileStatementSyntax(keyword, body, condition, TextSpan.FromBounds(keyword.Span.Start, condition.Span.End));
    }

    private StatementSyntax ParseForClauseStatement()
    {
        if (Is(StarTokenKind.Type))
        {
            var type = ParseType("Expected a variable type.");
            var name = ExpectName("Expected a variable name.");
            StarToken? equals = null;
            ExpressionSyntax? initializer = null;
            if (Is(StarTokenKind.Equals)) { equals = Consume(); initializer = ParseExpression(); }
            return new VariableDeclarationSyntax(ImmutableArray<StarToken>.Empty, type, name, equals, initializer, TextSpan.FromBounds(type.Span.Start, initializer?.Span.End ?? name.Span.End));
        }
        var target = ParseExpression();
        if (Is(StarTokenKind.Equals))
        {
            var equals = Consume();
            var expression = ParseExpression();
            return new AssignmentStatementSyntax(target, equals, expression, TextSpan.FromBounds(target.Span.Start, expression.Span.End));
        }
        return new ExpressionStatementSyntax(target, target.Span);
    }

    private ExploreStatementSyntax ParseExplore()
    {
        var keyword = Consume(); Expect(StarTokenKind.LParen, "Expected '(' after Explore."); var name = ExpectName("Expected an iteration variable.");
        var inKeyword = ExpectKeyword("in", "Expected 'in' in Explore."); var collection = ParseExpression(); Expect(StarTokenKind.RParen, "Expected ')' after Explore collection.");
        var body = ParseRequiredBlock("Expected '{' after Explore.");
        return new ExploreStatementSyntax(keyword, name, inKeyword, collection, body, TextSpan.FromBounds(keyword.Span.Start, body.Span.End));
    }

    private BlockSyntax ParseRequiredBlock(string message)
    {
        var open = Expect(StarTokenKind.LBrace, message); var statements = ImmutableArray.CreateBuilder<StatementSyntax>();
        while (!Is(StarTokenKind.RBrace) && !Is(StarTokenKind.EndOfFile)) { if (Is(StarTokenKind.Semicolon)) { Consume(); continue; } statements.Add(ParseStatement()); }
        if (Is(StarTokenKind.RBrace)) { var close = Consume(); return new BlockSyntax(statements.ToImmutable(), TextSpan.FromBounds(open.Span.Start, close.Span.End)); }
        Report("STR2002", open, "Unterminated block; expected '}'.");
        return new BlockSyntax(statements.ToImmutable(), TextSpan.FromBounds(open.Span.Start, Current.Span.Start));
    }

    private ExpressionSyntax ParseParenthesizedCondition(string construct) { Expect(StarTokenKind.LParen, $"Expected '(' after {construct}."); var condition = ParseExpression(); Expect(StarTokenKind.RParen, $"Expected ')' after {construct} condition."); return condition; }
    private ExpressionSyntax ParseExpression(int parentPrecedence = 0)
    {
        ExpressionSyntax left;
        var unaryPrecedence = GetUnaryPrecedence(Current.Kind);
        if (unaryPrecedence != 0) { var op = Consume(); var operand = ParseExpression(unaryPrecedence); left = new UnaryExpressionSyntax(op, operand, TextSpan.FromBounds(op.Span.Start, operand.Span.End)); }
        else left = ParsePrimary();
        while (true)
        {
            left = ParsePostfix(left);
            var precedence = GetBinaryPrecedence(Current.Kind);
            if (precedence == 0 || precedence <= parentPrecedence) break;
            var op = Consume(); var right = ParseExpression(precedence); left = new BinaryExpressionSyntax(left, op, right, TextSpan.FromBounds(left.Span.Start, right.Span.End));
        }
        return left;
    }

    private ExpressionSyntax ParsePostfix(ExpressionSyntax target)
    {
        while (true)
        {
            if (Is(StarTokenKind.Dot)) { var dot = Consume(); var member = ExpectName("Expected a member name after '.'."); target = new MemberAccessExpressionSyntax(target, dot, member, TextSpan.FromBounds(target.Span.Start, member.Span.End)); continue; }
            if (Is(StarTokenKind.LBracket)) { var open = Consume(); var index = ParseExpression(); var close = Expect(StarTokenKind.RBracket, "Expected ']' after index."); target = new IndexExpressionSyntax(target, open, index, close, TextSpan.FromBounds(target.Span.Start, close.Span.End)); continue; }
            if (Is(StarTokenKind.LParen)) { var open = Consume(); var args = ParseArguments(); var close = Expect(StarTokenKind.RParen, "Expected ')' after arguments."); target = new CallExpressionSyntax(target, open, args, close, TextSpan.FromBounds(target.Span.Start, close.Span.End)); continue; }
            return target;
        }
    }

    private ExpressionSyntax ParsePrimary()
    {
        if (Is(StarTokenKind.Number) || Is(StarTokenKind.String) || Is(StarTokenKind.Char) || IsKeyword("true") || IsKeyword("false")) return new LiteralExpressionSyntax(Consume());
        if (IsKeyword("new")) { var keyword = Consume(); var type = ParseType("Expected a Constellation name after 'new'."); StarToken? open = null; StarToken? close = null; var args = ImmutableArray<ExpressionSyntax>.Empty; if (Is(StarTokenKind.LParen)) { open = Consume(); args = ParseArguments(); close = Expect(StarTokenKind.RParen, "Expected ')' after constructor arguments."); } return new NewExpressionSyntax(keyword, type, open, args, close, TextSpan.FromBounds(keyword.Span.Start, (close?.Span.End ?? type.Span.End))); }
        if (Is(StarTokenKind.LBracket)) { var open = Consume(); var elements = ParseArguments(StarTokenKind.RBracket); var close = Expect(StarTokenKind.RBracket, "Expected ']' after list elements."); return new ListExpressionSyntax(open, elements, close, TextSpan.FromBounds(open.Span.Start, close.Span.End)); }
        if (Is(StarTokenKind.LParen)) { var open = Consume(); var expression = ParseExpression(); var close = Expect(StarTokenKind.RParen, "Expected ')'."); return new ParenthesizedExpressionSyntax(open, expression, close, TextSpan.FromBounds(open.Span.Start, close.Span.End)); }
        if (Is(StarTokenKind.Identifier) || Is(StarTokenKind.Type) || IsKeyword("this")) return new NameExpressionSyntax(Consume());
        var unexpected = Consume(); Report("STR2004", unexpected, $"Expected an expression, found '{unexpected.Value}'."); return new NameExpressionSyntax(unexpected);
    }

    private ImmutableArray<ExpressionSyntax> ParseArguments(StarTokenKind closing = StarTokenKind.RParen)
    {
        var arguments = ImmutableArray.CreateBuilder<ExpressionSyntax>();
        while (!Is(closing) && !Is(StarTokenKind.EndOfFile)) { arguments.Add(ParseExpression()); if (!Is(StarTokenKind.Comma)) break; Consume(); }
        return arguments.ToImmutable();
    }
    private TypeSyntax ParseType(string message) => new(ExpectName(message));
    private StarToken ExpectName(string message) => (Is(StarTokenKind.Identifier) || Is(StarTokenKind.Type)) ? Consume() : Missing(message);
    private StarToken ExpectKeyword(string value, string message) => IsKeyword(value) ? Consume() : Missing(message);
    private StarToken Expect(StarTokenKind kind, string message) => Is(kind) ? Consume() : Missing(message);
    private StarToken Missing(string message) { Report("STR2003", Current, message); return new StarToken(StarTokenKind.Identifier, string.Empty, new TextSpan(Current.Span.Start, 0)); }
    private void ConsumeStatementTerminator() { if (Is(StarTokenKind.Semicolon)) Consume(); }
    private void Report(string code, StarToken token, string message) => _diagnostics.ReportError(code, token.Span, message);
    private static int GetUnaryPrecedence(StarTokenKind kind) => kind is StarTokenKind.Plus or StarTokenKind.Minus ? 7 : 0;
    private static int GetBinaryPrecedence(StarTokenKind kind) => kind switch { StarTokenKind.Or => 1, StarTokenKind.And => 2, StarTokenKind.EqualsEquals or StarTokenKind.NotEquals => 3, StarTokenKind.LessThan or StarTokenKind.GreaterThan => 4, StarTokenKind.Plus or StarTokenKind.Minus => 5, StarTokenKind.Multiply or StarTokenKind.Divide => 6, _ => 0 };
}
