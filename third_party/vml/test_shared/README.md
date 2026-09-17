# test_shared —— 共享库 C 函数单元测试

## 这个目录是干什么的

给 `Lib/shared/src/*.c` 里的**每一个导出函数**写一个验证用例，逐个检查正确性。

- **规模**：`genlib -s` 报 **1013 个导出函数 / 76 个文件**（数字会随库增长，以 `--scan` 为准）。
- **为什么值得做**：本次修 `printf` 时，一个 static 助手 `_printf_itoa` 的调用被链接器
  后缀启发式**误重定向**到 `convert.c` 的 `itoa`（参数顺序相反），导致 **`printf` 的
  所有 `%` 转换静默失效、一个字都不输出**，而整个仓库**没有一个用例会红** ——
  从 vML 库存在至今没人发现。这类"函数能编、能跑、结果错、不报错"的缺陷，
  只有**逐函数对着算**才抓得到。

## 办法

每个函数一个 `.c`，放在本目录下（`test_shared/<函数名或模块名>.c`），
**判据是"自己算一遍，再和库的返回比"**，不靠肉眼看输出：

```c
// test_shared/strlen.c
int main() {
    // 期望值在**测试里自己算**，不从被测函数里推导（否则等于自己证明自己）
    if (strlen("") != 0)      { print_str("FAIL strlen empty\n"); return 1; }
    if (strlen("abc") != 3)   { print_str("FAIL strlen abc\n"); return 1; }
    print_str("PASS strlen\n");
    return 0;
}
```

**不经过 stdio 做判据**（本仓踩过：C 的 `puts` 在字面量多的程序里会串行/重复，
把 stdio 的毛病看成 codegen 的毛病）。判据值用 `print_int` / `print_str`（`builtins` 模块）
或直接 `return` 退出码。

## ⚠ 没有返回值的函数怎么判 —— 「没有返回值」≠「无从观测」

判据不是「有没有返回值」，而是**「有没有可观测的副作用」**。按副作用写在哪，实测分布：

| 类 | 数量 | 占比 | 怎么断言 |
|---|---|---|---|
| **A 有返回值** | 638 | **66%** | 直接比返回值 |
| **B void + 指针形参** | 178 | **18%** | **从被写穿的内存断言** —— `sprintf`/`strcpy`/`memcpy`/`memset` 全在这类：缓冲区就是那个"返回值"，逐字节比 |
| **C void + 往标准输出写** | 32 | 3% | 断言**程序自己的 stdout 字节**（`printf`/`puts`/`putchar`/`newline`）—— 本目录的 runner / `vml-out-probe` 本来就是逐字节比对 |
| D void + 无指针形参 | 111 | 11% | 见下（**其中大半还能救**） |

⇒ **89% 是可以真正判好坏的**，不需要靠返回值。

**D 类不是"没有副作用"，是"副作用写到设备上去了"**，各自有观测通道：

- `graph.*` / `graphics.*`（BGI 绘图）、`browser_gfx.*` → **场景图元可数**（`VmlScene`）
  或宿主截屏**逐像素比对**（CLAUDE.md ⑲ 那次「真机图元体检」就是这套）
- `crt.CRT_*`（DOS 控制台）→ 同上，或断言写出去的字符序列
- `gpio.__gpio_*` / 设备类 → 宿主侧探针
- `console.newline` / `console.clear_screen` → 其实属于 **C 类**（往 stdout 写）

**真·冒烟（`SMOKE`）只证明三件事：没崩、没挂、没超时。** 用例报 `SMOKE` 时
**不会被计成 PASS** —— runner 单列一档并在汇总里写明，**不许拿"没崩"冒充"正确"**。

## 跑法

```bash
third_party/vml/test_shared/run.sh            # 全部
third_party/vml/test_shared/run.sh strlen     # 挑一个
```

底层用 `scripts/vmlcli`（与手机端等价的编译+运行流水线）。

用例的三种结论：`PASS`（断言全过）/ `SKIP <理由>`（明确不测，如浮点路径当前不可用）/
`SMOKE`（只到"没崩"这一层）。

## 优先级建议

按"出错代价 × 被复用面"排：

1. **格式化**（`printf` / `sprintf` / `vsnprintf` / `snprintf`）：本次出事的地方，
   且是**所有 22 门语言**的公共出口
2. **字符串**（`string.c` / `ustring.c` / `wstring.c`）：`strlen`/`strcpy`/`strcmp`/`memcpy`…
3. **转换**（`convert.c` / `conv.c` / `convert64.c`）：`atoi`/`itoa`/`atoI64`…
   —— 本次误重定向的受害者就在这一族
4. **数学**（`math.c` / `fixed.c` / `math64.c`）：注意浮点路径目前不可用
   （`(int)(cos(1.0)*1000.0)` 得 INT_MAX），整数部分先测
5. **内存 / 数组 / 位运算**（`memory.c` / `array.c` / `bitops.c`）
6. 其余按 `--scan` 清单补齐

## 已知的坑（写用例时先看一眼）

- **浮点基本不可用**：`cos/sin/exp` 经 `(int)` 转换得到 `INT_MAX`/`0`/`INT_MIN`。
  浮点类函数先只测整数性质，或标 `SKIP` 并写明原因。
- **名字含短名的函数要重点测**：`_printf_itoa`（含 `itoa`）就是被后缀匹配误伤的。
  凡是名字里含另一个函数名的，都值得一个"调用是否真的进到自己"的用例。
- **`Lib/` 是同步目录**（`sync.sh` 用 rsync `--delete`）⇒ 用例**不要放在 `Lib/` 里面**，
  放本目录；被测的库改动则必须配补丁（见 `third_party/vml/patches/`）。
