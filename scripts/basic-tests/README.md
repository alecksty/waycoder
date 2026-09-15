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
