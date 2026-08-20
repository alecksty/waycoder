# AI 编程助手 / Coding Agent 资料汇编

> 面向「构建 AI 编程助手」的调研资料，按理解难度与价值排序。

## 一、必读原理（架构类）

### 1. Anthropic《Building Effective Agents》
- 链接：https://www.anthropic.com/engineering/building-effective-agents
- 官方权威。核心区分：
  - **Workflow（工作流）**：LLM 和工具通过预定义代码路径编排
  - **Agent（智能体）**：LLM 动态主导自身流程和工具使用
- 五大模式：
  1. Prompt chaining（提示链）：任务拆成固定子步骤串行
  2. Routing（路由）：分类输入并分流到专门处理
  3. Parallelization（并行化）：Sectioning 分片 / Voting 投票
  4. Orchestrator-workers（编排者-工作者）：一个主模型派发子任务
  5. Evaluator-optimizer（评估-优化）：一个生成一个评估迭代
- 金句：**从 LLM API 直接开始，很多模式几行代码就能实现**；用框架前先理解底层。

### 2. Lilian Weng《LLM Powered Autonomous Agents》
- 链接：https://lilianweng.github.io/posts/2023-06-23-agent/
- Agent 三要素（经典理论框架）：
  - **Planning**（规划）：任务分解 + 反思（self-reflection）
  - **Memory**（记忆）：短期（上下文内）+ 长期（外部存储/向量检索）
  - **Tool Use**（工具使用）：调用外部 API/函数
- 配套技术：ReAct、Reflexion、Chain-of-Thought、Tree of Thoughts

### 3. Claude Code 官方文档
- 链接：https://docs.anthropic.com/en/docs/claude-code/overview
- 闭源但文档齐全，可对照概念：
  - Agent loop（主循环）
  - Tools / Hooks（工具与钩子）
  - Subagents（子智能体）
  - Context management（上下文管理）
  - Slash commands / MCP

---

## 二、读源码学习（开源 Coding Agent 项目）

| 项目 | Stars | 语言 | 仓库 | 可学什么 |
|------|-------|------|------|----------|
| **OpenHands** | 84.2k | TypeScript/Node | github.com/All-Hands-AI/OpenHands | 最完整的 agent server + Docker 沙箱 + 多后端架构 |
| **Goose** (block) | 52.9k | Rust | github.com/block/goose | 桌面+CLI+API 三合一，扩展机制，ACP 协议 |
| **Aider** | 48.3k | Python | github.com/Aider-AI/aider | 最轻量精炼的 agent loop、repo map、自动 git commit、lint/test 反馈 |
| **Cline** | ~40k+ | TypeScript | github.com/cline/cline | IDE 插件形态的 agent |
| **Continue** | ~20k+ | TypeScript | github.com/continuedev/continue | IDE 内自动补全 + 聊天 agent |

### 重点推荐：Aider（最贴近 CLI 形态）
- repo map：给 LLM 提供整个代码库的树状图 + 符号，帮助大项目理解
- 自动 commit：每次改动后生成合理的 commit message
- lint/test 反馈循环：改动后自动跑 lint/test，把错误回喂给 LLM 修复
- 这些与 WayCoder 的 `RepoMapGenerator.cs`、auto-commit、lint/test feedback 高度对应

---

## 三、关键术语（构建时对齐）

| 术语 | 含义 |
|------|------|
| **Agent Loop** | `用户输入 → LLM → 工具调用 → 执行 → 反馈 → 循环`，直到无工具调用或达到最大轮次 |
| **Tool Calling** | 模型调用外部工具的标准机制（OpenAI function calling 兼容） |
| **MCP** (Model Context Protocol) | Anthropic 提出的工具/资源接入协议，让第三方工具生态互通 |
| **ACP** (Agent Client Protocol) | 让不同 agent 互操作（Goose/OpenHands 在用） |
| **Context Management** | 上下文压缩/摘要/分层，处理 token 溢出 |
| **Subagent** | 子智能体，独立上下文并行执行子任务 |
| **Repo Map** | 代码库地图（树 + 符号），帮助 LLM 理解项目结构 |
| **Sandbox** | 沙箱，隔离 agent 的文件系统/命令执行权限 |

---

## 四、一个典型 CLI Coding Agent 的核心模块（对照 WayCoder）

```
入口 (Program.cs)
  └─ 主循环 Agent Loop (Agent.cs)
       ├─ LLM 客户端（流式 SSE + tool call 解析）
       ├─ 工具执行（Bash / ReadFile / WriteFile / Edit ...）
       ├─ 权限确认（PermissionManager）
       ├─ 钩子系统（Hooks：pre/post tool-use）
       ├─ 上下文管理（ContextManager：压缩/摘要）
       ├─ 代码库地图（RepoMapGenerator）
       ├─ 会话持久化（SessionManager）
       └─ 记忆系统（Memory / StructuredMemory）
```

---

## 五、延伸阅读

- 中文教程：`article/07-build-your-own.md`（本仓库，自建 agent 教程）
- 竞品分析：`docs/openclaw-analysis.md`、`docs/competitor-analysis.md`（本仓库）
- Anthropic Cookbook：https://github.com/anthropics/anthropic-cookbook（可运行的 agent 示例）
- Awesome Coding Agents：社区聚合清单

