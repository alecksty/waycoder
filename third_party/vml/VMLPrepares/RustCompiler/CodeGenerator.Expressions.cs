using System;
using System.Collections.Generic;
using System.Text;
using VMLAssembler;
using CompilerBase;

namespace RustCompiler
{
    public partial class CodeGenerator
    {
        public void Visit(BinaryOperationNode node)
        {
            var left = WrapExpr(node.Left);
            var right = WrapExpr(node.Right);
            if (_expr!.EmitStandardBinaryOps(node.Operator, left, right)) return;
            if (_expr!.EmitBitwiseOps(node.Operator, left, right)) return;
            if (node.Operator == "as")
            {
                // Rust as 类型转换
                if (node.Right is LiteralNode typeLit)
                {
                    string targetTypeStr = typeLit.Value?.ToString() ?? "int";
                    RustType toType = GetRustTypeFromString(targetTypeStr);
                    GenerateExpressionWithType(node.Left, toType);
                }
                else node.Left.Accept(this);
                return;
            }
            throw new CodeGenerationException(VMLPlugins.Localization.Get("rust.unsupported_binary") + $": {node.Operator}");
        }
        
        public void Visit(UnaryOperationNode node)
        {
            switch (node.Operator)
            {
                case "-":
                    node.Operand.Accept(this);
                    _expr!.EmitNeg(ExpVar.Reg(0, InferExpType(node.Operand)));
                    break;
                case "!":
                    node.Operand.Accept(this);
                    _expr!.EmitNot(ExpVar.Reg(0, InferExpType(node.Operand)));
                    break;
                case "~":
                    _expr!.EmitBitNot(WrapExpr(node.Operand));
                    break;
                case "&":
                case "&mut":
                    // 引用运算符：获取变量的内存地址
                    if (node.Operand is IdentifierNode idNode)
                    {
                        if (_variables.TryGetValue(idNode.Name, out int varOffset))
                        {
                            // 局部变量：LEA R0, offset(R12) 计算栈地址
                            AddInstruction(OpCode.MOVE, "R0", $"{varOffset}(R12)");
                            bool isMut = node.Operator == "&mut";
                            MarkBorrowed(idNode.Name, isMut);
                        }
                        else
                        {
                            // 全局变量/函数：LOAD R0, label 即加载地址
                            AddInstruction(OpCode.MOVE, "R0", idNode.Name);
                        }
                    }
                    else
                    {
                        // 复杂表达式（如 &arr[i]）：求值后 R0 已是地址
                        node.Operand.Accept(this);
                    }
                    break;
                case "*":
                    // 解引用：从 R0 指向的地址加载值，根据类型选择 LOADB/LOADH/FLOAD/DLOAD
                    node.Operand.Accept(this);
                    if (node.Operand is IdentifierNode ptrId
                        && _variableTypes.TryGetValue(ptrId.Name, out var ptrType)
                        && ptrType.StartsWith("*"))
                    {
                        var pointedType = ptrType.Substring(1); // "*i32" → "i32"
                        var (size, isFloat, isDouble, isLong) = TypeInfoFromString(pointedType);
                        AddInstruction(ExpressionManager.SelectLoadOp(size, isFloat, isDouble, isLong), "R0", "(R0)");
                    }
                    else
                    {
                        AddInstruction(OpCode.MOVE, "R0", "(R0)");
                    }
                    break;
                default:
                    throw new CodeGenerationException(VMLPlugins.Localization.Get("rust.unsupported_unary") + $": {node.Operator}");
            }
        }
        
