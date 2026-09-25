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

        /// <summary>进函数时的 `_varTypes` 快照 —— 出函数时恢复（见函数入口那段注释）。</summary>
        private Dictionary<string, string>? _varTypesSnapshot;

        /// <summary>
        /// **全局/静态数组**的名字。
        ///
        /// ⚠ 必须与 <see cref="_isArrayVar"/> **分开**：那张表是**局部**变量的，
        ///   而 `GenerateFunction` 一进门就把它 `Clear()` 掉（每个函数一份局部作用域）。
        ///   全局数组登记在那边的话，**进第一个函数就被抹掉** ——
        ///   症状是"登记了、也判了，就是不生效"，实测踩过一次。
        /// </summary>
        private readonly HashSet<string> _globalArrays = new();

        /// <summary>
        /// **字节数组**（`char x[]` / `char buf[N]` / `unsigned char` / `bool`）的名字。
        ///
        /// 这门前端的数组默认是 VML 那套「`[4 字节长度头][元素，每格 4 字节]`」的布局，
        /// 而**字符数组不能是那个布局** —— 它要当 C 字符串用（`outtextxy` / `strlen` /
        /// `ui_text` 收的都是「一串以 NUL 结尾的字节」）。
        /// 拿带长度头的 4 字节格数组去当 C 字符串，读到的第一个"字符"是**长度头**、
        /// 第二个字节是 0 ⇒ 屏幕上只剩**一个乱字符**。
        /// 实测（`Examples/bgi/barChart.cpp` 那条路）：`char title[] = "Bar Chart";`
        /// 画出来是 2 个豆腐块；`hut.cpp` 的 `char stringData1[] = "Home Sweet Home";`
        /// 整串画不出来。
        ///
        /// ⇒ 这里给字符数组**一条自己的布局**：**元素 1 字节、没有长度头**（与 C 一致），
        ///   数据段存 `byte[]`，下标步长 1、不加 `+4`。
        ///   `[]` 的读/写/取址三处都要按这张表走（见 `ArrayIndexInfo`）。
        ///
        /// ⚠ 与 <see cref="_globalArrays"/> 一样**不随函数清空** —— 它记的是"这个名字是
        ///   什么布局"，名字在整份程序里唯一。
        /// </summary>
        private readonly HashSet<string> _byteArrays = new();

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
        /// <summary>构造函数符号名 —— 与 <see cref="MethodSymbol"/> 同一套口味，单独一个前缀好认。</summary>
        private static string CtorSymbol(string className, IEnumerable<string> paramTypes)
        {
            string suffix = "";
            foreach (var t in paramTypes) suffix += t switch
            {
                "float" => "f", "double" => "d", "char" => "c", "bool" => "b",
                "int" or "long" or "short" or "unsigned" or "unsigned int" => "i",
                _ => t.EndsWith("*") ? "p" : "v",
            };
            return suffix.Length > 0 ? $"ctor_{className}_{suffix}" : $"ctor_{className}";
        }

        private string? _currentClass = null;    // Track current class context for RTTI
        /// <summary>
        /// 成员函数里 `this` 所在的**局部槽偏移**。序言从栈上把它取出来存进这个槽，
        /// `ThisExpr` 与裸成员访问都读它。
        ///
        /// ⚠ **「有没有这个槽」必须用 <see cref="_hasThis"/> 判，绝不能用 `_thisSlot >= 0`**。
        ///   `VarMemManager.AllocLocal` 是**从帧指针向负方向**分配的（`_localBottom - size`），
        ///   所以第一个局部量 —— 也就是这里的 `__this` —— 偏移恒为 **-4**。
        ///   写成 `_thisSlot >= 0` 就是把"分配到了槽"判成"没有槽"：帧照开（4 字节）、
        ///   `this` 的**读**又恰好有一条 `-4` 兜底（见 `GenerateExpr` 的隐式 `this->f`
        ///   与 `ThisExpr`），于是**只有写槽那一句被跳过** —— 读到一个从没写过的槽，
        ///   值恒为 0，而生成的指令看着一句不少。
        /// </summary>
        private int _thisSlot = -1;

        /// <summary>当前函数是不是成员函数（有 `this` 槽）。见 <see cref="_thisSlot"/> 的警告。</summary>
        private bool _hasThis;

        /// <summary>
        /// 正在生成**函数体**（而不是顶层/全局作用域）。
        ///
        /// <para>
        /// 用来决定"一个变量声明该落在**栈帧**里还是数据段里"。此前这个判断**整个是缺的** ——
        /// 函数体里的 `int i;` 会被当成**全局量**（`var_i`）发出去，而所有函数的 `i`
        /// **共用同一个标签**。后果极隐蔽：`Game::Draw` 里
        /// <c>while (i &lt; 8) { actors[i]-&gt;Draw(); i = i + 1; }</c> 的第一个 `Draw()`
        /// 进去（`Sky::Draw` 自己也有个 `i`，循环到 26）回来之后，`var_i` 已经是 26 ——
        /// **循环直接结束**，后面 7 个一个都不画。
        /// 而"白天"那一路 `Sky::Draw` 不走那个循环 ⇒ 同一个程序**白天正常、夜里丢东西**，
        /// 看着像绘制问题、其实是变量作用域。
        /// </para>
        /// <para>
        /// ⚠ 局部量上栈帧之后，**帧大小必须在函数体生成完之后回填**，
        /// 见 <c>GenerateFunction</c> 里 `frameOperand` 那两句。
        /// </para>
        /// </summary>
        private bool _inFunctionBody;

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
                    {
                        // ⚠ 类成员方法在 AST 里**同时**挂在 ClassDecl 下、又被提升成一个顶层
                        //   FunctionDecl（这样调用点才能按普通函数名找到它）。于是方法体被
                        //   生成**两趟**：ClassDecl 那趟带着 `_currentClass`（成员访问能解析），
                        //   顶层这趟没有 ⇒ 方法体里的裸字段名一路退化成"读同名全局变量"，
                        //   生成出来的指令完全正常、只是值恒为 0，且**哪一趟先谁后**取决于
                        //   声明顺序 —— 这类"看着对、值不对"最难查。
                        //   用 FunctionDecl 自带的 ClassName 把上下文补齐，两趟一致。
                        string? savedCls = _currentClass;
                        if (fd.IsMember && !string.IsNullOrEmpty(fd.ClassName))
                            _currentClass = CleanType(fd.ClassName!);
                        GenerateFunction(fd);
                        _currentClass = savedCls;
                    }
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

                    // ⚠ 判据是 **ClassHasVirtualDeep**，不是 `cd.Members.Any(IsVirtual)`：
                    //   派生类可能一个新虚函数都没声明（只是继承），深判据为真、浅判据为假 ——
                    //   那样的类**也需要自己的一张表**（表里放基类的实现），因为它的构造函数
                    //   会把这张表的地址写进对象；漏了就是链接期"未定义的标签"。
                    if (ClassHasVirtualDeep(cd.Name))
                    {
                        // 虚表 = **连续**的指针数组：`[typeid][槽 0][槽 1]…`。
                        //
                        // ⚠ 原先那张"表"是**一组各自独立的标签**（`vtab_类_序号_方法名`），
                        //   根本没有连续地址 ⇒ 运行期没法按槽号索引 ⇒ 只能退化成
                        //   "拿对象里的 typeid 一路比下去"的级联比较。连续数组才能 `[vptr+4*槽]` 取。
                        //   元素是**标签名字符串**放在 `object[]` 里 —— 那正是本仓"指针表"的形态
                        //   （见 `VmlProgram.DataRefs`：只有 `object[]` 里的字符串是标签名）。
                        var slots = VirtualSlots(cd.Name);
                        int n = slots.Count;
                        var table = new object[n + 1];
                        table[0] = typeIdLabel;                       // 槽 -1：type_info
                        foreach (var kv in slots)
                        {
                            // 槽里放**这个类实际生效的那个实现**（重写则放自己的，否则沿基类找）
                            table[kv.Value + 1] = VirtualSlotSymbol(cd.Name, kv.Key)
                                                  ?? typeIdLabel;     // 理论上到不了，防御
                        }
                        dataSection[$"{cd.Name}_vtable"] = table;
                        dataSection[$"{cd.Name}_vtable_slots"] = n;
                    }
                    // Generate ALL methods (not just virtual), tracking class context
                    string? savedClass = _currentClass;
                    _currentClass = cd.Name;
                    foreach (var m in cd.Members)
                    {
                        // ⚠ 构造函数（IsConstructor）**不在 IsMethod 里** —— 原来这一支只认
                        //   IsMethod，于是构造函数体从没被生成过（`P p;` 之后字段全是 0）。
                        if (m.IsConstructor && m.Method != null)
                        {
                            string clabel = CtorSymbol(cd.Name, m.Method.Parameters.Select(p => p.Type));
                            labels[clabel] = instructions.Count;
                            GenerateFunction(m.Method);
                        }
                        else if (m.IsMethod && m.Method != null)
                        {
                            string label = MethodSymbol(cd.Name, m.Method.Name, m.Method!.Parameters.Select(p => p.Type));
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
            // ⚠ `_thisSlot` / `_hasThis` 与 `_currentClass` 也要 save/restore：
            //   生成一个函数体的过程中会**再进 GenerateFunction**（模板实例化、内联展开），
            //   而函数开头那句复位会把外层方法的 this 上下文冲掉 —— 回到外层函数体时
            //   判定就变成"没有 this"，成员访问悄悄退化成"读同名全局变量"。
            //   症状极隐蔽：生成出来的指令看着正常，只是值恒为 0。
            int savedThisSlot = _thisSlot;
            bool savedHasThis = _hasThis;
            bool savedInFunction = _inFunctionBody;
            string? savedClassCtx = _currentClass;
            _currentFuncReturnLabel = labelCounter++;
            _inFunctionBody = true;
            Vars?.ResetLocals();
            _stackOffset = 0;
            _variables.Clear();
            _isArrayVar.Clear();      // ⚠ 只是**局部**那张；全局数组在 `_globalArrays`，别加进来
            _arrayTotalSizes.Clear();
            _classVars.Clear();
            _classVarScopes.Clear();
            _isReferenceVar.Clear();

            // ⚠⚠ **`_varTypes` 要存快照、出函数时恢复** —— 它是**唯一**跨函数残留的表。
            //
            //   上面那六张表都是"进函数就 Clear"，唯独 `_varTypes` 不能照做：
            //   它还装着**全局量**的类型（`GenerateGlobalVar` 里登记的），清掉就全没了。
            //   于是形参的类型登记（`_varTypes[param.Name] = param.Type`）会**留下来**，
            //   污染后面所有函数里**同名**的东西。
            //
            //   实测（`scripts/vml-cpp-probe/cases/f40_single_letter_param.cpp`）：
            //   `int p7(int a,int b,int c,int d,int e,int f,int g)` 的形参 `g` 登记成 `int`，
            //   而 `main` 里 `G g;` 声明完再读 `g.Get()` 时，`_varTypes["g"]` 已经不是 `G` 了
            //   ⇒ `ResolveClassOf` 拿不到类 ⇒ 符号退化成 `method_g_Get`（拿变量名当类名）
            //   ⇒ 链接期「未定义的函数」，**而解析期一个错都不报**。
            //   这解释了那条一直没定位的 OPEN #17："单字母形参 + 参数多"只是让它更容易撞名，
            //   真正的条件是**形参名与后面某处的变量名重名**。
            _varTypesSnapshot = new Dictionary<string, string>(_varTypes);

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

            // ── 成员函数的 `this`：先**分配槽位**（必须在下面算 frameSize 之前，
            //    否则这个局部槽不占栈帧）──
            // 调用方**最先**压 `this`（见 GenerateCallExpr 的 hasThis），之后才右到左压实参
            // ⇒ `this` 落在所有形参**之上**：R14 相对偏移 = 12 + 4×形参个数。
            //
            // ⚠ 这一级原先整个是缺的：`FunctionDecl.IsMember` 解析器设了、**代码生成从没读过**。
            //   调用点压进去的 `this` 没人接，而 `ThisExpr` 读的是 R14 —— 那是**帧指针**，
            //   于是 `this->x` 指向栈帧。初始化列表那段更早，假设 `this` 在 `R14+8`
            //   （那是**返回地址**），还用 `MOVE R14, 8(R14)` 把帧指针本身覆盖掉了。
            // ⚠ 判据**必须带上 `func.ClassName`**：`IsMember` 是解析时才有的东西
            //   （`ParseClassMember` 那条路设），而生成期真正要靠的是"这个函数属于哪个类"。
            //   两者任一为真就认 —— 只认 `IsMember` 会让任何漏设该位的路径（模板实例化
            //   造出来的 `FunctionDecl` 等）静默丢掉 `this`。
            //
            // ⚠ 「有没有槽」记在 `_hasThis` 上，**不要用 `_thisSlot >= 0` 判** ——
            //   槽偏移是从帧指针向负方向分配的，第一个局部量恒为 `-4`（见 `_thisSlot` 的注释）。
            _thisSlot = -1;
            _hasThis = false;
            if (!string.IsNullOrEmpty(func.ClassName))
                _currentClass = CleanType(func.ClassName!);
            if (func.IsMember || !string.IsNullOrEmpty(func.ClassName))
            {
                var thisInfo = Vars?.AllocLocal("__this", 4, "int*");
                _thisSlot = thisInfo?.Offset ?? -4;
                _hasThis = true;
            }

            // Prologue
            Add(OpCode.PUSH, "R15");
            Add(OpCode.PUSH, "R14");
            Add(OpCode.MOVE, "R14", "R13");
            // ⚠ 帧大小**先按当前已分配量开**，函数体生成完之后再**回填一次**
            //   （见本函数末尾那处 `frameOperand.Value = …`）。
            //   原因：局部变量是在**生成函数体的过程中**才逐个分配的，
            //   而这里必须给出一个数 —— 老写法只算"参数 + `__this`"，
            //   于是函数体里的局部量**一个都不在帧里**。此前它们的落点被
            //   整个绕过（当全局量用），所以看不出来；一旦让它们真的落在栈帧上，
            //   不回头改这个数就会写到帧**外面**去。
            int frameSize = (Vars?.LocalFrameSize ?? 64) + 64; // Vars local + 64 buffer
            var frameOperand = new Operand(OperandType.IMMEDIATE, frameSize);
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> {
                new(OperandType.REGISTER, 13), frameOperand
            }));

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

            // `this` 从栈上取出来存进刚才那个槽（帧已经开好了，R14 是帧指针）
            if (_hasThis)
            {
                Add(OpCode.MOVE, "R0", $"{12 + func.Parameters.Count * 4}(R14)");
                Add(OpCode.MOVE, Vars?.FormatOffset(_thisSlot) ?? $"R14-{_thisSlot}", "R0");
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

            // 函数体里的局部变量到这一步才全都分配完 ⇒ **回填帧大小**
            // （与开头那句成对，理由见那里）。
            frameOperand.Value = (Vars?.LocalFrameSize ?? 64) + 64;

            _currentFuncReturnLabel = savedReturnLabel;
            // 恢复 `_varTypes`（见函数入口那段注释）—— **必须放在所有生成动作之后**，
            // 否则调用方后续还要读的类型就没了。
            _varTypes.Clear();
            if (_varTypesSnapshot != null)
            {
                foreach (var kv in _varTypesSnapshot) _varTypes[kv.Key] = kv.Value;
            }

            _thisSlot = savedThisSlot;
            _hasThis = savedHasThis;
            _currentClass = savedClassCtx;
            _inFunctionBody = savedInFunction;
        }

        /// <summary>
        /// 声明的**元素类型**是不是「1 字节宽」（`char` / `signed char` / `unsigned char` / `bool`）。
        /// 传进来的是类型串，可能带修饰（`const char`）与指针（`char*`）—— 指针一律**不算**
        /// （`char *rows[]` 是指针表，元素 4 字节）。
        /// </summary>
        private static bool IsCharLikeType(string? type)
        {
            if (string.IsNullOrEmpty(type) || type.Contains('*')) return false;
            var t = type.Replace("const", " ").Replace("volatile", " ").Trim().ToLowerInvariant();
            while (t.Contains("  ")) t = t.Replace("  ", " ");
            return t is "char" or "signed char" or "unsigned char" or "bool" or "_bool" or "char8_t";
        }

        /// <summary>
        /// 这条声明是不是一个**字节数组**（`char x[]` / `char buf[N]`，见 <see cref="_byteArrays"/>）。
        /// 指针数组（`char *rows[]`）**不是** —— 它的元素是指针。
        /// </summary>
        private static bool IsByteArrayDecl(string? type, bool isArray)
            => isArray && IsCharLikeType(type);

        /// <summary>
        /// 字节数组的**数据段内容**：字符串字面量取正文、初始化列表取各元素、
        /// 其余（无初始化器）全 0；长度按声明的元素个数，没给就按内容长度
        /// （字符串含结尾 NUL）。
        ///
        /// ⚠ **不能**像普通数组那样套 `[长度头][元素]` 的布局 —— 它不是 VML 数组，
        ///   是一段 C 字符串（见 <see cref="_byteArrays"/>）。
        /// </summary>
        private static byte[] BuildByteArrayData(Expr? init, IntLiteral? declaredSize)
        {
            var bytes = new List<byte>();
            switch (init)
            {
                case StringLiteral sl:
                    foreach (var ch in sl.Value ?? "") bytes.Add((byte)(ch & 0xFF));
                    bytes.Add(0);                        // C 字符串的结尾 NUL
                    break;
                case InitializerListExpr il:
                    var flat = new List<object>();
                    FlattenInitListStatic(il, flat);
                    foreach (var e in flat)
                        bytes.Add(e is int iv ? (byte)(iv & 0xFF)
                                : e is string lbl ? (byte)0      // 元素是字符串标签 ⇒ 认不出，给 0
                                : (byte)0);
                    break;
                default:
                    break;                               // 无初始化器 ⇒ 全 0
            }
            int declared = declaredSize?.Value ?? bytes.Count;
            if (declared < 1) declared = 1;
            while (bytes.Count < declared) bytes.Add(0);
            if (bytes.Count > declared) bytes.RemoveRange(declared, bytes.Count - declared);
            return bytes.ToArray();
        }

        /// <summary>
        /// `FlattenInitList` 的**静态**版本（同一套语义，只是不需要实例）。
        ///
        /// ⚠ 两份实现是不得已：`FlattenInitList` 在模板解析失败时要往 `Diags` 报错，
        ///   而这里（数据段的静态构造）拿不到诊断上下文。**规则本身必须一致** ——
        ///   只认字面量、嵌套列表摊平、认不出的给 0。
        /// </summary>
        private static void FlattenInitListStatic(InitializerListExpr list, List<object> result)
        {
            foreach (var elem in list.Elements)
            {
                switch (elem)
                {
                    case InitializerListExpr nested: FlattenInitListStatic(nested, result); break;
                    case IntLiteral i: result.Add(i.Value); break;
                    case BoolLiteral b: result.Add(b.Value ? 1 : 0); break;
                    case CharLiteral c: result.Add((int)c.Value); break;
                    case StringLiteral sl: result.Add(sl.Value ?? ""); break;
                    default: result.Add(0); break;
                }
            }
        }

        private void GenerateGlobalVar(VariableDecl vd)
        {
            string label = $"var_{vd.Name}";

            // 全局量的**类型**要在这里就记下来，不能等到下面那条标量分支 ——
            // 数组（含**指针数组**）走的是各自的分支并 `return`，永远到不了那句赋值，
            // 于是 `_varTypes` 里没有它。而 `ResolveClassOf` 正是靠 `_varTypes` 找
            // 接收者的类 ⇒ 全局的 `Building* BL[4]` 取 `(*BL[i])` 时解析不出类，
            // 方法符号退化成 `method__Setup`（类名是空串）⇒ 链接期"未定义的函数"。
            // 放在最前面一句，所有分支都覆盖得到。
            if (!string.IsNullOrEmpty(vd.Type)) _varTypes[vd.Name] = vd.Type;

            // ── 字节数组（`char x[] = "…"` / `char buf[N]`）──────────────────────
            //
            // ⚠ **必须排在下面那条"数组 / 聚合初始化"之前**：那条给所有数组都套
            //   `[长度头][每格 4 字节]` 的布局，而字符数组要的是**一段 C 字符串**。
            //   排在后面 = 永远走不到（`vd.IsArray` 先命中）。
            //
            //   实测（用户报的）：`barChart.cpp` 的 `char title[] = "Bar Chart";`
            //   画出来是**两个豆腐块**；`Hut.cpp` 的 `char stringData1[] = "Home Sweet Home";`
            //   整串画不出来。原因就是这条路径此前对 `char[] = "字面量"` **什么都不做**：
            //   `vd.Initializer` 是 `StringLiteral`（不是 `InitializerListExpr`）⇒ 摊平结果是空
            //   ⇒ 数组长度按 0 算、正文一个字节都不落盘。
            if (IsByteArrayDecl(vd.Type, vd.IsArray))
            {
                dataSection[label] = BuildByteArrayData(vd.Initializer, vd.ArraySize as IntLiteral);
                _globalArrays.Add(vd.Name);
                _byteArrays.Add(vd.Name);
                return;
            }

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
            if (vd.IsArray || vd.Initializer is InitializerListExpr)
            {
                var flat = new List<object>();
                if (vd.Initializer is InitializerListExpr listInit) FlattenInitList(listInit, flat);

                // 补足到声明的元素个数（`int g[4] = {1,2};` 后面两个是 0）
                int declared = vd.ArraySize is IntLiteral sz ? sz.Value : flat.Count;
                // ⚠ 补的量按 **`declared × 每个元素占几格`**，不是 `declared` 个格 ——
                //   见下面 `elemWords` 的说明。
                int elemWords = Math.Max(1, (SizeOfDeclaredType(vd.Type) + 3) / 4);
                while (flat.Count < declared * elemWords) flat.Add(0);

                // ⚠ **必须补那个 4 字节长度头**。C++ 前端的数组布局是
                //   `[长度(4B)][元素…]` —— 见 `GenerateLocalVar` 的注释
                //   "VML array layout: 4 bytes header (length) + elements"，
                //   而 `[]` 的代码生成也会 `+4` 跳过它。数据段不补的话，
                //   **读到的"第一个元素"其实是长度头**，后面全部错位。
                //   （C 前端是**另一套**布局：没有头、`[]` 也不 `+4` ——
                //    两边各自自洽，别按 C 那边照抄。）
                var withHeader = new List<object> { declared };
                withHeader.AddRange(flat);
                dataSection[label] = withHeader.ToArray();

                // ⚠ **登记成数组**。`IdentExpr` 里有一条"数组要返回**地址**而不是值"的分支
                //   （`CodeGenerator.Expressions.cs` 的 `_isArrayVar` 那一支），
                //   而此前**只有 `GenerateLocalVar` 登记**、全局数组一个都没登记
                //   ⇒ 全局数组走"普通变量"那条 ⇒ 取出来的是**第一个元素的值**，
                //   再拿它当地址加下标 ⇒ 读到的永远是垃圾。
                //   这是"助手已经写好了、调用点绕过去了"的又一例。
                _globalArrays.Add(vd.Name);
                return;
            }

            if (vd.Initializer != null)
            {
                // Try compile-time constant evaluation for data section
                if (EvaluateConstant(vd.Initializer, out int constVal))
                {
                    dataSection[label] = constVal;
                }
                // ── 指针 = 字符串字面量：`char *p = "…";` ────────────────────────
                //
                // ⚠ 此前它落到下面的 else：`GenerateExpr(StringLiteral)` 发一条 `MOVE R0, str_N`、
                //   再发一条 `MOVE [var_p], R0`。这两条**落在顶层指令流里**（不是任何函数体内），
                //   而顶层那一段**根本不会被执行**（前面刚 `ret` 过）⇒
                //   `var_p` 永远是 0 ⇒ 程序拿到空指针、**一个字都画不出来，也不报错**。
                //   实测：`char *gp = "GPOINT"; outtextxy(10,10,gp);` C 正确、C++ 一片空白。
                //
                // 正解是**不生成任何运行期代码**：把"指向那段字符串"这件事直接写进数据段
                //   （`LabelRef` ⇒ 序列化成 `.word str_N`，链接器解析成地址）——
                //   与 C 前端 `CodeGenerator.Functions.cs` 那条**同一条通路**。
                else if (vd.Initializer is StringLiteral strPtrInit && vd.Type.Contains('*'))
                {
                    string strLitLabel = NewLabel();
                    dataSection[strLitLabel] = strPtrInit.Value ?? "";
                    dataSection[label] = new LabelRef(strLitLabel);
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
            else if (vd.IsArray)
            {
                // 未初始化的全局数组：同样要有 `[长度][元素…]`（见上面那段说明）
                //
                // ⚠⚠ **一格必须占"一个元素"那么大，不能恒按 4 字节算。**
                //   访问侧（`ArrayIndexInfo` → `GetElementSize`）早就是按元素的真实字节数
                //   跨步的，这里却写死"一格 4 字节"——**同一件事两处实现**，
                //   只要元素不是 4 字节就必然分家：
                //     `static Tree TREES[6]`（`Tree` 4 个 int = 16 字节）
                //     ⇒ 这里只给 6 格（24 字节），而访问侧按 16 跨步
                //     ⇒ **第 2 个元素就写出数据段**，踩在别的全局量上。
                //   实测症状（gorilla.cpp）：`--frame` 只剩天空与楼房、
                //   `--frames` **一帧都不导**（`Game::Draw` 的流程被写坏，
                //   `ui_present()` 根本执行不到），而编译期**零报错**。
                //   这条与 F21「对象数组的元素步长」是同一族：**分配与访问必须同源**。
                int declared = vd.ArraySize is IntLiteral sz0 ? sz0.Value : 0;
                int elemWords = Math.Max(1, (SizeOfDeclaredType(vd.Type) + 3) / 4);
                var zeros = new List<object> { declared };
                for (int i = 0; i < declared * elemWords; i++) zeros.Add(0);
                dataSection[label] = zeros.ToArray();
                _globalArrays.Add(vd.Name);
            }
            else if (_classes.TryGetValue(CleanType(vd.Type), out var gcls))
            {
                // ── **全局对象**：`static Clock gclk;` 这类 ─────────────────────
                //
                // ⚠⚠ 原先它落到下面那个兜底 `dataSection[label] = 0` ⇒ **只占 1 个 word**，
                //   而类可能有几十个字段 ⇒ 对字段的每一次写都**越过自己那块**、
                //   踩到**紧挨着的下一个全局量**上。
                //
                //   实测（gorilla.cpp 的 `static ClockProbe gcp;`，3 个字段）：
                //     var_gcp:    .word 0      ← 只 1 格
                //     var_gHour:  .word 7      ← 被 gcp.phour 踩掉
                //     var_gMinute:.word 30     ← 被 gcp.pminute 踩掉
                //     var_gDayL:  .word 0      ← 被 gcp.pday 踩掉
                //   症状是"调用全局对象的方法之后，**别的**全局量莫名其妙变成 0"，
                //   而那个方法本身完全正确 —— 这与 OPEN #18 追了很多轮的表现一致
                //   （先前一直从"类 / 方法 / 字段"的角度找，其实**问题在分配**）。
                //
                //   口径与局部对象、成员对象**同源**：都用 `ClassSizeDeep`
                //   （含基类子对象、含 vptr）—— 本仓记过多次"分配与访问必须同源"。
                int gwords = Math.Max(1, ClassSizeDeep(gcls.Name) / 4);
                if (gwords == 1)
                {
                    // 只有一个 word 的对象（无字段的类）没必要开数组
                    dataSection[label] = ClassHasVirtualDeep(gcls.Name)
                        ? new object[] { new LabelRef($"{gcls.Name}_typeid") }
                        : (object)0;
                }
                else
                {
                    var cells = new object[gwords];
                    // ⚠ **vptr 直接写进数据段**（`LabelRef` 由链接器解析成 `{类}_typeid` 的地址）。
                    //   不能靠"顶层指令流"去写 —— 那一段根本不执行（本仓记过：
                    //   顶层生成的 `MOVE [var_x], R0` 落在任何函数体之外，永远跑不到）。
                    if (ClassHasVirtualDeep(gcls.Name))
                        cells[0] = new LabelRef($"{gcls.Name}_typeid");
                    for (int gi = 1; gi < gwords; gi++) cells[gi] = 0;
                    dataSection[label] = cells;
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

        /// <summary>
        /// ⚠ **本方法当前没有任何调用点（死代码）** —— 全仓 grep 只有注释提到它。
        ///
        /// 局部变量声明实际走的是 <c>CodeGenerator.Expressions.cs</c> 里
        /// 「<c>ae.Target is IdentExpr &amp;&amp; ae.ArraySize &gt; 0</c>」那一支（连标量也走那儿），
        /// 全局的走 <see cref="GenerateGlobalVar"/>（有调用点，见 <c>CodeGenerator.cs:141/144</c>）。
        ///
        /// 保留它是因为里面的类/模板/引用等分支可能还会被接回去；但**改数组初始化时别只改这里** ——
        /// 改在这儿不会影响任何产物，实测不出来也验证不了（本仓最忌讳的"说不清效果的改动"）。
        /// 真要修，改那两条活路径，并各自加判据。
        /// </summary>
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

                // ⚠ **初始值要逐个存进去**（布局与 `[]` 的 `+4` 对齐：头在 `firstWordOff`，
                //   元素从 `+4` 起、每格 4 字节）。
                //   ⚠⚠ 但**这段目前不生效**：本方法没有调用点（见方法头注释）。局部数组实际走
                //   `CodeGenerator.Expressions.cs` 的 `ArraySize > 0` 那一支 —— 真正的修复在那边。
                if (vd.Initializer is InitializerListExpr locInit)
                {
                    var flat = new List<object>();
                    FlattenInitList(locInit, flat);
                    for (int i = 0; i < flat.Count && i < arrSize.Value; i++)
                    {
                        // 值是 int 就给立即数，否则是 `FlattenInitList` 分配的**数据段标签**
                        //（字符串那种），直接 `MOVE` 标签地址即可。
                        Add(OpCode.MOVE, "R0", flat[i] is int iv ? $"#{iv}" : flat[i].ToString()!);
                        int off = firstWordOff + 4 + i * 4;
                        Add(OpCode.MOVE, Vars?.FormatOffset(off) ?? $"R14-{off}", "R0");
                    }
                }
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

            // ── `new T[n]`：**数组形式**，要分配「n 个元素」那么大 ──────────────
            // ⚠ 原先这条路上 `ne.Size` **被整个忽略**：`new int[8]` 只 `alloc(4)`，
            //   于是 `a[0..7]` 全写在分配块之外 —— **越界写别人的内存，且一声不响**。
            //   实测 `int* a = new int[8]; a[7] = 77;` 照样打出 77（堆恰好够大），
            //   这正是"看起来能跑"的假象：`alloc`（SYSCALL #40）只管给一块内存，
            //   给少了它也不知道；换个分配顺序、或者中间多 new 几次，
            //   踩到的就是别人的数据（症状会跑到很远的地方才现形）。
            //   ⚠ **不要再加 4 字节长度头** —— `[]` 的寻址侧（`ArrayIndexInfo`）
            //     对 `int*` 这种指针判的是 `IsPointerDeref` ⇒ **没有头**，
            //     按 C 一致；加了头反而整体偏 4 字节。
            if (ne.Size != null)
            {
                int elemSz = isClass ? ClassSizeDeep(ne.Type) : GetTypeLoadInfo(ne.Type).byteSize;
                if (elemSz <= 0) elemSz = 4;
                GenerateExpr(ne.Size);                        // R0 = n
                Add(OpCode.PUSH, "R0");                       // [R13+8] 循环计数
                Add(OpCode.MOVE, "R1", $"#{elemSz}");
                Add(OpCode.MUL, "R0", "R0", "R1");            // R0 = n × 元素字节数
                Add(OpCode.CALL, "alloc");                    // R0 = base
                Add(OpCode.PUSH, "R0");                       // [R13+4] base（返回值）
                Add(OpCode.PUSH, "R0");                       // [R13+0] 游标

                // 元素是类、且有能空参调用的构造函数 ⇒ **逐个构造**
                //（C++ 语义：`new A[n]` 是"n 个已构造的对象"，不是 n 块原始内存）
                var elemCtor = isClass ? FindCtor(clsInfo, 0) : null;
                if (elemCtor != null)
                {
                    string loopLbl = NewLabel(), doneLbl = NewLabel();
                    labels[loopLbl] = instructions.Count;
                    Add(OpCode.MOVE, "R1", "8(R13)");         // 剩余计数
                    Add(OpCode.CMP, "R1", "#0");
                    Add(OpCode.JE, doneLbl);
                    Add(OpCode.MOVE, "R0", "4(R13)");         // this = 游标
                    Add(OpCode.PUSH, "R0");                   // ← R13 下移一格……
                    EmitCtorCallOnPushedThis(ne.Type, elemCtor, null);
                    // ……它自己把那格弹掉（内部固定 `ADD R13, #(n+1)*4`），
                    // 所以下面几条读到的偏移又回到原始位置 —— 用之前先确认这一点。
                    Add(OpCode.MOVE, "R1", "4(R13)");
                    Add(OpCode.ADD, "R1", $"#{elemSz}");      // 游标 += 元素大小
                    Add(OpCode.MOVE, "4(R13)", "R1");
                    Add(OpCode.MOVE, "R1", "8(R13)");
                    Add(OpCode.SUB, "R1", "#1");              // 计数 -= 1
                    Add(OpCode.MOVE, "8(R13)", "R1");
                    Add(OpCode.JMP, loopLbl);
                    labels[doneLbl] = instructions.Count;
                }

                Add(OpCode.MOVE, "R0", "4(R13)");             // 返回值 = base
                Add(OpCode.ADD, "R13", "#12");                // 清掉那三个格子
                return;
            }

            // ⚠ 对象大小**必须问 `ClassSizeDeep`**，不能按"字段个数 × 4"自己算。
            //   那个算式漏掉三样东西，而且**漏了也不报错**（`alloc` 只管给一块内存，
            //   给少了就是越界写别人的内存）：数组字段（`int cell[9]` 是 36 字节不是 4）、
            //   内嵌对象（`Inner in` 是整个 Inner 那么大）、以及**基类子对象的字节数**。
            //   `ClassSizeDeep` 早就是"对象占多少字节"的唯一实现（F21 元素步长那条
            //   把访问侧收过来时用的就是它）—— 这里是**第二处**在自算，本该一并收掉。
            int objSize = isClass ? ClassSizeDeep(ne.Type) : 4;
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

            // ── `new A(实参)`：alloc 出来的新对象**也要跑构造函数** ──────────────
            // ⚠ 原先这条路只把实参当"标量初值"写进对象第一个字（下面那段 `Init[0]`），
            //   **构造函数一次都不调** —— `new A()` 的字段停在 `alloc` 给的原始内存上
            //   （内存是不是 0 由分配器决定，不是语言保证），而 `new A(1,2)` 更糟：
            //   它把 `1` 写进对象首字、`2` 直接丢掉。两条都是"编得过、值不对"。
            //   实参表交给 `FindCtor` 选构造函数（它已处理"默认参数能补齐"的情形）。
            if (isClass)
            {
                var ctor = FindCtor(clsInfo, ne.Init.Count);
                if (ctor != null)
                {
                    Add(OpCode.PUSH, "R0");      // this —— 同时也是 `new` 的返回值
                    EmitCtorCallOnPushedThis(ne.Type, ctor, ne.Init, resultIsThis: true);
                    return;
                }
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
                        string label = MethodSymbol(cd.Name, m.Method.Name, m.Method!.Parameters.Select(p => p.Type));
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
