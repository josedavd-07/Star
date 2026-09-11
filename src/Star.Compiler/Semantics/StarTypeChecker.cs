using System.Collections.Immutable;
using Star.Compiler.Diagnostics;
using Star.Compiler.Lexing;
using Star.Compiler.StandardLibrary;
using Star.Compiler.Syntax;
using Star.Compiler.Text;

namespace Star.Compiler.Semantics;

/// <summary>Performs Star's first static type checks after name resolution.</summary>
public sealed class StarTypeChecker
{
    private const string Unknown = "<unknown>";
    private const string Auto = "Auto";
    private readonly DiagnosticBag _diagnostics = new();
    private readonly Dictionary<string, ConstellationInfo> _constellations = new(StringComparer.Ordinal);
    private readonly Dictionary<ExpressionSyntax, string> _expressionTypes = new(ReferenceEqualityComparer.Instance);
    private string? _currentConstellation;
    private bool _inConstructor;

    public static TypeCheckResult Check(CompilationUnitSyntax root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var checker = new StarTypeChecker();
        var global = new TypeScope();
        foreach (var type in StarStandardLibrary.Core.Types) global.Declare(type.Name, type.Name);
        foreach (var function in StarStandardLibrary.Core.Functions) global.Declare(function.Name, function.ReturnType);
        checker.RegisterConstellations(root.Statements);
        checker.CheckStatements(root.Statements, global, null);
        return new TypeCheckResult(checker._diagnostics.ToImmutable(), checker._expressionTypes.ToImmutableDictionary(ReferenceEqualityComparer.Instance));
    }

    private void CheckStatements(ImmutableArray<StatementSyntax> statements, TypeScope scope, string? returnType)
    {
        foreach (var statement in statements)
        {
            if (statement is ConstellationDeclarationSyntax constellation) scope.Declare(constellation.Identifier.Value, constellation.Identifier.Value);
            if (statement is FunctionDeclarationSyntax function) scope.Declare(function.Identifier.Value, function.ReturnType?.Identifier.Value ?? "Nova");
        }
        foreach (var statement in statements) CheckStatement(statement, scope, returnType);
    }

    private void CheckStatement(StatementSyntax statement, TypeScope scope, string? returnType)
    {
        switch (statement)
        {
            case ConstellationDeclarationSyntax constellation:
                var previousConstellation = _currentConstellation;
                _currentConstellation = constellation.Identifier.Value;
                var constellationScope = new TypeScope(scope);
                constellationScope.Declare("this", constellation.Identifier.Value);
                CheckStatements(constellation.Body.Statements, constellationScope, null);
                _currentConstellation = previousConstellation;
                break;
            case FunctionDeclarationSyntax function:
                var functionScope = new TypeScope(scope);
                foreach (var parameter in function.Parameters) functionScope.Declare(parameter.Identifier.Value, parameter.Type.Identifier.Value);
                var previousConstructor = _inConstructor;
                _inConstructor = _currentConstellation is not null && function.Identifier.Value == "Constructor";
                CheckStatements(function.Body.Statements, functionScope, function.ReturnType?.Identifier.Value ?? "Nova");
                _inConstructor = previousConstructor;
                break;
            case VariableDeclarationSyntax variable:
                if (variable.Initializer is not null) EnsureAssignable(variable.Type.Identifier.Value, GetType(variable.Initializer, scope), variable.Initializer.Span, "initializer");
                scope.Declare(variable.Identifier.Value, variable.Type.Identifier.Value); break;
            case PropertyDeclarationSyntax property:
                scope.Declare(property.Identifier.Value, property.Type.Identifier.Value); break;
            case AssignmentStatementSyntax assignment:
                EnsureAssignable(GetAssignableType(assignment.Target, scope), GetType(assignment.Expression, scope), assignment.Expression.Span, "assignment"); break;
            case EmitStatementSyntax emit: GetType(emit.Expression, scope); break;
            case ReturnStatementSyntax statementReturn:
                if (returnType is not null) EnsureAssignable(returnType, statementReturn.Expression is null ? "Nova" : GetType(statementReturn.Expression, scope), statementReturn.Span, "return value"); break;
            case WhenStatementSyntax @when:
                EnsureBoolean(GetType(@when.Condition, scope), @when.Condition.Span, "When condition"); CheckStatements(@when.Body.Statements, new TypeScope(scope), returnType);
                foreach (var clause in @when.ElseWhenClauses) { EnsureBoolean(GetType(clause.Condition, scope), clause.Condition.Span, "ElseWhen condition"); CheckStatements(clause.Body.Statements, new TypeScope(scope), returnType); }
                if (@when.OtherwiseClause is not null) CheckStatements(@when.OtherwiseClause.Body.Statements, new TypeScope(scope), returnType); break;
            case WhileStatementSyntax @while: EnsureBoolean(GetType(@while.Condition, scope), @while.Condition.Span, "While condition"); CheckStatements(@while.Body.Statements, new TypeScope(scope), returnType); break;
            case OrbitStatementSyntax orbit:
                var orbitScope = new TypeScope(scope);
                if (orbit.Initializer is not null) CheckStatement(orbit.Initializer, orbitScope, returnType);
                if (orbit.Condition is not null) EnsureBoolean(GetType(orbit.Condition, orbitScope), orbit.Condition.Span, "Orbit condition");
                if (orbit.Step is not null) CheckStatement(orbit.Step, orbitScope, returnType);
                CheckStatements(orbit.Body.Statements, new TypeScope(orbitScope), returnType); break;
            case SpinWhileStatementSyntax spin: CheckStatements(spin.Body.Statements, new TypeScope(scope), returnType); EnsureBoolean(GetType(spin.Condition, scope), spin.Condition.Span, "SpinWhile condition"); break;
            case ExploreStatementSyntax explore:
                GetType(explore.Collection, scope); var exploreScope = new TypeScope(scope); exploreScope.Declare(explore.Identifier.Value, Auto); CheckStatements(explore.Body.Statements, exploreScope, returnType); break;
            case ExpressionStatementSyntax expression: GetType(expression.Expression, scope); break;
        }
    }

