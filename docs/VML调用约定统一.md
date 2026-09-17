# VML 调用约定统一到「微软 stdcall」—— 决定与实施方案

> 2026-09-17 用户拍板。**这是一次架构级重构，不是修补。**
> 前情：22 种语言已全部跑通游戏骨架（补丁 0019–0033），但排查过程中反复撞到
> **VML 自身调用约定不一致** —— 它既解释了 `print_*` 栈漂移，也是一整类
> 「同一段代码有时对有时错」缺陷的温床。

## 一、用户决定（两条，都已明确）

1. **参数传递：全部参数右到左压栈，一个都不走寄存器**（= 真·微软 stdcall 的传参形态）。
   被调方清栈。**放弃前 4 参走 R0–R3 的快速路径。**
2. **`Lib/` 用当前前端整个重新生成** —— 一次性消掉「两套栈清理约定并存」的分裂。

## 二、为什么这是对的（证据）

### 现状：VML 内部至少三种约定

| 位置 | 形态 | 证据 |
|---|---|---|
| C 前端**寄存器路径** | 右到左压栈 | `CCompiler/CodeGenerator.Expressions.Calls.cs:225` 循环 `i = argCount-1; i >= 0; i--`，注释明写「压栈是从右到左，所以 arg[k] 落在 `[R13 + sum(size[0..k-1])]`」 |
| C 前端 **`case CallingConvention.Stdcall:`** | **左到右** | 同文件 `:284` 循环 `i = 0; i < Count; i++`，注释明写「全部参数从左到右压栈」 |
| `Lib` 的 `vmlui` 家族（`ui_rect` 等）| 右到左、**全从栈读** | `Lib/shared/vmlui.vml:561` 读 `[R12+12]→R0=x(arg0)`、`[R12+16]→R1=y` … |
| `Lib` 的 `builtins` 家族（`print_int` 等）| 值在 **R0**，但**仍要求栈上留一个槽** | 收尾蹦床 `move R1 [R13]; add R13 #8; push R1; ret` |

**同一个前端里两条注释直接互相矛盾。** 而设备上游戏画得是对的 ⇒ 实际生效的是**右到左**那条。

### 关键结论：`push` 方向与 arg0 位置

- `VMLRuntime.Instructions.cs:452`：`sp -= 4; SetMemory(sp, value)` ⇒ **栈向下生长**（同 x86）
- 三条互证 ⇒ **arg0 在最低地址 = 最后压入 = 右到左**，且 `[R12+12]` 恒为 arg0

**这决定了改造的低风险路线**：`Lib` 里所有「从 `[R12+12]` 起读参数」的包装**布局不变、不用改**；
真正要动的是**前端的调用生成**与**`Lib` 里读寄存器的那些**（`builtins` 家族）。

## 三、与微软 stdcall 的逐条对照（改造目标）

| 维度 | 微软 stdcall | 现状 | 目标 |
|---|---|---|---|
| 压栈方向 | 右 → 左 | 两条相反路径 | **右 → 左，唯一一条** |
| 谁清栈 | 被调方 | 被调方（蹦床 `add R13 #N`）| 被调方 |
| 参数位置 | **全部走栈** | 混用（R0–R3 / 栈 / 两者兼有）| **全部走栈** |
| 名称修饰 | `_name@N` | 前缀 `func_`/`method_`，链接器剥离 | **本方案不动**（VML 的标签体系是另一套，`@N` 与汇编语法冲突） |

> ⚠ **只对 32 位 stdcall 成立**。x64 Windows 没有 stdcall（前 4 参进 RCX/RDX/R8/R9 + 32 字节 shadow space + **调用方**清栈）。
> 若将来 `VMLRuntime.Syscall.FFI.cs` 的 `NativeCallEx` 要在 x64 上调真 DLL，**不能**套这套。

## 四、实施顺序（**每步都要能独立验证，不许一口气改完再试**）

### 第 0 步：前置（**必做，否则白干**）
**设备验收 agent 正在跑**（验证补丁 0019–0033 在真机上的 22/22）。
**它跑完之前不许改 `third_party/vml/**`** —— 否则又是一份「跨构建混在一起」的基线（本仓已踩过一次）。

### 第 1 步：先立判据（在改动之前）
写一个**调用约定探针**，钉死「arg0 落在哪、谁清栈」：

