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

    /// <summary>
    /// 是否接管。**由 <see cref="EditorEntryHandler"/> 在按 StyleId 认出「这就是编辑器那个输入框」
    /// 时置位** —— 不再由页面去「认类型再接线」（那条路一旦认不出来就静默失效，
    /// 用户看到的就是「擦除键没反应」）。
    /// </summary>
    public bool InterceptEnabled { get; set; }

    /// <summary>IME 是否正在组合（拼音未上屏）。组合中一律不接管 —— 退格该归输入法删拼音缓冲。</summary>
    private bool _composing;

    /// <summary>
    /// **输入法真实的组合区间**是不是空的。
    ///
    /// 为什么不信自己那个 <see cref="_composing"/> 标志（真机实测的症状就是它闹的）：
    /// 标志只由 <c>SetComposingText</c> 置位、由 <c>CommitText</c>/<c>FinishComposingText</c> 清掉，
    /// 而**程序里给输入框写文本**（进编辑态 `LineEditor.Text = …`、合行、辅助条插入）会让输入法
    /// 重新同步 —— 它可能就此留下一个名不副实的组合态，标志于是**永久为真**，
    /// 之后每一次退格都被判给输入法。
    /// 用户看到的正是：**候选区随退格变化、文本却一个字都不动**。
    ///
    /// 这里改成问**平台真正的组合区间**（`BaseInputConnection.GetComposingSpan*`）——
    /// 它是权威的，不依赖我们有没有把标志维护对。
    /// </summary>
    private bool ReallyComposing()
    {
        try
        {
            var editable = EditableText;
            if (editable == null) return false;
            int s = BaseInputConnection.GetComposingSpanStart(editable);
            int e = BaseInputConnection.GetComposingSpanEnd(editable);
            return s >= 0 && e > s;
        }
        catch
        {
            // 拿不到就退回自己的标志（保守：宁可相信「在组合」，也不要在拼音中途乱删）
            return _composing;
        }
    }

    /// <summary>最近一次建出来的输入连接（未包装的那个）—— 用来主动清组合区。</summary>
    private IInputConnection? _lastIc;

    public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
    {
        var ic = base.OnCreateInputConnection(outAttrs);
        _lastIc = ic;
        if (ic == null || !InterceptEnabled) return ic;
        return new Hook(ic, this);
    }

    /// <summary>
    /// **清掉输入法残留的组合区**。
    ///
    /// 为什么需要它：当时的真机日志显示，按退格时**三条路一条都没触发**
    /// （只有 `OnCreateInputConnection` 那一条日志）—— 说明退格根本没到应用层，
    /// 被输入法当成「还在组合中」在自己的缓冲里处理掉了，而那个组合态是**残留的**：
    /// 页面进编辑态时会 `LineEditor.Text = …` 写文本，输入法因此重新同步并留下一个
    /// 名不副实的组合区。
    ///
    /// 所以**每次程序化写完输入框的文本，都要调一次这里**，把它的状态清干净。
    /// ⚠ 用 `FinishComposingText` 而不是 `InputMethodManager.RestartInput` ——
    /// 后者会重启输入法（键盘会闪一下），是最后手段。
    /// </summary>
    public void ClearStaleComposing()
    {
        try
        {
            _lastIc?.FinishComposingText();
        }
        catch { }
    }

    private sealed class Hook : InputConnectionWrapper
    {
        private readonly BackspaceAwareEditText _owner;

        public Hook(IInputConnection target, BackspaceAwareEditText owner) : base(target, false) => _owner = owner;

        // ── 组合态跟踪 ──
        // ⚠ 这三条**不打日志**：它们每敲一个字都会来一次，把 logcat 淹掉的同时
        // 还在输入路径上做字符串格式化。它们只负责维护 `_composing` 标志。
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
            => HandleBackspace(beforeLength, "InCodePoints")
               || base.DeleteSurroundingTextInCodePoints(beforeLength, afterLength);

        public override bool DeleteSurroundingText(int beforeLength, int afterLength)
            => HandleBackspace(beforeLength, "Surrounding")
               || base.DeleteSurroundingText(beforeLength, afterLength);

        public override bool SendKeyEvent(KeyEvent? e)
        {
            if (e is { Action: KeyEventActions.Down } && !_owner.ReallyComposing())
            {
                var page = Pages.EditorPage.Active;
                if (page != null)
                {
                    if (e.KeyCode == Keycode.Del && _owner.SelectionStart == 0
                        && page.TryJoinWithPreviousLine())
                        return true;

                    if (e.KeyCode == Keycode.ForwardDel && _owner.SelectionStart >= _owner.Text?.Length
                        && page.TryJoinWithNextLine())
                        return true;
                }
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
        private bool HandleBackspace(int before, string via)
        {
            bool comp = _owner.ReallyComposing();
            var page = Pages.EditorPage.Active;

            bool sel = false, join = false;
            if (!comp && page != null)
            {
                // ⚠ 这两个调用**有副作用**（真的会删），所以只调一次、把结果记下来复用
                sel = page.TryDeleteSelection();
                if (!sel && before > 0 && _owner.SelectionStart == 0)
                    join = page.TryJoinWithPreviousLine();
            }

            return sel || join;
        }
    }
}