---

## 六、源码走读：Aider 的两个核心实现（附代码定位）

> 下面是 Aider 真实源码的精读结论，直接来自其 GitHub 仓库。

### 6.1 Repo Map 如何实现（`aider/repomap.py`）

Aider 的 repo map 不是简单目录树，而是**基于符号引用图的 PageRank 排序**：

1. **提取符号**：用 tree-sitter（`get_tags_raw`）对每个文件提取两类 tag：
   - `def`（定义，来自 `name.definition.*` query）
   - `ref`（引用，来自 `name.reference.*` query）
   - 每类语言有独立 `query_scm`（`get_scm_fname(lang)`）
2. **建图**：`defines` ↔ `references` 构成有向图，文件是节点，引用关系是边
3. **排序**：`get_ranked_tags` 用 `networkx` 的 **PageRank** 给文件排名——被大量引用的核心文件排名高
4. **个性化**：`personalization` 字典把「当前对话中提到的文件 / 标识符」加权，让相关文件排名上升
5. **缓存**：用 SQLite（`diskcache`）缓存 tag 结果，key 是文件路径，以 `mtime` 失效
6. **token 预算**：`max_map_tokens`（默认 1024）控制注入 prompt 的地图大小

**启示（对 WayCoder 的 `RepoMapGenerator.cs`）**：现有实现是「ASCII 树 + 符号提取」，Aider 的增量点是「PageRank 排名 + 会话提及文件加权」，可让大项目的 map 更聚焦。

### 6.2 Agent Loop 骨架（`aider/coders/base_coder.py`）

`Coder` 类是 agent 循环核心，几个值得注意的设计：

- **edit_format 多态**：`diff / whole / udiff / architect` 等编辑格式各是一个子类，`create()` 工厂按模型能力选择。切换格式时会把旧对话摘要掉，防止旧格式污染 LLM
- **自动 lint/test**：`auto_lint = True`、`auto_test`（默认关）、`test_cmd`，改动后回喂错误给 LLM
- **反思上限**：`max_reflections = 3`，防止 LLM 无限自我修正
- **健壮性计数器**：`num_exhausted_context_windows`（上下文耗尽次数）、`num_malformed_responses`（畸形响应次数），用于触发降级/压缩
- **自动 commit**：`auto_commits` + `commit_before_message`，每次改动后生成 commit message

**启示**：WayCoder 的 `Agent.cs` 已有「lint/test 反馈 + 自动 commit」，可参考 Aider 补充「畸形响应计数 → 触发上下文压缩」这一联动。

---

## 七、Lilian Weng 经典文章精读（Agent 三要素 + 关键论文）

> 源自 `lilianweng.github.io/posts/2023-06-23-agent/`，是构建 agent 的「理论地基」。

### 7.1 三大组件

1. **Planning（规划）**：任务分解 + 自我反思
2. **Memory（记忆）**：短期（上下文内 in-context）+ 长期（外部向量库 + 快速检索）
3. **Tool Use（工具使用）**：调用外部 API 补充模型权重里缺失的信息（实时信息、代码执行、专有数据）

### 7.2 任务分解的关键技术

| 技术 | 全称 | 要点 |
|------|------|------|
| **CoT** | Chain of Thought | 「think step by step」，把大任务拆小，用更多 test-time 计算 |
| **ToT** | Tree of Thoughts | 每步探索多个推理分支，BFS/DFS 搜索，分类器或多数投票评估 |
| **LLM+P** | LLM + Planner | 外包给经典规划器（PDDL），LLM 只做翻译 |

### 7.3 自我反思的关键技术

| 技术 | 要点 |
|------|------|
| **ReAct** | 推理+行动合一，模板：`Thought → Action → Observation` 循环；比纯 Act（无 Thought）更好 |
| **Reflexion** | 动态记忆 + 反思；用「失败轨迹 → 理想反思」的 two-shot 示例，反思最多保留 3 条进工作记忆 |
| **CoH** | Chain of Hindsight：把「逐渐改进的输出序列」喂给模型，训练它自我反思变好 |
| **AD** | Algorithm Distillation：把跨 episode 的学习历史浓缩进模型，学会「RL 过程」本身 |

**启示**：ReAct 的 `Thought → Action → Observation` 正是所有现代 coding agent 主循环的雏形；Reflexion 的「反思条数上限（3 条）」对应 Aider 的 `max_reflections = 3`。

---

## 八、MCP（Model Context Protocol）速览

> 源自 `modelcontextprotocol.io/introduction`。

- **定位**：连接 AI 应用与外部系统的开放标准，类比「AI 的 USB-C 接口」
- **三种接入物**：数据源（本地文件/数据库）、工具（搜索引擎/计算器）、工作流（专用 prompt）
- **三角色**：MCP Server（暴露数据/工具）、MCP Client（AI 应用/agent）、MCP App（跑在 AI 客户端里的交互应用）
- **生态**：Claude、ChatGPT、VS Code、Cursor 等均已支持——「构建一次，处处集成」
- **对 WayCoder 的意义**：`Tools/` 目录 39 个工具目前是内置的，接入 MCP 后可直接挂载第三方工具生态，无需自己实现

---

*整理时间：2026 年。数据为公开仓库快照，Stars 会随时间变化。*