```
C:  void probe(int a,int b,int c,int d,int e,int f);
    probe(11,22,33,44,55,66);   // 每个值互不相同
```
- 汇编侧：断言调用点的压栈顺序是 `66,55,44,33,22,11`（右到左）
- 运行侧：`probe` 内部把每个形参原样报回来（走 `ui_beep` 频率或返回值，**别经 stdio**）
- 存量基线：本仓 `.scratch/vmlhost/tests/argorder.c` 有先例（`probe(p+3*k, q+3*k, 3*k)` 曾报 R0=101 R1=**101** R2=72）

### 第 2 步：改前端（22 个，**方向统一为右到左 + 全部走栈**）
- **C 系（C/C++/ObjC）**：`CodeGenerator.Expressions.Calls.cs` 的寄存器路径改成「全部右到左压栈」；
  删掉 `case CallingConvention.Stdcall` 那条左到右分支（或让它与 Pascal 合并 —— **注意：现在这两条循环方向是一样的，本身就是个待查的疑点**）
- **其余 19 个前端**：各自查一遍调用点的压栈方向与是否清栈，统一成同一形态
- **`vmlui.vml` 的包装不用动**（它们本来就右到左从栈读）
- ⚠ **`print_bool` 读 `[R12+12]`** 已经是对的形态；**`print_int` 读 R0** 要改成从栈读

### 第 3 步：重新生成 `Lib/`
- 源头是 `Lib/shared/src/*.c`（`builtins.c` 等）⇒ 用**改完的 C 前端**重新编译成 `.vml`
- 这一步**必须在上游 VML 仓库做**（`Lib/` 受 `sync.sh` 管辖，本地改会被 `--delete` 冲掉）；
  若只能本地做，则结果要**整个进补丁**（参考 `0033` 给 `Lib/lua/luatable.vml` 新增文件的做法 —— ⚠ **新文件要 `git add -N` 才进得了 `git diff`**）
- 目标：消掉「764 个被调方清栈 vs 446 个调用方清栈」（数字来自 CLAUDE.md §⑨）

### 第 4 步：回归（**判据要能跑，不是目测**）
1. `scripts/vmlcli` 跑 22 条骨架语料 → 仍须 **22/22 出 `SKEL-SUM=14`**
2. **`print_*` 漂移探针**（现成的，见下）→ 必须消失
3. 上游 `Examples/` 全量（cpp 12 / lua 9 / rust 8 / ruby 12 / dart 12 / fortran 11 …）编译+运行
4. `scripts/check-vml-patches.sh` 全绿
5. 桌面自测 `dotnet run -- --test`（WayCoder 不引 VML，跑的是护栏那几条）
6. **真机全量**（`scripts/maui-vml-verify.sh`）

## 五、现成的回归资产（别重写）

- **`print_*` 漂移探针**：`/tmp/pyprobe.py`（会话内）与固化版见下 —— 修前 `T2/T4/T5` 全为 `0`，
  修后应分别为 `50 / 60 / 40`。**这是判定「漂移是否消失」最省事的判据**：

```python
a = [10, 20, 30, 40]
a[1] += 5
print_str("T1="); println_int(a[0] + a[1] + a[2] + a[3])   # 105
a[3] += a[0]
print_str("T2="); println_int(a[3])                         # 本轮修后应为 50
b = a[99]
print_str("T3="); println_int(b)                            # 0
a[99] = 777
print_str("T4="); println_int(a[0] + a[3])                  # 60
print_str("T5="); println_int(a[-1])                        # 40
```

- **单点隔离版**（每个用例只打一次，避免被漂移污染）：`/tmp/p_{plain,compound1,compound2,oobread,oobwrite,negidx}.py`
  —— 上一轮实测全对（77 / 25 / 50 / 0 / 10 / 40），**改造后必须仍然全对**
- **`--vml <文件>`** 导出链接后的汇编（`scripts/vmlcli` 的参数）—— 查压栈顺序用这个
  ⚠ 实测：产物里**搜不到用户函数名**（`ToString()` 做死代码消除 + 库里同名标签很多），
  所以**别指望从 dump 里找用户函数**，要查方向就直接读前端的生成代码

## 六、风险与已知坑

