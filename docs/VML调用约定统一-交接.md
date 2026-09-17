# VML 调用约定统一 —— 交接说明（v0.96.202 时点）

> 换电脑继续时先读这一份。方案与全部实测记录在同目录的 `docs/VML调用约定统一.md`，
> 判据与活单在 `scripts/vml-abi-probe/README.md`。

## 一、这件事在干什么

把 VML（自研虚拟机）内部的**至少三种调用约定**收成一条：

> **全部实参右到左压栈、一个都不走寄存器、调用方清栈**，压完之后把 `arg0..arg3`
> 镜像进 `R0-R3`（给 `Lib` 里 543 处 `asm("SYSCALL #6")` 用）。

统一之前，调用点与被调方一旦对不上就是**静默的实参错位或栈漂移** ——
本仓历史上的「同一段代码有时对有时错」「print 几次之后局部变量读成 0」都是这一族。

## 二、当前状态（判据全绿到什么程度）

```bash
scripts/vml-abi-probe/run.sh          # C 前端 6 条        → 6/6
scripts/vml-abi-probe/run-langs.sh    # 跨语言 14 条       → 13/14
# 22 语言骨架（每条判据 SKEL-SUM=14）  → 22/22
bash scripts/check-vml-patches.sh     # 补丁/重生成守卫    → 全绿
cd WayCoder && dotnet run -- --test   # 桌面自测           → 5859/5859
```

**唯一红的是 `drift.m`**（ObjC），见下「四」。

## 三、已经做完的

| 版本 | 内容 |
|---|---|
| v0.96.199 | 调用约定落到 `Lib/`（就地用 GenLib 重生成 1621 个文件）；`check-vml-patches.sh` 的 `Lib/` 段改按「可重生成」验 |
| v0.96.200 | 推平到其余前端：C++ / Java / Ruby / Lua；发现并修掉**总根源** —— `Lib/{lang}/**` 包装器一直在用寄存器收参数 |
| v0.96.201 | 单词名库函数的包装器**自调用**（java / javascript 的 `PrimaryPrefix` 为空 + camelCase） |
| v0.96.202 | `ParseFunctions` 剥注释；撞名保险；**Lua 的 `for` 循环变量必须先 `local`** |

几条值得记住的**机制**（不是「改了哪个文件」，是下次还会撞上的）：

1. **「单参调用看着都对、多参才露馅」** —— 单参时 `R0` 恰好等于刚求值完的那个实参；
   多参才需要 `R1-R3`，而那是残留值。所以 22 语言骨架（库调用几乎都是单参）**证明不了**调用约定。
2. **`Lib/{lang}/**` 的包装器曾经从 `R0-R3` 收参数**（`GenLib.EmitPushParam` 发 `PUSH R{i}`），
   现已改成从**自己的栈帧**读（`[R12+12+4i]`）并在转发前镜像一次。**镜像从此只在这一层做**。
3. **被调方一律裸 `ret`** —— 凡是「压了实参指望对方弹掉」的补偿，现在都是**净漏栈**
   （D/Fortran/Ruby 的 `**`/`^^`、R 的 print、Forth 的 `."` 都踩过）。
4. **标签与 C 符号名同名 = 自调用**（无限递归）。GenLib 现在撞名会自动改名。
5. **22 语言骨架全绿 ≠ 该语言没问题** —— 已经被证伪两次（Lua 的 for、ObjC 的循环里调库）。

## 四、剩下的活（按建议顺序）

### ① `drift.m` —— ObjC：**外部库调用在循环体里会让循环提前退出**（唯一红项）

已经定位得很窄了，直接照这三组数继续：

