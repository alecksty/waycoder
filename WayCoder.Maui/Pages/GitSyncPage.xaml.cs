using System.Text.RegularExpressions;
using WayCoder.Git;
using WayCoder.Maui.Services;
using ZXing;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 跨设备代码同步页：git 作为同步介质——克隆/拉取远程仓库到沙箱 workspace，
/// 在手机编辑器改完「提交并推送」，家里电脑 pull 即可继续。
/// 复用纯 C# GitCore/GitRemote（AOT 安全），远端操作放后台线程避免卡 UI。
/// </summary>
public partial class GitSyncPage : ContentPage
{
    /// <summary>同步目标：沙箱 workspace（仓库根）。</summary>
    private string RepoRoot => MauiBootstrap.WorkspaceDir;

    public GitSyncPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // 读当前仓库 origin url + SecureStorage 凭证
        try
        {
            var gitDir = Path.Combine(RepoRoot, ".git");
            if (Directory.Exists(gitDir))
            {
                var url = GitCore.ReadRemoteUrl(gitDir);
                if (!string.IsNullOrEmpty(url)) RepoUrlEntry.Text = url;
            }
            var cred = await GitCredentialStore.LoadAsync();
            if (cred is { } c)
            {
                UserEntry.Text = c.User;
                TokenEntry.Text = c.Secret;
            }
        }
        catch { }
        ShowStatus();
    }

    /// <summary>刷新状态区：仓库是否存在 + 分支 + 改动摘要。</summary>
    private void ShowStatus()
    {
        try
        {
            var gitDir = Path.Combine(RepoRoot, ".git");
            if (!Directory.Exists(gitDir))
            {
                StatusLabel.Text = "尚未克隆仓库。填仓库地址点「📥 克隆 / 拉取」，\n同步后文件会出现在「文件」页沙箱工作区，可直接编辑。";
                return;
            }
            var branch = GitCore.Run(RepoRoot, "branch").Trim();
            var status = GitCore.Status(RepoRoot);
            StatusLabel.Text = $"仓库：{RepoUrlEntry.Text ?? ""}\n分支：{branch}\n\n{status}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"状态读取失败：{ex.Message}";
        }
    }

    /// <summary>保存凭证（SecureStorage + 写 .git/config）。</summary>
    private async Task EnsureCredentialsAsync()
    {
        var user = UserEntry.Text?.Trim();
        var token = TokenEntry.Text?.Trim();
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(token)) return;
        await GitCredentialStore.SaveAsync(user, token, isToken: true);
        GitCredentialStore.WriteToRepo(RepoRoot, user, token, isToken: true);
    }

    /// <summary>克隆（非仓库）或拉取（已是仓库）。</summary>
    private async void OnPullClicked(object? sender, EventArgs e)
    {
        var url = RepoUrlEntry.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            await DisplayAlertAsync("缺少仓库地址", "请填写仓库 URL（如 https://gitee.com/user/repo.git）", "确定");
            return;
        }

        PullBtn.IsEnabled = false;
        PullBtn.Text = "同步中…";
        StatusLabel.Text = "⏳ 克隆/拉取中…";
        try
        {
            await EnsureCredentialsAsync();
            var gitDir = Path.Combine(RepoRoot, ".git");
            var result = await Task.Run(() => Directory.Exists(gitDir)
                ? GitRemote.Pull(RepoRoot, [])
                : GitRemote.Clone(RepoRoot, [url]));
            StatusLabel.Text = result;
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"同步失败：{ex.Message}";
        }
        finally
        {
            PullBtn.IsEnabled = true;
            PullBtn.Text = "📥 克隆 / 拉取";
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
            var bmp = Android.Graphics.BitmapFactory.DecodeStream(stream);
            if (bmp == null)
            {
                await DisplayAlertAsync("扫码失败", "无法解码图片", "关闭");
                return;
            }
            var w = bmp.Width; var h = bmp.Height;
            var pixels = new int[w * h];
            bmp.GetPixels(pixels, 0, w, 0, 0, w, h);
            // ARGB int → RGB byte（ZXing RGBLuminanceSource 期望 3 字节/像素）
            var rgb = new byte[w * h * 3];
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                rgb[i * 3] = (byte)((p >> 16) & 0xFF);
                rgb[i * 3 + 1] = (byte)((p >> 8) & 0xFF);
                rgb[i * 3 + 2] = (byte)(p & 0xFF);
            }
            var source = new RGBLuminanceSource(rgb, w, h);
            var result = new BarcodeReaderGeneric().Decode(source);
            if (result == null)
            {
                await DisplayAlertAsync("未识别", "未能从图片识别二维码", "关闭");
                return;
            }
            FillFromJson(result.Text);
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

    /// <summary>解析桌面 /sync-qr 生成的 JSON（url/user/token）填入输入框。</summary>
    private void FillFromJson(string json)
    {
        var url = Regex.Match(json, "\"url\":\"([^\"]*)\"").Groups[1].Value;
        var user = Regex.Match(json, "\"user\":\"([^\"]*)\"").Groups[1].Value;
        var token = Regex.Match(json, "\"token\":\"([^\"]*)\"").Groups[1].Value;
        if (!string.IsNullOrEmpty(url)) RepoUrlEntry.Text = url;
        if (!string.IsNullOrEmpty(user)) UserEntry.Text = user;
        if (!string.IsNullOrEmpty(token)) TokenEntry.Text = token;
    }

    /// <summary>提交并推送：add 全部改动 + commit + push。</summary>
    private async void OnPushClicked(object? sender, EventArgs e)
    {
        var gitDir = Path.Combine(RepoRoot, ".git");
        if (!Directory.Exists(gitDir))
        {
            await DisplayAlertAsync("未克隆仓库", "先点「📥 克隆 / 拉取」同步仓库，再编辑推送。", "确定");
            return;
        }

        var msg = string.IsNullOrWhiteSpace(CommitMsgEntry.Text)
            ? $"手机同步 {DateTime.Now:MM-dd HH:mm}" : CommitMsgEntry.Text.Trim();

        PushBtn.IsEnabled = false;
        PushBtn.Text = "推送中…";
        StatusLabel.Text = "⏳ 提交推送中…";
        try
        {
            await EnsureCredentialsAsync();
            var result = await Task.Run(() =>
            {
                var add = GitCore.Add(RepoRoot, ".");
                var commit = GitCore.Commit(RepoRoot, msg);
                var push = GitRemote.Push(RepoRoot, []);
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
}
