using System;
using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;

namespace LuaCompiler
{
    /// <summary>
    /// Lua 数据类型枚举
    /// </summary>
    public enum LuaType
    {
        Nil,        // nil类型
        Number,     // 数字类型（整数或浮点数）
        String,     // 字符串类型
        Boolean,    // 布尔类型
        Table,      // 表类型
        Function,   // 函数类型
        Userdata,   // 用户数据类型
        Thread,     // 线程类型
        Object      // 通用对象类型
    }

    public partial class CodeGenerator : TypedCodeGen<LuaType>
    {
    }
}
