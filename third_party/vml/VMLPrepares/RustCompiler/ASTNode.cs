using System;
using System.Collections.Generic;

namespace RustCompiler
{
    /// <summary>
    /// AST节点基类
    /// </summary>
    public abstract class ASTNode
    {
        public int Line { get; set; }
        public int Column { get; set; }
        
        public abstract void Accept(IVisitor visitor);
    }
    
    /// <summary>
    /// 访问者接口
    /// </summary>
    public interface IVisitor
    {
        void Visit(ProgramNode node);
        void Visit(FunctionNode node);
        void Visit(VariableDeclarationNode node);
        void Visit(AssignmentNode node);
        void Visit(BinaryOperationNode node);
        void Visit(UnaryOperationNode node);
        void Visit(LiteralNode node);
        void Visit(IdentifierNode node);
        void Visit(IfStatementNode node);
        void Visit(WhileStatementNode node);
        void Visit(ForStatementNode node);
        void Visit(ReturnStatementNode node);
        void Visit(ExpressionStatementNode node);
        void Visit(BlockNode node);
        void Visit(CallExpressionNode node);
        void Visit(PrintlnStatementNode node);
        void Visit(CompoundAssignmentNode node);
        void Visit(ConstantDeclarationNode node);
        void Visit(LoopStatementNode node);
        void Visit(BreakStatementNode node);
        void Visit(ContinueStatementNode node);
        void Visit(MatchStatementNode node);
        void Visit(EnumDeclNode node);
        void Visit(StructDeclNode node);
        void Visit(ImplBlockNode node);
        void Visit(TraitDeclNode node);
        void Visit(ArrayLiteralNode node);
        void Visit(IndexAccessNode node);
        void Visit(StructLiteralNode node);
        void Visit(TupleExprNode node);
        void Visit(ClosureExprNode node);
        void Visit(MemberAccessNode node);
        void Visit(EnumPatternNode node);
        void Visit(IfLetStatementNode node);
    }

    public class StructLiteralNode : ASTNode
    {
        public string StructName { get; set; }
        public List<(string Name, ASTNode Value)> Fields { get; set; } = new();
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    /// <summary>
    /// 程序节点
    /// </summary>
    public class ProgramNode : ASTNode
    {
        public List<ASTNode> Statements { get; } = new List<ASTNode>();
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 函数节点
    /// </summary>
    public class FunctionNode : ASTNode
    {
        public string Name { get; set; }
        public List<ParameterNode> Parameters { get; } = new List<ParameterNode>();
        public string ReturnType { get; set; }
        public BlockNode Body { get; set; }
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 参数节点
    /// </summary>
    public class ParameterNode : ASTNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        
        public override void Accept(IVisitor visitor)
        {
            // 参数节点通常由函数节点处理
        }
    }
    
