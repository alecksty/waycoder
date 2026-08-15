// =============================================================
// Vm.cs —— 栈式虚拟机执行器
//
// 执行 Chunk 字节码。采用 double/string 混合栈：每个槽位记录
// 类型标记，运算时按类型分派。支持运行时错误定位到源码行号。
// 输出回调可注入（默认写到 Console）。提供 InputProvider 供 INPUT
// 语句读取用户输入。
//
// 扩展（GORILLA.BAS 支持）：
//   - 例程调用 CallRoutine/EndRoutine（SUB/FUNCTION）
//   - ON ERROR GOTO / RESUME
//   - 图形语句 GfxScreen/Cls/Line/Circle/Pset/Paint/Color/Palette/Locate
//   - sprite GET/PUT（GfxGet/GfxPut，离屏像素缓冲）
//   - 交互 LineInput/Inkey$/Sleep/Timer/Play/Beep
//   - 类型化数组（lo TO hi 下标）、字段数组、DEF FN
// 图形输出通过 GfxDevice 注入（可离屏自测或终端渲染）。
// =============================================================
using System.Globalization;
using System.Text;

namespace QBasic.Compiler;

/// <summary>运行时错误。</summary>
public class RuntimeError : Exception
{
    public int Line;
    public RuntimeError(string msg, int line) : base(msg) { Line = line; }
}

/// <summary>输入源（供 INPUT 语句使用）。</summary>
public interface IInputProvider
{
    string? ReadLine();
}

/// <summary>控制台输入源。</summary>
public class ConsoleInputProvider : IInputProvider
{
    public string? ReadLine() => Console.ReadLine();
}

/// <summary>预置输入源（自测用，依次取列表元素）。</summary>
public class QueueInputProvider : IInputProvider
{
    private readonly Queue<string> _q;
    public QueueInputProvider(IEnumerable<string> lines) { _q = new Queue<string>(lines); }
    public string? ReadLine() => _q.Count > 0 ? _q.Dequeue() : null;
}

/// <summary>输出目标。</summary>
public interface IOutputSink
{
    void Print(string s);
    void PrintLine(string s);
    void Newline();
}

/// <summary>Console 输出。</summary>
public class ConsoleOutput : IOutputSink
{
    public void Print(string s) { Console.Out.Write(s); Console.Out.Flush(); }
    public void PrintLine(string s) { Console.Out.WriteLine(s); Console.Out.Flush(); }
    public void Newline() { Console.Out.WriteLine(); Console.Out.Flush(); }
}

/// <summary>内存输出（自测用）。</summary>
public class MemoryOutput : IOutputSink
{
    private readonly StringBuilder _sb = new();
    public string All => _sb.ToString();
    public void Print(string s) => _sb.Append(s);
    public void PrintLine(string s) => _sb.Append(s).Append('\n');
    public void Newline() => _sb.Append('\n');
    public void Clear() => _sb.Clear();
}

/// <summary>栈槽位（值 + 类型标记）。</summary>
internal struct Slot
{
    public double N;
    public string S;
    public bool IsStr;
}

/// <summary>数组描述（含下界与基址偏移）。</summary>
internal struct ArrayDesc
{
    public double[] Data;
    public string[] StrData;
    public int Lo0, Hi0;
    public bool IsStr;
    public bool IsRange;
}

/// <summary>虚拟机。</summary>
public sealed class Vm
{
    private Chunk? _chunk;
    private readonly List<Slot> _stack = new();
    private readonly List<Slot> _vars = new();
    private readonly List<ArrayDesc> _arrays = new();
    private readonly List<double[,]?> _arrays2 = new();
    private readonly List<string[,]?> _strArrays2 = new();
    private readonly List<int> _callStack = new();
    private int _dataIdx;
    private Random _rnd = new();
    private DateTime _lastPresent = DateTime.MinValue;

    private readonly IInputProvider _input;
    private readonly IOutputSink _output;
    /// <summary>图形设备（可注入，缺省离屏）。</summary>
    public GfxDevice? Gfx { get; set; }
    /// <summary>键盘服务（INKEY$/LINE INPUT）。</summary>
    public IKeyProvider? Keys { get; set; }
    /// <summary>外部呈现回调（终端渲染器挂接，每帧调用）。</summary>
    public Action<GfxDevice>? Present { get; set; }
    /// <summary>是否处于图形渲染模式（true 时每帧呈现）。</summary>
    public bool RenderMode { get; set; }
    /// <summary>最大循环保护步数。</summary>
    public int MaxSteps { get; set; } = 200_000_000;

    // ON ERROR 处理
    private int _errHandler = -1;
    private int _errResumePc = -1;

    // 例程栈：每帧 CallRoutine 记录返回地址
    private readonly List<int> _routineStack = new();

    public Vm(IInputProvider? input = null, IOutputSink? output = null)
    {
        _input = input ?? new ConsoleInputProvider();
        _output = output ?? new ConsoleOutput();
        if (Gfx == null) Gfx = new GfxDevice();
    }

