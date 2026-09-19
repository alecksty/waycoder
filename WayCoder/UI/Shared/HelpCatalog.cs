namespace WayCoder.UI.Shared;

/// <summary>
/// 「关于 → 使用说明」的**层级目录**（一级分类 → 二级主题 → 正文），
/// 正文是随包的 markdown（`WayCoder.Maui/Resources/Raw/help/**`），渲染复用编辑器那套
/// `MarkdownPreview` —— 不另写渲染器。
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
    /// <param name="Children">
    /// 子主题 —— 有子主题的节点**自己没有正文**，点开是下一级列表。
    /// 目前只有「22 种语言」用到（分类 → 主题 → 每种语言一份）。
    /// </param>
    public readonly record struct Topic(string Id, string Title, string Summary, Topic[]? Children = null);

    /// <summary>一个分类（对应「关于」页上的一个按钮）。</summary>
    /// <param name="Key">路由里传的分类标识。</param>
    /// <param name="Title">分类名。</param>
    /// <param name="Icon">分类图标（emoji —— 与底部 Tab 同一套选法，只用 U+1F300 以上那块）。</param>
    /// <param name="Topics">分类下的主题；<b>只有一个主题时点分类直接进正文</b>（少一级点击）。</param>
    public readonly record struct Category(string Key, string Title, string Icon, Topic[] Topics);

    /// <summary>
    /// 22 种语言各一份说明（「22 种语言」这个节点的子主题）。
    ///
    /// 与 `Resources/Raw/help/vml/lang/*.md` **一一对应** —— 自测里有一条会两边对账，
    /// 少一个文件 / 多一个孤儿文件都会红。
    /// </summary>
    private static readonly Topic[] LanguageTopics =
    [
            new("vml/lang/c", "C", "最完整的一条路：绘图、音效、手柄全都能用"),
            new("vml/lang/cpp", "C++", "与 C 同一套接口，另有类与模板"),
            new("vml/lang/csharp", "C#", "class + Main，需要声明外部函数"),
            new("vml/lang/objc", "Objective-C", "别 include UI 头文件，直接调用"),
            new("vml/lang/java", "Java", "static native 声明外部函数"),
            new("vml/lang/kotlin", "Kotlin", "与 Java 同一套写法"),
            new("vml/lang/swift", "Swift", "直接调用，无需声明"),
            new("vml/lang/go", "Go", "package main + func main"),
            new("vml/lang/rust", "Rust", "fn main，直接调用"),
            new("vml/lang/d", "D", "void main，直接调用"),
            new("vml/lang/dart", "Dart", "external 声明外部函数"),
            new("vml/lang/python", "Python", "写起来最快；可变网格要用 ui_g 那套"),
            new("vml/lang/javascript", "JavaScript", "function main 后要手动调用一次"),
            new("vml/lang/lua", "Lua", "轻快，适合小游戏"),
            new("vml/lang/ruby", "Ruby", "完全平铺写，函数支持有限"),
            new("vml/lang/r", "R", "向量语言，写法要注意"),
            new("vml/lang/pascal", "Pascal", "注释里只能写 ASCII"),
            new("vml/lang/fortran", "Fortran", "call 调用，科学计算友好"),
            new("vml/lang/basic", "BASIC", "NATIVE 声明，老式写法"),
            new("vml/lang/forth", "Forth", "栈式写法，字符串用 S 引号"),
            new("vml/lang/scheme", "Scheme", "顶层扁平写，函数看不见全局变量"),
            new("vml/lang/ladder", "Ladder", "PLC 风格，做不了界面程序")
    ];

    /// <summary>全部目录。**加一篇说明只改这里 + 放一个 .md 文件。**</summary>
    public static readonly Category[] Categories =
    [
        new("quickstart", "快速上手", "🚀",
        [
            new("quickstart", "五步跑起第一个程序", "装完先干什么：钥匙、模型、点「运行」"),
        ]),

        new("vml", "VML 编译器", "🧩",
        [
            new("vml/index", "VML 是什么", "一台跑在手机里的虚拟机，22 种语言都能编"),
            new("vml/build", "编译与运行", "源码 → .vml → .vmb 三级产物，各管什么"),
            new("vml/languages", "22 种语言", "每种语言怎么写、怎么跑、有哪些坑", LanguageTopics),
            new("vml/ui", "UI 开发", "开窗、绘图、收输入、出声音 —— 宿主接口全表"),
            new("vml/errors", "常见错误", "看得懂报错、找得到原因"),
        ]),

        new("editor", "编辑器", "✏️",
        [
            new("editor/basic", "基本操作", "只读与编辑、保存、字号、全屏"),
            new("editor/edit", "编辑与查找替换", "多行输入、撤销、查找、替换、辅助输入条"),
            new("editor/run", "编译运行与诊断", "边写边跑，错误画在出错那一格上"),
        ]),

        new("cli", "命令行", "💻",
        [
            new("cli/shell", "命令行页", "真 shell，`cd` 会改工作目录"),
            new("cli/vml", "vml 命令", "vml run / vml test，扩展名自动识别"),
        ]),

        new("files", "文件与会话", "📁",
        [
            new("files/browse", "文件管理", "导入、打开、改名、编译运行"),
            new("files/sessions", "会话与记忆", "继续上次的对话、多会话、槽位"),
        ]),

        new("settings", "设置与模型", "⚙️",
        [
            new("settings/model", "服务商与模型", "填 Key、选模型、大模型与小模型分工"),
            new("settings/permission", "权限与工作模式", "Ask/Auto/Yolo、建造/计划/聊天"),
        ]),
    ];

    /// <summary>按 key 找分类（找不到返回 null）。</summary>
    public static Category? Find(string? key)
        => Categories.FirstOrDefault(c => c.Key == key);

    /// <summary>
    /// 按 id 找主题（找不到返回 null）。**会进子主题** —— 三级的 id（`vml/lang/c`）
    /// 与二级的写在同一个命名空间里，只查第一层的话它们全都找不到。
    ///
    /// ⚠ 找不到的后果不是报错而是**退化成兜底值**：正文页拿它取标题，
    /// 拿不到就显示成通用的「使用说明」—— 页面照常打开、内容也对，只是标题不对，
    /// 这种"半对"最难被发现。
    /// </summary>
    public static Topic? FindTopic(string? id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var c in Categories)
            if (Lookup(c.Topics, id) is { } hit) return hit;
        return null;

        static Topic? Lookup(Topic[] topics, string id)
        {
            foreach (var t in topics)
            {
                if (t.Id == id) return t;
                if (t.Children is { Length: > 0 } kids && Lookup(kids, id) is { } sub) return sub;
            }
            return null;
        }
    }

    /// <summary>这篇说明的随包路径。</summary>
    public static string AssetPath(string id) => $"help/{id}.md";

}
