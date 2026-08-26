namespace WayCoder.Tools;

/// <summary>
/// 全局工作目录锚点 —— 跨命令跟踪 cwd，AsyncLocal 确保每个异步上下文
/// 跟踪自己的工作目录，并行调用不会产生竞态。
///
/// 从 <see cref="BashTool"/> 抽出，使「cwd 跟踪」与「进程执行」两个正交概念解耦：
/// 移动端（MAUI）无 bash 进程（不编译 BashTool），但文件工具（read/write/edit/glob/grep 等）
/// 仍需基于被跟踪工作目录解析相对路径，故统一改引用本类型而非 BashTool。
/// </summary>
public static class CwdContext
{
    /// <summary>当前被跟踪工作目录（cd 命令更新；null 表示未设置，回退进程启动目录）。</summary>
    public static readonly AsyncLocal<string?> Current = new();
}
