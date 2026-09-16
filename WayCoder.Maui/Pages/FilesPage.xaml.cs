using WayCoder.Maui.Services;

namespace WayCoder.Maui.Pages;

public partial class FilesPage : ContentPage
{
    /// <summary>当前相对沙箱根的目录（"" = 根）。</summary>
    private string _currentDir = "";

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
        PathLabel.Text = "/" + _currentDir.TrimStart('/');
    }

    private void OnUpClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentDir)) return; // 已到根
        var idx = _currentDir.TrimEnd('/').LastIndexOf('/');
        _currentDir = idx <= 0 ? "" : _currentDir[..idx];
        Refresh();
    }

    /// <summary>编辑模式：开启后点任意项弹删除/改名/打包菜单，正常点击不进入目录/打开文件。</summary>
    private bool _editMode;

    private async void OnEditModeClicked(object? sender, EventArgs e)
    {
        _editMode = !_editMode;
        EditBtn.Text = _editMode ? "✔ 完成" : "✏️ 编辑";
        PathLabel.Text = _editMode
            ? "/" + _currentDir.TrimStart('/') + "（编辑模式：点目录/文件可删除/改名/打包）"
            : "/" + _currentDir.TrimStart('/');
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
        ShellPage.PendingVml = job;

        // 切 Tab 走**直接指定当前项**，不走 `GoToAsync("//shell")`（真机上那条路会抛
        // `ArgumentOutOfRangeException`，见 ShellPage.SwitchToShellTab 的注释）。
        if (ShellPage.SwitchToShellTab()) return;

        // 没切过去就把信箱清掉 —— 留着它会让用户下次**碰巧**进命令行页时
        // 莫名其妙地跑起一个程序（是这条交接唯一的坑）。
        ShellPage.PendingVml = null;
        ErrorLog.Error("FilesPage", "找不到「命令行」页（AppShell.xaml 的 Route 变了？）", null);
        await DisplayAlertAsync("无法打开命令行页",
            "没找到「命令行」页 —— AppShell.xaml 里的 Route=\"shell\" 可能被改过。", "关闭");
    }

    /// <summary>把文件/目录打包为 ZIP（同目录下 同名.zip）。</summary>
    private async Task CreateZipAsync(SandboxFsService.FsEntry entry)
    {
        var rel = SandboxFsService.ToRelative(entry.FullPath) ?? entry.Name;
        var zipRel = await Task.Run(() => SandboxFsService.CreateZip(rel));
        if (zipRel == null)
        {
            await DisplayAlertAsync("打包失败", "同名 .zip 已存在，或目录不可写", "关闭");
            return;
        }
        Refresh();
        await DisplayAlertAsync("已打包", $"已生成 {Path.GetFileName(zipRel)}", "确定");
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
        if (SandboxFsService.Delete(rel))
            Refresh();
        else
            await DisplayAlertAsync("删除失败", "无法删除该项", "关闭");
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
