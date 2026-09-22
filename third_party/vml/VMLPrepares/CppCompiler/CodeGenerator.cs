using VMLAssembler;
using VMLPlugins;
using CompilerBase;

namespace CppCompiler
{
    public enum CppType { Void, Char, Short, Int, Long, Float, Double, Bool, Pointer, Reference }

    public partial class CodeGenerator : CLikeCodegen<CodeGenerator>
    {
        private readonly Program _program;
        private int _nextString;
        private int _stackOffset;
        private readonly Dictionary<string, int> _variables = new();
        private readonly Dictionary<string, string> _varTypes = new();
        private readonly Dictionary<string, bool> _isArrayVar = new();
        private readonly Dictionary<string, int> _arrayTotalSizes = new();
        private readonly Dictionary<string, int> _arrayInnerDim = new(); // 最内层维度大小 (用于多维数组stride计算)
        private int _currentFuncReturnLabel = -1;
        private readonly Dictionary<string, ClassDecl> _classes = new();
        private readonly Dictionary<string, TemplateClassDecl> _templateClasses = new();
        private readonly Dictionary<string, ClassDecl> _templateInstances = new();
        private readonly Dictionary<string, TemplateFunctionDecl> _templateFunctions = new();
        private readonly HashSet<string> _instantiatedTemplateFuncs = new();
        private readonly List<FunctionDecl> _deferredTemplateFuncs = new();
        private readonly List<(string varName, string className)> _classVars = new();
        private readonly Stack<List<(string varName, string className)>> _classVarScopes = new();
        private readonly Dictionary<string, int> _enumValues = new();
        private readonly Dictionary<string, FunctionDecl> _functionDecls = new();
        private readonly HashSet<string> _definedFunctions = new();
        private readonly Dictionary<string, bool> _isReferenceVar = new();
        private string? _currentClass = null;    // Track current class context for RTTI

        /// <summary>
        /// 当前语句来自**哪个文件**（`ASTNode.OriginalFile`，由解析器在语句入口盖）。
        /// 覆写 <see cref="DiagFile"/> 用它 —— **这是头文件里的错能指向头文件的唯一一环**：
        /// 不区分文件的话，`#include` 进来的声明出问题时报的是"主文件 + 头文件的行号"，
        /// 宿主只能把这行号贴到用户自己那句根本没问题的代码上。
        /// </summary>
        private string? _currentOriginFile;

        public CodeGenerator(Program program)
        {
            _program = program;
            InitSimpleCompiler(framePointerReg: 14, threeOperandInt: false, newLabel: () => NewLabel());
        }

        // ====== CLikeCodegen 抽象方法实现 ======
        protected override int GetTypeSizeByEnum(int typeEnum) => ((CppType)typeEnum) switch {
            CppType.Void => 0, CppType.Char => 1, CppType.Short => 2, CppType.Int => 4,
            CppType.Long => 4, CppType.Float => 4, CppType.Double => 8, CppType.Bool => 1,
            CppType.Pointer => 4, CppType.Reference => 4, _ => 4
        };
        protected override bool IsFloatType(int typeEnum) => (CppType)typeEnum is CppType.Float or CppType.Double;
        protected override int GetDefaultType() => (int)CppType.Int;

        private static CppType MapToCppType(string typeName) => typeName switch {
            "void" => CppType.Void, "char" => CppType.Char, "short" => CppType.Short,
            "int" => CppType.Int, "long" => CppType.Long, "float" => CppType.Float,
            "double" => CppType.Double, "bool" => CppType.Bool, _ => CppType.Int
        };

        public override VmlProgram GenerateCode()
        {
            // Pre-pass: collect classes, template classes, enums, and function signatures
            foreach (var decl in _program.Declarations)
            {
                CollectDeclarations(decl);
            }

            foreach (var decl in _program.Declarations)
            {
                GenerateDecl(decl);
            }

            // 生成延迟实例化的模板函数（在 main 等函数体之后、BuildProgram 之前）
            foreach (var fd in _deferredTemplateFuncs)
                GenerateFunction(fd);

            if (!labels.ContainsKey("main"))
                AddDefaultMain();

            return BuildProgram("main");
        }

        private void CollectDeclarations(ASTNode decl)
        {
            switch (decl)
            {
                case ClassDecl cd: _classes[cd.Name] = cd; break;
                case TemplateClassDecl tcd: _templateClasses[tcd.Name] = tcd; break;
                case EnumDecl ed:
                    foreach (var m in ed.Members)
                        _enumValues[m.Name] = m.Value;
                    break;
                case FunctionDecl fd:
                    _functionDecls[fd.Name] = fd;
                    break;
                case TemplateFunctionDecl tfd:
                    _templateFunctions[tfd.Name] = tfd;
                    break;
                case NamespaceDecl ns:
                    foreach (var m in ns.Members) CollectDeclarations(m);
                    break;
                case ExternBlock ext:
                    foreach (var m in ext.Members) CollectDeclarations(m);
                    break;
            }
        }

