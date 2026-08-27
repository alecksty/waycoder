using System.Text;
using WayCoder.Git;
using WayCoder.Infra;
using WayCoder.UI.Tui.Screens;
using ZXing.Common;
using ZXing.QrCode.Internal;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /sync-qr —— 为跨设备代码同步生成二维码（含仓库 URL + 凭证 JSON），手机扫码免输入。
/// 终端渲染半块 ASCII 二维码 + 存 sync-qr.png。用 ZXing.Net 编码（纯托管，AOT 安全）。
/// </summary>
public class SyncQrCommand : SlashCommand
{
    public override string Name => "/sync-qr";
    public override string Description => "生成代码同步二维码（仓库+凭证），手机扫码一键同步";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        try
        {
            var repoRoot = GitCore.FindRepoRoot(Environment.CurrentDirectory);
            if (repoRoot == null)
            {
                screen.AddSystemMsg("❌ 当前目录不是 git 仓库（或未初始化）。先 git init / 在仓库目录运行。");
                return Task.CompletedTask;
            }

            var gitDir = Path.Combine(repoRoot, ".git");
            var url = GitCore.ReadRemoteUrl(gitDir) ?? "";
            var cred = GitCore.ReadCredential(gitDir);

            var payload = new StringBuilder("{");
            payload.Append("\"url\":\"").Append(Escape(url)).Append('"');
            if (cred is { } c)
            {
                payload.Append(",\"user\":\"").Append(Escape(c.User)).Append('"');
                payload.Append(",\"token\":\"").Append(Escape(c.Secret)).Append('"');
                payload.Append(",\"isToken\":").Append(c.IsToken ? "true" : "false");
            }
            payload.Append('}');
            var json = payload.ToString();

            // ZXing 编码为 BitMatrix（字节模式 + M 纠错——比 L 抗照片反光/噪声，手机扫码更稳）
            var matrix = ZXing.QrCode.Internal.Encoder.encode(json, ErrorCorrectionLevel.M).Matrix;
            screen.AddSystemMsg($"📱 手机「代码同步」页点「扫二维码」扫描（或扫 sync-qr.png）：\n仓库 {url}\n凭证 {(cred is { } ? "已含（用户名+Token）" : "未配置（/git credential 设置后重新生成）")}");
            screen.AddSystemMsg(RenderAscii(matrix));

            // 存 PNG（项目手写 PngEncoder，AOT 安全；scale 10 更清晰）
            try
            {
                var png = RenderPng(matrix, scale: 10);
                var path = Path.Combine(Environment.CurrentDirectory, "sync-qr.png");
                File.WriteAllBytes(path, png);
                screen.AddSystemMsg($"💾 已存二维码图片：{path}");
            }
            catch (Exception ex)
            {
                screen.AddSystemMsg($"⚠️ PNG 保存失败：{ex.Message}");
            }
        }
        catch (Exception ex)
        {
            screen.AddSystemMsg($"❌ 生成二维码失败：{ex.Message}");
        }
        return Task.CompletedTask;
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>ASCII 渲染（正方形模块：每模块 2 字符宽 × 1 行，终端等宽字体下模块≈正方形，手机可扫码）。</summary>
    private static string RenderAscii(ZXing.QrCode.Internal.ByteMatrix m)
    {
        var sb = new StringBuilder();
        for (int y = 0; y < m.Height; y++)
        {
            for (int x = 0; x < m.Width; x++)
                sb.Append(m[x, y] != 0 ? "██" : "  ");
            sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>渲染为 RGBA PNG（PngEncoder.Encode）。</summary>
    private static byte[] RenderPng(ZXing.QrCode.Internal.ByteMatrix m, int scale)
    {
        int size = m.Width * scale;
        var rgba = new byte[size * size * 4];
        for (int y = 0; y < m.Height; y++)
            for (int x = 0; x < m.Width; x++)
            {
                var dark = m[x, y] != 0;
                for (int dy = 0; dy < scale; dy++)
                    for (int dx = 0; dx < scale; dx++)
                    {
                        int i = (((y * scale + dy) * size) + (x * scale + dx)) * 4;
                        rgba[i] = rgba[i + 1] = rgba[i + 2] = (byte)(dark ? 0 : 255);
                        rgba[i + 3] = 255;
                    }
            }
        return PngEncoder.Encode(size, size, rgba);
    }
}
