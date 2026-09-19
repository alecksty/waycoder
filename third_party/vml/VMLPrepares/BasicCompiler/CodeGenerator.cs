using CompilerBase;
using VMLAssembler;
using VMLPlugins;

namespace BasicCompiler
{
    /// <summary>
    /// BASIC 数据类型枚举
    /// </summary>
    public enum BasicType
    {
        Integer,
        Single,
        Double,
        String,
        Byte,
        Boolean,
        Long,
        Custom
    }

    public partial class CodeGenerator : TypedCodeGen<BasicType>
    {
        /// <summary>
        /// 源文件里出现过 `OPTION EXPLICIT` ⇒ **变量必须先声明**，未声明的引用报**错误**；
        /// 没出现则报**警告**（QBasic 默认的「未声明即隐式全局」是合法语义）。
        ///
        /// 由 <c>BasicCompiler</c> 从 `<c>Parser.OptionExplicit</c>` 传进来 ——
        /// 这是**每文件**的属性，不能做成 `ImplicitDeclarationAllowed` 那种编译期常量。
        /// </summary>
        public bool StrictDeclarations { get; set; }

        // 字符串缓冲区（data section 分配，链接器解析地址，非固定地址）
        public const string StringBufferLabel = "__strbuf";

        // 动态分配的静态数据区 — 程序启动时通过 SYSCALL #40 分配
        // 基址存储在 0x6FD4，所有 StaticBase 引用改为 LOAD R1, [0x6FD4]; ADD R1, #offset
        // 布局: [0x0000:DATA] [0x3000:Palette] [0x3800:Palette13] [0x4000:Sound]
        public const int STATIC_DATA_OFFSET   = 0x0000; // DATA area (was StaticBase - 0x3000)
        public const int STATIC_FILE_OFFSET   = 0x1000; // File handles (was StaticBase - 0x1000)
        public const int STATIC_STRING_OFFSET = 0x2000; // String buffer
        public const int STATIC_PAL_OFFSET    = 0x3000; // EGA palette (was StaticBase - 0x100)
        public const int STATIC_PAL13_OFFSET  = 0x3800; // VGA 256 palette (was StaticBase + 0x800)
        public const int STATIC_SOUND_OFFSET  = 0x4000; // Sound buffer (was StaticBase + 0x2000)
        // 全局变量区：**追加在现有硬编码分区之后**（0x0000 DATA / 0x1000 文件句柄 / 0x3000 调色板
        // / 0x3800 VGA 调色板 / 0x4000 声音），不动它们，免得重新编号引入新错。
        public const int STATIC_GLOBALS_OFFSET = 0x5000;
        public const int STATIC_TOTAL_SIZE    = 0x7000; // 原 0x5000；尾部 8KB(≈2048 个 int) 给全局变量
        public const int STATIC_BASE_ADDR     = 0x6FD4; // where the dynamic base pointer is stored

        // Backward-compat property for code that still uses StaticBase directly
        // Returns the dynamic base address via runtime lookup (0x6FD4)
        public int StaticBase => STATIC_BASE_ADDR; // flag value: use dynamic base

