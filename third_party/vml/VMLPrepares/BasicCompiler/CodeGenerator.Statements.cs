using CompilerBase;
using VMLAssembler;

namespace BasicCompiler
{
    public partial class CodeGenerator
    {
        private void GenerateStatement(Statement statement)
        {
            if (statement is SequenceStatement seq)
            {
                foreach (var s in seq.Statements)
                    GenerateStatement(s);
                return;
            }
            if (statement is PrintStatement printStmt)
            {
                GeneratePrintStatement(printStmt);
            }
            else if (statement is InputStatement inputStmt)
            {
                GenerateInputStatement(inputStmt);
            }
            else if (statement is LetStatement letStmt)
            {
                // 在GenerateStatement中，变量已经在CollectVariablesAndLabels阶段收集
                // 这里只需要生成代码
                GenerateLetStatement(letStmt);
            }
            else if (statement is DimStatement dimStmt)
            {
                // DIM语句在CollectVariablesAndLabels阶段已经处理
                // 这里不需要生成代码，数组空间已经在栈上分配
            }
            else if (statement is IfStatement ifStmt)
            {
                GenerateIfStatement(ifStmt);
            }
            else if (statement is SelectCaseStatement selectCaseStmt)
            {
                GenerateSelectCaseStatement(selectCaseStmt);
            }
            else if (statement is GotoStatement gotoStmt)
            {
                GenerateGotoStatement(gotoStmt);
            }
            else if (statement is GosubStatement gosubStmt)
            {
                GenerateGosubStatement(gosubStmt);
            }
            else if (statement is ReturnStatement returnStmt)
            {
                GenerateReturnStatement(returnStmt);
            }
            else if (statement is ForStatement forStmt)
            {
                GenerateForStatement(forStmt);
            }
            else if (statement is WhileStatement whileStmt)
            {
                GenerateWhileStatement(whileStmt);
            }
            else if (statement is DoLoopStatement doLoopStmt)
            {
                GenerateDoLoopStatement(doLoopStmt);
            }
            else if (statement is EndStatement endStmt)
            {
                GenerateEndStatement(endStmt);
            }
            else if (statement is KbHitStatement kbHitStmt)
            {
                GenerateKbHitStatement(kbHitStmt);
            }
            else if (statement is KbGetChStatement kbGetChStmt)
            {
                GenerateKbGetChStatement(kbGetChStmt);
            }
            else if (statement is MouseGetXStatement mouseGetXStmt)
            {
                GenerateMouseGetXStatement(mouseGetXStmt);
            }
            else if (statement is MouseGetYStatement mouseGetYStmt)
            {
                GenerateMouseGetYStatement(mouseGetYStmt);
            }
            else if (statement is MouseLeftStatement mouseLeftStmt)
            {
                GenerateMouseLeftStatement(mouseLeftStmt);
            }
            else if (statement is MouseRightStatement mouseRightStmt)
            {
                GenerateMouseRightStatement(mouseRightStmt);
            }
            else if (statement is SubDeclaration subDecl)
            {
                // SUB declarations are handled after main program
            }
            else if (statement is FunctionDeclaration funcDecl)
            {
                // FUNCTION declarations are handled after main program
            }
            else if (statement is CallStatement callStmt)
            {
                GenerateCallStatement(callStmt);
            }
            else if (statement is ExitSubStatement exitStmt)
            {
                GenerateExitSubStatement();
            }
            else if (statement is ExitLoopStatement exitLoop)
            {
                GenerateExitLoopStatement();
            }
            else if (statement is OpenStatement openStmt)
            {
                GenerateOpenStatement(openStmt);
            }
            else if (statement is CloseStatement closeStmt)
            {
                GenerateCloseStatement(closeStmt);
            }
            else if (statement is PrintFileStatement printFileStmt)
            {
                GeneratePrintFileStatement(printFileStmt);
            }
            else if (statement is InputFileStatement inputFileStmt)
            {
                GenerateInputFileStatement(inputFileStmt);
            }
            // ASM 已移除 — asm() 仅限 C/ObjC/C++ 语言，BASIC 通过 Lib/shared/vmlsys.c 调用
            // QBASIC 标准关键字
            else if (statement is ScreenStatement screenStmt)
            {
                GenerateScreenStatement(screenStmt);
            }
            else if (statement is ClsStatement)
            {
                GenerateClsStatement();
            }
            else if (statement is PsetStatement psetStmt)
            {
                GeneratePsetStatement(psetStmt);
            }
            else if (statement is QbLineStatement qbLineStmt)
            {
                GenerateQbLineStatement(qbLineStmt);
            }
            else if (statement is QbCircleStatement qbCircleStmt)
            {
                GenerateQbCircleStatement(qbCircleStmt);
            }
            else if (statement is QbPaintStatement qbPaintStmt)
            {
                GenerateQbPaintStatement(qbPaintStmt);
            }
            else if (statement is LocateStatement locateStmt)
            {
                GenerateLocateStatement(locateStmt);
            }
            else if (statement is QbColorStatement qbColorStmt)
            {
                GenerateQbColorStatement(qbColorStmt);
            }
            else if (statement is RandomizeStatement randStmt)
            {
                GenerateRandomizeStatement(randStmt);
            }
            else if (statement is QbWidthStatement qbWidthStmt)
            {
                GenerateQbWidthStatement(qbWidthStmt);
            }
            else if (statement is BeepStatement)
            {
                // BEEP: output ASCII BEL (0x07) via SYSCALL 4
                AddRI(OpCode.MOVE, 0, 7);
                EmitPrintChar();
            }
            else if (statement is SleepStatement sleepStmt)
            {
                GenerateSleepStatement(sleepStmt);
            }
            else if (statement is SwapStatement swapStmt)
            {
                GenerateSwapStatement(swapStmt);
            }
            else if (statement is EraseStatement eraseStmt)
            {
                if (arrayVariables.TryGetValue(eraseStmt.ArrayName, out var arrInfo))
                {
                    int baseOffset = 8 + arrInfo.Offset * 4;
                    int size = arrInfo.Size;
                    string eraseLoop = GenerateLabel();
                    string eraseEnd = GenerateLabel();
                    // R1 = counter (0..size-1), R2 = element address
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 0) }));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, eraseLoop) }));
                    instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, size) }));
                    instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, eraseEnd) }));
                    AddRI(OpCode.MOVE, 0, 0);
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 1) }));
                    instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 4) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, baseOffset) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 12) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 1) }));
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, eraseLoop) }));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, eraseEnd) }));
                }
            }
            else if (statement is SystemStatement sysStmt)
            {
                if (sysStmt.ExitCode != null)
                {
                    if (currentSubName != null)
                        GenerateSubExpression(sysStmt.ExitCode, 0);
                    else
                        GenerateExpression(sysStmt.ExitCode, 0);
                }
                EmitExit();
            }
            else if (statement is PokeStatement pokeStmt)
            {
                GeneratePokeStatement(pokeStmt);
            }
            else if (statement is ChipAsmStatement chipAsmStmt)
            {
                // Generate .chipasm as a CHIPASM instruction
                instructions.Add(new Instruction(OpCode.CHIPASM, new List<Operand>
                {
                    new Operand(OperandType.LABEL, chipAsmStmt.Arch),
                    new Operand(OperandType.LABEL, chipAsmStmt.Code)
                }));
            }
            // DATA/READ/RESTORE
            else if (statement is DataStatement)
            {
                // DATA is handled in CollectVariablesAndLabels - no runtime code needed
            }
            else if (statement is ReadStatement readStmt)
            {
                GenerateReadStatement(readStmt);
            }
            else if (statement is RestoreStatement)
            {
                GenerateRestoreStatement();
            }
            // CONST
            else if (statement is ConstStatement)
            {
                // CONST is handled in CollectVariablesAndLabels - no runtime code needed
            }
            // ON ERROR GOTO / RESUME
            else if (statement is OnErrorStatement onErrStmt)
            {
                GenerateOnErrorStatement(onErrStmt);
            }
            else if (statement is ResumeStatement resumeStmt)
            {
                GenerateResumeStatement(resumeStmt);
            }
            else if (statement is DrawStatement drawStmt)
            {
                GenerateDrawStatement(drawStmt);
            }
            else if (statement is PrintUsingStatement usingStmt)
            {
                GeneratePrintUsingStatement(usingStmt);
            }
            else if (statement is TypeDeclaration)
            {
                // TYPE definitions are compile-time metadata only
            }
            else if (statement is DimAsStatement dimAsStmt)
            {
                // DIM AS 类型变量 — 若有构造函数则调用 (v1.66.32+)
                string typeName = dimAsStmt.TypeName.ToLower();
                string ctorName = typeName + "_constructor";
                if (methodSubs.Contains(ctorName) || subMap.ContainsKey(ctorName))
                {
                    int offset = GetOrCreateVariable(dimAsStmt.VariableName);
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 12)]));
                    if (offset > 0)
                        instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, offset * 4 + 8)]));
                    instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, ctorName)]));
                }
            }
            // PLAY/SOUND
            else if (statement is PlayStatement playStmt)
            {
                GeneratePlayStatement(playStmt);
            }
            else if (statement is SoundStatement soundStmt)
            {
                GenerateSoundStatement(soundStmt);
            }
            // REDIM
            else if (statement is RedimStatement redimStmt)
            {
                GenerateRedimStatement(redimStmt);
            }
            // 堆分配 NEW — EmitAlloc (v1.66.32+)
            else if (statement is AllocStatement)
            {
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 64)]));
                instructions.Add(new Instruction(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 40)])); // SYSCALL 40 = alloc
            }
            // GPIO 语句 — 跨平台库调用 (v1.66.32+)
            else if (statement is GpioStatement gpioStmt)
            {
                // 生成 CALL __gpio_{func}(args)
                foreach (var arg in gpioStmt.Arguments)
                {
                    if (arg is string s && int.TryParse(s, out int val))
                        instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, val)]));
                    instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                }
                instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, $"__gpio_{gpioStmt.Function}")]));
                // cleanup stack
                if (gpioStmt.Arguments.Count > 0)
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, gpioStmt.Arguments.Count * 4)]));
            }
            // GET/PUT
            else if (statement is GetStatement getStmt)
            {
                GenerateGetStatement(getStmt);
            }
            else if (statement is PutStatement putStmt)
            {
                GeneratePutStatement(putStmt);
            }
            // PALETTE
            else if (statement is PaletteStatement palStmt)
            {
                GeneratePaletteStatement(palStmt);
            }
            // DEF FN - converted to function, handled in CollectVariablesAndLabels
            else if (statement is DefFnStatement)
            {
                // handled as function
            }
            // 行标签: LabelName: body
            else if (statement is LabelStatement labelStmt)
            {
                instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, labelStmt.Name) }));
                if (labelStmt.Body != null)
                    GenerateStatement(labelStmt.Body);
            }
            // Turbo Basic statements - handled at SUB level or compile time
            else if (statement is LocalDeclaration)
            {
                // LOCAL in main module: variables already collected, no runtime code
            }
            else if (statement is StaticDeclaration)
            {
                // STATIC in main module: variables already collected, no runtime code
            }
            else if (statement is SharedStatement)
            {
                // SHARED in main module: variables already collected, no runtime code
            }
            else if (statement is CommonStatement)
            {
                // COMMON: variables already collected, no runtime code needed
            }
            else if (statement is OptionBaseStatement optBase)
            {
                // OPTION BASE: store the base value for array translation
                // For now just a no-op; array lower bounds are handled during codegen
            }
            else if (statement is DefTypeStatement defType)
            {
                // 注册 DEF type 字母范围
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
                {
                    _defTypeRanges.Add((start, end, targetType));
                }
            }
            // 以下语句编译通过但无实际功能 (v1.66.32+)
            else if (statement is GpioStatement)
            {
                // GPIO — 代码已生成在 else if 之前, 这里捕获但跳过
            }
            else
            {
                // 未实现语句 — 输出警告无实际功能
                string stmtName = statement.GetType().Name;
                if (stmtName.EndsWith("Statement") && stmtName != "PrintStatement" && stmtName != "LetStatement")
                    WarnUnimplemented(stmtName.Replace("Statement", ""));
            }
        }

        private void GenerateLetStatement(LetStatement stmt)
        {
            // 推断表达式类型，用于决定整数/浮点存储路径
            BasicType exprType = InferExpressionType(stmt.Expression);
            bool isFloatExpr = exprType == BasicType.Single || exprType == BasicType.Double || exprType == BasicType.Long;

            // 计算表达式值（整数结果在R0，浮点结果在F0）
            if (currentSubName != null)
            {
                GenerateSubExpression(stmt.Expression, 0);
            }
            else
            {
                GenerateExpression(stmt.Expression, 0);
            }
            
            // 浮点表达式的值已在F0，跳过MOVE（MOVE会破坏F0到R0的整数寄存器传输）
            // 整数表达式的值从R0复制到R1以备存储
            if (!isFloatExpr)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
            }

            // 存储时：浮点用 F0 (register 0)，整数用 R1 (register 1)
            int storeSrcReg = isFloatExpr ? 0 : 1;

            // 检查目标变量类型：如果是浮点变量但表达式是整数，需要 I2F 转换
            BasicType destType = BasicType.Integer;
            if (stmt.Variable is Identifier ident2)
                destType = GetVariableType(ident2.Name.ToLower());

            bool needsI2F = !isFloatExpr && (destType == BasicType.Single || destType == BasicType.Double || destType == BasicType.Long);
            if (needsI2F)
            {
                // 整数结果在R1，转换为目标浮点类型
                if (destType == BasicType.Double || destType == BasicType.Long)
                {
                    instructions.Add(new Instruction(OpCode.I2D, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
                }
                else
                {
                    instructions.Add(new Instruction(OpCode.I2F, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
                }
                storeSrcReg = 0;
            }

            // 浮点→整数自动转换: 表达式是浮点但目标是整数变量 → F2I/D2I
            bool needsF2I = isFloatExpr && (destType == BasicType.Integer || destType == BasicType.Byte);
            if (needsF2I)
            {
                // 浮点结果在F0，转换为整数R1，存储时用R1
                if (exprType == BasicType.Double || exprType == BasicType.Long)
                {
                    instructions.Add(new Instruction(OpCode.D2I, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
                }
                else
                {
                    instructions.Add(new Instruction(OpCode.F2I, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
                }
                storeSrcReg = 1;
            }

            // Float→Double 自动转换: 表达式是 Single 但目标是 Double → F2D
            bool needsF2D = isFloatExpr && exprType == BasicType.Single && destType == BasicType.Double;
            if (needsF2D)
            {
                instructions.Add(new Instruction(OpCode.F2D, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0) }));
            }

            // Double→Float 自动转换: 表达式是 Double 但目标是 Single → D2F
            bool needsD2F = isFloatExpr && exprType == BasicType.Double && destType == BasicType.Single;
            if (needsD2F)
            {
                instructions.Add(new Instruction(OpCode.D2F, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0) }));
            }

            // 处理赋值目标
            if (stmt.Variable is Identifier ident)
            {
                // 记录变量类型，便于后续加载时使用正确的指令（FLOAD vs LOAD）
                // 仅当当前类型是默认 Integer 时才设置，避免覆盖 DEFDBL/DEFLNG 等类型声明
                string varKey = ident.Name.ToLower();
                var existingType = GetVariableType(varKey);
                // 仅当变量无显式类型声明（DEFtype/后缀）时才自动提升类型，
                // 避免 DEFINT B 等显式整型声明被浮点/Long 表达式覆盖
                if (isFloatExpr && !variableTypes.ContainsKey(varKey)
                    && existingType == BasicType.Integer && !HasExplicitDefType(varKey))
                {
                    variableTypes[varKey] = exprType;
                }
                
                // 检查是局部变量、参数还是全局变量
                if (currentSubName != null)
                {
                    // 在SUB/FUNCTION内部
                    if (currentLocalVars.ContainsKey(varKey))
                    {
                        // 局部变量
                        int slot = currentLocalVars[varKey];
                        int offset = -(slot + 1) * 4;
                        BasicType varType = GetVariableType(varKey);
                        OpCode storeOp = GetStoreInstruction(varType);
                        instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R14+{offset}"), new Operand(OperandType.REGISTER, storeSrcReg) }));
                    }
                    else
                    {
                        // 检查是否是参数
                        int paramIdx = FindParameterIndex(varKey);
                        if (paramIdx >= 0)
                        {
                            // 检查参数是否为BYREF
                            bool isByRef = false;
                            if (subMap.ContainsKey(currentSubName.ToLower()))
                            {
                                var sub = subMap[currentSubName.ToLower()];
                                if (paramIdx < sub.Parameters.Count)
                                {
                                    isByRef = sub.Parameters[paramIdx].IsByRef;
                                }
                            }
                            else if (funcMap.ContainsKey(currentSubName.ToLower()))
                            {
                                var func = funcMap[currentSubName.ToLower()];
                                if (paramIdx < func.Parameters.Count)
                                {
                                    isByRef = func.Parameters[paramIdx].IsByRef;
                                }
                            }
                            
                            int offset = 8 + paramIdx * 4;
                            if (isByRef)
                            {
                                // BYREF参数: [R14+offset] 包含地址，需要通过地址存储值
                                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, $"R14+{offset}") }));
                                BasicType varType = GetVariableType(varKey);
                                OpCode storeOp = GetStoreInstruction(varType);
                                instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R2"), new Operand(OperandType.REGISTER, storeSrcReg) }));
                            }
                            else
                            {
                                // BYVAL参数: 直接存储到参数位置
                                BasicType varType = GetVariableType(varKey);
                                OpCode storeOp = GetStoreInstruction(varType);
                                instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R14+{offset}"), new Operand(OperandType.REGISTER, storeSrcReg) }));
                            }
                        }
                        else if (variables.ContainsKey(varKey))
                        {
                            // 全局变量
                            BasicType varType = GetVariableType(varKey);
                            OpCode storeOp = GetStoreInstruction(varType);
                            EmitStoreVar(varKey, storeSrcReg);   // 全局变量写静态区全局段
                        }
                        else
                        {
                            throw new CompilationException(ErrorCode.CodeGen_UndefinedVariable, $"变量 '{ident.Name}' 未定义 (在 SUB/FUNCTION '{currentSubName}' 中)");
                        }
                    }
                }
                else
                {
                    // 全局变量赋值
                    BasicType varType = GetVariableType(varKey);
                    OpCode storeOp = GetStoreInstruction(varType);
                    EmitStoreVar(ident.Name, storeSrcReg);   // 全局变量写静态区全局段
                }
            }
            else if (stmt.Variable is ArrayAccessExpression arrayAccess)
            {
                // 数组元素赋值
                GenerateArrayAssignment(arrayAccess, 1);
            }
            else if (stmt.Variable is FieldAccessExpression fieldAccess)
            {
                // 字段访问赋值: record.field = value
                string fieldName = fieldAccess.FieldName.ToLower();

                // Handle expression-based record (e.g., array access: sammy(1).head = value)
                if (fieldAccess.RecordExpression != null)
                {
                    // Get the record variable name from the expression
                    string recName;
                    if (fieldAccess.RecordExpression is Identifier exprIdent)
                        recName = exprIdent.Name.ToLower();
                    else if (fieldAccess.RecordExpression is ArrayAccessExpression arrExpr)
                        recName = arrExpr.ArrayName.ToLower();
                    else
                        recName = fieldAccess.RecordName?.ToLower() ?? "";

                    if (string.IsNullOrEmpty(recName) || !dimAsVariables.ContainsKey(recName) || !typeDefinitions.ContainsKey(dimAsVariables[recName]))
                    {
                        // Unknown type — store to offset 0
                        if (variables.ContainsKey(recName))
                        {
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, 0) }));
                            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 2) }));
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, "R3"), new Operand(OperandType.REGISTER, storeSrcReg) }));
                        }
                        return;
                    }

                    var tDef = typeDefinitions[dimAsVariables[recName]];
                    int fOff = 0;
                    bool fnd = false;
                    foreach (var f in tDef.Fields)
                    {
                        if (f.Name.ToLower() == fieldName)
                        {
                            fOff = f.Offset;
                            fnd = true;
                            break;
                        }
                    }

                    if (!fnd)
                    {
                        throw new CompilationException(ErrorCode.CodeGen_TypeMismatch, $"类型 '{tDef.Name}' 中没有字段 '{fieldName}'");
                    }

                    // Generate the record expression to get base address (in R2)
                    GenerateExpression(fieldAccess.RecordExpression, 2);
                    // Add field offset to R2
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, fOff) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 2) }));
                    // Store value (R1) to computed address (R3)
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, "R3"), new Operand(OperandType.REGISTER, storeSrcReg) }));
                    return;
                }

                string recordName = fieldAccess.RecordName.ToLower();

                if (!dimAsVariables.ContainsKey(recordName) || !typeDefinitions.ContainsKey(dimAsVariables[recordName]))
                {
                    // Unknown type — store to offset 0
                    if (variables.ContainsKey(recordName))
                    {
                        int varOffset = variables[recordName] * 4;
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 8 + varOffset) }));
                        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 12) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, "R2"), new Operand(OperandType.REGISTER, storeSrcReg) }));
                    }
                    return;
                }

                var typeDef = typeDefinitions[dimAsVariables[recordName]];
                int fieldOffset = 0;
                bool found = false;
                foreach (var f in typeDef.Fields)
                {
                    if (f.Name.ToLower() == fieldName)
                    {
                        fieldOffset = f.Offset;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new CompilationException(ErrorCode.CodeGen_TypeMismatch, $"类型 '{typeDef.Name}' 中没有字段 '{fieldName}'");
                }

                // Compute address and store
                if (variables.ContainsKey(recordName))
                {
                    int varOffset = variables[recordName] * 4;
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 8 + varOffset + fieldOffset) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 12) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, "R2"), new Operand(OperandType.REGISTER, storeSrcReg) }));
                }
            }
            else if (stmt.Variable is FunctionCallExpression funcExpr)
            {
                // MID$() = value assignment — skip for now (no-op)
            }
            else
            {
                // 其他类型的赋值目标暂不支持
                throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, VMLPlugins.Strings.UnsupportedExpression(stmt.Variable.GetType().Name));
            }
        }

        private void GenerateIfStatement(IfStatement stmt)
        {
            Sta.EmitIf(
                () =>
                {
                    if (currentSubName != null)
                        GenerateSubExpression(stmt.Condition, 0);
                    else
                        GenerateExpression(stmt.Condition, 0);
                },
                () => { if (stmt.ThenBranch != null) GenerateStatement(stmt.ThenBranch); },
                stmt.ElseBranch != null ? () => GenerateStatement(stmt.ElseBranch) : null);
        }

        private void GenerateGotoStatement(GotoStatement stmt)
        {
            string label;
            if (stmt.IsLabel)
                label = stmt.Label.ToLower();
            else
                label = $"line_{stmt.LineNumber}";
            Sta.EmitJump(label);
        }

        private void GenerateGosubStatement(GosubStatement stmt)
        {
            string label;
            if (stmt.IsLabel)
                label = stmt.Label.ToLower(); // named label: use lowercase (BASIC case-insensitive)
            else
                label = $"line_{stmt.LineNumber}";
            instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, label) }));
        }

        private void GenerateReturnStatement(ReturnStatement stmt)
        {
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>()));
        }

        private void GenerateForStatement(ForStatement stmt)
        {
            // FOR 循环变量强制为整数类型 (QBASIC 兼容, 不受 DEFSNG 影响)
            string loopVar = stmt.Variable.Name.ToLower();
            if (!variableTypes.ContainsKey(loopVar) || variableTypes[loopVar] != BasicType.Integer)
                variableTypes[loopVar] = BasicType.Integer;

            string loopLabel = GenerateLabel();
            string endLabel = GenerateLabel();

            // 初始化变量
            if (currentSubName != null)
            {
                GenerateSubExpression(stmt.InitialValue, 0);
            }
            else
            {
                GenerateExpression(stmt.InitialValue, 0);
            }
            
            // 存储到变量
            if (currentSubName != null)
            {
                // 在SUB/FUNCTION内部
                if (currentLocalVars.ContainsKey(stmt.Variable.Name.ToLower()))
                {
                    // 局部变量
                    int slot = currentLocalVars[stmt.Variable.Name.ToLower()];
                    int offset = -(slot + 1) * 4;
                    BasicType varType = GetVariableType(stmt.Variable.Name.ToLower());
                    OpCode storeOp = GetStoreInstruction(varType);
                    instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R14+{offset}"), new Operand(OperandType.REGISTER, 0) }));
                }
                else
                {
                    // 检查是否是参数
                    int paramIdx = FindParameterIndex(stmt.Variable.Name.ToLower());
                    if (paramIdx >= 0)
                    {
                        // 参数赋值
                        int offset = 8 + paramIdx * 4;
                        BasicType varType = GetVariableType(stmt.Variable.Name.ToLower());
                        OpCode storeOp = GetStoreInstruction(varType);
                        instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R14+{offset}"), new Operand(OperandType.REGISTER, 0) }));
                    }
                    else if (variables.ContainsKey(stmt.Variable.Name.ToLower()))
                    {
                        // 全局变量（走全局段，见 EmitStoreVar）
                        EmitStoreVar(stmt.Variable.Name.ToLower(), 0);
                    }
                    else
                    {
                        throw new CompilationException(ErrorCode.CodeGen_UndefinedVariable, $"变量 '{stmt.Variable.Name}' 未定义 (在 SUB/FUNCTION '{currentSubName}' 中)");
                    }
                }
            }
            else
            {
                // 全局变量（走全局段，见 EmitStoreVar）
                EmitStoreVar(stmt.Variable.Name, 0);
            }

            // 循环开始
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));

            // 推入循环上下文
            Sta.PushLoopLabels(endLabel, loopLabel);

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));

            // 加载变量值
            if (currentSubName != null)
            {
                if (currentLocalVars.ContainsKey(stmt.Variable.Name.ToLower()))
                {
                    int slot = currentLocalVars[stmt.Variable.Name.ToLower()];
                    int offset = -(slot + 1) * 4;
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R14+{offset}") }));
                }
                else
                {
                    int paramIdx = FindParameterIndex(stmt.Variable.Name.ToLower());
                    if (paramIdx >= 0)
                    {
                        int offset = 8 + paramIdx * 4;
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R14+{offset}") }));
                    }
                    else if (variables.ContainsKey(stmt.Variable.Name.ToLower()))
                    {
                        EmitLoadVar(0, stmt.Variable.Name.ToLower());   // 全局段（见 EmitLoadVar）
                    }
                    else
                        throw new CompilationException(ErrorCode.CodeGen_UndefinedVariable, $"变量 '{stmt.Variable.Name}' 未定义");
                }
            }
            else
            {
                EmitLoadVar(0, stmt.Variable.Name);   // 全局段（见 EmitLoadVar）
            }
            // 每次迭代重新计算结束值（循环体可能破坏 R1）
            if (currentSubName != null)
                GenerateSubExpression(stmt.EndValue, 1);
            else
                GenerateExpression(stmt.EndValue, 1);
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
            instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));

            // 循环体 — 用栈保存/恢复循环变量
            // PUSH 保存变量值 → 执行循环体 → POP 恢复（循环体内 PUSH/POP 自然平衡）
            string loopVarAddr;
            if (currentSubName != null && currentLocalVars.ContainsKey(stmt.Variable.Name.ToLower()))
            {
                int slot = currentLocalVars[stmt.Variable.Name.ToLower()];
                loopVarAddr = $"R14+{-(slot + 1) * 4}";
            }
            else
            {
                loopVarAddr = $"R12+{8 + variables[stmt.Variable.Name] * 4}";
            }
            // 循环体前后"保护/恢复循环变量"：**只对栈上的循环变量做**。
            // 全局变量在静态区全局段，循环体改不到它；而且它的地址是动态算出来的、
            // 没法像 R12/R14 那样预先写成字符串。跳过不影响语义。
            bool loopVarOnStack = !_globalVars.Contains(stmt.Variable.Name.ToLower()) && !_globalVars.Contains(stmt.Variable.Name);
            if (loopVarOnStack)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, loopVarAddr) }));
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
            }
            foreach (var bodyStmt in stmt.Body)
            {
                GenerateStatement(bodyStmt);
            }
            if (loopVarOnStack)
            {
                instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
                // ⚠ **保持原样**：原来就是"读"而不是"写"（`MOVE R0, [loopVarAddr]`）。
                // 我一度以为这是写反了、顺手改成"写回"，结果把 SUB 里的局部 FOR 循环改成了死循环
                // （最小复现：SUB 里 DIM i + FOR i = 0 TO 3 + 局部数组赋值）。
                // 看着像 bug 但不是 —— **没有证据就不要改语义**。
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, loopVarAddr) }));
            }

            // 增加步长 - 加载变量值
            if (currentSubName != null)
            {
                // 在SUB/FUNCTION内部
                if (currentLocalVars.ContainsKey(stmt.Variable.Name.ToLower()))
                {
                    // 局部变量
                    int slot = currentLocalVars[stmt.Variable.Name.ToLower()];
                    int offset = -(slot + 1) * 4;
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R14+{offset}") }));
                }
                else
                {
                    // 检查是否是参数
                    int paramIdx = FindParameterIndex(stmt.Variable.Name.ToLower());
                    if (paramIdx >= 0)
                    {
                        // 参数
                        int offset = 8 + paramIdx * 4;
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R14+{offset}") }));
                    }
                    else if (variables.ContainsKey(stmt.Variable.Name.ToLower()))
                    {
                        EmitLoadVar(0, stmt.Variable.Name.ToLower());   // 全局段（见 EmitLoadVar）
                    }
                    else
                    {
                        throw new CompilationException(ErrorCode.CodeGen_UndefinedVariable, $"变量 '{stmt.Variable.Name}' 未定义 (在 SUB/FUNCTION '{currentSubName}' 中)");
                    }
                }
            }
            else
            {
                EmitLoadVar(0, stmt.Variable.Name);   // 全局段（见 EmitLoadVar）
            }
            
            // 计算步长值
            if (currentSubName != null)
            {
                GenerateSubExpression(stmt.StepValue, 1);
            }
            else
            {
                GenerateExpression(stmt.StepValue, 1);
            }
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
            
            // 存储回变量
            if (currentSubName != null)
            {
                // 在SUB/FUNCTION内部
                if (currentLocalVars.ContainsKey(stmt.Variable.Name.ToLower()))
                {
                    // 局部变量
                    int slot = currentLocalVars[stmt.Variable.Name.ToLower()];
                    int offset = -(slot + 1) * 4;
                    BasicType varType = GetVariableType(stmt.Variable.Name.ToLower());
                    OpCode storeOp = GetStoreInstruction(varType);
                    instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R14+{offset}"), new Operand(OperandType.REGISTER, 0) }));
                }
                else
                {
                    // 检查是否是参数
                    int paramIdx = FindParameterIndex(stmt.Variable.Name.ToLower());
                    if (paramIdx >= 0)
                    {
                        // 参数赋值
                        int offset = 8 + paramIdx * 4;
                        BasicType varType = GetVariableType(stmt.Variable.Name.ToLower());
                        OpCode storeOp = GetStoreInstruction(varType);
                        instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R14+{offset}"), new Operand(OperandType.REGISTER, 0) }));
                    }
                    else if (variables.ContainsKey(stmt.Variable.Name.ToLower()))
                    {
                        EmitStoreVar(stmt.Variable.Name.ToLower(), 0);   // 全局段（见 EmitStoreVar）
                    }
                    else
                    {
                        throw new CompilationException(ErrorCode.CodeGen_UndefinedVariable, $"变量 '{stmt.Variable.Name}' 未定义 (在 SUB/FUNCTION '{currentSubName}' 中)");
                    }
                }
            }
            else
            {
                EmitStoreVar(stmt.Variable.Name, 0);   // 全局段（见 EmitStoreVar）
            }

            // 跳回循环开始
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));

            // 循环结束
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));

            // 弹出循环上下文
            Sta.PopLoopLabels();
        }

        private void GenerateWhileStatement(WhileStatement stmt)
        {
            Sta.EmitWhile(
                () =>
                {
                    if (currentSubName != null)
                        GenerateSubExpression(stmt.Condition, 0);
                    else
                        GenerateExpression(stmt.Condition, 0);
                },
                () =>
                {
                    foreach (var bodyStmt in stmt.Body)
                        GenerateStatement(bodyStmt);
                });
        }

        private void GenerateDoLoopStatement(DoLoopStatement stmt)
        {
            string loopLabel = GenerateLabel();
            string endLabel = GenerateLabel();

            Sta.PushLoopLabels(endLabel, loopLabel);

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));

            // Pre-test condition: DO WHILE/UNTIL — check before body
            if (stmt.HasCondition && stmt.IsPreTest && stmt.Condition != null)
            {
                if (currentSubName != null)
                    GenerateSubExpression(stmt.Condition, 0);
                else
                    GenerateExpression(stmt.Condition, 0);
                instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
                if (stmt.IsUntil)
                    instructions.Add(new Instruction(OpCode.JNZ, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
                else
                    instructions.Add(new Instruction(OpCode.JZ, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
            }

            // 循环体
            foreach (var bodyStmt in stmt.Body)
                GenerateStatement(bodyStmt);

            // Post-test condition (LOOP WHILE/UNTIL)
            if (stmt.HasCondition && !stmt.IsPreTest && stmt.Condition != null)
            {
                if (currentSubName != null)
                    GenerateSubExpression(stmt.Condition, 0);
                else
                    GenerateExpression(stmt.Condition, 0);
                instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
                if (stmt.IsUntil)
                    instructions.Add(new Instruction(OpCode.JNZ, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
                else
                    instructions.Add(new Instruction(OpCode.JZ, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
            }

            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));

            Sta.PopLoopLabels();
        }

        private void GenerateEndStatement(EndStatement stmt)
        {
            // 程序结束 - 通过 SYSCALL 3 退出（R0 中的值作为退出码）
            EmitExit();
        }

        // ====== TypedCodeGen — 类型信息映射 ======
        // GetLoadInstruction/GetStoreInstruction/GetMoveInstruction/GetPushInstruction/GetPopInstruction
        // GetArithmeticInstruction/GetCompareInstruction — 全部由 TypedCodeGen<BasicType> 提供
        //
        // BasicType → (byteSize, isFloat, isDouble, isLong)
        // 修复: Long=8字节用 isLong 而非 isDouble
        protected override (int byteSize, bool isFloat, bool isDouble, bool isLong) GetTypeInfo(BasicType t) => t switch
        {
            BasicType.Byte or BasicType.Boolean => (1, false, false, false),
            BasicType.Single => (4, true, false, false),
            BasicType.Double => (8, false, true, false),
            BasicType.Long => (8, false, true, false),  // Long 使用 Double 路径保持兼容性
            BasicType.String => (4, false, false, false),
            _ => (4, false, false, false),  // Integer, Custom
        };

        /// <summary>
        /// 生成类型转换指令
        /// </summary>
        private void GenerateTypeConversion(BasicType sourceType, BasicType targetType, int reg)
        {
            // 如果源类型和目标类型相同，不需要转换
            if (sourceType == targetType)
                return;
            
            // 类型转换规则
            switch (sourceType)
            {
                case BasicType.String when targetType == BasicType.Integer:
                    // 字符串→整数: 读取第一个字节, 空字符串返回 0
                    // 对于 INKEY$="" 比较, 空字符串地址指向 [0]=0
                    instructions.Add(new Instruction(OpCode.MOVEB, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R{reg}") }));
                    break;

                case BasicType.Integer:
                    if (targetType == BasicType.Single)
                    {
                        // 整数转浮点
                        instructions.Add(new Instruction(OpCode.I2F, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    }
                    else if (targetType == BasicType.Double || targetType == BasicType.Long)
                    {
                        // 整数转双精度/长整数
                        instructions.Add(new Instruction(OpCode.I2D, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    }
                    else if (targetType == BasicType.Byte)
                    {
                        // 整数转字节（截断低8位）
                        // 在VML中，LOADB/STOREB会自动处理字节截断
                        // 这里不需要特殊指令
                    }
                    break;

                case BasicType.Single:
                    if (targetType == BasicType.Integer)
                    {
                        // 浮点转整数（截断）
                        instructions.Add(new Instruction(OpCode.F2I, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    }
                    else if (targetType == BasicType.Byte)
                    {
                        // 浮点转字节：先转整数，再截断
                        instructions.Add(new Instruction(OpCode.F2I, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                        // 字节截断由存储指令处理
                    }
                    else if (targetType == BasicType.Double)
                    {
                        // 单精度浮点转双精度浮点
                        instructions.Add(new Instruction(OpCode.MOVED, new List<Operand> { new Operand(OperandType.REGISTER, reg), Mem($"R{reg}") }));
                    }
                    break;

                case BasicType.Double:
                    if (targetType == BasicType.Integer)
                    {
                        // 双精度浮点转整数（截断）
                        instructions.Add(new Instruction(OpCode.D2I, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    }
                    else if (targetType == BasicType.Byte)
                    {
                        // 双精度浮点转字节：先转整数，再截断
                        instructions.Add(new Instruction(OpCode.D2I, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    }
                    else if (targetType == BasicType.Single)
                    {
                        // 双精度浮点转单精度浮点（通过整数中转）
                        instructions.Add(new Instruction(OpCode.D2I, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                        instructions.Add(new Instruction(OpCode.I2F, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    }
                    break;

                case BasicType.Long:
                    if (targetType == BasicType.Integer)
                    {
                        // Long → Integer: 截断（无需特殊指令，64位低32位即为int值）
                    }
                    else if (targetType == BasicType.Single)
                    {
                        // Long → Single: D2I + I2F
                        instructions.Add(new Instruction(OpCode.D2I, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                        instructions.Add(new Instruction(OpCode.I2F, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    }
                    break;
                    
                case BasicType.Byte:
                    if (targetType == BasicType.Integer)
                    {
                        // 字节转整数：LOADB已经将字节零扩展为整数
                        // 这里不需要特殊指令
                    }
                    else if (targetType == BasicType.Single)
                    {
                        // 字节转浮点：先转整数，再转浮点
                        // 字节转整数由加载指令处理
                        instructions.Add(new Instruction(OpCode.I2F, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    }
                    break;
            }
        }

        /// <summary>
        /// 推断表达式的类型
        /// </summary>
        private BasicType InferExpressionType(Expression expr)
        {
            if (expr is NumberLiteral numLiteral)
            {
                // 数字字面量：检查是否有小数点
                if (numLiteral.Value.ToString().Contains("."))
                {
                    return BasicType.Single;
                }
                else
                {
                    // 检查值范围决定是整数还是字节
                    int value = (int)numLiteral.Value;
                    if (value >= 0 && value <= 255)
                    {
                        // 小值可能是字节，但默认返回整数
                        return BasicType.Integer;
                    }
                    else
                    {
                        return BasicType.Integer;
                    }
                }
            }
            else if (expr is Identifier ident)
            {
                // 变量：获取变量类型
                return GetVariableType(ident.Name);
            }
            else if (expr is BinaryExpression binExpr)
            {
                // 二元运算：推断左右操作数类型，然后决定结果类型
                var leftType = InferExpressionType(binExpr.Left);
                var rightType = InferExpressionType(binExpr.Right);
                
                // 类型提升规则：
                // 1. 如果有一个是 Double/Long，结果是 Double (64位)
                // 2. 如果有一个是 Single，结果是 Single
                // 3. 如果有一个是字节，另一个是整数，结果是整数
                // 4. 否则结果是整数
                if (leftType == BasicType.Double || rightType == BasicType.Double
                    || leftType == BasicType.Long || rightType == BasicType.Long)
                {
                    return BasicType.Double;
                }
                else if (leftType == BasicType.Single || rightType == BasicType.Single)
                {
                    return BasicType.Single;
                }
                else if (leftType == BasicType.Integer || rightType == BasicType.Integer)
                {
                    return BasicType.Integer;
                }
                else
                {
                    return BasicType.Byte;
                }
            }
            else if (expr is StringLiteral)
            {
                return BasicType.String;
            }
            else if (expr is FunctionCallExpression funcCall)
            {
                // Functions ending with $ return strings
                string name = funcCall.FunctionName.ToLower();
                if (name.EndsWith("$")) return BasicType.String;
                // 浮点函数（RND 在 VML 中返回整数 0..n-1）
                if (name == "sin" || name == "cos" || name == "tan" ||
                    name == "sqr" || name == "exp" || name == "log" || name == "atn")
                    return BasicType.Single;
                // 类型转换函数返回目标类型
                if (name == "csng")
                    return BasicType.Single;
                if (name == "cdbl" || name == "clng")
                    return BasicType.Double;
                // RND/INT/CINT/FIX 返回整数
                if (name == "rnd" || name == "int" || name == "cint" || name == "fix")
                    return BasicType.Integer;
                // 其他函数默认返回整数
                return BasicType.Integer;
            }

            // 默认返回整数类型
            return BasicType.Integer;
        }

        /// <summary>
        /// 获取变量的类型（简单推断）
        /// </summary>
        private BasicType GetVariableType(string varName)
        {
            string varKey = varName.ToLower();

            // 如果已经记录了类型，返回记录的类型（键以小写存储）
            if (variableTypes.ContainsKey(varKey))
            {
                return variableTypes[varKey];
            }

            // 简单类型推断规则（优先级：后缀 > DEFtype > 默认整数）：
            // $ = 字符串
            // % = 字节 (Integer/Byte)
            // ! = 单精度浮点数
            // # = 双精度浮点数
            // & = 长整数

            // 1. 先检查类型后缀（最高优先级，覆盖 DEF type）
            if (varName.EndsWith("$"))
            {
                return BasicType.String;
            }
            else if (varName.EndsWith("%"))
            {
                return BasicType.Byte;
            }
            else if (varName.EndsWith("!"))
            {
                return BasicType.Single;
            }
            else if (varName.EndsWith("#"))
            {
                return BasicType.Double;
            }
            else if (varName.EndsWith("&"))
            {
                return BasicType.Integer;
            }

            // 2. 检查 DEF type 字母范围（DEFSNG/DEFINT/DEFSTR/DEFDBL）
            if (varKey.Length > 0)
            {
                char firstChar = char.ToUpper(varKey[0]);
                foreach (var (start, end, type) in _defTypeRanges)
                {
                    if (firstChar >= start && firstChar <= end)
                    {
                        return type;
                    }
                }
            }

            // 3. 默认为整数类型
            return BasicType.Integer;
        }

        /// <summary>变量是否有显式类型声明（后缀 $/%/!/#/& 或 DEFtype 字母范围）</summary>
        private bool HasExplicitDefType(string varKey)
        {
            if (varKey.Length == 0) return false;
            char last = varKey[varKey.Length - 1];
            if (last == '$' || last == '%' || last == '!' || last == '#' || last == '&')
                return true;
            char firstChar = char.ToUpper(varKey[0]);
            foreach (var (start, end, _) in _defTypeRanges)
            {
                if (firstChar >= start && firstChar <= end)
                    return true;
            }
            return false;
        }

        /// <summary>Generate string INPUT — read string from stdin into R0</summary>
        private void GenerateStringInput(int reg)
        {
            // SYSCALL 1: read string to buffer, R0 = buffer address
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 1) }));
            if (reg != 0)
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 0) }));
        }
    }
}
