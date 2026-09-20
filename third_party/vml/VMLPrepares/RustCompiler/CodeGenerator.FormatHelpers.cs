using System;
using System.Collections.Generic;
using System.Text;
using VMLAssembler;
using CompilerBase;

namespace RustCompiler
{
    public partial class CodeGenerator
    {
        /// <summary>
        /// 处理格式化字符串，支持{}占位符
        /// </summary>
        private void ProcessFormatString(string formatString, List<ASTNode> formatArgs)
        {
            int argIndex = 0;
            StringBuilder currentPart = new StringBuilder();
            bool inPlaceholder = false;
            
            for (int i = 0; i < formatString.Length; i++)
            {
                char c = formatString[i];
                
                if (c == '{')
                {
                    // 检查是否是转义的{{}}
                    if (i + 1 < formatString.Length && formatString[i + 1] == '{')
                    {
                        currentPart.Append('{');
                        i++; // 跳过第二个{
                    }
                    else
                    {
                        // 输出当前部分
                        if (currentPart.Length > 0)
                        {
                            OutputStringPart(currentPart.ToString());
                            currentPart.Clear();
                        }
                        inPlaceholder = true;
                    }
                }
                else if (c == '}')
                {
                    if (inPlaceholder)
                    {
                        // 检查是否是转义的}}
                        if (i + 1 < formatString.Length && formatString[i + 1] == '}')
                        {
                            currentPart.Append('}');
                            i++; // 跳过第二个}
                        }
                        else
                        {
                            // 处理占位符
                            if (argIndex < formatArgs.Count)
                            {
                                OutputFormatArg(formatArgs[argIndex]);
                                argIndex++;
                            }
                            else
                            {
                                // 参数不足，输出错误占位符
                                OutputStringPart("{?}");
                            }
                            inPlaceholder = false;
                        }
                    }
                    else
                    {
                        // 不在占位符中，检查是否是转义的}}
                        if (i + 1 < formatString.Length && formatString[i + 1] == '}')
                        {
                            currentPart.Append('}');
                            i++; // 跳过第二个}
                        }
                        else
                        {
                            // 单独的}，直接输出
                            currentPart.Append(c);
                        }
                    }
                }
                else if (inPlaceholder)
                {
                    // 占位符内的内容（如格式化说明符），暂时忽略
                    // 在完整实现中，这里可以解析格式化说明符如{:?}, {:.2}, etc.
                    continue;
                }
                else
                {
                    currentPart.Append(c);
                }
            }
            
            // 输出最后的部分
            if (currentPart.Length > 0)
            {
                OutputStringPart(currentPart.ToString());
            }
            
            // 如果还有未使用的参数，忽略（Rust的println!会忽略多余的参数）
        }
        
        /// <summary>输出字符串部分（数据段字符串→基类方法）</summary>
        private void OutputStringPart(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            string label = NewLabel("str_part") ?? throw new CodeGenerationException("生成的字符串标签为null");
            dataSection[label] = text;
            AddInstruction(OpCode.MOVE, "R0", label);
            EmitPrintString();
        }

        /// <summary>浮点数→字符串 + 输出 (处理 MCU 定点格式 ×1000)</summary>
        private void OutputFloatFromReg()
        {
            AddInstruction(OpCode.MOVE, "R1", "R0");
            string fltTrue = NewLabel("float_true"), fltFalse = NewLabel("float_false"), fltEnd = NewLabel("float_end");
            AddInstruction(OpCode.CMP, "R1", "#0");
            AddInstruction(OpCode.JGE, fltFalse);
            string minusLabel = NewLabel("float_minus"); dataSection[minusLabel] = "-";
            AddInstruction(OpCode.MOVE, "R0", minusLabel); EmitPrintString();
            AddInstruction(OpCode.NEG, "R1");
            AddLabel(fltFalse);
            AddInstruction(OpCode.MOVE, "R0", "R1"); AddInstruction(OpCode.DIV, "R0", "#1000");
            AddInstruction(OpCode.MOVE, "R2", "R0");
            AddInstruction(OpCode.MUL, "R2", "#1000"); AddInstruction(OpCode.SUB, "R1", "R2");
            AddInstruction(OpCode.MOVE, "R0", "R2"); EmitPrintInt();
            string dotLabel = NewLabel("float_dot"); dataSection[dotLabel] = ".";
            AddInstruction(OpCode.MOVE, "R0", dotLabel); EmitPrintString();
            AddInstruction(OpCode.MOVE, "R0", "R1"); EmitPrintInt();
            AddLabel(fltEnd);
        }

