using WayCoder.Infra;
using ZXing.QrCode.Internal;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>同步二维码：ZXing 编码矩阵 + PngEncoder PNG 渲染（/sync-qr 命令核心逻辑）。</summary>
    private static void TestSyncQr(Action<string, bool> Check)
    {
        // 编码 JSON payload（含 url/user/token）
        var matrix = ZXing.QrCode.Internal.Encoder
            .encode("{\"url\":\"https://gitee.com/a/b.git\",\"user\":\"u\",\"token\":\"t\"}", ErrorCorrectionLevel.L)
            .Matrix;
        Check("QR: 矩阵非空", matrix.Width > 0 && matrix.Height > 0);
        Check("QR: 矩阵含深色模块", HasDarkModule(matrix));

        // PNG 渲染（与 SyncQrCommand.RenderPng 同逻辑，内联验证）
        const int scale = 8;
        int size = matrix.Width * scale;
        var rgba = new byte[size * size * 4];
        for (int y = 0; y < matrix.Height; y++)
            for (int x = 0; x < matrix.Width; x++)
            {
                var dark = matrix[x, y] != 0;
                for (int dy = 0; dy < scale; dy++)
                    for (int dx = 0; dx < scale; dx++)
                    {
                        int i = (((y * scale + dy) * size) + (x * scale + dx)) * 4;
                        rgba[i] = rgba[i + 1] = rgba[i + 2] = (byte)(dark ? 0 : 255);
                        rgba[i + 3] = 255;
                    }
            }
        var png = PngEncoder.Encode(size, size, rgba);
        Check("QR: PNG 头有效", png.Length > 100 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47);
        Check("QR: PNG 数据合理", png.Length > 1000);
    }

    private static bool HasDarkModule(ByteMatrix m)
    {
        for (int y = 0; y < m.Height; y++)
            for (int x = 0; x < m.Width; x++)
                if (m[x, y] != 0) return true;
        return false;
    }
}
