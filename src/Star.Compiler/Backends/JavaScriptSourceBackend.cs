using System.Collections.Immutable;
using System.Text;
using Star.Compiler.Diagnostics;
using Star.Compiler.Ir;
using Star.Compiler.Text;

namespace Star.Compiler.Backends;

/// <summary>Small browser target for the supported Star IR subset. It writes no files.</summary>
public sealed class JavaScriptSourceBackend : IStarBackend
{
    public static StarBackendDescriptor BackendDescriptor { get; } = new("star.web.javascript", "Browser JavaScript", StarBackendCapabilities.None);
    public StarBackendDescriptor Descriptor => BackendDescriptor;

    public StarBackendEmission Emit(StarIrCompilationUnit compilation)
    {
        var emitter = new Emitter();
        var script = emitter.Emit(compilation);
        return emitter.HasErrors ? new StarBackendEmission([], emitter.Diagnostics) : new StarBackendEmission(
            [new StarBackendArtifact("app.js", "text/javascript", Encoding.UTF8.GetBytes(script).ToImmutableArray()),
             new StarBackendArtifact("index.html", "text/html", Encoding.UTF8.GetBytes("<!doctype html><meta charset=\"utf-8\"><title>Star Web</title><pre id=\"star-output\"></pre><script src=\"app.js\"></script>").ToImmutableArray())], []);
    }

