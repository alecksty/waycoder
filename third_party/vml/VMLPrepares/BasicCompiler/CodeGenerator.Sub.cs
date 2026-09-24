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
            ResetLocalVars();
            currentParamCount = subDecl.Parameters.Count;

            // 形参声明为 STRING（`SUB f(s AS STRING)`）⇒ 登记成字符串类型。
            // 不登记的话 `GetVariableType("s")` 走"无后缀 ⇒ Integer"那条默认，
            // 于是 `PRINT s` 把**字符串指针**当整数打出来 —— 实测打出 `1024`（一个地址），
            // 一个字都不像"字符串坏了"的样子。
            //
            // **数组形参的类型名**（`BCoor() AS XYPoint`）同样要登记进 `dimAsVariables` ——
            // 那是"字段偏移从哪张表查"的唯一入口，`BCoor(i).XCoor` 在 SUB 体里要靠它。
            // 不登记的话字段访问会退化成"记录类型未知" ⇒ 返回 0（静默，不报错）。
            foreach (var pDecl in subDecl.Parameters)
            {
                if (pDecl.IsString)
                    variableTypes[pDecl.Name.ToLower()] = BasicType.String;
                // `AS DOUBLE` 这类**内置类型名**也要登记（理由同上面 STRING 那条：
                // 不登记就走"无后缀 ⇒ Integer"默认，double 形参按 4 字节整数读）。
                // `ParamDeclaredType` 是唯一的类型判据，它认 `DeclaredType`。
                else if (!string.IsNullOrEmpty(pDecl.DeclaredType))
                    variableTypes[pDecl.Name.ToLower()] = ParamDeclaredType(pDecl);
                if (pDecl.IsArray && !string.IsNullOrEmpty(pDecl.TypeName))
                    dimAsVariables[pDecl.Name.ToLower()] = pDecl.TypeName.ToLower();
            }

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
            //   ⚠ 按各自**字节数**清零（8 字节的写两下）—— 与 `LocalVarOffset` 同源，
            //     免得留半个 double 在高位里。见 `DeclareLocal` 的说明。
            foreach (var kv in currentLocalVars)
            {
                int offset = LocalVarOffset(kv.Key);
                AddRI(OpCode.MOVE, 0, 0);
                // ⚠ **操作数顺序：`[内存] = R0` 才是"存"** —— 这里此前写成了
                //   `R0 = [内存]`（一个**读**），于是"把局部变量清零"**一次都没发生**：
                //   帧是复用的，读到的是上一次调用留下的残值（里面常躺着别处算出来的**地址**）。
                //   实测形态见 `vmlcli --trace-draw` 抓到的 GORILLA 坐标：
                //   `x`/`y` 读出的是"变量自己的地址"，因为它们的槽里就存着那个地址。
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, $"R12-{-offset}"), new Operand(OperandType.REGISTER, 0) }));
                if (LocalVarSize(kv.Key) >= 8)
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, $"R12-{-offset + 4}"), new Operand(OperandType.REGISTER, 0) }));
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
            ResetLocalVars();
            currentParamCount = funcDecl.Parameters.Count;

            // 形参声明为 STRING / 内置类型名（`AS DOUBLE`…）⇒ 登记进 `variableTypes`
            // （同 SUB 那处，理由见那里）
            foreach (var pDecl in funcDecl.Parameters)
            {
                if (pDecl.IsString)
                    variableTypes[pDecl.Name.ToLower()] = BasicType.String;
                else if (!string.IsNullOrEmpty(pDecl.DeclaredType))
                    variableTypes[pDecl.Name.ToLower()] = ParamDeclaredType(pDecl);
                if (pDecl.IsArray && !string.IsNullOrEmpty(pDecl.TypeName))
                    dimAsVariables[pDecl.Name.ToLower()] = pDecl.TypeName.ToLower();
            }

            // The function name is a special local variable for the return value
            DeclareLocal(funcDecl.Name.ToLower());

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
            //   ⚠ 与 SUB 序言**同一口径**（按字节数清零、地址走 `LocalVarOffset`）。
            foreach (var kv in currentLocalVars)
            {
                int offset = LocalVarOffset(kv.Key);
                AddRI(OpCode.MOVE, 0, 0);
                // ⚠ **操作数顺序：`[内存] = R0` 才是"存"** —— 这里此前写成了
                //   `R0 = [内存]`（一个**读**），于是"把局部变量清零"**一次都没发生**：
                //   帧是复用的，读到的是上一次调用留下的残值（里面常躺着别处算出来的**地址**）。
                //   实测形态见 `vmlcli --trace-draw` 抓到的 GORILLA 坐标：
                //   `x`/`y` 读出的是"变量自己的地址"，因为它们的槽里就存着那个地址。
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, $"R12-{-offset}"), new Operand(OperandType.REGISTER, 0) }));
                if (LocalVarSize(kv.Key) >= 8)
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, $"R12-{-offset + 4}"), new Operand(OperandType.REGISTER, 0) }));
            }

            // Generate body
            foreach (var stmt in funcDecl.Body)
            {
                CurrentSourceLine = stmt.Line; CurrentSourceColumn = stmt.Column;
                GenerateSubStatement(stmt);
            }

            // Load return value into R0 (the function name variable)
            int retValOffset = LocalVarOffset(funcDecl.Name.ToLower());
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"R12-{-retValOffset}") }));

            // Epilogue: LEAVE; RET
            instructions.Add(new Instruction(OpCode.LEAVE, new List<Operand>()));
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>()));

            currentSubName = null;
        }

        private bool IsStaticVariable(string varName)
        {
            if (currentSubName == null) return false;
            string scopedName = $"{SymbolKey(currentSubName)}__{varName.ToLower()}";
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
        ///
        /// ⚠ **例外见 <see cref="CollectLocalVariables"/> 的 `ForStatement` 分支**（循环计数器
        /// 不许跨 SUB 共享）—— 那一条**不**走本判据。
        /// </summary>
        private bool IsModuleVariable(string varName)
        {
            if (string.IsNullOrEmpty(varName)) return false;
            string key = varName.ToLower();
            if (currentSubName != null && key == SymbolKey(currentSubName)) return false;
            return variables.ContainsKey(key);
        }

        /// <summary>
        /// `DIM SHARED` 过的模块级变量（才允许**跨 SUB 共享一个循环计数器**）。
        /// 见 <see cref="CollectLocalVariables"/> 的 `ForStatement` 分支。
        /// </summary>
        private bool IsSharedVariable(string varName)
            => !string.IsNullOrEmpty(varName) && _sharedVariables.Contains(varName.ToLower());

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
                        DeclareLocal(ident.Name.ToLower());
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
                // ⚠ **循环计数器是个例外：不认"模块级变量"这一条，只有 `DIM SHARED` 才共享。**
                //
                // 判据若照抄下面那几条（`!IsModuleVariable`），代价是**两个毫不相干的 SUB
                // 会共用同一个循环计数器**：GORILLA.BAS 的模块体里（`GOSUB InitVars` 那段
                // 就写在顶层）有一个 `FOR i = 0 TO 8`，于是 `i` 成了模块级变量；接着
                // `PlayGame` 的 `FOR i = 1 TO NumGames`、`MakeCityScape` 的
                // `FOR i = BHeight - 3 TO 7`、`VictoryDance` 的 `FOR i# = 1 TO 4`
                // **全都是同一个 `i`** —— 内层循环跑完把外层的计数冲成内层的收尾值，
                // 外层循环当场提前退出。实测：第一局命中之后 `PlayGame` 的 `i` 已经是 3
                // ⇒ `FOR i = 1 TO 3` 第一局就结束（比分只来得及 +1 就进了 GAME OVER 画面）。
                //
                // 为什么这里可以对"模块级变量"破例：**循环计数器是循环自己的实现细节**，
                // 把它当跨 SUB 的共享状态从来不是程序的本意；而"SUB 读模块级标量"那条
                // （`t4` / `tetris.bas` 依赖的语义）**不受影响** —— 本分支只决定
                // 「`FOR x` 的那个 `x` 用哪个槽」，SUB 里其它地方读 `x` 时仍然按
                // 模块级变量走（`IsModuleVariable` 一个字没改）。
                // 想真的共享计数器就写 `DIM SHARED k`（这时 `IsSharedVariable` 为真，不建局部）。
                //
                // 最小复现（顶层 FOR + SUB 内同名 FOR）：
                //     FOR i = 1 TO 3 : PRINT "outer"; i : CALL S : NEXT : END
                //     SUB S : FOR i = 1 TO 2 : PRINT " i="; i : NEXT : END SUB
                // 修前只打一轮外层就退出，修后 3 轮各带 2 次内层。
                if (!currentLocalVars.ContainsKey(forStmt.Variable.Name.ToLower()) &&
                    !IsParameter(forStmt.Variable.Name.ToLower()) &&
                    !IsStaticVariable(forStmt.Variable.Name.ToLower()) &&
                    !IsSharedVariable(forStmt.Variable.Name))
                {
                    DeclareLocal(forStmt.Variable.Name.ToLower());
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
                    // UDT 数组：元素占整个记录的字节数（与模块级那处同一口径，见 ArrayElementSlots）。
                    // GORILLA 的 `DIM BCoor(0 TO 30) AS XYPoint` 写在 `SUB PlayGame` 里，走的就是这条。
                    int elemSlots = ArrayElementSlots(dimStmt.TypeName);
                    arrayVariables[dimStmt.VariableName] = new ArrayInfo
                    {
                        Size = arrSize,
                        Offset = variableCount,
                        ElementBytes = elemSlots * 4,
                        Dimensions = dimStmt.Dimensions.Count > 0
                            ? new List<int>(dimStmt.Dimensions)
                            : new List<int> { arrSize },
                        LowerBounds = dimStmt.LowerBounds.Count > 0
                            ? new List<int>(dimStmt.LowerBounds)
                            : new List<int> { 0 }
                    };
                    for (int i = 0; i < arrSize; i++)
                    {
                        string elementName = $"{dimStmt.VariableName}({i})";
                        for (int s = 0; s < elemSlots; s++)
                        {
                            string slotName = s == 0 ? elementName : $"{elementName}_slot_{s}";
                            if (!variables.ContainsKey(slotName))
                            {
                                variables[slotName] = variableCount++;
                                varByteSizes[slotName] = 4;
                            }
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
                        DeclareLocal(varName);
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
                    string scopedName = $"{SymbolKey(currentSubName)}__{varName}";
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
                        {
                            // UDT 局部量按 **4 字节槽**排（字段偏移以 4 字节为单位），
                            // 所以这里**不走 `DeclareLocal`**（那个按类型可能给 8 字节）。
                            // ⚠ 大小仍要在这里记下 —— 地址走 `LocalVarOffset`，两处必须一致。
                            currentLocalVars[slotName] = currentLocalVarCount;
                            localVarSizes[slotName] = 4;
                            currentLocalVarCount++;
                        }
                    }
                }
                else if (!currentLocalVars.ContainsKey(varName) && !IsParameter(varName))
                {
                    DeclareLocal(varName);
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
            if (subMap.ContainsKey(SymbolKey(currentSubName)))
            {
                var sub = subMap[SymbolKey(currentSubName)];
                foreach (var p in sub.Parameters)
                    if (p.Name.ToLower() == name) return true;
            }
            if (funcMap.ContainsKey(SymbolKey(currentSubName)))
            {
                var func = funcMap[SymbolKey(currentSubName)];
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
                    DeclareLocal(ident.Name.ToLower());
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
                        int offset = LocalVarOffset(varName);
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
                    int offset = LocalVarOffset(varName);
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.MEMORY, $"R12-{-offset}") }));
                }
                else
                {
                    int paramIdx = FindParameterIndex(varName);
                    if (paramIdx >= 0)
                    {
                        EmitParamLoad(paramIdx, OpCode.MOVE, 1);
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

            /* ⚠ 存目标的**类型**才决定搬运指令：`MOVEF` 只搬单精度那一组，
               **双精度必须用 `MOVED`**（D 寄存器组）。这里原来一律 `MOVEF`，
               于是 `SclX# = ScrWidth / 320`（`/` 在 BASIC 里是整数除？不 —— 是整数表达式的
               结果是整数 ⇒ `isFloat` 为假 ⇒ `MOVE`）写进去的是**整数位型**，
               而读侧按变量类型走 `MOVED` ⇒ 读出来是垃圾。
               GORILLA.BAS 的 `FUNCTION Scl (n!) = CINT(n! / 2 * SclX#)` 就靠它算
               太阳半径/大猩猩肢体尺寸 —— 实测太阳半径成了天文数字，
               紧接着 `PAINT (x,y), SUNATTR` 从圆心灌满了**整个屏幕**。
               （模块级那条路走 `EmitStoreVar` → 按类型选，本来是好的；
                 这里是 SUB/局部/形参那条，漏了。） */
            if (stmt.Variable is Identifier tid
                && !(currentSubName != null && SymbolKey(tid.Name) == SymbolKey(currentSubName)))
            {
                // ⚠ **函数的返回值变量除外**：`FUNCTION GetNum#` 的返回槽就是那个
                //   与函数同名的局部量，而本前端的函数返回约定是**整数**（`R0`，
                //   见 `GenerateFunctionDeclaration` 的尾声）。按名字后缀把它当双精度存
                //   （MOVED + I2D）会让尾声的整数读拿到半截位型 —— 实测直接跑飞。
                //   双精度返回值本前端不支持（如实如此，不是这里能顺手补的）。
                BasicType destT = GetVariableType(tid.Name);
                if (destT == BasicType.Double || destT == BasicType.Long)
                {
                    // 目标 64 位浮点：搬运用 MOVED，而且**整数值必须先转成双精度**
                    // （`SclX# = 640 / 320` 的右边是整数 2 —— 直接把 2 的位型交给 MOVED
                    //  写进去的是 3e-323，后面按双精度读出来就是垃圾）。
                    storeOp = OpCode.MOVED;
                    if (exprType == BasicType.Single)
                        instructions.Add(new Instruction(OpCode.F2D, new List<Operand> { new Operand(OperandType.REGISTER, srcReg), new Operand(OperandType.REGISTER, srcReg) }));
                    else if (exprType != BasicType.Double && exprType != BasicType.Long)
                        instructions.Add(new Instruction(OpCode.I2D, new List<Operand> { new Operand(OperandType.REGISTER, srcReg), new Operand(OperandType.REGISTER, srcReg) }));
                }
            }
            else if (stmt.Variable is Identifier fname
                     && currentSubName != null && SymbolKey(fname.Name) == SymbolKey(currentSubName))
            {
                /* **函数的返回值槽**：本前端的函数返回约定是**整数**（尾声 `MOVE R0, [槽]`，
                   调用方把 R0 当整数用），所以浮点表达式必须**显式转回整数**再存。

                   ⚠ 原来这里什么都不做 ⇒ `storeOp` 停在 `isFloat ? MOVEF : MOVE`，
                   而值在 R0 里是**双精度位型** —— `P3 = a#`（a# = 45.0）用 `MOVEF`
                   存进去的是 double 45.0 的**低半字**（= 0）。
                   实测 `FUNCTION P3 (a#) / P3 = a#` 恒返回 0、`PRINT P3(45.5)` 打出 0，
                   而**同样签名换成 SUB 就正常** —— 这就是 GORILLA 的 `PlotShot` 一类
                   `FUNCTION` 在调用方读到 0 / 读到地址的根因。

                   （`FUNCTION GetNum#` 那种"双精度返回值"本前端不支持 —— 返回值就是 R0
                     里一个整数，如实如此，不是这里能顺手补的。） */
                if (exprType == BasicType.Double || exprType == BasicType.Long)
                    instructions.Add(new Instruction(OpCode.D2I, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0) }));
                else if (exprType == BasicType.Single)
                    instructions.Add(new Instruction(OpCode.F2I, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 0) }));
                storeOp = OpCode.MOVE;
                srcReg = 0;
            }

            if (stmt.Variable is Identifier ident)
            {
                // 记录浮点变量的类型（让后续的读用 MOVEF/MOVED 而不是 MOVE）。
                //
                // ⚠ 三个条件缺一不可，这里与**模块级那处**（`Statements.cs` 的
                //   `GenerateLetStatement`）是同一口径：
                //     · `isFloat`    —— 只有浮点表达式才提升类型；
                //     · `!ContainsKey` —— 已经记过的不覆盖；
                //     · `!HasExplicitDefType` —— **`gravity#` 这类写了后缀的不能被改写**。
                //   少了第三条的后果不是"精度差一点"：`gravity# = VAL(grav$)`（VAL 返回 Single）
                //   会把 `gravity#` 的类型从 Double 改成 Single ⇒ 它的字节数 8→4 ⇒
                //   **它之后所有全局变量的偏移全少 4**，而主程序那份地址早已编进指令里
                //   ⇒ 主程序与 SUB 对"全局变量在哪"各执一词。实测 GORILLA.BAS：
                //   `Mode` 在主程序里读 `#21988`、在 `MakeCityScape` 里读 `#21984`（别人的槽）
                //   ⇒ `IF Mode = 9` 走 else 分支 ⇒ `BottomLine` 335→190、`HtInc` 10→6
                //   ⇒ 整座城市画到屏幕外，**一个错都不报**。
                string varName = ident.Name.ToLower();
                // ⚠ **函数名不参与类型提升**：它的槽是**整数**返回槽（见上面那段），
                //   记成 Double 会让别处按 8 字节读它（而槽只有 4 字节）。
                bool isFuncReturnSlot = currentSubName != null && SymbolKey(ident.Name) == SymbolKey(currentSubName);
                if (isFloat && !isFuncReturnSlot && !variableTypes.ContainsKey(varName) && !HasExplicitDefType(varName))
                    variableTypes[varName] = exprType;

                // Try local variable first
                if (currentLocalVars.ContainsKey(varName))
                {
                    int offset = LocalVarOffset(varName);
                    instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12-{-offset}"), new Operand(OperandType.REGISTER, srcReg) }));
                }
                else
                {
                    // Check if it's a parameter
                    int paramIdx = FindParameterIndex(ident.Name.ToLower());
                    if (paramIdx >= 0)
                    {
                        // 写形参走唯一口径：地址格解引用后写（BYREF 的回流语义）、内联槽直接写
                        EmitParamStore(paramIdx, ident.Name, srcReg);
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
                        // 类型未知 ⇒ 按字段偏移 0 写（与模块级那处同一兜底）
                        EmitFieldStore(fieldAccess.RecordExpression, 0, srcReg, storeOp);
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

                    // ⚠ 这里从前是**一条读指令**（`MOVE R1, [R3]`）：SUB 体内的
                    //   `BCoor(i).XCoor = x` 除了算出个地址之外什么都没干，值根本没写进去
                    //   —— 而 `b(i).F = v` 是 GORILLA 建整座城市最核心的一句
                    //   （`MakeCityScape` 的每座楼都要写 XCoor/YCoor），写不进去 =
                    //   "跑到 PlayGame 但画面只有底色"。
                    //   正解：**取地址**（不是取值，见 `EmitRecordBaseAddr`）+ 按 storeOp 往里写。
                    EmitFieldStore(fieldAccess.RecordExpression, fOff, srcReg, storeOp);
                    return;
                }

                string recordName = fieldAccess.RecordName.ToLower();

                if (!dimAsVariables.ContainsKey(recordName) || !typeDefinitions.ContainsKey(dimAsVariables[recordName]))
                {
                    // Unknown type — treat field access as plain var access (offset 0)
                    if (currentLocalVars.ContainsKey(recordName))
                    {
                        int baseOffset = LocalVarOffset(recordName);
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
                    int baseOffset = LocalVarOffset(recordName);
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
            //
            // ⚠ **返回值只在"紧接着的那一条访问指令"之前有效** —— 模块级变量那一路是把地址
            //   **算进 R3**（`EmitStaticAddr` 只会算到寄存器，不会返回一个稳定的地址串），
            //   而 R3 是本前端最常用的临时寄存器（数组赋值把值先落 R3、图形/数组码到处在用）。
            //   所以**不要**把它存进一个变量、夹几条别的语句之后再用 —— 那读/写的是 R3 里的残留值。
            //   实测（GORILLA.BAS）：`SUB PlaceGorillas` 里 `FOR i = 1 TO 2` 的 `i` 是模块级变量，
            //   循环体跑完再用这句地址去读循环变量 ⇒ 地址是循环体留下的垃圾（浮点位型），
            //   当场「内存错误(PC=…): MOVE @R0, @3 — 地址=C00000xx」，且**每次跑值都不同**。
            //   要"算完再用"必须**重新调用一次**，或者用下面那两个组合助手。
            string GetVarAddr()
            {
                if (currentLocalVars.ContainsKey(varName))
                {
                    return LocalVarMemRef(varName);
                }
                if (IsStaticVariable(varName))
                {
                    string staticName = $"{SymbolKey(currentSubName)}__{varName}";
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
                    // 地址算进 R3（理由与"只能紧接着用"的约束见上面那段）。
                    EmitStaticAddr(3, STATIC_GLOBALS_OFFSET + GetVarByteOffset(varName));
                    return "R3";
                }
                throw new CompilationException(ErrorCode.CodeGen_UndefinedVariable, $"FOR 变量 '{stmt.Variable.Name}' 未定义");
            }

            // 「算地址 + 立刻访问」的**组合助手** —— 循环变量的读/写一律走它们。
            //
            // 把两者绑在一起，是为了让上面那条约束**不可能被后来的改动破坏**：
            // 单独调 `GetVarAddr()` 之后随手插一条别的语句，就会踩回那个崩溃。
            void EmitLoadLoopVarToR0()
            {
                string addr = GetVarAddr();
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, addr) }));
            }
            void EmitStoreR0ToLoopVar()
            {
                string addr = GetVarAddr();
                // ⚠ 操作数顺序：MOVE 是 **dest-first**（`move [mem], reg` 是"存"、`move reg, [mem]` 是"取"）。
                //   这里原来写成 [REGISTER 0, MEMORY addr] —— 那是**取**不是存（`move R0 [R12-4]`），
                //   于是 `FOR i = 0 TO 3` 的初值根本没写进去；自增那处同样写反 ⇒ 循环变量永不推进，
                //   实测就是"10 秒跑 10 亿条指令"的死循环（t10）。
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.MEMORY, addr), new Operand(OperandType.REGISTER, 0) }));
            }

            GenerateSubExpression(stmt.InitialValue, 0);
            EmitStoreR0ToLoopVar();

            instructions.Add(new Instruction(OpCode.LABEL, new List<Operand> { new Operand(OperandType.LABEL, loopLabel) }));

            Sta.PushLoopLabels(endLabel, loopLabel);

            // Load variable and compare with end value
            EmitLoadLoopVarToR0();
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
            EmitLoadLoopVarToR0();
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));

            foreach (var bodyStmt in stmt.Body)
                GenerateSubStatement(bodyStmt);

            instructions.Add(new Instruction(OpCode.POP, new List<Operand> { new Operand(OperandType.REGISTER, 0) }));
            // ⚠ 这里曾经复用循环体**之前**取回的地址（模块级变量 ⇒ 那是"R3"）——
            //   而循环体整个跑在中间，R3 早被踩烂。这行本身就是冗余的（自增第一句又会重新读），
            //   但**冗余不等于可以不安全**：它会以垃圾为地址读 4 字节，直接崩。
            EmitLoadLoopVarToR0();

            // Increment
            EmitLoadLoopVarToR0();
            GenerateSubExpression(stmt.StepValue, 1);
            instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1) }));
            // ⚠ 同理：step 求值（`GenerateSubExpression`）可能改掉 R3，存回去之前必须**重算地址**。
            EmitStoreR0ToLoopVar();

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
            // ⚠ **`TIMER` / `DATE$` / `TIME$` 必须与顶层那条路一样有三档分支**。
            //
            //   它们此前**只**挂在 `GenerateExpression`（顶层表达式）上，而 SUB/FUNCTION 体内
            //   走的是本方法 ⇒ 落到方法末尾那个**没有 else 的兜底**（静默什么都不生成），
            //   `reg` 里留着上一条语句的残值 —— 不报错、不崩，只是值不对。
            //
            //   实测（GORILLA.BAS 的 `SUB Rest (t#)`）：`s# = TIMER` 编成 `R1 = R0`
            //   （R0 是刚清零的局部量），`TIMER - s#` 于是恒为常数、`LOOP UNTIL` 永不成立
            //   —— 转屏/等待全变成死循环；`PlotShot` 里同一个 `Rest` 就在主循环里。
            //
            //   判据与 Inkey 同源：**这一类"没有参数的内置函数"在两条路上必须各有一档**，
            //   加新内置函数时两处一起加（`GenerateExpression` 的那一组是清单）。
            else if (expr is TimerFunctionExpression)
            {
                GenerateTimerFunction(reg);
            }
            else if (expr is DateFunctionExpression)
            {
                GenerateDateFunction(reg);
            }
            else if (expr is TimeFunctionExpression)
            {
                GenerateTimeFunction(reg);
            }
            else if (expr is NumberLiteral numLiteral)
            {
                // ⚠ 带小数点的字面量必须**发 MOVEF（浮点立即数）**，原来一律
                //   `MOVE reg, #(int)值` —— `0.5` 被截成 `0`。于是 SUB 体内
                //   `INT(0.5 * 1000)` 得 **0**（该 500）、任何浮点常量在 SUB 里都是 0，
                //   而且**不报错**。判据与顶层（`CodeGenerator.Expressions.cs` 的
                //   NumberLiteral 分支）逐字一致：整数放得下走 MOVE，否则走 MOVEF。
                double litValue = numLiteral.Value;
                if (litValue == (int)litValue && litValue >= int.MinValue && litValue <= int.MaxValue)
                    AddRI(OpCode.MOVE, reg, (int)litValue);
                else
                {
                    instructions.Add(new Instruction(OpCode.MOVEF, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, (float)litValue) }));
                    _lastExprFloatType = BasicType.Single;
                }
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
                    // ⚠ **按类型取指令**（`MOVED`/`MOVEL`/`MOVEF`/`MOVE`），与写回那条路
                    //   （`GenerateSubVariableStore` 的 `GetStoreInstruction`）**对称**。
                    //   这里原来是死写 `MOVE`（4 字节），而双精度局部量是用 `MOVED` 写的
                    //   8 字节 ⇒ 读回来只剩低半字（1.5 的低 4 字节 = 0），
                    //   于是 `S# = S# + T2#` 恒等于"加 0"。实测 `.scratch/gor/mre_dcmp.bas`：
                    //   `SUB` 里 `A# = 1.5 + 2.25` 判成 `A# <= 3.5`。
                    string localName = ident.Name.ToLower();
                    int offset = LocalVarOffset(localName);
                    instructions.Add(new Instruction(GetLoadInstruction(GetVariableType(localName)), new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R12-{-offset}") }));
                    BasicType lt = GetVariableType(localName);
                    if (lt == BasicType.Single || lt == BasicType.Double) _lastExprFloatType = lt;
                }
                else
                {
                    // Try parameter (positive offsets from BP: BP+8, BP+12, ...)
                    int paramIdx = FindParameterIndex(ident.Name.ToLower());
                    if (paramIdx >= 0)
                    {
                        // 取值要**按形参类型**（MOVEF/MOVED）—— 硬写 `MOVE` 会把
                        // `FUNCTION Scl (n!)` 里的 `n!` 当整数读（实参 12 的位型按单精度
                        // 解释是 1.7e-44 ⇒ 算出来是 0 且不报错）。GORILLA 的太阳半径靠它。
                        // 「槽里是地址还是值」走唯一口径（BYREF **或 8 字节类型**）。
                        EmitParamLoad(paramIdx, GetLoadInstruction(GetVariableType(ident.Name)), reg);
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

                // 字符串比较**同样先分叉**（同一份实现，见 GenerateStringComparison）——
                // 落到下面会走 `CMP` 比**指针**（未初始化变量是 0、字面量 `""` 是地址 ⇒
                // `Char$ = ""` 恒假 ⇒ 老 BASIC 那句 `DO WHILE Char$ = "": …: LOOP` 卡死）。
                if (GenerateStringComparison(binary, reg)) return;

                // ⚠ 运算符要**大小写无关**地比：操作数文本取自 token 的 `Value`（保留源码大小写），
                //   所以 `mod` / `Mod` / `MOD` 是三个不同的字符串。顶层那套按 `"MOD"` 精确比，
                //   小写 mod 同样编不出代码（同一个坑，只是这边顺手一起兜住）。
                string op = binary.Operator.ToUpperInvariant();

                /* ── 浮点分岔（v0.96.4xx）─────────────────────────────────────────
                   SUB/FUNCTION 体内的算术**原来只有整数一条路**：`RND(1) * x`、
                   `COS(a) * v` 这类表达式会把**浮点位型当整数去乘**，结果是无意义的
                   大数而且**一个错都不报**（实测 `DEF FnRan (x) = INT(RND(1) * x) + 1`
                   —— 老 BASIC 里"取 1..x 随机数"的通用写法 —— 恒等于 1）。

                   判据与顶层**完全同源**（`InferExpressionType` + `WidenType`），
                   寄存器也沿用这条路的 R1/R2 约定（浮点寄存器与整数寄存器共用编号，
                   `SetFloatValue` 会同步 `registers[]`，所以 `PUSH R1/POP R2` 对浮点同样有效）。

                   只接 **Single**：Double 走的是另一组寄存器（D0-D7）与另一套转换
                   （`I2D`/`D2F`），混进来会把两套约定搅在一起 —— 那种表达式**维持原样**
                   （它本来也是坏的，只是不归这一次改）。`\`/MOD/AND/OR/^ 按 BASIC 语义
                   就是整数运算 ⇒ 也走原路。 */
                ExpType subLeftT = InferExpType(binary.Left);
                ExpType subRightT = InferExpType(binary.Right);
                ExpType subResultT = ExpressionManager.WidenType(subLeftT, subRightT);
                bool subDouble = subResultT.IsDouble();
                bool subFloat = (subResultT.IsFloat() || subDouble)
                    && op is "+" or "-" or "*" or "/" or "=" or "<>" or "<" or "<=" or ">" or ">=";

                // 左值要跨过"右操作数的求值"活下来。整数路用 PUSH/POP，浮点路必须用
                // **FPUSH/FPOP**：运行时 `ExecutePop` 只写 `registers[]`、
                // **不同步 `floatRegisters[]`**（同步只在 `SetFloatValue` 里做，即反向那半边），
                // 于是 `POP R2` 之后 `MOVEF F0,F2` 读到的是**上一次留在 F2 里的旧值**
                // —— 实测 `INT(0.5 * 1000)` 在 SUB 里恒为 0。FPUSH/FPOP 走的是
                // `GetFloatValue`/`SetFloatValue`，两头都是浮点语义，没有这个缺口。
                OpCode pushOp = subDouble ? OpCode.DPUSH : subFloat ? OpCode.FPUSH : OpCode.PUSH;
                OpCode popOp = subDouble ? OpCode.DPOP : subFloat ? OpCode.FPOP : OpCode.POP;

                // 操作数先统一到结果类型（整数 → I2F/I2D，单精度 → F2D）
                EmitSubOperand(binary.Left, subLeftT, subDouble, subFloat);
                instructions.Add(new Instruction(pushOp, new List<Operand> { new Operand(OperandType.REGISTER, 1) }));
                EmitSubOperand(binary.Right, subRightT, subDouble, subFloat);
                instructions.Add(new Instruction(popOp, new List<Operand> { new Operand(OperandType.REGISTER, 2) }));

                // 算术与位运算：都满足 `reg = 左 OP 右`
                OpCode? arithOp = op switch
                {
                    "+" => subDouble ? OpCode.DADD : subFloat ? OpCode.FADD : OpCode.ADD,
                    "-" => subDouble ? OpCode.DSUB : subFloat ? OpCode.FSUB : OpCode.SUB,
                    "*" => subDouble ? OpCode.DMUL : subFloat ? OpCode.FMUL : OpCode.MUL,
                    "/" => subDouble ? OpCode.DDIV : subFloat ? OpCode.FDIV : OpCode.DIV,
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
                    if (subFloat)
                    {
                        /* ⚠ 浮点算术**必须用三操作数形式** `FOP dst, a, b`（Double 同理，用 DPUSH/DPOP 与 D 组指令）：运行时
                           `ExecuteFadd/Fsub/Fmul` 开头就是 `if (operands.Count < 3) return;`
                           —— **两操作数形式是个静默空操作**。整数那边
                           （`ExecuteAdd`）两种形式都认，所以照抄整数路的
                           `MOVEF reg,R2` + `FMUL reg,R1` 形状不会报错、只是"什么也没算"：
                           实测 `INT(0.5 * 1000)` 在 SUB 里恒为 0（顶层同一条是 500）。
                           左值在 R2、右值在 R1 ⇒ `FOP reg, R2, R1`。 */
                        OpCode fop = arithOp.Value;
                        if (reg == 1)
                        {
                            // reg == 1 就是**右操作数**的寄存器，直接写它会自毁 ⇒ 先做进 R2 再搬
                            instructions.Add(new Instruction(fop, new List<Operand> { Reg(2), Reg(2), Reg(1) }));
                            instructions.Add(new Instruction(subDouble ? OpCode.MOVED : OpCode.MOVEF, new List<Operand> { Reg(1), Reg(2) }));
                        }
                        else
                        {
                            instructions.Add(new Instruction(fop, new List<Operand> { Reg(reg), Reg(2), Reg(1) }));
                        }
                    }
                    else if (reg == 1)
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
                    AddRR(subDouble ? OpCode.DCMP : subFloat ? OpCode.FCMP : OpCode.CMP, 2, 1);
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
                GenerateArrayAccess(arrayAccess, reg);
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
            
            if (subMap.ContainsKey(SymbolKey(currentSubName)))
            {
                var sub = subMap[SymbolKey(currentSubName)];
                for (int i = 0; i < sub.Parameters.Count; i++)
                {
                    if (sub.Parameters[i].Name.ToLower() == paramName)
                        return i;
                }
            }
            if (funcMap.ContainsKey(SymbolKey(currentSubName)))
            {
                var func = funcMap[SymbolKey(currentSubName)];
                for (int i = 0; i < func.Parameters.Count; i++)
                {
                    if (func.Parameters[i].Name.ToLower() == paramName)
                        return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 当前 SUB/FUNCTION 里第 <paramref name="idx"/> 个形参的声明（没有就 null）。
        ///
        /// <para>与 <see cref="FindParameterIndex"/> 是一对：一个由名字找下标、一个由下标找声明。
        /// 数组形参的判据（`IsArray`）与类型名（`TypeName`）都只在这份声明上，
        /// 而代码生成两头都要用（元素寻址要判"是不是数组形参"，字段访问要类型）。</para>
        /// </summary>
        private ParameterNode FindParameterDecl(int idx)
        {
            if (currentSubName == null || idx < 0) return null;
            string key = SymbolKey(currentSubName);
            if (subMap.TryGetValue(key, out var sub) && idx < sub.Parameters.Count)
                return sub.Parameters[idx];
            if (funcMap.TryGetValue(key, out var func) && idx < func.Parameters.Count)
                return func.Parameters[idx];
            return null;
        }

        /* ───────────────── 实参区（参数传递）的**唯一口径** ─────────────────

           约定（调用方 `EmitCallArguments` 与这里**共用同一份判据**）：

             · 实参区**一格 4 字节、一行一格**，形参 i 在 `R12 + 8 + 4i`。
             · 槽里放什么由 <see cref="ParamSlotHoldsAddress"/> 决定：
                 声明的 **BYREF**，或者形参类型是 **8 字节**（Double/Long）⇒ 槽里放**地址**；
                 其余 ⇒ 槽里放**内联值**。

           **为什么 8 字节也要放地址**：一格只有 4 字节，double 塞不下。早先的做法是
           "调用方一律压一个值、被调方按 `GetVarByteOffset` 给 Double 算 8 字节" ——
           同一处布局两套算法，于是 `CALL S3(45.5)` 读到隔壁槽的垃圾、`FUNCTION P3(a#)`
           拿到半个 double。统一成"放地址"之后，**所有槽都是 4 字节**，
           调用方与被调方对布局只有一个说法。

           这一组函数是**全前端唯一实现**：下面各处（取值 / 存值 / 取地址 / INPUT）
           一律走它们，别再手算 `8 + paramIdx * 4`（本仓头号坑：同一规则两处实现，
           一有 8 字节类型就漂）。 */

        /// <summary>形参 <paramref name="idx"/> 在实参区里的槽偏移（相对 R12）。</summary>
        private int ParamSlotOffset(int idx) => 8 + idx * 4;

        /// <summary>形参 <paramref name="idx"/> 的类型是不是 8 字节（Double/Long）。</summary>
        private bool ParamIsWide(int idx)
        {
            var d = FindParameterDecl(idx);
            return d != null && IsWideType(ParamDeclaredType(d));
        }

        /// <summary>形参 <paramref name="idx"/> 的槽里放的是**地址**还是**内联值**。</summary>
        private bool ParamSlotHoldsAddress(int idx)
        {
            var d = FindParameterDecl(idx);
            if (d == null) return false;
            return d.IsByRef || IsWideType(ParamDeclaredType(d));
        }

        /// <summary>8 字节类型（一格装不下 ⇒ 与 BYREF 同形传地址）。</summary>
        private static bool IsWideType(BasicType t) => t == BasicType.Double || t == BasicType.Long;

        /// <summary>
        /// 读形参 <paramref name="idx"/> 到 <paramref name="reg"/>。
        /// 取值指令按**声明类型**选（`MOVEF`/`MOVED`/`MOVE`）—— 硬写 `MOVE` 会把
        /// `FUNCTION Scl (n!)` 里的 `n!` 当整数读（实参 12 的位型按单精度解释是 1.7e-44）。
        /// </summary>
        private void EmitParamLoad(int idx, string paramName, int reg)
        {
            EmitParamLoad(idx, GetLoadInstruction(GetVariableType(paramName)), reg);
        }

        /// <summary>同上，但由调用方给定取值指令（声明类型已算好的场合）。</summary>
        private void EmitParamLoad(int idx, OpCode loadOp, int reg)
        {
            int off = ParamSlotOffset(idx);
            if (ParamSlotHoldsAddress(idx))
            {
                // 槽里是地址：先取地址、再按宽度解引用
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R12+{off}") }));
                instructions.Add(new Instruction(loadOp, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R{reg}") }));
            }
            else
            {
                instructions.Add(new Instruction(loadOp, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R12+{off}") }));
            }
        }

        /// <summary>
        /// 把 <paramref name="srcReg"/> 写进形参 <paramref name="idx"/>。
        /// 内联槽直接写；地址格**解引用后写**（写入调用方的那个变量 —— BYREF 的回流语义，
        /// QBasic 的 `SUB GetInputs (…, NumGames)` 靠的就是它）。
        /// </summary>
        private void EmitParamStore(int idx, string paramName, int srcReg)
        {
            int off = ParamSlotOffset(idx);
            OpCode storeOp = GetStoreInstruction(GetVariableType(paramName));
            if (ParamSlotHoldsAddress(idx))
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, 2), new Operand(OperandType.MEMORY, $"R12+{off}") }));
                instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, "R2"), new Operand(OperandType.REGISTER, srcReg) }));
            }
            else
            {
                instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12+{off}"), new Operand(OperandType.REGISTER, srcReg) }));
            }
        }

        /// <summary>
        /// 取形参 <paramref name="idx"/> **本身的地址**（`&x`，用于把它再 BYREF 传下去）。
        ///
        /// <para>地址格（BYREF / 8 字节）里存的**就是**那个地址 ⇒ 取出来即可；
        /// 内联槽（4 字节 BYVAL）里存的是值 ⇒ 地址就是槽本身。
        /// 早先一律返回槽地址，于是 `SUB A (x) / CALL B(x)` 的转手把
        /// **A 的槽**交给了 B —— B 写进去、A 读到的却是另一个地址，全程不报错。</para>
        /// </summary>
        private void EmitParamAddress(int idx, int reg)
        {
            int off = ParamSlotOffset(idx);
            if (ParamSlotHoldsAddress(idx))
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.MEMORY, $"R12+{off}") }));
            }
            else
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, off) }));
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 12) }));
            }
        }

        /// <summary>
        /// 生成变量地址到指定寄存器
        /// </summary>
        private void GenerateVariableAddress(Expression expr, int reg)
        {
            // `arr()`（整个数组）当实参 —— 传出去的是**数组基址**。
            //
            // 判据在解析期就定下了（`ArrayAccessExpression.IsWholeArray`），这里只管取地址；
            // 与标量 BYREF 实参**同一个约定**（槽里放"实参的地址"），所以被调方那边
            // 数组形参的读法就是"槽里的值 = 基址"（见 `GenerateArrayElementAddr`）。
            if (expr is ArrayAccessExpression wholeArray && wholeArray.IsWholeArray)
            {
                if (!GenerateArrayBaseAddr(wholeArray.ArrayName, reg))
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, 0) }));
                }
                return;
            }

            // `arr(i)`（数组元素）当实参 —— 元素地址本身就能当左值（QBasic 的 BYREF 语义），
            // `SUB t(a) / a = 5` + `t arr(1)` 必须把 arr(1) 改掉。
            if (expr is ArrayAccessExpression elem && !elem.IsWholeArray)
            {
                if (GenerateArrayElementAddr(elem, reg, addressOnly: true)) return;
            }

            // `记录.字段` 当实参（`pts(2).XCoor`）：字段地址同样是左值。
            if (expr is FieldAccessExpression fa)
            {
                int fieldOff = ResolveFieldOffset(fa);
                if (fieldOff >= 0)
                {
                    EmitRecordBaseAddr(fa.RecordExpression, reg);
                    if (fieldOff != 0)
                    {
                        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> {
                            new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, fieldOff) }));
                    }
                    return;
                }
            }


            if (expr is Identifier ident)
            {
                // 在SUB/FUNCTION内部
                if (currentSubName != null)
                {
                    // 检查是否为局部变量
                    if (currentLocalVars.ContainsKey(ident.Name.ToLower()))
                    {
                        int offset = LocalVarOffset(ident.Name.ToLower());
                        // 计算局部变量地址: R12 + offset
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.IMMEDIATE, offset) }));
                        instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, reg), new Operand(OperandType.REGISTER, 12) }));
                    }
                    // 检查是否为参数
                    else if (FindParameterIndex(ident.Name.ToLower()) >= 0)
                    {
                        // `&形参` 走唯一口径（地址格里存的就是地址、内联槽的地址就是槽本身）。
                        EmitParamAddress(FindParameterIndex(ident.Name.ToLower()), reg);
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
                int offset = LocalVarOffset(varName);
                BasicType varType = GetVariableType(varName);
                OpCode storeOp = GetStoreInstruction(varType);
                instructions.Add(new Instruction(storeOp, new List<Operand> { new Operand(OperandType.MEMORY, $"R12-{-offset}"), new Operand(OperandType.REGISTER, valueReg) }));
            }
            else
            {
                int paramIdx = FindParameterIndex(varName);
                if (paramIdx >= 0)
                {
                    EmitParamStore(paramIdx, varName, valueReg);
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

            // native SUB/FUNCTION: 使用裸名 CALL (无 sub_/func_ 前缀)，**且保留声明处的大小写**（外部符号）
            bool isNative = (subDecl?.IsNative ?? false) || (funcDecl?.IsNative ?? false);
            string nativeName = subDecl?.Name ?? funcDecl?.Name ?? stmt.SubName;
            string subLabel = isNative
                ? NativeLabel(nativeName)
                : (subDecl != null ? SubLabel(stmt.SubName) : FunctionLabel(stmt.SubName));

            // BYREF 判据对 SUB / FUNCTION 两种声明都成立
            int declParamCount = subDecl?.Parameters.Count ?? funcDecl?.Parameters.Count ?? 0;
            bool IsByRefAt(int i) => i < declParamCount &&
                (subDecl != null ? subDecl.Parameters[i].IsByRef : funcDecl!.Parameters[i].IsByRef);

            // Push arguments (right-to-left)
            int argBytes = EmitCallArguments(stmt.Arguments, IsByRefAt, currentSubName != null,
                i => declParamCount > i
                    ? ParamDeclaredType(subDecl != null ? subDecl.Parameters[i] : funcDecl!.Parameters[i])
                    : BasicType.Integer);

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
            if (argBytes > 0)
            {
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, argBytes) }));
            }
        }

        /// <summary>
        /// 一次调用的实参发射 —— **三条调用路径共用这一份实现**
        /// （语句位 `CALL x` / 表达式里的 `f(…)` / SUB 体内表达式里的 `f(…)`）。
        /// 各写一份必然漂移，本仓已经吃过这个亏。
        /// </summary>
        /// <param name="isByRefAt">形参 i 是不是 BYREF（NATIVE 声明已在 CodeGenerator 里被强制 BYVAL）。</param>
        /// <param name="subScope">当前是否在 SUB/FUNCTION 体内（决定用 GenerateSubExpression 还是 GenerateExpression）。</param>
        /// <returns>调用方要清掉的栈字节数。</returns>
        private int EmitCallArguments(IReadOnlyList<Expression> args, Func<int, bool> isByRefAt, bool subScope,
                                      Func<int, BasicType> paramTypeAt = null)
        {
            int n = args.Count;

            /* 实参区的口径（与被调方共用，见 `ParamSlotHoldsAddress` 那段）：
                 · **一格 4 字节、一行一格**（被调方按 `R12 + 8 + 4i` 取）；
                 · 槽里放**地址**的条件是「声明的 BYREF **或**形参类型 8 字节」，
                   其余放内联值。

               **8 字节形参为什么也放地址**：一格 4 字节装不下 double。早先这里是
               "一律压一个值"，而被调方按 `GetVarByteOffset` 给 Double 算 8 字节 ⇒
               同一处布局两套算法：`CALL S3(45.5)` 读到隔壁槽的垃圾、BYVAL 的 `a#`
               读到半个 double。改成"地址格"之后**所有槽都是 4 字节**。 */

            BasicType PType(int i) => paramTypeAt?.Invoke(i) ?? BasicType.Integer;
            bool Wide(int i) => IsWideType(PType(i));
            // 槽里要放地址（= 需要临时量的情形与 BYREF 同形）
            bool AddrSlot(int i) => isByRefAt(i) || Wide(i);

            /* ① **先压"要地址的实参"的临时量**，而且必须压在**实参块之外**。
               理由：被调方按固定步长 4 取形参，每个实参只能占一格；
               把"值"那一格插进实参块里会把后面所有形参整体错位。
               所以临时量统一压在最上面（高地址），实参槽里放它的地址。
               语义：QBasic 对"非左值实参"就是造个临时量，被调方写它写进临时量、出去即丢；
               BYVAL 的 8 字节形参同理 —— **拷贝一份**，被调方写它不回流（BYVAL 的本义）。 */
            var tempIndex = new Dictionary<int, int>();   // 实参下标 → 临时量序号（0 = 最先压的）
            var tempSizes = new List<int>();              // 各临时量的**字节数**（8 字节形参占两格）
            for (int i = n - 1; i >= 0; i--)
            {
                if (!AddrSlot(i)) continue;
                // BYREF 且实参是左值 ⇒ 直接取实参地址，不必造临时量
                if (isByRefAt(i) && IsAddressableArg(args[i])) continue;
                EmitArgValue(args[i], 0, subScope);
                // ⚠ 临时量里要放**形参类型**的那几个字节：形参是单精度而表达式是整数时，
                //   直接压整数位型、被调方按 `MOVEF` 读 ⇒ 读出来是 1e-44 那种垃圾
                //   （实测 `FUNCTION Scl (n!)` 恒返回 0）。8 字节形参同理要先 I2D/F2D，
                //   否则写进去的是整数位型（后面按双精度读出来是 3e-323）。
                EmitCoerceToParamType(args[i], PType(i));
                int size = Wide(i) ? 8 : 4;
                /* ⚠ 8 字节的临时量**不能用 `DPUSH`** —— `DPUSH`/`FPUSH` 压的是 VM 自己的
                   `doubleStack`/`floatStack`（见 `VMLRuntime.Float.cs` 的 `ExecuteDpush`），
                   **不是机器栈 `R13`**；被调方是从 `R12+8+4i` 这块内存里取实参的，
                   压进浮点栈等于没传。正确做法是在机器栈上**真的留出 8 字节**再按
                   `MOVED` 存进去（小端，低字在低地址 ⇒ 被调方按 double 读出来正是这个值）。 */
                if (size == 8)
                {
                    AddRI(OpCode.SUB, 13, 8);
                    AddInstruction(OpCode.MOVED, new Operand(OperandType.MEMORY, "R13"), Reg(0));
                }
                else
                {
                    AddInstruction(OpCode.PUSH, Reg(0));
                }
                tempIndex[i] = tempSizes.Count;
                tempSizes.Add(size);
            }
            int temps = tempSizes.Count;
            int tempBytes = 0;
            foreach (int s in tempSizes) tempBytes += s;

            // ② 压实参（右到左）：地址格 = 取地址或占位（③ 回填），内联格 = 值
            for (int i = n - 1; i >= 0; i--)
            {
                if (tempIndex.ContainsKey(i))
                    AddRI(OpCode.MOVE, 0, 0);              // 占位，③ 回填临时量地址
                else if (isByRefAt(i))
                    GenerateVariableAddress(args[i], 0);
                else if (Wide(i))
                    // 到不了：① 对"宽且非 BYREF"一律造了临时量。留一条显式防线，
                    // 免得将来改了 ① 的判据后这里**静默**压个值（那就是半个 double）。
                    throw new CompilationException(ErrorCode.CodeGen_TypeMismatch,
                        $"内部错误：8 字节形参 '{i}' 没有临时量（{currentSubName ?? "主程序"}）");
                else
                    EmitArgValue(args[i], 0, subScope);
                AddInstruction(OpCode.PUSH, Reg(0));
            }

            /* ③ 回填临时量地址。压完之后 R13 正好指向实参块底（= 实参 0 的槽）：
                实参块在 [R13, R13 + 4n)，临时量区在它上面 [R13 + 4n, R13 + 4n + tempBytes)。
               PUSH 是**先减后存**，所以最先压的（序号 0）落在临时量区的**最高**地址；
               临时量 k 的最低字节离临时量区底 `tempBytes - prefix(k+1)`。 */
            var prefix = new int[temps + 1];
            for (int j = 0; j < temps; j++) prefix[j + 1] = prefix[j] + tempSizes[j];
            foreach (var kv in tempIndex)
            {
                AddInstruction(OpCode.MOVE, Reg(0), Reg(13));
                AddRI(OpCode.ADD, 0, n * 4 + tempBytes - prefix[kv.Value + 1]);
                AddInstruction(OpCode.MOVE, new Operand(OperandType.MEMORY, $"R13+{4 * kv.Key}"), Reg(0));
            }

            return tempBytes + n * 4;
        }

        /// <summary>
        /// 把 <paramref name="e"/> 的值**按形参类型**补齐位型（临时量要用）。
        /// 单精度形参 + 整数表达式 ⇒ `I2F`；8 字节形参 + 整数/单精度 ⇒ `I2D`/`F2D`。
        /// 不补的后果都是"编得过、跑起来是垃圾"：位型不对，被调方按声明的宽度读出来就是
        /// 1e-44 / 3e-323 那种数。
        /// </summary>
        private void EmitCoerceToParamType(Expression e, BasicType target)
        {
            BasicType src = InferExpressionType(e);
            if (target == BasicType.Double || target == BasicType.Long)
            {
                if (src == BasicType.Double || src == BasicType.Long) return;
                AddRR(src == BasicType.Single ? OpCode.F2D : OpCode.I2D, 0, 0);
            }
            else if (target == BasicType.Single)
            {
                if (src == BasicType.Single || src == BasicType.Double) return;
                AddRR(OpCode.I2F, 0, 0);
            }
        }

        /// <summary>
        /// SUB/FUNCTION 体内"二元运算的一个操作数"：求值到寄存器 1，并按结果类型做类型提升。
        /// 整数 → `I2F`/`I2D`；单精度 → `F2D`（往双精度提升时）；已经是目标类型的原样不动。
        /// </summary>
        private void EmitSubOperand(Expression operand, ExpType operandType, bool targetDouble, bool floatPath)
        {
            GenerateSubExpression(operand, 1);
            if (!floatPath) return;
            if (targetDouble)
            {
                // ⚠ **顺序不能反**：`ExpType.IsFloat()` 对 `F32` **和** `F64` 都为真
                //   （见 `ExpTypeExtensions.IsFloat`），所以原来先判 `IsFloat` 的分支
                //   会把**已经是双精度**的操作数也送进 `F2D` ——
                //   把 double 的位型当 float 再转一次 double，值当场变成垃圾。
                //   实测 `.scratch/gor/mre_dcmp.bas`：`SUB` 里 `A# = 1.5 + 2.25`
                //   算成 `A# <= 3.5`（顶层同一段是对的，因为顶层那条路的判断顺序是对的）。
                if (operandType.IsDouble())
                {
                    // 已经是双精度：原样
                }
                else if (operandType.IsFloat())
                    instructions.Add(new Instruction(OpCode.F2D, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1) }));
                else
                    instructions.Add(new Instruction(OpCode.I2D, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1) }));
            }
            else
            {
                if (!operandType.IsFloat() && !operandType.IsDouble())
                    instructions.Add(new Instruction(OpCode.I2F, new List<Operand> { new Operand(OperandType.REGISTER, 1), new Operand(OperandType.REGISTER, 1) }));
            }
        }

        /// <summary>
        /// 形参声明的类型 —— 名字后缀优先（`n!` → Single、`x#` → Double），
        /// 其次 `AS STRING`，其余按整数。给"实参临时量要按形参类型填字节"用
        /// （见 <see cref="EmitCallArguments"/> 里那条 I2F）。
        /// </summary>
        private static BasicType ParamDeclaredType(ParameterNode p)
        {
            string n = p?.Name ?? "";
            if (p != null && p.IsString) return BasicType.String;
            if (n.EndsWith("$", StringComparison.Ordinal)) return BasicType.String;
            if (n.EndsWith("!", StringComparison.Ordinal)) return BasicType.Single;
            if (n.EndsWith("#", StringComparison.Ordinal)) return BasicType.Double;
            if (n.EndsWith("&", StringComparison.Ordinal)) return BasicType.Integer;
            if (n.EndsWith("%", StringComparison.Ordinal)) return BasicType.Byte;
            // `AS DOUBLE` 这类**内置类型名**（没有后缀写法时唯一的类型来源）。
            // 不认它的后果不是"精度差一点"：宽度判错 ⇒ 实参槽的口径与被调方的取值
            // 对不上，读出来是隔壁槽的内存（`CALL S(45.5)` 打出 1110835200）。
            switch (p?.DeclaredType)
            {
                case "double": return BasicType.Double;
                case "single": return BasicType.Single;
                case "long": return BasicType.Long;
                case "byte": return BasicType.Byte;
                case "boolean": return BasicType.Boolean;
                case "integer": return BasicType.Integer;
            }
            return BasicType.Integer;
        }

        /// <summary>按当前是否在 SUB 体内选表达式发射路径。</summary>
        private void EmitArgValue(Expression e, int reg, bool subScope)
        {
            if (subScope) GenerateSubExpression(e, reg);
            else GenerateExpression(e, reg);
        }

        /// <summary>
        /// 实参能不能"取地址后交给被调方读写"。
        ///
        /// <list type="bullet">
        /// <item><description><b>标量变量</b>：能（`ident` 的地址）。</description></item>
        /// <item><description><b>数组元素</b>（`arr(i)`）：**能** —— 元素地址本身就是合法的左值，
        /// `SUB t(a) / a = 5` + `t arr(1)` 必须把 `arr(1)` 改掉（QBasic 就是这样）。</description></item>
        /// <item><description><b>整个数组</b>（`arr()`）：能，取的是**基址**
        /// （`CALL MakeCityScape(BCoor())` 要的就是这个）。</description></item>
        /// <item><description><b>常量</b>：不能 —— 把 `DoSun SUNHAPPY`（`CONST SUNHAPPY = FALSE`）
        /// 当左值传地址，被调方一写就把常量区改了（QBasic 会造临时量）。</description></item>
        /// <item><description>SUB/FUNCTION 名、认不出的数组：不能（走临时量，被调方的写不回流）。</description></item>
        /// </list>
        /// </summary>
        private bool IsAddressableArg(Expression e)
        {
            // 数组（整数组 / 元素）：只要是**已知**的数组（DIM 出来的，或是数组形参）就能取地址。
            // 判"认不认得"这一步不能省：认不出时 `GenerateVariableAddress` 会给 0，
            // 而那等于把一个野地址交给被调方去写。
            if (e is ArrayAccessExpression acc)
                return IsKnownArray(acc.ArrayName);

            // 记录字段（`pts(2).XCoor` / `p.X`）：字段偏移查得到就能取地址
            // （查不到就还是走临时量 —— 给个 0 当地址会让被调方写到野地址上）
            if (e is FieldAccessExpression fa)
                return ResolveFieldOffset(fa) >= 0;

            if (e is not Identifier id) return false;
            string key = id.Name.ToLower();
            if (constants.ContainsKey(key)) return false;
            if (arrayVariables.ContainsKey(key)) return false;
            if (subMap.ContainsKey(SymbolKey(id.Name))) return false;
            if (funcMap.ContainsKey(SymbolKey(id.Name))) return false;
            return true;
        }

        /// <summary>
        /// <paramref name="arrayName"/> 是不是一个**已知数组** —— 模块级/SUB 内 `DIM` 出来的，
        /// 或者当前 SUB/FUNCTION 的**数组形参**。判据只有这一份（`GenerateArrayElementAddr`
        /// 与 `GenerateArrayBaseAddr` 用的也是它）。大小写不敏感（`arrayVariables` 的键按书写
        /// 原样存，`BCoor` 与 `bcoord` 要都能查到）。
        /// </summary>
        private bool IsKnownArray(string arrayName)
        {
            if (arrayVariables.ContainsKey(arrayName)) return true;
            if (currentSubName == null) return false;
            int pIdx = FindParameterIndex(arrayName.ToLower());
            if (pIdx < 0) return false;
            var pDecl = FindParameterDecl(pIdx);
            return pDecl != null && pDecl.IsArray;
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
                    // `RND(1)` —— 见 `GenerateRndValue`：库给的是原始随机整数，
                    // 要换算成 QBasic 语义的 [0,1) 单精度分数，**两处内置表都要走它**
                    // （这一处与 `CodeGenerator.Expressions.cs` 的 `GenerateMainFunctionCall`
                    // 是同一个 switch 的两份，本仓的老毛病）。
                    EmitSaveRegsExcept(reg, 0,1,2,3,4,5); GenerateRndValue(reg); EmitRestoreRegsExcept(reg, 0,1,2,3,4,5); return;
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
            
            // native FUNCTION: 使用裸名 CALL (无 func_ 前缀)，**且保留声明处的大小写**（外部符号）
            funcMap.TryGetValue(SymbolKey(funcCall.FunctionName), out var fd);
            string funcLabel = (fd != null && fd.IsNative)
                ? NativeLabel(fd.Name)
                : FunctionLabel(funcCall.FunctionName);

            // Push arguments (right-to-left) —— 与另外两条调用路径共用同一份实现
            int argBytes = EmitCallArguments(funcCall.Arguments,
                i => fd != null && i < fd.Parameters.Count && fd.Parameters[i].IsByRef,
                true,
                i => fd != null && i < fd.Parameters.Count
                        ? ParamDeclaredType(fd.Parameters[i]) : BasicType.Integer);

            // CALL
            instructions.Add(new Instruction(OpCode.CALL, new List<Operand> { new Operand(OperandType.LABEL, funcLabel) }));

            // Clean up stack
            if (argBytes > 0)
            {
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand> { new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, argBytes) }));
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
