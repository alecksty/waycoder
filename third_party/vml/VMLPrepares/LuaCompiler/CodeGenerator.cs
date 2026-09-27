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
        /// <summary>
        /// **浮点**数字 —— Lua 的 `number` 在运行期就是 IEEE 双精度，但本前端把
        /// "整数值/浮点值"分成两档来生成代码：整数走 32 位通用寄存器与 4 字节槽
        /// （**与 Lib / 其它语言的调用约定一致**），浮点走 `D0` 与 8 字节槽。
        ///
        /// <para>
        /// ⚠ 只把 `Number` 整个改成 `F64` 会把**整门语言的实参**都变成 8 字节，
        /// 与 `Lib` 的 4 字节形参、以及 `[R12+12+4i]` 的形参区**当场对不上**
        /// （实测 `abi.lua` 的 `ipow(2,3)` 从 8 变成 2 —— 第二个实参读不到了）。
        /// 所以分两档：**整数保持原样**，只有浮点才是双精度。
        /// </para>
        /// </summary>
        Float,
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
