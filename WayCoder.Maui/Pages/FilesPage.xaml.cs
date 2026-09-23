using WayCoder.Maui.Services;
using WayCoder.UI.Shared;

namespace WayCoder.Maui.Pages;

public partial class FilesPage : ContentPage
{
    /// <summary>当前相对沙箱根的目录（"" = 根）。</summary>
    private string _currentDir = "";

    /// <summary>
    /// 正在做耗时文件操作（删除 / 打包）——期间挡住重入。
    /// 用户实测报过「删大目录卡死闪退」，删除期间连点会叠出好几次递归遍历。
    /// </summary>
    private bool _busy;

    /// <summary>
    /// 显示/隐藏耗时操作的进度遮罩。遮罩**铺满整页**（见 XAML 注释），
    /// 所以「没完成之前不让点其他地方」是它天然带来的，不必再去逐个禁用按钮。
    /// `BusySpin` 一直在转 —— 这本身就是"还活着"的证据：大目录递归删是分钟级的，
    /// 屏幕上什么都不动和卡死看起来一模一样（用户报的「卡死」有一半是这个）。
    /// </summary>
    private void ShowBusy(string text)
    {
        BusyLabel.Text = text;
        BusyOverlay.IsVisible = true;
    }

    private void HideBusy() => BusyOverlay.IsVisible = false;

    public FilesPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

    private void Refresh()
    {
        FileList.ItemsSource = SandboxFsService.ListDir(_currentDir);
        UpdatePathLabel();
    }

    /// <summary>
    /// 路径行 = 当前目录（+ 编辑模式提示）。
    ///
    /// 显示形态走 <see cref="SandboxPath.Display"/>，**不在这里拼 `/`** ——
    /// 此前这一行文本在「刷新」与「开关编辑模式」两处各拼了一遍，尾部多余的分隔符
    /// 只在其中一处被清掉，于是同一个目录在两个时刻显示成两种样子。
    /// </summary>
    private void UpdatePathLabel()
    {
        var path = SandboxPath.Display(_currentDir);
        PathLabel.Text = _editMode ? path + "（编辑模式：点目录/文件可删除/改名/打包）" : path;
    }

    /// <summary>
    /// 退到上一级目录。**返回键与「↩ 上级」按钮共用这一份**。
    ///
    /// 已经在根（没有上一级）时返回 false，**由调用方决定「到根了」该怎么办**：
    /// 按钮什么都不做，返回键则问一句要不要退出应用 —— 路径计算只有一份，
    /// 两个入口各自保留自己的收尾语义。
    /// </summary>
    private bool TryGoUp()
    {
        if (SandboxPath.ParentOf(_currentDir) is not { } parent) return false;   // 已在工作区根
        _currentDir = parent;
        Refresh();
        return true;
    }

    private void OnUpClicked(object? sender, EventArgs e) => TryGoUp();

    /// <summary>
    /// 系统返回键（硬件键 / 手势）—— 文件页的两级语义：
    ///
    ///   ① 不在工作区根 ⇒ 退一层目录（与「↩ 上级」按钮**同一条路径计算**）；
    ///   ② 已经在工作区根 ⇒ 问一句是否退出应用，确认才退。
    ///
    /// 两条都返回 true（这一次返回由我们接管）：交给系统的话，本页是个 Tab 根页、
    /// 没有上一层可退，系统会直接**退出应用** —— 而用户此时多半只是想退一层目录，
    /// 一次误触就把 App 关掉。
    ///
    /// ⚠ 只对文件页 Tab 生效：Shell 只把返回键交给**当前可见页**
    /// （`Shell.OnBackButtonPressed` → `GetVisiblePage().SendBackButtonPressed()`），
    /// 别的 Tab 的返回行为一个字没动。
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        // 耗时操作（删除 / 打包）进行中：那块遮罩是**模态**的，返回键一并挡住 ——
        // 否则「退一层」会让列表在删除途中换成别的目录，「退出应用」更狠，
        // 直接把跑了一半的递归删打断（进程没了）。
        if (_busy) return true;

        if (TryGoUp()) return true;

