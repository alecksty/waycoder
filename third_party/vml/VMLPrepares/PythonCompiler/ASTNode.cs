using System.Collections.Generic;

namespace PythonCompiler
{
    /// <summary>
    /// Python AST 节点类型
    /// </summary>
    public enum ASTNodeType
    {
        // 模块/语句
        Program,
        FunctionDef, ClassDef,
        If, While, For,
        Break, Continue, Return,
        Assign, AugAssign,
        Del, Assert, Raise,
        Global, Nonlocal, Import,
        With, Try,
        Match, Case,
        ExprStmt, Pass,

        // 表达式
        BinOp, UnaryOp, Compare, BoolOp,
        Lambda, Yield, Await,
        Call, Attribute, Subscript,
        Name, Constant, FString,
        ListComp, List, Dict, Tuple, Set,
        Slice, Starred,
        NamedExpr,  // walrus :=
        MultiAssign, MethodCall,
    }

    /// <summary>
    /// Python AST 节点基类
    /// </summary>
    public abstract class ASTNode
    {
        public ASTNodeType Type { get; }
        public int Line { get; }
        public int Column { get; }

        protected ASTNode(ASTNodeType type, int line, int column)
        {
            Type = type;
            Line = line;
            Column = column;
        }

        public abstract void Accept(IASTVisitor visitor);
    }

    /// <summary>
    /// AST 访问者接口
    /// </summary>
    public interface IASTVisitor
    {
        void VisitProgram(ProgramNode node);
        void VisitFunctionDef(FunctionDefNode node);
        void VisitClassDef(ClassDefNode node);
        void VisitIf(IfNode node);
        void VisitWhile(WhileNode node);
        void VisitFor(ForNode node);
        void VisitBreak(BreakNode node);
        void VisitContinue(ContinueNode node);
        void VisitReturn(ReturnNode node);
        void VisitAssign(AssignNode node);
        void VisitAugAssign(AugAssignNode node);
        void VisitBinOp(BinOpNode node);
        void VisitUnaryOp(UnaryOpNode node);
        void VisitCompare(CompareNode node);
        void VisitBoolOp(BoolOpNode node);
        void VisitLambda(LambdaNode node);
        void VisitYield(YieldNode node);
        void VisitAwait(AwaitNode node);
        void VisitCall(CallNode node);
        void VisitAttribute(AttributeNode node);
        void VisitSubscript(SubscriptNode node);
        void VisitName(NameNode node);
        void VisitConstant(ConstantNode node);
        void VisitListComp(ListCompNode node);
        void VisitList(ListNode node);
        void VisitDict(DictNode node);
        void VisitTuple(TupleNode node);
        void VisitSet(SetNode node);
        void VisitSlice(SliceNode node);
        void VisitStarred(StarredNode node);
        void VisitNamedExpr(NamedExprNode node);
        void VisitDel(DelNode node);
        void VisitAssert(AssertNode node);
        void VisitRaise(RaiseNode node);
        void VisitGlobal(GlobalNode node);
        void VisitNonlocal(NonlocalNode node);
        void VisitImport(ImportNode node);
        void VisitWith(WithNode node);
        void VisitTry(TryNode node);
        void VisitMatch(MatchNode node);
        void VisitCase(CaseNode node);
        void VisitExprStmt(ExprStmtNode node);
        void VisitPass(PassNode node);
        void VisitFString(FStringNode node);
        void VisitMultiAssign(MultiAssignNode node);
        void VisitMethodCall(MethodCallNode node);
    }

    // 具体节点类
    public class ProgramNode : ASTNode
    {
        public List<ASTNode> Body { get; }
        public ProgramNode(List<ASTNode> body, int line, int column) : base(ASTNodeType.Program, line, column) { Body = body; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitProgram(this);
    }