        public void Visit(LiteralNode node)
        {
            switch (node.Type)
            {
                case "int":
                    AddInstruction(OpCode.MOVE, "R0", $"#{node.Value}");
                    break;
                case "long":
                    // i64 字面量: MOVEL 从数据段加载完整 64 位到 L0 (MOVE 只写 32 位)
                    EmitLoadConstant(node.Value is long lv ? lv : Convert.ToInt64(node.Value));
                    break;
                case "float":
                    if (node.Value != null && float.TryParse(node.Value.ToString(), out float floatValue))
                    {
                        // 存储 IEEE 754 位模式到数据段，通过 MOVEF 加载（避免 I2F 截断小数部分）
                        string flabel = NewLabel("flt");
                        if (flabel == null) { AddInstruction(OpCode.MOVE, "R0", "#0"); break; }
                        dataSection[flabel] = BitConverter.SingleToInt32Bits(floatValue);
                        AddInstruction(OpCode.MOVEF, "R0", flabel);
                    }
                    else
                    {
                        AddInstruction(OpCode.MOVE, "R0", "#0");
                        AddInstruction(OpCode.I2F, "R0", "R0");
                    }
                    break;
                case "string":
                    // 为字符串字面量生成唯一标签
                    string stringLabel = NewLabel("str_lit");
                    if (stringLabel == null)
                    {
                        throw new CodeGenerationException("生成的字符串标签为null");
                    }
                    // 确保标签唯一性：添加基于内容的哈希
                    string content = node.Value?.ToString() ?? "";
                    stringLabel = $"{stringLabel}_{content.GetHashCode():X}";
                    dataSection[stringLabel] = content;
                    AddInstruction(OpCode.MOVE, "R0", stringLabel);
                    break;
                case "char":
                    char charValue = (char)node.Value;
                    AddInstruction(OpCode.MOVE, "R0", $"#{(int)charValue}");
                    break;
                case "bool":
                    bool boolValue = (bool)node.Value;
                    AddInstruction(OpCode.MOVE, "R0", boolValue ? "#1" : "#0");
                    break;
                default:
                    AddInstruction(OpCode.MOVE, "R0", "#0");
                    break;
            }
        }
        
        public void Visit(IdentifierNode node)
        {
            // 检查node.Name是否为null
            if (string.IsNullOrEmpty(node.Name))
            {
                AddInstruction(OpCode.MOVE, "R0", "#0");
                return;
            }

            // 所有权检查：使用已被移动的变量
            if (_movedVariables.Contains(node.Name))
            {
                throw new CodeGenerationException(
                    $"use of moved value: `{node.Name}` (value moved to another binding)");
            }

            // 获取变量类型
            RustType varType = RustType.Int; // 默认整数类型
            if (_rustVarTypes.TryGetValue(node.Name, out RustType type))
            {
                varType = type;
            }
            
            // 获取正确的加载指令
            OpCode loadOp = GetLoadInstruction(varType);
            
            // Enum variant construction (unit variant without parens, e.g., Option::None)
            if (_enumVariants.TryGetValue(node.Name, out var variantInfo))
            {
                int tag = variantInfo.index;
                if (variantInfo.payloadType != null)
                {
                    // Data-carrying variant without args — should not happen normally
                    AddInstruction(OpCode.MOVE, "R0", "#8");
                    AddInstruction(OpCode.SYSCALL, "#40");
                    AddInstruction(OpCode.MOVE, "R1", "R0");
                    AddInstruction(OpCode.MOVE, "R0", $"#{tag}");
                    AddInstruction(OpCode.MOVE, "R0", "(R1)");
                    AddInstruction(OpCode.MOVE, "R0", "#0");
                    AddInstruction(OpCode.MOVE, "R0", "4(R1)");
                    AddInstruction(OpCode.MOVE, "R0", "R1");
                }
                else
                {
                    // Unit variant: allocate memory for tag so match can dereference
                    AddInstruction(OpCode.MOVE, "R0", "#4");
                    AddInstruction(OpCode.SYSCALL, "#40");
                    AddInstruction(OpCode.MOVE, "R1", "R0");
                    AddInstruction(OpCode.MOVE, "R0", $"#{tag}");
                    AddInstruction(OpCode.MOVE, "R0", "(R1)");
                    AddInstruction(OpCode.MOVE, "R0", "R1");
                }
                return;
            }

            // 检查编译时常量（包含 enum 变体）
            if (constants.TryGetValue(node.Name, out object constVal))
            {
                if (constVal is int intVal)
                    AddInstruction(OpCode.MOVE, "R0", $"#{intVal}");
                else
                    AddInstruction(OpCode.MOVE, "R0", "#0");
                return;
            }

            // 查找变量位置
            if (_variables.TryGetValue(node.Name, out int offset))
            {
                AddInstruction(loadOp, "R0", $"{offset}(R12)");
            }
            else
            {
                // 局部表里没有 ⇒ 这个名字**从未声明过**。
                //
                // ⚠ 原注释写的是「全局变量或函数名」，但 Rust 前端**既没有 static/全局项的
                //   代码生成、也没有函数名表**（`grep 'StaticNode|FuncDef'` 在 CodeGenerator*.cs
                //   零命中）⇒ 这个分支实际只可能是"没声明"。此前它直接发 `MOVE R0, <裸名>`，
                //   引用一个不存在的标签（值取决于汇编器/内存残值，连"确定的 0"都不是）。
                ReportUndefined(node.Name, ErrorCode.CodeGen_UndefinedVariable, "变量");
                EmitUndefinedFallback();
            }
        }
        
    }
}
