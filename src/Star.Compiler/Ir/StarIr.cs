using System.Collections.Immutable;
using Star.Compiler.Diagnostics;
using Star.Compiler.Semantics;
using Star.Compiler.Text;

namespace Star.Compiler.Ir;

/// <summary>
/// Platform-independent, immutable intermediate representation for a valid Star compilation.
/// It describes program intent only; it neither executes code nor selects a target platform.
/// </summary>
public sealed record StarIrCompilationUnit(
    ImmutableArray<StarIrStatement> Statements,
    TextSpan Span);

/// <summary>The complete outcome of lowering, including diagnostics when no IR can be produced.</summary>
public sealed record StarIrLoweringResult(
    StarIrCompilationUnit? Ir,
    ImmutableArray<Diagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}

public abstract record StarIrNode(TextSpan Span);
public abstract record StarIrStatement(TextSpan Span) : StarIrNode(Span);

public sealed record StarIrNamespace(string Name, TextSpan Span) : StarIrStatement(Span);
public sealed record StarIrConstellation(string Name, ImmutableArray<StarIrStatement> Members, Symbol? Symbol, TextSpan Span) : StarIrStatement(Span);
public sealed record StarIrFunction(string Name, ImmutableArray<string> Modifiers, ImmutableArray<StarIrParameter> Parameters, string ReturnType, ImmutableArray<StarIrStatement> Body, Symbol? Symbol, TextSpan Span) : StarIrStatement(Span);
public sealed record StarIrParameter(string Name, string TypeName, Symbol? Symbol, TextSpan Span) : StarIrNode(Span);
public sealed record StarIrVariable(string Name, ImmutableArray<string> Modifiers, string TypeName, StarIrExpression? Initializer, Symbol? Symbol, TextSpan Span) : StarIrStatement(Span);
public sealed record StarIrProperty(string Name, ImmutableArray<string> Modifiers, string TypeName, Symbol? Symbol, TextSpan Span) : StarIrStatement(Span);
public sealed record StarIrAssignment(StarIrExpression Target, StarIrExpression Expression, TextSpan Span) : StarIrStatement(Span);
public sealed record StarIrEmit(StarIrExpression Expression, bool AppendNewLine, TextSpan Span) : StarIrStatement(Span);
public sealed record StarIrReturn(StarIrExpression? Expression, TextSpan Span) : StarIrStatement(Span);
public sealed record StarIrConditional(StarIrExpression Condition, ImmutableArray<StarIrStatement> Then, ImmutableArray<StarIrConditionalBranch> ElseWhen, ImmutableArray<StarIrStatement>? Otherwise, TextSpan Span) : StarIrStatement(Span);
public sealed record StarIrConditionalBranch(StarIrExpression Condition, ImmutableArray<StarIrStatement> Body, TextSpan Span) : StarIrNode(Span);
public sealed record StarIrWhile(StarIrExpression Condition, ImmutableArray<StarIrStatement> Body, TextSpan Span) : StarIrStatement(Span);
public sealed record StarIrOrbit(StarIrStatement? Initializer, StarIrExpression? Condition, StarIrStatement? Step, ImmutableArray<StarIrStatement> Body, TextSpan Span) : StarIrStatement(Span);
public sealed record StarIrSpinWhile(ImmutableArray<StarIrStatement> Body, StarIrExpression Condition, TextSpan Span) : StarIrStatement(Span);
public sealed record StarIrExplore(string VariableName, StarIrExpression Collection, ImmutableArray<StarIrStatement> Body, Symbol? Symbol, TextSpan Span) : StarIrStatement(Span);
public sealed record StarIrExpressionStatement(StarIrExpression Expression, TextSpan Span) : StarIrStatement(Span);

public abstract record StarIrExpression(string TypeName, TextSpan Span) : StarIrNode(Span);
public sealed record StarIrLiteral(string Value, string TypeName, TextSpan Span) : StarIrExpression(TypeName, Span);
public sealed record StarIrName(string Name, Symbol? Symbol, string TypeName, TextSpan Span) : StarIrExpression(TypeName, Span);
public sealed record StarIrUnary(string Operator, StarIrExpression Operand, string TypeName, TextSpan Span) : StarIrExpression(TypeName, Span);
public sealed record StarIrBinary(StarIrExpression Left, string Operator, StarIrExpression Right, string TypeName, TextSpan Span) : StarIrExpression(TypeName, Span);
public sealed record StarIrCall(StarIrExpression Target, ImmutableArray<StarIrExpression> Arguments, string TypeName, TextSpan Span) : StarIrExpression(TypeName, Span);
public sealed record StarIrMemberAccess(StarIrExpression Target, string MemberName, string TypeName, TextSpan Span) : StarIrExpression(TypeName, Span);
public sealed record StarIrIndex(StarIrExpression Target, StarIrExpression Index, string TypeName, TextSpan Span) : StarIrExpression(TypeName, Span);
public sealed record StarIrNew(string TypeName, ImmutableArray<StarIrExpression> Arguments, TextSpan Span) : StarIrExpression(TypeName, Span);
public sealed record StarIrList(ImmutableArray<StarIrExpression> Elements, string TypeName, TextSpan Span) : StarIrExpression(TypeName, Span);
