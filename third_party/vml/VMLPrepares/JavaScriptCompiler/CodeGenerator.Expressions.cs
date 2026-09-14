using VMLAssembler;
using System.Collections.Generic;
using CompilerBase;

namespace JavaScriptCompiler
{
    public partial class CodeGenerator
    {
        private void GenerateLiteral(LiteralExpression literal)
        {
            // JS 数字统一按 F32 处理 (与 InferJSType 一致): double → float, 避免 MOVED 64位加载
            // 使 floatToStr 等 float 参数函数正确接收 32 位浮点位模式
            if (literal.Value is double d)
                EmitLoadConstant((float)d);
            else
                EmitLoadConstant(literal.Value);
        }
        
        private void GenerateVariable(VariableExpression variable)
        {
            if (_localVarOffsets.TryGetValue(variable.Name, out int off))
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.MEMORY, $"R14{off}")
                }));
            }
            else
            {
                string varLabel = $"var_{variable.Name}";
                if (!dataSection.ContainsKey(varLabel))
                    dataSection[varLabel] = 0;
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.LABEL, varLabel)
                }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.MEMORY, "R0")
                }));
            }
        }
        
        private static ExpType InferJSType(Expression expr)
        {
            if (expr is LiteralExpression lit)
            {
                if (lit.Value is double) return ExpType.F32;
                if (lit.Value is string) return ExpType.Ptr32;
                if (lit.Value is bool) return ExpType.I8;
            }
            return ExpType.I32;
        }

        private ExpVar WrapExpr(Expression node)
        {
            return ExpVar.Eval(InferJSType(node), () => GenerateExpression(node));
        }

        private ExpVar WrapTargetExpr(Expression expr)
        {
            if (expr is VariableExpression varExpr)
            {
                if (_localVarOffsets.TryGetValue(varExpr.Name, out int offset))
                    return ExpVar.Stack(offset, 14, ExpType.I32);
                string label = $"var_{varExpr.Name}";
                if (!dataSection.ContainsKey(label))
                    dataSection[label] = 0;
                return ExpVar.Data(label, ExpType.I32);
            }
            return WrapExpr(expr);
        }

        private void GenerateBinary(BinaryExpression binary)
        {
            var left = WrapExpr(binary.Left);
            var right = WrapExpr(binary.Right);
            string op = binary.Operator switch
            {
                TokenType.Plus => "+", TokenType.Minus => "-", TokenType.Multiply => "*",
                TokenType.Divide => "/", TokenType.Modulo => "%",
                TokenType.Equal => "==", TokenType.StrictEqual => "==",
                TokenType.NotEqual => "!=", TokenType.StrictNotEqual => "!=",
                TokenType.LessThan => "<", TokenType.LessThanOrEqual => "<=",
                TokenType.GreaterThan => ">", TokenType.GreaterThanOrEqual => ">=",
                TokenType.LogicalAnd => "&&", TokenType.LogicalOr => "||",
                TokenType.BitwiseAnd => "&", TokenType.BitwiseOr => "|", TokenType.BitwiseXor => "^",
                TokenType.LeftShift => "<<", TokenType.RightShift => ">>", TokenType.UnsignedRightShift => ">>",
                _ => ""
            };
            if (_expr!.EmitStandardBinaryOps(op, left, right)) return;
            if (_expr!.EmitBitwiseOps(op, left, right)) return;
            if (binary.Operator == TokenType.Exponent)
            {
                // ** 幂运算 — push right (exponent), push left (base), CALL shared_pow
                GenerateExpression(binary.Right); AddInstruction(OpCode.PUSH, Reg(0));
                GenerateExpression(binary.Left); AddInstruction(OpCode.PUSH, Reg(0));
                AddCall("pow");
                return;
            }
            _expr!.EmitBinOp(left, right, "+"); // fallback
        }
        
        private void GenerateUnary(UnaryExpression unary)
        {
            switch (unary.Operator)
            {
                case TokenType.Minus:
                    _expr!.EmitNeg(WrapExpr(unary.Operand));
                    break;

                case TokenType.LogicalNot:
                    _expr!.EmitNot(WrapExpr(unary.Operand));
                    break;

                case TokenType.BitwiseNot:
                    _expr!.EmitBitNot(WrapExpr(unary.Operand));
                    break;

                case TokenType.Increment:
                    if (unary.Operand is VariableExpression)
                    {
                        var target = WrapTargetExpr(unary.Operand);
                        if (unary.IsPostfix) _expr!.EmitPostfixInc(target);
                        else _expr!.EmitPrefixInc(target);
                    }
                    break;
                case TokenType.Decrement:
                    if (unary.Operand is VariableExpression)
                    {
                        var target = WrapTargetExpr(unary.Operand);
                        if (unary.IsPostfix) _expr!.EmitPostfixDec(target);
                        else _expr!.EmitPrefixDec(target);
                    }
                    break;
                case TokenType.Typeof:
                    // typeof: 返回类型字符串的地址
                    string typeStr;
                    if (unary.Operand is LiteralExpression lit)
                    {
                        typeStr = lit.Type switch { "number" => "number", "string" => "string", "boolean" => "boolean", "object" => "object", "undefined" => "undefined", _ => "number" };
                    }
                    else
                    {
                        // dynamic: check at runtime (simplified: always "object")
                        typeStr = "object";
                    }
                    string typeLabel = $"typeof_str_{labelCounter++}";
                    dataSection[typeLabel] = typeStr + "\0";
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, typeLabel)]));
                    break;

                default:
                    // 其他一元操作符，保持不变
                    break;
            }
        }
        
        private void GenerateAssignment(AssignmentExpression assign)
        {
            // 处理 this.x = value
            if (assign.Left is MemberExpression memExpr && memExpr.Object is VariableExpression ve && ve.Name == "this")
            {
                GenerateExpression(assign.Right);
                int fieldOff = GetFieldOffset(memExpr.Property);
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                string thisLabel = FindThisLabel();
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, thisLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
                instructions.Add(new Instruction(OpCode.POP, [Reg(0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R1+{fieldOff}"), Reg(0)]));
                return;
            }

            // 处理 obj.prop = value for known-class objects
            if (assign.Left is MemberExpression objMemExpr && objMemExpr.Object is VariableExpression objVe2
                && _varClassMap.TryGetValue(objVe2.Name, out string objClsName))
            {
                int fieldOff = GetClassFieldOffset(objClsName, objMemExpr.Property);
                if (fieldOff > 0)
                {
                    GenerateExpression(assign.Right);
                    instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                    GenerateExpression(objMemExpr.Object); // R1 = object ptr
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), Reg(0)]));
                    instructions.Add(new Instruction(OpCode.POP, [Reg(0)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R1+{fieldOff}"), Reg(0)]));
                    return;
                }
            }

            // 处理 obj[expr] = value
            if (assign.Left is IndexExpression idxExpr)
            {
                // Generate object base and save on stack (index eval may clobber registers)
                GenerateExpression(idxExpr.Object);
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                // Generate index → R0, then compute offset
                GenerateExpression(idxExpr.Index);
                instructions.Add(new Instruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
                // Restore object base and compute destination address
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
                // Save destination address on stack before evaluating RHS (which may clobber registers)
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 1)]));
                GenerateExpression(assign.Right);
                // Restore destination address and store
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0)]));
                return;
            }

            // 复合赋值: +=, -=, *=, /=, %=
            if (assign.Operator != TokenType.Assign && assign.Left is VariableExpression cvExpr)
            {
                string op = assign.Operator switch
                {
                    TokenType.PlusAssign => "+",
                    TokenType.MinusAssign => "-",
                    TokenType.MultiplyAssign => "*",
                    TokenType.DivideAssign => "/",
                    TokenType.ModuloAssign => "%",
                    _ => "+"
                };
                _expr!.EmitCompoundAssign(WrapTargetExpr(assign.Left), WrapExpr(assign.Right), op);
                return;
            }

            // 生成右值
            GenerateExpression(assign.Right);

            // 存储到变量
            if (assign.Left is VariableExpression varExpr)
            {
                if (_localVarOffsets.TryGetValue(varExpr.Name, out int aOff))
                {
                    // MOVE [R14+off], R0 — 统一 STORE (内存优先)
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                        new Operand(OperandType.MEMORY, $"R14{aOff}"),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                }
                else
                {
                    string varLabel = $"var_{varExpr.Name}";
                    if (!dataSection.ContainsKey(varLabel))
                        dataSection[varLabel] = 0;
                    // 全局变量: LEA R1, varLabel; MOVE [R1], R0
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                        new Operand(OperandType.REGISTER, 1),
                        new Operand(OperandType.LABEL, varLabel)
                    }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                        new Operand(OperandType.MEMORY, "R1"),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                }
            }
        }
        private void GenerateArrayLiteral(ArrayLiteralExpression array)
        {
            int count = array.Elements.Count;
            // Allocate array: 4 bytes (length) + count * 4 bytes (elements)
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 4 + count * 4)]));
            EmitAlloc();
            // Store length at [ptr+0]
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), Reg(0)]));
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(2), new Operand(OperandType.IMMEDIATE, count)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), Reg(2)]));
            // Evaluate and store each element
            for (int i = 0; i < count; i++)
            {
                GenerateExpression(array.Elements[i]);
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R1+{4 + i * 4}"), Reg(0)]));
            }
            // Return array ptr in R0
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), Reg(1)]));
        }

        private void GenerateObjectLiteral(ObjectLiteralExpression obj)
        {
            // 简化处理：为每个属性生成代码
            foreach (var kvp in obj.Properties)
            {
                GenerateExpression(kvp.Value);
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> {
                    new Operand(OperandType.REGISTER, 0)
                }));
            }
            // 计算属性: { [expr]: value }
            foreach (var (keyExpr, valueExpr) in obj.ComputedProperties)
            {
                // keyExpr 在运行时可求值，此处简化：仅生成 value
                GenerateExpression(valueExpr);
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> {
                    new Operand(OperandType.REGISTER, 0)
                }));
            }
        }

        private void GenerateIndexRead(IndexExpression indexExpr)
        {
            // Generate object base address and save on stack (index eval may clobber registers)
            GenerateExpression(indexExpr.Object);
            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
            // Generate index expression → R0
            GenerateExpression(indexExpr.Index);
            // Compute offset
            instructions.Add(new Instruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
            // Restore base pointer and compute final address
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
            instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1")]));
        }

        private void GenerateMember(MemberExpression member)
        {
            // Handle super.x reads — resolve from parent class
            if (member.Object is VariableExpression sve && sve.Name == "super")
            {
                if (_currentClassName != null && _classParentMap.TryGetValue(_currentClassName, out string? superParent))
                {
                    int superFieldOff = GetClassFieldOffset(superParent, member.Property);
                    string thisLabel = FindThisLabel();
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, thisLabel)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                    if (superFieldOff > 0)
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, $"R0+{superFieldOff}")]));
                    else
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
                    return;
                }
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
                return;
            }

            // Handle this.x reads
            if (member.Object is VariableExpression ve && ve.Name == "this")
            {
                int fieldOff = GetFieldOffset(member.Property);
                string thisLabel = FindThisLabel();
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, thisLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, $"R0+{fieldOff}")]));
                return;
            }

            // Handle obj.prop for objects with known class (compile-time field resolution + prototype chain)
            if (member.Object is VariableExpression objVe && _varClassMap.TryGetValue(objVe.Name, out string objClassName))
            {
                int fieldOff = GetClassFieldOffset(objClassName, member.Property);
                if (fieldOff > 0)
                {
                    GenerateExpression(member.Object); // R0 = object ptr
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, $"R0+{fieldOff}")]));
                    return;
                }
            }

            GenerateExpression(member.Object);
            if (member.Property == "length")
            {
                // .length on array: read [ptr+0] (length header)
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]));
            }
        }

        private void GenerateConditional(ConditionalExpression condExpr)
        {
            string falseLabel = $"cond_false_{labelCounter++}";
            string endLabel = $"cond_end_{labelCounter++}";
            GenerateExpression(condExpr.Condition);
            instructions.Add(new Instruction(OpCode.JZ, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, falseLabel)]));
            GenerateExpression(condExpr.TrueValue);
            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, endLabel)]));
            labels[falseLabel] = instructions.Count;
            GenerateExpression(condExpr.FalseValue);
            labels[endLabel] = instructions.Count;
        }

        private void GenerateArrayIterateMethod(Expression arrayExpr, Expression callbackExpr, string method)
        {
            // 1. Generate array pointer and store it
            GenerateExpression(arrayExpr);
            string arrLabel = $"__arr_{labelCounter}";
            labelCounter++;
            dataSection[arrLabel] = 0;
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, arrLabel), Reg(0)]));

            // 2. Generate callback function (if inline) and get its label
            string cbFuncLabel;
            bool isInlineFunc = callbackExpr is FunctionExpression || callbackExpr is ArrowFunctionExpression;

            if (isInlineFunc)
            {
                // Emit JMP to skip function body in normal execution flow
                string afterFuncLabel = $"after_func_{labelCounter++}";
                instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, afterFuncLabel)]));
                int prevLabelId = labelCounter;
                GenerateExpression(callbackExpr);
                cbFuncLabel = callbackExpr is FunctionExpression ? $"func_expr_{prevLabelId}" : $"arrow_{prevLabelId}";
                labels[afterFuncLabel] = instructions.Count;
            }
            else
            {
                // Variable callback: store address, fallback (VML CALL doesn't support register-indirect)
                string cbStoreLabel = $"__cb_{labelCounter++}";
                dataSection[cbStoreLabel] = 0;
                GenerateExpression(callbackExpr);
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, cbStoreLabel), Reg(0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
                return;
            }

            // 3. For map/filter: allocate result array
            string? resultLabel = null;
            string? resultIdxLabel = null;
            if (method != "forEach")
            {
                resultLabel = $"__result_{labelCounter}";
                resultIdxLabel = $"__res_idx_{labelCounter}";
                labelCounter++;
                dataSection[resultLabel] = 0;
                dataSection[resultIdxLabel] = 0;
                // Allocate: length header (4 bytes) + length * 4 for elements
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, arrLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")])); // length
                instructions.Add(new Instruction(OpCode.SHL, [Reg(0), new Operand(OperandType.IMMEDIATE, 2)])); // length * 4
                instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 4)])); // + 4 for header
                EmitAlloc(); // malloc
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, resultLabel), Reg(0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), Reg(0)])); // R1 = result ptr
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, arrLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")])); // length
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), Reg(0)])); // store length
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, resultIdxLabel), Reg(0)]));
            }

            // 4. Set up loop variables
            string loopStart = $"loop_{labelCounter}";
            string loopEnd = $"loop_end_{labelCounter}";
            string idxLabel = $"__idx_{labelCounter}";
            labelCounter++;
            dataSection[idxLabel] = 0;

            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, idxLabel), Reg(0)]));

            // 5. Loop: while (index < array.length)
            labels[loopStart] = instructions.Count;

            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, idxLabel)]));
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, arrLabel)]));
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
            instructions.Add(new Instruction(OpCode.CMP, [Reg(0), Reg(1)]));
            instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, loopEnd)]));

            // Load element: arr[4 + index*4]
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, idxLabel)]));
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
            instructions.Add(new Instruction(OpCode.SHL, [Reg(0), new Operand(OperandType.IMMEDIATE, 2)]));
            instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 4)]));
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, arrLabel)]));
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
            instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(1)]));
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));

            // 6. Call callback(element)
            instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, cbFuncLabel)]));
            instructions.Add(new Instruction(OpCode.ADD, [Reg(13), new Operand(OperandType.IMMEDIATE, 4)]));

            // 7. For map: store callback result
            if (method == "map")
            {
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)])); // save result
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, resultIdxLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                instructions.Add(new Instruction(OpCode.SHL, [Reg(0), new Operand(OperandType.IMMEDIATE, 2)]));
                instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 4)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, resultLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
                instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(1)]));
                instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), Reg(1)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, resultIdxLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, resultIdxLabel), Reg(0)]));
            }

            // 8. For filter: if callback returns truthy, copy element
            if (method == "filter")
            {
                string skipStoreLabel = $"filter_skip_{labelCounter++}";
                instructions.Add(new Instruction(OpCode.JZ, [Reg(0), new Operand(OperandType.LABEL, skipStoreLabel)]));
                // Reload element
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, idxLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                instructions.Add(new Instruction(OpCode.SHL, [Reg(0), new Operand(OperandType.IMMEDIATE, 2)]));
                instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 4)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, arrLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
                instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(1)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                // Store at result[4 + resultIdx*4]
                instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, resultIdxLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                instructions.Add(new Instruction(OpCode.SHL, [Reg(0), new Operand(OperandType.IMMEDIATE, 2)]));
                instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 4)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, resultLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
                instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(1)]));
                instructions.Add(new Instruction(OpCode.POP, [Reg(1)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), Reg(1)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, resultIdxLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, resultIdxLabel), Reg(0)]));
                labels[skipStoreLabel] = instructions.Count;
            }

            // 9. Increment loop index
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, idxLabel)]));
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
            instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, idxLabel), Reg(0)]));

            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, loopStart)]));

            // 10. Loop end
            labels[loopEnd] = instructions.Count;

            if (method == "forEach")
            {
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
            }
            else
            {
                // Update result length to actual count and return ptr
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, resultIdxLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, resultLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), Reg(0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, resultLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
            }
        }

        private void GenerateTemplate(TemplateExpression template)
        {
            // Check if all parts are string literals for compile-time concatenation
            bool allStatic = true;
            foreach (var part in template.Parts)
            {
                if (!(part is LiteralExpression lit && lit.Value is string))
                {
                    allStatic = false;
                    break;
                }
            }

            if (allStatic)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var part in template.Parts)
                    sb.Append(((LiteralExpression)part).Value as string);
                string result = sb.ToString();
                string label = $"str_{labelCounter++}";
                dataSection[label] = WStr(result);
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, label)]));
            }
            else
            {
                // Dynamic template: allocate buffer and concatenate
                string bufLabel = $"__tpl_{labelCounter++}";
                dataSection[bufLabel] = 0;

                // Estimate total length for allocation
                int totalLen = 1; // null terminator
                foreach (var part in template.Parts)
                {
                    if (part is LiteralExpression lit && lit.Value is string s)
                        totalLen += s.Length;
                    else
                        totalLen += 16; // estimate for dynamic expressions
                }

                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, totalLen)]));
                EmitAlloc();
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, bufLabel), Reg(0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), Reg(0)])); // R1 = write cursor

                foreach (var part in template.Parts)
                {
                    if (part is LiteralExpression lit && lit.Value is string s)
                    {
                        string strLabel = $"str_{labelCounter++}";
                        dataSection[strLabel] = WStr(s);
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, strLabel)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(2), Reg(0)])); // R2 = src
                        for (int i = 0; i < s.Length; i++)
                        {
                            instructions.Add(new Instruction(OpCode.MOVEB, [Reg(0), new Operand(OperandType.MEMORY, "R2")]));
                            instructions.Add(new Instruction(OpCode.MOVEB, [new Operand(OperandType.MEMORY, "R1"), Reg(0)]));
                            if (i < s.Length - 1)
                            {
                                instructions.Add(new Instruction(OpCode.ADD, [Reg(2), new Operand(OperandType.IMMEDIATE, 1)]));
                                instructions.Add(new Instruction(OpCode.ADD, [Reg(1), new Operand(OperandType.IMMEDIATE, 1)]));
                            }
                        }
                        if (s.Length > 0)
                        {
                            instructions.Add(new Instruction(OpCode.ADD, [Reg(2), new Operand(OperandType.IMMEDIATE, 1)]));
                            instructions.Add(new Instruction(OpCode.ADD, [Reg(1), new Operand(OperandType.IMMEDIATE, 1)]));
                        }
                    }
                    else
                    {
                        GenerateExpression(part);
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), Reg(0)]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(1), new Operand(OperandType.IMMEDIATE, 4)]));
                    }
                }

                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
                instructions.Add(new Instruction(OpCode.MOVEB, [new Operand(OperandType.MEMORY, "R1"), Reg(0)]));
                instructions.Add(new Instruction(OpCode.ADD, [Reg(1), new Operand(OperandType.IMMEDIATE, 1)]));

                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, bufLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
            }
        }

        private void EmitLogicalAnd()
        {
            string falseLabel = $"land_false_{labelCounter}";
            string endLabel = $"land_end_{labelCounter}";
            labelCounter++;
            // R1 = left, R0 = right
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
            instructions.Add(new Instruction(OpCode.JZ, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, falseLabel) }));
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.JZ, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, falseLabel) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
            labels[falseLabel] = instructions.Count;
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
            labels[endLabel] = instructions.Count;
        }

        private void EmitLogicalOr()
        {
            string trueLabel = $"lor_true_{labelCounter}";
            string endLabel = $"lor_end_{labelCounter}";
            labelCounter++;
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
            instructions.Add(new Instruction(OpCode.JNZ, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, trueLabel) }));
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.JNZ, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, trueLabel) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
            labels[trueLabel] = instructions.Count;
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) }));
            labels[endLabel] = instructions.Count;
        }

    }
}
