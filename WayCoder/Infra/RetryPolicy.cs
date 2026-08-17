namespace WayCoder;

/// <summary>
/// 智能重试策略 —— 支持指数退避、异常过滤、最大延迟限制。
/// 用于工具调用的自动恢复，减少因瞬时错误导致的人工干预。
/// </summary>
public class RetryPolicy
{
    /// <summary>默认重试配置：3次，100ms起，5s上限</summary>
    public static readonly RetryConfig Default = new();

    /// <summary>
    /// 带重试的异步执行。按 <see cref="RetryConfig"/> 的设定自动重试，
    /// 指数退避公式：delay = min(BaseDelayMs * 2^retry, MaxDelayMs)。
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="action">要执行的异步操作</param>
    /// <param name="config">重试配置，null 使用默认</param>
    /// <param name="onRetry">每次重试时的回调（重试次数, 异常, 延迟毫秒）</param>
    /// <returns>操作结果</returns>
    /// <exception cref="Exception">所有重试耗尽后抛出最后一次异常</exception>
    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> action,
        RetryConfig? config = null,
        Action<int, Exception, int>? onRetry = null)
    {
        var cfg = config ?? Default;
        // 负数 MaxRetries 钳制为 0：否则 for 条件立即为 false，action 一次都不执行，
        // 却落到「不可达终点」抛误导性的 InvalidOperationException。
        var maxRetries = Math.Max(0, cfg.MaxRetries);

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (cfg.ShouldRetry(ex) && attempt < maxRetries)
            {
                var baseDelayMs = (int)Math.Min(
                    cfg.BaseDelayMs * Math.Pow(2, attempt),
                    cfg.MaxDelayMs);
                // 对称 jitter：在基础退避延迟上做 ±ratio 抖动，打破多客户端同时重试的「惊群」
                var delayMs = ComputeJitteredDelay(baseDelayMs, cfg.JitterRatio, Random.Shared.NextDouble());
                onRetry?.Invoke(attempt + 1, ex, delayMs);
                await Task.Delay(delayMs);
            }
        }

        // 不可达：每次迭代要么成功返回，要么异常因 when 过滤器不满足
        // （不可重试或已耗尽 attempt == MaxRetries）而原样向外抛出最后一次异常。
        throw new InvalidOperationException("重试循环不可达终点。");
    }

    /// <summary>
    /// 计算对称 jitter 后的延迟：delay = base * (1 + (unit*2 - 1) * ratio)。
    /// unit 为 [0,1) 的随机数，纯逻辑便于自测；ratio ≤ 0 时原样返回 base。
    /// </summary>
    internal static int ComputeJitteredDelay(int baseDelayMs, double jitterRatio, double unit)
    {
        if (jitterRatio <= 0) return baseDelayMs;
        var factor = 1.0 + (unit * 2.0 - 1.0) * jitterRatio;
        return (int)Math.Round(baseDelayMs * factor);
    }

    /// <summary>无返回值的重试版本。</summary>
    public static async Task RetryAsync(
        Func<Task> action,
        RetryConfig? config = null,
        Action<int, Exception, int>? onRetry = null)
    {
        await RetryAsync<object?>(async () =>
        {
            await action();
            return null;
        }, config, onRetry);
    }
}

/// <summary>
/// 重试配置 —— 最大重试次数、延迟策略、异常过滤。
/// </summary>
public class RetryConfig
{
    /// <summary>最大重试次数（默认 3）</summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>基础延迟毫秒（默认 100ms）</summary>
    public int BaseDelayMs { get; init; } = 100;

    /// <summary>最大延迟毫秒（默认 5000ms）</summary>
    public int MaxDelayMs { get; init; } = 5_000;

    /// <summary>
    /// 对称 jitter 比例（0..1）。重试延迟在 ±ratio 范围内随机抖动，
    /// 打破多客户端同时重试的「惊群」效应（对标 deepseek-harness jitterRatio=0.1）。
    /// 0 = 禁用（确定性退避）。默认 0.1。
    /// </summary>
    public double JitterRatio { get; init; } = 0.1;

    /// <summary>
    /// 允许重试的异常类型全名集合。null 或空 = 所有异常都重试。
    /// 示例：["System.Net.Http.HttpRequestException", "System.TimeoutException"]
    /// </summary>
    public HashSet<string>? RetryableExceptions { get; init; }

    /// <summary>
    /// 禁止重试的异常类型全名集合（优先级高于白名单）。
    /// 默认禁止：ArgumentException, ArgumentNullException, InvalidOperationException,
    ///           OperationCanceledException, TaskCanceledException
    /// </summary>
    public HashSet<string> NoRetryExceptions { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "System.ArgumentException",
        "System.ArgumentNullException",
        "System.ArgumentOutOfRangeException",
        "System.InvalidOperationException",
        "System.OperationCanceledException",
        "System.Threading.Tasks.TaskCanceledException",
        "System.NotSupportedException",
        "System.NotImplementedException",
    };

    /// <summary>判断给定异常是否应重试。</summary>
    public bool ShouldRetry(Exception ex)
    {
        var typeName = ex.GetType().FullName ?? ex.GetType().Name;

        // 黑名单优先（?. 保护：调用方显式置 null 时视为「不禁止任何异常」，与 RetryableExceptions 的 is 判断一致）
        if (NoRetryExceptions?.Contains(typeName) == true)
            return false;

        // 白名单过滤
        if (RetryableExceptions is { Count: > 0 })
            return RetryableExceptions.Contains(typeName);

        return true; // 默认：所有异常都重试（除黑名单）
    }
}
