using System.Text;
using WayCoder.Git;
using WayCoder.Infra;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /sync-qr —— 为跨设备代码同步生成二维码（含仓库 URL + 凭证 JSON），手机扫码免输入。
/// 手写 QrEncoder（ISO/IEC 18004，AOT 安全）编码 + 终端半块窄二维码 + 存 sync-qr.png（小文件）。
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

            // 手写 QR 编码（字节模式 + M 纠错——比 L 抗照片反光/噪声，手机扫码更稳）
            var qr = QrEncoder.EncodeText(json, QrEcLevel.Medium);
            screen.AddSystemMsg($"📱 手机「代码同步」页点「扫二维码」扫描（或扫 sync-qr.png）：\n仓库 {url}\n凭证 {(cred is { } ? "已含（用户名+Token）" : "未配置（/git credential 设置后重新生成）")}");
            screen.AddSystemMsg(RenderAscii(qr.Matrix));

            // 存 PNG（项目手写 PngEncoder + QrEncoder，AOT 安全；scale 5 + 4 模块白边：文件小且手机易扫）
            try
            {
                var png = RenderPng(qr.Matrix, scale: 5, quiet: 4);
                var path = Path.Combine(Environment.CurrentDirectory, "sync-qr.png");
                File.WriteAllBytes(path, png);
                screen.AddSystemMsg($"💾 已存二维码图片：{path}（{qr.Size + 8}×{qr.Size + 8}px 含白边，v{qr.Version}）");
            }
            catch (Exception ex)
            {
                screen.AddSystemMsg($"⚠️ PNG 保存失败：{ex.Message}");
            }

            // 全屏大二维码：手机扫屏（白底黑块，ANSI 背景色填充，与 PNG 同对比——修复深色终端
            // 前景 █ ASCII 反色扫不到的问题）。只用全块大方块（半块字形 macOS 有缝隙弃用），
            // 终端行/列足够才进入全屏，Esc/q 返回；不够则提示拉高/拉宽窗口或打开 sync-qr.png。
            // 聊天里的小 ASCII 预览已在上方保留作上下文记录。
            int grid = qr.Size + 8; // 含 quiet zone 4 模块白边
            if (TuiManager.Instance.IsActive && QrScanScreen.CanFit(qr.Matrix, Tty.Cols, Tty.Rows))
            {
                TuiManager.Instance.PushScreen(new QrScanScreen(qr.Matrix));
            }
            else if (TuiManager.Instance.IsActive)
            {
                screen.AddSystemMsg($"📱 全屏二维码：需 ≥{grid} 行 × ≥{grid * 2} 列（当前 {Tty.Cols}×{Tty.Rows}）以大方块显示。请拉高/拉宽终端窗口后重试 /sync-qr，或打开 sync-qr.png 扫码。");
            }
        }
        catch (Exception ex)
        {
            screen.AddSystemMsg($"❌ 生成二维码失败：{ex.Message}");
        }
        return Task.CompletedTask;
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>
    /// 窄版 ASCII 二维码：半块字符（▀▄█ + 空格）把 2 个模块行合成 1 个终端行、1 字符/模块列，
    /// 等宽终端下模块≈正方形且宽度比旧「██ 2 字符」减半。四周加 2 模块安静区提升扫码率。
    /// 输入矩阵 [y, x]，true=黑。
    /// </summary>
    internal static string RenderAscii(bool[,] m)
    {
        int size = m.GetLength(0);
        const int qz = 2; // quiet zone（模块）
        int grid = size + qz * 2;         // 含安静区的网格边长（列 = 行数）
        int lines = (grid + 1) / 2;       // 半块：每终端行覆盖 2 个模块行
        var sb = new StringBuilder();
        for (int line = 0; line < lines; line++)
        {
            int my0 = line * 2 - qz;      // 上半模块行（含安静区偏移）
            int my1 = my0 + 1;            // 下半模块行
            for (int gx = 0; gx < grid; gx++)
            {
                int mx = gx - qz;
                bool top = In(m, mx, my0);
                bool bot = In(m, mx, my1);
                sb.Append(top ? (bot ? '█' : '▀') : (bot ? '▄' : ' '));
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>矩阵外（quiet zone / 相邻半块越界）视为白。</summary>
    private static bool In(bool[,] m, int x, int y)
    {
        int n = m.GetLength(0);
        return x >= 0 && x < n && y >= 0 && y < n && m[y, x];
    }

    /// <summary>渲染为 RGBA PNG（PngEncoder.Encode），含 quiet zone 白边后整体放大 scale 倍。</summary>
    internal static byte[] RenderPng(bool[,] m, int scale, int quiet)
    {
        int n = m.GetLength(0);
        int full = n + quiet * 2;
        int px = full * scale;
        var rgba = new byte[px * px * 4];
        for (int y = 0; y < full; y++)
            for (int x = 0; x < full; x++)
            {
                bool dark = x >= quiet && x < quiet + n && y >= quiet && y < quiet + n && m[y - quiet, x - quiet];
                for (int dy = 0; dy < scale; dy++)
                    for (int dx = 0; dx < scale; dx++)
                    {
                        int i = (((y * scale + dy) * px) + (x * scale + dx)) * 4;
                        rgba[i] = rgba[i + 1] = rgba[i + 2] = (byte)(dark ? 0 : 255);
                        rgba[i + 3] = 255;
                    }
            }
        return PngEncoder.Encode(px, px, rgba);
    }
}