1. **禁改窗口**：设备验收跑完前不许动 `third_party/vml/**`（见第 0 步）
2. **`Lib/` 重新生成是上游的事**；本地做要整个进补丁，且**新文件要 `git add -N`**
3. **别一刀切**：`Lib` 里 `newline` 根本没有 `add R13 #8`、`print_double/print_long` 在 `io64.vml` 而不是 `builtins.vml`、
   `print_bool` 走栈读而 `print_int` 走 R0 —— **逐个核对过才不会把没病的改坏**
4. **`CompilerOptionsContext.RunWith` 对这 22 个编译器是空转**（`CompilerBase.SyncToContext()` 在每个入口无条件覆盖它）——
   别指望从外面传编译器选项
5. **`Lib/` 里 `add R13 #N` 的 N 各不相同**（`print_int` 是 `#8`、`ui_rect` 是 `#12`）——
   改造时**逐个函数核对退栈量**，不要按印象写
6. **改完必须重打 APK**：补丁改了 `Lib/` 或前端，手机端不重装就还是旧行为

## 七、这件事的收益（值不值得做）

- 消掉 **`print_*` 栈漂移**（现在任何打了两次以上 `print` 的程序局部变量就会读成 0 ——
  实测 Python 前端 5 次打印后 `T2/T4/T5` 全为 0；**C 一样坏**）
- 消掉一整族「同一段代码有时对有时错」的缺陷（CLAUDE.md 已记过两例：
  `d9ee4b53` 查到根因是「Lib 里两套栈清理约定并存」；`e37db3a4` 的「Swift 第 5 参起全错」）
- 让 22 种语言**真正**共用一套 ABI，而不是「碰巧都对」

---

# 实测记录（2026-09-17，第 1~2 步）

> 下面全部是**跑出来的**，不是推断。复跑方式见 `scripts/vml-abi-probe/README.md`。

## 第 1 步：判据已立（`scripts/vml-abi-probe/`，6 条，约 1 秒/条）

判据只有一条：**跑得完 = 通过**。刻意不看探针打印出来的数 —— 栈漂移踩的正是
打印路径自己要用的栈，实测出现过「掩码印出 0、紧接着的分支却走了失败那一支」
（同一个局部连读两遍得 `0` 和 `242`）。

| 探针 | 改动前 | 现在 | 钉的是什么 |
|---|---|---|---|
| `p1_args_order` | PASS | PASS | 6 个实参形参位置不错位 |
| `p2_args_expr` | PASS | PASS | 表达式实参（历史寄存器覆盖伤） |
| `p3_local_after_lib` | FAIL | **FAIL** | 调库后局部必须一字未动 |
| `p4_local_declared` | PASS | PASS | 同 p3，但声明了 `__stdcall` |
| `p5_array_after_lib` | FAIL | **FAIL** | 调库后数组下标仍算得对 |
| `p6_arg_types` | FAIL | **PASS** | `char`/`short`/`double` 混合实参 |

## 根因（实测确认）

链接进来的 `println_int`（`Lib/shared/src/console.c:73`；`builtins.c:41` **也**定义了同名，
链接器选了 console 那份）是**被调方清栈**：

```asm
println_int:
        push R15 / push R12 / move R12 R13
        syscall #6            ; 参数直接吃 R0，从不读 [R12+12]
        ...
        move R1 @13           ; 存返回地址
        add R13 #8            ; ← 被调方清掉「返回地址 + 1 个参数槽」
        push R1 / ret
```

而调用点（无论新旧）都发 `add R13 #4` ⇒ **每次调用净 +4 字节**。SP 一路爬进调用方栈帧，
把局部覆盖掉。对照 `ui_rect` 是裸 `ret`（调用方清栈）——**同一份程序里两套约定并存**。

## 第 2 步：C 前端已改（4/6 绿，**22/22 语言全绿**）

`CodeGenerator.Expressions.Calls.cs` / `CodeGenerator.Functions.cs`：

- 删掉 cdecl/stdcall/pascal/fastcall/basic **五条分流**，统一成「全部实参右到左压栈、
  一个都不装 R0-R3、调用方清栈」。`__stdcall` 等修饰符仍能解析但**不再影响代码生成**。
- 序言删掉「把 R0-R3 存回参数槽」那一段。
- 尾声一律裸 `ret`（删掉 stdcall 的被调方清栈块）。
- **顺带修掉 p6**：形参槽改成「每标量一个 4 字节槽、64 位占两格」，
  由 `ParamStackBytes` 一条规则同时服务调用点与被调方。原先被调方按
  `AlignmentForSize(1)=1 / (2)=2` 的自然大小分配，与调用方的「每参数 4 字节」对不上，
  形参整体错位（`mix(char,short,int,double,int)` 从第 2 个起全错）。
