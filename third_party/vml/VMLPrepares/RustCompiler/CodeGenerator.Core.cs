using System;
using System.Collections.Generic;
using System.Text;
using VMLAssembler;
using CompilerBase;

namespace RustCompiler
{
    public partial class CodeGenerator
    {
        private int _variableOffset = 0;
        private string _firstLocalVarName = ""; // first local var name (for main exit RO type)
        /// <summary>
        /// 把"当前源码位置"挪到这个语句上（语义见 `CodeGeneratorBase.CurrentSourceLine`）。
        ///
        /// ⚠ 这门语言的前端是**逐节点 `Visit` 重载**（没有集中的语句分发），所以挂点选在
        /// **语句列表的遍历处** —— `Visit(ProgramNode)` 与 `Visit(BlockNode)`，
        /// 那是所有语句到达代码生成的公共通道。挂在每个 `Visit(XxxStatement)` 里要改十几处，
        /// 而且将来新增节点类型容易漏。
        /// </summary>
        private void SetCurrentSource(ASTNode node)
        {
            if (node == null) return;
            if (node.Line > 0) { CurrentSourceLine = node.Line; CurrentSourceColumn = node.Column; }
        }

        /// <summary>
        /// `ASTNode.Accept` 的**唯一漏斗**：每进入一个节点就把它自己的位置盖上。
        ///
        /// 与 <see cref="SetCurrentSource"/> 的分工：那个在**语句列表**处调用（粒度到语句），
        /// 这个在**每个节点**处调用（粒度到标识符）。表达式节点的位置由解析器的原子入口
        /// `ParsePrimary` 统一盖，所以往里走一层就精确一层 ——
        /// `let c = a + b + nosuch;` 报错时停在 `nosuch`，而不是 `let`。
        /// 两者共用同一套判据（`Line > 0` 才盖，置 0 会把上一句的正确位置冲掉）。
        /// </summary>
        public void EnterNode(int line, int column)
        {
            if (line > 0) { CurrentSourceLine = line; CurrentSourceColumn = column; }
        }

        private readonly Dictionary<string, int> _variables = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _variableTypes = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _variableStructTypes = new Dictionary<string, string>(); // 变量名 -> 结构体名
        private readonly Dictionary<string, RustType> _rustVarTypes = new Dictionary<string, RustType>(); // 变量名 -> RustType
        private readonly Stack<Dictionary<string, int>> _scopeStack = new Stack<Dictionary<string, int>>();
        private readonly Dictionary<string, List<StructField>> _structFields = new();
        private readonly Dictionary<string, (int index, string? payloadType)> _enumVariants = new();
        private string _currentStructName = "";
        private bool _hasMatch = false; // 函数体是否包含match语句，影响main退出码选择
        private readonly HashSet<string> _movedVariables = new(); // 所有权：已被移动的变量名
        private readonly HashSet<string> _immutBorrowed = new(); // 不可变借用：&x（允许多个）
        private readonly HashSet<string> _mutBorrowed = new();    // 可变借用：&mut x（排斥所有其他借用）
        private readonly Stack<HashSet<string>> _scopeMovedStack = new(); // 作用域内的移动跟踪
        private readonly Dictionary<string, List<string>> _traitMethods = new(); // trait名 → 方法名列表
        private readonly Dictionary<string, Dictionary<string, string>> _traitImpls = new(); // "StructName:TraitName" → (traitMethod → structMethod)

        private static ExpType RustTypeToExpType(RustType t) => t switch
        {
            RustType.Float => ExpType.F32,
            RustType.F64 => ExpType.F64,
            RustType.I64 => ExpType.I64,  // 64 位整数用 long 路径 (MOVEL/ADDL)
            RustType.Bool => ExpType.I8,
            RustType.Char => ExpType.I8,
            RustType.String => ExpType.Ptr32,
            _ => ExpType.I32,
        };

        private ExpType InferExpType(ASTNode node)
        {
            string typeStr = InferTypeFromExpression(node);
            return RustTypeToExpType(GetRustTypeFromString(typeStr));
        }

        private ExpVar WrapExpr(ASTNode node)
        {
            return ExpVar.Eval(InferExpType(node), () => node.Accept(this));
        }

        private ExpVar WrapTargetExpr(ASTNode node)
        {
            if (node is IdentifierNode id)
            {
                if (_variables.TryGetValue(id.Name, out int offset))
                {
                    var expType = InferExpType(node);
                    int pointedSize = 0;
                    if (_variableTypes.TryGetValue(id.Name, out var varType) && varType.StartsWith("*"))
                    {
                        expType = ExpType.Ptr32;
                        var pointedType = varType.Substring(1);
                        var (size, _, _, _) = TypeInfoFromString(pointedType);
                        pointedSize = size;
                    }
                    var tv = ExpVar.Stack(offset, 12, expType);
                    if (pointedSize > 1)
                        tv = tv.WithPointedTypeSize(pointedSize);
                    return tv;
                }
                return ExpVar.Data($"var_{id.Name}", InferExpType(node));
            }
            return WrapExpr(node);
        }
    }
}
