using System;
using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace PascalCompiler
{
    public partial class CodeGenerator
    {
        private void CollectArrayBounds(ArrayTypeNode arr, List<(int, int)> bounds)
        {
            bounds.Add((EvaluateConstantExpr(arr.LowerBound), EvaluateConstantExpr(arr.UpperBound)));
            if (arr.ElementType is ArrayTypeNode nested)
                CollectArrayBounds(nested, bounds);
        }

        private ASTNode astNode;
        private Dictionary<string, List<(int lower, int upper)>> arrayBounds;
        
        // 栈帧管理
        private Dictionary<string, int> globalVarOffsets = new(); // 全局变量偏移
        private Dictionary<string, int> localVarOffsets = new(); // 局部变量偏移(BP相对)
        private Dictionary<string, string> globalVarTypes = new(); // 全局变量类型
        private Dictionary<string, string> localVarTypes = new(); // 局部变量类型
        private Dictionary<string, TypeNode> globalVarDeclarations = new(); // 全局变量声明
        private Dictionary<string, TypeNode> localVarDeclarations = new(); // 局部变量声明
        private Dictionary<string, TypeNode> definedTypeAliases = new(); // 类型别名
        private HashSet<string> dynamicArrayNames = new(); // 动态数组变量
        private Dictionary<string, RecordTypeNode> definedRecordTypes = new(); // 已定义的record类型
        private Dictionary<string, Dictionary<string, (int offset, string type)>> recordFieldLayouts = new(); // record字段布局
        private Dictionary<string, string> variableRecordTypes = new(); // 变量对应的record类型名
        private HashSet<string> constNames = new(); // 常量名称(在dataSection中)
        private HashSet<string> recordsBeingComputed = new(); // 正在计算布局的record,防递归
        private int currentLocalSize = 0; // 当前函数局部变量大小(4字节单元)
        private int currentParamSize = 0; // 当前函数参数大小
        private bool isInSubprogram = false; // 是否在过程/函数中
        private HashSet<string> varParameters = new(); // var参数(引用传递)
        private Dictionary<string, int> paramOffsets = new(); // 参数偏移(BP正方向)
        private Stack<string> subprogramExitLabels = new(); // 子程序 exit 标签栈
        private HashSet<string> functionNames = new(); // 函数名集合,用于识别无括号函数调用
        public static Dictionary<string, string> ExternalFuncTypes = new(); // 外部函数返回类型 (v1.66.46)

        /* ── `uses` 单元的 interface 符号（登记处见 `PascalCompiler.RegisterUnitFunctions`）──
         *
         * 这三张表让**单元文件成为声明的唯一真源**：`Lib/pascal/graph.pas` 里写
         * `function GetMaxX: integer;` / `const Detect = 0;`，前端就认得出该发什么指令。
         * 好处是**不必为某个库在 C# 里再写一张名字表**（本仓头号坑：同一份清单两处维护），
         * 换个库只要加一个 `.pas`。
         *
         * ⚠ 三张表都是 **static**（沿用它上面 `ExternalFuncTypes` 的形状），
         *   `PascalCompiler.CompileFile` 在入口清空它们 —— 理由见那里的注释。 */
        public static Dictionary<string, int> UnitConstInts = new();                        // interface 整数常量
        public static Dictionary<string, SubprogramDeclarationNode> UnitSubprograms = new(); // interface 子程序头

        /// <summary>
        /// `uses` 单元的 **interface 类型声明**（`type Registers = record … end;` 这种）。
        ///
        /// <para>
        /// ⚠ 这张表是**后加的**，而缺了它的后果很具体：`Dos` 单元里的 `Registers` /
        /// `SearchRec` 是**记录类型**，老程序写 `var Regs: Registers;` 之后再用 `Regs.AX`，
        /// 而前端只在**程序自己**的 `type` 段里建记录布局（`ProcessTypeDeclarations`）
        /// ⇒ 单元里的类型从来没人登记 ⇒ 报「变量 'Regs' 不是 record类型，无法访问字段」。
        /// 实测这一条**同时卡住 12 份语料**（`Registers`/`SearchRec`/… 各不相同但根因同一个）。
        /// </para>
        ///
        /// <para>
        /// ⚠ 与另外两张表不同，这张**不在这里被消费** —— 记录布局要算进
        /// `recordFieldLayouts` / `definedRecordTypes`，而那是 `CodeGenerator` 的**实例**字段。
        /// 所以流程是：`RegisterUnitFunctions` 往这里**收集**，
        /// `CompileFile` 建好 `CodeGenerator` 之后调 `ApplyUnitTypeDeclarations()`
        /// **在 `GenerateCode()` 之前**灌进去 —— 顺序有意如此：程序自己的 `type` 段
        /// 在 `GenerateProgramCode` 里随后处理，于是**程序可以覆盖单元的同名类型**
        ///（Pascal 的常规作用域规则）。
        /// </para>
        /// </summary>
        public static List<DeclarationNode> UnitTypeDeclarations = new();

        /// <summary>把 `uses` 单元的 interface 类型灌进记录布局表（见 <see cref="UnitTypeDeclarations"/>）。</summary>
        internal void ApplyUnitTypeDeclarations()
            => ProcessTypeDeclarations(UnitTypeDeclarations);

        /// <summary>
        /// Pascal 的 <c>System</c> 单元里**不用 <c>uses</c> 就在作用域内**的预定义常量。
        ///
        /// <para>
        /// 它们是**语言的一部分**（`System` 由编译器隐式 `uses`），所以没有相应的库文件可挂，
        /// 只能内建。老程序裸写它们 —— 语料里 <c>Pi</c> 出现 60 次、分布在 24 个文件
        /// （`T := -Pi;`、`T := T + Pi/2;`），从前一律报"未声明的变量 'Pi'"。
        /// </para>
        ///
        /// <para>
        /// ⚠ 值走 <see cref="CodeGeneratorBase.EmitLoadConstant"/>：`Pi` 是 <c>float</c>
        /// （Pascal 的 `Real` 是 32 位单精度，**不能**写 `double` —— 那会发出一条 `MOVED`，
        /// 而程序那边按 32 位读同一个槽，等于读到双精度的高半截）。
        /// </para>
        /// </summary>
        public static readonly Dictionary<string, object> StdConsts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["pi"]     = (float)Math.PI,
            ["maxint"] = 2147483647,

            // ── Turbo Pascal 的**段模型内建**与**命令行参数**（老程序兼容，语义桩）───────────
            // 老程序裸写它们（语料里 `ParamCount` 30 个文件、`Dseg`/`PrefixSeg` 11 个文件），
            // 从前一律报"未声明的变量"。
            //
            // ⚠ **这几个值是「语义桩」而不是「等价实现」**，别把它们当真的：
            //   · 本平台是**平坦内存模型**，没有实模式那套 `段:偏移` ⇒ `Dseg`/`PrefixSeg`
            //     没有对应物，取 0 表示"没有段基址"（`Ofs`/`Seg` 两个**带参数**的见
            //     `CodeGenerator.Expressions` 里那两条内建分支）。
            //   · VML 程序**拿不到命令行参数**（宿主接口里没有这一项）⇒ `ParamCount` 恒 0，
            //     `ParamStr(i)` 恒空串。老程序据此走"没有参数就打印用法/进交互"的分支，
            //     那正是我们想要的默认行为。
            //   依赖段地址做**指针运算**的老程序（`Ptr(Seg,Ofs)` 之类）在这套模型下没有意义，
            //   不属兼容范围 —— 这是有意接受的边界（见 `docs/` 里"老程序兼容性"三条定案）。
            ["paramcount"] = 0,
            ["dseg"]       = 0,
            ["prefixseg"]  = 0,
        };

        /// <summary>
        /// `Crt` 里那些**裸写**（不带括号）就能用的标准函数 → 生成目标。
        ///
        /// <para>
        /// 与 <see cref="CodeGenerator.Misc"/> 里那两张 Crt 名字表**同源**：这些名字
        /// 在"带括号"的分支里早就有了（`clrscr`/`gotoxy`/`keypressed`…），
        /// 而 Turbo Pascal 里它们本来就是**无参函数**，老程序一律裸写 ——
        /// `if KeyPressed then`、`Until Keypressed or (K &gt; 50);`、`Ch := ReadKey;`。
        /// 裸写会解析成 `VariableNode`（不是 `FunctionCallNode`），于是落进"未声明的变量"。
        /// </para>
        ///
        /// <para>
        /// 语料统计：`keypressed` 46 份程序用、`readkey` 231 次 —— 不收这两个，
        /// 老 Pascal 程序**基本都编不过**。这里只收"名字与目标不同"的两个：
        /// 名字恰好等于目标标签的那些走 <see cref="UnitSubprograms"/>（单元声明）。
        /// </para>
        /// </summary>
        private static readonly Dictionary<string, string> BareStdCalls = new(StringComparer.OrdinalIgnoreCase)
        {
            ["keypressed"] = "CRT_KEYPRESSED",
            ["readkey"]    = "CRT_READKEY",
        };

        public CodeGenerator(ProgramNode ast) : base()
        {
            this.astNode = ast;
            arrayBounds = new Dictionary<string, List<(int lower, int upper)>>();
            InitSimpleCompiler();
        }

        public CodeGenerator(UnitNode ast) : base()
        {
            this.astNode = ast;
            arrayBounds = new Dictionary<string, List<(int lower, int upper)>>();
            InitSimpleCompiler();
        }

        public override VmlProgram GenerateCode()
        {
            if (astNode is ProgramNode programNode)
                return GenerateProgramCode(programNode);
            else if (astNode is UnitNode unitNode)
                return GenerateUnitCode(unitNode);
            throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, "不支持的AST节点类型");
        }

        private VmlProgram GenerateProgramCode(ProgramNode ast)
        {
            // 处理类型声明(记录record类型)
            ProcessTypeDeclarations(ast.Declarations);

            // 处理常量声明
            foreach (var decl in ast.Declarations)
            {
                if (decl is ConstDeclarationNode constDecl)
                {
                    // 处理数组常量初始化: const arr: array[1..5] of integer = (1, 2, 3, 4, 5);
                    if (constDecl.ArrayValues != null && constDecl.ArrayValues.Count > 0)
                    {
                        int[] values = new int[constDecl.ArrayValues.Count];
                        for (int i = 0; i < constDecl.ArrayValues.Count; i++)
                        {
                            if (constDecl.ArrayValues[i] is LiteralNode lit)
                                values[i] = Convert.ToInt32(lit.Value);
                            else
                                values[i] = 0;
                        }
                        dataSection[constDecl.Name] = values;
                        constNames.Add(constDecl.Name);

                        /* ⚠ **常量数组的下界也必须登记** —— 否则取元素时退回 `lower = 0`，
                           于是**声明成 `array[1..N]` 的常量数组整体错位一格**：
                           实测 `const A: array[1..3] of integer = (10,20,30);` 读 `A[1]` 得 **20**、
                           `A[3]` 得 **0**（应 10 / 30）；`array[5..7]` 更是全 0。
                           而**变量**数组一直是对的（`AllocateVariable` 里就登记了）——
                           所以症状是"同样的下标，变量数组对、常量数组错"，最难往这上面想。

                           数据段里的元素**本来就是按声明顺序排的**，下标换算全靠这个下界，
                           两个取值分支（常量折叠 / 运行期索引）读的都是它 ⇒ 只登记这一处即可。
                           用与变量路径**同一个** `CollectArrayBounds`，免得两份规则漂移。 */
                        var constArrType = constDecl.ConstType is null
                            ? null : ResolveTypeAlias(constDecl.ConstType) as ArrayTypeNode;
                        if (constArrType != null && !constArrType.IsDynamic)
                        {
                            var constBounds = new List<(int, int)>();
                            CollectArrayBounds(constArrType, constBounds);
                            if (constBounds.Count > 0) arrayBounds[constDecl.Name] = constBounds;
                        }
                    }
                    else if (constDecl.Value is LiteralNode literal)
                    {
                        if (literal.Type == TokenType.REAL_LITERAL)
                        {
                            constants[constDecl.Name] = Convert.ToDouble(literal.Value);
                        }
                        else if (literal.Type == TokenType.INTEGER_LITERAL)
                        {
                            // 整数常量直接放入数据段
                            dataSection[constDecl.Name] = Convert.ToInt32(literal.Value);
                            constNames.Add(constDecl.Name);
                        }
                        else if (literal.Type == TokenType.BOOLEAN)
                        {
                            // 布尔常量直接放入数据段
                            dataSection[constDecl.Name] = (bool)literal.Value ? 1 : 0;
                            constNames.Add(constDecl.Name);
                        }
                        else if (literal.Type == TokenType.STRING_LITERAL)
                        {
                            dataSection[constDecl.Name] = literal.Value.ToString();
                            constNames.Add(constDecl.Name);
                        }
                        else
                        {
                            dataSection[constDecl.Name] = Convert.ToInt32(literal.Value);
                            constNames.Add(constDecl.Name);
                        }
                    }
                }
            }

            // 初始化全局变量（必须在生成子程序之前，以便子程序能引用全局变量类型）
            foreach (var decl in ast.Declarations)
            {
                if (decl is VarDeclarationNode varDecl)
                {
                    if (!dataSection.ContainsKey(varDecl.Name))
                    {
                        AllocateVariable(varDecl);
                    }
                }
            }

            // 生成所有子程序(过程/函数)
            foreach (var subprogram in ast.Subprograms)
            {
                GenerateSubprogram(subprogram);
            }

            // 添加主程序入口标签
            AddLabel("main");

            // 生成主程序代码（主程序变量已在上面初始化）
            // 生成主程序代码
            GenerateBlock(ast.Block);

            // 加载最后一个纯字母变量值到 R0 作为退出码（避免计数器变量如 i/j/k 被选为退出码）
            string lastVar = null;
            foreach (var kv in dataSection.OrderBy(kv => kv.Key))
                if (kv.Key.All(char.IsLetter) && !functionNames.Contains(kv.Key.ToLower()))
                    lastVar = kv.Key;
            if (lastVar != null)
                AddInstruction(OpCode.MOVE, Reg(0), Mem(lastVar));
            EmitExit();

            return BuildProgram("main");
        }

        private VmlProgram GenerateUnitCode(UnitNode ast)
        {
            // 处理interface中的类型声明
            ProcessTypeDeclarations(ast.InterfaceDeclarations);
            ProcessTypeDeclarations(ast.ImplementationDeclarations);

            // interface常量
            foreach (var decl in ast.InterfaceDeclarations)
            {
                if (decl is ConstDeclarationNode constDecl && constDecl.Value is LiteralNode literal)
                {
                    dataSection[constDecl.Name] = Convert.ToInt32(literal.Value);
                    constNames.Add(constDecl.Name);
                }
            }

            // interface变量
            foreach (var decl in ast.InterfaceDeclarations)
            {
                if (decl is VarDeclarationNode varDecl && !dataSection.ContainsKey(varDecl.Name))
                    AllocateVariable(varDecl);
            }
            foreach (var decl in ast.ImplementationDeclarations)
            {
                if (decl is VarDeclarationNode varDecl && !dataSection.ContainsKey(varDecl.Name))
                    AllocateVariable(varDecl);
            }

            // 生成 interface 子程序
            //
            // ⚠ **interface 里的声明不该生成任何代码**（`IsForward` 由解析器在
            // `ParseSubprogramHeader` 里统一置上，因为 interface 段只解析得到子程序头）。
            //
            // 从前这里无条件 `GenerateSubprogram(sub)`，而它会给每个声明发一个**只有
            // `exit_<名>` 的空壳标签**（`Subprogram.GenerateSubprogram` 一开头就
            // `AddLabel(subprogram.Name)`）。后果不是"多几条指令"，而是**静默劫持**：
            // 单元自己定义了标签 ⇒ 主程序里 `CALL GetMaxX` 解析到**这个空壳**、
            // 而不是库里的实现 ⇒ 返回 R0 里的垃圾值、**一个错都不报**。
            // 实测（改之前）：一个 `unit testv; interface function ExtFoo(x: integer): integer;`
            // 的单元 + `uses testv` 的主程序，产物里 `ExtFoo:` 后面只有
            // `enter #4 / exit_ExtFoo: / move R0,[R12-4] / leave / ret`。
            //
            // 这正是"单元声明 + 库实现"这套结构成立的前提：**声明归声明，实现归库**。
            foreach (var sub in ast.InterfaceSubprograms)
                if (!sub.IsForward) GenerateSubprogram(sub);
            // 生成implementation子程序
            foreach (var sub in ast.ImplementationSubprograms)
                GenerateSubprogram(sub);

            // 初始化代码
            if (ast.InitializationBlock != null)
            {
                AddLabel($"unit_{ast.Name}_init");
                GenerateBlock(ast.InitializationBlock);
                AddInstruction(OpCode.RET);
            }

            Vars?.LogStats();
            var vmlProgram = new VmlProgram(instructions, labels, dataSection, constants);
            vmlProgram.EntryPoint = $"unit_{ast.Name}_init";

            // 导出 interface 符号 (v1.66.32+: unit 系统)
            foreach (var sub in ast.InterfaceSubprograms)
                vmlProgram.Exports[sub.Name] = sub.Name;
            foreach (var decl in ast.InterfaceDeclarations)
            {
                if (decl is VarDeclarationNode varDecl && dataSection.ContainsKey(varDecl.Name))
                    vmlProgram.Exports[varDecl.Name] = varDecl.Name;
                if (decl is ConstDeclarationNode constDecl && !vmlProgram.Constants.ContainsKey(constDecl.Name))
                    vmlProgram.Exports[constDecl.Name] = constDecl.Name;
            }

            return vmlProgram;
        }

        private void AllocateVariable(VarDeclarationNode varDecl)
        {
            int varSlots = GetVariableSlots(varDecl.Type);
            TypeNode resolvedType = ResolveTypeAlias(varDecl.Type);
            if (resolvedType is ArrayTypeNode arrType)
            {
                if (arrType.IsDynamic)
                    dynamicArrayNames.Add(varDecl.Name);
                else
                {
                    var bounds = new List<(int, int)>();
                    CollectArrayBounds(arrType, bounds);
                    arrayBounds[varDecl.Name] = bounds;
                }
            }
            if (varSlots <= 1)
            {
                dataSection[varDecl.Name] = 0;
            }
            else
            {
                int[] slots = new int[varSlots];
                dataSection[varDecl.Name] = slots;
            }
            globalVarOffsets[varDecl.Name] = dataSection.Count - 1;
            globalVarTypes[varDecl.Name] = GetTypeName(resolvedType);
            globalVarDeclarations[varDecl.Name] = resolvedType;

            if (varDecl.Type is RecordTypeNode recordType)
            {
                string recordTypeName = FindRecordTypeName(recordType);
                if (recordTypeName != null)
                    variableRecordTypes[varDecl.Name] = recordTypeName;
            }
            else if (varDecl.Type is SimpleTypeNode simpleType)
            {
                if (definedRecordTypes.ContainsKey(simpleType.TypeName.ToUpper()))
                    variableRecordTypes[varDecl.Name] = simpleType.TypeName.ToUpper();
            }
            else if (varDecl.Type is ArrayTypeNode arrayType && arrayType.ElementType is SimpleTypeNode elemSimple)
            {
                if (definedRecordTypes.ContainsKey(elemSimple.TypeName.ToUpper()))
                    variableRecordTypes[varDecl.Name] = elemSimple.TypeName.ToUpper();
            }
        }
    }
}
