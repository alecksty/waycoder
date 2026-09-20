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

## 剪枝：手机端用不到的已经**物理**删掉（2026-09-20）

上面那张表里的「排掉 `crt`/`dos`/…」说的是**打包时**的排除（`scripts/make-vml-lib.sh`
的 `-x`）—— 文件还躺在树里，只是不进 `vml_lib.zip`。2026-09-20 又做了一轮**物理剪枝**：
把手机端用不到的文件直接删掉，并落成一份**声明清单** `PRUNED.txt`
（本轮 **2653 个文件 / 约 15 MB**）。

| 组 | 内容 | 量 |
|---|---|---|
| ① | `Lib/*/Device/**`（MCU 与古董机的外设/寄存器定义：ATmega / STM32 / ESP32 / ZX Spectrum / NES / Sega / Apple II / IBM-PC…，由 `tools/GenDev` 生成）、`Lib/vml/{Device,Bios}/**`（含 23 个模拟 BIOS 的启动汇编） | 12.7 MB |
| ② | `Lib/*/ext/**` + `Lib/dynamic/**`（桌面 GPU/GUI 动态库绑定：imgui / opencv / opengl / skia，由 `tools/GenDyn` 生成） | 0.5 MB |
| ③ | `Lib/shared/backup/**`（`shared/*.vml` 的陈旧副本）、PC VGA 字库、`Lib/pascal/vga.pas`、PC 专有头、宿主机构建脚本 | 2.2 MB |

**`Lib/c/vmlib.h` 有意保留**：它是 `Lib/c/vmdevice.h` 的 `#include` 目标，而 `vmdevice.h`
属另一族（MCU 设备头，未列入本轮）。删了会留下悬空 include。

⚠ **桌面端也读同一份 `Lib/`** ⇒ 这些能力在桌面 VML 里一并没有了。这符合「手机端专用」的
定性；若哪天要在桌面上跑用 `graphics.h` 的 C 程序，`git checkout` 就能把对应文件拿回来。

生成器（`tools/GenDev` / `tools/GenDyn`）**留在本仓、也仍有能力重新产出它们**，但
**没有任何脚本或构建会去跑它们**（唯一被自动调用的是 `tools/GenLib`，而它不产这两族）。
将来要重生成，先想清楚手机端是否真的需要。

## `check-vml-patches.sh`：判据已换成分家后的模型（2026-09-20）

旧判据「vendor + 补丁 == 工作区」的前提随分家消失了（没有 rsync 了），继续跑它会把
**正当的分家改动**报成红 —— 实测在干净的 v0.96.312 上 **131 个 ✘**，其中 49 个属这一类。
现在验三条仍然有意义的：

| 判据 | 验什么 | 失败意味着 |
|---|---|---|
| ① | `Lib/` 生成物 == 用本仓 GenLib 重生成的结果 | 改了源码/前端/生成器却没重生成 |
| ② | 剪枝清单 == 磁盘现实（两个方向） | 清单在说谎，或发生了**清单外的意外删除** |
| ③ | GenLib 不产出的手工文件已被 git 跟踪 | 新写了手工文件却忘了 `git add`（丢了没法重生成） |

参考系从「vendor 提交」换成了**当前 git 索引**：分家后树本身就是真源。
（那轮重生成还抓出**签入的 `Lib/shared/*.vml` 出自更早版本的 GenLib** —— 那时还不发
`; <行号>: <源码>` 注释，82 个文件对不上，已跑 `GenLib -A` 拉齐。）

## 改动了就是改动了 —— 不需要补丁

**直接改 `third_party/vml/` 下的文件即可**，改完就是最终状态。没有 rsync 会来覆盖它，
所以**不存在「改完还要做成补丁」这一步**（那是分家之前被覆盖式同步逼出来的流程）。

真要从上游挑某个修复，**手工挑拣**那一处改动，别整目录同步。

## `patches/` 现在还留着干什么

**它已经没有功能作用了**，只剩「我们相对上游改了什么」的**历史记录** ——
分家之前那些改动是按补丁形式落地的，逐条记录在案。现在代码本身就是真源。

⚠ **别照着老习惯往 `patches/` 里加新补丁**：分家之后新改动直接在文件里改。
`scripts/check-vml-patches.sh` **已不再验这批补丁**（2026-09-20 重写，见上）——
分家后没有 rsync，「改动有没有进补丁」不再有任何意义，继续验只会把**正当的分家改动**
报成红（实测 131 个 ✘ 里 49 个是这么来的）。

## 生成物一律就地重生成

`Lib/` 下的 `.vml` / `<语言>/shared.*` 绑定 / `<语言>/builtin.vml` 等**都是生成物**，
判据是「**重生成后与工作区逐字节相同**」，不是「有没有进补丁」：

```bash
cd third_party/vml
dotnet run --project tools/GenLib -- -b   # 共享库 .vml（改过前端就要全量：先 touch Lib/shared/src/*.c）
dotnet run --project tools/GenLib -- -A   # 各语言绑定 shared.*
```

`check-vml-patches.sh` 的**判据①**就是跑一遍重生成再逐字节比。**看到「与重生成结果不一致」
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
