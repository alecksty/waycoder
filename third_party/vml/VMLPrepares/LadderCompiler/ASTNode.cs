using System;
using System.Collections.Generic;

namespace LadderCompiler
{
    /// <summary>
    /// AST节点基类
    /// </summary>
    public abstract class ASTNode
    {
        public int Line { get; set; }
        public int Column { get; set; }
        
        public abstract void Accept(IASTVisitor visitor);
    }
    
    /// <summary>
    /// AST访问者接口
    /// </summary>
    public interface IASTVisitor
    {
        void Visit(ProgramNode node);
        void Visit(VariableDeclarationNode node);
        void Visit(LadderRungNode node);
        void Visit(ContactNode node);
        void Visit(CoilNode node);
        void Visit(FunctionBlockNode node);
        void Visit(AssignmentNode node);
        void Visit(ExpressionNode node);
        void Visit(StIfNode node);
        void Visit(StForNode node);
        void Visit(StWhileNode node);
        void Visit(IdentifierNode node);
        void Visit(LiteralNode node);
        void Visit(BinaryExpressionNode node);
        void Visit(UnaryExpressionNode node);
        void Visit(CallNode node);
        void Visit(ArrayAccessNode node);
        void Visit(MemberAccessNode node);
    }
    
    /// <summary>
    /// 程序节点
    /// </summary>
    public class ProgramNode : ASTNode
    {
        public string Name { get; set; }
        public List<VariableDeclarationNode> Variables { get; set; } = new List<VariableDeclarationNode>();
        public List<LadderRungNode> Rungs { get; set; } = new List<LadderRungNode>();
        public List<FunctionDefinitionNode> Functions { get; set; } = new List<FunctionDefinitionNode>();
        public List<ASTNode> StStatements { get; set; } = new List<ASTNode>(); // ST IF/WHILE/FOR in program body

        public override void Accept(IASTVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// FUNCTION 定义节点 (IEC 61131-3)
    /// </summary>
    public class FunctionDefinitionNode : ASTNode
    {
        public string Name { get; set; }
        public string ReturnType { get; set; } = "INT";
        public List<VariableDeclarationNode> InputParams { get; set; } = new List<VariableDeclarationNode>();
        public List<VariableDeclarationNode> LocalVars { get; set; } = new List<VariableDeclarationNode>();
        public List<LadderRungNode> Rungs { get; set; } = new List<LadderRungNode>();
        public List<ASTNode> StStatements { get; set; } = new List<ASTNode>(); // IF/WHILE/FOR ST 语句

        public override void Accept(IASTVisitor visitor)
        {
            // Not needed for basic codegen
        }
    }
    
    /// <summary>
    /// 变量声明节点
    /// </summary>
    public class VariableDeclarationNode : ASTNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string InitialValue { get; set; }
        public string MemoryLocation { get; set; } // 如 %I0.0, %Q0.0, %M0.0
        public bool IsInput { get; set; }
        public bool IsOutput { get; set; }
        public bool IsRetain { get; set; }
        
        public override void Accept(IASTVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 梯形图梯级节点
    /// </summary>
    public class LadderRungNode : ASTNode
    {
        public List<ASTNode> Elements { get; set; } = new List<ASTNode>();
        public ASTNode Output { get; set; }
        
        public override void Accept(IASTVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 触点节点
    /// </summary>
    public class ContactNode : ASTNode
    {
        public string Variable { get; set; }
        public bool NormallyOpen { get; set; } = true; // true = 常开, false = 常闭
        public bool IsPositiveTransition { get; set; }
        public bool IsNegativeTransition { get; set; }
        
        public override void Accept(IASTVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 线圈节点
    /// </summary>
    public class CoilNode : ASTNode
    {
        public string Variable { get; set; }
        public CoilType Type { get; set; } = CoilType.Normal;
        public ExpressionNode Value { get; set; }
        
        public override void Accept(IASTVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 线圈类型
    /// </summary>
    public enum CoilType
    {
        Normal,     // ( )
        Set,        // (S)
        Reset,      // (R)
        Positive,   // (P)
        Negative    // (N)
    }
    
    /// <summary>
    /// 函数块节点
    /// </summary>
    public class FunctionBlockNode : ASTNode
    {
        public string Name { get; set; }
        public Dictionary<string, ExpressionNode> Parameters { get; set; } = new Dictionary<string, ExpressionNode>();
        public Dictionary<string, string> Outputs { get; set; } = new Dictionary<string, string>();
        
        public override void Accept(IASTVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 赋值节点
    /// </summary>
    public class AssignmentNode : ASTNode
    {
        public string Variable { get; set; }
        public ExpressionNode Value { get; set; }
        
        public override void Accept(IASTVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 表达式节点基类
    /// </summary>
    public abstract class ExpressionNode : ASTNode
    {
        public string Type { get; set; }
    }
    
    /// <summary>
    /// 标识符节点
    /// </summary>
    public class IdentifierNode : ExpressionNode
    {
        public string Name { get; set; }
        
        public override void Accept(IASTVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 字面量节点
    /// </summary>
    public class LiteralNode : ExpressionNode
    {
        public object Value { get; set; }
        
        public override void Accept(IASTVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 二元表达式节点
    /// </summary>
    public class BinaryExpressionNode : ExpressionNode
    {
        public ExpressionNode Left { get; set; }
        public string Operator { get; set; }
        public ExpressionNode Right { get; set; }
        
        public override void Accept(IASTVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 一元表达式节点
    /// </summary>
    public class UnaryExpressionNode : ExpressionNode
    {
        public string Operator { get; set; }
        public ExpressionNode Operand { get; set; }
        
        public override void Accept(IASTVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 函数调用节点
    /// </summary>
    public class CallNode : ExpressionNode
    {
        public string FunctionName { get; set; }
        public List<ExpressionNode> Arguments { get; set; } = new List<ExpressionNode>();
        
        public override void Accept(IASTVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// 数组元素访问节点
    /// </summary>
    public class ArrayAccessNode : ExpressionNode
    {
        public ExpressionNode Target { get; set; }
        public ExpressionNode Index { get; set; }

        public override void Accept(IASTVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// 结构体/功能块成员访问节点
    /// </summary>
    public class MemberAccessNode : ExpressionNode
    {
        public ExpressionNode Target { get; set; }
        public string Member { get; set; }

        public override void Accept(IASTVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// ST (Structured Text) IF-THEN-ELSE 语句
    /// </summary>
    public class StIfNode : ASTNode
    {
        public ExpressionNode Condition { get; set; }
        public List<ASTNode> ThenBody { get; set; } = new();
        public List<ASTNode> ElseBody { get; set; } = new();
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }

    /// <summary>
    /// ST (Structured Text) FOR 循环语句
    /// </summary>
    public class StForNode : ASTNode
    {
        public string VarName { get; set; }
        public ExpressionNode Start { get; set; }
        public ExpressionNode End { get; set; }
        public List<ASTNode> Body { get; set; } = new();
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }

    /// <summary>
    /// ST (Structured Text) WHILE 循环语句
    /// </summary>
    public class StWhileNode : ASTNode
    {
        public ExpressionNode Condition { get; set; }
        public List<ASTNode> Body { get; set; } = new();
        public override void Accept(IASTVisitor visitor) => visitor.Visit(this);
    }
}
