using System.Collections.Immutable;
using Star.Compiler.Diagnostics;
using Star.Compiler.StandardLibrary;
using Star.Compiler.Syntax;
using Star.Compiler.Text;

namespace Star.Compiler.Semantics;

/// <summary>Resolves Star declarations and names without executing source code.</summary>
public sealed class StarSemanticAnalyzer
{
    private readonly DiagnosticBag _diagnostics = new();
    private readonly Scope _global = new();
    private readonly Dictionary<SyntaxNode, Symbol> _symbols = new(ReferenceEqualityComparer.Instance);

    private StarSemanticAnalyzer()
    {
        foreach (var type in StarStandardLibrary.Core.Types) _global.TryDeclare(new Symbol(type.Name, SymbolKind.Type, type.Name, default));
        foreach (var function in StarStandardLibrary.Core.Functions) _global.TryDeclare(new Symbol(function.Name, SymbolKind.Function, function.ReturnType, default));
    }

    public static SemanticResult Analyze(CompilationUnitSyntax root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var analyzer = new StarSemanticAnalyzer();
        analyzer.AnalyzeStatements(root.Statements, analyzer._global, null);
        return new SemanticResult(analyzer._global, analyzer._diagnostics.ToImmutable(), analyzer._symbols.ToImmutableDictionary(ReferenceEqualityComparer.Instance));
    }

    private void AnalyzeStatements(ImmutableArray<StatementSyntax> statements, Scope scope, string? constellation)
    {
        foreach (var statement in statements) AnalyzeStatement(statement, scope, constellation);
    }

    private void AnalyzeStatement(StatementSyntax statement, Scope scope, string? constellation)
    {
        switch (statement)
        {
            case NamespaceDeclarationSyntax: return;
            case ConstellationDeclarationSyntax node:
                _symbols[node] = Declare(scope, new Symbol(node.Identifier.Value, SymbolKind.Constellation, node.Identifier.Value, node.Identifier.Span));
                AnalyzeStatements(node.Body.Statements, new Scope(scope), node.Identifier.Value);
                return;
            case FunctionDeclarationSyntax node:
                _symbols[node] = Declare(scope, new Symbol(node.Identifier.Value, SymbolKind.Function, node.ReturnType?.Identifier.Value ?? "Nova", node.Identifier.Span));
                AnalyzeFunction(node, scope, constellation);
                return;
            case VariableDeclarationSyntax node: AnalyzeVariable(node, scope); return;
            case PropertyDeclarationSyntax node: AnalyzeProperty(node, scope); return;
            case EmitStatementSyntax node: AnalyzeExpression(node.Expression, scope); return;
            case ReturnStatementSyntax { Expression: not null } node: AnalyzeExpression(node.Expression, scope); return;
            case WhenStatementSyntax node:
                AnalyzeExpression(node.Condition, scope); AnalyzeStatements(node.Body.Statements, new Scope(scope), constellation);
                foreach (var clause in node.ElseWhenClauses) { AnalyzeExpression(clause.Condition, scope); AnalyzeStatements(clause.Body.Statements, new Scope(scope), constellation); }
                if (node.OtherwiseClause is not null) AnalyzeStatements(node.OtherwiseClause.Body.Statements, new Scope(scope), constellation);
                return;
            case WhileStatementSyntax node: AnalyzeExpression(node.Condition, scope); AnalyzeStatements(node.Body.Statements, new Scope(scope), constellation); return;
            case OrbitStatementSyntax node:
                var orbitScope = new Scope(scope);
                if (node.Initializer is not null) AnalyzeStatement(node.Initializer, orbitScope, constellation);
                if (node.Condition is not null) AnalyzeExpression(node.Condition, orbitScope);
                if (node.Step is not null) AnalyzeStatement(node.Step, orbitScope, constellation);
                AnalyzeStatements(node.Body.Statements, new Scope(orbitScope), constellation);
                return;
            case SpinWhileStatementSyntax node: AnalyzeStatements(node.Body.Statements, new Scope(scope), constellation); AnalyzeExpression(node.Condition, scope); return;
            case ExploreStatementSyntax node:
                AnalyzeExpression(node.Collection, scope);
                var exploreScope = new Scope(scope); Declare(exploreScope, new Symbol(node.Identifier.Value, SymbolKind.Variable, "Auto", node.Identifier.Span));
                AnalyzeStatements(node.Body.Statements, exploreScope, constellation);
                return;
            case AssignmentStatementSyntax node: AnalyzeExpression(node.Target, scope); AnalyzeExpression(node.Expression, scope); return;
            case ExpressionStatementSyntax node: AnalyzeExpression(node.Expression, scope); return;
        }
    }

