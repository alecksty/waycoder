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
| ~~`abi.cpp`~~ | **`8` ✓** | 已修：`CppCompiler` 的四条分流（extern/stdcall/fastcall/cdecl）合成一条 —— 右到左压栈 + 镜像移出压栈循环 + 一律调用方清栈；被调方那边同样收口（形参只有 `R12+12+4i` 一种布局、尾声裸 `pop R15`） |
| ~~`abi.java`~~ | **`8` ✓** | 已修：调用点原先「第 1 个实参进 R0、其余右→左压栈」（CCv2 寄存器约定，与库完全不兼容——库里 C 编译出来的函数从 `[R12+12+4i]` 取参，R0 里那个它根本看不到）。改成全部右到左压栈 + 全部由调用方清；被调方也统一成每个形参都从 `[R14+16+4i]` 取 |
| ~~`abi.rb`~~ | **`8` ✓** | 已修：调用点改右到左 + **包装器改从自己的栈帧读实参**（见下「总根源」） |
| ~~`abi.lua`~~ | **`8` ✓** | 已修：Lua 的「压栈」原先**不动 R13**（假压栈），改成真压 + 调用方清；被调方也改成每个形参都从 `[R12+12+4i]` 取（原先从 R0-R3，第 4 个之后还不支持） |
| ~~`abi.js`~~ | **`8` ✓** | 已修：根因是 `naming.json` 给 javascript 的 `PrimaryPrefix` 为空，而它的 `LabelStyle` 是 camelCase ⇒ **单词名函数**（`ipow`/`pow`/`abs`/`sqrt`）的主标签与 C 符号名同名，`LABEL ipow … CALL ipow` 成了自调用。补上 `js_` 前缀即可（java 同病，一并补 `java_`）。GenLib 里加了护栏：真实存在的函数一旦撞名就告警 |
| ~~`drift.lua`~~ | **`126` ✓** | 已修：**与调用约定无关** —— Lua 的数值 `for` **要求循环变量事先用 `local` 声明过**，否则循环体一次都不执行。见下 |
| `drift.m` | `2059` | ObjC 的多参外部调用（**既存缺陷**：重生成前基线也是 2059；栈漂移那部分已修） |

### ⚠ 总根源：`Lib/{lang}/**` 的包装器仍在用**寄存器**收参数

`abi.rb` / `abi.js` / `drift.lua` / `drift.m` 这四条追下去都汇到同一处 ——
GenLib 生成的包装器（`Lib/ruby/math.vml`、`Lib/javascript/math.vml` …）长这样：

```asm
func_ipow:
    jmp ruby_ipow
ruby_ipow:
    push R1        ; ← 第 2 个参数从 **R1** 取
    push R0        ; ← 第 1 个从 **R0**
    call lib_math_ipow
    add R13 #8
    ret
```

`EmitPushParam`（`tools/GenLib/Program.cs`）发的是 `PUSH R{i}` —— 这是**旧寄存器 ABI**。
被调方 `lib_math_ipow` 从 `[R12+12]`/`[R12+16]` 读栈，于是拿到的是 R0/R1 里的残留
（实测 `ipow(2,3)` 得 `1` = `ipow(x, 0)`，因为 R1 恰为 0）。

**这解释了「为什么单参调用看着都对、多参才露馅」**：单参时 `R0` 恰好等于刚求值完的第 1 个实参；
多参才需要 R1-R3，而那是垃圾。C 前端能跑，只因为它在调用点做了 **R0-R3 镜像**（commit 5655c305）。

**修法**（下一步）：让包装器**从自己的栈帧读实参**而不是从寄存器 ——

```asm
LABEL ruby_ipow
    push R15
    push R12
    move R12 R13
    move R0 [R12+16]      ; arg1
    push R0
    move R0 [R12+12]      ; arg0
    push R0
    move R0 [R13+0]       ; 镜像 arg0..arg3 给下一层的内联汇编用
    move R1 [R13+4]
    CALL ipow
    ADD R13 #8
    move R13 R12
    pop R12
    pop R15
    RET
```