- 顺带修掉间接调用的一个潜伏 bug：被调地址原先存 R8，而实参求值会用 R0-R11 当临时寄存器
  （实测 `main` 的产物里就在用 R2/R5/R8/R11），现在改成**压栈保存**、调用前取回。

**22 种语言骨架全部通过**（`scripts/vmlcli` 逐条跑，判据 `SKEL-SUM=14`）。

## 剩余阻塞：Lib 那一侧，**范围比原方案预估的大**

方案原写「用当前前端把 `Lib/` 整个重新生成」。实测下来这一步有三条没想到的代价：

1. **Lib 源码本身依赖寄存器 ABI。** `Lib/shared/src/*.c` 里有 **514 处 `asm(`，其中 197 处涉及 R0**。
   典型如 `builtins.c`：
   ```c
   __stdcall void println_int(int val) {
       asm("SYSCALL #6");    // ← 形参 val 在 C 里从未被引用，靠的就是「第一个形参在 R0」
       asm("MOVE R0 #10");
       asm("SYSCALL #4");
   }
   ```
   **重新生成救不了它** —— 用新前端重建出来的 `println_int` 仍然只有 `syscall #6`，
   因为源码自己就没读 `[R12+12]`。这类地方得**改源码**。

2. **仓库里那份 `Lib/**/*.vml` 用当前源码复现不出来（是过期的）。** 做对照实验：
   用**未改动**的前端重建 `builtins.vml`，与仓库现有文件仍差 **480 行** ——
   差在标签格式（`L94240004` → `L_94240004`）、局部布局（`[R12-4]` → `[R12-8]`，
   即我们自己那个 `__asm_result_slot` 修复）、以及**整批 `.linked` 指令消失**。
   「整个重新生成」会把这些无关改动一并拖进来，每个模块几百行，且 `.linked` 的去留未查清。

3. **机械剥离被调方清栈能全绿，但会打断 thunk 链。** 试过：把
   `move R13 R12 / pop R12 / pop R15 / move R1 @13 / add R13 #N / push R1 / ret`
   机械改写成裸 `ret`（65 个文件、719 处）—— **6/6 判据立刻全绿**。
   但 22 语言掉到 19/22（csharp / fortran / forth 回归）。原因：
   `Lib/c/console.vml` 里的 thunk **自己也依赖被调方清栈**：
   ```asm
   LABEL c_println_int
       PUSH R0              ; 为「被调方会弹掉一个参数槽」而多压的一格
       CALL println_int
       RET                  ; ← 它指望 println_int 把那一格弹掉
   ```
   `println_int` 改成不弹之后，`RET` 弹掉的就是自己刚压的参数 ⇒ 跳飞。
   **Forth 前端里也有同款变通**（`ForthCompiler/CodeGenerator.Operations.cs` 的注释白纸黑字写着
   「压栈 + 被调用方弹掉 = 净 0」，所以它故意多压一个参数抵消）。

### 更正：`.vml` 是生成物，别手改（用户 2026-09-17 指出）

一度试过「外科手术」：直接改 `Lib/**/*.vml`，把被调方清栈的尾声剥成裸 `ret`。
**这条路是错的**，已撤回。理由：

- `Lib/**/*.vml` 是**生成物**。手改它们等于把生成器的输出改脏，
  下次重生成就没了；而且 `sync.sh` 的 rsync 列表**含 `Lib` 且带 `--delete`**，
  上游一同步照样冲掉。
- 剥完会打断 thunk 链（`Lib/c/console.vml` 的 `c_println_int` 是
  `PUSH R0 / CALL println_int / RET`，那一格原本由被调方弹掉）——
  22 种语言会掉到 19/22。而在 `.vml` 层修 thunk 又是在改生成物，越陷越深。

**真正要动的只有一处：`Lib/shared/src/*.c` 里的内联汇编。**

`asm("SYSCALL #6")` 这类块把「第一个形参在 R0」**烙死在了源码里** ——
`builtins.c` 的 `println_int(int val)` 里 `val` 从未被引用，靠的就是那条寄存器 ABI。
换 ABI 之后这些地方必须显式从 `[R12+12 + 4k]` 取形参。

