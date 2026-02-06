using System;
using System.Collections.Generic;
using StarCompiler.AST;
using StarCompiler.AST.Expressions;
using StarCompiler.AST.Statements;
using StarCompiler.Lexer;

namespace StarCompiler.Parser;

/// <summary>
/// Parser basado en AST que genera un árbol de sintaxis abstracta.
/// AST-based parser that generates an abstract syntax tree.
/// </summary>
public class ASTParser
{
    private readonly List<Token> _tokens;
    private int _position;

    public ASTParser(List<Token> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }

    private Token Current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];
    private Token Consume() => _tokens[_position++];
    private Token Peek(int offset = 1) => _position + offset < _tokens.Count ? _tokens[_position + offset] : _tokens[^1];

    /// <summary>
    /// Parsea el programa completo y devuelve una lista de declaraciones.
    /// Parses the entire program and returns a list of statements.
    /// </summary>
    public List<Statement> Parse()
    {
        var statements = new List<Statement>();

        while (Current.Type != TokenType.EOF)
        {
            if (Current.Type == TokenType.Semicolon)
            {
                Consume();
                continue;
            }

            statements.Add(ParseStatement());
            ConsumeSemicolon();
        }

        return statements;
    }

    private void ConsumeSemicolon()
    {
        while (Current.Type == TokenType.Semicolon)
        {
            Consume();
        }
    }

    private Statement ParseStatement()
    {
        if (Current.Type == TokenType.Keyword)
        {
            return Current.Value switch
            {
                "Emit" => ParseEmit(newline: false),
                "EmitLn" => ParseEmit(newline: true),
                "When" => ParseIfStatement(),
                "While" => ParseWhileStatement(),
                "Orbit" => ParseForStatement(),
                "SpinWhile" => ParseDoWhileStatement(),
                "Explore" => ParseForeachStatement(),
                "StarFunction" => ParseFunctionDeclaration(),
                "return" => ParseReturnStatement(),
                "StarName" => ParseNamespace(),
                "Constellation" => ParseConstellation(),
                _ => throw new Exception($"Unexpected keyword: {Current.Value}")
            };
        }
        else if (Current.Type == TokenType.Type)
        {
            return ParseVariableDeclaration();
        }
        else if (Current.Type == TokenType.Identifier)
        {
            return ParseIdentifierStatement();
        }
        else
        {
            throw new Exception($"Unexpected token: {Current.Value}");
        }
    }

    // ========================================
    // Statement Parsing
    // ========================================

    private EmitStatement ParseEmit(bool newline)
    {
        Consume(); // Emit or EmitLn

        Expression expr;

        if (Current.Type == TokenType.LParen)
        {
            Consume(); // (
            expr = ParseExpression();

            if (Current.Type != TokenType.RParen)
                throw new Exception("Expected ')' after Emit(");

            Consume(); // )
        }
        else
        {
            expr = ParseExpression();
        }

        return new EmitStatement(expr, newline);
    }

    private VariableDeclaration ParseVariableDeclaration()
    {
        Token typeToken = Consume(); // Type
        if (Current.Type != TokenType.Identifier)
            throw new Exception("Expected variable name");

        string varName = Consume().Value;

        if (Current.Type != TokenType.Equals)
            throw new Exception("Expected '=' in variable declaration");

        Consume(); // =
        Expression value = ParseExpression();

        return new VariableDeclaration(typeToken.Value, varName, value);
    }

    private Statement ParseIdentifierStatement()
    {
        // We have an identifier. It could be:
        // 1. Assignment: name = expr
        // 2. Index Assignment: name[index] = expr
        // 3. Expression Statement: func(args) or just a variable (though useless)

        // For now, let's keep it simple: if it's followed by '=', it's assignment.
        // If it's followed by '(' or '[', we need to be careful.
        // Actually, we can parse an expression and see what follows.

        int startPos = _position;
        string name = Consume().Value;

        if (Current.Type == TokenType.Equals)
        {
            _position = startPos;
            return ParseAssignment();
        }

        // If not '=' directly, it might be Indexing or Call
        _position = startPos;
        Expression expr = ParseExpression();

        if (Current.Type == TokenType.Equals)
        {
            // Case: name[index] = expr
            Consume(); // =
            Expression value = ParseExpression();
            // TODO: Support IndexAssignmentStatement
            throw new Exception("Index assignment name[index] = value not implemented in AST yet");
        }

        return new ExpressionStatement(expr);
    }

    private AssignmentStatement ParseAssignment()
    {
        string name = Consume().Value;
        if (Current.Type != TokenType.Equals)
            throw new Exception("Expected '=' after variable name");
        Consume(); // =

        Expression value = ParseExpression();

        return new AssignmentStatement(name, value);
    }

    private IfStatement ParseIfStatement()
    {
        Consume(); // When

        if (Current.Type != TokenType.LParen)
            throw new Exception("Expected '(' after When");
        Consume();

        Expression condition = ParseExpression();

        if (Current.Type != TokenType.RParen)
            throw new Exception("Expected ')' after condition");
        Consume();

        if (Current.Type != TokenType.LBrace)
            throw new Exception("Expected '{' after condition");

        List<Statement> thenBlock = ParseBlock();

        // Handle ElseWhen
        var elseIfClauses = new List<(Expression Condition, List<Statement> Block)>();
        while (Current.Type == TokenType.Keyword && Current.Value == "ElseWhen")
        {
            Consume(); // ElseWhen

            if (Current.Type != TokenType.LParen)
                throw new Exception("Expected '(' after ElseWhen");
            Consume();

            Expression elseWhenCond = ParseExpression();

            if (Current.Type != TokenType.RParen)
                throw new Exception("Expected ')' after ElseWhen condition");
            Consume();

            if (Current.Type != TokenType.LBrace)
                throw new Exception("Expected '{' after ElseWhen condition");

            List<Statement> elseWhenBlock = ParseBlock();
            elseIfClauses.Add((elseWhenCond, elseWhenBlock));
        }

        // Handle Otherwise
        List<Statement>? elseBlock = null;
        if (Current.Type == TokenType.Keyword && Current.Value == "Otherwise")
        {
            Consume(); // Otherwise
            if (Current.Type != TokenType.LBrace)
                throw new Exception("Expected '{' after Otherwise");

            elseBlock = ParseBlock();
        }

        return new IfStatement(condition, thenBlock, elseIfClauses, elseBlock);
    }

    private WhileStatement ParseWhileStatement()
    {
        Consume(); // While

        if (Current.Type != TokenType.LParen)
            throw new Exception("Expected '(' after While");
        Consume();

        Expression condition = ParseExpression();

        if (Current.Type != TokenType.RParen)
            throw new Exception("Expected ')' after While condition");
        Consume();

        if (Current.Type != TokenType.LBrace)
            throw new Exception("Expected '{' after While condition");

        List<Statement> body = ParseBlock();

        return new WhileStatement(condition, body);
    }

    private ForStatement ParseForStatement()
    {
        Consume(); // Orbit
        if (Current.Type != TokenType.LParen)
            throw new Exception("Expected '(' after Orbit");
        Consume();

        // 1. Initialization
        Statement? init = null;
        if (Current.Type == TokenType.Type)
        {
            init = ParseVariableDeclaration();
        }
        else if (Current.Type == TokenType.Identifier)
        {
            init = ParseAssignment();
        }

        if (Current.Type != TokenType.Semicolon)
            throw new Exception("Expected ';' after Orbit init");
        Consume();

        // 2. Condition
        Expression condition = ParseExpression();

        if (Current.Type != TokenType.Semicolon)
            throw new Exception("Expected ';' after Orbit condition");
        Consume();

        // 3. Step (Increment)
        Statement? step = null;
        if (Current.Type == TokenType.Identifier)
        {
            step = ParseAssignment();
        }

        if (Current.Type != TokenType.RParen)
            throw new Exception("Expected ')' after Orbit step");
        Consume();

        if (Current.Type != TokenType.LBrace)
            throw new Exception("Expected '{' after Orbit");

        List<Statement> body = ParseBlock();

        return new ForStatement(init, condition, step, body);
    }

    private DoWhileStatement ParseDoWhileStatement()
    {
        Consume(); // SpinWhile
        if (Current.Type != TokenType.LBrace)
            throw new Exception("Expected '{' after SpinWhile");

        List<Statement> body = ParseBlock();

        if (Current.Type != TokenType.LParen)
            throw new Exception("Expected '(' after SpinWhile block");
        Consume(); // (

        Expression condition = ParseExpression();

        if (Current.Type != TokenType.RParen)
            throw new Exception("Expected ')' after SpinWhile condition");
        Consume(); // )

        return new DoWhileStatement(body, condition);
    }

    private ForeachStatement ParseForeachStatement()
    {
        Consume(); // Explore
        if (Current.Type != TokenType.LParen)
            throw new Exception("Expected '(' after Explore");
        Consume();

        if (Current.Type != TokenType.Identifier)
            throw new Exception("Expected variable name in Explore");
        string varName = Consume().Value;

        if (Current.Type != TokenType.Keyword || Current.Value != "in")
            throw new Exception("Expected 'in' keyword in Explore");
        Consume(); // in

        if (Current.Type != TokenType.Identifier)
            throw new Exception("Expected collection name in Explore");
        string collectionName = Consume().Value;

        if (Current.Type != TokenType.RParen)
            throw new Exception("Expected ')' after Explore condition");
        Consume();

        if (Current.Type != TokenType.LBrace)
            throw new Exception("Expected '{' after Explore");

        List<Statement> body = ParseBlock();

        return new ForeachStatement(varName, collectionName, body);
    }

    private FunctionDeclaration ParseFunctionDeclaration()
    {
        Consume(); // StarFunction

        if (Current.Type != TokenType.Identifier)
            throw new Exception("Expected function name");
        string funcName = Consume().Value;

        if (Current.Type != TokenType.LParen)
            throw new Exception("Expected '(' after function name");
        Consume();

        // Parse parameters
        var parameters = new List<Parameter>();
        if (Current.Type != TokenType.RParen)
        {
            while (true)
            {
                if (Current.Type != TokenType.Type)
                    throw new Exception("Expected parameter type");
                string paramType = Consume().Value;

                if (Current.Type != TokenType.Identifier)
                    throw new Exception("Expected parameter name");
                string paramName = Consume().Value;

                parameters.Add(new Parameter(paramType, paramName));

                if (Current.Type == TokenType.RParen)
                    break;
                if (Current.Type != TokenType.Comma)
                    throw new Exception("Expected ',' or ')' in parameter list");
                Consume(); // ,
            }
        }
        Consume(); // )

        // Parse return type (optional)
        string? returnType = null;
        if (Current.Type == TokenType.Arrow)
        {
            Consume(); // ->
            if (Current.Type != TokenType.Type)
                throw new Exception("Expected return type after '->'");
            returnType = Consume().Value;
        }

        if (Current.Type != TokenType.LBrace)
            throw new Exception("Expected '{' after function signature");

        List<Statement> body = ParseBlock();

        return new FunctionDeclaration(funcName, parameters, returnType, body);
    }

    private Statement ParseConstellation()
    {
        Consume(); // Constellation

        if (Current.Type != TokenType.Identifier)
            throw new Exception("Expected constellation name");

        string constName = Consume().Value;
        var constellation = new ConstellationDeclaration(constName);

        if (Current.Type != TokenType.LBrace)
            throw new Exception("Expected '{' after constellation name");

        Consume(); // {

        while (Current.Type != TokenType.RBrace && Current.Type != TokenType.EOF)
        {
            // Optional modifiers
            Accessibility access = Accessibility.Private;
            bool isStatic = false;

            while (Current.Type == TokenType.Keyword &&
                  (Current.Value == "Public" || Current.Value == "Private" || Current.Value == "Protected" || Current.Value == "Static"))
            {
                if (Current.Value == "Static") isStatic = true;
                else if (Current.Value == "Public") access = Accessibility.Public;
                else if (Current.Value == "Private") access = Accessibility.Private;
                else if (Current.Value == "Protected") access = Accessibility.Protected;
                Consume();
            }

            if (Current.Type == TokenType.Keyword && Current.Value == "StarFunction")
            {
                var method = ParseFunctionDeclaration();
                method.Accessibility = access;
                method.IsStatic = isStatic;
                constellation.Methods.Add(method);
            }
            else if (Current.Type == TokenType.Type)
            {
                var field = ParseVariableDeclaration();
                field.Accessibility = access;
                field.IsStatic = isStatic;
                constellation.Fields.Add(field);
            }
            else if (Current.Type == TokenType.Semicolon)
            {
                Consume();
            }
            else
            {
                throw new Exception($"Unexpected token in constellation: {Current.Value}");
            }
        }

        if (Current.Type != TokenType.RBrace)
            throw new Exception("Expected '}' after constellation body");

        Consume(); // }

        return constellation;
    }
    private Statement ParseNamespace()
    {
        Consume(); // StarName

        var sb = new System.Text.StringBuilder();

        if (Current.Type != TokenType.Identifier && Current.Type != TokenType.Type)
            throw new Exception("Expected identifier or type after StarName");

        sb.Append(Consume().Value);

        while (Current.Type == TokenType.Dot)
        {
            Consume(); // .
            if (Current.Type != TokenType.Identifier && Current.Type != TokenType.Type)
                throw new Exception("Expected identifier or type after '.' in StarName");

            sb.Append(".");
            sb.Append(Consume().Value);
        }

        return new NamespaceDeclaration(sb.ToString());
    }

    private ReturnStatement ParseReturnStatement()
    {
        Consume(); // return

        Expression? value = null;
        if (Current.Type != TokenType.Semicolon && Current.Type != TokenType.RBrace)
        {
            value = ParseExpression();
        }

        return new ReturnStatement(value);
    }

    private List<Statement> ParseBlock()
    {
        Consume(); // {
        var statements = new List<Statement>();

        while (Current.Type != TokenType.RBrace && Current.Type != TokenType.EOF)
        {
            if (Current.Type == TokenType.Semicolon)
            {
                Consume();
                continue;
            }

            statements.Add(ParseStatement());
            ConsumeSemicolon();
        }

        if (Current.Type != TokenType.RBrace)
            throw new Exception("Expected '}' to close block");
        Consume(); // }

        return statements;
    }

    // ========================================
    // Expression Parsing with Precedence
    // ========================================

    private Expression ParseExpression()
    {
        return ParseLogicalOr();
    }

    private Expression ParseLogicalOr()
    {
        var left = ParseLogicalAnd();

        while (Current.Type == TokenType.Or)
        {
            Consume();
            var right = ParseLogicalAnd();
            left = new BinaryExpression(left, TokenType.Or, right);
        }
        return left;
    }

    private Expression ParseLogicalAnd()
    {
        var left = ParseEquality();

        while (Current.Type == TokenType.And)
        {
            Consume();
            var right = ParseEquality();
            left = new BinaryExpression(left, TokenType.And, right);
        }
        return left;
    }

    private Expression ParseEquality()
    {
        var left = ParseComparison();

        while (Current.Type == TokenType.EqualsEquals || Current.Type == TokenType.NotEquals)
        {
            var op = Consume().Type;
            var right = ParseComparison();
            left = new BinaryExpression(left, op, right);
        }
        return left;
    }

    private Expression ParseComparison()
    {
        var left = ParseAdditive();

        while (Current.Type == TokenType.LessThan || Current.Type == TokenType.GreaterThan)
        {
            var op = Consume().Type;
            var right = ParseAdditive();
            left = new BinaryExpression(left, op, right);
        }
        return left;
    }

    private Expression ParseAdditive()
    {
        var left = ParseMultiplicative();

        while (Current.Type == TokenType.Plus || Current.Type == TokenType.Minus)
        {
            var op = Consume().Type;
            var right = ParseMultiplicative();
            left = new BinaryExpression(left, op, right);
        }

        return left;
    }

    private Expression ParseMultiplicative()
    {
        var left = ParsePostfix();

        while (Current.Type == TokenType.Multiply || Current.Type == TokenType.Divide)
        {
            var op = Consume().Type;
            var right = ParsePostfix();
            left = new BinaryExpression(left, op, right);
        }

        return left;
    }

    private Expression ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Current.Type == TokenType.LBracket)
            {
                // Array/List indexing: expr[index]
                Consume(); // [
                Expression index = ParseExpression();
                if (Current.Type != TokenType.RBracket)
                    throw new Exception("Expected ']'");
                Consume(); // ]
                expr = new IndexExpression(expr, index);
            }
            else if (Current.Type == TokenType.Dot)
            {
                // Member access: expr.member
                Consume(); // .
                if (Current.Type != TokenType.Identifier && Current.Type != TokenType.Type)
                    throw new Exception("Expected identifier after '.'");
                string memberName = Consume().Value;
                expr = new MemberAccessExpression(expr, memberName);
            }
            else if (Current.Type == TokenType.LParen)
            {
                // Function call: expr(args)
                Consume(); // (
                var args = new List<Expression>();

                if (Current.Type != TokenType.RParen)
                {
                    while (true)
                    {
                        args.Add(ParseExpression());
                        if (Current.Type == TokenType.RParen)
                            break;
                        if (Current.Type != TokenType.Comma)
                            throw new Exception("Expected ',' or ')' in argument list");
                        Consume(); // ,
                    }
                }
                Consume(); // )
                expr = new CallExpression(expr, args);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private Expression ParsePrimary()
    {
        if (Current.Type == TokenType.Number)
        {
            string val = Consume().Value;
            if (val.Contains("."))
                return new LiteralExpression(double.Parse(val));
            return new LiteralExpression(int.Parse(val));
        }
        else if (Current.Type == TokenType.String)
        {
            return new LiteralExpression(Consume().Value);
        }
        else if (Current.Type == TokenType.Char)
        {
            return new LiteralExpression(Consume().Value[0]);
        }
        else if (Current.Type == TokenType.Keyword && (Current.Value == "true" || Current.Value == "false"))
        {
            return new LiteralExpression(bool.Parse(Consume().Value));
        }
        else if (Current.Type == TokenType.Keyword && Current.Value == "new")
        {
            Consume(); // new
            if (Current.Type != TokenType.Identifier && Current.Type != TokenType.Type)
                throw new Exception("Expected constellation name after 'new'");

            string constName = Consume().Value;

            // Arguments
            var args = new List<Expression>();
            if (Current.Type == TokenType.LParen)
            {
                Consume(); // (
                if (Current.Type != TokenType.RParen)
                {
                    while (true)
                    {
                        args.Add(ParseExpression());
                        if (Current.Type == TokenType.RParen) break;
                        if (Current.Type != TokenType.Comma)
                            throw new Exception("Expected ',' or ')' in constructor arguments");
                        Consume(); // ,
                    }
                }
                Consume(); // )
            }

            return new NewExpression(constName, args);
        }
        else if (Current.Type == TokenType.Identifier)
        {
            string name = Consume().Value;
            return new VariableExpression(name);
        }
        else if (Current.Type == TokenType.LBracket)
        {
            return ParseListLiteral();
        }
        else if (Current.Type == TokenType.LParen)
        {
            Consume(); // (
            Expression result = ParseExpression();
            if (Current.Type != TokenType.RParen)
                throw new Exception("Expected ')'");
            Consume(); // )
            return result;
        }

        throw new Exception($"Unexpected token in expression: {Current.Value}");
    }

    private ListLiteralExpression ParseListLiteral()
    {
        Consume(); // [
        var elements = new List<Expression>();

        if (Current.Type != TokenType.RBracket)
        {
            while (true)
            {
                elements.Add(ParseExpression());

                if (Current.Type == TokenType.RBracket)
                    break;
                if (Current.Type != TokenType.Comma)
                    throw new Exception("Expected ',' in list literal");
                Consume();
            }
        }
        Consume(); // ]
        return new ListLiteralExpression(elements);
    }
}
