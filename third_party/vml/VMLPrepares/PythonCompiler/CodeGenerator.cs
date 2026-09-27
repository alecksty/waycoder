using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace PythonCompiler
{
    /// <summary>
    /// Python 数据类型枚举
    /// </summary>
    public enum PythonType
    {
        Int,        // 32 位整数
        Int64,      // **64 位整数**（用户 2026-09-27 要求「每种语言都要有 int64 运算」；
                    //   Python 的 int 本是无界的，本平台退一步给到 64 位）
        Float,      // 浮点类型（本平台是 **32 位** F32 —— 与 F 寄存器对齐）
        Bool,       // 布尔类型
        String,     // 字符串类型
        None,       // None类型
        List,       // 列表类型
        Dict,       // 字典类型
        Tuple,      // 元组类型
        Set,        // 集合类型
        Object      // 通用对象类型
    }

    /// <summary>
    /// 类信息结构
    /// </summary>
    public class ClassInfo
    {
        public string TypeLabel { get; set; }
        public string InitLabel { get; set; }
        public string? ParentName { get; set; }
        public Dictionary<string, string> Methods { get; set; } = new();
        public Dictionary<string, object> ClassVars { get; set; } = new();
    }

    public partial class CodeGenerator : TypedCodeGen<PythonType>, IASTVisitor
    {
    }
}
