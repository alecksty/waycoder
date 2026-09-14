using VMLAssembler;
using System.Collections.Generic;
using CompilerBase;

namespace JavaCompiler
{
    public partial class CodeGenerator
    {
        private void GenerateExpression(Expression expression)
        {
            if (expression is LiteralExpression literal)
            {
                GenerateLiteral(literal);
            }
            else if (expression is VariableExpression variable)
            {
                GenerateVariable(variable);
            }
            else if (expression is BinaryExpression binary)
            {
                GenerateBinaryExpression(binary);
            }
            else if (expression is UnaryExpression unary)
            {
                GenerateUnaryExpression(unary);
            }
            else if (expression is MethodCallExpression methodCall)
            {
                GenerateMethodCall(methodCall);
            }
            else if (expression is AssignmentExpression assignment)
            {
                GenerateAssignment(assignment);
            }
            else if (expression is ParenthesizedExpression paren)
            {
                GenerateExpression(paren.Expression);
            }
            else if (expression is CastExpression cast)
            {
                JavaTypeEnum fromType = InferExpressionType(cast.Expression);
                JavaTypeEnum toType = GetJavaTypeEnum(cast.TargetType);
                GenerateExpression(cast.Expression);
                if (fromType != toType)
                {
                    GenerateTypeConversion(fromType, toType);
                }
            }
            else if (expression is NewExpression newExpr)
            {
                if (newExpr.Type.EndsWith("[]"))
                {
                    // new Type[N] — 数组分配
                    GenerateExpression(newExpr.Arguments[0]); // size in R0
                    // Allocate: (size + 1) * 4 bytes
                    AddRI(OpCode.ADD, 0, 1);
                    AddInstruction(OpCode.SHL, Imm(2), Reg(0));
                    AddSyscall(40);
                    AddRR(OpCode.MOVE, 0, 1);
                    AddRR(OpCode.MOVE, 0, 0);
                    AddInstruction(OpCode.MOVE, Mem("R1"), Reg(0));
                    AddRR(OpCode.MOVE, 1, 0);
                    return;
                }
                // 分配对象内存（简化：8 字节）
                AddInstruction(OpCode.MOVE, Reg(0), Imm(8));
                AddSyscall(40);
                // 调用构造函数
                string ctorLabel = newExpr.Type;
                for (int i = newExpr.Arguments.Count - 1; i >= 0; i--)
                {
                    GenerateExpression(newExpr.Arguments[i]);
                    AddInstruction(OpCode.PUSH, Reg(0));
                }
                AddInstruction(OpCode.PUSH, Reg(0)); // this
                AddCall(ctorLabel);
                AddInstruction(OpCode.ADD, Reg(13), Imm((newExpr.Arguments.Count + 1) * 4));
            }
            else if (expression is FieldAccessExpression fieldExpr)
            {
                if (fieldExpr.FieldName == "length" && fieldExpr.Target is VariableExpression arrVar)
                {
                    // array.length → 加载数组指针，读取 [ptr+0]
                    string ptrLabel = $"__lenptr_{labelCounter}";
                    labelCounter++;
                    dataSection[ptrLabel] = 0;
                    GenerateExpression(fieldExpr.Target);
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, ptrLabel), new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.LABEL, ptrLabel)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1")]));
                    return;
                }
                // 枚举常量引用: EnumType.VALUE → dataSection[EnumType_VALUE]
                if (fieldExpr.Target is VariableExpression enumVar && dataSection.ContainsKey($"{enumVar.Name}_{fieldExpr.FieldName}"))
                {
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, $"{enumVar.Name}_{fieldExpr.FieldName}")]));
                    return;
                }
                GenerateExpression(fieldExpr.Target);
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]));
            }
            else if (expression is ConditionalExpression condExpr)
            {
                _expr!.EmitConditional(WrapExpr(condExpr.Condition), WrapExpr(condExpr.TrueValue), WrapExpr(condExpr.FalseValue));
            }
            else if (expression is ArrayAccessExpression arrExpr)
            {
                GenerateArrayAccess(arrExpr);
            }
            else if (expression is CastExpression castExpr)
            {
                JavaTypeEnum fromType = InferExpressionType(castExpr.Expression);
                JavaTypeEnum toType = GetJavaTypeEnum(castExpr.TargetType);
                GenerateExpression(castExpr.Expression);
                if (fromType != toType)
                {
                    GenerateTypeConversion(fromType, toType);
                }
            }
            else if (expression is ArrayInitializerExpression arrInit)
            {
                int count = arrInit.Elements.Count;
                int allocSize = (count + 1) * 4;
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, allocSize)]));
                EmitAlloc();
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, count)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), new Operand(OperandType.REGISTER, 0)]));
                for (int i = 0; i < arrInit.Elements.Count; i++)
                {
                    GenerateExpression(arrInit.Elements[i]);
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, $"R1+{(i + 1) * 4}"), new Operand(OperandType.REGISTER, 0)]));
                }
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
            }
            else if (expression is InstanceofExpression instExpr)
            {
                EmitCompareToBool(() => GenerateExpression(instExpr.Expression), OpCode.JNE);
            }
            else
            {
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
            }
        }
        
        /// <summary>
        /// 推断表达式的类型
        /// </summary>
        private JavaTypeEnum InferExpressionType(Expression expr)
        {
            if (expr is LiteralExpression literal)
            {
                if (literal.Value is bool) return JavaTypeEnum.Boolean;
                if (literal.Value is char) return JavaTypeEnum.Char;
                if (literal.Value is int i)
                {
                    if (i >= -128 && i <= 127) return JavaTypeEnum.Byte;
                    if (i >= -32768 && i <= 32767) return JavaTypeEnum.Short;
                    return JavaTypeEnum.Int;
                }
                if (literal.Value is long) return JavaTypeEnum.Long;
                if (literal.Value is float) return JavaTypeEnum.Float;
                if (literal.Value is double) return JavaTypeEnum.Double;
                if (literal.Value is string) return JavaTypeEnum.String;
            }
            else if (expr is VariableExpression varExpr)
            {
                return _varTypes.ContainsKey(varExpr.Name) ? _varTypes[varExpr.Name] : JavaTypeEnum.Int;
            }
            else if (expr is BinaryExpression binExpr)
            {
                // 返回两个操作数的更宽类型
                JavaTypeEnum left = InferExpressionType(binExpr.Left);
                JavaTypeEnum right = InferExpressionType(binExpr.Right);
                return WiderType(left, right);
            }
            else if (expr is CastExpression castExpr)
            {
                return GetJavaTypeEnum(castExpr.TargetType);
            }
            else if (expr is MethodCallExpression methodCallExpr)
            {
                if (_methodReturnTypes.TryGetValue(methodCallExpr.MethodName, out var retType))
                    return retType;
                // 未知方法默认返回 Int (如 System.out.print 返回 void，但此处用作表达式默认)
                return JavaTypeEnum.Int;
            }

            return JavaTypeEnum.Int;
        }

        /// <summary>返回两个类型中更宽的类型（用于二元表达式类型推断）</summary>
        private static JavaTypeEnum WiderType(JavaTypeEnum a, JavaTypeEnum b)
        {
            // 任一为 double → double
            if (a == JavaTypeEnum.Double || b == JavaTypeEnum.Double) return JavaTypeEnum.Double;
            // 任一为 float → float
            if (a == JavaTypeEnum.Float || b == JavaTypeEnum.Float) return JavaTypeEnum.Float;
            // 任一为 long → long
            if (a == JavaTypeEnum.Long || b == JavaTypeEnum.Long) return JavaTypeEnum.Long;
            // 默认 int
            if (a == JavaTypeEnum.Int || b == JavaTypeEnum.Int) return JavaTypeEnum.Int;
            return a > b ? a : b;
        }

        private void GenerateLiteral(LiteralExpression literal)
        {
            // 64位整数字面量 → MOVEL (即使值在 32 位范围内, 也需 64 位表示以匹配 L 寄存器语义)
            if (literal.Value is long l)
            {
                string dlabel = $"i64_{labelCounter++}";
                dataSection[dlabel] = l;
                instructions.Add(new Instruction(OpCode.MOVEL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, dlabel)]));
                return;
            }
            EmitLoadConstant(literal.Value);
        }
        
        private void GenerateVariable(VariableExpression variable)
        {
            if (variable.Name == "this") return; // this 引用
            JavaTypeEnum varType = _varTypes.ContainsKey(variable.Name) ? _varTypes[variable.Name] : JavaTypeEnum.Int;
            OpCode loadOp = GetLoadInstruction(varType);

            if (_varOffsets.TryGetValue(variable.Name, out int offset))
            {
                instructions.Add(new Instruction(loadOp, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, Vars.FormatOffset(-offset))]));
            }
            else
            {
                string label = $"var_{variable.Name}";
                if (!dataSection.ContainsKey(label)) dataSection[label] = 0;
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, label)]));
            }
        }
        
        private static ExpType JavaTypeToExpType(JavaTypeEnum t) => t switch
        {
            JavaTypeEnum.Boolean or JavaTypeEnum.Byte => ExpType.I8,
            JavaTypeEnum.Short or JavaTypeEnum.Char => ExpType.I16,
            JavaTypeEnum.Int => ExpType.I32,
            JavaTypeEnum.Long => ExpType.I64,   // 64位整数使用 L 族指令 (MOVEL/ADDL/DIVL)
            JavaTypeEnum.Float => ExpType.F32,
            JavaTypeEnum.Double => ExpType.F64,
            JavaTypeEnum.String or JavaTypeEnum.Array or JavaTypeEnum.Object => ExpType.Ptr32,
            _ => ExpType.I32,
        };

        private ExpVar WrapExpr(Expression expr)
        {
            var javaType = InferExpressionType(expr);
            return ExpVar.Eval(JavaTypeToExpType(javaType), () => GenerateExpression(expr));
        }

        private ExpVar WrapTargetExpr(Expression expr)
        {
            if (expr is VariableExpression varExpr && _varOffsets.TryGetValue(varExpr.Name, out int offset))
            {
                var javaType = _varTypes.TryGetValue(varExpr.Name, out var t) ? t : JavaTypeEnum.Int;
                return ExpVar.Stack(-offset, 14, JavaTypeToExpType(javaType));
            }
            return WrapExpr(expr);
        }

        private void GenerateBinaryExpression(BinaryExpression binary)
        {
            JavaTypeEnum leftType = InferExpressionType(binary.Left);
            JavaTypeEnum rightType = InferExpressionType(binary.Right);

            // String concat
            if (binary.Operator == TokenType.Plus && (leftType == JavaTypeEnum.String || rightType == JavaTypeEnum.String))
            {
                EmitStrCat(() => GenerateExpression(binary.Left), () => GenerateExpression(binary.Right));
                return;
            }

            // Shift operators
            if (binary.Operator == TokenType.LeftShift)
            {
                _expr!.EmitShl(WrapExpr(binary.Left), WrapExpr(binary.Right));
                return;
            }
            if (binary.Operator is TokenType.RightShift or TokenType.UnsignedRightShift)
            {
                _expr!.EmitShr(WrapExpr(binary.Left), WrapExpr(binary.Right));
                return;
            }

            var left = WrapExpr(binary.Left);
            var right = WrapExpr(binary.Right);

            switch (binary.Operator)
            {
                case TokenType.Plus:  _expr!.EmitBinOp(left, right, "+"); break;
                case TokenType.Minus: _expr!.EmitBinOp(left, right, "-"); break;
                case TokenType.Multiply: _expr!.EmitBinOp(left, right, "*"); break;
                case TokenType.Divide: _expr!.EmitBinOp(left, right, "/"); break;
                case TokenType.Modulo: _expr!.EmitBinOp(left, right, "%"); break;

                case TokenType.BitwiseAnd: _expr!.EmitBitAnd(left, right); break;
                case TokenType.BitwiseOr:  _expr!.EmitBitOr(left, right); break;
                case TokenType.BitwiseXor: _expr!.EmitBitXor(left, right); break;
                case TokenType.LogicalAnd: _expr!.EmitAnd(left, right); break;
                case TokenType.LogicalOr:  _expr!.EmitOr(left, right); break;

                case TokenType.Equal:            _expr!.EmitCmp(left, right, "=="); break;
                case TokenType.NotEqual:         _expr!.EmitCmp(left, right, "!="); break;
                case TokenType.LessThan:         _expr!.EmitCmp(left, right, "<"); break;
                case TokenType.LessThanOrEqual:  _expr!.EmitCmp(left, right, "<="); break;
                case TokenType.GreaterThan:      _expr!.EmitCmp(left, right, ">"); break;
                case TokenType.GreaterThanOrEqual: _expr!.EmitCmp(left, right, ">="); break;
            }
        }

        private void GenerateUnaryExpression(UnaryExpression unary)
        {
            switch (unary.Operator)
            {
                case TokenType.Minus:
                    _expr!.EmitNeg(WrapExpr(unary.Operand));
                    break;
                case TokenType.LogicalNot:
                case TokenType.BitwiseNot:
                    _expr!.EmitNot(WrapExpr(unary.Operand));
                    break;
                case TokenType.Increment:
                    if (unary.Operand is VariableExpression incVar && _varOffsets.ContainsKey(incVar.Name))
                    {
                        var target = WrapTargetExpr(unary.Operand);
                        if (unary.IsPostfix) _expr!.EmitPostfixInc(target);
                        else _expr!.EmitPrefixInc(target);
                    }
                    break;
                case TokenType.Decrement:
                    if (unary.Operand is VariableExpression decVar && _varOffsets.ContainsKey(decVar.Name))
                    {
                        var target = WrapTargetExpr(unary.Operand);
                        if (unary.IsPostfix) _expr!.EmitPostfixDec(target);
                        else _expr!.EmitPrefixDec(target);
                    }
                    break;
            }
        }
        
        private void GenerateMethodCall(MethodCallExpression methodCall)
        {
            // String 方法处理
    // String.length() → 调用外置 vml_str_len
    if (methodCall.Target != null && methodCall.MethodName == "length" && methodCall.Arguments.Count == 0)
    {
        GenerateExpression(methodCall.Target);
        // MOVE R1, R0 保存字符串地址；CALL vml_str_len；结果在 R0
        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));
        instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "strlen")]));
        return;
    }

    // String.charAt(index) → 调用外置 vml_str_at
    if (methodCall.Target != null && methodCall.MethodName == "charAt" && methodCall.Arguments.Count == 1)
    {
        GenerateExpression(methodCall.Target);    // R0 = string
        instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
        GenerateExpression(methodCall.Arguments[0]); // R0 = index
        instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)])); // R1 = string
        instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "str_charat")]));
        return;
    }

    // String.equals(other) → 调用外置 vml_str_cmp
    if (methodCall.MethodName == "equals" && methodCall.Arguments.Count == 1 && methodCall.Target != null)
    {
        EmitStrCmp(
            () => GenerateExpression(methodCall.Target),
            () => GenerateExpression(methodCall.Arguments[0]));
        // vml_str_cmp returns -1/0/1 in R0; if R0 == 0 (strings equal), return 1, else 0
        EmitCompareToBool(() => {}, OpCode.JE);
        return;
    }

    // System.arraycopy(src, srcPos, dest, destPos, length)
            if (methodCall.MethodName == "arraycopy" && methodCall.Arguments.Count == 5)
            {
                string loopLabel = $"__ac_loop_{labelCounter}";
                string doneLabel = $"__ac_done_{labelCounter}";
                labelCounter++;

                // byte count = length * 4 → R3
                GenerateExpression(methodCall.Arguments[4]); // length → R0
                instructions.Add(new Instruction(OpCode.SHL, [new Operand(OperandType.IMMEDIATE, 2), new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 0)]));

                // src_ptr = src + 4 + srcPos * 4 → R1
                GenerateExpression(methodCall.Arguments[1]); // srcPos → R0
                instructions.Add(new Instruction(OpCode.SHL, [new Operand(OperandType.IMMEDIATE, 2), new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0)]));
                GenerateExpression(methodCall.Arguments[0]); // src → R0
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 4)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0)]));

                // dest_ptr = dest + 4 + destPos * 4 → R2
                GenerateExpression(methodCall.Arguments[3]); // destPos → R0
                instructions.Add(new Instruction(OpCode.SHL, [new Operand(OperandType.IMMEDIATE, 2), new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0)]));
                GenerateExpression(methodCall.Arguments[2]); // dest → R0
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 4)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0)]));

                // Copy loop: for (; R3 > 0; R1+=4, R2+=4, R3-=4)
                AddLabel(loopLabel);
                instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 0)]));
                instructions.Add(new Instruction(OpCode.JLE, [new Operand(OperandType.LABEL, doneLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1")]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R2"), new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4)]));
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 4)]));
                instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 4)]));
                instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, loopLabel)]));
                AddLabel(doneLabel);
                return;
            }

            // Static function-style calls: peek*(addr), poke*(addr, val), chipasm(arch, code)
            if (methodCall.Target == null && methodCall.MethodName.StartsWith("peek") && methodCall.Arguments.Count == 1)
            {
                string runtimeFn = methodCall.MethodName;
                GenerateExpression(methodCall.Arguments[0]);
                instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, runtimeFn) }));
                return;
            }
            if (methodCall.Target == null && methodCall.MethodName.StartsWith("poke") && methodCall.Arguments.Count == 2)
            {
                string runtimeFn = methodCall.MethodName;
                GenerateExpression(methodCall.Arguments[0]); // addr → R0
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
                GenerateExpression(methodCall.Arguments[1]); // val → R0
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) })); // R1 = val
                instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 0) })); // R0 = addr
                instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, runtimeFn) }));
                return;
            }
            if (methodCall.Target == null && methodCall.MethodName == "chipasm" && methodCall.Arguments.Count >= 2)
            {
                return;
            }
            // asm() 已移除 — 仅限 C/ObjC/C++ 语言使用，Java 通过 Lib/shared/vmlsys.c 调用系统功能

            // 生成参数 (CCv2: 第一个参数在R0, 其余从右到左压栈)
            for (int i = methodCall.Arguments.Count - 1; i >= 0; i--)
            {
                GenerateExpression(methodCall.Arguments[i]);
                if (i > 0)
                {
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> {
                        new Operand(OperandType.REGISTER, 0)
                    }));
                }
            }
            
            if (methodCall.MethodName == "exit" && methodCall.Arguments.Count == 1)
            {
                // System.exit(n): R0 already has the exit code from argument generation
                EmitExit();
            }
            else if (methodCall.MethodName == "println" || methodCall.MethodName == "print")
            {
                // 无参数时直接输出换行
                if (methodCall.Arguments.Count == 0 && methodCall.MethodName == "println")
                {
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 10)]));
                    EmitPrintChar();
                }
                else if (methodCall.Arguments.Count > 0)
                {
                    // 单参数 print: 表达式结果已在 R0, 直接调用
                    // 多参数: 参数在栈上, 需要逐个处理
                    var argType = InferExpressionType(methodCall.Arguments[0]);
                    bool isStringArg = argType == JavaTypeEnum.String ||
                        methodCall.Arguments[0] is LiteralExpression lit && lit.Type == "String";
                    bool isFloatArg = argType == JavaTypeEnum.Float || argType == JavaTypeEnum.Double;
                    bool isBoolArg = argType == JavaTypeEnum.Boolean;
                    bool isCharArg = argType == JavaTypeEnum.Char;

                    if (isFloatArg)
                        EmitPrintFloat();
                    else if (isBoolArg)
                        EmitPrintBool();
                    else if (isCharArg)
                        EmitPrintChar();
                    else if (isStringArg)
                    {
                        string targetLabel = methodCall.MethodName == "println" ? "println_str" : "print_str";
                        instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, targetLabel)]));
                    }
                    else
                    {
                        string targetLabel = methodCall.MethodName == "println" ? "println_int" : "print_int";
                        instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, targetLabel)]));
                    }

                    // println: 非字符串/整数路径需要手动追加换行 (字符串/整数的 println 标签内部已含换行)
                    if (methodCall.MethodName == "println" && (isFloatArg || isBoolArg || isCharArg))
                        EmitPrintNewline();
                }
            }
            else
            {
                // native 方法：直接 CALL 共享库标签
                if (nativeMethods.Contains(methodCall.MethodName))
                {
                    instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, methodCall.MethodName)]));
                }
                else
                {
                    // 普通方法调用
                    instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, $"method_{methodCall.MethodName}")]));
                }
            }
            
            // 清理参数 (只清理压栈的参数, 不包括R0中的第一个参数)
            int pushedArgs = methodCall.Arguments.Count > 0 ? methodCall.Arguments.Count - 1 : 0;
            if (pushedArgs > 0)
            {
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> {
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.IMMEDIATE, pushedArgs * 4),
                    new Operand(OperandType.REGISTER, 13)
                }));
            }
        }
                
        private void GenerateAssignment(AssignmentExpression assignment)
        {
            if (assignment.Left is VariableExpression varExpr)
            {
                if (_varOffsets.TryGetValue(varExpr.Name, out int offset))
                {
                    if (assignment.Operator != TokenType.Assign)
                    {
                        var target = WrapTargetExpr(new VariableExpression(varExpr.Name));
                        string op = assignment.Operator switch
                        {
                            TokenType.PlusAssign => "+",
                            TokenType.MinusAssign => "-",
                            TokenType.MultiplyAssign => "*",
                            TokenType.DivideAssign => "/",
                            TokenType.ModuloAssign => "%",
                            TokenType.AndAssign => "&",
                            TokenType.OrAssign => "|",
                            TokenType.XorAssign => "^",
                            TokenType.LeftShiftAssign => "<<",
                            TokenType.RightShiftAssign => ">>",
                            TokenType.UnsignedRightShiftAssign => ">>",
                            _ => throw new CompilationException(ErrorCode.CodeGen_InvalidOperand, $"Unknown compound operator: {assignment.Operator}")
                        };
                        _expr!.EmitCompoundAssign(target, WrapExpr(assignment.Right), op);
                        return;
                    }

                    GenerateExpression(assignment.Right);
                    JavaTypeEnum rightType = InferExpressionType(assignment.Right);
                    _varTypes[varExpr.Name] = rightType;
                    OpCode storeOp = GetStoreInstruction(rightType);
                    instructions.Add(new Instruction(storeOp, new List<Operand> {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.MEMORY, Vars.FormatOffset(-offset))
                    }));
                }
                else
                {
                    GenerateExpression(assignment.Right);
                    JavaTypeEnum rightType = InferExpressionType(assignment.Right);
                    _varTypes[varExpr.Name] = rightType;
                    string label = $"var_{varExpr.Name}";
                    if (!dataSection.ContainsKey(label)) dataSection[label] = 0;
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                        new Operand(OperandType.LABEL, label),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                }
            }
        }
        
        private void GenerateVariableDeclaration(VariableDeclStatement varDecl)
        {
            if (varDecl.Initializer != null)
            {
                GenerateExpression(varDecl.Initializer);
                // 将结果存储到栈中
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 13)
                }));
            }
            else
            {
                // 初始化变量为0
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 0)
                }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 13)
                }));
            }
        }
    }
}
