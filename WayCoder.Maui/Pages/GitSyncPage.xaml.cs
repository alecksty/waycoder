using System.Text.RegularExpressions;
using WayCoder.Git;
using WayCoder.Maui.Services;
using ZXing;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 跨设备代码同步页：git 作为同步介质——克隆/拉取远程仓库到沙箱 workspace/<项目名>/，
/// 每个项目独立 .git、多项目互不影响；在手机编辑器改完「提交并推送」，家里电脑 pull 继续。
/// 复用纯 C# GitCore/GitRemote（AOT 安全），远端操作放后台线程避免卡 UI。
/// </summary>
public partial class GitSyncPage : ContentPage
{
    public GitSyncPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var cred = await GitCredentialStore.LoadAsync();
            if (cred is { } c)
            {
                UserEntry.Text = c.User;
                TokenEntry.Text = c.Secret;
            }
        }
        catch { }
        LoadRecentRepos();
        ShowStatus();
    }

    /// <summary>历史仓库文件：Global.Home/git_repos.json（拉取成功的仓库地址列表）。</summary>
    private static string RecentReposFile => Path.Combine(WayCoder.Global.Home, "git_repos.json");

    /// <summary>加载拉取成功过的仓库地址到下拉列表。</summary>
    private void LoadRecentRepos()
    {
        try
        {
            var list = new List<string>();
            if (File.Exists(RecentReposFile))
            {
                var root = WayCoder.Infra.Json.Parse(File.ReadAllText(RecentReposFile));
                if (root is { Kind: WayCoder.Infra.JKind.Array } arr)
                    foreach (var item in arr.Items)
                        if (item?.AsString() is { Length: > 0 } s) list.Add(s);
            }
            if (list.Count == 0)
            {
                RecentRepoPicker.Title = "📚 历史仓库（拉取成功自动记录）";
                RecentRepoPicker.ItemsSource = null;
            }
            else
            {
                RecentRepoPicker.Title = "📚 历史仓库（选一个填入）";
                RecentRepoPicker.ItemsSource = list;
            }
        }
        catch { }
    }

    /// <summary>记录一个拉取成功的仓库地址（去重，最多保留 50 个，超出的删最旧）。</summary>
    private void SaveRecentRepo(string url)
    {
        try
        {
            var list = new List<string>();
            if (File.Exists(RecentReposFile))
            {
                var root = WayCoder.Infra.Json.Parse(File.ReadAllText(RecentReposFile));
                if (root is { Kind: WayCoder.Infra.JKind.Array } arr)
                    foreach (var item in arr.Items)
                        if (item?.AsString() is { Length: > 0 } s && s != url) list.Add(s);
            }
            list.Insert(0, url);
            const int max = 50;
            if (list.Count > max) list.RemoveRange(max, list.Count - max);   // 删最旧（尾部）
            var json = WayCoder.Infra.JNode.Array();
            foreach (var s in list) json.Add(WayCoder.Infra.JNode.Str(s));
            File.WriteAllText(RecentReposFile, json.ToJson());
            RecentRepoPicker.ItemsSource = list;
        }
        catch { }
    }

    /// <summary>历史仓库下拉选中 → 填入 URL 输入框并刷新分支/状态。</summary>
    private void OnRecentRepoPicked(object? sender, EventArgs e)
    {
        if (RecentRepoPicker.SelectedItem is string s && s.Length > 0)
        {
            RepoUrlEntry.Text = s;
            RefreshBranches(ResolveProjectRoot(s));
        }
    }

    /// <summary>
    /// 项目根目录：workspace/&lt;仓库名&gt;/（每个项目独立 .git）。由仓库 URL 推项目名；
    /// URL 为空返回 workspace 根（兼容查看）。
    /// </summary>
    private static string? ResolveProjectRoot(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return MauiBootstrap.WorkspaceDir;
        var name = Path.GetFileName(url.TrimEnd('/'));
        if (name.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) name = name[..^4];
        name = new string(name.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.').ToArray());
        if (string.IsNullOrEmpty(name)) return MauiBootstrap.WorkspaceDir;
        return Path.Combine(MauiBootstrap.WorkspaceDir, name);
    }

    /// <summary>刷新状态区：工作区 + 已有项目 + 当前项目分支/改动。</summary>
    private void ShowStatus()
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("工作区：").Append(MauiBootstrap.WorkspaceDir).Append('\n');

            // 列出已有项目（workspace 下含 .git 的子目录）
            var projects = new List<string>();
            try
            {
                foreach (var d in Directory.EnumerateDirectories(MauiBootstrap.WorkspaceDir))
                    if (Directory.Exists(Path.Combine(d, ".git")))
                        projects.Add(Path.GetFileName(d));
            }
            catch { }
            if (projects.Count > 0)
                sb.Append("项目：").Append(string.Join("、", projects)).Append('\n');

            var root = ResolveProjectRoot(RepoUrlEntry.Text);
            var gitDir = root != null ? Path.Combine(root, ".git") : null;
            if (gitDir == null || !Directory.Exists(gitDir))
            {
                sb.Append("\n填仓库地址点「📥 克隆 / 拉取」→ 每个仓库独立存 workspace/项目名/，可同时管理多个。");
                StatusLabel.Text = sb.ToString();
                return;
            }
            var branch = GitCore.Run(root!, "branch").Trim();
            var status = GitCore.Status(root!);
            sb.Append("当前：").Append(root).Append('\n');
            sb.Append("分支：").Append(branch).Append("\n\n").Append(status);
            StatusLabel.Text = sb.ToString();
            RefreshBranches(root);
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"状态读取失败：{ex.Message}";
        }
    }

    /// <summary>刷新分支下拉：本地分支 + 远程分支（origin/xxx），选中当前分支。</summary>
    private void RefreshBranches(string? projectRoot)
    {
        try
        {
            var branches = new List<string>();
            if (projectRoot != null && Directory.Exists(Path.Combine(projectRoot, ".git")))
            {
                try
                {
                    var list = GitCore.Run(projectRoot, "branch");
                    foreach (var line in list.Split('\n'))
                    {
                        var name = line.Trim().TrimStart('*').Trim();
                        if (name.Length > 0 && !name.Contains(' ')) branches.Add(name);
                    }
                }
                catch { }
            }
            // 远程分支（ls-refs），标注 origin/ 前缀
            var url = RepoUrlEntry.Text?.Trim();
            if (!string.IsNullOrEmpty(url))
            {
                var user = UserEntry.Text?.Trim();
                var token = TokenEntry.Text?.Trim();
                GitCredential? cred = user != null && token != null
                    ? new GitCredential(user, token, IsToken: true) : null;
                foreach (var rb in GitRemote.ListRemoteBranches(url, cred))
                    if (!branches.Contains(rb)) branches.Add("origin/" + rb);
            }
            var current = "";
            try
            {
                var head = File.ReadAllText(Path.Combine(projectRoot ?? "", ".git", "HEAD"));
                if (head.Contains("refs/heads/")) current = head.Split("refs/heads/")[1].Trim();
            }
            catch { }
            var distinct = branches.Distinct().OrderBy(b => b, StringComparer.Ordinal).ToList();
            if (distinct.Count == 0) distinct.Add("master");
            BranchPicker.ItemsSource = distinct;
            BranchPicker.SelectedItem = distinct.FirstOrDefault(b => b == current)
                ?? distinct.FirstOrDefault(b => b == "master") ?? distinct[0];
        }
        catch { }
    }

    /// <summary>保存凭证（SecureStorage + 写项目 .git/config）。</summary>
    private async Task EnsureCredentialsAsync(string projectRoot)
    {
        var user = UserEntry.Text?.Trim();
        var token = TokenEntry.Text?.Trim();
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(token)) return;
        await GitCredentialStore.SaveAsync(user, token, isToken: true);
        GitCredentialStore.WriteToRepo(projectRoot, user, token, isToken: true);
    }

    /// <summary>克隆（项目目录不存在）或拉取（已克隆），每个项目独立子目录。</summary>
    private async void OnPullClicked(object? sender, EventArgs e)
    {
        var url = RepoUrlEntry.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            await DisplayAlertAsync("缺少仓库地址", "请填写仓库 URL（如 https://gitee.com/user/repo.git）", "确定");
            return;
        }
        var branch = (BranchPicker.SelectedItem?.ToString() ?? "master").Replace("origin/", "");
        var projectRoot = ResolveProjectRoot(url)!;

        PullBtn.IsEnabled = false;
        PullBtn.Text = "同步中…";
        StatusLabel.Text = "⏳ 准备同步…";
        try
        {
            await EnsureCredentialsAsync(projectRoot);
            Directory.CreateDirectory(projectRoot);
            var gitDir = Path.Combine(projectRoot, ".git");
            // 进度回调：后台线程 → 主线程刷新状态（大仓库下载/解包/检出可见，避免误以为卡死）
            void Report(string msg) => MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = "⏳ " + msg);
            var result = await Task.Run(() =>
            {
                if (!Directory.Exists(gitDir))
                    return GitRemote.Clone(projectRoot, [url, branch], Report);   // 克隆所选分支
                // 已克隆：切到所选分支再拉取（pull 带 origin 名 + 分支）
                if (!string.IsNullOrEmpty(branch) && branch != "master")
                    GitCore.Run(projectRoot, $"checkout {branch}");
                return GitRemote.Pull(projectRoot, ["origin", branch], Report);
            });
            StatusLabel.Text = result;
            SaveRecentRepo(url);       // 拉取成功记录，供下次下拉
            RefreshBranches(projectRoot);
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"同步失败：{ex.Message}";
        }
        finally
        {
            PullBtn.IsEnabled = true;
            PullBtn.Text = "📥 克隆 / 拉取所选分支";
        }
    }

    /// <summary>提交并推送：add 全部改动 + commit + push（作用于当前 URL 对应的项目目录）。</summary>
    private async void OnPushClicked(object? sender, EventArgs e)
    {
        var url = RepoUrlEntry.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            await DisplayAlertAsync("缺少仓库地址", "请填写仓库 URL", "确定");
            return;
        }
        var projectRoot = ResolveProjectRoot(url)!;
        var gitDir = Path.Combine(projectRoot, ".git");
        if (!Directory.Exists(gitDir))
        {
            await DisplayAlertAsync("未克隆该项目", "先点「📥 克隆 / 拉取」同步，再编辑推送。", "确定");
            return;
        }

        var msg = string.IsNullOrWhiteSpace(CommitMsgEntry.Text)
            ? $"手机同步 {DateTime.Now:MM-dd HH:mm}" : CommitMsgEntry.Text.Trim();

        PushBtn.IsEnabled = false;
        PushBtn.Text = "推送中…";
        StatusLabel.Text = "⏳ 提交推送中…";
        try
        {
            await EnsureCredentialsAsync(projectRoot);
            var result = await Task.Run(() =>
            {
                var add = GitCore.Add(projectRoot, ".");
                var commit = GitCore.Commit(projectRoot, msg);
                var push = GitRemote.Push(projectRoot, []);
                return $"{add}\n{commit}\n{push}";
            });
            StatusLabel.Text = result;
            CommitMsgEntry.Text = "";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"推送失败：{ex.Message}";
        }
        finally
        {
            PushBtn.IsEnabled = true;
            PushBtn.Text = "📤 提交并推送";
        }
    }

    /// <summary>扫桌面 /sync-qr 二维码（含仓库 URL + 凭证 JSON）→ 自动填参数。</summary>
    private async void OnScanClicked(object? sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo == null) return;
            await using var stream = await photo.OpenReadAsync();

