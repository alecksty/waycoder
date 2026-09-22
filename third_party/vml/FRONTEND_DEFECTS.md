# 前端编译器缺陷台账

写例子程序时一条条踩出来的问题，攒在这里统一修。

**每条都写清三件事**：现象 / **最小复现** / **判据**（怎么确认它真的坏了，而不是我代码写错）。
判据最重要——"我以为的边界"和"真的边界"差的往往就是这些。

状态图例：🔴 未修 · 🟡 已修（注明在哪个补丁/版本）· ⚪ 限制（不是 bug，是没做）

---

## C

### 🟡 取**形参**的地址得到的是裸 `R12`（**已缩小并修复**）

**发现**：v0.96.252 写 `Examples/c/calc.c` 时。当时报的是
`内存错误(PC=000002AB): MOVE @1, R0 — 地址=FFFFFFF1`，只观察到现象、没缩小，
台账里记的是「可能是"通过指针形参写回"」等四种猜测 —— **那四条猜测全都不对**。

**缩小到的最小复现**（2026-09-19 定案）：
```c
void par(int v)
{
    int* p = &v;          /* ← 取形参地址 */
    printf("PAR-RD=%d\n", *p);   /* 实测 65528，应为 42 */
    *p = 123;
    printf("PAR-WR=%d\n", v);    /* 实测 42，写回一个字都没生效 */
}
```
对照：`int a; int* q = &a;`（取**局部**地址）、`&g`（取全局）、
`void wrp(int* p){ *p = 77; }` 配 `wrp(&x)`（通过指针形参写回）**全都是好的**。
所以「指针写回一律坏」是错的，**坏的只有「取形参地址」这一个操作**。

**真身**：C 前端里「取地址」有**两份实现** ——
`GenerateUnaryOp` 的 `case "&"` 里手写了一份，`GenerateAddressOf` 是另一份。
手写那份算的是 `offset = stackFrameSize - variables[name]` 再 `SUB R0, offset`，
而 **`stackFrameSize` 只被赋过一次 0、再没更新过**（编译器自己会报 CS0414）：
- 局部变量偏移是**负**的（`R12-8`）⇒ `0 - (-8) = 8 > 0` ⇒ SUB 出 `R12-8`，**碰巧对**；
- 形参偏移是**正**的（`R12+12`）⇒ `0 - 12 = -12`，那个 `if (offset > 0)` 不成立
  ⇒ **一句不加，`&形参` 就是裸 `R12`**。

`GenerateAddressOf` 用的 `FormatVarOffset` 是照**带符号偏移**来的
（`offset >= 0 ? R12+offset : R12-offset`），本来就对 ——
**同一规则两处实现、只对了一半**，正是本仓反复踩的那一类。

**已修**（v0.96.258）：`case "&"` 整个改成 `GenerateAddressOf(unaryOp.Operand)`（收敛成一份），
并删掉那个从来没被赋过值的 `stackFrameSize`。
**判据**：`scripts/vml-abi-probe/probes/p7_param_addr.c` ——
读形参地址、写回形参地址、以及「取局部地址」「取全局地址」三组一起钉。
**反证过**：把旧实现放回去，这条立刻报 `ABI-FAIL 取形参地址读回不对` 并挂死（不响的自测比没有更糟）。

**绕过**：取形参地址改成先拷进局部变量再取（`int t = v; int* p = &t;`）。

---

### 🟡 `printf` / `sprintf` 的 `%` 转换产出零个字符（**已修**）

**现象**（v0.96.252 时）：`sprintf(b, "d=[%d]", 42)` 打出 `d=[` 之后就崩在 `MOVEB R0, @0`。
**2026-09-19 复测的现象已经变了**：栈漂移那一半（见下面「全局」节）随 `Lib/` 重生成好了，
剩下的是**变参表读偏**：
```c
sprintf(b, "x");            /* → "x"        （对） */
sprintf(b, "ab%dcd", 9);    /* → "ab" + 地址的十进制 + "cd"  ✗ */
sprintf(b, "%s", "abc");    /* → "%s"       ✗  %s 打出了格式串自己 */
printf("%d\n", 42);         /* → 42         （对，纯属巧合，见下） */
```

**真身**：`Lib/shared/src/printf.c` 里取变参表写的是 `int *stack_args = (int*)(&fmt + 4);`。
`&fmt` 是 `const char**`，`+4` 会按 **4 字节缩放成 +16 字节** —— 那是 `fmt` 之后的**第 4 个**槽。
- `printf(const char* fmt, ...)` 只有**一个**形参，变参紧跟其后 ⇒ `R12+16` **碰巧就是第一个变参**，所以它一直是对的；
- `sprintf(char* buf, const char* fmt, ...)` 多一个 `buf` ⇒ 整个读偏一格，
  `args[0]` 读到的是 **`fmt` 自己** ⇒ `%s` 把格式串原样打出来、`%d` 打出地址。

**修法（两处一起）**：
1. C 前端「取形参地址」的修复（见上一条）—— 不修它 `&fmt` 本身还是裸 `R12`；
2. `printf.c` 改成 `(int*)&fmt + 1`：按 `int` 步长加一格，**与形参个数无关**。

**判据**：`.scratch` 期的四行对照（`x` / `ab%dcd` / `%s` / `printf`）现在**四行全对**；
`scripts/vml-out-probe` 的 `nat.c`（C 自己的标准输出那条路）也在 28/28 里。

**绕过**（若将来又坏）：写数字自己转字符串（见 `plane.c` 的 `itoa_`）。

---

### 🟡 复合字面量 `(int[]){…}` 解析认得、却不产出地址，**而且不报错**

**现象**：编出来的代码把上一个寄存器（正好是"点数"）当指针推下去，宿主去地址 3 读坐标、越界。

**判据**：把汇编打出来一眼可辨 —— 具名数组是 `move R0 R12 / sub R0 #28 / push R0`，
复合字面量是 `move R0 #3 / push R0`。

**已修**（patch 0008）：解析到该分支直接 `Error(...)`，并且把顶层的容错恢复 catch 里的
`Parser_UnexpectedToken` 约定为**不可恢复** —— 只加一句报错是不够的，那个 catch 会把异常
吞成一行 `[RECOVER]` 日志继续编，用户拿到"能跑但少一段"的程序，等于没报。

**绕过**：顶点用**具名全局数组**（`plane.c` / `starfall.c` 都是这么写的）。

---

### 🟡 全局 `char` / `short` 数组的元素访问退化成 32 位

**现象**：`InferExpressionType(ArrayAccess)` 只查 `variableTypes`（只装**局部**变量）⇒
全局 `char`/`short` 数组查不到就退化成 `ExprType.Int` ⇒ 元素访问走 32 位 `MOVE`
（该用 `MOVEB`/`MOVEH`），于是"长度对、内容不对"，**写还会越界**。

**已修**（patch 0004）。⚠ **局部数组一直是好的**，这个坑只在全局数组上冒头。

---

### 🟡 初始化器里的负数被静默编成 0

**现象**：
```c
int A[4] = {  0,  1,  0, -1 };   /* 程序实际读到  0  1  0  0 */
```
真身是兜底分支吃掉了整类节点：摊平初始化器的两条路只认
`NumberLiteral`/`CharLiteral`/`StringLiteral`/`Identifier`，**其余 `result.Add(0)`**；
而 `-3` 根本不是 `NumberLiteral`，是 `UnaryOp("-", NumberLiteral(3))` ⇒ **负数整类变 0**。

**影响面**：方向表首当其冲（`pacman.c` 的 `int DX[4] = {0,1,0,-1}` 变成 `{0,1,0,0}`
⇒ 只剩「右」「下」两个方向存在）。

**已修**：新增 `ConstFold`（一元/二元常量折叠），两处摊平函数接上。
整数运算走 `unchecked`（= C 的 int 回绕语义），与 `Parser.TryConstInt` 刻意不同
（那个用于数组维度必须 `checked` 报溢出）—— **两处需求相反，是两份实现、不是重复**。

---

### ⚪ 限制：`#define` 不支持反斜杠续行

多行宏要写成一行。**不是 bug，是没做**（写例子时按单行写即可）。

### ⚪ 限制：`${}` 只认局部变量

⇒ 调 syscall 一律走 `waycoder_ui.h` 的包装函数，别自己写内联汇编。

---

### 🔴 `float[]` / `double[]` 数组元素的**存**到不了内存（2026-09-20 实测）

