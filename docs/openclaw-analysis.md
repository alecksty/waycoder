# OpenClaw 对比分析报告

> v0.58.1 | 2026-08-15
>
> 分析对象：OpenClaw（开源 AI 编程智能体，TypeScript/Node）
> 分析方式：通过 GitHub API 拉取仓库目录与关键源文件，做架构级对比，提炼可借鉴点。

---

## 一、OpenClaw 是什么

OpenClaw 是一个开源的通用 AI 智能体（agent）框架，与 WayCoder 同属「编程智能体」赛道。其架构与 WayCoder 的「单进程 + 主循环」不同，采用更重型的**网关 + 客户端**架构：

- **Gateway daemon（网关守护进程）**：常驻后台进程，集中承载模型会话、工具执行、上下文管理、轨迹记录等核心能力。
- **Clients（客户端）**：通过 RPC 连入网关，负责 UI/交互（CLI、TUI、Web 等），本身不持有核心状态。

这种「核心集中、界面分离」的架构是 OpenClaw 最鲜明的特征，也是它与 WayCoder（以及 Claude Code、Aider 等单进程 CLI）的本质差异。

---

## 二、架构对比

| 维度 | **WayCoder** | **OpenClaw** |
|---|---|---|
| 语言/运行时 | C# (.NET 10) NativeAOT 单文件 | TypeScript (Node) |
| 进程模型 | 单进程 + 主循环（10 槽位后台线程并行） | Gateway daemon + 多客户端 RPC |
| 上下文管理 | 三层让步压缩（裁剪→摘要→硬折叠）+ Crush 风格 token 追踪 | 可插拔 ContextEngine（多种 promptAuthority 策略） |
| 工具元数据 | `ITool.Schema()` → JSON Schema | ToolDescriptor + 可用性表达式 + outputSchema |
| 轨迹记录 | 无 | **trajectory JSONL 回放** |
| 安全审计 | BashGuard + 权限确认 | **security self-audit（自审计）** |
| 降级策略 | 模型回退链（6 模型）+ Tiny/Economy 模式 | **fallback/degraded 降级原因码** |
| 部署 | 单 exe 零依赖 | npm 多包 |

---

## 三、可借鉴点（按投入产出比排序）

### 🥇 1. Trajectory 轨迹回放（投入产出比最高）

**OpenClaw 做法**：把每次 agent 运行的完整过程——每轮 LLM 的 token 消耗、每个工具调用的入参/结果/耗时——以**结构化 JSONL** 逐条落盘，形成可回放、可分析、可复现的「运行轨迹」。这是调试 agent 行为（为什么这么干）、评估模型质量（哪轮决策烂）、复现 bug（把轨迹喂回去）的基石。

**WayCoder 现状**：无。会话只存了最终 `messages`（`SessionManager`），丢失了「每轮用了多少 token」「每个工具花了多久」「工具成败」这些过程性元数据。

**借鉴方案**（v0.58.1 已实现）：
- 新增 `WayCoder/Agent/Trajectory.cs`，版本化 JSONL 事件流（`traceSchema`/`schemaVersion`/`runId`/`sessionId`/`type`/`ts`/`seq`/`data`）。
- 事件类型：`run_start` / `llm_turn`（每轮 token + 内容长度 + 工具数 + 推理长度）/ `tool_call`（工具名 + 入参摘要 + 结果摘要 + 成败 + 耗时）/ `run_end`（汇总）。
- 落盘 `.waycoder/trajectory/<runId>.jsonl`（已被 `.gitignore` 覆盖）。
- 开关：`WAYCODER_TRAJECTORY`（默认开，`=0` 关闭）。
- 纯手搓 JSONL 追加（`File.AppendAllText` + `lock` + `Interlocked` 序列号），AOT 安全，零依赖。

### 🥈 2. Context Engine 抽象（降级原因码 + 溢出权威）