    /// <summary>执行一个编译好的 Chunk。可传入 CancellationToken 实现协作式中断。</summary>
    public void Run(Chunk chunk, CancellationToken cancel = default)
    {
        _chunk = chunk;
        _dataIdx = 0;
        int pc = 0;
        long steps = 0;
        var code = chunk.Code;
        var lines = chunk.Lines;

        while (pc < code.Count)
        {
            if (cancel.IsCancellationRequested)
                throw new OperationCanceledException();
            if (++steps > MaxSteps)
                throw new RuntimeError("循环步数超限（疑似死循环）", lines[pc]);
            int line = lines[pc];
            var op = (OpCode)code[pc];
            pc++;
            try
            {
                switch (op)
                {
                    case OpCode.Nop: break;
                    case OpCode.ConstNum:
                    {
                        int idx = Read16(code, pc); pc += 2;
                        PushNum(chunk.ConstNums[idx]);
                        break;
                    }
                    case OpCode.ConstStr:
                    {
                        int idx = Read16(code, pc); pc += 2;
                        PushStr(chunk.ConstStrs[idx]);
                        break;
                    }
                    case OpCode.VarLoad:
                    {
                        int idx = Read16(code, pc); pc += 2;
                        EnsureVars(idx + 1);
                        Push(_vars[idx]);
                        break;
                    }
                    case OpCode.VarStore:
                    {
                        int idx = Read16(code, pc); pc += 2;
                        EnsureVars(idx + 1);
                        if (chunk.ArrayVars.Contains(idx))
                        {
                            var sizeSlot = Pop();
                            if (!sizeSlot.IsStr)
                                EnsureArray(idx, (int)sizeSlot.N, chunk.VarNames[idx].EndsWith('$'));
                        }
                        else
                        {
                            var v = Pop();
                            if (chunk.VarNames[idx].EndsWith('$') && !v.IsStr)
                                v = Str(v.N.ToString(CultureInfo.InvariantCulture));
                            if (!chunk.VarNames[idx].EndsWith('$') && v.IsStr && !chunk.VarNames[idx].StartsWith("~"))
                                throw new RuntimeError($"不能把字符串赋给数值变量 {chunk.VarNames[idx]}", line);
                            _vars[idx] = v;
                        }
                        break;
                    }
                    case OpCode.ArrLoad:
                    {
                        int idx = Read16(code, pc); pc += 2;
                        var idxSlot = Pop();
                        if (idxSlot.IsStr) throw new RuntimeError("数组下标必须是数值", line);
                        int i = (int)idxSlot.N;
                        var arr = GetArrayDesc(idx, line);
                        int k = arr.IsRange ? i - arr.Lo0 : i;
                        if (arr.IsStr)
                        {
                            if (k < 0 || k >= arr.StrData.Length) throw new RuntimeError($"数组下标越界: {i}", line);
                            Push(Str(arr.StrData[k] ?? ""));
                        }
                        else
                        {
                            if (k < 0 || k >= arr.Data.Length) throw new RuntimeError($"数组下标越界: {i}", line);
                            Push(arr.Data[k] == double.MaxValue ? Num(0) : Num(arr.Data[k]));
                        }
                        break;
                    }
                    case OpCode.ArrStore:
                    {
                        int idx = Read16(code, pc); pc += 2;
                        var val = Pop();
                        var idxSlot = Pop();
                        if (idxSlot.IsStr) throw new RuntimeError("数组下标必须是数值", line);
                        int i = (int)idxSlot.N;
                        var arr = GetArrayDesc(idx, line);
                        int k = arr.IsRange ? i - arr.Lo0 : i;
                        if (arr.IsStr)
                        {
                            if (!val.IsStr) throw new RuntimeError("类型不匹配：字符串数组需要字符串", line);
                            if (k < 0 || k >= arr.StrData.Length) throw new RuntimeError($"数组下标越界: {i}", line);
                            arr.StrData[k] = val.S;
                        }
                        else
                        {
                            if (val.IsStr) throw new RuntimeError("类型不匹配：数值数组需要数值", line);
                            if (k < 0 || k >= arr.Data.Length) throw new RuntimeError($"数组下标越界: {i}", line);
                            arr.Data[k] = val.N;
                        }
                        break;
                    }
                    case OpCode.Pop: Pop(); break;
                    case OpCode.Add:
                    {
                        var r = Pop(); var l = Pop();
                        if (l.IsStr || r.IsStr)
                        {
                            if (!l.IsStr) l = Str(l.N.ToString(CultureInfo.InvariantCulture));
                            if (!r.IsStr) r = Str(r.N.ToString(CultureInfo.InvariantCulture));
                            PushStr(l.S + r.S);
                        }
                        else PushNum(l.N + r.N);
                        break;
                    }
                    case OpCode.Sub: { var r = PopNum(); var l = PopNum(); PushNum(l.N - r.N); break; }
                    case OpCode.Mul: PushNum(PopNum().N * PopNum().N); break;
                    case OpCode.Div:
                    {
                        var r = PopNum(); var l = PopNum();
                        if (r.N == 0) throw new RuntimeError("除零错误", line);
                        PushNum(l.N / r.N);
                        break;
                    }
                    case OpCode.Idiv:
                    {
                        var r = PopNum(); var l = PopNum();
                        if (r.N == 0) throw new RuntimeError("除零错误", line);
                        PushNum(Math.Truncate(l.N / r.N));
                        break;
                    }
                    case OpCode.Mod:
                    {
                        var r = PopNum(); var l = PopNum();
                        if (r.N == 0) throw new RuntimeError("取模除零", line);
                        PushNum(l.N % r.N);
                        break;
                    }
                    case OpCode.Power: { var r = PopNum(); var l = PopNum(); PushNum(Math.Pow(l.N, r.N)); break; }
                    case OpCode.Neg: { var v = PopNum(); PushNum(-v.N); break; }
                    case OpCode.Concat: { var r = PopStr(); var l = PopStr(); PushStr(l.S + r.S); break; }
                    case OpCode.Eq: PushNum(BoolToNum(PopValue().EqualsValue(PopValue()))); break;
                    case OpCode.Ne: PushNum(BoolToNum(!PopValue().EqualsValue(PopValue()))); break;
                    // 关系比较：字符串按字典序，数值按大小（支持 CASE "0" TO "9" 这类字符串范围）
                    case OpCode.Lt: { var r = Pop(); var l = Pop(); PushNum(BoolToNum(l.CompareValue(r) < 0)); break; }
                    case OpCode.Le: { var r = Pop(); var l = Pop(); PushNum(BoolToNum(l.CompareValue(r) <= 0)); break; }
                    case OpCode.Gt: { var r = Pop(); var l = Pop(); PushNum(BoolToNum(l.CompareValue(r) > 0)); break; }
                    case OpCode.Ge: { var r = Pop(); var l = Pop(); PushNum(BoolToNum(l.CompareValue(r) >= 0)); break; }
                    case OpCode.And: { var r = PopNum(); var l = PopNum(); PushNum(BoolToNum(IsTrue(l.N) && IsTrue(r.N))); break; }
                    case OpCode.Or: { var r = PopNum(); var l = PopNum(); PushNum(BoolToNum(IsTrue(l.N) || IsTrue(r.N))); break; }
                    case OpCode.Not: { var v = PopNum(); PushNum(BoolToNum(!IsTrue(v.N))); break; }
                    case OpCode.Call:
                    {
                        int fIdx = Read16(code, pc); pc += 2;
                        int argc = Read16(code, pc); pc += 2;
                        string fn = chunk.FuncNames[fIdx];
                        var args = new Slot[argc];
                        for (int i = argc - 1; i >= 0; i--) args[i] = Pop();
                        Push(CallFunc(fn, args, line));
                        break;
                    }
                    case OpCode.Jump: { int target = Read16(code, pc); pc = target; break; }
                    case OpCode.JumpIfFalse: { int target = Read16(code, pc); pc += 2; if (!IsTrue(PopNum().N)) pc = target; break; }
                    case OpCode.JumpIfTrue: { int target = Read16(code, pc); pc += 2; if (IsTrue(PopNum().N)) pc = target; break; }
                    case OpCode.ForCheck:
                    {
                        int varIdx = Read16(code, pc);
                        int limitIdx = Read16(code, pc + 2);
                        int stepIdx = Read16(code, pc + 4);
                        pc += 6;
                        EnsureVars(Math.Max(varIdx, Math.Max(limitIdx, stepIdx)) + 1);
                        double step = _vars[stepIdx].N;
                        _vars[varIdx] = Num(_vars[varIdx].N + step);
                        bool cont = step >= 0 ? _vars[varIdx].N <= _vars[limitIdx].N : _vars[varIdx].N >= _vars[limitIdx].N;
                        PushNum(BoolToNum(cont));
                        break;
                    }
                    case OpCode.Input:
                    {
                        int idx = Read16(code, pc); pc += 2;
                        EnsureVars(idx + 1);
                        string? input = Keys?.ReadLine() ?? _input.ReadLine();
                        if (input == null) input = "";
                        var v = ParseInput(input);
                        if (chunk.VarNames[idx].EndsWith('$')) _vars[idx] = Str(v.Item2);
                        else _vars[idx] = Num(v.Item1);
                        break;
                    }
                    case OpCode.Print:
                    {
                        var v = Pop();
                        WriteText(Format(v)); Newline();
                        break;
                    }
                    case OpCode.PrintSemicolon:
                    {
                        var v = Pop();
                        WriteText(Format(v));
                        break;
                    }
                    case OpCode.PrintComma:
                    {
                        var v = Pop();
                        WriteText(Format(v));
                        WriteText("\t");
                        break;
                    }
                    case OpCode.PrintNewline:
                        Newline();
                        break;
                    case OpCode.PrintTab:
                    {
                        int col = Read16(code, pc); pc += 2;
                        // 跳到指定列
                        if (RenderMode && Gfx != null)
                        {
                            Gfx.Text.SetCursor(Gfx.Text.CurRow, col);
                        }
                        else
                        {
                            var buf = new string(' ', Math.Max(1, col));
                            WriteText(buf);
                        }
                        break;
                    }
                    case OpCode.Randomize:
                        _rnd = new Random();
                        break;
                    case OpCode.Restore:
                    {
                        int idx = Read16(code, pc); pc += 2;
                        _dataIdx = idx;
                        break;
                    }
                    case OpCode.Read:
                    {
                        int ridx = Read16(code, pc); pc += 2;
                        if (_dataIdx >= chunk.Data.Count)
                            throw new RuntimeError("DATA 数据不足（READ 越界）", line);
                        var item = chunk.Data[_dataIdx++];
                        EnsureVars(ridx + 1);
                        bool strVar = chunk.VarNames[ridx].EndsWith('$');
                        if (strVar)
                        {
                            _vars[ridx] = item.IsStr ? Str(item.Str) : Str(item.Num.ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            if (item.IsStr) throw new RuntimeError($"不能把字符串数据读入数值变量 {chunk.VarNames[ridx]}", line);
                            _vars[ridx] = Num(item.Num);
                        }
                        break;
                    }
                    case OpCode.DimArray2:
                    {
                        int aidx = Read16(code, pc); pc += 2;
                        var s2 = Pop(); var s1 = Pop();
                        if (s1.IsStr || s2.IsStr) throw new RuntimeError("数组尺寸必须是数值", line);
                        int m = (int)s1.N, n = (int)s2.N;
                        if (m <= 0) m = 1;
                        if (n <= 0) n = 1;
                        EnsureArray2(aidx, m, n, chunk.VarNames[aidx].EndsWith('$'));
                        break;
                    }
                    case OpCode.Arr2Load:
                    {
                        int aidx = Read16(code, pc); pc += 2;
                        var j = Pop(); var i = Pop();
                        if (j.IsStr || i.IsStr) throw new RuntimeError("数组下标必须是数值", line);
                        int ii = (int)i.N, jj = (int)j.N;
                        if (chunk.VarNames[aidx].EndsWith('$'))
                        {
                            var arr = GetStr2(aidx, line);
                            if (ii < 0 || ii >= arr.GetLength(0) || jj < 0 || jj >= arr.GetLength(1))
                                throw new RuntimeError($"数组下标越界: ({ii},{jj})", line);
                            Push(Str(arr[ii, jj] ?? ""));
                        }
                        else
                        {
                            var arr = GetNum2(aidx, line);
                            if (ii < 0 || ii >= arr.GetLength(0) || jj < 0 || jj >= arr.GetLength(1))
                                throw new RuntimeError($"数组下标越界: ({ii},{jj})", line);
                            double v = arr[ii, jj];
                            Push(v == double.MaxValue ? Num(0) : Num(v));
                        }
                        break;
                    }
                    case OpCode.Arr2Store:
                    {
                        int aidx = Read16(code, pc); pc += 2;
                        var val = Pop(); var j = Pop(); var i = Pop();
                        if (j.IsStr || i.IsStr) throw new RuntimeError("数组下标必须是数值", line);
                        int ii = (int)i.N, jj = (int)j.N;
                        if (chunk.VarNames[aidx].EndsWith('$'))
                        {
                            if (!val.IsStr) throw new RuntimeError("类型不匹配：字符串数组需要字符串", line);
                            var arr = GetStr2(aidx, line);
                            if (ii < 0 || ii >= arr.GetLength(0) || jj < 0 || jj >= arr.GetLength(1))
                                throw new RuntimeError($"数组下标越界: ({ii},{jj})", line);
                            arr[ii, jj] = val.S;
                        }
                        else
                        {
                            if (val.IsStr) throw new RuntimeError("类型不匹配：数值数组需要数值", line);
                            var arr = GetNum2(aidx, line);
                            if (ii < 0 || ii >= arr.GetLength(0) || jj < 0 || jj >= arr.GetLength(1))
                                throw new RuntimeError($"数组下标越界: ({ii},{jj})", line);
                            arr[ii, jj] = val.N;
                        }
                        break;
                    }
                    case OpCode.DimRange:
                    {
                        int aidx = Read16(code, pc); pc += 2;
                        var hi = Pop(); var lo = Pop();
                        if (hi.IsStr || lo.IsStr) throw new RuntimeError("数组尺寸必须是数值", line);
                        int loN = (int)lo.N, hiN = (int)hi.N;
                        if (loN > hiN) { int t = loN; loN = hiN; hiN = t; }
                        EnsureRangeArray(aidx, loN, hiN, chunk.VarNames[aidx].EndsWith('$'));
                        break;
                    }
                    case OpCode.Gosub:
                    {
                        int target = Read16(code, pc); pc += 2;
                        _callStack.Add(pc);
                        pc = target;
                        break;
                    }
                    case OpCode.Return:
                    {
                        if (_callStack.Count == 0) throw new RuntimeError("RETURN 无对应 GOSUB", line);
                        pc = _callStack[^1];
                        _callStack.RemoveAt(_callStack.Count - 1);
                        break;
                    }
                    case OpCode.CallRoutine:
                    {
                        int target = Read16(code, pc); pc += 2;
                        _routineStack.Add(pc);
                        pc = target;
                        break;
                    }
                    case OpCode.EndRoutine:
                    {
                        if (_routineStack.Count == 0) return; // 例程顶层结束
                        pc = _routineStack[^1];
                        _routineStack.RemoveAt(_routineStack.Count - 1);
                        break;
                    }
                    case OpCode.SetErrHandler:
                        _errHandler = Read16(code, pc); pc += 2;
                        break;
                    case OpCode.ClearErrHandler:
                        _errHandler = -1;
                        break;
                    case OpCode.Resume:
                    {
                        int mode = Read16(code, pc); pc += 2;
                        if (mode == 1) pc = _errResumePc; // RESUME NEXT
                        else pc = _errResumePc - (pc <= _errResumePc ? 0 : 0);
                        _errHandler = -1;
                        break;
                    }
                    case OpCode.LineInput:
                    {
                        int idx = Read16(code, pc); pc += 2;
                        EnsureVars(idx + 1);
                        string s = Keys?.ReadLine() ?? _input.ReadLine() ?? "";
                        _vars[idx] = Str(s);
                        break;
                    }
                    // ---- 图形 ----
                    case OpCode.GfxScreen:
                    {
                        var m = PopNum();
                        Gfx!.SetMode((int)m.N);
                        break;
                    }
                    case OpCode.GfxCls:
                        Gfx!.Cls();
                        PresentFrame();
                        break;
                    case OpCode.GfxLine:
                    {
                        int mode = Read16(code, pc); pc += 2;
                        var col = PopNum(); var y2 = PopNum(); var x2 = PopNum(); var y1 = PopNum(); var x1 = PopNum();
                        var px = Gfx!.Pixels;
                        int c = (int)col.N & 15;
                        if (mode == 2) px.FillRect((int)x1.N, (int)y1.N, (int)x2.N, (int)y2.N, c);
                        else px.Line((int)x1.N, (int)y1.N, (int)x2.N, (int)y2.N, c);
                        break;
                    }
                    case OpCode.GfxCircle:
                    {
                        var has = PopNum();
                        var aspect = PopNum(); var end = PopNum(); var start = PopNum(); var col = PopNum();
                        var r = PopNum(); var y = PopNum(); var x = PopNum();
                        Gfx!.Pixels.Circle((int)x.N, (int)y.N, r.N, (int)col.N & 15, start.N, end.N, has.N != 0, aspect.N);
                        break;
                    }
                    case OpCode.GfxPset:
                    {
                        var col = PopNum(); var y = PopNum(); var x = PopNum();
                        Gfx!.Pixels.Set((int)x.N, (int)y.N, (int)col.N & 15);
                        break;
                    }
                    case OpCode.GfxPaint:
                    {
                        var boundary = PopNum(); var col = PopNum(); var y = PopNum(); var x = PopNum();
                        Gfx!.Pixels.Flood((int)x.N, (int)y.N, (int)col.N & 15, (int)boundary.N & 15);
                        break;
                    }
                    case OpCode.GfxColor:
                    {
                        var bg = PopNum(); var fg = PopNum();
                        Gfx!.Text.Fg = (int)fg.N & 15; Gfx!.Text.Bg = (int)bg.N & 15;
                        break;
                    }
                    case OpCode.GfxPalette:
                    {
                        var v = PopNum(); var c = PopNum();
                        Gfx!.Palette.SetEga((int)c.N, (int)v.N);
                        break;
                    }
                    case OpCode.GfxLocate:
                    {
                        var col = PopNum(); var row = PopNum();
                        Gfx!.Text.SetCursor((int)row.N, (int)col.N);
                        break;
                    }
                    case OpCode.GfxGet:
                    {
                        int aidx = Read16(code, pc); pc += 2;
                        var y2 = PopNum(); var x2 = PopNum(); var y1 = PopNum(); var x1 = PopNum();
                        var data = Gfx!.Pixels.GetSprite((int)x1.N, (int)y1.N, (int)x2.N, (int)y2.N);
                        StoreSprite(aidx, data, line);
                        break;
                    }
                    case OpCode.GfxPut:
                    {
                        int aidx = Read16(code, pc); pc += 2;
                        int mode = Read16(code, pc); pc += 2;
                        var y = PopNum(); var x = PopNum();
                        var data = LoadSprite(aidx, line);
                        if (data != null)
                            Gfx!.Pixels.PutSprite((int)x.N, (int)y.N, data, mode == 1);
                        break;
                    }
                    case OpCode.GfxPlay:
                    {
                        var s = Pop();
                        // 音乐串解析后忽略（可发提示音）。保证不中断。
                        if (Gfx != null && s.IsStr && s.S.Length > 0) BeepSound();
                        break;
                    }
                    case OpCode.GfxSleep:
                    {
                        var s = PopNum();
                        PresentFrame();
                        System.Threading.Thread.Sleep((int)Math.Max(0, s.N * 1000));
                        break;
                    }
                    case OpCode.GfxWidth:
                    {
                        var w = PopNum();
                        int cols = (int)w.N;
                        if (Gfx != null) Gfx.Text.Resize(cols, Gfx.Text.Rows);
                        break;
                    }
                    case OpCode.Beep:
                        BeepSound();
                        break;
                    case OpCode.Halt:
                        FlushFrame();
                        return;
                    default: throw new RuntimeError($"未知操作码 {op}", line);
                }
                // 每帧呈现
                if (RenderMode && (op == OpCode.GfxPset || op == OpCode.GfxLine || op == OpCode.GfxCircle || op == OpCode.GfxPut || op == OpCode.GfxPaint))
                    PresentFrame();
            }
            catch (RuntimeError)
            {
                // ON ERROR 处理
                if (_errHandler >= 0 && pc > 0)
                {
                    _errResumePc = pc; // 出错指令之后的 pc（RESUME NEXT 用）
                    pc = _errHandler;
                    _errHandler = -1;
                    continue;
                }
                throw;
            }
        }
    }
    private void WriteText(string s)
    {
        if (RenderMode && Gfx != null)
        {
            foreach (var c in s) Gfx.Text.Write(c);
            PresentFrame();
        }
        else
        {
            _output.Print(s);
        }
    }

    private void Newline()
    {
        if (RenderMode && Gfx != null)
        {
            Gfx.Text.Write('\n');
            PresentFrame();
        }
        else
        {
            _output.Newline();
        }
    }

    private void PresentFrame()
    {
        if (RenderMode && Present != null && Gfx != null)
        {
            // 帧率限制：距上次呈现不足 33ms（约 30fps）则跳过，避免逐字符/逐像素全屏重绘导致疯狂渲染
            var now = DateTime.UtcNow;
            if ((now - _lastPresent).TotalMilliseconds < 33) return;
            _lastPresent = now;
            Present(Gfx);
        }
    }

    /// <summary>强制呈现当前帧（程序结束 HALT 时刷新最后一帧，不受帧率限制）。</summary>
    private void FlushFrame()
    {
        if (RenderMode && Present != null && Gfx != null)
        {
            _lastPresent = DateTime.MinValue;
            Present(Gfx);
        }
    }

    private static void BeepSound()
    {
        try
        {
#if !WINDOWS
            Console.Out.Write("\a");
            Console.Out.Flush();
#else
            Console.Beep(880, 30);
#endif
        } catch { }
    }

    // ---------- sprite 存储 ----------
    private void StoreSprite(int aidx, int[] data, int line)
    {
        EnsureArrays(aidx);
        var d = data;
        var arr = _arrays[aidx];
        if (arr.Data.Length < d.Length)
            arr.Data = new double[d.Length];
        for (int i = 0; i < d.Length; i++) arr.Data[i] = d[i];
        arr.IsStr = false;
        arr.Lo0 = 0; arr.Hi0 = d.Length - 1;
        arr.IsRange = false;
        _arrays[aidx] = arr;
    }

    private int[]? LoadSprite(int aidx, int line)
    {
        if (aidx >= _arrays.Count) return null;
        var arr = _arrays[aidx];
        if (arr.Data.Length < 2) return null;
        var res = new int[arr.Data.Length];
        for (int i = 0; i < arr.Data.Length; i++) res[i] = (int)arr.Data[i];
        return res;
    }

    private void EnsureArrays(int aidx)
    {
        while (_arrays.Count <= aidx) _arrays.Add(new ArrayDesc { Data = Array.Empty<double>(), StrData = Array.Empty<string>(), Lo0 = 0, Hi0 = 0 });
    }

    // ---------- 数组访问 ----------
    private ArrayDesc GetArrayDesc(int idx, int line)
    {
        EnsureArrays(idx);
        var arr = _arrays[idx];
        if (arr.Data.Length == 0 && arr.StrData.Length == 0)
            throw new RuntimeError($"数组未声明或未初始化: {_chunk!.VarNames[idx]}", line);
        return arr;
    }

    private void EnsureRangeArray(int idx, int lo, int hi, bool isStr)
    {
        EnsureArrays(idx);
        int size = hi - lo + 1;
        if (size < 0) size = 0;
        var arr = _arrays[idx];
        if (isStr)
        {
            if (arr.StrData.Length < size)
            {
                arr.StrData = new string[size];
                for (int i = 0; i < size; i++) arr.StrData[i] = "";
                arr.Data = Array.Empty<double>();
            }
        }
        else
        {
            if (arr.Data.Length < size)
            {
                arr.Data = new double[size];
                for (int i = 0; i < size; i++) arr.Data[i] = double.MaxValue;
                arr.StrData = Array.Empty<string>();
            }
        }
        arr.Lo0 = lo; arr.Hi0 = hi;
        arr.IsStr = isStr; arr.IsRange = true;
        _arrays[idx] = arr;
    }

    private void EnsureArray(int idx, int size, bool isStr)
    {
        EnsureArrays(idx);
        if (size <= 0) size = 10;
        var arr = _arrays[idx];
        if (isStr)
        {
            if (arr.StrData.Length < size)
            {
                arr.StrData = new string[size];
                for (int i = 0; i < size; i++) arr.StrData[i] = "";
                arr.Data = Array.Empty<double>();
            }
        }
        else
        {
            if (arr.Data.Length < size)
            {
                arr.Data = new double[size];
                for (int i = 0; i < size; i++) arr.Data[i] = double.MaxValue;
                arr.StrData = Array.Empty<string>();
            }
        }
        arr.Lo0 = 0; arr.Hi0 = size - 1;
        arr.IsStr = isStr; arr.IsRange = false;
        _arrays[idx] = arr;
    }

    private double[] GetArray(int idx, int line)
    {
        var arr = GetArrayDesc(idx, line);
        return arr.Data;
    }

    private string[] GetStrArray(int idx, int line)
    {
        var arr = GetArrayDesc(idx, line);
        return arr.StrData;
    }

    private void EnsureArray2(int idx, int m, int n, bool isStr)
    {
        if (isStr)
        {
            while (_strArrays2.Count <= idx) _strArrays2.Add(null);
            if (_strArrays2[idx] == null || _strArrays2[idx]!.GetLength(0) < m || _strArrays2[idx]!.GetLength(1) < n)
            {
                var arr = new string[m, n];
                for (int a = 0; a < m; a++) for (int b = 0; b < n; b++) arr[a, b] = "";
                _strArrays2[idx] = arr;
            }
        }
        else
        {
            while (_arrays2.Count <= idx) _arrays2.Add(null);
            if (_arrays2[idx] == null || _arrays2[idx]!.GetLength(0) < m || _arrays2[idx]!.GetLength(1) < n)
            {
                var arr = new double[m, n];
                for (int a = 0; a < m; a++) for (int b = 0; b < n; b++) arr[a, b] = double.MaxValue;
                _arrays2[idx] = arr;
            }
        }
    }

    private double[,] GetNum2(int idx, int line)
    {
        if (idx >= _arrays2.Count || _arrays2[idx] == null)
            throw new RuntimeError($"二维数组未声明或未初始化: {_chunk!.VarNames[idx]}", line);
        return _arrays2[idx]!;
    }

    private string[,] GetStr2(int idx, int line)
    {
        if (idx >= _strArrays2.Count || _strArrays2[idx] == null)
            throw new RuntimeError($"二维数组未声明或未初始化: {_chunk!.VarNames[idx]}", line);
        return _strArrays2[idx]!;
    }

    private void EnsureVars(int count)
    {
        while (_vars.Count < count) _vars.Add(Num(0));
    }

    // ---------- 内置函数 ----------
    private static string CharOf(Slot s) =>
        s.IsStr ? (s.S.Length > 0 ? s.S.Substring(0, 1) : "") : ((char)(int)s.N).ToString();

    private static (double, string) ParseInput(string s)
    {
        s = s.Trim();
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            return (d, s);
        return (0, s);
    }

    private Slot CallFunc(string name, Slot[] args, int line)
    {
        switch (name)
        {
            case "INT": RequireNum(args, 1, line); return Num(Math.Floor(args[0].N));
            case "SQR": RequireNum(args, 1, line); if (args[0].N < 0) throw new RuntimeError("SQR 负数", line); return Num(Math.Sqrt(args[0].N));
            case "ABS": RequireNum(args, 1, line); return Num(Math.Abs(args[0].N));
            case "SIN": RequireNum(args, 1, line); return Num(Math.Sin(args[0].N));
            case "COS": RequireNum(args, 1, line); return Num(Math.Cos(args[0].N));
            case "TAN": RequireNum(args, 1, line); return Num(Math.Tan(args[0].N));
            case "ATN": RequireNum(args, 1, line); return Num(Math.Atan(args[0].N));
            case "CINT": RequireNum(args, 1, line); return Num(Math.Round(args[0].N, MidpointRounding.ToEven));
            case "CDBL": case "CSNG": RequireNum(args, 1, line); return Num(args[0].N);
            case "TIMER": return Num(GetTimer());
            case "PEEK": return Num(0); // 硬件内存读取：非 DOS 环境返回 0（NumLock 等状态无关紧要）
            case "INKEY$": return Str(Keys?.ReadKey() ?? "");
            case "POINT":
            {
                RequireNum(args, 2, line);
                if (Gfx == null) return Num(0);
                return Num(Gfx.Pixels.Get((int)args[0].N, (int)args[1].N));
            }
            case "LEN": RequireStr(args, 1, line); return Num(args[0].S.Length);
            case "CHR$": RequireNum(args, 1, line); return Str(((char)(int)args[0].N).ToString());
            case "VAL": RequireStr(args, 1, line); return Num(double.TryParse(args[0].S.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0);
            case "STR$": RequireNum(args, 1, line); return Str(args[0].N.ToString(CultureInfo.InvariantCulture));
            case "MID$": RequireStrNum(args, 0, 2, line); { int st = (int)args[1].N - 1; int len = (int)args[2].N; if (st < 0) st = 0; if (st >= args[0].S.Length) return Str(""); int take = Math.Min(len, args[0].S.Length - st); return Str(args[0].S.Substring(st, take)); }
            case "LEFT$": RequireStrNum(args, 0, 1, line); { int n = (int)args[1].N; if (n < 0) n = 0; if (n > args[0].S.Length) n = args[0].S.Length; return Str(args[0].S.Substring(0, n)); }
            case "RIGHT$": RequireStrNum(args, 0, 1, line); { int n = (int)args[1].N; if (n < 0) n = 0; if (n > args[0].S.Length) n = args[0].S.Length; return Str(args[0].S.Substring(args[0].S.Length - n)); }
            case "RND": { for (int i = 0; i < args.Length; i++) if (args[i].IsStr) throw new RuntimeError("函数参数类型错误", line); return Num(_rnd.NextDouble()); }
            case "INSTR":
            {
                if (args.Length < 2 || args.Length > 3) throw new RuntimeError("函数参数个数不足", line);
                int start = 1;
                string hay, needle;
                if (args.Length == 3)
                {
                    if (args[0].IsStr || !args[1].IsStr || !args[2].IsStr) throw new RuntimeError("函数参数类型错误", line);
                    start = (int)args[0].N; hay = args[1].S; needle = args[2].S;
                }
                else
                {
                    if (!args[0].IsStr || !args[1].IsStr) throw new RuntimeError("函数参数类型错误", line);
                    hay = args[0].S; needle = args[1].S;
                }
                if (start < 1) start = 1;
                if (start > hay.Length + 1) return Num(0);
                int idx = hay.IndexOf(needle, start - 1, StringComparison.Ordinal);
                return Num(idx < 0 ? 0 : idx + 1);
            }
            case "UCASE$": RequireStr(args, 1, line); return Str(args[0].S.ToUpperInvariant());
            case "LCASE$": RequireStr(args, 1, line); return Str(args[0].S.ToLowerInvariant());
            case "STRING$": RequireNum(args, 1, line); { int n = (int)args[0].N; if (n < 0) n = 0; return Str(new string(CharOf(args[1])[0], n)); }
            case "SPACE$": RequireNum(args, 1, line); { int n = (int)args[0].N; if (n < 0) n = 0; return Str(new string(' ', n)); }
            case "LTRIM$": RequireStr(args, 1, line); return Str(args[0].S.TrimStart());
            case "RTRIM$": RequireStr(args, 1, line); return Str(args[0].S.TrimEnd());
            default: throw new RuntimeError($"未知函数 {name}", line);
        }
    }

    private static double GetTimer()
    {
        return (DateTime.Now - DateTime.UnixEpoch).TotalSeconds;
    }

    private static void RequireNum(Slot[] a, int n, int line)
    {
        for (int i = 0; i < n; i++) if (a[i].IsStr) throw new RuntimeError("函数参数类型错误", line);
    }
    private static void RequireStr(Slot[] a, int n, int line)
    {
        if (n > a.Length) throw new RuntimeError("函数参数个数不足", line);
        for (int i = 0; i < n; i++) if (!a[i].IsStr) throw new RuntimeError("函数参数类型错误", line);
    }

    private static void RequireStrNum(Slot[] a, int strIdx, int numCount, int line)
    {
        if (numCount + strIdx > a.Length) throw new RuntimeError("函数参数个数不足", line);
        if (!a[strIdx].IsStr) throw new RuntimeError("函数参数类型错误", line);
        for (int i = 1; i <= numCount; i++) if (a[strIdx + i].IsStr) throw new RuntimeError("函数参数类型错误", line);
    }

    // ---------- 栈操作 ----------
    private void Push(Slot s) => _stack.Add(s);
    private void PushNum(double d) => _stack.Add(Num(d));
    private void PushStr(string s) => _stack.Add(Str(s));
    private Slot Pop() { var s = _stack[^1]; _stack.RemoveAt(_stack.Count - 1); return s; }
    private Slot PopNum() { var s = Pop(); if (s.IsStr) throw new RuntimeError("类型不匹配：需要数值", _chunk!.Lines[0]); return s; }
    private Slot PopStr() { var s = Pop(); if (!s.IsStr) throw new RuntimeError("类型不匹配：需要字符串", _chunk!.Lines[0]); return s; }
    private Slot PopValue() => Pop();

    private static Slot Num(double d) => new() { N = d, IsStr = false };
    private static Slot Str(string s) => new() { S = s, IsStr = true };

    private static double BoolToNum(bool b) => b ? 1 : 0;
    private static bool IsTrue(double d) => d != 0;
    private static string Format(Slot s) => s.IsStr ? s.S : s.N.ToString(CultureInfo.InvariantCulture);

    private static int Read16(List<byte> code, int pc) => code[pc] | (code[pc + 1] << 8);
}

internal static class SlotExt
{
    public static bool EqualsValue(this Slot a, Slot b) => a.IsStr && b.IsStr ? a.S == b.S : a.N == b.N;
    /// <summary>关系比较：均为字符串按字典序，否则按数值。</summary>
    public static int CompareValue(this Slot a, Slot b) =>
        a.IsStr && b.IsStr ? string.CompareOrdinal(a.S, b.S) : a.N.CompareTo(b.N);
}
