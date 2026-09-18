using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using Java.Lang;
using Microsoft.Maui.Platform;

namespace WayCoder.Maui;

/// <summary>
/// 专为编辑器那一个输入框存在的 <see cref="MauiAppCompatEditText"/>：**能看见「行首退格」**。
///
/// <para>
/// **为什么只能靠子类**：软键盘的退格**不走按键事件**（所以页面上现用的 <c>View.KeyPress</c>
/// 收不到），而是走 <c>InputConnection</c> —— Gboard 用 <c>deleteSurroundingTextInCodePoints</c>，
/// 部分输入法用 <c>deleteSurroundingText</c>，文本为空/行首时回落 <c>sendKeyEvent(KEYCODE_DEL)</c>。
/// 三条路都得拦。而 <c>onCreateInputConnection</c> 是 <c>TextView</c> 的 <c>protected</c> 方法，
/// **没有第二种挂法** —— mapper 拿到的是已经建好的实例，覆写不了。
/// </para>
///
/// <para>
/// **行为边界**：只有 <see cref="InterceptEnabled"/> 为真（= 页面确认这是编辑器的那个输入框）
/// 时才包装；关着时本类与父类**逐字节相同**，包装器只做透传。
/// </para>
/// </summary>
internal sealed class BackspaceAwareEditText : MauiAppCompatEditText
{
    public BackspaceAwareEditText(Context ctx) : base(ctx) { }

    /// <summary>是否接管。由页面在 <c>HandlerChanged</c> 里按 <c>StyleId</c> 确认后置位。</summary>
    public bool InterceptEnabled { get; set; }

    /// <summary>行首退格（返回 true = 已处理，吃掉这次事件）。</summary>
    public Func<bool>? OnBackspaceAtLineStart { get; set; }

    /// <summary>行尾 Delete（对称：与下一行合行）。硬件键盘才发得出来。</summary>
    public Func<bool>? OnDeleteAtLineEnd { get; set; }

    /// <summary>有选区时先删选区（标准编辑器语义）。没有选区时须返回 false。</summary>
    public Func<bool>? OnDeleteSelection { get; set; }

    /// <summary>IME 是否正在组合（拼音未上屏）。组合中一律不接管 —— 退格该归输入法删拼音缓冲。</summary>
    private bool _composing;

    public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
    {
        var ic = base.OnCreateInputConnection(outAttrs);
        if (ic == null || !InterceptEnabled) return ic;
        return new Hook(ic, this);
    }

    private sealed class Hook : InputConnectionWrapper
    {
        private readonly BackspaceAwareEditText _owner;

        public Hook(IInputConnection target, BackspaceAwareEditText owner) : base(target, false) => _owner = owner;

        // ── 组合态跟踪 ──
        public override bool SetComposingText(ICharSequence? text, int newCursorPosition)
        {
            _owner._composing = true;
            return base.SetComposingText(text, newCursorPosition);
        }

        public override bool CommitText(ICharSequence? text, int newCursorPosition)
        {
            _owner._composing = false;
            return base.CommitText(text, newCursorPosition);
        }

        public override bool FinishComposingText()
        {
            _owner._composing = false;
            return base.FinishComposingText();
        }

        // ── 退格的三条路 ──
        public override bool DeleteSurroundingTextInCodePoints(int beforeLength, int afterLength)
            => HandleBackspace(beforeLength)
               || base.DeleteSurroundingTextInCodePoints(beforeLength, afterLength);

        public override bool DeleteSurroundingText(int beforeLength, int afterLength)
            => HandleBackspace(beforeLength)
               || base.DeleteSurroundingText(beforeLength, afterLength);

        public override bool SendKeyEvent(KeyEvent? e)
        {
            if (e is { Action: KeyEventActions.Down } && !_owner._composing)
            {
                if (e.KeyCode == Keycode.Del && _owner.SelectionStart == 0
                    && _owner.OnBackspaceAtLineStart?.Invoke() == true)
                    return true;

                if (e.KeyCode == Keycode.ForwardDel && _owner.SelectionStart >= _owner.Text?.Length
                    && _owner.OnDeleteAtLineEnd?.Invoke() == true)
                    return true;
            }
            return base.SendKeyEvent(e);
        }

        /// <summary>
        /// 返回 true = 我们吃掉了这次的「删前面 <paramref name="before"/> 个字符」。
        ///
        /// **门控是安全的关键**：只有「光标在第 0 列且确实要往前删」时才接管。光标在第 0 列时，
        /// 平台的 <c>deleteSurroundingText</c> 本来就删不掉任何东西 ⇒ 就算这里判断错了
        /// （比如回调没接上），行为也**不会比现在更糟**。
        /// </summary>
        private bool HandleBackspace(int before)
        {
            if (_owner._composing) return false;                        // 拼音组合中 → 归输入法
            if (_owner.OnDeleteSelection?.Invoke() == true) return true; // 有可见选区 → 删选区
            if (before > 0 && _owner.SelectionStart == 0)
                return _owner.OnBackspaceAtLineStart?.Invoke() == true;
            return false;
        }
    }
}
