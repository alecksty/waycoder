using VMLAssembler;
using CompilerBase;

namespace GoCompiler
{
    public partial class CodeGenerator : CLikeCodegen<CodeGenerator>
    {
        private void GenerateIdentifier(Identifier ident)
        {
            if (variables.ContainsKey(ident.Name))
            {
                int offset = variables[ident.Name];
                
                // 获取变量类型并使用正确的加载指令
                GoTypeEnum varType = _varTypes.ContainsKey(ident.Name) ? _varTypes[ident.Name] : GoTypeEnum.Int;
                OpCode loadOp = GetLoadInstruction(varType);
                
                AddInstruction(loadOp, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    Mem($"R14-{offset}")
                });
            }
            else if (dataSection.ContainsKey(ident.Name))
            {
                // 全局变量，使用类型敏感的MOVE指令
                GoTypeEnum varType = _varTypes.ContainsKey(ident.Name) ? _varTypes[ident.Name] : GoTypeEnum.Int;
                OpCode moveOp = GetMoveInstruction(varType);
                
                AddInstruction(moveOp, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.MEMORY, ident.Name)
                });
            }
            else
            {
                // 局部表与 `dataSection` 都没有 ⇒ 这个名字**从未声明过**。
                // 此前这里「假设是全局变量」直接发一条 `MOVE R0, [name]` ——
                // 既不建槽也不报错，引用的是一个可能根本不存在的标签：
                // `x := 1; y := x + nosuch` 编得过，运行期读到的值取决于汇编器/内存残值
                //（连"确定的 0"都不是），用户要等到运行才发现。
                ReportUndefined(ident.Name, ErrorCode.CodeGen_UndefinedVariable, "变量");
                EmitUndefinedFallback();
            }
        }

        private void GenerateExpression(ASTNode expr)
        {
            if (expr == null) return;

            if (expr is Identifier ident)
            {
                GenerateIdentifier(ident);
            }
            else if (expr is NumberLiteral numLit)
            {
                GenerateNumberLiteral(numLit);
            }
            else if (expr is StringLiteral strLit)
            {
                GenerateStringLiteral(strLit);
            }
            else if (expr is BoolLiteral boolLit)
            {
                EmitLoadConstant(boolLit.Value);
            }
            else if (expr is CharLiteral charLit && !string.IsNullOrEmpty(charLit.Value))
            {
                EmitLoadConstant((int)charLit.Value[0]);
            }
            else if (expr is NilLiteral)
            {
                EmitLoadConstant(0);
            }
            else if (expr is BinaryOp binary)
            {
                GenerateBinaryOp(binary);
            }
            else if (expr is UnaryOp unary)
            {
                GenerateUnaryOp(unary);
            }
            else if (expr is FunctionCall call)
            {
                GenerateFunctionCall(call);
            }
            else if (expr is MethodCall methodCall)
            {
                GenerateMethodCall(methodCall);
            }
            else if (expr is IndexExpr indexExpr)
            {
                GenerateIndexExpr(indexExpr);
            }
            else if (expr is SelectorExpr selector)
            {
                GenerateSelectorExpr(selector);
            }
            else if (expr is ArrayLiteral arrLit)
            {
                // 数组字面量: [n]type{...} 分配内存并存储元素
                int an = arrLit.Elements?.Count ?? 0;
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, an * 4 + 4)]));
                AddSyscall(40);
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, an)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0)]));
                for (int ei = 0; ei < an; ei++)
                {
                    GenerateExpression(arrLit.Elements[ei]);
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R1+{4 + ei * 4}"), new Operand(OperandType.REGISTER, 0)]));
                }
            }
            else if (expr is SliceExpr sliceExpr)
            {
                // 切片表达式 arr[low:high] — 创建完整切片头 [data_ptr, len, cap]
                GenerateExpression(sliceExpr.Array); // R0 = array ptr
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)])); // R1 = array
                // 分配切片头 12 字节 (ptr+len+cap)
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 12)]));
                AddSyscall(40); // malloc
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0)])); // R2 = slice header
                // 计算 data_ptr = array + 4 + low*4
                if (sliceExpr.Low != null) {
                    GenerateExpression(sliceExpr.Low);
                    instructions.Add(new Instruction(OpCode.SHL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 2)]));
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
                } else {
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
                }
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R2"), new Operand(OperandType.REGISTER, 0)])); // slice.ptr
                // 计算 len = high - low
                if (sliceExpr.High != null) {
                    GenerateExpression(sliceExpr.High); // R0 = high
                    if (sliceExpr.Low != null) {
                        instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                        GenerateExpression(sliceExpr.Low);
                        instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                        instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                    }
                } else {
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0)])); // array.len
                    if (sliceExpr.Low != null) {
                        instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                        GenerateExpression(sliceExpr.Low);
                        instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                        instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                    }
                }
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R2+4"), new Operand(OperandType.REGISTER, 0)])); // slice.len
                // cap = array.len - low
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0)])); // array.len
                if (sliceExpr.Low != null) {
                    instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    GenerateExpression(sliceExpr.Low);
                    instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                    instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                }
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R2+8"), new Operand(OperandType.REGISTER, 0)])); // slice.cap
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2)])); // return slice header
            }
            else if (expr is SliceLiteral sliceLit)
            {
                // Slice 字面量: 分配内存并逐个存储元素
                int n = sliceLit.Elements?.Count ?? 0;
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, n * 4 + 4)]));
                AddSyscall(40);
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, n)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0)]));
                for (int ei = 0; ei < n; ei++)
                {
                    GenerateExpression(sliceLit.Elements[ei]);
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R1+{4 + ei * 4}"), new Operand(OperandType.REGISTER, 0)]));
                }
            }
            else if (expr is CompositeLiteral compLit)
            {
                // 复合字面量：结构体初始化 {field: val, ...}
                // 分配空间并逐个字段赋值
                if (compLit.Elements != null && compLit.Elements.Count > 0)
                {
                    int fieldCount = compLit.Elements.Count;
                    // Allocate struct memory (fieldCount * 4 bytes)
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, fieldCount * 4)]));
                    AddSyscall(40); // malloc
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)])); // R1 = base
                    for (int i = 0; i < fieldCount; i++)
                    {
                        if (compLit.Elements[i] is KeyValueExpr kv)
                        {
                            GenerateExpression(kv.Value);
                            int fieldOffset = i * 4;
                            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R1+{fieldOffset}"), new Operand(OperandType.REGISTER, 0)]));
                        }
                    }
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)])); // return base
                }
                else
                {
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                }
            }
            else if (expr is TypeAssertion typeAssert)
            {
                GenerateExpression(typeAssert.X);
            }
            else if (expr is TypeConversion conv)
            {
                GoTypeEnum fromType = InferExpressionType(conv.Arg);
                GoTypeEnum toType = GetGoTypeEnum(conv.Type);
                GenerateExpression(conv.Arg);
                GenerateTypeConversion(fromType, toType);
            }
            else if (expr is FuncLiteral funcLit)
            {
                // 函数字面量: 生成一个命名函数并返回标签
                string lambdaLabel = $"lambda_{instructions.Count}";
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, lambdaLabel)]));
            }
            else if (expr is ReceiveExpr receive)
            {
                // Channel receive: <-ch
                if (!VMLPlugins.CompilerOptionsContext.Current.IsMCU)
                {
                    GenerateExpression(receive.Chan);
                    instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "__runtime_chan_recv")]));
                }
                else
                {
                    VMLPlugins.WarningEmitter.Emit("go", "MCU模式: <-chan 被忽略（MCU 无 channel 支持）");
                    EmitLoadConstant(0);
                }
            }
        }

        private void GenerateNumberLiteral(NumberLiteral numLit)
        {
            object value;
            GoTypeEnum type;

            if (numLit.IsFloat)
            {
                if (double.TryParse(numLit.Value, out double d))
                {
                    // 将 float 位模式存入 data section，用 MOVEF 加载，避免整数/浮点寄存器混淆
                    float fval = (float)d;
                    string flabel = $"flt_{instructions.Count}";
                    dataSection[flabel] = BitConverter.SingleToInt32Bits(fval);
                    instructions.Add(new Instruction(OpCode.MOVEF, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.MEMORY, flabel)
                    }, instructions.Count));
                    return;
                }
                else
                {
                    value = 0;
                }
                type = GoTypeEnum.Float;
            }
            else
            {
                if (long.TryParse(numLit.Value, out long l))
                    value = (int)l;
                else
                    value = 0;
                type = GoTypeEnum.Int;
            }

            // 使用类型敏感的移动指令
            OpCode moveOp = GetMoveInstruction(type);
            instructions.Add(new Instruction(moveOp, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, value)
            }, instructions.Count));
        }

        private void GenerateStringLiteral(StringLiteral strLit)
        {
            string label = $"str_{NewLabel()}";
            dataSection[label] = strLit.Value;
            
            // 加载字符串地址到R0（使用LABEL类型）
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.LABEL, label)
            }, instructions.Count));
        }

        /// <summary>GoTypeEnum → ExpType 映射
        /// 注意：Int64/UInt64 映射到 F64 以使用 VML double 操作路径，
        /// 因为 R0-R15 的 long 操作只处理 32 位</summary>
        private static ExpType GoTypeToExpType(GoTypeEnum t) => t switch
        {
            GoTypeEnum.Bool or GoTypeEnum.Byte => ExpType.I8,
            GoTypeEnum.Rune => ExpType.I16,
            GoTypeEnum.Float => ExpType.F32,
            GoTypeEnum.Double or GoTypeEnum.Int64 or GoTypeEnum.UInt64 => ExpType.F64,
            GoTypeEnum.String => ExpType.Ptr32,
            _ => ExpType.I32, // Int, UInt, etc.
        };

        /// <summary>求值表达式并包装为 ExpVar.Eval（延迟求值，首次 EmitLoad 时执行）</summary>
        private ExpVar WrapExpr(ASTNode expr)
        {
            var goType = InferExpressionType(expr);
            var expType = GoTypeToExpType(goType);
            return ExpVar.Eval(expType, () => GenerateExpression(expr));
        }

        private void GenerateBinaryOp(BinaryOp binary)
        {
            GoTypeEnum leftType = InferExpressionType(binary.Left);
            GoTypeEnum rightType = InferExpressionType(binary.Right);

            // 字符串拼接：保留现有实现（涉及复杂的字符串分配逻辑）
            if (leftType == GoTypeEnum.String && rightType == GoTypeEnum.String && binary.Op == "+")
            {
                GenerateBinaryOp_StringConcat(binary);
                return;
            }

            var left = WrapExpr(binary.Left);
            var right = WrapExpr(binary.Right);

            if (_expr!.EmitStandardBinaryOps(binary.Op, left, right)) return;
            if (binary.Op == "&^")
            {
                // Go AND-NOT: a &^ b = a & (~b)
                _expr!.EmitBitAnd(WrapExpr(binary.Left), _expr!.EmitBitNot(WrapExpr(binary.Right)));
            }
        }

        /// <summary>字符串拼接（Go 特有）</summary>
        private void GenerateBinaryOp_StringConcat(BinaryOp binary)
        {
            // 求值左右操作数（传统方式，因为需要直接操作 R2/R3 寄存器）
            GenerateExpression(binary.Left);
            OpCode pushOp = GetPushInstruction(GoTypeEnum.String);
            instructions.Add(new Instruction(pushOp, [new Operand(OperandType.REGISTER, 0)], instructions.Count));
            GenerateExpression(binary.Right);
            OpCode moveOp = GetMoveInstruction(GoTypeEnum.String);
            instructions.Add(new Instruction(moveOp, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)], instructions.Count));
            OpCode popOp = GetPopInstruction(GoTypeEnum.String);
            instructions.Add(new Instruction(popOp, [new Operand(OperandType.REGISTER, 0)], instructions.Count));

            // R0 = left string address, R1 = right string address
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0)])); // R2 = left addr
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 1)])); // R3 = right addr

            // CALL shared_strlen(left) → R4
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 2)]));
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "strlen")]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0)]));

            // CALL shared_strlen(right) → R5
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 3)]));
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "strlen")]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 0)]));

            // Allocate left_len + right_len + 1 bytes
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 5)]));
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
            AddSyscall(40);
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 0)])); // R6 = new buffer

            // CALL shared_memcpy(dst=R6, src=left, n=left_len)
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 6)]));
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 4)]));
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 2)]));
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "memcpy")]));

            // CALL shared_memcpy(dst=R6+left_len, src=right, n=right_len)
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 4)]));
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 5)]));
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 3)]));
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "memcpy")]));

            // Null terminate
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 5)]));
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 0)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0)]));
            instructions.Add(new Instruction(OpCode.MOVEB, [Mem("R0"), new Operand(OperandType.REGISTER, 1)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 6)]));
        }

        private void GenerateUnaryOp(UnaryOp unary)
        {
            switch (unary.Op)
            {
                case "-":
                    _expr!.EmitNeg(WrapExpr(unary.Operand));
                    break;
                case "!":
                    _expr!.EmitNot(WrapExpr(unary.Operand));
                    break;
                case "^":
                    // Go 按位取反: ^x = ~x
                    _expr!.EmitBitNot(WrapExpr(unary.Operand));
                    break;
                case "*":
                    // 解引用: *ptr → 根据指向类型选择 LOADB/LOADH/FLOAD/DLOAD/LOAD
                    GenerateExpression(unary.Operand);
                    OpCode goDerefOp = OpCode.MOVE;
                    if (unary.Operand is Identifier ptrId && _varTypes.TryGetValue(ptrId.Name, out var goPtrType))
                    {
                        // GoTypeEnum.Pointer → infer from stored pointed-to type
                        var (goSize, goFloat, goDbl) = TypeInfo(goPtrType);
                        goDerefOp = ExpressionManager.SelectLoadOp(goSize, goFloat, goDbl);
                    }
                    instructions.Add(new Instruction(goDerefOp, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 0)], instructions.Count));
                    break;
                case "&":
                    // 取地址: &var → MOVE R0, R14; SUB R0, #offset
                    if (unary.Operand is Identifier addrId && variables.TryGetValue(addrId.Name, out int goAddrOff))
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 14)], instructions.Count));
                        if (goAddrOff != 0)
                            instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, goAddrOff)], instructions.Count));
                    }
                    break;
            }
        }

        private void GenerateFunctionCall(FunctionCall call)
        {
            // 检查是否是内置函数println或print
            if (call.Function is Identifier ident && (ident.Name == "println" || ident.Name == "print"))
            {
                GeneratePrintln(call);
                return;
            }
            
            // 内置函数: len, cap, append
            if (call.Function is Identifier builtin)
            {
                if (builtin.Name == "len" && call.Arguments.Count == 1)
                {
                    var lenArgType = InferExpressionType(call.Arguments[0]);
                    if (lenArgType == GoTypeEnum.String)
                    {
                        EmitStrLen(() => GenerateExpression(call.Arguments[0]));
                    }
                    else
                    {
                        GenerateExpression(call.Arguments[0]);
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 0)]));
                    }
                    return;
                }
                if (builtin.Name == "cap" && call.Arguments.Count == 1)
                {
                    GenerateExpression(call.Arguments[0]);
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0+4"), new Operand(OperandType.REGISTER, 0)]));
                    return;
                }
                if (builtin.Name == "append" && call.Arguments.Count >= 1)
                {
                    GenerateExpression(call.Arguments[0]);
                    if (call.Arguments.Count >= 2)
                    {
                        // 保存slice指针, 检查是否需要扩容
                        instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 1)])); // len
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0+4"), new Operand(OperandType.REGISTER, 2)])); // cap
                        string appendNoGrow = NewLabel(), appendDone = NewLabel();
                        instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 2)]));
                        instructions.Add(new Instruction(OpCode.JL, [new Operand(OperandType.LABEL, appendNoGrow)]));
                        // 扩容: 新容量 = cap*2 + 1
                        instructions.Add(new Instruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 2)]));
                        instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 2)])); // newCap
                        instructions.Add(new Instruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 4)]));
                        instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 8)])); // allocSize = cap*4+8
                        instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)])); // 保存len
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2)]));
                        AddSyscall(40); // alloc
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0)])); // new slice ptr in R4
                        instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)])); // 恢复len
                        // 复制旧数据
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R4"), new Operand(OperandType.REGISTER, 1)])); // new.len = len
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R4+4"), new Operand(OperandType.REGISTER, 3)])); // new.cap = newCap
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R13+4"), new Operand(OperandType.REGISTER, 2)])); // old slice ptr
                        // 循环复制 len 个元素
                        string copyLoop = NewLabel(), copyEnd = NewLabel();
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 0)])); // i = 0
                        AddLabel(copyLoop);
                        instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 1)]));
                        instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, copyEnd)]));
                        instructions.Add(new Instruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 4)]));
                        instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 8)]));
                        instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2)])); // old[8+i*4]
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 0)]));
                        instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // save val
                        instructions.Add(new Instruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 4)]));
                        instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 8)]));
                        instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 4)])); // new[8+i*4]
                        instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)])); // val
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 1)]));
                        instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 1)]));
                        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, copyLoop)]));
                        AddLabel(copyEnd);
                        // 替换栈上的旧指针为新指针
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R13+4"), new Operand(OperandType.REGISTER, 4)]));
                        AddLabel(appendNoGrow);
                        // 计算目标地址, 写入新元素
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R13+4"), new Operand(OperandType.REGISTER, 1)])); // slice ptr
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0)])); // len
                        instructions.Add(new Instruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
                        instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 8)]));
                        instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)])); // dst = ptr+8+len*4
                        GenerateExpression(call.Arguments[1]);
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R0+0"), new Operand(OperandType.REGISTER, 0)]));
                        // 更新len
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R13+4"), new Operand(OperandType.REGISTER, 0)])); // slice ptr
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 2)])); // len
                        instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 2)]));
                        AddLabel(appendDone);
                        instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 0)])); // 返回slice指针
                    }
                    return;
                }
                if (builtin.Name == "copy" && call.Arguments.Count == 2)
                {
                    // copy(dst, src) — copy elements from src slice to dst slice
                    // Slice layout: [len:4][cap:4][data...]
                    // Returns min(len(dst), len(src)) in R0

                    // Evaluate dst and src, save pointers
                    GenerateExpression(call.Arguments[0]); // R0 = dst
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0)])); // R4 = dst
                    GenerateExpression(call.Arguments[1]); // R0 = src
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 0)])); // R5 = src

                    // R1 = src.len, R2 = dst.len
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R5"), new Operand(OperandType.REGISTER, 1)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R4"), new Operand(OperandType.REGISTER, 2)]));

                    // R2 = min(dst.len, src.len)
                    string copyCmp = NewLabel();
                    instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 1)]));
                    instructions.Add(new Instruction(OpCode.JLE, [new Operand(OperandType.LABEL, copyCmp)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 1)]));
                    AddLabel(copyCmp);

                    // Loop: i (R3) from 0 to count-1
                    string copyLoop = NewLabel(), copyEnd = NewLabel();
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 0)]));
                    AddLabel(copyLoop);
                    instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 2)]));
                    instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, copyEnd)]));

                    // Load src[8 + i*4]
                    instructions.Add(new Instruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 4)]));
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 5)]));
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 8)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)])); // save value

                    // Store to dst[8 + i*4]
                    instructions.Add(new Instruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 4)]));
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 4)]));
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 8)]));
                    instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)])); // restore value
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 1)]));

                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 1)]));
                    instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, copyLoop)]));
                    AddLabel(copyEnd);

                    // Return count in R0
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2)]));
                    return;
                }
                if (builtin.Name == "make" && call.Arguments.Count >= 1)
                {
                    // make(type, len) — MCU简化: 分配内存
                    // 切片布局: [len:4][cap:4][elem0][elem1]...
                    int lenVal = 0;
                    if (call.Arguments.Count >= 2 && call.Arguments[1] is NumberLiteral numLit)
                        int.TryParse(numLit.Value, out lenVal);
                    else if (call.Arguments.Count >= 2)
                    {
                        GenerateExpression(call.Arguments[1]);
                        instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    }
                    int allocSize = 8 + lenVal * 4;
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, allocSize)]));
                    AddSyscall(40);
                    // 写入 len 和 cap
                    if (call.Arguments.Count >= 2)
                    {
                        if (call.Arguments[1] is NumberLiteral)
                            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, lenVal)]));
                        else
                            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                    }
                    else
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 1)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0+4"), new Operand(OperandType.REGISTER, 1)]));
                    return;
                }
                // peek*(addr) → vml_peek*  (register-convention: R0=addr, result in R0)
                if (builtin.Name.StartsWith("peek") && call.Arguments.Count == 1)
                {
                    string runtimeFn = "vml_" + builtin.Name;
                    GenerateExpression(call.Arguments[0]);  // R0 = addr
                    instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, runtimeFn) }));
                    return;
                }
                // poke*(addr, val) → vml_poke*  (register-convention: R0=val, R1=addr)
                if (builtin.Name.StartsWith("poke") && call.Arguments.Count == 2)
                {
                    GenerateExpression(call.Arguments[0]);  // addr → R1
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                    GenerateExpression(call.Arguments[1]);  // val → R0
                    instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, "vml_" + builtin.Name) }));
                    return;
                }
                if (builtin.Name == "chipasm" && call.Arguments.Count >= 2)
                {
                    return;
                }
                // asm() 已移除 — 仅限 C/ObjC/C++ 语言使用，Go 通过 Lib/shared/vmlsys.c 调用系统功能
            }

            // 检查是否是fmt.Println或fmt.Printf
            if (call.Function is SelectorExpr selector)
            {
                if (selector.Expr is Identifier pkg && pkg.Name == "fmt")
                {
                    if (selector.Sel == "Println" || selector.Sel == "Printf" || selector.Sel == "Print")
                    {
                        GeneratePrintln(call);
                        return;
                    }
                }
            }

            // 先解析函数名 (conv 库函数需按命名约定确定首参数 C 类型)
            string funcName;
            if (call.Function is Identifier ident2)
            {
                funcName = ident2.Name;
            }
            else if (call.Function is SelectorExpr selExpr2)
            {
                // 方法调用: obj.Method() → 直接使用方法名
                funcName = selExpr2.Sel;
            }
            else if (call.Function is FuncLiteral funcLit)
            {
                // Inline anonymous function body and call it
                string lambdaLabel = $"lambda_{instructions.Count}";
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, lambdaLabel)]));
                // Clean up arg stack (none for no-arg case)
                AddPendingFuncLiteral(lambdaLabel, funcLit);
                return;
            }
            else
            {
                funcName = "unknown_func";
            }

            // conv 库函数首参数 C 类型: long→L0 寄存器, double→D0 寄存器
            // Go 的 int64 走 double 路径(D0), 而 conv 的 long 走 L0, 必须直接加载到 L0
            string? convArgType = ConvArgType(funcName);

            // 从右到左压入参数
            for (int i = call.Arguments.Count - 1; i >= 0; i--)
            {
                if (i == 0 && convArgType != null)
                {
                    GenerateConvArg(call.Arguments[i], convArgType);
                }
                else
                {
                    GenerateExpression(call.Arguments[i]);
                }
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0)
                }, instructions.Count));
            }

            instructions.Add(new Instruction(OpCode.CALL, new List<Operand>
            {
                new Operand(OperandType.LABEL, funcName)
            }, instructions.Count));

            // 清理参数栈
            if (call.Arguments.Count > 0)
            {
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.IMMEDIATE, call.Arguments.Count * 4)
                }, instructions.Count));
            }
        }

        /// <summary>conv 库函数按命名约定确定首参数的 C 类型 (long→L0, double→D0)</summary>
        private static string? ConvArgType(string funcName)
        {
            var f = funcName.ToLower();
            if (f.StartsWith("long_") || f.StartsWith("ulong_") || f.StartsWith("longto") || f == "ltoa")
                return "long";
            if (f.StartsWith("double_") || f.StartsWith("doubleto") || f == "dtoa")
                return "double";
            return null;
        }

        /// <summary>按 C 类型加载 conv 首参数: long→MOVEL(L0 寄存器), double→MOVED(D0 寄存器)</summary>
        private void GenerateConvArg(ASTNode arg, string cType)
        {
            if (cType == "long" && arg is NumberLiteral intLit && !intLit.IsFloat
                && long.TryParse(intLit.Value, out long lv))
            {
                // MOVEL 从数据段加载完整 64 位到 L0 (不截断为 int32)
                EmitLoadConstant(lv);
                return;
            }
            if (cType == "double" && arg is NumberLiteral flit && flit.IsFloat
                && double.TryParse(flit.Value, out double dv))
            {
                // MOVED 从数据段加载 double 到 D0 (不用 float32 截断)
                EmitLoadConstant(dv);
                return;
            }
            // 兜底: 变量/表达式等按普通方式生成
            GenerateExpression(arg);
        }

        private void GeneratePrintln(FunctionCall call)
        {
            for (int i = 0; i < call.Arguments.Count; i++)
            {
                var arg = call.Arguments[i];
                bool isString = arg is StringLiteral
                    || (arg is FunctionCall fc && fc.Function is Identifier fid && IsStringReturningFunc(fid.Name))
                    || (arg is Identifier id && IsStringReturningFunc(id.Name));
                bool isFloat = arg is NumberLiteral numLit && numLit.IsFloat;
                bool isBool = arg is BoolLiteral;

                GenerateExpression(arg);
                if (isFloat)
                    EmitPrintFloat();
                else if (isBool)
                    EmitPrintBool();
                else if (isString)
                    EmitPrintString();
                else
                    EmitPrintInt();

                if (i < call.Arguments.Count - 1)
                {
                    AddInstruction(OpCode.MOVE, Reg(0), Imm(32));
                    EmitPrintChar();
                }
            }
            EmitPrintNewline();
        }

        private void GenerateMethodCall(MethodCall methodCall)
        {
            // 简化处理
            GenerateExpression(methodCall.Receiver);
            // 压入接收者作为第一个参数
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0)
            }, instructions.Count));

            // 从右到左压入参数
            for (int i = methodCall.Arguments.Count - 1; i >= 0; i--)
            {
                GenerateExpression(methodCall.Arguments[i]);
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0)
                }, instructions.Count));
            }

            instructions.Add(new Instruction(OpCode.CALL, new List<Operand>
            {
                new Operand(OperandType.LABEL, methodCall.MethodName)
            }, instructions.Count));

            if (methodCall.Arguments.Count + 1 > 0)
            {
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.IMMEDIATE, (methodCall.Arguments.Count + 1) * 4)
                }, instructions.Count));
            }
        }

        private void GenerateIndexExpr(IndexExpr indexExpr)
        {
            var arrType = InferExpressionType(indexExpr.Array);
            if (arrType == GoTypeEnum.String)
            {
                // 字符串索引: 1字节步长, LOADB
                GenerateExpression(indexExpr.Array);
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                GenerateExpression(indexExpr.Index);
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                // ⚠ v0.96.193：原本是 `MOVEB [R0], R0` —— dest/src 写反，"取字节"变成了
                //   "把地址存回自己指向的地方"。同族问题（见下面数组分支的注释）。
                instructions.Add(new Instruction(OpCode.MOVEB, [new Operand(OperandType.REGISTER, 0), Mem("R0")]));
                return;
            }
            // 计算数组地址
            GenerateExpression(indexExpr.Array);

            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0)
            }, instructions.Count));

            GenerateExpression(indexExpr.Index);

            // 计算偏移量
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 1),
                new Operand(OperandType.IMMEDIATE, 4)
            }, instructions.Count));
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 1)
            }, instructions.Count));

            // ⚠ v0.96.193 修（两处）：
            //   ① **少跳了 VML 数组头**：`AllocateVmlArray`（基类）的布局是
            //      `[count, e0, e1, …]`，元素 i 在 `base + i*4 + 4`；这里原来只算 `base + i*4`
            //      ⇒ 下标 0 读到的是 count、其余整体错位一格。
            //   ② 最后那句"加载元素"写的是 `MOVE R0, R0` —— **自赋值、空操作**
            //      ⇒ R0 里留着的是**地址**，被当成元素值返回。实测：`a[2]`→8、`a[5]`→20、
            //      `a[7]`→28，**全是 `idx*4`**。这与 Swift 的 patch 0011 ③ 是同一个病
            //      （`MOVE dest, src` 的操作数写反），**"地址当值"族第六次**。
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.IMMEDIATE, 4)
            }, instructions.Count));

            instructions.Add(new Instruction(OpCode.POP, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 1)
            }, instructions.Count));

            // R0 = base + idx*4 + 4 → 取元素值回 R0
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.REGISTER, 1)
            }, instructions.Count));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.MEMORY, "R0")
            }, instructions.Count));
        }

        private void GenerateSelectorExpr(SelectorExpr selector)
        {
            // 检查是否是 struct 字段访问
            string fieldName = selector.Sel;
            GoType st = null;

            if (selector.Expr is Identifier ident && _varTypes.TryGetValue(ident.Name, out var vt) && vt == GoTypeEnum.Struct)
                st = GetVarStructType(ident.Name);

            if (st != null && st.Fields != null)
            {
                // struct 字段访问: 计算 base+offset 并加载
                int offset = GetFieldOffset(st, fieldName);
                GenerateExpression(selector.Expr); // 变量地址到 R0
                if (offset > 0)
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, offset)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 0)]));
            }
            else
            {
                // 非 struct：简化处理（原行为）
                GenerateExpression(selector.Expr);
            }
        }
    }
}