    private string GetType(ExpressionSyntax expression, TypeScope scope)
    {
        var type = expression switch
        {
            LiteralExpressionSyntax literal => LiteralType(literal.LiteralToken),
            NameExpressionSyntax name => scope.Lookup(name.Identifier.Value),
            ParenthesizedExpressionSyntax parenthesized => GetType(parenthesized.Expression, scope),
            UnaryExpressionSyntax unary => CheckUnary(unary, scope),
            BinaryExpressionSyntax binary => CheckBinary(binary, scope),
            CallExpressionSyntax call => CheckCall(call, scope),
            MemberAccessExpressionSyntax member => GetMemberType(member, scope),
            IndexExpressionSyntax index => GetIndexType(index, scope),
            NewExpressionSyntax node => CheckNew(node, scope),
            ListExpressionSyntax list => CheckList(list, scope),
            _ => Unknown
        };
        _expressionTypes[expression] = type;
        return type;
    }

    private string CheckBinary(BinaryExpressionSyntax binary, TypeScope scope)
    {
        var left = GetType(binary.Left, scope); var right = GetType(binary.Right, scope);
        return binary.OperatorToken.Kind switch
        {
            StarTokenKind.Plus when left == "String" && right == "String" => "String",
            StarTokenKind.Plus or StarTokenKind.Minus or StarTokenKind.Multiply or StarTokenKind.Divide => NumericResult(left, right, binary.OperatorToken.Span),
            StarTokenKind.LessThan or StarTokenKind.GreaterThan => NumericComparison(left, right, binary.OperatorToken.Span),
            StarTokenKind.And or StarTokenKind.Or => LogicalResult(left, right, binary.OperatorToken.Span),
            StarTokenKind.EqualsEquals or StarTokenKind.NotEquals => "Bool",
            _ => Unknown
        };
    }

