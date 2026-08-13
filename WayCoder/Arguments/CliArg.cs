namespace WayCoder.Arguments;

/// <summary>
/// CLI 参数基类 —— 所有命令行参数定义继承此类。
/// 声明参数的 Key（唯一标识）、Names（长短名数组）、描述、值数量等元数据。
/// 子类覆写 OnMatch 可实现自定义匹配行为（如立即执行工具并退出）。
///
/// 注册时自动检测名称冲突（长短名全局唯一），有重复立即抛异常。
/// </summary>
public class CliArg
{
    /// <summary>参数唯一标识（驼峰命名）</summary>
    public string Key { get; }

    /// <summary>所有匹配名称，首项为规范长名。如 ["-m", "--model"]</summary>
    public string[] Names { get; }

    /// <summary>帮助文本（一行）</summary>
    public virtual string Description => "";

    /// <summary>
    /// 值的数量：
    ///   0  = 标志（无值，如 --version）
    ///   1  = 必须带一个值（如 --model &lt;名称>）
    ///  -1  = 可选值（仅当下一个 arg 不以 - 开头时消耗，如 --test [模块名]）
    /// </summary>
    public virtual int ValueCount => 0;

    /// <summary>值标签（帮助中显示在名称后），null 则不显示</summary>
    public virtual string? ValueLabel => null;

    /// <summary>多次出现时累积值而非覆盖（用于排队场景）</summary>
    public virtual bool AllowMultiple => false;

    /// <summary>贪婪模式：消耗后续参数直到遇到下一个以 - 开头的旗标（用于 --config / --model 等变长子命令）</summary>
    public virtual bool Greedy => false;

    /// <summary>是否为内部/开发参数（默认不在帮助中显示）</summary>
    public virtual bool Internal => false;

    protected CliArg(string key, params string[] names)
    {
        Key = key;
        Names = names.Length > 0 ? names : throw new ArgumentException($"CliArg '{key}' 至少需要一个名称");
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("CliArg Key 不能为空");
    }

    /// <summary>
    /// 参数匹配时调用。
    /// values: 消耗掉的值列表（ValueCount=0 时为空列表）。
    /// 返回 null → 继续执行；返回 int → 立即退出并返回该退出码。
    /// </summary>
    public virtual int? OnMatch(List<string> values) => null;

    /// <summary>帮助显示用的名称部分，如 "-m, --model &lt;名称>"</summary>
    public string NameDisplay
    {
        get
        {
            var names = string.Join(", ", Names);
            return ValueLabel != null ? $"{names} <{ValueLabel}>" : names;
        }
    }
}
