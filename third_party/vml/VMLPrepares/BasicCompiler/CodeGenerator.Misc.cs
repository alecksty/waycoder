using VMLAssembler;

namespace BasicCompiler
{
    public partial class CodeGenerator
    {
        /// <summary>
        /// 如果表达式结果为浮点（FLOAD 到 F 寄存器），生成 F2I 转换回整数寄存器
        /// BASIC 内置函数 (SIN/COS/TAN/ATN/SQR) 需要整数寄存器输入
        /// </summary>
        private void EnsureIntReg(Expression expr, int reg)
        {
            BasicType t = InferExpressionType(expr);
            if (t == BasicType.Single || t == BasicType.Double)
                AddRR(OpCode.F2I, reg, reg);
        }

        private void GenerateExitSubStatement()
        {
            instructions.Add(new Instruction(OpCode.LEAVE, new List<Operand>()));
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>()));
        }

        private void GenerateExitLoopStatement()
        {
            Sta.EmitBreak();
        }

        private void GenerateBuiltInPeek(FunctionCallExpression funcCall, int reg)
        {
            // ⚠ 名字**不加 `vml_` 前缀**：`peek`/`peekb` 是 `Lib/shared/builtins.vml` 里的
            //   **`__stdcall` 函数**（`asm("MOVE [@R0] R0")` —— 地址走 R0、不走栈），
            //   lib 里的标签就叫 `peek` / `peekb`。从前拼成 `vml_peek`，
            //   链接期报「未定义的函数 'vml_peek'」（GORILLA.BAS 的 `PEEK(1047)` 就是这条）。
            //   ⚠ 别顺手加 `vml_` 又别顺手改成 `basic_`：全仓只有 `Lib/shared/*.vml`
            //   里那几个 `vml_xxx` 是真带前缀的，`peek` 不在其中。
            string runtimeFn = funcCall.FunctionName.ToLower();
            GenerateExpr(funcCall.Arguments[0], reg);  // addr → R0
            instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, runtimeFn) }));
        }

        // 字符串缓冲区（data section 分配，链接器解析）
        private const int StringBufferSize = 512;

        private void EmitStrBufAddr(int r)
        {
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                new Operand(OperandType.REGISTER, r),
                new Operand(OperandType.LABEL, StringBufferLabel)
            }));
        }

        /// <summary>生成对共享库函数的 CALL：求值参数→压栈→CALL→**调用方清栈**→结果入reg
        ///
        /// ⚠ 2026-09-17 调用约定统一后改的：旧注释写「__stdcall: 被调用者清理栈，调用者不需要 ADD R13」，
        /// 那是 `Lib` 里那些函数还没重生成时的形态（收尾会替调用方弹掉实参槽）。现在被调方
        /// 一律裸 `ret`，**压了就必须自己清** —— 否则每次库调用净漏 `实参个数 × 4` 字节，
        /// 攒够就把调用方的栈帧踩花（与 D 的 `^^`、Fortran/Ruby 的 `**`、Forth 的 `."` 同族）。</summary>
        private void GenerateLibraryCall(string funcName, FunctionCallExpression funcCall, int reg, bool returnsFloat = false)
        {
            // 从右到左求值参数并压栈（使用 EvalIntCoord 确保 float→int 转换）
            for (int i = funcCall.Arguments.Count - 1; i >= 0; i--)
            {
                EvalIntCoord(funcCall.Arguments[i], 0);
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
            }
            // CALL 库函数
            instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, funcName) }));
            // ⚠ 清栈必须在**取返回值之前还是之后**都行（返回值在 R0，与 R13 无关），
            //    放在这里紧跟 CALL，与其它前端一致。
            if (funcCall.Arguments.Count > 0)
            {
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> {
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.IMMEDIATE, funcCall.Arguments.Count * 4) }));
            }
            // 结果移到目标寄存器 (浮点函数用F0,整数函数用R0)
            if (returnsFloat)
            {
                if (reg != 0)
                    instructions.Add(new Instruction(OpCode.FADD, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0.0f) }));
            }
            else
            {
                if (reg != 0)
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 0) }));
            }
        }

        private void GenerateCommand(FunctionCallExpression funcCall, int reg)
        {
            // COMMAND$ — 获取命令行参数 (SYSCALL 362)
            // 在栈上分配缓冲区, 调用GetArgs
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 1024) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 13) }));
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 362) }));
            // R0 = argc, 栈上缓冲区填充了参数
            // 简单返回: R0指向参数字符串（首个参数地址在buf+4）
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, "R13+4") }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, 1024) }));
        }

        // ===== 文件操作 =====

        private void GenerateOpenStatement(OpenStatement stmt)
        {
            // OPEN "filename" FOR mode AS #n
            // 使用SYSCALL 104 (DeviceControl) 命令0 (OpenFile)
            
            // 1. 获取文件名地址到R0
            if (stmt.FileName is StringLiteral strLit)
            {
                // 字符串常量: 在数据段创建
                string dataLabel = "str_open_" + labelCounter++;
                dataSection[dataLabel] = new DataString(strLit.Value);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, dataLabel) }));
            }
            else
            {
                GenerateExpression(stmt.FileName, 0);
            }
            
            // 2. 打开"fs"设备
            string fsLabel = "str_fs_" + labelCounter++;
            dataSection[fsLabel] = "fs";
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, fsLabel) }));
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 100) }));
            // R0 = 设备句柄
            
            // 3. 准备OpenFile命令参数: [mode, path...]
            // mode: 0=read, 1=write, 2=append
            int mode = stmt.Mode switch
            {
                FileOpenMode.Input => 0,
                FileOpenMode.Output => 1,
                FileOpenMode.Append => 2,
                _ => 0
            };
            
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) })); // command = OpenFile
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, mode) })); // mode
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 104) }));
            
            // 4. 保存文件句柄到变量
            if (stmt.FileNumber != null)
            {
                // 将文件句柄存入变量(使用文件号作为变量名)
                GenerateExpression(stmt.FileNumber, 1); // 获取文件号
                // 使用文件号*4+0x9D000作为文件句柄存储地址
                instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4) }));
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x9D000) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            }
        }

        private void GenerateCloseStatement(CloseStatement stmt)
        {
            // CLOSE #n 或 CLOSE
            if (stmt.FileNumber != null)
            {
                // 获取文件句柄
                GenerateExpression(stmt.FileNumber, 1);
                instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4) }));
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x9D000) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
                
                // SYSCALL 101 (DeviceClose)
                instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 101) }));
            }
        }

        private void GeneratePrintFileStatement(PrintFileStatement stmt)
        {
            // PRINT #n, expr1, expr2...
            // 获取文件句柄
            GenerateExpression(stmt.FileNumber, 1);
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x9D000) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R1") }));
            // R1 = 文件句柄
            
            // 对每个表达式, 转换为字符串并写入文件
            for (int i = 0; i < stmt.Expressions.Count; i++)
            {
                var expr = stmt.Expressions[i];
                if (expr is StringLiteral strLit)
                {
                    // 字符串常量: 写入数据段标签
                    string dataLabel = "str_pf_" + labelCounter++;
                    dataSection[dataLabel] = new DataString(strLit.Value);
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, dataLabel) }));
                    // 计算字符串长度
                    string lenLoop = GenerateLabel();
                    string lenEnd = GenerateLabel();
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 0) }));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lenLoop) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R2") }));
                    instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 0) }));
                    instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, lenEnd) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 1) }));
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, lenLoop) }));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lenEnd) }));
                    // R3 = 长度, R0 = 地址
                    // SYSCALL 103 (DeviceWrite): R0=handle, R1=buffer, R2=count
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) })); // handle
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 3) })); // count
                    EmitDeviceWrite();
                }
                else
                {
                    // 数字: 转换为字符串再写入
                    GenerateExpression(expr, 0);
                    // 使用STR$内置函数转换
                    var fakeCall = new FunctionCallExpression(0, 0, "STR$");
                    fakeCall.Arguments.Add(expr);
                    GenerateLibraryCall("basic_str_int", fakeCall, 0);
                    // R0 = 字符串地址, 需要计算长度
                    string lenLoop = GenerateLabel();
                    string lenEnd = GenerateLabel();
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 0) }));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lenLoop) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R2") }));
                    instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 0) }));
                    instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, lenEnd) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 1) }));
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, lenLoop) }));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lenEnd) }));
                    // SYSCALL 103
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 3) }));
                    EmitDeviceWrite();
                }
            }
        }

        private void GenerateInputFileStatement(InputFileStatement stmt)
        {
            // INPUT #n, var1, var2...
            // 获取文件句柄
            GenerateExpression(stmt.FileNumber, 1);
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x9D000) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R1") }));
            // R1 = 文件句柄
            
            // 对每个变量, 从文件读取
            for (int i = 0; i < stmt.Variables.Count; i++)
            {
                string varName = stmt.Variables[i].Name;
                // 分配读取缓冲区
                string bufLabel = "input_buf_" + labelCounter++;
                dataSection[bufLabel] = new string('\0', 64); // 64字节缓冲区
                
                // SYSCALL 102 (DeviceRead): R0=handle, R1=buffer, R2=count
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) })); // handle
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.LABEL, bufLabel) })); // buffer
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 64) })); // count
                instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 102) }));
                
                // 将读取的字符串转换为整数存入变量
                if (variables.ContainsKey(varName))
                {
                    // 简化: 直接存储缓冲区地址
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, bufLabel) }));
                    // ⚠ 这一行其实是**读**（把变量当前值取到 R0），不是写 —— 它紧跟着把上面刚放进去的
                    //   缓冲区地址覆盖掉了（`INPUT #` 整体是坏的，属既有缺陷）。这里只把寻址从
                    //   `R12+8+索引*4`（主帧、且是另一套偏移算法）改成全局段的统一入口。
                    EmitLoadVar(0, varName);
                }
            }
        }

        private void GenerateSelectCaseStatement(SelectCaseStatement stmt)
        {
            // 生成测试表达式的值
            if (currentSubName != null)
            {
                GenerateSubExpression(stmt.TestExpression, 0);
            }
            else
            {
                GenerateExpression(stmt.TestExpression, 0);
            }
            
            // PUSH 保护测试值 → 保存到 R10
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 0) }));
            
            // 生成CASE块的结束标签
            string endLabel = GenerateLabel();
            
            // 处理每个CASE块
            foreach (var caseBlock in stmt.CaseBlocks)
            {
                // 为当前CASE块生成标签
                string caseEndLabel = GenerateLabel();
                bool hasCondition = false;
                
                // 为当前CASE块生成跳过标签（如果所有条件都不匹配，跳到这里）
                string skipCaseLabel = GenerateLabel();
                
                // 生成条件检查
                foreach (var condition in caseBlock.Conditions)
                {
                    hasCondition = true;
                    string nextConditionLabel = GenerateLabel();
                    
                    switch (condition.Type)
                    {
                        case CaseCondition.ConditionType.Value:
                            // CASE 5: 检查是否等于特定值
                            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 10) }));
                            if (currentSubName != null)
                            {
                                GenerateSubExpression(condition.Value, 1);
                            }
                            else
                            {
                                GenerateExpression(condition.Value, 1);
                            }
                            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 10) }));
                            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 1) }));
                            instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, nextConditionLabel) }));
                            break;
                            
                        case CaseCondition.ConditionType.Range:
                            // CASE 1 TO 10: 检查是否在范围内
                            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 10) }));
                            if (currentSubName != null)
                            {
                                GenerateSubExpression(condition.FromValue, 1);
                                GenerateSubExpression(condition.ToValue, 2);
                            }
                            else
                            {
                                GenerateExpression(condition.FromValue, 1);
                                GenerateExpression(condition.ToValue, 2);
                            }
                            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 10) }));
                            // 检查是否 >= from
                            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 1) }));
                            instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, nextConditionLabel) }));
                            // 检查是否 <= to
                            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 2) }));
                            instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, nextConditionLabel) }));
                            break;
                            
                        case CaseCondition.ConditionType.Comparison:
                            // CASE IS > 5: 检查比较条件
                            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 10) }));
                            if (currentSubName != null)
                            {
                                GenerateSubExpression(condition.CompareValue, 1);
                            }
                            else
                            {
                                GenerateExpression(condition.CompareValue, 1);
                            }
                            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 10) }));
                            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.REGISTER, 1) }));
                            
                            // 根据比较运算符生成跳转
                            switch (condition.ComparisonOp)
                            {
                                case TokenType.EQUALS:
                                    instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, nextConditionLabel) }));
                                    break;
                                case TokenType.NOT_EQUAL:
                                    instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, nextConditionLabel) }));
                                    break;
                                case TokenType.LESS:
                                    instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, nextConditionLabel) }));
                                    break;
                                case TokenType.GREATER:
                                    instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, nextConditionLabel) }));
                                    break;
                                case TokenType.LESS_EQUAL:
                                    instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, nextConditionLabel) }));
                                    break;
                                case TokenType.GREATER_EQUAL:
                                    instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, nextConditionLabel) }));
                                    break;
                            }
                            break;
                            
                        case CaseCondition.ConditionType.Else:
                            // CASE ELSE: 总是匹配
                            break;
                    }
                    
                    // 如果条件匹配，跳转到CASE块体
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, caseEndLabel) }));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, nextConditionLabel) }));
                }
                
                // 所有条件都不匹配，跳过这个CASE块
                if (hasCondition && caseBlock.Conditions.All(c => c.Type != CaseCondition.ConditionType.Else))
                {
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, skipCaseLabel) }));
                }
                
                // 如果没有条件（不应该发生），跳过这个CASE块
                if (!hasCondition)
                {
                    continue;
                }
                
                // CASE块体
                instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, caseEndLabel) }));
                foreach (var bodyStmt in caseBlock.Body)
                {
                    if (currentSubName != null)
                    {
                        GenerateSubStatement(bodyStmt);
                    }
                    else
                    {
                        GenerateStatement(bodyStmt);
                    }
                }
                // 执行完CASE块后跳转到SELECT结束（先POP清理栈）
                instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
                instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
                
                // 跳过标签（如果所有条件都不匹配，跳到这里）
                instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, skipCaseLabel) }));
            }
            
            // ELSE块（如果有）
            if (stmt.ElseBlock != null)
            {
                foreach (var elseStmt in stmt.ElseBlock)
                {
                    if (currentSubName != null)
                    {
                        GenerateSubStatement(elseStmt);
                    }
                    else
                    {
                        GenerateStatement(elseStmt);
                    }
                }
            }
            
            // 清理栈上保存的测试值（未匹配任何case / ELSE路径）
            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));

            // SELECT CASE结束
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
        }

        // ===== KB/MOUSE 硬件接口 =====

        private void GenerateKbHitStatement(KbHitStatement stmt)
        {
            AddRI(OpCode.MOVE, 0, 0);
        }

        private void GenerateKbGetChStatement(KbGetChStatement stmt)
        {
            AddRI(OpCode.MOVE, 0, 0);
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 5) }));
        }

        private void GenerateMouseGetXStatement(MouseGetXStatement stmt)
        {
            AddRI(OpCode.MOVE, 0, 0);
        }

        private void GenerateMouseGetYStatement(MouseGetYStatement stmt)
        {
            AddRI(OpCode.MOVE, 0, 0);
        }

        private void GenerateMouseLeftStatement(MouseLeftStatement stmt)
        {
            AddRI(OpCode.MOVE, 0, 0);
        }

        private void GenerateMouseRightStatement(MouseRightStatement stmt)
        {
            AddRI(OpCode.MOVE, 0, 0);
        }

        // ===== SCREEN 模式输出路由 =====

        /// <summary>
        /// 生成屏幕模式检查：如果 SCREEN 为 255 或 -1，跳过 VGA 输出。
        /// 返回 skipLabel，调用者在 VGA 代码之后放置此标签。
        /// 使用 R5, R6 作为临时寄存器。
        /// </summary>
        private string EmitScreenCheckForVga()
        {
            string skipLabel = newLabel();
            // Load screen mode from 0x6FF0
            AddRI(OpCode.MOVE, 5, 0x6FF0);
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R5") }));
            // Check SCREEN 255 (disable VGA)
            AddRI(OpCode.CMP, 5, 255);
            instructions.Add(new Instruction(OpCode.JE, new List<Operand> {
                new Operand(OperandType.LABEL, skipLabel) }));
            // Check SCREEN -1 (disable VGA)
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, -1) }));
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> {
                new Operand(OperandType.REGISTER, 5), new Operand(OperandType.REGISTER, 6) }));
            instructions.Add(new Instruction(OpCode.JE, new List<Operand> {
                new Operand(OperandType.LABEL, skipLabel) }));
            return skipLabel;
        }

        /// <summary>CRT 模式: PRINT 走 TTY 通道 (SYSCALL #400/#401)</summary>
        internal bool CrtMode;

        /// <summary>
        /// 输出字符：CRT 模式 → TTY (#400)，否则 → stdout (#4)。
        /// 调用前 R0 必须包含字符码。
        /// </summary>
        internal new void EmitPrintChar()
        {
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> {
                new Operand(OperandType.IMMEDIATE, CrtMode ? 400 : 4) }));
            if (!CrtMode)
            {
                string skipVga = EmitScreenCheckForVga();
                EmitVgaTextChar();
                AddLabel(skipVga);
            }
        }

        /// <summary>
        /// 输出字符串：CRT 模式 → TTY (#401)，否则 → stdout (#1)。
        /// 调用前 R0 必须包含字符串地址。
        /// </summary>
        internal new void EmitPrintString()
        {
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> {
                new Operand(OperandType.IMMEDIATE, CrtMode ? 401 : 1) }));
            if (!CrtMode)
            {
                string skipVga = EmitScreenCheckForVga();
                EmitVgaTextChar();
                AddLabel(skipVga);
            }
        }

        // ===== VGA text mode output — write char to 0xB8000 framebuffer =====

        private void EmitVgaTextChar()
        {
            // Write character (in R0) to VGA text framebuffer at 0xB8000
            // 保留内联实现: BASIC PRINT 与整数→字符串转换紧密耦合，寄存器约定与C库不兼容
            // 增强的 vga_text_putchar C库可供其他语言(Pascal/C等)使用

            // Save all registers we modify: R0 (char), R1-R3 (scratch)
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 1) }));
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 2) }));
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 3) }));

            // Load cursor row (R1) and col (R2), attr (R3) from MMIO
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FF4) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R1") }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0x6FF8) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R2") }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 0x6FFC) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R3") }));

            // Compute VGA addr: 0xB8000 + (row*80+col)*2
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 80) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 2) }));
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 2) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0xB8000) }));

            // Store char and attr byte
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R1") }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.MEMORY, "R1") }));

            // Advance cursor with wrap
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FF8) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R1") }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
            string noWrap = newLabel();
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 80) }));
            instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, noWrap) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0x6FF4) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R2") }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 0x6FF4) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R3") }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, noWrap) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0x6FF8) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R2") }));

            // Restore registers (reverse order)
            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 3) }));
            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 2) }));
            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 1) }));
            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
        }

        // 8x8 font — 95 printable ASCII chars, 8 bytes per char (MSB=left pixel)
        private static readonly byte[] Font8x8 = new byte[95 * 8];
        private bool _fontDataEmitted = false;

        static CodeGenerator()
        {
            // Classic 8x8 pixel font for ASCII 32-126
            string bits =
                "00000000 00000000 00000000 00000000 00000000 00000000 00000000 00000000 " + // 32 space
                "00010000 00010000 00010000 00010000 00010000 00000000 00010000 00000000 " + // 33 !
                "00101000 00101000 00101000 00000000 00000000 00000000 00000000 00000000 " + // 34 "
                "00101000 00101000 01111100 00101000 01111100 00101000 00101000 00000000 " + // 35 #
                "00010000 00111100 01010000 00111000 00010100 01111000 00010000 00000000 " + // 36 $
                "01100010 01100100 00001000 00010000 00100000 01001100 01000110 00000000 " + // 37 %
                "00110000 01001000 01001000 00110000 01001010 01000100 00111010 00000000 " + // 38 &
                "00010000 00010000 00100000 00000000 00000000 00000000 00000000 00000000 " + // 39 '
                "00001000 00010000 00100000 00100000 00100000 00010000 00001000 00000000 " + // 40 (
                "00100000 00010000 00001000 00001000 00001000 00010000 00100000 00000000 " + // 41 )
                "00000000 00010000 01010100 00111000 01010100 00010000 00000000 00000000 " + // 42 *
                "00000000 00010000 00010000 01111100 00010000 00010000 00000000 00000000 " + // 43 +
                "00000000 00000000 00000000 00000000 00011000 00011000 00001000 00010000 " + // 44 ,
                "00000000 00000000 00000000 01111100 00000000 00000000 00000000 00000000 " + // 45 -
                "00000000 00000000 00000000 00000000 00000000 00011000 00011000 00000000 " + // 46 .
                "00000000 00000100 00001000 00010000 00100000 01000000 00000000 00000000 " + // 47 /
                "00111000 01000100 01001100 01010100 01100100 01000100 00111000 00000000 " + // 48 0
                "00010000 00110000 00010000 00010000 00010000 00010000 00111000 00000000 " + // 49 1
                "00111000 01000100 00000100 00001000 00010000 00100000 01111100 00000000 " + // 50 2
                "00111000 01000100 00000100 00011000 00000100 01000100 00111000 00000000 " + // 51 3
                "00001000 00011000 00101000 01001000 01111100 00001000 00001000 00000000 " + // 52 4
                "01111100 01000000 01111000 00000100 00000100 01000100 00111000 00000000 " + // 53 5
                "00111000 01000100 01000000 01111000 01000100 01000100 00111000 00000000 " + // 54 6
                "01111100 00000100 00001000 00010000 00100000 00100000 00100000 00000000 " + // 55 7
                "00111000 01000100 01000100 00111000 01000100 01000100 00111000 00000000 " + // 56 8
                "00111000 01000100 01000100 00111100 00000100 01000100 00111000 00000000 " + // 57 9
                "00000000 00011000 00011000 00000000 00011000 00011000 00000000 00000000 " + // 58 :
                "00000000 00011000 00011000 00000000 00011000 00011000 00001000 00010000 " + // 59 ;
                "00001000 00010000 00100000 01000000 00100000 00010000 00001000 00000000 " + // 60 <
                "00000000 00000000 01111100 00000000 01111100 00000000 00000000 00000000 " + // 61 =
                "00100000 00010000 00001000 00000100 00001000 00010000 00100000 00000000 " + // 62 >
                "00111000 01000100 00000100 00001000 00010000 00000000 00010000 00000000 " + // 63 ?
                "00111000 01000100 01011100 01010100 01011100 01000000 00111000 00000000 " + // 64 @
                "00010000 00101000 01000100 01000100 01111100 01000100 01000100 00000000 " + // 65 A
                "01111000 01000100 01000100 01111000 01000100 01000100 01111000 00000000 " + // 66 B
                "00111000 01000100 01000000 01000000 01000000 01000100 00111000 00000000 " + // 67 C
                "01111000 01000100 01000100 01000100 01000100 01000100 01111000 00000000 " + // 68 D
                "01111100 01000000 01000000 01111000 01000000 01000000 01111100 00000000 " + // 69 E
                "01111100 01000000 01000000 01111000 01000000 01000000 01000000 00000000 " + // 70 F
                "00111000 01000100 01000000 01001100 01000100 01000100 00111000 00000000 " + // 71 G
                "01000100 01000100 01000100 01111100 01000100 01000100 01000100 00000000 " + // 72 H
                "00111000 00010000 00010000 00010000 00010000 00010000 00111000 00000000 " + // 73 I
                "00011100 00001000 00001000 00001000 00001000 01001000 00110000 00000000 " + // 74 J
                "01000100 01001000 01010000 01100000 01010000 01001000 01000100 00000000 " + // 75 K
                "01000000 01000000 01000000 01000000 01000000 01000000 01111100 00000000 " + // 76 L
                "01000100 01101100 01010100 01010100 01000100 01000100 01000100 00000000 " + // 77 M
                "01000100 01100100 01010100 01001100 01000100 01000100 01000100 00000000 " + // 78 N
                "00111000 01000100 01000100 01000100 01000100 01000100 00111000 00000000 " + // 79 O
                "01111000 01000100 01000100 01111000 01000000 01000000 01000000 00000000 " + // 80 P
                "00111000 01000100 01000100 01000100 01010100 01001000 00110100 00000000 " + // 81 Q
                "01111000 01000100 01000100 01111000 01010000 01001000 01000100 00000000 " + // 82 R
                "00111000 01000100 01000000 00111000 00000100 01000100 00111000 00000000 " + // 83 S
                "01111100 00010000 00010000 00010000 00010000 00010000 00010000 00000000 " + // 84 T
                "01000100 01000100 01000100 01000100 01000100 01000100 00111000 00000000 " + // 85 U
                "01000100 01000100 01000100 01000100 00101000 00101000 00010000 00000000 " + // 86 V
                "01000100 01000100 01000100 01010100 01010100 01101100 01000100 00000000 " + // 87 W
                "01000100 01000100 00101000 00010000 00101000 01000100 01000100 00000000 " + // 88 X
                "01000100 01000100 00101000 00010000 00010000 00010000 00010000 00000000 " + // 89 Y
                "01111100 00000100 00001000 00010000 00100000 01000000 01111100 00000000 " + // 90 Z
                "00111000 00100000 00100000 00100000 00100000 00100000 00111000 00000000 " + // 91 [
                "00000000 01000000 00100000 00010000 00001000 00000100 00000000 00000000 " + // 92 backslash
                "00111000 00001000 00001000 00001000 00001000 00001000 00111000 00000000 " + // 93 ]
                "00010000 00101000 01000100 00000000 00000000 00000000 00000000 00000000 " + // 94 ^
                "00000000 00000000 00000000 00000000 00000000 00000000 01111100 00000000 " + // 95 _
                "00100000 00010000 00001000 00000000 00000000 00000000 00000000 00000000 " + // 96 `
                "00000000 00000000 00111000 00000100 00111100 01000100 00111100 00000000 " + // 97 a
                "01000000 01000000 01111000 01000100 01000100 01000100 01111000 00000000 " + // 98 b
                "00000000 00000000 00111000 01000100 01000000 01000100 00111000 00000000 " + // 99 c
                "00000100 00000100 00111100 01000100 01000100 01000100 00111100 00000000 " + // 100 d
                "00000000 00000000 00111000 01000100 01111100 01000000 00111000 00000000 " + // 101 e
                "00001100 00010000 00111000 00010000 00010000 00010000 00010000 00000000 " + // 102 f
                "00000000 00111100 01000100 01000100 00111100 00000100 00111000 00000000 " + // 103 g
                "01000000 01000000 01111000 01000100 01000100 01000100 01000100 00000000 " + // 104 h
                "00010000 00000000 00110000 00010000 00010000 00010000 00111000 00000000 " + // 105 i
                "00001000 00000000 00011000 00001000 00001000 01001000 00110000 00000000 " + // 106 j
                "01000000 01000000 01001000 01010000 01100000 01010000 01001000 00000000 " + // 107 k
                "00110000 00010000 00010000 00010000 00010000 00010000 00111000 00000000 " + // 108 l
                "00000000 00000000 01101100 01010100 01010100 01000100 01000100 00000000 " + // 109 m
                "00000000 00000000 01111000 01000100 01000100 01000100 01000100 00000000 " + // 110 n
                "00000000 00000000 00111000 01000100 01000100 01000100 00111000 00000000 " + // 111 o
                "00000000 01111000 01000100 01000100 01111000 01000000 01000000 00000000 " + // 112 p
                "00000000 00111100 01000100 01000100 00111100 00000100 00000100 00000000 " + // 113 q
                "00000000 00000000 01011000 01100100 01000000 01000000 01000000 00000000 " + // 114 r
                "00000000 00000000 00111100 01000000 00111000 00000100 01111000 00000000 " + // 115 s
                "00010000 00010000 01111100 00010000 00010000 00010000 00001100 00000000 " + // 116 t
                "00000000 00000000 01000100 01000100 01000100 01000100 00111100 00000000 " + // 117 u
                "00000000 00000000 01000100 01000100 00101000 00101000 00010000 00000000 " + // 118 v
                "00000000 00000000 01000100 01000100 01010100 01010100 00101000 00000000 " + // 119 w
                "00000000 00000000 01000100 00101000 00010000 00101000 01000100 00000000 " + // 120 x
                "00000000 01000100 01000100 00101000 00010000 00100000 01000000 00000000 " + // 121 y
                "00000000 00000000 01111100 00001000 00010000 00100000 01111100 00000000 " + // 122 z
                "00001100 00010000 00010000 01100000 00010000 00010000 00001100 00000000 " + // 123 {
                "00010000 00010000 00010000 00010000 00010000 00010000 00010000 00000000 " + // 124 |
                "01100000 00010000 00010000 00001100 00010000 00010000 01100000 00000000 " + // 125 }
                "00000000 00000000 00110010 01001100 00000000 00000000 00000000 00000000 " + // 126 ~
                "";
            var parts = bits.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length && i < Font8x8.Length; i++)
                Font8x8[i] = Convert.ToByte(parts[i], 2);
        }

        private void EnsureFontData()
        {
            if (_fontDataEmitted) return;
            _fontDataEmitted = true;
            // Store font as object[] of ints → writes as .word entries (avoids null byte issues)
            dataSection["__gfx_font_8x8"] = Font8x8.Select(b => (object)(int)b).ToArray();
        }


        private void EmitGfxPrintChar()
        {
            // Render character glyph using configurable font (registers at 0x6FE8-0x6FEF)
            //   FONT_MODE (0x6FE8): 0=8x8,32bit/row (char-32)*32+row*4; 1=8x16,byte/row char*16+row
            //   FONT_WIDTH (0x6FE9): font width (default 8)
            //   FONT_HEIGHT (0x6FEA): font height (default 8 or 16)
            //   FONT_ADDR (0x6FEC): font base address (32-bit)

            // Save registers R0-R10
            for (int r = 0; r <= 10; r++)
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, r) }));

            // Check bpp: skip if text mode (bpp==2)
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, VGA_BPP_ADDR) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R2") }));
            string gfxSkip = newLabel();
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 2) }));
            instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, gfxSkip) }));

            // Load font config registers: R8=font_addr, R9=font_height, R10=font_mode
            EmitGfxLoadFontAddr(8);    // R8 = font base address
            EmitGfxLoadFontHeight(9);  // R9 = font height (rows per glyph)
            EmitGfxLoadFontMode(10);   // R10 = font mode (0=word/row, 1=byte/row)

            // Load cursor: R1=row (0x6FF4), R2=col (0x6FF8)
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0x6FF4) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R1") })); // R1 = cursor row
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 0x6FF8) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R2") })); // R2 = cursor col

            // y = row*16 + (16-font_height)/2  (center glyph in standard 16px cell), x = col*8
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 16) })); // R1 = row*16
            // R1 = row*16 + (16 - font_height)/2
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 16) }));
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 9) })); // R7 = 16 - height
            instructions.Add(new Instruction(OpCode.DIV, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 2) })); // R7 = offset
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 7) })); // R1 = y_top
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 8) })); // R2 = x_left

            // Load screen width into R5
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.IMMEDIATE, VGA_WIDTH_ADDR) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 5), new Operand(OperandType.MEMORY, "R5") })); // R5 = width

            // Font lookup: check if char in printable range 32-126
            string charSkip = newLabel();
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 32) }));
            instructions.Add(new Instruction(OpCode.JL, new List<Operand> { new Operand(OperandType.LABEL, charSkip) }));
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 127) }));
            instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, charSkip) }));

            // Compute glyph base address based on font mode
            // R0 = char code
            instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 32) })); // R0 = char-32

            // Branch on font mode: mode==0 → word-per-row layout; mode!=0 → byte-per-row layout
            string modeByteLayout = newLabel(), modeDone = newLabel();
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, modeByteLayout) }));

            // === MODE 0: 32-bit word per row ===  offset = (char-32) * height * 4
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 9) })); // *height
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4) })); // *4 bytes/row
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 8) })); // +font_base
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 0) })); // R3 = glyph base
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, modeDone) }));

            // === MODE 1+: byte per row ===  offset = char * height
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, modeByteLayout) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 32) })); // restore char
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 9) })); // *height
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 8) })); // +font_base
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 0) })); // R3 = glyph base
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, modeDone) }));

            // For each row of the glyph (R9 = font_height)
            string fontYLoop = newLabel(), fontYEnd = newLabel();
            AddRI(OpCode.MOVE, 0, 0); // R0 = row counter
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, fontYLoop) }));
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 9) }));
            instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, fontYEnd) }));

            // Load glyph row based on mode
            // Mode 0: R4 = LOAD[glyph_base + row*4]
            // Mode 1: R4 = LOADB[glyph_base + row]
            string rowByte = newLabel(), rowDone = newLabel();
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 10), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, rowByte) }));

            // Mode 0: 32-bit load
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0) }));
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.IMMEDIATE, 4) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 3) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R4") }));
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, rowDone) }));

            // Mode 1: byte load
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, rowByte) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 3) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.REGISTER, 0) }));
            instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, 4), new Operand(OperandType.MEMORY, "R4") }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, rowDone) }));

            // Compute VRAM addr: R6 = 0xA0000 + (y+row)*width + x
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 1) }));
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 0) })); // y+row
            instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 5) })); // *width
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.REGISTER, 2) })); // +x
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 6), new Operand(OperandType.IMMEDIATE, VGA_BASE) })); // R6 = VRAM addr

            // For each of 8 columns: test bit 7..0, write white (15) if set
            for (int gx = 0; gx < 8; gx++)
            {
                int bitMask = 0x80 >> gx;
                string skipPx = newLabel();
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.REGISTER, 4) }));
                instructions.Add(new Instruction(OpCode.AND, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, bitMask) }));
                instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 0) }));
                instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, skipPx) }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 7), new Operand(OperandType.IMMEDIATE, 15) }));
                EmitGfxWritePixel(7, 6);
                string afterSkip = newLabel();
                instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, afterSkip) }));
                instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, skipPx) }));
                EmitGfxSkipPixel(6);
                instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, afterSkip) }));
            }
            // Advance row counter and loop
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1) }));
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, fontYLoop) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, fontYEnd) }));

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, charSkip) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, gfxSkip) }));

            // Restore registers R10..R0
            for (int r = 10; r >= 0; r--)
                instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, r) }));
        }
    }
}