        private void GenerateDecl(ASTNode decl)
        {
            // 顶层声明也把位置交给诊断（语义与 `GenerateStmt` 那句逐字相同）——
            // 全局初始化式里的「未声明变量」就靠它指向**那一行**，而不是上一句遗留的行号。
            if (decl.Line > 0)
            {
                CurrentSourceLine = decl.Line;
                CurrentSourceColumn = decl.Column;
                CurrentSourceOriginalLine = decl.OriginalLine;
                _currentOriginFile = decl.OriginalFile;
            }
            switch (decl)
            {
                case FunctionDecl fd:
                    if (fd.Body != null)
                        GenerateFunction(fd);
                    // 前向声明 (extern等): 仅存储签名 (_functionDecls 已在 pre-pass 中填充)
                    break;
                case VariableDecl vd:
                    if (vd.Type != "__template_skip__")
                        GenerateGlobalVar(vd);
                    break;
                case MultiVarDecl mvd:
                    foreach (var v in mvd.Variables) GenerateGlobalVar(v);
                    break;
                case ClassDecl cd:
                    // Always generate type_info for RTTI (even non-virtual classes)
                    string typeIdLabel = $"{cd.Name}_typeid";
                    dataSection[typeIdLabel] = cd.Name;

                    bool hasVirtual = cd.Members.Any(m => m.IsVirtual);
                    if (hasVirtual)
                    {
                        // Build vtable: array of function pointers
                        string vtableLabel = $"{cd.Name}_vtable";
                        dataSection[vtableLabel + "_len"] = cd.Members.Count(m => m.IsVirtual) + 1; // +1 for typeid
                        // First vtable entry: type_info pointer
                        dataSection[$"vtab_{cd.Name}_typeid"] = typeIdLabel;
                        int vi = 0;
                        foreach (var m in cd.Members)
                        {
                            if (m.IsVirtual && m.Method != null)
                            {
                                string vte = $"vtab_{cd.Name}_{vi}_{m.Method.Name}";
                                dataSection[vte] = $"method_{cd.Name}_{m.Method.Name}";
                                vi++;
                            }
                        }
                    }
                    // Generate ALL methods (not just virtual), tracking class context
                    string? savedClass = _currentClass;
                    _currentClass = cd.Name;
                    foreach (var m in cd.Members)
                    {
                        if (m.IsMethod && m.Method != null)
                        {
                            string label = $"method_{cd.Name}_{m.Method.Name}";
                            labels[label] = instructions.Count;
                            GenerateFunction(m.Method);
                        }
                    }
                    _currentClass = savedClass;
                    break;
                case NamespaceDecl ns:
                    foreach (var m in ns.Members) GenerateDecl(m);
                    break;
                case ExternBlock ext:
                    foreach (var m in ext.Members) GenerateDecl(m);
                    break;
                case TemplateClassDecl tcd:
                    break; // Stored in pre-pass; instantiated on demand
                case TemplateFunctionDecl:
                    break; // Deferred: instantiated on first call site
                case EnumDecl:
                    break; // Already collected in pre-pass
            }
        }