    private string CheckUnary(UnaryExpressionSyntax unary, TypeScope scope)
    {
        var operand = GetType(unary.Operand, scope);
        if (!IsNumeric(operand) && operand != Unknown) Report("STR4001", unary.OperatorToken.Span, $"Operator '{unary.OperatorToken.Value}' requires a numeric operand, not '{operand}'.");
        return operand;
    }
    private string NumericResult(string left, string right, TextSpan span)
    {
        if (left == Unknown || right == Unknown) return Unknown;
        if (!IsNumeric(left) || !IsNumeric(right)) { Report("STR4001", span, "Arithmetic operators require numeric operands."); return Unknown; }
        return left is "Double" or "Float" || right is "Double" or "Float" ? "Double" : "Int";
    }
    private string NumericComparison(string left, string right, TextSpan span) { NumericResult(left, right, span); return "Bool"; }
    private string LogicalResult(string left, string right, TextSpan span)
    {
        if ((left != Unknown && left != "Bool") || (right != Unknown && right != "Bool")) Report("STR4002", span, "Logical operators require Bool operands.");
        return "Bool";
    }
    private string CheckNew(NewExpressionSyntax node, TypeScope scope)
    {
        foreach (var argument in node.Arguments) GetType(argument, scope);
        var typeName = node.Type.Identifier.Value;
        if (!scope.IsType(typeName)) return Unknown;
        if (!_constellations.TryGetValue(typeName, out var constellation)) return typeName;
        if (!constellation.Members.TryGetValue("Constructor", out var constructor))
        {
            if (node.Arguments.Length != 0)
                Report("STR5003", node.Span, $"Constellation '{typeName}' has no Constructor that accepts {node.Arguments.Length} argument(s).");
            return typeName;
        }
        if (!constructor.IsMethod || constructor.TypeName != "Nova")
        {
            Report("STR5001", node.Span, $"Member 'Constructor' of Constellation '{typeName}' must be a StarFunction that returns Nova.");
            return typeName;
        }
        if (node.Arguments.Length != constructor.ParameterTypes.Length)
            Report("STR5003", node.Span, $"Constructor of '{typeName}' expects {constructor.ParameterTypes.Length} argument(s), but received {node.Arguments.Length}.");
        foreach (var pair in node.Arguments.Zip(constructor.ParameterTypes))
            EnsureAssignable(pair.Second, GetType(pair.First, scope), pair.First.Span, $"argument for Constructor of '{typeName}'");
        return typeName;
    }
    private string CheckList(ListExpressionSyntax list, TypeScope scope) { foreach (var element in list.Elements) GetType(element, scope); return "Galaxy"; }
    private string GetMemberType(MemberAccessExpressionSyntax member, TypeScope scope)
    {
        var targetType = GetType(member.Target, scope);
        var resolved = ResolveMember(targetType, member.Identifier.Value, member.Identifier.Span);
        return resolved?.TypeName ?? Unknown;
    }

    private string CheckCall(CallExpressionSyntax call, TypeScope scope)
    {
        foreach (var argument in call.Arguments) GetType(argument, scope);
        if (call.Target is NameExpressionSyntax name && StarStandardLibrary.Core.TryGetFunction(name.Identifier.Value, out var standardFunction))
        {
            if (call.Arguments.Length != standardFunction!.ParameterTypes.Length)
                Report("STR5003", call.Span, $"StarFunction '{standardFunction.Name}' expects {standardFunction.ParameterTypes.Length} argument(s), but received {call.Arguments.Length}.");
            foreach (var pair in call.Arguments.Zip(standardFunction.ParameterTypes))
                EnsureAssignable(pair.Second, GetType(pair.First, scope), pair.First.Span, $"argument for '{standardFunction.Name}'");
            return standardFunction.ReturnType;
        }
        if (call.Target is not MemberAccessExpressionSyntax member) return GetType(call.Target, scope);
        var targetType = GetType(member.Target, scope);
        var resolved = ResolveMember(targetType, member.Identifier.Value, member.Identifier.Span);
        if (resolved is null) return Unknown;
        if (!resolved.IsMethod)
        {
            Report("STR5001", member.Identifier.Span, $"'{member.Identifier.Value}' is a field, not a StarFunction.");
            return Unknown;
        }
        if (call.Arguments.Length != resolved.ParameterTypes.Length)
        {
            Report("STR5003", call.Span, $"StarFunction '{member.Identifier.Value}' expects {resolved.ParameterTypes.Length} argument(s), but received {call.Arguments.Length}.");
        }
        foreach (var pair in call.Arguments.Zip(resolved.ParameterTypes))
        {
            EnsureAssignable(pair.Second, GetType(pair.First, scope), pair.First.Span, $"argument for '{member.Identifier.Value}'");
        }
        return resolved.TypeName;
    }