#if ANDROID
            // 照片流拷进内存（可重复 seek），多尺度尝试解码（QR 在照片中大小未知）
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            string? text = null;
            var reader = new BarcodeReaderGeneric
            {
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true,
                    PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE },
                },
            };

            // 先读原始尺寸
            var bounds = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
            ms.Position = 0;
            Android.Graphics.BitmapFactory.DecodeStream(ms, null, bounds);
            int maxDim = Math.Max(bounds.OutWidth, bounds.OutHeight);
            if (maxDim <= 0) maxDim = 3000;

            // 多尺度：原图、1/2、1/4、1/8（QR 小则用原图，大则缩小后更稳）
            var samples = new[] { 1, 2, 4, 8 }.Where(s => maxDim / s >= 200).ToList();
            foreach (var s in samples)
            {
                ms.Position = 0;
                var opts = new Android.Graphics.BitmapFactory.Options { InSampleSize = s };
                using var bmp = Android.Graphics.BitmapFactory.DecodeStream(ms, null, opts);
                if (bmp == null) continue;

                // 相机照片可能带 EXIF 旋转（Android 拍照 90°/270° 常见，微信自动处理但我们没读），
                // 4 个旋转角都试一次。注意：只释放旋转出的新位图，bmp 由外层 using 释放。
                foreach (var angle in new[] { 0, 90, 180, 270 })
                {
                    Android.Graphics.Bitmap? rotated = null;
                    var toDecode = bmp;
                    if (angle != 0)
                    {
                        rotated = RotateBitmap(bmp, angle);
                        toDecode = rotated;
                    }
                    try
                    {
                        var w = toDecode.Width; var h = toDecode.Height;
                        var pixels = new int[w * h];
                        toDecode.GetPixels(pixels, 0, w, 0, 0, w, h);
                        // ARGB int → RGB byte（ZXing RGBLuminanceSource 期望 3 字节/像素）
                        var rgb = new byte[w * h * 3];
                        for (int i = 0; i < pixels.Length; i++)
                        {
                            var p = pixels[i];
                            rgb[i * 3] = (byte)((p >> 16) & 0xFF);
                            rgb[i * 3 + 1] = (byte)((p >> 8) & 0xFF);
                            rgb[i * 3 + 2] = (byte)(p & 0xFF);
                        }
                        text = reader.Decode(new RGBLuminanceSource(rgb, w, h))?.Text;
                    }
                    finally
                    {
                        rotated?.Dispose(); // 仅释放旋转产生的新位图
                    }
                    if (text != null) break;
                }
                if (text != null) break;
            }

            if (text == null)
            {
                await DisplayAlertAsync("未识别", "未能从图片识别二维码。请对准二维码、避免反光/模糊，或扫 sync-qr.png 图片文件。", "关闭");
                return;
            }
            FillFromJson(text);
            ShowStatus();
            await DisplayAlertAsync("已识别", "已从二维码填入仓库/凭证，点「📥 克隆 / 拉取」同步。", "确定");