        private void GenerateFunction(FunctionDecl func)
        {
            _definedFunctions.Add(func.Name);
            string label = func.Name == "main" ? "main" : $"func_{func.Name}";
            labels[label] = instructions.Count;

            // 添加函数注释
            EmitMethodHeader(func.Name, func.ReturnType,
                func.Parameters.Select(p => (p.Type, p.Name)).ToList());
            AddLabel(label);

            int savedReturnLabel = _currentFuncReturnLabel;
            _currentFuncReturnLabel = labelCounter++;
            Vars?.ResetLocals();
            _stackOffset = 0;
            _variables.Clear();
            _isArrayVar.Clear();
            _arrayTotalSizes.Clear();
            _classVars.Clear();
            _classVarScopes.Clear();
            _isReferenceVar.Clear();

            // Pre-allocate parameters via Vars (get offsets before prologue)
            //
            // ⚠ **只有一种布局了**（2026-09-17 调用约定统一）：实参全部右到左压栈 ⇒
            //     第 i 个形参在 `R12 + 12 + 4*i`（R12 上方依次是 R15、R14、返回地址，再往上就是实参区）。
            // 原先这里是三条分支（fastcall 前 4 参进 R0-R3 / stdcall 左到右 / 默认右到左）——
            // 那是**同一门语言里三套约定**，与调用点合不上就是静默的实参错位。
            // `__stdcall` / `__fastcall` 等修饰符仍能解析，但不再影响布局。
            var paramAllocs = new List<(string name, int localOff, string type, bool isRef, int paramOff, bool isRegParam)>();

            int cumOff = 12; // start after R14+R15 push
            for (int i = 0; i < func.Parameters.Count; i++)
            {
                var param = func.Parameters[i];
                bool isStructVal = GetStructSlotCount(param.Type) > 1;
                var paramInfo = Vars?.AllocParam(param.Name, 4, param.Type, param.IsReference || isStructVal);
                int localOff = paramInfo?.Offset ?? cumOff;
                paramAllocs.Add((param.Name, localOff, param.Type, param.IsReference, cumOff, false));
                _variables[param.Name] = localOff;
                _varTypes[param.Name] = param.Type;
                if (param.IsReference || isStructVal)
                    _isReferenceVar[param.Name] = true;
                cumOff += 4;
            }

            // Prologue
            Add(OpCode.PUSH, "R15");
            Add(OpCode.PUSH, "R14");
            Add(OpCode.MOVE, "R14", "R13");
            int frameSize = (Vars?.LocalFrameSize ?? 64) + 64; // Vars local + 64 buffer
            Add(OpCode.SUB, "R13", $"#{frameSize}");

            // Load/save parameters into local slots
            int regIdx = 0;
            foreach (var pa in paramAllocs)
            {
                if (pa.isRegParam)
                {
                    // Fastcall register param: caller already placed them in R0,R1,R2,R3 (left-to-right)
                    string reg = $"R{regIdx}";
                    if (pa.type == "float")
                    {
                        Add(OpCode.I2F, reg, reg);
                        Add(OpCode.MOVEF, Vars?.FormatOffset(pa.localOff) ?? $"R14-{pa.localOff}", reg);
                    }
                    else if (pa.type == "double")
                    {
                        string dreg = $"R{regIdx + 16}"; // D0-D3 = R16-R19
                        Add(OpCode.I2D, dreg, reg);
                        Add(OpCode.MOVED, Vars?.FormatOffset(pa.localOff) ?? $"R14-{pa.localOff}", dreg);
                    }
                    else
                    {
                        Add(OpCode.MOVE, Vars?.FormatOffset(pa.localOff) ?? $"R14-{pa.localOff}", reg);
                    }
                    regIdx++;
                }
                else if (pa.type == "float")
                {
                    Add(OpCode.MOVE, "R0", $"{pa.paramOff}(R14)");
                    Add(OpCode.I2F, "R0", "R0");
                    Add(OpCode.MOVEF, Vars?.FormatOffset(pa.localOff) ?? $"R14-{pa.localOff}", "R0");
                }
                else if (pa.type == "double")
                {
                    Add(OpCode.MOVE, "R0", $"{pa.paramOff}(R14)");
                    Add(OpCode.I2D, "R16", "R0");
                    Add(OpCode.MOVED, Vars?.FormatOffset(pa.localOff) ?? $"R14-{pa.localOff}", "R16");
                }
                else
                {
                    Add(OpCode.MOVE, "R0", $"{pa.paramOff}(R14)");
                    Add(OpCode.MOVE, Vars?.FormatOffset(pa.localOff) ?? $"R14-{pa.localOff}", "R0");
                }
            }

            // 构造函数初始化列表: : member1(val1), member2(val2)
            if (func.InitList.Count > 0)
            {
                Add(OpCode.MOVE, "R3", "R14");
                // "this" pointer is at R14+8 (after saved R15, R14)
                Add(OpCode.MOVE, "R14", "8(R14)");
                int fieldOffset = 0;
                // If class has virtual methods, store vtable pointer as first word of object
                bool hasVirt = _classes.TryGetValue(_currentClass ?? "", out var curCls) && curCls.Members.Any(m => m.IsVirtual);
                if (hasVirt)
                {
                    fieldOffset = 1;
                    // Store type_info address as first word of object (for RTTI + virtual dispatch)
                    Add(OpCode.MOVE, "R0", $"{_currentClass}_typeid");
                    Add(OpCode.MOVE, "(R14)", "R0"); // *this = type_info ptr
                }
                foreach (var init in func.InitList)
                {
                    if (init.Value != null)
                    {
                        GenerateExpr(init.Value);
                        Add(OpCode.MOVE, $"R14-{fieldOffset * 4}", "R0");
                    }
                    fieldOffset++;
                }
                Add(OpCode.MOVE, "R14", "R3");
            }

            if (func.Body != null)
                GenerateStmt(func.Body);

            // RAII: call destructors for class-typed locals in reverse order
            for (int i = _classVars.Count - 1; i >= 0; i--)
            {
                var cv = _classVars[i];
                if (_variables.TryGetValue(cv.varName, out int off))
                {
                    Add(OpCode.MOVE, "R0", Vars?.FormatOffset(off) ?? $"R14-{off}");
                    Add(OpCode.PUSH, "R0"); // this
                    Add(OpCode.CALL, $"method_{cv.className}_~{cv.className}");
                    Add(OpCode.ADD, "R13", "#4");
                }
            }

            string returnLabel = $"ret_{_currentFuncReturnLabel}";
            labels[returnLabel] = instructions.Count;
            Add(OpCode.MOVE, "R13", "R14");
            Add(OpCode.POP, "R14");
            // ⚠ **被调方一律裸 `ret`、不弹实参**（2026-09-17 调用约定统一）。
            // 原先声明成 `__stdcall` 的函数会在这里自己弹掉 `4 + 形参个数×4` 字节，
            // 而调用方那条路也清一次 ⇒ 每次调用净漏栈。现在清栈只归调用方。
            Add(OpCode.POP, "R15");
            // main 返回后通过 SYSCALL 3 退出（R0 中的值作为退出码）
            if (func.Name == "main")
                EmitExit();
            else
                Add(OpCode.RET);

            _currentFuncReturnLabel = savedReturnLabel;
        }

