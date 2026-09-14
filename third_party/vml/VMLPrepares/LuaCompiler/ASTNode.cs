namespace LuaCompiler
{
    /// <summary>
    /// AST 节点基类
    /// </summary>
    public abstract class ASTNode
    {
        public int Line { get; }
        public int Column { get; }

        protected ASTNode(int line, int column)
        {
            Line = line;
            Column = column;
        }
    }

    /// <summary>
    /// 程序节点
    /// </summary>
    public class ProgramNode : ASTNode
    {
        public List<ASTNode> Statements { get; }

        public ProgramNode(List<ASTNode> statements, int line, int column)
            : base(line, column)
        {
            Statements = statements;
        }
    }

    /// <summary>
    /// 变量声明节点
    /// </summary>
    public class VariableDeclarationNode : ASTNode
    {
        public List<string> Names { get; }
        public List<ASTNode> Values { get; }
        public bool IsLocal { get; }

        public VariableDeclarationNode(List<string> names, List<ASTNode> values, bool isLocal, int line, int column)
            : base(line, column)
        {
            Names = names;
            Values = values;
            IsLocal = isLocal;
        }
    }

    /// <summary>
    /// 赋值语句节点
    /// </summary>
    public class AssignmentNode : ASTNode
    {
        public List<ASTNode> Variables { get; }
        public List<ASTNode> Values { get; }

        public AssignmentNode(List<ASTNode> variables, List<ASTNode> values, int line, int column)
            : base(line, column)
        {
            Variables = variables;
            Values = values;
        }
    }

    /// <summary>
    /// 函数调用节点
    /// </summary>
    public class FunctionCallNode : ASTNode
    {
        public ASTNode Function { get; }
        public List<ASTNode> Arguments { get; }

        public FunctionCallNode(ASTNode function, List<ASTNode> arguments, int line, int column)
            : base(line, column)
        {
            Function = function;
            Arguments = arguments;
        }
    }

    /// <summary>
    /// 匿名函数表达式节点（local f = function(...) ... end）
    /// </summary>
    public class FunctionExpressionNode : ASTNode
    {
        public string GeneratedName { get; set; }
        public List<string> Parameters { get; }
        public List<ASTNode> Body { get; }

        public FunctionExpressionNode(string generatedName, List<string> parameters, List<ASTNode> body, int line, int column)
            : base(line, column)
        {
            GeneratedName = generatedName;
            Parameters = parameters;
            Body = body;
        }
    }

    /// <summary>
    /// 标识符节点
    /// </summary>
    public class IdentifierNode : ASTNode
    {
        public string Name { get; }

        public IdentifierNode(string name, int line, int column)
            : base(line, column)
        {
            Name = name;
        }
    }

    /// <summary>
    /// 常量节点
    /// </summary>
    public class ConstantNode : ASTNode
    {
        public object Value { get; }
        public string Type { get; }

        public ConstantNode(object value, string type, int line, int column)
            : base(line, column)
        {
            Value = value;
            Type = type;
        }
    }

    /// <summary>
    /// 二元运算节点
    /// </summary>
    public class BinaryOperationNode : ASTNode
    {
        public ASTNode Left { get; }
        public TokenType Operator { get; }
        public ASTNode Right { get; }

        public BinaryOperationNode(ASTNode left, TokenType op, ASTNode right, int line, int column)
            : base(line, column)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }

    /// <summary>
    /// 一元运算节点
    /// </summary>
    public class UnaryOperationNode : ASTNode
    {
        public TokenType Operator { get; }
        public ASTNode Operand { get; }

        public UnaryOperationNode(TokenType op, ASTNode operand, int line, int column)
            : base(line, column)
        {
            Operator = op;
            Operand = operand;
        }
    }

    /// <summary>
    /// If语句节点
    /// </summary>
    public class IfStatementNode : ASTNode
    {
        public List<(ASTNode Condition, List<ASTNode> Body)> Conditions { get; }
        public List<ASTNode> ElseBody { get; }

        public IfStatementNode(List<(ASTNode, List<ASTNode>)> conditions, List<ASTNode> elseBody, int line, int column)
            : base(line, column)
        {
            Conditions = conditions;
            ElseBody = elseBody;
        }
    }

    /// <summary>
    /// While循环节点
    /// </summary>
    public class WhileStatementNode : ASTNode
    {
        public ASTNode Condition { get; }
        public List<ASTNode> Body { get; }

        public WhileStatementNode(ASTNode condition, List<ASTNode> body, int line, int column)
            : base(line, column)
        {
            Condition = condition;
            Body = body;
        }
    }

    /// <summary>
    /// Repeat循环节点
    /// </summary>
    public class RepeatStatementNode : ASTNode
    {
        public ASTNode Condition { get; }
        public List<ASTNode> Body { get; }

        public RepeatStatementNode(ASTNode condition, List<ASTNode> body, int line, int column)
            : base(line, column)
        {
            Condition = condition;
            Body = body;
        }
    }

    /// <summary>
    /// For循环节点
    /// </summary>
    public class ForStatementNode : ASTNode
    {
        public string Variable { get; }
        public ASTNode Start { get; }
        public ASTNode End { get; }
        public ASTNode Step { get; }
        public List<ASTNode> Body { get; }

        public ForStatementNode(string variable, ASTNode start, ASTNode end, ASTNode step, List<ASTNode> body, int line, int column)
            : base(line, column)
        {
            Variable = variable;
            Start = start;
            End = end;
            Step = step;
            Body = body;
        }
    }

    /// <summary>
    /// for-in 循环节点 (for k,v in pairs(t) do ... end)
    /// </summary>
    public class ForInStatementNode : ASTNode
    {
        public List<string> Variables { get; }
        public ASTNode IteratorExpr { get; }
        public List<ASTNode> Body { get; }

        public ForInStatementNode(List<string> variables, ASTNode iteratorExpr, List<ASTNode> body, int line, int column)
            : base(line, column)
        {
            Variables = variables;
            IteratorExpr = iteratorExpr;
            Body = body;
        }
    }

    /// <summary>
    /// Return语句节点
    /// </summary>
    public class ReturnStatementNode : ASTNode
    {
        public List<ASTNode> Values { get; }

        public ReturnStatementNode(List<ASTNode> values, int line, int column)
            : base(line, column)
        {
            Values = values;
        }
    }

    /// <summary>
    /// 函数定义节点
    /// </summary>
    public class FunctionDefinitionNode : ASTNode
    {
        public string Name { get; }
        public List<string> Parameters { get; }
        public List<ASTNode> Body { get; }
        public bool IsLocal { get; }

        public FunctionDefinitionNode(string name, List<string> parameters, List<ASTNode> body, bool isLocal, int line, int column)
            : base(line, column)
        {
            Name = name;
            Parameters = parameters;
            Body = body;
            IsLocal = isLocal;
        }
    }

    /// <summary>
    /// 表构造器节点
    /// </summary>
    public class TableConstructorNode : ASTNode
    {
        public List<(ASTNode Key, ASTNode Value)> Fields { get; }

        public TableConstructorNode(List<(ASTNode, ASTNode)> fields, int line, int column)
            : base(line, column)
        {
            Fields = fields;
        }
    }

    /// <summary>
    /// Break语句节点
    /// </summary>
    public class BreakStatementNode : ASTNode
    {
        public BreakStatementNode(int line, int column)
            : base(line, column)
        {
        }
    }

    /// <summary>
    /// goto 语句节点
    /// </summary>
    public class GotoStatementNode : ASTNode
    {
        public string LabelName { get; }
        public GotoStatementNode(string labelName, int line, int column)
            : base(line, column) { LabelName = labelName; }
    }

    /// <summary>
    /// 标签声明节点 ::label::
    /// </summary>
    public class LabelStatementNode : ASTNode
    {
        public string LabelName { get; }
        public LabelStatementNode(string labelName, int line, int column)
            : base(line, column) { LabelName = labelName; }
    }

    /// <summary>
    /// 表访问节点
    /// </summary>
    public class TableAccessNode : ASTNode
    {
        public ASTNode Table { get; }
        public ASTNode Key { get; }

        public TableAccessNode(ASTNode table, ASTNode key, int line, int column)
            : base(line, column)
        {
            Table = table;
            Key = key;
        }
    }
}
