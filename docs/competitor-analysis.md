# WayCoder（道码）竞品分析报告

> v0.30.2 | 2026-08-11

## 一、WayCoder 自身定位

| 维度 | 现状 |
|---|---|
| 类型 | CLI 终端编程智能体 |
| 语言/运行时 | C# (.NET 10), NativeAOT 编译为单文件 exe（~8MB） |
| 许可证 | MIT |
| 版本 | v0.30.2 |
| 独特标签 | **中文原生、零依赖部署、全屏 TUI、双模型架构、10 槽位多工作区** |

---

## 二、竞品全景对比

### 2.1 直接竞品（CLI 终端类）

| 维度 | **WayCoder** | Claude Code | Aider | Codex CLI |
|---|---|---|---|---|
| 语言 | C# AOT | TypeScript (Node) | Python | Rust |
| 部署 | **单 exe，零依赖** | npm install | pip install | cargo install |
| 内置编辑器 | **14 语言语法高亮** | 无 | 无 | 无 |
| Git 自动提交 | 有 | 无 | **最强（每次编辑=commit）** | 无 |
| 子智能体 | **并行 4 并发** | Teams (beta) | 无 | 有 |
| 模型灵活性 | OpenAI 兼容 | Anthropic Only | **100+ 模型** | 多模型 |
| MCP | HTTP+SSE | **1864+ 服务器** | 无 | 有 |
| 最大上下文 | 128K | **1M tokens** | 模型决定 | 1M tokens |
| 中文原生 | **是** | 否 | 否 | 否 |

### 2.2 IDE 类竞品

| 维度 | Cursor | Copilot | Windsurf |
|---|---|---|---|
| 类型 | AI IDE（VS Code fork） | IDE 扩展 | AI IDE（VS Code fork） |
| 用户量 | 200 万+ | 最广泛 | 被 Cognition 收购后不稳定 |
| 定价 | $20-200/月 | 免费 / $10-100/月 | 免费 / $15/月 |
| 强项 | 最佳 Tab 补全、Agent 模式 | 最广 IDE 支持、GitHub 深度集成 | 性价比高 |
| 弱项 | 仅 VS Code fork、按量计费 | Agent 模式成熟度不如 Cursor | 产品方向不确定 |

### 2.3 开源扩展类

| 维度 | Cline | Continue |
|---|---|---|
| 类型 | VS Code 扩展 | VS Code + JetBrains 扩展 |
| 安装量 | 500 万+, 64K GitHub Stars | 250 万+, 33K GitHub Stars |
| 强项 | 任意模型（12+ 提供商）、浏览器自动化、CLI 2.0 CI/CD | 100% 开源、源码控制配置、JetBrains 支持 |
| 弱项 | VS Code 中心、无自动 Git 提交 | 配置复杂、UX 不够精致 |

### 2.4 企业平台类

| 维度 | Amazon Q | Tabnine | Cody |
|---|---|---|---|
| 定价 | 免费 / $19/月 | $39-59/月 | ~$16K+ 起 |
| 强项 | AWS 深度集成、安全扫描、IP 赔偿 | 气隙部署、零代码保留、合规全覆盖 | 多仓库代码感知 |
| 弱项 | 非 AWS 场景质量下降 | 最贵选项、仅企业 | Free/Pro 已停、仅企业 |

---

## 三、WayCoder 优势分析

### 🟢 独占优势（无竞品具备）

1. **AOT 单文件部署** — 所有竞品都需要运行时，WayCoder 一个 ~8MB exe 即可运行
2. **内置 TUI 编辑器** — 14 语言语法高亮 + 30 个控件组成的完整 GUI 工具包
3. **双模型自动分工** — 大小模型自动切换，显著降低 API 费用
4. **10 槽位多 Agent 工作区 (F1-F10)** — 独立会话 + 跨槽位消息传递
5. **文件锁机制** — FileLockManager 防多 Agent 并发修改冲突，30s 超时释放
6. **1306 项自测** — 随二进制发布，9 个模块，无需外部框架
7. **中文原生设计** — 书名号标记 `«»` 避免 CJK 冲突，全中文 UI
8. **系统极限报告 (`--limits`)** — 60 项限制，含源码定位

### 🟡 相对优势

| 能力 | WayCoder | Claude Code | Cursor | Aider | Cline |
|---|---|---|---|---|---|
| 工具数量 | **38 个** | 30+ | ~15 | ~12 | ~20 |
| 三层上下文压缩 | **有** | 有 | 无 | 无 | 无 |
| 模型回退链 | **有 (4 级)** | 无 | Auto mode | 无 | 无 |
| SKILL.md 兼容 | **有** | 有 | 无 | 无 | 无 |
| Doc 工具 | **有 (30+ 框架)** | 无 | 无 | 无 | 无 |
| LSP 工具 | **有 (6 语言)** | 无 | IDE 内置 | 无 | 无 |
| Import 一键迁移 | **有** | 无 | 无 | 无 | 无 |
| CJK 宽度计算 | **有** | 无 | 无 | 无 | 无 |

---

## 四、差距分析

### 🔴 关键差距

| 差距 | 竞品对标 |
|---|---|
| **无 IDE 集成** | Cline/Continue VS Code, Copilot 6+ IDE |
| **上下文窗口仅 128K** | Claude Code 1M, Gemini CLI 1M |
| **无多模态支持** | Codex CLI、Gemini CLI |
| **品牌认知度低** | Cursor 200万+用户, Copilot 无处不在 |

### 🟣 已发现并修复的 Bug

1. ~~`AgentTool.MaxDepth`（硬编码 3）与 `Config.SubAgentMaxDepth` 不同步~~ ✅ 已修复 — v0.31.0 改为动态读取 Config
2. ~~`SandboxManager.MaxCpuTimeSeconds = 300` 声明但未实现~~ ✅ 已修复 — 添加 MonitorCpuAsync，监控真实 CPU 处理器时间

---

## 五、战略定位

> **"零摩擦的 CLI 编程智能体 —— 专为中国开发者打造"**

- 部署零摩擦：单 exe，无运行时依赖
- TUI 自给自足：内置编辑器 + 14 语言高亮
- 中文原生体验：全中文 UI + CJK 宽度精确计算

## 六、改进路线图

### 近期（v0.31-v0.32）
1. 修复 2 个 Bug（MaxDepth 同步、Sandbox CPU 监控）
2. Watch 模式可配置化
3. 突破硬限制（Session 分页、FallbackLLM 优雅降级、Bash 50K+）

### 中期（v0.33+）
4. VS Code 插件 → JetBrains 插件
5. 插件生态（MCP 市场、SKILL.md 分享）
6. 多模态支持

### 长期（Phase 2）
7. GUI 桌面应用 / 团队协作 / SDK