        private void GenerateGlobalVar(VariableDecl vd)
        {
            string label = $"var_{vd.Name}";

            // ── 数组 / 聚合初始化：`int g[4] = {10,20,30,40}` ─────────────────────
            //
            // ⚠ 这一段长期**整个缺失**，是本仓那个"能编、能跑、结果错、不报错"的典型：
            //   `InitializerListExpr` 落到下面的 `EvaluateConstant` 永远返回 false
            //   ⇒ 走 else 分支 ⇒ `dataSection[label] = 0`（**一个 word**），
            //   数组的初始值**全部丢掉而且一声不响**。
            //
            //   实测最小复现：`int g[4] = {10,20,30,40};` —— C 编出来 `G=10203040`，
            //   C++ 编出来 `G=0000`。
            //
            //   影响面极大：**任何带全局数组初始化的 C++ 程序**。最扎眼的一个是
            //   BGI 兼容层里那张 16 色调色板（`static int _bgi_pal[16] = {…}`）——
            //   它变成全 0 之后**所有颜色都成黑的**，于是所有 graphics.h 老程序
            //   都是"跑完了、什么都不报、屏幕一片黑"（v0.96.379 那轮查了半天的那个）。
            if (vd.Initializer is InitializerListExpr listInit)
            {
                var flat = new List<object>();
                FlattenInitList(listInit, flat);
                dataSection[label] = flat.ToArray();
                return;
            }

            if (vd.Initializer != null)
            {
                // Try compile-time constant evaluation for data section
                if (EvaluateConstant(vd.Initializer, out int constVal))
                {
                    dataSection[label] = constVal;
                }
                else
                {
                    // Non-constant initializer: emit init code (will execute if reachable)
                    GenerateExpr(vd.Initializer);
                    dataSection[label] = 0;
                    // v1.65.177: 根据类型选择 store 指令 (float→MOVEF, double→MOVED)
                    var cppType = MapToCppType(vd.Type);
                    var storeOp = cppType switch
                    {
                        CppType.Float => OpCode.MOVEF,
                        CppType.Double => OpCode.MOVED,
                        _ => OpCode.MOVE
                    };
                    Add(storeOp, label, "R0");
                }
            }
            else
            {
                dataSection[label] = 0;
            }
        }

        /// <summary>
        /// 把初始化列表摊平成一维的值数组（与 C 前端 `FlattenArrayInitializer` **同一套语义**，
        /// 只是那边用的是 `ArrayInitializer`/`NumberLiteral`、这边是
        /// `InitializerListExpr`/`IntLiteral` —— 两门前端的 AST 类型名不同，规则是一样的）。
        /// </summary>
        private void FlattenInitList(InitializerListExpr list, List<object> result)
        {
            foreach (var elem in list.Elements)
            {
                switch (elem)
                {
                    case InitializerListExpr nested:
                        FlattenInitList(nested, result);
                        break;
                    case IntLiteral i: result.Add(i.Value); break;
                    case BoolLiteral b: result.Add(b.Value ? 1 : 0); break;
                    case CharLiteral c: result.Add((int)c.Value); break;

                    // ⚠ **字符串必须给它分配一个数据段标签，数组元素存标签名**。
                    //   把内容直接塞进数组的后果是序列化出 `.word abc`（拿内容当标签名），
                    //   而那个标签根本不存在 ⇒ 运行期读到 (null)。
                    //   症状：`char *rows[] = {"abc","def"};` 连顶层全局都取不到。
                    //   **C 前端在同一个地方踩过同一个坑**（那里的注释记着），
                    //   所以这里照它的处置写。
                    case StringLiteral sl:
                        var strLabel = NewLabel();
                        dataSection[strLabel] = sl.Value;
                        result.Add(strLabel);
                        break;

                    default:
                        // 认不出的（表达式、变量引用、函数调用…）**报出来再给 0**。
                        // 「静默当 0」正是本仓反复记的最坏形态：编译成功、程序照跑、结果是错的。
                        Diags.AddError(DiagFile, CurrentSourceLine, CurrentSourceColumn,
                            ErrorCode.Parser_SyntaxError,
                            $"全局数组的初始化里暂不支持这种写法（{elem?.GetType().Name ?? "空元素"}）",
                            "目前只支持字面量（整数/字符/布尔/字符串）与嵌套的 {…}；"
                            + "需要算出来的值请在 main 里赋值。");
                        result.Add(0);
                        break;
                }
            }
        }

        /// Evaluate a compile-time constant expression to an integer value
        private bool EvaluateConstant(Expr expr, out int value)
        {
            value = 0;
            if (expr is IntLiteral il) { value = il.Value; return true; }
            if (expr is BoolLiteral bl) { value = bl.Value ? 1 : 0; return true; }
            if (expr is CharLiteral cl) { value = (int)cl.Value; return true; }
            if (expr is UnaryExpr ue && ue.Op == "-" && EvaluateConstant(ue.Operand, out int negVal))
            { value = -negVal; return true; }
            if (expr is UnaryExpr ue2 && ue2.Op == "+" && EvaluateConstant(ue2.Operand, out int posVal))
            { value = posVal; return true; }
            return false;
        }

