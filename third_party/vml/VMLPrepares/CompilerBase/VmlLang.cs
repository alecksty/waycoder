namespace CompilerBase
{
    /// <summary>
    /// 编译器诊断文案的**界面语言** —— 让「未声明的变量 'x'」这类**编译错误/警告**也能按语言出。
    ///
    /// <para>
    /// <b>为什么在本工程内建，而不直接引用 <c>WayCoder.L</c></b>：
    /// 本工程（<c>VMLPrepares</c>）**不引用** <c>WayCoder</c>（`grep WayCoder *.csproj` 零命中），
    /// 反过来 <c>WayCoder</c> 引用本工程 —— 依赖是**单向**的。所以这里不能 `using WayCoder`。
    /// </para>
    ///
    /// <para>
    /// <b>但又绝不能自己再判一次语言</b>（本仓头号坑是「同一规则两处实现」）：语言的真源只有
    /// <c>WayCoder/UI/Shared/Lang.cs</c> 的 <c>L</c> 一个。所以这里只留一个**注入点** ——
    /// 宿主（App/TUI/自测）启动时把 <see cref="IsZh"/> 设成 <c>L.IsZh</c>，
    /// 本工程内部一律读它。**没注入时默认中文**，与改造前的行为逐字节相同。
    /// </para>
    ///
    /// <para>
    /// ⚠ 与 <c>L.Pick</c> 同一条硬规则：**引用 <see cref="Pick"/> 的成员要写成表达式体属性**
    /// （<c>public static string X =&gt; VmlLang.Pick("中文", "English")</c>），
    /// **不要写 <c>static readonly</c> 字段** —— 后者会在首次访问时把语言**冻死**，
    /// 于是英文设备上永久显示中文，且只在某些启动顺序下复现。
    /// </para>
    /// </summary>
    public static class VmlLang
    {
        // ⚠ 默认 zh（不是"跟随系统"）：没人注入的场合（库调用、单测、第三方嵌入）行为与
        //   改造前**逐字节相同**。跟随系统由各入口**显式**注入 —— 与 `L` 的设计同一个理由：
        //   默认值安全 + 入口显式，两头都不靠约定。
        private static volatile bool _isZh = true;

        /// <summary>当前是否中文。**本工程内唯一的判语言处**，别在别处自己判。</summary>
        public static bool IsZh => _isZh;

        /// <summary>注入语言（各入口启动时调用一次；自测可随时钉住）。</summary>
        public static void Set(bool isZh) => _isZh = isZh;

        /// <summary>
        /// <b>取诊断文案的唯一入口</b>。两个实参就是"键" —— 没有键表、没有查找，
        /// 所以「键没跟上」这个失败模式**在语法上不存在**（少一个实参编不过）。
        /// </summary>
        public static string Pick(string zh, string en) => _isZh ? zh : en;
    }
}