        /// <summary>布尔值→字符串 + 输出 ("true"/"false")</summary>
        private void OutputBoolFromReg()
        {
            string trueL = NewLabel("bool_true"), falseL = NewLabel("bool_false"), endL = NewLabel("bool_end");
            AddInstruction(OpCode.CMP, "R0", "#0"); AddInstruction(OpCode.JE, falseL);
            string tsl = NewLabel("bool_true_str") ?? throw new CodeGenerationException("bool标签为null");
            dataSection[tsl] = "true"; AddInstruction(OpCode.MOVE, "R0", tsl); EmitPrintString();
            AddInstruction(OpCode.JMP, endL);
            AddLabel(falseL);
            string fsl = NewLabel("bool_false_str") ?? throw new CodeGenerationException("bool标签为null");
            dataSection[fsl] = "false"; AddInstruction(OpCode.MOVE, "R0", fsl); EmitPrintString();
            AddLabel(endL);
        }
        
        /// <summary>
        /// 输出格式化参数
        /// </summary>
        private void OutputFormatArg(ASTNode argNode)
        {
            // 根据参数类型生成不同的输出代码
            if (argNode is LiteralNode literal)
            {
                if (literal.Type == "string")
                {
                    // 字符串直接输出
                    string label = NewLabel("str_arg");
                    if (label == null)
                    {
                        throw new CodeGenerationException("生成的字符串标签为null");
                    }
                    dataSection[label] = literal.Value?.ToString() ?? "";
                    AddInstruction(OpCode.MOVE, "R0", label);
                    EmitPrintString();
                }
                else if (literal.Type == "integer" || literal.Type == "int" || literal.Type == "long")
                {
                    // ⚠ 判据必须含 `"int"` / `"long"` —— 解析器产出的整数类型名是
                    //   `Parser.Expressions.cs:284` 的 `"int"`（`"long"` 是 :285），
                    //   而这里原先只认 `"integer"`。**两个字符串对不上 ⇒ 整个分支从不进入
                    //   ⇒ 这个 if/else 链又没有兜底 else ⇒ 静默产空、实参凭空消失**。
                    //   实测：`println!("OUT-INT={}", 42)` 只打出 `OUT-INT=`（42 丢了），
                    //   而字符串/浮点/字符/布尔的类型名恰好都对得上 ⇒ **只有整数被丢**。
                    //   （与 C++ 前端 `printf` 那个"只取第 0 个实参"是同一族：
                    //     格式串旁挂的实参被静默丢掉、编译链接全绿。）
                    if (literal.Value != null && long.TryParse(literal.Value.ToString(), out long iv))
                    {
                        AddInstruction(OpCode.MOVE, "R0", $"#{(int)iv}");
                        EmitPrintInt();
                    }
                    else
                    {
                        // 宁可报错也不静默丢 —— 静默丢正是这个 bug 藏了这么久的原因
                        throw new CodeGenerationException(
                            $"println! 格式实参的整数值无法解析: {literal.Value}");
                    }
                }
                else if (literal.Type == "float")
                {
                    if (literal.Value != null && float.TryParse(literal.Value.ToString(), out float fv))
                    {
                        int sv = (int)(fv * 1000);
                        string fs = $"{sv/1000}.{Math.Abs(sv%1000):D3}";
                        string label = NewLabel("float_arg") ?? throw new CodeGenerationException("float标签为null");
                        dataSection[label] = fs;
                        AddInstruction(OpCode.MOVE, "R0", label);
                        EmitPrintString();
                    }
                }
                else if (literal.Type == "char")
                {
                    char cv = literal.Value?.ToString()?[0] ?? ' ';
                    AddInstruction(OpCode.MOVE, "R0", $"#{(int)cv}");
                    EmitPrintChar();
                }
                else if (literal.Type == "bool")
                {
                    string label = NewLabel("bool_arg") ?? throw new CodeGenerationException("bool标签为null");
                    dataSection[label] = literal.Value?.ToString()?.ToLower() == "true" ? "true" : "false";
                    AddInstruction(OpCode.MOVE, "R0", label);
                    EmitPrintString();
                }
            }
            else if (argNode is IdentifierNode identifier)
            {
                // 变量输出 - 根据变量类型生成不同的输出
                if (_variables.ContainsKey(identifier.Name))
                {
                    int offset = _variables[identifier.Name];
                    AddInstruction(OpCode.MOVE, "R0", $"{offset}(R12)");
                    
                    // 根据变量类型选择SYSCALL
                    if (_variableTypes.TryGetValue(identifier.Name, out string varType))
                    {
                        switch (varType)
                        {
                            case "string": EmitPrintString(); break;
                            case "float": OutputFloatFromReg(); break;
                            case "char": EmitPrintChar(); break;
                            case "bool": OutputBoolFromReg(); break;
                            default: EmitPrintInt(); break;
                        }
                    }
                    else
                    {
                        EmitPrintInt(); // 未知类型，默认输出整数
                    }
                }
                else
                {
                    // 变量未找到，输出错误
                    string label = NewLabel("var_error");
                    if (label == null)
                    {
                        throw new CodeGenerationException("生成的错误标签为null");
                    }
                    dataSection[label] = $"[错误: 找不到变量 '{identifier.Name}']";
                    AddInstruction(OpCode.MOVE, "R0", label);
                    EmitPrintString();
                }
            }
            else if (argNode is BinaryOperationNode binary)
            {
                // 计算二元运算表达式
                binary.Accept(this);
                
                // 根据表达式类型输出结果
                string exprType = InferTypeFromExpression(binary);
                
                if (exprType == "float") OutputFloatFromReg();
                else if (exprType == "bool") OutputBoolFromReg();
                else EmitPrintInt();
            }
            else if (argNode is IndexAccessNode indexArg)
            {
                // 下标表达式 `xs[0]` —— 此前**没有这条分支**，一路落到兜底的 `[expr]` 占位符，
                // 于是 `println!("{}", xs[0])` 打出的是**字面量 `[expr]`**（不是元素值、也不报错）。
                // ⚠ 特别阴的一点：同一个表达式放进二元运算里（`println!("{}", xs[1] + 10)`）
                //   走的是 BinaryOperationNode 那条，**是对的** —— 只测那一种形态永远照不出来。
                // `Visit(IndexAccessNode)` 收尾把元素值留在 R0，这里直接接着打即可。
                indexArg.Accept(this);
                if (InferTypeFromExpression(indexArg) == "float") OutputFloatFromReg();
                else EmitPrintInt();
            }
            else if (argNode is CallExpressionNode callExpr)
            {
                // 函数调用: 求值后根据函数名判断返回类型 (v1.66.53)
                callExpr.Accept(this);
                if (IsStringReturningFunc(callExpr.FunctionName))
                    EmitPrintString();
                else
                    EmitPrintInt();
            }
            else
            {
                // 其他类型的表达式 —— **报错，不再打占位符**。
                // 原来这里往数据段塞一个字面量 "[expr]" 就完事：程序照样编过、照样运行，
                // 屏幕上多一行 `[expr]`，**没有一点点提示**。本文件上面那条整数字面量的分支
                // 早就写着「宁可报错也不静默丢 —— 静默丢正是这个 bug 藏了这么久的原因」，
                // 这条兜底属于同一族，一并改掉。
                throw new CodeGenerationException(
                    $"println!/print! 的格式实参暂不支持这种表达式：{argNode.GetType().Name}");
            }
        }
        
    }
}
