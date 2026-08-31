namespace WayCoder.UI.Shared;

/// <summary>
/// 模型价格统一格式化（四端共用）：显示多重价格 —— 输入价 / 输出价，有闲时价时附「闲」。
/// 单位 $/MTok。免费模型显示 Free。纯函数无反射，AOT 安全。
/// </summary>
public static class ModelPrice
{
    /// <summary>
    /// 格式化模型价格。
    /// </summary>
    /// <param name="inputPrice">忙时输入价（$ / MTok）</param>
    /// <param name="outputPrice">忙时输出价</param>
    /// <param name="inputPriceOffpeak">闲时输入价（0=无闲时价）</param>
    /// <param name="outputPriceOffpeak">闲时输出价</param>
    /// <example>免费 → Free；$1.2/$4.5；忙$1.2/$4.5 闲$0.9/$3.0</example>
    public static string Format(double inputPrice, double outputPrice,
        double inputPriceOffpeak = 0, double outputPriceOffpeak = 0)
    {
        bool free = inputPrice <= 0 && outputPrice <= 0
                 && inputPriceOffpeak <= 0 && outputPriceOffpeak <= 0;
        if (free) return "Free";

        var inS = Price(inputPrice);
        var outS = Price(outputPrice);
        bool hasOffpeak = inputPriceOffpeak > 0 && outputPriceOffpeak > 0
            && (inputPriceOffpeak != inputPrice || outputPriceOffpeak != outputPrice);
        var s = $"{inS}/{outS}";
        return hasOffpeak
            ? $"{s} 闲{Price(inputPriceOffpeak)}/{Price(outputPriceOffpeak)}"
            : s;
    }

    private static string Price(double p) => p switch
    {
        <= 0 => "Free",
        < 0.01 => "<$0.01",
        _ => $"${p:F2}",
    };
}
