using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using VMLAssembler;
using VMLPlugins;

namespace CompilerBase
{
    /// <summary>
    /// 所有语言编译器代码生成器的抽象基类
    /// 提取了共同的字段、辅助方法和指令生成模式
    /// </summary>
    public abstract class CodeGeneratorBase
    {
        /// <summary>
        /// 指令列表 — 继承 List&lt;Instruction&gt;，在 Add 时自动根据 CurrentSourceLine 设置 SourceLine
        /// </summary>
        public class InstrList : List<Instruction>
        {
            /// <summary>所属的 CodeGeneratorBase 实例</summary>
            public CodeGeneratorBase Owner = null!;
            public new void Add(Instruction item)
            {
                if (Owner != null && Owner.CurrentSourceLine >= 0)
                    item.SourceLine = Owner.CurrentSourceLine;
                base.Add(item);
            }
        }

        #region 公共字段

        protected InstrList instructions;

        /// <summary>
        /// 代码生成期间的诊断（语义检查收集到这里，**继续生成**，最后在 `BuildProgram`
        /// 一次性抛出 —— 见那里的说明）。
        ///
        /// **生成器自持，不由外部注入**：22 个 `CompileFile` 入口没有一个会把
        /// `DiagnosticBag` 传进 codegen，外部注入意味着改 22 个文件；自持则**改零个入口**。
        /// 与词法/语法那套（`LexerBase.Diagnostics` / `ParserBase.Diagnostics`，外部注入的）
        /// 并存，两边最后都汇进 `CompileWithDiagnostics` 那一个 bag。
        /// </summary>
        /// ⚠ 类型名**不加 `CompilerBase.` 限定** —— 这个命名空间里正好有一个叫
        /// `CompilerBase` 的类，限定名会被解析到那个类上（CS0426）。
        protected readonly DiagnosticBag Diags = new();

        /// <summary>
        /// 这门语言允不允许「不声明就直接用」。
        ///
        /// **默认 false**（大多数语言要先声明再引用）；**动态语言覆写成 true 豁免**：
        /// JavaScript / Lua / Python / R / Ruby / Scheme —— 对它们来说
        /// 「未声明即隐式全局」是**合法语义**，不是缺陷。
        ///
        /// ⚠ 做成**一个虚拟属性**而不是在 16 处散写 `if (lang != "python")`：
        /// 散写就是本仓头号坑「同一规则两处实现」，改一处忘一处。
        /// </summary>
        protected virtual bool ImplicitDeclarationAllowed => false;

        // ⚠ `CurrentSourceLine` **基类里已经有了**（就是 Dart/Basic 在设、`InstrList.Add`
        //   读的那个），别在这儿再声明一次（CS0102）。它同时驱动两件事：
        //   ① 报错带行号；② `InstrList` 把它写进指令的 `SourceLine`，
        //   于是前端产物里出现 `; N:` 注释、汇编器再读回来。

        /// <summary>
        /// 报一个「未声明的标识符」——**收集，不抛**。
        ///
        /// 这是「一次多报」的关键：遇到第一个错就抛的话，用户一次只能看到一个；
        /// 收集起来继续生成，最后在 <see cref="BuildProgram"/> 一次性抛出去
        /// （顺便让调用方用 <see cref="EmitUndefinedFallback"/> 发个占位值，
        /// 后续生成才不会级联崩）。
        ///
        /// 同一个名字**写 100 遍只报 1 条** —— 去重交给 <see cref="Diags"/> 自己
        /// （它按「码+文件+行+列+消息」去重，同一行写两次仍是一条）。
        /// </summary>
        protected void ReportUndefined(string name, ErrorCode code, string kind, string? hint = null)
        {
            if (ImplicitDeclarationAllowed) return;
            // `CompilerHelper.CurrentSourceFile` 是 `[ThreadStatic]` 的，由
            // `CompileFileStandard` 在进编译器前设好 —— 与 `CompileWithDiagnostics`
            // 取文件名的地方同源。
            Diags.AddError(DiagFile, DiagLine, CurrentSourceColumn, code,
                $"未声明的{kind} '{name}'", hint ?? $"先声明它（{kind}要先声明再引用）；名字拼错了也会报这一条。");
        }

        /// <summary>
        /// 报一条「未声明的标识符」的**警告**（收集、不挡编译）。
        ///
        /// 用在**语言语义允许隐式声明**的场合 —— 此时它不是错误，但用户仍然值得知道
        /// 「这个名字没有声明过，是按语言默认规则隐式造出来的」。两处调用：
        /// Fortran（没写 `implicit none` ⇒ 隐式类型是合法语义）、
        /// QBasic（没写 `OPTION EXPLICIT` ⇒ 未声明即隐式全局是合法语义）。
        ///
        /// 与 <see cref="ReportUndefined"/> 只差级别 —— 别把这两处判据各写一遍。
        /// </summary>
        protected void WarnUndefined(string name, ErrorCode code, string kind, string? hint = null)
        {
            Diags.AddWarning(DiagFile, DiagLine, CurrentSourceColumn, code,
                $"隐式声明的{kind} '{name}'",
                hint ?? $"它没有声明过，按这门语言的默认规则被隐式创建；建议显式声明。");
        }

        /// <summary>
        /// 报一个「**实参个数不足**」—— 收集，不抛（理由同 <see cref="ReportUndefined"/>：
        /// 一次多报，且后续生成不级联崩）。
        ///
        /// 用在那些「前端**直接生成指令**」的内置函数上（`getenv`/`setenv`/`PEEK`/`POKE`）：
        /// 它们要在编译期取 `Args[0]`/`Args[1]`，少一个就会
        /// `ArgumentOutOfRangeException` 抛到用户脸上 —— 一行 .NET 异常文本，
        /// **没有文件名、没有行号、没有诊断码**。实测 `int main(){ (void)getenv(); }`
        /// 报的就是 `Index was out of range...`。
        /// </summary>
        protected void ReportArgCount(string funcName, int expected, int actual)
        {
            Diags.AddError(DiagFile, DiagLine, CurrentSourceColumn, ErrorCode.CodeGen_ArgCountMismatch,
                $"'{funcName}' 需要 {expected} 个参数，这里只给了 {actual} 个",
                "它是由编译器直接生成指令的内置函数，参数个数必须在编译期就定下来 —— 少一个就没法生成。");
        }

        /// <summary>
        /// 报一条「定义了但从未使用」的**警告**（不挡编译）。
        ///
        /// 用户要的：「那些定义了，却没有使用的局部变量或者函数（外部访问不了的），
        /// 要出警告，可以给 IDE 报警告提示用」。
        ///
        /// <paramref name="line"/> 传 -1 表示用当前的 <see cref="CurrentSourceLine"/>；
        /// 显式传行号是为了那些"在末尾统一清理"的场合 —— 那时游标早已不在声明处，
        /// 用 <see cref="CurrentSourceLine"/> 会指到**毫不相干的一行**上（比没有行号更糟）。
        /// </summary>
        protected void WarnUnused(string name, ErrorCode code, string kind, int line = -1, int col = 0, string? hint = null)
        {
            Diags.AddWarning(DiagFile,
                line >= 0 ? line : DiagLine,
                // 显式给了行号时列也一并给（不给就写 0）——**别顺手用 `CurrentSourceColumn`**：
                // 这个调用通常发生在生成之后的清理段，游标早就不在声明处了，
                // 那一列会指到毫不相干的位置上，比没有列更糟。
                line >= 0 ? col : CurrentSourceColumn, code,
                $"定义了但从未使用的{kind} '{name}'",
                hint ?? $"它不会出现在编译产物里；如果确实用不到，删掉它能少一份维护负担。");
        }

        /// <summary>
        /// 未声明标识符的**占位值**：发一个 0 让代码生成继续跑。
        ///
        /// 这不是"随便糊一个" —— 那 16 门语言本来就在做同一件事（查不到就
        /// `dataSection["var_x"] = 0` 建个初值 0 的槽，或者干脆什么都不发让 R0 留残值）。
        /// 明写成一句指令，至少**行为是确定的**（不像"留残值"那样取决于上一条指令）。
        /// </summary>
        protected void EmitUndefinedFallback()
        {
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
        }