        private void GenerateLocalVar(VariableDecl vd)
        {
            // Try template instantiation for unknown class-like types
            if (!_classes.ContainsKey(vd.Type) && !IsBuiltinSTLType(vd.Type, out _))
                TryInstantiateTemplate(vd.Type);

            // Handle built-in STL types: store a POINTER to heap-allocated container
            if (IsBuiltinSTLType(vd.Type, out string stlContainer))
            {
                var stlInfo = Vars?.AllocLocal(vd.Name, 4, vd.Type, false, 0, 0, false);
                int ptrOff = stlInfo?.Offset ?? (_stackOffset + 4);
                _stackOffset = -(Vars?.LocalFrameSize ?? 0); // sync _stackOffset with Vars
                _variables[vd.Name] = ptrOff;
                _varTypes[vd.Name] = vd.Type;
                if (vd.Initializer != null)
                {
                    GenerateExpr(vd.Initializer);           // R0 = container ptr
                    Add(OpCode.MOVE, Vars?.FormatOffset(ptrOff) ?? $"R14-{ptrOff}", "R0");
                }
                else
                {
                    // Default construct: allocate empty container with capacity 16
                    int cap = 16;
                    int totalSize = 8 + cap * 4; // header(8: length+capaity) + data
                    Add(OpCode.MOVE, "R0", $"#{totalSize}");
                    Add(OpCode.CALL, "alloc");
                    Add(OpCode.MOVE, "R1", "R0");
                    Add(OpCode.MOVE, "R0", "#0");
                    Add(OpCode.MOVE, "(R1)", "R0");       // length = 0
                    Add(OpCode.MOVE, "R0", $"#{cap}");
                    Add(OpCode.MOVE, "4(R1)", "R0");      // capacity = 16
                    Add(OpCode.MOVE, "R0", "R1");
                    Add(OpCode.MOVE, Vars?.FormatOffset(ptrOff) ?? $"R14-{ptrOff}", "R0");
                }
                return;
            }

            bool isClass = _classes.TryGetValue(vd.Type, out var clsInfo);
            bool hasVirt = isClass && clsInfo.Members.Any(m => m.IsVirtual);
            int varSize = 4;
            int firstWordOff;
            if (vd.IsArray && vd.ArraySize is IntLiteral arrSize)
            {
                // VML array layout: 4 bytes header (length) + elements
                int elemSize = 4;
                var arrInfo = Vars?.AllocLocalArray(vd.Name, elemSize, arrSize.Value, vd.Type);
                firstWordOff = arrInfo?.Offset ?? (_stackOffset + 4);
                _stackOffset = -(Vars?.LocalFrameSize ?? 0);
                _isArrayVar[vd.Name] = true;
                _arrayTotalSizes[vd.Name] = arrInfo?.Size ?? varSize;
                // Initialize array header with element count
                Add(OpCode.MOVE, "R0", $"#{arrSize.Value}");
                Add(OpCode.MOVE, Vars?.FormatOffset(firstWordOff) ?? $"R14-{firstWordOff}", "R0");
            }
            else if (isClass)
            {
                int fieldCount = clsInfo.Members.Count(m => !m.IsMethod);
                varSize = (hasVirt ? 4 : 0) + fieldCount * 4;
                var classInfo = Vars?.AllocLocal(vd.Name, varSize, vd.Type);
                if (classInfo != null) classInfo.StructType = vd.Type;
                firstWordOff = classInfo?.Offset ?? (_stackOffset + 4);
                _stackOffset = -(Vars?.LocalFrameSize ?? 0);
                // Track for RAII
                _classVars.Add((vd.Name, vd.Type));
            }
            else
            {
                var scalarInfo = Vars?.AllocLocal(vd.Name, varSize, vd.Type,
                    isRef: vd.IsReference);
                firstWordOff = scalarInfo?.Offset ?? (_stackOffset + 4);
                _stackOffset = -(Vars?.LocalFrameSize ?? 0);
            }
            _variables[vd.Name] = firstWordOff;
            _varTypes[vd.Name] = vd.Type;
            if (hasVirt)
            {
                Add(OpCode.MOVE, "R0", $"{vd.Type}_typeid");
                Add(OpCode.MOVE, Vars?.FormatOffset(firstWordOff) ?? $"R14-{firstWordOff}", "R0");
            }
            if (vd.Initializer != null)
            {
                GenerateExpr(vd.Initializer);
                int storeOff = firstWordOff + (hasVirt ? 4 : 0);
                // For arrays, initializer goes after the 4-byte header
                if (vd.IsArray && vd.ArraySize is IntLiteral)
                    storeOff = firstWordOff + 4;
                Add(OpCode.MOVE, Vars?.FormatOffset(storeOff) ?? $"R14-{storeOff}", "R0");
            }
        }

        /// <summary>
        /// 诊断该用的文件：**优先当前语句的原文件**（头文件里的错就报头文件），
        /// 拿不到才退回基类的默认（当前编译的源文件）。
        /// </summary>
        protected override string DiagFile => _currentOriginFile ?? base.DiagFile;