```c
float  fv[8];   fv[1] = 7.0;      /* 之后那块内存里是 0（循环里填也一样） */
double dv[4];   dv[1] = 2.5;      /* 同上 */
```

**标量完全正常**（`float x; x = 7.0;` 读回来就是 7）——所以这不是"浮点坏了"，
而是**浮点往内存里写的那个形态**坏了。
判据（不经过 printf，直接看内存）：用一个 `int` 数组当"内存窗口"，
把目标数组的地址（从一个 `float*` 形参上取 `(int)v`）交给宿主 dump 出来 ——
`fv[1] = 7.0` 之后 32 字节里全是 0，而 7.0f 的位模式应当是 `0x40E00000`。

**影响面**：任何"把浮点存进数组再读回来"的写法（游戏里的坐标表、物理参数表…）。

**绕过**（`Examples/c/callports.c` 就是这么写的）：用一个 `int` 数组**手工摆位模式**，
再把它的地址当 `float*` / `double*` 传 —— `int` 数组的读写是好的。

### 🔴 全局 `long` / `double` 标量按 32 位存（截断）

```c
long g;   g = 3000000000;   /* 读回来是 2147483647（INT_MAX 截断） */
double gd; gd = 2.5;        /* 读回来是 2 */
```

**局部变量正常**（栈上的是 64 位）——只有**全局**的 64 位标量被当成 int 存。

### 🔴 同一个 `long[]` 连续存多个元素会丢

```c
long lv[4];
lv[0] = 3; lv[1] = 1; lv[2] = 2; lv[3] = 3;
/* 内存里只有 lv[0] 与 lv[1] 落了地，lv[2] / lv[3] 是 0 */
```

两条落地、后两条丢 —— 与"浮点数组存不进去"是同一族（往内存写 64 位值），
但表现不同（这个至少前两条是对的）。

### 🟡 `//` 出现在**宏体**里就被当成行注释 —— 字符串字面量也照吃不误（**已修**）

**发现**：v0.96.357 之后跑老程序兼容性体检。`sl`（mtoyoda/sl，295 行）**编译不过**，
报 `词法错误：未知字符：\`；此前 `docs/老程序兼容性.md` 记的是「偶尔词法错误（未结）」，
实测**确定性失败**（同一文件连跑 4 次全挂）。缩小后才看清：报错文本只是**连带伤**。

**最小复现**（2026-09-22 钉死，2 行核心）：
```c
#define M "a//b"
int main(void) { printf("[%s]\n", M); return 0; }
```
报 `词法错误 ... 未结束的字符串`。**把同样的字符串直接写进代码就没事**：
```c
printf("[%s]\n", "a//b");                    /* ✅ 通过，输出 [a//b] */
printf("[%s]\n", "http://example.com/x");    /* ✅ 通过 */
```

**先排除掉的几种猜测**（都验过，都不是）：
- **转义反斜杠**不是触发点 —— `#define M "x\\_y"`、`"a\\b\\c"` 全通过；
- **头文件**不是 —— 宏定义在本文件与被 `#include` 的头里一样挂；
- **数组初始化器**不是 —— 直接 `printf(..., M)` 就挂；
- **`//` 出现在普通字符串里**不是 —— 见上面的对照。

**真身**：`#define` 的**替换文本这条解析路径不认字符串字面量** ——
`//` 一律按行注释起始处理，于是**闭引号连同该行剩余部分一起被吃掉**，
后面报的「未结束的字符串」/「未知字符：`\`」都是这一下的余波
（同一个根因，报错却长得不一样，这也是它一直没被认出来的原因）。

**影响面**：凡是宏体里含 `//` 的程序一律编不过。`sl.h` 的
`#define LWHL22 "//// \\_/      \\_/    "` 正是这一形态 ⇒ `sl` 全军覆没；
含 `//` 的 ASCII 图、URL 常量宏、被注释掉一半的路径宏都会中招。

**判据**：`#define M "a//b"` 编得出「未结束的字符串」而字面量版通过 —— 两条一比即可。

**修法**：`CompilerBase/Preprocessor.Directives.cs` 的 `StripComments` 加上字符串/字符字面量识别
（碰到 `"` / `'` 就整段照抄、含转义，里面的 `//` 与 `/*` 都不算注释）。原注释说的
「C99 翻译阶段 3：注释在宏展开前删除」本身没错 —— **错在漏了同一阶段里字面量已被识别**。

⚠ 顺带发现：**C 有两个 `Preprocessor`**（`CCompiler/Preprocessor.cs` 是整份拷贝，只有
`CCompiler/Program.cs` 那条独立 CLI 走它；库入口 `CCompiler.Compile` 走 `CompilerBase`），
**只有后者削宏体注释** ⇒ 两个入口行为本来就不一致 —— 改动要落在 `CompilerBase` 那个。

**验证**：`sl` 的词法错误清零（从「编不过」降级为「缺 `usleep`/`mvcur` 两个库」）；
C 探针 31 例 26/2/3、自测 6394 项全绿，均与基线逐项相同。

### 🟡 函数式宏把**字符串字面量**里的同名文字也展开掉（**已修**）

**发现**：同一天，原本是想验证「能不能用空宏 `#define __attribute__(x)` 抹掉扩展关键字」，
结果这条路**被它挡住** —— `#define ATTR(x)` + `printf("%s", "ATTR(keep)")` **输出 `[]`**，
字符串里的内容被啃掉了。于是先修它，再回去用空宏。

**最小复现**：
```c
#include <stdio.h>
#define ATTR(x)
int main(void) { printf("[%s]\n", "ATTR(keep)"); return 0; }   /* 实测 []，应 [ATTR(keep)] */
```
对照：**对象宏那条路认得字面量** —— `#define FOO 42` 配 `"FOO stays literal"` 输出正确。

**真身**：`CompilerBase/Preprocessor.Expressions.cs` 里**四处**都要处理字面量，**两份漏了**：

| 位置 | 认字面量？ |
|---|---|
| `ReplaceOutsideLiterals`（对象式宏替换） | ✅ |
| `SplitMacroArgs`（实参切分） | ✅ |
| `FindMatchingParen`（配对括号） | ✘ **漏** |
| `ExpandFunctionMacros`（函数式宏找名/替换） | ✘ **漏** |

后果不止"字符串被啃"：`F(")")` 会在字符串里那个 `)` 上**提前收尾**，
把后面的实参与语句一并吞掉。**影响面**：任何函数式宏 × 字符串里出现同名文字即中招
（`#define MAX(a,b)` 会把 `"MAX(x,y)"` 啃掉）。

**修法**：判据收成**一处** `BuildCodeMask(line)`（标出哪些下标在"代码区"），四处都查它 ——
不再各写一份状态机。行被改写后判据作废（`code = null`，下次迭代重建）。

**验证**：`litmac2` / `litmac3` 现在输出正确（`[ATTR(keep)]` 与 `V=9 [far __attribute__((x)) literal]`）；
C 探针 26/2/3、自测 6394/0、Examples 全量编译检查全绿 —— 均与基线逐项相同。

### 🔴 `sizeof(<数组>)` 返回的是**元素大小**，不是数组总大小

**发现**：2026-09-22 写 `Examples/c/old/old_std_primes.c`（素数筛）时撞出来的。
程序里那句最经典的 `memset(flags, 1, sizeof(flags))` **只清了 4 个字节** ⇒
筛法一个素数都没筛出来（只报「共 2 个」，即 2 与 3）。

**最小复现**：
```c
#include <stdio.h>
static char g[10000];
int main(void) {
    char l[100];
    printf("G=%d L=%d\n", (int)sizeof(g), (int)sizeof(l));   /* 实测 4 / 1；应 10000 / 100 */
    return 0;
}
```

**实测矩阵**（`.scratch/oldprogs/bisect/sizeofarr.c`）：

| 形态 | 实测 | 应为 |
|---|---:|---:|
| 全局 `char[8]` / `char[100]` / `char[10000]` | 4 / 4 / 4 | 8 / 100 / 10000 |
| 局部 `char[8]` / `char[100]` | 1 / 1 | 8 / 100 |
| 全局 `int[100]` / 局部 `int[100]` | 4 / 4 | 400 / 400 |

⇒ 规律是「返回的是**元素大小**」（`char`→1、`int`→4）；全局 `char` 那三格给的是 4，
像是走了另一条兜底路径。

