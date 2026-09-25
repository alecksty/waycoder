using VMLAssembler;
using CompilerBase;

namespace CppCompiler
{
    public partial class CodeGenerator
    {
        private ExpType InferExpType(Expr e)
        {
            if (e is FloatLiteral fl) return fl.IsFloatSuffix ? ExpType.F32 : ExpType.F64;
            if (e is CastExpr ce)
            {
                var (_, isFloat, isDouble) = GetTypeLoadInfo(ce.TargetType);
                if (isDouble) return ExpType.F64;
                if (isFloat) return ExpType.F32;
                return ExpType.I32;
            }
            if (e is IdentExpr ie && _varTypes.TryGetValue(ie.Name, out var vt))
            {
                if (vt.Contains("*")) return ExpType.Ptr32;
                if (vt == "float") return ExpType.F32;
                if (vt == "double") return ExpType.F64;
            }
            return ExpType.I32;
        }

        /// <summary>获取表达式的完整类型信息 (用于类型转换)</summary>
        private (int byteSize, bool isFloat, bool isDouble, bool isLong) GetExprTypeInfo(Expr e)
        {
            if (e is FloatLiteral fl) return fl.IsFloatSuffix ? (4, true, false, false) : (8, false, true, false);
            if (e is IdentExpr ie && _varTypes.TryGetValue(ie.Name, out var vt))
            {
                var (byteSize, isFloat, isDouble) = GetTypeLoadInfo(vt);
                // C++ long long 使用 8 字节但不用 VML Long 路径 (用 double 路径 MOVED)
                return (byteSize, isFloat, isDouble, false);
            }
            return (4, false, false, false); // 默认为 int
        }

        /// <summary>获取 [] 操作的元素字节大小 (char*→1, short*→2, default→4)</summary>
        private int GetElementSize(Expr e)
        {
            if (e is IdentExpr ie)
            {
                // ⚠ 字节数组（`char x[]`）要排在指针之前判 —— 它的 `_varTypes` 里是 `char`
                //   （不带 `*`），走下面那条会拿到默认的 4（见 `_byteArrays` 的注释）。
                if (_byteArrays.Contains(ie.Name)) return 1;
                if (_varTypes.TryGetValue(ie.Name, out var vt0) && vt0.Contains("*"))
                    return GetPointerStepSize(vt0);
            }
            return 4; // 默认 int / 数组元素
        }
        private static int ElemShift(int size) => size switch { 1 => 0, 2 => 1, _ => 2 };
        /// <summary>表达式是否是简单指针解引用 (char*/int*/float*等, 非VML数组, 无需+4 header)</summary>
        private bool IsPointerDeref(Expr e) => e is IdentExpr ie && _varTypes.TryGetValue(ie.Name, out var vt)
            && vt.Contains("*") && !vt.Contains("[") && !vt.Contains("(");

        /// <summary>
        /// `[]` 寻址要用的**两个参数**：元素字节数、有没有那个 4 字节长度头。
        ///
        /// 抽成一个方法的理由：这件事在**四处**都要用（读 `a[i]`、写 `a[i]=v`、
        /// 取址 `&a[i]`、成员基址 `a[i].f`），各写一份必然漂移 —— 本仓头号坑。
        /// 四处的判据必须同源，否则会出现"读对了、写错了"这种最难查的分叉。
        ///
        /// 字节数组（`char x[]`）是**唯一**没有长度头的一类：它就是一段 C 字符串
        /// （见 `CodeGenerator._byteArrays`）。
        /// </summary>
        private (int ElemSize, bool HasHeader) ArrayIndexInfo(Expr baseExpr, bool isNested)
        {
            if (!isNested && baseExpr is IdentExpr bid && _byteArrays.Contains(bid.Name))
                return (1, false);
            return (GetElementSize(baseExpr), !IsPointerDeref(baseExpr) || isNested);
        }

        /// <summary>按元素宽度选**读取**指令（1 字节 → `MOVEB`、2 字节 → `MOVEH`、其余 → `MOVE`）。</summary>
        private static OpCode LoadOpFor(int elemSize)
            => elemSize == 1 ? OpCode.MOVEB : elemSize == 2 ? OpCode.MOVEH : OpCode.MOVE;

        /// <summary>按元素宽度选**存储**指令。见 <see cref="LoadOpFor"/>。</summary>
        private static OpCode StoreOpFor(int elemSize)
            => elemSize == 1 ? OpCode.MOVEB : elemSize == 2 ? OpCode.MOVEH : OpCode.MOVE;

        /// <summary>获取指针步长: 根据变量类型字符串返回 sizeof(*ptr)</summary>
        private int GetPointerStepSize(string? varTypeStr)
        {
            if (string.IsNullOrEmpty(varTypeStr) || !varTypeStr.Contains("*"))
                return 1;
            var (byteSize, _, _) = GetTypeLoadInfo(varTypeStr);
            return byteSize > 0 ? byteSize : 4;
        }

        /// <summary>
        /// Get (byteSize, isFloat, isDouble) for a pointed-to type string.
        /// E.g., "char*" → (1,false,false), "short*" → (2,false,false), "float*" → (4,true,false), "double*" → (8,false,true).
        /// Returns (4,false,false) for unknown types.
        /// </summary>
        private static (int byteSize, bool isFloat, bool isDouble) GetTypeLoadInfo(string? typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return (4, false, false);
            // Strip pointer suffix: "char*" → "char"
            string baseType = typeName.Replace("*", "").Trim().ToLower();
            return baseType switch
            {
                "char" or "signed char" or "unsigned char" or "bool" or "_bool" => (1, false, false),
                "short" or "signed short" or "unsigned short" or "short int" => (2, false, false),
                "float" => (4, true, false),
                "double" => (8, false, true),
                "int" or "signed int" or "unsigned int" or "long" or "signed long" or "unsigned long" or "int8_t" or "uint8_t" or "int16_t" or "uint16_t" or "int32_t" or "uint32_t" or "size_t" => (4, false, false),
                _ when baseType.Contains("int") || baseType == "long long" => (8, false, true), // 64-bit types use MOVED (double path)
                _ => (4, false, false),
            };
        }

        private ExpVar WrapExpr(Expr node)
        {
            var ev = ExpVar.Eval(InferExpType(node), () => GenerateExpr(node));
            if (node is IdentExpr ie && _varTypes.TryGetValue(ie.Name, out var vt) && vt.Contains("*"))
            {
                int pointedSize = GetPointerStepSize(vt);
                if (pointedSize > 1)
                    ev = ev.WithPointedTypeSize(pointedSize);
            }
            return ev;
        }

        /// <summary>
        /// 创建可存储的 ExpVar（Stack/Data loc），用于 ++/--/复合赋值等需要写回的操作。
        /// 对于无法识别为变量/全局量的表达式，退回 Eval 模式。
        /// </summary>
        private ExpVar WrapTargetExpr(Expr node)
        {
            if (node is IdentExpr ie)
            {
                var expType = InferExpType(node);
                int pointedSize = 0;
                if (_varTypes.TryGetValue(ie.Name, out var vt) && vt.Contains("*"))
                    pointedSize = GetPointerStepSize(vt);

                ExpVar tv;
                if (_variables.TryGetValue(ie.Name, out int off))
                    tv = ExpVar.Stack(off, 14, expType);
                else
                    tv = ExpVar.Data($"var_{ie.Name}", expType);

                if (pointedSize > 1)
                    tv = tv.WithPointedTypeSize(pointedSize);
                if (_isReferenceVar.TryGetValue(ie.Name, out var isRef) && isRef)
                    tv = tv.WithIsReference();
                return tv;
            }
            return WrapExpr(node);
        }

        /// <summary>
        /// 让随后生成的指令/诊断带上**这个表达式自己的**行列（语义见
        /// `CodeGeneratorBase.CurrentSourceLine`/`CurrentSourceColumn`）。
        ///
        /// 与语句入口那句是同一个道理，但**粒度细一层**：语句入口给出的是「这一句从哪开始」，
        /// 而 `ReportUndefined` 要指的用户真正写错的那个名字。表达式生成是递归的 ⇒
        /// 越往里越精确，最后停在**最内层那个节点**上（`a + b + nosuch` 停在 `nosuch`）。
        ///
        /// 判据 `Line > 0`：位置由解析器的原子入口统一盖（`ParsePrimary`/`ParseFactor`），
        /// 二元/一元节点的 Line 仍是 0 —— 置 0 会把刚盖好的原子位置冲掉，
        /// 那正是「列停在语句起始」的老毛病。
        /// </summary>
        private void GenerateExpr(Expr expr)
        {
        if (expr.Line > 0) { CurrentSourceLine = expr.Line; CurrentSourceColumn = expr.Column; }
            switch (expr)
            {
                case IntLiteral il:
                    Add(OpCode.MOVE, "R0", $"#{il.Value}");
                    break;
                case LongLiteral ll:
                    {
                        string llLabel = $"lng_{labelCounter++}";
                        dataSection[llLabel] = ll.Value;
                        Add(OpCode.MOVEL, "R0", llLabel);
                    }
                    break;
                case FloatLiteral fl:
                    if (fl.IsFloatSuffix)
                    {
                        // float 字面量：存储 32-bit IEEE 754 到 data section (flt_ 前缀 → .word)
                        string flabel = $"flt_{labelCounter++}";
                        int fbits = BitConverter.SingleToInt32Bits((float)fl.Value);
                        dataSection[flabel] = fbits;
                        instructions.Add(new Instruction(OpCode.MOVEF,
                            [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, flabel)],
                            instructions.Count));
                    }
                    else
                    {
                        // double 字面量：存储 64-bit IEEE 754 到 data section (dbl_ 前缀 → .dword)
                        string flabel = $"dbl_{labelCounter++}";
                        dataSection[flabel] = fl.Value; // 存 double 对象，VMLProgram 序列化时用 .dword
                        instructions.Add(new Instruction(OpCode.MOVED,
                            [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, flabel)],
                            instructions.Count));
                    }
                    break;
                case StringLiteral sl:
                    {
                        string label = $"str_{_nextString++}";
                        if (sl.Width == 16)
                            dataSection[label] = new VMLAssembler.DataString(sl.Value, VMLAssembler.StringWidth.Wide);
                        else if (sl.Width == 32)
                            dataSection[label] = new VMLAssembler.DataString(sl.Value, VMLAssembler.StringWidth.Unicode);
                        else
                            dataSection[label] = sl.Value;
                        Add(OpCode.MOVE, "R0", label);
                    }
                    break;
                case BoolLiteral bl:
                    Add(OpCode.MOVE, "R0", bl.Value ? "#1" : "#0");
                    break;
                case CharLiteral cl:
                    Add(OpCode.MOVE, "R0", $"#{(int)cl.Value}");
                    break;
                case IdentExpr id:
                    if (id.Name == "cout" || id.Name == "cin" || id.Name == "std_cout" || id.Name == "std_cin")
                    {
                        Add(OpCode.MOVE, "R0", "#0"); // I/O stream handle
                        return;
                    }
                    // Enum constant lookup
                    if (_enumValues.TryGetValue(id.Name, out int enumVal))
                    {
                        Add(OpCode.MOVE, "R0", $"#{enumVal}");
                        return;
                    }
                    // ⚠ 判据要**同时**看两张表：`_isArrayVar` 是局部的（每进一个函数就被清），
                    //   `_globalArrays` 是全局的。只看前者的话全局数组永远走不进这一支。
                    if ((_isArrayVar.TryGetValue(id.Name, out bool isArr) && isArr)
                        || _globalArrays.Contains(id.Name))
                    {
                        // Array variables: return the ADDRESS, not the value
                        if (_variables.TryGetValue(id.Name, out int arrOff))
                            Add(OpCode.MOVE, "R0", Vars?.FormatOffset(arrOff) ?? $"R14-{arrOff}");
                        else
                            instructions.Add(new Instruction(OpCode.MOVE, [
                                new(OperandType.REGISTER, 0),
                                new(OperandType.LABEL, $"var_{id.Name}")
                            ]));
                    }
                    else if (_variables.TryGetValue(id.Name, out int offset))
                    {
                        Add(OpCode.MOVE, "R0", Vars?.FormatOffset(offset) ?? $"R14-{offset}");
                        // For reference variables, dereference the pointer to get the actual value
                        if (_isReferenceVar.TryGetValue(id.Name, out bool isRef) && isRef)
                            Add(OpCode.MOVE, "R0", "(R0)");
                    }
                    else if (_definedFunctions.Contains(id.Name))
                    {
                        // 函数名用作值（函数指针赋值）→ 加载函数入口地址
                        string funcLabel = id.Name == "main" ? "main" : $"func_{id.Name}";
                        Add(OpCode.MOVE, "R0", funcLabel);
                    }
                    else
                    {
                        // 全局/静态变量：根据类型选择 load 指令（float→MOVEF, double→MOVED）
                        string varLabel = $"var_{id.Name}";
                        OpCode loadOp = OpCode.MOVE;
                        if (_varTypes.TryGetValue(id.Name, out string? varType) && varType != null)
                        {
                            var cppType = MapToCppType(varType);
                            loadOp = cppType switch
                            {
                                CppType.Float => OpCode.MOVEF,
                                CppType.Double => OpCode.MOVED,
                                _ => OpCode.MOVE
                            };
                        }
                        // ── 隐式 `this->field` ──
                        // 方法体里直接写字段名（不写 this->）时走到这里。这是**第 5 级**回退：
                        // 局部变量 → 函数名 → 全局变量 → **当前类的字段** → 报未声明。
                        // ⚠ 这一级原先整个是缺的 —— 所以「方法体看不见成员变量」，
                        //   而同样的写法在**构造函数**里却好用（那是内联展开的，没有 this 这一层）。
                        // ⚠ 判定**只认 `_currentClass`**，不认 `_thisSlot`：
                        //   "有没有 this"记在 `_hasThis` 上，而 `_thisSlot` 是个**偏移**
                        //   （第一个局部量 = `-4`，负数！），拿它当布尔用必然出错。
                        //   槽位固定取 `_thisSlot`，没有时退回那个约定值 —— 它是该方法里
                        //   **第一个**分配的局部量（紧随 `Vars.ResetLocals()` 之后）。
                        int thisOff = _hasThis ? _thisSlot : -4;
                        if (_currentClass != null
                            && TryFieldOffset(_currentClass, id.Name, out int implicitOff))
                        {
                            Add(OpCode.MOVE, "R0", Vars?.FormatOffset(thisOff) ?? $"R14-{thisOff}");
                            if (implicitOff > 0)
                                Add(OpCode.ADD, "R0", $"#{implicitOff}");
                            // ⚠ 偏移写 `0(R0)` 而不是 `(R0)` —— 序列化器认的是
                            //   `偏移(寄存器)` 这个形状，`(R0)` 会被写成 `@0`（寄存器 0）。
                            Add(OpCode.MOVE, "R0", "0(R0)");
                            break;
                        }
                        if (!dataSection.ContainsKey(varLabel))
                        {
                            // 局部表、函数表、`dataSection` 三处都没有 ⇒ 这个名字**从未声明过**。
                            // 此前这里直接发 `MOVE R0, var_x` —— 引用的是一个**可能根本不存在**的
                            // 标签（连"确定的 0"都不是，值取决于汇编器对该符号的处理）。
                            // `int a = 1; return a + nosuch;` 就是这么编过去的。
                            ReportUndefined(id.Name, ErrorCode.CodeGen_UndefinedVariable, "变量");
                            EmitUndefinedFallback();
                            break;
                        }
                        Add(loadOp, "R0", varLabel);
                    }
                    break;
                case ThisExpr _:
                    // `this` 由函数序言取出来存在局部槽里（见 CodeGenerator.GenerateFunction）。
                    // ⚠ 原来读的是 R14 —— 那是**帧指针**，不是 this。
                    //    非成员函数里没有 this，沿用旧行为免得影响别处。
                    if (_hasThis)
                        Add(OpCode.MOVE, "R0", Vars?.FormatOffset(_thisSlot) ?? $"R14-{_thisSlot}");
                    else
                        Add(OpCode.MOVE, "R0", "R14");
                    break;
                case BinaryExpr be:
                    GenerateBinaryExpr(be);
                    break;
                case UnaryExpr ue:
                    GenerateUnaryExpr(ue);
                    break;
                case CallExpr ce:
                    GenerateCallExpr(ce);
                    break;
                case TypeIdExpr te:
                    // typeid(expr): return the type_info pointer for a class instance
                    // Try static type resolution first, then fall back to runtime vtable lookup
                    string? resolvedClass = null;
                    if (te.Expression is ThisExpr)
                    {
                        resolvedClass = _currentClass;
                    }
                    else if (te.Expression is IdentExpr tie && _varTypes.TryGetValue(tie.Name, out string? vartype))
                    {
                        // Check if the variable type is a known class
                        string cleanType = vartype.Replace("*", "").Replace("&", "").Trim();
                        if (_classes.ContainsKey(cleanType))
                            resolvedClass = cleanType;
                    }

