namespace Star.Compiler.Syntax;

/// <summary>Node categories in Star's platform-independent syntax tree.</summary>
public enum SyntaxKind
{
    CompilationUnit, Block, Type, QualifiedName, Parameter,
    NamespaceDeclaration, ConstellationDeclaration, FunctionDeclaration, VariableDeclaration, PropertyDeclaration,
    EmitStatement, ReturnStatement, WhenStatement, ElseWhenClause, OtherwiseClause, WhileStatement,
    OrbitStatement, SpinWhileStatement, ExploreStatement, AssignmentStatement, ExpressionStatement,
    LiteralExpression, NameExpression, ParenthesizedExpression, UnaryExpression, BinaryExpression,
    CallExpression, MemberAccessExpression, IndexExpression, NewExpression, ListExpression
}
