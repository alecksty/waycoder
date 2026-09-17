using System;
using System.Collections.Generic;
using System.Text;
using VMLAssembler;
using CompilerBase;

namespace RustCompiler
{
    public partial class CodeGenerator
    {
        public void Visit(VariableDeclarationNode node)
        {
            // 使用声明的类型，如果有的话；否则从初始值推断
            string varType = (node.Type != null && node.Type != "i32")
                ? node.Type.ToLower()
                : InferTypeFromExpression(node.Initializer ?? new LiteralNode { Type = "int", Value = 0 });

            // 为变量分配栈空间（根据类型大小）
            var (typeSize, _, _, _) = TypeInfoFromString(varType);
            _variableOffset -= typeSize;
            _variables[node.Name] = _variableOffset;
            if (string.IsNullOrEmpty(_firstLocalVarName)) _firstLocalVarName = node.Name;

            // 记录变量到当前作用域（用于退出时自动drop）
            RecordVarInScope(node.Name);

            // 清除旧移动状态（重新声明）
            ClearMoved(node.Name);
            ClearBorrowed(node.Name);

            // 存储变量类型
            _variableTypes[node.Name] = varType;
            _rustVarTypes[node.Name] = GetRustTypeFromString(varType);

            // 如果有初始化表达式
            if (node.Initializer != null)
            {

                // 记录结构体名称（用于字段访问）
                if (node.Initializer is StructLiteralNode structLit)
                {
                    _variableStructTypes[node.Name] = structLit.StructName;
                }
                else if (node.Initializer is IdentifierNode idInit2)
                {
                    // 继承源变量的结构体名称
                    if (_variableStructTypes.TryGetValue(idInit2.Name, out string srcStructName))
                    {
                        _variableStructTypes[node.Name] = srcStructName;
                    }
                }

                // 所有权检查：如果初始化器是一个变量（不是字面量/表达式）
                // 先检查源变量是否已被移动或借用（编译期错误）
                if (node.Initializer is IdentifierNode idInit)
                {
                    if (_movedVariables.Contains(idInit.Name))
                    {
                        throw new CodeGenerationException(
                            $"use of moved value: `{idInit.Name}` (value moved here in previous assignment)");
                    }
                    if (!IsCopyVariable(idInit.Name) && IsBorrowed(idInit.Name))
                    {
                        throw new CodeGenerationException(
                            $"cannot move out of `{idInit.Name}` because it is borrowed");
                    }
                }

                // 计算初始化表达式（读取源变量的值）
                node.Initializer.Accept(this);

                // 如果源是非Copy类型，在成功读取值后标记为移动
                if (node.Initializer is IdentifierNode idInitAfter)
                {
                    if (!IsCopyVariable(idInitAfter.Name))
                    {
                        MarkMoved(idInitAfter.Name);
                    }
                }

                // 获取正确的存储指令
                RustType rustType = GetRustTypeFromString(varType);
                OpCode storeOp = GetStoreInstruction(rustType);

                // 源类型 → 目标类型转换 (int→i64/f64, float→f64, double→float, i64→i32 等)
                RustType initRustType = GetRustTypeFromString(InferTypeFromExpression(node.Initializer));
                EmitTypeConversion(initRustType, rustType);

                // 将值存储到变量位置 — 统一 store (dest-first): 内存为 dest
                AddInstruction(storeOp, $"{_variableOffset}(R12)", "R0");
            }
            else
            {
                // 没有初始化表达式，默认为整数类型
                _variableTypes[node.Name] = "int";
                _rustVarTypes[node.Name] = RustType.Int;
            }
        }
        
        public void Visit(ConstantDeclarationNode node)
        {
            // 常量存储在常量表中，而不是栈上
            if (node.Initializer != null)
            {
                // 计算常量值
                node.Initializer.Accept(this);
                
                // 将常量值存储到常量表
                // 注意：这里假设R0包含常量值
                // 在实际实现中，需要根据常量类型处理
                constants[node.Name] = GetConstantValue(node.Initializer);
                
            }
            else
            {
                throw new CodeGenerationException($"常量 '{node.Name}' 必须有初始值");
            }
        }
        
        /// <summary>
        /// 从表达式节点获取常量值
        /// </summary>
        private object GetConstantValue(ASTNode expression)
        {
            if (expression is LiteralNode literal)
            {
                return literal.Value;
            }
            else if (expression is IdentifierNode identifier)
            {
                // 如果是引用其他常量
                if (constants.TryGetValue(identifier.Name, out object value))
                {
                    return value;
                }
                else
                {
                    throw new CodeGenerationException(VMLPlugins.Localization.Get("rust.undefined_const") + $": {identifier.Name}");
                }
            }
            else if (expression is BinaryOperationNode binaryOp)
            {
                // 简单的常量表达式求值
                object left = GetConstantValue(binaryOp.Left);
                object right = GetConstantValue(binaryOp.Right);
                
                // 这里只处理整数运算
                if (left is int leftInt && right is int rightInt)
                {
                    return binaryOp.Operator switch
                    {
                        "+" => leftInt + rightInt,
                        "-" => leftInt - rightInt,
                        "*" => leftInt * rightInt,
                        "/" => rightInt != 0 ? leftInt / rightInt : 0,
                        _ => 0
                    };
                }
            }
            
            // 默认返回0
            return 0;
        }
        
