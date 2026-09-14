using System.Collections.Generic;

namespace PascalCompiler
{
    /// <summary>
    /// AST 节点基类
    /// </summary>
    public abstract class ASTNode
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }

    /// <summary>
    /// 程序节点
    /// </summary>
    public class ProgramNode : ASTNode
    {
        public string Name { get; set; } = "";
        public List<DeclarationNode> Declarations { get; set; } = new List<DeclarationNode>();
        public List<SubprogramDeclarationNode> Subprograms { get; set; } = new List<SubprogramDeclarationNode>();
        public BlockNode Block { get; set; } = new BlockNode();
    }

    public class UnitNode : ASTNode
    {
        public string Name { get; set; } = "";
        public List<DeclarationNode> InterfaceDeclarations { get; set; } = new();
        public List<SubprogramDeclarationNode> InterfaceSubprograms { get; set; } = new();
        public List<DeclarationNode> ImplementationDeclarations { get; set; } = new();
        public List<SubprogramDeclarationNode> ImplementationSubprograms { get; set; } = new();
        public BlockNode? InitializationBlock { get; set; }
    }

    /// <summary>
    /// 声明节点基类
    /// </summary>
    public abstract class DeclarationNode : ASTNode { }

    /// <summary>
    /// 子程序声明节点基类 (过程/函数)
    /// </summary>
    public abstract class SubprogramDeclarationNode : ASTNode
    {
        public string Name { get; set; } = "";
        public List<ParameterNode> Parameters { get; set; } = new List<ParameterNode>();
        public List<VarDeclarationNode> LocalVariables { get; set; } = new List<VarDeclarationNode>();
        public List<SubprogramDeclarationNode> NestedSubprograms { get; set; } = new List<SubprogramDeclarationNode>();
        public BlockNode Body { get; set; } = new BlockNode();
        public bool IsForward { get; set; }
        public bool IsConstructor { get; set; }
        public bool IsDestructor { get; set; }
    }

    /// <summary>
    /// 过程声明节点
    /// </summary>
    public class ProcedureDeclarationNode : SubprogramDeclarationNode { }

    /// <summary>
    /// 函数声明节点
    /// </summary>
    public class FunctionDeclarationNode : SubprogramDeclarationNode
    {
        public TypeNode ReturnType { get; set; } = new SimpleTypeNode();
    }

    /// <summary>
    /// 参数节点
    /// </summary>
    public class ParameterNode : ASTNode
    {
        public string Name { get; set; } = "";
        public TypeNode Type { get; set; } = new SimpleTypeNode();
        public bool IsVarParameter { get; set; } // var参数(引用传递)
    }

    /// <summary>
    /// 变量声明节点
    /// </summary>
    public class VarDeclarationNode : DeclarationNode
    {
        public string Name { get; set; } = "";
        public TypeNode Type { get; set; } = new SimpleTypeNode();
        public ExpressionNode? InitialValue { get; set; }
    }

    /// <summary>
    /// 常量声明节点
    /// </summary>
    public class ConstDeclarationNode : DeclarationNode
    {
        public string Name { get; set; } = "";
        public ExpressionNode Value { get; set; } = new LiteralNode();
        public TypeNode? ConstType { get; set; } = null; // 带类型标注的常量 (如 const arr: array[1..5] of integer = (...);)
        public List<ExpressionNode>? ArrayValues { get; set; } = null; // 数组初始化值列表
    }

    /// <summary>
    /// 类型声明节点
    /// </summary>
    public class TypeDeclarationNode : DeclarationNode
    {
        public string Name { get; set; } = "";
        public TypeNode Type { get; set; } = new SimpleTypeNode();
    }

    /// <summary>
    /// 类型节点基类
    /// </summary>
    public abstract class TypeNode : ASTNode { }

    /// <summary>
    /// 简单类型节点
    /// </summary>
    public class SimpleTypeNode : TypeNode
    {
        public string TypeName { get; set; } = "INTEGER"; // INTEGER, REAL, BOOLEAN, CHAR, STRING
    }

    /// <summary>
    /// 数组类型节点
    /// </summary>
    public class ArrayTypeNode : TypeNode
    {
        public bool IsDynamic { get; set; }
        public ExpressionNode LowerBound { get; set; } = new LiteralNode();
        public ExpressionNode UpperBound { get; set; } = new LiteralNode();
        public TypeNode ElementType { get; set; } = new SimpleTypeNode();
    }

    public class PointerTypeNode : TypeNode
    {
        public TypeNode TargetType { get; set; } = new SimpleTypeNode();
    }

    public class SubrangeTypeNode : TypeNode
    {
        public ExpressionNode LowerBound { get; set; } = new LiteralNode();
        public ExpressionNode UpperBound { get; set; } = new LiteralNode();
    }

    public class ProcedureTypeNode : TypeNode
    {
        public bool IsFunction { get; set; }
        public bool IsConstructor { get; set; }
        public bool IsDestructor { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsOverride { get; set; }
        public List<ParameterNode> Parameters { get; set; } = new List<ParameterNode>();
        public TypeNode? ReturnType { get; set; }
    }

    /// <summary>
    /// 集合类型节点
    /// </summary>
    public class SetTypeNode : TypeNode
    {
        public ExpressionNode LowerBound { get; set; } = new LiteralNode();
        public ExpressionNode UpperBound { get; set; } = new LiteralNode();
        public TypeNode BaseType { get; set; } = new SimpleTypeNode();
    }

    /// <summary>
    /// 记录类型节点
    /// </summary>
    public class RecordTypeNode : TypeNode
    {
        public List<VarDeclarationNode> Fields { get; set; } = new List<VarDeclarationNode>();
    }

    /// <summary>
    /// 文件类型节点
    /// </summary>
    public class FileTypeNode : TypeNode
    {
        public TypeNode? ElementType { get; set; }  // null表示TEXT文件，非null表示FILE OF <type>
        public bool IsText { get { return ElementType == null; } }
    }

    /// <summary>
    /// 枚举类型节点
    /// </summary>
    public class EnumTypeNode : TypeNode
    {
        public List<string> Values { get; set; } = new List<string>();
    }

    /// <summary>
    /// 块节点
    /// </summary>
    public class BlockNode : ASTNode
    {
        public List<StatementNode> Statements { get; set; } = new List<StatementNode>();
    }

    /// <summary>
    /// 语句节点基类
    /// </summary>
    public abstract class StatementNode : ASTNode { }

    /// <summary>
    /// 赋值语句节点
    /// </summary>
    public class AssignmentNode : StatementNode
    {
        public VariableNode Variable { get; set; } = new VariableNode();
        public ExpressionNode Expression { get; set; } = new LiteralNode();
    }

    /// <summary>
    /// 过程调用语句节点
    /// </summary>
    public class ProcedureCallNode : StatementNode
    {
        public string Name { get; set; } = "";
        public List<ExpressionNode> Arguments { get; set; } = new List<ExpressionNode>();
    }

    /// <summary>
    /// 复合语句节点
    /// </summary>
    public class CompoundStatementNode : StatementNode
    {
        public List<StatementNode> Statements { get; set; } = new List<StatementNode>();
    }

    /// <summary>
    /// 返回语句节点 (用于函数)
    /// </summary>
    public class ReturnNode : StatementNode
    {
        public ExpressionNode? Value { get; set; }
    }

    /// <summary>
    /// If 语句节点
    /// </summary>
    public class IfNode : StatementNode
    {
        public ExpressionNode Condition { get; set; } = new LiteralNode();
        public StatementNode ThenBranch { get; set; } = new CompoundStatementNode();
        public StatementNode? ElseBranch { get; set; }
    }

    /// <summary>
    /// While 语句节点
    /// </summary>
    public class WhileNode : StatementNode
    {
        public ExpressionNode Condition { get; set; } = new LiteralNode();
        public StatementNode Body { get; set; } = new CompoundStatementNode();
    }

    /// <summary>
    /// For 语句节点
    /// </summary>
    public class ForNode : StatementNode
    {
        public string Variable { get; set; } = "";
        public ExpressionNode StartValue { get; set; } = new LiteralNode();
        public ExpressionNode EndValue { get; set; } = new LiteralNode();
        public bool IsDownTo { get; set; }
        public StatementNode Body { get; set; } = new CompoundStatementNode();
    }

    /// <summary>
    /// Repeat 语句节点
    /// </summary>
    public class BreakNode : StatementNode { }
    public class ContinueNode : StatementNode { }

    public class RepeatNode : StatementNode
    {
        public List<StatementNode> Statements { get; set; } = new List<StatementNode>();
        public ExpressionNode Condition { get; set; } = new LiteralNode();
    }

    /// <summary>
    /// Case分支节点
    /// </summary>
    public class CaseBranchNode : ASTNode
    {
        public List<ExpressionNode> Values { get; set; } = new List<ExpressionNode>(); // 支持多个值: 1, 3..5
        public StatementNode Statement { get; set; } = new CompoundStatementNode();
    }

    /// <summary>
    /// Case 语句节点
    /// </summary>
    public class CaseNode : StatementNode
    {
        public ExpressionNode Expression { get; set; } = new LiteralNode();
        public List<CaseBranchNode> Branches { get; set; } = new List<CaseBranchNode>();
        public StatementNode? OtherwiseBranch { get; set; }
    }

    public class WithNode : StatementNode
    {
        public List<VariableNode> Variables { get; set; } = new List<VariableNode>();
        public StatementNode Body { get; set; } = new CompoundStatementNode();
    }

    public class GotoNode : StatementNode
    {
        public string Label { get; set; } = "";
    }

    public class LabeledStatementNode : StatementNode
    {
        public string Label { get; set; } = "";
        public StatementNode Statement { get; set; } = new CompoundStatementNode();
    }

    /// <summary>
    /// 表达式节点基类
    /// </summary>
    public abstract class ExpressionNode : ASTNode { }

    /// <summary>
    /// 二元运算节点
    /// </summary>
    public class BinaryOpNode : ExpressionNode
    {
        public TokenType Operator { get; set; }
        public ExpressionNode Left { get; set; } = new LiteralNode();
        public ExpressionNode Right { get; set; } = new LiteralNode();
    }

    /// <summary>
    /// 一元运算节点
    /// </summary>
    public class UnaryOpNode : ExpressionNode
    {
        public TokenType Operator { get; set; }
        public ExpressionNode Operand { get; set; } = new LiteralNode();
    }

    /// <summary>
    /// 变量节点
    /// </summary>
    public class VariableNode : ExpressionNode
    {
        public string Name { get; set; } = "";
        public List<ExpressionNode> Indices { get; set; } = new List<ExpressionNode>(); // 数组索引
        public string? Field { get; set; } // 记录字段 (向后兼容)
        public List<string> Fields { get; set; } = new List<string>(); // 多级记录字段链 b.a.v
        public int DereferenceCount { get; set; }
    }

    public class AddressOfNode : ExpressionNode
    {
        public VariableNode Variable { get; set; } = new VariableNode();
    }

    public class DereferenceNode : ExpressionNode
    {
        public ExpressionNode Pointer { get; set; } = new LiteralNode();
    }

    /// <summary>
    /// 字面量节点
    /// </summary>
    public class LiteralNode : ExpressionNode
    {
        public object Value { get; set; } = 0;
        public TokenType Type { get; set; } = TokenType.INTEGER_LITERAL;
    }

    /// <summary>
    /// 函数调用节点
    /// </summary>
    public class FunctionCallNode : ExpressionNode
    {
        public string Name { get; set; } = "";
        public List<ExpressionNode> Arguments { get; set; } = new List<ExpressionNode>();
    }

    public class SetExpressionNode : ExpressionNode
    {
        public List<ExpressionNode> Elements { get; set; } = new List<ExpressionNode>();
    }
}
