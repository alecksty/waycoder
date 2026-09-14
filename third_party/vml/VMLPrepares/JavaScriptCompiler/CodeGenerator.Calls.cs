using VMLAssembler;
using System.Collections.Generic;
using CompilerBase;

namespace JavaScriptCompiler
{
    public partial class CodeGenerator
    {
        private void GenerateCall(CallExpression call)
        {
            // Built-in: peek*(addr)
            if (call.Callee is VariableExpression ppv && ppv.Name.StartsWith("peek") && call.Arguments.Count == 1)
            {
                string runtimeFn = "vml_" + ppv.Name;
                GenerateExpression(call.Arguments[0]);
                AddCall(runtimeFn);
                return;
            }
            // Built-in: poke*(addr, val)
            if (call.Callee is VariableExpression pv && pv.Name.StartsWith("poke") && call.Arguments.Count == 2)
            {
                string runtimeFn = "vml_" + pv.Name;
                GenerateExpression(call.Arguments[0]);
                AddInstruction(OpCode.PUSH, Reg(0));
                GenerateExpression(call.Arguments[1]);
                AddInstruction(OpCode.POP, Reg(1));
                AddCall(runtimeFn);
                return;
            }
            if (call.Callee is VariableExpression cv2 && cv2.Name == "chipasm" && call.Arguments.Count >= 2)
            {
                return;
            }
            // asm() 已移除 — 仅限 C/ObjC/C++ 语言使用，JavaScript 通过 Lib/shared/vmlsys.c 调用系统功能
            // exit(n) → MOVE R0, n; SYSCALL 3
            if (call.Callee is VariableExpression ev && ev.Name == "exit" && call.Arguments.Count >= 1)
            {
                GenerateExpression(call.Arguments[0]);
                EmitExit();
                return;
            }

            string functionName = "unknown";

            if (call.Callee is MemberExpression memberExpr)
            {
                if (memberExpr.Object is VariableExpression objVarExpr)
                {
                    // super.method() — resolve and emit call to parent class method
                    if (objVarExpr.Name == "super")
                    {
                        if (_currentClassName != null && _classParentMap.TryGetValue(_currentClassName, out string? superParent))
                        {
                            string superFuncName = $"{superParent}_{memberExpr.Property}";
                            for (int i = call.Arguments.Count - 1; i >= 0; i--)
                            {
                                GenerateExpression(call.Arguments[i]);
                                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                            }
                            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, superFuncName)]));
                            if (call.Arguments.Count > 0)
                                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, call.Arguments.Count * 4)]));
                            return;
                        }
                        // No parent class — fall through to error
                        functionName = "super_method_undefined";
                    }
                    string method = memberExpr.Property;
                    if (method == "push" && call.Arguments.Count == 1)
                    {
                        GenerateExpression(memberExpr.Object); // R0 = array ptr
                        AddInstruction(OpCode.PUSH, Reg(0));    // save array ptr
                        GenerateExpression(call.Arguments[0]);  // R0 = value
                        AddInstruction(OpCode.MOVE, Reg(1), Reg(0)); // R1 = value
                        AddInstruction(OpCode.POP, Reg(0));    // R0 = array ptr
                        AddCall("arr_push"); // arr_push(arr, value)
                        // Return new length
                        AddInstruction(OpCode.MOVE, Reg(0), Mem("R0")); // R0 = arr[0] = new length
                        return;
                    }
                    if (method == "pop" && call.Arguments.Count == 0)
                    {
                        GenerateExpression(memberExpr.Object); // R0 = array ptr
                        AddCall("arr_pop"); // arr_pop(arr) → R0 = popped value
                        return;
                    }
                    if (method == "log")
                    {
                        // console.log(...) — MCU/OS模式感知
                        foreach (var arg in call.Arguments)
                        {
                            GenerateExpression(arg);
                            if (arg is LiteralExpression lit && lit.Value is string)
                            {
                                int strSyscall = VMLPlugins.CompilerOptionsContext.Current.IsMCU ? 1 : 391;
                                instructions.Add(new Instruction(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, strSyscall)]));
                            }
                            else EmitPrintInt();
                        }
                        EmitPrintNewline();
                        return;
                    }
                    if (method == "charAt" && call.Arguments.Count == 1)
                    {
                        GenerateExpression(memberExpr.Object); // R0 = string ptr
                        AddInstruction(OpCode.PUSH, Reg(0));    // save string ptr
                        GenerateExpression(call.Arguments[0]);  // R0 = index
                        AddInstruction(OpCode.POP, Reg(1));    // R1 = string ptr
                        AddCall("str_charat"); // shared_str_charat(str, index) → R0 = char
                        return;
                    }
                    if (method == "toUpperCase" && call.Arguments.Count == 0)
                    {
                        GenerateExpression(memberExpr.Object);    // R0 = string ptr
                        AddInstruction(OpCode.MOVE, Reg(1), Reg(0)); // R1 = src = R0 (in-place)
                        AddCall("str_toupper"); // shared_str_toupper(dst=R0, src=R1)
                        return;
                    }
                    if (method == "toLowerCase" && call.Arguments.Count == 0)
                    {
                        GenerateExpression(memberExpr.Object);    // R0 = string ptr
                        AddInstruction(OpCode.MOVE, Reg(1), Reg(0)); // R1 = src = R0 (in-place)
                        AddCall("str_tolower"); // shared_str_tolower(dst=R0, src=R1)
                        return;
                    }
                    if (method == "trim" && call.Arguments.Count == 0)
                    {
                        GenerateExpression(memberExpr.Object);    // R0 = string ptr
                        AddInstruction(OpCode.MOVE, Reg(1), Reg(0)); // R1 = src = R0 (in-place)
                        AddCall("str_trim"); // shared_str_trim(dst=R0, src=R1)
                        return;
                    }
                    if (method == "startsWith" && call.Arguments.Count >= 1)
                    {
                        GenerateExpression(memberExpr.Object); // R0 = string
                        AddInstruction(OpCode.PUSH, Reg(0));
                        GenerateExpression(call.Arguments[0]); // R0 = prefix
                        AddInstruction(OpCode.POP, Reg(1));
                        AddCall("str_startswith"); // shared_str_startswith(str, prefix) → 1/0
                        return;
                    }
                    if (method == "endsWith" && call.Arguments.Count >= 1)
                    {
                        GenerateExpression(memberExpr.Object);
                        AddInstruction(OpCode.PUSH, Reg(0));
                        GenerateExpression(call.Arguments[0]);
                        AddInstruction(OpCode.POP, Reg(1));
                        AddCall("str_endswith");
                        return;
                    }
                    if (method == "forEach" && call.Arguments.Count == 1)
                    {
                        GenerateArrayIterateMethod(memberExpr.Object, call.Arguments[0], "forEach");
                        return;
                    }
                    if (method == "map" && call.Arguments.Count == 1)
                    {
                        GenerateArrayIterateMethod(memberExpr.Object, call.Arguments[0], "map");
                        return;
                    }
                    if (method == "filter" && call.Arguments.Count == 1)
                    {
                        GenerateArrayIterateMethod(memberExpr.Object, call.Arguments[0], "filter");
                        return;
                    }
                    if (method == "concat" && call.Arguments.Count >= 1)
                    {
                        // arr.concat(other) — CALL arr_concat
                        GenerateExpression(memberExpr.Object); // R0 = arr a
                        AddInstruction(OpCode.PUSH, Reg(0));    // save a
                        GenerateExpression(call.Arguments[0]);  // R0 = arr b
                        AddInstruction(OpCode.PUSH, Reg(0));    // save b
                        // allocate dst: (a[0]+b[0]+1)*4 bytes
                        AddInstruction(OpCode.MOVE, Reg(0), new Operand(OperandType.MEMORY, "R0")); // b[0]
                        AddInstruction(OpCode.POP, Reg(1));    // R1 = b (need a[0] from stack)
                        AddInstruction(OpCode.MOVE, Reg(2), new Operand(OperandType.MEMORY, "R13")); // a[0] from stack
                        AddInstruction(OpCode.ADD, Reg(0), Reg(2)); // total = a[0] + b[0]
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
                        AddInstruction(OpCode.SHL, Reg(0), new Operand(OperandType.IMMEDIATE, 2));
                        EmitAlloc(); // malloc → R0 = dst
                        AddInstruction(OpCode.MOVE, Reg(2), Reg(0)); // R2 = dst
                        AddInstruction(OpCode.POP, Reg(1));    // R1 = b
                        AddInstruction(OpCode.POP, Reg(0));    // R0 = a
                        AddCall("arr_concat");
                        AddInstruction(OpCode.MOVE, Reg(0), Reg(2)); // return dst
                        return;
                    }

                    if (method == "reduce" && call.Arguments.Count >= 1)
                    {
                        // arr.reduce(callback, initialValue) — left fold
                        bool hasInit = call.Arguments.Count >= 2;
                        string rdArrLbl = $"__rd_arr_{labelCounter}";
                        string rdLenLbl = $"__rd_len_{labelCounter}";
                        string rdAccLbl = $"__rd_acc_{labelCounter}";
                        string rdIdxLbl = $"__rd_i_{labelCounter}";
                        string rdLoopLbl = $"__rd_loop_{labelCounter}";
                        string rdEndLbl = $"__rd_end_{labelCounter}";
                        labelCounter++;
                        dataSection[rdArrLbl] = 0; dataSection[rdLenLbl] = 0;
                        dataSection[rdAccLbl] = 0; dataSection[rdIdxLbl] = 0;

                        // Save array and length
                        GenerateExpression(memberExpr.Object);
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, rdArrLbl), Reg(0)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, rdLenLbl), Reg(0)]));

                        // Get callback label
                        string rdCbLabel;
                        if (call.Arguments[0] is FunctionExpression || call.Arguments[0] is ArrowFunctionExpression)
                        {
                            string afterFuncLabel = $"after_func_{labelCounter++}";
                            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, afterFuncLabel)]));
                            int prevLabelId = labelCounter;
                            GenerateExpression(call.Arguments[0]);
                            rdCbLabel = call.Arguments[0] is FunctionExpression ? $"func_expr_{prevLabelId}" : $"arrow_{prevLabelId}";
                            labels[afterFuncLabel] = instructions.Count;
                        }
                        else
                        {
                            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
                            return;
                        }

                        // Set initial accumulator: initialValue or arr[0]
                        if (hasInit)
                        {
                            GenerateExpression(call.Arguments[1]);
                        }
                        else
                        {
                            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, rdArrLbl)]));
                            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0+4")]));
                        }
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, rdAccLbl), Reg(0)]));

                        // Start index: 0 if hasInit, 1 if not
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, hasInit ? 0 : 1)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, rdIdxLbl), Reg(0)]));

                        labels[rdLoopLbl] = instructions.Count;
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, rdIdxLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, rdLenLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
                        instructions.Add(new Instruction(OpCode.CMP, [Reg(0), Reg(1)]));
                        instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, rdEndLbl)]));

                        // Load element
                        instructions.Add(new Instruction(OpCode.SHL, [Reg(0), new Operand(OperandType.IMMEDIATE, 2)]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 4)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, rdArrLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(1)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));

                        // Call callback(accumulator, element) — push acc then element
                        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)])); // element
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, rdAccLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)])); // accumulator
                        instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, rdCbLabel)]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(13), new Operand(OperandType.IMMEDIATE, 8)]));
                        // Update accumulator
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, rdAccLbl), Reg(0)]));

                        // i++
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, rdIdxLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, rdIdxLbl), Reg(0)]));
                        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, rdLoopLbl)]));

                        labels[rdEndLbl] = instructions.Count;
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, rdAccLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        return;
                    }
                    if (method == "find" && call.Arguments.Count == 1)
                    {
                        string fdArrLbl = $"__fd_arr_{labelCounter}";
                        string fdLenLbl = $"__fd_len_{labelCounter}";
                        string fdIdxLbl = $"__fd_i_{labelCounter}";
                        string fdValLbl = $"__fd_val_{labelCounter}";
                        string fdLoopLbl = $"__fd_loop_{labelCounter}";
                        string fdFoundLbl = $"__fd_found_{labelCounter}";
                        string fdNotFoundLbl = $"__fd_nf_{labelCounter}";
                        string fdEndLbl = $"__fd_end_{labelCounter}";
                        labelCounter++;
                        dataSection[fdArrLbl] = 0; dataSection[fdLenLbl] = 0;
                        dataSection[fdIdxLbl] = 0; dataSection[fdValLbl] = 0;

                        GenerateExpression(memberExpr.Object);
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, fdArrLbl), Reg(0)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, fdLenLbl), Reg(0)]));

                        string fdCbLabel;
                        if (call.Arguments[0] is FunctionExpression || call.Arguments[0] is ArrowFunctionExpression)
                        {
                            string afterFuncLabel = $"after_func_{labelCounter++}";
                            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, afterFuncLabel)]));
                            int prevLabelId = labelCounter;
                            GenerateExpression(call.Arguments[0]);
                            fdCbLabel = call.Arguments[0] is FunctionExpression ? $"func_expr_{prevLabelId}" : $"arrow_{prevLabelId}";
                            labels[afterFuncLabel] = instructions.Count;
                        }
                        else { instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)])); return; }

                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, fdIdxLbl), Reg(0)]));

                        labels[fdLoopLbl] = instructions.Count;
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, fdIdxLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, fdLenLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
                        instructions.Add(new Instruction(OpCode.CMP, [Reg(0), Reg(1)]));
                        instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, fdNotFoundLbl)]));

                        // Load element, save to temp
                        instructions.Add(new Instruction(OpCode.SHL, [Reg(0), new Operand(OperandType.IMMEDIATE, 2)]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 4)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, fdArrLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(1)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, fdValLbl), Reg(0)]));

                        // Call callback(element)
                        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                        instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, fdCbLabel)]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(13), new Operand(OperandType.IMMEDIATE, 4)]));
                        instructions.Add(new Instruction(OpCode.JNZ, [Reg(0), new Operand(OperandType.LABEL, fdFoundLbl)]));

                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, fdIdxLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, fdIdxLbl), Reg(0)]));
                        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, fdLoopLbl)]));

                        labels[fdFoundLbl] = instructions.Count;
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, fdValLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, fdEndLbl)]));

                        labels[fdNotFoundLbl] = instructions.Count;
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));

                        labels[fdEndLbl] = instructions.Count;
                        return;
                    }
                    if (method == "some" && call.Arguments.Count == 1)
                    {
                        string smArrLbl = $"__sm_arr_{labelCounter}";
                        string smLenLbl = $"__sm_len_{labelCounter}";
                        string smIdxLbl = $"__sm_i_{labelCounter}";
                        string smLoopLbl = $"__sm_loop_{labelCounter}";
                        string smTrueLbl = $"__sm_true_{labelCounter}";
                        string smFalseLbl = $"__sm_false_{labelCounter}";
                        string smEndLbl = $"__sm_end_{labelCounter}";
                        labelCounter++;
                        dataSection[smArrLbl] = 0; dataSection[smLenLbl] = 0; dataSection[smIdxLbl] = 0;

                        GenerateExpression(memberExpr.Object);
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, smArrLbl), Reg(0)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, smLenLbl), Reg(0)]));

                        string smCbLabel;
                        if (call.Arguments[0] is FunctionExpression || call.Arguments[0] is ArrowFunctionExpression)
                        {
                            string afterFuncLabel = $"after_func_{labelCounter++}";
                            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, afterFuncLabel)]));
                            int prevLabelId = labelCounter;
                            GenerateExpression(call.Arguments[0]);
                            smCbLabel = call.Arguments[0] is FunctionExpression ? $"func_expr_{prevLabelId}" : $"arrow_{prevLabelId}";
                            labels[afterFuncLabel] = instructions.Count;
                        }
                        else { instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)])); return; }

                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, smIdxLbl), Reg(0)]));

                        labels[smLoopLbl] = instructions.Count;
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, smIdxLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, smLenLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
                        instructions.Add(new Instruction(OpCode.CMP, [Reg(0), Reg(1)]));
                        instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, smFalseLbl)]));

                        instructions.Add(new Instruction(OpCode.SHL, [Reg(0), new Operand(OperandType.IMMEDIATE, 2)]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 4)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, smArrLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(1)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                        instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, smCbLabel)]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(13), new Operand(OperandType.IMMEDIATE, 4)]));
                        instructions.Add(new Instruction(OpCode.JNZ, [Reg(0), new Operand(OperandType.LABEL, smTrueLbl)]));

                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, smIdxLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, smIdxLbl), Reg(0)]));
                        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, smLoopLbl)]));

                        labels[smTrueLbl] = instructions.Count;
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
                        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, smEndLbl)]));

                        labels[smFalseLbl] = instructions.Count;
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));

                        labels[smEndLbl] = instructions.Count;
                        return;
                    }
                    if (method == "every" && call.Arguments.Count == 1)
                    {
                        string evArrLbl = $"__ev_arr_{labelCounter}";
                        string evLenLbl = $"__ev_len_{labelCounter}";
                        string evIdxLbl = $"__ev_i_{labelCounter}";
                        string evLoopLbl = $"__ev_loop_{labelCounter}";
                        string evFalseLbl = $"__ev_false_{labelCounter}";
                        string evAllTrueLbl = $"__ev_alltrue_{labelCounter}";
                        string evEndLbl = $"__ev_end_{labelCounter}";
                        labelCounter++;
                        dataSection[evArrLbl] = 0; dataSection[evLenLbl] = 0; dataSection[evIdxLbl] = 0;

                        GenerateExpression(memberExpr.Object);
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, evArrLbl), Reg(0)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, evLenLbl), Reg(0)]));

                        string evCbLabel;
                        if (call.Arguments[0] is FunctionExpression || call.Arguments[0] is ArrowFunctionExpression)
                        {
                            string afterFuncLabel = $"after_func_{labelCounter++}";
                            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, afterFuncLabel)]));
                            int prevLabelId = labelCounter;
                            GenerateExpression(call.Arguments[0]);
                            evCbLabel = call.Arguments[0] is FunctionExpression ? $"func_expr_{prevLabelId}" : $"arrow_{prevLabelId}";
                            labels[afterFuncLabel] = instructions.Count;
                        }
                        else { instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 1)])); return; }

                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, evIdxLbl), Reg(0)]));

                        labels[evLoopLbl] = instructions.Count;
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, evIdxLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, evLenLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
                        instructions.Add(new Instruction(OpCode.CMP, [Reg(0), Reg(1)]));
                        instructions.Add(new Instruction(OpCode.JGE, [new Operand(OperandType.LABEL, evAllTrueLbl)]));

                        instructions.Add(new Instruction(OpCode.SHL, [Reg(0), new Operand(OperandType.IMMEDIATE, 2)]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 4)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, evArrLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R1")]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(1)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.PUSH, [Reg(0)]));
                        instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, evCbLabel)]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(13), new Operand(OperandType.IMMEDIATE, 4)]));
                        instructions.Add(new Instruction(OpCode.JZ, [Reg(0), new Operand(OperandType.LABEL, evFalseLbl)]));

                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, evIdxLbl)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, evIdxLbl), Reg(0)]));
                        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, evLoopLbl)]));

                        labels[evFalseLbl] = instructions.Count;
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
                        instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, evEndLbl)]));

                        labels[evAllTrueLbl] = instructions.Count;
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));

                        labels[evEndLbl] = instructions.Count;
                        return;
                    }
                    if (method == "slice" && (call.Arguments.Count == 1 || call.Arguments.Count == 2))
                    {
                        // arr.slice(begin, end) — CALL arr_slice
                        GenerateExpression(memberExpr.Object); // R0 = arr
                        AddInstruction(OpCode.PUSH, Reg(0));    // save arr
                        // start index
                        GenerateExpression(call.Arguments[0]);  // R0 = begin
                        AddInstruction(OpCode.PUSH, Reg(0));    // save begin
                        // count = (end - begin) or (len - begin)
                        if (call.Arguments.Count >= 2) {
                            GenerateExpression(call.Arguments[1]); // R0 = end
                            AddInstruction(OpCode.POP, Reg(1));    // R1 = begin
                            AddInstruction(OpCode.SUB, Reg(0), Reg(1)); // R0 = end - begin = count
                        } else {
                            AddInstruction(OpCode.POP, Reg(1));    // R1 = begin
                            AddInstruction(OpCode.POP, Reg(2));    // R2 = arr (need len)
                            AddInstruction(OpCode.MOVE, Reg(0), new Operand(OperandType.MEMORY, "R2")); // R0 = len
                            AddInstruction(OpCode.SUB, Reg(0), Reg(1)); // R0 = len - begin = count
                            AddInstruction(OpCode.PUSH, Reg(2));    // re-save arr
                        }
                        AddInstruction(OpCode.PUSH, Reg(0));    // save count
                        // allocate dst: (count+1)*4 bytes
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
                        AddInstruction(OpCode.SHL, Reg(0), new Operand(OperandType.IMMEDIATE, 2));
                        EmitAlloc(); // malloc → R0 = dst
                        // setup params: arr_slice(src, start, count, dst)
                        AddInstruction(OpCode.MOVE, Reg(3), Reg(0)); // R3 = dst
                        AddInstruction(OpCode.POP, Reg(2));    // R2 = count
                        AddInstruction(OpCode.POP, Reg(1));    // R1 = start
                        AddInstruction(OpCode.POP, Reg(0));    // R0 = arr
                        AddCall("arr_slice");
                        AddInstruction(OpCode.MOVE, Reg(0), Reg(3)); // return dst
                        return;
                    }

                    if (method == "join" && call.Arguments.Count <= 1)
                    {
                        // arr.join(sep) — CALL arr_join_str
                        GenerateExpression(memberExpr.Object); // R0 = arr
                        AddInstruction(OpCode.PUSH, Reg(0));    // save arr
                        // delimiter: default ','
                        if (call.Arguments.Count >= 1)
                            GenerateExpression(call.Arguments[0]); // R0 = sep string ptr
                        else {
                            string defComma = $"__comma_{labelCounter}";
                            dataSection[defComma] = ",";
                            labelCounter++;
                            AddInstruction(OpCode.MOVE, Reg(0), LabelOp(defComma));
                        }
                        AddInstruction(OpCode.MOVEB, Reg(1), new Operand(OperandType.MEMORY, "R0")); // R1 = delim char
                        // allocate buffer: max 16 bytes per element
                        AddInstruction(OpCode.POP, Reg(0));    // R0 = arr
                        AddInstruction(OpCode.MOVE, Reg(2), new Operand(OperandType.MEMORY, "R0")); // R2 = len
                        AddInstruction(OpCode.SHL, Reg(2), new Operand(OperandType.IMMEDIATE, 4)); // len * 16
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(2), Reg(2), new Operand(OperandType.IMMEDIATE, 16)]));
                        AddInstruction(OpCode.MOVE, Reg(0), Reg(2));
                        EmitAlloc(); // malloc → R0 = dst
                        AddInstruction(OpCode.MOVE, Reg(2), Reg(0)); // R2 = dst
                        AddInstruction(OpCode.POP, Reg(0));    // R0 = arr (was on stack)
                        AddCall("arr_join_str"); // arr_join_str(arr, delim, dst)
                        AddInstruction(OpCode.MOVE, Reg(0), Reg(2)); // return dst
                        return;
                    }

                    if (method == "indexOf" && call.Arguments.Count >= 1)
                    {
                        GenerateExpression(memberExpr.Object); // R0 = array ptr
                        AddInstruction(OpCode.PUSH, Reg(0));    // save array ptr
                        GenerateExpression(call.Arguments[0]);  // R0 = search value
                        AddInstruction(OpCode.MOVE, Reg(1), Reg(0)); // R1 = value
                        AddInstruction(OpCode.POP, Reg(0));    // R0 = array ptr
                        AddCall("arr_indexof"); // arr_indexof(arr, value) → R0 = index
                        return;
                    }
                    if (method == "includes" && call.Arguments.Count >= 1)
                    {
                        GenerateExpression(memberExpr.Object); // R0 = array ptr
                        AddInstruction(OpCode.PUSH, Reg(0));    // save array ptr
                        GenerateExpression(call.Arguments[0]);  // R0 = search value
                        AddInstruction(OpCode.MOVE, Reg(1), Reg(0)); // R1 = value
                        AddInstruction(OpCode.POP, Reg(0));    // R0 = array ptr
                        AddCall("arr_contains"); // arr_contains(arr, value) → R0 = 1/0
                        return;
                    }
                    if (method == "reverse" && call.Arguments.Count == 0)
                    {
                        GenerateExpression(memberExpr.Object); // R0 = array ptr
                        AddCall("arr_reverse"); // arr_reverse(arr) — in-place
                        return;
                    }
                    if (method == "sort" && call.Arguments.Count <= 1)
                    {
                        GenerateExpression(memberExpr.Object); // R0 = array ptr
                        AddCall("arr_sort_bubble"); // arr_sort_bubble(arr) — in-place sort
                        return;
                    }
                    if (method == "repeat" && call.Arguments.Count == 1)
                    {
                        // shared_str_repeat(dst, src, n): R0=dst, R1=src, R2=n
                        GenerateExpression(memberExpr.Object); // R0 = src
                        AddInstruction(OpCode.PUSH, Reg(0));   // save src on stack
                        GenerateExpression(call.Arguments[0]); // R0 = n
                        AddInstruction(OpCode.PUSH, Reg(0));   // save n on stack
                        // strlen(src): load src from stack
                        AddInstruction(OpCode.MOVE, Reg(0), Mem("R13+4")); // R0 = src
                        AddCall("strlen"); // R0 = len (may clobber R1-R3)
                        // buffer size = len * n + 1
                        AddInstruction(OpCode.MOVE, Reg(2), Mem("R13")); // R2 = n (from stack top)
                        AddInstruction(OpCode.MUL, Reg(0), Reg(2)); // R0 = len * n
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
                        EmitAlloc(); // malloc → R0 = dst
                        // Pop saved values into correct regs: R2=n, R1=src
                        AddInstruction(OpCode.POP, Reg(2));   // R2 = n
                        AddInstruction(OpCode.POP, Reg(1));   // R1 = src
                        // R0 already = dst
                        AddCall("str_repeat");
                        return;
                    }
                    if (method == "padStart" && call.Arguments.Count == 2)
                    {
                        // shared_str_padstart(dst, src, totalLen, padChar): R0=dst, R1=src, R2=totalLen, R3=padChar
                        GenerateExpression(memberExpr.Object); // R0 = src
                        AddInstruction(OpCode.PUSH, Reg(0));   // save src
                        GenerateExpression(call.Arguments[0]); // R0 = totalLen
                        AddInstruction(OpCode.PUSH, Reg(0));   // save totalLen
                        GenerateExpression(call.Arguments[1]); // R0 = padChar
                        AddInstruction(OpCode.PUSH, Reg(0));   // save padChar
                        // Allocate buffer: totalLen + 1
                        AddInstruction(OpCode.MOVE, Reg(0), Mem("R13+4")); // R0 = totalLen
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
                        EmitAlloc(); // malloc → R0 = dst
                        // Pop params: R3=padChar, R2=totalLen, R1=src
                        AddInstruction(OpCode.POP, Reg(3));   // R3 = padChar
                        AddInstruction(OpCode.POP, Reg(2));   // R2 = totalLen
                        AddInstruction(OpCode.POP, Reg(1));   // R1 = src
                        // R0 already = dst
                        AddCall("str_padstart");
                        return;
                    }
                    if (method == "padEnd" && call.Arguments.Count == 2)
                    {
                        // shared_str_padend(dst, src, totalLen, padChar): R0=dst, R1=src, R2=totalLen, R3=padChar
                        GenerateExpression(memberExpr.Object); // R0 = src
                        AddInstruction(OpCode.PUSH, Reg(0));   // save src
                        GenerateExpression(call.Arguments[0]); // R0 = totalLen
                        AddInstruction(OpCode.PUSH, Reg(0));   // save totalLen
                        GenerateExpression(call.Arguments[1]); // R0 = padChar
                        AddInstruction(OpCode.PUSH, Reg(0));   // save padChar
                        // Allocate buffer: totalLen + 1
                        AddInstruction(OpCode.MOVE, Reg(0), Mem("R13+4")); // R0 = totalLen
                        instructions.Add(new Instruction(OpCode.ADD, [Reg(0), Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
                        EmitAlloc(); // malloc → R0 = dst
                        // Pop params: R3=padChar, R2=totalLen, R1=src
                        AddInstruction(OpCode.POP, Reg(3));   // R3 = padChar
                        AddInstruction(OpCode.POP, Reg(2));   // R2 = totalLen
                        AddInstruction(OpCode.POP, Reg(1));   // R1 = src
                        // R0 already = dst
                        AddCall("str_padend");
                        return;
                    }
                    if (method == "split" && call.Arguments.Count == 1)
                    {
                        // shared_str_split(src, delim, parts, maxParts): R0=src, R1=delim, R2=parts, R3=maxParts
                        GenerateExpression(memberExpr.Object); // R0 = src
                        AddInstruction(OpCode.PUSH, Reg(0));   // save src
                        GenerateExpression(call.Arguments[0]); // R0 = separator
                        // Extract first char as delimiter
                        AddInstruction(OpCode.MOVEB, Reg(1), new Operand(OperandType.MEMORY, "R0")); // R1 = first char
                        AddInstruction(OpCode.PUSH, Reg(1));   // save delim char
                        // Allocate parts array: 32 pointers * 4 = 128 bytes
                        AddInstruction(OpCode.MOVE, Reg(0), new Operand(OperandType.IMMEDIATE, 128));
                        EmitAlloc(); // malloc → R0 = parts array
                        // Setup params: R3=maxParts, R2=parts, R1=delim, R0=src
                        AddInstruction(OpCode.MOVE, Reg(3), new Operand(OperandType.IMMEDIATE, 32)); // maxParts
                        AddInstruction(OpCode.MOVE, Reg(2), Reg(0)); // R2 = parts array
                        AddInstruction(OpCode.POP, Reg(1));   // R1 = delim
                        AddInstruction(OpCode.POP, Reg(0));   // R0 = src
                        AddCall("str_split"); // returns count in R0, parts written to array
                        AddInstruction(OpCode.MOVE, Reg(0), Reg(2)); // return parts array
                        return;
                    }
                    // Handle other methods on objects: try to call as function
                    string objName = objVarExpr.Name == "this" ? _currentClassName : objVarExpr.Name;
                    functionName = $"{objName}_{method}";
                    // Walk full parent chain to find method (deep inheritance support)
                    if (!labels.ContainsKey(functionName) && !labels.ContainsKey("func_" + functionName)
                        && objName != null)
                    {
                        string? chainClass = objName;
                        while (_classParentMap.TryGetValue(chainClass, out string parentClass))
                        {
                            string parentFuncName = $"{parentClass}_{method}";
                            if (labels.ContainsKey(parentFuncName) || labels.ContainsKey("func_" + parentFuncName))
                            {
                                functionName = parentFuncName;
                                break;
                            }
                            chainClass = parentClass;
                        }
                    }
                }
                // Handle other MemberExpression calls: try to resolve method name
                else if (memberExpr.Property != null)
                {
                    functionName = memberExpr.Property;
                }
                // Handle Array.isArray(x) as built-in
                if (functionName == "isArray" && call.Arguments.Count == 1)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
                    return;
                }
                // Object.create(proto): allocate new object with prototype pointer
                if (functionName == "Object_create" && call.Arguments.Count == 1)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 8)]));
                    AddSyscall(40);
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), Reg(0)]));
                    GenerateExpression(call.Arguments[0]);
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), Reg(0)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), Reg(1)]));
                    return;
                }
                // Object.getPrototypeOf(obj): read proto pointer at offset 0
                if (functionName == "Object_getPrototypeOf" && call.Arguments.Count == 1)
                {
                    GenerateExpression(call.Arguments[0]);
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));
                    return;
                }
                // Object.setPrototypeOf(obj, proto): write proto pointer at offset 0
                if (functionName == "Object_setPrototypeOf" && call.Arguments.Count == 2)
                {
                    GenerateExpression(call.Arguments[0]);
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), Reg(0)]));
                    GenerateExpression(call.Arguments[1]);
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R1"), Reg(0)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), Reg(1)]));
                    return;
                }
                if (functionName == "console_log" || functionName == "log")
                {
                    foreach (var arg in call.Arguments)
                    {
                        GenerateExpression(arg);
                        EmitPrintString();
                    }
                    return;
                }
                foreach (var arg in call.Arguments)
                {
                    GenerateExpression(arg);
                    instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                }
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, functionName)]));
                if (call.Arguments.Count > 0)
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, call.Arguments.Count * 4)]));
                return;
            }

            if (call.Callee is VariableExpression varExpr)
            {
                functionName = varExpr.Name;
                // print/println: 类型感知输出 (字符串/整数/浮点/布尔)
                if (functionName == "print" || functionName == "println")
                {
                    foreach (var arg in call.Arguments)
                    {
                        GenerateExpression(arg);
                        EmitTypedPrint(arg);
                    }
                    if (functionName == "println")
                        EmitPrintNewline();
                    return;
                }
                // 全类型转换函数 (intToStr/strToInt/floatToStr/boolToStr 等) → 直接 CALL 语言包装器
                // (Lib/javascript/conv.vml 中的 LABEL intToStr 等, 转发到 shared/conv.vml 的 int_to_str)
                if (IsConvFunction(functionName) && call.Arguments.Count == 1)
                {
                    GenerateExpression(call.Arguments[0]);
                    AddCall(functionName);
                    return;
                }
                // 内置函数处理
                if (functionName == "parseInt" && call.Arguments.Count >= 1)
                {
                    // 数值字面量直接返回整数值，无需字符串解析
                    if (call.Arguments[0] is LiteralExpression lit && lit.Value is int intVal)
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, intVal)]));
                        return;
                    }
                    GenerateExpression(call.Arguments[0]); // R0 = string ptr
                    AddInstruction(OpCode.PUSH, Reg(0));
                    AddCall("atoi");
                    instructions.Add(new Instruction(OpCode.ADD, [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, 4)]));
                    return;
                }
                if (functionName == "parseFloat" && call.Arguments.Count >= 1)
                {
                    // 数值字面量直接返回浮点值，无需字符串解析
                    if (call.Arguments[0] is LiteralExpression lit && lit.Value is double doubleVal)
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, (int)doubleVal)]));
                        return;
                    }
                    if (call.Arguments[0] is LiteralExpression lit2 && lit2.Value is int intVal2)
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, intVal2)]));
                        return;
                    }
                    GenerateExpression(call.Arguments[0]); // R0 = string ptr
                    AddInstruction(OpCode.PUSH, Reg(0));
                    AddCall("atof");
                    instructions.Add(new Instruction(OpCode.ADD, [Reg(13), Reg(13), new Operand(OperandType.IMMEDIATE, 4)]));
                    return;
                }
                if (functionName == "isNaN" && call.Arguments.Count == 1)
                {
                    GenerateExpression(call.Arguments[0]);
                    return;
                }
                if (functionName == "isFinite" && call.Arguments.Count == 1)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
                    return;
                }
                if (functionName == "Array_isArray" && call.Arguments.Count == 1)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
                    return;
                }
                // Object.create/getPrototypeOf/setPrototypeOf are handled in MemberExpression branch above
            }

            foreach (var arg in call.Arguments)
            {
                GenerateExpression(arg);
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
            }

            // 函数标签使用 func_ 前缀
            string callTarget = functionName;
            bool isDirectCall = false;
            if (!string.IsNullOrEmpty(functionName) && functionName != "unknown"
                && labels.ContainsKey("func_" + functionName))
            {
                callTarget = "func_" + functionName;
                isDirectCall = true;
            }
            else if (!string.IsNullOrEmpty(functionName) && functionName != "unknown"
                && labels.ContainsKey(functionName))
            {
                isDirectCall = true;
            }

            if (isDirectCall)
            {
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, callTarget)]));
            }
            else
            {
                // 间接调用: 从变量 var_functionName 加载函数地址, 然后 CALL R0
                string varLabel = $"var_{functionName}";
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, varLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R1")]));
                instructions.Add(new Instruction(OpCode.CALL, [Reg(0)]));
            }
            if (call.Arguments.Count > 0)
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, call.Arguments.Count * 4)]));
        }

        /// <summary>print/println 的类型感知输出: 根据参数类型选择 print_str/print_int/print_float/print_bool</summary>
        private void EmitTypedPrint(Expression arg)
        {
            if (arg is LiteralExpression lit)
            {
                if (lit.Value is string) { EmitPrintString(); return; }
                if (lit.Value is float || lit.Value is double) { EmitPrintFloat(); return; }
                if (lit.Value is bool) { EmitPrintBool(); return; }
                EmitPrintInt(); // int/long 字面量
                return;
            }
            if (arg is CallExpression callExpr && callExpr.Callee is VariableExpression calleeVar)
            {
                string name = calleeVar.Name;
                // 字符串返回函数 (intToStr/floatToStr/boolToStr 等)
                if (IsStringReturningFunc(name)) { EmitPrintString(); return; }
                // 整数返回函数 (strToInt/strToLong/atoi 等)
                if (IsIntReturningFunc(name)) { EmitPrintInt(); return; }
            }
            // 变量/未知类型: 保守按字符串处理 (与 console.log 的默认一致)
            EmitPrintString();
        }

        /// <summary>判断函数名是否为全类型转换库函数 (intToStr/strToInt/floatToStr/... 或 int_to_str/str_to_int/...)</summary>
        private static bool IsConvFunction(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var normalized = FunctionNameNormalizer.Normalize(name);
            if (normalized == null) return false;
            return normalized.Contains("_to_")
                || normalized is "atoi" or "atol" or "itoa" or "ftoa" or "dtoa" or "ltoa";
        }

        /// <summary>判断函数名是否返回整数 (strToInt/strToLong/atoi/str_to_int 等)</summary>
        private static bool IsIntReturningFunc(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.Contains("ToInt") || name.Contains("ToLong") || name.Contains("ToShort")
                || name.Contains("ToByte") || name.Contains("ToChar") || name.Contains("ToBool")
                || name.Contains("to_int") || name.Contains("to_long")
                || name == "atoi" || name == "atol" || name == "ltoa";
        }
    }
}