        public void Visit(AssignmentNode node)
        {
            // 下标赋值: a[i] = value（左值后缀链里 `[` 这条，见 Parser.ParseAssignment）
            if (node.Target is IndexAccessNode iaTarget)
            {
                // 值与地址全程走栈 —— 右值表达式（如 `inc(a[i])`）里可能调函数，寄存器靠不住。
                node.Value.Accept(this);                        // R0 = 右值
                AddInstruction(OpCode.PUSH, "R0");
                iaTarget.Target.Accept(this);                   // R0 = 基址
                AddInstruction(OpCode.PUSH, "R0");
                iaTarget.Index.Accept(this);                    // R0 = 下标
                AddInstruction(OpCode.POP, "R1");               // R1 = 基址
                AddInstruction(OpCode.SHL, "R0", "#2");         // idx*4
                AddInstruction(OpCode.ADD, "R0", "#4");         // 跳过 VML 数组头
                AddInstruction(OpCode.ADD, "R0", "R1");         // R0 = 元素地址
                AddInstruction(OpCode.POP, "R1");               // R1 = 右值
                AddInstruction(OpCode.MOVE, "(R0)", "R1");      // [地址] = 右值（dest 在前）
                return;
            }

            // 成员字段赋值: p.x = value
            if (node.Target != null)
            {
                node.Value.Accept(this); // value in R0
                AddInstruction(OpCode.PUSH, "R0", "");
                // Compute member address (without final LOAD)
                if (node.Target is MemberAccessNode ma)
                {
                    ma.Target.Accept(this); // base address in R0
                    // Look up struct type to determine field offset
                    if (ma.Target is IdentifierNode id && _variableStructTypes.TryGetValue(id.Name, out string structName))
                    {
                        if (!string.IsNullOrEmpty(structName) && _structFields.TryGetValue(structName, out var fields))
                        {
                            int fieldIndex = fields.FindIndex(f => f.Name == ma.Member);
                            if (fieldIndex >= 0)
                            {
                                int fieldOff = fieldIndex * 4;
                                if (fieldOff > 0)
                                    AddInstruction(OpCode.ADD, "R0", $"#{fieldOff}");
                            }
                        }
                    }
                }
                else
                {
                    node.Target.Accept(this); // fallback
                }
                AddInstruction(OpCode.POP, "R1", "");
                // ⚠ 原来是 `MOVE R1, (R0)` —— `MOVE dest, src` 的 **dest 在前**，
                //   那是把**字段地址处的内容读进 R1**（并且 R0 里的地址随之丢失）；
                //   要存就得把 `(R0)` 放 dest 位。**"操作数写反"族。**
                AddInstruction(OpCode.MOVE, "(R0)", "R1");
                return;
            }

            // 所有权检查：不能给一个已被借用的变量赋值
            if (IsBorrowed(node.VariableName))
            {
                string kind = _mutBorrowed.Contains(node.VariableName) ? "mutably " : "";
                throw new CodeGenerationException(
                    $"cannot assign to `{node.VariableName}` because it is {kind}borrowed");
            }

            // 所有权检查：如果赋值源是一个变量
            // 先检查源变量是否已被移动或借用（编译期错误）
            if (node.Value is IdentifierNode idVal)
            {
                if (_movedVariables.Contains(idVal.Name))
                {
                    throw new CodeGenerationException(
                        $"use of moved value: `{idVal.Name}` (value moved here in previous assignment)");
                }
                if (!IsCopyVariable(idVal.Name) && IsBorrowed(idVal.Name))
                {
                    throw new CodeGenerationException(
                        $"cannot move out of `{idVal.Name}` because it is borrowed");
                }
            }

            // 计算右侧表达式的值（读取源变量的值）
            node.Value.Accept(this);

            // 如果源是非Copy类型，在成功读取值后标记为移动（移动语义）
            if (node.Value is IdentifierNode idValAfter)
            {
                if (!IsCopyVariable(idValAfter.Name))
                {
                    MarkMoved(idValAfter.Name);
                }
            }

            // 推断右侧表达式的类型
            string valueType = InferTypeFromExpression(node.Value);
            RustType rustType = GetRustTypeFromString(valueType);

            // 更新变量类型（目标变量获得新所有权）
            if (_variables.ContainsKey(node.VariableName))
            {
                _variableTypes[node.VariableName] = valueType;
                _rustVarTypes[node.VariableName] = rustType;
                // 传播结构体名称
                if (node.Value is IdentifierNode idValSrc && _variableStructTypes.TryGetValue(idValSrc.Name, out string srcStructName))
                {
                    _variableStructTypes[node.VariableName] = srcStructName;
                }
                // 目标变量被重新赋值，清除其旧移动/借用状态
                ClearMoved(node.VariableName);
                ClearBorrowed(node.VariableName);
            }

            // 获取正确的存储指令
            OpCode storeOp = GetStoreInstruction(rustType);

            // 如果目标是 I64，源值是 int，需要 I2L 转换
            // 如果目标是 F64，源值是 int，需要 I2D 转换
            if ((rustType == RustType.I64 || rustType == RustType.F64)
                && (valueType == "i64" || valueType == "f64"))
            {
                string srcType = InferTypeFromExpression(node.Value);
                if (srcType == "int" || srcType == "i32")
                {
                    if (rustType == RustType.I64)
                        AddInstruction(OpCode.I2L, "R0", "R0");
                    else
                        AddInstruction(OpCode.I2D, "R0", "R0");
                }
            }

            // 查找变量位置 — 统一 store (dest-first): 内存为 dest
            if (_variables.TryGetValue(node.VariableName, out int offset))
            {
                AddInstruction(storeOp, $"{offset}(R12)", "R0");
            }
            else
            {
                // 全局变量 — 统一 store: 标签为 dest
                AddInstruction(storeOp, node.VariableName, "R0");
            }
        }
        
    }
}
