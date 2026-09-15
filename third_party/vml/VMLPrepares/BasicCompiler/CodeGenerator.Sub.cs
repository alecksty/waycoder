using CompilerBase;
using VMLAssembler;

namespace BasicCompiler
{
    public partial class CodeGenerator
    {
        private void GenerateSubDeclaration(SubDeclaration subDecl)
        {
            // native SUB: 管线器会在链接时提供标签，跳过函数体生成
            if (subDecl.IsNative)
                return;

            Regs.Reset();
            string subLabel = "sub_" + subDecl.Name.ToLower();
            currentSubName = subDecl.Name;
            currentLocalVars = new Dictionary<string, int>();
            currentLocalVarCount = 0;
            currentParamCount = subDecl.Parameters.Count;

            // Collect local variables from body
            foreach (var stmt in subDecl.Body)
            {
                CollectLocalVariables(stmt);
            }

            // VML function-level comments
            Emit(OpCode.NOP, new List<Operand>(), "; --------------------------------------------");
            // Build source declaration
            var sourceDecl = "SUB " + subDecl.Name + "(";
            for (var i = 0; i < subDecl.Parameters.Count; i++)
            {
                var typeStr = subDecl.Parameters[i].IsString ? "STRING" : "INTEGER";
                sourceDecl += subDecl.Parameters[i].Name + " AS " + typeStr;
                if (i < subDecl.Parameters.Count - 1) sourceDecl += ", ";
            }
            sourceDecl += ")";
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; source   : {sourceDecl}"));
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; procedure: {subDecl.Name}"));
            foreach (var param in subDecl.Parameters)
            {
                var typeStr = param.IsString ? "STRING" : "INTEGER";
                instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; param    : {param.Name} AS {typeStr}"));
            }
            instructions.Add(new Instruction(OpCode.NOP, [], 0, "; return   : (none)"));
            Emit(OpCode.NOP, new List<Operand>(), "; --------------------------------------------");

            // SUB entry point
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, subLabel) }));

            // Stack frame: ENTER localSize
            int subLocalSize = currentLocalVarCount * 4;
            instructions.Add(new Instruction(OpCode.ENTER, new List<Operand> { new Operand(OperandType.IMMEDIATE, subLocalSize) }));

            // Initialize local variables to 0 (negative offsets from BP)
            foreach (var kv in currentLocalVars)
            {
                int offset = -(kv.Value + 1) * 4;
                AddRI(OpCode.MOVE, 0, 0);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12-{-offset}") }));
            }

            // Generate body
            foreach (var stmt in subDecl.Body)
            {
                CurrentSourceLine = stmt.Line;
                GenerateSubStatement(stmt);
            }

            // Epilogue: LEAVE; RET
            instructions.Add(new Instruction(OpCode.LEAVE, new List<Operand>()));
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>()));

            currentSubName = null;
        }

        private void GenerateFunctionDeclaration(FunctionDeclaration funcDecl)
        {
            // native FUNCTION: 管线器会在链接时提供标签，跳过函数体生成
            if (funcDecl.IsNative)
                return;

            Regs.Reset();
            string funcLabel = "func_" + funcDecl.Name.ToLower();
            currentSubName = funcDecl.Name;
            currentLocalVars = new Dictionary<string, int>();
            currentLocalVarCount = 0;
            currentParamCount = funcDecl.Parameters.Count;

            // The function name is a special local variable for the return value
            currentLocalVars[funcDecl.Name.ToLower()] = currentLocalVarCount++;

            // Collect local variables from body
            foreach (var stmt in funcDecl.Body)
            {
                CollectLocalVariables(stmt);
            }

            // VML function-level comments
            Emit(OpCode.NOP, new List<Operand>(), "; --------------------------------------------");
            // Build source declaration
            var sourceDecl = "FUNCTION " + funcDecl.Name + "(";
            for (var i = 0; i < funcDecl.Parameters.Count; i++)
            {
                var typeStr = funcDecl.Parameters[i].IsString ? "STRING" : "INTEGER";
                sourceDecl += funcDecl.Parameters[i].Name + " AS " + typeStr;
                if (i < funcDecl.Parameters.Count - 1) sourceDecl += ", ";
            }
            sourceDecl += ")";
            if (funcDecl.IsStringFunction)
                sourceDecl += " AS STRING";
            else
                sourceDecl += " AS INTEGER";
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; source   : {sourceDecl}"));
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; function : {funcDecl.Name}"));
            foreach (var param in funcDecl.Parameters)
            {
                var typeStr = param.IsString ? "STRING" : "INTEGER";
                instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; param    : {param.Name} AS {typeStr}"));
            }
            var retType = funcDecl.IsStringFunction ? "STRING" : "INTEGER";
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; return   : {retType}"));
            Emit(OpCode.NOP, new List<Operand>(), "; --------------------------------------------");

            // FUNCTION entry point
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, funcLabel) }));

            // Stack frame: ENTER localSize
            int funcLocalSize = currentLocalVarCount * 4;
            instructions.Add(new Instruction(OpCode.ENTER, new List<Operand> { new Operand(OperandType.IMMEDIATE, funcLocalSize) }));

            // Initialize local variables to 0 (use negative offsets from BP)
            foreach (var kv in currentLocalVars)
            {
                int offset = -(kv.Value + 1) * 4; // Local vars below BP
                AddRI(OpCode.MOVE, 0, 0);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12-{-offset}") }));
            }

            // Generate body
            foreach (var stmt in funcDecl.Body)
            {
                CurrentSourceLine = stmt.Line;
                GenerateSubStatement(stmt);
            }

            // Load return value into R0 (the function name variable)
            int retValOffset = -(currentLocalVars[funcDecl.Name.ToLower()] + 1) * 4;
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12-{-retValOffset}") }));

            // Epilogue: LEAVE; RET
            instructions.Add(new Instruction(OpCode.LEAVE, new List<Operand>()));
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>()));

            currentSubName = null;
        }

        private bool IsStaticVariable(string varName)
        {
            if (currentSubName == null) return false;
            string scopedName = $"{currentSubName.ToLower()}__{varName.ToLower()}";
            return variables.ContainsKey(scopedName);
        }

        /// <summary>
        /// 这个名字是不是**模块级变量**（含 `DIM SHARED`）—— 是的话，SUB 内**不许**把它当局部变量。
        ///
        /// ⚠ 原来没有这一条：CollectLocalVariables 会把 SUB 里出现过的赋值目标统统登记成
        /// 局部变量，而局部变量**未初始化就是 0** —— 于是模块级 `DIM BW` / `BW = 10` 在 SUB 里
        /// 读出来是 **0**（实测）。任何"用全局常量排版"的写法（拿格宽算坐标之类）都会因此
        /// 算错、甚至踩运行时除零。
        ///
        /// 模块级变量在 <c>variables</c> 里是**裸名**，STATIC 是 `子程序名__变量名`，
        /// 用键的形状就能分开（见 <see cref="IsStaticVariable"/>）。
        /// </summary>
        private bool IsModuleVariable(string varName)
        {
            if (string.IsNullOrEmpty(varName)) return false;
            string key = varName.ToLower();
            if (currentSubName != null && key == currentSubName.ToLower()) return false;
            return variables.ContainsKey(key);
        }

        private void CollectLocalVariables(Statement stmt)
        {
            if (stmt is SequenceStatement seq)
            {
                foreach (var s in seq.Statements)
                    CollectLocalVariables(s);
                return;
            }
            if (stmt is LetStatement letStmt)
            {
                if (letStmt.Variable is Identifier ident)
                {
                    // Don't collect the function name as a local - it's already added
                    if (ident.Name.ToLower() != currentSubName.ToLower() &&
                        !currentLocalVars.ContainsKey(ident.Name.ToLower()) &&
                        !IsParameter(ident.Name.ToLower()) &&
                        !IsStaticVariable(ident.Name.ToLower()) &&
                        !IsModuleVariable(ident.Name))   // 模块级变量不许被局部遮蔽
                    {
                        currentLocalVars[ident.Name.ToLower()] = currentLocalVarCount++;
                    }
                }
                CollectLocalVariablesFromExpr(letStmt.Expression);
            }
            else if (stmt is PrintStatement printStmt)
            {
                foreach (var expr in printStmt.Expressions)
                    CollectLocalVariablesFromExpr(expr);
            }
            else if (stmt is IfStatement ifStmt)
            {
                CollectLocalVariablesFromExpr(ifStmt.Condition);
                if (ifStmt.ThenBranch != null) CollectLocalVariables(ifStmt.ThenBranch);
                if (ifStmt.ElseBranch != null) CollectLocalVariables(ifStmt.ElseBranch);
            }
            else if (stmt is ForStatement forStmt)
            {
                if (!currentLocalVars.ContainsKey(forStmt.Variable.Name.ToLower()) &&
                    !IsParameter(forStmt.Variable.Name.ToLower()) &&
                    !IsStaticVariable(forStmt.Variable.Name.ToLower()) &&
                    !IsModuleVariable(forStmt.Variable.Name))   // 模块级变量不许被局部遮蔽
                {
                    currentLocalVars[forStmt.Variable.Name.ToLower()] = currentLocalVarCount++;
                }
                CollectLocalVariablesFromExpr(forStmt.InitialValue);
                CollectLocalVariablesFromExpr(forStmt.EndValue);
                CollectLocalVariablesFromExpr(forStmt.StepValue);
                foreach (var bodyStmt in forStmt.Body)
                    CollectLocalVariables(bodyStmt);
            }
            else if (stmt is WhileStatement whileStmt)
            {
                CollectLocalVariablesFromExpr(whileStmt.Condition);
                foreach (var bodyStmt in whileStmt.Body)
                    CollectLocalVariables(bodyStmt);
            }
            else if (stmt is CallStatement callStmt)
            {
                foreach (var arg in callStmt.Arguments)
                    CollectLocalVariablesFromExpr(arg);
            }
            // Turbo Basic: LOCAL declaration -- always add to local vars
            else if (stmt is LocalDeclaration localDecl)
            {
                foreach (var varIdent in localDecl.Variables)
                {
                    string varName = varIdent.Name.ToLower();
                    if (!currentLocalVars.ContainsKey(varName) && !IsParameter(varName))
                    {
                        currentLocalVars[varName] = currentLocalVarCount++;
                    }
                }
            }
            // Turbo Basic: STATIC declaration -- add to global variables with scope prefix
            else if (stmt is StaticDeclaration staticDecl)
            {
                foreach (var varIdent in staticDecl.Variables)
                {
                    string varName = varIdent.Name.ToLower();
                    // Static vars are allocated in global scope with a unique prefix
                    string scopedName = $"{currentSubName.ToLower()}__{varName}";
                    if (!variables.ContainsKey(scopedName))
                    {
                        variables[scopedName] = variableCount++;
                    }
                }
            }
            // Turbo Basic: SHARED declaration -- reference existing globals, do not add to locals
            else if (stmt is SharedStatement sharedStmt)
            {
                foreach (var varIdent in sharedStmt.Variables)
                {
                    string varName = varIdent.Name.ToLower();
                    // SHARED variables must exist in global scope
                    if (!variables.ContainsKey(varName))
                    {
                        variables[varName] = variableCount++;
                    }
                }
            }
            else if (stmt is DimAsStatement dimAs)
            {
                // DIM var AS TypeName inside SUB — register in dimAsVariables
                string varName = dimAs.VariableName.ToLower();
                dimAsVariables[varName] = dimAs.TypeName.ToLower();
                // Also allocate the type's slots as local variables
                if (typeDefinitions.ContainsKey(dimAs.TypeName.ToLower()))
                {
                    int slots = (typeDefinitions[dimAs.TypeName.ToLower()].TotalSize + 3) / 4;
                    for (int s = 0; s < slots; s++)
                    {
                        string slotName = s == 0 ? varName : $"{varName}_slot_{s}";
                        if (!currentLocalVars.ContainsKey(slotName) && !IsParameter(slotName))
                            currentLocalVars[slotName] = currentLocalVarCount++;
                    }
                }
                else if (!currentLocalVars.ContainsKey(varName) && !IsParameter(varName))
                {
                    currentLocalVars[varName] = currentLocalVarCount++;
                }
            }
            else if (stmt is DoLoopStatement doLoop)
            {
                if (doLoop.HasCondition && doLoop.Condition != null)
                    CollectLocalVariablesFromExpr(doLoop.Condition);
                foreach (var bodyStmt in doLoop.Body)
                    CollectLocalVariables(bodyStmt);
            }
            else if (stmt is SelectCaseStatement selectCase)
            {
                CollectLocalVariablesFromExpr(selectCase.TestExpression);
                foreach (var caseBlock in selectCase.CaseBlocks)
                {
                    foreach (var cond in caseBlock.Conditions)
                    {
                        if (cond.Value != null) CollectLocalVariablesFromExpr(cond.Value);
                        if (cond.FromValue != null) CollectLocalVariablesFromExpr(cond.FromValue);
                        if (cond.ToValue != null) CollectLocalVariablesFromExpr(cond.ToValue);
                        if (cond.CompareValue != null) CollectLocalVariablesFromExpr(cond.CompareValue);
                    }
                    foreach (var bodyStmt in caseBlock.Body)
                        CollectLocalVariables(bodyStmt);
                }
                foreach (var elseStmt in selectCase.ElseBlock)
                    CollectLocalVariables(elseStmt);
            }
        }

        private bool IsParameter(string name)
        {
            if (subMap.ContainsKey(currentSubName.ToLower()))
            {
                var sub = subMap[currentSubName.ToLower()];
                foreach (var p in sub.Parameters)
                    if (p.Name.ToLower() == name) return true;
            }
            if (funcMap.ContainsKey(currentSubName.ToLower()))
            {
                var func = funcMap[currentSubName.ToLower()];
                foreach (var p in func.Parameters)
                    if (p.Name.ToLower() == name) return true;
            }
            return false;
        }

        private void CollectLocalVariablesFromExpr(Expression expr)
        {
            if (expr is Identifier ident)
            {
                if (!currentLocalVars.ContainsKey(ident.Name.ToLower()) &&
                    !variables.ContainsKey(ident.Name.ToLower()) &&
                    !IsParameter(ident.Name.ToLower()) &&
                    !IsStaticVariable(ident.Name.ToLower()) &&
                    !IsModuleVariable(ident.Name))   // 模块级变量不许被局部遮蔽
                {
                    currentLocalVars[ident.Name.ToLower()] = currentLocalVarCount++;
                }
            }
            else if (expr is BinaryExpression binary)
            {
                CollectLocalVariablesFromExpr(binary.Left);
                CollectLocalVariablesFromExpr(binary.Right);
            }
            else if (expr is UnaryExpression unary)
            {
                CollectLocalVariablesFromExpr(unary.Expression);
            }
            else if (expr is FunctionCallExpression funcCall)
            {
                foreach (var arg in funcCall.Arguments)
                    CollectLocalVariablesFromExpr(arg);
            }
            else if (expr is ArrayAccessExpression arrayAccess)
            {
                CollectLocalVariablesFromExpr(arrayAccess.Index);
            }
            else if (expr is FieldAccessExpression fieldAccess)
            {
                if (fieldAccess.RecordExpression != null)
                    CollectLocalVariablesFromExpr(fieldAccess.RecordExpression);
            }
        }

        private void GenerateSubStatement(Statement stmt)
        {
            if (stmt is SequenceStatement seq)
            {
                foreach (var s in seq.Statements)
                    GenerateSubStatement(s);
                return;
            }
            if (stmt is PrintStatement printStmt)
            {
                GeneratePrintStatement(printStmt);
            }
            else if (stmt is LetStatement letStmt)
            {
                GenerateSubLetStatement(letStmt);
            }
            else if (stmt is IfStatement ifStmt)
            {
                GenerateSubIfStatement(ifStmt);
            }
            else if (stmt is ForStatement forStmt)
            {
                GenerateSubForStatement(forStmt);
            }
            else if (stmt is WhileStatement whileStmt)
            {
                GenerateSubWhileStatement(whileStmt);
            }
            else if (stmt is DoLoopStatement doLoopStmt)
            {
                GenerateSubDoLoopStatement(doLoopStmt);
            }
            else if (stmt is LocalDeclaration localDecl)
            {
                // LOCAL in SUB: variables already added to currentLocalVars in CollectLocalVariables
                // Initialize to zero
                foreach (var varIdent in localDecl.Variables)
                {
                    string varName = varIdent.Name.ToLower();
                    if (currentLocalVars.ContainsKey(varName))
                    {
                        int slot = currentLocalVars[varName];
                        int offset = -(slot + 1) * 4;
                        AddRI(OpCode.MOVE, 0, 0);
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12-{-offset}") }));
                    }
                }
            }
            else if (stmt is StaticDeclaration)
            {
                // STATIC in SUB: variables already allocated in global scope during CollectVariables
                // No runtime initialization needed (already zero-initialized)
            }
            else if (stmt is SharedStatement)
            {
                // SHARED in SUB: variables reference globals, nothing to generate
            }
            else if (stmt is CallStatement callStmt)
            {
                GenerateCallStatement(callStmt);
            }
            else if (stmt is ExitSubStatement exitStmt)
            {
                GenerateExitSubStatement();
            }
            else if (stmt is ReturnStatement retStmt)
            {
                GenerateExitSubStatement();
            }
            else if (stmt is ExitLoopStatement exitLoop)
            {
                GenerateExitLoopStatement();
            }
            else if (stmt is ScreenStatement screenStmt)
            {
                GenerateScreenStatement(screenStmt);
            }
            else if (stmt is ClsStatement)
            {
                GenerateClsStatement();
            }
            else if (stmt is PsetStatement psetStmt)
            {
                GeneratePsetStatement(psetStmt);
            }
            // ASM 已移除 — asm() 仅限 C/ObjC/C++ 语言，BASIC 通过 Lib/shared/vmlsys.c 调用
            else if (stmt is QbLineStatement qbLineStmt)
            {
                GenerateQbLineStatement(qbLineStmt);
            }
            else if (stmt is QbCircleStatement qbCircleStmt)
            {
                GenerateQbCircleStatement(qbCircleStmt);
            }
            else if (stmt is QbPaintStatement qbPaintStmt)
            {
                GenerateQbPaintStatement(qbPaintStmt);
            }
            else if (stmt is LocateStatement locateStmt)
            {
                GenerateLocateStatement(locateStmt);
            }
            else if (stmt is QbColorStatement qbColorStmt)
            {
                GenerateQbColorStatement(qbColorStmt);
            }
            else if (stmt is RandomizeStatement randStmt)
            {
                GenerateRandomizeStatement(randStmt);
            }
            else if (stmt is QbWidthStatement qbWidthStmt)
            {
                GenerateQbWidthStatement(qbWidthStmt);
            }
            else if (stmt is GosubStatement gosubStmt)
            {
                GenerateSubGosubStatement(gosubStmt);
            }
            else if (stmt is GotoStatement gotoStmt)
            {
                if (gotoStmt.IsLabel)
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, $"{gotoStmt.Label}".ToLower()) }));
                else
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, $"line_{gotoStmt.LineNumber}") }));
            }
            else if (stmt is BeepStatement)
            {
                AddRI(OpCode.MOVE, 0, 7);
                instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 4) }));
            }
            else if (stmt is SleepStatement sleepStmt)
            {
                GenerateSleepStatement(sleepStmt);
            }
            else if (stmt is SwapStatement swapStmt)
            {
                GenerateSwapStatement(swapStmt);
            }
            else if (stmt is SystemStatement)
            {
                EmitExit();
            }
            else if (stmt is PokeStatement pokeStmt)
            {
                GenerateSubPokeStatement(pokeStmt);
            }
            else if (stmt is ChipAsmStatement chipAsmStmt)
            {
                instructions.Add(new Instruction(OpCode.CHIPASM, new List<Operand>
                {
                    new Operand(OperandType.LABEL, chipAsmStmt.Arch),
                    new Operand(OperandType.LABEL, chipAsmStmt.Code)
                }));
            }
            else if (stmt is InputStatement inputStmt)
            {
                GenerateSubInputStatement(inputStmt);
            }
            // DATA/READ/RESTORE
            else if (stmt is DataStatement)
            {
                // handled at compile time
            }
            else if (stmt is ReadStatement readStmt)
            {
                GenerateReadStatement(readStmt);
            }
            else if (stmt is RestoreStatement)
            {
                GenerateRestoreStatement();
            }
            // CONST
            else if (stmt is ConstStatement)
            {
                // handled at compile time
            }
            // ON ERROR GOTO / RESUME
            else if (stmt is OnErrorStatement onErrStmt)
            {
                GenerateOnErrorStatement(onErrStmt);
            }
            else if (stmt is ResumeStatement resumeStmt)
            {
                GenerateResumeStatement(resumeStmt);
            }
            else if (stmt is DrawStatement drawStmt)
            {
                GenerateDrawStatement(drawStmt);
            }
            else if (stmt is PrintUsingStatement usingStmt)
            {
                GeneratePrintUsingStatement(usingStmt);
            }
            else if (stmt is TypeDeclaration)
            {
                // TYPE definitions are compile-time metadata only
            }
            else if (stmt is DimAsStatement)
            {
                // DIM AS allocates space in CollectVariables - no runtime code needed
            }
            // PLAY/SOUND
            else if (stmt is PlayStatement playStmt)
            {
                GeneratePlayStatement(playStmt);
            }
            else if (stmt is SoundStatement soundStmt)
            {
                GenerateSoundStatement(soundStmt);
            }
            // REDIM
            else if (stmt is RedimStatement redimStmt)
            {
                GenerateRedimStatement(redimStmt);
            }
            // DEF FN
            else if (stmt is DefFnStatement)
            {
                // handled as function
            }
            // GET/PUT
            else if (stmt is GetStatement getStmt)
            {
                GenerateGetStatement(getStmt);
            }
            else if (stmt is PutStatement putStmt)
            {
                GeneratePutStatement(putStmt);
            }
            // PALETTE
            else if (stmt is PaletteStatement palStmt)
            {
                GeneratePaletteStatement(palStmt);
            }
            // 行标签
            else if (stmt is LabelStatement labelStmt)
            {
                instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, labelStmt.Name) }));
                if (labelStmt.Body != null)
                    GenerateSubStatement(labelStmt.Body);
            }
        }

        private void GenerateSubInputStatement(InputStatement stmt)
        {
            // Read an integer from input
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 7) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));
            if (stmt.Variables.Count > 0)
            {
                var varIdent = stmt.Variables[0];
                var varName = varIdent.Name;
                if (currentLocalVars.ContainsKey(varName))
                {
                    int slot = currentLocalVars[varName];
                    int offset = -(slot + 1) * 4;
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R12-{-offset}") }));
                }
                else
                {
                    int paramIdx = FindParameterIndex(varName);
                    if (paramIdx >= 0)
                    {
                        int offset = 8 + paramIdx * 4;
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R12+{offset}") }));
                    }
                }
            }
        }

        private void GenerateSubGosubStatement(GosubStatement stmt)
        {
            if (stmt.IsLabel)
            {
                AddRI(OpCode.MOVE, 0, 0);
                instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, $"{stmt.Label}".ToLower()) }));
            }
            else
            {
                AddRI(OpCode.MOVE, 0, 0);
                instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, $"line_{stmt.LineNumber}") }));
            }
        }

        private void GenerateSubLetStatement(LetStatement stmt)
        {
            BasicType exprType = InferExpressionType(stmt.Expression);
            bool isFloat = exprType == BasicType.Single || exprType == BasicType.Double;

            GenerateSubExpression(stmt.Expression, 0);
            if (!isFloat)
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 0) }));

            int srcReg = isFloat ? 0 : 1;
            OpCode storeOp = isFloat ? OpCode.MOVEF : OpCode.MOVE;

            if (stmt.Variable is Identifier ident)
            {
                // Track variable type for float
                string varName = ident.Name.ToLower();
                if (isFloat && !variableTypes.ContainsKey(varName))
                    variableTypes[varName] = exprType;

                // Try local variable first
                if (currentLocalVars.ContainsKey(varName))
                {
                    int slot = currentLocalVars[varName];
                    int offset = -(slot + 1) * 4;
                    instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12-{-offset}"), new Operand(OperandType.REGISTER, srcReg) }));
                }
                else
                {
                    // Check if it's a parameter
                    int paramIdx = FindParameterIndex(ident.Name.ToLower());
                    if (paramIdx >= 0)
                    {
                        // Check if parameter is BYREF
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
                            // BYREF parameter: [R12+offset] contains address, store through address
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, $"R12+{offset}") }));
                            instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R2"), new Operand(OperandType.REGISTER, srcReg) }));
                        }
                        else
                        {
                            // BYVAL parameter: store directly to parameter location
                            instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12+{offset}"), new Operand(OperandType.REGISTER, srcReg) }));
                        }
                    }
                    else if (IsStaticVariable(ident.Name.ToLower()))
                    {
                        // STATIC variable (scoped in globals)
                        string staticName = $"{currentSubName.ToLower()}__{ident.Name.ToLower()}";
                        int varOffset = variables[staticName] * 4;
                        instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12+{8 + varOffset}"), new Operand(OperandType.REGISTER, srcReg) }));
                    }
                    else if (currentClassName != null && classDefinitions.TryGetValue(currentClassName.ToLower(), out var clsDef))
                    {
                        // Phase 2: 方法内字段赋值 — 通过 THIS 指针
                        int fieldOff = -1;
                        foreach (var f in clsDef.Fields)
                        {
                            if (f.Name.ToLower() == varName)
                            {
                                fieldOff = f.Offset;
                                break;
                            }
                        }
                        if (fieldOff >= 0)
                        {
                            // THIS 指针位于 R12+8 (第一个参数)
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R12+8") }));
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, fieldOff) }));
                            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 2) }));
                            instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, "R3"), new Operand(OperandType.REGISTER, srcReg) }));
                        }
                        else
                        {
                            // Not a field — create global variable
                            int varOffset = GetOrCreateVariable(ident.Name) * 4;
                            instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12+{8 + varOffset}"), new Operand(OperandType.REGISTER, srcReg) }));
                        }
                    }
                    else
                    {
                        // 模块级（全局）变量 —— 写静态区全局段，与 EmitLoadVar 的读侧对称。
                        //
                        // ⚠ 这里原来是 `R12+{8 + index*4}`：SUB 的 R12 是子帧，写进去等于丢在子帧里，
                        //   而读侧已经改从全局段读 ⇒ **写读两边各写各的**：`counter = counter + 5`
                        //   读回 0、写进子帧，永远不累加（实测 t8 得 0）；更糟的是循环变量不推进，
                        //   表现为**死循环**（`draw_board` 里的 `WHILE i < BW` 就是这么卡住的）。
                        GetOrCreateVariable(ident.Name);
                        EmitStoreVar(ident.Name, srcReg);
                    }
                }
            }
            else if (stmt.Variable is ArrayAccessExpression arrayAccess)
            {
                GenerateArrayAssignment(arrayAccess, 1);
            }
            else if (stmt.Variable is FieldAccessExpression fieldAccess)
            {
                string fieldName = fieldAccess.FieldName.ToLower();

                // Handle expression-based record (e.g., array access: sammy(1).head = value)
                if (fieldAccess.RecordExpression != null)
                {
                    string recName;
                    if (fieldAccess.RecordExpression is Identifier exprIdent)
                        recName = exprIdent.Name.ToLower();
                    else if (fieldAccess.RecordExpression is ArrayAccessExpression arrExpr)
                        recName = arrExpr.ArrayName.ToLower();
                    else
                        recName = fieldAccess.RecordName?.ToLower() ?? "";

                    if (string.IsNullOrEmpty(recName) || !dimAsVariables.ContainsKey(recName) || !typeDefinitions.ContainsKey(dimAsVariables[recName]))
                    {
                        // Unknown type — store to offset 0 via R2
                        GenerateSubExpression(fieldAccess.RecordExpression, 2);
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R2") }));
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
                        throw new CompilationException(ErrorCode.CodeGen_TypeMismatch, $"类型 '{tDef.Name}' 中没有字段 '{fieldName}'");

                    GenerateSubExpression(fieldAccess.RecordExpression, 2);
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, fOff) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 2) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R3") }));
                    return;
                }

                string recordName = fieldAccess.RecordName.ToLower();

                if (!dimAsVariables.ContainsKey(recordName) || !typeDefinitions.ContainsKey(dimAsVariables[recordName]))
                {
                    // Unknown type — treat field access as plain var access (offset 0)
                    if (currentLocalVars.ContainsKey(recordName))
                    {
                        int slot = currentLocalVars[recordName];
                        int baseOffset = -(slot + 1) * 4;
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, baseOffset) }));
                        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 14) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R2") }));
                    }
                    else if (variables.ContainsKey(recordName))
                    {
                        int varOffset = variables[recordName] * 4;
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 8 + varOffset) }));
                        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 12) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R2") }));
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
                    throw new CompilationException(ErrorCode.CodeGen_TypeMismatch, $"类型 '{typeDef.Name}' 中没有字段 '{fieldName}'");

                // In SUB context, check locals first
                if (currentLocalVars.ContainsKey(recordName))
                {
                    int slot = currentLocalVars[recordName];
                    int baseOffset = -(slot + 1) * 4;
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, baseOffset + fieldOffset) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 14) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R2") }));
                }
                else if (variables.ContainsKey(recordName))
                {
                    int varOffset = variables[recordName] * 4;
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.IMMEDIATE, 8 + varOffset + fieldOffset) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 12) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, "R2") }));
                }
            }
        }

        private void GenerateSubIfStatement(IfStatement stmt)
        {
            GenerateSubExpression(stmt.Condition, 0);
            string elseLabel = GenerateLabel();
            string endLabel = GenerateLabel();
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.JZ, new List<Operand> { new Operand(OperandType.LABEL, elseLabel) }));
            if (stmt.ThenBranch != null) GenerateSubStatement(stmt.ThenBranch);
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, elseLabel) }));
            if (stmt.ElseBranch != null) GenerateSubStatement(stmt.ElseBranch);
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
        }

        private void GenerateSubForStatement(ForStatement stmt)
        {
            // FOR 循环变量强制为整数类型
            string varName = stmt.Variable.Name.ToLower();
            if (!variableTypes.ContainsKey(varName) || variableTypes[varName] != BasicType.Integer)
                variableTypes[varName] = BasicType.Integer;

            string loopLabel = GenerateLabel();
            string endLabel = GenerateLabel();

            // Helper: get store target address for variable
            string GetVarAddr()
            {
                if (currentLocalVars.ContainsKey(varName))
                {
                    int slot = currentLocalVars[varName];
                    return $"R12-{(slot + 1) * 4}";
                }
                if (IsStaticVariable(varName))
                {
                    string staticName = $"{currentSubName.ToLower()}__{varName}";
                    int varOffset = variables[staticName] * 4;
                    return $"R12+{8 + varOffset}";
                }
                throw new CompilationException(ErrorCode.CodeGen_UndefinedVariable, $"FOR 变量 '{stmt.Variable.Name}' 未定义");
            }

            GenerateSubExpression(stmt.InitialValue, 0);
            string initAddr = GetVarAddr();
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, initAddr) }));

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));

            Sta.PushLoopLabels(endLabel, loopLabel);

            // Load variable and compare with end value
            string loadAddr = GetVarAddr();
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, loadAddr) }));
            GenerateSubExpression(stmt.EndValue, 1);
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
            instructions.Add(new Instruction(OpCode.JG, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));

            foreach (var bodyStmt in stmt.Body)
                GenerateSubStatement(bodyStmt);

            // Increment
            string incAddr = GetVarAddr();
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, incAddr) }));
            GenerateSubExpression(stmt.StepValue, 1);
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, incAddr) }));

            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));

            Sta.PopLoopLabels();
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
        }

        private void GenerateSubWhileStatement(WhileStatement stmt)
        {
            string loopLabel = GenerateLabel();
            string endLabel = GenerateLabel();

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));
            GenerateSubExpression(stmt.Condition, 0);
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.JZ, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));

            foreach (var bodyStmt in stmt.Body)
                GenerateSubStatement(bodyStmt);

            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
        }

        private void GenerateSubDoLoopStatement(DoLoopStatement stmt)
        {
            string loopLabel = GenerateLabel();
            string endLabel = GenerateLabel();

            Sta.PushLoopLabels(endLabel, loopLabel);

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));

            // Pre-test condition: DO WHILE/UNTIL — check before body
            if (stmt.HasCondition && stmt.IsPreTest && stmt.Condition != null)
            {
                GenerateSubExpression(stmt.Condition, 0);
                instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
                if (stmt.IsUntil)
                    instructions.Add(new Instruction(OpCode.JNZ, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
                else
                    instructions.Add(new Instruction(OpCode.JZ, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
            }

            foreach (var bodyStmt in stmt.Body)
                GenerateSubStatement(bodyStmt);

            // Post-test condition (LOOP WHILE/UNTIL)
            if (stmt.HasCondition && !stmt.IsPreTest && stmt.Condition != null)
            {
                GenerateSubExpression(stmt.Condition, 0);
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

        /// <summary>递归检查表达式中是否包含函数调用</summary>
        private static bool ContainsFunctionCall(Expression? expr)
        {
            if (expr == null) return false;
            if (expr is FunctionCallExpression) return true;
            if (expr is BinaryExpression bin)
                return ContainsFunctionCall(bin.Left) || ContainsFunctionCall(bin.Right);
            if (expr is UnaryExpression un)
                return ContainsFunctionCall(un.Expression);
            return false;
        }

        private void GenerateSubExpression(Expression expr, int reg)
        {
            if (expr is StringLiteral strLit)
            {
                if (string.IsNullOrEmpty(strLit.Value))
                {
                    // 空字符串在比较上下文中直接返回 0
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                }
                else
                {
                    // 非空字符串: LEA 加载地址
                    string strLabel = $"str_data_{GenerateLabel()}";
                    dataSection[strLabel] = new DataString(strLit.Value);
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.LABEL, strLabel) }));
                }
                return;
            }
            else if (expr is InkeyExpression inkey)
            {
                GenerateInkeyExpression(inkey, reg);
            }
            else if (expr is NumberLiteral numLiteral)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, (int)numLiteral.Value) }));
            }
            else if (expr is Identifier ident)
            {
                // Check if this is a compile-time constant
                if (constants.ContainsKey(ident.Name.ToLower()))
                {
                    var val = constants[ident.Name.ToLower()];
                    if (val is int intVal)
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, intVal) }));
                    }
                    else
                    {
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                    }
                    return;
                }
                // Try local variable first (negative offsets from BP)
                if (currentLocalVars.ContainsKey(ident.Name.ToLower()))
                {
                    int slot = currentLocalVars[ident.Name.ToLower()];
                    int offset = -(slot + 1) * 4;
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R12-{-offset}") }));
                }
                else
                {
                    // Try parameter (positive offsets from BP: BP+8, BP+12, ...)
                    int paramIdx = FindParameterIndex(ident.Name.ToLower());
                    if (paramIdx >= 0)
                    {
                        int offset = 8 + paramIdx * 4;
                        
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
                        
                        if (isByRef)
                        {
                            // BYREF参数: [R12+offset] 包含地址，需要通过地址加载值
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R12+{offset}") }));
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R{reg}") }));
                        }
                        else
                        {
                            // BYVAL参数: 直接加载值
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R12+{offset}") }));
                        }
                    }
                    // Check STATIC variable (scoped in globals)
                    else if (IsStaticVariable(ident.Name.ToLower()))
                    {
                        string staticName = $"{currentSubName.ToLower()}__{ident.Name.ToLower()}";
                        int varOffset = variables[staticName] * 4;
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R12+{8 + varOffset}") }));
                    }
                    // Phase 2: 方法内字段读取 — 通过 THIS 指针
                    else if (currentClassName != null && classDefinitions.TryGetValue(currentClassName.ToLower(), out var clsDefRead))
                    {
                        int fieldOff = -1;
                        string fieldVarName = ident.Name.ToLower();
                        foreach (var f in clsDefRead.Fields)
                        {
                            if (f.Name.ToLower() == fieldVarName)
                            {
                                fieldOff = f.Offset;
                                break;
                            }
                        }
                        if (fieldOff >= 0)
                        {
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, "R12+8") }));
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.IMMEDIATE, fieldOff) }));
                            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 3), new Operand(OperandType.REGISTER, 2) }));
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, "R3") }));
                        }
                        else
                        {
                            GetOrCreateVariable(ident.Name);
                            EmitLoadVar(reg, ident.Name);   // 全局变量走静态区全局段（见 EmitLoadVar）
                        }
                    }
                    else
                    {
                        // Auto-create undefined global variable (QBasic behavior: default to 0)
                        GetOrCreateVariable(ident.Name);
                        EmitLoadVar(reg, ident.Name);   // 全局变量走静态区全局段（见 EmitLoadVar）
                    }
                }
            }
            else if (expr is BinaryExpression binary)
            {
                // Save R1 to stack before computing right side (R1 may be clobbered by CALL)
                bool hasFuncCall = ContainsFunctionCall(binary.Right);
                if (hasFuncCall)
                {
                    GenerateSubExpression(binary.Left, 1);
                    instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 1) }));
                    GenerateSubExpression(binary.Right, 2);
                    instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 1) }));
                }
                else
                {
                    GenerateSubExpression(binary.Left, 1);
                    GenerateSubExpression(binary.Right, 2);
                }
                switch (binary.Operator)
                {
                    case "+":
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 1) }));
                        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 2) }));
                        break;
                    case "-":
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 1) }));
                        instructions.Add(new Instruction(OpCode.SUB, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 2) }));
                        break;
                    case "*":
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 1) }));
                        instructions.Add(new Instruction(OpCode.MUL, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 2) }));
                        break;
                    case "/":
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 1) }));
                        instructions.Add(new Instruction(OpCode.DIV, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 2) }));
                        break;
                    case "=":
                        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 2) }));
                        string eqLabel1 = GenerateLabel();
                        string eqLabel2 = GenerateLabel();
                        instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, eqLabel1) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 1) }));
                        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, eqLabel2) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, eqLabel1) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, eqLabel2) }));
                        break;
                    case "<>":
                        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 2) }));
                        string neLabel1 = GenerateLabel();
                        string neLabel2 = GenerateLabel();
                        instructions.Add(new Instruction(OpCode.JE, new List<Operand> { new Operand(OperandType.LABEL, neLabel1) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 1) }));
                        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, neLabel2) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, neLabel1) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, neLabel2) }));
                        break;
                    case "<":
                        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 2) }));
                        string lLabel1 = GenerateLabel();
                        string lLabel2 = GenerateLabel();
                        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, lLabel1) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 1) }));
                        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, lLabel2) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lLabel1) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, lLabel2) }));
                        break;
                    case ">":
                        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 2) }));
                        string gLabel1 = GenerateLabel();
                        string gLabel2 = GenerateLabel();
                        instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, gLabel1) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 1) }));
                        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, gLabel2) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, gLabel1) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, gLabel2) }));
                        break;
                    case "<=":
                        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 2) }));
                        string leLabel1 = GenerateLabel();
                        string leLabel2 = GenerateLabel();
                        instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, leLabel1) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, leLabel2) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, leLabel1) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 1) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, leLabel2) }));
                        break;
                    case ">=":
                        instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 2) }));
                        string geLabel1 = GenerateLabel();
                        string geLabel2 = GenerateLabel();
                        instructions.Add(new Instruction(OpCode.JGE, new List<Operand> { new Operand(OperandType.LABEL, geLabel1) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                        instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, geLabel2) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, geLabel1) }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 1) }));
                        instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, geLabel2) }));
                        break;
                    case "AND":
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 1) }));
                        instructions.Add(new Instruction(OpCode.AND, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 2) }));
                        break;
                    case "OR":
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 1) }));
                        instructions.Add(new Instruction(OpCode.OR, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 2) }));
                        break;
                }
            }
            else if (expr is UnaryExpression unary)
            {
                GenerateSubExpression(unary.Expression, reg);
                if (unary.Operator == "-")
                {
                    instructions.Add(new Instruction(OpCode.NEG, new List<Operand> { new Operand(OperandType.REGISTER, reg) }));
                }
                else if (unary.Operator == "NOT")
                {
                    string notLabel1 = GenerateLabel();
                    string notLabel2 = GenerateLabel();
                    instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                    instructions.Add(new Instruction(OpCode.JNE, new List<Operand> { new Operand(OperandType.LABEL, notLabel1) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 1) }));
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, notLabel2) }));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, notLabel1) }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, notLabel2) }));
                }
            }
            else if (expr is FunctionCallExpression funcCall)
            {
                GenerateSubFunctionCall(funcCall, reg);
            }
            else if (expr is ArrayAccessExpression arrayAccess)
            {
                GenerateArrayAccess(arrayAccess, reg, false);
            }
            else if (expr is FieldAccessExpression fieldAccess)
            {
                GenerateFieldAccessExpression(fieldAccess, reg);
            }
        }

        private int FindParameterIndex(string paramName)
        {
            // Find parameter index from current sub/function
            if (currentSubName == null)
            {
                return -1;
            }
            
            if (subMap.ContainsKey(currentSubName.ToLower()))
            {
                var sub = subMap[currentSubName.ToLower()];
                for (int i = 0; i < sub.Parameters.Count; i++)
                {
                    if (sub.Parameters[i].Name.ToLower() == paramName)
                        return i;
                }
            }
            if (funcMap.ContainsKey(currentSubName.ToLower()))
            {
                var func = funcMap[currentSubName.ToLower()];
                for (int i = 0; i < func.Parameters.Count; i++)
                {
                    if (func.Parameters[i].Name.ToLower() == paramName)
                        return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 生成变量地址到指定寄存器
        /// </summary>
        private void GenerateVariableAddress(Expression expr, int reg)
        {
            if (expr is Identifier ident)
            {
                // 在SUB/FUNCTION内部
                if (currentSubName != null)
                {
                    // 检查是否为局部变量
                    if (currentLocalVars.ContainsKey(ident.Name.ToLower()))
                    {
                        int slot = currentLocalVars[ident.Name.ToLower()];
                        int offset = -(slot + 1) * 4;
                        // 计算局部变量地址: R12 + offset
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, offset) }));
                        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 12) }));
                    }
                    // 检查是否为参数
                    else if (FindParameterIndex(ident.Name.ToLower()) >= 0)
                    {
                        int paramIdx = FindParameterIndex(ident.Name.ToLower());
                        int offset = 8 + paramIdx * 4;
                        // 计算参数地址: R12 + offset
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, offset) }));
                        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 12) }));
                    }
                    // 全局变量：**取静态区全局段的地址**（主程序与 SUB 共用同一份）
                    else if (variables.ContainsKey(ident.Name.ToLower()))
                    {
                        if (_globalVars.Contains(ident.Name.ToLower()))
                        {
                            EmitStaticAddr(reg, STATIC_GLOBALS_OFFSET + GetVarByteOffset(ident.Name.ToLower()));
                        }
                        else
                        {
                            int off = GetVarByteOffset(ident.Name.ToLower());
                            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 8 + off) }));
                            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 12) }));
                        }
                    }
                    else
                    {
                        // Auto-create undefined variable (QBasic behavior)
                        GetOrCreateVariable(ident.Name);
                        EmitStaticAddr(reg, STATIC_GLOBALS_OFFSET + GetVarByteOffset(ident.Name.ToLower()));
                    }
                }
                else
                {
                    // 在主程序中，只有全局变量
                    int varOffset = GetOrCreateVariable(ident.Name) * 4;
                    // 计算全局变量地址: R12 + 8 + varOffset
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 8 + varOffset) }));
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 12) }));
                }
            }
            else
            {
                // BYREF: 非简单变量的表达式，先求值作为地址
                // (例如 BYREF 传递的数组元素或计算表达式)
                if (currentSubName != null)
                    GenerateSubExpression(expr, reg);
                else
                    GenerateExpression(expr, reg);
            }
        }

        /// <summary>
        /// 在 SUB/FUNCTION 上下文中存储值到变量
        /// </summary>
        private void GenerateSubVariableStore(string varName, int valueReg)
        {
            if (currentLocalVars.ContainsKey(varName))
            {
                int slot = currentLocalVars[varName];
                int offset = -(slot + 1) * 4;
                BasicType varType = GetVariableType(varName);
                OpCode storeOp = GetStoreInstruction(varType);
                instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12-{-offset}"), new Operand(OperandType.REGISTER, valueReg) }));
            }
            else
            {
                int paramIdx = FindParameterIndex(varName);
                if (paramIdx >= 0)
                {
                    int offset = 8 + paramIdx * 4;
                    BasicType varType = GetVariableType(varName);
                    OpCode storeOp = GetStoreInstruction(varType);
                    instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12+{offset}"), new Operand(OperandType.REGISTER, valueReg) }));
                }
                else if (variables.ContainsKey(varName))
                {
                    int varOffset = variables[varName] * 4;
                    BasicType varType = GetVariableType(varName);
                    OpCode storeOp = GetStoreInstruction(varType);
                    instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12+{8 + varOffset}"), new Operand(OperandType.REGISTER, valueReg) }));
                }
                else
                {
                    throw new CompilationException(ErrorCode.CodeGen_UndefinedVariable, $"变量 '{varName}' 未定义 (在 SUB/FUNCTION '{currentSubName}' 中)");
                }
            }
        }

        private void GenerateCallStatement(CallStatement stmt)
        {
            // 获取SUB声明以检查参数是否为BYREF + native
            SubDeclaration subDecl = null;
            subMap.TryGetValue(stmt.SubName.ToLower(), out subDecl);

            // native SUB: 使用裸名 CALL (无 sub_ 前缀)
            string subLabel = (subDecl != null && subDecl.IsNative)
                ? stmt.SubName.ToLower()
                : "sub_" + stmt.SubName.ToLower();

            // Push arguments (right-to-left)
            for (int i = stmt.Arguments.Count - 1; i >= 0; i--)
            {
                bool isByRef = false;
                if (subDecl != null && i < subDecl.Parameters.Count)
                {
                    isByRef = subDecl.Parameters[i].IsByRef;
                }
                
                if (isByRef)
                {
                    // BYREF: 传递变量地址
                    GenerateVariableAddress(stmt.Arguments[i], 0);
                }
                else
                {
                    // BYVAL: 传递值
                    if (currentSubName != null)
                    {
                        GenerateSubExpression(stmt.Arguments[i], 0);
                    }
                    else
                    {
                        GenerateExpression(stmt.Arguments[i], 0);
                    }
                }
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
            }

            // 在 CALL 之前保存活跃的寄存器，调用后恢复
            var (saveInstrs, restoreInstrs) = Regs.SaveForCall();
            foreach (var si in saveInstrs)
                instructions.Add(si);

            // CALL
            instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, subLabel) }));

            // 恢复寄存器
            foreach (var ri in restoreInstrs)
                instructions.Add(ri);

            // Clean up stack (caller cleans)
            if (stmt.Arguments.Count > 0)
            {
                int stackCleanup = stmt.Arguments.Count * 4;
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, stackCleanup) }));
            }
        }

        private void GenerateSubFunctionCall(FunctionCallExpression funcCall, int reg)
        {
            string funcName = funcCall.FunctionName.ToLower();
            
            // 内置函数分发 — 调用前保护 R0-R5（结果寄存器除外）
            // Protects caller registers from being clobbered by expression evaluation inside built-ins
            switch (funcName)
            {
                // === 数学函数 (C 库实现) ===
                case "sqr":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_sqr", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "int":
                case "fix":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_int", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "rnd":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_rnd", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "sin":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_sin", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "cos":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_cos", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "tan":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_tan", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "exp":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_exp", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "log":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_log", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "atn":
                case "atan":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_atn", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                // === 硬件/系统函数 (保留内联) ===
                case string p when p == "peek" || p == "peekb" || p == "peekh" || p == "peekl" || p == "peekf" || p == "peekd":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateBuiltInPeek(funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "point":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_point", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "command$":
                case "command":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateCommand(funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "tab":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateBuiltInTab(funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                // === C 库实现 (CALL basic_xxx) ===
                case "abs":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_abs", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "sgn":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_sgn", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "len":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_len", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "chr$":
                case "chr":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_chr", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "asc":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_asc", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "left$":
                case "left":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_left", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "right$":
                case "right":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_right", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "mid$":
                case "mid":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_mid3", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "str$":
                case "str":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_str_int", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "val":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_val", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "space$":
                case "space":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_space", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "ucase$":
                case "ucase":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_ucase", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "lcase$":
                case "lcase":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_lcase", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "ltrim$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_ltrim", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "rtrim$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_rtrim", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                // === 新 C 库函数 ===
                case "instr":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_instr", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "string$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5);
                    if (funcCall.Arguments.Count >= 2 && funcCall.Arguments[1] is StringLiteral)
                        GenerateLibraryCall("basic_stringS", funcCall, reg);
                    else
                        GenerateLibraryCall("basic_stringN", funcCall, reg);
                    EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "hex$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_hex", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "oct$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_oct", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "date$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_date_str", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "time$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_time_str", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "timer":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_timer", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "input$":
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateLibraryCall("basic_inputN", funcCall, reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
                case "lof":
                case "eof":
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                    return;
                default:
                    break;
            }
            
            // native FUNCTION: 使用裸名 CALL (无 func_ 前缀)
            funcMap.TryGetValue(funcCall.FunctionName.ToLower(), out var fd);
            string funcLabel = (fd != null && fd.IsNative)
                ? funcCall.FunctionName.ToLower()
                : "func_" + funcCall.FunctionName.ToLower();

            // Push arguments (right-to-left)
            for (int i = funcCall.Arguments.Count - 1; i >= 0; i--)
            {
                GenerateSubExpression(funcCall.Arguments[i], 0);
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
            }

            // CALL
            instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, funcLabel) }));

            // Clean up stack
            if (funcCall.Arguments.Count > 0)
            {
                int stackCleanup = funcCall.Arguments.Count * 4;
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, stackCleanup) }));
            }

            // Return value is in R0
            if (reg != 0)
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 0) }));
            }
        }

        private void GenerateBuiltInTab(FunctionCallExpression funcCall, int reg)
        {
            if (funcCall.Arguments.Count > 0)
                GenerateExpr(funcCall.Arguments[0], reg);
            else
                AddRI(OpCode.MOVE, reg, 1);
        }
    }
}
