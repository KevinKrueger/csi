using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csi.Interpreter.Nodes
{
    // Basisklasse für alle AST-Knoten
    public abstract class ASTNode
    {
        public int LineNumber { get; set; }
    }

    public class ProgramNode : ASTNode
    {
        public List<ASTNode> Statements = new List<ASTNode>();
    }

    // Verschiedene Arten von Knoten
    public abstract class ExpressionNode : ASTNode { }

    public class NumberNode : ExpressionNode
    {
        public double Value;
    }

    public class StringNode : ExpressionNode
    {
        public string Value;
    }

    public class VariableNode : ExpressionNode
    {
        public string Name;
    }

    public class BinaryOperationNode : ExpressionNode
    {
        public ExpressionNode Left;
        public string Operator;
        public ExpressionNode Right;
    }

    public class UnaryOperationNode : ExpressionNode
    {
        public string Operator;
        public ExpressionNode Operand;
    }

    public class AssignmentNode : ASTNode
    {
        public string VariableName;
        public ExpressionNode Expression;
    }

    public class IfNode : ASTNode
    {
        public ExpressionNode Condition;
        public ASTNode ThenBranch;
        public ASTNode ElseBranch;
    }

    public class WhileNode : ASTNode
    {
        public ExpressionNode Condition;
        public ASTNode Body;
    }

    public class ForNode : ASTNode
    {
        public ASTNode Initializer;
        public ExpressionNode Condition;
        public ExpressionNode Iterator;
        public ASTNode Body;
    }

    public class BlockNode : ASTNode
    {
        public List<ASTNode> Statements;
    }

    public class FunctionNode : ASTNode
    {
        public string Name;
        public List<string> Parameters;
        public ASTNode Body;
    }

    public class ReturnNode : ASTNode
    {
        public ExpressionNode Expression;
    }

    public class FunctionCallNode : ExpressionNode
    {
        public string FunctionName;
        public List<ExpressionNode> Arguments;
    }

    public class PrintNode : ASTNode
    {
        public ExpressionNode Expression;
    }

    public class ClassNode : ASTNode
    {
        public string Name;
        public string BaseClassName;
        public List<ClassMemberNode> Members;
    }

    public abstract class ClassMemberNode : ASTNode
    {
        public string Name;
    }

    public class FieldNode : ClassMemberNode
    {
        public string AccessModifier;
        public ExpressionNode InitialValue;
    }

    public class MethodNode : ClassMemberNode
    {
        public string AccessModifier;
        public bool IsVirtual;
        public bool IsOverride;
        public List<string> Parameters;
        public ASTNode Body;
    }

    public class NewExpressionNode : ExpressionNode
    {
        public string ClassName;
    }

    public class MemberAccessNode : ExpressionNode
    {
        public ExpressionNode Object;
        public string MemberName;
    }

    public class MethodCallNode : ExpressionNode
    {
        public ExpressionNode Target;
        public List<ExpressionNode> Arguments;
    }

    public class TryCatchFinallyNode : ASTNode
    {
        public ASTNode TryBlock;
        public string ExceptionVariable;
        public ASTNode CatchBlock;
        public ASTNode FinallyBlock;
    }

    public class ThrowNode : ASTNode
    {
        public ExpressionNode Expression;
    }

    public class BreakNode : ASTNode { }

    public class ContinueNode : ASTNode { }

    public class ImportNode : ASTNode
    {
        public string ModuleName;
    }

    public class InterfaceNode : ASTNode
    {
        public string Name;
        public List<MethodSignatureNode> Methods;
    }

    public class MethodSignatureNode : ASTNode
    {
        public string Name;
        public List<string> Parameters;
    }

    public class SuperCallNode : ExpressionNode
    {
        public string MethodName;
        public List<ExpressionNode> Arguments;
    }

    public class SuperExpressionNode : ExpressionNode { }
}