⚠ **与"元素访问退化成 32 位"那条无关**（那条已修）：逐元素读写实测是好的 ——
`for (i…) f[i] = 1;` 之后读回 `1111`，`f[5000] = 0` 也只影响它自己。

**影响面**：`memset(a, 0, sizeof(a))` / `memset(a, 1, sizeof(a))` 是最常见的 C 惯用法之一，
中招就是**静默只清前几个字节**，程序照跑、结果全错（本仓最恶的一类）。
凡是用 `sizeof(数组)` 算长度的循环（`for (i = 0; i < sizeof(a); i++)`）同样中招。

**判据**：`sizeof` 一个全局 `char` 数组，看它报 4 还是 10000；再逐元素赋 1 读回，
确认元素访问本身是好的（两条一比就能把责任分开）。

### 🔴 printf 的 64 位格式符（`%ld` / `%lu`）一律输出 `0`

**发现**：同一天写 `Examples/c/old/old_std_wc.c` 时撞出来的 ——
`printf("字符 %ld，词 %ld\n", chars, words)` 打出「字符 0，词 」（第二个还是空的）。

**最小复现**：
```c
#include <stdio.h>
int main(void) {
    long big = 1234567;
    printf("CAST=%d\n", (int)big);   /* 实测 1234567 —— 数**存对了** */
    printf("LD=%ld\n",  big);        /* 实测 0     —— 坏在格式化 */
    printf("D=%d\n",   1234567);     /* 实测 1234567 —— int 通路对照 */
    printf("LU=%lu\n", (unsigned long)big);   /* 实测 0 */
    return 0;
}
```

**责任划分（这张表就是判据）**：

| 表达式 | 实测 | 期望 |
|---|---:|---:|
| `(int)big`（转换取值） | 1234567 | 1234567 |
| `%d` 打印 int（对照） | 1234567 | 1234567 |
| **`%ld` 打印 long** | **0** | 1234567 |
| **`%lu` 打印 unsigned long** | **0** | 1234567 |

⇒ **变量里是好的**（`(int)big` 拿得到 1234567），**坏的是 printf 的 64 位格式符**。

⚠ 注意这与台账里那两条 64 位**存储**缺陷（`float[]`/`long[]` 存不进内存、全局 64 位标量按 32 位存）
**不是一回事**：这条的存储侧是好的，只在**输出格式化**这一层丢。修之前别把它们混在一起排。

**影响面**：`%ld` 是打印 `long`（尺寸、计数、时间戳）的标准写法，
中招就是**数字全变 0**、程序照跑（又一类"能跑但输出全错"）。

#### 往下一层：根因是 **64 位移位不发 L 变体**，但改成 L 变体仍不对（**层次比预想深，已回退**）

`printf` 的 `readLong` 靠 `((unsigned long long)hi << 32) | lo` 拼位（`Lib/shared/src/printf.c:92`），
而它上面那段注释**早就写着**「实测本前端的 64 位**移位不工作**」——
`readDouble` 当初绕开了这条路，**只有整数被留下**（所以坏的只有 `%ld`/`%lu`）。

把移位本身钉出来（判据全部只走 `%d`，绕开 printf 的 64 位路径）：

| 表达式 | 实测（修复尝试前） | 期望 |
|---|---|---|
| `(int)(((long long)0x1234) << 32)` 低半 | 4660 | 0 |
| 同一个的高半 | 4660 | 4660 |
| `(int)(((long long)1) << 40 >> 32)` | 1 | 256 |
| `1 << 20`（32 位内） | 1048576 ✓ | ✓ |

⇒ **64 位左移等于没移**（两半都保持原值）。

**代码层的原因很清楚**：`ExpressionManager.EmitBitOp` 明明算出了 `s`/`l`（宽度与 IsLong）
并用在了 `PUSHL`/`POPL`/`MOVEL` 上，**却在选指令时丢掉了宽度**：

```csharp
bool l = resultType.IsLong();                 // ← 算出来了
var bitOp = SelectBitwiseOp(op);              // ← 这里没用它，永远 32 位 SHL/SHR
```

而**算术那条路早就是按宽度选**的（`SelectArithmeticOp` 里 `if (isLong) … ADDL/SUBL/MULL/DIVL`），
ISA 里也**确实有** `ANDL/ORL/XORL/SHLL/SHRL`（`OpCode.cs:153-158`）—— 典型的"同一形状两处实现、只对了一半"。

⚠ **但把选择器改成 L 变体之后仍然不对**（已实测、**已回退**，别再走一遍）：

| 表达式 | 改成 `SHLL`/`SHRL` 后 | 期望 |
|---|---|---|
| `(long long)1 << 4` / `<< 8` / `<< 16` / `<< 32` | **全是 2**（与移位量无关的常数） | 16 / 256 / 65536 / 0 |
| `(int)(((long long)0x1234) << 32 >> 32)`（链式、不经变量） | 0 | 4660 |

`1 << n` 得常数 2 说明这条 L 指令**在更下面某一层被解释成了别的东西**（像加法），
所以问题不止选择器一处 —— 还要往下查 `SHLL` 的**汇编/序列化**与运行时
（`ExecuteShlL` 在 `VMLRuntime.Float.cs:1030`，它按 `operands.Count` 分两种形参约定）
这条链。**"改成另一种错"不比原来的错好**，所以那次改动没有留下。

**下一步判据**（给接手的人）：先只测小移位量 `(long long)1 << 4` 看能不能得 16 ——
得 16 就说明 L 指令通了、问题在移位量 ≥ 32 的掩码；得常数（如 2）就要往汇编器查。

---

## Ruby

### 🟡 `def` 报 `Unexpected token: End` —— **一个函数都写不了**

**2026-09-19 复测：已经好了**（本条目是陈旧的）。台面上一共验了六种形态：
无参/1 参/2 参、带括号与不带括号的调用、`return`、`def resetGame … end`
（台账原文点名的那个形态）—— 全部编译运行正常。
中间那次「调用约定统一」（2026-09-17）应该是把它一起带好了，
但**当时没人回头复测这一条**，于是 `catch.rb` 顶上的注释和这份台账一起陈旧了两个版本。
⇒ **教训：改了前端要回头把台账里 🔴 的条目重跑一遍**，别信"当时是坏的"。

### 🟡 不认 `&&` / `||`（**已修**）

词法器**完全没有** `&` 与 `|` 这两个 case，一写就 `Unexpected char: &` ——
于是「多个条件」只能写成嵌套 `if`（`Examples/ruby/catch.rb` 就是这么绕的）。
解析器那边**本来就认** `TokenType.And`/`Or`（`and`/`or` 关键字走的就是它），
缺的只是词法这一层。

**已修**（v0.96.259）：`&&` → `TokenType.And`、`||` → `TokenType.Or`，
代码生成里与 `and`/`or` 并列（`case "and": case "&&":`）。
单写的 `&` / `|` / `^`（Ruby 的**位运算**，优先级也更紧）**仍然没做** ——
现在给的是明确的报错文案，不是静默算错。

### 🟡 `puts(变量)` 打出的是**地址**而不是字符串（**新发现，已修**）

```ruby
s = "VAR"
puts(s)          # → 1024      ✗（1024 是那个字符串的地址）
puts("LIT")      # → LIT       ✔
```

`puts`/`print` 在 VML 里有**两条完全不同的实现**（`print_str` 收地址、`print_int` 收数值），
值本身**没有类型标记**，只能编译期判。原判据是「字符串字面量 或 名字像返回串的函数」
—— **变量一律不算**，于是走整数那条把地址打了出来。

**已修**（v0.96.259）：按「赋值来源」记一笔（`_stringVars`），
并在**预扫描**里记下「哪个函数在第几个实参位上收到过字符串」，据此标记形参。
认不准时**保守取整数**（认错成字符串会拿小整数当地址解引用，更糟）。

⚠ **仍然做不到的一种情形**：同一个形参在不同调用点传不同类型
（`f(7)` 与 `f("LIT")` 并存是合法 Ruby）—— 静态判不了，两个答案都不对。
现在的规则是「**所有已知调用点一致才认字符串**」，混合时退回原来的整数口径
（**保证不比改之前更差**）。要根治得给值加运行期类型标记，那是另一件事。

---

## Kotlin

### 🟡 顶层属性读回是 0 —— **根因不在数组，在解析器的兜底分支**（已修）