                    if (resolvedClass != null && dataSection.ContainsKey($"{resolvedClass}_typeid"))
                    {
                        AddInstruction(OpCode.MOVE, Reg(0), LabelOp($"{resolvedClass}_typeid"));
                    }
                    else
                    {
                        // Runtime type_info lookup: object's first word = type_info address
                        GenerateExpr(te.Expression);
                        Add(OpCode.MOVE, "R0", "(R0)"); // load type_info ptr from object[0]
                        Add(OpCode.MOVE, "R0", "(R0)"); // load class name string
                    }
                    break;
                case CastExpr ce when ce.TargetType == "dynamic_cast":
                    // dynamic_cast<T*>(ptr): check if ptr's typeid matches
                    // Simplified: just return the pointer as-is (no runtime check)
                    GenerateExpr(ce.Expression);
                    break;
                case MemberExpr me:
                    GenerateMemberExpr(me);
                    break;
                case AssignExpr ae:
                    GenerateAssignExpr(ae);
                    break;
                case NewExpr ne:
                    GenerateNewExpr(ne);
                    break;
                case DeleteExpr de:
                    GenerateExpr(de.Target);
                    Add(OpCode.CALL, "free");
                    break;
                case ConditionalExpr ce:
                    _expr!.EmitConditional(
                        WrapExpr(ce.Condition),
                        WrapExpr(ce.TrueExpr),
                        WrapExpr(ce.FalseExpr));
                    break;
                case CastExpr ce:
                    {
                        var srcType = GetExprTypeInfo(ce.Expression);
                        var (dstSize, dstFloat, dstDouble) = GetTypeLoadInfo(ce.TargetType);
                        // C++ long long 不使用 VML Long 路径
                        GenerateExpr(ce.Expression);
                        _expr!.EmitConversion(srcType.byteSize, srcType.isFloat, srcType.isDouble,
                                               dstSize, dstFloat, dstDouble,
                                               srcType.isLong, false);
                    }
                    break;
                case LambdaExpr le:
                    {
                        string ll = $"lambda_{labelCounter++}";
                        string skipLambda = $"skip_{ll}";
                        // JMP over lambda body (don't fall through!)
                        Add(OpCode.JMP, skipLambda);
                        // Lambda body starts here
                        labels[ll] = instructions.Count;
                        int savedRet = _currentFuncReturnLabel;
                        _currentFuncReturnLabel = labelCounter++;
                        int savedStackOffset = _stackOffset;
                        var savedVariables = new Dictionary<string, int>(_variables);
                        _stackOffset = 0;
                        _variables.Clear();
                        // prologue — standard frame: PUSH R15(ret), PUSH R14(fp), MOVE R14,R13
                        Add(OpCode.PUSH, "R15");
                        Add(OpCode.PUSH, "R14");
                        Add(OpCode.MOVE, "R14", "R13");
                        // Parameters are above BP: [R14+8] = first param, [R14+12] = second, etc.
                        // Map params to positive frame offsets (above saved R14 and R15)
                        for (int i = 0; i < le.Parameters.Count; i++)
                        {
                            // VML CALL pushes R15 (ret addr) to stack, so params are at [R14+12] not [R14+8]
                            int paramOffset = 12 + (le.Parameters.Count - 1 - i) * 4;
                            _variables[le.Parameters[i]] = paramOffset;
                        }
                        GenerateStmt(le.Body);
                        // epilogue + RET
                        string rl = $"ret_{_currentFuncReturnLabel}";
                        labels[rl] = instructions.Count;
                        Add(OpCode.MOVE, "R13", "R14");
                        Add(OpCode.POP, "R14");
                        Add(OpCode.POP, "R15");
                        Add(OpCode.RET);
                        // restore enclosing scope state
                        _currentFuncReturnLabel = savedRet;
                        _stackOffset = savedStackOffset;
                        _variables.Clear();
                        foreach (var kv in savedVariables)
                            _variables[kv.Key] = kv.Value;
                        // skip target: load lambda address into R0
                        labels[skipLambda] = instructions.Count;
                        Add(OpCode.MOVE, "R0", ll);
                    }
                    break;
                case SizeofExpr so:
                    int size = 4;
                    // （`SizeOfExprValue` 见本文件末尾；**它绝不抛**，查不到就退 4。）
                    // ⚠ `sizeof(表达式)` 走的是**另一条路**（解析器把 `sizeof(a)` 收成
                    //   `SizeofExpr { Expression = … }`，`TypeName` 是 **null**），
                    //   而这里原样只读 `TypeName` ⇒ 走到下面那行 `.Contains("*")` 直接
                    //   **NullReferenceException**：编译器崩掉，而且报错里**连行列都没有**
                    //   （实测：`int a=0; sizeof(a);` 只回一句 "Object reference not set…"）。
                    //   `sizeof(arr)` / `sizeof(a)` 在老程序里很常见（`memcpy(…, sizeof(x))`）。
                    if (so.TypeName == null) size = SizeOfExprValue(so.Expression);
                    else if (so.TypeName == "char") size = 1;
                    else if (so.TypeName == "short") size = 2;
                    else if (so.TypeName == "int" || so.TypeName == "float" || so.TypeName == "long") size = 4;
                    else if (so.TypeName == "double" || so.TypeName == "long long") size = 8;
                    else if (so.TypeName.Contains("*")) size = 4; // pointer
                    Add(OpCode.MOVE, "R0", $"#{size}");
                    break;
                case InitializerListExpr il:
                    if (il.Elements.Count > 0)
                        GenerateExpr(il.Elements[0]);
                    else
                        Add(OpCode.MOVE, "R0", "#0");
                    break;
                case CommaExpr ce:
                    // 逗号表达式：**先求左边**（值丢弃，但副作用必须真的生成代码），
                    // 再求右边 —— 右边的值留在 R0 作为整个表达式的值。
                    // ⚠ 左边那一句不能省：老程序写 `sprintf(…), settextstyle(…), outtextxy(…)`
                    //   靠的就是**每一条都执行**；只取最后一个等于把前两个调用丢掉。
                    //   （C 前端当初就写成 `expr = right;`，`getmaxyx` 这类宏的副作用整段消失。）
                    GenerateExpr(ce.Left);
                    GenerateExpr(ce.Right);
                    break;
                default:
                    Add(OpCode.MOVE, "R0", "#0");
                    break;
            }
        }

        private bool IsClassType(Expr expr, out string className)
        {
            className = "";
            if (expr is IdentExpr ie && _classes.ContainsKey(ie.Name))
            { className = ie.Name; return true; }
            if (expr is ThisExpr) { return true; }
            if (expr is MemberExpr me && _classes.ContainsKey(me.Member))
            { className = me.Member; return true; }
            return false;
        }

        private bool IsFloat(Expr e) => e is FloatLiteral;

        private bool IsStringExpr(Expr e)
        {
            if (e is StringLiteral) return true;
            if (e is IdentExpr ie && _varTypes.TryGetValue(ie.Name, out string? vt))
                return vt == "std_string" || vt == "string";
            return false;
        }

        /// Check if expression refers to an STL container (vector or string) variable
        private bool IsSTLExpr(Expr e)
        {
            if (e is IdentExpr ie && _varTypes.TryGetValue(ie.Name, out string? vt))
                return vt.StartsWith("std_vector_") || vt == "std_string" || vt == "string";
            return false;
        }

        private void GenerateBinaryExpr(BinaryExpr be)
        {
            // cout << expr (inline output via base class), 支持链式: cout << a << b << endl
            if (be.Op == "<<")
            {
                // 沿 << 链追溯到最左端，检查是否源自 cout (防止误判位移运算符)
                bool isCoutChain = false;
                Expr chainRoot = be;
                while (chainRoot is BinaryExpr rootBe && rootBe.Op == "<<")
                {
                    if (rootBe.Left is IdentExpr rootId && (rootId.Name == "cout" || rootId.Name == "std_cout"))
                    {
                        isCoutChain = true;
                        break;
                    }
                    chainRoot = rootBe.Left;
                }

                if (isCoutChain)
                {
                    // 递归处理左操作数 (cout 本身跳过, 嵌套的 << 递归生成)
                    if (be.Left is not IdentExpr)
                        GenerateBinaryExpr((BinaryExpr)be.Left);

                    // endl 必须提前判断 — 否则 GenerateExpr 会把它当作不存在的变量加载
                    if (be.Right is IdentExpr endlId && (endlId.Name == "endl" || endlId.Name.Contains("endl")))
                    {
                        EmitPrintNewline();
                    }
                    else
                    {
                        GenerateExpr(be.Right);
                        if (be.Right is IntLiteral || be.Right is CharLiteral || be.Right is BoolLiteral || be.Right is IdentExpr)
                            EmitPrintInt();
                        else if (be.Right is CallExpr ce2)
                        {
                            string? calleeName = (ce2.Callee as IdentExpr)?.Name;
                            if (IsStringReturningFunc(calleeName))
                                EmitPrintString();
                            else
                                EmitPrintInt();
                        }
                        else EmitPrintString();
                    }
                    Add(OpCode.MOVE, "R0", "#0"); // cout << returns 0
                    return;
                }
            }

            // std::string concatenation: s1 + s2
            if (be.Op == "+" && IsStringExpr(be.Left))
            {
                GenerateExpr(be.Left);                   // R0 = left string ptr
                Add(OpCode.PUSH, "R0");                  // save left ptr
                GenerateExpr(be.Right);                  // R0 = right string/value
                Add(OpCode.PUSH, "R0");                  // save right ptr
                Add(OpCode.CALL, "str_concat");      // R0 = new concatenated string
                Add(OpCode.ADD, "R13", "#8");            // pop args
                return;
            }

            // STL container indexing: v[i] (header is 8 bytes: length + capacity)
            if (be.Op == "[]" && IsSTLExpr(be.Left))
            {
                GenerateExpr(be.Left);                   // R0 = container ptr
                Add(OpCode.PUSH, "R0");
                GenerateExpr(be.Right);                  // R0 = index
                Add(OpCode.MOVE, "R1", "R0");
                Add(OpCode.POP, "R0");                   // R0 = container ptr
                Add(OpCode.SHL, "R1", "#2");             // index * 4
                Add(OpCode.ADD, "R1", "#8");             // skip length + capacity headers
                Add(OpCode.ADD, "R1", "R0");             // &container[index]
                Add(OpCode.MOVE, "R0", "(R1)");          // R0 = value
                return;
            }

            // Operator overloading for class types
            if (IsClassType(be.Left, out string clsName))
            {
                string opName = be.Op switch { "+" => "operator+", "-" => "operator-", "*" => "operator*", "/" => "operator/", "%" => "operator%", "==" => "operator==", "!=" => "operator!=", "<" => "operator<", "<=" => "operator<=", ">" => "operator>", ">=" => "operator>=", "<<" => "operator<<", ">>" => "operator>>", "&&" => "operator&&", "||" => "operator||", "&" => "operator&", "|" => "operator|", "^" => "operator^", "[]" => "operator[]", _ => $"operator_{be.Op}" };
                GenerateExpr(be.Left); Add(OpCode.PUSH, "R0");
                GenerateExpr(be.Right); Add(OpCode.PUSH, "R0");
                string lbl = $"method_{clsName}_{opName}";
                Add(OpCode.CALL, lbl); Add(OpCode.ADD, "R13", "#8");
                return;
            }

            // 算术/比较/逻辑运算 → ExpressionManager 统一处理
            switch (be.Op)
            {
                case "+": case "-": case "*": case "/": case "%":
                    _expr!.EmitBinOp(WrapExpr(be.Left), WrapExpr(be.Right), be.Op);
                    return;
                case "==": case "!=": case "<": case "<=": case ">": case ">=":
                    _expr!.EmitCmp(WrapExpr(be.Left), WrapExpr(be.Right), be.Op);
                    return;
                case "&&":
                    _expr!.EmitAnd(WrapExpr(be.Left), WrapExpr(be.Right));
                    return;
                case "||":
                    _expr!.EmitOr(WrapExpr(be.Left), WrapExpr(be.Right));
                    return;
            }

            // 位运算使用 ExpressionManager 统一处理
            switch (be.Op)
            {
                case "&": _expr!.EmitBitAnd(WrapExpr(be.Left), WrapExpr(be.Right)); break;
                case "|": _expr!.EmitBitOr(WrapExpr(be.Left), WrapExpr(be.Right)); break;
                case "^": _expr!.EmitBitXor(WrapExpr(be.Left), WrapExpr(be.Right)); break;
                case "<<": _expr!.EmitShl(WrapExpr(be.Left), WrapExpr(be.Right)); break;
                case ">>": _expr!.EmitShr(WrapExpr(be.Left), WrapExpr(be.Right)); break;
                case "[]":
                    GenerateExpr(be.Left);
                    Add(OpCode.PUSH, "R0");
                    GenerateExpr(be.Right);
                    Add(OpCode.MOVE, "R1", "R0");
                    Add(OpCode.POP, "R0");
                    // R0 = base address, R1 = index
                    bool isNestedArray = be.Left is BinaryExpr inner && inner.Op == "[]";
                    // 元素字节数 / 有没有长度头 —— **四处 `[]` 共用同一套判据**
                    //（读、写、取址、成员基址），见 `ArrayIndexInfo`。
                    var (elemSize, hasHeader) = ArrayIndexInfo(be.Left, isNestedArray);
                    // 获取内层维度用于 stride (如 arr[2][3] 的内层维度为3)
                    int innerDim = 1;
                    if (!isNestedArray && be.Left is IdentExpr arrId && _arrayInnerDim.TryGetValue(arrId.Name, out int idim))
                        innerDim = idim;
                    if (innerDim > 1)
                    {
                        Add(OpCode.MOVE, "R2", $"#{innerDim}");
                        Add(OpCode.MUL, "R1", "R1", "R2");
                        if (elemSize > 1) Add(OpCode.SHL, "R1", $"#{ElemShift(elemSize)}");
                        if (hasHeader) Add(OpCode.ADD, "R1", "#4");
                        Add(OpCode.ADD, "R0", "R0", "R1");
                    }
                    else
                    {
                        if (elemSize == 2) Add(OpCode.SHL, "R1", "#1");
                        else if (elemSize > 2) Add(OpCode.SHL, "R1", "#2");   // default: *4
                        if (hasHeader && !isNestedArray)
                            Add(OpCode.ADD, "R1", "#4");
                        Add(OpCode.ADD, "R1", "R0");
                        // 根据元素大小选择加载指令
                        if (elemSize == 1)
                            Add(OpCode.MOVEB, "R0", $"[R1]");
                        else if (elemSize == 2)
                            Add(OpCode.MOVEH, "R0", $"[R1]");
                        else
                            Add(OpCode.MOVE, "R0", "(R1)");
                    }
                    break;
                default: break;
            }
        }

        private void GenerateUnaryExpr(UnaryExpr ue)
        {
            string? vt;
            switch (ue.Op)
            {
                case "-":
                    GenerateExpr(ue.Operand);
                    _expr!.EmitNeg(ExpVar.Reg(0, InferExpType(ue.Operand)));
                    break;
                case "!":
                    _expr!.EmitNot(WrapExpr(ue.Operand));
                    break;
                case "~":
                    _expr!.EmitBitNot(WrapExpr(ue.Operand));
                    break;
                // `++`/`--` 作用在**隐式 `this->字段`** 上时，走 `Emit*Inc/Dec` 会落到
                // `WrapTargetExpr` 的 `Data("var_字段名")` 分支 —— 改的是全局量、不是字段
                // （与 `v += e` 同一条根因）。这里降级成 `v = v ± 1`，由成员赋值那条路处理。
                //
                // ⚠ 已知局限：作为**子表达式**用时（`x = v++`）返回的是**自增后**的值，
                //   与 C++ 的后缀语义（返回旧值）不同。语句位置（`v++;` 独占一行）不受影响，
                //   而 `_expr` 那套 ExpVar 本来就只对 Stack/Data 落点做得正确 ——
                //   这是"字段上的自增"此前**完全不能用**与"取值语义略有出入"之间的取舍。
                case "++" or "++post" or "--" or "--post"
                    when IsImplicitThisField(ue.Operand, out var incMem):
                    GenerateAssignExpr(new AssignExpr
                    {
                        Target = incMem,
                        Op = "=",
                        Value = new BinaryExpr
                        {
                            Left = incMem,
                            Op = ue.Op.StartsWith("+") ? "+" : "-",
                            Right = new IntLiteral { Value = 1 },
                        },
                    });
                    break;
                case "++":
                    _expr!.EmitPrefixInc(WrapTargetExpr(ue.Operand));
                    break;
                case "++post":
                    _expr!.EmitPostfixInc(WrapTargetExpr(ue.Operand));
                    break;
                case "--":
                    _expr!.EmitPrefixDec(WrapTargetExpr(ue.Operand));
                    break;
                case "--post":
                    _expr!.EmitPostfixDec(WrapTargetExpr(ue.Operand));
                    break;
                case "*": // dereference — use type-sensitive load
                    GenerateExpr(ue.Operand);
                    // Look up pointed-to type: if operand is a typed variable (char*/short*/float*/double*)
                    // use the appropriate sized load instruction.
                    string? pointedType = null;
                    if (ue.Operand is IdentExpr derefId && _varTypes.TryGetValue(derefId.Name, out vt))
                        pointedType = vt;
                    var (size, isFloat, isDouble) = GetTypeLoadInfo(pointedType);
                    instructions.Add(new Instruction(
                        ExpressionManager.SelectLoadOp(size, isFloat, isDouble),
                        new List<Operand> {
                            new(OperandType.REGISTER, 0),
                            new(OperandType.MEMORY, "R0")
                        }));
                    break;
                case "&": // address-of
                    GenerateAddressOf(ue.Operand);
                    break;
            }
        }

        private string MangleName(string name, List<Expr> args)
        {
            // Simple name mangling: func_i, func_ii, etc. for overloading
            string suffix = "";
            foreach (var arg in args)
            {
                if (arg is IntLiteral) suffix += "i";
                else if (arg is FloatLiteral) suffix += "f";
                else if (arg is StringLiteral) suffix += "s";
                else if (arg is CharLiteral) suffix += "c";
                else if (arg is BoolLiteral) suffix += "b";
                else if (arg is IdentExpr) suffix += "v";
                else suffix += "x";
            }
            return suffix.Length > 0 ? $"{name}_{suffix}" : name;
        }

        /// Generate the address (l-value) of an expression into R0
        private void GenerateAddressOf(Expr expr)
        {
            // ── `&字段`（隐式 `this->字段`）────────────────────────────────
            // 方法体里写 `&sky` 要的是**这个对象的那个字段**的地址。
            // ⚠ 原先这一支不存在：`IsImplicitThisField` 只在**取值**那条路上用过，
            //   取地址这条一路落到下面的 `IdentExpr` 分支，拿名字当**全局量**
            //   （`var_sky`）—— 那个标签根本不存在（或指向别处），
            //   于是 `actors[0] = &sky` 存进去是个野地址，
            //   再 `actors[i]->Draw()` 就是**拿着野地址当函数指针调**：
            //   实测跳飞了、程序"正常结束"、屏幕上什么都没有、**一句报错都没有**。
            //   必须排在下面"局部量 / 全局量"之前（字段不在 `_variables` 里，
            //   但它确实属于 `this`）。
            if (expr is IdentExpr idf && IsImplicitThisField(idf, out var implicitAddrMem))
            {
                GenerateMemberAddress(implicitAddrMem);
            }
            else if (expr is IdentExpr ie && _variables.TryGetValue(ie.Name, out int offset))
            {
                // For reference vars, the local slot already contains the address; load it directly
                if (_isReferenceVar.TryGetValue(ie.Name, out bool isRef) && isRef)
                {
                    Add(OpCode.MOVE, "R0", Vars?.FormatOffset(offset) ?? $"R14-{offset}");
                }
                else
                {
                    // For regular local vars, compute the stack address
                    Add(OpCode.MOVE, "R0", "R14");
                    if (offset > 0) Add(OpCode.SUB, "R0", $"#{offset}");
                    else if (offset < 0) Add(OpCode.SUB, "R0", $"#{-offset}");
                }
            }
            else if (expr is IdentExpr ie2)
            {
                // Global variable: load its data-section label address (must use LABEL type, not MEMORY)
                // MEMORY type dereferences and reads the value; LABEL type returns the address
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.LABEL, $"var_{ie2.Name}")
                }));
            }
            else if (expr is BinaryExpr be && be.Op == "[]")
            {
                // &a[i]: compute address of array element
                GenerateExpr(be.Left);           // R0 = base address
                Add(OpCode.PUSH, "R0");
                GenerateExpr(be.Right);          // R0 = index
                Add(OpCode.MOVE, "R1", "R0");
                Add(OpCode.POP, "R0");           // R0 = base
                // 与读/写**同一套判据**（见 `ArrayIndexInfo`）—— 字节数组没有长度头、步长 1
                var (aElemSize, aHasHeader) = ArrayIndexInfo(be.Left, isNested: false);
                if (aElemSize == 2) Add(OpCode.SHL, "R1", "#1");
                else if (aElemSize > 2) Add(OpCode.SHL, "R1", "#2");   // index * 4
                if (aHasHeader) Add(OpCode.ADD, "R1", "#4");           // +4 (skip header)
                Add(OpCode.ADD, "R0", "R1");     // R0 = base + index*elemSize (+ header)
            }
            else if (expr is UnaryExpr ue && ue.Op == "*")
            {
                // `&*p` **恒等于** `p`（C++ 的恒等式），取地址与解引用互相抵消。
                //
                // ⚠ 原先落到下面的兜底分支 `GenerateExpr(expr)`，而 `*p` 生成的是
                //   "从 p 读一个字"的**值** ⇒ `&(*p)` 变成 `[p]` —— 对类对象来说
                //   那一个字是 **vptr**，于是拿到一个完全无关的地址（实测读它当场
                //   内存越界，PC 落在被调方的第一条成员访问上，看着像"this 传错了"）。
                GenerateExpr(ue.Operand);
            }
            else if (expr is MemberExpr me)
            {
                GenerateMemberAddress(me);
            }
            else
            {
                // Fallback: generate the expression value (may not work for all l-values)
                GenerateExpr(expr);
            }
        }

        private string RecognizeStdType(Expr expr)
        {
            // Detect if an expression represents a known std type
            if (expr is IdentExpr ie)
            {
                // Look up the variable's type from stack tracking
                return "";
            }
            return "";
        }

        private bool IsStdVectorType(MemberExpr me)
        {
            if (me.Object is IdentExpr ie)
            {
                // Check if variable type starts with std_vector
                string varType = "";
                if (_variables.ContainsKey(ie.Name))
                    varType = ""; // need type tracking
                return varType.StartsWith("std_vector");
            }
            return false;
        }

        /// <summary>Get the number of 4-byte slots a struct type occupies</summary>
        private int GetStructSlotCount(string? type)
        {
            if (string.IsNullOrEmpty(type)) return 1;
            string clean = CleanType(type);
            if (_classes.TryGetValue(clean, out var cls))
                return Math.Max(1, cls.Members.Count(m => !m.IsMethod));
            return 1;
        }

        /// <summary>Push all fields of a struct argument onto the stack (reverse order for right-to-left push)</summary>
        private void PushStructArgFields(Expr arg, ClassDecl cls, bool reverse = true)
        {
            var fields = cls.Members.Where(m => !m.IsMethod).ToList();
            if (reverse) fields.Reverse();
            int vtableOff = GetVtableOffset(cls);
            if (arg is IdentExpr argId)
            {
                string srcLabel = $"var_{argId.Name}";
                foreach (var m in fields)
                {
                    int idx = cls.Members.IndexOf(m);
                    int off = vtableOff + idx * 4;
                    instructions.Add(new Instruction(OpCode.MOVE, [
                        new(OperandType.REGISTER, 0),
                        new(OperandType.LABEL, srcLabel)
                    ]));
                    if (off > 0) Add(OpCode.ADD, "R0", $"#{off}");
                    Add(OpCode.MOVE, "R0", "(R0)");
                    Add(OpCode.PUSH, "R0");
                }
            }
        }

        private void GenerateCallExpr(CallExpr ce)
        {
            // ===== STL container method calls =====
            if (ce.Callee is MemberExpr stlMe)
            {
                // std::vector::push_back(value)
                if (stlMe.Member == "push_back" && ce.Arguments.Count >= 1)
                {
                    string pbOk = $"pb_ok_{labelCounter++}";
                    string pbEnd = $"pb_end_{labelCounter++}";

                    GenerateExpr(stlMe.Object);        // R0 = vector ptr
                    Add(OpCode.PUSH, "R0");            // [sp] = vector ptr
                    Add(OpCode.MOVE, "R1", "(R0)");    // R1 = length
                    Add(OpCode.MOVE, "R3", "4(R0)");   // R3 = capacity
                    Add(OpCode.CMP, "R1", "R3");
                    Add(OpCode.JL, pbOk);              // if length < capacity, ok

                    // Overflow: reallocate via runtime function
                    GenerateAddressOf(stlMe.Object); // R0 = &variable
                    Add(OpCode.PUSH, "R0");          // push &variable
                    Add(OpCode.CALL, "vector_grow"); // R0 = new_ptr, *(&variable) updated
                    Add(OpCode.ADD, "R13", "#8");    // pop args
                    Add(OpCode.PUSH, "R0");          // push new_ptr for fast path
                    Add(OpCode.MOVE, "R1", "(R0)");  // R1 = length
                    Add(OpCode.JMP, pbOk);

                    labels[pbOk] = instructions.Count;
                    Add(OpCode.MOVE, "R2", "R1");
                    Add(OpCode.SHL, "R2", "#2");       // R2 = length * 4
                    Add(OpCode.ADD, "R2", "#8");       // R2 = 8 + length*4 (skip 8-byte header)
                    Add(OpCode.ADD, "R2", "R0");       // R2 = &container[length]
                    GenerateExpr(ce.Arguments[0]);      // R0 = value to push
                    Add(OpCode.MOVE, "(R2)", "R0");   // store value
                    Add(OpCode.POP, "R0");             // R0 = vector ptr
                    Add(OpCode.MOVE, "R1", "(R0)");    // R1 = length
                    Add(OpCode.ADD, "R1", "#1");       // length++
                    Add(OpCode.MOVE, "(R0)", "R1");   // store updated length
                    Add(OpCode.MOVE, "R0", "#0");      // return 0 (ok)
                    labels[pbEnd] = instructions.Count;
                    return;
                }
                // std::vector::size() / std::string::length() / std::string::size()
                if ((stlMe.Member == "size" || stlMe.Member == "length") && ce.Arguments.Count == 0)
                {
                    GenerateExpr(stlMe.Object);         // R0 = container pointer
                    Add(OpCode.MOVE, "R0", "(R0)");     // R0 = first element (length header)
                    return;
                }
                // std::string::c_str()
                if (stlMe.Member == "c_str" && ce.Arguments.Count == 0)
                {
                    GenerateExpr(stlMe.Object);         // R0 = string/container pointer
                    Add(OpCode.ADD, "R0", "#8");        // skip 8-byte header, return data pointer
                    return;
                }
                // std::vector::pop_back() / std::string::pop_back()
                if (stlMe.Member == "pop_back" && ce.Arguments.Count == 0)
                {
                    GenerateExpr(stlMe.Object);          // R0 = container pointer
                    Add(OpCode.MOVE, "R1", "(R0)");      // R1 = length
                    string skipLabel = $"pop_{labelCounter++}";
                    Add(OpCode.CMP, "R1", "#0");
                    Add(OpCode.JE, skipLabel);            // skip if already empty
                    Add(OpCode.SUB, "R1", "#1");          // length--
                    Add(OpCode.MOVE, "(R0)", "R1");      // store updated length
                    labels[skipLabel] = instructions.Count;
                    Add(OpCode.MOVE, "R0", "#0");          // return 0
                    return;
                }
                // std::vector::empty() / std::string::empty()
                if (stlMe.Member == "empty" && ce.Arguments.Count == 0)
                {
                    EmitCompareToBool(() => { GenerateExpr(stlMe.Object); Add(OpCode.MOVE, "R0", "(R0)"); }, OpCode.JE);
                    return;
                }
                // std::vector::clear() / std::string::clear()
                if (stlMe.Member == "clear" && ce.Arguments.Count == 0)
                {
                    GenerateExpr(stlMe.Object);          // R0 = container pointer
                    Add(OpCode.MOVE, "R1", "#0");
                    Add(OpCode.MOVE, "(R0)", "R1");      // length = 0
                    Add(OpCode.MOVE, "R0", "#0");          // return 0
                    return;
                }
                // std::vector::front() / std::string::front()
                if ((stlMe.Member == "front" || stlMe.Member == "back") && ce.Arguments.Count == 0)
                {
                    GenerateExpr(stlMe.Object);          // R0 = container pointer
                    Add(OpCode.MOVE, "R1", "(R0)");       // R1 = length
                    if (stlMe.Member == "back")
                        Add(OpCode.SUB, "R1", "#1");      // back: offset = length-1
                    else
                        Add(OpCode.MOVE, "R1", "#0");     // front: offset = 0
                    Add(OpCode.SHL, "R1", "#2");          // R1 = offset * 4
                    Add(OpCode.ADD, "R1", "#8");          // skip 8-byte header
                    Add(OpCode.ADD, "R1", "R0");          // R1 = &container + 8 + offset*4
                    Add(OpCode.MOVE, "R0", "(R1)");       // R0 = value
                    return;
                }
                // std::vector::operator[] is handled via BinaryExpr "[]" indexing, dispatched from GenerateExpr
                // std::string::substr(pos, count)
                if (stlMe.Member == "substr" && ce.Arguments.Count == 2)
                {
                    string copyLoop = $"sub_lp_{labelCounter++}";
                    string copyDone = $"sub_dn_{labelCounter++}";

                    GenerateExpr(stlMe.Object);           // R0 = src string ptr
                    Add(OpCode.PUSH, "R0");                // [sp] = src

                    GenerateExpr(ce.Arguments[1]);         // R0 = requested count
                    Add(OpCode.MOVE, "R3", "R0");          // R3 = count

                    GenerateExpr(ce.Arguments[0]);         // R0 = pos
                    Add(OpCode.MOVE, "R2", "R0");          // R2 = pos

                    Add(OpCode.POP, "R1");                 // R1 = src
                    Add(OpCode.MOVE, "R4", "(R1)");        // R4 = src length
                    Add(OpCode.SUB, "R4", "R2");           // R4 = length - pos
                    string clampOk = $"sub_cl_{labelCounter++}";
                    Add(OpCode.CMP, "R3", "R4");
                    Add(OpCode.JLE, clampOk);              // if count <= available, ok
                    Add(OpCode.MOVE, "R3", "R4");          // else count = available
                    labels[clampOk] = instructions.Count;

                    // Allocate new string: header(8) + count*4
                    Add(OpCode.MOVE, "R0", "R3");
                    Add(OpCode.SHL, "R0", "#2");
                    Add(OpCode.ADD, "R0", "#8");           // header = 8 bytes
                    Add(OpCode.CALL, "alloc");         // R0 = dst ptr
                    Add(OpCode.PUSH, "R0");                // [sp] = dst (save for return)

                    Add(OpCode.MOVE, "(R0)", "R3");       // *dst = count (length header)

                    // src ptr = src + 8 + pos*4
                    Add(OpCode.SHL, "R2", "#2");
                    Add(OpCode.ADD, "R2", "#8");
                    Add(OpCode.ADD, "R2", "R1");           // R2 = &src[pos]

                    Add(OpCode.ADD, "R0", "#8");           // R0 = dst data start
                    Add(OpCode.MOVE, "R1", "R2");          // R1 = src data ptr
                    Add(OpCode.MOVE, "R2", "R0");          // R2 = dst data ptr
                    Add(OpCode.MOVE, "R4", "R3");          // R4 = loop counter

                    labels[copyLoop] = instructions.Count;
                    Add(OpCode.CMP, "R4", "#0");
                    Add(OpCode.JE, copyDone);
                    Add(OpCode.MOVE, "R0", "(R1)");
                    Add(OpCode.MOVE, "(R2)", "R0");
                    Add(OpCode.ADD, "R1", "#4");
                    Add(OpCode.ADD, "R2", "#4");
                    Add(OpCode.SUB, "R4", "#1");
                    Add(OpCode.JMP, copyLoop);
                    labels[copyDone] = instructions.Count;

                    Add(OpCode.POP, "R0");                 // R0 = dst ptr
                    return;
                }
            }

            // ===== 外置库函数调用（改为 CALL 而非内联 SYSCALL） =====

            // putchar(c) → CALL vml_print_char
            if (ce.Callee is IdentExpr pcId && pcId.Name.Contains("putchar"))
            {
                if (ce.Arguments.Count > 0) GenerateExpr(ce.Arguments[0]);
                Add(OpCode.CALL, "putchar");
                return;
            }
            // getchar() → CALL vml_input_int
            if (ce.Callee is IdentExpr gcId && gcId.Name.Contains("getchar"))
            {
                Add(OpCode.CALL, "input_int");
                return;
            }
            // puts(s) → CALL vml_println_str
            if (ce.Callee is IdentExpr psId && psId.Name.Contains("puts"))
            {
                if (ce.Arguments.Count > 0) GenerateExpr(ce.Arguments[0]);
                Add(OpCode.CALL, "println_str");
                return;
            }
            // printf("纯字符串") → CALL print_str。**只在"恰好一个实参"时走这条捷径**。
            //
            // ⚠ 这里原先写的是 `ce.Arguments.Count > 0` —— 于是**多实参的 printf 也只取第一个**、
            //    第 2 个起全部丢掉，而且**一个字都不报**。实测
            //    `printf("OUT-INT=%d\n", 42)` 打出来的就是字面量 `OUT-INT=%d`，42 凭空消失
            //    （探针 `scripts/vml-out-probe/langs/nat.cpp`）。
            //    多实参意味着格式串里有 `%d`/`%s` —— 那正是被丢掉的东西。
            //    交给普通调用路径落到 `Lib/shared/printf.vml` 的真实现（C 前端就是这么做的，
            //    它没有这条特例，所以 C 的 printf 一直是对的 —— 又一处"两门语言同一件事两种实现"）。
            if (ce.Callee is IdentExpr pfId && pfId.Name.Contains("printf") && ce.Arguments.Count == 1)
            {
                GenerateExpr(ce.Arguments[0]);
                Add(OpCode.CALL, "print_str");
                return;
            }
            // exit(n) → MOVE R0, n; SYSCALL #3
            if (ce.Callee is IdentExpr exId && exId.Name.Contains("exit"))
            {
                if (ce.Arguments.Count > 0) GenerateExpr(ce.Arguments[0]);
                EmitExit();
                return;
            }
            // get_platform() → SYSCALL #374
            if (ce.Callee is IdentExpr gpId && gpId.Name.Contains("get_platform"))
            {
                Add(OpCode.SYSCALL, "#374");
                return;
            }
            // endl → CALL vml_newline
            if (ce.Callee is IdentExpr endlId && endlId.Name.Contains("endl"))
            {
                Add(OpCode.CALL, "newline");
                return;
            }
            // peek*(addr) → CALL shared_peek*
            if (ce.Callee is IdentExpr peekName && peekName.Name.StartsWith("peek"))
            {
                string runtimeFn = peekName.Name;
                if (ce.Arguments.Count >= 1) GenerateExpr(ce.Arguments[0]);
                Add(OpCode.CALL, runtimeFn);
                return;
            }
            // poke*(addr, val) → CALL poke*
            if (ce.Callee is IdentExpr pokeName && pokeName.Name.StartsWith("poke") && ce.Arguments.Count >= 2)
            {
                string runtimeFn = pokeName.Name;
                GenerateExpr(ce.Arguments[0]);
                Add(OpCode.PUSH, "R0");
                GenerateExpr(ce.Arguments[1]);
                Add(OpCode.MOVE, "R1", "R0");
                Add(OpCode.POP, "R0");
                Add(OpCode.CALL, runtimeFn);
                return;
            }

            // std::move(x) → just return the value
            if (ce.Callee is IdentExpr moveId && (moveId.Name.Contains("_move") || moveId.Name == "move"))
            {
                if (ce.Arguments.Count == 1)
                {
                    GenerateExpr(ce.Arguments[0]);
                    return;
                }
            }
            // make_unique<T>(args) → CALL vml_alloc
            if (ce.Callee is IdentExpr makeId && makeId.Name.Contains("make_unique"))
            {
                Add(OpCode.MOVE, "R0", "#4");
                Add(OpCode.CALL, "alloc");
                foreach (var arg in ce.Arguments)
                {
                    GenerateExpr(arg);
                    Add(OpCode.PUSH, "R0");
                }
                if (ce.Arguments.Count > 0)
                    Add(OpCode.POP, "R1");
                Add(OpCode.MOVE, "(R0)", "R1");
                return;
            }
            // make_shared<T>(args) → CALL vml_alloc
            if (ce.Callee is IdentExpr makeSharedId && makeSharedId.Name.Contains("make_shared"))
            {
                Add(OpCode.MOVE, "R0", "#8");
                Add(OpCode.CALL, "alloc");
                Add(OpCode.MOVE, "R1", "R0");
                Add(OpCode.MOVE, "R0", "#1");
                Add(OpCode.MOVE, "(R1)", "R0");
                if (ce.Arguments.Count > 0)
                {
                    GenerateExpr(ce.Arguments[0]);
                    Add(OpCode.MOVE, "4(R1)", "R0");
                }
                Add(OpCode.MOVE, "R0", "R1");
                return;
            }

            string funcName = "";
            bool hasThis = false;
            // 虚调用的槽号（-1 = 不是虚调用）。非 -1 时走**间接调用**：
            // 实现地址从**对象自己的** vptr 取，而不是编译期写死某个符号。
            int virtualSlot = -1;
            // ===== std::min / std::max / std::swap — MCU built-in =====
            if (ce.Callee is IdentExpr stlFn)
            {
                string fn = stlFn.Name;
                if (fn == "std::min" && ce.Arguments.Count >= 2)
                {
                    GenerateExpr(ce.Arguments[0]); Add(OpCode.PUSH, "R0");
                    GenerateExpr(ce.Arguments[1]); Add(OpCode.POP, "R1");
                    Add(OpCode.CMP, "R0", "R1"); string ml = $"min_{labelCounter++}";
                    Add(OpCode.JLE, ml); Add(OpCode.MOVE, "R0", "R1"); labels[ml] = instructions.Count;
                    return;
                }
                if (fn == "std::max" && ce.Arguments.Count >= 2)
                {
                    GenerateExpr(ce.Arguments[0]); Add(OpCode.PUSH, "R0");
                    GenerateExpr(ce.Arguments[1]); Add(OpCode.POP, "R1");
                    Add(OpCode.CMP, "R0", "R1"); string ml = $"max_{labelCounter++}";
                    Add(OpCode.JGE, ml); Add(OpCode.MOVE, "R0", "R1"); labels[ml] = instructions.Count;
                    return;
                }
                if (fn == "std::swap" && ce.Arguments.Count >= 2)
                {
                    // Generate: tmp = *a; *a = *b; *b = tmp
                    GenerateExpr(ce.Arguments[0]); Add(OpCode.PUSH, "R0");
                    Add(OpCode.MOVE, "R1", "(R0)"); Add(OpCode.PUSH, "R1");
                    GenerateExpr(ce.Arguments[1]); Add(OpCode.MOVE, "R2", "(R0)");
                    Add(OpCode.POP, "R1"); Add(OpCode.POP, "R0");
                    Add(OpCode.MOVE, "(R0)", "R2");
                    GenerateExpr(ce.Arguments[1]);
                    Add(OpCode.MOVE, "(R0)", "R1");
                    Add(OpCode.MOVE, "R0", "#0");
                    return;
                }
                // std::sort(vec) → CALL arr_sort_bubble (MCU内联vector排序)
                if (fn == "std::sort" && ce.Arguments.Count >= 1)
                {
                    GenerateExpr(ce.Arguments[0]); // R0 = vector ptr
                    Add(OpCode.ADD, "R0", "#8");   // skip 8-byte header → data array
                    Add(OpCode.PUSH, "R0");
                    Add(OpCode.CALL, "arr_sort_bubble");
                    Add(OpCode.MOVE, "R0", "#0");
                    return;
                }
                // std::find(vec, value) → CALL arr_indexof (return index or -1)
                if (fn == "std::find" && ce.Arguments.Count >= 2)
                {
                    GenerateExpr(ce.Arguments[0]); Add(OpCode.ADD, "R0", "#8"); Add(OpCode.PUSH, "R0");
                    GenerateExpr(ce.Arguments[1]); Add(OpCode.PUSH, "R0");
                    Add(OpCode.CALL, "arr_indexof");
                    return;
                }
            }

            if (ce.Callee is IdentExpr ie)
            {
                funcName = ie.Name;

                // ── 隐式 `this` 的**成员方法调用** ──────────────────────────────
                // 方法体里直接写 `Double()`（不带对象）调的是**自己**的成员方法，
                // 接收者就是 `this`。原先这条**完全没处理**：`funcName` 直接取标识符名
                // （`Double` 而不是 `method_Ape_Double`），也没压 `this` ⇒
                // 被调方从 `[12(R14)]` 读到的是**实参或残留值**当 this 用，
                // 而它读的是自己的局部槽 ⇒ 值恒为 0；若那个垃圾值恰好像指针，
                // 就是"内存访问越界"直接崩（实测：`HandX()` 写在 `Shoot()` 里）。
                //
                // 判据用 `FindMethodDeep`（沿继承链找方法声明）：名字确实是**当前类
                // （或其基类）的方法**时才算，否则照旧当自由函数。
                // 顺序与显式调用一致：`this` **最先**压，然后实参右到左。
                if (_hasThis && !string.IsNullOrEmpty(_currentClass)
                    && FindMethodDeep(_currentClass!, ie.Name, ce.Arguments.Count, out string ownerCls, out var mDecl)
                    && mDecl != null)
                {
                    if (mDecl.IsVirtual && VirtualSlots(ownerCls).TryGetValue(ie.Name, out int mslot))
                    {
                        virtualSlot = mslot;
                    }
                    else
                    {
                        funcName = MethodSymbol(ownerCls, ie.Name, mDecl.Parameters.Select(p => p.Type));
                    }
                    GenerateExpr(new ThisExpr { Line = ce.Line, Column = ce.Column });
                    Add(OpCode.PUSH, "R0");
                    hasThis = true;
                }
            }
            else if (ce.Callee is MemberExpr me)
            {
                // obj.method() / obj->method() —— 接收者**不一定是简单标识符**：
                // `list[i]->m()`、`(*p).m()`、`a.b.m()` 都走这里。
                //
                // ⚠ 这里原先写成 `if (me.Object is IdentExpr objId) { … }` 而**没有 else**：
                //   接收者不是标识符时整段被跳过 ⇒ `funcName` 停在 `""` ⇒ 一路落到下面
                //   "函数指针间接调用"那条路，发一条 **`call R0`**，而 R0 里装的是
                //   **对象指针**（不是函数地址）⇒ 跳到数据上执行。
                //   症状是"程序跑完、一行输出都没有"，**编译期零报错** ——
                //   而 `Entity* e = list[0]; e->m()` 这种绕一步的写法又是好的，
                //   所以只看"指针调用能不能用"会误判成没问题。
                //   顺带把三分支（虚 / 非虚 / 无类）合并：它们本来就只差 `funcName` 一行，
                //   分开写正是"漏一个分支"的土壤。
                var recvCls = ResolveClassOf(me.Object);
                // 虚调用 = 从**对象自己的** vptr（对象第 0 个字）按槽号取实现。
                // （实参压栈与间接调用在后面**共用**的那段里发，那里才知道实参个数。）
                if (recvCls != null && VirtualSlots(recvCls.Name).TryGetValue(me.Member, out int slot))
                {
                    virtualSlot = slot;
                }
                else
                {
                    funcName = MethodSymbolForCall(me.Object, me.Member, ce.Arguments.Count);
                }
                // ⚠ `this` 要的是**对象地址**，不是对象的值：`GenerateExpr` 对类类型的变量
                //   发的是值加载（`move @R0 [var_k]`），压进去就成了"对象的第一个字段当指针"。
                GenerateMemberBase(me);
                Add(OpCode.PUSH, "R0");
                hasThis = true;
            }

            // Look up function signature for reference-parameter handling
            _functionDecls.TryGetValue(funcName, out FunctionDecl? calleeDecl);

            // 模板函数实例化: template<T> T add(T a,T b){...} 在调用点按实参类型实例化
            if (calleeDecl == null && !string.IsNullOrEmpty(funcName)
                && _templateFunctions.TryGetValue(funcName, out var tplFn))
            {
                var concrete = InstantiateTemplateFunction(tplFn, ce.Arguments);
                if (concrete != null)
                {
                    _functionDecls[funcName] = concrete;
                    calleeDecl = concrete;
                }
            }

            // 跨语言调用列表 (extern/FFI + 标准库)
            var crossLangFuncs = new HashSet<string>
            {
                "getconfig", "print_int", "print_hex", "print_str",
                "putchar", "newline", "random", "get_date",
                "get_time", "exit", "putchar", "println_str",
                "print_str", "input_int", "print_newline",
                "peek", "poke", "peekb", "pokeb",
                "alloc", "free", "str_len", "str_at", "str_cmp", "str_concat",
                "vector_grow",
                // FFI
                "get_platform", "dl_open", "dl_sym", "dl_close", "native_call", "native_call_ex",
            };

            // 判断是否为 extern/FFI 调用
            bool isExternCall = crossLangFuncs.Contains(funcName) || !_definedFunctions.Contains(funcName);

            // 确定调用约定
            CallingConvention callConv = calleeDecl?.Convention ?? CallingConvention.Cdecl;

            // ══ 统一调用约定（2026-09-17）════
            //
            // 一条循环取代原来的 extern / stdcall / fastcall / cdecl **四条分流**：
            //   **全部实参右到左压栈、一个都不走寄存器、调用方清栈**，
            //   压完之后把 arg0..arg3 镜像进 R0-R3。
            //
            // 原来的四条里 extern 与 stdcall 是「左到右 + 被调方清栈」、cdecl 是「右到左 +
            // 调用方清栈」、fastcall 是混的 —— **同一门语言里三套约定**，调用点与被调方
            // 一旦对不上就是静默的实参错位或栈漂移。
            // 实测（`scripts/vml-abi-probe/langs/abi.cpp`）：`ipow(2, 3)` 得 **9**
            // （= `ipow(3, 2)`，实参整体反序），而同一份程序在 C 前端得 **8**。
            //
            // ⚠ 镜像必须**在所有实参求值完成之后**。表达式生成器拿 R0 当暂存，
            //   原来那句 `if (i > 0) MOVE Ri, R0` 写在压栈循环**里面**：
            //   i=3 装好 R3 之后还会被 i=2/1/0 的求值冲掉 —— R1-R3 从来没镜像对过。
            //   （与 C 前端 `CodeGenerator.Expressions.Calls.cs` 注释里记的
            //     「实参求值之间不能夹带寄存器装载」是同一条教训：历史伤
            //     `probe(p + 3*k, q + 3*k, 3*k)` 传出去 R1 = 第一个实参的值。）
            //
            // `__stdcall` / `__fastcall` 等修饰符**仍能被解析**，但不再影响代码生成 ——
            // 这才是「统一之后写不写声明都必须是对的」。
            for (int i = ce.Arguments.Count - 1; i >= 0; i--)
            {
                bool isRefArg = calleeDecl != null
                    && i < calleeDecl.Parameters.Count
                    && calleeDecl.Parameters[i].IsReference;
                // 结构体按值传参：被调方拿到的也是**地址**（见 CodeGenerator.cs 里
                // `isStructVal → _isReferenceVar`），所以这里同样压地址。
                bool isStructValArg = !isRefArg && calleeDecl != null
                    && i < calleeDecl.Parameters.Count
                    && GetStructSlotCount(calleeDecl.Parameters[i].Type) > 1;
                if (isRefArg || isStructValArg)
                    GenerateAddressOf(ce.Arguments[i]);
                else
                    GenerateExpr(ce.Arguments[i]);
                Add(OpCode.PUSH, "R0");
            }

            // 镜像 arg0..arg3 进 R0-R3 —— 读的是刚压好的栈，不再求值，结构上免疫被冲掉。
            // 这一份是给 `Lib` 里那 543 处内联汇编（`asm("SYSCALL #6")` 直接吃 R0）
            // 与各语言包装器（`PUSH R0 / CALL x`）用的。
            for (int i = 0; i < ce.Arguments.Count && i < 4; i++)
            {
                instructions.Add(new Instruction(OpCode.MOVE,
                    [new Operand(OperandType.REGISTER, i), new Operand(OperandType.MEMORY, $"R13+{i * 4}")],
                    instructions.Count));
            }

            // 每个实参一个 4 字节槽（与被调方 `cumOff += 4` 同一口径），方法调用的 `this`
            // 在此块之前就已压好，一并计入待清理量。
            int argsSize = (ce.Arguments.Count + (hasThis ? 1 : 0)) * 4;

            // ── 虚调用：`CALL [ [this] + 4*(槽+1) ]` ───────────────────────────
            //
            // 编译期写的是"第几个槽"，**具体是哪个实现由对象自己决定** ——
            // 这正是"基类指针调出派生实现"能成立的原因。
            // `this` 在实参**之上**（本文件开头的调用约定：this 最先压），
            // 所以它的位置是 `[R13 + 实参个数*4]`。
            if (virtualSlot >= 0)
            {
                int thisOff = ce.Arguments.Count * 4;
                // ⚠ 地址写成 `R13+0`，**不带方括号** —— `Add` 的字符串形态里
                //   `[...]` 会把方括号**含在值里**存进 MEMORY 操作数，序列化时再包一层
                //   ⇒ 编出 `[[R13+0]]` 这种读不出东西的操作数（实测当场内存越界）。
                //   下面镜像实参那段用的就是不带括号的写法。
                Add(OpCode.MOVE, "R0", $"R13+{thisOff}");       // R0 = this
                Add(OpCode.MOVE, "R0", "(R0)");                 // R0 = vptr（对象第 0 个字）
                Add(OpCode.ADD, "R0", $"#{4 * (virtualSlot + 1)}");  // 第 0 项是 typeid
                Add(OpCode.MOVE, "R0", "(R0)");                 // R0 = 实现地址
                Add(OpCode.CALL, "R0");
                if (argsSize > 0)
                    Add(OpCode.ADD, "R13", $"#{argsSize}");
                return;
            }

            // 函数指针间接调用（Callee 非简单标识符，如 fa[i](r)）
            if (string.IsNullOrEmpty(funcName))
            {
                GenerateExpr(ce.Callee);                        // R0 = function pointer value
                Add(OpCode.CALL, "R0");                        // indirect call
                if (argsSize > 0)
                    Add(OpCode.ADD, "R13", $"#{argsSize}");
                return;
            }

            string label;
            if (funcName == "main")
                label = "main";
            else if (crossLangFuncs.Contains(funcName) || !_definedFunctions.Contains(funcName))
            {
                // 检查是否为函数指针变量（局部变量或全局变量）
                string varLabel = $"var_{funcName}";
                if (_variables.TryGetValue(funcName, out int fpOffset))
                {
                    // 局部变量: LOAD R0, [R14-offset], CALL R0
                    Add(OpCode.MOVE, "R0", Vars?.FormatOffset(fpOffset) ?? $"R14-{fpOffset}");
                    Add(OpCode.CALL, "R0");
                    if (argsSize > 0)
                        Add(OpCode.ADD, "R13", $"#{argsSize}");
                    return;
                }
                if (dataSection.ContainsKey(varLabel))
                {
                    // 全局变量: LOAD R0, var_op, CALL R0
                    Add(OpCode.MOVE, "R0", varLabel);
                    Add(OpCode.CALL, "R0");
                    if (argsSize > 0)
                        Add(OpCode.ADD, "R13", $"#{argsSize}");
                    return;
                }
                label = funcName;
            }
            else
                label = $"func_{funcName}";
            Add(OpCode.CALL, label);

            // 栈清理：**一律调用方清**（原先是「cdecl/fastcall 调用方清、stdcall/extern
            // 被调方清」两条路，被调方那边见 CodeGenerator.cs 的同批改动）。
            if (argsSize > 0)
            {
                Add(OpCode.ADD, "R13", $"#{argsSize}");
            }
        }

        /// <summary>返回 class/struct 是否有虚函数，有则字段前有 vtable 指针 (4 字节)</summary>
        private static int GetVtableOffset(ClassDecl cls)
        {
            return cls.Members.Any(m => m.IsVirtual) ? 4 : 0;
        }

        /// <summary>
        /// 类（**含继承链**）里有没有虚函数 —— 有就说明对象最前面是 vtable/typeid 指针。
        /// ⚠ `GetVtableOffset(cls)` 只看当前类自己：派生类自己没写 virtual、但基类有，
        ///   布局里**照样**有那个 4 字节。两处判据不一致 ⇒ 继承来的字段偏移整体差 4。
        /// </summary>
        private bool ClassHasVirtualDeep(string className)
        {
            var seen = 0;
            while (!string.IsNullOrEmpty(className) && _classes.TryGetValue(className, out var cls) && seen < 32)
            {
                if (cls.Members.Any(m => m.IsVirtual)) return true;
                className = string.IsNullOrEmpty(cls.BaseClass) ? "" : CleanType(cls.BaseClass!);
                seen++;
            }
            return false;
        }

        /// <summary>
        /// 一个字段占几个字节 —— **唯一实现**（字段偏移步进、对象大小、跳过基类子对象
        /// 三处都问它）。
        ///
        /// <para>
        /// ⚠ 此前这三处**都写死了 `* 4`**，而**内嵌一个对象时它不是 4**
        /// （`class Team { Ape a0; Ape a1; }` 里 `Ape` 是 8 字节）。
        /// 后果不是"报错"而是**静默的布局错**：`a1` 的偏移算成 4（应为 8）⇒
        /// `a1.Set(10, 20)` 把值写进了 `a0`，而 `a0.y` 读出来是 10；
        /// 更要命的是 `Team` 的对象只分了 2 个字（应为 4），**写第二个成员就写到对象外面**。
        /// 一个对象里嵌另一个对象是类的基本用法，所以这条一直是个洞，只是没有例子踩到。
        /// </para>
        /// </summary>
        private int FieldSizeOf(ClassMember m)
        {
            string ct = CleanType(m.Type);
            if (_classes.ContainsKey(ct)) return ClassSizeDeep(ct);   // 内嵌对象
            return 4;                                                  // 标量 / 指针
        }

        /// <summary>
        /// 继承链上**所有字段**的字节数（不含 vtable 指针）—— 唯一实现。
        ///
        /// <para>
        /// ⚠ 单独抽出来是因为"vptr 算几次"必须只有一处说了算。vptr **整个对象共用一份**
        /// （它排在**最外层**的基类子对象之前），所以"跳过一个基类子对象"要加的是
        /// **字段区**的字节数，不是 <see cref="ClassSizeDeep"/> —— 后者把那一份 vptr
        /// 又算了一遍。此前没抽这一层，是因为虚表从来没生效过（`IsVirtual` 恒 false）、
        /// 两份数**碰巧相等**；虚派发一接通就露出来了：`x` 在构造里被写在偏移 8、
        /// 而 `Entity` 类型的引用读的是偏移 4 ⇒ **读回 0**。
        /// </para>
        /// </summary>
        private int FieldBytesDeep(string className)
        {
            int size = 0;
            var seen = 0;
            string? c = className;
            while (!string.IsNullOrEmpty(c) && _classes.TryGetValue(c!, out var cls) && seen < 32)
            {
                foreach (var m in cls.Members)
                    if (!m.IsMethod && !m.IsConstructor && !m.IsDestructor)
                        size += FieldSizeOf(m);
                c = string.IsNullOrEmpty(cls.BaseClass) ? null : CleanType(cls.BaseClass!);
                seen++;
            }
            return size;
        }

        /// <summary>对象占多少字节（**含基类子对象**、含 vtable 指针）—— 唯一实现。</summary>
        private int ClassSizeDeep(string className)
        {
            int size = ClassHasVirtualDeep(className) ? 4 : 0;
            var seen = 0;
            while (!string.IsNullOrEmpty(className) && _classes.TryGetValue(className, out var cls) && seen < 32)
            {
                foreach (var m in cls.Members)
                    if (!m.IsMethod && !m.IsConstructor && !m.IsDestructor)
                        size += FieldSizeOf(m);
                // 基类子对象在前：它的字节数已经含在上面那次 +4 里了吗？不含 —— 递归累加
                className = string.IsNullOrEmpty(cls.BaseClass) ? "" : CleanType(cls.BaseClass!);
                seen++;
            }
            return Math.Max(4, size);
        }

        /// <summary>
        /// 字段在对象里的**字节偏移** —— **唯一实现**（`obj.field`、`this->field`、
        /// 隐式 `field` 三条路都问它）。
        ///
        /// 布局：`[vtable 指针?] [基类子对象] [本类字段…]`（标准 C++ 顺序）。
        /// ⚠ 原先这条规则在 <see cref="GenerateMemberExpr"/> 与
        ///   <see cref="GenerateMemberAddress"/> 里**各写了一遍**，而且两处都只遍历
        ///   当前类自己的成员 ⇒ **继承来的字段偏移算错**（`a.baseField` 会读到派生类的字段，
        ///   两者都不报错，只是值不对）。
        /// </summary>
        private bool TryFieldOffset(string className, string fieldName, out int offset)
        {
            // vptr **只算这一次**（整个对象共用一份，排在基类子对象之前）；
            // 往下走到基类里找时**不能再算**，见 `FieldBytesDeep` 的注释。
            offset = ClassHasVirtualDeep(className) ? 4 : 0;
            return TryFieldOffsetWalk(className, fieldName, ref offset);
        }

        /// <summary>沿继承链走，把已经数出来的 <paramref name="offset"/> 接着往下加。</summary>
        private bool TryFieldOffsetWalk(string className, string fieldName, ref int offset)
        {
            var seen = 0;
            string? c = className;
            while (!string.IsNullOrEmpty(c) && _classes.TryGetValue(c!, out var cls) && seen < 32)
            {
                // 基类子对象排在前面：先到基类里找，找不到就把整个基类子对象跳过
                if (!string.IsNullOrEmpty(cls.BaseClass))
                {
                    string baseName = CleanType(cls.BaseClass!);
                    if (_classes.ContainsKey(baseName))
                    {
                        int probe = offset;
                        if (TryFieldOffsetWalk(baseName, fieldName, ref probe))
                        {
                            offset = probe;
                            return true;
                        }
                        offset += FieldBytesDeep(baseName);   // **不含** vptr（对象只有一份）
                    }
                }
                foreach (var m in cls.Members)
                {
                    if (m.IsMethod || m.IsConstructor || m.IsDestructor) continue;
                    if (m.Name == fieldName) return true;
                    offset += FieldSizeOf(m);   // ⚠ 不是 `+= 4`：内嵌对象占好几个字
                }
                return false;   // 基类那一支已经 return，这里只可能走到"本类里没有"
            }
            return false;
        }

        /// <summary>形参类型 → 一个字符的修饰码（方法符号名用）。</summary>
        private static string TypeCode(string t) => t switch
        {
            "float" => "f",
            "double" => "d",
            "char" => "c",
            "bool" => "b",
            "int" or "long" or "short" or "unsigned" or "unsigned int" => "i",
            _ => t.EndsWith("*") ? "p" : "v",
        };

        /// <summary>
        /// 方法符号名 —— **调用侧与定义侧共用的唯一算法**。
        ///
        /// ⚠ 此前两边各有一套、而且**永远对不上**：
        ///     定义侧 `method_{类名}_{方法名}`（无后缀）
        ///     调用侧 `MangleName($"method_{**变量名**}_{方法名}", 实参)`（有后缀）
        ///   ⇒ 任何带参方法都报「未定义的函数 'method_t_Set_i'」。
        ///
        /// 更麻烦的是旧的后缀取自**实参表达式的种类**（字面量 `3`→`i`、变量 `x`→`v`）
        /// ⇒ 同一个方法按你传字面量还是传变量，会去找**两个不同的符号**，
        /// 那是"把名字对齐"修不好的。现在改成按**形参类型**修饰 —— 两边都算得出来。
        /// 类名取**声明该方法的那个类**（含继承链查找），不是接收者变量的静态类型。
        /// </summary>
        private string MethodSymbol(string className, string methodName, IEnumerable<string> paramTypes)
        {
            string suffix = "";
            foreach (var t in paramTypes) suffix += TypeCode(CleanType(t));
            return suffix.Length > 0
                ? $"method_{className}_{methodName}_{suffix}"
                : $"method_{className}_{methodName}";
        }

        /// <summary>
        /// 沿继承链找方法，返回**声明它的那个类**与方法声明 —— 两者都要，因为符号名用的是
        /// 声明处的类名（`Ape a; a.Base()` 里 `Base` 声明在 `Entity` ⇒ 符号名是
        /// `method_Entity_Base`，不是 `method_Ape_Base`）。
        /// </summary>
        private bool FindMethodDeep(string className, string methodName, int argCount, out string owner, out FunctionDecl? decl)
        {
            owner = className;
            decl = null;
            var seen = 0;
            while (!string.IsNullOrEmpty(className) && _classes.TryGetValue(className, out var cls) && seen < 32)
            {
                var m = cls.Members.FirstOrDefault(x => x.IsMethod && x.Method != null
                                                        && x.Method.Name == methodName
                                                        && x.Method.Parameters.Count == argCount)
                     ?? cls.Members.FirstOrDefault(x => x.IsMethod && x.Method != null && x.Method.Name == methodName);
                if (m?.Method != null) { owner = className; decl = m.Method; return true; }
                className = string.IsNullOrEmpty(cls.BaseClass) ? "" : CleanType(cls.BaseClass!);
                seen++;
            }
            return false;
        }

        /// <summary>方法调用点的符号名：按接收者的类沿继承链找到声明处，再用形参类型修饰。</summary>
        private string MethodSymbolForCall(Expr objExpr, string methodName, int argCount)
        {
            var cls = ResolveClassOf(objExpr);
            string className = cls?.Name ?? (objExpr is IdentExpr oid ? oid.Name : "");
            if (FindMethodDeep(className, methodName, argCount, out string owner, out var decl))
                return MethodSymbol(owner, methodName, decl!.Parameters.Select(p => p.Type));
            return MethodSymbol(className, methodName, Enumerable.Empty<string>());
        }

        /// <summary>
        /// 沿继承链找**字段的声明**（`obj.a0` / 隐式 `a0` 两条路都要它的类型）。
        ///
        /// <para>
        /// `ResolveClassOf` 原先只认"变量"（`_varTypes`）—— 而**字段**不在 `_varTypes` 里，
        /// 于是"一个对象里嵌另一个对象"（`Team { Ape a0; }` 里 `a0.Total()`）解析不出接收者的类，
        /// 符号名退化成拿**字段名**当类名（`method_a0_Total`）⇒ 链接期"未定义的函数"。
        /// </para>
        /// </summary>
        private bool FindFieldDeep(string className, string fieldName, out string fieldType)
        {
            fieldType = "";
            var seen = 0;
            string? c = className;
            while (!string.IsNullOrEmpty(c) && _classes.TryGetValue(c!, out var cls) && seen < 32)
            {
                var fm = cls.Members.FirstOrDefault(m => !m.IsMethod && !m.IsConstructor && !m.IsDestructor
                                                         && m.Name == fieldName);
                if (fm != null) { fieldType = fm.Type; return true; }
                c = string.IsNullOrEmpty(cls.BaseClass) ? null : CleanType(cls.BaseClass!);
                seen++;
            }
            return false;
        }

        /// <summary>
        /// 隐式的 `this->字段`、且这个字段**本身是对象**（不是指针）——
        /// 取它的地址才是 `obj.field` 那条链要的基址。
        ///
        /// <para>
        /// ⚠ 与"字段是指向对象的**指针**"（`Ape* p`）要分开：那种要的是指针的**值**。
        /// 两者混了就是"把一个对象的内容当指针用"，读到的是一片别的内存。
        /// </para>
        /// </summary>
        private bool IsImplicitThisClassField(IdentExpr id, out MemberExpr member)
        {
            member = null!;
            if (!IsImplicitThisField(id, out var m)) return false;
            if (!FindFieldDeep(_currentClass!, id.Name, out string ft)) return false;
            if (!_classes.ContainsKey(CleanType(ft))) return false;    // `Ape*` 不在此列
            member = m;
            return true;
        }

        /// <summary>清理类型字符串: 去掉 struct/union/class 前缀</summary>
        private static string CleanType(string type)
        {
            if (string.IsNullOrEmpty(type)) return type;
            return type.Replace("struct ", "").Replace("union ", "").Replace("class ", "").Trim();
        }

        /// <summary>Resolve the ClassDecl from an expression's type (handles ptr-to-class too)</summary>
        private ClassDecl? ResolveClassOf(Expr expr)
        {
            // ⚠ `this` **必须**在这一列里。原先没有这一支，于是 `this->b` 一路返回 null：
            //   `GenerateMemberAddress`/`GenerateMemberExpr` 拿不到类，字段偏移那条
            //   `if (fieldOffset != 0) ADD` 整个被跳过 ⇒ **`this->b` 读写的是 `this->a`**
            //   （偏移恒 0）。只写一个字段的类看不出来 —— 而本仓的类例子恰好都是那种，
            //   所以这个洞一直活着。显式 `this->f` 与隐式 `f` 是两条路（后者走
            //   `_currentClass`），两边必须都能定位到同一个字段。
            if (expr is ThisExpr)
            {
                if (!string.IsNullOrEmpty(_currentClass)
                    && _classes.TryGetValue(_currentClass!, out var thisCls))
                    return thisCls;
                return null;
            }
            if (expr is IdentExpr ie)
            {
                if (_classes.TryGetValue(ie.Name, out var directCls))
                    return directCls;
                if (_varTypes.TryGetValue(ie.Name, out var vt))
                {
                    string clean = CleanType(vt);
                    if (_classes.TryGetValue(clean, out var cls))
                        return cls;
                    // Pointer to class: e.g. "struct Pt*" → "Pt"
                    if (clean.EndsWith("*") && _classes.TryGetValue(clean.TrimEnd('*').Trim(), out var ptrCls))
                        return ptrCls;
                }
                // ── 隐式的 `this->字段` ──
                // 字段不在 `_varTypes` 里，上面两条都查不到 ⇒ 一个对象里嵌另一个对象时
                // （`Team { Ape a0; }` 里的 `a0.Total()`）接收者解析不出类。
                // 这条放在**最后**：局部量/变量优先于字段（字段在 `_varTypes` 里本来就没有，
                // 顺序上只是把"先查变量"的既有语义写清楚）。
                if (IsImplicitThisField(ie, out _)
                    && FindFieldDeep(_currentClass!, ie.Name, out string fieldType))
                {
                    string ft = CleanType(fieldType);
                    if (_classes.TryGetValue(ft, out var fieldCls))
                        return fieldCls;
                    if (ft.EndsWith("*") && _classes.TryGetValue(ft.TrimEnd('*').Trim(), out var fieldPtrCls))
                        return fieldPtrCls;
                }
            }
            else if (expr is UnaryExpr ue && ue.Op == "*")
            {
                // (*ptr).member — 解引用指针，获取指向类型的类定义
                return ResolveClassOf(ue.Operand);
            }
            else if (expr is MemberExpr me)
            {
                // Nested access: resolve inner member's class, then look up the field's type
                var outerCls = ResolveClassOf(me.Object);
                if (outerCls != null)
                {
                    var fm = outerCls.Members.FirstOrDefault(m => !m.IsMethod && m.Name == me.Member);
                    if (fm != null)
                    {
                        string ft = CleanType(fm.Type);
                        if (_classes.TryGetValue(ft, out var fcls))
                            return fcls;
                        if (ft.EndsWith("*") && _classes.TryGetValue(ft.TrimEnd('*').Trim(), out var fpcls))
                            return fpcls;
                    }
                }
            }
            else if (expr is BinaryExpr be && be.Op == "[]")
            {
                // Array subscript: resolve the element type (e.g. arr[i] where arr is struct Pt[])
                if (be.Left is IdentExpr arrId && _varTypes.TryGetValue(arrId.Name, out var arrType))
                {
                    string clean = CleanType(arrType);
                    if (_classes.TryGetValue(clean, out var arrCls))
                        return arrCls;
                    // ⚠ **指针数组**：`Entity* list[4]; list[0]->Value()` ——
                    //   类型串是 `Entity*`，去掉星号才是类名。漏了这一支，
                    //   `list[i]` 就解析不出接收者的类 ⇒ 调用点拿不到 `funcName`、
                    //   退化成"把对象指针当函数指针"的**间接调用**（`call R0`），
                    //   而 R0 里装的是对象地址 ⇒ 跳到数据上执行。
                    //   症状是"程序跑完但一行输出都没有"，且**编译期零报错**。
                    if (clean.EndsWith("*") && _classes.TryGetValue(clean.TrimEnd('*').Trim(), out var arrPtrCls))
                        return arrPtrCls;
                }
            }
            return null;
        }

        /// <summary>Is the expression a pointer to a class type (needs value-as-address)?</summary>
        private bool IsPtrToClass(IdentExpr ie)
        {
            if (_varTypes.TryGetValue(ie.Name, out var vt))
            {
                string clean = CleanType(vt);
                return clean.EndsWith("*") && _classes.ContainsKey(clean.TrimEnd('*').Trim());
            }
            return false;
        }

        /// <summary>Is the expression a direct class-typed variable (needs address)?</summary>
        private bool IsClassTyped(IdentExpr ie)
        {
            if (_varTypes.TryGetValue(ie.Name, out var vt))
                return _classes.ContainsKey(CleanType(vt));
            return false;
        }

        /// <summary>
        /// 成员访问的**基址** —— `obj.field` 与 `ptr->field` 的唯一分界。
        ///
        /// <list type="bullet">
        /// <item><c>.</c>（<c>Arrow == false</c>）：<c>obj</c> 是**对象本身**，要它的**地址**；</item>
        /// <item><c>-&gt;</c>（<c>Arrow == true</c>）：<c>obj</c> 是**指针**，要它存的**值**。</item>
        /// </list>
        ///
        /// <para>
        /// ⚠ 原先一律走 <see cref="GenerateBaseForMember"/>，而它按"对象的地址"算 ——
        /// `list[i]->m()` 于是把**元素的地址**当成了对象指针（`&list[i]` 而不是 `list[i]`），
        /// 读到的是一片别处。`. ` 那条没错（`GenerateBaseForMember` 对指针类型的变量
        /// 本来就发值加载），所以只有"指针来自表达式"（数组元素、`(*p)`、嵌套成员）
        /// 时才错 —— 而又只有 `->` 才需要区分，`Arrow` 这个位 AST 里一直有。
        /// </para>
        /// </summary>
        private void GenerateMemberBase(MemberExpr me)
        {
            if (me.Arrow)
                GenerateExpr(me.Object);        // `->`：对象是**指针**，取它的值
            else
                GenerateBaseForMember(me.Object); // `.`：对象本身，取它的地址
        }

        /// <summary>Generate base for member access: address for class-typed vars, value for ptr-to-class</summary>
        private void GenerateBaseForMember(Expr obj)
        {
            if (obj is IdentExpr ie)
            {
                // 隐式 `this->字段`、且字段本身是对象 ⇒ 要的是**字段的地址**
                // （与"类类型的变量"同一条处理）。漏了这一支会落到 `GenerateExpr`，
                // 那条路把字段的**值**（对象第一个字）当 this 用。
                if (IsImplicitThisClassField(ie, out var thisField))
                {
                    GenerateMemberAddress(thisField);
                    return;
                }
                bool needAddr = _classes.TryGetValue(ie.Name, out _) || IsClassTyped(ie);
                if (needAddr)
                    GenerateAddressOf(ie);
                else
                    GenerateExpr(ie);
            }
            else if (obj is MemberExpr innerMe)
            {
                GenerateMemberAddress(innerMe);
            }
            else if (obj is BinaryExpr be && be.Op == "[]")
            {
                // Array element address (without final LOAD — caller adds field offset)
                GenerateExpr(be.Left);     // R0 = array base address
                Add(OpCode.PUSH, "R0");
                GenerateExpr(be.Right);    // R0 = index
                Add(OpCode.MOVE, "R1", "R0");
                Add(OpCode.POP, "R0");     // R0 = base
                // Determine element stride
                int stride = 4;
                if (be.Left is IdentExpr arrId0 && _byteArrays.Contains(arrId0.Name))
                    stride = 1;                                        // 字节数组（见 ArrayIndexInfo）
                else if (be.Left is IdentExpr arrId && _varTypes.TryGetValue(arrId.Name, out var arrType))
                {
                    if (_classes.TryGetValue(CleanType(arrType), out var arrCls))
                    {
                        // ⚠ 元素步长**必须**与"分配时按多大一个元素算"是**同一个数**。
                        //   分配那边用的是 `ClassSizeDeep`（见 `GenerateAssignExpr` 的 `objWords`），
                        //   这里原先写的是"本类字段数 × 4" —— 两者**不相等**：
                        //   `ClassSizeDeep` 把**基类子对象**和 **vptr** 一起算进去，字段数只数本类。
                        //   于是 `Tree TREES[6]`（4 个 int = 16 字节）按 16 分配、按 **20** 步进
                        //   ⇒ 写到第 4 个元素就越过数组尾巴、**踩到别的全局量**上。
                        //   本仓头号坑"同一份数据两处实现"，这里是它的内存版。
                        stride = ClassSizeDeep(arrCls.Name);
                        if (stride < 4) stride = 4;
                    }
                }
                if (stride == 4) Add(OpCode.SHL, "R1", "#2");
                else if (stride == 8) Add(OpCode.SHL, "R1", "#3");
                else { Add(OpCode.MOVE, "R2", $"#{stride}"); Add(OpCode.MUL, "R1", "R1", "R2"); }
                // 与读/写**同一套判据**（见 `ArrayIndexInfo`）：字节数组没有长度头
                if (ArrayIndexInfo(be.Left, isNested: false).HasHeader)
                    Add(OpCode.ADD, "R1", "#4");  // +4 (skip VML array length header)
                Add(OpCode.ADD, "R0", "R1");  // R0 = &arr[i]
            }
            else if (obj is UnaryExpr ue && ue.Op == "*")
            {
                // (*ptr).member — dereference the pointer to get the address
                GenerateExpr(ue.Operand);
            }
            else
            {
                GenerateExpr(obj);
            }
        }

        private void GenerateMemberExpr(MemberExpr me)
        {
            GenerateMemberBase(me);
            var cls = ResolveClassOf(me.Object);
            if (cls != null)
            {
                // 同上：查不到时 `out` 里留的是 vptr 那 4 个字节，照着用就是整体偏一个字
                int fieldOffset = TryFieldOffset(cls.Name, me.Member, out int fo) ? fo : 0;
                Add(OpCode.MOVE, "R1", "R0");
                Add(OpCode.MOVE, "R0", $"{fieldOffset}(R1)");
            }
            else
            {
                Add(OpCode.MOVE, "R0", "(R0)");
            }
        }

        /// Compute the address of a member field into R0
        private void GenerateMemberAddress(MemberExpr me)
        {
            GenerateMemberBase(me);
            var cls = ResolveClassOf(me.Object);
            if (cls != null)
            {
                // ⚠ 判**返回值**，不能只看 `fieldOffset`：查不到时那个 `out` 里留的是
                //   "vptr 占的 4"（`offset` 的初值），照着加就会**整体偏一个字**，
                //   而症状是"读到了隔壁字段"这种看着像逻辑错的值。
                if (TryFieldOffset(cls.Name, me.Member, out int fieldOffset) && fieldOffset != 0)
                    Add(OpCode.ADD, "R0", $"#{fieldOffset}");
            }
        }

        /// <summary>Do a field-by-field copy from src global var to dst global var</summary>
        private void EmitStructFieldCopy(string srcLabel, string dstLabel, int fieldCount)
        {
            for (int i = 0; i < fieldCount; i++)
            {
                int off = i * 4;
                // Load src field address
                instructions.Add(new Instruction(OpCode.MOVE, [
                    new(OperandType.REGISTER, 0),
                    new(OperandType.LABEL, srcLabel)
                ]));
                if (off > 0) Add(OpCode.ADD, "R0", $"#{off}");
                Add(OpCode.MOVE, "R1", "(R0)");
                // Store to dst field
                instructions.Add(new Instruction(OpCode.MOVE, [
                    new(OperandType.REGISTER, 0),
                    new(OperandType.LABEL, dstLabel)
                ]));
                if (off > 0) Add(OpCode.ADD, "R0", $"#{off}");
                Add(OpCode.MOVE, "(R0)", "R1");
            }
        }

        /// <summary>
        /// 虚函数**槽位表**（方法名 → 槽号）—— 槽号在整条继承链上**稳定**：
        /// 基类的虚函数占前面的槽，派生类的**重写占同一个槽**（C++ 的规则，也正是
        /// "基类指针调出派生实现"能成立的原因），派生类新增的排在其后。
        ///
        /// <para>
        /// 定义侧（生成虚表）与调用侧（算槽号）**共用这一份** —— 两边各算一遍就是
        /// 本仓头号坑"同一规则两处实现"，而这里的症状会是"调到了隔壁那个方法"，
        /// 比"没调到"更难查。按**方法名**匹配（不按签名）：这个前端没有重载解析。
        /// </para>
        /// </summary>
        private Dictionary<string, int> VirtualSlots(string className)
        {
            // 继承链**从根往下**排，保证基类先占槽
            var chain = new List<ClassDecl>();
            int seen = 0;
            string? c = className;
            while (!string.IsNullOrEmpty(c) && _classes.TryGetValue(c!, out var cls) && seen < 32)
            {
                chain.Insert(0, cls);
                c = string.IsNullOrEmpty(cls.BaseClass) ? null : CleanType(cls.BaseClass!);
                seen++;
            }

            var slots = new Dictionary<string, int>();
            foreach (var cls in chain)
                foreach (var m in cls.Members)
                    if (m.IsVirtual && m.Method != null && !slots.ContainsKey(m.Method.Name))
                        slots[m.Method.Name] = slots.Count;
            return slots;
        }

        /// <summary>
        /// <paramref name="className"/> 这个类在 <paramref name="methodName"/> 这个槽里
        /// **实际生效**的实现符号 —— 本类重写了就是本类的，否则沿继承链往上找第一个声明的。
        ///
        /// <para>返回 null = 整条链上都没有这个虚函数（调用方不该走到这里）。</para>
        /// </summary>
        private string? VirtualSlotSymbol(string className, string methodName)
        {
            int seen = 0;
            string? c = className;
            while (!string.IsNullOrEmpty(c) && _classes.TryGetValue(c!, out var cls) && seen < 32)
            {
                var m = cls.Members.FirstOrDefault(x => x.IsVirtual && x.Method != null && x.Method.Name == methodName);
                if (m != null)
                    return MethodSymbol(cls.Name, m.Method!.Name, m.Method.Parameters.Select(p => p.Type));
                c = string.IsNullOrEmpty(cls.BaseClass) ? null : CleanType(cls.BaseClass!);
                seen++;
            }
            return null;
        }

        /// <summary>
        /// 「这个表达式是**隐式的 `this->字段`** 吗」—— 唯一判据。
        ///
        /// <para>
        /// 方法体里裸写字段名（不写 `this->`）时走这一支。读那条路
        /// （<see cref="GenerateExpr"/> 的 `IdentExpr`）早就有它；写这条原先没有，
        /// 于是同一个 `v` 读的是 `this->v`、写的是全局 `var_v` —— 值写进去读不出来。
        /// </para>
        /// <para>
        /// 三条前提，缺一不可：
        /// <list type="number">
        /// <item>正处于某个类的方法体里（`_currentClass` 非空）；</item>
        /// <item>**不是局部量** —— 局部量遮蔽同名字段（C++ 的作用域规则）；</item>
        /// <item>名字确实是当前类（含基类）的字段 —— 否则它就是个普通全局量。</item>
        /// </list>
        /// 命中时给出等价的显式 `this->字段`，交给**已经存在**的成员读写路径去处理
        /// （偏移含继承，见 <see cref="GenerateMemberAddress"/>）。
        /// </para>
        /// </summary>
        private bool IsImplicitThisField(Expr e, out MemberExpr member)
        {
            member = null!;
            if (e is not IdentExpr id) return false;
            if (string.IsNullOrEmpty(_currentClass)) return false;
            if (_variables.ContainsKey(id.Name)) return false;
            if (!_classes.ContainsKey(_currentClass!)) return false;
            if (!TryFieldOffset(_currentClass!, id.Name, out _)) return false;
            member = new MemberExpr { Object = new ThisExpr(), Member = id.Name };
            return true;
        }

        /// <summary>
        /// 按**实参个数**挑一个构造函数；没有就返回 null（"这类没有用户构造函数"，
        /// 调用方沿旧行为处理）。
        ///
        /// <para>
        /// 只按个数匹配，不按类型：这个前端没有"实参 → 形参"的类型推导（`InferExpType`
        /// 给的是枚举、不是类型名），而按个数匹配已经能覆盖"重载只在参数个数上不同"的
        /// 绝大多数写法。同名同个数不同类型两个构造函数会挑到先声明的那个 ——
        /// 这是个**已知的**局限，不是"碰巧对"。
        /// </para>
        /// <para>
        /// 只看本类，**不往基类找**：C++ 不继承构造函数（派生类的构造函数得自己调基类的），
        /// 往基类找会把 `Derived d;` 接到 `Base()` 上去，那比不调更糟。
        /// </para>
        /// </summary>
        private static FunctionDecl? FindCtor(ClassDecl cls, int argCount)
            => cls.Members.FirstOrDefault(m => m.IsConstructor && m.Method != null
                                               && m.Method.Parameters.Count == argCount)?.Method;

        /// <summary>
        /// 发一次构造函数调用 —— 调用约定与 `GenerateCallExpr` 的方法调用那条**逐位相同**：
        /// `this` **最先**压（于是它在最上面，被调方按 `12 + 4×形参个数` 取），
        /// 然后实参右到左压，再镜像 arg0..3 进 R0-R3，最后调用方清栈。
        ///
        /// <para>
        /// ⚠ 清栈量是 `(实参个数 + 1) × 4` —— 那个 `+1` 是 `this`。漏了它每建一个对象
        /// 栈指针就漂 4 字节，症状是"对象建到第二个之后值开始不对"。
        /// </para>
        /// </summary>
        /// <summary>
        /// 构造 <paramref name="objLabel"/> 这个对象的**成员对象**（C++ 的隐式成员构造）。
        ///
        /// <para>
        /// ⚠ 原先整个是缺的：`class Game { Sky sky; Ape a0; }` 里那几个成员**从来没被构造过** ——
        /// 构造 `Game` 只会把**它自己的标量字段**清零。后果有两级：
        /// <list type="bullet">
        /// <item>成员的字段全是数据段的 0（构造函数体白写了）；</item>
        /// <item>**成员的 vptr 也是 0** —— 于是 `&成员` 存进基类指针数组、
        ///   再 `p->Draw()` 时，`[vptr]` 取出 0、`call [0+4]` 跳到地址 4 上。
        ///   实测的表现是"程序正常结束、屏幕上什么都没有、一句报错都没有"，
        ///   而所有 `Draw()` 里插的探针**一个都没响**（调用根本没进函数）。</item>
        /// </list>
        /// </para>
        /// <para>
        /// 递归往下做（成员里还嵌着成员也一样），并在有虚函数时**顺手写 vptr** ——
        /// 与构造点那句同源（`GenerateAssignExpr` 的类分支）。**只有这一处实现**：
        /// 成员构造不放进构造函数体里，否则"有没有用户构造函数"又会分叉成两条路。
        /// </para>
        /// </summary>
        private void EmitMemberCtorCalls(string objLabel, string className, int baseOff)
        {
            if (!_classes.TryGetValue(className, out var cls)) return;

            int off = 0;
            foreach (var m in cls.Members)
            {
                if (m.IsMethod || m.IsConstructor || m.IsDestructor) continue;
                int size = FieldSizeOf(m);
                string mt = CleanType(m.Type);
                if (_classes.ContainsKey(mt))
                {
                    int fieldOff = baseOff + off;
                    if (FieldHasVptrBackedType(mt))
                    {
                        // R1 = 成员地址；R0 = 它的虚表；*R1 = R0
                        instructions.Add(new Instruction(OpCode.MOVE, [
                            new(OperandType.REGISTER, 1), new(OperandType.LABEL, objLabel)]));
                        if (fieldOff > 0) Add(OpCode.ADD, "R1", $"#{fieldOff}");
                        instructions.Add(new Instruction(OpCode.MOVE, [
                            new(OperandType.REGISTER, 0), new(OperandType.LABEL, $"{mt}_vtable")]));
                        Add(OpCode.MOVE, "(R1)", "R0");
                    }
                    var ctor = FindCtor(_classes[mt], 0);
                    if (ctor != null)
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, [
                            new(OperandType.REGISTER, 0), new(OperandType.LABEL, objLabel)]));
                        if (fieldOff > 0) Add(OpCode.ADD, "R0", $"#{fieldOff}");
                        Add(OpCode.PUSH, "R0");
                        Add(OpCode.CALL, CtorSymbol(mt, ctor.Parameters.Select(p => p.Type)));
                        Add(OpCode.ADD, "R13", "#4");
                    }
                    EmitMemberCtorCalls(objLabel, mt, fieldOff);
                }
                off += size;
            }
        }

        /// <summary>—— 只为可读性：这个类型是不是"有虚表的那一类"。</summary>
        private bool FieldHasVptrBackedType(string typeName) => ClassHasVirtualDeep(typeName);

        private void EmitCtorCall(string objLabel, string className, FunctionDecl ctor, List<Expr>? args)
        {
            args ??= new List<Expr>();

            // this = 对象地址（LABEL 操作数取的是**地址**，不是那一格的内容）
            instructions.Add(new Instruction(OpCode.MOVE, [
                new(OperandType.REGISTER, 0),
                new(OperandType.LABEL, objLabel)
            ]));
            Add(OpCode.PUSH, "R0");

            for (int i = args.Count - 1; i >= 0; i--)
            {
                GenerateExpr(args[i]);
                Add(OpCode.PUSH, "R0");
            }

            // 镜像 arg0..arg3 —— 与 GenerateCallExpr 同一份理由（`Lib` 里的内联汇编吃 R0）
            for (int i = 0; i < args.Count && i < 4; i++)
            {
                instructions.Add(new Instruction(OpCode.MOVE,
                    [new Operand(OperandType.REGISTER, i), new Operand(OperandType.MEMORY, $"R13+{i * 4}")],
                    instructions.Count));
            }

            Add(OpCode.CALL, CtorSymbol(className, ctor.Parameters.Select(p => p.Type)));
            Add(OpCode.ADD, "R13", $"#{(args.Count + 1) * 4}");
        }

        private void GenerateAssignExpr(AssignExpr ae)
        {
            // ── 隐式 `this->field` 的**写**：先降级成显式 `this->field`，再走下面那条
            //    **已经存在**的成员赋值路径（`ae.Target is MemberExpr`）─────────────
            //
            // ⚠ 读那条路（`GenerateExpr` 的 `IdentExpr`）早就有这一级，**写**这条原先没有：
            //   方法体里写 `v = x;` 一路落到最后那句 `Add(varStoreOp, "var_v", "R0")` ——
            //   发一条写**全局** `var_v` 的指令，而同一句 `v` 当成**读**时读的是 `this->v`
            //   ⇒「写进去的值读不出来」。指令一条不缺、没有任何报错，症状与
            //   v0.96.453 那条（`this` 槽没写）**一模一样**，所以这次把两处一起钉住。
            //
            // 降级而不是另写一遍：成员赋值那条路已经处理了多级继承的字段偏移
            // （`GenerateMemberAddress`），另写一份就是本仓头号坑"同一规则两处实现"。
            //
            // 三条前提（见 `IsImplicitThisField`）：`DeclType` 为空（这是**赋值**不是**声明** ——
            // `int v = 3;` 要老老实实声明一个局部量）、不是局部量、名字确实是字段。
            //
            // 复合赋值走**同一条**降级（`v += e` → `v = v + e`）：不降级的话它会去
            // `EmitCompoundAssign(WrapTargetExpr(…))`，而 `WrapTargetExpr` 只认
            // `Stack`/`Data` 两种落点，字段在那里被当成全局 `var_v` —— 又是"读了 this、
            // 写了全局"。降级后左右两边都是普通表达式、由既有路径处理。
            // ⚠ `ae.Value` 在新树里**只出现一次** ⇒ 不会被求值两遍（`v += f()` 只调一次）。
            if (string.IsNullOrEmpty(ae.DeclType) && IsImplicitThisField(ae.Target, out var implMem))
            {
                Expr value = ae.Value;
                if (ae.Op != "=")
                {
                    value = new BinaryExpr
                    {
                        Left = implMem,
                        Op = ae.Op.TrimEnd('='),
                        Right = ae.Value,
                    };
                }
                GenerateAssignExpr(new AssignExpr { Target = implMem, Op = "=", Value = value });
                return;
            }

            // Struct-to-struct copy: detect BEFORE GenerateExpr consumes the value
            if (ae.Op == "=" && ae.Target is IdentExpr tId && ae.Value is IdentExpr srcId
                && IsClassTyped(srcId))
            {
                string? st = ae.DeclType;
                if (string.IsNullOrEmpty(st)) _varTypes.TryGetValue(tId.Name, out st);
                if (!string.IsNullOrEmpty(st) && _classes.TryGetValue(CleanType(st), out var copyCls))
                {
                    string lbl = $"var_{tId.Name}";
                    if (!_varTypes.ContainsKey(tId.Name)) _varTypes[tId.Name] = st;
                    int fc = copyCls.Members.Count(m => !m.IsMethod);
                    if (!dataSection.ContainsKey(lbl))
                        dataSection[lbl] = fc > 1 ? new int[fc] : 0;
                    EmitStructFieldCopy($"var_{srcId.Name}", lbl, fc);
                    Add(OpCode.MOVE, "R0", lbl);
                    return;
                }
            }

            if (ae.Op == "=")
            {
                GenerateExpr(ae.Value);
            }
            else
            {
                _expr!.EmitCompoundAssign(WrapTargetExpr(ae.Target), WrapExpr(ae.Value), ae.Op.TrimEnd('='));
                return;
            }
            if (ae.Target is IdentExpr ie && _variables.TryGetValue(ie.Name, out int offset))
            {
                _expr!.EmitStore(WrapTargetExpr(ae.Target));
            }
            else if (ae.Target is UnaryExpr derefTarget && derefTarget.Op == "*")
            {
                Add(OpCode.MOVE, "R1", "R0");
                GenerateExpr(derefTarget.Operand);
                string? pointedType = null;
                if (derefTarget.Operand is IdentExpr derefId && _varTypes.TryGetValue(derefId.Name, out string? vt))
                    pointedType = vt;
                var (size, isFloat, isDouble) = GetTypeLoadInfo(pointedType);
                instructions.Add(new Instruction(
                    ExpressionManager.SelectStoreUnifiedOp(size, isFloat, isDouble),
                    new List<Operand> {
                        new(OperandType.MEMORY, "R0"),
                        new(OperandType.REGISTER, 1)
                    }));
                Add(OpCode.MOVE, "R0", "R1");
            }
            else if (ae.Target is IdentExpr ie2)
            {
                string label = $"var_{ie2.Name}";
                if (!string.IsNullOrEmpty(ae.DeclType) && !_varTypes.ContainsKey(ie2.Name))
                    _varTypes[ie2.Name] = ae.DeclType;

                // ── 字节数组（`char s[] = "…"` / `char buf[N]`）───────────────────
                //
                // 见 `CodeGenerator._byteArrays` 的类注释：字符数组**不是** VML 那套
                // `[4 字节长度头][每格 4 字节]` 的数组，而是一段 C 字符串。
                //
                // ⚠ 此前 `char lbuf[] = "LOCAL";` 在这里被当成普通数组：数据段落成
                //   `int[2]`（头 + 一格），随后那句 `Add(varStoreOp, label, "R0")` 又把
                //   **字符串常量的地址**写进长度头那一格 ⇒ 传出去的是"长度头所在处的地址"，
                //   宿主按 C 字符串读它，读到的正是那个地址的低字节 ——
                //   屏幕上是**一个乱字符**（用户报的"就输出一个字符就没了，而且是乱字符"）。
                if (ae.ArraySize > 0 && IsByteArrayDecl(ae.DeclType, isArray: true))
                {
                    if (!dataSection.ContainsKey(label))
                        dataSection[label] = BuildByteArrayData(ae.Value, new IntLiteral { Value = ae.ArraySize });
                    _isArrayVar[ie2.Name] = true;
                    _byteArrays.Add(ie2.Name);
                    return;
                }

                // ── 真正的**局部变量**：落在栈帧里 ──────────────────────────────
                //
                // ⚠ 此前函数体里的 `int i;` 会被当成**全局量**（`var_i`）发出去 ——
                //   **所有函数的 `i` 共用同一个标签**。后果极隐蔽：
                //   `while (i < 8) { f(); i = i + 1; }` 里 `f()` 内部也有个 `i` 的话，
                //   回来时 `i` 已经被改过 ⇒ 循环次数随被调方而变。
                //   实测症状是"白天正常、夜里丢一半画面"（`Sky::Draw` 夜里才走那个循环），
                //   看着像绘制问题，其实是变量作用域。
                //
                // 只管**标量**：数组（`ae.ArraySize > 0`）、字节数组、以及类类型的对象
                // 仍走各自的分支 —— 它们的数据段布局/构造流程是另一套，不在这条路上改。
                if (_inFunctionBody && Vars != null && ae.ArraySize == 0
                    && !string.IsNullOrEmpty(ae.DeclType)
                    && !_variables.ContainsKey(ie2.Name)          // 参数等已登记的优先
                    && !_classes.ContainsKey(CleanType(ae.DeclType!)))
                {
                    var localInfo = Vars.AllocLocal(ie2.Name, 4, ae.DeclType);
                    _variables[ie2.Name] = localInfo.Offset;
                    _varTypes[ie2.Name] = ae.DeclType!;
                    OpCode stOp = OpCode.MOVE;
                    var cppT = MapToCppType(ae.DeclType!);
                    if (cppT == CppType.Float) stOp = OpCode.MOVEF;
                    else if (cppT == CppType.Double) stOp = OpCode.MOVED;
                    Add(stOp, Vars.FormatOffset(localInfo.Offset), "R0");
                    return;
                }

                string? structType = ae.DeclType;
                if (string.IsNullOrEmpty(structType)) _varTypes.TryGetValue(ie2.Name, out structType);
                if (!string.IsNullOrEmpty(structType) && _classes.TryGetValue(CleanType(structType), out var allocCls))
                {
                    string allocName = CleanType(structType);
                    int fieldCount = allocCls.Members.Count(m => !m.IsMethod);
                    // 对象占几个字要走 `ClassSizeDeep`（**含基类子对象**），不是"本类字段数"：
                    // 继承来的字段同样是这个对象的一部分，按本类字段数分配会**少分**，
                    // 写基类字段就越界写到隔壁变量上（不报错、只是把别人的值改掉）。
                    int objWords = Math.Max(1, ClassSizeDeep(allocName) / 4);
                    if (!dataSection.ContainsKey(label))
                    {
                        if (ae.ArraySize > 0)
                        {
                            // VML array: 1 header word + N * 每个元素的字数
                            dataSection[label] = new int[1 + ae.ArraySize * objWords];
                            _isArrayVar[ie2.Name] = true;
                        }
                        else
                            dataSection[label] = objWords > 1 ? new int[objWords] : 0;
                    }
                    // ── 有虚函数的类：把**本类**虚表的地址写进对象第 0 个字 ──────────
                    // 虚调用的第一步就是读这个字（见调用点的 `(R0)`）。**在这里写**而不是
                    // 写进构造函数里，是因为"没有用户构造函数"的类也要有 ——
                    // 漏了的话 vptr 是 0，`CALL [0 + …]` 直接跳到地址 0 上，是**崩溃**而不是错值。
                    if (ClassHasVirtualDeep(allocName))
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, [
                            new(OperandType.REGISTER, 1), new(OperandType.LABEL, label)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [
                            new(OperandType.REGISTER, 0), new(OperandType.LABEL, $"{allocName}_vtable")]));
                        Add(OpCode.MOVE, "(R1)", "R0");
                    }
                    // 先构造成员对象（C++ 里成员比构造函数的**函数体**更早构造），
                    // 它们各自的构造函数与 vptr 都在这里落地 —— 见 `EmitMemberCtorCalls`。
                    if (ae.ArraySize == 0 && ae.Value is not CallExpr)
                        EmitMemberCtorCalls(label, allocName, 0);

                    // ── `T x;` / `T x(args);` 要**真的调一次构造函数** ──────────────
                    // 此前这一支压根没有调用点：构造函数体生成得完完整整（见
                    // `GenerateDecl` 的 ClassDecl 分支），却**没有任何地方 call 它** ——
                    // 对象上全是数据段里的 0。而 `Counter c(100)` 那句会被下面那句
                    // `Add(varStoreOp, label, "R0")` 当成"初值 100"写进**第 0 个字**，
                    // 于是「只有一个字段、且恰好偏移 0」的类**看起来是对的**
                    // （`c.Get()` 真能读到 100），字段一多或一有继承就现形 ——
                    // 这类"例子恰好都对"的洞最难靠跑例子发现。
                    if (ae.ArraySize == 0 && ae.Value is not CallExpr)
                    {
                        var ctor = FindCtor(allocCls, ae.CtorArgs?.Count ?? 0);
                        if (ctor != null)
                        {
                            EmitCtorCall(label, allocName, ctor, ae.CtorArgs);
                            return;   // 对象已经由构造函数初始化，别再往第 0 格里塞初值
                        }
                    }
                    // Struct init from function call: copy N fields from (R0)
                    if (fieldCount > 1 && ae.Value is CallExpr)
                    {
                        // R0 = returned struct ptr; copy fields
                        Add(OpCode.MOVE, "R2", "R0");
                        for (int i = 0; i < fieldCount; i++)
                        {
                            if (i == 0)
                                Add(OpCode.MOVE, "R1", "(R2)");
                            else
                            {
                                Add(OpCode.MOVE, "R0", "R2");
                                Add(OpCode.ADD, "R0", $"#{i * 4}");
                                Add(OpCode.MOVE, "R1", "(R0)");
                            }
                            instructions.Add(new Instruction(OpCode.MOVE, [
                                new(OperandType.REGISTER, 0),
                                new(OperandType.LABEL, label)
                            ]));
                            if (i > 0) Add(OpCode.ADD, "R0", $"#{i * 4}");
                            Add(OpCode.MOVE, "(R0)", "R1");
                        }
                        Add(OpCode.MOVE, "R0", label);
                    }
                    else
                    {
                        Add(OpCode.MOVE, label, "R0");
                    }
                }
                else
                {
                    if (!dataSection.ContainsKey(label))
                    {
                        if (ae.ArraySize > 0)
                        {
                            // VML array: header word (length) + elements
                            dataSection[label] = new int[1 + ae.ArraySize];
                            _isArrayVar[ie2.Name] = true;
                            // 保存最内层维度 (用于多维stride)
                            if (ae.Dimensions.Count >= 2)
                                _arrayInnerDim[ie2.Name] = ae.Dimensions[ae.Dimensions.Count - 1];
                            // 初始化 header: length = total elements
                            Add(OpCode.MOVE, "R1", $"#{ae.ArraySize}");
                            Add(OpCode.MOVE, label, "R1");

                            // ⚠ **初始值要逐个写进去**。此前这里只有长度头，元素一个都没写
                            //   ⇒ `int loc[3] = {1,2,3};` 读到的是数据段里的 0（实测）。
                            //   数组初始化一共两处**活**的代码：这里（局部，含 C++ 的"栈上数组"）
                            //   与全局的 `GenerateGlobalVar` —— 两处都犯过同一个毛病，改的时候一起看。
                            //   （`GenerateLocalVar` 里也有一段同样的逻辑，但那个方法**没有调用点**，
                            //   改它不会影响任何产物，见它的方法头注释。）
                            //   布局：头在 `label`，元素从 `+4` 起、每格 4 字节（与 `[]` 的 `+4` 对齐）。
                            if (ae.Value is InitializerListExpr locInit2)
                            {
                                var flat2 = new List<object>();
                                FlattenInitList(locInit2, flat2);
                                for (int i = 0; i < flat2.Count && i < ae.ArraySize; i++)
                                {
                                    Add(OpCode.MOVE, "R0", flat2[i] is int iv2 ? $"#{iv2}" : flat2[i].ToString()!);
                                    Add(OpCode.MOVE, "R1", "R0");
                                    // ⚠ 这里要的是**地址**（LABEL），不是那个标签处的**内容**（MEMORY）。
                                    //   走 `Add(OpCode, string…)` 的话 `label`（形如 `var_a`）会被
                                    //   字面规则判成 MEMORY（见 `CodeGenerator.Add` 的 `StartsWith("var_")`）
                                    //   ⇒ 编出 `move @R0 [var_a]`，取到的是**长度头 3**，
                                    //   再 `+4` = 地址 7，于是三个元素全写到了地址 7 上（实测恒读到 0）。
                                    //   表达式路径取数组地址用的就是 LABEL 操作数（见本文件 IdentExpr 的
                                    //   `_globalArrays` 那一支），这里与它对齐。
                                    instructions.Add(new Instruction(OpCode.MOVE, [
                                        new(OperandType.REGISTER, 0),
                                        new(OperandType.LABEL, label)
                                    ]));
                                    Add(OpCode.ADD, "R0", $"#{4 + i * 4}");
                                    Add(OpCode.MOVE, "(R0)", "R1");
                                }
                            }
                        }
                        else
                            dataSection[label] = 0;
                    }
                    // v1.65.177: 根据变量类型选择 store 指令
                    OpCode varStoreOp = OpCode.MOVE;
                    if (_varTypes.TryGetValue(ie2.Name, out string? varType2))
                    {
                        var cppType2 = MapToCppType(varType2);
                        varStoreOp = cppType2 switch
                        {
                            CppType.Float => OpCode.MOVEF,
                            CppType.Double => OpCode.MOVED,
                            _ => OpCode.MOVE
                        };
                    }
                    Add(varStoreOp, label, "R0");
                }
            }
            else if (ae.Target is BinaryExpr be2 && be2.Op == "[]")
            {
                Add(OpCode.MOVE, "R3", "R0");
                GenerateExpr(be2.Left);
                Add(OpCode.PUSH, "R0");
                GenerateExpr(be2.Right);
                Add(OpCode.MOVE, "R2", "R0");
                Add(OpCode.POP, "R1");
                Add(OpCode.MOVE, "R0", "R2");
                // 多维数组 stride
                bool isNestedArr = be2.Left is BinaryExpr inner2 && inner2.Op == "[]";
                // 与读取那条**同一套判据**（见 `ArrayIndexInfo`）：
                // 写错一处就是"读对了、写错了"，而那种分叉最难查。
                var (wElemSize, wHasHeader) = ArrayIndexInfo(be2.Left, isNestedArr);
                int innerDimW = 1;
                if (!isNestedArr && be2.Left is IdentExpr arrIdW && _arrayInnerDim.TryGetValue(arrIdW.Name, out int idimW))
                    innerDimW = idimW;
                if (innerDimW > 1)
                {
                    Add(OpCode.MOVE, "R2", $"#{innerDimW}");
                    Add(OpCode.MUL, "R0", "R0", "R2");   // index * innerDim
                }
                if (wElemSize == 2) Add(OpCode.SHL, "R0", "#1");
                else if (wElemSize > 2) Add(OpCode.SHL, "R0", "#2");   // * 4 → 字节偏移
                if (wHasHeader && !isNestedArr)
                    Add(OpCode.ADD, "R0", "#4");         // +4 header
                Add(OpCode.ADD, "R0", "R1");             // + base
                // 存储指令同样按元素宽度选（字节数组 → `MOVEB`，只写一格）
                Add(StoreOpFor(wElemSize), "(R0)", "R3");
                Add(OpCode.MOVE, "R0", "R3");
            }
            else if (ae.Target is MemberExpr me)
            {
                // Save value in R2 (not R1 — GenerateBaseForMember uses R1 as scratch)
                Add(OpCode.MOVE, "R2", "R0");
                GenerateMemberAddress(me);
                Add(OpCode.MOVE, "(R0)", "R2");
                Add(OpCode.MOVE, "R0", "R2");
            }
        }

        /// <summary>
        /// `sizeof(表达式)` 的取值。
        ///
        /// **绝不抛**：查不到就给 4 —— 编译器崩掉（`NullReferenceException`、报错里没有行列）
        /// 比给一个宽一点的值糟得多，而 `sizeof` 在这里本来就是**尽力而为**：
        /// 真实的类型信息在这个前端里散在 `_varTypes`（名字 → 类型串）与数据段里，
        /// 没有一份"表达式的类型"。
        ///
        /// 数组按 **C 的语义**给**总字节数**（`int a[4]` → 16、`double d[3]` → 24）：
        /// 元素个数不用另建表 —— 数据段里那份声明就是 `int[1 + N]`（**多一个长度头**，
        /// 见数组声明那条路），`Length - 1` 就是 N。
        /// </summary>
        private int SizeOfExprValue(Expr? e)
        {
            if (e is IdentExpr id)
            {
                string tn = _varTypes.TryGetValue(id.Name, out var t) && t != null ? t : "int";
                int elem = GetTypeSizeByEnum((int)MapToCppType(tn));

                bool isArr = (_isArrayVar.TryGetValue(id.Name, out var ia) && ia)
                             || _globalArrays.Contains(id.Name);
                if (isArr)
                {
                    string lbl = $"var_{id.Name}";
                    if (dataSection.TryGetValue(lbl, out var v))
                    {
                        // ⚠ 两种元素类型都要认：**局部**数组存的是 `int[]`
                        //   （`new int[1 + N]`），而**全局**数组存的是 `object[]`
                        //   （`withHeader.ToArray()`，因为元素可能是字符串标签等）。
                        //   只认 `int[]` 的话全局数组会静默退回"一个元素" ——
                        //   实测就是 `sizeof(g)` 给 4 而不是 24。
                        // 字节数组（`char x[]`）是**第三**种：数据段里就是 `byte[N]`、
                        // **没有长度头**，所以直接就是 N 个字节（见 `_byteArrays`）。
                        if (v is byte[] barr) return barr.Length;
                        int? len = v switch { int[] a1 => a1.Length, object[] a2 => a2.Length, _ => null };
                        if (len is int n && n > 0) return elem * (n - 1);
                    }
                    // 数组但查不到声明（例如来自别处的别名）：退回"一个元素"，不猜
                }
                return elem;
            }
            if (e is StringLiteral sl) return sl.Value.Length + 1;   // 字面量含结尾 NUL
            return 4;
        }
    }
}
