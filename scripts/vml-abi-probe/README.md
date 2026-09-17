# VML 调用约定判据

> 为 `docs/VML调用约定统一.md` 立的前置判据：**改动之前先钉死「现在是什么样」**，
> 改完逐条复跑，看的是「红变绿、绿不变红」，不是目测。

两套，各管一件事：

| 跑法 | 管什么 | 现状 |
|---|---|---|
| `scripts/vml-abi-probe/run.sh` | **C 前端**的调用约定（6 条栈/实参探针） | 6/6 绿 |
| `scripts/vml-abi-probe/run-langs.sh` | **跨语言**的实参顺序与栈漂移（13 条） | 7 绿 / 6 红 —— 红的都是**其余前端还没统一** |

```bash
scripts/vml-abi-probe/run.sh            # C 的 6 条
scripts/vml-abi-probe/run.sh p3 p5      # 按前缀挑几条
scripts/vml-abi-probe/run-langs.sh      # 跨语言的 13 条
scripts/vml-abi-probe/run-langs.sh cpp  # 按扩展名挑
```

---

## 跨语言那套（`run-langs.sh`）

**为什么 22 语言骨架全绿还不够**：`scripts/maui-vml-verify/corpus/` 的骨架里库调用都是单参、
或参数不参与判据，所以「实参反序」「压了不清的栈漂移」这两种缺陷它一条都照不出来。

两类探针（`langs/` 下，按文件名前缀分）：

- `abi.<ext>` —— 调 `ipow(2,3)` 并打印，判据 `ABI=8`。
  `ABI=9` = 实参整体反序；`ABI=1` = 第二个实参读成 0；`ABI=0` = 第二个实参压根没传到。
  选 `ipow` 是因为它**非交换**（`ipow(2,3)=8`、反序 `ipow(3,2)=9`）。
- `drift.<ext>` —— 反复调用库函数后，累加/循环变量必须一字未动。
  「压了不清」的每次调用净漏 4~8 字节，攒几次就把调用方栈帧踩花。
  判据是各语言自己那个和（表在 `run-langs.sh` 的 `DRIFT_EXPECT`）。

### 已知红项（= 还没做的活，别当噪音忽略）

| 探针 | 实测 | 指向 |
|---|---|---|
| `abi.cpp` | `9` | `CppCompiler/CodeGenerator.Expressions.cs` 的 extern/stdcall 分支**左→右**压栈；cdecl 分支的 R0-R3 镜像写在**压栈循环内**（会被后续实参求值冲掉） |
| `abi.java` | `1` | 调用点「第 1 个实参进 R0、其余右→左压栈」——是旧约定本体，第 2 个实参到不了 `[R12+16]` |
| `abi.rb` | `1` | 同上（`RubyCompiler/CodeGenerator.Expressions.cs:177-183` 左→右） |
| `abi.js` | 无输出 | `JavaScriptCompiler/CodeGenerator.Calls.cs:737-745` 左→右；且**同一前端内** `super`/`new` 两处却是右→左 |
| `drift.lua` | `0` | `LuaCompiler/CodeGenerator.Statements_B.cs:465-493` 的「压栈」**不动 R13**，实参只进 R0-R3 ⇒ 被调方读不到（第 5 个起静默丢弃） |
| `drift.m` | `2059` | ObjC 的多参外部调用（**既存缺陷**：重生成前基线也是 2059；栈漂移那部分已修） |

> `drift.m` 与 `drift.lua` 的数值在 `Lib/` 重生成**前后**都错 ⇒ 不是本次回归；
> `abi.cpp` / `abi.java` / `abi.rb` / `abi.js` 同理（骨架文件头早就记着这些形态）。
> 与之相对，`drift.d` / `drift.f90` / `drift.rb` 这三条在重生成**前是对的**（126 / 126 / 65528），
> 重生成后变错、补上调用方清栈后转绿 —— 那才是本次的回归，已修。

---

## C 的那套（`run.sh`）

判据只有一条，六条探针共用：

