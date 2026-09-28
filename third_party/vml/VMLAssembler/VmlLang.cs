namespace VMLAssembler
{
    /// <summary>
    /// 编译器诊断文案的**界面语言** —— 让「未声明的变量 'x'」这类**编译错误/警告**也能按语言出。
    ///
    /// <para>
    /// <b>为什么在本工程内建，而不直接引用 <c>WayCoder.L</c></b>：
    /// 本工程（<c>VMLAssembler</c>）**不引用** <c>WayCoder</c>（`grep WayCoder *.csproj` 零命中），
    /// 反过来 <c>WayCoder</c> 引用本工程 —— 依赖是**单向**的。所以这里不能 `using WayCoder`。
    ///
    /// <para>
    /// ⚠ <b>放在 <c>VMLAssembler</c> 而不是 <c>VMLPrepares/CompilerBase</c></b>：诊断文案不止
    /// 前端有 —— **链接器**（<c>VMLAssembler/LibraryLinker.cs</c>）也会往用户脸上打
    /// <c>未定义的函数 'x'（引用 N 次）</c>，而依赖方向是 <c>VMLPrepares → VMLAssembler</c>，
    /// 放上面那一层链接器**根本引不到**（实测：全量示例在英文模式下只剩这一串中文）。
    /// 本类在依赖链的最底层，两边都够得着。
    /// </para>
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

        /// <summary>
        /// 「被引用的东西」的类别 —— 诊断模板里那个 <c>{kind}</c> 槽。
        ///
        /// <para>
        /// <b>为什么是枚举而不是 <c>string kind</c></b>：模板是
        /// <c>未声明的{kind}</c> / <c>undefined {kind}</c>，中间那个词**必须跟着语言变**
        /// （中文「变量」/ 英文 <c>variable</c>）。原先调用方直接传中文字面量，
        /// 于是英文模式下打出 <c>undefined 变量 'x'</c> —— **半中半英**，而这正是本仓
        /// 反复出现的「同一件事两处实现」：模板翻译了、喂进模板的词没翻译。
        /// 用枚举后，调用方**在语法上无法**再塞一个自己的中文名词进来（编译不过），
        /// 措辞收在 <see cref="Noun"/> 一处。
        /// </para>
        ///
        /// <para>
        /// ⚠ 新增一个类别时**只加枚举成员 + <see cref="Noun"/> 里一行**，
        /// 别在调用点再写字符串。英文一律用**单数、小写、不带冠词**的形态
        /// （模板里已经有冠词位："undefined {kind}"、"the {kind} …"），
        /// 中文用名词原形（"未声明的变量"、"未声明的局部变量"）。
        /// </para>
        /// </summary>
        public enum DiagKind
        {
            /// <summary>变量（全局或未细分的）</summary>
            Variable,
            /// <summary>局部变量</summary>
            LocalVariable,
            /// <summary>函数 / 子程序</summary>
            Function,
            /// <summary>数组</summary>
            Array,
        }

        /// <summary>把类别翻成当前语言的措辞（<see cref="DiagKind"/> 的唯一渲染点）。</summary>
        public static string Noun(DiagKind kind) => _isZh
            ? kind switch
            {
                DiagKind.Variable      => "变量",
                DiagKind.LocalVariable => "局部变量",
                DiagKind.Function      => "函数",
                DiagKind.Array         => "数组",
                _                      => "标识符",
            }
            : kind switch
            {
                DiagKind.Variable      => "variable",
                DiagKind.LocalVariable => "local variable",
                DiagKind.Function      => "function",
                DiagKind.Array         => "array",
                _                      => "identifier",
            };
    }
}
