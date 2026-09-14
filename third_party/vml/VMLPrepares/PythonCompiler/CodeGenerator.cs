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
        Int,        // 整数类型
        Float,      // 浮点类型
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