    public class FunctionDefNode : ASTNode
    {
        public string Name { get; }
        public List<string> Args { get; }
        public List<ASTNode> Body { get; }
        public List<ASTNode> Decorators { get; }
        public FunctionDefNode(string name, List<string> args, List<ASTNode> body, int line, int column, List<ASTNode>? decorators = null) : base(ASTNodeType.FunctionDef, line, column)
        { Name = name; Args = args; Body = body; Decorators = decorators ?? new List<ASTNode>(); }
        public override void Accept(IASTVisitor visitor) => visitor.VisitFunctionDef(this);
    }

    public class ClassDefNode : ASTNode
    {
        public string Name { get; }
        public string? ParentName { get; }
        public List<ASTNode> Body { get; }
        public List<ASTNode> Decorators { get; }
        public ClassDefNode(string name, List<ASTNode> body, string? parentName, int line, int column, List<ASTNode>? decorators = null) : base(ASTNodeType.ClassDef, line, column)
        { Name = name; Body = body; ParentName = parentName; Decorators = decorators ?? new List<ASTNode>(); }
        public override void Accept(IASTVisitor visitor) => visitor.VisitClassDef(this);
    }

    public class IfNode : ASTNode
    {
        public ASTNode Test { get; }
        public List<ASTNode> Body { get; }
        public List<ASTNode> Orelse { get; }
        public IfNode(ASTNode test, List<ASTNode> body, List<ASTNode> orelse, int line, int column) : base(ASTNodeType.If, line, column)
        { Test = test; Body = body; Orelse = orelse; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitIf(this);
    }

    public class WhileNode : ASTNode
    {
        public ASTNode Test { get; }
        public List<ASTNode> Body { get; }
        public List<ASTNode>? Orelse { get; }
        public WhileNode(ASTNode test, List<ASTNode> body, List<ASTNode>? orelse, int line, int column) : base(ASTNodeType.While, line, column)
        { Test = test; Body = body; Orelse = orelse; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitWhile(this);
    }

    public class ForNode : ASTNode
    {
        public string Target { get; }
        public ASTNode Iter { get; }
        public List<ASTNode> Body { get; }
        public List<ASTNode>? Orelse { get; }
        public ForNode(string target, ASTNode iter, List<ASTNode> body, List<ASTNode>? orelse, int line, int column) : base(ASTNodeType.For, line, column)
        { Target = target; Iter = iter; Body = body; Orelse = orelse; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitFor(this);
    }

    public class BreakNode : ASTNode
    {
        public BreakNode(int line, int column) : base(ASTNodeType.Break, line, column) { }
        public override void Accept(IASTVisitor visitor) => visitor.VisitBreak(this);
    }

    public class ContinueNode : ASTNode
    {
        public ContinueNode(int line, int column) : base(ASTNodeType.Continue, line, column) { }
        public override void Accept(IASTVisitor visitor) => visitor.VisitContinue(this);
    }

    public class ReturnNode : ASTNode
    {
        public ASTNode Value { get; }
        public ReturnNode(ASTNode value, int line, int column) : base(ASTNodeType.Return, line, column) { Value = value; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitReturn(this);
    }

    public class AssignNode : ASTNode
    {
        public string Target { get; }
        public ASTNode? TargetExpr { get; }    // 非空时为属性/下标赋值
        public ASTNode Value { get; }
        public AssignNode(string target, ASTNode value, int line, int column) : base(ASTNodeType.Assign, line, column)
        { Target = target; Value = value; TargetExpr = null; }
        public AssignNode(ASTNode targetExpr, ASTNode value, int line, int column) : base(ASTNodeType.Assign, line, column)
        { Target = ""; TargetExpr = targetExpr; Value = value; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitAssign(this);
    }