这一改的好处是**包装器不再关心调用方的约定**（只认「实参在栈上、右到左」），
`Lib/` 里那 543 处 `asm("SYSCALL #6")` 也由包装器这一层的镜像喂饱。
⚠ 代价是 `Lib/{lang}/**` 要整体重生成一次（1933 个文件），改法与验证路径与上一轮相同
（`GenLib -A` + `scripts/check-vml-patches.sh` 的「可重生成」判据）。

### `drift.lua` 的真实根因：Lua 的 `for` 循环变量必须先 `local` 声明

它跟调用约定**没有关系** —— 顺藤摸下去是一条纯粹的 Lua 前端缺陷。实测（`ipow` 只是恰好被写在循环里）：

```lua
function main()
    local a = {1, 2, 3, 4}
    local s = 0
    for i = 1, 4 do  s = s + a[i]  end
    print(s)          -- 0     ✗ 循环体一次都没执行（循环里的 print 也不输出）
end
```

**只差一句前置声明就对**：

```lua
function main()
    local a = {1, 2, 3, 4}
    local s = 0
    local i = 0       -- ← 就这一句
    for i = 1, 4 do  s = s + a[i]  end
    print(s)          -- 10    ✓
end
```

`scripts/maui-vml-verify/corpus/lua/skel.lua` 里恰好有 `local i = 0`（语料作者的习惯写法），
所以 22 语言骨架一直是绿的 —— **循环变量没预声明**这条路径从来没被覆盖。

**根因**（读生成的 VML 看出来的）：没预声明时循环变量落在**数据段的全局 `var_i`**，而那条路
对同一个标签有两种解读 —— 循环头发 `move R1 var_i`（把标签当**地址**取），循环体读 `i` 发
`move R0 [var_i]`（按地址**取值**）。于是 `R1` 拿到一个远大于上界的地址，`cmp/jg` 立刻跳出。

**已修**：`GenerateForStatement` 里循环变量**无条件分配一格局部槽**（不管此前是否在符号表里），
两种写法从此走同一条 `[R12-off]`。`drift.lua` 由 `0` 转 **`126`**。

### 顺带挖出的一条独立缺陷：`ParseFunctions` 把**注释**当函数签名

给 `abi.js` 加护栏（标签与 C 符号名同名就告警）之后，csharp 一次报出 `Arrays`、`Manipulation`、
`CRC`、`GetDate`、`GetTime` …… 一查全是**虚构条目** —— `GenLib.ParseFunctions` 的正则
（`RET NAME(params)`）没先剥注释，于是 `array64.c` 第 4 行的

```c
// VML Shared Array64 Library — 64-bit Integer Arrays (long* with long indices)
```

被读成「返回 `Integer`、函数名 `Arrays`、参数 `long* with long indices`」。
后果：`funcMap` 与 `Lib/modules.json` 里多出一批不存在的函数，每个还会生成一个
`LABEL x … CALL x` 的**自调用死包装器**（不排除将来和真标签撞上 —— 与上面那族同一个机制）。

**修法**（下一步）：`ParseFunctions` 先剥 `//` 与 `/* */` 注释再匹配；
并清掉 `modules.json` 里已收进来的虚构条目。**本次只告警、未修**，护栏里也已写明
「这条是虚构条目、可以忽略」。

> 审计（覆盖全部 22 个前端）还查出几条**探针没覆盖**的同类问题，一并记在这里当活单：
> Python 的**被调方**仍从 R0-R3 拷形参（`Statements_A.cs:61-71`）而调用点只压栈 ⇒ 只有 arg0 侥幸正确；
> Kotlin 的被调方取参公式（`CodeGenerator.cs:90-96`）与调用点的压栈方向**相反**；
> Java 的被调方 param0 读 R0；Swift 的被调方前 4 参读 R0-R3；
> Go 的方法调用把 receiver 压在最前、落在最高地址 ⇒ 被当成最后一个形参；
> Lua 的实参**第 5 个起静默丢弃**；Basic/Pascal 的 builtin 调用仍有「被调方清栈」残留。

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
