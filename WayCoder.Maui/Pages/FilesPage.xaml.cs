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
        PathLabel.Text = _editMode
            ? path + L.Pick("（编辑模式：点目录/文件可删除/改名/打包）", " (Edit mode: tap an item to delete/rename/zip)")
            : path;
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
            if (await DisplayAlertAsync(
                    L.Pick("退出应用", "Quit app"),
                    L.Pick("已到工作区根目录，确定要退出吗？", "You are at the workspace root. Quit the app?"),
                    L.Pick("退出", "Quit"), L.Pick("取消", "Cancel")))
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
        EditBtn.Text = _editMode ? L.Pick("✔ 完成", "✔ Done") : L.Pick("✏️ 编辑", "✏️ Edit");
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
            try { await DisplayAlertAsync(L.Pick("操作失败", "Action failed"), ex.ToString(), L.Pick("关闭", "Close")); } catch { /* 连弹框都失败就只剩日志 */ }
        }
    }

    private async Task HandleSelectionAsync(SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not SandboxFsService.FsEntry entry) return;
        FileList.SelectedItem = null; // 清选中态，允许再次点同一项

        // 编辑模式：点任意项 → 删除/改名/打包菜单
        if (_editMode)
        {
            // 四项文案存局部变量、判定按它们分派（与下面那份菜单同一个口径，只是这边没有 Id 列表）；
            // `case` 标签必须是编译期常量 ⇒ 用 `case var a when a == …`。
            var kind = entry.IsDirectory ? L.Pick("目录", "Folder") : L.Pick("文件", "File");
            var renameItem = L.Pick("重命名", "Rename");
            var deleteItem = L.Pick("删除", "Delete");
            var zipItem = L.Pick("创建 ZIP 压缩包", "Create ZIP");
            var action = await DisplayActionSheetAsync(
                $"{kind}{L.Pick("：", ": ")}{entry.Name}",
                L.Pick("取消", "Cancel"), null, renameItem, deleteItem, zipItem);
            switch (action)
            {
                case var a when a == renameItem:
                    await RenameAsync(entry);
                    break;
                case var a when a == deleteItem:
                    await DeleteAsync(entry);
                    break;
                case var a when a == zipItem:
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
        //   头文件（.h/.hpp/.bi/.inc）→ **两样都没有**，只留「打开」——
        //     它在链上，但不是一份完整的翻译单元，编译/运行都无从谈起（用户定的）
        //   ⚠ 判据问 SandboxFsService.RoleCanCompile / RoleCanRun，**不在这里重列一遍角色** ——
        //     以前这里是 `!= None`，新增一个角色就会静默获得运行权限（`Header` 正撞上这条）。
        var canCompile = SandboxFsService.RoleCanCompile(entry.Vml);
        var canRun = SandboxFsService.RoleCanRun(entry.Vml);

        // 菜单项 = (**稳定 Id**, 显示文案)。判定按 Id，显示用 Label。
        // ⚠ 原先按中文 `case "打开"` 判定 —— 文案一翻，整张菜单变成点了没反应（六条全落空）。
        //   `DisplayActionSheetAsync` 只回传**文案**、不回传索引，所以这里按 Label 反查 Id；
        //   反查用的就是我们刚传进去的那份列表 ⇒ 文案与反查同步变，翻译不会打断它。
        var actions = new List<(string Id, string Label)>();
        if (entry.CanEdit) actions.Add(("open", L.Pick("打开", "Open")));   // 是文本就该能改（含 `.vml` 与各种可编译源码）
        if (canCompile) actions.Add(("compile", L.Pick("VML 编译", "Compile")));
        if (canRun) actions.Add(("run", L.Pick("VML 运行", "Run")));
        actions.Add(("external", L.Pick("用外部应用打开", "Open with external app")));
        actions.Add(("rename", L.Pick("重命名", "Rename")));
        actions.Add(("delete", L.Pick("删除", "Delete")));

        var picked = await DisplayActionSheetAsync(entry.Name, L.Pick("取消", "Cancel"), null, [.. actions.Select(a => a.Label)]);

        // 动作 + 返回值各落一行日志：这一条链路跨了「文件页 → Shell 导航 → 命令行页」三处，
        // 而 `async void` 里出的错以前只会留下一句"点了没反应"。一行 Info 换一个可查的现场，值。
        ErrorLog.Info("FilesPage", $"菜单选择：{entry.Name}（Vml={entry.Vml}）→ {(picked ?? "(取消)")}");

        var at = picked is null ? -1 : actions.FindIndex(a => a.Label == picked);
        switch (at < 0 ? null : actions[at].Id)
        {
            case "open":
                await OpenInEditorAsync(entry);
                break;
            case "compile":
                await CompileVmlAsync(entry);
                break;
            case "run":
                await RunVmlFileAsync(entry);
                break;
            case "external":
                await OpenWithExternalAsync(entry);
                break;
            case "rename":
                await RenameAsync(entry);
                break;
            case "delete":
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
            // 两项文案同样存局部变量（判定用的就是它们，不能在别处再写一遍中文字面量）。
            var overwriteItem = L.Pick("覆盖", "Overwrite");
            var renameItem = L.Pick("重命名", "Rename");
            var choice = await DisplayActionSheetAsync(
                L.Pick($"已存在 {Path.GetFileName(outRel)}", $"{Path.GetFileName(outRel)} already exists"),
                L.Pick("取消", "Cancel"), null, overwriteItem, renameItem);
            if (choice == renameItem)
            {
                var renamed = await DisplayPromptAsync(
                    L.Pick("重命名产物", "Rename output"),
                    L.Pick($"输入新的 {outExt} 文件名", $"Enter a new {outExt} file name"),
                    accept: L.Pick("确定", "OK"), cancel: L.Pick("取消", "Cancel"),
                    initialValue: Path.GetFileName(outRel), maxLength: 100);
                if (string.IsNullOrWhiteSpace(renamed)) return;
                outRel = VmlOutputRename(outRel, renamed.Trim(), outExt);
            }
            else if (choice != overwriteItem) return;
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

        await DisplayAlertAsync(L.Pick("无法打开命令行页", "Cannot open the Shell page"),
            L.Pick("没找到「命令行」页 —— AppShell.xaml 里的 Route=\"shell\" 可能被改过。",
                "The \"shell\" page was not found — Route=\"shell\" in AppShell.xaml may have been changed."),
            L.Pick("关闭", "Close"));
    }

    /// <summary>把文件/目录打包为 ZIP（同目录下 同名.zip）。</summary>
    private async Task CreateZipAsync(SandboxFsService.FsEntry entry)
    {
        // 打包与删除同一类：大目录上都是长耗时。Task.Run 早就对了，
        // 补的是**重入**与**异常**这两条（与 DeleteAsync 保持一致的口径）。
        if (_busy) return;
        _busy = true;
        ShowBusy(L.Pick($"正在打包「{entry.Name}」…", $"Zipping {entry.Name}…"));
        try
        {
            var rel = SandboxFsService.ToRelative(entry.FullPath) ?? entry.Name;
            var zipRel = await Task.Run(() => SandboxFsService.CreateZip(rel));
            HideBusy();
            if (zipRel == null)
            {
                await DisplayAlertAsync(L.Pick("打包失败", "Zip failed"),
                    L.Pick("同名 .zip 已存在，或目录不可写", "A .zip with that name already exists, or the folder is not writable"),
                    L.Pick("关闭", "Close"));
                return;
            }
            Refresh();
            await DisplayAlertAsync(L.Pick("已打包", "Zip created"),
                L.Pick($"已生成 {Path.GetFileName(zipRel)}", $"Created {Path.GetFileName(zipRel)}"),
                L.Pick("确定", "OK"));
        }
        catch (Exception ex)
        {
            ErrorLog.Warning("FilesPage", $"打包 {entry.Name} 失败", ex);
            HideBusy();
            await DisplayAlertAsync(L.Pick("打包失败", "Zip failed"), ex.Message, L.Pick("关闭", "Close"));
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
        var newName = await DisplayPromptAsync(L.Pick("重命名", "Rename"), L.Pick("输入新名称", "Enter a new name"),
            accept: L.Pick("确定", "OK"), cancel: L.Pick("取消", "Cancel"),
            initialValue: entry.Name, maxLength: 100);
        if (string.IsNullOrWhiteSpace(newName) || newName == entry.Name) return;
        if (SandboxFsService.Rename(rel, newName))
            Refresh();
        else
            await DisplayAlertAsync(L.Pick("重命名失败", "Rename failed"),
                L.Pick("目标名称已存在或路径非法", "The target name already exists, or the path is invalid"),
                L.Pick("关闭", "Close"));
    }

    private async Task DeleteAsync(SandboxFsService.FsEntry entry)
    {
        var rel = SandboxFsService.ToRelative(entry.FullPath) ?? entry.Name;
        var confirmed = await DisplayAlertAsync(L.Pick("删除确认", "Confirm delete"),
            L.Pick($"确定删除「{entry.Name}」？此操作不可撤销。", $"Delete {entry.Name}? This cannot be undone."),
            L.Pick("删除", "Delete"), L.Pick("取消", "Cancel"));
        if (!confirmed) return;

        // ⚠ **删除必须离开 UI 线程**。`SandboxFsService.Delete` 里是
        //    `Directory.Delete(full, recursive: true)` —— **同步递归**，
        //    而本方法是在 UI 线程上跑的：目录一大（App 自带那套 `vml/Lib` 是 5245 个文件）
        //    主线程被堵死 ⇒ ANR ⇒ 用户看到的就是「删除卡死闪退」。
        //    顺带挡重入：删除期间连点会叠出好几趟递归遍历，越叠越慢。
        if (_busy) return;
        _busy = true;
        ShowBusy(L.Pick($"正在删除「{entry.Name}」…", $"Deleting {entry.Name}…"));
        try
        {
            var ok = await Task.Run(() => SandboxFsService.Delete(rel));
            HideBusy();                       // 先撤遮罩再弹结果，免得弹框压在遮罩下面
            if (ok) Refresh();
            else await DisplayAlertAsync(L.Pick("删除失败", "Delete failed"),
                L.Pick("无法删除该项", "Could not delete this item"), L.Pick("关闭", "Close"));
        }
        catch (Exception ex)
        {
            // 删到一半失败（被占用 / 无权限）不能把整个 App 带崩，如实报出来
            ErrorLog.Warning("FilesPage", $"删除 {rel} 失败", ex);
            HideBusy();
            await DisplayAlertAsync(L.Pick("删除失败", "Delete failed"), ex.Message, L.Pick("关闭", "Close"));
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
            await DisplayAlertAsync(L.Pick("无法打开", "Cannot open"),
                L.Pick($"无法在编辑器中打开 {entry.Name}（{probe.Reason}）",
                    $"Cannot open {entry.Name} in the editor ({probe.Reason})"),
                L.Pick("关闭", "Close"));
            return;
        }

        await Shell.Current.GoToAsync($"editor?path={Uri.EscapeDataString(rel)}");
    }

    /// <summary>用系统外部应用打开沙箱内文件（HTML→浏览器等）。FsEntry.FullPath 已是沙箱根内绝对路径。</summary>
    private async Task OpenWithExternalAsync(SandboxFsService.FsEntry entry)
    {
        var ok = await FileOpenService.OpenWithExternalAsync(entry.FullPath, entry.Name);
        if (!ok)
            await DisplayAlertAsync(L.Pick("无法打开", "Cannot open"),
                L.Pick($"没有可打开 {entry.Name} 的应用，或文件不在可共享位置",
                    $"No app can open {entry.Name}, or the file is not in a shareable location"),
                L.Pick("关闭", "Close"));
    }

    /// <summary>在当前目录新建空文件。</summary>
    private async void OnNewFileClicked(object? sender, EventArgs e)
        => await CreateNewAsync(isDirectory: false);

    /// <summary>在当前目录新建子文件夹。</summary>
    private async void OnNewDirClicked(object? sender, EventArgs e)
        => await CreateNewAsync(isDirectory: true);

    private async Task CreateNewAsync(bool isDirectory)
    {
        var prompt = isDirectory ? L.Pick("新建文件夹", "New folder") : L.Pick("新建文件", "New file");
        var name = await DisplayPromptAsync(prompt, L.Pick("输入名称", "Enter a name"),
            accept: L.Pick("确定", "OK"), cancel: L.Pick("取消", "Cancel"), maxLength: 100);
        if (string.IsNullOrWhiteSpace(name)) return;

        var rel = string.IsNullOrEmpty(_currentDir)
            ? name.Trim()
            : $"{_currentDir.TrimEnd('/')}/{name.Trim()}";

        var ok = isDirectory ? SandboxFsService.CreateDir(rel) : SandboxFsService.CreateFile(rel);
        if (ok) Refresh();
        else await DisplayAlertAsync(L.Pick("新建失败", "Create failed"),
            L.Pick("名称已存在或路径非法", "The name already exists, or the path is invalid"), L.Pick("关闭", "Close"));
    }

    private async void OnImportClicked(object? sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = L.Pick("选择要导入沙箱的文件", "Choose a file to import into the workspace"),
            });
            if (result == null) return;

            var rel = await SandboxFsService.ImportAsync(result);
            Refresh();
            await DisplayAlertAsync(L.Pick("已导入", "Imported"), rel, L.Pick("确定", "OK"));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(L.Pick("导入失败", "Import failed"), ex.Message, L.Pick("关闭", "Close"));
        }
    }
}