台账原文是「文件级的 `arrayOf` 读回是 0」，绕过写的是「状态数组写在 `main` 内部」。
**缩小之后比这宽得多**：

```kotlin
val n = 5
val xs = arrayOf(1, 2, 3)
fun main() { println(n); println(xs[0]) }   /* 实测 0 和 0 */
```

**顶层标量一样是 0**，所以根因与数组无关。两处凑成：

1. `Parser.Parse()` 只认 `external`/`fun`/`data`/`class`/`interface`/`sealed`/`object`，
   **没有一条分支认 `val`/`var`** ⇒ 兜底的 `else Advance()` 把声明**一个 token 一个 token 地
   静默吃掉**（连报错都没有）；
2. 就算声明在，`VarRef` 那条也只在 `_varOffsets` 里查（那是**函数局部**表、每进一个函数就 Clear），
   查不到就**一条指令都不生成** ⇒ R0 留着上一步的残值 ⇒ 读出来恒为 0。

**已修**（v0.96.260）：解析器认顶层 `val`/`var`；生成器把顶层属性放**数据段**，
在 `main` 开头统一初始化一次；`VarRef`/`AssignStmt` 各补一条全局分支。
判据：`scripts/vml-out-probe/langs/nat.kt`（值放顶层属性里）。

### 🟡 `step` / `until` / `downTo` 被当成硬关键字（已修）+ 步长**根本没生效**（同批修）

三者在 Kotlin 里都是**软关键字**（只在 `for (i in a..b step c)` 这个位置有意义），
放进词法关键字表等于**禁止用户拿它们当变量名**：`var step = 5` 报
`Expected variable name ... got KEYWORD 'step'`。
**已修**：从关键字表移出，`for` 那三处改成按**文本**比对（`Cur.Value == "step"`）。

⚠ **改完顺手验了一下 `for..step`，发现步长压根没生效**：`ForStmt.Step` 解析出来了，
但**代码生成从没读过它** —— 增量写死 `±1`，`for (i in 0..10 step 2)` 静默打出十一个数。
同批修掉（step 表达式进循环前求值一次存槽位）。
判据：`nat.kt` 里那个 `0..12 step 2` 累加得 **42**；步长不生效时是 78，一眼分得出来。

### 🟡 `println(变量)` 打出的是**地址**（新发现，已修）

与 Ruby 的 `puts(变量)` 是同一族：`println`/`print` 在 VML 里有两条实现
（`print_str` 收地址、`print_int` 收数值），原判据只看「字符串字面量 / 名字像返回串的函数」，
**变量一律不算** ⇒ `val s = "abc"; println(s)` 打出 `1032`。
**已修**（v0.96.260）：按初始值登记 `_stringVars`（函数作用域）/ `_stringGlobals`（顶层）。
⚠ **形参仍做不到** —— 解析器把形参的**类型标注整个丢掉了**（`pars.Add(...)` 只存名字），
所以 `fun f(s: String) { println(s) }` 还是打地址。要修得先把形参类型留着。

---

## Rust

### 🟡 数组字面量必须写在一行（**已过时**）+ `println!` 下标实参打出 `[expr]`（**已修**）

台账原文是「跨行的 `];` 会报非法 token」。**2026-09-19 复测：跨行数组已经能编了**（陈旧条目）。
但顺着试了一眼输出，抓到另一个真问题：

```rust
let xs = [1, 2, 3];
println!("{}", xs[0]);        // → 打出字面量 "[expr]"   ✗
println!("{}", xs[1] + 10);   // → 12                    ✔
```

`println!` 的格式实参是**按 AST 节点类型**分派的（字面量 / 标识符 / 二元运算 / 函数调用），
**没有下标这一条** ⇒ 掉进兜底分支，那分支往数据段塞一个字面量 `"[expr]"` 就打出来了。
**同一个表达式放进二元运算里反而是对的** —— 只测那一种形态永远照不出来。

**已修**（v0.96.261）：补 `IndexAccessNode` 分支；兜底分支**改成报错**不再打占位符
（本文件上面那条整数字面量的分支早就写着「宁可报错也不静默丢」，这条属同一族）。

### ⚪ 部分标准库函数没有

用之前先试。

---

## Fortran

### 🟡 `if` 条件里解析不了「紧跟括号的除法」（**已过时**）

**2026-09-19 复测：好了**。`if ((a / b) > 3) then` 正常编出 `DIV-OK`。
（与 Ruby 那条 `def` 一样，是中间某次前端改动顺带带好的，**没人回头复测**。）

---

## Pascal

### 🟡 注释是 `{ }` 不是 `//`（**已修**）

词法器只认 `{ }` 与 `(* *)`，`//` 被原样吐成两个 `/` 交给语法分析 ⇒ 报的是**莫名其妙的语法错**。
而现代 Pascal（Delphi / Free Pascal —— 本前端的目标就是它们）都认 `//`。
**已修**（v0.96.264）：`//` 跳到行尾即止，不跨行。

### 🟡 注释里只能写 ASCII（**已过时**）

**2026-09-19 复测：好了** —— `{ 中文注释 —— 破折号、逗号都在这儿 }` 正常编译。

---

## BASIC

### 🟡 形参名不能叫 `on`（关键字）—— **已修掉"假崩溃"，但引用侧仍认不出**

**症状分两层，原条目只写了后一层。** 拿 `on`（BASIC 保留字，`ON ERROR` / `ON…GOTO` 用）
当形参名时：

1. `ParseFunctionDeclaration` 的形参检查是 `if (Peek().Type != TokenType.IDENTIFIER) return null;`，
   而 `return null` 在调用方（`Parser.Core` 的 `case TokenType.FUNCTION`）只表示
   「这条语句没解析出来」⇒ **整条声明被静默丢掉**，词法位置却停在形参列表**中间** ⇒
   后面的 token 全被当成顶层语句继续解析。编出来的东西运行期报
   **`内存不足，无法分配!`** —— 屏幕上没有一个字提到真正的错处。
2. 就算声明解析出来了，**引用侧也认不出**：表达式解析器只在 `TokenType.IDENTIFIER` 上
   建 `Identifier` 节点，而 `on` 词法上是 `TokenType.ON` ⇒ `add = on + 1` 里那个 `on` 读成 0。

**已修第 1 层**（v0.96.262）：形参名的判据从「必须是 IDENTIFIER」改成
「**这个 token 的文本能不能当名字**」，与它是不是关键字无关。
⚠ 一开始写成「一律报错」，**立刻打掉了本来能用的写法**：
`Examples/basic/sysinfo.bas` 里 `NATIVE FUNCTION ui_call_json_s(fn AS STRING, …)` 的 `fn`
就是 `TokenType.FN`，而它 NATIVE 无函数体、声明里根本用不到形参 ⇒ 以前丢掉声明也照样跑。
**先别急着把"静默"改成"报错" —— 先数一遍有多少正常写法在靠那个静默。**

**第 2 层没修**（有意）：要根治得让表达式解析器在**每一个** `TokenType.IDENTIFIER` 判据处
也认「文本与已声明名字相同的关键字」，而解析器**根本没有变量名表**（只有 arrays/functions/subs）
—— 得先立一张（DIM / 赋值目标 / 形参 / FOR 变量…都要登记）。影响面远超收益，且
`ON`/`FN` 在 BASIC 里本来就是保留字。**现状：不再崩溃、不再报假错，但读出来是 0。**

### 🟡 裸调「有返回值的声明函数」会被静默丢弃

**现象**：用户报「只弹对话框、不弹绘图窗口」。`ui_win_open` 声明的是 `NATIVE FUNCTION`
（进 `declaredFunctions`），而标识符语句分支**只认 `declaredSubs`** ⇒ 落到
`ParseLetStatement()`、那一行又没有 `=` ⇒ **静默丢掉**（连 `line_N` 行标都不发，
汇编里 `line_80` 直接跳到 `line_82`，**根本看不出少了东西**）。

**为什么只有 `ui_win_open` 中招**：它是唯一「**有返回值 + 当语句裸调**」的那个
（其余 `ui_dlg_msg`/`ui_clear`/`ui_rect` 都是 `NATIVE SUB`，裸调用正常）。

**已修**：解析器认「已声明的函数名 + 后一个 token 不是 `=`」⇒ 当隐式调用；
并且 `GenerateCallStatement` 的判据与表达式路径对齐（此前只查 `subMap`，
函数在 `funcMap` ⇒ 标签被编成 `sub_ui_win_open`、永远解析不到）。

