using System.Collections.Immutable;
using Star.Compiler.Compilation;
using Star.Compiler.Semantics;
using Star.Compiler.Syntax;

namespace Star.Compiler.Ir;

/// <summary>Lowers a successful Star front-end result into target-neutral IR without executing it.</summary>
public static class StarIrLowerer
{
    /// <summary>
    /// Returns a result that always retains front-end diagnostics. Its IR is absent when errors
    /// exist, allowing callers to use this as a stable boundary without losing source locations.
    /// </summary>
    public static StarIrLoweringResult Lower(CompilationResult compilation)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        if (compilation.HasErrors) return new StarIrLoweringResult(null, compilation.Diagnostics);

        var lowerer = new Lowerer(compilation.Semantics, compilation.Types);
        var ir = new StarIrCompilationUnit(
            compilation.Syntax.Statements.Select(lowerer.LowerStatement).ToImmutableArray(),
            compilation.Syntax.Span);
        return new StarIrLoweringResult(ir, compilation.Diagnostics);
    }

    private sealed class Lowerer(SemanticResult semantics, TypeCheckResult types)
    {
        private readonly SemanticResult _semantics = semantics;
        private readonly TypeCheckResult _types = types;

        public StarIrStatement LowerStatement(StatementSyntax statement) => statement switch
        {
            NamespaceDeclarationSyntax node => new StarIrNamespace(string.Join('.', node.Name.Parts.Select(part => part.Value)), node.Span),
            ConstellationDeclarationSyntax node => new StarIrConstellation(node.Identifier.Value, LowerStatements(node.Body.Statements), SymbolFor(node), node.Span),
            FunctionDeclarationSyntax node => new StarIrFunction(node.Identifier.Value, Modifiers(node.Modifiers), node.Parameters.Select(parameter => new StarIrParameter(parameter.Identifier.Value, parameter.Type.Identifier.Value, SymbolFor(parameter), parameter.Span)).ToImmutableArray(), node.ReturnType?.Identifier.Value ?? "Nova", LowerStatements(node.Body.Statements), SymbolFor(node), node.Span),
            VariableDeclarationSyntax node => new StarIrVariable(node.Identifier.Value, Modifiers(node.Modifiers), node.Type.Identifier.Value, node.Initializer is null ? null : LowerExpression(node.Initializer), SymbolFor(node), node.Span),
            PropertyDeclarationSyntax node => new StarIrProperty(node.Identifier.Value, Modifiers(node.Modifiers), node.Type.Identifier.Value, SymbolFor(node), node.Span),
            AssignmentStatementSyntax node => new StarIrAssignment(LowerExpression(node.Target), LowerExpression(node.Expression), node.Span),
            EmitStatementSyntax node => new StarIrEmit(LowerExpression(node.Expression), node.Keyword.Value == "EmitLn", node.Span),
            ReturnStatementSyntax node => new StarIrReturn(node.Expression is null ? null : LowerExpression(node.Expression), node.Span),
            WhenStatementSyntax node => new StarIrConditional(LowerExpression(node.Condition), LowerStatements(node.Body.Statements), node.ElseWhenClauses.Select(clause => new StarIrConditionalBranch(LowerExpression(clause.Condition), LowerStatements(clause.Body.Statements), clause.Span)).ToImmutableArray(), node.OtherwiseClause is null ? null : LowerStatements(node.OtherwiseClause.Body.Statements), node.Span),
            WhileStatementSyntax node => new StarIrWhile(LowerExpression(node.Condition), LowerStatements(node.Body.Statements), node.Span),
            OrbitStatementSyntax node => new StarIrOrbit(node.Initializer is null ? null : LowerStatement(node.Initializer), node.Condition is null ? null : LowerExpression(node.Condition), node.Step is null ? null : LowerStatement(node.Step), LowerStatements(node.Body.Statements), node.Span),
            SpinWhileStatementSyntax node => new StarIrSpinWhile(LowerStatements(node.Body.Statements), LowerExpression(node.Condition), node.Span),
            ExploreStatementSyntax node => new StarIrExplore(node.Identifier.Value, LowerExpression(node.Collection), LowerStatements(node.Body.Statements), SymbolFor(node), node.Span),
            ExpressionStatementSyntax node => new StarIrExpressionStatement(LowerExpression(node.Expression), node.Span),
            _ => throw new InvalidOperationException($"Cannot lower syntax kind '{statement.Kind}'.")
        };

        private ImmutableArray<StarIrStatement> LowerStatements(ImmutableArray<StatementSyntax> statements) => statements.Select(LowerStatement).ToImmutableArray();

        private StarIrExpression LowerExpression(ExpressionSyntax expression) => expression switch
        {
            LiteralExpressionSyntax node => new StarIrLiteral(node.LiteralToken.Value, TypeOf(node), node.Span),
            NameExpressionSyntax node => new StarIrName(node.Identifier.Value, SymbolFor(node), TypeOf(node), node.Span),
            ParenthesizedExpressionSyntax node => LowerExpression(node.Expression),
            UnaryExpressionSyntax node => new StarIrUnary(node.OperatorToken.Value, LowerExpression(node.Operand), TypeOf(node), node.Span),
            BinaryExpressionSyntax node => new StarIrBinary(LowerExpression(node.Left), node.OperatorToken.Value, LowerExpression(node.Right), TypeOf(node), node.Span),
            CallExpressionSyntax node => new StarIrCall(LowerExpression(node.Target), node.Arguments.Select(LowerExpression).ToImmutableArray(), TypeOf(node), node.Span),
            MemberAccessExpressionSyntax node => new StarIrMemberAccess(LowerExpression(node.Target), node.Identifier.Value, TypeOf(node), node.Span),
            IndexExpressionSyntax node => new StarIrIndex(LowerExpression(node.Target), LowerExpression(node.Index), TypeOf(node), node.Span),
            NewExpressionSyntax node => new StarIrNew(node.Type.Identifier.Value, node.Arguments.Select(LowerExpression).ToImmutableArray(), node.Span),
            ListExpressionSyntax node => new StarIrList(node.Elements.Select(LowerExpression).ToImmutableArray(), TypeOf(node), node.Span),
            _ => throw new InvalidOperationException($"Cannot lower syntax kind '{expression.Kind}'.")
        };

        private Symbol? SymbolFor(SyntaxNode node) => _semantics.Symbols.TryGetValue(node, out var symbol) ? symbol : null;
        private string TypeOf(ExpressionSyntax expression) => _types.ExpressionTypes.TryGetValue(expression, out var type) ? type : "<unknown>";
        private static ImmutableArray<string> Modifiers(ImmutableArray<Star.Compiler.Lexing.StarToken> modifiers) => modifiers.Select(modifier => modifier.Value).ToImmutableArray();
    }
}