        protected Dictionary<string, int> labels;
        /// <summary>
        /// `extern` 声明的全局变量名 —— 它们**不占数据段槽位**（存储由别处定义），
        /// 但代码生成**必须认识这些名字**，否则引用它们会报「未声明的变量」。
        /// 生成的仍是 `MEMORY(name)`（标签引用），由链接器解析到库里那份定义
        /// （链接器会给库标签造裸别名）。
        /// 见 `VariableDecl.IsExtern`。
        /// </summary>
        protected readonly HashSet<string> externVariables = new();
        /// <summary>
        /// `extern` 声明的**数组**变量名（`externVariables` 的子集）。
        ///
        /// **必须与 `externVariables` 分开**，因为数组与非数组在表达式里**语义相反**：
        /// 数组名退化为**首地址**（用标签），而非数组（含指针变量）要**解引用**取值。
        /// 把两者混成一个集合，只能二选一，必然错一半：
        ///
        ///   · `extern char buf[64];` → `buf` 的值是**首地址** ⇒ 标签
        ///   · `extern WINDOW *stdscr;` → `stdscr` 的值在**槽里** ⇒ 解引用
        ///
        /// ⚠ 实测踩过：当初图省事把所有 extern 塞进"全局数组退化"那条分支，
        /// 于是 `stdscr` 求值求到的是**它自己的槽地址**而不是它指向的 `sc_win`
        /// ⇒ 用户程序里 `wgetch(stdscr)` 传进去一个错指针，
        /// 而 `(int)stdscr == (int)&stdscr` 正是它的指纹（见 `cases/20-curses-api.c`）。
        /// </summary>
        protected readonly HashSet<string> externArrayVariables = new();
        protected Dictionary<string, object> dataSection;
        protected Dictionary<string, object> constants;
        /// <summary>原始汇编指令行 (如 .skip 0x6000, 0x1000)，直接插入到输出中</summary>
        protected List<string> rawDirectives = new List<string>();
        protected int labelCounter;

        /// <summary>
        /// 当前正在处理的源码行号（-1 表示未知）
        /// </summary>
        public int CurrentSourceLine { get; set; } = -1;

        /// <summary>
        /// 当前正在处理的源码**列号**（1-based）；**0 = 未知**。
        ///
        /// 与 <see cref="CurrentSourceLine"/> 配对：诊断的 `file:line:col:` 两段分别取它们。
        /// 只有**手里真有列号**的语言才设（目前是 C —— 它的 `ASTNode` 补上了 `Line`/`Column`，
        /// 解析器在语句入口一起盖）；其余语言留 0，`CompilerError.LocationString` 会照常
        /// 打出 `:0`，而宿主侧 `VmlDiagnostics` 的 GCC 正则把列当可选，锚点仍落在行上。
        /// </summary>
        public int CurrentSourceColumn { get; set; } = 0;

        /// <summary>
        /// 当前语句在**原文件**里的行号（1-based）；**0 = 未知**（退回 <see cref="CurrentSourceLine"/>）。
        ///
        /// 与 <see cref="CurrentSourceLine"/> 的分工：后者是**预处理后**的行号，
        /// 它要跟 `SourceLines`（= 预处理后的文本）同一套索引 —— 产物里 `; N: &lt;原文&gt;` 注释
        /// 就靠这个索引取原文。而**报给用户的行号**必须是原文件的，否则 `#include` 一展开
        /// 全部推后（实测 `Examples/c/gomoku.c` 差 138 行）。
        ///
        /// 所以诊断一律取这个（拿不到才退回去），而 `Instruction.SourceLine` 仍取
        /// <see cref="CurrentSourceLine"/>。
        /// </summary>
        public int CurrentSourceOriginalLine { get; set; } = 0;

        /// <summary>诊断该用的行号：优先原文件行号，没有就退回预处理后的行号。</summary>
        private int DiagLine => DiagPosition().Line;

        /// <summary>
        /// 诊断该用的**文件**（与 <see cref="DiagLine"/> 配对）。
        ///
        /// <para>
        /// 之所以做成虚属性：**错误其实在头文件里**时，若照旧报「主文件名 + 头文件的行号」，
        /// 宿主侧只能把这行号硬贴到用户正在看的那个文件上 —— 用户看到的是"编译器指着我
        /// 这句没问题的代码报错"（实测：`#include &lt;stdio.h&gt;` 之后的错被贴到用户文件
        /// 第 112 行一句无害的 `/// &lt;summary&gt;` 上）。有 `OriginalFile` 的语言覆写它即可
        ///（C++ 就是这么做的）。
        /// </para>
        /// </summary>
        protected virtual string DiagFile => DiagPosition().File;

        /// <summary>
        /// **诊断位置（文件 + 行）的唯一判据** —— 三个报错出口（`ReportUndefined` /
        /// `WarnUndefined` / `WarnUnused`）与 `DiagFile`/`DiagLine` 全从这里取。
        ///
        /// <para>
        /// 先查一次**生效中的映射表**（词法器在构造时认领的那份，见
        /// `CompilerHelper.ActiveLineMap`）：`CurrentSourceLine` 是**预处理后**的行号，
        /// 查它就能拿回 `(原文件, 原行)`。然后按"这门语言自己给了什么"分两支：
        /// </para>
        ///
        /// <para>
        /// ① **给了原文件行号**（`CurrentSourceOriginalLine`：C/C++ 的 `ASTNode.OriginalLine`）
        /// ⇒ 行号以它为准（它来自"报错的那个节点"，比游标更精确），
        /// **不能再拿它去查表**（那会把"原文件第 30 行"当成"拼接流第 30 行"，
        /// 查出来是**另一个文件**的行 —— 比不映射更糟）；
        /// 但**文件仍取自映射表**：C 的 `ASTNode` 只带 `OriginalLine`、**不带 `OriginalFile`**，
        /// 只靠它就等于报「主文件名 + 头文件的行号」，而宿主会把这个行号硬贴到用户
        /// 正在看的那个文件上 —— 正是要修的那个 bug 的另一种形态（实测就是这么表现的）。
        /// C++ 两者都有，取到的是同一个答案（见它自己的 `DiagFile` 覆写）。
        /// </para>
        ///
        /// <para>
        /// ② **没给**（其余 20 门）⇒ 靠映射表把游标行换回原文件。这一条正是它们
        /// 从前缺的那一环：只给 `CurrentSourceLine`（= 拼接流行号），于是
        /// 「错在 `Lib/c/time.h` 第 30 行」被报成「用户文件第 112 行」。
        /// </para>
        ///
        /// <para>
        /// 没有生效表时**原样退回**，与从前逐字相同（不碰 `#` 的源码走这条，行为零变化）。
        /// </para>
        /// </summary>
        protected (string File, int Line) DiagPosition()
        {
            // 生效中的映射表（词法器在构造时认领的那一份，见 `CompilerHelper.ActiveLineMap`）：
            // `CurrentSourceLine` 是**预处理后**的行号，查它就能拿回 `(原文件, 原行)`。
            //
            // ⚠ 判"有没有映射"要看 `ActiveMapCovers`，**不能只看文件是不是 null** ——
            //   内存里编的源码没有文件名，表里的文件是占位符 `<unknown>`，
            //   而它会被规范成 null（见那一处的说明）。只看文件的话，「有表、但文件名未知」
            //   会被误当成「没有表」，于是**行号那一半修复也跟着丢了**
            //   （实测：主文件第 5 行的错在 `#include` 之后又被报成第 7 行）。
            var mappedFile = (string?)null;
            int mappedLine = CurrentSourceLine;
            if (CompilerHelper.ActiveMapCovers(CurrentSourceLine))
                (mappedFile, mappedLine) = CompilerHelper.MapActiveOriginal(CurrentSourceLine);

            // ① 语言自己给了原文件行号（`CurrentSourceOriginalLine`：C/C++ 的 `ASTNode.OriginalLine`）
            //    ⇒ 行号以它为准（它来自"报错的那个节点"，比游标更精确），
            //    但**文件仍从映射表取** —— C 的 `ASTNode` 只带 `OriginalLine`、**不带 OriginalFile**，
            //    只靠它就等于报「主文件名 + 头文件的行号」，而宿主会把这个行号硬贴到
            //    用户正在看的那个文件上 —— 正是要修的那个 bug 的另一种形态。
            //    （C++ 两者都有，取到的是同一个答案；见它自己的 `DiagFile` 覆写。）
            if (CurrentSourceOriginalLine > 0)
                return (mappedFile ?? BaseDiagFile, CurrentSourceOriginalLine);

            // ② 没有自带的原文件行号 ⇒ 用映射表换回原文件（20 门语言走这条）。
            return (mappedFile ?? BaseDiagFile, mappedLine);
        }

        /// <summary>没映射可用时的文件名（= 从前 `DiagFile` 的默认实现，逐字保留）。</summary>
        private string BaseDiagFile => CompilerHelper.CurrentSourceFile ?? "<input>";

        /// <summary>
        /// 源码行文本数组（行号 0 对应第 1 行），为 null 时不生成源码注释
        /// </summary>
        public string[]? SourceLines { get; set; }

        /// <summary>
        /// 统一寄存器分配管理器（null = 未启用，子类需在构造函数中 new 以启用）
        /// </summary>
        protected RegisterManager? Regs { get; set; }

        /// <summary>
        /// 统一变量内存分配管理器（null = 未启用，子类需在构造函数中 new 以启用）
        /// </summary>
        protected VarMemManager? Vars { get; set; }

