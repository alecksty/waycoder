using System;
using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace PascalCompiler
{
    public partial class CodeGenerator
    {
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
        private void GenerateExpression(ExpressionNode expr)
        {
        if (expr.Line > 0) { CurrentSourceLine = expr.Line; CurrentSourceColumn = expr.Column; }
            if (expr is SetExpressionNode setExpr)
            {
                // 集合字面量: OR所有元素值 [e1, e2, ...] → e1 | e2 | ...
                if (setExpr.Elements.Count > 0)
                {
                    GenerateExpression(setExpr.Elements[0]); // R0 = first
                    for (int i = 1; i < setExpr.Elements.Count; i++)
                    {
                        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)])); // save acc
                        GenerateExpression(setExpr.Elements[i]); // R0 = next elem
                        instructions.Add(new Instruction(OpCode.POP, [Reg(1)])); // R1 = acc
                        instructions.Add(new Instruction(OpCode.OR,
                            [Reg(0), Reg(0), Reg(1)])); // R0 = elem | acc
                    }
                }
                else
                    AddRI(OpCode.MOVE, 0, 0);
                return;
            }
            if (expr is LiteralNode literal)
            {
                // Pascal REAL 是 32-bit float, 但 Lexer 存储为 double → 转换
                if (literal.Type == TokenType.REAL_LITERAL && literal.Value is double d)
                    EmitLoadConstant((float)d);
                else
                    EmitLoadConstant(literal.Value);
            }
            else if (expr is VariableNode variable)
            {
                if (variable.DereferenceCount > 0)
                {
                    GenerateDereferencedVariableValue(variable);
                    return;
                }
                // 检查是否为无括号函数调用
                if (functionNames.Contains(variable.Name.ToLower()) && variable.Indices.Count == 0)
                {
                    instructions.Add(new Instruction(OpCode.CALL, new List<Operand>
                    {
                        new Operand(OperandType.LABEL, variable.Name)
                    }));
                    return;
                }
                /* **单元声明的无参函数，裸写**（`GetMaxX div 2`、`ErrorCode := GraphResult;`、
                 * `if KeyPressed then`）。
                 *
                 * Pascal 里"无参函数"和"变量"在**语法上就是同一个形状**（都不带括号），
                 * 所以只能靠**声明**分：这个名字在某个 `uses` 单元的 interface 里是个
                 * 零参子程序 ⇒ 它是一次调用。表由 `PascalCompiler.RegisterUnitFunctions`
                 * 从单元文件里读出来，**不是**在 C# 里硬编码一张 BGI 名字表。
                 *
                 * ⚠ 标签用**声明里的那个大小写**（`sub.Name`）而不是源码里的 ——
                 *   Pascal 不区分大小写（`Getmaxx` 合法），而 VML 的标签表**区分**
                 *   ⇒ 不归一的话，写法少一个大写字母就是"未解析标签"。 */
                if (variable.Indices.Count == 0
                    && UnitSubprograms.TryGetValue(variable.Name.ToLower(), out var bareUnitSub)
                    && bareUnitSub.Parameters.Count == 0)
                {
                    instructions.Add(new Instruction(OpCode.CALL, new List<Operand>
                    {
                        new Operand(OperandType.LABEL, bareUnitSub.Name)
                    }));
                    return;
                }
                /* Crt 里那些"名字与生成目标不同"的裸写标准函数（见 BareStdCalls 的注释）。
                 *
                 * ⚠ 走 `EmitCallBuiltin` 而**不是**裸 `CALL`：这两个是 **Crt 内建**，
                 *   而内建的调用约定是"参数镜像占 4 个槽"（`BuiltinArgSlots`）——
                 *   上面那条 `name == "readkey"` 的分支就是这么发的。
                 *   两条路发同一条 CALL 却是两种栈约定，正是本仓记过的"同一件事两处实现"。
                 *   （上面**单元函数**那条相反：库函数按普通调用约定传参、0 参就不压栈，
                 *    与带括号的写法一致 —— 两者不是同一种东西，别顺手统一。） */
                if (variable.Indices.Count == 0
                    && BareStdCalls.TryGetValue(variable.Name, out string? bareStdLabel))
                {
                    EmitCallBuiltin(bareStdLabel);
                    return;
                }
                // 检查是否是浮点常量 (在 dataSection 中以 int bits 存储)
                if (constants.ContainsKey(variable.Name) && constants[variable.Name] is double doubleVal)
                {
                    // 浮点常量引用: 确保在 dataSection 中有对应的条目
                    if (!dataSection.ContainsKey(variable.Name))
                    {
                        dataSection[variable.Name] = BitConverter.SingleToInt32Bits((float)doubleVal);
                    }
                    instructions.Add(new Instruction(OpCode.MOVEF, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.MEMORY, variable.Name)
                    }));
                    return;
                }
                // 检查是否是已声明的常量(整数/布尔/字符串)
                if (constNames.Contains(variable.Name))
                {
                    if (dataSection[variable.Name] is string)
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.LABEL, variable.Name)
                        }));
                    }
                    else if (dataSection[variable.Name] is int intVal)
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.IMMEDIATE, intVal)
                        }));
                    }
                    return;
                }

                bool isFloat = IsFloatVariable(variable.Name);
                bool isSet = IsSetVariable(variable.Name);

                if (variable.Indices.Count > 0)
                {
                    GenerateVariableAddress(variable);
                    if (variable.Field != null)
                    {
                        // 数组元素 + 记录字段: 地址已在R0，加字段偏移后加载 (支持多级 b.a.v)
                        var (fieldOffset, fieldType) = ResolveFieldChain(variable.Name, variable.Field, variable.Fields);
                        if (fieldOffset > 0)
                        {
                            instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                            {
                                new Operand(OperandType.REGISTER, 0),
                                new Operand(OperandType.REGISTER, 0),
                                new Operand(OperandType.IMMEDIATE, fieldOffset * 4)
                            }));
                        }
                        bool fieldIsFloat = fieldType == "REAL";
                        if (fieldIsFloat)
                            instructions.Add(new Instruction(OpCode.MOVEF, new List<Operand> { new Operand(OperandType.REGISTER, 0), Mem("R0") }));
                        else
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), Mem("R0") }));
                    }
                    else
                    {
                        PascalType varType = GetVariablePascalType(variable.Name);
                        OpCode loadOp = GetLoadInstruction(varType);
                        instructions.Add(new Instruction(loadOp, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            Mem("R0")
                        }));
                    }
                }
                else if (isSet)
                {
                    // 集合变量：返回地址而不是值
                    GenerateVariableAddress(variable);
                }
                else if (localVarOffsets.ContainsKey(variable.Name))
                {
                    int offset = localVarOffsets[variable.Name];
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.REGISTER, 12)
                    }));
                    // 加偏移 (slot index → byte offset)
                    if (offset != 0)
                    {
                        instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.IMMEDIATE, offset * 4)
                        }));
                    }

                    // 使用数据类型敏感指令选择
                    PascalType varType = GetVariablePascalType(variable.Name);
                    OpCode loadOp = GetLoadInstruction(varType);
                    instructions.Add(new Instruction(loadOp, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        Mem("R0")
                    }));
                }
                else if (paramOffsets.ContainsKey(variable.Name))
                {
                    // 参数访问
                    int offset = paramOffsets[variable.Name];
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.REGISTER, 12)
                    }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, offset)
                    }));

                    // 如果是var参数, 需要解引用(参数本身是地址)
                    if (varParameters.Contains(variable.Name))
                    {
                        // R0现在是地址, 加载地址指向的值
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            Mem("R0")
                        }));
                    }
                    else if (isSet)
                    {
                        // 集合参数：返回地址
                        // R0已经是地址，不需要加载值
                    }
                    else
                    {
                        // 普通值参数, 直接加载
                        if (isFloat)
                        {
                            instructions.Add(new Instruction(OpCode.MOVEF, new List<Operand>
                            {
                                new Operand(OperandType.REGISTER, 0),
                                Mem("R0")
                            }));
                        }
                        else
                        {
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                            {
                                new Operand(OperandType.REGISTER, 0),
                                Mem("R0")
                            }));
                        }
                    }
                }
                else
                {
                    // 对于 record 字段访问，跳过全局加载——字段路径会自行处理 (v1.66.36 fix)
                    if (variable.Field == null)
                    {
                    if (isSet)
                    {
                        // 集合全局变量：返回地址
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.LABEL, variable.Name)
                        }));
                    }
                    else
                    {
                        if (!dataSection.ContainsKey(variable.Name))
                        {
                            /* ── 最后一站：**单元 interface 常量** 与 **System 预定义常量** ──
                             *
                             * 位置是刻意的：放在**所有"这个文件自己声明的东西"之后**
                             * （局部量 / 形参 / 全局量 / 本文件的 const 都在前面处理掉了），
                             * 所以程序自己写的 `Brown`、`Red` 之类**永远压过**库里的同名常量 ——
                             * 老程序里 `Red`/`Green`/`White` 既是 BGI 颜色又是极常见的变量名，
                             * 顺序反了就会"用户的变量被库常量悄悄顶掉"，而那种错查不出来。
                             *
                             * ① 单元常量（`uses graph` 给的 `Detect`/`grOk`/`SolidFill`…）：
                             *    登记处见 `PascalCompiler.RegisterUnitFunctions`。
                             * ② `Pi`/`MaxInt`：Pascal 的 `System` 单元**不用 uses 就在作用域内**，
                             *    而它是语言的一部分、没有相应的库文件可挂 ⇒ 在这里内建。
                             *    老程序裸写它们（语料里 `Pi` 60 处、24 个文件），
                             *    从前一律报"未声明的变量"。 */
                            if (variable.Field == null)
                            {
                                if (UnitConstInts.TryGetValue(variable.Name.ToLower(), out int unitConst))
                                {
                                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                                    {
                                        new Operand(OperandType.REGISTER, 0),
                                        new Operand(OperandType.IMMEDIATE, unitConst)
                                    }));
                                    return;
                                }
                                if (StdConsts.TryGetValue(variable.Name.ToLower(), out object? stdConst))
                                {
                                    EmitLoadConstant(stdConst);
                                    return;
                                }
                            }

                            // 局部集合 / 局部变量 / 形参三个分支都没命中，`dataSection` 里也没有
                            // ⇒ 这个名字**从未声明过**。此前这里静默建个初值 0 的槽就当它是全局，
                            // 于是 `WriteLn(nosuch);` 编得过、运行期打出一个 0。
                            //
                            // 这里**保留原来那句建槽**（不像别处改成 `EmitUndefinedFallback + return`）：
                            // 这一段嵌在四层 if/else 里，提前 return 会跳过后面收尾的指令生成，
                            // 而"建个 0 槽照旧往下走"与旧行为**逐字相同**、没有任何结构风险。
                            // 编译反正会因为上面这条诊断失败（`BuildProgram` 见 `Diags.HasErrors` 就抛），
                            // 生成出来的代码给谁看都无所谓。
                            ReportUndefined(variable.Name, ErrorCode.CodeGen_UndefinedVariable, "变量");
                            dataSection[variable.Name] = 0;
                        }

                        // 使用数据类型敏感指令选择：先取全局变量地址，再间接加载
                        PascalType varType = GetVariablePascalType(variable.Name);
                        OpCode loadOp = GetLoadInstruction(varType);
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.LABEL, variable.Name)
                        }));
                        instructions.Add(new Instruction(loadOp, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            Mem("R0")
                        }));
                    }
                }
                } // 关闭 variable.Field == null 的 if 块 (v1.66.36)

                // 处理record字段访问: 计算地址后LOAD (支持多级 b.a.v)
                if (variable.Field != null)
                {
                    var (fieldOffset, fieldType) = ResolveFieldChain(variable.Name, variable.Field, variable.Fields);

                    // 先获取基地址
                    if (localVarOffsets.ContainsKey(variable.Name))
                    {
                        int offset = localVarOffsets[variable.Name];
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.REGISTER, 12)
                        }));
                        if (offset != 0)
                        {
                            instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                            {
                                new Operand(OperandType.REGISTER, 0),
                                new Operand(OperandType.REGISTER, 0),
                                new Operand(OperandType.IMMEDIATE, offset * 4)
                            }));
                        }
                    }
                    else
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.LABEL, variable.Name)
                        }));
                    }

                    // 添加字段偏移
                    if (fieldOffset > 0)
                    {
                        instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.IMMEDIATE, fieldOffset * 4)
                        }));
                    }

                    // 从地址加载值
                    bool fieldIsFloat = fieldType == "REAL";
                    if (fieldIsFloat)
                    {
                        instructions.Add(new Instruction(OpCode.MOVEF, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            Mem("R0")
                        }));
                    }
                    else
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            Mem("R0")
                        }));
                    }
                }
            }
            else if (expr is BinaryOpNode binaryOp)
            {
                bool leftIsSet = IsSetExpression(binaryOp.Left);
                bool rightIsSet = IsSetExpression(binaryOp.Right);
                bool isSetOp = leftIsSet && rightIsSet;

                switch (binaryOp.Operator)
                {
                    case TokenType.PLUS:
                        if (isSetOp) { GenerateSetOperation(binaryOp, OpCode.OR); return; }
                        _expr!.EmitBinOp(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right), "+");
                        break;
                    case TokenType.MINUS:
                        if (isSetOp) { GenerateSetOperation(binaryOp, OpCode.AND, true); return; }
                        _expr!.EmitBinOp(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right), "-");
                        break;
                    case TokenType.STAR:
                        if (isSetOp) { GenerateSetOperation(binaryOp, OpCode.AND); return; }
                        _expr!.EmitBinOp(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right), "*");
                        break;
                    case TokenType.SLASH:
                        // Pascal / 总是产生 Real, 但操作数保持原类型以触发 I2F 转换 (v1.66.38 fix)
                        _expr!.EmitBinOp(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right), "/");
                        break;
                    case TokenType.DIV:
                        _expr!.EmitBinOp(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right), "/");
                        break;
                    case TokenType.MOD:
                        _expr!.EmitBinOp(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right), "%");
                        break;
                    case TokenType.IN:
                        if (IsSetTypeExpression(binaryOp.Right))
                            GenerateInOperation(binaryOp);
                        else
                            Error("IN 运算符的右操作数必须是集合类型");
                        break;
                    case TokenType.EQUALS: _expr!.EmitCmp(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right), "=="); break;
                    case TokenType.NOT_EQUALS: _expr!.EmitCmp(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right), "!="); break;
                    case TokenType.LESS_THAN: _expr!.EmitCmp(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right), "<"); break;
                    case TokenType.LESS_EQUAL: _expr!.EmitCmp(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right), "<="); break;
                    case TokenType.GREATER_THAN: _expr!.EmitCmp(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right), ">"); break;
                    case TokenType.GREATER_EQUAL: _expr!.EmitCmp(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right), ">="); break;
                    case TokenType.AND: _expr!.EmitBitAnd(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right)); break;
                    case TokenType.OR: _expr!.EmitBitOr(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right)); break;
                    case TokenType.XOR: _expr!.EmitBitXor(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right)); break;
                    case TokenType.SHL: _expr!.EmitShl(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right)); break;
                    case TokenType.SHR: _expr!.EmitShr(WrapExpr(binaryOp.Left), WrapExpr(binaryOp.Right)); break;
                    default:
                        throw new CompilationException(ErrorCode.CodeGen_InvalidOperand, $"不支持的二元运算符: {binaryOp.Operator}");
                }
            }
            else if (expr is FunctionCallNode funcCall)
            {
                GenerateFunctionCall(funcCall);
            }
            else if (expr is AddressOfNode addressOf)
            {
                GenerateVariableAddress(addressOf.Variable);
            }
            else if (expr is DereferenceNode dereference)
            {
                GenerateExpression(dereference.Pointer);
                // Use type-sensitive load based on what the pointer points to
                OpCode loadOp = OpCode.MOVE;
                if (dereference.Pointer is VariableNode ptrVar)
                {
                    var pointedType = GetPointedPascalType(ptrVar.Name);
                    var (size, isFloat, isDouble, isLong) = TypeInfo(pointedType);
                    loadOp = ExpressionManager.SelectLoadOp(size, isFloat, isDouble, isLong);
                }
                instructions.Add(new Instruction(loadOp, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.MEMORY, "R0")
                }));
            }
            else if (expr is UnaryOpNode unaryOp)
            {
                GenerateUnaryExpression(unaryOp);
            }
            else
            {
                throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, $"不支持的表达式类型: {expr.GetType().Name}");
            }
        }

        private void GenerateFunctionCall(FunctionCallNode funcCall)
        {
            string name = funcCall.Name.ToLower();

            // 内置函数
            if (name == "abs")
            {
                GenerateExpression(funcCall.Arguments[0]);
                EmitCallAbs();
                return;
            }
            else if (name == "sqr")
            {
                GenerateExpression(funcCall.Arguments[0]);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.REGISTER, 0)
                }));
                instructions.Add(new Instruction(OpCode.MUL, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.REGISTER, 0)
                }));
                return;
            }
            else if (name == "chr")
            {
                GenerateExpression(funcCall.Arguments[0]);
                return;
            }
            else if (name == "ord")
            {
                GenerateExpression(funcCall.Arguments[0]);
                return;
            }
            else if (name == "pred")
            {
                GenerateExpression(funcCall.Arguments[0]);
                int step = 1;
                if (funcCall.Arguments[0] is VariableNode vn)
                {
                    string varType = GetVariableType(vn.Name);
                    if (varType.StartsWith("^"))
                        step = GetPointerTargetSize(vn.Name) * 4;
                }
                instructions.Add(new Instruction(OpCode.SUB, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, step)
                }));
                return;
            }
            else if (name == "succ")
            {
                GenerateExpression(funcCall.Arguments[0]);
                int step = 1;
                if (funcCall.Arguments[0] is VariableNode vn)
                {
                    string varType = GetVariableType(vn.Name);
                    if (varType.StartsWith("^"))
                        step = GetPointerTargetSize(vn.Name) * 4;
                }
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, step)
                }));
                return;
            }
            else if (name == "odd")
            {
                GenerateExpression(funcCall.Arguments[0]);
                instructions.Add(new Instruction(OpCode.AND, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 1)
                }));
                return;
            }
            else if (name == "trunc")
            {
                GenerateExpression(funcCall.Arguments[0]);
                instructions.Add(new Instruction(OpCode.F2I, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 0)
                }));
                return;
            }
            else if (name == "round")
            {
                // round(x) = trunc(x + 0.5) — 四舍五入
                GenerateExpression(funcCall.Arguments[0]);
                instructions.Add(new Instruction(OpCode.FADD, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 0.5f)
                }));
                instructions.Add(new Instruction(OpCode.F2I, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 0)
                }));
                return;
            }
            else if (name == "sqrt")
            {
                // Sqrt使用牛顿迭代法: x_{n+1} = (x_n + a/x_n) / 2
                // 这里生成调用库函数的代码
                GenerateExpression(funcCall.Arguments[0]);
                EmitCallBuiltin("lib_sqrt");
                return;
            }
            else if (name == "random")
            {
                // Random(n) 返回 0 到 n-1 之间的随机数
                // 调用库函数 lib_random
                GenerateExpression(funcCall.Arguments[0]);
                EmitCallBuiltin("lib_random");
                return;
            }
            else if (name == "randomize")
            {
                // Randomize 初始化随机数种子
                EmitCallBuiltin("lib_randomize");
                return;
            }
            else if (name.StartsWith("peek"))
            {
                string runtimeFn = "vml_" + name;
                GenerateExpression(funcCall.Arguments[0]);
                EmitCallBuiltin(runtimeFn);
                return;
            }
            else if (name == "sin")
            {
                GenerateExpression(funcCall.Arguments[0]);
                EmitCallBuiltin("lib_sin");
                return;
            }
            else if (name == "cos")
            {
                GenerateExpression(funcCall.Arguments[0]);
                EmitCallBuiltin("lib_cos");
                return;
            }
            else if (name == "exp")
            {
                GenerateExpression(funcCall.Arguments[0]);
                EmitCallBuiltin("lib_exp");
                return;
            }
            else if (name == "ln")
            {
                GenerateExpression(funcCall.Arguments[0]);
                EmitCallBuiltin("lib_ln");
                return;
            }
            else if (name == "length")
            {
                GenerateExpression(funcCall.Arguments[0]);
                EmitCallBuiltin("lib_length");
                return;
            }
            else if (name == "sizeof")
            {
                int size = 4; // default: 1 word = 4 bytes
                if (funcCall.Arguments.Count >= 1 && funcCall.Arguments[0] is VariableNode varNode)
                {
                    // Look up the variable's type to compute its size
                    if (globalVarDeclarations.TryGetValue(varNode.Name, out var typeNode))
                        size = GetVariableSlots(typeNode) * 4;
                    else if (localVarDeclarations.TryGetValue(varNode.Name, out typeNode))
                        size = GetVariableSlots(typeNode) * 4;
                }
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, size)
                }));
                return;
            }
            else if (name == "copy")
            {
                GenerateCopyFunction(funcCall);
                return;
            }
            else if (name == "filesize")
            {
                GenerateFileControlFunction(funcCall, 0);
                return;
            }
            else if (name == "filepos")
            {
                GenerateFileControlFunction(funcCall, 2);
                return;
            }
            else if (name == "eof")
            {
                GenerateEofFunction(funcCall);
                return;
            }
            else if (name == "eoln")
            {
                // SYSCALL 114 (FileControl) with command=3 to check if next char is newline
                GenerateExpression(funcCall.Arguments[0]);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.IMMEDIATE, 3)
                }));
                instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                {
                    new Operand(OperandType.IMMEDIATE, 114)
                }));
                return;
            }
            else if (name == "concat")
            {
                GenerateExpression(funcCall.Arguments[0]);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.REGISTER, 0)
                }));
                GenerateExpression(funcCall.Arguments[1]);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 2),
                    new Operand(OperandType.REGISTER, 1)
                }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.REGISTER, 0)
                }));
                EmitCallBuiltin("lib_concat");
                return;
            }
            else if (name == "round")
            {
                GenerateExpression(funcCall.Arguments[0]);
                EmitCallBuiltin("lib_round");
                return;
            }

            // Crt单元函数
            else if (name == "gotoxy")
            {
                // GotoXY(x, y)
                GenerateExpression(funcCall.Arguments[1]); // y参数
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.REGISTER, 0)
                }));
                GenerateExpression(funcCall.Arguments[0]); // x参数
                EmitCallBuiltin("CRT_GOTOXY");
                return;
            }
            else if (name == "clrscr")
            {
                // ClrScr
                EmitCallBuiltin("CRT_CLRSCR");
                return;
            }
            else if (name == "wherex")
            {
                // WhereX: 返回当前光标X坐标
                EmitCallBuiltin("CRT_WHEREX");
                return;
            }
            else if (name == "wherey")
            {
                // WhereY: 返回当前光标Y坐标
                EmitCallBuiltin("CRT_WHEREY");
                return;
            }
            else if (name == "textcolor")
            {
                // TextColor(color)
                GenerateExpression(funcCall.Arguments[0]);
                EmitCallBuiltin("CRT_TEXTCOLOR");
                return;
            }
            else if (name == "textbackground")
            {
                // TextBackground(color)
                GenerateExpression(funcCall.Arguments[0]);
                EmitCallBuiltin("CRT_TEXTBACKGROUND");
                return;
            }
            else if (name == "delay")
            {
                // Delay(ms)
                GenerateExpression(funcCall.Arguments[0]);
                EmitCallBuiltin("CRT_DELAY");
                return;
            }
            else if (name == "sound")
            {
                // Sound(freq)
                GenerateExpression(funcCall.Arguments[0]);
                EmitCallBuiltin("CRT_SOUND");
                return;
            }
            else if (name == "nosound")
            {
                // NoSound
                EmitCallBuiltin("CRT_NOSOUND");
                return;
            }
            else if (name == "keypressed")
            {
                // KeyPressed: 返回是否有按键按下
                EmitCallBuiltin("CRT_KEYPRESSED");
                return;
            }
            else if (name == "readkey")
            {
                // ReadKey: 读取一个按键
                EmitCallBuiltin("CRT_READKEY");
                return;
            }
            else if (name == "window")
            {
                // Window(x1, y1, x2, y2)
                GenerateExpression(funcCall.Arguments[3]); // y2
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 3),
                    new Operand(OperandType.REGISTER, 0)
                }));
                GenerateExpression(funcCall.Arguments[2]); // x2
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 2),
                    new Operand(OperandType.REGISTER, 0)
                }));
                GenerateExpression(funcCall.Arguments[1]); // y1
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.REGISTER, 0)
                }));
                GenerateExpression(funcCall.Arguments[0]); // x1
                EmitCallBuiltin("CRT_WINDOW");
                return;
            }
            else if (name == "normvideo")
            {
                // NormVideo
                EmitCallBuiltin("CRT_NORMVIDEO");
                return;
            }
            else if (name == "highvideo")
            {
                // HighVideo
                EmitCallBuiltin("CRT_HIGHVIDEO");
                return;
            }
            else if (name == "lowvideo")
            {
                // LowVideo
                EmitCallBuiltin("CRT_LOWVIDEO");
                return;
            }
            else if (name == "insline")
            {
                // InsLine
                EmitCallBuiltin("CRT_INSLINE");
                return;
            }
            else if (name == "delline")
            {
                // DelLine
                EmitCallBuiltin("CRT_DELLINE");
                return;
            }
            else if (name == "cursoron")
            {
                // CursorOn
                EmitCallBuiltin("CRT_CURSORON");
                return;
            }
            else if (name == "cursoroff")
            {
                // CursorOff
                EmitCallBuiltin("CRT_CURSOROFF");
                return;
            }

            // 用户自定义函数调用
            for (int i = funcCall.Arguments.Count - 1; i >= 0; i--)
            {
                GenerateExpression(funcCall.Arguments[i]);
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0)
                }));
            }

            instructions.Add(new Instruction(OpCode.CALL, new List<Operand>
            {
                new Operand(OperandType.LABEL, funcCall.Name)
            }));

            if (funcCall.Arguments.Count > 0)
            {
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.IMMEDIATE, funcCall.Arguments.Count * 4)
                }));
            }
        }

        private void GenerateDereferencedVariableValue(VariableNode variable)
        {
            var baseVariable = new VariableNode { Name = variable.Name, Line = variable.Line, Column = variable.Column };
            GenerateExpression(baseVariable);
            for (int i = 1; i < variable.DereferenceCount; i++)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    Mem("R0")
                }));
            }

            if (variable.Indices.Count > 0)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 2),
                    new Operand(OperandType.REGISTER, 0)
                }));
                GenerateExpression(variable.Indices[0]);
                instructions.Add(new Instruction(OpCode.MUL, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 4)
                }));
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 2),
                    new Operand(OperandType.REGISTER, 0)
                }));
            }

            // Use type-aware load for the final dereference
            OpCode finalLoadOp = OpCode.MOVE;
            var pointedType = GetPointedPascalType(variable.Name);
            for (int i = 1; i < variable.DereferenceCount; i++)
            {
                pointedType = GetPascalType(GetVariableType(variable.Name).ToUpper().Substring(1));
            }
            var (finalSize, finalFloat, finalDouble, finalLong) = TypeInfo(pointedType);
            finalLoadOp = ExpressionManager.SelectLoadOp(finalSize, finalFloat, finalDouble, finalLong);

            instructions.Add(new Instruction(finalLoadOp, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.MEMORY, "R0")
            }));
        }

        private void GenerateFileControlFunction(FunctionCallNode funcCall, int command)
        {
            if (funcCall.Arguments.Count == 0 || funcCall.Arguments[0] is not VariableNode fileVar)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
                return;
            }
            LoadFileHandle(fileVar);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, command) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 114) }));
        }

        private void GenerateEofFunction(FunctionCallNode funcCall)
        {
            if (funcCall.Arguments.Count == 0 || funcCall.Arguments[0] is not VariableNode fileVar)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) }));
                return;
            }
            GenerateFileControlFunction(new FunctionCallNode { Arguments = { fileVar } }, 2);
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(3), Reg(0)]));
            GenerateFileControlFunction(new FunctionCallNode { Arguments = { fileVar } }, 0);
            instructions.Add(new Instruction(OpCode.CMP, [Reg(3), Reg(0)]));
            EmitBoolFromBranch(OpCode.JGE);
        }

        private void GenerateCopyFunction(FunctionCallNode funcCall)
        {
            if (funcCall.Arguments.Count == 0)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
                return;
            }

            if (funcCall.Arguments[0] is VariableNode source && dynamicArrayNames.Contains(source.Name))
            {
                int count = 1;
                if (funcCall.Arguments.Count >= 3 && funcCall.Arguments[2] is LiteralNode lit)
                    count = Convert.ToInt32(lit.Value);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, count * 4) }));
                EmitAlloc();
                AlignAllocatedPointer();
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0) }));
                GenerateExpression(new VariableNode { Name = source.Name });
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
                for (int i = 0; i < count; i++)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 3),
                        new Operand(OperandType.REGISTER, 1),
                        new Operand(OperandType.IMMEDIATE, i * 4)
                    }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 3),
                        new Operand(OperandType.REGISTER, 4),
                        new Operand(OperandType.IMMEDIATE, i * 4)
                    }));
                }
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 4) }));
                return;
            }

            GenerateExpression(funcCall.Arguments[0]);
        }
    }
}
