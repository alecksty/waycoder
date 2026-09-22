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
            if (e is IdentExpr ie && _varTypes.TryGetValue(ie.Name, out var vt) && vt.Contains("*"))
                return GetPointerStepSize(vt);
            return 4; // 默认 int / 数组元素
        }
        private static int ElemShift(int size) => size switch { 1 => 0, 2 => 1, _ => 2 };
        /// <summary>表达式是否是简单指针解引用 (char*/int*/float*等, 非VML数组, 无需+4 header)</summary>
        private bool IsPointerDeref(Expr e) => e is IdentExpr ie && _varTypes.TryGetValue(ie.Name, out var vt)
            && vt.Contains("*") && !vt.Contains("[") && !vt.Contains("(");

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
                    if (so.TypeName == "char") size = 1;
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
                    // 获取元素大小: 从表达式类型推断 (char*=1, short*=2, int*=4, 默认4)
                    int elemSize = GetElementSize(be.Left);
                    // 指针无header, 数组(含多维)有header
                    bool hasHeader = !IsPointerDeref(be.Left) || isNestedArray;
                    // 获取内层维度用于 stride (如 arr[2][3] 的内层维度为3)
                    int innerDim = 1;
                    if (!isNestedArray && be.Left is IdentExpr arrId && _arrayInnerDim.TryGetValue(arrId.Name, out int idim))
                        innerDim = idim;
                    if (innerDim > 1)
                    {
                        Add(OpCode.MOVE, "R2", $"#{innerDim}");
                        Add(OpCode.MUL, "R1", "R1", "R2");
                        if (elemSize != 1) Add(OpCode.SHL, "R1", $"#{ElemShift(elemSize)}");
                        else if (elemSize != 1) { } // char: no shift needed
                        if (hasHeader) Add(OpCode.ADD, "R1", "#4");
                        Add(OpCode.ADD, "R0", "R0", "R1");
                    }
                    else
                    {
                        if (elemSize == 1) { /* char: 无需乘 */ }
                        else if (elemSize == 2) Add(OpCode.SHL, "R1", "#1");
                        else Add(OpCode.SHL, "R1", "#2");      // default: *4
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
            if (expr is IdentExpr ie && _variables.TryGetValue(ie.Name, out int offset))
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
                Add(OpCode.SHL, "R1", "#2");     // index * 4
                Add(OpCode.ADD, "R1", "#4");     // +4 (skip header)
                Add(OpCode.ADD, "R0", "R1");     // R0 = base + index*4 + 4
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
            }
            else if (ce.Callee is MemberExpr me)
            {
                // obj.method() or obj->method()
                if (me.Object is IdentExpr objId)
                {
                    // Check if it's a virtual method call
                    if (_classes.TryGetValue(objId.Name, out var cls))
                    {
                        var virtMethod = cls.Members.FirstOrDefault(m => m.IsMethod && m.Method?.Name == me.Member && m.IsVirtual);
                        if (virtMethod != null)
                        {
                            // Virtual dispatch through vtable
                            int vtableIndex = cls.Members.TakeWhile(m => !(m.IsMethod && m.Method?.Name == me.Member && m.IsVirtual)).Count(m => m.IsVirtual);
                            // Push this pointer
                            GenerateExpr(me.Object);
                            Add(OpCode.PUSH, "R0");
                            hasThis = true;
                            // Find derived classes that override this method
                            var overriders = _classes.Values
                                .Where(c => c.BaseClass == objId.Name
                                    && c.Members.Any(m => m.IsMethod && m.IsVirtual && m.Method?.Name == me.Member))
                                .ToList();
                            if (overriders.Count > 0)
                            {
                                // Generate typeid-based virtual dispatch cascade
                                string vdispEnd = $"vdisp_end_{labelCounter++}";
                                // R0 = this pointer (from GenerateExpr above, already pushed)
                                // Object's first word = type_info address
                                Add(OpCode.MOVE, "R2", "(R0)");  // R2 = type_info ptr from object
                                string baseLabel = MangleName($"method_{objId.Name}_{me.Member}", ce.Arguments);
                                // Pre-generate label names for overrides
                                var ovrLabels = new List<(ClassDecl dc, string ovrLabel, string jeLabel)>();
                                foreach (var dc in overriders)
                                {
                                    string ovrLabel = MangleName($"method_{dc.Name}_{me.Member}", ce.Arguments);
                                    string jeLabel = $"vdisp_ovr_{labelCounter++}";
                                    ovrLabels.Add((dc, ovrLabel, jeLabel));
                                }
                                // Emit typeid comparisons (compare type_info addresses directly)
                                foreach (var ol in ovrLabels)
                                {
                                    Add(OpCode.MOVE, "R1", $"{ol.dc.Name}_typeid"); // R1 = expected type_info addr
                                    Add(OpCode.CMP, "R2", "R1");
                                    Add(OpCode.JE, ol.jeLabel);
                                }
                                // Fall through: base implementation
                                Add(OpCode.CALL, baseLabel);
                                Add(OpCode.JMP, vdispEnd);
                                // Override targets
                                foreach (var ol in ovrLabels)
                                {
                                    labels[ol.jeLabel] = instructions.Count;
                                    Add(OpCode.CALL, ol.ovrLabel);
                                    Add(OpCode.JMP, vdispEnd);
                                }
                                labels[vdispEnd] = instructions.Count;
                                funcName = null; // already emitted CALL
                            }
                            else
                            {
                                funcName = MangleName($"method_{objId.Name}_{me.Member}", ce.Arguments);
                            }
                        }
                        else
                        {
                            funcName = MangleName($"method_{objId.Name}_{me.Member}", ce.Arguments);
                            GenerateExpr(me.Object);
                            Add(OpCode.PUSH, "R0");
                            hasThis = true;
                        }
                    }
                    else
                    {
                        funcName = MangleName($"method_{objId.Name}_{me.Member}", ce.Arguments);
                        GenerateExpr(me.Object);
                        Add(OpCode.PUSH, "R0");
                        hasThis = true;
                    }
                }
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

        /// <summary>清理类型字符串: 去掉 struct/union/class 前缀</summary>
        private static string CleanType(string type)
        {
            if (string.IsNullOrEmpty(type)) return type;
            return type.Replace("struct ", "").Replace("union ", "").Replace("class ", "").Trim();
        }

        /// <summary>Resolve the ClassDecl from an expression's type (handles ptr-to-class too)</summary>
        private ClassDecl? ResolveClassOf(Expr expr)
        {
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

        /// <summary>Generate base for member access: address for class-typed vars, value for ptr-to-class</summary>
        private void GenerateBaseForMember(Expr obj)
        {
            if (obj is IdentExpr ie)
            {
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
                if (be.Left is IdentExpr arrId && _varTypes.TryGetValue(arrId.Name, out var arrType))
                {
                    if (_classes.TryGetValue(CleanType(arrType), out var arrCls))
                        stride = arrCls.Members.Count(m => !m.IsMethod) * 4;
                }
                if (stride == 4) Add(OpCode.SHL, "R1", "#2");
                else if (stride == 8) Add(OpCode.SHL, "R1", "#3");
                else { Add(OpCode.MOVE, "R2", $"#{stride}"); Add(OpCode.MUL, "R1", "R1", "R2"); }
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
            GenerateBaseForMember(me.Object);
            var cls = ResolveClassOf(me.Object);
            if (cls != null)
            {
                int fieldOffset = GetVtableOffset(cls);
                foreach (var member in cls.Members)
                {
                    if (member.IsMethod) continue;
                    if (member.Name == me.Member) break;
                    fieldOffset += 4;
                }
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
            GenerateBaseForMember(me.Object);
            var cls = ResolveClassOf(me.Object);
            if (cls != null)
            {
                int fieldOffset = GetVtableOffset(cls);
                foreach (var member in cls.Members)
                {
                    if (member.IsMethod) continue;
                    if (member.Name == me.Member) break;
                    fieldOffset += 4;
                }
                if (fieldOffset != 0)
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

        private void GenerateAssignExpr(AssignExpr ae)
        {
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
                string? structType = ae.DeclType;
                if (string.IsNullOrEmpty(structType)) _varTypes.TryGetValue(ie2.Name, out structType);
                if (!string.IsNullOrEmpty(structType) && _classes.TryGetValue(CleanType(structType), out var allocCls))
                {
                    int fieldCount = allocCls.Members.Count(m => !m.IsMethod);
                    if (!dataSection.ContainsKey(label))
                    {
                        if (ae.ArraySize > 0)
                        {
                            // VML array: 1 header word + N * fieldCount elements
                            dataSection[label] = new int[1 + ae.ArraySize * fieldCount];
                            _isArrayVar[ie2.Name] = true;
                        }
                        else
                            dataSection[label] = fieldCount > 1 ? new int[fieldCount] : 0;
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
                int innerDimW = 1;
                if (!isNestedArr && be2.Left is IdentExpr arrIdW && _arrayInnerDim.TryGetValue(arrIdW.Name, out int idimW))
                    innerDimW = idimW;
                if (innerDimW > 1)
                {
                    Add(OpCode.MOVE, "R2", $"#{innerDimW}");
                    Add(OpCode.MUL, "R0", "R0", "R2");   // index * innerDim
                }
                Add(OpCode.SHL, "R0", "#2");            // * 4 → 字节偏移
                if (!isNestedArr)
                    Add(OpCode.ADD, "R0", "#4");         // +4 header
                Add(OpCode.ADD, "R0", "R1");             // + base
                Add(OpCode.MOVE, "(R0)", "R3");
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
    }
}
