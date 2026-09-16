using VMLAssembler;
using CompilerBase;

namespace GoCompiler
{
    public partial class CodeGenerator : CLikeCodegen<CodeGenerator>
    {
        private void GenerateGlobalVariable(VariableDecl varDecl)
        {
            // 获取全局变量类型
            GoTypeEnum varType = varDecl.Type != null ? GetGoTypeEnum(varDecl.Type) : GoTypeEnum.Int;
            
            foreach (var name in varDecl.Names)
            {
                // 跟踪全局变量类型
                _varTypes[name] = varType;
                
                if (varDecl.Values.Count > 0)
                {
                    var value = EvaluateConstant(varDecl.Values[0]);
                    dataSection[name] = value;
                }
                else
                {
                    dataSection[name] = 0;
                }
            }
        }

        private void GenerateGlobalConstant(ConstantDecl constDecl)
        {
            foreach (var name in constDecl.Names)
            {
                if (constDecl.Values.Count > 0)
                {
                    var valueNode = constDecl.Values[0];
                    var value = EvaluateConstant(valueNode);
                    constants[name] = value;
                    
                    // 推断常量类型并跟踪
                    GoTypeEnum constType = InferExpressionType(valueNode);
                    _varTypes[name] = constType;
                }
            }
        }

        private void GenerateLocalConstant(ConstantDecl constDecl)
        {
            foreach (var name in constDecl.Names)
            {
                if (constDecl.Values.Count > 0)
                {
                    var valueNode = constDecl.Values[0];
                    var value = EvaluateConstant(valueNode);
                    constants[name] = value;
                    
                    // 推断常量类型并跟踪
                    GoTypeEnum constType = InferExpressionType(valueNode);
                    _varTypes[name] = constType;
                }
            }
        }

        private object EvaluateConstant(ASTNode node)
        {
            if (node is NumberLiteral numLit)
            {
                if (numLit.IsFloat)
                {
                    if (double.TryParse(numLit.Value, out double d))
                        return d;
                }
                else
                {
                    if (long.TryParse(numLit.Value, out long l))
                        return l;
                }
            }
            else if (node is StringLiteral strLit)
            {
                return strLit.Value;
            }
            return 0;
        }