    private sealed class Emitter
    {
        private readonly StringBuilder _source = new("const output = document.getElementById('star-output');\nconst emit = value => output.textContent += String(value);\nconst emitLn = value => output.textContent += String(value) + '\\n';\nconst Length = values => values.length;\nconst Contains = (values, value) => values.includes(value);\nconst ToText = value => String(value);\n\n");
        private readonly List<Diagnostic> _diagnostics = [];
        public bool HasErrors => _diagnostics.Count > 0;
        public ImmutableArray<Diagnostic> Diagnostics => _diagnostics.ToImmutableArray();
        public string Emit(StarIrCompilationUnit unit)
        {
            var main = unit.Statements.OfType<StarIrFunction>().SingleOrDefault(function => function.Name == "Main");
            if (main is null) Unsupported(unit.Span, "A runnable Star web program requires a top-level StarFunction Main().");
            else if (main.Parameters.Length != 0 || main.ReturnType != "Nova") Unsupported(main, "StarFunction Main must have no parameters and return Nova for the browser target.");
            foreach (var statement in unit.Statements)
            {
                if (statement is StarIrNamespace) continue;
                if (statement is StarIrConstellation constellation) Constellation(constellation);
                if (statement is StarIrFunction function) Function(function);
                else if (statement is not StarIrConstellation) Unsupported(statement, "the browser target currently supports top-level StarFunction and Constellation declarations only");
            }
            if (!HasErrors) _source.Append("\nMain();\n");
            return _source.ToString();
        }
        private void Function(StarIrFunction function)
        {
            _source.Append("function ").Append(function.Name).Append('(').AppendJoin(", ", function.Parameters.Select(parameter => parameter.Name)).Append(") {\n");
            foreach (var statement in function.Body) Statement(statement);
            _source.Append("}\n\n");
        }
        private void Constellation(StarIrConstellation constellation)
        {
            _source.Append("class ").Append(constellation.Name).Append(" {\n");
            foreach (var member in constellation.Members)
            {
                switch (member)
                {
                    case StarIrVariable field:
                        _source.Append(field.Modifiers.Contains("Static") ? "static " : string.Empty).Append(field.Name);
                        _source.Append(field.Initializer is null ? ";\n" : " = " + Expression(field.Initializer) + ";\n");
                        break;
                    case StarIrProperty property:
                        _source.Append(property.Modifiers.Contains("Static") ? "static " : string.Empty).Append(property.Name).Append(";\n");
                        break;
                    case StarIrFunction method when method.Name == "Constructor": Method(method, "constructor"); break;
                    case StarIrFunction method: Method(method, method.Name); break;
                    default: Unsupported(member, $"'{member.GetType().Name}' is not supported as a web Constellation member"); break;
                }
            }
            _source.Append("}\n\n");
        }
        private void Method(StarIrFunction function, string name)
        {
            _source.Append(function.Modifiers.Contains("Static") ? "static " : string.Empty).Append(name).Append('(').AppendJoin(", ", function.Parameters.Select(parameter => parameter.Name)).Append(") {\n");
            foreach (var statement in function.Body) Statement(statement);
            _source.Append("}\n");
        }
        private void Statement(StarIrStatement statement)
        {
            switch (statement)
            {
                case StarIrVariable variable: _source.Append("let ").Append(variable.Name).Append(variable.Initializer is null ? ";\n" : " = " + Expression(variable.Initializer) + ";\n"); break;
                case StarIrAssignment assignment: _source.Append(Expression(assignment.Target)).Append(" = ").Append(Expression(assignment.Expression)).Append(";\n"); break;
                case StarIrEmit emit: _source.Append(emit.AppendNewLine ? "emitLn(" : "emit(").Append(Expression(emit.Expression)).Append(");\n"); break;
                case StarIrReturn result: _source.Append(result.Expression is null ? "return;\n" : "return " + Expression(result.Expression) + ";\n"); break;
                case StarIrExpressionStatement expression: _source.Append(Expression(expression.Expression)).Append(";\n"); break;
                case StarIrConditional conditional:
                    _source.Append("if (").Append(Expression(conditional.Condition)).Append(") {\n");
                    foreach (var nested in conditional.Then) Statement(nested);
                    _source.Append("}\n");
                    foreach (var branch in conditional.ElseWhen) { _source.Append("else if (").Append(Expression(branch.Condition)).Append(") {\n"); foreach (var nested in branch.Body) Statement(nested); _source.Append("}\n"); }
                    if (conditional.Otherwise is not null) { _source.Append("else {\n"); foreach (var nested in conditional.Otherwise.Value) Statement(nested); _source.Append("}\n"); }
                    break;
                case StarIrWhile loop:
                    _source.Append("while (").Append(Expression(loop.Condition)).Append(") {\n"); foreach (var nested in loop.Body) Statement(nested); _source.Append("}\n"); break;
                case StarIrOrbit orbit:
                    _source.Append("for (").Append(ForClause(orbit.Initializer)).Append("; ").Append(orbit.Condition is null ? string.Empty : Expression(orbit.Condition)).Append("; ").Append(ForClause(orbit.Step)).Append(") {\n"); foreach (var nested in orbit.Body) Statement(nested); _source.Append("}\n"); break;
                case StarIrSpinWhile loop:
                    _source.Append("do {\n"); foreach (var nested in loop.Body) Statement(nested); _source.Append("} while (").Append(Expression(loop.Condition)).Append(");\n"); break;
                case StarIrExplore explore:
                    _source.Append("for (const ").Append(explore.VariableName).Append(" of ").Append(Expression(explore.Collection)).Append(") {\n"); foreach (var nested in explore.Body) Statement(nested); _source.Append("}\n"); break;
                default: Unsupported(statement, $"'{statement.GetType().Name}' is not supported by the browser target"); break;
            }
        }
        private string ForClause(StarIrStatement? statement) => statement switch
        {
            null => string.Empty,
            StarIrVariable variable => "let " + variable.Name + (variable.Initializer is null ? string.Empty : " = " + Expression(variable.Initializer)),
            StarIrAssignment assignment => Expression(assignment.Target) + " = " + Expression(assignment.Expression),
            StarIrExpressionStatement expression => Expression(expression.Expression),
            _ => UnsupportedForClause(statement)
        };
        private string UnsupportedForClause(StarIrStatement statement) { Unsupported(statement, $"'{statement.GetType().Name}' is not supported in a web Orbit clause"); return string.Empty; }
        private string Expression(StarIrExpression expression) => expression switch
        {
            StarIrLiteral literal when literal.TypeName == "String" => $"\"{literal.Value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
            StarIrLiteral literal => literal.Value,
            StarIrName name => name.Name,
            StarIrBinary binary => $"({Expression(binary.Left)} {binary.Operator} {Expression(binary.Right)})",
            StarIrUnary unary => unary.Operator + Expression(unary.Operand),
            StarIrCall call => $"{Expression(call.Target)}({string.Join(", ", call.Arguments.Select(Expression))})",
            StarIrList list => $"[{string.Join(", ", list.Elements.Select(Expression))}]",
            StarIrIndex index => $"{Expression(index.Target)}[{Expression(index.Index)}]",
            StarIrMemberAccess member => $"{Expression(member.Target)}.{member.MemberName}",
            StarIrNew @new => $"new {@new.TypeName}({string.Join(", ", @new.Arguments.Select(Expression))})",
            _ => UnsupportedExpression(expression)
        };
        private string UnsupportedExpression(StarIrExpression expression) { Unsupported(expression, $"'{expression.GetType().Name}' is not supported by the browser target"); return "undefined"; }
        private void Unsupported(StarIrNode node, string message) => _diagnostics.Add(new Diagnostic("STR7001", DiagnosticSeverity.Error, message, node.Span));
        private void Unsupported(TextSpan span, string message) => _diagnostics.Add(new Diagnostic("STR7001", DiagnosticSeverity.Error, message, span));
    }
}