---

### v0.96.330 一轮：立判据 + 修 6 条

这一轮为「把更多 QBasic 经典改成手机版」先立了**语言特性判据**（`scripts/vml-basic-probe/`，21 例，
顶层 vs SUB 内各一遍），再逐条修。**修掉的 6 条都已在 `CHANGELOG.md` 的 v0.96.330 里详述**，
这里只登记"还没修的"与"这一轮暴露的方法论"：

**已修（要点）**：`FOR` 负步长（两套实现都无条件 `JG`）、`CHR$`（PRINT 路径把字符串指针当字符码）、
`SELECT CASE` 顶层（`Expect` 只断言不消费 ⇒ 幽灵块永远匹配）、`SELECT CASE` 在 SUB
（`GenerateSubStatement` 分派漏了一支 ⇒ 整支不生成）、`DATA`/`READ`（`DATA` 被整条丢弃 +
4 处 `MOVE` 方向写反）、字符串临时量共用缓冲区（`basic_concat` 只有一块 `_buf3`）。

**v0.96.331 又修两条**（同一轮里由 `blackjack.bas` / `lander.bas` 逼出来的）：

- **`EXIT WHILE` / `EXIT FOR` 在 SUB/FUNCTION 里失效**：`GenerateSubWhileStatement`
  **从未调 `Sta.PushLoopLabels`**（旁边的 `GenerateSubDoLoopStatement` 一直有），于是
  `EmitBreak` 见循环栈为空 ⇒ **一条指令都不发、静默空操作**。症状两极：
  **唯一的出口就是那句 EXIT** 的循环（`WHILE sum > 21`）⇒ **死循环**（BLACKJACK 的
  `handValue` 实测卡死在自检第一处调用上、整轮超时）；条件也会自己结束的 ⇒
  **多跑完剩下的圈数**（最小复现 `d5.bas`：`f(4)` 应返回 5、实测 100）。
  判据：`scripts/vml-basic-probe/cases/26-exitloop.bas`。
- **用 CONST 当数组维度不生效**：解析 `DIM a(N)` 时标识符维度被写死成 `lowerBound = 1`
  占位，注释还说"实际大小在代码生成阶段算"——**那个阶段根本没人算** ⇒ 只分 2 格、
  一写就越界。**这条会静默写穿数组**：LANDER 实测循环变量一路跑到 `c=760`（= `sh` 的值）、
  把地形数组和邻居变量一起写花，画面表现为"地形画到天上去了"。
  修法：解析器立一张 `_constValues`（只收字面量 CONST），`DIM` 的维度查它。
  判据：`cases/27-constdim.bas`。

**v0.96.332 补一条（写 NIBBLES 时撞出来的）**：

- 🔴 **字符串函数的返回值共用 `_buf1`**：`STR$(1) + "/" + STR$(2)` 实测打出 **`1/1`**
  （应 `1/2`）—— `STR$`/`LEFT$`/`MID$`/`CHR$`/`UCASE$`… **全都返回同一个 `_buf1`**，
  同一个表达式里两次调用必然互相覆盖（谁后写谁赢）。
  这与已修的「拼接结果共用 `_buf3`」是**同一个家族**，但修法不同：拼接那边前端能给
  **每个拼接点**分配槽位（`basic_concat_slot`），而这些是**库函数**、调用点信息进不来。
  可行路径要么给它们一个**轮转池**（常见写法 2~3 个就不会撞），要么照 concat 的形状
  让前端传槽位。判据 `cases/28-strfnbuf.bas`，**标记为 KNOWN-RED**
  （runner 会单独报，不计入通过/失败 —— 否则「N/N 全绿」这个信号被一条已知项永久污染）。
  NIBBLES 的进度显示原本就是被这条打花的（`0/5` 显示成 `0/0`）。

**仍未修（都有最小复现）**：

- 🟡 **数组形参不支持**：`FUNCTION f(n AS INTEGER, a(12) AS INTEGER)` —— 函数体里的
  `a(0)` 被当成**函数调用**去链接，报 `未定义的函数 'func_a'`（实测 `d3.bas`）。
  要根治得让调用点传**数组基址**、被调方按基址索引，是一次 ABI 改动（解析器 + 代码生成
  + 调用约定三处），评估后**未在本轮做**。现阶段的替代写法：**两个数组合成一个、
  用偏移区分**（BLACKJACK 的玩家/庄家手牌就是这么放的：`hand(0..11)` / `hand(12..23)`）
  —— 好处是"算点数"那条规则仍然只有一份。
- 🟡 **裸名调用无参 FUNCTION 不触发调用**：`FUNCTION three() AS INTEGER` 用**带括号**
  调用（`three()`）是对的（`A=7`），但裸名 `PRINT three` 会当成**变量**读、得 0。
  QBasic 里 `PRINT three` 应该触发调用。影响面窄（本仓的游戏一律带括号写），未修。
- ⚪ **`DIM x AS INTEGER` 一律报「隐式声明的变量」警告**（值是对的，纯噪音）——
  噪音太多会淹掉真警告，值得单独收一次。

**方法论（这一轮反复用到的两条）**：

1. **"少执行一段代码"类缺陷不报错** —— 上面 6 条里有 4 条的形态是「编译全绿、只是某段代码
   根本没生成 / 生成反了」。**判据必须是"打印出来的值"**，不能是"编过了"。
2. **同一个 bug 常被复制成两份**：`FOR` 负步长在主程序与 SUB 两套实现里各写了一遍
   （两边都无条件 `JG`）。**修的时候要顺手合成一份共用实现**，别再复制第三遍。

---

## Scheme

### 🟡 用户函数看不见顶层变量（**已修**）+ 三条被它掩盖的老问题

**根因**：顶层绑定按 `R12 + (12 - off)` 寻址，而 `R12` 在**函数里是那个函数自己的帧指针**
（序言 `push R15; push R12; move R12 R13`）⇒ 同一个偏移读出来是该函数帧里的某个槽，两边互相踩。

**已修**（v0.96.265）：入口时把 `R12` 存进数据段 `__scheme_top_fp`，
凡是要碰**顶层绑定**的地方都改用它当基址（`EmitLoadTopVar` / `EmitStoreTopVar`）。
`main` 全程只 `sub R13`、不动 `R12` ⇒ 顶层处两者相等，同一套代码在顶层也成立。

⚠ **修的时候又踩了本仓的老毛病：`set!` 在三个地方各写了一遍**
（顶层 / 表达式与函数体 / 另一条运行时分支），**只改了第一处** ⇒ 函数体里的
`(set! g …)` 照旧写 `[R12+off]`。症状是**函数永不返回**（VM 超时），
而汇编里一眼能看到 `move [R12+8] R0` 与上面三行 `move r1 [__scheme_top_fp]` 不是一套基址。
三处已收敛成 `EmitStoreVar` 一份。

**判据**：`(define g 10)(define (bump n)(set! g (+ g n)) g)(print (bump 5))(print g)` → `1515`。

---

#### ⚠ 修完之后「六种形态各用一遍」才发现的：三条**一直被上面那条掩盖**的老问题

`scripts/vml-abi-probe/...` 之外单独写了一份 `<自测：六种形态>` 探针。**改动前整份程序崩在野地址上**
（就是上面那条），所以后面几条**从来没有跑到过** —— 「骨架全绿只证明这条路径没坏」。

| # | 形态 | 现象 | 状态 |
|---|---|---|---|
| 1 | 函数体**多形式** | `(define (multi x) (set! g x) (+ x 1))` 的 `(multi 9)` 返回 **9**（set! 的值），应 10 | **已修**（v0.96.265）：只生成 `l.Items[2]`（第一个形式），第二个起被静默丢掉 —— 与 `let`/`let*`/`letrec` 三处**同一族**，那三处上一轮已修成循环，**唯独函数体漏了** |
| 2 | 命名 let | `(let loop ((i 0)(s 0)) (if (< i 5) (loop (+ i 1)(+ s i)) s))` 得 **61**，应 10 | 🔴 **未修** |
| 3 | N 元算术 | `(+ a b c d e)` 得 **3**，应 15 | 🔴 **未修**（见下） |

