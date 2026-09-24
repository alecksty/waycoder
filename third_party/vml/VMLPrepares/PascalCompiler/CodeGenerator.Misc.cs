using System;
using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace PascalCompiler
{
    public partial class CodeGenerator
    {
        private void GenerateUnaryExpression(UnaryOpNode unaryOp)
        {
            if (unaryOp.Operator == TokenType.MINUS)
            {
                bool isFloat = IsFloatExpression(unaryOp.Operand);
                _expr!.EmitNeg(WrapExpr(unaryOp.Operand, isFloat ? PascalType.Real : PascalType.Integer));
            }
            else if (unaryOp.Operator == TokenType.NOT)
            {
                _expr!.EmitNot(ExpVar.Eval(ExpType.I32, () => GenerateExpression(unaryOp.Operand)));
            }
        }

        private void GenerateProcedureCall(ProcedureCallNode call)
        {
            string callName = call.Name.ToLower();
            if (IsFileIoCall(call))
            {
                GenerateFileIoCall(call);
            }
            else if (callName == "new")
            {
                GenerateNewCall(call);
            }
            else if (callName == "dispose")
            {
                GenerateDisposeCall(call);
            }
            else if (callName == "setlength")
            {
                GenerateSetLengthCall(call);
            }
            else if (callName == "exit")
            {
                if (subprogramExitLabels.Count > 0)
                {
                    AddInstruction(OpCode.JMP, LabelOp(subprogramExitLabels.Peek()));
                }
            }
            else if (callName == "inc" || callName == "dec")
            {
                // Inc(var, n) / Dec(var, n) — 标准 Pascal 内置过程
                if (call.Arguments.Count >= 1 && call.Arguments[0] is VariableNode incVar)
                {
                    // 获取递增/递减量 (默认 1)
                    int delta = 1;
                    if (call.Arguments.Count >= 2 && call.Arguments[1] is LiteralNode deltaLit)
                        delta = Convert.ToInt32(deltaLit.Value);
                    if (callName == "dec") delta = -delta;

                    // 加载变量值
                    GenerateExpression(incVar);
                    // R0 = value, save to R1
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), Reg(0)]));
                    // 计算新值
                    if (delta >= 0)
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(1), Imm(delta)]));
                    else
                        instructions.Add(new Instruction(OpCode.SUB, [Reg(0), Reg(1), Imm(-delta)]));
                    // 保存到变量
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), Reg(0)]));
                    GenerateVariableAddress(incVar);
                    instructions.Add(new Instruction(OpCode.MOVE, [Mem("R0"), Reg(1)]));
                }
            }
            else if (callName == "seek")
            {
                // seek(file, pos) — SYSCALL 114 command 1
                if (call.Arguments.Count >= 2)
                {
                    GenerateExpression(call.Arguments[0]); // file
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0)
                    }));
                    GenerateExpression(call.Arguments[1]); // pos
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 1)
                    }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 2),
                        new Operand(OperandType.IMMEDIATE, 1)
                    }));
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                    {
                        new Operand(OperandType.IMMEDIATE, 114)
                    }));
                }
            }
            else if (call.Name.ToLower() == "writeln")
            {
                foreach (var arg in call.Arguments)
                {
                    GenerateWriteArgument(arg);
                }
                
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 10)
                }));
                
                instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                {
                    new Operand(OperandType.IMMEDIATE, 4)
                }));
            }
            else if (call.Name.ToLower() == "write")
            {
                foreach (var arg in call.Arguments)
                {
                    GenerateWriteArgument(arg);
                }
            }
            else if (call.Name.ToLower() == "readln")
            {
                if (call.Arguments.Count == 0)
                {
                    // ReadLn without arguments: skip line
                    // Read and discard characters until newline
                    string readlnLoop = $"readln_skip_{labelCounter++}";
                    string readlnEnd = $"readln_end_{labelCounter++}";
                    AddLabel(readlnLoop);
                    instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                    {
                        new Operand(OperandType.IMMEDIATE, 5)
                    }));
                    instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, 10)
                    }));
                    instructions.Add(new Instruction(OpCode.JNE, new List<Operand>
                    {
                        new Operand(OperandType.LABEL, readlnLoop)
                    }));
                    AddLabel(readlnEnd);
                }
                else if (call.Arguments.Count == 1 && call.Arguments[0] is VariableNode variable)
                {
                    // 检查变量类型，决定使用整数输入还是字符串输入
                    string varType = GetVariableType(variable.Name);
                    
                    if (varType == "STRING" || varType == "CHAR")
                    {
                        // 字符串输入: SYSCALL 2
                        // 首先获取变量地址
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
                            // 全局变量
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                            {
                                new Operand(OperandType.REGISTER, 0),
                                new Operand(OperandType.LABEL, variable.Name)
                            }));
                        }
                        
                        // 调用SYSCALL 2读取字符串
                        instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                        {
                            new Operand(OperandType.IMMEDIATE, 2)
                        }));
                    }
                    else
                    {
                        // 整数输入: SYSCALL 7 → R0, 然后存储到变量
                        instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                        {
                            new Operand(OperandType.IMMEDIATE, 7)
                        }));
                        // 保存读取的值到 R1，因为之后需要 R0 作为地址寄存器
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 1),
                            new Operand(OperandType.REGISTER, 0)
                        }));

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
                            if (!dataSection.ContainsKey(variable.Name))
                            {
                                dataSection[variable.Name] = 0;
                            }

                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                            {
                                new Operand(OperandType.REGISTER, 0),
                                new Operand(OperandType.LABEL, variable.Name)
                            }));
                        }

                        // 存储 R1 (读取的值) 到 R0 (变量地址)
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            Mem("R0"),
                            new Operand(OperandType.REGISTER, 1)
                        }));
                    }
                }
                else if (call.Arguments.Count > 1)
                {
                    // 支持多个参数，如 ReadLn(a, b, c)
                    foreach (var arg in call.Arguments)
                    {
                        if (arg is VariableNode varArg)
                        {
                            // 为每个变量生成读取代码
                            string varType = GetVariableType(varArg.Name);
                            
                            if (varType == "STRING" || varType == "CHAR")
                            {
                                // 字符串输入
                                if (localVarOffsets.ContainsKey(varArg.Name))
                                {
                                    int offset = localVarOffsets[varArg.Name];
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
                                        new Operand(OperandType.LABEL, varArg.Name)
                                    }));
                                }
                                
                                instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                                {
                                    new Operand(OperandType.IMMEDIATE, 2)
                                }));
                            }
                            else
                            {
                                // 整数输入: SYSCALL 7 → R0, 然后存储到变量
                                instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand>
                                {
                                    new Operand(OperandType.IMMEDIATE, 7)
                                }));
                                // 保存读取的值到 R1，因为之后需要 R0 作为地址寄存器
                                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                                {
                                    new Operand(OperandType.REGISTER, 1),
                                    new Operand(OperandType.REGISTER, 0)
                                }));

                                if (localVarOffsets.ContainsKey(varArg.Name))
                                {
                                    // 局部变量: 计算 BP 偏移地址到 R0
                                    int offset = localVarOffsets[varArg.Name];
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
                                    // 全局变量: 加载标签地址到 R0
                                    if (!dataSection.ContainsKey(varArg.Name))
                                    {
                                        dataSection[varArg.Name] = 0;
                                    }

                                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                                    {
                                        new Operand(OperandType.REGISTER, 0),
                                        new Operand(OperandType.LABEL, varArg.Name)
                                    }));
                                }

                                // 存储 R1 (读取的值) 到 R0 (变量地址)
                                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                                {
                                    Mem("R0"),
                                    new Operand(OperandType.REGISTER, 1)
                                }));
                            }
                        }
                        else
                        {
                            throw new CompilationException(ErrorCode.CodeGen_InvalidOperand, "ReadLn参数必须是变量");
                        }
                    }
                }
                else
                {
                    throw new CompilationException(ErrorCode.CodeGen_InvalidOperand, "ReadLn参数错误");
                }
            }
            else if (call.Name.ToLower() == "halt")
            {
                if (call.Arguments.Count > 0)
                    GenerateExpression(call.Arguments[0]);
                // R0 中的值作为退出码传递给 SYSCALL 3
                EmitExit();
            }
            else if (call.Name.ToLower() == "clrscr")
            {
                EmitCallBuiltin("CRT_CLRSCR");
            }
            /* `Randomize`：**无参过程**，老程序一律裸写（不带括号，语料 15 份程序用）。
             * 表达式那边（`CodeGenerator.Expressions.cs`）早就有这一条，语句这边漏了 ——
             * 于是 `Randomize;` 落进 `GenerateUserDefinedProcedureCall` 发一条 `CALL Randomize`，
             * 而 `Lib/` 里没有这个名字 ⇒ 链接期"未解析标签"。 */
            else if (call.Name.ToLower() == "randomize")
            {
                EmitCallBuiltin("randomize");
            }
            /* `ReadKey`（**无参函数**，但老程序常把它**当语句**用：`ReadKey;` 就是"等一下"）。
             * 表达式那边有它（`Ch := ReadKey;`），语句这边漏了 —— 于是裸写 `ReadKey;`
             * 落进 `GenerateUserDefinedProcedureCall` 发 `CALL ReadKey`，
             * 而 `Lib/` 里没有这个标签 ⇒ 链接期"未定义的函数"。 */
            else if (call.Name.ToLower() == "readkey")
            {
                EmitCallBuiltin("CRT_READKEY");
            }
            // Crt单元过程
            else if (call.Name.ToLower() == "gotoxy")
            {
                // GotoXY(x, y)
                GenerateExpression(call.Arguments[1]); // y参数
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.REGISTER, 0)
                }));
                GenerateExpression(call.Arguments[0]); // x参数
                EmitCallBuiltin("CRT_GOTOXY");
            }
            else if (call.Name.ToLower() == "textcolor")
            {
                // TextColor(color)
                GenerateExpression(call.Arguments[0]);
                EmitCallBuiltin("CRT_TEXTCOLOR");
            }
            else if (call.Name.ToLower() == "textbackground")
            {
                // TextBackground(color)
                GenerateExpression(call.Arguments[0]);
                EmitCallBuiltin("CRT_TEXTBACKGROUND");
            }
            else if (call.Name.ToLower() == "delay")
            {
                // Delay(ms)
                GenerateExpression(call.Arguments[0]);
                EmitCallBuiltin("CRT_DELAY");
            }
            else if (call.Name.ToLower() == "sound")
            {
                // Sound(freq)
                GenerateExpression(call.Arguments[0]);
                EmitCallBuiltin("CRT_SOUND");
            }
            else if (call.Name.ToLower() == "nosound")
            {
                // NoSound
                EmitCallBuiltin("CRT_NOSOUND");
            }
            else if (call.Name.ToLower() == "window")
            {
                // Window(x1, y1, x2, y2)
                GenerateExpression(call.Arguments[3]); // y2
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 3),
                    new Operand(OperandType.REGISTER, 0)
                }));
                GenerateExpression(call.Arguments[2]); // x2
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 2),
                    new Operand(OperandType.REGISTER, 0)
                }));
                GenerateExpression(call.Arguments[1]); // y1
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.REGISTER, 0)
                }));
                GenerateExpression(call.Arguments[0]); // x1
                EmitCallBuiltin("CRT_WINDOW");
            }
            else if (call.Name.ToLower() == "normvideo")
            {
                // NormVideo
                EmitCallBuiltin("CRT_NORMVIDEO");
            }
            else if (call.Name.ToLower() == "highvideo")
            {
                // HighVideo
                EmitCallBuiltin("CRT_HIGHVIDEO");
            }
            else if (call.Name.ToLower() == "lowvideo")
            {
                // LowVideo
                EmitCallBuiltin("CRT_LOWVIDEO");
            }
            else if (call.Name.ToLower() == "insline")
            {
                // InsLine
                EmitCallBuiltin("CRT_INSLINE");
            }
            else if (call.Name.ToLower() == "delline")
            {
                // DelLine
                EmitCallBuiltin("CRT_DELLINE");
            }
            else if (call.Name.ToLower() == "cursoron")
            {
                // CursorOn
                EmitCallBuiltin("CRT_CURSORON");
            }
            else if (call.Name.ToLower() == "cursoroff")
            {
                // CursorOff
                EmitCallBuiltin("CRT_CURSOROFF");
            }
            else if (call.Name.ToLower().StartsWith("poke") && call.Arguments.Count >= 2)
            {
                string runtimeFn = "vml_" + call.Name.ToLower();
                GenerateExpression(call.Arguments[0]);  // addr
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
                GenerateExpression(call.Arguments[1]);  // val
                instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 1) }));
                EmitCallBuiltin(runtimeFn);
            }
            else if (call.Name.ToLower().StartsWith("peek") && call.Arguments.Count >= 1)
            {
                string runtimeFn = "vml_" + call.Name.ToLower();
                GenerateExpression(call.Arguments[0]);  // addr
                EmitCallBuiltin(runtimeFn);
            }
            else if (call.Name.ToLower() == "chipasm")
            {
                // chipasm("arch", "code") — simplified (转译必需，所有语言保留)
            }
            // asm() 已移除 — 仅限 C/ObjC/C++ 语言使用，Pascal 通过 Lib/c/vmlsys.c 调用系统功能
            else
            {
                GenerateUserDefinedProcedureCall(call);
            }
        }

        private bool IsFileIoCall(ProcedureCallNode call)
        {
            string name = call.Name.ToLower();
            if (name == "assign" || name == "reset" || name == "rewrite" || name == "append" || name == "close")
                return true;
            if ((name == "read" || name == "readln" || name == "write" || name == "writeln") && call.Arguments.Count > 0 && call.Arguments[0] is VariableNode fileVar)
                return GetVariablePascalType(fileVar.Name) == PascalType.File;
            return false;
        }

    }
}