### 正确的重生成链路

| 环节 | 位置 | 说明 |
|---|---|---|
| 共享库源码 | `Lib/shared/src/*.c` | **要改的就是这里的 `asm(...)`**；514 处，197 处涉 R0 |
| 共享库 `.vml` | `Lib/shared/*.vml` | 由 `Lib/shared/rebuild_shared.sh` 从上面生成（用 C 前端） |
| 各语言 native 包装 | `Lib/<lang>/*.vml` | 由 **GenLib** 生成 —— 它**只在上游**，不在 `sync.sh` 的 rsync 列表里 |

⚠ **GenLib 只存在于上游仓库**（`~/Desktop/source/vml/vml/tools/GenLib`）。
所以这一整套**必须在上游做完再同步下来**，本地改 `Lib/` 会白改。

> 而且 CHANGELOG 里早就记着一条同族问题：
> 「Forth Conv 测试 — Forth↔C **调用约定不匹配** — 包装器 `PUSH R0` 传递栈顶值…
> 需修改 **GenLib 包装器**或 Forth 编译器以正确处理外部 C 函数调用」。
> 也就是说这次要统一的东西，上游自己也撞到过、只在包装器层面绕开了。

### 因此剩余工作是

1. **上游**：把 `Lib/shared/src/*.c` 的内联汇编从「形参在 R0-R3」改成「从 `[R12+12+4k]` 读」。
2. **上游**：GenLib 的包装器生成规则一并改（它的 `PUSH R0 / CALL x / RET` 就是为旧约定写的）。
3. 重生成 `Lib/`（shared + 各语言包装），同步下来，按 `scripts/check-vml-patches.sh` 补 `patches/`。
4. 删掉前端里的补偿变通（`ForthCompiler/CodeGenerator.Operations.cs` 的额外 `PUSH` 有白纸黑字的注释）。
5. 重打 APK 才到得了手机（`scripts/make-vml-lib.sh`）。

> ⚠ 顺带查清的一条：我加的 `scripts/vmlcli --rebuild-lib` 能重生成 `Lib/shared/*.vml`，
> 但**不产出 `.linked` 抬头**，与仓库里现有那份对不齐；而现有那份本身也**用当前源码复现不出来**
> （对照差 480 行）。所以**别拿它当真源**去批量重生成，`.linked` 的去留要先问上游。

---

# 配方已验证（2026-09-17）

## `${}` 的确定语义

实现在 `CCompiler/CodeGenerator.Statements.cs:68`（`GenerateAsmStatement`）：

```csharp
int regIdx = 0;                       // ← 每条 asm 语句各自从 0 开始
while ((dollar = code.IndexOf("${", searchStart)) >= 0) {
    if (variables.TryGetValue(varName, out int offset)) {
        instructions.Add(MOVE R{regIdx}, <变量的内存>);   // 载入
        code = code[..dollar] + $"R{regIdx}" + ...;       // 文本里换成 R{regIdx}
        regIdx++;
    }
}
```

三条确定行为（都实测过）：

1. **它会从栈上载入形参** —— 形参在 `variables` 里，`FormatVarOffset` 给出 `[R12+12+4k]`。
2. **`regIdx` 是每条 `asm` 语句独立计数的** ⇒ 拆成多条 `asm()` 时，每条的第一个 `${}` 都载入
   **R0**，后者覆盖前者（实测 `24 = 12+12`）。**一条 asm 里写多个 `${}` 才拿到 R0、R1、R2…**
3. **只做载入，不做存回** —— 存结果仍用 `MOVE [_全局], R0`（Lib 源码本来就这么写）。
   ⚠ 分隔指令**不能用 `;`**（那是 VML 的注释，实测后半截被整段吃掉）。

## 改造配方（已验证）

```c
asm("SYSCALL #6")          /* 旧：靠「第一个形参在 R0」 */
asm("SYSCALL #6, ${val}")  /* 新：展开成 move R0 [R12+12]; syscall #6 */
```

`SYSCALL #N` 分支会**忽略尾随文本**（`CodeGenerator.Statements.cs:106` 的
`numStr.Split(',', ' ', ';')[0]`），所以占位符只是顺带把载入发出来 —— 正好合用。

## 实测效果