| 程序 | 实测 | 应得 |
|---|---|---|
| `ipow(2,3)` 不套循环（`langs/abi.m`） | `8` ✓ | 8 —— **实参传递没问题** |
| 循环里不调库 `for(i=1;i<=6;i=i+1) s = s + i;` | `21` ✓ | 21（跑满 6 轮） |
| 循环里调库、**常量**实参 `s = s + ipow(2,3)` | **`16`** ✗ | 48（= 8×**2** ⇒ 只跑 2 轮） |
| 循环里调库、变量实参 `s = s + ipow(2,i)` | **`2059`** ✗ | 126 |

骨架 `corpus/objc/skel.m` 没事，是因为它循环里调的是 `inc`（**本地函数**，走
`CodeGenerator.Expressions.cs` 的另一条分支）；坏的是**外部函数**那条。

落点：`third_party/vml/VMLPrepares/ObjCCompiler/CodeGenerator.Expressions.cs` 的外部分支 +
`CompilerBase/StatementManager.cs:151` 的 `EmitFor`（条件结果放 `R0`、循环变量在栈帧槽）。
**下一个要查的**是那次 `CALL` 前后哪个槽被覆盖 —— 建议先把 `b.m` 那种程序 dump 成 VML
（`scripts/vmlcli <文件> --vml /tmp/x.vml`）逐行看循环体。

### ② `Lib/modules.json` 里约 50 条历史虚构条目

`ParseFunctions` 剥注释之前收进来的（如 `graphics` 模块下的 `R0` / `PUSH` / `Sector`）。
它们是**死包装器**、且已被撞名保险兜住，所以不急；要清就得谨慎地重写配置
（`LoadOrCreate` 只在文件缺失时生成，不会自动覆盖）。

### ③ 审计里还有几条**探针没覆盖**的同类问题

全 22 前端审计的结论在 `scripts/vml-abi-probe/README.md` 末节，摘要：

- Python 的**被调方**仍从 `R0-R3` 拷形参（`Statements_A.cs:61-71`）而调用点只压栈 ⇒ 只有 arg0 侥幸正确；
- Kotlin 的被调方取参公式（`CodeGenerator.cs:90-96`）与调用点的压栈方向**相反**；
- Swift 的被调方前 4 参读 `R0-R3`；
- Go 的方法调用把 receiver 压在最前、落在最高地址 ⇒ 被当成最后一个形参；
- Lua 的实参**第 5 个起静默丢弃**；
- Basic / Pascal 的 builtin 调用仍有「被调方清栈」残留。

### ④ 到手机上还差一步

`vml_lib.zip` 每次都随 `Lib/` 重打了，但 **APK 没重打** —— 不重装，手机上跑的还是旧库。
命令见 `CLAUDE.md` 的打包段（**必须带签名参数**，否则装不上已装的 App）。

## 五、别踩的坑（本仓已有的约定）

- **改了 `third_party/vml/VMLPrepares/**` 就要重新生成 `patches/0034`**，否则
  `check-vml-patches.sh` 变红。生成脚本的套路在 `scripts/check-vml-patches.sh` 的注释里：
  建 vendor worktree → 打 `0001..0033` → `git add -A` → 把工作区的对应目录覆盖过去 → `git diff`。
  ⚠ **别把 0034 自己也打进基准**（第一版就这么把 C 前端的改动从补丁里弄丢过）。
- **`Lib/` 是生成物**，别手改；改 `Lib/shared/src/*.c` 或 `GenLib` 之后跑
  `touch Lib/shared/src/*.c && dotnet run --project third_party/vml/tools/GenLib -- -A -r .`。
  ⚠ `-b` 是**按时间戳增量**的，不 `touch` 会报「0 编译, N 跳过」而什么都没做。
- **改完 `Lib/` 要重打 `vml_lib.zip`**：`bash scripts/make-vml-lib.sh`。
- 判据的跑法依赖 `scripts/vmlcli`（与手机端逐字等价、秒级）：
  **改了 `third_party/vml` 之后必须 `dotnet build scripts/vmlcli -c Release` 才生效**。
- 环境：分支 `mac`，远端 `origin` = Gitee（`github` 远端停在 v0.96.157，很久没同步）。