第 3 条**不是**「多局部量」的问题（局部量本身是对的）：`GenCall` 只把
`l.Items.Count == 3`（即**恰好两个操作数**）的 `+ - * /` 特判成一条指令，
三个以上落到通用调用路径 ⇒ 去调一个不存在五参的 `+` 库函数。Scheme 的 `+` 是**变参**的，
要按左折叠展开成 `((a+b)+c)…`。

### 🟡 六条互不相干的帧/调用错误（骨架全绿也照不出来）

① `main` 不占帧 ⇒ 顶层变量只能放 3 个 · ② ≥2 个形参读错实参（形参槽偏移）·
③ 从用户函数里调库函数必崩（尾调用优化对**别的**函数等于跳过序言）·
④ 函数体 / `let` body 只编译第一个形式 · ⑤ 命名 let 完全不占帧 ·
⑥ 帧大小按生成结束时的 `varOff` 算（低估）。

**已修**。教训：**骨架全绿只证明"这条路径没坏"，不证明这门语言能用** ——
`skel.scm` 的函数只有**一个参数**、循环全在**顶层**、且**从不从函数里调库**，
正好绕开全部六条。补例程时要**刻意把每种形态各用一遍**。

---

## Python

### 🟡 列表「写不生效」：`b[i] = v` 之后读回来还是 0（**已过时**）

**2026-09-19 复测：好了** —— `b = [0,0,0]; b[1] = 7; print(b[1])` 打出 `7`。

---

## JavaScript

### ⚪ 入口要自己调一次

定义 `function main()` 之后**记得手动调用**（不是自动执行）。

---

## Objective-C

### 🟡 `#include <waycoder_ui.h>` 报 `expected )`（**已修**）

**真身**：头文件里有**两个形参叫 `id`**（`ui_timer_kill(int id)`、`ui_gradient(char* id, …)`），
而 **`id` 在 Objective-C 里是保留的*类型名*** ⇒ ObjC 前端在形参位置读到 `IdType 'id'`
就报 `expected )`。

**已修**（v0.96.263）：两个形参改名（`timerId` / `gradId`）。
**原型里的形参名对 C 没有任何语义，改名零风险** —— 而 `Lib/c/waycoder_ui.h` 是
22 门语言共用的那一份，让它对 ObjC 也能 `#include` 才合理。

**判据**：`#include <waycoder_ui.h>` + `ui_clear(...)` / `ui_rect(...)` 编译通过；
`Examples/objc/*.m` 四个例子与 `drift.m` 全部照旧。

⚠ 顺带实测到一条**没记过、也还没修**的：ObjC 前端**不认 `0x` 十六进制字面量** ——
`ui_clear(0xFF000000)` 报 `expected ) (got Identifier 'xFF000000' at line …)`（`0` 和 `xFF000000` 被切成两个 token）。
现阶段颜色按**负数十进制**写（与 `Examples/objc/snake.m` 一致）。

---

## Ladder

### ⚪ 做不了界面程序

没有「带字符串参数的函数调用」这种语法 ⇒ UI 那整套接口一个都调不到。
只能用「编译与运行」本身。

---

## 全局（跨语言 / 链接期）

### 🔴 Forth 里**任何** `#param lib(新模块)` 都会让 `builtin` 一族变成未定义（**未修**）

**现象**：Forth 源码里只要写一句 `#param lib("<不是默认集里的模块>")`，
本该由默认包装器提供的函数（`print_int` 等）就变成**未定义的函数**：

```forth
#param lib("base64")
42 . CR
```
```
<file>:2: error: 未定义的函数 'print_int'（引用 1 次）
```

**已缩小的部分**（都是实测）：

| 输入 | 结果 |
|---|---|
| `42 . CR` | 正常 ✓ |
| `#param lib("math")` + `42 . CR` | 正常 ✓ —— **`math` 本来就在默认集里** |
| `#param lib("parserexp")` + `42 . CR` | ✘ `print_int` 未定义 |
| `#param lib("parserexpf" / "base64" / "matrix" / "color")` + `42 . CR` | ✘ 四个**都**报同一个错 |

⇒ **与具体模块无关**：只要 `#param lib(...)` 往 `LinkStandardLibrary` 的
`libraryPaths` 里**加进一条默认集之外的路径**，就会丢包装器。
（`math` 那条不坏，正因为它加进去的路径**已经在** `allPaths` 里 —— 集合没变化。）

**为什么这条值得记**：它正是 `Examples/forth/parserexp_demo.fs` 编译失败的原因，
而那个例程**本来**该用 `#param lib("parserexp")` 表达依赖 —— 也就是说
**「在 Forth 里声明依赖」这条路本身是坏的**，用户只要声明任何额外库就会掉进这个坑。
（`Examples/dart/parserexpf_demo.dart` 能过，说明别的语言没这个问题。）

**已证伪的假设（别重走）**：怀疑是 `ForthCompiler.CompileFile` 里那句
`else if (allLibraryPaths.Count > 0) LibraryLinker.LinkLibraries(prog, allLibraryPaths)`
抢先用**残缺清单**链了一次、把 `print_int` 判死。**实测把这个分支整个删掉，症状一字不变**
（`print_int` 仍未定义）⇒ 不相干，已还原。**更奇怪的是**：删掉之后 `#param lib(...)`
在这条链上就**完全没有作用**了，而症状还在 —— 说明差异产生在**更早**的地方，
不在 `CompileFile` 对 `#param` 的处理里。

**已经查到的事实**（可直接接着查）：

1. 文件链是 `vmlcli` → 插件 `CompileFileWithIncludes` → `CompilerHelper.BuildCompileFileWithIncludes`，
   后者把 `GetStandardLibraryIncludes(langName, useSharedLibrary)` 与**调用方给的** `libraryPaths`
   合成 `includeFiles`，再 `ToVmlTextWithIncludes` 写成 `.linked` 指令。
   ⇒ **文件里的 `#param lib(...)` 收集在 `CompileFile` 的局部变量里，压根到不了 `includeFiles`**。
2. 真正决定链哪些库的是**汇编出来的 `.linked` 清单**（`CompilerHelper` 里 `SharedPrefixMap`
   上方那段注释已经记过这件事：那张映射表在这条链上是死代码）。
3. 症状与模块无关（parserexp / parserexpf / base64 / matrix / color 五个体感一致），
   而 `math` 不坏 —— 因为 `Lib/forth/math.vml`**本来就在默认集里**。

⇒ **下一步**：在 `ToVmlTextWithIncludes` 的 `includeFiles` 上打一行（有/无 `#param lib` 各跑一次），
看那份 `.linked` 清单到底差在哪 —— 差异一定在那里，而不是在 C# 侧的链接调用里。
位置数字与判据**一个字都不要动** —— 现有探针全在断这个。

**状态**：🔴 未修。**绕过**：Forth 里不要写 `#param lib(...)`；需要额外库就改那门语言的
默认 `.linked` 清单（`Lib/forth/builtin.vml` 一族）。

### 🟡 `Lib/` 里两套栈清理约定并存（**已消解**，2026-09-19 复核）

C 前端生成的函数是**调用方清参数**（`move R13 R12; pop R12; pop R15; ret`，
调用点后面跟 `add R13 #4/#8`），而 `Lib/` 里 **764 个函数是"被调用方自己清"**，
另有 446 个与前端一致。⇒ **每调一次那 764 个之一，调用方的栈指针就多释放一次**。
漂了之后凡是**用 `pop` 取临时值**的地方都读错。

⚠ 影响面是「**漂了之后谁用 pop**」，不是"调了就坏" —— 只调 `waycoder_ui.h` 包装函数的
程序不受影响（那些是"调用方清"，与前端一致）。

**正解**：用当前前端把 `Lib/` 整个**重新生成**（`touch Lib/shared/src/*.c` + `GenLib -b`）。
只诊断、不要一刀切统一 —— 库函数之间也互相调，A 调 B 时 A 是否清参数取决于 B 的约定，
统一会改坏另一半。

**复核（2026-09-19）**：**已经消解了**。机械扫 `Lib/**/*.vml` 里「被调用方自己清」的收尾形态
（`pop R15` 紧跟 `move R1 [R13]`）一共 **703 处、分布在 63 个文件，
**全部在 `Lib/shared/backup/` 下**，而 `backup/` **没有任何东西引用**
（`grep -rn 'backup/'` 在 `.vml`/`.xml`/`.json`/`.cs` 里零命中）。
也就是说 2026-09-17 那次 `Lib/` 重生成把现役文件全换成了「调用方清」这一套。
⇒ **这条留着当历史**：真正要记的是「**同一个 ABI 有两套约定、且没有编译期检查**」这个教训。

