using System;
using System.Collections.Generic;

namespace VMLAssembler
{
    /// <summary>
    /// 链接之后，**用户代码里**还有调用指向不存在的函数。
    ///
    /// 为什么单独立一个类型而不是 `InvalidOperationException`：
    /// - 调用方要能把它与"链接器自己出问题了"区分开 —— 前者是**用户源码的错**，
    ///   报错文案要指到源码（行列号、函数名），后者是内部故障；
    /// - 它是"编译期就该拦下"的那一类，宿主侧（CLI / MAUI / LSP）要把它当成
    ///   **编译失败**而不是崩溃（`VmlDiagnostics.Parse` 按 GCC 格式解析成编辑器里的气泡）。
    ///
    /// ⚠ 只对**用户代码**抛。库代码里的未解析标签（历史遗留的死包装器）仍走警告 ——
    /// 库是打进 APK 的，一次坏重生成会让**每一个**用户程序都编不过，那是可用性事故。
    /// 两者的分界见 <see cref="LibraryLinker.LinkLibraries"/> 入口的 `userEnd`。
    /// </summary>
    public class UnresolvedSymbolException : Exception
    {
        /// <summary>未解析的标识符 → 被引用次数。</summary>
        public IReadOnlyDictionary<string, int> Symbols { get; }

        public UnresolvedSymbolException(string message, IReadOnlyDictionary<string, int> symbols)
            : base(message)
        {
            Symbols = symbols;
        }
    }
}
