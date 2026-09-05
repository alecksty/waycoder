namespace WayCoder.Git;

/// <summary>
/// Git 二进制辅助 —— 大端整数读取。从 GitCore/PackFile 抽取共享，消除两处逐字重复。
/// </summary>
internal static class GitBin
{
    public static int ReadInt32BE(byte[] b, int off)
        => (b[off] << 24) | (b[off + 1] << 16) | (b[off + 2] << 8) | b[off + 3];

    public static long ReadInt64BE(byte[] b, int off)
    {
        long v = 0;
        for (int i = 0; i < 8; i++) v = (v << 8) | b[off + i];
        return v;
    }
}