        _ = ConfirmExitAsync();
        return true;
    }

    /// <summary>
    /// 已经在工作区根时再按返回 ⇒ 问一句是否退出。
    ///
    /// **确认才退**（`Application.Current.Quit()`；Android 上落成
    /// `FinishAndRemoveTask()` + `Exit(0)`，见 MAUI 的 `ApplicationHandler.MapTerminate`），
    /// 「取消」原地不动。
    ///
    /// 不做「再按一次返回就退出」那套双击确认：那是隐藏手势、屏幕上没有任何提示，
    /// 用户只会觉得「第一次按了没反应」。
    /// </summary>
    private async Task ConfirmExitAsync()
    {
        try
        {
            if (await DisplayAlertAsync("退出应用", "已到工作区根目录，确定要退出吗？", "退出", "取消"))
                Application.Current?.Quit();
        }
        catch (Exception ex)
        {
            // 这个任务是用 `_ =` 起的（`OnBackButtonPressed` 是同步的），
            // 抛出去没人接、只会变成一条「未观察的任务异常」。
            ErrorLog.Warning("FilesPage", "退出确认弹框失败", ex);
        }
    }

    /// <summary>编辑模式：开启后点任意项弹删除/改名/打包菜单，正常点击不进入目录/打开文件。</summary>
    private bool _editMode;

    private void OnEditModeClicked(object? sender, EventArgs e)
    {
        _editMode = !_editMode;
        EditBtn.Text = _editMode ? "✔ 完成" : "✏️ 编辑";
        UpdatePathLabel();
    }

    /// <summary>
    /// 点列表项的统一入口 —— **只做一件事：把异常兜住**。
    ///
    /// 为什么必须兜：这是个 `async void`（事件处理器的签名定死了），里面任何一处抛出都是
    /// **进程级未处理异常**；而 MAUI 在几条路径上还会先把它吞掉，于是现场只剩应用自己
    /// `FirstChance` 日志里一行 `ArgumentOutOfRangeException` —— **连是哪一步炸的都看不出来**，
    /// 用户看到的就是"点了没反应"（实测踩过：文件页点「VML 运行」毫无动静）。
    /// 现在异常连同**堆栈**落进错误日志（`sdcard/waycoder/config/logs/`，adb 可直接读），
    /// 并且给用户一句人话，不再静默。
    /// </summary>
    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            await HandleSelectionAsync(e);
        }
        catch (Exception ex)
        {
            ErrorLog.Error("FilesPage", "文件项点击处理失败", ex);
            try { await DisplayAlertAsync("操作失败", ex.ToString(), "关闭"); } catch { /* 连弹框都失败就只剩日志 */ }
        }
    }

    private async Task HandleSelectionAsync(SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not SandboxFsService.FsEntry entry) return;
        FileList.SelectedItem = null; // 清选中态，允许再次点同一项

        // 编辑模式：点任意项 → 删除/改名/打包菜单
        if (_editMode)
        {
            var kind = entry.IsDirectory ? "目录" : "文件";
            var action = await DisplayActionSheetAsync($"{kind}：{entry.Name}", "取消", null, "重命名", "删除", "创建 ZIP 压缩包");
            switch (action)
            {
                case "重命名":
                    await RenameAsync(entry);
                    break;
                case "删除":
                    await DeleteAsync(entry);
                    break;
                case "创建 ZIP 压缩包":
                    await CreateZipAsync(entry);
                    break;
            }
            return;
        }

        if (entry.IsDirectory)
        {
            _currentDir = SandboxFsService.ToRelative(entry.FullPath) ?? _currentDir;
            Refresh();
            return;
        }

        // 文件：按类型给操作——源码/文本进编辑器；图片/音频/视频/未知仅系统软件打开。
        // VML 相关文件再按**角色**补菜单项（判据全在 FsEntry.Vml，这里不重判扩展名）：
        //   高级语言源文件（.c/.py/.rs…）→ 「VML 编译」（产出 .vml）+「VML 运行」
        //   .vml（汇编）                 → 「VML 编译」（产出 .vmb，跑得更快）+「VML 运行」
        //   .vmb（字节码）               → 只有「VML 运行」（它已经是终态了）
        var canCompile = entry.Vml is SandboxFsService.VmlRole.Compilable or SandboxFsService.VmlRole.Assembly;
        var canRun = entry.Vml != SandboxFsService.VmlRole.None;

        var actions = new List<string>();
        if (entry.CanEdit) actions.Add("打开");   // 是文本就该能改（含 `.vml` 与各种可编译源码）
        if (canCompile) actions.Add("VML 编译");
        if (canRun) actions.Add("VML 运行");
        actions.Add("用外部应用打开");
        actions.Add("重命名");
        actions.Add("删除");

        var action2 = await DisplayActionSheetAsync(entry.Name, "取消", null, [.. actions]);

        // 动作 + 返回值各落一行日志：这一条链路跨了「文件页 → Shell 导航 → 命令行页」三处，
        // 而 `async void` 里出的错以前只会留下一句"点了没反应"。一行 Info 换一个可查的现场，值。
        ErrorLog.Info("FilesPage", $"菜单选择：{entry.Name}（Vml={entry.Vml}）→ {(action2 ?? "(取消)")}");

        switch (action2)
        {
            case "打开":
                await OpenInEditorAsync(entry);
                break;
            case "VML 编译":
                await CompileVmlAsync(entry);
                break;
            case "VML 运行":
                await RunVmlFileAsync(entry);
                break;
            case "用外部应用打开":
                await OpenWithExternalAsync(entry);
                break;
            case "重命名":
                await RenameAsync(entry);
                break;
            case "删除":
                await DeleteAsync(entry);
                break;
        }
    }

    /// <summary>
    /// 「VML 编译」：产出**下一级产物**，落在同目录的同名文件里。
    ///
    /// 产物按源文件**在 VML 链上的位置**决定（两级，与 <c>VmlRole</c> 一一对应）：
    ///   · 高级语言源文件（<c>.c</c>/<c>.py</c>…）→ <c>.vml</c>（自包含的汇编文本；与上游 CLI
    ///     `vmltool main.c` 的默认产物同名同形，见 <c>MauiVml.CompileToVml</c>）
    ///   · <c>.vml</c>（已经是一份汇编）→ <c>.vmb</c>（字节码，装载即跑，比每次现场汇编快）
    ///   · <c>.vmb</c> 不上菜单 —— 它已经是终态
    ///
    /// 同名产物**已存在时必须先问**，而且是「覆盖 / 重命名 / 取消」三选而不是「确定要覆盖吗」两选：
    /// 产物正好是用户可以手改的那种文件，静默覆盖是不可逆的；而只给"是否覆盖"的话，
    /// 想保旧产物的用户只能先退出去改名再回来（`CreateZip` 那种"已存在就失败"在这里更不合适
    /// —— 白等一场编译，最后什么也没拿到）。
    /// </summary>
    private async Task CompileVmlAsync(SandboxFsService.FsEntry entry)
    {
        var srcRel = SandboxFsService.ToRelative(entry.FullPath) ?? entry.Name;
        // 产物名走**唯一那份命名规则**（`MauiVml.NextArtifact`），命令行页执行时用的是同一个。
        var outRel = MauiVml.NextArtifact(srcRel);
        var outExt = Path.GetExtension(outRel);

        // 同名产物已存在 → 三选一（点了外面/取消都是 null，一并当取消）。
        // **询问留在文件页**：得在能看见文件列表的地方问，命令行页那边只管执行。
        if (SandboxFsService.ResolveInSandbox(outRel) is { } existing && File.Exists(existing))
        {
            var choice = await DisplayActionSheetAsync(
                $"已存在 {Path.GetFileName(outRel)}", "取消", null, "覆盖", "重命名");
            if (choice == "重命名")
            {
                var renamed = await DisplayPromptAsync("重命名产物", $"输入新的 {outExt} 文件名",
                    accept: "确定", cancel: "取消",
                    initialValue: Path.GetFileName(outRel), maxLength: 100);
                if (string.IsNullOrWhiteSpace(renamed)) return;
                outRel = VmlOutputRename(outRel, renamed.Trim(), outExt);
            }
            else if (choice != "覆盖") return;
        }

        await HandOffToShellAsync(new ShellPage.PendingVmlJob(Compile: true, entry.FullPath, outRel));
    }

    /// <summary>
    /// 把产物换到同目录下的新名字。只取**文件名部分**（防路径逃逸，与 <c>SandboxFsService.Rename</c>
    /// 同一口径）；没写对扩展名就补上 —— 别让用户得到一个没有后缀、点它连运行项都不出现的文件。
    /// </summary>
    private static string VmlOutputRename(string relPath, string newName, string ext)
    {
        var dir = Path.GetDirectoryName(relPath)?.Replace('\\', '/') ?? "";
        var safe = Path.GetFileName(newName);
        if (!safe.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) safe += ext;
        return dir.Length == 0 ? safe : dir + "/" + safe;
    }

    /// <summary>
    /// 「VML 运行」：切到命令行页去跑，**不在文件页里内嵌一个输出框**。
    ///
    /// 理由不是省事：VML 程序会等 stdin、会开绘图窗口、会吐成百上千行输出、会在崩的时候
    /// dump 一整屏寄存器 —— 这四件事命令行页都已经做好了（一问一答的输入行、可中断的运行、
    /// 回滚缓冲、cwd 顶栏），在文件页再搭一套就是第二份实现，且立刻会比那边少东西。
    /// </summary>
    private Task RunVmlFileAsync(SandboxFsService.FsEntry entry)
        => HandOffToShellAsync(new ShellPage.PendingVmlJob(Compile: false, entry.FullPath, ""));

    /// <summary>
    /// 把一件 VML 活交给命令行页（并切过去）。
    ///
    /// 交接走**绝对路径**（文件页可能停在子目录里，而命令行页的 cwd 是工作区根），
    /// 并且**绕开命令文本**（`vml run &lt;路径&gt;` 是空白切分的，路径里带空格就会断成两截）。
    /// </summary>
    private async Task HandOffToShellAsync(ShellPage.PendingVmlJob job)
    {
        // 交接本体在 ShellPage.HandOff（**唯一实现** —— 编辑器那边也调它）。
        // 这里只负责失败时给用户一句人话。各写一份的话，信箱清空的时机、失败回退、
        // 错误日志任何一处改动都会漂移，而这条链正是「点了没反应」的高发区。
        if (ShellPage.HandOff(job)) return;

        await DisplayAlertAsync("无法打开命令行页",
            "没找到「命令行」页 —— AppShell.xaml 里的 Route=\"shell\" 可能被改过。", "关闭");
    }

    /// <summary>把文件/目录打包为 ZIP（同目录下 同名.zip）。</summary>
    private async Task CreateZipAsync(SandboxFsService.FsEntry entry)
    {
        // 打包与删除同一类：大目录上都是长耗时。Task.Run 早就对了，
        // 补的是**重入**与**异常**这两条（与 DeleteAsync 保持一致的口径）。
        if (_busy) return;
        _busy = true;
        ShowBusy($"正在打包「{entry.Name}」…");
        try
        {
            var rel = SandboxFsService.ToRelative(entry.FullPath) ?? entry.Name;
            var zipRel = await Task.Run(() => SandboxFsService.CreateZip(rel));
            HideBusy();
            if (zipRel == null)
            {
                await DisplayAlertAsync("打包失败", "同名 .zip 已存在，或目录不可写", "关闭");
                return;
            }
            Refresh();
            await DisplayAlertAsync("已打包", $"已生成 {Path.GetFileName(zipRel)}", "确定");
        }
        catch (Exception ex)
        {
            ErrorLog.Warning("FilesPage", $"打包 {entry.Name} 失败", ex);
            HideBusy();
            await DisplayAlertAsync("打包失败", ex.Message, "关闭");
        }
        finally
        {
            HideBusy();
            _busy = false;
        }
    }

    private async Task RenameAsync(SandboxFsService.FsEntry entry)
    {
        var rel = SandboxFsService.ToRelative(entry.FullPath) ?? entry.Name;
        var newName = await DisplayPromptAsync("重命名", "输入新名称", accept: "确定", cancel: "取消",
            initialValue: entry.Name, maxLength: 100);
        if (string.IsNullOrWhiteSpace(newName) || newName == entry.Name) return;
        if (SandboxFsService.Rename(rel, newName))
            Refresh();
        else
            await DisplayAlertAsync("重命名失败", "目标名称已存在或路径非法", "关闭");
    }

    private async Task DeleteAsync(SandboxFsService.FsEntry entry)
    {
        var rel = SandboxFsService.ToRelative(entry.FullPath) ?? entry.Name;
        var confirmed = await DisplayAlertAsync("删除确认", $"确定删除「{entry.Name}」？此操作不可撤销。", "删除", "取消");
        if (!confirmed) return;

        // ⚠ **删除必须离开 UI 线程**。`SandboxFsService.Delete` 里是
        //    `Directory.Delete(full, recursive: true)` —— **同步递归**，
        //    而本方法是在 UI 线程上跑的：目录一大（App 自带那套 `vml/Lib` 是 5245 个文件）
        //    主线程被堵死 ⇒ ANR ⇒ 用户看到的就是「删除卡死闪退」。
        //    顺带挡重入：删除期间连点会叠出好几趟递归遍历，越叠越慢。
        if (_busy) return;
        _busy = true;
        ShowBusy($"正在删除「{entry.Name}」…");
        try
        {
            var ok = await Task.Run(() => SandboxFsService.Delete(rel));
            HideBusy();                       // 先撤遮罩再弹结果，免得弹框压在遮罩下面
            if (ok) Refresh();
            else await DisplayAlertAsync("删除失败", "无法删除该项", "关闭");
        }
        catch (Exception ex)
        {
            // 删到一半失败（被占用 / 无权限）不能把整个 App 带崩，如实报出来
            ErrorLog.Warning("FilesPage", $"删除 {rel} 失败", ex);
            HideBusy();
            await DisplayAlertAsync("删除失败", ex.Message, "关闭");
        }
        finally
        {
            HideBusy();
            _busy = false;
        }
    }

    /// <summary>点文件 → 跳内置编辑器（携带沙箱相对路径）。</summary>
    private async Task OpenInEditorAsync(SandboxFsService.FsEntry entry)
    {
        var rel = SandboxFsService.ToRelative(entry.FullPath) ?? entry.Name;

        // 只探头部 8KB 判「是不是文本」，**不要**先全量读一遍再交给编辑器读第二遍。
        // 旧实现在这里调 ReadText（= File.ReadAllText），而它只对「不存在/路径越界」返回 null ——
        // 对已存在的二进制文件从不返回 null，所以那段「二进制检测」其实是死代码，
        // 代价是打开任何文件都要先全量读一次（大文件就是双倍 IO + 双倍峰值内存）。
        var probe = SandboxFsService.ProbeText(rel);
        if (!probe.IsText)
        {
            await DisplayAlertAsync("无法打开", $"无法在编辑器中打开 {entry.Name}（{probe.Reason}）", "关闭");
            return;
        }

        await Shell.Current.GoToAsync($"editor?path={Uri.EscapeDataString(rel)}");
    }

    /// <summary>用系统外部应用打开沙箱内文件（HTML→浏览器等）。FsEntry.FullPath 已是沙箱根内绝对路径。</summary>
    private async Task OpenWithExternalAsync(SandboxFsService.FsEntry entry)
    {
        var ok = await FileOpenService.OpenWithExternalAsync(entry.FullPath, entry.Name);
        if (!ok)
            await DisplayAlertAsync("无法打开", $"没有可打开 {entry.Name} 的应用，或文件不在可共享位置", "关闭");
    }

    /// <summary>在当前目录新建空文件。</summary>
    private async void OnNewFileClicked(object? sender, EventArgs e)
        => await CreateNewAsync(isDirectory: false);

    /// <summary>在当前目录新建子文件夹。</summary>
    private async void OnNewDirClicked(object? sender, EventArgs e)
        => await CreateNewAsync(isDirectory: true);

    private async Task CreateNewAsync(bool isDirectory)
    {
        var prompt = isDirectory ? "新建文件夹" : "新建文件";
        var name = await DisplayPromptAsync(prompt, "输入名称", accept: "确定", cancel: "取消", maxLength: 100);
        if (string.IsNullOrWhiteSpace(name)) return;

        var rel = string.IsNullOrEmpty(_currentDir)
            ? name.Trim()
            : $"{_currentDir.TrimEnd('/')}/{name.Trim()}";

        var ok = isDirectory ? SandboxFsService.CreateDir(rel) : SandboxFsService.CreateFile(rel);
        if (ok) Refresh();
        else await DisplayAlertAsync("新建失败", "名称已存在或路径非法", "关闭");
    }

    private async void OnImportClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "选择要导入沙箱的文件",
            });
            if (result == null) return;

            var rel = await SandboxFsService.ImportAsync(result);
            Refresh();
            await DisplayAlertAsync("已导入", rel, "确定");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("导入失败", ex.Message, "关闭");
        }
    }
}