        private void GenerateStmt(Stmt stmt)
        {
            // 让随后生成的每条指令带上源码行号（语义见 CodeGeneratorBase.CurrentSourceLine）。
            // 判据 `> 0`：行号是 1-based，没填的节点是 0，置 0 会把上一句的行号冲掉。
            if (stmt.Line > 0)
            {
                CurrentSourceLine = stmt.Line;
                CurrentSourceColumn = stmt.Column;
                // 原文件行号/文件：诊断给人看的位置（见 CodeGeneratorBase.CurrentSourceOriginalLine）。
                // ⚠ 无条件一起设（不像行号那样判 `> 0`）：换了文件却没换行号，
                //   就会拿**上一个文件的行号**去配**这个文件的名字**，比不设更糟。
                CurrentSourceOriginalLine = stmt.OriginalLine;
                _currentOriginFile = stmt.OriginalFile;
            }
            switch (stmt)
            {
                case ExprStmt es:
                    if (es.Expression != null) GenerateExpr(es.Expression);
                    break;
                case BlockStmt bs:
                    foreach (var s in bs.Statements) GenerateStmt(s);
                    break;
                case IfStmt ifs:
                    Sta.EmitIf(
                        () => GenerateExpr(ifs.Condition),
                        () => GenerateStmt(ifs.ThenBranch),
                        ifs.ElseBranch != null ? () => GenerateStmt(ifs.ElseBranch) : null);
                    break;
                case WhileStmt ws:
                    Sta.EmitWhile(
                        () => GenerateExpr(ws.Condition),
                        () => GenerateStmt(ws.Body));
                    break;
                case ForStmt fs:
                    Sta.EmitFor(
                        emitInit: fs.Initializer != null ? () => GenerateStmt(fs.Initializer) : null,
                        emitCondition: fs.Condition != null ? () => GenerateExpr(fs.Condition) : null,
                        emitIncrement: fs.Increment != null ? () => GenerateExpr(fs.Increment) : null,
                        emitBody: () => GenerateStmt(fs.Body));
                    break;
                case ReturnStmt rs:
                    if (rs.Value != null)
                    {
                        // Struct return: return struct ADDRESS so caller can copy fields
                        if (rs.Value is IdentExpr retId && IsClassTyped(retId))
                        {
                            instructions.Add(new Instruction(OpCode.MOVE, [
                                new(OperandType.REGISTER, 0),
                                new(OperandType.LABEL, $"var_{retId.Name}")
                            ]));
                        }
                        else
                        {
                            GenerateExpr(rs.Value);
                        }
                    }
                    int rl = _currentFuncReturnLabel >= 0 ? _currentFuncReturnLabel : labelCounter - 1;
                    Add(OpCode.JMP, $"ret_{rl}");
                    break;
                case TryStmt ts:
                    string catchLabel = $"catch_{labelCounter++}";
                    string endTryLabel = $"endtry_{labelCounter++}";
                    Add(OpCode.CATCH, catchLabel);
                    GenerateStmt(ts.Body);
                    Add(OpCode.JMP, endTryLabel);
                    labels[catchLabel] = instructions.Count;
                    foreach (var cc in ts.Catches)
                    {
                        if (cc.VariableName != null)
                        {
                            var catchInfo = Vars?.AllocLocal(cc.VariableName, 4, cc.ExceptionType);
                            int catchOff = catchInfo?.Offset ?? (_stackOffset + 4);
                            _stackOffset = -(Vars?.LocalFrameSize ?? 0);
                            _variables[cc.VariableName] = catchOff;
                            if (cc.ExceptionType != null)
                                _varTypes[cc.VariableName] = cc.ExceptionType;
                            Add(OpCode.MOVE, Vars?.FormatOffset(catchOff) ?? $"R14-{catchOff}", "R0");
                        }
                        GenerateStmt(cc.Body);
                        Add(OpCode.ENDCATCH);
                    }
                    labels[endTryLabel] = instructions.Count;
                    break;
                case ThrowStmt ths:
                    if (ths.Expression != null) GenerateExpr(ths.Expression);
                    else Add(OpCode.MOVE, "R0", "#0");
                    Add(OpCode.THROW, "R0");
                    break;
                case SwitchStmt ss:
                    {
                        var cases = new List<(int CaseValue, Action EmitBody)>();
                        Action? defaultBody = null;

                        foreach (var sc in ss.Cases)
                        {
                            if (sc.Value == null)
                            {
                                defaultBody = () => { foreach (var s in sc.Body) GenerateStmt(s); };
                            }
                            else if (sc.Value is IntLiteral il)
                            {
                                var capSc = sc;
                                cases.Add((il.Value, () => { foreach (var s in capSc.Body) GenerateStmt(s); }));
                            }
                        }

                        Sta.EmitSwitch(
                            () => GenerateExpr(ss.Value),
                            cases,
                            defaultBody
                        );
                    }
                    break;
                case BreakStmt:
                    Sta.EmitBreak();
                    break;
                case ContinueStmt:
                    Sta.EmitContinue();
                    break;
                case LabelStmt ls:
                    AddLabel(ls.Name);
                    break;
                case GotoStmt gs:
                    Sta.EmitJump(gs.Target);
                    break;
                case AsmStmt asmSt:
                    Add(OpCode.ASM, asmSt.Code);
                    break;
                default:
                    break;
            }
        }

        /// Check if a type name refers to a built-in STL container (std::vector<T> or std::string)
        private static bool IsBuiltinSTLType(string type, out string container)
        {
            container = "";
            if (type == "std_string" || type == "string") { container = "string"; return true; }
            if (type.StartsWith("std_vector_")) { container = "vector"; return true; }
            return false;
        }

        /// Allocate memory for a built-in STL container: [length(4B), capacity(4B), data...]
        private void AllocSTLContainer(int capacity = 4)
        {
            int totalSize = 8 + capacity * 4;
            Add(OpCode.MOVE, "R0", $"#{totalSize}");
            Add(OpCode.CALL, "alloc");
            Add(OpCode.MOVE, "R1", "R0");
            Add(OpCode.MOVE, "R0", "#0");
            Add(OpCode.MOVE, "(R1)", "R0");         // length = 0
            Add(OpCode.MOVE, "R0", $"#{capacity}");
            Add(OpCode.MOVE, "4(R1)", "R0");        // capacity = cap
            Add(OpCode.MOVE, "R0", "R1");
        }

