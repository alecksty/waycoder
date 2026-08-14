# WayCoder（道码）竞品分析报告

> v0.54.0 | 2026-08-15

## 一、WayCoder 自身定位

| 维度 | 现状 |
|---|---|
| 类型 | CLI 终端编程智能体 |
| 语言/运行时 | C# (.NET 10), NativeAOT 编译为单文件 exe（~8MB） |
| 许可证 | MIT |
| 版本 | v0.54.0 |
| 工具数 | 43 个内置工具 + MCP 自动发现 + 编译期插件 |
| 自测 | 2228 项（随二进制发布，12 模块） |
| 独特标签 | **中文原生、零依赖部署、全屏 TUI、双模型架构、10 槽位多工作区、极致成本控制** |

---

## 二、竞品全景对比

### 2.1 直接竞品（CLI 终端类）

| 维度 | **WayCoder** | Claude Code | Codex CLI | Aider |
|---|---|---|---|---|
| 语言 | C# AOT | TypeScript (Node) | **Rust** | Python |
| 部署 | **单 exe，零依赖** | npm install | **单二进制，零依赖** | pip install |
| 内置编辑器 | **14 语言高亮 + Lint 诊断** | 无 | TUI（Ratatui） | 无 |
| Git 自动提交 | 有 | 无 | 有 | **最强（每次编辑=commit）** |
| 子智能体 | **并行 4 并发 + 10 槽位** | **几十~几百并行（Workflows）** | 6 并行（depth 1） | 无 |
| 模型灵活性 | OpenAI 兼容 + 6 模型回退链 | Anthropic Only | 多模型 | **100+ 模型（litellm）** |
| MCP | Stdio+HTTP+SSE 三传输 | **1864+ 服务器** | client + server | 无 |
| 上下文窗口 | 128K（按模型切换） | **1M** | 200K | 模型决定 |
| 原生沙箱 | 软件级（内存/CPU 监控） | 自托管沙箱(beta) | **内核级（Seatbelt/Landlock）** | 无 |
| 中文原生 | **是** | 否 | 否 | 否 |
| 结构化输出 | `--json` | `claude agents --json` | `--json`/`codex exec` | 无 |

### 2.2 IDE 类竞品

| 维度 | Cursor | Cline | Copilot |
|---|---|---|---|
| 类型 | **Agent 平台（3.0 起非 VS Code fork）** | VS Code 扩展 | IDE 扩展 |
| 用户量/安装 | 200 万+ | 500 万+, 64K Stars | 最广泛 |
| 定价 | $20-200/月（6 档） | 免费 + $9/月（ClinePass） | 免费 / $10-100/月 |
| 核心差异 | Composer 2.5 自研模型 + Cloud Agents | Plan/Act/YOLO + 1M 上下文 | GitHub 深度集成 |
| 并行能力 | Cloud 多代理（VM 隔离） | 单代理逐步骤 | 有 |

### 2.3 开源/其他 CLI

| 维度 | Goose (Block) | Antigravity CLI（原 Gemini CLI） |
|---|---|---|
| 类型 | 开源 Agent（桌面+CLI+API） | Go 闭源二进制 |
| 模型 | BYOK，25+ 提供商 400+ 模型 | Gemini 3.5（1M 上下文） |
| 亮点 | MCP-native，70+ 扩展 | 多代理后台工作流、Search grounding |
| 现状 | 活跃，51K Stars | 2026.6 取代 Gemini CLI（后者停免费） |

---

## 三、WayCoder 优势分析

### 🟢 独占优势（无竞品具备）

1. **AOT 单文件部署** — 唯一 C# NativeAOT；Codex CLI 虽也做到 Rust 单二进制，但 WayCoder 仍独有「中文原生 + 全屏 TUI 控件库」组合
2. **内置 TUI 编辑器** — 14 语言语法高亮 + 10+ Linter 诊断 + 40+ 控件，竞品 CLI 均无
3. **双模型自动分工** — 大模型干活 + 小模型压缩/摘要，自动省钱（Aider Architect Mode 需手动配，非自动）
4. **10 槽位多 Agent 工作区（F1-F10）** — 独立会话 + 后台并行 + 跨槽位消息，竞品无等效
5. **极致成本控制三层** — Tiny（本地小模型免费）+ Economy（云端降本）+ 双模型分工，Claude Code/Cursor 均无
6. **2228 项自测** — 随二进制发布，无需外部框架（所有竞品均无内置自测）
7. **中文原生设计** — 书名号标记 `«»` 避免 CJK 冲突，全中文 UI
8. **系统极限报告（`--limits`）** — 60 项限制含源码定位

### 🟡 相对优势

