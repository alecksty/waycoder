namespace CompilerBase;

/// <summary>
/// 「**没有进展 ⇒ 死循环**」的唯一判据 —— 把「编译卡死」变成一条**看得见的报错**。
///
/// <para>
/// 起因是真事：Dart 前端遇到「文件不完整（EOF 处少 `}`）」时会**死循环** ——
/// 进程不退出、不打任何输出，用户那边就是「点了编译，界面永远卡在编译中」。
/// 而这类成因**修不完**：任何一门语言的任何一层，只要有一个循环写的条件不对、
/// 又没有推进保证，就是同一个症状。所以要有一条**与具体缺陷无关的兜底**。
/// </para>
///
/// <para>
/// **判据为什么是"位置长时间不动"而不是"总步数超限"**：
/// 总步数超限对**大文件**必然误报（合法解析本来就要走很多步，步数与文件大小成正比）；
/// 而"两次推进之间读了 N 次当前位置都没动"在合法输入上**不可能发生** ——
/// 正常解析在两次消费之间只做常数次（看几位前瞻），跟文件多大无关。
/// 所以这个阈值可以给得很松（百万级）却仍然精确。
/// </para>
///
/// <para>
/// ⚠ 用法：**热路径每次读当前位置时 `Tick` 一次**（解析器在 `Cur`、词法器在 `Peek`），
/// 返回 true 就由调用方抛一条带位置的错。判据只此一处 —— 各层自己写一遍的话，
/// 阈值与语义必然漂移。
/// </para>
/// </summary>
internal sealed class ProgressGuard
{
    private readonly int _limit;
    private int _lastPos = -1;
    private int _samePosCount;

    /// <param name="limit">同一位置最多容忍读多少次。正常解析在两次推进之间只读常数次，
    /// 所以这个值给到百万级仍然精确 —— 它只在**真的卡住**时才会到。</param>
    public ProgressGuard(int limit) => _limit = limit;

    /// <summary>热路径每读一次当前位置调一次。返回 <c>true</c> = 判定为死循环。</summary>
    public bool Tick(int pos)
    {
        if (pos != _lastPos)
        {
            _lastPos = pos;
            _samePosCount = 0;
            return false;
        }
        return ++_samePosCount > _limit;
    }
}