- **通过** = stdout 里有 `ABI-OK`，且没有 `VM execution cancelled`
- **失败** = 探针自己打印 `ABI-FAIL …` 然后挂死，被 `--timeout` 截成 `VM execution cancelled`

> ⚠ **不要只看探针打印出来的数**。栈漂移踩的正是打印路径自己要用的栈，
> 实测出现过「掩码印出 `0`、紧接着的分支却走了失败那一支」
> （同一个局部连续读两遍得到 `0` 和 `242`）。**「跑不完」才是最硬的那一路信号**，
> 打印出来的数只当线索用。

## 改动前基线（2026-09-17 实测，连跑 3 次结果一致）

| 探针 | 基线 | 钉的是什么 |
|---|---|---|
| `p1_args_order` | **PASS** | 6 个互不相同的实参，形参位置不错位 |
| `p2_args_expr` | **PASS** | 实参是带运算的表达式时位置仍对（历史上的寄存器覆盖伤） |
| `p3_local_after_lib` | **FAIL**（失配 14） | **调库之后调用方的局部必须一字未动** |
| `p4_local_declared` | **PASS** | 同 p3，但库函数声明了 `__stdcall`（现状的变通） |
| `p5_array_after_lib` | **FAIL**（掩码 34） | 调库之后数组下标仍然算得对 |
| `p6_arg_types` | **FAIL**（第 2 个形参） | `char` / `short` / `double` 混合类型实参 |

**改造目标：六条全绿**（p1/p2/p4 保持绿，p3/p5/p6 由红转绿）。

## p3 / p4 这一对是重点

它们做的是同一件事，只差一句 `__stdcall` 声明：

- `p4`（声明了）通过 —— 因为 C 前端走 stdcall 路径就不再加「调用方清栈」
- `p3`（没声明）失败 —— 前端走 cdecl 路径，**调用方与被调方各清一次栈，每次调用净 +4 字节**

根因是**库里两套栈清理约定并存**：

| 家族 | 尾声 | 谁清栈 | 与 C 前端 cdecl 路径 |
|---|---|---|---|
| `vmlui`（`ui_rect` …） | 裸 `ret` | **调用方** | 一致 ✓ |
| `builtins`（`println_int` / `print_str` …） | `move R1 @13; add R13 #8; push R1; ret` | **被调方** | **不一致 ✗** |

`builtins` 家族还少一半：它们**参数直接吃 R0、从不读 `[R12+12]`**
（`syscall #6` 用的是寄存器里的值），而 `vmlui` 家族是老老实实 `move R0 [R12+12]` … 逐个从栈取。

所以现状的可用性完全建立在「每个调用方都记得写 `__stdcall`」上 ——
`scripts/maui-vml-verify/corpus/c/skel.c` 的头注释里记的正是这个变通。
**但那对 22 种语言前端、对任何没读到那份头的程序都不成立。**

## 两条硬约束

1. **探针要够「敏感」**：漂移是 **+4/次**，要爬到某个局部头上才踩得到。
   「变量少 + 调用少」的写法**测不出来** —— `p3`/`p5` 的第一版就是这么假绿的。
   现在它们都用「多个局部 + 至少 4~8 次库调用」，让漂移量必定穿过整个栈帧。
   `p3` 还多加了一个自洽性判据：一旦循环变量被踩，失配计数会**超过循环次数本身**
   （实测 14 > 8）—— 这种「不自洽的数」比任何单点断言都硬。

2. **判据必须进仓库**：上一轮用过的 `.scratch/vmlhost/tests/argorder.c` 已经随着
   临时目录消失过一次，导致那份基线无从复现。这套探针落在 `scripts/` 下、受版本管理。

## 跑法依赖

桌面 `scripts/vmlcli`（与手机端逐字等价的编译+运行流水线，约 1 秒/条，而不是打 APK 的 2 分钟）：

```bash
dotnet build scripts/vmlcli -c Release    # 改了 third_party/vml 后必须重编才生效
```

它 2026-09-17 从 `.scratch/vmlcli/`（临时目录，未跟踪也未 gitignore，随时会丢）**转正**到这里——
这套判据全靠它跑，工具不能住在会被清掉的地方。