    private MemberInfo? ResolveMember(string targetType, string name, TextSpan span)
    {
        if (targetType == Unknown || targetType == Auto) return null;
        if (!_constellations.TryGetValue(targetType, out var constellation) || !constellation.Members.TryGetValue(name, out var member))
        {
            Report("STR5001", span, $"Constellation '{targetType}' has no member named '{name}'.");
            return null;
        }
        if (member.Accessibility is "Private" or "Protected" && _currentConstellation != targetType)
        {
            Report("STR5002", span, $"Member '{name}' of Constellation '{targetType}' is {member.Accessibility}.");
        }
        return member;
    }
    private string GetIndexType(IndexExpressionSyntax index, TypeScope scope) { GetType(index.Target, scope); EnsureAssignable("Int", GetType(index.Index, scope), index.Index.Span, "index"); return Auto; }
    private string GetAssignableType(ExpressionSyntax expression, TypeScope scope)
    {
        if (expression is NameExpressionSyntax name) return scope.Lookup(name.Identifier.Value);
        if (expression is MemberAccessExpressionSyntax member)
        {
            var targetType = GetType(member.Target, scope);
            var resolved = ResolveMember(targetType, member.Identifier.Value, member.Identifier.Span);
            var writingOwnProperty = _inConstructor && _currentConstellation == targetType && member.Target is NameExpressionSyntax { Identifier.Value: "this" };
            if (resolved is { IsReadOnly: true } && !writingOwnProperty)
                Report("STR5004", member.Identifier.Span, $"Property '{member.Identifier.Value}' of Constellation '{targetType}' is Read and can only be assigned by its Constructor.");
            return resolved?.TypeName ?? Unknown;
        }
        return GetType(expression, scope);
    }
    private void EnsureBoolean(string actual, TextSpan span, string context) { if (actual != Unknown && actual != "Bool") Report("STR4002", span, $"{context} must be Bool, not '{actual}'."); }
    private void EnsureAssignable(string expected, string actual, TextSpan span, string context) { if (expected != Unknown && expected != Auto && expected != "Object" && actual != Unknown && actual != Auto && expected != actual) Report("STR4003", span, $"Cannot use '{actual}' as '{expected}' in {context}."); }
    private static string LiteralType(StarToken token) => token.Kind switch { StarTokenKind.String => "String", StarTokenKind.Char => "Char", StarTokenKind.Number when token.Value.Contains('.') => "Double", StarTokenKind.Number => "Int", StarTokenKind.Keyword when token.Value is "true" or "false" => "Bool", _ => Unknown };
    private static bool IsNumeric(string type) => type is "Int" or "Double" or "Float";
    private void Report(string code, TextSpan span, string message) => _diagnostics.ReportError(code, span, message);

    private void RegisterConstellations(ImmutableArray<StatementSyntax> statements)
    {
        foreach (var constellation in statements.OfType<ConstellationDeclarationSyntax>())
        {
            var members = new Dictionary<string, MemberInfo>(StringComparer.Ordinal);
            foreach (var field in constellation.Body.Statements.OfType<VariableDeclarationSyntax>())
                members.TryAdd(field.Identifier.Value, new MemberInfo(field.Type.Identifier.Value, false, AccessibilityOf(field.Modifiers), ImmutableArray<string>.Empty, false));
            foreach (var property in constellation.Body.Statements.OfType<PropertyDeclarationSyntax>())
                members.TryAdd(property.Identifier.Value, new MemberInfo(property.Type.Identifier.Value, false, AccessibilityOf(property.Modifiers), ImmutableArray<string>.Empty, property.Modifiers.Any(modifier => modifier.Value == "Read")));
            foreach (var method in constellation.Body.Statements.OfType<FunctionDeclarationSyntax>())
                members.TryAdd(method.Identifier.Value, new MemberInfo(method.ReturnType?.Identifier.Value ?? "Nova", true, AccessibilityOf(method.Modifiers), method.Parameters.Select(parameter => parameter.Type.Identifier.Value).ToImmutableArray(), false));
            _constellations.TryAdd(constellation.Identifier.Value, new ConstellationInfo(members));
        }
    }

    private static string AccessibilityOf(ImmutableArray<StarToken> modifiers) => modifiers.FirstOrDefault(token => token.Value is "Public" or "Private" or "Protected")?.Value ?? "Private";

    private sealed record ConstellationInfo(Dictionary<string, MemberInfo> Members);
    private sealed record MemberInfo(string TypeName, bool IsMethod, string Accessibility, ImmutableArray<string> ParameterTypes, bool IsReadOnly);

    private sealed class TypeScope(TypeScope? parent = null)
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);
        public void Declare(string name, string type) => _values.TryAdd(name, type);
        public string Lookup(string name) => _values.TryGetValue(name, out var type) ? type : parent?.Lookup(name) ?? Unknown;
        public bool IsType(string name) => Lookup(name) != Unknown;
    }
}
