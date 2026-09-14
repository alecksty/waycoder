using VMLAssembler;
using System.Collections.Generic;
using CompilerBase;

namespace JavaScriptCompiler
{
    public partial class CodeGenerator
    {
        private void GenerateArrayDestructure(ArrayDestructureStatement arrDest)
        {
            if (arrDest.Initializer == null) return;

            GenerateExpression(arrDest.Initializer);
            // R0 = array pointer
            AddRR(OpCode.MOVE, 1, 0);

            for (int i = 0; i < arrDest.Names.Count; i++)
            {
                string varLabel = $"var_{arrDest.Names[i]}";
                if (!dataSection.ContainsKey(varLabel))
                    dataSection[varLabel] = 0;
                // Load element: arr[4 + i*4]
                AddInstruction(OpCode.MOVE, Reg(0), Mem($"R1+{4 + i * 4}"));
                // 全局变量存储: LEA R1, varLabel; MOVE [R1], R0
                AddInstruction(OpCode.MOVE, Reg(1), LabelOp(varLabel));
                instructions.Add(new Instruction(OpCode.MOVE, [Mem("R1"), Reg(0)]));
            }
        }

        private void GenerateObjectDestructure(ObjectDestructureStatement objDest)
        {
            if (objDest.Initializer == null) return;

            GenerateExpression(objDest.Initializer);
            // R0 = object pointer, each property at [R0 + offset]
            int objFieldOff = 0;
            foreach (var name in objDest.Names)
            {
                string varLabel = $"var_{name}";
                if (!dataSection.ContainsKey(varLabel))
                    dataSection[varLabel] = 0;
                objFieldOff += 4;
                // Load property value: R0 = [R0+fieldOff]
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, $"R0+{objFieldOff}")]));
                // Global store: LEA R1, varLabel; MOVE [R1], R0
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, varLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Mem("R1"), Reg(0)]));
            }
        }

        private void GenerateVariableDecl(VariableDeclStatement varDecl)
        {
            if (varDecl.Initializer != null)
            {
                GenerateExpression(varDecl.Initializer);
                string varLabel = $"var_{varDecl.Name}";
                if (!dataSection.ContainsKey(varLabel))
                    dataSection[varLabel] = 0;
                // 全局变量: LEA R1, varLabel; MOVE [R1], R0 (存储 R0 到标签地址)
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.REGISTER, 1),
                    new Operand(OperandType.LABEL, varLabel)
                }));
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.MEMORY, "R1"),
                    new Operand(OperandType.REGISTER, 0)
                }));
                // Track variable -> class mapping for prototype chain property access
                if (varDecl.Initializer is NewExpression newExpr)
                    _varClassMap[varDecl.Name] = newExpr.Type;
            }
        }
        
        private void GenerateFunctionDecl(FunctionDeclStatement funcDecl)
        {
            string funcLabel = funcDecl.Name;
            AddLabel(funcLabel);

            // native 函数：标签已添加，跳过函数体（由外部共享库提供实现）
            if (funcDecl.IsNative)
                return;

            // 添加函数注释
            instructions.Add(new Instruction(OpCode.NOP, [], 0, "; -------------------------------------------"));
            // 生成源函数声明
            var sourceDecl = $"function {funcDecl.Name}(";
            for (var i = 0; i < funcDecl.Parameters.Count; i++)
            {
                sourceDecl += $"var {funcDecl.Parameters[i].Name}";
                if (i < funcDecl.Parameters.Count - 1)
                {
                    sourceDecl += ",";
                }
            }
            sourceDecl += ")";
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; source   : {sourceDecl}"));
            // 生成函数名注释
            instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; function : {funcDecl.Name}"));
            // 生成参数注释
            foreach (var param in funcDecl.Parameters)
            {
                instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; param   : var {param.Name}"));
            }
            // 生成返回类型注释
            instructions.Add(new Instruction(OpCode.NOP, [], 0, "; return   : var"));
            instructions.Add(new Instruction(OpCode.NOP, [], 0, "; --------------------------------------------"));

            // 函数序言
            instructions.Add(new Instruction(OpCode.PUSH, new List<Operand> {
                new Operand(OperandType.REGISTER, 14) // 保存BP
            }));
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                new Operand(OperandType.REGISTER, 14),
                new Operand(OperandType.REGISTER, 13)
            }));

            // 重置局部变量表，使用栈帧存储(R14负偏移)
            Vars.ResetLocals();
            _localVarOffsets.Clear();
            _localVarSize = 0;

            // 从栈上读取参数并存储到栈帧
            int paramIndex = 0;
            foreach (var paramDef in funcDecl.Parameters)
            {
                _localVarSize += 4;
                _localVarOffsets[paramDef.Name] = -_localVarSize;
                Vars.AllocLocal(paramDef.Name, 4);
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, Vars.FormatOffset(8 + paramIndex * 4))]));

                // 默认参数：如果参数值为0/null，使用默认值
                if (paramDef.DefaultValue != null)
                {
                    string skipDefaultLabel = $"skip_default_{labelCounter++}";
                    // R0 holds the parameter value; if non-zero, skip default
                    instructions.Add(new Instruction(OpCode.JNZ, [Reg(0), new Operand(OperandType.LABEL, skipDefaultLabel)]));
                    // R0 is 0 (null/undefined), evaluate default expression
                    GenerateExpression(paramDef.DefaultValue);
                    labels[skipDefaultLabel] = instructions.Count;
                }

                // 存储参数到本地栈槽: MOVE [R14-offset], R0 (统一 STORE 格式 - 内存优先)
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, Vars.FormatOffset(-_localVarSize)), Reg(0)]));
                paramIndex++;
            }

            // 分配栈空间
            if (_localVarSize > 0)
                instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, _localVarSize)]));

            // 生成函数体
            GenerateBlock(funcDecl.Body);

            // 函数尾声：恢复栈帧并返回
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 14)]));
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 14)]));
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>()));
        }

        private void GenerateClassDecl(ClassDeclStatement classDecl)
        {
            string className = classDecl.Name;

            // Register parent class for method lookup fallback
            if (!string.IsNullOrEmpty(classDecl.ParentClass))
            {
                _classParentMap[className] = classDecl.ParentClass;
            }

            // Generate methods as {ClassName}_{methodName}
            foreach (var method in classDecl.Methods)
            {
                string savedName = method.Name;
                method.Name = $"{className}_{savedName}";
                GenerateFunctionDecl(method);
                method.Name = savedName;
            }

            // Generate constructor
            if (classDecl.Constructor != null)
            {
                var ctor = classDecl.Constructor;
                string ctorLabel = className;
                labels[ctorLabel] = instructions.Count;

                instructions.Add(new Instruction(OpCode.NOP, [], 0, "; -------------------------------------------"));
                instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; class    : {className}"));
                if (!string.IsNullOrEmpty(classDecl.ParentClass))
                    instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; extends  : {classDecl.ParentClass}"));
                instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; constructor"));
                instructions.Add(new Instruction(OpCode.NOP, [], 0, "; --------------------------------------------"));

                // Prologue
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 14)]));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 14), new Operand(OperandType.REGISTER, 13)]));

                // Allocate object: proto_ptr(4 bytes) + fields
                int estimatedFields = 8;
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, estimatedFields * 4 + 4)]));
                EmitAlloc();

                // Store proto_ptr: if extends, set to parent class label; otherwise 0
                if (!string.IsNullOrEmpty(classDecl.ParentClass))
                {
                    // Store parent class reference as prototype pointer at offset 0
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.LABEL, $"proto_{classDecl.ParentClass}")]));
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), Reg(1)]));
                }
                else
                {
                    instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.MEMORY, "R0"), new Operand(OperandType.IMMEDIATE, 0)]));
                }

                // Store object pointer as "this" label in data section
                string thisLabel = $"this_{labelCounter++}";
                dataSection[thisLabel] = 0;
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, thisLabel), Reg(0)]));

                // Reset locals and per-class field tracking
                Vars.ResetLocals();
                _localVarOffsets.Clear();
                _localVarSize = 0;
                _currentClassName = className;
                if (!_classFieldMaps.ContainsKey(className))
                    _classFieldMaps[className] = new Dictionary<string, int>();
                // Inherit parent fields
                if (!string.IsNullOrEmpty(classDecl.ParentClass) && _classFieldMaps.TryGetValue(classDecl.ParentClass, out var parentFields))
                {
                    foreach (var pf in parentFields)
                        _classFieldMaps[className][pf.Key] = pf.Value;
                }

                // Read constructor params from stack
                int ctorParamIndex = 0;
                foreach (var paramDef in ctor.Parameters)
                {
                    _localVarSize += 4;
                    _localVarOffsets[paramDef.Name] = -_localVarSize;
                    Vars.AllocLocal(paramDef.Name, 4);
                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, Vars.FormatOffset(8 + ctorParamIndex * 4))]));

                    if (paramDef.DefaultValue != null)
                    {
                        string skipDefaultLabel = $"skip_default_{labelCounter++}";
                        instructions.Add(new Instruction(OpCode.JNZ, [Reg(0), new Operand(OperandType.LABEL, skipDefaultLabel)]));
                        GenerateExpression(paramDef.DefaultValue);
                        labels[skipDefaultLabel] = instructions.Count;
                    }

                    instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, Vars.FormatOffset(-_localVarSize))]));
                    ctorParamIndex++;
                }

                if (_localVarSize > 0)
                    instructions.Add(new Instruction(OpCode.SUB, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, _localVarSize)]));

                // Generate constructor body
                GenerateBlock(ctor.Body);

                // Return the allocated object (this)
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.LABEL, thisLabel)]));
                instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.MEMORY, "R0")]));

                // Epilogue
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 14)]));
                instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 14)]));
                instructions.Add(new Instruction(OpCode.RET, []));

                _currentClassName = null;
            }
        }

        private void GenerateNewExpression(NewExpression newExpr)
        {
            // Push constructor arguments in reverse order
            for (int i = newExpr.Arguments.Count - 1; i >= 0; i--)
            {
                GenerateExpression(newExpr.Arguments[i]);
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
            }

            string ctorLabel = newExpr.Type;
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, ctorLabel)]));

            // Pop arguments
            if (newExpr.Arguments.Count > 0)
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, newExpr.Arguments.Count * 4)]));
        }

        /// <summary>JS 'in' operator: "key" in obj — 编译期属性查找, 运行时默认1</summary>
        private void GenerateInExpression(InExpression inExpr)
        {
            // 编译期优化: key 是字符串字面量时, 检查类是否包含此属性
            if (inExpr.Left is LiteralExpression keyLit && keyLit.Value is string key)
            {
                string? clsName = null;
                if (inExpr.Right is VariableExpression varExpr && varExpr.Name == "this")
                    clsName = _currentClassName;
                else if (inExpr.Right is VariableExpression v2 && _classFieldMaps.ContainsKey(v2.Name))
                    clsName = v2.Name;

                // 沿原型链查找属性
                while (clsName != null)
                {
                    if (_classFieldMaps.TryGetValue(clsName, out var fields) && fields.ContainsKey(key))
                    {
                        AddRI(OpCode.MOVE, 0, 1);  // 找到 → true
                        return;
                    }
                    _classParentMap.TryGetValue(clsName, out clsName);
                }
                // 编译期未找到 → 运行时也可能有 (动态属性)
            }
            // 求值两侧, 默认返回1 (乐观: 对象可能包含此属性)
            GenerateExpression(inExpr.Left);
            GenerateExpression(inExpr.Right);
            AddRI(OpCode.MOVE, 0, 1);
        }

        private void GenerateInstanceof(InstanceofExpression instExpr)
        {
            string targetProto = $"proto_{instExpr.TypeName}";
            // Ensure proto label exists in data section
            if (!dataSection.ContainsKey(targetProto))
                dataSection[targetProto] = 0;

            string loopLabel = $"inst_loop_{labelCounter++}";
            string foundLabel = $"inst_found_{labelCounter++}";
            string notFoundLabel = $"inst_notfound_{labelCounter++}";

            // R0 = object to check
            GenerateExpression(instExpr.Left);
            // Walk prototype chain at runtime
            labels[loopLabel] = instructions.Count;
            instructions.Add(new Instruction(OpCode.CMP, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
            instructions.Add(new Instruction(OpCode.JZ, [Reg(0), new Operand(OperandType.LABEL, notFoundLabel)]));
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(1), new Operand(OperandType.MEMORY, "R0")])); // R1 = obj.proto_ptr
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(2), new Operand(OperandType.LABEL, targetProto)])); // R2 = target proto address
            instructions.Add(new Instruction(OpCode.CMP, [Reg(1), Reg(2)]));
            instructions.Add(new Instruction(OpCode.JE, [Reg(1), new Operand(OperandType.LABEL, foundLabel)]));
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), Reg(1)])); // obj = obj.proto
            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, loopLabel)]));
            labels[notFoundLabel] = instructions.Count;
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 0)]));
            instructions.Add(new Instruction(OpCode.JMP, [new Operand(OperandType.LABEL, $"inst_end_{notFoundLabel}")]));
            labels[foundLabel] = instructions.Count;
            instructions.Add(new Instruction(OpCode.MOVE, [Reg(0), new Operand(OperandType.IMMEDIATE, 1)]));
            labels[$"inst_end_{notFoundLabel}"] = instructions.Count;
        }

        private void GenerateSuperCall(SuperExpression superExpr)
        {
            // Look up parent class name
            if (_currentClassName == null || !_classParentMap.TryGetValue(_currentClassName, out string parentClass))
                throw new CompilationException(ErrorCode.CodeGen_UndefinedFunction, "super() can only be called inside a subclass constructor");

            // Push arguments in reverse order (same as GenerateNewExpression)
            for (int i = superExpr.Arguments.Count - 1; i >= 0; i--)
            {
                GenerateExpression(superExpr.Arguments[i]);
                instructions.Add(new Instruction(OpCode.PUSH, [new Operand(OperandType.REGISTER, 0)]));
            }

            // CALL parent class constructor
            instructions.Add(new Instruction(OpCode.CALL, [new Operand(OperandType.LABEL, parentClass)]));

            // Pop arguments
            if (superExpr.Arguments.Count > 0)
                instructions.Add(new Instruction(OpCode.ADD, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.IMMEDIATE, superExpr.Arguments.Count * 4)]));

            // Update this label to point to parent-allocated object (R0)
            string thisLabel = FindThisLabel();
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.LABEL, thisLabel), Reg(0)]));
        }

        private void GenerateReturn(ReturnStatement returnStmt)
        {
            if (returnStmt.Value != null)
            {
                GenerateExpression(returnStmt.Value);
            }
            else
            {
                instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.IMMEDIATE, 0)
                }));
            }
            
            instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 13), new Operand(OperandType.REGISTER, 14)]));
            instructions.Add(new Instruction(OpCode.POP, [new Operand(OperandType.REGISTER, 14)]));
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>()));
        }
        
        private void GenerateIf(IfStatement ifStmt)
        {
            Sta!.EmitIf(
                () => GenerateExpression(ifStmt.Condition),
                () => GenerateStatement(ifStmt.ThenBranch),
                ifStmt.ElseBranch != null ? () => GenerateStatement(ifStmt.ElseBranch) : null);
        }
        
        private void GenerateWhile(WhileStatement whileStmt)
        {
            Sta!.EmitWhile(
                () => GenerateExpression(whileStmt.Condition),
                () => GenerateStatement(whileStmt.Body));
        }
        
        private void GenerateFor(ForStatement forStmt)
        {
            System.Action? emitInit = forStmt.Initializer != null
                ? () => GenerateStatement(forStmt.Initializer) : null;

            Sta!.EmitFor(
                emitInit,
                forStmt.Condition != null ? () => GenerateExpression(forStmt.Condition) : null,
                forStmt.Increment != null ? () => GenerateExpression(forStmt.Increment) : null,
                () => GenerateStatement(forStmt.Body));
        }
        
        private void GenerateDoWhile(DoWhileStatement doStmt)
        {
            Sta!.EmitDoWhile(
                () => GenerateStatement(doStmt.Body),
                () => GenerateExpression(doStmt.Condition));
        }

        private void GenerateSwitch(SwitchStatement switchStmt)
        {
            var emitCaseValues = new List<Action>();
            var caseBodies = new List<Action>();
            Action? defaultBody = null;

            foreach (var sc in switchStmt.Cases)
            {
                if (sc.Value == null)
                {
                    defaultBody = () =>
                    {
                        foreach (var stmt in sc.Body)
                            GenerateStatement(stmt);
                    };
                }
                else
                {
                    var capSc = sc;
                    emitCaseValues.Add(() => GenerateExpression(capSc.Value!));
                    caseBodies.Add(() =>
                    {
                        foreach (var stmt in capSc.Body)
                            GenerateStatement(stmt);
                    });
                }
            }

            Sta!.EmitSwitchCustom(
                () => GenerateExpression(switchStmt.Value),
                emitCaseValues,
                caseBodies,
                defaultBody
            );
        }

    }
}