    public class AugAssignNode : ASTNode
    {
        public string Target { get; }
        public ASTNode? TargetExpr { get; }
        public string Op { get; }
        public ASTNode Value { get; }
        public AugAssignNode(string target, string op, ASTNode value, int line, int column) : base(ASTNodeType.AugAssign, line, column)
        { Target = target; Op = op; Value = value; TargetExpr = null; }
        public AugAssignNode(ASTNode targetExpr, string op, ASTNode value, int line, int column) : base(ASTNodeType.AugAssign, line, column)
        { Target = ""; TargetExpr = targetExpr; Op = op; Value = value; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitAugAssign(this);
    }

    public class BinOpNode : ASTNode
    {
        public ASTNode Left { get; }
        public string Op { get; }
        public ASTNode Right { get; }
        public BinOpNode(ASTNode left, string op, ASTNode right, int line, int column) : base(ASTNodeType.BinOp, line, column)
        { Left = left; Op = op; Right = right; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitBinOp(this);
    }

    public class UnaryOpNode : ASTNode
    {
        public string Op { get; }
        public ASTNode Operand { get; }
        public UnaryOpNode(string op, ASTNode operand, int line, int column) : base(ASTNodeType.UnaryOp, line, column)
        { Op = op; Operand = operand; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitUnaryOp(this);
    }

    public class CompareNode : ASTNode
    {
        public ASTNode Left { get; }
        public string Op { get; }
        public ASTNode Right { get; }
        public CompareNode(ASTNode left, string op, ASTNode right, int line, int column) : base(ASTNodeType.Compare, line, column)
        { Left = left; Op = op; Right = right; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitCompare(this);
    }

    public class BoolOpNode : ASTNode
    {
        public string Op { get; }
        public List<ASTNode> Values { get; }
        public BoolOpNode(string op, List<ASTNode> values, int line, int column) : base(ASTNodeType.BoolOp, line, column)
        { Op = op; Values = values; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitBoolOp(this);
    }

    public class CallNode : ASTNode
    {
        public string FuncName { get; }
        public List<ASTNode> Args { get; }
        public CallNode(string funcName, List<ASTNode> args, int line, int column) : base(ASTNodeType.Call, line, column)
        { FuncName = funcName; Args = args; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitCall(this);
    }

    public class AttributeNode : ASTNode
    {
        public ASTNode Value { get; }
        public string Attr { get; }
        public AttributeNode(ASTNode value, string attr, int line, int column) : base(ASTNodeType.Attribute, line, column)
        { Value = value; Attr = attr; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitAttribute(this);
    }

    public class SubscriptNode : ASTNode
    {
        public ASTNode Value { get; }
        public ASTNode Index { get; }
        public SubscriptNode(ASTNode value, ASTNode index, int line, int column) : base(ASTNodeType.Subscript, line, column)
        { Value = value; Index = index; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitSubscript(this);
    }

    public class NameNode : ASTNode
    {
        public string Name { get; }
        public NameNode(string name, int line, int column) : base(ASTNodeType.Name, line, column) { Name = name; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitName(this);
    }

    public class ConstantNode : ASTNode
    {
        public object Value { get; }
        public string ValueType { get; }
        public ConstantNode(object value, string valueType, int line, int column) : base(ASTNodeType.Constant, line, column)
        { Value = value; ValueType = valueType; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitConstant(this);
    }

    public class ListCompNode : ASTNode
    {
        public ASTNode Expr { get; }         // expression to compute for each element
        public string VarName { get; }       // loop variable
        public ASTNode Iter { get; }         // iterable source
        public ASTNode? IfCond { get; }      // optional filter condition
        public ListCompNode(ASTNode expr, string varName, ASTNode iter, ASTNode? ifCond, int line, int column)
            : base(ASTNodeType.ListComp, line, column)
        { Expr = expr; VarName = varName; Iter = iter; IfCond = ifCond; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitListComp(this);
    }

    public class ListNode : ASTNode
    {
        public List<ASTNode> Elements { get; }
        public ListNode(List<ASTNode> elements, int line, int column) : base(ASTNodeType.List, line, column) { Elements = elements; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitList(this);
    }