        /// <summary>Emit code to load dynamic static base address into reg</summary>
        private void EmitStaticBase(int reg)
        {
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                new(OperandType.REGISTER, reg), new(OperandType.IMMEDIATE, STATIC_BASE_ADDR) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                new(OperandType.REGISTER, reg), new(OperandType.MEMORY, $"R{reg}") }));
        }

        /// <summary>Emit code to compute addr = dynamic_base + offset into reg</summary>
        private void EmitStaticAddr(int reg, int offset)
        {
            EmitStaticBase(reg);
            if (offset != 0)
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> {
                    new(OperandType.REGISTER, reg), new(OperandType.IMMEDIATE, offset) }));
        }

        // VGA 帧缓冲基址（可通过 SYSCALL 60/GetConfig 动态获取，默认 0xA0000）
        private int _vgaBase = 0xA0000;
        public int VgaBase { get => _vgaBase; set => _vgaBase = value; }

        /// <summary>当前 BASIC 方言 (v1.66.32+)</summary>
        private VMLPlugins.BasicDialect CurrentDialect =>
            VMLPlugins.CompilerOptionsContext.Current.BasicDialect;
        private bool IsDialect(VMLPlugins.BasicDialect d) => CurrentDialect == d;

        /// <summary>输出未实现特性警告 — 编译通过但无实际功能 (v1.66.32+)</summary>
        private void WarnUnimplemented(string feature)
        {
            instructions.Add(new Instruction(OpCode.LABEL,
                [new Operand(OperandType.IMMEDIATE, 0)]) { Label = $"; WARNING: {feature} — not implemented, no runtime effect" });
        }

        private BasicProgram program;
        private Dictionary<string, int> variables;
        private Dictionary<string, BasicType> variableTypes;
        private Dictionary<string, ArrayInfo> arrayVariables;
        private HashSet<string> _sharedVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>模块级变量（含 DIM SHARED）—— 放静态区的全局段，主程序与 SUB 共用同一份内存。</summary>
        private HashSet<string> _globalVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>获取变量的内存引用字符串。SHARED 变量使用绝对地址, 普通变量使用 BP+offset</summary>
        private string VarMemRef(string varName)
        {
            int offset = GetVarByteOffset(varName);
            if (_sharedVariables.Contains(varName))
                return $"{StaticBase + 8 + offset}";
            return $"R12+{8 + offset}";
        }

        /// <summary>
        /// 读变量到 <paramref name="reg"/>。**全局变量（模块级）走静态区的全局段**，用
        /// <see cref="EmitStaticAddr"/> 就地算出绝对地址再间接读 —— 于是主程序与 SUB 指向同一块内存；
        /// 其余（SUB 局部/参数）仍走 `R12+偏移` 的相对寻址。
        ///
        /// 为什么不"让某个寄存器长期存基址"：已确认 R7–R11 全被大量使用（33~123 处），
        /// **没有空闲寄存器**可以专用；所以就地在目标寄存器里算地址，只用一个寄存器。
        /// </summary>
        private void EmitLoadVar(int reg, string name)
        {
            if (_globalVars.Contains(name))
            {
                EmitStaticAddr(reg, STATIC_GLOBALS_OFFSET + GetVarByteOffset(name));
                // ⚠ 读也要**按类型**（MOVEF/MOVED/MOVEL）—— 与 EmitStoreVar 的
                //   `GetStoreInstruction(GetVariableType(name))` 对称。固定发 MOVE 的话，
                //   浮点/64 位全局变量会写进 F 寄存器组、却用整数寄存器读回来（值进不了目标组）。
                instructions.Add(new Instruction(GetLoadInstruction(GetVariableType(name)), new List<Operand>
                {
                    new Operand(OperandType.REGISTER, reg), new Operand(OperandType.INDIRECT, reg)
                }));
                return;
            }
            instructions.Add(new Instruction(GetLoadInstruction(GetVariableType(name)), new List<Operand>
            {
                new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, VarMemRef(name))
            }));
        }

        /// <summary>
        /// 把 <paramref name="srcReg"/> 写进变量。全局变量同样写进静态区的全局段。
        ///
        /// 地址临时用 **R2**：调用点（Let 语句）把值放在 R0/R1（浮点用 R0、整数用 R1），
        /// R2 在那一带本来就是拿来算地址的临时寄存器（见 `Statements.cs` 里 `MOVE R2,[R14+off]` 那段）。
        /// </summary>
        private void EmitStoreVar(string name, int srcReg)
        {
            if (_globalVars.Contains(name))
            {
                EmitStaticAddr(2, STATIC_GLOBALS_OFFSET + GetVarByteOffset(name));
                instructions.Add(new Instruction(GetStoreInstruction(GetVariableType(name)), new List<Operand>
                {
                    new Operand(OperandType.INDIRECT, 2), new Operand(OperandType.REGISTER, srcReg)
                }));
                return;
            }
            instructions.Add(new Instruction(GetStoreInstruction(GetVariableType(name)), new List<Operand>
            {
                new Operand(OperandType.MEMORY, VarMemRef(name)), new Operand(OperandType.REGISTER, srcReg)
            }));
        }

        /// <summary>
        /// 记录（TYPE）字段的地址 → <paramref name="reg"/>。全局记录走静态区全局段
        /// （基址 + 全局段偏移 + 字段偏移），与 <see cref="EmitLoadVar"/>/<see cref="EmitStoreVar"/> 同源；
        /// 其余（SUB 内以 STATIC 登记的名字）仍按 R12 相对。
        ///
        /// 为什么要有这个 helper：字段地址原先在**六处**各拼一遍 `MOVE reg,#8+索引*4+字段偏移; ADD reg,R12`
        /// —— ① 全局记录被写到主帧/子帧上，与已经改走全局段的记录整体读写**各写各的**
        /// （SUB 里 `p.Y = 42`、主程序 `PRINT p.Y` 读回 0）；② 用的是"索引×4"，而同一变量的整体
        /// 寻址走 `GetVarByteOffset`（Double/Long 算 8 字节）—— 同一件事两处算法，有 8 字节类型就漂。
        /// 现在两处算法只有一份。
        /// </summary>
        private void EmitRecordFieldAddr(int reg, string recordName, int fieldOffset)
        {
            if (_globalVars.Contains(recordName))
            {
                EmitStaticAddr(reg, STATIC_GLOBALS_OFFSET + GetVarByteOffset(recordName) + fieldOffset);
                return;
            }
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 8 + variables[recordName] * 4 + fieldOffset)
            }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
            {
                new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 12)
            }));
        }

        /// <summary>
        /// 取变量本身的**地址**（不是值）→ <paramref name="reg"/>。用于 BYREF 实参。
        /// 全局变量必须给静态区全局段的地址 —— 给 `R12+8+偏移`（主帧）等于把一个跟变量无关的
        /// 栈地址传进去，被调方按地址读写的是主帧，与变量的实际位置无关。
        /// </summary>
        private void EmitVarAddr(int reg, string name)
        {
            if (_globalVars.Contains(name))
            {
                EmitStaticAddr(reg, STATIC_GLOBALS_OFFSET + GetVarByteOffset(name));
                return;
            }
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 8 + GetVarByteOffset(name))
            }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
            {
                new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 12)
            }));
        }

        /// <summary>获取变量类型对应的字节偏移步长 — 委托到 TypedCodeGen.GetTypeInfo</summary>
        private int GetVarByteSize(BasicType t) => GetTypeInfo(t).byteSize;

        /// <summary>根据变量索引计算实际字节偏移 (考虑 Double/Long 的 8 字节宽度)</summary>
        private int GetVarByteOffset(string varName)
        {
            int idx = variables[varName];
            int offset = 0;
            // 按索引顺序累加每个变量的字节宽度
            foreach (var kv in variables)
            {
                if (kv.Value < idx)
                {
                    var t = GetVariableType(kv.Key);
                    offset += GetVarByteSize(t);
                }
            }
            return offset;
        }

        private int variableCount;
        private Dictionary<int, int> lineLabels;
        
        // SUB/FUNCTION support
        private List<SubDeclaration> subDeclarations;
        private List<FunctionDeclaration> funcDeclarations;
        private Dictionary<string, SubDeclaration> subMap;
        private Dictionary<string, FunctionDeclaration> funcMap;
        private string currentSubName;
        private string currentClassName; // Track which class's method we're generating (Phase 2)
        private HashSet<string> methodSubs = new HashSet<string>(); // SUB names synthesized from methods
        // GPIO 库是否已链接 (v1.66.32+), reserved for future use