### 🟡 `LibraryLinker` 用纯后缀匹配重定向 CALL ⇒ `_printf_itoa` 被认成 `itoa`

**现象**：C 的 `printf` 一个字都不输出（但**执行继续**，后面的 `puts` 照常）。

**真身**：`shared/src/printf.c` 的 static 助手叫 `_printf_itoa`，链接后是
`lib_printf__printf_itoa` —— 它 **`EndsWith("_itoa")`** ⇒ 被当成「itoa 的包装器」，
整个调用被重定向到 `convert.c` 的 `itoa(int value, char* dst)`（**参数顺序完全相反**）
⇒ 所有 `%` 转换静默失效。

**修法**：判据要带边界 —— target 得**恰好**是 `lib_<模块>_<裸名>`；
并且"取最长匹配"而不是"break 在字典遍历顺序第一个匹配"（那本身是不确定行为）。

⚠ 中途改过一版"取最长匹配"**没修好**（`globalLabelMapping` 里只有 `itoa`、
没有更长的 `_printf_itoa`），那是未经验证的链接器行为变更，**已撤回** ——
**没修好就先撤，别留一个说不清效果的改动**。

### 🟡 同一个东西可能有第二份实现，而且不在你以为的地方（**已修**）

`Lib/c/stdio.h` 里一句 `#param lib("stdio_funcs")` 把 `c/stdio_funcs.vml` 拉进链接，
而那份文件（**连 `.c` 源都没有**）里只有 printf 家族、且是坏的。
**判据极其干净**：同一份 C 代码，唯一差别是那个 include ——
不带 → `call lib_printf_printf` 三行全对；带 → `call lib_stdio_funcs_printf` 一字全无。

**「有没有第二份实现」要按「链接进哪个模块」查，不能按「源码里搜同名」** ——
`#param lib(...)` / `.linked` / 前端的「函数名→模块」映射是三条独立入口，任一条都能把调用导走。

**复核（2026-09-19）**：原来那份坏实现已删；各语言的 `stdio_funcs.vml` 现在是**转发壳**
（`.linked "../shared/printf.vml"` + `../shared/scanf.vml`），内容只有 3 行。
`d` 与 `objc` 的 `stdio.h` 里那句 `#param lib("stdio_funcs")` 还在，但链到的是这个壳 ⇒ 无副作用。
**实测**：ObjC 的 `printf("%d", 42)` → `42`、`sprintf("%s")` → `abc`，都对。

---

## Dart

### 🟡 **文件不完整（EOF 处少 `}`）会让编译器挂死**（**已修** —— 真因在共用基类 `Check`）

**现象**：不是报错，是**死循环** —— 进程不退出、不打任何输出（实测 `timeout 15` 拿到退出码 124）。
对「写了一半就先存一下」这种日常操作，表现为「点了编译，界面永远卡在编译中」。

**最小复现**（独立的单个文件，喂 `vmlcli <文件>`）：

```dart
void main() {
```

**对照（这些不挂）**：

| 输入 | 结果 |
|---|---|
| `void main() {}` | 正常编译 ✓ |
| `void main() { x` | 不挂（且**编过了** —— 另一个问题，见下） |
| `class A {` | 不挂 ✓ |
| `{` | 不挂 ✓ |
| `"abc`（裸的未终止字符串） | 报错退出 ✓ |
| `void main() {` ↵ `  var s = "abc` | **挂**（与第一条同族） |

⇒ 触发条件是 **`void` 开头的函数声明 + 函数体没闭合**，**不是**「字符串没闭合」、也**不是**「括号没闭合」
（`class A {` 同样少括号却不挂）。

**已排除**（都是实测，不是推测）：

1. **不是本轮改动引入的**：把未提交改动全部暂存、再把 `DartCompiler/Parser.cs` 换回改动前的版本，
   **照样挂**（同样 124）⇒ 既有缺陷。
2. **解析器的四个主循环不是旋转点**：给 `Parse()` / `ParseBlockBody()` / `ParseStatement()` /
   `ParseExpression()` 各加「每 20 万次迭代打一行」的插桩，重编后跑复现输入，**一行都没打**。
3. 词法器**有**追加 `EOF` 标记（`DartCompiler/Lexer.cs` 主循环之后那一句）——
   所以「`Cur` 到尾部后不再前进 ⇒ `while (!Check(EOF)) Advance();` 永转」这条最常见成因
   也要另找落脚点。

**下一步该往哪查**（留给接手的人）：把插桩从「主循环」换成「**逐函数进入/退出计数**」，
或直接对挂住的进程取一次栈（`dotnet-dump` / 调试器附加）。目前的收窄只证明「不在那四个循环里」。

**顺带发现的另一个问题**（同一族输入，先记下、别混进这一条）：`void main() { x` **编译成功**、退出码 0
—— 一个**函数体没闭合**的程序不该编得过。

**真因（2026-09-20 定案）—— 不在 Dart，在**共用基类**：**

```csharp
// ParserBase.Check（修前）
protected bool Check(TTokenType type) =>
    !IsAtEnd && EqualityComparer<TTokenType>.Default.Equals(GetTokenType(Cur), type);
//   ^^^^^^^^^ 这个短路让 `Check(EOF)` **永远返回 false** —— 末尾时 `IsAtEnd` 为真，被短路掉
```

于是 `ParseBlockBody` 的

```csharp
while (!Check(TokenType.RBrace) && !Check(TokenType.EOF))   // 末尾处两个条件同时为假 ⇒ while(true)
{
    var stmt = ParseStatement();        // ParseExprStmt 立刻返回、**一个 token 都不消费**
}
```

在**跑到输入末尾**时就是 `while (true)`，而循环体里的 `Advance()` 到末尾也不再推进 ⇒ **死循环**。
`void main() {` 正好让 `ParseBlockBody` 在 EOF 处进入这个状态。

**影响面（机械扫过）**：`Check(...EOF)` 这种写法全仓 **104 处**、跨 **8 门** ——
都是「畸形输入跑到尾部」才发作的潜伏死循环；同样形态的 `Match(...EOF)` 另有 2 处（Pascal）。
语料里全是完整文件，所以一直没露头。

**修法**：去掉那个短路。**代价为零** —— 22 门的词法器**全部**在末尾追加了 EOF 哨兵（逐个核过），
末尾处 `Cur` 就是那个 EOF token ⇒ `Check(EOF)` 该为真、其余类型该为假，与从前**只差 EOF 这一格**。
（空 token 列表另作兜底返回 false，避免 `Cur` 去取 `_tokens[^1]` 而抛。）

**判据**：`void main() {` / `void main() {` ↵ `  var s = "abc` 全部由
**挂死（`timeout` 退出码 124）变成报错退出（退出码 1）**；DiagProbe 三档仍 22/22；
`vml-diag-probe` 61/61、`vml-out-probe` 30/30、`vml-abi-probe` 7/7、桌面自测 6134/0、
Examples **79/3 且零超时**（与基线相同）。

**顺带修掉的第二条**：`class A {`、`void main() { x` 这类**函数体/类体没闭合**的输入，
此前是**静默编过**（退出码 0）—— 现在也报错了（同一个 `Check` 修复的连带效果）。

**另外加了通用兜底**（防同类问题再变成挂死）：`ProgressGuard` ——
「**位置长时间不动**」判定为死循环，抛一条带位置的错而不是挂死；解析器 `Cur` 与词法器 `Peek`
两处热路径各挂一个。判据选「位置不动」而非「总步数超限」：后者对大文件必然误报，
前者在合法输入上不可能发生（两次推进之间只读常数次）。

**怎么查出真因的**（可复用的手法）：先给「主循环」加迭代插桩 —— **一行都没打**，
说明旋转点不在那几个循环里；于是改成**让正在转的线程自己打栈**（后台线程置标志、
热路径看见标志就 `Environment.StackTrace`），栈顶直接指到 `ParseExprStmt` 的第一行 ⇒
"被反复调用"⇒ 往上追到 `ParseBlockBody` 的循环条件。
⚠ 两个坑：栈要**写文件**（`Console.Error` 在重定向下是缓冲的，`Environment.Exit` 不帮你刷，
实测一个字没落盘）；**不能在守卫抛错的表达式里再读 `Cur`**（会再触发守卫自己的消息 ⇒
无限递归 ⇒ 栈溢出，实测 1.3 MB 的 "Stack overflow" dump）。
上面就是全部已知信息，**别把"猜测的成因"当结论用**。

