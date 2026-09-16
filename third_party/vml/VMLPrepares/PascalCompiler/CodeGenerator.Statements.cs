using System;
using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace PascalCompiler
{
    public partial class CodeGenerator
    {
        private void GenerateStatement(StatementNode statement)
        {
            if (statement is AssignmentNode assignment)
            {
                GenerateAssignment(assignment);
            }
            else if (statement is ProcedureCallNode call)
            {
                GenerateProcedureCall(call);
            }
            else if (statement is CompoundStatementNode compound)
            {
                // CompoundStatementNode包含Statement列表，直接生成每个语句
                foreach (var stmt in compound.Statements)
                {
                    GenerateStatement(stmt);
                }
            }
            else if (statement is IfNode ifNode)
            {
                GenerateIfStatement(ifNode);
            }
            else if (statement is WhileNode whileNode)
            {
                GenerateWhileStatement(whileNode);
            }
            else if (statement is ForNode forNode)
            {
                GenerateForStatement(forNode);
            }
            else if (statement is RepeatNode repeatNode)
            {
                GenerateRepeatStatement(repeatNode);
            }
            else if (statement is CaseNode caseNode)
            {
                GenerateCaseStatement(caseNode);
            }
            else if (statement is WithNode withNode)
            {
                GenerateStatement(withNode.Body);
            }
            else if (statement is GotoNode gotoNode)
            {
                AddInstruction(OpCode.JMP, LabelOp($"user_label_{gotoNode.Label}"));
            }
            else if (statement is LabeledStatementNode labeled)
            {
                AddLabel($"user_label_{labeled.Label}");
                GenerateStatement(labeled.Statement);
            }
            else if (statement is ReturnNode returnNode)
            {
                GenerateReturnStatement(returnNode);
            }
            else if (statement is BreakNode)
            {
                Sta!.EmitBreak();
            }
            else if (statement is ContinueNode)
            {
                Sta!.EmitContinue();
            }
            else
            {
                throw new CompilationException(ErrorCode.CodeGen_UnsupportedExpression, $"不支持的语句类型: {statement.GetType().Name}");
            }
        }
        
        private void GenerateReturnStatement(ReturnNode returnNode)
        {
            if (returnNode.Value != null)
            {
                GenerateExpression(returnNode.Value);
            }
            
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 13),
                new Operand(OperandType.REGISTER, 12)
            }));
            instructions.Add(new Instruction(OpCode.POP, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 12)
            }));
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>()));
        }
        
        private void GenerateIfStatement(IfNode ifNode)
        {
            Sta!.EmitIf(
                () => GenerateExpression(ifNode.Condition),
                () => GenerateStatement(ifNode.ThenBranch),
                ifNode.ElseBranch != null ? () => GenerateStatement(ifNode.ElseBranch) : null);
        }
        
        private void GenerateWhileStatement(WhileNode whileNode)
        {
            Sta!.EmitWhile(
                () => GenerateExpression(whileNode.Condition),
                () => GenerateStatement(whileNode.Body));
        }
        
        private void GenerateForStatement(ForNode forNode)
        {
            string loopStartLabel = $"for_start_{labelCounter++}";
            string loopEndLabel = $"for_end_{labelCounter++}";
            string loopBodyLabel = $"for_body_{labelCounter++}";
            string loopIncrementLabel = $"for_inc_{labelCounter++}";
            Sta!.PushLoopLabels(loopEndLabel, loopIncrementLabel);

            // 判断循环变量是局部变量还是全局变量
            bool isLocalVar = localVarOffsets.ContainsKey(forNode.Variable);

            // 辅助方法：加载循环变量值到 R0
            void EmitLoadLoopVar()
            {
                if (isLocalVar)
                {
                    int offset = localVarOffsets[forNode.Variable];
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.MEMORY, $"R12{offset * 4}")
                    }));
                }
                else
                {
                    if (!dataSection.ContainsKey(forNode.Variable))
                        dataSection[forNode.Variable] = 0;
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.MEMORY, forNode.Variable)
                    }));
                }
            }

            // 辅助方法：存储 R0 到循环变量
            void EmitStoreLoopVar()
            {
                if (isLocalVar)
                {
                    int offset = localVarOffsets[forNode.Variable];
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.MEMORY, $"R12{offset * 4}"),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                }
                else
                {
                    if (!dataSection.ContainsKey(forNode.Variable))
                        dataSection[forNode.Variable] = 0;
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.MEMORY, forNode.Variable),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                }
            }

            // 初始化循环变量
            if (!isLocalVar && !dataSection.ContainsKey(forNode.Variable))
            {
                dataSection[forNode.Variable] = 0;
            }

            // 设置起始值
            GenerateExpression(forNode.StartValue);
            EmitStoreLoopVar();

            // 跳转到条件检查
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
            {
                new Operand(OperandType.LABEL, loopStartLabel)
            }));

            // 循环体标签
            AddLabel(loopBodyLabel);

            // 循环体
            GenerateStatement(forNode.Body);

            // 执行完循环体后跳转到递增
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
            {
                new Operand(OperandType.LABEL, loopIncrementLabel)
            }));

            // 循环开始标签（条件检查）
            AddLabel(loopStartLabel);

            // 加载循环变量值并 PUSH 到栈上保护
            EmitLoadLoopVar();
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0)
            }));

            // 加载结束值到R0 (可能使用 R1 作为临时寄存器)
            GenerateExpression(forNode.EndValue);

            // POP 循环变量值到 R1，比较
            instructions.Add(new Instruction(OpCode.POP, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 1)
            }));
            instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 1),
                new Operand(OperandType.REGISTER, 0)
            }));

            if (forNode.IsDownTo)
            {
                // FOR ... DOWNTO ... 循环变量递减
                instructions.Add(new Instruction(OpCode.JL, new List<Operand>
                {
                    new Operand(OperandType.LABEL, loopEndLabel)
                }));

                instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
                {
                    new Operand(OperandType.LABEL, loopBodyLabel)
                }));

                // 循环变量递减
                AddLabel(loopIncrementLabel);
                EmitLoadLoopVar();
                instructions.Add(new Instruction(OpCode.SUB, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 1)
                }));
                EmitStoreLoopVar();
            }
            else
            {
                // FOR ... TO ... 循环变量递增
                instructions.Add(new Instruction(OpCode.JG, new List<Operand>
                {
                    new Operand(OperandType.LABEL, loopEndLabel)
                }));

                instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
                {
                    new Operand(OperandType.LABEL, loopBodyLabel)
                }));

                // 循环变量递增
                AddLabel(loopIncrementLabel);
                EmitLoadLoopVar();
                instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 1)
                }));
                EmitStoreLoopVar();
            }

            // 跳回条件检查
            instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
            {
                new Operand(OperandType.LABEL, loopStartLabel)
            }));

            AddLabel(loopEndLabel);
            Sta!.PopLoopLabels();
        }

        private void GenerateRepeatStatement(RepeatNode repeatNode)
        {
            Sta!.EmitDoWhile(
                () => { foreach (var statement in repeatNode.Statements) GenerateStatement(statement); },
                () => GenerateExpression(repeatNode.Condition));
        }

        private void GenerateCaseStatement(CaseNode caseNode)
        {
            // 计算case表达式，结果在R0
            GenerateExpression(caseNode.Expression);

            // 保存case表达式值到R1 (寄存器保护)
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 1),
                new Operand(OperandType.REGISTER, 0)
            }));

            string caseEndLabel = $"case_end_{labelCounter++}";

            // break 标签栈 — 允许 case body 中使用 break 跳出
            Sta!.PushLoopLabels(caseEndLabel, null);

            // 为每个分支生成比较和跳转
            foreach (var branch in caseNode.Branches)
            {
                string branchLabel = $"case_branch_{labelCounter++}";

                // 检查值列表
                for (int i = 0; i < branch.Values.Count; )
                {
                    var value = branch.Values[i];

                    // 检查是否是范围 (当前值和下一个值组成范围)
                    if (i + 1 < branch.Values.Count &&
                        branch.Values[i + 1] is LiteralNode nextLit &&
                        value is LiteralNode currLit)
                    {
                        // 范围: currLit..nextLit (LOAD immediate 不修改 R1，无需 PUSH/POP)
                        int low = Convert.ToInt32(currLit.Value);
                        int high = Convert.ToInt32(nextLit.Value);

                        // 检查 R1 >= low
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.IMMEDIATE, low)
                        }));
                        instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 1),
                            new Operand(OperandType.REGISTER, 0)
                        }));
                        string skipLowLabel = $"case_skip_low_{labelCounter++}";
                        instructions.Add(new Instruction(OpCode.JL, new List<Operand>
                        {
                            new Operand(OperandType.LABEL, skipLowLabel)
                        }));

                        // 检查 R1 <= high
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.IMMEDIATE, high)
                        }));
                        instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 1),
                            new Operand(OperandType.REGISTER, 0)
                        }));
                        instructions.Add(new Instruction(OpCode.JLE, new List<Operand>
                        {
                            new Operand(OperandType.LABEL, branchLabel)
                        }));

                        AddLabel(skipLowLabel);
                        i += 2;
                    }
                    else
                    {
                        // 单个值 — PUSH R1 保护 switch 值, GenerateExpression 可能修改 R1
                        instructions.Add(new Instruction(OpCode.PUSH, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 1)
                        }));
                        GenerateExpression(value);
                        instructions.Add(new Instruction(OpCode.POP, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 1)
                        }));
                        instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.REGISTER, 1)
                        }));
                        instructions.Add(new Instruction(OpCode.JE, new List<Operand>
                        {
                            new Operand(OperandType.LABEL, branchLabel)
                        }));
                        i++;
                    }
                }

                // 如果都不匹配，跳到下一个分支
                string nextBranchLabel = $"case_next_{labelCounter++}";
                instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
                {
                    new Operand(OperandType.LABEL, nextBranchLabel)
                }));

                // 分支代码
                AddLabel(branchLabel);
                GenerateStatement(branch.Statement);
                instructions.Add(new Instruction(OpCode.JMP, new List<Operand>
                {
                    new Operand(OperandType.LABEL, caseEndLabel)
                }));

                AddLabel(nextBranchLabel);
            }

            // otherwise分支
            if (caseNode.OtherwiseBranch != null)
            {
                GenerateStatement(caseNode.OtherwiseBranch);
            }

            AddLabel(caseEndLabel);
            Sta!.PopLoopLabels();
        }

        private void GenerateAssignment(AssignmentNode assignment)
        {
            // 检测目标类型 (record 字段则用字段类型)
            bool targetIsFloat;
            bool targetIsSet;
            if (assignment.Variable.Field != null)
            {
                var (_, fieldType) = ResolveFieldChain(assignment.Variable.Name, assignment.Variable.Field, assignment.Variable.Fields);
                string ft = ResolveTypeName(fieldType);
                targetIsFloat = ft == "REAL";
                targetIsSet = ft == "SET";
            }
            else
            {
                targetIsFloat = IsFloatVariable(assignment.Variable.Name);
                targetIsSet = IsSetVariable(assignment.Variable.Name);
            }
            bool exprIsFloat = IsFloatExpression(assignment.Expression);
            bool needsConversion = (targetIsFloat && !exprIsFloat) || (!targetIsFloat && exprIsFloat);
            
            // 检测是否需要块复制 (SET 或 RECORD 多 slot 类型)
            bool targetIsRecord = false;
            if (!targetIsSet && assignment.Variable.Field == null)
            {
                string varTypeName = GetVariableType(assignment.Variable.Name);
                targetIsRecord = varTypeName == "RECORD" || (assignment.Variable.DereferenceCount == 0
                    && globalVarTypes.TryGetValue(assignment.Variable.Name, out var gt) && gt == "RECORD");
            }
            int copySlots = targetIsSet || targetIsRecord ? GetVariableSlotsForVariable(assignment.Variable.Name) : 0;

            if (copySlots > 1)
            {
                // 多 slot 块复制 (SET 或 RECORD) — v1.66.40 fix: 正确区分 load 和 store 指令
                // 计算目标地址 → 保存到 R2
                GenerateVariableAddress(assignment.Variable);
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 2),
                    new Operand(OperandType.REGISTER, 0)
                }));
                // 计算源地址 → 保存到 R0
                if (targetIsSet)
                    GenerateExpression(assignment.Expression);
                else
                    GenerateVariableAddress((assignment.Expression as VariableNode)!);
                // 块复制: R1←[R0+i*4], [R2+i*4]←R1 (v1.66.40: 用 MEMORY 操作数而非 3-op MOVE)
                for (int i = 0; i < copySlots; i++)
                {
                    if (i == 0)
                    {
                        // 第一个 slot: 直接间接寻址
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 1),
                            Mem("R0")
                        }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            Mem("R2"),
                            new Operand(OperandType.REGISTER, 1)
                        }));
                    }
                    else
                    {
                        // 后续 slot: 用 MEMORY 操作数的偏移格式
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 1),
                            new Operand(OperandType.MEMORY, $"R0+{i * 4}")
                        }));
                        instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                        {
                            new Operand(OperandType.MEMORY, $"R2+{i * 4}"),
                            new Operand(OperandType.REGISTER, 1)
                        }));
                    }
                }
            }
            else if (targetIsSet)
            {
                // 集合赋值 (单 slot, 保留原逻辑)
                GenerateVariableAddress(assignment.Variable);
                // 将目标地址保存到R2
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 2),
                    new Operand(OperandType.REGISTER, 0)
                }));
                
                // 获取源集合的地址（假设表达式是集合变量）
                GenerateExpression(assignment.Expression);
                // 源地址在R0中
                
                // 获取集合大小（槽位数）
                int slots = GetVariableSlotsForVariable(assignment.Variable.Name);
                
                // 复制集合数据
                for (int i = 0; i < slots; i++)
                {
                    // 加载源集合的第i个槽位到R1
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 1),
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, i * 4)
                    }));
                    
                    // 存储到目标集合的第i个槽位
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 1),
                        new Operand(OperandType.REGISTER, 2),
                        new Operand(OperandType.IMMEDIATE, i * 4)
                    }));
                }
            }
            else
            {
                // 非集合类型的赋值
                GenerateExpression(assignment.Expression);

                // 如果需要类型转换
                if (targetIsFloat && !exprIsFloat)
                {
                    // 整数转浮点
                    instructions.Add(new Instruction(OpCode.I2F, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                }
                else if (!targetIsFloat && exprIsFloat)
                {
                    // 浮点转整数（截断）
                    instructions.Add(new Instruction(OpCode.F2I, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                }

                // 保存表达式的值到R1 (仅非浮点类型; 浮点值已在F0中)
                if (!targetIsFloat)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 1),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                }

                GenerateVariableAddress(assignment.Variable);

                // 使用数据类型敏感指令选择 (record 字段用字段类型)
                PascalType varType;
                if (assignment.Variable.Field != null)
                {
                    var (_, fieldType) = ResolveFieldChain(assignment.Variable.Name, assignment.Variable.Field, assignment.Variable.Fields);
                    varType = GetPascalType(ResolveTypeName(fieldType));
                }
                else if (assignment.Variable.DereferenceCount > 0)
                    varType = GetPointedPascalType(assignment.Variable.Name);
                else
                    varType = GetVariablePascalType(assignment.Variable.Name);
                OpCode storeOp = GetStoreInstruction(varType);
                if (targetIsFloat)
                {
                    // 统一 store: dest=mem first (MOVEF [R0], R0 — 存储 R0 的浮点值到 R0 指向的地址)
                    instructions.Add(new Instruction(storeOp, new List<Operand>
                    {
                        Mem("R0"),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                }
                else
                {
                    // 统一 store: dest=indirect first (MOVE (R0), R1 — 存储 R1 到 R0 指向的地址)
                    instructions.Add(new Instruction(storeOp, new List<Operand>
                    {
                        Mem("R0"),
                        new Operand(OperandType.REGISTER, 1)
                    }));
                }
            }
        }
        
        private void GenerateVariableAddress(VariableNode variable)
        {
            if (variable.DereferenceCount > 0)
            {
                var baseVariable = new VariableNode { Name = variable.Name, Line = variable.Line, Column = variable.Column };
                GenerateExpression(baseVariable);
                for (int i = 1; i < variable.DereferenceCount; i++)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.REGISTER, 0)
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
                return;
            }

            if (variable.Indices.Count > 0 && dynamicArrayNames.Contains(variable.Name))
            {
                GenerateExpression(new VariableNode { Name = variable.Name, Line = variable.Line, Column = variable.Column });
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
                return;
            }

            if (paramOffsets.ContainsKey(variable.Name) && varParameters.Contains(variable.Name))
            {
                // var参数: 参数本身是地址, 直接加载
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
                // 现在R0是参数的地址(即var参数的值), 再加载一次得到实际变量地址
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.REGISTER, 0)
                }));
                return;
            }
            
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
                
                if (variable.Indices.Count > 0)
                {
                    // 保存基地址到 R2 (v1.66.33 fix: 防止被索引计算覆盖)
                    int arrOff = localVarOffsets[variable.Name] * 4;
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, arrOff)
                    }));
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 2),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                    // 处理索引: R0 = (index - lower) * 4
                    GenerateExpression(variable.Indices[0]);
                    int arrLower = arrayBounds.TryGetValue(variable.Name, out var ab) && ab.Count > 0 ? ab[0].lower : 1;
                    if (arrLower != 0)
                    {
                        instructions.Add(new Instruction(OpCode.SUB, new List<Operand>
                        {
                            new Operand(OperandType.REGISTER, 0),
                            new Operand(OperandType.IMMEDIATE, arrLower)
                        }));
                    }
                    instructions.Add(new Instruction(OpCode.MUL, new List<Operand>
                    {
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
                return;
            }
            
            // 检查是否是函数返回值（在函数内部）
            if (isInSubprogram && localVarOffsets.ContainsKey(variable.Name))
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
                return;
            }
            
            // 全局变量 — 仅当尚未分配时才初始化 (v1.66.33 fix: 数组由 AllocateVariable 分配正确大小)
            if (!dataSection.ContainsKey(variable.Name) && !globalVarTypes.ContainsKey(variable.Name))
            {
                dataSection[variable.Name] = 0;
            }
            
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, 0),
                new Operand(OperandType.LABEL, variable.Name)
            }));
            
            if (variable.Indices.Count > 0)
            {
                if (variable.Indices.Count == 1)
                {
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 2),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                    
                    GenerateExpression(variable.Indices[0]);
                    
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 3),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                    
                    // 使用真实数组边界（回退到 [1..10]）
                    var arrBoundsList = arrayBounds.TryGetValue(variable.Name, out var abl) ? abl : null;
                    int arrLower = arrBoundsList != null && arrBoundsList.Count > 0 ? arrBoundsList[0].lower : 1;
                    int arrUpper = arrBoundsList != null && arrBoundsList.Count > 0 ? arrBoundsList[0].upper : 10;

                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 4),
                        new Operand(OperandType.IMMEDIATE, arrLower)
                    }));
                    instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 3),
                        new Operand(OperandType.REGISTER, 4)
                    }));
                    
                    string lowerBoundLabel = GenerateLabel();
                    instructions.Add(new Instruction(OpCode.JGE, new List<Operand>
                    {
                        new Operand(OperandType.LABEL, lowerBoundLabel)
                    }));
                    GenerateRuntimeError("数组索引越界（小于下界）");
                    AddLabel(lowerBoundLabel);
                    
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 4),
                        new Operand(OperandType.IMMEDIATE, arrUpper)
                    }));
                    instructions.Add(new Instruction(OpCode.CMP, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 3),
                        new Operand(OperandType.REGISTER, 4)
                    }));
                    
                    string upperBoundLabel = GenerateLabel();
                    instructions.Add(new Instruction(OpCode.JLE, new List<Operand>
                    {
                        new Operand(OperandType.LABEL, upperBoundLabel)
                    }));
                    GenerateRuntimeError("数组索引越界（大于上界）");
                    AddLabel(upperBoundLabel);
                    
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.REGISTER, 3)
                    }));
                    
                    instructions.Add(new Instruction(OpCode.SUB, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, arrLower)
                    }));
                    
                    instructions.Add(new Instruction(OpCode.MUL, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.IMMEDIATE, 4)
                    }));
                    
                    // ⚠ v0.96.191 修：这里原来是 `SUB R2, R0` —— **地址算反了方向**。
                    //   数组块在数据段里是**向上**排的（汇编器 `labels[label] = currentAddress`
                    //   指向块的第一个字），元素地址必须是 `base + (i - 下界) * 元素大小`。
                    //   用 SUB 的后果：下标 = 下界时侥幸对（偏移 0），**下标 ≥ 下界+1 就写到数组
                    //   "前面"去了** —— 实测 `a: array[1..8] of integer` 里 `a[3] := 5` 把 8 字节
                    //   写到了数据段起点之前，程序随即跑飞：连 `ui_win_open` 都没调到、**一帧不出**。
                    //   （诊断口径：`--trace` 下 500–599 号段一次调用都没有 ⇒ 根本没走到开窗。）
                    instructions.Add(new Instruction(OpCode.ADD, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 2),
                        new Operand(OperandType.REGISTER, 0)
                    }));
                    
                    instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
                    {
                        new Operand(OperandType.REGISTER, 0),
                        new Operand(OperandType.REGISTER, 2)
                    }));
                }
                else
                {
                    // 多维数组支持
                    var bounds = arrayBounds.TryGetValue(variable.Name, out var bl) ? bl : null;
                    int elemSize = 4;
                    for (int d = 0; d < variable.Indices.Count; d++)
                    {
                        int dimLower = (bounds != null && d < bounds.Count) ? bounds[d].lower : 1;
                        int dimUpper = (bounds != null && d < bounds.Count) ? bounds[d].upper : 10;
                        int dimSize = dimUpper - dimLower + 1;

                        // 计算后续维度的总大小
                        int stride = elemSize;
                        for (int k = d + 1; k < variable.Indices.Count; k++)
                        {
                            int kl = (bounds != null && k < bounds.Count) ? bounds[k].lower : 1;
                            int ku = (bounds != null && k < bounds.Count) ? bounds[k].upper : 10;
                            stride *= (ku - kl + 1);
                        }

                        // 生成此维度的索引表达式
                        if (d == 0)
                            GenerateExpression(variable.Indices[d]);
                        else
                        {
                            // 暂存当前R0，计算下一个索引
                            instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
                            GenerateExpression(variable.Indices[d]);
                            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 1)]));
                            // R0 = index, R1 = accumulated
                        }

                        // 减去下限
                        instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, dimLower)]));
                        // 乘以步长
                        if (stride > 1)
                            instructions.Add(new Instruction(OpCode.MUL, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, stride)]));
                    }
                    // 同上：多维路径也是"基址 + 偏移"，不是减
                    instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 2), new Operand(OperandType.REGISTER, 0)]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.REGISTER, 2)]));
                }
            }
            
            // 处理record字段访问 (支持多级 b.a.v)
            if (variable.Field != null)
            {
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
            }
        }
        
        private string GetVariableRecordType(string varName)
        {
            if (variableRecordTypes.ContainsKey(varName))
                return variableRecordTypes[varName];
            if (localVarTypes.ContainsKey(varName))
            {
                string typeName = localVarTypes[varName];
                if (definedRecordTypes.ContainsKey(typeName))
                    return typeName;
            }
            if (globalVarTypes.ContainsKey(varName))
            {
                string typeName = globalVarTypes[varName];
                if (definedRecordTypes.ContainsKey(typeName))
                    return typeName;
                // 检查是否是数组并查找元素记录类型
                if (astNode is ProgramNode prog)
                {
                    foreach (var decl in prog.Declarations)
                    {
                        if (decl is VarDeclarationNode varDecl && varDecl.Name.ToUpper() == varName.ToUpper())
                        {
                            if (varDecl.Type is ArrayTypeNode arrType && arrType.ElementType is SimpleTypeNode elemSimple)
                            {
                                if (definedRecordTypes.ContainsKey(elemSimple.TypeName.ToUpper()))
                                    return elemSimple.TypeName.ToUpper();
                            }
                        }
                    }
                }
            }
            // 搜索子程序局部变量声明
            if (astNode is ProgramNode prog2)
            {
                foreach (var sub in prog2.Subprograms)
                {
                    foreach (var decl in sub.LocalVariables)
                    {
                        if (decl is VarDeclarationNode varDecl && varDecl.Name.ToUpper() == varName.ToUpper())
                        {
                            if (varDecl.Type is ArrayTypeNode arrType && arrType.ElementType is SimpleTypeNode elemSimple)
                            {
                                if (definedRecordTypes.ContainsKey(elemSimple.TypeName.ToUpper()))
                                    return elemSimple.TypeName.ToUpper();
                            }
                        }
                    }
                }
            }
            return null;
        }

        private (int totalOffset, string finalType) ResolveFieldChain(string varName, string firstField, List<string> additionalFields)
        {
            string recordTypeName = GetVariableRecordType(varName);
            if (recordTypeName == null || !recordFieldLayouts.ContainsKey(recordTypeName))
                throw new CompilationException(ErrorCode.CodeGen_TypeMismatch, $"变量 '{varName}' 不是record类型，无法访问字段");

            int totalOffset = 0;
            string currentRecordType = recordTypeName;
            string finalType = "";

            var allFields = new List<string> { firstField.ToUpper() };
            allFields.AddRange(additionalFields);

            foreach (var fieldName in allFields)
            {
                var layout = recordFieldLayouts[currentRecordType];
                string upperField = fieldName.ToUpper();
                if (!layout.ContainsKey(upperField))
                    throw new CompilationException(ErrorCode.CodeGen_TypeMismatch, $"record类型 '{currentRecordType}' 中不存在字段 '{fieldName}'");

                var (offset, type) = layout[upperField];
                totalOffset += offset;
                finalType = type;

                string upperType = type.ToUpper();
                if (definedRecordTypes.ContainsKey(upperType))
                    currentRecordType = upperType;
            }

            return (totalOffset, finalType);
        }
    }
}