#else
            await DisplayAlertAsync("扫码", "当前平台暂不支持拍照扫码，请手动填写。", "关闭");
#endif
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("扫码失败", ex.Message, "关闭");
        }
    }

#if ANDROID
    /// <summary>旋转位图（处理相机照片 EXIF 旋转）。</summary>
    private static Android.Graphics.Bitmap RotateBitmap(Android.Graphics.Bitmap src, int angle)
    {
        if (angle == 0) return src;
        var matrix = new Android.Graphics.Matrix();
        matrix.PostRotate(angle);
        return Android.Graphics.Bitmap.CreateBitmap(src, 0, 0, src.Width, src.Height, matrix, true);
    }
#endif

    /// <summary>解析桌面 /sync-qr 生成的 JSON（url/user/token）填入输入框。</summary>
    private void FillFromJson(string json)
    {
        var url = Regex.Match(json, "\"url\":\"([^\"]*)\"").Groups[1].Value;
        var user = Regex.Match(json, "\"user\":\"([^\"]*)\"").Groups[1].Value;
        var token = Regex.Match(json, "\"token\":\"([^\"]*)\"").Groups[1].Value;
        if (!string.IsNullOrEmpty(url)) { RepoUrlEntry.Text = url; LoadRecentRepos(); }
        if (!string.IsNullOrEmpty(user)) UserEntry.Text = user;
        if (!string.IsNullOrEmpty(token)) TokenEntry.Text = token;
    }
}