    public class DictNode : ASTNode
    {
        public List<(ASTNode key, ASTNode value)> Items { get; }
        public DictNode(List<(ASTNode, ASTNode)> items, int line, int column) : base(ASTNodeType.Dict, line, column) { Items = items; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitDict(this);
    }

    public class TupleNode : ASTNode
    {
        public List<ASTNode> Elements { get; }
        public TupleNode(List<ASTNode> elements, int line, int column) : base(ASTNodeType.Tuple, line, column) { Elements = elements; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitTuple(this);
    }

    public class ExprStmtNode : ASTNode
    {
        public ASTNode Value { get; }
        public ExprStmtNode(ASTNode value, int line, int column) : base(ASTNodeType.ExprStmt, line, column) { Value = value; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitExprStmt(this);
    }

    public class PassNode : ASTNode
    {
        public PassNode(int line, int column) : base(ASTNodeType.Pass, line, column) { }
        public override void Accept(IASTVisitor visitor) => visitor.VisitPass(this);
    }

    // ── 新增表达式节点 ───────────────────────────────────────
    public class LambdaNode : ASTNode
    {
        public List<string> Args { get; }
        public ASTNode Body { get; }
        public LambdaNode(List<string> args, ASTNode body, int line, int column)
            : base(ASTNodeType.Lambda, line, column) { Args = args; Body = body; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitLambda(this);
    }

    public class YieldNode : ASTNode
    {
        public ASTNode Value { get; }
        public YieldNode(ASTNode value, int line, int column)
            : base(ASTNodeType.Yield, line, column) { Value = value; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitYield(this);
    }

    public class AwaitNode : ASTNode
    {
        public ASTNode Value { get; }
        public AwaitNode(ASTNode value, int line, int column)
            : base(ASTNodeType.Await, line, column) { Value = value; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitAwait(this);
    }

    public class SetNode : ASTNode
    {
        public List<ASTNode> Elements { get; }
        public SetNode(List<ASTNode> elements, int line, int column)
            : base(ASTNodeType.Set, line, column) { Elements = elements; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitSet(this);
    }

    public class SliceNode : ASTNode
    {
        public ASTNode Lower { get; }
        public ASTNode Upper { get; }
        public ASTNode Step { get; }
        public SliceNode(ASTNode lower, ASTNode upper, ASTNode step, int line, int column)
            : base(ASTNodeType.Slice, line, column) { Lower = lower; Upper = upper; Step = step; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitSlice(this);
    }

    public class StarredNode : ASTNode
    {
        public ASTNode Value { get; }
        public StarredNode(ASTNode value, int line, int column)
            : base(ASTNodeType.Starred, line, column) { Value = value; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitStarred(this);
    }

    public class NamedExprNode : ASTNode
    {
        public string Target { get; }
        public ASTNode Value { get; }
        public NamedExprNode(string target, ASTNode value, int line, int column)
            : base(ASTNodeType.NamedExpr, line, column) { Target = target; Value = value; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitNamedExpr(this);
    }

    // ── 新增语句节点 ────────────────────────────────────────
    public class DelNode : ASTNode
    {
        public string Target { get; }
        public DelNode(string target, int line, int column)
            : base(ASTNodeType.Del, line, column) { Target = target; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitDel(this);
    }

    public class AssertNode : ASTNode
    {
        public ASTNode Test { get; }
        public ASTNode Msg { get; }
        public AssertNode(ASTNode test, ASTNode msg, int line, int column)
            : base(ASTNodeType.Assert, line, column) { Test = test; Msg = msg; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitAssert(this);
    }

    public class RaiseNode : ASTNode
    {
        public ASTNode Exc { get; }
        public RaiseNode(ASTNode exc, int line, int column)
            : base(ASTNodeType.Raise, line, column) { Exc = exc; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitRaise(this);
    }

    public class GlobalNode : ASTNode
    {
        public List<string> Names { get; }
        public GlobalNode(List<string> names, int line, int column)
            : base(ASTNodeType.Global, line, column) { Names = names; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitGlobal(this);
    }

