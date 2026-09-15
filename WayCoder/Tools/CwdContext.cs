namespace WayCoder.Tools;

/// <summary>
/// 全局工作目录锚点 —— 跨命令跟踪 cwd，每个**作用域**跟踪自己的工作目录，
/// 并行调用不会产生竞态。
///
/// 从 <see cref="BashTool"/> 抽出，使「cwd 跟踪」与「进程执行」两个正交概念解耦：
/// 移动端（MAUI）无 bash 进程（不编译 BashTool），但文件工具（read/write/edit/glob/grep 等）
/// 仍需基于被跟踪工作目录解析相对路径，故统一改引用本类型而非 BashTool。
///
/// ## 为什么是「持盒子的 AsyncLocal」而不是「AsyncLocal&lt;string&gt;」
///
/// 早期实现直接用 <c>AsyncLocal&lt;string?&gt;</c> 存 cwd，**`cd` 因此从来没生效过**：
/// 往 <c>AsyncLocal.Value</c> 赋值 = 给**当前上下文换一个新值**，而该写入只对当前
/// 上下文的子树可见；异步方法返回时，调用方用它自己在 await 处捕获的上下文继续执行，
/// 于是写入被丢弃。工具是**在 async 被调方里**跑的（`Agent` → `RunToolAndRecordAsync`
/// → `ExecuteToolAsync` → `tool.ExecuteAsync`，多工具还各起一个并行分支），所以
/// `cd` 回一句「✔ 工作目录: …」、紧接着 `pwd`/`ls`/`git` 仍按旧目录解析 —— 现象就是
/// **`cd` 是个只说好话的 no-op**（手机端实测：`cd vmltest` 成功后 `git init` 仍报
/// 「禁止在 workspace 根目录执行 git 操作」，因为 cwd 压根没变）。
///
/// 现在 AsyncLocal 里放的是**一个盒子对象**，`Current` 的 setter 改的是**盒子的内容**
/// （堆上那个对象），而不是换 AsyncLocal 的值 —— 同一个盒子被父子上下文共享，
/// 子上下文的修改自然被子树之外的调用方看到。于是 `cd` 真的生效。
///
/// ## 作用域（盒子）的边界
///
/// 「改盒子内容」会向上传播，所以**一个盒子 = 一个逻辑智能体的 cwd**。需要隔离的地方
/// 必须在任务入口显式 <see cref="PushScope"/> 开一个新盒子，否则会互相串：
///   - 桌面 TUI 的 F1–F10 槽位（`Program.Repl` 的槽位后台任务）
///   - Web 版每个浏览器页面（`WebChat` 的槽位任务）
///   - 子智能体（`AgentTool`）—— 不隔离就是「子智能体 cd 污染父智能体」
///   - 自测用例（避免互相污染，也避免污染进程级默认目录）
///
/// 盒子是**惰性创建**的：没人 PushScope 时，第一个读它的上下文建一个，
/// 其子树共享 —— 单智能体的简单场景无需任何额外调用。
/// </summary>
public static class CwdContext
{
    /// <summary>盒子：包一层可变引用，使「改内容」能沿 async 链回传（见类型注释）。</summary>
    private sealed class Scope
    {
        public string? Value;
    }

    private static readonly AsyncLocal<Scope?> Box = new();

    /// <summary>
    /// 进程级默认工作目录 —— **惰性盒子的播种值**，也是最后的兜底。
    ///
    /// 为什么必须有一个「进程级」的默认，而不能只靠 PushScope + <c>Directory.GetCurrentDirectory()</c>：
    /// AsyncLocal 的作用域是**按 async 流**传播的，而「设置作用域的时机」与「消费作用域的 async 流」
    /// 未必是同一条 —— 进程启动时在 A 流里 PushScope，之后由平台调起的回调（MAUI 的 UI 事件、
    /// Activity 重建后新建的处理器）可能在 B 流里执行，那里 <c>Box.Value</c> 是 null。
    /// 此时惰性新建的盒子若按进程 cwd 播种就会取到**错误的目录**。
    /// 手机端实测：`MauiBootstrap` 把进程 cwd 设成了 `Global.Home`（= sdcard/waycoder/**config**），
    /// 而工作区是 `…/workspace`；切一次系统深浅色导致 Activity 重建后，Agent 的 box 丢失、
    /// 回退到进程 cwd ⇒ 它在 **config 目录**里 mkdir/cd/clone，随后每一次 write/edit 都被
    /// 「沙箱：路径在项目根外」拒绝，整轮卡死（同一轮里 cd 与 clone 却是"成功"的，极具迷惑性）。
    /// 因此默认值必须由启动流程显式给出（手机 = workspace），且不受 async 流影响。
    /// </summary>
    private static string? _defaultCwd;

    /// <summary>设置进程级默认工作目录（启动流程调用一次；手机传 workspace 绝对路径）。</summary>
    public static void SetDefault(string? dir) => _defaultCwd = dir;

    private static Scope Cur => Box.Value ??= new Scope { Value = _defaultCwd };

    /// <summary>
    /// 当前被跟踪工作目录（cd 命令更新；null 表示未设置，回退进程默认目录）。
    /// setter 是**就地修改本作用域的盒子**，因此能被创建该作用域的调用方读到。
    /// </summary>
    public static string? Current
    {
        get => Cur.Value;
        set => Cur.Value = value;
    }

    /// <summary>
    /// 开一个**新作用域**（新盒子），初始 cwd 为 <paramref name="initialCwd"/>。
    /// 槽位任务 / 子智能体 / 自测用例的入口调用；在其中发生的 <c>cd</c> 只影响本作用域。
    /// </summary>
    public static void PushScope(string? initialCwd) => Box.Value = new Scope { Value = initialCwd };

    /// <summary>
    /// 当前生效工作目录根：被跟踪 cwd → 进程默认目录 → 进程启动目录，逐级兜底。
    /// （末级 `Directory.GetCurrentDirectory()` 只该在既没设默认、也没设作用域时出现，
    ///   桌面端就是这种情况；手机端启动时一定会 SetDefault。）
    /// </summary>
    public static string Root => Cur.Value ?? _defaultCwd ?? Directory.GetCurrentDirectory();

    /// <summary>
    /// 按被跟踪工作目录解析路径（cd 后相对路径基于被跟踪工作目录，而非进程启动目录）。
    /// 消除各工具「Path.GetFullPath(x, CwdContext.Root)」的逐字重复。
    /// </summary>
    public static string Resolve(string path)
        => Path.GetFullPath(path, Root);
}
