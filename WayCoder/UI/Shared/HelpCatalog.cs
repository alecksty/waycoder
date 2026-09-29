namespace WayCoder.UI.Shared;

/// <summary>
/// 「关于 → 使用说明」的**入口目录**（一级分类 → 二级主题 → 正文），
/// 正文是随包的 markdown（`WayCoder.Maui/Resources/Raw/help/**`），渲染复用编辑器那套
/// `MarkdownPreview` —— 不另写渲染器。
///
/// ## 更深的层级不在这里 —— 在正文的链接里
///
/// **多少级都行**：正文里写 `[C 语言](help:vml/lang/c)` 就跳到那一篇。
/// 层级由内容决定，加页面只写 markdown、不动 C#、不动列表页。
///
/// 这里**刻意只保留"入口"**（关于页上那六个分类，以及各自的几篇）：
/// 它要的是"不用搜就能点进去"，而不是把整棵树抄一遍。
/// ⚠ 树的第二份实现（早先那个 `Topic.Children`）已经删掉 —— 两套层级机制并存时，
/// "哪些节点是目录、哪些有正文"的判断漏一处，现象就是**点下去什么也不发生**。
///
/// ## 为什么目录写在代码里、正文写在 markdown 里
///
/// · **目录写在代码里**：它是"导航"，要能**自测**（`SelfTest` 里有一条：目录里的每个 id
///   都必须在包里有对应的 .md —— 写错一个字母，真机上只会看到一片空白，最难查），
///   而且标题要在列表页显示、要能排序、要能按分类分组 —— 这些用 markdown 表达反而绕。
/// · **正文写成 markdown**：它是给人读的长文，带表格、代码块、列表；而且
///   **同一份文件在仓库里能被读懂、能被 grep、将来也能直接喂给 AI** ——
///   写成 C# 常量字符串的话，改一个错别字都要动代码。
///
/// ## 加一篇说明
///
/// 1. 在 `Resources/Raw/help/<分类>/<名字>.md` 写正文（**markdown**，中文，面向手机用户）；
/// 2. 在下面 <see cref="Categories"/> 里加一行 —— 就这两步，不用动页面代码。
///    （加完跑一次自测：漏了第 1 步会被那条"目录 ↔ 文件"的用例当场抓住。）
/// </summary>
public static class HelpCatalog
{
    /// <summary>一篇说明。</summary>
    /// <param name="Id">随包路径（不含 `help/` 前缀、不含 `.md`），也是页面路由里传的 id。</param>
    /// <param name="Title">列表里显示的名字。</param>
    /// <param name="Summary">列表里的第二行小字（一句话说清这篇讲什么）。</param>
    public readonly record struct Topic(string Id, string Title, string Summary);

    /// <summary>一个分类（对应「关于」页上的一个按钮）。</summary>
    /// <param name="Key">路由里传的分类标识。</param>
    /// <param name="Title">分类名。</param>
    /// <param name="Icon">分类图标（emoji —— 与底部 Tab 同一套选法，只用 U+1F300 以上那块）。</param>
    /// <param name="Topics">分类下的主题；<b>只有一个主题时点分类直接进正文</b>（少一级点击）。</param>
    public readonly record struct Category(string Key, string Title, string Icon, Topic[] Topics);