只改 `Lib/shared/src/console.c` 的 `print_int` / `println_int` 两个函数
（`asm("SYSCALL #6")` → `asm("SYSCALL #6, ${val}")`），用
`scripts/vmlcli --rebuild-lib` 重生成 `Lib/shared/console.vml`：

| | 改前 | 改后 |
|---|---|---|
| `p1`~`p6` | 4/6（p3/p5 红） | **6/6 全绿** |
| 生成的 `println_int` | 靠 R0，不读 `[R12+12]` | `move R0 [R12+12]` 后 `syscall #6` |

**`p3`/`p5` 正是「栈漂移」那两条 —— 转绿说明配方确实消灭了漂移本体。**

## 两个必须记住的操作事实

1. **本地迭代回路可用**：`--rebuild-lib` 重生成的模块**不带 `.linked` 抬头**，
   但照样能跑（实测 C 骨架仍出 `SKEL-SUM=14`）⇒ 可以先在本地
   「改源码 → 重生成 → 跑判据」快速试，配方定了再回上游做正式重生成。

2. ⚠ **不能只改模块里的一部分函数**：把一个模块重生成、但里面还有没移植的函数时，
   22 语言会从 22 掉到 11（实测）。原因有两层 —— 重生成会换掉整个文件的标签格式与
   `.linked`；且同一模块内**新旧 ABI 混着**必然对不上。
   **移植要以「模块」为单位做完，不能跨模块零敲碎打。**

---

# 排期结论：这件事**不能分模块做**（2026-09-17 实测）

## 实验（三个状态各跑一次 22 语言骨架）

| 状态 | 22 语言 |
|---|---|
| 现状（Lib 未动） | **22 通过** |
| `console.c` **未移植**，只把 `console.vml` 重生成 | **20 通过**（掉 csharp / forth） |
| `console.c` **整模块移植**（13 个函数），再重生成 | **11 通过**（掉 11 个） |

## 破坏分成两层，而且可分离

**第一层：重生成本身（-2 个语言）。** 源码一字不改、只重生成 `console.vml`，
csharp 与 forth 就掉。原因是重生成会换掉标签格式（`L94240004` → `L_94240004`）、
局部布局（我们那个 `__asm_result_slot` 修复），并**丢掉 `.linked` 抬头**。
这一层是**工具链口径问题**，靠「在上游用正规流程重生成」解决，不是 ABI 的账。

**第二层：新旧 ABI 混着（-9 个语言）。** `console.vml` 移植成新约定之后，
它**调用的其它模块**（`builtins` / `io` / `crt` …）与**各语言的前端**还停在旧约定上
⇒ 互相调不通。

## 所以

⚠ **移植必须以「整个 `Lib/` + 全部前端」为单位一次做完**，不能一个模块一个模块地推。
每完成一个模块就跑 22 语言，看到的会是「掉一批、但掉的是别人」，这个信号没有诊断价值。

正确姿势是把工作拆成**两段、各自内部一次性完成**：

1. **上游**：把 `Lib/shared/src/*.c` **所有模块**的内联汇编一次移植完
   （配方已验，见上一节），并顺带解决生成口径（`.linked` 抬头、标签格式）。
   GenLib 的包装器生成规则同时改。
2. **本仓**：`Lib/` 同步下来后，一次性删掉 22 个前端里为旧约定写的补偿代码
   （Forth 那处有白纸黑字的注释），再重打 APK。

中间态（一个模块新、其余旧）是**没有可用基线**的，只能靠判据（`scripts/vml-abi-probe/`）
而不是 22 语言骨架来判进度 —— 判据是**运行期**行为，不受模块间新旧混杂影响：
上面第二次实验里判据就已经是 **6/6 全绿**了。

---

# 上游重生成：具体命令与要改的两处（2026-09-17 实地查证）

## 关键前提：上游的 `CCompiler` 是 `Exe`

`~/Desktop/source/vml/vml/VMLPrepares/CCompiler/CCompiler.csproj` 是 `<OutputType>Exe</OutputType>`；
**我们 vendored 的那份是 `Library`**（vmlcli 与 WayCoder.Maui 要以项目引用它）。
所以 `Lib/shared/rebuild_shared.sh` 那种靠 `dotnet run` 的脚本**只在上游跑得通**，
在本仓会报「可运行的项目应面向可运行的 TFM 且 OutputType 为 Exe」。
（本仓那份 `scripts/vmlcli --rebuild-lib` 是为绕开这一点写的，走同一个 `CompileFile` API。）

