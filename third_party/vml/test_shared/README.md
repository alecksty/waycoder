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

## 跑法

```bash
third_party/vml/test_shared/run.sh            # 全部
third_party/vml/test_shared/run.sh strlen     # 挑一个
```

底层用 `scripts/vmlcli`（与手机端等价的编译+运行流水线）。

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
