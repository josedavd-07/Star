using System.Collections.Immutable;
using System.Text;
using Star.Compiler.Diagnostics;
using Star.Compiler.Ir;
using Star.Compiler.Text;

namespace Star.Compiler.Backends;

/// <summary>
/// Produces deterministic C# source for the small, explicitly supported subset of Star IR.
/// It is an incremental .NET backend: source persistence and assembly compilation remain host concerns.
/// </summary>
public sealed class DotNetSourceBackend : IStarBackend
{
    public static StarBackendDescriptor BackendDescriptor { get; } = new(
        "star.dotnet.source",
        "Managed .NET source",
        StarBackendCapabilities.None);

    public StarBackendDescriptor Descriptor => BackendDescriptor;

    public StarBackendEmission Emit(StarIrCompilationUnit compilation)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        var emitter = new Emitter();
        var source = emitter.Emit(compilation);
        return emitter.HasErrors
            ? new StarBackendEmission(ImmutableArray<StarBackendArtifact>.Empty, emitter.Diagnostics)
            : new StarBackendEmission([new StarBackendArtifact("Program.cs", "text/x-csharp", Encoding.UTF8.GetBytes(source).ToImmutableArray())], emitter.Diagnostics);
    }

    private sealed class Emitter
    {
        private readonly StringBuilder _source = new();
        private readonly List<Diagnostic> _diagnostics = [];
        private readonly HashSet<string> _constellations = new(StringComparer.Ordinal);
        private int _indent;

        public bool HasErrors => _diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        public ImmutableArray<Diagnostic> Diagnostics => _diagnostics.ToImmutableArray();

        public string Emit(StarIrCompilationUnit compilation)
        {
            foreach (var constellation in compilation.Statements.OfType<StarIrConstellation>()) _constellations.Add(constellation.Name);
            ValidateEntryPoint(compilation);
            Line("using System;");
            Line("using System.Linq;");
            Line();
            EmitRuntime();
            Line();
            foreach (var constellation in compilation.Statements.OfType<StarIrConstellation>())
            {
                EmitConstellation(constellation);
                Line();
            }
            Line("internal static class StarProgram");
            OpenBlock();
            foreach (var statement in compilation.Statements)
            {
                if (statement is StarIrNamespace) continue;
                if (statement is StarIrFunction function) EmitFunction(function, isStatic: true);
                else if (statement is not StarIrConstellation) Unsupported(statement, "only top-level StarFunction and Constellation declarations are supported");
            }
            CloseBlock();
            return _source.ToString();
        }

        private void ValidateEntryPoint(StarIrCompilationUnit compilation)
        {
            var main = compilation.Statements.OfType<StarIrFunction>().SingleOrDefault(function => function.Name == "Main");
            if (main is null) { Unsupported(compilation.Span, "A runnable Star program requires a top-level StarFunction Main()."); return; }
            if (main.Parameters.Length != 0 || main.ReturnType != "Nova")
                Unsupported(main, "StarFunction Main must have no parameters and return Nova for the managed .NET target.");
        }

        private void EmitRuntime()
        {
            Line("internal static class StarRuntime");
            OpenBlock();
            Line("public static int Length(object[] values) => values.Length;");
            Line("public static bool Contains(object[] values, object value) => values.Contains(value);");
            Line("public static string ToText(object value) => Convert.ToString(value) ?? string.Empty;");
            CloseBlock();
        }

        private void EmitConstellation(StarIrConstellation constellation)
        {
            Line($"public sealed class {constellation.Name}");
            OpenBlock();
            foreach (var member in constellation.Members)
            {
                switch (member)
                {
                    case StarIrVariable field:
                        var fieldModifiers = MemberModifiers(field.Modifiers, field.Span, field.Name, isMethod: false);
                        Line(field.Initializer is null
                            ? $"{fieldModifiers}{TypeName(field.TypeName, field.Span)} {field.Name};"
                            : $"{fieldModifiers}{TypeName(field.TypeName, field.Span)} {field.Name} = {Expression(field.Initializer)};");
                        break;
                    case StarIrProperty property:
                        var propertyModifiers = MemberModifiers(property.Modifiers, property.Span, property.Name, isMethod: false);
                        Line($"{propertyModifiers}{TypeName(property.TypeName, property.Span)} {property.Name} {{ get; {(property.Modifiers.Contains("Read") ? string.Empty : "set; ")} }}");
                        break;
                    case StarIrFunction method:
                        if (method.Name == "Constructor") EmitConstructor(constellation, method);
                        else EmitFunction(method, isStatic: false);
                        break;
                    default:
                        Unsupported(member, $"'{member.GetType().Name}' is not supported as a Constellation member by the managed .NET source backend");
                        break;
                }
            }
            CloseBlock();
        }

        private void EmitConstructor(StarIrConstellation constellation, StarIrFunction constructor)
        {
            if (constructor.ReturnType != "Nova")
            {
                Unsupported(constructor, $"Constructor of '{constellation.Name}' must return Nova");
                return;
            }
            if (constructor.Modifiers.Contains("Static"))
            {
                Unsupported(constructor, $"Constructor of '{constellation.Name}' cannot be Static");
                return;
            }
            var accessibility = constructor.Modifiers.Contains("Public") ? "public" : constructor.Modifiers.Contains("Protected") ? "protected" : "private";
            var parameters = string.Join(", ", constructor.Parameters.Select(parameter => $"{TypeName(parameter.TypeName, parameter.Span)} {parameter.Name}"));
            Line($"{accessibility} {constellation.Name}({parameters})");
            OpenBlock();
            foreach (var statement in constructor.Body) EmitStatement(statement);
            CloseBlock();
        }

        private void EmitFunction(StarIrFunction function, bool isStatic)
        {
            var returnType = TypeName(function.ReturnType, function.Span);
            var parameters = string.Join(", ", function.Parameters.Select(parameter => $"{TypeName(parameter.TypeName, parameter.Span)} {parameter.Name}"));
            var modifiers = isStatic ? "public static " : MemberModifiers(function.Modifiers, function.Span, function.Name, isMethod: true);
            Line($"{modifiers}{returnType} {function.Name}({parameters})");
            OpenBlock();
            foreach (var statement in function.Body) EmitStatement(statement);
            CloseBlock();
        }

        private void EmitStatement(StarIrStatement statement)
        {
            switch (statement)
            {
                case StarIrVariable variable:
                    Line(variable.Initializer is null
                        ? $"{TypeName(variable.TypeName, variable.Span)} {variable.Name};"
                        : $"{TypeName(variable.TypeName, variable.Span)} {variable.Name} = {Expression(variable.Initializer)};");
                    break;
                case StarIrAssignment assignment:
                    Line($"{Expression(assignment.Target)} = {Expression(assignment.Expression)};");
                    break;
                case StarIrEmit emit:
                    Line($"Console.{(emit.AppendNewLine ? "WriteLine" : "Write")}({Expression(emit.Expression)});");
                    break;
                case StarIrReturn result:
                    Line(result.Expression is null ? "return;" : $"return {Expression(result.Expression)};");
                    break;
                case StarIrConditional conditional:
                    Line($"if ({Expression(conditional.Condition)})");
                    OpenBlock();
                    foreach (var branchStatement in conditional.Then) EmitStatement(branchStatement);
                    CloseBlock();
                    foreach (var branch in conditional.ElseWhen)
                    {
                        Line($"else if ({Expression(branch.Condition)})");
                        OpenBlock();
                        foreach (var branchStatement in branch.Body) EmitStatement(branchStatement);
                        CloseBlock();
                    }
                    if (conditional.Otherwise is not null)
                    {
                        Line("else");
                        OpenBlock();
                        foreach (var branchStatement in conditional.Otherwise.Value) EmitStatement(branchStatement);
                        CloseBlock();
                    }
                    break;
                case StarIrWhile loop:
                    Line($"while ({Expression(loop.Condition)})");
                    EmitBlock(loop.Body);
                    break;
                case StarIrOrbit orbit:
                    Line($"for ({ForClause(orbit.Initializer)}; {(orbit.Condition is null ? string.Empty : Expression(orbit.Condition))}; {ForClause(orbit.Step)})");
                    EmitBlock(orbit.Body);
                    break;
                case StarIrSpinWhile loop:
                    Line("do");
                    EmitBlock(loop.Body);
                    Line($"while ({Expression(loop.Condition)});");
                    break;
                case StarIrExplore explore:
                    Line($"foreach (var {explore.VariableName} in {Expression(explore.Collection)})");
                    EmitBlock(explore.Body);
                    break;
                case StarIrExpressionStatement expression:
                    Line($"{Expression(expression.Expression)};");
                    break;
                default:
                    Unsupported(statement, $"'{statement.GetType().Name}' is not supported by the managed .NET source backend");
                    break;
            }
        }

        private void EmitBlock(ImmutableArray<StarIrStatement> statements)
        {
            OpenBlock();
            foreach (var statement in statements) EmitStatement(statement);
            CloseBlock();
        }

        private string ForClause(StarIrStatement? statement) => statement switch
        {
            null => string.Empty,
            StarIrVariable variable => variable.Initializer is null
                ? $"{TypeName(variable.TypeName, variable.Span)} {variable.Name}"
                : $"{TypeName(variable.TypeName, variable.Span)} {variable.Name} = {Expression(variable.Initializer)}",
            StarIrAssignment assignment => $"{Expression(assignment.Target)} = {Expression(assignment.Expression)}",
            StarIrExpressionStatement expression => Expression(expression.Expression),
            _ => UnsupportedForClause(statement)
        };

        private string UnsupportedForClause(StarIrStatement statement)
        {
            Unsupported(statement, $"'{statement.GetType().Name}' is not supported in an Orbit clause by the managed .NET source backend");
            return string.Empty;
        }

        private string Expression(StarIrExpression expression) => expression switch
        {
            StarIrLiteral literal => Literal(literal),
            StarIrName name => name.Name,
            StarIrUnary unary => $"{unary.Operator}{Expression(unary.Operand)}",
            StarIrBinary binary => $"({Expression(binary.Left)} {binary.Operator} {Expression(binary.Right)})",
            StarIrCall { Target: StarIrName { Name: "Length" } } call => $"StarRuntime.Length({string.Join(", ", call.Arguments.Select(Expression))})",
            StarIrCall { Target: StarIrName { Name: "Contains" } } call => $"StarRuntime.Contains({string.Join(", ", call.Arguments.Select(Expression))})",
            StarIrCall { Target: StarIrName { Name: "ToText" } } call => $"StarRuntime.ToText({string.Join(", ", call.Arguments.Select(Expression))})",
            StarIrCall call => $"{Expression(call.Target)}({string.Join(", ", call.Arguments.Select(Expression))})",
            StarIrMemberAccess member => $"{Expression(member.Target)}.{member.MemberName}",
            StarIrIndex index => $"{Expression(index.Target)}[{Expression(index.Index)}]",
            StarIrList list => $"new object[] {{ {string.Join(", ", list.Elements.Select(Expression))} }}",
            StarIrNew @new => $"new {TypeName(@new.TypeName, @new.Span)}({string.Join(", ", @new.Arguments.Select(Expression))})",
            _ => UnsupportedExpression(expression)
        };

        private string Literal(StarIrLiteral literal) => literal.TypeName switch
        {
            "String" => $"\"{literal.Value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            "Char" => $"'{literal.Value.Replace("'", "\\'", StringComparison.Ordinal)}'",
            "Bool" => literal.Value,
            _ => literal.Value
        };

        private string UnsupportedExpression(StarIrExpression expression)
        {
            Unsupported(expression, $"'{expression.GetType().Name}' is not supported by the managed .NET source backend");
            return "default";
        }

        private string TypeName(string starType, TextSpan span) => starType switch
        {
            "Int" => "int",
            "String" => "string",
            "Double" => "double",
            "Float" => "float",
            "Char" => "char",
            "Bool" => "bool",
            "Nova" => "void",
            "Object" => "object",
            "Galaxy" => "object[]",
            _ when _constellations.Contains(starType) => starType,
            _ => UnsupportedType(starType, span)
        };

        private string MemberModifiers(ImmutableArray<string> modifiers, TextSpan span, string name, bool isMethod)
        {
            var accessibility = modifiers.Contains("Public") ? "public " : modifiers.Contains("Protected") ? "protected " : "private ";
            var isStatic = modifiers.Contains("Static");
            if (isStatic && !isMethod) return accessibility + "static ";
            if (isStatic) return accessibility + "static ";
            return accessibility;
        }

        private string UnsupportedType(string type, TextSpan span)
        {
            Unsupported(span, $"Star type '{type}' is not supported by the managed .NET source backend");
            return "object";
        }

        private void Unsupported(StarIrNode node, string message) => Unsupported(node.Span, message);
        private void Unsupported(TextSpan span, string message) => _diagnostics.Add(new Diagnostic("STR6001", DiagnosticSeverity.Error, message, span));

        private void OpenBlock() { Line("{"); _indent++; }
        private void CloseBlock() { _indent--; Line("}"); }
        private void Line(string text = "") => _source.Append(' ', _indent * 4).AppendLine(text);
    }
}