        /// <summary>
        /// 统一控制语句管理器（null = 未启用，子类需在构造函数中 new 以启用）
        /// </summary>
        protected StatementManager? Sta { get; set; }

        /// <summary>
        /// 当前编译器选项（mode/ram/int64/float32/float64 等），由 CompilerOptionsContext 提供
        /// </summary>
        protected CompilerOptions CurrentOptions => CompilerOptionsContext.Current;

        /// <summary>
        /// MCU 模式下返回纯字符串，OS 模式下包装为 DataString(Wide)
        /// </summary>
        protected static object WStr(string s) =>
            CompilerOptionsContext.Current.IsMCU ? (object)s : new DataString(s, StringWidth.Wide);

        #endregion

        #region 构造

        protected CodeGeneratorBase()
        {
            instructions = new InstrList { Owner = this };
            labels = new Dictionary<string, int>();
            dataSection = new Dictionary<string, object>();
            constants = new Dictionary<string, object>();
            labelCounter = 0;
        }

        /// <summary>
        /// 初始化简单编译器的通用组件：Vars(VarMemManager) + ExpressionManager + StatementManager
        /// 适用于 Dart/D/Ruby/R/ObjC/Fortran 等不使用 CLikeCodegen 的编译器
        /// </summary>
        /// <param name="framePointerReg">帧指针寄存器号（默认 R12）</param>
        /// <param name="threeOperandInt">整数运算是否使用三操作数格式</param>
        /// <param name="newLabel">自定义标签生成函数（默认 "L{labelCounter++}"）</param>
        /// <param name="placeLabel">自定义标签放置回调（默认 AddLabel）</param>
        /// <param name="paramStart">参数起始偏移（C/C++/Go 用 12 正向，Go 编译器等用 -4 负向）</param>
        [MemberNotNull(nameof(_expr))]
        protected void InitSimpleCompiler(int framePointerReg = 12, bool threeOperandInt = true,
            Func<string>? newLabel = null, Action<string>? placeLabel = null, int paramStart = 12)
        {
            Vars = new VarMemManager(framePointerReg, paramStart);
            _expr = new ExpressionManager(
                emit: (op, ops) => AddInstruction(op, ops),
                newLabel: newLabel ?? (() => $"_L{labelCounter++}"),
                placeLabel: placeLabel ?? (lbl => AddLabel(lbl)),
                framePointerReg: framePointerReg,
                threeOperandInt: threeOperandInt);
            Sta = new StatementManager(instructions, labels, () => $"sta_{labelCounter++}");
        }

        // ====== VarMemManager 便捷包装器 ======

        /// <summary>分配栈局部变量 → 返回格式化内存操作数 (如 "R12-4")</summary>
        protected string AllocStackVar(string name, int size = 4) { Vars!.AllocLocal(name, size); return Vars.FormatOffset(name); }

        /// <summary>分配函数参数 → 返回格式化内存操作数 (如 "R12+12")</summary>
        protected string AllocParamVar(string name, int size = 4) { Vars!.AllocParam(name, size); return Vars.FormatOffset(name); }

        /// <summary>重置局部变量分配（进入新作用域时调用）</summary>
        protected void ResetStackVars() => Vars!.ResetLocals();

        /// <summary>延迟初始化的 ExpressionManager（与 _expr 同对象）</summary>
        protected ExpressionManager Expr => _expr ??= new ExpressionManager(
            emit: (op, ops) => AddInstruction(op, ops),
            newLabel: () => $"_L{labelCounter++}",
            placeLabel: lbl => AddLabel(lbl),
            framePointerReg: 12,
            threeOperandInt: true);

        /// <summary>表达式管理器（由 InitSimpleCompiler 或子类初始化）</summary>
        protected ExpressionManager? _expr;

        // ====== 通用代码生成辅助（消除编译器间重复） ======

        /// <summary>
        /// 生成三元条件表达式: cond ? thenExpr : elseExpr
        /// 模式: 求值条件→CMP→JE elseLabel→then→JMP end→else→end
        /// </summary>
        /// <param name="jumpOp">条件跳转指令 (默认 JE，Ruby 用 JZ)</param>
        protected void EmitTernary(Action emitCond, Action emitThen, Action emitElse, OpCode jumpOp = OpCode.JE)
        {
            string elseLabel = NewLabel();
            string endLabel = NewLabel();
            emitCond();
            AddInstruction(OpCode.CMP, [Reg(0), Imm(0)]);
            AddInstruction(jumpOp, [new Operand(OperandType.LABEL, elseLabel)]);
            emitThen();
            AddInstruction(OpCode.JMP, [new Operand(OperandType.LABEL, endLabel)]);
            AddLabel(elseLabel);
            emitElse();
            AddLabel(endLabel);
        }