        private void GenerateNewExpr(NewExpr ne)
        {
            // Try template instantiation for unknown class-like types
            if (!_classes.ContainsKey(ne.Type) && !IsBuiltinSTLType(ne.Type, out _))
                TryInstantiateTemplate(ne.Type);

            // Handle built-in STL types: std::vector<T>, std::string
            if (IsBuiltinSTLType(ne.Type, out string _))
            {
                int cap = Math.Max(ne.Init.Count, 16);
                int totalSize = 8 + cap * 4;
                Add(OpCode.MOVE, "R0", $"#{totalSize}");
                Add(OpCode.CALL, "alloc");
                // Set capacity header
                Add(OpCode.MOVE, "R5", "R0");
                Add(OpCode.MOVE, "R4", $"#{cap}");
                Add(OpCode.MOVE, "4(R5)", "R4");       // capacity at offset 4
                if (ne.Init.Count > 0)
                {
                    // Store initializer values
                    Add(OpCode.PUSH, "R0");              // save ptr
                    for (int i = 0; i < ne.Init.Count; i++)
                    {
                        GenerateExpr(ne.Init[i]);        // R0 = value
                        Add(OpCode.MOVE, "R2", "R0");    // save value in R2
                        Add(OpCode.POP, "R1");           // R1 = ptr
                        Add(OpCode.MOVE, "R3", "(R1)");  // R3 = length
                        Add(OpCode.MOVE, "R4", "R3");
                        Add(OpCode.SHL, "R4", "#2");     // R4 = length * 4
                        Add(OpCode.ADD, "R4", "#8");     // skip 8-byte header
                        Add(OpCode.ADD, "R4", "R1");     // R4 = ptr + 8 + length*4
                        Add(OpCode.MOVE, "(R4)", "R2"); // store value
                        Add(OpCode.ADD, "R3", "#1");     // length++
                        Add(OpCode.MOVE, "(R1)", "R3"); // update length
                        Add(OpCode.PUSH, "R1");           // re-save ptr for next iteration
                    }
                    Add(OpCode.ADD, "R13", "#4");        // pop saved ptr
                    Add(OpCode.MOVE, "R0", "4(R13)");    // restore final ptr → R0 (was first push)
                }
                else
                {
                    Add(OpCode.PUSH, "R0");
                    Add(OpCode.MOVE, "R0", "#0");
                    Add(OpCode.POP, "R1");
                    Add(OpCode.MOVE, "(R1)", "R0");     // length = 0
                    Add(OpCode.MOVE, "R0", $"#{cap}");
                    Add(OpCode.MOVE, "4(R1)", "R0");    // capacity = cap
                    Add(OpCode.MOVE, "R0", "R1");
                }
                return;
            }

            bool isClass = _classes.TryGetValue(ne.Type, out var clsInfo);
            bool hasVirt = isClass && clsInfo.Members.Any(m => m.IsVirtual);
            int fieldCount = isClass ? clsInfo.Members.Count(m => !m.IsMethod) : 1;
            int objSize = (hasVirt ? 4 : 0) + fieldCount * 4;
            Add(OpCode.MOVE, "R0", $"#{objSize}");
            Add(OpCode.CALL, "alloc");      // R0 = allocated ptr
            if (hasVirt)
            {
                Add(OpCode.PUSH, "R0");
                Add(OpCode.MOVE, "R0", $"{ne.Type}_typeid");
                Add(OpCode.POP, "R1");
                Add(OpCode.MOVE, "(R1)", "R0");
                Add(OpCode.MOVE, "R0", "R1");
            }
            if (ne.Init.Count > 0)
            {
                Add(OpCode.PUSH, "R0");          // save ptr
                GenerateExpr(ne.Init[0]);        // R0 = init value
                Add(OpCode.POP, "R1");           // R1 = ptr
                int storeOff = hasVirt ? 4 : 0;
                Add(OpCode.MOVE, $"{storeOff}(R1)", "R0");
                Add(OpCode.MOVE, "R0", "R1");    // R0 = ptr (return)
            }
        }

        /// Try to instantiate a template class if the type name matches a known template pattern.
        /// Returns the concrete ClassDecl, or null if not a template type.
        private ClassDecl? TryInstantiateTemplate(string typeName)
        {
            // Check cache first
            if (_templateInstances.TryGetValue(typeName, out var cached))
                return cached;

            // Try to decompose typeName into TemplateBase + TypeArgs
            // e.g., "vector_int" → base="vector", args=["int"]
            foreach (var (tplName, tplDecl) in _templateClasses)
            {
                if (!typeName.StartsWith(tplName + "_"))
                    continue;

                string argsPart = typeName.Substring(tplName.Length + 1);
                var typeArgs = argsPart.Split('_');

                if (typeArgs.Length != tplDecl.TypeParams.Count)
                    continue;

                // Build concrete ClassDecl by cloning members with type substitution
                var cd = new ClassDecl
                {
                    Name = typeName,
                    BaseClass = tplDecl.BaseClass,
                    CurrentAccess = AccessSpec.Private
                };

                for (int i = 0; i < tplDecl.Members.Count; i++)
                {
                    var src = tplDecl.Members[i];
                    string concreteType = SubstituteTemplateType(src.Type, tplDecl.TypeParams, typeArgs);
                    string concreteName = src.Name;

                    var member = new ClassMember
                    {
                        Access = src.Access,
                        Type = concreteType,
                        Name = concreteName,
                        Initializer = src.Initializer,
                        IsMethod = src.IsMethod,
                        IsConstructor = src.IsConstructor,
                        IsDestructor = src.IsDestructor,
                        IsVirtual = src.IsVirtual,
                        IsStatic = src.IsStatic
                    };

                    if (src.Method != null)
                    {
                        var concretizedMethod = new FunctionDecl
                        {
                            Name = src.Method.Name,
                            ReturnType = SubstituteTemplateType(src.Method.ReturnType, tplDecl.TypeParams, typeArgs),
                            IsMember = true,
                            IsVirtual = src.Method.IsVirtual,
                            IsOverride = src.Method.IsOverride,
                            IsConst = src.Method.IsConst,
                            ClassName = typeName
                        };
                        foreach (var p in src.Method.Parameters)
                        {
                            concretizedMethod.Parameters.Add(new Parameter
                            {
                                Type = SubstituteTemplateType(p.Type, tplDecl.TypeParams, typeArgs),
                                Name = p.Name,
                                IsReference = p.IsReference
                            });
                        }
                        concretizedMethod.Body = src.Method.Body;
                        foreach (var init in src.Method.InitList)
                            concretizedMethod.InitList.Add(new InitEntry { MemberName = init.MemberName, Value = init.Value });
                        member.Method = concretizedMethod;
                    }

                    cd.Members.Add(member);
                }

                _templateInstances[typeName] = cd;
                _classes[typeName] = cd;

                // Generate code for the instantiated class
                string? savedClass = _currentClass;
                _currentClass = cd.Name;
                foreach (var m in cd.Members)
                {
                    if (m.IsMethod && m.Method != null)
                    {
                        string label = $"method_{cd.Name}_{m.Method.Name}";
                        if (!labels.ContainsKey(label))
                        {
                            labels[label] = instructions.Count;
                            GenerateFunction(m.Method);
                        }
                    }
                }
                _currentClass = savedClass;

                return cd;
            }

            return null;
        }