    private void AnalyzeFunction(FunctionDeclarationSyntax function, Scope scope, string? constellation)
    {
        var functionScope = new Scope(scope);
        if (constellation is not null) functionScope.TryDeclare(new Symbol("this", SymbolKind.Variable, constellation, function.Identifier.Span));
        foreach (var parameter in function.Parameters)
        {
            ValidateType(parameter.Type, scope);
            _symbols[parameter] = Declare(functionScope, new Symbol(parameter.Identifier.Value, SymbolKind.Parameter, parameter.Type.Identifier.Value, parameter.Identifier.Span));
        }
        if (function.ReturnType is not null) ValidateType(function.ReturnType, scope);
        AnalyzeStatements(function.Body.Statements, functionScope, constellation);
    }

    private void AnalyzeVariable(VariableDeclarationSyntax variable, Scope scope)
    {
        ValidateType(variable.Type, scope);
        if (variable.Initializer is not null) AnalyzeExpression(variable.Initializer, scope);
        _symbols[variable] = Declare(scope, new Symbol(variable.Identifier.Value, SymbolKind.Variable, variable.Type.Identifier.Value, variable.Identifier.Span));
    }
    private void AnalyzeProperty(PropertyDeclarationSyntax property, Scope scope)
    {
        ValidateType(property.Type, scope);
        _symbols[property] = Declare(scope, new Symbol(property.Identifier.Value, SymbolKind.Variable, property.Type.Identifier.Value, property.Identifier.Span));
    }

    private void AnalyzeExpression(ExpressionSyntax expression, Scope scope)
    {
        switch (expression)
        {
            case NameExpressionSyntax node:
                if (!scope.TryLookup(node.Identifier.Value, out var symbol)) Report("STR3002", node.Identifier.Span, $"The name '{node.Identifier.Value}' does not exist in this scope.");
                else _symbols[node] = symbol!;
                return;
            case ParenthesizedExpressionSyntax node: AnalyzeExpression(node.Expression, scope); return;
            case UnaryExpressionSyntax node: AnalyzeExpression(node.Operand, scope); return;
            case BinaryExpressionSyntax node: AnalyzeExpression(node.Left, scope); AnalyzeExpression(node.Right, scope); return;
            case CallExpressionSyntax node: AnalyzeExpression(node.Target, scope); foreach (var argument in node.Arguments) AnalyzeExpression(argument, scope); return;
            case MemberAccessExpressionSyntax node: AnalyzeExpression(node.Target, scope); return;
            case IndexExpressionSyntax node: AnalyzeExpression(node.Target, scope); AnalyzeExpression(node.Index, scope); return;
            case NewExpressionSyntax node: ValidateType(node.Type, scope); foreach (var argument in node.Arguments) AnalyzeExpression(argument, scope); return;
            case ListExpressionSyntax node: foreach (var element in node.Elements) AnalyzeExpression(element, scope); return;
        }
    }

    private void ValidateType(TypeSyntax type, Scope scope)
    {
        if (!scope.TryLookup(type.Identifier.Value, out var symbol) || symbol!.Kind is not (SymbolKind.Type or SymbolKind.Constellation))
            Report("STR3003", type.Span, $"The type '{type.Identifier.Value}' does not exist in this scope.");
    }
    private Symbol Declare(Scope scope, Symbol symbol)
    {
        if (scope.TryDeclare(symbol)) return symbol;
        Report("STR3001", symbol.Span, $"The name '{symbol.Name}' is already declared in this scope.");
        scope.TryLookup(symbol.Name, out var existing);
        return existing!;
    }
    private void Report(string code, TextSpan span, string message) => _diagnostics.ReportError(code, span, message);
}