        /// <summary>
        /// 生成 cdecl 风格函数调用: 从右到左压栈参数→CALL→清理栈
        /// </summary>
        protected void EmitCdeclCall(string funcLabel, int argCount, Action<int> emitArg, bool popCleanup = false)
        {
            for (int i = argCount - 1; i >= 0; i--)
            {
                emitArg(i);
                AddInstruction(OpCode.PUSH, [Reg(0)]);
            }
            AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, funcLabel)]);
            if (!popCleanup)
                AddInstruction(OpCode.ADD, [Reg(13), Reg(13), Imm(argCount * 4)]);
            else
                for (int i = 0; i < argCount; i++)
                    AddInstruction(OpCode.POP, [Reg(0)]);
        }

        // ====== 字符串运行时快捷包装 ======

        /// <summary>生成 shared_strcat(left, right) 调用</summary>
        protected void EmitStrCat(Action emitLeft, Action emitRight)
        {
            emitRight(); AddInstruction(OpCode.PUSH, [Reg(0)]);
            emitLeft();  AddInstruction(OpCode.PUSH, [Reg(0)]);
            AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, "strcat")]);
            AddInstruction(OpCode.ADD, [Reg(13), Reg(13), Imm(8)]);
        }

        /// <summary>生成 shared_strlen(str) 调用</summary>
        protected void EmitStrLen(Action emitStr)
        {
            emitStr();
            AddInstruction(OpCode.PUSH, [Reg(0)]);
            AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, "strlen")]);
            AddInstruction(OpCode.ADD, [Reg(13), Reg(13), Imm(4)]);
        }

        /// <summary>生成 shared_strcmp(a, b) 调用</summary>
        protected void EmitStrCmp(Action emitA, Action emitB)
        {
            emitB(); AddInstruction(OpCode.PUSH, [Reg(0)]);
            emitA(); AddInstruction(OpCode.PUSH, [Reg(0)]);
            AddInstruction(OpCode.CALL, [new Operand(OperandType.LABEL, "strcmp")]);
            AddInstruction(OpCode.ADD, [Reg(13), Reg(13), Imm(8)]);
        }

        #endregion

        // ====== 内存布局辅助 ======

        /// <summary>
        /// 将 R0 中的索引转换为数组元素偏移: R0 = index * elementSize + headerSize
        /// 调用者负责 ADD R0, baseReg 完成最终地址计算
        /// </summary>
        protected void EmitArrayElementOffset(int headerSize = 4, int elementSize = 4)
        {
            if (elementSize == 2)
                AddInstruction(OpCode.SHL, [Reg(0), Imm(1)]);
            else if (elementSize >= 4)
                AddInstruction(OpCode.SHL, [Reg(0), Imm(2)]);
            if (headerSize > 0)
                AddInstruction(OpCode.ADD, [Reg(0), Imm(headerSize)]);
        }

        #region 抽象接口

        /// <summary>
        /// 子类实现此方法生成完整的 VML 程序
        /// </summary>
        public abstract VmlProgram GenerateCode();

        #endregion

        #region 标签管理

        /// <summary>
        /// 生成一个新标签名，前缀默认为 "L"
        /// </summary>
        protected string NewLabel(string prefix = "L")
        {
            return $"{prefix}_{labelCounter++}";
        }

        /// <summary>
        /// 在当前位置添加一个标签
        /// </summary>
        protected void AddLabel(string name)
        {
            labels[name] = instructions.Count;
            Emit(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, name) }, name);
        }

        /// <summary>
        /// 在 dataSection 中分配字符串常量，返回标签名
        /// </summary>
        protected string AddString(string value)
        {
            string label = $"str_{labelCounter++}";
            dataSection[label] = value;
            return label;
        }

        /// <summary>
        /// 带缓存的字符串分配：相同字符串复用 dataSection 条目
        /// 替代各编译器中手动维护的 _stringCache 字典
        /// </summary>
        protected string AddStringCached(string value)
        {
            _stringCache ??= new Dictionary<string, string>();
            if (!_stringCache.TryGetValue(value, out string? label))
            {
                label = $"str_{System.Guid.NewGuid().ToString("N")[..8]}";
                _stringCache[value] = label;
                dataSection[label] = value;
            }
            return label;
        }

        /// <summary>
        /// 在 dataSection 中分配一个带唯一标签的全局变量，初始值为 initialValue（默认 0）。
        /// 替代各编译器中重复的 dataSection[label] = 0 模式。
        /// </summary>
        protected string EmitGlobalData(string prefix, object? initialValue = null)
        {
            string label = $"{prefix}_{labelCounter++}";
            dataSection[label] = initialValue ?? 0;
            return label;
        }

        private Dictionary<string, string>? _stringCache;

        // ====== 统一变量存取（替代各编译器手写的 LoadVar/StoreVar） ======

        /// <summary>
        /// 变量偏移表。子类在构造函数中将自身的符号表赋值给此字段即可启用统一变量存取。
        /// 默认 null → EmitLoadVar/EmitStoreVar 始终走 dataSection 回退路径（保守安全）。
        /// </summary>
        protected Dictionary<string, int>? _varOffsets = null;

        /// <summary>变量名规范化。Fortran 覆写为 ToLowerInvariant。</summary>
        protected virtual string NormalizeVarName(string name) => name;

        /// <summary>获取变量的 LOAD 操作码。类型感知编译器（D/Dart/Fortran/ObjC）覆写。</summary>
        protected virtual OpCode GetVarLoadOp(string varName) => OpCode.MOVE;

        /// <summary>获取变量的 STORE 操作码。</summary>
        protected virtual OpCode GetVarStoreOp(string varName) => OpCode.MOVE;

        /// <summary>新变量的默认字节大小。类型感知编译器覆写。</summary>
        protected virtual int GetNewVarSize(string varName) => 4;

        /// <summary>
        /// 分配栈空间并注册变量到符号表，返回偏移量。
        /// 子类必须覆写（至少将 name→offset 写入符号表并累加 nextStackOffset）。
        /// </summary>
        protected virtual int AllocAndRegisterVar(string name, int size) => 0;

        /// <summary>
        /// 统一变量加载：查符号表→emit "LOAD R0, MemOff(offset)"，否则→dataSection 回退。
        /// 替代 Ruby/R/D/Dart/Fortran/ObjC 的重复实现。
        /// </summary>
        protected virtual void EmitLoadVar(string name)
        {
            string key = NormalizeVarName(name);
            OpCode loadOp = GetVarLoadOp(key);
            if (_varOffsets != null && _varOffsets.TryGetValue(key, out int offset))
            {
                instructions.Add(new Instruction(loadOp,
                    [Reg(0), new Operand(OperandType.MEMORY, MemOff(offset))],
                    instructions.Count));
            }
            else
            {
                string dataLabel = $"var_{key}";
                if (!dataSection.ContainsKey(dataLabel))
                    dataSection[dataLabel] = 0;
                instructions.Add(new Instruction(loadOp,
                    [Reg(0), new Operand(OperandType.MEMORY, dataLabel)],
                    instructions.Count));
            }
        }

        /// <summary>
        /// 统一变量存储：查符号表→emit "STORE MemOff(offset), R0"；若未分配→自动分配→存入dataSection。
        /// </summary>
        protected virtual void EmitStoreVar(string name)
        {
            string key = NormalizeVarName(name);
            OpCode storeOp = GetVarStoreOp(key);
            if (_varOffsets != null && _varOffsets.TryGetValue(key, out int offset))
            {
                instructions.Add(new Instruction(storeOp,
                    [new Operand(OperandType.MEMORY, MemOff(offset)), Reg(0)],
                    instructions.Count));
            }
            else
            {
                if (!dataSection.ContainsKey($"var_{key}"))
                {
                    int size = GetNewVarSize(key);
                    int newOff = AllocAndRegisterVar(key, size);
                    instructions.Add(new Instruction(storeOp,
                        [new Operand(OperandType.MEMORY, MemOff(newOff)), Reg(0)],
                        instructions.Count));
                    return;
                }
                string dataLabel = $"var_{key}";
                if (!dataSection.ContainsKey(dataLabel))
                    dataSection[dataLabel] = 0;
                instructions.Add(new Instruction(storeOp,
                    [new Operand(OperandType.MEMORY, dataLabel), Reg(0)],
                    instructions.Count));
            }
        }

        #endregion

        #region 指令添加

        /// <summary>
        /// 添加一条指令（无操作数）
        /// </summary>
        protected void AddInstruction(OpCode opcode)
        {
            instructions.Add(new Instruction(opcode, new List<Operand>(), instructions.Count));
        }

        /// <summary>
        /// 添加一条指令（带操作数列表）
        /// </summary>
        protected void AddInstruction(OpCode opcode, List<Operand> operands)
        {
            instructions.Add(new Instruction(opcode, operands, instructions.Count));
        }

        /// <summary>
        /// 添加一条指令（单个操作数）
        /// </summary>
        protected void AddInstruction(OpCode opcode, Operand operand)
        {
            instructions.Add(new Instruction(opcode, new List<Operand> { operand }, instructions.Count));
        }

        /// <summary>
        /// 添加一条指令（两个操作数）
        /// </summary>
        protected void AddInstruction(OpCode opcode, Operand op1, Operand op2)
        {
            instructions.Add(new Instruction(opcode, new List<Operand> { op1, op2 }, instructions.Count));
        }

        /// <summary>
        /// 别名：生成一条指令（兼容旧代码中的 Emit 调用）
        /// </summary>
        protected void Emit(OpCode opcode, List<Operand> operands, string label = "")
        {
            if (!string.IsNullOrEmpty(label))
            {
                labels[label] = instructions.Count;
            }
            instructions.Add(new Instruction(opcode, operands, instructions.Count, label));
        }

        /// <summary>
        /// Emit 重载：两个操作数
        /// </summary>
        protected void Emit(OpCode opcode, Operand op1, Operand op2, string label = "")
        {
            if (!string.IsNullOrEmpty(label))
            {
                labels[label] = instructions.Count;
            }
            instructions.Add(new Instruction(opcode, new List<Operand> { op1, op2 }, instructions.Count, label));
        }

        /// <summary>
        /// Emit 重载：单个操作数
        /// </summary>
        protected void Emit(OpCode opcode, Operand op1, string label = "")
        {
            if (!string.IsNullOrEmpty(label))
            {
                labels[label] = instructions.Count;
            }
            instructions.Add(new Instruction(opcode, new List<Operand> { op1 }, instructions.Count, label));
        }

        /// <summary>
        /// Emit 重载：无操作数
        /// </summary>
        protected void Emit(OpCode opcode, string label = "")
        {
            if (!string.IsNullOrEmpty(label))
            {
                labels[label] = instructions.Count;
            }
            instructions.Add(new Instruction(opcode, new List<Operand>(), instructions.Count, label));
        }

        #endregion

        #region 栈帧与变量管理

        /// <summary>内存偏移量格式化: 正数 R12-N, 负数 R12+N</summary>
        protected static string MemOff(int offset) => offset >= 0 ? $"R12-{offset}" : $"R12+{-offset}";

        /// <summary>生成 return 语句: 计算表达式→R0, MOVE R13 R12, POP R12, POP R15, RET</summary>
        /// <remarks>用帧指针恢复栈，确保递归/局部变量场景下栈位置正确</remarks>
        protected void EmitReturn(Action? emitValue = null)
        {
            if (emitValue != null) emitValue();
            else AddRI(OpCode.MOVE, 0, 0);
            // 通过帧指针恢复栈，避免递归调用污染 R13
            instructions.Add(new Instruction(OpCode.MOVE,
                new List<Operand> { new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 12) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.POP,
                new List<Operand> { new Operand(OperandType.REGISTER, 12) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.POP,
                new List<Operand> { new Operand(OperandType.REGISTER, 15) }, instructions.Count));
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>(), instructions.Count));
        }

        /// <summary>生成函数序言: PUSH R15; PUSH R12; MOVE R12, R13</summary>
        protected void EmitPrologue()
        {
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 15)]));
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 12)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 12), new Operand(OperandType.REGISTER, 13)]));
        }

        /// <summary>生成函数尾声: MOVE R13, R12; POP R12; POP R15; RET</summary>
        protected void EmitEpilogue()
        {
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 12)]));
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 12)]));
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 15)]));
            instructions.Add(new Instruction(OpCode.RET, []));
        }

        /// <summary>生成函数序言 + 保留栈帧空间: PUSH R15; PUSH R12; MOVE R12,R13; SUB R13,R13,#frameSize</summary>
        /// <param name="frameSize">局部变量区字节数 (含安全边界，通常为 maxOffset + 8)</param>
        protected void EmitPrologueWithFrame(int frameSize)
        {
            EmitPrologue();
            if (frameSize > 0)
                instructions.Add(new Instruction(OpCode.SUB,
                    [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, frameSize)]));
        }

        /// <summary>生成函数注释头部（NOP 注释指令块），替代各编译器手写的函数签名文档</summary>
        protected void EmitMethodHeader(string name, string returnType, List<(string Type, string Name)> parameters)
        {
            instructions.Add(new Instruction(OpCode.NOP, [], 0, "; -------------------------------------------"));
            var sourceDecl = $"{returnType} {name}(";
            for (var i = 0; i < parameters.Count; i++)
            {
                sourceDecl += $"{parameters[i].Type} {parameters[i].Name}";
                if (i < parameters.Count - 1) sourceDecl += ",";
            }
            sourceDecl += ")";
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; source   : {sourceDecl}"));
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; function : {name}"));
            foreach (var param in parameters)
                instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; param   : {param.Type} {param.Name}"));
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; return   : {returnType}"));
            instructions.Add(new Instruction(OpCode.NOP, [], 0, "; --------------------------------------------"));
        }

        // ====== 共享输出辅助方法 (CALL → Lib/shared/builtins.c 的 C 函数) ======
        // 所有输出操作外置为 C 语言实现 (Lib/shared/src/builtins.c)
        // 链接时由 CompilerHelper 自动链接 builtins.vml
        // R0 承载参数，C 函数内部使用 __stdcall + asm("SYSCALL #N")

        /// <summary>输出字符串: CALL print_str (R0 = .string 标签地址)</summary>
        protected void EmitPrintString()
        {
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "print_str")]));
        }

        /// <summary>
        /// 判断函数名是否返回字符串类型（按命名约定自动识别）.
        /// 用于 print 参数类型判断：int_to_str/float_to_str 等函数返回 const char*
        /// </summary>
        protected static bool IsStringReturningFunc(string? funcName)
        {
            if (string.IsNullOrEmpty(funcName)) return false;
            return funcName.EndsWith("_str") || funcName.EndsWith("_wstr") || funcName.EndsWith("_ustr")
                || funcName.Contains("ToStr") || funcName.Contains("to_str")
                || funcName == "itoa" || funcName == "ftoa" || funcName == "dtoa" || funcName == "ltoa";
        }

        /// <summary>输出整数: CALL print_int (R0 = 整数值)</summary>
        protected void EmitPrintInt()
        {
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "print_int")]));
        }

        /// <summary>输出换行符: CALL newline</summary>
        protected void EmitPrintNewline()
        {
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "newline")]));
        }

        /// <summary>输出字符: SYSCALL #4 (R0 = charCode, 单指令不宜外置)</summary>
        protected void EmitPrintChar()
        {
            instructions.Add(new Instruction(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 4)]));
        }

        /// <summary>输出浮点: CALL print_float (R0 = float value)</summary>
        protected void EmitPrintFloat()
        {
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "print_float")]));
        }

        /// <summary>输出布尔: CALL vml_print_bool (R0 = 0/1, prints "true"/"false")</summary>
        protected void EmitPrintBool()
        {
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "print_bool")]));
        }

        /// <summary>输出双精度: CALL vml_print_double (v1.66.41)</summary>
        protected void EmitPrintDouble()
        {
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "print_double")]));
        }

        /// <summary>输出长整数: CALL vml_print_long (v1.66.41)</summary>
        protected void EmitPrintLong()
        {
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "print_long")]));
        }

        /// <summary>统一的打印参数输出 — 求值后 CALL print_str 或 print_int</summary>
        /// <param name="emitExpr">求值表达式，结果在 R0</param>
        /// <param name="isString">true=CALL print_str, false=CALL print_int</param>
        /// <param name="isFloat">true=CALL print_float</param>
        protected void EmitPrintArg(Action emitExpr, bool isString = false, bool isFloat = false)
        {
            emitExpr();
            string func = isFloat ? "print_float" : (isString ? "print_str" : "print_int");
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, func)]));
        }

        /// <summary>
        /// 标准 print 参数循环：依次输出每个参数，参数间空格分隔，末尾换行。
        /// 替代 Go/Fortran 等编译器中重复的 foreach arg → EmitPrintArg → 空格 → 换行 模式。
        /// </summary>
        /// <param name="argCount">参数个数</param>
        /// <param name="emitArg">求值第 i 个参数到 R0 的委托</param>
        /// <param name="isArgString">判断第 i 个参数是否为字符串类型的委托</param>
        /// <param name="addSpace">参数间是否添加空格（默认 true）</param>
        /// <param name="addNewline">末尾是否添加换行（默认 true）</param>
        protected void EmitPrintArgs(int argCount, Action<int> emitArg, Func<int, bool> isArgString,
            bool addSpace = true, bool addNewline = true)
        {
            for (int i = 0; i < argCount; i++)
            {
                bool isStr = isArgString(i);
                EmitPrintArg(() => emitArg(i), isStr);
                if (addSpace && i < argCount - 1)
                {
                    AddInstruction(OpCode.MOVE, Reg(0), Imm(32));
                    EmitPrintChar();
                }
            }
            if (addNewline)
                EmitPrintNewline();
        }

        // ====== 共享数学辅助方法 (CALL → Lib/shared/builtins.c 的 C 函数) ======

        /// <summary>绝对值 (C库): CALL vml_abs (R0 = x, 返回 |x| 在 R0)</summary>
        protected void EmitCallAbs()
        {
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "abs")]));
        }

        /// <summary>最小值 (C库): CALL vml_min (R0=a, R1=b, 返回 min(a,b) 在 R0)</summary>
        protected void EmitCallMin()
        {
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "min")]));
        }

        /// <summary>最大值 (C库): CALL vml_max (R0=a, R1=b, 返回 max(a,b) 在 R0)</summary>
        protected void EmitCallMax()
        {
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "max")]));
        }

        /// <summary>随机数 (C库): CALL vml_random (返回伪随机数在 R0)</summary>
        protected void EmitCallRandom()
        {
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "random")]));
        }

        // ====== 共享表达式求值辅助方法 ======

        /// <summary>标准二目运算求值: emitLeft(); PUSH R0; emitRight(); POP R1; R0=R1 op R0</summary>
        /// <param name="emitLeft">求值左操作数→R0</param>
        /// <param name="emitRight">求值右操作数→R0</param>
        /// <param name="op">运算符: + - * / % &amp; | ^</param>
        protected void EmitBinaryOp(Action emitLeft, Action emitRight, string op)
        {
            emitLeft();
            AddInstruction(OpCode.PUSH, Reg(0));
            emitRight();
            AddInstruction(OpCode.POP, Reg(1));
            // R1 = left, R0 = right
            switch (op)
            {
                case "+": AddInstruction(OpCode.ADD, Reg(0), Reg(1)); break;
                case "-": AddInstruction(OpCode.SUB, Reg(1), Reg(0)); AddInstruction(OpCode.MOVE, Reg(0), Reg(1)); break;
                case "*": AddInstruction(OpCode.MUL, Reg(0), Reg(1)); break;
                case "/": AddInstruction(OpCode.DIV, Reg(1), Reg(0)); AddInstruction(OpCode.MOVE, Reg(0), Reg(1)); break;
                case "%": AddInstruction(OpCode.MOD, Reg(1), Reg(0)); AddInstruction(OpCode.MOVE, Reg(0), Reg(1)); break;
                case "&": case "and": AddInstruction(OpCode.AND, Reg(0), Reg(1)); break;
                case "|": case "or": AddInstruction(OpCode.OR, Reg(0), Reg(1)); break;
                case "^": case "xor": AddInstruction(OpCode.XOR, Reg(0), Reg(1)); break;
                default: break; // 未知操作符，R0保持右值
            }
        }

        /// <summary>栈帧偏移格式化: offset>=0 → R{baseReg}-{offset}, offset&lt;0 → R{baseReg}+{-offset}</summary>
        protected static string MemOff(int offset, int baseReg = 12)
            => offset >= 0 ? $"R{baseReg}-{offset}" : $"R{baseReg}+{-offset}";

        /// <summary>寄存器保护 CALL: PUSH R0-R3; CALL label; POP R3-R1; ADD R13,#4 (丢弃旧R0)</summary>
        /// <param name="label">被调用函数标签</param>
        /// <remarks>R0 保留返回值，栈上旧 R0 通过 ADD R13,#4 丢弃</remarks>
        protected void EmitCallWithRegSave(string label)
        {
            AddInstruction(OpCode.PUSH, Reg(0));
            AddInstruction(OpCode.PUSH, Reg(1));
            AddInstruction(OpCode.PUSH, Reg(2));
            AddInstruction(OpCode.PUSH, Reg(3));
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, label)]));
            AddInstruction(OpCode.POP, Reg(3));
            AddInstruction(OpCode.POP, Reg(2));
            AddInstruction(OpCode.POP, Reg(1));
            // 丢弃栈上旧R0（R0保留返回值）
            AddInstruction(OpCode.ADD, Reg(13), new Operand(OperandType.IMMEDIATE, 4));
        }

        /// <summary>比较并返回布尔值: emitExpr(); CMP/分支; R0=0/1</summary>
        /// <param name="emitExpr">计算表达式，结果在 R0</param>
        /// <param name="branchOp">条件分支操作码 (JE/JNE/JG/JL/JGE/JLE/JZ/JNZ)</param>
        /// <param name="compareValue">比较立即数 (默认 0)</param>
        protected void EmitCompareToBool(Action emitExpr, OpCode branchOp, int compareValue = 0)
        {
            emitExpr();
            instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, compareValue)]));

            // v1.66.5: 使用 CMOVZ/CMOVNZ 替换 JE/JNE 分支模式 (6→3条指令)
            if (branchOp == OpCode.JE || branchOp == OpCode.JZ)
            {
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                instructions.Add(new Instruction(OpCode.CMOVZ, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
            }
            else if (branchOp == OpCode.JNE || branchOp == OpCode.JNZ)
            {
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                instructions.Add(new Instruction(OpCode.CMOVNZ, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
            }
            else
            {
                // JG/JL/JGE/JLE — 需要组合标志, 保持原有分支模式
                var trueLabel = NewLabel();
                var endLabel = NewLabel();
                instructions.Add(new Instruction(branchOp, [new Operand(OperandType.LABEL, trueLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, endLabel)]));
                instructions.Add(new Instruction(OpCode.LABEL, [new Operand(OperandType.LABEL, trueLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
                instructions.Add(new Instruction(OpCode.LABEL, [new Operand(OperandType.LABEL, endLabel)]));
            }
        }

        /// <summary>比较后返回布尔值: 假设 CMP 已由调用方发出，只处理分支→R0=0/1</summary>
        /// <param name="branchOp">条件分支操作码 (JE/JNE/JG/JL/JGE/JLE)</param>
        protected void EmitBoolFromBranch(OpCode branchOp)
        {
            // v1.66.5: 使用 CMOVZ/CMOVNZ 替换 JE/JNE 分支模式 (6→3条指令)
            if (branchOp == OpCode.JE || branchOp == OpCode.JZ)
            {
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                instructions.Add(new Instruction(OpCode.CMOVZ, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
            }
            else if (branchOp == OpCode.JNE || branchOp == OpCode.JNZ)
            {
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                instructions.Add(new Instruction(OpCode.CMOVNZ, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
            }
            else
            {
                var trueLabel = NewLabel();
                var endLabel = NewLabel();
                instructions.Add(new Instruction(branchOp, [new Operand(OperandType.LABEL, trueLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, endLabel)]));
                instructions.Add(new Instruction(OpCode.LABEL, [new Operand(OperandType.LABEL, trueLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
                instructions.Add(new Instruction(OpCode.LABEL, [new Operand(OperandType.LABEL, endLabel)]));
            }
        }

        #endregion

        #region 通用操作数创建

        /// <summary>
        /// 创建一个寄存器操作数
        /// </summary>
        protected static Operand Reg(int regNum)
        {
            return new Operand(OperandType.REGISTER, regNum);
        }

        /// <summary>
        /// 创建立即数操作数
        /// </summary>
        protected static Operand Imm(object value)
        {
            return new Operand(OperandType.IMMEDIATE, value);
        }

        /// <summary>
        /// 创建一个标签/地址操作数
        /// </summary>
        protected static Operand LabelOp(string name)
        {
            return new Operand(OperandType.LABEL, name);
        }

        /// <summary>
        /// 创建间接寻址操作数
        /// </summary>
        protected static Operand Indir(object address)
        {
            // 新约定: 统一使用 MEMORY 代替 INDIRECT
            return new Operand(OperandType.MEMORY, address is int regIdx ? $"R{regIdx}" : address);
        }

        /// <summary>
        /// 创建内存地址操作数
        /// </summary>
        protected static Operand Mem(string addr)
        {
            return new Operand(OperandType.MEMORY, addr);
        }

        #endregion

        #region 便捷指令发射方法

        /// <summary>
        /// 统一字面量加载到 R0: 根据值类型自动选择指令和数据段存储策略。
        /// int→MOVE | long→data section+MOVEL | float→data section+MOVEF | double→data section+MOVED
        /// string→AddStringCached+MOVE | bool→MOVE 0/1 | null→MOVE 0
        /// 替代各编译器 20-40 行重复的 GenerateLiteral switch-case。
        /// </summary>
        protected void EmitLoadConstant(object? value)
        {
            if (value == null)
            {
                AddRI(OpCode.MOVE, 0, 0);
            }
            else if (value is int i)
            {
                AddRI(OpCode.MOVE, 0, i);
            }
            else if (value is long l)
            {
                // 必须始终 MOVEL: MOVE 只写 32 位 registers[0]，不更新 64 位 longRegisters[0]，
                // 后续 MOVEL 存储/长整数运算会读到 0 (v1.66.64 修复)
                string dlabel = $"lng_{instructions.Count}";
                dataSection[dlabel] = l;
                instructions.Add(new Instruction(OpCode.MOVEL,
                    [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, dlabel)],
                    instructions.Count));
            }
            else if (value is float f)
            {
                string flabel = $"flt_{instructions.Count}";
                dataSection[flabel] = f;
                instructions.Add(new Instruction(OpCode.MOVEF,
                    [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, flabel)],
                    instructions.Count));
            }
            else if (value is double d)
            {
                string dlabel = $"dbl_{instructions.Count}";
                dataSection[dlabel] = d;
                instructions.Add(new Instruction(OpCode.MOVED,
                    [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, dlabel)],
                    instructions.Count));
            }
            else if (value is bool b)
            {
                AddRI(OpCode.MOVE, 0, b ? 1 : 0);
            }
            else if (value is string s)
            {
                string lbl = AddStringCached(s);
                AddInstruction(OpCode.MOVE, Reg(0), LabelOp(lbl));
            }
            else
            {
                // 兜底: 尝试转为 int (兼容 char/byte/short 等)
                AddRI(OpCode.MOVE, 0, Convert.ToInt32(value));
            }
        }

        protected void AddR(OpCode op, int reg) =>
            AddInstruction(op, Reg(reg));

        protected void AddRR(OpCode op, int dst, int src) =>
            AddInstruction(op, Reg(dst), Reg(src));

        protected void AddRI(OpCode op, int reg, int imm) =>
            AddInstruction(op, Reg(reg), Imm(imm));

        protected void AddCall(string label) =>
            AddInstruction(OpCode.CALL, LabelOp(label));

        /// <summary>CALL 到 builtins.vml 中的共享函数，替代内联生成</summary>
        protected void EmitCallBuiltin(string name) =>
            AddInstruction(OpCode.CALL, LabelOp(name));

        /// <summary>断言: emitCondition 结果非0通过, 0触发SYSCALL #7</summary>
        /// <param name="emitCondition">条件表达式放入R0的委托</param>
        /// <param name="messageLabel">可选失败消息的 data section 标签</param>
        protected void EmitAssert(Action emitCondition, string? messageLabel = null)
        {
            var passLabel = NewLabel();
            emitCondition();
            AddInstruction(OpCode.TEST, Reg(0), Reg(0));
            AddInstruction(OpCode.JNZ, LabelOp(passLabel));
            if (messageLabel != null)
                AddInstruction(OpCode.MOVE, Reg(0), LabelOp(messageLabel));
            else
                AddInstruction(OpCode.MOVE, Reg(0), Imm(0));
            AddSyscall(7);
            AddLabel(passLabel);
        }

        protected void AddSyscall(int num) =>
            AddInstruction(OpCode.SYSCALL, Imm(num));

        /// <summary>SYSCALL 3: 退出程序（R0 作为退出码）</summary>
        protected void EmitExit() => AddSyscall(3);

        // ====== 命名 SYSCALL 便捷方法 ======

        /// <summary>SYSCALL 53: GetTick → R0 (毫秒时间戳)</summary>
        protected void EmitGetTick() => AddSyscall(53);

        /// <summary>SYSCALL 5: InputChar → R0 (读取单个字符)</summary>
        protected void EmitInputChar() => AddSyscall(5);

        /// <summary>SYSCALL 7: InputInt → R0 (读取整数)</summary>
        protected void EmitInputInt() => AddSyscall(7);

        /// <summary>SYSCALL 41: Free(addr) — R0=addr</summary>
        protected void EmitFree() => AddSyscall(41);

        /// <summary>SYSCALL 50: Random → R0</summary>
        protected void EmitRandom() => AddSyscall(50);

        /// <summary>SYSCALL 102: DeviceRead (R0=handle, R1=buffer, R2=count)</summary>
        protected void EmitDeviceRead() => AddSyscall(102);

        /// <summary>SYSCALL 103: DeviceWrite (R0=handle, R1=data, R2=count)</summary>
        protected void EmitDeviceWrite() => AddSyscall(103);

        /// <summary>SYSCALL 104: DeviceControl (R0=handle, R1=cmd, R2=data, R3=length)</summary>
        protected void EmitDeviceControl() => AddSyscall(104);

        /// <summary>SYSCALL 100: DeviceOpen (R0=name_ptr → R0=handle)</summary>
        protected void EmitDeviceOpen() => AddSyscall(100);

        /// <summary>SYSCALL 101: DeviceClose (R0=handle)</summary>
        protected void EmitDeviceClose() => AddSyscall(101);

        /// <summary>
        /// 生成默认程序入口点 main 标签。设置栈/帧指针，可选自定义体，最后退出。
        /// 替代各编译器手写的 AddDefaultMain（C#/JS/Java 等约 6 处）。
        /// </summary>
        /// <param name="stackTop">栈顶地址（0 表示不显式设置 SP，由运行时初始化）</param>
        /// <param name="frameReg">帧指针寄存器编号（默认 R12）</param>
        /// <param name="stackReg">栈指针寄存器编号（默认 R13）</param>
        /// <param name="customBody">在 SP/BP 设置之后、退出之前执行的自定义代码</param>
        /// <param name="useHalt">true=使用 HALT 退出，false=使用 SYSCALL 3 退出</param>
        protected void EmitDefaultMain(int stackTop = 0, int frameReg = 12, int stackReg = 13,
            Action? customBody = null, bool useHalt = false)
        {
            labels["main"] = instructions.Count;
            if (stackTop > 0)
                AddInstruction(OpCode.MOVE, Reg(stackReg), Imm(stackTop));
            AddInstruction(OpCode.MOVE, Reg(frameReg), Reg(stackReg));
            customBody?.Invoke();
            if (useHalt)
                AddInstruction(OpCode.HALT);
            else
                EmitExit();
        }

        // ====== MCU 安全异常处理 ======

        /// <summary>
        /// MCU 模式 throw: 求值表达式(可选)→EmitExit。
        /// 返回 true=MCU路径已处理(调用方应 return), false=OS模式继续
        /// </summary>
        protected bool HandleMCUThrow(Action? emitExpr = null)
        {
            if (!CurrentOptions.IsMCU) return false;
            emitExpr?.Invoke();
            EmitExit();
            return true;
        }

        /// <summary>
        /// MCU 模式 try: 执行 body, 跳过 catch (MCU无异常运行时)。
        /// 返回 true=MCU路径已处理, false=OS模式继续
        /// </summary>
        protected bool HandleMCUTry(Action emitBody)
        {
            if (!CurrentOptions.IsMCU) return false;
            emitBody();
            return true;
        }

        // ====== 寄存器保护 ======
        /// </summary>
        protected void EmitSaveRegisters(params int[] regs)
        {
            // 从高到低 PUSH，这样栈顶是 regs[0]
            for (int i = regs.Length - 1; i >= 0; i--)
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, regs[i]) }));
        }

        /// <summary>
        /// 从栈恢复寄存器（与 EmitSaveRegisters 配对使用，按相同顺序 POP）
        /// 用法: EmitRestoreRegisters(4, 5, 6, 7)
        /// </summary>
        protected void EmitRestoreRegisters(params int[] regs)
        {
            // 按传入顺序 POP（与 PUSH 顺序相反，实现正确恢复）
            for (int i = 0; i < regs.Length; i++)
                instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, regs[i]) }));
        }

        /// <summary>堆分配: SYSCALL 40 (R0已有size)</summary>
        protected void EmitAlloc() => AddSyscall(40);

        /// <summary>堆分配: MOVE R0, #size; SYSCALL 40</summary>
        protected void EmitAlloc(int size)
        {
            AddRI(OpCode.MOVE, 0, size);
            AddSyscall(40);
        }

        // ═══════════════════════════════════════════════════
        //  寄存器保护工具 — 防止被覆盖
        // ═══════════════════════════════════════════════════

        /// <summary>保存指定寄存器到栈 (PUSH from first to last)</summary>
        protected void EmitSaveRegs(params int[] regs)
        {
            foreach (var r in regs)
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new(OperandType.REGISTER, r) }));
        }

        /// <summary>恢复指定寄存器 (POP from last to first — 与 EmitSaveRegs 配对)</summary>
        protected void EmitRestoreRegs(params int[] regs)
        {
            for (int i = regs.Length - 1; i >= 0; i--)
                instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new(OperandType.REGISTER, regs[i]) }));
        }

        /// <summary>关键区：保存 regs，执行 body，恢复 regs。防止寄存器被内部调用覆盖。</summary>
        protected void EmitPreserveRegs(Action body, params int[] regs)
        {
            EmitSaveRegs(regs);
            body();
            EmitRestoreRegs(regs);
        }

        /// <summary>保存除 exceptReg 外的所有指定寄存器</summary>
        protected void EmitSaveRegsExcept(int exceptReg, params int[] regs)
        {
            for (int i = regs.Length - 1; i >= 0; i--)
                if (regs[i] != exceptReg)
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new(OperandType.REGISTER, regs[i]) }));
        }

        /// <summary>恢复除 exceptReg 外的所有指定寄存器（与 EmitSaveRegsExcept 配对）</summary>
        protected void EmitRestoreRegsExcept(int exceptReg, params int[] regs)
        {
            for (int i = 0; i < regs.Length; i++)
                if (regs[i] != exceptReg)
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new(OperandType.REGISTER, regs[i]) }));
        }

        /// <summary>浮点转整数: F2I R0, R0</summary>
        protected void EmitF2I() => AddRR(OpCode.F2I, 0, 0);
        /// <summary>整数转浮点: I2F R0, R0</summary>
        protected void EmitI2F() => AddRR(OpCode.I2F, 0, 0);
        /// <summary>双精度转整数: D2I R0, R0</summary>
        protected void EmitD2I() => AddRR(OpCode.D2I, 0, 0);
        /// <summary>整数转双精度: I2D R0, R0</summary>
        protected void EmitI2D() => AddRR(OpCode.I2D, 0, 0);
        /// <summary>浮点转双精度: F2D R0, R0</summary>
        protected void EmitF2D() => AddRR(OpCode.F2D, 0, 0);
        /// <summary>双精度转浮点: D2F R0, R0</summary>
        protected void EmitD2F() => AddRR(OpCode.D2F, 0, 0);

        /// <summary>输出字符串: LEA R0, label; SYSCALL 1</summary>
        protected void EmitPrintStr(string label)
        {
            AddInstruction(OpCode.MOVE, Reg(0), LabelOp(label));
            AddSyscall(1);
        }

        /// <summary>输出字符: MOVE R0, #char; SYSCALL 4</summary>
        protected void EmitPrintChar(int c)
        {
            AddRI(OpCode.MOVE, 0, c);
            AddSyscall(4);
        }

        /// <summary>二元运算: POP R1; OP R0,R1,R0</summary>
        protected void EmitBinary(OpCode op)
        {
            AddInstruction(OpCode.POP, Reg(1));
            AddInstruction(op, new List<Operand> { Reg(0), Reg(1), Reg(0) });
        }

        #endregion

        #region 数据段管理

        protected void DataString(string label, string value) =>
            dataSection[label] = value;

        protected void DataWord(string label, int value) =>
            dataSection[label] = value;

        /// <summary>分配字符串常量到 dataSection 并返回标签名</summary>
        protected string EmitStringConstant(string value)
        {
            var label = AddString(value);
            return label;
        }

        /// <summary>加载数据段标签到 R0（LEA 指令）</summary>
        protected void EmitLoadDataLabel(string label)
        {
            AddInstruction(OpCode.MOVE, Reg(0), LabelOp(label));
        }

        /// <summary>分配字符串并加载到 R0: dataSection + LEA</summary>
        protected string EmitLoadStrLEA(string value)
        {
            var label = EmitStringConstant(value);
            EmitLoadDataLabel(label);
            return label;
        }

        /// <summary>整数取绝对值: CMP R0,#0; JGE skip; SUB R0,#0,R0; LABEL skip</summary>
        protected void EmitAbs()
        {
            var skipLabel = NewLabel("abs");
            AddRI(OpCode.CMP, 0, 0);
            AddInstruction(OpCode.JGE, LabelOp(skipLabel));
            AddInstruction(OpCode.SUB, new List<Operand> { Reg(0), Imm(0), Reg(0) });
            AddLabel(skipLabel);
        }

        /// <summary>R0 为零时跳转: TEST R0,R0; JZ label</summary>
        protected void EmitBranchIfZero(string label)
        {
            AddInstruction(OpCode.TEST, Reg(0), Reg(0));
            AddInstruction(OpCode.JZ, LabelOp(label));
        }

        /// <summary>R0 非零时跳转</summary>
        protected void EmitBranchIfNotZero(string label)
        {
            AddInstruction(OpCode.TEST, Reg(0), Reg(0));
            AddInstruction(OpCode.JNZ, LabelOp(label));
        }

        /// <summary>加载对象字段: R0 = [R0 + offset]</summary>
        protected void EmitFieldRead(int offset)
        {
            if (offset != 0)
                AddRI(OpCode.ADD, 0, offset);
            AddInstruction(OpCode.MOVE, Reg(0), Mem("R0"));
        }

        /// <summary>写入对象字段: [R1 + offset] = R0</summary>
        protected void EmitFieldWrite(int offset)
        {
            AddInstruction(OpCode.MOVE, Mem($"R1+{offset}"), Reg(0));
        }

        #endregion

        #region 类型敏感指令选择（虚方法，子类可重写）

        /// <summary>
        /// 根据类型获取加载指令
        /// </summary>
        protected virtual OpCode GetLoadInstruction(string typeName) => OpCode.MOVE;

        /// <summary>
        /// 根据类型获取存储指令
        /// </summary>
        protected virtual OpCode GetStoreInstruction(string typeName) => OpCode.MOVE;

        /// <summary>
        /// 根据类型获取传送指令
        /// </summary>
        protected virtual OpCode GetMoveInstruction(string typeName) => OpCode.MOVE;

        /// <summary>
        /// 根据类型获取压栈指令
        /// </summary>
        protected virtual OpCode GetPushInstruction(string typeName) => OpCode.PUSH;

        /// <summary>
        /// 根据类型获取出栈指令
        /// </summary>
        protected virtual OpCode GetPopInstruction(string typeName) => OpCode.POP;

        /// <summary>
        /// 根据运算和类型获取算术指令
        /// </summary>
        protected virtual OpCode GetArithmeticInstruction(string op, string typeName)
        {
            return op switch
            {
                "+" => OpCode.ADD, "-" => OpCode.SUB, "*" => OpCode.MUL,
                "/" => OpCode.DIV, "%" => OpCode.MOD, _ => OpCode.ADD
            };
        }

        /// <summary>
        /// 根据类型获取比较指令
        /// </summary>
        protected virtual OpCode GetCompareInstruction(string typeName) => OpCode.CMP;

        // ====== 新：类型大小感知重载（委托到 ExpressionManager 静态方法） ======

        /// <summary>
        /// 根据类型大小获取加载指令 (1→LOADB, 2→LOADH, 4→LOAD, 8→DLOAD/MOVEL, float→FLOAD)
        /// </summary>
        protected static OpCode GetLoadInstruction(int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
            => ExpressionManager.SelectLoadOp(byteSize, isFloat, isDouble, isLong);

        /// <summary>
        /// 根据类型大小获取存储指令
        /// </summary>
        protected static OpCode GetStoreInstruction(int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
            => ExpressionManager.SelectStoreUnifiedOp(byteSize, isFloat, isDouble, isLong);

        /// <summary>
        /// 根据类型大小获取传送指令
        /// </summary>
        protected static OpCode GetMoveInstruction(int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
            => ExpressionManager.SelectMoveOp(byteSize, isFloat, isDouble, isLong);

        /// <summary>
        /// 根据类型大小获取压栈指令
        /// </summary>
        protected static OpCode GetPushInstruction(int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
            => ExpressionManager.SelectPushOp(byteSize, isFloat, isDouble, isLong);

        /// <summary>
        /// 根据类型大小获取出栈指令
        /// </summary>
        protected static OpCode GetPopInstruction(int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
            => ExpressionManager.SelectPopOp(byteSize, isFloat, isDouble, isLong);

        /// <summary>
        /// 根据运算和类型获取算术指令
        /// </summary>
        protected static OpCode GetArithmeticInstruction(string op, int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
            => ExpressionManager.SelectArithmeticOp(op, isFloat, isDouble, isLong);

        /// <summary>
        /// 根据类型获取比较指令
        /// </summary>
        protected static OpCode GetCompareInstruction(int byteSize, bool isFloat, bool isDouble = false, bool isLong = false)
            => ExpressionManager.SelectCompareOp(isFloat, isDouble, isLong);

        #endregion

        #region 结构体/记录操作

        /// <summary>
        /// 计算 struct-return 调用所需的 padding 字节数。
        /// 被调用者会将 (argCount+1) 个寄存器参数（隐藏返回指针 + N 个参数）
        /// 保存到 [R12+12], [R12+16], ...，可能覆盖调用者栈帧数据。
        /// 返回需要在 struct 空间和保存的寄存器之间插入的 padding 字节数。
        /// </summary>
        protected static int StructCallPadding(int structSize, int argCount)
        {
            int calleeParamBytes = (argCount + 1) * 4; // 隐藏返回指针 + N 个参数
            return (calleeParamBytes > structSize) ? (calleeParamBytes - structSize) : 0;
        }

        /// <summary>
        /// 按字拷贝结构体：从 [srcReg] 拷贝 byteSize 字节到 [dstReg]。
        /// 使用 tmpReg（默认 R0）作为临时寄存器。
        /// </summary>
        protected void EmitStructCopy(int dstReg, int srcReg, int byteSize, int tmpReg = 0)
        {
            int numWords = (byteSize + 3) / 4;
            for (int w = 0; w < numWords; w++)
            {
                AddInstruction(OpCode.MOVE, Reg(tmpReg), Mem($"R{srcReg}+{w * 4}"));
                AddInstruction(OpCode.MOVE, Mem($"R{dstReg}+{w * 4}"), Reg(tmpReg));
            }
        }

        #endregion

        #region VML程序构造

        /// <summary>
        /// 从当前状态构建 VmlProgram（默认入口点 "main"）
        /// 自动修复 LABEL 指令的标签索引，输出变量统计
        /// </summary>
        protected VmlProgram BuildProgram(string entryPoint = "main")
        {
            // 修复标签：遍历所有 LABEL 指令，将标签索引存入 labels 字典
            for (int i = 0; i < instructions.Count; i++)
                if (instructions[i].Opcode == OpCode.LABEL && instructions[i].Operands.Count > 0)
                    if (instructions[i].Operands[0].Value is string ln)
                        labels[ln] = i;

            // ── 诊断收口 ────────────────────────────────────────────────────────
            // **这里是"一次多报"杠杆最大的一处**：`BuildProgram` 被全部 22 门前端
            // 在自己的 `GenerateCode()` 末尾调用，且对 `Compile` / `CompileFile` /
            // `CompileFileWithIncludes` **三条入口全部生效** —— 不用碰任何一门语言的入口代码。
            //
            // 语义检查（未定义的标识符）**收集并继续生成**，而不是遇到第一个就抛 ——
            // 这正是「一次报出多条」的实现方式：把这一轮能看到的错全收进 bag，
            // 最后一次性抛出去（异常文本是 GCC 风格的多行，宿主侧 `VmlDiagnostics`
            // 本来就是遍历全部匹配、每条各显示一个气泡）。
            if (Diags.HasErrors)
                throw new CompilationException(Diags.FirstErrorCode, Diags.FormatAll());

            // ── 警告要有出口 ────────────────────────────────────────────────────
            // `Diags.AddWarning` 一直是**零调用点**，而这里也从来不打 —— 收集了没人看得见
            // 等于没有。警告与错误的分工：错误挡住编译、警告只是提示
            //（用户要的「定义了却没用的局部变量/函数，要出警告，可以给 IDE 报警告提示用」
            //  就走这条）。格式是 `CompilerError.ToString()` 的 GCC 风格
            // `file:line:col: warning: …`，宿主侧 `VmlDiagnostics` 本来就认这个形状。
            if (Diags.WarningCount > 0)
                foreach (var warn in Diags.Warnings)
                    Console.Error.WriteLine(warn.ToString().TrimEnd());

            Vars?.LogStats();
            return new VmlProgram(instructions, labels, dataSection, constants)
            {
                EntryPoint = entryPoint,
                // 把**这次编译生效中的行号映射**盖上程序对象 —— **必须在这里盖**。
                //
                // 用处：链接器报「未定义的函数」时，要把 `instr.SourceLine`（预处理
                // **拼接后**的行号）换回「原文件的原行」，否则错在 `#include` 进来的
                // 头文件里时会报成用户文件里被顶下去的那一行、文件名也只能写死 `<input>`。
                //
                // ⚠ **不能盖在 `CompileWithDiagnostics` 的出口**：有几门（如 JS）在
                //   自己的编译委托**内部**就调了 `LinkStandardLibrary` —— 那是"先链接、
                //   后盖章"，链接期根本读不到表（实测 js/scm 两门就是这样漏掉的）。
                //   这里是程序**被造出来的那一刻**，早于任何链接，且 22 门的
                //   `GenerateCode()` 末尾都走这一句。
                SourceLineMap = CompilerHelper.ActiveLineMap,
                SourceLines = SourceLines,
                SourceCommentEnabled = VMLPlugins.CompilerOptionsContext.Current.SourceComment,
                RawDirectives = rawDirectives
            };
        }

        #endregion
    }
}