    /// <summary>
    /// 变量声明节点
    /// </summary>
    public class VariableDeclarationNode : ASTNode
    {
        public string Name { get; set; }
        public string Type { get; set; } = "i32"; // 默认类型
        public bool IsMutable { get; set; }
        public ASTNode Initializer { get; set; }
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 常量声明节点
    /// </summary>
    public class ConstantDeclarationNode : ASTNode
    {
        public string Name { get; set; }
        public string Type { get; set; } = "i32"; // 默认类型
        public ASTNode Initializer { get; set; }
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 赋值节点
    /// </summary>
    public class AssignmentNode : ASTNode
    {
        public string VariableName { get; set; }
        public ASTNode Value { get; set; }
        public ASTNode? Target { get; set; } // 成员访问目标 (p.x = ...)

        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// 二元运算节点
    /// </summary>
    public class BinaryOperationNode : ASTNode
    {
        public string Operator { get; set; }
        public ASTNode Left { get; set; }
        public ASTNode Right { get; set; }
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 一元运算节点
    /// </summary>
    public class UnaryOperationNode : ASTNode
    {
        public string Operator { get; set; }
        public ASTNode Operand { get; set; }
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 字面量节点
    /// </summary>
    public class LiteralNode : ASTNode
    {
        public object Value { get; set; }
        public string Type { get; set; } // "int", "float", "string", "char", "bool"
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 标识符节点
    /// </summary>
    public class IdentifierNode : ASTNode
    {
        public string Name { get; set; } = "";
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// if语句节点
    /// </summary>
    public class IfStatementNode : ASTNode
    {
        public ASTNode Condition { get; set; }
        public BlockNode ThenBlock { get; set; }
        public ASTNode ElseBlock { get; set; } // 可以是BlockNode或IfStatementNode
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// while语句节点
    /// </summary>
    public class WhileStatementNode : ASTNode
    {
        public ASTNode Condition { get; set; }
        public BlockNode Body { get; set; }
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// for语句节点
    /// </summary>
    public class ForStatementNode : ASTNode
    {
        public string VariableName { get; set; }
        public ASTNode RangeStart { get; set; }
        public ASTNode RangeEnd { get; set; }
        public bool Inclusive { get; set; } // true表示..=，false表示..
        public BlockNode Body { get; set; }
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// return语句节点
    /// </summary>
    public class ReturnStatementNode : ASTNode
    {
        public ASTNode Value { get; set; }
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 表达式语句节点
    /// </summary>
    public class ExpressionStatementNode : ASTNode
    {
        public ASTNode Expression { get; set; }
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 代码块节点
    /// </summary>
    public class BlockNode : ASTNode
    {
        public List<ASTNode> Statements { get; } = new List<ASTNode>();
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 函数调用表达式节点
    /// </summary>
    public class CallExpressionNode : ASTNode
    {
        public string FunctionName { get; set; }
        public List<ASTNode> Arguments { get; } = new List<ASTNode>();
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// println!宏语句节点
    /// </summary>
    public class PrintlnStatementNode : ASTNode
    {
        public List<ASTNode> Arguments { get; } = new List<ASTNode>();
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    /// <summary>
    /// 复合赋值语句节点 (+=, -=, *=, /=, %=)
    /// </summary>
    public class CompoundAssignmentNode : ASTNode
    {
        public string VariableName { get; set; }
        public string Operator { get; set; }  // "+=", "-=", "*=", "/=", "%="
        public ASTNode Value { get; set; }
        
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class LoopStatementNode : ASTNode
    {
        public BlockNode Body { get; set; }
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    public class BreakStatementNode : ASTNode
    {
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    public class ContinueStatementNode : ASTNode
    {
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    public class MatchStatementNode : ASTNode
    {
        public ASTNode Value { get; set; }
        public List<MatchArm> Arms { get; set; } = new();
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    public class MatchArm
    {
        public ASTNode Pattern { get; set; } // LiteralNode or IdentifierNode ("_")
        public ASTNode Body { get; set; }    // Expression or Block
    }

    public class EnumVariant
    {
        public string Name { get; set; }
        public string? PayloadType { get; set; } // null = unit variant, non-null = data-carrying
    }

    public class EnumDeclNode : ASTNode
    {
        public string Name { get; set; }
        public List<EnumVariant> Variants { get; set; } = new();
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    public class EnumPatternNode : ASTNode
    {
        public string VariantName { get; set; }
        public string? BindName { get; set; } // the variable to bind payload to
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    public class IfLetStatementNode : ASTNode
    {
        public ASTNode Pattern { get; set; }
        public ASTNode Value { get; set; }
        public BlockNode ThenBlock { get; set; }
        public ASTNode? ElseBlock { get; set; }
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    public class StructDeclNode : ASTNode
    {
        public string Name { get; set; }
        public List<StructField> Fields { get; set; } = new();
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    public class StructField
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class ImplBlockNode : ASTNode
    {
        public string StructName { get; set; }
        public string? TraitName { get; set; }
        public List<FunctionNode> Methods { get; set; } = new();
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    public class TraitDeclNode : ASTNode
    {
        public string Name { get; set; }
        public List<TraitMethod> Methods { get; set; } = new();
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    public class TraitMethod
    {
        public string Name { get; set; }
        public List<ParameterNode> Parameters { get; set; } = new();
        public string ReturnType { get; set; } = "void";
    }

    public class ArrayLiteralNode : ASTNode
    {
        public List<ASTNode> Elements { get; set; } = new();
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    public class IndexAccessNode : ASTNode
    {
        public ASTNode Target { get; set; }
        public ASTNode Index { get; set; }
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    public class ClosureExprNode : ASTNode
    {
        public List<string> Parameters { get; set; } = new();
        public ASTNode Body { get; set; }
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    public class TupleExprNode : ASTNode
    {
        public List<ASTNode> Elements { get; set; } = new();
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }

    public class MemberAccessNode : ASTNode
    {
        public ASTNode Target { get; set; }
        public string Member { get; set; }
        public List<ASTNode> Arguments { get; set; } = new();
        public override void Accept(IVisitor visitor) { visitor.Visit(this); }
    }
}