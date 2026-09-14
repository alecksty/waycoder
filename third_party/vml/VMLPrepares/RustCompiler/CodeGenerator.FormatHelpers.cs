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
                else if (literal.Type == "integer")
                {
                    if (literal.Value != null && int.TryParse(literal.Value.ToString(), out int iv))
                    {
                        AddInstruction(OpCode.MOVE, "R0", $"#{iv}");
                        EmitPrintInt();
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
                    dataSection[label] = $"[Error: variable '{identifier.Name}' not found]";
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
                // 其他类型的表达式，暂时输出占位符
                string label = NewLabel("expr_arg");
                if (label == null)
                {
                    throw new CodeGenerationException("生成的表达式标签为null");
                }
                dataSection[label] = "[expr]";
                AddInstruction(OpCode.MOVE, "R0", label);
                EmitPrintString();
            }
        }
        
    }
}
