# `HOST_LANG`(#568) 端到端探针

判据：C / Python 侧的 `ui_get_language()` **真的调到宿主**（`HOST_LANG` #568）并拿到 `0`（中文）/ `1`（英文）。

```bash
# 中文（本机即是）
dotnet run --project scripts/vmlcli -- scripts/vml-ui-lang-probe/lang_probe.c
# 强制非中文：把 CurrentUICulture 按掉（本机是 zh-CN，判定规则里"任一个标签是中文就算中文"）
LANG=en_US.UTF-8 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    dotnet run --project scripts/vmlcli -- scripts/vml-ui-lang-probe/lang_probe.c
```

## 为什么必须看**三行合起来**

单看 `LANG=0` 会被"没人写 R0、残留巧合是 0"骗过去：

| 输出 | 它证明什么 |
|---|---|
| `W=480` | `ui_*` 所在的 vmlui 模块链进来了、syscall 通道是活的（480 = `--screen` 给宿主的值，只有宿主报得出来） |
| `LANG=0` / `LANG=1` | 宿主认领了 #568 并回答了语言码；两种系统语言给出**两个不同的数**才算接通 |
| `UNKNOWN=-100` | **对照组**：同号段里没实现的 #599 返回 `-100` ⇒「没实现」永远不是 0 ⇒ `LANG=0` 是真答案 |

`ctrl_probe.c` 是拆出来的对照件；`lang_probe.py` 用 Python 绑定跑同一条判据（验证"非 C 语言的绑定也通"）。

## 记录（v0.96.571 实测）

```
默认（中文系统）:  W=480   LANG=0   UNKNOWN=-100
强制非中文:        W=480   LANG=1   UNKNOWN=-100
```
Python 侧同样 `0` / `1`。