**绕过**：确保送进编译器的文件至少括号配平。

---

## 写新例子时的防御性写法（都是从上面这些坑倒推出来的）

```c
/* 1. 不要通过指针形参写回 —— 用全局量（见 C 的第一条） */
int rx, ry, rw, rh;
void key_rect(int i) { rx = …; ry = …; }

/* 2. 不要 `(int[]){…}` 复合字面量 —— 顶点用具名全局数组 */
int g_pts[24];
void draw_thing(void) { g_pts[0] = …; ui_polygon(g_pts, n, …); }

/* 3. 不要 `%` 格式化 —— 自己转字符串 */
char g_buf[16];
void itoa_(char* b, int v) { … }

/* 4. 不要 `#define` 续行 —— 多行宏写成一行 */

/* 5. 别在参数位置写嵌套三元（虽然没证实是它，但改成 if 也不亏） */
```

---

## 工具链上的两个坑（2026-09-19 各踩一次）

**① 重生成 `Lib/` 必须用 GenLib，不能用 `vmlcli --rebuild-lib`。**
`--rebuild-lib` 只走裸的 `CompileFile`，**不设 `CompilerOptionsContext`** ⇒ 默认 Soft 模式 ⇒
`convert64/conv/array64/math64…` 这些必须用**硬件 64 位指令**的模块会退化成库调用
（实测把 `printf.vml` 重生成后 `movel` 从 47 掉到 1、`pushl/popl/cmpl` 全成 0）。
`GenLib` 的 `BuildShared` 显式 `RunWith(Int64Mode.Hard + Float64Mode.Hard)`（与
`vmltool.config.xml` 的 `<Int64>hard</Int64>` 一致），并且**跳过 mtime 比 `.c` 新的 `.vml`** ——
只想重建一个模块时，`touch Lib/shared/*.vml` 再把它单独 `touch -t` 改老即可
（实测 `完成: 1 编译, 105 跳过`，diff 只有 11 行）。
`.vml` 是 **LF**（`.gitattributes` 里 `eol=lf`），GenLib 的 `WriteGen` 正好写 LF，别手动转 CRLF。

**② `scripts/vml-out-probe/run-langs.sh` 里用了 `timeout`，而那是 GNU coreutils 的命令。**
macOS 默认没有 ⇒ 整条命令行 `command not found` ⇒ 抓到空输出 ⇒
**28 条探针一起报 FAIL，看上去像"所有语言都坏了"**（实测就是这样，手工单跑 `out.c` 却三行全对）。
已改成「有 `timeout` 用它、其次 `gtimeout`、都没有就不加外壳」（vmlcli 自己的 `--timeout`
已经能在 VM 层掐掉跑不完的程序）。**判据脚本坏了比没有更糟 —— 它会指挥你去修错的东西。**

---

## 待办：每条编译错误都带一个「英文错误 ID」（多语言版本的底座，**尚未做**）

**要做的事**（用户 2026-09-20 定）：所有编译错误统一成下面这个形状 ——
`[英文错误 ID]` 是稳定 ASCII，将来把它换成别的语言就直接得到多语言版本，
不用再动任何一个前端：

```
file:xx,line:xx,col:xx,error:<本地化消息> [English_Error_Id]
```

**现状**（2026-09-20 通查 22 门前端 + CompilerBase 的结论）：

- **只有一条路带 ID**：走 `DiagnosticBag` / `GccError` 的那些（`CompilerError.ToString()`
  在句尾拼 `[Code]`，宿主 `VmlDiagnostics.StripCode` 再摘走）。
  实测**源里写明 `[…]` 的报文只有 10 条**（9 条预处理指令 + 1 条「找不到头文件」的警告），
  另有十来处经 `WarnUndefined`/`WarnUnused`/`ReportUndefined` 的辅助函数借 `AddError`/`AddWarning`
  带上 code —— 相对 1000 多个 `Expect` 调用点，等于**没有**。
- **绝大多数没有 ID**：`ParserBase.Error(string)` / `LexerBase.Error(string)` /
  `CompilerPluginBase` 的兜底全走 `ErrorCode.Unknown`。仅
  `Expect(TokenType, "…")` 这一族就有 **1020 多个调用点**（22 门加起来），
  全部拿不到 ID —— 它们正是用户最常看到的那批（「期望 ';' 在 break 后」）。
- **`ErrorCode` 枚举只有 57 个成员**（`Lexer_`/`Parser_`/`Preprocessor_`/`CodeGen_`/`Compilation_`
  五个前缀，值是 1000/1100/1200/1300/1400 分段整数），粒度是**分类**不是**逐条消息** ——
  要按 ID 做本地化，先得把「一条消息 = 一个 ID」这层补出来。
- **文案是硬编码中文**：换语言 = 改 350 多处源码，改不动。
- **`VMLPlugins` 里其实已经有一套 Localization**（`Resources/Locale.*.resx`，
  含 `zh-CN`/`zh-TW`/`en`/`fr`/`es`/`ru`/`ar` 七份 + `lang.*.json`），
  但 **VMLPrepares 里只有 4 处调它**，而且其中 4 个键
  （`rust.unsupported_binary` / `rust.unsupported_unary` / `rust.undefined_const` /
  `syntax.dict_set_mix`）**resx 里根本没有** ⇒ `Localization.Get` 回退成
  `[键名]` 字面量，用户看到的是 `[rust.unsupported_binary]: +`。
  2026-09-20 那一轮已就地改成内联中文（与其余 20 门一致）；
  **Localization 这条线在 VMLPrepares 里等于从没接上**，要复用得先把键补齐。

**做的时候必须守住的（都是被现有代码钉死的）**：

1. **ID 必须纯 ASCII + 下划线**：`VmlDiagnostics.StripCode` 是「把 ID 从正文摘出来」的
   唯一实现，正则写死 `\[([A-Za-z_][A-Za-z0-9_]*)\]\s*$`，形状一变就摘不掉，
   ID 会连同方括号一起留在气泡正文里。
2. **`error:` / `warning:` / `note:` 级别标签不许翻**：`VmlDiagnostics.SeverityOf` 按
   这三个字面量判严重度（`GccRx` 也按 `(error|warning|note)` 匹配），翻了就
   「警告被当成错误」。
3. **三段各有人解析，位置在前、级别在中间、ID 在句尾 —— 这个骨架不能动**：
   `tools/DiagProbe` 抽行号（`^(\S+?):(\d+):(\d+):`）、
   `scripts/vml-diag-probe/run.sh` 抽符号名与 `error:`、
   `scripts/vml-abi-probe/run.sh` 只看退出码与 `ABI-OK`。
4. **文案进 resx 之后，「上下文串」要变成带参模板**：现在
   `Expect(TokenType.Semicolon, "期望 ';' 在 break 后")` 是**一句整串**，
   而中/英/日的语序不同（英语是 `Expected ';' after break`）⇒
   必须拆成 `{0}`/`{1}` 的模板（`expect.token` / `expect.identifier` 已经是现成的两个键，
   可以照它们的样子扩）。
5. **别只做一半**：`ErrorCode.Unknown` 的调用点有几百处。要么逐门语言把
   `Error(message)` 全换成带 code 的重载，要么先只给「宿主会解析的那几族」加 ID ——
   **「一半有 ID、一半没有」比完全没有更难用**（下游按 ID 分支时会被漏掉的那半误导）。
6. 位置数字与它的拼接表达式**一个字都不要动**：`scripts/vml-diag-probe`（61/61）、
   `tools/DiagProbe`（未定义标识符 22/22）、`scripts/vml-out-probe`（30/30）
   全都在断位置，动了就是全红。

---

## 怎么用这份台账

- **发现新缺陷**：按上面的格式加一条（现象 / 最小复现 / **判据** / 状态 / 绕过）。
  **判据必须能跑** —— "改回去就复现、改对了就正常"才算证据。
- **修完**：状态改成 🟡 并注明补丁号/版本，**别删** —— 后来的人需要知道这里踩过坑。
- ⚠ **一条只观察到、还没缩小的缺陷，状态写清楚**（比如 C 的第一条），
  别把"猜测的原因"写成结论 —— 那会误导下一个来修的人。
