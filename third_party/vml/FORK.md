# 本副本已与上游 VML 分家 —— 不要再同步

**自 2026-09-17 起，`third_party/vml/` 是「移动设备手机端专用」的独立分支，不再从上游同步。**
原先的 `sync.sh`（`rsync -a --delete` 整目录覆盖 + 重放补丁）与它的
`WAYCODER_VML_FORCE_SYNC=1` 逃生口**已删除**。

## 为什么分家

内置的 VML 与上游走的是不同的路，再同步只会互相打架：

| 维度 | 上游 | 本副本（手机端） |
|---|---|---|
| 调用约定 | 混用（调用方清 / 被调用方清并存） | **统一为调用方清参数** |
| `Lib/` | 上游生成物，整目录同步 | **在本仓就地重生成**（`GenLib`） |
| GenLib 包装规则 | 上游版本 | 本仓调整过（聚合清单、模块映射…） |
| 模块集合 | 全量（含 PC/DOS/单片机） | **排掉 `crt`/`dos`/`vga_text`/`conio`/`graphics`/`graph`/`browser_gfx`/`gpio`** |

`tools/`（GenLib / GenDyn / GenDev）已复制进本仓，`Lib/` 就地重生成；上游只作**参考**。

## 改动了就是改动了 —— 不需要补丁

**直接改 `third_party/vml/` 下的文件即可**，改完就是最终状态。没有 rsync 会来覆盖它，
所以**不存在「改完还要做成补丁」这一步**（那是分家之前被覆盖式同步逼出来的流程）。

真要从上游挑某个修复，**手工挑拣**那一处改动，别整目录同步。

## `patches/` 现在还留着干什么

**它已经没有功能作用了**，只剩「我们相对上游改了什么」的**历史记录** ——
分家之前那些改动是按补丁形式落地的，逐条记录在案。现在代码本身就是真源。

⚠ **别照着老习惯往 `patches/` 里加新补丁**：分家之后新改动直接在文件里改。
`scripts/check-vml-patches.sh` 的判据①（`vendor + 补丁 == 工作区`）验的就是这批历史补丁。

## 生成物一律就地重生成

`Lib/` 下的 `.vml` / `<语言>/shared.*` 绑定 / `<语言>/builtin.vml` 等**都是生成物**，
判据是「**重生成后与工作区逐字节相同**」，不是「有没有进补丁」：

```bash
cd third_party/vml
dotnet run --project tools/GenLib -- -b   # 共享库 .vml（改过前端就要全量：先 touch Lib/shared/src/*.c）
dotnet run --project tools/GenLib -- -A   # 各语言绑定 shared.*
```

`check-vml-patches.sh` 的判据②a 就是跑一遍重生成再逐字节比。**看到「与重生成结果不一致」
就照提示跑对应的 GenLib 阶段，别去改生成物本身。**

## 必须手工维护的 csproj 适配（35 个 csproj）

这些是**编得过/编不过**的差别，不是风格问题。**新增或重生成 csproj 时四条都要照做**：

| 改动 | 不做会怎样 |
|---|---|
| `OutputType`: `Exe` → `Library` | NETSDK1150：自包含应用不能引用非自包含 Exe |
| 去掉 `StartupObject` | CS2017：库不能指定 `/main` |
| 去掉 `RuntimeIdentifiers` | NETSDK1047：它的列表里没有 `android-arm64` |
| 去掉 `PublishAot` | AOT 发布要求 RID；我们只要它们的代码，不做 AOT 发布 |

（MAUI 排除 `UI/TUI/**` 却编译 `Tools/**`、`Agent/**`，所以这些工程必须能被 MAUI 引用 ——
这正是上面四条的原因。）

## 自测

```bash
cd third_party/vml/test_shared && bash run.sh   # 共享库 C 函数单元测试
bash scripts/vml-out-probe/run-langs.sh         # 各语言输出探针
```