**OpenClaw 做法**：上下文管理不是硬编码三段式，而是可插拔的 `ContextEngine`，通过 `promptAuthority` 决定「当前上下文该由谁说了算」——`assembled`（正常组装）、`preassembly_may_overflow`（预组装可能溢出，需提前压缩）等状态码驱动不同降级策略；溢出时走 `thread_bootstrap` 投影 + `quarantine`（隔离）机制，并对降级原因给出明确码（fallback/degraded）。

**WayCoder 现状**：已有「三层让步压缩」（50% 裁剪→70% 摘要→90% 硬折叠）+ 真实 token 追踪（`AddUsage`/`ShouldStopAndSummarize`），但**降级原因没有结构化编码**——目前只有「压缩了」这个动作，没有「为什么降级到哪一层」的可观测信号。

**借鉴方案**（P1，未实现）：
- 引入 `ContextDegradeReason` 枚举（`None`/`Budget`/`Overflow`/`SummaryFailed`/`HardFold`），压缩路径返回原因码。
- 把「是否可能溢出」的预判（`preassembly_may_overflow`）前移：在组装消息前就估算是否会爆窗口，而非爆了再补。
- 目的不是重写压缩逻辑，而是给现有三层让步**加可观测性**，让「为什么这次压缩这么狠」可追溯。

### 🥉 3. Tool 元数据（声明式可用性 + outputSchema）

**OpenClaw 做法**：`ToolDescriptor` 除 `schema` 外，还带**可用性表达式**（声明工具在什么条件下可用/禁用，而非运行时硬编码）与 **outputSchema**（工具输出结构化的 schema，供模型/框架理解结果）。

**WayCoder 现状**：`ITool` 有 `Name`/`Description`/`Parameters`/`Schema()`，但可用性靠 `PermissionManager` 运行时判定，输出无 schema（自由文本 + 结果分类器兜底）。

**借鉴方案**（P2，未实现）：
- 给 `ITool` 加 `AvailabilityExpression`（声明式禁用条件，如 `"platform=windows"`、`"sandbox=off"`），构造期过滤，替代部分运行时 if。
- `outputSchema` 可选，先只做 `bash`/`read_file` 等高频工具，供「工具输出是否成功」的结构化判断（可进一步替代 `ToolResultClassifier` 的启发式标记）。

### 4. Security 自审计（Security Self-Audit）

**OpenClaw 做法**：内置「安全自审计」——agent 运行期间对自己的行为做安全风险评估（执行了哪些敏感命令、是否越权），输出审计结论。

**WayCoder 现状**：`BashGuard`（70+ 禁止 + 47 白名单）+ `PermissionManager` 行内确认，是**事前拦截 + 事中确认**，缺**事后审计**（这次运行到底碰了哪些敏感资源）。

**借鉴方案**（P2，未实现）：
- 在 Trajectory 轨迹基础上叠加：对 `tool_call` 事件做敏感度标注（命令命中 BashGuard 禁止/白名单/需确认），`run_end` 汇总「本次运行敏感操作清单」，形成事后审计视图。
- 与 Trajectory（第 1 项）天然互补——轨迹是原料，审计是加工后的安全结论。

---

## 四、实施建议

| 优先级 | 借鉴项 | 状态 | 说明 |
|---|---|---|---|
| 🥇 P0 | Trajectory 轨迹回放 | ✅ v0.58.1 | 过程性元数据落盘，调试/评估/复现的基石 |
| 🥈 P1 | Context 降级原因码 | ⬜ 待做 | 给三层让步加可观测性 |
| 🥉 P2 | Tool 声明式可用性 + outputSchema | ⬜ 待做 | 构造期过滤 + 输出结构化 |
| P2 | Security 自审计 | ⬜ 待做 | 事后敏感操作清单，与轨迹互补 |

> **结论**：OpenClaw 的 Gateway 架构与 WayCoder 的「单进程 + 多槽位后台线程」路线不同，不必照搬进程模型；但其 **Trajectory 轨迹**、**Context 降级原因码**、**Tool 声明式元数据**、**Security 自审计** 四个设计点都值得吸收。其中 Trajectory 投入产出比最高，已优先实现。
