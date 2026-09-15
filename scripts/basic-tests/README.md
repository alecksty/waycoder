# BASIC 前端回归语料

**为什么有这套东西**：`third_party/vml/Examples/basic/` 下那两个 `.bas` 文件内容是
`404: Not Found`（坏掉的下载残留），`Lib/basic/` 又全是 `asm("SYSCALL ...")` 那种死绑定 ——
**BASIC 前端实际上没有可用的回归语料**。而没有语料就没法安全地改它（改完不知道有没有打坏别的）。

## 用法

```bash
# 需要一个能跑 VML 的宿主（仓库根的 .scratch/vmlhost，或手机端 `vml run`）
cd scripts/basic-tests && bash run.sh
```

`run.sh` 里的 `HOST` 默认指向 `.scratch/vmlhost`；装到手机上跑就把 `HOST` 换成 `vml run`。

## 当前基线（v0.96.168，未修前）

| 用例 | 覆盖 | 结果 |
|---|---|---|
| `t1_scalar.bas` | 标量 / 算术 / 整除 `\` | ✅ `idiv=5 mul=200` |
| `t2_array.bas` | 数组读写 | ✅ `a5=15 a2=6` |
| `t3_sub_param.bas` | SUB 传参（BYVAL） | ✅ `in=42` |
| `t4_global_in_sub.bas` | **SUB 读模块级变量** | ❌ `sub=0`（应为 10） |
| `t5_loop.bas` | FOR / WHILE / 条件 | ✅ `sum=15 w=3` |
| `t6_bridge.bas` | 跨语言调 C 共享库 | ✅ `w=395` |
| `t7_shared.bas` | **`DIM SHARED` 跨 SUB** | ❌ `shared=0`（应为 77） |

**两个 ❌ 是同一个根因**：BASIC 的**跨帧变量访问根本没实现** —— 模块级变量与 `DIM SHARED`
在 SUB 里都读成 0。见 `CHANGELOG.md` 的 v0.96.168 条目。

## 追加用例（v0.96.169）

| 用例 | 覆盖 | 结果 |
|---|---|---|
| `t8_sub_write_global.bas` | **SUB 写模块级变量** | ✅ `counter=10`（修前 0） |
| `t9_func_global.bas` | 模块级 FUNCTION 读写模块变量 + 被 SUB 调用 | ❌ 死循环（**既有缺陷**） |
| `t10_sub_local_for.bas` | **SUB 内局部 FOR 循环** | ❌ 死循环（**既有缺陷**） |
| `t11_sub_local_array.bas` | **SUB 内局部数组赋值** | ❌ 读出 0（**既有缺陷**） |

> **t9/t10/t11 是前端既有缺陷，与全局变量那次改动无关** —— 用 `git stash` 把改动全部暂存、
> 重编出"改动前"的编译器后，这三个用例的表现**一模一样**（a3 同样死循环、a4 同样读 0）。
> 记这一笔是为了下次不再重复验证：**先取基线，再判断是不是自己改坏的**。
>
> 影响：`Examples/basic/tetris.bas` 里 `draw_board` / `clear_line` 等**全是 SUB 内局部 FOR 循环**
> ⇒ 撞上 t10 就卡死。所以 BASIC 版俄罗斯方块暂时搁置，**先用 C 实现**（见
> `third_party/vml/Examples/c/tetris.c`），BASIC 前端这几个缺陷后续单独修。
