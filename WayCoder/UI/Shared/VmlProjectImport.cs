namespace WayCoder.UI.Shared;

/// <summary>
/// **其它构建系统 → `.vmk` 的导入器**。Makefile 是第一个，将来还会有（CMake / MSBuild / cargo…）。
///
/// <para>
/// ## 为什么要有这层抽象（而不是让 `make --import` 直接调 <see cref="MakefileImporter"/>）
///
/// `.vmk` 的定位是**枢纽格式**：各种老项目的构建描述**一次性转进来**，此后以 `.vmk` 为准
/// （不做"每次构建去读原格式"—— 两处真源必然漂移，而漂移的症状是"改了原文件却不生效"）。
/// 既然是枢纽，那"加一种格式"就该是**加一个类 + 注册一行**，而不是回到 CLI 里去改分支。
/// </para>
///
/// <para>
/// ⚠ **每个导入器都必须遵守同一条铁律**：认不出的东西**报出来**，不猜、不静默跳过。
/// 老构建系统里塞得下任何东西，导入器要做的不是"尽量多认"，而是**让用户知道
/// 哪些没被带过来** —— 静默的结果是"编过了、少了半个程序"，本仓最怕的那种失败。
/// </para>
/// </summary>
public interface IProjectImporter
{
    /// <summary>给人看的名字（`make` / `cmake`…）。</summary>
    string Name { get; }

    /// <summary>这个文件名归不归我管（`Makefile`、`makefile`、`*.mk`…）。</summary>
    bool CanHandle(string fileName);

    /// <summary>导入。<paramref name="entryHint"/> 可显式指定入口（null = 自己找）。</summary>
    ProjectImportResult Import(string path, string? entryHint = null);
}

/// <summary>导入结果：工程 + 要**原样告诉用户**的话（不是错误，但必须看到）。</summary>
public sealed record ProjectImportResult(VmlProject Project, List<string> Notes);

/// <summary>
/// 导入器的注册表。**加一种格式 = 写一个 <see cref="IProjectImporter"/> + 在
/// <see cref="All"/> 里加一行** —— CLI 与手机端都不用动。
/// </summary>
public static class ProjectImporters
{
    /// <summary>所有认得的导入器（顺序 = 匹配优先级）。</summary>
    public static readonly IReadOnlyList<IProjectImporter> All = new IProjectImporter[]
    {
        new MakefileImporter(),
        // 将来：new CMakeImporter(), new MSBuildImporter(), …
    };

    /// <summary>按文件名找一个能处理的导入器（都不认返回 null）。</summary>
    public static IProjectImporter? Resolve(string fileName) =>
        All.FirstOrDefault(i => i.CanHandle(fileName));

    /// <summary>支持的文件名形态（写进错误信息，让用户知道能喂什么）。</summary>
    public static string SupportedNames => "Makefile / makefile / *.mk";
}
