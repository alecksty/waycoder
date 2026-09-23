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
            string subLabel = SubLabel(subDecl.Name);
            currentSubName = subDecl.Name;
            currentLocalVars = new Dictionary<string, int>();
            currentLocalVarCount = 0;
            currentParamCount = subDecl.Parameters.Count;

            // 形参声明为 STRING（`SUB f(s AS STRING)`）⇒ 登记成字符串类型。
            // 不登记的话 `GetVariableType("s")` 走"无后缀 ⇒ Integer"那条默认，
            // 于是 `PRINT s` 把**字符串指针**当整数打出来 —— 实测打出 `1024`（一个地址），
            // 一个字都不像"字符串坏了"的样子。
            foreach (var pDecl in subDecl.Parameters)
                if (pDecl.IsString)
                    variableTypes[pDecl.Name.ToLower()] = BasicType.String;

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
                CurrentSourceLine = stmt.Line; CurrentSourceColumn = stmt.Column;
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
            string funcLabel = FunctionLabel(funcDecl.Name);
            currentSubName = funcDecl.Name;
            currentLocalVars = new Dictionary<string, int>();
            currentLocalVarCount = 0;
            currentParamCount = funcDecl.Parameters.Count;

            // 形参声明为 STRING ⇒ 登记成字符串类型（同 SUB 那处，理由见那里）
            foreach (var pDecl in funcDecl.Parameters)
                if (pDecl.IsString)
                    variableTypes[pDecl.Name.ToLower()] = BasicType.String;

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
                CurrentSourceLine = stmt.Line; CurrentSourceColumn = stmt.Column;
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
            // `DIM arr(n)` / `DIM arr(n) AS INTEGER` 写在 SUB 里 —— **数组**。
            //
            // ⚠ 此前**完全没有这条分支**：数组名与它的元素槽一个都没建，
            //   于是 `arr(2) = 7` 静默什么都不做、`PRINT arr(2)` 恒为 0
            //   （`GenerateArrayAccess` 查不到 `arrayVariables` 就"返回 0 / 不生成代码"）。
            //   实测 t11：`SUB g() / DIM arr(8) AS INTEGER / arr(2) = 7 / PRINT arr(2)` 打出 0。
            //
            // 存储位置：**静态区全局段**（与模块级数组同一块），不是子帧。理由是
            // `GenerateArrayAccess` 现在只认静态区寻址（见那里的注释：帧相对寻址在跨层时
            // 会读到别的帧的暂存区）。**代价要说清楚**：这样 DIM 出来的数组
            // **跨调用保留**、且**递归不安全** —— 与 QBasic 的"每次调用一份局部数组"不同。
            // 之所以接受这个偏离：原来它根本不工作（不是语义不同，是没有语义），
            // 而"静态数组"至少行为确定、可解释；真要递归就得把数组也做成帧相对，
            // 那是另一件事（要同时改 `GenerateArrayAccess` 的寻址模型）。
            else if (stmt is DimStatement dimStmt && (dimStmt.Dimensions.Count > 0 || dimStmt.Size > 1))
            {
                if (!arrayVariables.ContainsKey(dimStmt.VariableName))
                {
                    int arrSize = dimStmt.Size > 0 ? dimStmt.Size : 1;
                    arrayVariables[dimStmt.VariableName] = new ArrayInfo
                    {
                        Size = arrSize,
                        Offset = variableCount,
                        Dimensions = dimStmt.Dimensions.Count > 0
                            ? new List<int>(dimStmt.Dimensions)
                            : new List<int> { arrSize }
                    };
                    for (int i = 0; i < arrSize; i++)
                    {
                        string elementName = $"{dimStmt.VariableName}({i})";
                        if (!variables.ContainsKey(elementName))
                        {
                            variables[elementName] = variableCount++;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(dimStmt.TypeName))
                    dimAsVariables[dimStmt.VariableName.ToLower()] = dimStmt.TypeName.ToLower();
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

                // `DIM s AS STRING` / `DIM n AS INTEGER` —— **类型名要接上**。
                // 只写进 `dimAsVariables`（那是记录字段布局用的表）不够：变量本身的类型
                // 由 `GetVariableType` 决定，而它只看后缀与 DEFtype ⇒ `DIM s AS STRING`
                // 明明写着 STRING，`PRINT s` 仍旧按整数打（实测打出 `1024`，是个栈地址）。
                // 与主程序那处（`CodeGenerator.cs` 的 DimAsStatement 分支）**同一口径**。
                RegisterDimAsType(varName, dimAs.TypeName);
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
            // ⚠ **`SELECT CASE` 必须在这里也接一支**（v0.96.330 修）。
            //   它是 SUB/FUNCTION 分派里**唯一漏掉**的常用语句 —— 而本方法是个
            //   if/else 链，落到最后就**悄无声息地什么都不生成**（没有 else 兜底、
            //   不报错、不警告）。症状极具欺骗性：函数**编译得过、也能调用**，
            //   只是整个 `SELECT CASE` 一条指令都没有 ⇒ 恒等于"一个分支都没进"，
            //   于是 `FUNCTION f(n)` 里 `f = 10/20/30` 三支全不执行、返回值永远是 0
            //   （实测 `f(0)/f(1)/f(2)` 都是 0，而同样逻辑写成 IF/ELSE 就对）。
            //   生成器本身是**共用**的（`GenerateSelectCaseStatement` 里已经按
            //   `currentSubName` 分了两条路），这里补的只是**入口**。
            else if (stmt is SelectCaseStatement selectCaseStmt)
            {
                GenerateSelectCaseStatement(selectCaseStmt);
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
                        EmitRecordFieldAddr(2, recordName, 0);   // 全局记录走静态区全局段
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
                    EmitRecordFieldAddr(2, recordName, fieldOffset);   // 全局记录走静态区全局段
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
                // 模块级变量（含 `DIM SHARED`）：按本前端既有的设计用**静态区的全局段**
                //（`_globalVars`——"主程序与 SUB 共用同一份内存"，见 `EmitLoadVar`）。
                //
                // ⚠ 这里原来是 `throw 未定义`，而 SUB 内部的预扫又刻意**不**把"                模块级变量"登记成局部
                //   （`CollectLocalVariables` 里的 `!IsModuleVariable` 守卫"模块级变量不许被局部遮蔽"）
                //   ⇒ 只要一个名字在顶层当过循环变量（`i` 这种），
                //   SUB 里再用同名循环就当场报错。GORILLA.BAS 就是这么挂的。
                if (IsModuleVariable(stmt.Variable.Name))
                {
                    // 地址得算进一个寄存器（`EmitStaticAddr` 是"算到寄存器"而不是返回字符串）。
                    // 用 R3：四个调用点都是"算完地址 ⇒ 紧跟一条访问"，
                    // 而 FOR 自己只用 R0/R1/R2（见 `EmitForLoopTest`）。
                    EmitStaticAddr(3, STATIC_GLOBALS_OFFSET + GetVarByteOffset(varName));
                    return "R3";
                }
                throw new CompilationException(ErrorCode.CodeGen_UndefinedVariable, $"FOR 变量 '{stmt.Variable.Name}' 未定义");
            }

            GenerateSubExpression(stmt.InitialValue, 0);
            string initAddr = GetVarAddr();
            // ⚠ 操作数顺序：MOVE 是 **dest-first**（`move [mem], reg` 是"存"、`move reg, [mem]` 是"取"）。
            //   这里原来写成 [REGISTER 0, MEMORY addr] —— 那是**取**不是存（`move R0 [R12-4]`），
            //   于是 `FOR i = 0 TO 3` 的初值根本没写进去；自增那处同样写反 ⇒ 循环变量永不推进，
            //   实测就是"10 秒跑 10 亿条指令"的死循环（t10）。两处都改成 [MEMORY addr, REGISTER 0]。
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, initAddr), new Operand(OperandType.REGISTER, 0) }));

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));

            Sta.PushLoopLabels(endLabel, loopLabel);

            // Load variable and compare with end value
            string loadAddr = GetVarAddr();
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, loadAddr) }));
            // 循环测试与主程序**共用一份**（`EmitForLoopTest`）—— 从前两处各写一遍无条件 `JG`，
            // 于是"负步长一次都不执行"那个 bug 被复制成了两份。判据与说明都在那个方法上。
            string bodyLabel = GenerateLabel();
            EmitForLoopTest(
                () => GenerateSubExpression(stmt.EndValue, 1),
                () => GenerateSubExpression(stmt.StepValue, 2),
                bodyLabel, endLabel);

            // 循环体前后"保护/恢复循环变量" —— 与主程序的 GenerateForStatement **同一形状**，
            // 两套 FOR 实现不再一个有一个没有。
            //
            // ⚠ 说清楚它到底做了什么：PUSH 的是"循环变量当前值"，POP 回 R0，紧随其后那句是**读**
            //   （`MOVE R0, [addr]`）而不是写回。循环变量本身在内存里、全程没被动过，所以这一对
            //   PUSH/POP 对**变量**是空操作（主程序那条路径同样如此，见 Statements.cs 里那段注释）；
            //   它唯一的效果是让 R0 跨循环体保持一致，而自增的第一句又会重新装载 R0 ⇒ 可观测行为为零。
            //   照抄而不"改进"的理由：BASIC 里循环体内给循环变量赋值是合法的（应当生效），
            //   写成"从栈里写回"反而会把体内对循环变量的修改吞掉 —— 那是改语义，不是修 bug。
            string loopVarAddr = GetVarAddr();
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, loopVarAddr) }));
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));

            foreach (var bodyStmt in stmt.Body)
                GenerateSubStatement(bodyStmt);

            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, loopVarAddr) }));

            // Increment
            string incAddr = GetVarAddr();
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, incAddr) }));
            GenerateSubExpression(stmt.StepValue, 1);
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, incAddr), new Operand(OperandType.REGISTER, 0) }));

            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));

            Sta.PopLoopLabels();
            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
        }

        private void GenerateSubWhileStatement(WhileStatement stmt)
        {
            string loopLabel = GenerateLabel();
            string endLabel = GenerateLabel();

            // ⚠ **必须登记循环标签**（v0.96.331 修）—— 旁边的 `GenerateSubDoLoopStatement`
            //   一直有这一句，WHILE 这条**漏了**。后果是 SUB/FUNCTION 里的 `EXIT WHILE`
            //   **一条指令都发不出来**（`EmitBreak` 见循环栈为空 ⇒ 什么都不做 ⇒ 静默空操作），
            //   循环只能靠条件自己结束。症状极具欺骗性：
            //   `WHILE sum > 21` 这种**唯一的出口就是那句 EXIT** 的写法 ⇒ **死循环**
            //   （BLACKJACK 的 `handValue` 实测卡死在自检的第一处调用上、整轮超时）；
            //   而"条件也会自己结束"的循环则表现为**多跑完剩下的圈数**
            //   （最小复现 `d5.bas`：`f(4)` 应返回 5，实测 100）。
            //   顶层 WHILE 一直是对的 —— 又是一次「SUB 体才是重灾区」。
            Sta.PushLoopLabels(endLabel, loopLabel);

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));
            GenerateSubExpression(stmt.Condition, 0);
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0) }));
            instructions.Add(new Instruction(OpCode.JZ, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));

            foreach (var bodyStmt in stmt.Body)
                GenerateSubStatement(bodyStmt);

            instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));
            Sta.PopLoopLabels();
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
                    // ⚠ 空串**不再是整数 0**，而是**全前端唯一那个空串地址**
                    //   （`EmptyStringLabel`）—— 理由见那里的长注释：字符串比较就是指针比较，
                    //   `INKEY$ <> ""` 这种写法要求"所有空串是同一个地址"，
                    //   而 0 / `str_data_N` / `INKEY$` 的返回地址三者互不相等 ⇒ 死循环。
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.LABEL, EmptyStringLabel) }));
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
                        // 形参 / STATIC / 类字段三条都排除之后落到这里 ⇒ 没声明过。
                        // 「报错还是只警告」的判据**不写在这里** —— 收在
                        // `GetOrCreateVariable` 一处（那是全前端唯一"没见过就造一个"的出口，
                        // 写在这里会与它重复计数，也漏掉 `Expressions.cs` 那条无条件调用）。
                        GetOrCreateVariable(ident.Name);
                        EmitLoadVar(reg, ident.Name);   // 全局变量走静态区全局段（见 EmitLoadVar）
                    }
                }
            }
            else if (expr is BinaryExpression binary)
            {
                // ══════════════════════════════════════════════════════════════════════
                // 二元表达式 —— **SUB 体那一套最贵的一处缺陷**（顶层走 GenerateExpression，与此无关）
                //
                // 这里原来是：
                //     GenerateSubExpression(left, 1);
                //     GenerateSubExpression(right, 2);          // 右侧带函数调用时先 push R1
                //     case "*": MOVE reg, R1; MUL reg, R2;
                //
                // 两个错叠在一起，**都不报错**：
                //   ① **操作数寄存器硬编码 R1/R2，且不保护左值** —— 右侧只要是个复合子表达式，
                //      它自己又会用 R1（把外层辛辛苦苦算好的左值冲掉）；
                //   ② **合并式 `MOVE reg, R1` 在 reg == 2 时自毁** —— 那正是"右操作数被求值时
                //      所用的 reg"，`MOVE R2, R1` 先把右值覆盖掉，紧接着 `OP R2, R2` = 自己跟自己算。
                //   实测：`h = 1 + (2 * 3)` 得 **6**（内层先算成 4 且把外层的 1 覆盖成 2 ⇒ 2+4）。
                //   受影响的是**一切右侧为复合表达式的算式与比较**，其中最隐蔽的是条件：
                //   `IF bi > NB - 1 THEN …`（NB 是 CONST）里右侧 `NB - 1` 把 `bi` 冲掉，
                //   比较变成 `6 > 0` ⇒ **无条件成立**（bi = 0 也会被改成 5）。
                //
                // 现在的形状：**左右都算到 R1，左值用栈保管**。递归任意深都不会互相覆盖，
                // 也不需要"右侧有没有函数调用"这种特判（原来那个 `hasFuncCall` 只挡住了
                // 函数调用这一种覆盖来源，复合子表达式照样覆盖）。
                // 求值结束时：**左值在 R2、右值在 R1**。
                // ══════════════════════════════════════════════════════════════════════
                // 字符串拼接**先分叉**（与顶层 GenerateExpression 同一条判据、同一个库函数）
                if (GenerateStringConcat(binary, reg)) return;

                GenerateSubExpression(binary.Left, 1);
                instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 1) }));
                GenerateSubExpression(binary.Right, 1);
                instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 2) }));

                // ⚠ 运算符要**大小写无关**地比：操作数文本取自 token 的 `Value`（保留源码大小写），
                //   所以 `mod` / `Mod` / `MOD` 是三个不同的字符串。顶层那套按 `"MOD"` 精确比，
                //   小写 mod 同样编不出代码（同一个坑，只是这边顺手一起兜住）。
                string op = binary.Operator.ToUpperInvariant();

                // 算术与位运算：都满足 `reg = 左 OP 右`
                OpCode? arithOp = op switch
                {
                    "+" => OpCode.ADD,
                    "-" => OpCode.SUB,
                    "*" => OpCode.MUL,
                    "/" => OpCode.DIV,
                    "\\" => OpCode.DIV,        // BASIC 的整除：VML 的 DIV 对整数就是整除
                    "MOD" => OpCode.MOD,
                    "AND" => OpCode.AND,
                    "OR" => OpCode.OR,
                    _ => null
                };

                if (arithOp != null)
                {
                    // reg == 1 时结果寄存器**就是右操作数所在的寄存器**，`MOVE reg, R2` 会把它冲掉；
                    // 改为在 R2（左值）上就地累加、最后搬回 R1。
                    if (reg == 1)
                    {
                        AddRR(arithOp.Value, 2, 1);
                        AddRR(OpCode.MOVE, 1, 2);
                    }
                    else
                    {
                        AddRR(OpCode.MOVE, reg, 2);
                        AddRR(arithOp.Value, reg, 1);
                    }
                }
                else if (op == "^")
                {
                    // 幂：R0 = 1; while (R1 > 0) { R0 = R0 * R2; R1 = R1 - 1; }
                    // 用 R0 当累加器 —— 此刻 R0 不是活的（左值在 R2、右值在 R1/栈上）。
                    string powLoop = GenerateLabel();
                    string powEnd = GenerateLabel();
                    AddRI(OpCode.MOVE, 0, 1);
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, powLoop) }));
                    AddRI(OpCode.CMP, 1, 0);
                    instructions.Add(new Instruction(OpCode.JLE, new List<Operand> { new Operand(OperandType.LABEL, powEnd) }));
                    AddRR(OpCode.MUL, 0, 2);
                    AddRI(OpCode.SUB, 1, 1);
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, powLoop) }));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, powEnd) }));
                    if (reg != 0) AddRR(OpCode.MOVE, reg, 0);
                }
                else if (op == "=" || op == "<>" || op == "<" || op == "<=" || op == ">" || op == ">=")
                {
                    // 比较：左在 R2、右在 R1 —— 跳转指令的**条件**与原来一字不差，
                    // 只是把 `CMP R1, R2` 换成 `CMP R2, R1`（操作数次序本来就在 CMP 里）。
                    AddRR(OpCode.CMP, 2, 1);
                    string falseLabel = GenerateLabel();
                    string endLabel = GenerateLabel();
                    // 「不满足」时跳到 falseLabel
                    OpCode jumpOp = op switch
                    {
                        "=" => OpCode.JNE,
                        "<>" => OpCode.JE,
                        "<" => OpCode.JGE,
                        "<=" => OpCode.JG,
                        ">" => OpCode.JLE,
                        _ => OpCode.JL      // ">="
                    };
                    instructions.Add(new Instruction(jumpOp, new List<Operand> { new Operand(OperandType.LABEL, falseLabel) }));
                    AddRI(OpCode.MOVE, reg, 1);
                    instructions.Add(new Instruction(OpCode.JMP, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, falseLabel) }));
                    AddRI(OpCode.MOVE, reg, 0);
                    instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, endLabel) }));
                }
                else
                {
                    // 认不出的运算符 —— 此前是**默默什么都不发**，于是 `100 \ 2` / `100 MOD 7`
                    // 编出来的是一条"没有运算"的赋值（实测都得到 61：上一次运算残留的值）。
                    // 现在报出来，别再让它静默。
                    WarnUnimplemented($"二元运算符 {binary.Operator}");
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
                    // 在主程序中，只有全局变量 —— 传出去的是**变量的地址**（BYREF 实参），
                    // 必须是静态区全局段的地址；给 `R12+8+偏移`（主帧）等于把一个跟变量无关的
                    // 栈地址交给被调方，被调方按地址读写的是主帧。
                    GetOrCreateVariable(ident.Name);
                    EmitVarAddr(reg, ident.Name);
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
                    // 全局变量写静态区全局段（与 EmitLoadVar 的读侧对称）。
                    // ⚠ 原来是 `R12+{8 + 索引*4}`：SUB 的 R12 是子帧 ⇒ 写进去等于丢在子帧里，
                    //   而读侧已改走全局段 —— `READ g` 在主程序里读回 0 就是这么来的。
                    EmitStoreVar(varName, valueReg);
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
            subMap.TryGetValue(SymbolKey(stmt.SubName), out subDecl);

            // 裸调用也可能落在**函数**上（`ui_win_open "T", 10, 20`，丢弃返回值）——
            // 函数声明在 `funcMap` 而不是 `subMap`，此前这里只查 subMap ⇒ `subDecl == null`
            // ⇒ 标签被编成 `sub_ui_win_open`（永远解析不到）。判据与表达式路径
            // （`GenerateSubFunctionCall`）保持一致：native 用**裸名**、否则 `func_` 前缀。
            FunctionDeclaration funcDecl = null;
            if (subDecl == null)
                funcMap.TryGetValue(SymbolKey(stmt.SubName), out funcDecl);

            // native SUB/FUNCTION: 使用裸名 CALL (无 sub_/func_ 前缀)
            bool isNative = (subDecl?.IsNative ?? false) || (funcDecl?.IsNative ?? false);
            string subLabel = isNative
                ? SymbolKey(stmt.SubName)
                : (subDecl != null ? SubLabel(stmt.SubName) : FunctionLabel(stmt.SubName));

            // BYREF 判据对 SUB / FUNCTION 两种声明都成立
            int declParamCount = subDecl?.Parameters.Count ?? funcDecl?.Parameters.Count ?? 0;
            bool IsByRefAt(int i) => i < declParamCount &&
                (subDecl != null ? subDecl.Parameters[i].IsByRef : funcDecl!.Parameters[i].IsByRef);

            // Push arguments (right-to-left)
            for (int i = stmt.Arguments.Count - 1; i >= 0; i--)
            {
                bool isByRef = IsByRefAt(i);

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
                // === 类型转换函数（直接生成转换 opcode）===
                //
                // ⚠ 这四条**在主程序那张表里有、在这张表里漏了** —— 于是同一个
                //   `CINT(x)`「写在主程序里能编、写在 SUB/FUNCTION 里报
                //   未定义的函数 'func_cint'」。实测最小复现：
                //     `FUNCTION Scl (n!) / Scl = CINT(n! / 2 + .1) / END FUNCTION`
                //   GORILLA.BAS 的 `Scl`/`GetNum#` 全是这个形状（`Scl` 被引用 107 次）。
                //   两张表的分工见 `CodeGenerator.Expressions.cs` 的 `GenerateMainFunctionCall`
                //   —— **两边是同一个 switch 的两份**，加/改内置函数必须同时改两处
                //   （本仓头号坑「同一规则两处实现」的又一例）。
                case "csng":
                {
                    GenerateSubExpression(funcCall.Arguments[0], reg);
                    BasicType argType = InferExpressionType(funcCall.Arguments[0]);
                    if (argType == BasicType.Integer)
                        instructions.Add(new Instruction(OpCode.I2F, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    else if (argType == BasicType.Double)
                        instructions.Add(new Instruction(OpCode.D2F, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    _lastExprFloatType = BasicType.Single;
                    return;
                }
                case "cdbl":
                {
                    GenerateSubExpression(funcCall.Arguments[0], reg);
                    BasicType argType = InferExpressionType(funcCall.Arguments[0]);
                    if (argType == BasicType.Integer)
                        instructions.Add(new Instruction(OpCode.I2D, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    else if (argType == BasicType.Single)
                        instructions.Add(new Instruction(OpCode.F2D, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    _lastExprFloatType = BasicType.Double;
                    return;
                }
                case "clng":
                {
                    GenerateSubExpression(funcCall.Arguments[0], reg);
                    BasicType argType = InferExpressionType(funcCall.Arguments[0]);
                    if (argType == BasicType.Integer)
                        instructions.Add(new Instruction(OpCode.I2D, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    else if (argType == BasicType.Single)
                        instructions.Add(new Instruction(OpCode.F2D, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    _lastExprFloatType = BasicType.Double;
                    return;
                }
                case "cint":
                {
                    GenerateSubExpression(funcCall.Arguments[0], reg);
                    BasicType argType = InferExpressionType(funcCall.Arguments[0]);
                    if (argType == BasicType.Single)
                        instructions.Add(new Instruction(OpCode.F2I, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    else if (argType == BasicType.Double)
                        instructions.Add(new Instruction(OpCode.D2I, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, reg) }));
                    _lastExprFloatType = null;
                    return;
                }
                case "lof":
                case "eof":
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                    return;
                default:
                    break;
            }
            
            // native FUNCTION: 使用裸名 CALL (无 func_ 前缀)
            funcMap.TryGetValue(SymbolKey(funcCall.FunctionName), out var fd);
            string funcLabel = (fd != null && fd.IsNative)
                ? SymbolKey(funcCall.FunctionName)
                : FunctionLabel(funcCall.FunctionName);

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