    public class NonlocalNode : ASTNode
    {
        public List<string> Names { get; }
        public NonlocalNode(List<string> names, int line, int column)
            : base(ASTNodeType.Nonlocal, line, column) { Names = names; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitNonlocal(this);
    }

    public class ImportNode : ASTNode
    {
        public string? ModuleName { get; }
        public List<(string name, string alias)> Items { get; }
        public ImportNode(List<(string, string)> items, int line, int column, string? moduleName = null)
            : base(ASTNodeType.Import, line, column) { Items = items; ModuleName = moduleName; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitImport(this);
    }

    public class WithNode : ASTNode
    {
        public List<(ASTNode item, string alias)> Items { get; }
        public List<ASTNode> Body { get; }
        public WithNode(List<(ASTNode, string)> items, List<ASTNode> body, int line, int column)
            : base(ASTNodeType.With, line, column) { Items = items; Body = body; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitWith(this);
    }

    public class TryNode : ASTNode
    {
        public List<ASTNode> Body { get; }
        public List<(List<string> types, ASTNode name, List<ASTNode> handler)> Handlers { get; }
        public List<ASTNode> Orelse { get; }
        public List<ASTNode> Finalbody { get; }
        public TryNode(List<ASTNode> body,
                      List<(List<string>, ASTNode, List<ASTNode>)> handlers,
                      List<ASTNode> orelse, List<ASTNode> finalbody,
                      int line, int column)
            : base(ASTNodeType.Try, line, column)
        { Body = body; Handlers = handlers; Orelse = orelse; Finalbody = finalbody; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitTry(this);
    }

    public class MatchNode : ASTNode
    {
        public ASTNode Subject { get; }
        public List<CaseNode> Cases { get; }
        public MatchNode(ASTNode subject, List<CaseNode> cases, int line, int column)
            : base(ASTNodeType.Match, line, column) { Subject = subject; Cases = cases; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitMatch(this);
    }

    public class CaseNode : ASTNode
    {
        public ASTNode Pattern { get; }
        public ASTNode Guard { get; }
        public List<ASTNode> Body { get; }
        public CaseNode(ASTNode pattern, ASTNode guard, List<ASTNode> body, int line, int column)
            : base(ASTNodeType.Case, line, column) { Pattern = pattern; Guard = guard; Body = body; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitCase(this);
    }

    /// <summary>
    /// f-string节点
    /// </summary>
    public class FStringNode : ASTNode
    {
        /// <summary>
        /// f-string的各个部分：字符串片段和表达式
        /// </summary>
        public List<object> Parts { get; }

        public FStringNode(List<object> parts, int line, int column)
            : base(ASTNodeType.FString, line, column)
        {
            Parts = parts;
        }

        public override void Accept(IASTVisitor visitor) => visitor.VisitFString(this);
    }

    /// <summary>
    /// 多元赋值节点: a, b = 1, 2
    /// </summary>
    public class MultiAssignNode : ASTNode
    {
        public List<string> Targets { get; }
        public ASTNode Value { get; }
        public MultiAssignNode(List<string> targets, ASTNode value, int line, int column)
            : base(ASTNodeType.MultiAssign, line, column) { Targets = targets; Value = value; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitMultiAssign(this);
    }

    /// <summary>
    /// 方法调用节点: obj.method(args)
    /// </summary>
    public class MethodCallNode : ASTNode
    {
        public ASTNode Receiver { get; }
        public string Method { get; }
        public List<ASTNode> Args { get; }
        public MethodCallNode(ASTNode receiver, string method, List<ASTNode> args, int line, int column)
            : base(ASTNodeType.MethodCall, line, column) { Receiver = receiver; Method = method; Args = args; }
        public override void Accept(IASTVisitor visitor) => visitor.VisitMethodCall(this);
    }
}
