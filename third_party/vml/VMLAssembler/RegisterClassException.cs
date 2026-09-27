using System;
using System.Collections.Generic;

namespace VMLAssembler
{
    /// <summary>
    /// **用错寄存器**（寄存器类不匹配）—— 用户 2026-09-27 定的规矩：`Rn`=32位 / `Ln`=64位 /
    /// `Fn`=32位 / `Dn`=64位，类由助记符决定，**用错必须报错、不许静默**。
    ///
    /// <para>
    /// 为什么要单独立一个类型：它与"链接器内部故障"不是一回事 —— 前者多半是**前端发射错了**
    /// （或手写汇编写错了），报错文案要能指到源码；宿主（CLI / MAUI / LSP）要把它当
    /// **编译失败**处理，而不是崩溃。
    /// </para>
    ///
    /// <para>
    /// ⚠ 与 <see cref="UnresolvedSymbolException"/> 同一条边界：**只对用户代码抛**，
    /// 库代码里的违规只打警告（库打进 APK，一次坏重生成会让每个用户程序都编不过）。
    /// </para>
    /// </summary>
    public class RegisterClassException : Exception
    {
        /// <summary>违规明细（指令下标 + 可读错因）。</summary>
        public IReadOnlyList<RegisterClassTable.Violation> Violations { get; }

        public RegisterClassException(string message, IReadOnlyList<RegisterClassTable.Violation> violations)
            : base(message)
        {
            Violations = violations;
        }
    }
}