        private static string SubstituteTemplateType(string type, List<string> typeParams, string[] typeArgs)
        {
            for (int i = 0; i < typeParams.Count; i++)
            {
                if (type == typeParams[i])
                    return typeArgs[i];
                if (type.EndsWith("*") && type.TrimEnd('*') == typeParams[i])
                    return typeArgs[i] + "*";
                if (type.EndsWith("&") && type.TrimEnd('&') == typeParams[i])
                    return typeArgs[i] + "&";
            }
            return type;
        }

        /// 根据实参表达式推断其运行时类型名（用于模板函数实例化）
        private static string InferExprType(Expr e) => e switch
        {
            IntLiteral => "int",
            LongLiteral => "long",
            FloatLiteral f => f.IsFloatSuffix ? "float" : "double",
            BoolLiteral => "bool",
            CharLiteral => "char",
            StringLiteral => "char*",
            _ => "int"
        };

        /// 按实参类型实例化模板函数，并登记为延迟生成。返回具体化后的 FunctionDecl。
        private FunctionDecl? InstantiateTemplateFunction(TemplateFunctionDecl tpl, List<Expr> args)
        {
            // 根据第一个实参推断模板参数类型（多参数模板暂按首个类型展开）
            string concreteType = args.Count > 0 ? InferExprType(args[0]) : "int";
            var typeArgs = new[] { concreteType };

            var fd = new FunctionDecl
            {
                Name = tpl.Name,
                ReturnType = SubstituteTemplateType(tpl.ReturnType, tpl.TypeParams, typeArgs),
                Convention = tpl.Convention
            };
            foreach (var p in tpl.Parameters)
            {
                fd.Parameters.Add(new Parameter
                {
                    Type = SubstituteTemplateType(p.Type, tpl.TypeParams, typeArgs),
                    Name = p.Name,
                    IsReference = p.IsReference
                });
            }
            fd.Body = tpl.Body;

            // 同名同类型只生成一次
            string key = $"{tpl.Name}_{concreteType}";
            if (_instantiatedTemplateFuncs.Add(key))
            {
                _definedFunctions.Add(fd.Name);
                _deferredTemplateFuncs.Add(fd);
            }
            return fd;
        }


        private void AddDefaultMain()
        {
            EmitDefaultMain(stackTop: 1048572, frameReg: 14);
        }

        // Instruction helpers
        private void Add(OpCode op, List<Operand> operands)
        {
            instructions.Add(new Instruction(op, operands));
        }

        private void Add(OpCode op, string? a = null, string? b = null, string? c = null)
        {
            var operands = new List<Operand>();
            void AddOp(string? s)
            {
                if (s == null) return;
                if (s.StartsWith("#")) operands.Add(new Operand(OperandType.IMMEDIATE, int.Parse(s.Substring(1))));
                else if (s == "(R0)" || s == "(R1)" || s == "(R2)" || s == "(R3)")
                    operands.Add(Mem(s.Substring(1, s.Length - 2)));
                else if (s.StartsWith("[") && s.EndsWith("]"))
                    operands.Add(new Operand(OperandType.MEMORY, s));  // [R0] → MEMORY, ParseMemoryString 处理
                else if (s.StartsWith("R") && s.Length > 1 && s.Substring(1).All(char.IsDigit))
                    operands.Add(new Operand(OperandType.REGISTER, int.Parse(s.Substring(1))));
                else if (s.StartsWith("var_"))
                    operands.Add(new Operand(OperandType.MEMORY, s));
                else if (s == "main" || s.StartsWith("func_") || s.StartsWith("method_") ||
                         s.StartsWith("sw") || s.StartsWith("ret_") ||
                         s.StartsWith("for") || s.StartsWith("while") || s.StartsWith("else") ||
                         s.StartsWith("endif") || s.StartsWith("wend") || s.StartsWith("cmp_") ||
                         s.StartsWith("swcase") || s.StartsWith("swnext") || s.StartsWith("swend") ||
                         s.StartsWith("str_"))
                    operands.Add(new Operand(OperandType.LABEL, s));
                else if (s.Contains("-") || s.Contains("+") || s.Contains("(") || s.Contains(")"))
                    operands.Add(new Operand(OperandType.MEMORY, s));
                else operands.Add(new Operand(OperandType.LABEL, s));
            }
            AddOp(a);
            if (b != null) AddOp(b);
            if (c != null) AddOp(c);
            instructions.Add(new Instruction(op, operands));
        }
    }
}
