namespace WayCoder.UI;

/// <summary>
/// 对话框动作结果 —— 对标 Crush 的 typed action structs。
/// 替代裸 object? 返回值，提供类型安全的对话框结果传递。
/// </summary>
public static class DialogAction
{
    /// <summary>关闭对话框（无选择）</summary>
    public record Close;

    /// <summary>用户确认（OK/Yes）</summary>
    public record Confirm;

    /// <summary>用户取消（Cancel/No/Esc）</summary>
    public record Cancel;

    /// <summary>用户选择了一个值</summary>
    public record Select<T>(T Value);

    /// <summary>权限确认结果</summary>
    public enum PermissionResponse { Allow, Deny, AllowAll }

    /// <summary>权限确认动作</summary>
    public record Permission(PermissionResponse Response);

    /// <summary>文件选择结果</summary>
    public record FilePicked(string FilePath);

    /// <summary>多选结果</summary>
    public record MultiSelect<T>(IReadOnlySet<T> Selected);

    /// <summary>文本输入结果</summary>
    public record TextInput(string Text);

    // ── 工厂方法 ──

    public static Close CloseAction => new();
    public static Confirm ConfirmAction => new();
    public static Cancel CancelAction => new();
    public static Select<T> Selection<T>(T value) => new(value);
    public static Permission Allow => new(PermissionResponse.Allow);
    public static Permission Deny => new(PermissionResponse.Deny);
    public static Permission AllowAll => new(PermissionResponse.AllowAll);
    public static FilePicked File(string path) => new(path);
    public static MultiSelect<T> Selected<T>(IReadOnlySet<T> s) => new(s);
    public static TextInput Input(string text) => new(text);
}