#pragma warning disable CS0414
        private bool _gpioLinked = false;
#pragma warning restore CS0414
        private Dictionary<string, int> currentLocalVars;
        private int currentLocalVarCount;
        private int currentParamCount;
        
        // 数据值列表 (用于 DATA/READ/RESTORE)
        private List<int> dataValues = new List<int>();
        private List<bool> dataIsString = new List<bool>();   // true if DATA value is string
        private List<string> dataStringLabels = new List<string>(); // string labels for string DATA
        
        // TYPE 定义元数据
        private Dictionary<string, TypeDeclaration> typeDefinitions = new Dictionary<string, TypeDeclaration>();
        // CLASS 定义元数据 (Phase 2)
        private Dictionary<string, ClassDeclaration> classDefinitions = new Dictionary<string, ClassDeclaration>();
        // DIM AS 变量映射: varName -> typeName
        private Dictionary<string, string> dimAsVariables = new Dictionary<string, string>();

        // DEF type ranges: DEFSNG/DEFINT/DEFSTR 字母范围 → 目标类型
        private List<(char Start, char End, BasicType Type)> _defTypeRanges = new List<(char, char, BasicType)>();

        // BasicTypeToExpType → 使用 TypedCodeGen.GetExpType(t) (自动从 GetTypeInfo 4-tuple 推导)

        private class ArrayInfo
        {
            public int Size { get; set; }
            public int Offset { get; set; }
            public List<int> Dimensions { get; set; }
        }

        public CodeGenerator(BasicProgram program)
        {
            this.program = program;
            InitSimpleCompiler(newLabel: () => NewLabel(), placeLabel: lbl => { });
            Regs = new RegisterManager();
            variables = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            variableTypes = new Dictionary<string, BasicType>();
            arrayVariables = new Dictionary<string, ArrayInfo>();
            variableCount = 0;

            lineLabels = new Dictionary<int, int>();
            
            subDeclarations = new List<SubDeclaration>();
            funcDeclarations = new List<FunctionDeclaration>();
            subMap = new Dictionary<string, SubDeclaration>();
            funcMap = new Dictionary<string, FunctionDeclaration>();
            currentSubName = null;
            currentLocalVars = new Dictionary<string, int>();
            currentLocalVarCount = 0;
            currentParamCount = 0;
        }

        private int GetOrCreateVariable(string name)
        {
            if (!variables.ContainsKey(name))
            {
                // 模块级表里没有，且 SUB 的局部/形参表里也没有 ⇒ 这个名字**从未声明过**。
                //
                // 这是全前端**唯一**「没见过就造一个」的出口 —— 报错/警告的判据收在这一处，
                // 而不是散在四五个调用点（`Expressions.cs` 有一条是**无条件**调用的）。
                //
                // QBasic 的默认语义就是「未声明即隐式全局、值 0」⇒ **默认只警告**；
                // 写了 `OPTION EXPLICIT` 才升级成错误。此前无论写没写都静默建槽，那条指令形同虚设。
                if (currentLocalVars == null || !currentLocalVars.ContainsKey(name))
                {
                    if (StrictDeclarations)
                        ReportUndefined(name, ErrorCode.CodeGen_UndefinedVariable, "变量");
                    else
                        WarnUndefined(name, ErrorCode.CodeGen_UndefinedVariable, "变量",
                            "QBasic 默认「未声明即隐式全局」(值为 0)；"
                            + "要让这类引用直接报错，请在程序开头写 `OPTION EXPLICIT`。");
                }

                variables[name] = variableCount;
                // **模块级创建的变量就是全局变量** —— 放静态区全局段。SUB 的局部/参数在
                // currentLocalVars 里、本来就不进 variables，所以不受影响。
                if (currentSubName == null) _globalVars.Add(name);
                // Double 和 Long 类型需要 8 字节，分配 2 个槽位
                BasicType varType = GetVariableType(name);
                int slots = (varType == BasicType.Double || varType == BasicType.Long) ? 2 : 1;
                variableCount += slots;
            }
            return variables[name];
        }

        public override VmlProgram GenerateCode()
        {
            // Built-in QBasic constants
            constants["true"] = -1;
            constants["false"] = 0;
            // ENUM values from dialect (v1.66.32+)
            foreach (var ev in program.EnumValues)
                constants[ev.Key.ToLower()] = ev.Value;
            
            // Load array types from parser's first pass
            foreach (var kv in program.ArrayTypes)
            {
                dimAsVariables[kv.Key.ToLower()] = kv.Value.ToLower();
            }
            // 第一遍：收集变量和行号标签
            CollectVariablesAndLabels();

            // 第二遍：生成代码
            GenerateInstructions();

            // 创建标签映射（从 LABEL 指令扫描 + StatementManager 写入的标签合并）
            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].Opcode == OpCode.LABEL && instructions[i].Operands.Count > 0)
                {
                    string labelName = instructions[i].Operands[0].Value.ToString().ToLower();
                    labels[labelName] = i;
                }
            }

            // 创建数据段（字符串缓冲区由链接器解析，无固定地址）
            this.dataSection[StringBufferLabel] = 0;

            // 创建数据段（合并类字段中的字符串数据）
            foreach (var variable in variables)
            {
                if (!this.dataSection.ContainsKey(variable.Key))
                    this.dataSection[variable.Key] = 0;
            }

            // 创建 VML 程序
            return BuildProgram("main");
        }

        private void CollectVariablesAndLabels()
        {
            // 先处理 DEF type 语句 (DEFINT/DEFLNG/DEFDBL 等)，以便后续变量分配时知道正确类型
            foreach (var statement in program.Statements)
            {
                if (statement is DefTypeStatement defType)
                {
                    BasicType targetType = defType.TypeName switch
                    {
                        "DEFSNG" => BasicType.Single,
                        "DEFDBL" => BasicType.Double,
                        "DEFLNG" => BasicType.Long,
                        "DEFSTR" => BasicType.String,
                        "DEFINT" => BasicType.Integer,
                        _ => BasicType.Integer
                    };
                    foreach (var (start, end) in defType.Ranges)
                        _defTypeRanges.Add((start, end, targetType));
                }
            }

            // Collect SUB/FUNCTION/DEF FN declarations
            foreach (var statement in program.Statements)
            {
                if (statement is SubDeclaration subDecl)
                {
                    subDeclarations.Add(subDecl);
                    subMap[subDecl.Name.ToLower()] = subDecl;
                }
                else if (statement is FunctionDeclaration funcDecl)
                {
                    funcDeclarations.Add(funcDecl);
                    funcMap[funcDecl.Name.ToLower()] = funcDecl;
                }
                else if (statement is DefFnStatement defFn)
                {
                    // Convert DEF FN to FUNCTION internally
                    string defFnName = defFn.FullFnName;
                    var defFuncDecl = new FunctionDeclaration(defFn.Line, defFn.Column, defFnName);
                    foreach (var p in defFn.Parameters)
                    {
                        defFuncDecl.Parameters.Add(new ParameterNode(p.Line, p.Column, p.Name, false, false));
                    }
                    // Body: LET funcName = expression
                    var defLetStmt = new LetStatement(defFn.Line, defFn.Column);
                    defLetStmt.Variable = new Identifier(defFn.Line, defFn.Column, defFnName);
                    defLetStmt.Expression = defFn.BodyExpression;
                    defFuncDecl.Body.Add(defLetStmt);
                    defFuncDecl.IsStringFunction = false;

                    funcDeclarations.Add(defFuncDecl);
                    funcMap[defFnName.ToLower()] = defFuncDecl;
                }
            }
            
            // Collect TYPE definitions (including those inside SUB/FUNCTION bodies)
            foreach (var statement in program.Statements)
            {
                CollectTypeDeclarations(statement);
            }

            // Phase 2: 将 CLASS 方法合成为 SUB 声明
            foreach (var kv in classDefinitions)
            {
                var classDecl = kv.Value;
                string clsName = classDecl.Name;
                foreach (var method in classDecl.Methods)
                {
                    string methodName = $"{clsName}_{method.Name}";
                    var subDecl = new SubDeclaration(method.Line, method.Column, methodName);
                    // THIS 指针作为第一个隐式参数
                    subDecl.Parameters.Add(new ParameterNode(method.Line, method.Column, "this", false, false));
                    // 显式参数
                    foreach (var pName in method.Parameters)
                        subDecl.Parameters.Add(new ParameterNode(method.Line, method.Column, pName, false, false));
                    subDecl.Body = method.Body;
                    subDeclarations.Add(subDecl);
                    subMap[methodName.ToLower()] = subDecl;
                    methodSubs.Add(methodName.ToLower());
                }
                // Constructor → SubDeclaration
                if (classDecl.ConstructorBody != null && classDecl.ConstructorBody.Count > 0)
                {
                    string ctorName = $"{clsName}_constructor";
                    var ctorSub = new SubDeclaration(classDecl.Line, classDecl.Column, ctorName);
                    ctorSub.Parameters.Add(new ParameterNode(classDecl.Line, classDecl.Column, "this", false, false));
                    ctorSub.Body = classDecl.ConstructorBody;
                    subDeclarations.Add(ctorSub);
                    subMap[ctorName.ToLower()] = ctorSub;
                    methodSubs.Add(ctorName.ToLower());
                }
                // Destructor → SubDeclaration (v1.66.32+)
                if (classDecl.DestructorBody != null && classDecl.DestructorBody.Count > 0)
                {
                    string dtorName = $"{clsName}_destructor";
                    var dtorSub = new SubDeclaration(classDecl.Line, classDecl.Column, dtorName);
                    dtorSub.Parameters.Add(new ParameterNode(classDecl.Line, classDecl.Column, "this", false, false));
                    dtorSub.Body = classDecl.DestructorBody;
                    subDeclarations.Add(dtorSub);
                    subMap[dtorName.ToLower()] = dtorSub;
                }
            }

            // Collect global variables
            foreach (var statement in program.Statements)
            {
                if (statement is SubDeclaration || statement is FunctionDeclaration)
                    continue;
                CollectVariablesFromStatement(statement);
            }
        }

        private void CollectTypeDeclarations(Statement statement)
        {
            if (statement is TypeDeclaration typeDecl)
            {
                typeDefinitions[typeDecl.Name.ToLower()] = typeDecl;
                int offset = 0;
                foreach (var field in typeDecl.Fields)
                {
                    field.Offset = offset;
                    switch (field.FieldType.ToUpper())
                    {
                        case "INTEGER": offset += 4; break;
                        case "SINGLE": offset += 4; break;
                        case "STRING": offset += (field.StringLength > 0 ? field.StringLength : 32); break;
                        default: offset += 4; break;
                    }
                }
                typeDecl.TotalSize = offset;
            }
            else if (statement is ClassDeclaration classDecl)
            {
                // CLASS → 注册为 TYPE (字段) + 存储方法列表 (v1.66.31+ Phase 2)
                var clsTypeDecl = new TypeDeclaration(classDecl.Line, classDecl.Column)
                    { Name = classDecl.Name, Fields = classDecl.Fields };
                int coff = 0;
                foreach (var f in classDecl.Fields)
                {
                    f.Offset = coff;
                    coff += f.FieldType.ToUpper() switch { "INTEGER" => 4, "SINGLE" => 4, _ => 4 };
                }
                clsTypeDecl.TotalSize = coff;
                typeDefinitions[classDecl.Name.ToLower()] = clsTypeDecl;
                classDefinitions[classDecl.Name.ToLower()] = classDecl;
            }
            else if (statement is SubDeclaration subDecl)
            {
                foreach (var bodyStmt in subDecl.Body)
                    CollectTypeDeclarations(bodyStmt);
            }
            else if (statement is FunctionDeclaration funcDecl)
            {
                foreach (var bodyStmt in funcDecl.Body)
                    CollectTypeDeclarations(bodyStmt);
            }
        }

        private void CollectVariablesFromStatement(Statement statement)
        {
            if (statement == null) return;
            if (statement is SequenceStatement seq)
            {
                foreach (var s in seq.Statements)
                    CollectVariablesFromStatement(s);
                return;
            }
            if (statement is LetStatement letStmt)
            {
                // 收集赋值目标中的变量
                if (letStmt.Variable is Identifier ident)
                {
                    if (!variables.ContainsKey(ident.Name))
                    {
                        GetOrCreateVariable(ident.Name);
                    }
                }
                else if (letStmt.Variable is ArrayAccessExpression arrayAccess)
                {
                    // 数组访问中的索引表达式可能包含变量
                    CollectVariablesFromExpression(arrayAccess.Index);
                }
                
                // 收集表达式中的变量
                CollectVariablesFromExpression(letStmt.Expression);
            }
            else if (statement is PrintStatement printStmt)
            {
                foreach (var expr in printStmt.Expressions)
                {
                    CollectVariablesFromExpression(expr);
                }
            }
            else if (statement is InputStatement inputStmt)
            {
                foreach (var var in inputStmt.Variables)
                {
                    if (!variables.ContainsKey(var.Name))
                    {
                        GetOrCreateVariable(var.Name);
                    }
                }
            }
            else if (statement is DimStatement dimStmt)
            {
                if (dimStmt.Dimensions.Count == 0 && dimStmt.Size == 1)
                {
                    // 简单变量声明: DIM SHARED varname / DIM varname
                    string varKey = dimStmt.VariableName;
                    if (!variables.ContainsKey(varKey))
                    {
                        GetOrCreateVariable(varKey);
                    }
                    if (dimStmt.IsShared)
                    {
                        _sharedVariables.Add(varKey);
                    }
                }
                else
                {
                    // 数组变量信息
                    if (!arrayVariables.ContainsKey(dimStmt.VariableName))
                    {
                        arrayVariables[dimStmt.VariableName] = new ArrayInfo
                        {
                            Size = dimStmt.Size,
                            Offset = variableCount,
                            Dimensions = dimStmt.Dimensions.Count > 0 ? new List<int>(dimStmt.Dimensions) : new List<int> { dimStmt.Size }
                        };

                        // 为数组元素分配变量槽位
                        for (int i = 0; i < dimStmt.Size; i++)
                        {
                            string elementName = $"{dimStmt.VariableName}({i})";
                            if (!variables.ContainsKey(elementName))
                            {
                                GetOrCreateVariable(elementName);
                            }
                        }
                    }
                }
                // If the array has a type (DIM arr(size) AS TypeName), register it for field access
                if (!string.IsNullOrEmpty(dimStmt.TypeName))
                {
                    dimAsVariables[dimStmt.VariableName.ToLower()] = dimStmt.TypeName.ToLower();
                }
            }
            else if (statement is ForStatement forStmt)
            {
                if (!variables.ContainsKey(forStmt.Variable.Name))
                {
                    GetOrCreateVariable(forStmt.Variable.Name);
                }
                CollectVariablesFromExpression(forStmt.InitialValue);
                CollectVariablesFromExpression(forStmt.EndValue);
                if (forStmt.StepValue != null)
                {
                    CollectVariablesFromExpression(forStmt.StepValue);
                }
                foreach (var bodyStmt in forStmt.Body)
                {
                    CollectVariablesFromStatement(bodyStmt);
                }
            }
            else if (statement is WhileStatement whileStmt)
            {
                CollectVariablesFromExpression(whileStmt.Condition);
                foreach (var bodyStmt in whileStmt.Body)
                {
                    CollectVariablesFromStatement(bodyStmt);
                }
            }
            else if (statement is CallStatement callStmt)
            {
                foreach (var arg in callStmt.Arguments)
                    CollectVariablesFromExpression(arg);
            }
            else if (statement is DoLoopStatement doLoopStmt)
            {
                foreach (var bodyStmt in doLoopStmt.Body)
                {
                    CollectVariablesFromStatement(bodyStmt);
                }
                if (doLoopStmt.HasCondition && doLoopStmt.Condition != null)
                {
                    CollectVariablesFromExpression(doLoopStmt.Condition);
                }
            }
            else if (statement is DataStatement dataStmt)
            {
                // Collect data values at compile time
                foreach (var val in dataStmt.Values)
                {
                    if (val is NumberLiteral num)
                    {
                        dataValues.Add((int)num.Value);
                        dataIsString.Add(false);
                    }
                    else if (val is StringLiteral str)
                    {
                        string lbl = $"_data_str_{dataStringLabels.Count}";
                        dataSection[lbl] = new DataString(str.Value);
                        dataValues.Add(0);
                        dataIsString.Add(true);
                        dataStringLabels.Add(lbl);
                    }
                    else
                    {
                        dataValues.Add(0);
                        dataIsString.Add(false);
                    }
                }
            }
            else if (statement is ReadStatement readStmt)
            {
                // Collect variables used in READ
                foreach (var varIdent in readStmt.Variables)
                {
                    if (!variables.ContainsKey(varIdent.Name))
                    {
                        GetOrCreateVariable(varIdent.Name);
                    }
                }
            }
            else if (statement is ConstStatement constStmt)
            {
                // Store constant value at compile time
                if (constStmt.Value is NumberLiteral num)
                {
                    constants[constStmt.Name.ToLower()] = (int)num.Value;
                }
                else if (constStmt.Value is StringLiteral str)
                {
                    constants[constStmt.Name.ToLower()] = str.Value;
                }
            }
            else if (statement is IfStatement ifStmt)
            {
                CollectVariablesFromExpression(ifStmt.Condition);
                if (ifStmt.ThenBranch != null)
                {
                    CollectVariablesFromStatement(ifStmt.ThenBranch);
                }
                if (ifStmt.ElseBranch != null)
                {
                    CollectVariablesFromStatement(ifStmt.ElseBranch);
                }
            }
            else if (statement is DrawStatement drawStmt)
            {
                CollectVariablesFromExpression(drawStmt.DrawString);
            }
            else if (statement is PrintUsingStatement usingStmt)
            {
                CollectVariablesFromExpression(usingStmt.Format);
                foreach (var v in usingStmt.Values)
                    CollectVariablesFromExpression(v);
            }
            else if (statement is DimAsStatement dimAsStmt)
            {
                string varName = dimAsStmt.VariableName.ToLower();
                if (!variables.ContainsKey(varName))
                {
                    GetOrCreateVariable(varName);
                }
                dimAsVariables[varName] = dimAsStmt.TypeName.ToLower();
                // Allocate slots based on type size
                if (typeDefinitions.ContainsKey(dimAsStmt.TypeName.ToLower()))
                {
                    int slots = (typeDefinitions[dimAsStmt.TypeName.ToLower()].TotalSize + 3) / 4;
                    for (int i = 0; i < slots - 1; i++)
                    {
                        string slotName = $"{varName}_slot_{i + 1}";
                        if (!variables.ContainsKey(slotName))
                            GetOrCreateVariable(slotName);
                    }
                }
            }
            else if (statement is PlayStatement playStmt)
            {
                CollectVariablesFromExpression(playStmt.CommandString);
            }
            else if (statement is SoundStatement soundStmt)
            {
                CollectVariablesFromExpression(soundStmt.Frequency);
                CollectVariablesFromExpression(soundStmt.Duration);
            }
            else if (statement is RedimStatement redimStmt)
            {
                CollectVariablesFromExpression(redimStmt.NewSize);
            }
            else if (statement is DefFnStatement defFnStmt)
            {
                // 参数变量不需要提前收集, 它们是函数内的局部变量
                CollectVariablesFromExpression(defFnStmt.BodyExpression);
            }
            else if (statement is GetStatement getStmt)
            {
                // GET uses array - collect array variable
                if (!string.IsNullOrEmpty(getStmt.ArrayName))
                {
                    string arrName = getStmt.ArrayName.ToLower();
                    if (!arrayVariables.ContainsKey(arrName) && !variables.ContainsKey(arrName))
                    {
                        // Add as a scalar variable reference (the array will be separately declared)
                        GetOrCreateVariable(arrName);
                    }
                }
                if (getStmt.X1 != null)
                    CollectVariablesFromExpression(getStmt.X1);
                if (getStmt.Y1 != null)
                    CollectVariablesFromExpression(getStmt.Y1);
                if (getStmt.X2 != null)
                    CollectVariablesFromExpression(getStmt.X2);
                if (getStmt.Y2 != null)
                    CollectVariablesFromExpression(getStmt.Y2);
            }
            else if (statement is PutStatement putStmt)
            {
                // PUT uses array
                string arrName = putStmt.ArrayName.ToLower();
                if (!arrayVariables.ContainsKey(arrName) && !variables.ContainsKey(arrName))
                {
                    GetOrCreateVariable(arrName);
                }
                CollectVariablesFromExpression(putStmt.X);
                CollectVariablesFromExpression(putStmt.Y);
            }
            else if (statement is PaletteStatement palStmt)
            {
                CollectVariablesFromExpression(palStmt.ColorIndex);
                CollectVariablesFromExpression(palStmt.Red);
                CollectVariablesFromExpression(palStmt.Green);
                CollectVariablesFromExpression(palStmt.Blue);
            }
            // 行标签: LabelName: body — 递归收集 body 中的变量
            else if (statement is LabelStatement labelStmt)
            {
                if (labelStmt.Body != null)
                    CollectVariablesFromStatement(labelStmt.Body);
            }
            // QBasic LINE: (x1,y1)-(x2,y2), color
            else if (statement is QbLineStatement qbLine)
            {
                CollectVariablesFromExpression(qbLine.X1);
                CollectVariablesFromExpression(qbLine.Y1);
                CollectVariablesFromExpression(qbLine.X2);
                CollectVariablesFromExpression(qbLine.Y2);
                if (qbLine.Color != null)
                    CollectVariablesFromExpression(qbLine.Color);
            }
            // QBasic CIRCLE: (x,y), radius, color
            else if (statement is QbCircleStatement qbCircle)
            {
                CollectVariablesFromExpression(qbCircle.X);
                CollectVariablesFromExpression(qbCircle.Y);
                CollectVariablesFromExpression(qbCircle.Radius);
                if (qbCircle.Color != null)
                    CollectVariablesFromExpression(qbCircle.Color);
            }
            // QBasic PAINT: (x,y), color [,border]
            else if (statement is QbPaintStatement qbPaint)
            {
                CollectVariablesFromExpression(qbPaint.X);
                CollectVariablesFromExpression(qbPaint.Y);
                CollectVariablesFromExpression(qbPaint.Color);
                if (qbPaint.Border != null)
                    CollectVariablesFromExpression(qbPaint.Border);
            }
            // PSET (x,y), color
            else if (statement is PsetStatement pset)
            {
                CollectVariablesFromExpression(pset.X);
                CollectVariablesFromExpression(pset.Y);
                if (pset.Color != null)
                    CollectVariablesFromExpression(pset.Color);
            }
            // SCREEN mode
            else if (statement is ScreenStatement screen)
            {
                if (screen.Mode != null)
                    CollectVariablesFromExpression(screen.Mode);
            }
            // LOCATE row, col
            else if (statement is LocateStatement locate)
            {
                if (locate.Row != null)
                    CollectVariablesFromExpression(locate.Row);
                if (locate.Col != null)
                    CollectVariablesFromExpression(locate.Col);
            }
            // COLOR fg [,bg]
            else if (statement is QbColorStatement colorStmt)
            {
                CollectVariablesFromExpression(colorStmt.Foreground);
                if (colorStmt.Background != null)
                    CollectVariablesFromExpression(colorStmt.Background);
            }
            // WIDTH cols, rows
            else if (statement is QbWidthStatement widthStmt)
            {
                if (widthStmt.Cols != null)
                    CollectVariablesFromExpression(widthStmt.Cols);
                if (widthStmt.Rows != null)
                    CollectVariablesFromExpression(widthStmt.Rows);
            }
            // RANDOMIZE [seed]
            else if (statement is RandomizeStatement randomStmt)
            {
                if (randomStmt.Seed != null)
                    CollectVariablesFromExpression(randomStmt.Seed);
            }
            // Turbo Basic statements -- collect variables from declarations
            else if (statement is LocalDeclaration localDecl)
            {
                foreach (var varIdent in localDecl.Variables)
                {
                    if (!variables.ContainsKey(varIdent.Name))
                    {
                        GetOrCreateVariable(varIdent.Name);
                    }
                }
            }
            else if (statement is StaticDeclaration staticDecl)
            {
                foreach (var varIdent in staticDecl.Variables)
                {
                    if (!variables.ContainsKey(varIdent.Name))
                    {
                        GetOrCreateVariable(varIdent.Name);
                    }
                }
            }
            else if (statement is SharedStatement sharedStmt)
            {
                foreach (var varIdent in sharedStmt.Variables)
                {
                    if (!variables.ContainsKey(varIdent.Name))
                    {
                        GetOrCreateVariable(varIdent.Name);
                    }
                }
            }
            else if (statement is CommonStatement commonStmt)
            {
                foreach (var varName in commonStmt.VariableNames)
                {
                    if (!variables.ContainsKey(varName))
                    {
                        GetOrCreateVariable(varName);
                    }
                }
            }
            else if (statement is OptionBaseStatement)
            {
                // OPTION BASE is just a flag, no variable allocation needed
            }
            else if (statement is SelectCaseStatement selectCase)
            {
                // Collect variables from SELECT CASE expression and each case branch
                CollectVariablesFromExpression(selectCase.TestExpression);
                foreach (var caseBlock in selectCase.CaseBlocks)
                {
                    foreach (var cond in caseBlock.Conditions)
                    {
                        if (cond.Value != null)
                            CollectVariablesFromExpression(cond.Value);
                        if (cond.FromValue != null)
                            CollectVariablesFromExpression(cond.FromValue);
                        if (cond.ToValue != null)
                            CollectVariablesFromExpression(cond.ToValue);
                        if (cond.CompareValue != null)
                            CollectVariablesFromExpression(cond.CompareValue);
                    }
                    foreach (var bodyStmt in caseBlock.Body)
                        CollectVariablesFromStatement(bodyStmt);
                }
                foreach (var elseStmt in selectCase.ElseBlock)
                    CollectVariablesFromStatement(elseStmt);
            }
        }

        private void CollectVariablesFromExpression(Expression expr)
        {
            if (expr == null) return;
            if (expr is Identifier ident)
            {
                // Skip compile-time constants
                if (constants.ContainsKey(ident.Name.ToLower()))
                    return;
                if (!variables.ContainsKey(ident.Name))
                {
                    GetOrCreateVariable(ident.Name);
                }
            }
            else if (expr is BinaryExpression binary)
            {
                CollectVariablesFromExpression(binary.Left);
                CollectVariablesFromExpression(binary.Right);
            }
            else if (expr is UnaryExpression unary)
            {
                CollectVariablesFromExpression(unary.Expression);
            }
            else if (expr is ArrayAccessExpression arrayAccess)
            {
                CollectVariablesFromExpression(arrayAccess.Index);
            }
            else if (expr is FunctionCallExpression funcCall)
            {
                foreach (var arg in funcCall.Arguments)
                    CollectVariablesFromExpression(arg);
            }
            else if (expr is FieldAccessExpression fieldAccess)
            {
                string recName = fieldAccess.RecordName;
                if (string.IsNullOrEmpty(recName) && fieldAccess.RecordExpression != null)
                {
                    // Derive record name from expression (e.g., array access)
                    if (fieldAccess.RecordExpression is Identifier recId)
                        recName = recId.Name;
                    else if (fieldAccess.RecordExpression is ArrayAccessExpression arrExpr)
                        recName = arrExpr.ArrayName;
                    // Also collect variables from the record expression recursively
                    CollectVariablesFromExpression(fieldAccess.RecordExpression);
                }
                if (!string.IsNullOrEmpty(recName))
                {
                    string recKey = recName.ToLower();
                    if (!variables.ContainsKey(recKey))
                    {
                        GetOrCreateVariable(recKey);
                    }
                }
            }
            else if (expr is FnCallExpression fnCall)
            {
                foreach (var arg in fnCall.Arguments)
                    CollectVariablesFromExpression(arg);
            }
        }

        private void GenerateInstructions()
        {
            Regs.Reset();
            // 生成 main 函数
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, "main") }));

            // 函数序言
            EmitPrologue();
            // 为临时变量/溢出槽预分配栈空间 (R12-4 到 R12-256)
            // 避免与 CALL/GOSUB 压入的返回地址碰撞
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 256) }));

            // 初始化寄存器
            AddRI(OpCode.MOVE, 0, 0);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, 0) }));

            // 生成变量初始化
            foreach (var variable in variables)
            {
                AddRI(OpCode.MOVE, 0, 0);
                BasicType varType = GetVariableType(variable.Key);
                OpCode storeOp = GetStoreInstruction(varType);
                instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12+{8 + variable.Value * 4}"), new Operand(OperandType.REGISTER, 0) }));
            }

            // 动态分配静态数据区 (替代固定地址 StaticBase)
            AddRI(OpCode.MOVE, 0, STATIC_TOTAL_SIZE);
            EmitAlloc();
            EmitStaticBase(1);

            // 把**全局变量区清 0** —— BASIC 语义里"未初始化的变量是 0"，
            // 而这块是 EmitAlloc 出来的裸内存（不保证是零）。清的范围只有全局段，
            // DATA 区等由各自的初始化逻辑负责，不碰。
            {
                string zeroLoop = GenerateLabel();
                string zeroDone = GenerateLabel();
                // R1 = 静态基址 + STATIC_GLOBALS_OFFSET —— 用现成的 EmitStaticAddr。
                // ⚠ 此前这两行是 `MOVE R1,#0x5000` + `ADD R1,[R1]`：上一行刚被 EmitStaticBase 算进 R1 的
                //   基址被立刻冲掉，`[R1]` 读的是**它自己要清的那块内存** ⇒ R1 = 0x5000 + mem[0x5000]。
                //   后果两层：① 全局区（基址+0x5000）一个字节都没清，"未初始化的全局变量是 0" 不成立；
                //   ② 8KB 被清到 0x5000+该值 的随机绝对地址上，还会把基址槽 0x6FD4 一起抹掉。
                EmitStaticAddr(1, STATIC_GLOBALS_OFFSET);
                AddRI(OpCode.MOVE, 2, STATIC_TOTAL_SIZE - STATIC_GLOBALS_OFFSET);   // R2 = 剩余字节数
                instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, zeroLoop) }));
                AddRI(OpCode.CMP, 2, 0);
                instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, zeroDone) }));
                AddRI(OpCode.MOVE, 0, 0);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.INDIRECT, 1), new Operand(OperandType.REGISTER, 0) }));
                AddRI(OpCode.ADD, 1, 4);
                AddRI(OpCode.SUB, 2, 4);
                instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, zeroLoop) }));
                instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, zeroDone) }));
            }
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));

            // 初始化 DATA 指针 (offset 0 in static area)
            AddRI(OpCode.MOVE, 0, 0);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FD0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));

            // 初始化 DATA 值到动态分配的静态区
            int strPtr = 0;
            for (int i = 0; i < dataValues.Count; i++)
            {
                if (i < dataIsString.Count && dataIsString[i] && strPtr < dataStringLabels.Count)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, dataStringLabels[strPtr++]) }));
                }
                else
                {
                    AddRI(OpCode.MOVE, 0, dataValues[i]);
                }
                EmitStaticAddr(1, STATIC_DATA_OFFSET + i * 4);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            }

            // 生成语句
            if (VMLPlugins.CompilerOptionsContext.Current.DebugMode)
                System.Console.Error.WriteLine($"program.Statements.Count={program.Statements.Count}");
            foreach (var statement in program.Statements)
            {
                if (statement == null) { continue; }
                if (VMLPlugins.CompilerOptionsContext.Current.DebugMode)
                    System.Console.Error.WriteLine($"Processing: {statement.GetType().Name}, Line={statement.Line}");
                CurrentSourceLine = statement.Line; CurrentSourceColumn = statement.Column;
                if (statement is SubDeclaration || statement is FunctionDeclaration
                    || statement is TypeDeclaration || statement is ClassDeclaration)
                {
                    continue; // Skip sub/function/type/class declarations in main program
                }
                
                // 为行号生成标签（GOTO/GOSUB目标）
                // 老式行号 BASIC 用 BasicLineNumber (如 "10 PRINT" 的 10)；无行号时回退到物理行号
                int labelLine = statement.BasicLineNumber > 0 ? statement.BasicLineNumber : statement.Line;
                if (labelLine > 0)
                {
                    instructions.Add(new Instruction(OpCode.LABEL,
                        new List<Operand> { new Operand(OperandType.LABEL, $"line_{labelLine}") },
                        instructions.Count));
                }
                // 为命名标签生成小写标签（GOSUB目标：ShowTitle: → label "showtitle"）
                if (statement is LabelStatement labelSt)
                {
                    instructions.Add(new Instruction(OpCode.LABEL,
                        new List<Operand> { new Operand(OperandType.LABEL, labelSt.Name.ToLower()) },
                        instructions.Count));
                }
                
                GenerateStatement(statement);
            }

            // 程序结束 - 加载第一个全局变量到 R0 作为退出码，然后退出
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R12+8") }));
            EmitExit();

            // Generate SUB procedures
            foreach (var subDecl in subDeclarations)
            {
                // Phase 2: 设置方法上下文，使字段访问通过 THIS 指针
                if (methodSubs.Contains(subDecl.Name.ToLower()))
                {
                    // 从方法名中提取类名: "ClassName_methodName" → "ClassName"
                    int lastUnderscore = subDecl.Name.LastIndexOf('_');
                    if (lastUnderscore > 0)
                    {
                        // 构造函数: "ClassName_constructor" → extract "ClassName"
                        string potentialClass = subDecl.Name.Substring(0, lastUnderscore);
                        if (classDefinitions.ContainsKey(potentialClass.ToLower()))
                            currentClassName = potentialClass;
                    }
                }
                GenerateSubDeclaration(subDecl);
                currentClassName = null;
            }

            // Generate FUNCTIONs
            foreach (var funcDecl in funcDeclarations)
            {
                GenerateFunctionDeclaration(funcDecl);
            }

            // vga_text_putchar 无操作存根（链接共享库时会被覆盖）
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, "vga_text_putchar") }));
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>()));
        }

    }
}
