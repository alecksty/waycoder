namespace WayCoder.UI.Shared;

/// <summary>
/// 思考流解析器 —— 把 LLM 的 token 流按 <c>«dim»…«/»</c> 标记拆成「思考」与「正文」两路。
///
/// **规则单一真源 = Web 的 <c>app.js</c> <c>handleToken</c>**（本类与它逐条对应，改一边记得改另一边）：
///
/// ① <c>«dim»</c> 开启思考块（深度置 1）；已在思考块内再遇 <c>«dim»</c> 是 no-op（Web 的 <c>ensureThink</c>
///    对已存在的块直接返回，不叠加深度）。
/// ② **结束思考块看层数，不是见到 <c>«/»</c> 就关** —— 推理正文里可以嵌别的标记
///    （LLM 思考超长时注入 <c>«orange3»… 思考内容过长…«/»</c>）。块内**新开启**的标记计入深度
///    （见 <see cref="CountMarkupOpeners"/>），<c>«/»</c> 逐层递减，归零才算思考结束。
/// ③ **不在思考块时 <c>«/»</c> 必须原样进正文** —— 它是**所有** «» 标记（颜色/粗体…）的统一结束符，
///    不是思考块专用的。剥掉会让渲染器的 <c>«cyan»…«/»</c> 配对失配、颜色错位。Web 端曾因无条件
///    把 <c>«/»</c> 当思考收尾而出现「完全不聊天了，所有内容都是已思考 n 秒」，这条就是那个修复。
/// ④ 一个 token 里可能同时含标记与正文（也见过正文紧贴标记，如 <c>"答案«/»\n"</c>），
///    所以按标记位置切开逐段归位，而不是整 token 二分。
///
/// 纯逻辑、零终端依赖 → 放 <c>UI/Shared/</c>（MAUI 也编译得到），便于自测。
/// </summary>
public sealed class ThinkStreamParser
{
    /// <summary>思考开启标记（长度 5）</summary>
    public const string DimTag = "«dim»";

    /// <summary>所有 «» 标记的统一结束符（长度 3）</summary>
    public const string CloseTag = "«/»";

    /// <summary>当前嵌套层数：0 = 不在思考块，≥1 = 思考中（1 = «dim» 自身那一层）</summary>
    private int _depth;

    /// <summary>是否正处在思考块内。</summary>
    public bool InThinking => _depth > 0;

    /// <summary>当前思考块内累积的正文（供调用方判断内容是否为空；不含被消费掉的标记）。</summary>
    public int ThinkChars { get; private set; }

    /// <summary>
    /// 喂入一个 token，按标记切分后回调分派。
    /// </summary>
    /// <param name="token">原始流式片段（可为 null/空，直接忽略）</param>
    /// <param name="onBody">正文片段</param>
    /// <param name="onThink">思考片段（仅深度 &gt; 0 时）</param>
    /// <param name="onThinkStart">思考块**开始**（深度 0→1 的那一次，重复的 <c>«dim»</c> 不触发）</param>
    /// <param name="onThinkEnd">思考块**结束**（深度归零的那一次）</param>
    public void Feed(string? token,
        Action<string> onBody, Action<string> onThink,
        Action? onThinkStart = null, Action? onThinkEnd = null)
    {
        var rest = token ?? "";
        while (rest.Length > 0)
        {
            int dimAt = rest.IndexOf(DimTag, StringComparison.Ordinal);
            int closeAt = rest.IndexOf(CloseTag, StringComparison.Ordinal);
            if (dimAt < 0 && closeAt < 0)
            {
                Emit(rest, onBody, onThink);
                return;
            }

            // 先出现的标记先处理（两者都在时取靠前的）
            bool useDim = dimAt >= 0 && (closeAt < 0 || dimAt < closeAt);
            int at = useDim ? dimAt : closeAt;

            if (at > 0)
            {
                var pre = rest[..at];
                if (useDim)
                {
                    // 推理前那一口换行（LLM 发 "\n«dim»"）不另起正文段 —— 否则会留一个空消息泡
                    // 排在思考行前面。已在思考块内时 pre 归思考，同样不需要纯空白。
                    if (pre.Trim().Length > 0) Emit(pre, onBody, onThink);
                }
                else
                {
                    // 这一段里**新开启**的标记要计入层数，否则内层的 «/» 会被当成思考收尾
                    if (_depth > 0) _depth += CountMarkupOpeners(pre);
                    Emit(pre, onBody, onThink);
                }
            }

            if (useDim)
            {
                rest = rest[(at + DimTag.Length)..];
                if (_depth == 0)
                {
                    _depth = 1;
                    ThinkChars = 0;
                    onThinkStart?.Invoke();
                }
                // 已在思考块内又遇 «dim»：消费掉但不改变层数（对齐 Web ensureThink 的早退）
            }
            else if (_depth > 0)
            {
                rest = rest[(at + CloseTag.Length)..];
                _depth--;
                if (_depth <= 0)
                {
                    _depth = 0;
                    onThinkEnd?.Invoke();
                }
                else
                {
                    Emit(CloseTag, onBody, onThink); // 只是块内某个标记的结束符 → 留在推理正文里配对
                }
            }
            else
            {
                Emit(CloseTag, onBody, onThink); // 别的标记的结束符：留给渲染器配对
                rest = rest[(at + CloseTag.Length)..];
            }
        }
    }

    /// <summary>复位（异常中断/强制折叠后调用）—— 下次 <c>«dim»</c> 会重新触发一次「思考开始」。</summary>
    public void Reset()
    {
        _depth = 0;
        ThinkChars = 0;
    }

    /// <summary>按当前状态归位一段文本。</summary>
    private void Emit(string text, Action<string> onBody, Action<string> onThink)
    {
        if (text.Length == 0) return;
        if (_depth > 0)
        {
            ThinkChars += text.Length;
            onThink(text);
        }
        else
        {
            onBody(text);
        }
    }

    /// <summary>
    /// 统计一段文本里**新开启**的嵌套标记数（<c>«x»</c>，不含结束符 <c>«/»</c>）。
    /// 对应 Web 的 <c>markupOpeners</c>（正则 <c>/«(?!\/)[^«»]*»/g</c>）：非重叠扫描，
    /// 标记体内不允许再出现 « 或 »（畸形标记不计）。
    /// </summary>
    public static int CountMarkupOpeners(string s)
    {
        int count = 0;
        int i = 0;
        while (i < s.Length)
        {
            int open = s.IndexOf('«', i);
            if (open < 0) break;
            int close = s.IndexOf('»', open + 1);
            if (close < 0) break; // 未闭合：正则同样匹配不到

            // 标记体内不得再含 « 或 »（否则不是这个标记的结束位置）
            int inner = -1;
            for (int k = open + 1; k < close; k++)
            {
                if (s[k] == '«' || s[k] == '»') { inner = k; break; }
            }
            if (inner >= 0)
            {
                i = open + 1; // 从下一个字符重试（对齐正则的回溯起点）
                continue;
            }

            // «/» 是统一结束符，不算「开启」
            bool isCloseTag = close == open + 2 && s[open + 1] == '/';
            if (!isCloseTag) count++;
            i = close + 1;
        }
        return count;
    }
}