    /// <summary>全部目录。**加一篇说明只改这里 + 放两个 .md 文件（zh 与 en 各一份）。**</summary>
    /// <remarks>
    /// ⚠ **刻意是表达式体属性，不是 `static readonly` 字段**：标题与摘要现在走 <see cref="L.Pick"/>，
    /// 而 `static readonly` 会把界面语言**冻在类型初始化那一刻**（`Lang.cs` 公理 A3）——
    /// 那是「今天对、明天错」的形态：谁把首次访问提前，语言就冻死，且只在某些启动顺序下复现。
    /// 代价是每次访问重建数组 —— 只在列表页与查找时发生，非热路径。
    /// </remarks>
    public static Category[] Categories =>
    [
        new("quickstart", L.Pick("快速上手", "Quick start"), "🚀",
        [
            new("quickstart", L.Pick("五步跑起第一个程序", "Get your first program running in five steps"), L.Pick("装完先干什么：钥匙、模型、点「运行」", "What to do first: API key, model, then tap Run")),
        ]),

        new("vml", L.Pick("VML 编译器", "VML compiler"), "🧩",
        [
            new("vml/index", L.Pick("VML 是什么", "What is VML"), L.Pick("一台跑在手机里的虚拟机，22 种语言都能编", "A virtual machine on your phone that compiles 22 languages")),
            new("vml/build", L.Pick("编译与运行", "Compile and run"), L.Pick("源码 → .vml → .vmb 三级产物，各管什么", "Source → .vml → .vmb: what each artifact is for")),
            new("vml/languages", L.Pick("22 种语言", "22 languages"), L.Pick("每种语言怎么写、怎么跑、有哪些坑", "How to write, run, and what to watch for in each language")),
            new("vml/ui", L.Pick("UI 开发", "UI development"), L.Pick("开窗、绘图、收输入、出声音 —— 宿主接口全表", "Windows, drawing, input, sound — the full host API")),
            new("vml/errors", L.Pick("常见错误", "Common errors"), L.Pick("看得懂报错、找得到原因", "Read an error message and find its cause")),
        ]),

        new("editor", L.Pick("编辑器", "Editor"), "✏️",
        [
            new("editor/basic", L.Pick("基本操作", "Basics"), L.Pick("只读与编辑、保存、字号、全屏", "Read-only vs. edit, saving, font size, fullscreen")),
            new("editor/edit", L.Pick("编辑与查找替换", "Editing, find and replace"), L.Pick("多行输入、撤销、查找、替换、辅助输入条", "Multi-line input, undo, find, replace, the assist bar")),
            new("editor/run", L.Pick("编译运行与诊断", "Compile, run, and diagnostics"), L.Pick("边写边跑，错误画在出错那一格上", "Run as you type; errors are drawn on the offending cell")),
        ]),

        new("cli", L.Pick("命令行", "Shell"), "💻",
        [
            new("cli/shell", L.Pick("命令行页", "The Shell page"), L.Pick("真 shell，`cd` 会改工作目录", "A real shell; `cd` changes the working directory")),
            new("cli/vml", L.Pick("vml 命令", "The vml command"), L.Pick("vml run / vml make / vml test，扩展名自动识别", "vml run / vml make / vml test, with automatic extension detection")),
        ]),

        new("files", L.Pick("文件与会话", "Files and sessions"), "📁",
        [
            new("files/browse", L.Pick("文件管理", "File management"), L.Pick("导入、打开、改名、编译运行", "Import, open, rename, compile, run")),
            new("files/sessions", L.Pick("会话与记忆", "Sessions and memory"), L.Pick("继续上次的对话、多会话、槽位", "Resume the last conversation, multiple sessions, slots")),
        ]),

        new("settings", L.Pick("设置与模型", "Settings and models"), "⚙️",
        [
            new("settings/model", L.Pick("服务商与模型", "Providers and models"), L.Pick("填 Key、选模型、大模型与小模型分工", "Enter a key, pick models, split work between the main and small model")),
            new("settings/permission", L.Pick("权限与工作模式", "Permissions and work modes"), L.Pick("Ask/Auto/Yolo、建造/计划/聊天", "Ask/Auto/Yolo, Build/Plan/Chat")),
        ]),
        new("legal", L.Pick("法律", "Legal"), "📄",
        [
            new("legal/privacy", L.Pick("隐私政策", "Privacy policy"), L.Pick("数据发到哪、存在哪、怎么删", "Where your data goes, where it is stored, how to delete it")),
            new("legal/terms", L.Pick("使用条款", "Terms of use"), L.Pick("用这个 App 的几条约定", "A few ground rules for using this app")),
        ]),
    ];

    /// <summary>按 key 找分类（找不到返回 null）。</summary>
    public static Category? Find(string? key)
        => Categories.FirstOrDefault(c => c.Key == key);

    /// <summary>按 id 找主题（找不到返回 null）。**只查目录表里列出的那些** ——
    /// 链接跳转过去的目标（`help:vml/lang/c`）不一定在表里，那种 id 的标题由正文自己给
    /// （见 <see cref="HeadingOf"/>）。</summary>
    public static Topic? FindTopic(string? id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var c in Categories)
            foreach (var t in c.Topics)
                if (t.Id == id) return t;
        return null;
    }

    /// <summary>
    /// 取一篇说明的**一级标题**（`# 标题` 那一行），拿不到返回 null。
    ///
    /// 页面标题以**正文自己写的为准**：层级既然由链接决定（加页面只写 markdown），
    /// 标题就不该再要求"同时在 C# 目录表里登记一遍" —— 那正是两级不一致的老毛病。
    /// 目录表里有的（从「关于」点进来的那些）仍然优先用它，因为那里的标题更短、更适合列表。
    /// </summary>
    public static string? HeadingOf(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return null;
        foreach (var raw in markdown.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                var t = line[2..].Trim();
                if (t.Length > 0) return t;
            }
            // 正文开始之前只找最靠前的那个一级标题；遇到围栏就停（代码块里的 # 不是标题）
            if (line.StartsWith("```", StringComparison.Ordinal)) return null;
        }
        return null;
    }

    /// <summary>
    /// 这篇说明的随包路径 —— **按界面语言分目录**（`help/zh/…` / `help/en/…`）。
    ///
    /// <para>
    /// 帮助文档是**整篇长文**，不像一句话那样能中英塞进同一个字符串，所以这一层按语言分目录。
    /// ⚠ 调用方（<c>HelpPage</c>）**必须在英文版缺失时显式报警、绝不回退中文** ——
    /// 回退会让"漏翻"永远看不见，用户看到的是"这个 App 一半英文一半中文"。
    /// </para>
    /// </summary>
    public static string AssetPath(string id) => $"help/{L.ResourceLangDir}/{id}.md";

}
