using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace GoCompiler
{
    /// <summary>
    /// Go语言数据类型枚举
    /// </summary>
    public enum GoTypeEnum
    {
        Int,        // int, int32
        UInt,       // uint, uint32
        Int64,      // int64
        UInt64,     // uint64
        Float,      // float32
        Double,     // float64
        Bool,       // bool
        Byte,       // byte (uint8)
        Rune,       // rune (int32)
        String,     // string
        Array,      // 数组
        Slice,      // 切片
        Map,        // 映射
        Chan,       // 通道
        Struct,     // 结构体
        Interface,  // 接口
        Pointer,    // 指针
        Object      // 对象类型
    }

    public partial class CodeGenerator : CLikeCodegen<CodeGenerator>
    {
    }
}
