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
        Exception? lastEx = null;

        for (int attempt = 0; attempt <= cfg.MaxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (cfg.ShouldRetry(ex) && attempt < cfg.MaxRetries)
            {
                lastEx = ex;
                var delayMs = (int)Math.Min(
                    cfg.BaseDelayMs * Math.Pow(2, attempt),
                    cfg.MaxDelayMs);
                onRetry?.Invoke(attempt + 1, ex, delayMs);
                await Task.Delay(delayMs);
            }
        }

        throw new AggregateException(
            $"操作在 {cfg.MaxRetries} 次重试后仍然失败。",
            lastEx ?? new Exception("未知错误"));
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

        // 黑名单优先
        if (NoRetryExceptions.Contains(typeName))
            return false;

        // 白名单过滤
        if (RetryableExceptions is { Count: > 0 })
            return RetryableExceptions.Contains(typeName);

        return true; // 默认：所有异常都重试（除黑名单）
    }
}