## 一键全流程

`tools/GenLib` 自带整条链 —— 编译 C→VML、生成各语言模块包装、聚合文件、源文件绑定：

```bash
cd ~/Desktop/source/vml/vml
dotnet run --project tools/GenLib -- -A          # 全流程
dotnet run --project tools/GenLib -- -A -l python # 只做某个语言
dotnet run --project tools/GenLib -- -b           # 只编译过期的 C 源
```

`-b` 是**进程内调用 CCompiler**，所以本仓对 C 前端做的调用约定改动**直接生效**
（前提是上游那份 `VMLPrepares/` 也是新版 —— 见下面「待办」）。

**所以「重生成 Lib」不是一条复杂工序，就是这一条命令。** 难的部分不在跑，在于跑完之后
本仓要能接得住（同步 + 补丁 + 22 前端 + 重打 APK）。

## GenLib 必须改的两处

### ① 清栈：`Program.cs` 的包装生成（约 :387）

```csharp
sb.AppendLine($"    CALL {funcName}");
if (isCdecl && totalArgBytes > 0)              // ← 现在只有 cdecl 才发调用方清栈
    sb.AppendLine($"    ADD R13 #{totalArgBytes}");
sb.AppendLine("    RET");
```

统一约定下**一律由调用方清** ⇒ 去掉 `isCdecl &&` 这个条件即可。
这正是那批 `PUSH R0 / CALL x / RET` thunk（`c_println_int` 等）的成因 ——
它们不发清栈，是因为**指望被调方弹掉自己压的那格**。

### ② 压参宽度：`EmitPushParam` 按自然大小压，与新的形参槽对不上

```csharp
case "char": ... return (new[] { $"    sub R13 #1", $"    moveb @13 R{regIdx}" }, 1);
case "short": ... return (new[] { $"    sub R13 #2", ... }, 2);
```

旁边还留着白纸黑字的耦合注释：

> `// 实现体用 moveb 读取 + add R13 #5 清栈, 包装器必须只压 1 字节`

而新前端把形参槽统一成「**每个标量一个 4 字节槽**」（`ParamStackBytes`）——
**这与 p6（`short` 形参错位）是同一个病，只差在包装器这一侧**。
`EmitPushParam` 里 1/2 字节那两支要改成压满一格（`PUSH R0` / `sub R13 #4`），
`totalArgBytes` 随之按 4 计；8 字节的（double/long）保持占两格。

## 顺序（照这个走，别换）

1. **上游**：把本仓 `VMLPrepares/CCompiler/` 的调用约定改动搬过去（或在两边同步同一份）。
2. **上游**：按上面 ①② 改 `tools/GenLib`。
3. **上游**：`dotnet run --project tools/GenLib -- -A`。
4. 上游跑自己的测试 `VMLTests/`（含 `GenLibGenDynTests`）。
5. **本仓**：`sync.sh` 同步下来（⚠ 它会 `--delete` 覆盖 `Lib/`），按需补 `patches/`。
6. **本仓**：删掉 22 个前端里为旧约定写的补偿代码（Forth
   `CodeGenerator.Operations.cs` 的额外 `PUSH` 有白纸黑字的注释）。
7. **判据**：`scripts/vml-abi-probe/run.sh` → 必须 **6/6**（这一步在同步完当场就能验）。
8. **22 语言**：`scripts/vmlcli` 逐条跑 → 22/22。
9. **重打 APK** + `scripts/maui-vml-verify.sh` 真机全量。

> ⚠ 第 5~6 步之间是本仓唯一没有可用基线的窗口 —— 那时 22 语言会大面积红。
> **判进度只看 `scripts/vml-abi-probe/`**（运行期行为，不受模块间新旧混杂影响）。

## 顺手要清的雷

`Lib/shared/src/console.c` 的 `printf2` / `printf3`：

```c
__stdcall void printf2(const char* fmt, int a1, int a2) {
    asm("MOVE R0 fmt");      // ← 把 C 变量名当 asm 操作数
    asm("MOVE R1 a1");
    asm("MOVE R2 a2");
    asm("CALL printf2");     // ← 在 printf2 自己里面递归调自己
    asm("ADD R13 #12");      // ← 清一块从没压过的栈
}
```

现在没炸只因没人调用它。重生成时一并处理（或删掉）。
