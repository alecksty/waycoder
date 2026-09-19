// VMB 编解码**穷举**往返检：每个 opcode × 每一种操作数编码形式，
// 装进一个 VmlProgram → ToVmbBytes → FromVmbBytes → 逐条比对。
//
// 为什么不留着"拿个真程序跑一遍"那种抽查：真程序只覆盖它用到的那些编码，
// 而这次抓到的四处不对称里有两处（偏移 ≥128、负偏移符号位）恰好是抽查漏网的。
using VMLAssembler;

var prog = new VmlProgram(new List<Instruction>(), new Dictionary<string, int>(),
                          new Dictionary<string, object>(), new Dictionary<string, object>());
prog.EntryPoint = "main";

// ── ① 每一个 opcode 都过一遍（用最简操作数，只验证 opcode 字节本身）──
// ⚠ **伪指令不写进 VMB**（`WriteCodeSection` 会过滤掉），所以穷举时也要跳过 ——
//    否则"读回来少了 6 条"会被误判成 bug（第一版就是这么误报的）。
var pseudo = new HashSet<OpCode> { OpCode.LABEL, OpCode.BREAK, OpCode.DUMP, OpCode.TRACE,
                                  OpCode.ASM, OpCode.CHIPASM };
var opcodes = Enum.GetValues<OpCode>().Where(o => !pseudo.Contains(o));
foreach (var op in opcodes)
    prog.Instructions.Add(new Instruction(op, new List<Operand>()));

// ── ② 每一种操作数编码形式 × 每个 opcode 的常见形态 ──
var operands = new List<Operand>();
foreach (var r in new[] { 0, 1, 12, 15 })
    operands.Add(new Operand(OperandType.REGISTER, r));
foreach (var v in new[] { 0, 1, -1, 127, -128, 128, -129, 32767, -32768, 32768,
                          int.MaxValue, int.MinValue })
    operands.Add(new Operand(OperandType.IMMEDIATE, v));
// 内存：绝对 / 寄存器相对（**含偏移 ≥128 那一档**，正是之前出错的地方）/ 标签
foreach (var v in new[] { 0, 127, 128, 200, 65535 })
    operands.Add(new Operand(OperandType.MEMORY, v));
foreach (var s in new[] { "R0+0", "R12+8", "R12-4", "R12+127", "R12+128", "R12+200",
                          "R12-128", "R15-32768" })
    operands.Add(new Operand(OperandType.MEMORY, s));
operands.Add(new Operand(OperandType.LABEL, "someLabel"));
operands.Add(new Operand(OperandType.INDIRECT, 0));
operands.Add(new Operand(OperandType.INDIRECT, 14));
// ⚠ 没有 `"@R1"` 这种输入形态：汇编器产出的间接寻址是**整数寄存器号**
//    （`@1` → Operand(INDIRECT, 1)），字符串形态只认 `[R14-8]` / `[R14]`。
//    塞一个 `"@R1"` 进去是测试自己造了个不存在的输入（写侧会当成寄存器 0）——
//    第一版就是这么误报出 6 处"差异"的。

// 每个操作数都挂到 MOVE / MOVEB / PUSH / JMP 这几种不同形态上各来一遍
foreach (var op in new[] { OpCode.MOVE, OpCode.MOVEB, OpCode.PUSH, OpCode.ADD, OpCode.JMP, OpCode.CALL })
    foreach (var o in operands)
        prog.Instructions.Add(new Instruction(op, new List<Operand> { o }));

// ── ③ 数据段：每一种类型（含 long 与数组 —— 这两处之前直接抛）──
prog.DataSection["dInt"] = 42;
prog.DataSection["dNeg"] = -7;
prog.DataSection["dLong"] = 1234567890123L;
prog.DataSection["dFloat"] = 1.5f;
prog.DataSection["dDouble"] = 2.25d;
prog.DataSection["dStr"] = "hello";
prog.DataSection["dArr"] = new List<object> { 1, 2, 3 };
prog.DataSection["dArrLong"] = new List<object> { 1L, 2L };
prog.DataSection["dArrMix"] = new List<object> { 1, 1.5f, 2.5d };

var before = prog.Instructions.Select(i => i.ToString()).ToList();
byte[] bytes;
try { bytes = prog.ToVmbBytes(); }
catch (Exception ex) { Console.WriteLine($"✘ ToVmbBytes 抛：{ex.GetType().Name}: {ex.Message}"); return 1; }

VmlProgram back;
try { back = VmlProgram.FromVmbBytes(bytes); }
catch (Exception ex) { Console.WriteLine($"✘ FromVmbBytes 抛：{ex.GetType().Name}: {ex.Message}"); return 1; }

var after = back.Instructions.Select(i => i.ToString()).ToList();
Console.WriteLine($"指令 {before.Count} 条 → {bytes.Length} 字节 → 读回 {after.Count} 条");

var bad = 0;
for (var i = 0; i < Math.Min(before.Count, after.Count); i++)
{
    if (before[i] == after[i]) continue;
    // `@N` / `@RN`（间接）与 `R{N}+0`（寄存器相对）**编码完全相同**，只是两种写法
    static string Norm(string s) => System.Text.RegularExpressions.Regex
        .Replace(s.Replace(" ", "").Replace("[", "").Replace("]", ""), @"@R?(\d+)", "R$1+0");
    if (Norm(before[i]) == Norm(after[i])) continue;
    if (bad < 8) Console.WriteLine($"  ✘ 第 {i} 条：原「{before[i]}」→ 读回「{after[i]}」");
    bad++;
}
if (before.Count != after.Count) { Console.WriteLine($"  ✘ 条数不同"); bad++; }

// 数据段也逐项比（类型与值都要对）
foreach (var kv in prog.DataSection)
{
    if (!back.DataSection.TryGetValue(kv.Key, out var got)) { Console.WriteLine($"  ✘ 数据段缺 {kv.Key}"); bad++; continue; }
    // ⚠ `long` 走的是 double 那一路编码（tag 0x10 + 8 字节小端）：装载侧对 tag 0x10 是
    //    **逐字节搬运、不按数值语义解释**，所以位模式原样保留、等价 —— 但 `FromVmbBytes`
    //    返回的字典里装的是 double，`ToString()` 看着天差地别（1234567890123 → 6.1E-312）。
    //    判据因此要**比字节**，不是比数值。
    static IEnumerable<byte> Bits(object v) => v switch
    {
        long l => BitConverter.GetBytes(l),
        double d => BitConverter.GetBytes(d),
        int i => BitConverter.GetBytes(i),
        float f => BitConverter.GetBytes(f),
        _ => System.Text.Encoding.UTF8.GetBytes(v.ToString() ?? ""),
    };
    static IEnumerable<byte> ListBits(object v)
    {
        if (v is not System.Collections.IList l) return Bits(v);
        var all = new List<byte>();
        foreach (var e in l) all.AddRange(Bits(e));
        return all;
    }
    var wantBits = kv.Value is System.Collections.IList ? ListBits(kv.Value).ToArray() : Bits(kv.Value).ToArray();
    var gotBits = got is System.Collections.IList ? ListBits(got).ToArray() : Bits(got).ToArray();
    if (!wantBits.SequenceEqual(gotBits))
    {
        Console.WriteLine($"  ✘ 数据段 {kv.Key}：原 [{string.Join(",", wantBits)}] → 读回 [{string.Join(",", gotBits)}]");
        bad++;
    }
}
Console.WriteLine(bad == 0 ? "✔ 全量往返一致" : $"✘ 共 {bad} 处不同");
return bad == 0 ? 0 : 1;
