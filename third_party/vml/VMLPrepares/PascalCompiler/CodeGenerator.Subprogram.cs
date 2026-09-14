using System;
using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace PascalCompiler
{
    public partial class CodeGenerator
    {
        private void GenerateSubprogram(SubprogramDeclarationNode subprogram)
        {
            // 保存当前状态
            var prevLocalOffsets = new Dictionary<string, int>(localVarOffsets);
            var prevLocalSize = currentLocalSize;
            var prevParamSize = currentParamSize;
            var prevInSubprogram = isInSubprogram;
            var prevVariableRecordTypes = new Dictionary<string, string>(variableRecordTypes);
            var prevLocalDeclarations = new Dictionary<string, TypeNode>(localVarDeclarations);
            
            localVarOffsets.Clear();
            localVarDeclarations.Clear();
            currentLocalSize = 0;
            currentParamSize = 0;
            isInSubprogram = true;
            varParameters.Clear();
            paramOffsets.Clear();
            // 保留全局变量的record类型映射
            // variableRecordTypes 不清除，只清除后续添加的局部变量条目
            
            // 计算参数大小 (每个参数4字节)
            currentParamSize = subprogram.Parameters.Count;
            
            // 记录参数偏移和var参数
            // 参数在BP正方向: BP+8 (返回地址), BP+12 (第1个参数), BP+16 (第2个参数)...
            for (int i = 0; i < subprogram.Parameters.Count; i++)
            {
                var param = subprogram.Parameters[i];
                paramOffsets[param.Name] = 8 + 4 * i; // BP+8+4*i
                if (param.IsVarParameter)
                {
                    varParameters.Add(param.Name);
                }
            }
            
            // 计算局部变量大小
            foreach (var varDecl in subprogram.LocalVariables)
            {
                TypeNode resolvedType = ResolveTypeAlias(varDecl.Type);
                int slots = GetVariableSlots(resolvedType);
                if (slots < 1) slots = 1;
                // 记录数组边界（支持多维）
                if (resolvedType is ArrayTypeNode arrType)
                {
                    if (arrType.IsDynamic)
                        dynamicArrayNames.Add(varDecl.Name);
                    else
                    {
                        var bounds = new List<(int, int)>();
                        CollectArrayBounds(arrType, bounds);
                        arrayBounds[varDecl.Name] = bounds;
                    }
                }
                localVarOffsets[varDecl.Name] = -(currentLocalSize + 1); // BP负方向
                localVarTypes[varDecl.Name] = GetTypeName(resolvedType);
                localVarDeclarations[varDecl.Name] = resolvedType;
                
                // 记录变量对应的record类型
                if (varDecl.Type is RecordTypeNode recordType)
                {
                    string recordTypeName = FindRecordTypeName(recordType);
                    if (recordTypeName != null)
                        variableRecordTypes[varDecl.Name] = recordTypeName;
                }
                else if (varDecl.Type is SimpleTypeNode simpleType)
                {
                    if (definedRecordTypes.ContainsKey(simpleType.TypeName.ToUpper()))
                        variableRecordTypes[varDecl.Name] = simpleType.TypeName.ToUpper();
                }
                
                currentLocalSize += slots;
            }
            
            // 如果是函数，添加隐式返回值变量
            if (subprogram is FunctionDeclarationNode funcDecl)
            {
                localVarOffsets[funcDecl.Name] = -(currentLocalSize + 1); // BP负方向
                localVarTypes[funcDecl.Name] = GetTypeName(funcDecl.ReturnType);
                currentLocalSize += 1;
                functionNames.Add(funcDecl.Name.ToLower());
            }
            
            // 生成子程序标签
            AddLabel(subprogram.Name);

            // VML function-level comments
            Emit(OpCode.NOP, new List<Operand>(), "; --------------------------------------------");
            // Build source declaration
            if (subprogram is FunctionDeclarationNode funcNode)
            {
                var sourceDecl = "function " + subprogram.Name + "(";
                for (var i = 0; i < subprogram.Parameters.Count; i++)
                {
                    sourceDecl += subprogram.Parameters[i].Name + ": " + GetTypeName(subprogram.Parameters[i].Type);
                    if (i < subprogram.Parameters.Count - 1) sourceDecl += "; ";
                }
                sourceDecl += "): " + GetTypeName(funcNode.ReturnType);
                instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; source   : {sourceDecl}"));
                instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; function : {subprogram.Name}"));
            }
            else
            {
                var sourceDecl = "procedure " + subprogram.Name + "(";
                for (var i = 0; i < subprogram.Parameters.Count; i++)
                {
                    sourceDecl += subprogram.Parameters[i].Name + ": " + GetTypeName(subprogram.Parameters[i].Type);
                    if (i < subprogram.Parameters.Count - 1) sourceDecl += "; ";
                }
                sourceDecl += ")";
                instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; source   : {sourceDecl}"));
                instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; procedure: {subprogram.Name}"));
            }
            foreach (var param in subprogram.Parameters)
            {
                instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; param    : {param.Name}: {GetTypeName(param.Type)}"));
            }
            if (subprogram is FunctionDeclarationNode fnDecl)
                instructions.Add(new Instruction(OpCode.NOP, [], 0, $"; return   : {GetTypeName(fnDecl.ReturnType)}"));
            else
                instructions.Add(new Instruction(OpCode.NOP, [], 0, "; return   : (none)"));
            Emit(OpCode.NOP, new List<Operand>(), "; --------------------------------------------");

            // 栈帧序言: ENTER localSize
            instructions.Add(new Instruction(OpCode.ENTER, new List<Operand> { new Operand(OperandType.IMMEDIATE, currentLocalSize * 4) }));

            // 构造函数: 分配对象内存 (v1.66.32+)
            if (subprogram.IsConstructor)
            {
                instructions.Add(new Instruction(OpCode.NOP, [], 0, "; constructor — alloc object"));
                instructions.Add(new Instruction(OpCode.MOVE, [new Operand(OperandType.REGISTER, 0), new Operand(OperandType.IMMEDIATE, 64)]));
                instructions.Add(new Instruction(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 40)]));
            }

            // exit 过程的目标标签 — 必须在 GenerateBlock 之前设置 (v1.66.35 fix)
            string exitLabel = $"exit_{subprogram.Name}";
            subprogramExitLabels.Push(exitLabel);
            // 占位：exit 标签的实际位置将在函数体生成后设置

            // 生成函数体
            GenerateBlock(subprogram.Body);

            // 析构函数: 释放对象内存 (v1.66.32+)
            if (subprogram.IsDestructor)
            {
                instructions.Add(new Instruction(OpCode.NOP, [], 0, "; destructor — free object"));
                instructions.Add(new Instruction(OpCode.SYSCALL, [new Operand(OperandType.IMMEDIATE, 41)]));
            }

            // 设置 exit 标签的实际位置
            labels[exitLabel] = instructions.Count;

            // 加载函数返回值到R0 (使用类型感知的 load 指令)
            if (subprogram is FunctionDeclarationNode funcRet)
            {
                int retOffset = localVarOffsets.TryGetValue(funcRet.Name, out int off) ? off : -1;
                PascalType retType = GetPascalType(GetTypeName(funcRet.ReturnType));
                OpCode loadOp = GetLoadInstruction(retType);
                instructions.Add(new Instruction(loadOp, new List<Operand>
                {
                    new Operand(OperandType.REGISTER, 0),
                    new Operand(OperandType.MEMORY, $"R12{retOffset * 4}")
                }));
            }
            // 栈帧尾声: LEAVE; RET
            instructions.Add(new Instruction(OpCode.LEAVE, new List<Operand>()));
            instructions.Add(new Instruction(OpCode.RET, new List<Operand>()));
            subprogramExitLabels.Pop();

            // 生成嵌套子程序（Turbo Pascal嵌套过程/函数）
            foreach (var nested in subprogram.NestedSubprograms)
            {
                GenerateSubprogram(nested);
            }

            // 恢复状态
            localVarOffsets = prevLocalOffsets;
            localVarTypes.Clear();
            localVarDeclarations = prevLocalDeclarations;
            variableRecordTypes = prevVariableRecordTypes;
            varParameters.Clear();
            paramOffsets.Clear();
            currentLocalSize = prevLocalSize;
            currentParamSize = prevParamSize;
            isInSubprogram = prevInSubprogram;
        }

        private void GenerateBlock(BlockNode block)
        {
            foreach (var statement in block.Statements)
            {
                GenerateStatement(statement);
            }
        }
    }
}