        private void GenerateFunction(Function function)
        {
            currentFunction = function;
            variables.Clear();
            localVarOffset = 0;
            Vars?.ResetLocals();

            // 函数标签
            string funcName = function.Name;
            AddLabel(funcName);

            // 添加函数注释
            Emit(OpCode.NOP, new List<Operand>(), "; -------------------------------------------");
            // 生成源函数声明
            var sourceDecl = $"func {function.Name}(";
            for (var i = 0; i < function.Parameters.Count; i++)
            {
                var param = function.Parameters[i];
                for (var j = 0; j < param.Names.Count; j++)
                {
                    sourceDecl += $"{param.Type.Name} {param.Names[j]}";
                    if (j < param.Names.Count - 1) sourceDecl += ",";
                }
                if (i < function.Parameters.Count - 1) sourceDecl += ",";
            }
            sourceDecl += ")";
            if (function.Results.Count > 0)
            {
                sourceDecl += " (";
                for (var i = 0; i < function.Results.Count; i++)
                {
                    sourceDecl += function.Results[i].Type.Name;
                    if (i < function.Results.Count - 1) sourceDecl += ",";
                }
                sourceDecl += ")";
            }
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; source   : {sourceDecl}"));
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; function : {function.Name}"));
            // 生成参数注释（每个参数名单独一行）
            foreach (var param in function.Parameters)
            {
                foreach (var name in param.Names)
                {
                    instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; param   : {param.Type.Name} {name}"));
                }
            }
            // 生成返回类型注释（多个返回值逗号分隔）
            var returnStr = "";
            for (var i = 0; i < function.Results.Count; i++)
            {
                returnStr += function.Results[i].Type.Name;
                if (i < function.Results.Count - 1) returnStr += ",";
            }
            if (string.IsNullOrEmpty(returnStr)) returnStr = "void";
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; return   : {returnStr}"));
            instructions.Add(new Instruction(OpCode.NOP, [], 0, "; --------------------------------------------"));

            AddLabel(funcName);

            // 保存帧指针
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 14)
            }, instructions.Count));

            // 建立新栈帧
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 14),
                new Operand(OperandType.REGISTER, 13)
            }, instructions.Count));

            // 注册参数到变量表并保存到栈帧
            // 注意: [R14-0]是保存的R14值，参数从[R14-4]开始
            int paramSpace = 0;
            localVarOffset = 4; // 跳过[R14-0] (保存的帧指针)
            for (int i = 0; i < function.Parameters.Count; i++)
            {
                foreach (var name in function.Parameters[i].Names)
                {
                    variables[name] = localVarOffset;
                    _varTypes[name] = GoTypeEnum.Int;
                    Vars?.AllocParam(name, 4);  // 向 VarMemManager 注册以追踪统计
                    localVarOffset += 4;
                    paramSpace += 4;
                }
                // 第一个参数在R0中，保存到栈帧
                if (i == 0)
                {
                    foreach (var name in function.Parameters[i].Names)
                    {
                        int off = variables[name];
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                                                        Mem($"R14-{off}"),
                            new Operand(OperandType.REGISTER, 0)
                        }, instructions.Count));
                    }
                }
            }

            // 计算局部变量空间
            int localSpace = 0;
            if (function.Body != null)
            {
                localSpace = CalculateLocalSpace(function.Body);
            }

            // 分配局部变量空间（含参数空间）
            int totalSpace = localSpace + paramSpace;
            if (totalSpace > 0)
            {
                instructions.Add(new Instruction(OpCode.SUB, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 13),
                    new Operand(OperandType.IMMEDIATE, totalSpace)
                }, instructions.Count));
            }

            // 函数结束标签（在生成函数体之前注册，以便return语句可以JMP到此处）
            string endLabel = NewLabel();
            functionEndLabels[function.Name] = endLabel;

            // 清空上一函数的 defer 队列
            _deferredCalls.Clear();

            // 生成函数体
            if (function.Body != null)
            {
                GenerateBlock(function.Body);
            }

            // main 函数自动生成退出系统调用
            if (function.Name == "main")
            {
                // 加载第一个局部变量 ([R14-4]) 到 R0 作为退出码
                if (localVarOffset > 4)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, [Mem("R14-4"), new Operand(OperandType.REGISTER, 0)]));
                }
                EmitExit();
            }

            // 发射 defer 调用 (LIFO 顺序 — 后进先出)
            if (_deferredCalls.Count > 0)
            {
                // 重定向 return 跳转目标: 先执行 defer 调用, 再执行尾声
                string deferCleanupLabel = NewLabel();
                functionEndLabels[function.Name] = deferCleanupLabel;
                AddLabel(deferCleanupLabel);
                for (int i = _deferredCalls.Count - 1; i >= 0; i--)
                {
                    GenerateExpression(_deferredCalls[i]);
                }
                _deferredCalls.Clear();
            }
            AddLabel(endLabel);

            // 恢复栈指针，然后恢复帧指针
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 13),
                new Operand(OperandType.REGISTER, 14)
            }, instructions.Count));
            instructions.Add(new Instruction(OpCode.POP, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 14)
            }, instructions.Count));

            // 返回
            Sta!.EmitReturn();

            currentFunction = null;
        }

        /// <summary>
        /// 估算函数体需要的局部栈空间（序言据此 `sub R13`）。
        ///
        /// ⚠ v0.96.193：**必须递归进 if / for / switch 的体内**。原来只看块里的顶层语句，
        /// 于是 `for i := 0; i &lt; 5; i++ { … }` 里的 `i`（以及循环体内声明的变量）**都没被算进去**
        /// ⇒ 帧开小了、局部变量落到了保留区之外，而表达式求值的 `push`/`pop` 正好写在那里
        /// ⇒ **每次求值都把循环变量冲掉**。实测症状极具迷惑性：`for i := 0; i &lt; 5; i++ { s = s + a[3] }`
        /// 只累加 2 次（10 而不是 25），而**同一个循环里不读数组**时又是对的（50）——
        /// 因为读数组的序列多了几条 `push/pop`，恰好踩中。
        /// 少算 ≠ 只浪费空间：**少算就是踩内存**，所以这里宁可多算。
        /// </summary>
        private int CalculateLocalSpace(Block block)
        {
            if (block == null) return 0;
            int space = 0;
            foreach (var stmt in block.Statements)
            {
                if (stmt is VariableDecl varDecl)
                {
                    GoTypeEnum varType = varDecl.Type != null ? GetGoTypeEnum(varDecl.Type) : GoTypeEnum.Int;
                    // 定长数组只占一个指针槽（块本身在数据段，见 GenerateLocalVarDecl）
                    int per = GoArrayLength(varDecl.Type) > 0 ? 4 : GetTypeSize(varType);
                    space += per * varDecl.Names.Count;
                }
                else if (stmt is ShortVarDecl shortVar)
                {
                    // := 声明的变量，根据推断的类型分配空间
                    for (int i = 0; i < shortVar.Names.Count; i++)
                    {
                        GoTypeEnum inferredType = i < shortVar.Values.Count
                            ? InferExpressionType(shortVar.Values[i])
                            : GoTypeEnum.Int;
                        space += GetTypeSize(inferredType);
                    }
                }
                else if (stmt is ConstantDecl)
                {
                    // 常量不占用栈空间
                }
                else if (stmt is IfStatement ifStmt)
                {
                    space += CountDeclSpace(ifStmt.Init);
                    space += CalculateLocalSpace(ifStmt.ThenBranch);
                    if (ifStmt.ElseIfBranches != null)
                        foreach (var (_, body) in ifStmt.ElseIfBranches)
                            space += CalculateLocalSpace(body);
                    space += CalculateLocalSpace(ifStmt.ElseBranch);
                }
                else if (stmt is ForStatement forStmt)
                {
                    space += CountDeclSpace(forStmt.Init);
                    if (forStmt.IsRangeLoop)
                    {
                        if (!string.IsNullOrEmpty(forStmt.KeyVar)) space += 4;
                        if (!string.IsNullOrEmpty(forStmt.ValueVar)) space += 4;
                    }
                    space += CountDeclSpace(forStmt.Post);
                    space += CalculateLocalSpace(forStmt.Body);
                }
                else if (stmt is SwitchStatement switchStmt)
                {
                    space += CountDeclSpace(switchStmt.Init);
                    if (switchStmt.Cases != null)
                        foreach (var c in switchStmt.Cases)
                            space += CalculateLocalSpace(c.Body);
                }
                else if (stmt is Block inner)
                {
                    space += CalculateLocalSpace(inner);
                }
            }
            return space;
        }

        /// <summary>单条语句可能带来的声明空间（供 if/for/switch 的 init/post 用）。</summary>
        private int CountDeclSpace(ASTNode stmt)
        {
            switch (stmt)
            {
                case null: return 0;
                case VariableDecl vd:
                    {
                        GoTypeEnum t = vd.Type != null ? GetGoTypeEnum(vd.Type) : GoTypeEnum.Int;
                        int per = GoArrayLength(vd.Type) > 0 ? 4 : GetTypeSize(t);
                        return per * vd.Names.Count;
                    }
                case ShortVarDecl sv:
                    {
                        int s = 0;
                        for (int i = 0; i < sv.Names.Count; i++)
                            s += GetTypeSize(i < sv.Values.Count ? InferExpressionType(sv.Values[i]) : GoTypeEnum.Int);
                        return s;
                    }
                default: return 0;
            }
        }

        private void GenerateBlock(Block block)
        {
            foreach (var stmt in block.Statements)
            {
                GenerateStatement(stmt);
            }
        }

        private void GenerateStatement(ASTNode stmt)
        {
            if (stmt == null) return;

            if (stmt is ReturnStatement ret)
            {
                GenerateReturn(ret);
            }
            else if (stmt is IfStatement ifStmt)
            {
                GenerateIf(ifStmt);
            }
            else if (stmt is ForStatement forStmt)
            {
                GenerateFor(forStmt);
            }
            else if (stmt is SwitchStatement switchStmt)
            {
                GenerateSwitch(switchStmt);
            }
            else if (stmt is BreakStatement)
            {
                Sta!.EmitBreak();
            }
            else if (stmt is ContinueStatement)
            {
                Sta!.EmitContinue();
            }
            else if (stmt is GotoStatement gotoStmt)
            {
                Sta!.EmitJump(gotoStmt.Label);
            }
            else if (stmt is LabeledStatement labeled)
            {
                AddLabel(labeled.Name);
                GenerateStatement(labeled.Statement);
            }
            else if (stmt is VariableDecl varDecl)
            {
                GenerateLocalVarDecl(varDecl);
            }
            else if (stmt is ConstantDecl constDecl)
            {
                GenerateLocalConstant(constDecl);
            }
            else if (stmt is ShortVarDecl shortVar)
            {
                GenerateShortVarDecl(shortVar);
            }
            else if (stmt is ExpressionStatement exprStmt)
            {
                GenerateExpression(exprStmt.Expression);
            }
            else if (stmt is Block block)
            {
                GenerateBlock(block);
            }
            else if (stmt is EmptyStatement)
            {
                // 空语句，什么都不做
            }
            else if (stmt is FallthroughStatement)
            {
                // Go fallthrough: execution continues to next case body.
                // EmitSwitchCustom with fallthrough=true already omits JMP endLabel,
                // so fallthrough happens naturally by instruction ordering.
            }
            else if (stmt is GoStatement goStmt)
            {
                // OS mode: emit goroutine launch via runtime
                if (!VMLPlugins.CompilerOptionsContext.Current.IsMCU && goStmt.Call != null)
                {
                    GenerateExpression(goStmt.Call);
                    instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "__runtime_go")]));
                }
            }
            else if (stmt is SendStatement sendStmt)
            {
                if (!VMLPlugins.CompilerOptionsContext.Current.IsMCU)
                {
                    GenerateExpression(sendStmt.Value);
                    instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                    GenerateExpression(sendStmt.Chan);
                    instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                    instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "__runtime_chan_send")]));
                }
                else
                {
                    VMLPlugins.WarningEmitter.Emit("go", "MCU模式: chan<- 被忽略（MCU 无 channel 支持）");
                }
            }
            else if (stmt is SelectStatement selectStmt)
            {
                if (!VMLPlugins.CompilerOptionsContext.Current.IsMCU && selectStmt.Clauses.Count > 0)
                {
                    instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, "__runtime_select")]));
                }
                else if (VMLPlugins.CompilerOptionsContext.Current.IsMCU)
                {
                    VMLPlugins.WarningEmitter.Emit("go", "MCU模式: select 被忽略（MCU 无 channel 支持）");
                }
            }
            else if (stmt is IncDecStatement incDec)
            {
                // i++ / i--
                if (incDec.Expression is Identifier incId)
                    GenerateIncDec(incId.Name, incDec.IsIncrement);
            }
            else if (stmt is DeferStatement deferStmt)
            {
                // defer: collect for LIFO execution at function exit (MCU & OS both supported)
                if (deferStmt.Call != null)
                    _deferredCalls.Add(deferStmt.Call);
            }
            else if (stmt is Assignment assignment)
            {
                GenerateAssignment(assignment);
            }
        }

        private void GenerateIncDec(string varName, bool isInc)
        {
            if (variables.ContainsKey(varName))
            {
                int off = variables[varName];
                var goType = _varTypes.GetValueOrDefault(varName, GoTypeEnum.Int);
                var target = ExpVar.Stack(-off, 14, GoTypeToExpType(goType));
                if (isInc)
                    _expr!.EmitPrefixInc(target);
                else
                    _expr!.EmitPrefixDec(target);
            }
        }

        private void GenerateAssignment(Assignment assignment)
        {
            for (int i = 0; i < assignment.Left.Count && i < assignment.Right.Count; i++)
            {
                // 解包左值（可能是 AssignmentWrapper）
                ASTNode leftNode = assignment.Left[i];
                while (leftNode is AssignmentWrapper aw)
                    leftNode = aw.Left;

                if (assignment.Op != "=" && leftNode is Identifier ident && variables.ContainsKey(ident.Name))
                {
                    // 复合赋值: id += expr → 使用 EmitCompoundAssign
                    int off = variables[ident.Name];
                    var goType = _varTypes.GetValueOrDefault(ident.Name, GoTypeEnum.Int);
                    var target = ExpVar.Stack(-off, 14, GoTypeToExpType(goType));
                    var value = WrapExpr(assignment.Right[i]);
                    _expr!.EmitCompoundAssign(target, value, assignment.Op);
                }
                else if (leftNode is IndexExpr idxLeft)
                {
                    // ⚠ v0.96.193 新增：**`a[i] = v` 原来整段没有代码生成** ——
                    //   赋值目标是下标时谁都不认，右值算完就被丢掉（实测 `a[3] = 5` 只编出
                    //   一句 `move R0 #5`，存储根本不存在）。这里补上：
                    //   先把值存进 R2（避开后面要用的 R0/R1），再算地址，最后 `MOVE [R0], R2`。
                    //   `MOVE dest, src` 是 **dest 在前** —— 别写反（这一族本仓库栽过六次）。
                    GenerateExpression(assignment.Right[i]);            // R0 = 值
                    instructions.Add(new Instruction(OpCode.MOVE,
                        [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0)],
                        instructions.Count));

                    GenerateExpression(idxLeft.Array);                  // R0 = 数组块地址
                    instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)], instructions.Count));
                    GenerateExpression(idxLeft.Index);                  // R0 = 下标
                    instructions.Add(new Instruction(OpCode.MOVE,
                        [new Operand(OperandType.REGISTER, 1), new Operand(OperandType.IMMEDIATE, 4)],
                        instructions.Count));
                    instructions.Add(new Instruction(OpCode.MUL,
                        [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)],
                        instructions.Count));                           // R0 = idx*4
                    instructions.Add(new Instruction(OpCode.ADD,
                        [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)],
                        instructions.Count));                           // +4 跳过 VML 数组头
                    instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)], instructions.Count));
                    instructions.Add(new Instruction(OpCode.ADD,
                        [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 1)],
                        instructions.Count));                           // R0 = base + idx*4 + 4
                    instructions.Add(new Instruction(OpCode.MOVE,
                        [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.REGISTER, 2)],
                        instructions.Count));                           // [R0] = 值
                }
                else
                {
                    // 计算右值 → R0
                    GenerateExpression(assignment.Right[i]);

                    // 存储到左变量
                    if (leftNode is Identifier ident2 && variables.ContainsKey(ident2.Name))
                    {
                        int off = variables[ident2.Name];
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                                                        Mem($"R14-{off}"),
                            new Operand(OperandType.REGISTER, 0)
                        }, instructions.Count));
                    }
                }
            }
        }

        private void GenerateReturn(ReturnStatement ret)
        {
            if (ret.Results.Count > 0)
            {
                GenerateExpression(ret.Results[0]);
            }

            if (currentFunction != null && functionEndLabels.ContainsKey(currentFunction.Name))
            {
                instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
                {
                    new Operand(OperandType.LABEL, functionEndLabels[currentFunction.Name])
                }, instructions.Count));
            }
            else
            {
                Sta!.EmitReturn();
            }
        }

        private void GenerateIf(IfStatement ifStmt)
        {
            // 初始化语句（如 if x := 42; x > 0 { ... }）
            if (ifStmt.Init != null)
                GenerateStatement(ifStmt.Init);

            var branches = new List<(System.Action, System.Action)>
            {
                (() => GenerateExpression(ifStmt.Condition), () => GenerateBlock(ifStmt.ThenBranch))
            };
            foreach (var (cond, body) in ifStmt.ElseIfBranches)
                branches.Add((() => GenerateExpression(cond), () => GenerateBlock(body)));

            Sta!.EmitIfChain(branches,
                ifStmt.ElseBranch != null ? () => GenerateBlock(ifStmt.ElseBranch) : null);
        }

        private void GenerateFor(ForStatement forStmt)
        {
            // range 循环: for i, v := range slice { ... } — 保持手动 label 管理
            if (forStmt.IsRangeLoop && forStmt.RangeExpr != null)
            {
                string startLabel = Sta!.NewLabel();
                string condLabel = Sta.NewLabel();
                string postLabel = Sta.NewLabel();
                string endLabel = Sta.NewLabel();

                Sta.PushLoopLabels(endLabel, postLabel);

                GenerateExpression(forStmt.RangeExpr);
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 8), new Operand(OperandType.REGISTER, 0)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 9), new Operand(OperandType.MEMORY, "R8")]));

                int offK = 0;
                if (!string.IsNullOrEmpty(forStmt.KeyVar))
                {
                    if (!variables.ContainsKey(forStmt.KeyVar)) variables[forStmt.KeyVar] = variables.Count;
                    offK = variables[forStmt.KeyVar];
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 0)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"{offK}(R12)")]));
                }

                instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, condLabel)]));
                AddLabel(startLabel);

                if (!string.IsNullOrEmpty(forStmt.ValueVar) && variables.ContainsKey(forStmt.ValueVar))
                {
                    int offV = variables[forStmt.ValueVar];
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"{offK}(R12)")]));
                    instructions.Add(new Instruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 8)]));
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 4)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, "R0")]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"{offV}(R12)")]));
                }

                GenerateBlock(forStmt.Body);

                AddLabel(postLabel);
                if (!string.IsNullOrEmpty(forStmt.KeyVar))
                {
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"{offK}(R12)")]));
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 1)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"{offK}(R12)")]));
                }

                AddLabel(condLabel);
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.MEMORY, $"{offK}(R12)")]));
                instructions.Add(new Instruction(OpCode.CMP, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 9)]));
                instructions.Add(new Instruction(OpCode.JL, [new Operand(OperandType.LABEL, startLabel)]));

                Sta.PlaceLabel(endLabel);
                Sta.PopLoopLabels();
                return;
            }

            // 普通 for 循环
            Sta!.EmitFor(
                emitInit: forStmt.Init != null ? () => GenerateStatement(forStmt.Init) : null,
                emitCondition: forStmt.Condition != null ? () => GenerateExpression(forStmt.Condition) : null,
                emitIncrement: forStmt.Post != null ? () => GenerateStatement(forStmt.Post) : null,
                emitBody: () => GenerateBlock(forStmt.Body));
        }

        private void GenerateSwitch(SwitchStatement switchStmt)
        {
            // 初始化
            if (switchStmt.Init != null)
            {
                GenerateStatement(switchStmt.Init);
            }

            if (switchStmt.Tag != null)
            {
                // ===== 表达式 switch: switch x { case 1: ... } =====
                var emitCaseValues = new List<Action>();
                var caseBodies = new List<Action>();
                Action? defaultBody = null;
                bool hasFallthrough = false;

                foreach (var clause in switchStmt.Cases)
                {
                    if (clause.IsDefault)
                    {
                        defaultBody = () => { if (clause.Body != null) GenerateBlock(clause.Body); };
                        continue;
                    }
                    // Check if this case ends with fallthrough
                    bool endsWithFallthrough = clause.Body != null && clause.Body.Statements.Count > 0
                        && clause.Body.Statements[^1] is FallthroughStatement;
                    if (endsWithFallthrough) hasFallthrough = true;

                    foreach (var caseExpr in clause.Cases)
                    {
                        var capClause = clause;
                        emitCaseValues.Add(() => GenerateExpression(caseExpr));
                        if (endsWithFallthrough)
                        {
                            // Fallthrough: strip the trailing FallthroughStatement,
                            // let EmitSwitchCustom omit JMP endLabel so execution falls through
                            caseBodies.Add(() => {
                                if (capClause.Body != null)
                                {
                                    var stmts = capClause.Body.Statements;
                                    for (int bi = 0; bi < stmts.Count - 1; bi++)
                                        GenerateStatement(stmts[bi]);
                                }
                            });
                        }
                        else
                        {
                            caseBodies.Add(() => { if (capClause.Body != null) GenerateBlock(capClause.Body); });
                        }
                    }
                }

                Sta!.EmitSwitchCustom(
                    () => GenerateExpression(switchStmt.Tag),
                    emitCaseValues,
                    caseBodies,
                    defaultBody,
                    fallthrough: hasFallthrough
                );
            }
            else
            {
                // ===== 无表达式 switch → if-else 链 =====
                var branches = new List<(System.Action, System.Action)>();
                Action? defaultBody = null;

                foreach (var clause in switchStmt.Cases)
                {
                    if (clause.IsDefault)
                    {
                        defaultBody = () => { if (clause.Body != null) GenerateBlock(clause.Body); };
                        continue;
                    }

                    var capClause = clause;
                    if (clause.Cases.Count == 1)
                    {
                        branches.Add((() => GenerateExpression(clause.Cases[0]),
                                      () => { if (capClause.Body != null) GenerateBlock(capClause.Body); }));
                    }
                    else
                    {
                        // 逗号分隔的多表达式 → OR 链
                        branches.Add((() =>
                        {
                            var exprs = new List<ExpVar>();
                            foreach (var c in clause.Cases)
                                exprs.Add(WrapExpr(c));
                            var result = exprs[0];
                            for (int i = 1; i < exprs.Count; i++)
                                result = _expr!.EmitOr(result, exprs[i]);
                        }, () => { if (capClause.Body != null) GenerateBlock(capClause.Body); }));
                    }
                }

                Sta!.EmitIfChain(branches, defaultBody);
            }
        }

        private void GenerateLocalVarDecl(VariableDecl varDecl)
        {
            // 获取变量类型
            GoTypeEnum varType = varDecl.Type != null ? GetGoTypeEnum(varDecl.Type) : GoTypeEnum.Int;
            int typeSize = GetTypeSize(varType);

            // ⚠ v0.96.193：**定长数组要真的分配**。原来 `var a [8]int` 只按 `GetTypeSize`
            //   （默认 4 字节）留了一个槽、**从不初始化** ⇒ `a[i]` 的基址是一个未初始化的栈槽
            //   （实测恒为 0），于是 `a[2]` 读到 8、`a[5]` 读到 20 —— 全是 `idx*4` 的"偏移"。
            //   现在：用基类现成的 `AllocateVmlArray`（布局 `[count, e0, e1, …]`）在数据段建块，
            //   再把这个**块的地址**存进变量槽 —— 这样 `GenerateIndexExpr` 里
            //   `GenerateExpression(数组名)` 拿到的就是块地址，与它后面的 `base + i*4 + 4` 对得上。
            int arrLen = GoArrayLength(varDecl.Type);
            if (arrLen > 0)
            {
                foreach (var name in varDecl.Names)
                {
                    variables[name] = localVarOffset;
                    _varTypes[name] = varType;
                    Vars?.AllocLocal(name, 4);      // 变量槽只占一个指针
                    localVarOffset += 4;
                    string blockLabel = AllocateVmlArray(name, arrLen);
                    instructions.Add(new Instruction(OpCode.MOVE,
                        [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.LABEL, blockLabel)],
                        instructions.Count));       // R0 = 块地址（LEA）
                    instructions.Add(new Instruction(OpCode.MOVE,
                        [Mem($"R14-{variables[name]}"), new Operand(OperandType.REGISTER, 0)],
                        instructions.Count));       // 存进变量槽（偏移取正数，与读取路径同一套）
                }
                return;
            }

            foreach (var name in varDecl.Names)
            {
                variables[name] = localVarOffset;
                _varTypes[name] = varType; // 跟踪变量类型
                Vars?.AllocLocal(name, typeSize);  // 向 VarMemManager 注册以追踪统计
                localVarOffset += typeSize;

                if (varDecl.Values.Count > 0)
                {
                    var index = varDecl.Names.IndexOf(name);
                    if (index < varDecl.Values.Count)
                    {
                        GenerateExpression(varDecl.Values[index]);
                        // 使用类型敏感的存储指令
                        // ⚠ v0.96.193 修：原来是 `Mem($"R14-{-localVarOffset + typeSize}")` ——
                        //   字面量里**已经有了那个负号**，再喂一个负数就成了 `R14--4`，
                        //   而 `ResolveRegOffsetString` 把它解析成 `R14 + 4`（往调用方那边写）
                        //   ⇒ **`var x int = 5` 的初值从来没有落到 x 上**。
                        //   读取路径（`GenerateIdentifier`）用的是 `R14-{variables[name]}`（正数），
                        //   这里跟着它写，两边才是同一个槽。
                        OpCode storeOp = GetStoreInstruction(varType);
                        instructions.Add(new Instruction(storeOp, new List<Operand>
                        {
                            Mem($"R14-{variables[name]}"),
                            new Operand(OperandType.REGISTER, 0)
                        }, instructions.Count));
                    }
                }
            }
        }

        private void GenerateShortVarDecl(ShortVarDecl shortVar)
        {
            // 先推断所有值的类型，以便分配正确的空间
            var inferredTypes = new GoTypeEnum[shortVar.Names.Count];
            for (int i = 0; i < shortVar.Names.Count; i++)
            {
                if (i < shortVar.Values.Count)
                    inferredTypes[i] = InferExpressionType(shortVar.Values[i]);
                else
                    inferredTypes[i] = GoTypeEnum.Int;
            }

            // 根据推断的类型分配空间
            for (int i = 0; i < shortVar.Names.Count; i++)
            {
                var name = shortVar.Names[i];
                int typeSize = GetTypeSize(inferredTypes[i]);
                variables[name] = localVarOffset;
                Vars?.AllocLocal(name, typeSize);  // 向 VarMemManager 注册以追踪统计
                localVarOffset += typeSize;
                _varTypes[name] = inferredTypes[i]; // 提前跟踪变量类型
            }

            // 计算并存储值
            for (int i = 0; i < shortVar.Values.Count && i < shortVar.Names.Count; i++)
            {
                var valueNode = shortVar.Values[i];
                var varName = shortVar.Names[i];

                GenerateExpression(valueNode);
                int offset = variables[varName];

                // 使用类型敏感的存储指令
                OpCode storeOp = GetStoreInstruction(inferredTypes[i]);
                instructions.Add(new Instruction(storeOp, new List<Operand>
                {
                    Mem($"R14-{offset}"),
                    new Operand(OperandType.REGISTER, 0)
                }, instructions.Count));
            }
        }
    }
}