| 能力 | WayCoder | Claude Code | Codex CLI | Cursor | Aider | Cline |
|---|---|---|---|---|---|---|
| 工具数量 | **43** | 30+ | ~20 | ~15 | ~12 | ~20 |
| 三层上下文压缩 | **有** | 有 | 有 | 无 | 无 | 有 |
| 模型回退链 | **有（6 模型）** | 无 | 无 | Auto mode | 无 | 无 |
| SKILL.md 兼容 | **有** | 有 | 有 | 无 | 无 | 有 |
| Doc 工具（30+ 框架） | **有** | 无 | 无 | 无 | 无 | 无 |
| LSP 工具 | **有（14 语言）** | 无 | 无 | IDE 内置 | 无 | 无 |
| Import 一键迁移 | **有** | 无 | 无 | 无 | 无 | 无 |
| Watch 模式（AI! 注释） | **有** | 无 | 无 | 无 | 有 | 无 |
| 批量任务引擎（多仓库） | **有** | 有（Workflows） | 有 | 有（Cloud） | 无 | 无 |
| 编译期插件系统 | **有（C# 源码）** | 有（Skills） | 有（市场） | 有（市场） | 无 | 有（MCP） |
| 多模态（图片+音频） | **有** | 有（视觉） | 有 | 有 | 无 | 有（browser） |

---

## 四、差距分析

### 🔴 关键差距（按严重程度）

| 差距 | 竞品对标 | WayCoder 现状 |
|---|---|---|
| **超大规模并行编排** | Claude Code Dynamic Workflows（几十~几百子代理）、Cursor Cloud Agents | 仅 4 并发子代理 + 10 槽位，无云代理/动态编排 |
| **上下文窗口** | Claude Code/Gemini/Cline 1M，Codex 200K | 128K（但按模型自动切换） |
| **内核级沙箱** | Codex CLI Seatbelt/Landlock、Claude 自托管沙箱 | 软件级沙箱（内存/CPU 监控，安全层级较低） |
| **品牌认知度** | Cursor 200万+，Cline 500万+ | 低（个人项目） |
| **IDE 深度集成** | Cline/Continue VS Code、Cursor 即 IDE | 仅 `--json` 桥接，无 VS Code 扩展 |

### 🟣 已闭环的差距（v0.31 后补齐）

1. ~~`AgentTool.MaxDepth` 同步 bug~~ ✅ v0.31.0
2. ~~`SandboxManager` CPU 监控未实现~~ ✅ v0.31.0
3. ~~多模态缺失~~ ✅ v0.48.9（view_image 图片 + transcribe 音频 + screenshot 抓屏）
4. ~~批量任务引擎缺失~~ ✅ v0.49.0（--batch 多仓库并行 + worktree 隔离）
5. ~~插件系统缺失~~ ✅ v0.50.0（IPlugin 编译期插件）
6. ~~IDE 集成缺失~~ ✅ v0.51.0（--json 结构化输出桥接，对标 Codex --json / Claude agents --json）
7. ~~自动升级缺失~~ ✅ v0.48.7（UpdateChecker 自替换，对标 claude update）
8. ~~MCP 资源/提示词缺失~~ ✅ v0.48.8（resources/prompts 发现）

---

## 五、战略定位

> **"极致成本控制的 CLI 编程智能体 —— 专为中国开发者打造"**

- 部署零摩擦：单 exe，无运行时依赖
- TUI 自给自足：内置编辑器 + 14 语言高亮
- 中文原生体验：全中文 UI + CJK 宽度精确计算
- 省钱三层体系：Tiny + Economy + 双模型分工（Claude Code / Cursor 均无）

---

## 六、竞品动态速览（2026 年 8 月）

| 竞品 | 最新版本 | 关键变化 |
|---|---|---|
| Claude Code | v2.1.x | Dynamic Workflows（GA）、Opus 4.8、`/goal` 自主长任务、Checkpoint 恢复、子代理最小权限 |
| Codex CLI | rust-v0.138 | **Rust 重写**（单二进制零依赖、冷启动 20x 快）、`codex exec` 非交互、`--json`、内核级沙箱 |
| Cursor | 3.8 | **Agent-first 平台**（脱离 VS Code fork）、Composer 2.5（Kimi K2.5）、Cloud Agents、`/best-of-n` |
| Aider | v0.86.2 | Architect Mode（双模型）、Repo map、仍无 MCP/子智能体 |
| Cline | v3.84 | 1M 上下文、gRPC API + SDK、JetBrains（Teams）、SWE-bench 65.8 |
| Gemini CLI | → Antigravity | 2026.6 停免费，Antigravity CLI（Go 闭源）接棒 |
| Goose | 活跃 | BYOK 多模型、MCP-native、51K Stars |

---

## 七、改进路线图（v0.51+ 优先级）

### P0（对标 Claude Code / Codex 的核心能力）
1. **上下文窗口突破 128K** — 对标 Claude Code/Gemini/Cline 1M（长任务场景刚需）
2. **超大规模并行编排** — 对标 Dynamic Workflows：动态 spawn N 个子代理 + 结果聚合（现有 4 并发是硬上限）

### P1（差异化补强）
3. **VS Code 扩展** — 复用 `--json` 桥接，做一个轻量 VS Code 插件（覆盖最大用户群）
4. **内核级沙箱** — 对标 Codex Seatbelt/Landlock，当前软件级沙箱安全层级不足

### P2（架构优雅度）
5. **事件总线权限架构** — 对标 Crush pub/sub 解耦（当前直接调用确认）
6. **多作用域配置** — 全局 + 工作区 JSON（当前单 `.env`）
