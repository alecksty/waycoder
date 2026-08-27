# WayCoder（道码）竞品分析与路线图

> 版本：v0.96.9 | 日期：2026-08-26

---

## 一、竞品全景对比（仅 CLI）

| 指标 | **WayCoder** | Claude Code | Codex CLI | Aider | Goose | Gemini CLI |
|------|:---:|:---:|:---:|:---:|:---:|:---:|
| **开源** | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ |
| **语言** | C# (.NET 10) | TypeScript | Rust | Python | Rust | TS/Go |
| **AOT 单文件** | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **内置 TUI 编辑器** | ✅ 14 语言 | ❌ | ❌ | ❌ | ❌ | ❌ |
| **编辑器 Lint 诊断** | ✅ 10+ linter | ❌ | ❌ | ❌ | ❌ | ❌ |
| **双模型架构** | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **模型灵活度** | OpenAI 兼容 | 仅 Anthropic | OSS 模式 | 100+ 模型 | 15+ 提供商 | 仅 Gemini |
| **Git 自动提交** | ✅ | ❌ | ✅ | ✅ | ❌ | ❌ |
| **子智能体** | ✅ 并行+依赖编排 | ✅ Teams+嵌套5层 | ✅ Goal 模式 | ❌ | ✅ via MCP | ✅ 多Agent并行 |
| **沙箱执行** | ✅ 软件级四级¹ | ❌ | ✅ 内核级四级 | ❌ | ❌ | ❌ |
| **自主学习系统** | ✅ /kb+教学模式+学习路径 | ❌ | ❌ | ❌ | ❌ | ❌ |
| **编辑级回滚** | ✅ 文件版本+快照 | ✅ 部分(/rewind) | ✅ SQLite fork/rollback | ❌ | ❌ | ❌ |
| **自动 Lint 反馈** | ✅ | ✅ | ✅ | ✅ lint+test | ❌ | ❌ |
| **自动 Test 循环** | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Prompt 缓存** | ✅ | ✅ | ✅ 75% | ❌ | ❌ | ❌ |
| **Watch 模式** | ✅ AI! 注释 | ❌ | ❌ | ✅ watch | ❌ | ❌ |
| **多模态** | ✅ 图片+音频 | ❌ | ✅ 图片+音频 | ❌ | ❌ | ✅ |
| **IDE 集成** | ✅ --json 桥接 | ✅ beta | ✅ | ✅ watch | ✅ 桌面 | ❌ |
| **上下文窗口** | 跟随模型⁴ | 1M | 200K | 模型决定 | 模型决定 | 1M |
| **中文原生** | ✅² | ❌ | ❌ | ❌ | ❌ | ❌ |
| **MCP 支持** | ✅ HTTP+SSE+目录+Claude共用 | ✅ 800+ | ✅ | ❌ | ✅ 原生 | ✅ |
| **彩色聊天 TUI** | ✅ v2 | ❌³ | ❌ | ❌ | ❌ | ❌ |
| **侧栏面板** | ✅ 4 标签页 | ❌ | ❌ | ❌ | ❌ | ❌ |
| **自测** | 4597 项 | 无 | 无 | 无 | 无 | 无 |
| **智能工作模式** | ✅ 4 模式 Shift+Tab | ❌ | ❌ | ❌ | ❌ | ❌ |
| **跨槽位消息** | ✅ F1-F10 | ❌ | ❌ | ❌ | ❌ | ❌ |
| **用户交互对话框** | ✅ AskUserQuestion 工具 | ✅ | ❌ | ❌ | ❌ | ❌ |
| **安装** | 单 exe | npm | npm/brew | pip | brew/cargo | npm/brew |
| **用量统计面板** | ✅ /stats | ✅ | ❌ | ❌ | ❌ | ❌ |
| **设置持久化** | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Checkpoint 持久化** | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

> ¹ WayCoder 为软件级沙箱（四级：off/project/network-off/hard，环境清理 + 内存监控），Codex CLI 为内核级沙箱（Seatbelt / bubblewrap / Windows 原生），安全层级不同——内核级更强，这是 WayCoder 短板。
> ² 中文原生渐成标配（通义灵码→Qoder CN、Trae 等亦中文优先），已非独家护城河；WayCoder 仍是中文原生**纯 CLI 智能体**中唯一带全屏 TUI + 学习系统的。
> ³ Claude Code 支持 ANSI 富文本聊天渲染，但非 WayCoder 级的全屏缓冲 TUI 控件库体系。
> ⁴ 上下文窗口跟随所选模型（如 deepseek-v4-pro=1M、deepseek-v4-flash=128K、未知模型兜底 128K），由 `ModelCatalog.ResolveContextWindow` 按模型解析、切模型时同步更新。

### 各竞品一句话总结

| 竞品 | 一句话 |
|------|--------|
| **Claude Code** | 最强自主性 + 动态工作流（1000 子智能体/run）+ Opus 4.8 + 闭源最贵，2026 CLI Agent 标杆（作者 ~10% 公共 GitHub 提交） |
| **Codex CLI** | Rust 高性能 + 内核级四级沙箱 + GPT-5.5 免费带，增长最快 |
| **Aider** | 最成熟开源 + Git 原生 + 100+ 模型随意换，但无 MCP 无子智能体 |
| **Goose** | Linux 基金会治理 + Block 全员部署，但编码正确率行业最低 |
| **Antigravity CLI** | Gemini CLI 继任（2026.6 日落），Go 重写 + 多 Agent 并行 + Gemini 3.5 Flash 免费档 |
| **Trae 3.0 / Qoder CN** | 国产 GUI 双子星（字节/阿里）：免费带模型 + 中文断层领先，但纯 CLI 深度不如 Claude Code/WayCoder |

---

## 二、WayCoder 差异化优势

### 🟢 独有优势（所有 CLI 竞品都没有）

| # | 优势 | 技术实现 | 护城河深度 |
|---|------|----------|:---:|
| 1 | **自主学习系统** | 知识库 /kb（mistake/bugfix/habit/gap/code 五类，TF-IDF+向量混合检索）+ 教学模式 /teach（讲解+测验闭环→gap 权重）+ 学习路径 /kb path + 会话复盘 /kb retro——竞品全是「记忆」，WayCoder 是「会学」 | ⭐⭐⭐ 极深 |
| 2 | **AOT 单文件部署** | .NET 10 NativeAOT → 单个 exe，0 依赖 | ⭐⭐⭐ 极深 |
| 3 | **内置 TUI 编辑器** | 14 种语言语法高亮 + 光标编辑 + 撤销栈 + Lint 诊断 | ⭐⭐⭐ 极深 |
| 4 | **双模型自动分工** | 大模型写代码，小模型做压缩/摘要，自动切换省钱 | ⭐⭐ 较深 |
| 5 | **全屏缓冲 TUI 控件库** | ScreenManager + 弹窗菜单 + 侧栏 + CJK 宽度感知 | ⭐⭐ 较深 |
| 6 | **中文原生纯 CLI** | 系统提示词 + 错误消息 + UI + 竞品分析全部中文；国产 GUI 竞品有中文但无纯 CLI 全屏 TUI | ⭐⭐ 较深 |
| 7 | **编辑级回滚** | 文件版本缓存（每次写前快照，按内容哈希分块）+ 整树 Checkpoint 快照 | ⭐⭐ 较深 |
| 8 | **文件锁机制** | 多 Agent 并发修改冲突防护，30s 超时自动释放 | ⭐⭐ 较深 |

### 🟡 相对优势（部分竞品有但不全）

| # | 优势 | 竞品情况 |
|---|------|------|
| 9 | **47 个内置工具** | Claude Code 相当，Aider 仅文件编辑，Goose 靠 MCP |
| 10 | **四层安全防护** | bash/git/rm/kill 四个工具各有独立拦截规则 |
| 11 | **模型回退链** | Claude Code 有，Codex/Aider/Goose 无自动回退 |
| 12 | **Hooks 生命周期** | Claude Code 更成熟，Codex 有，Aider/Goose 无 |
| 13 | **F1-F10 多槽位真并行** | Claude Code 有 Teams（共享任务列表+消息），WayCoder 是独立会话槽位，各有千秋 |
| 14 | **4597 项自测** | 所有竞品均无内置自测 |
| 15 | **环境变量精简（107→14）** | 对齐竞品（Claude Code/Codex 只留 API_KEY/BASE_URL/MODEL），config.json 权威源 |

---

## 三、已完成功能清单（截至 v0.87.17）

以下原是 Roadmap 中的 P0/P1 差距项，现已全部实现：

| 功能 | 版本 | 说明 |
|------|------|------|
| ✅ Git 自动提交 | v0.16.1 | `AutoGitCommit` + 小模型生成 commit message |
| ✅ CJK 感知 Token 估算 | v0.16.1 | CJK ~1.5 tok/char，ASCII ~0.25 tok/char，误差 <15% |
| ✅ Watch 模式 (AI! 注释) | v0.16.3 | FileSystemWatcher + 15+ 语言注释解析 + 线程安全队列 |
| ✅ 会话自动保存/恢复 | v0.16.2 | 退出自动保存 + 启动恢复提示 |
| ✅ 设置持久化 | v0.16.1 | Config.SaveToEnvFile + SettingsPage 保存按钮 |
| ✅ Diff-based Code Review | v0.16.2 | `git diff HEAD` 替代全文件内容 |
| ✅ Checkpoint 持久化 | v0.16.2 | 磁盘恢复检查点列表，重启后 `/undo` 不丢失 |
| ✅ AGENTS.md 支持 | v0.16.1 | 同时搜索 CLAUDE.md / AGENTS.md / .cursorrules |
| ✅ 对话历史搜索 | v0.16.0 | `/history` + `Ctrl+R` 交互搜索 |
| ✅ 用量统计面板 | v0.16.2 | `/stats` 模型/Token/花费/延迟全维度 |
| ✅ 自定义提示词模板 | v0.16.0 | 扫描 `.waycoder/prompt.md` 及 `.waycoder/*.md` |
| ✅ 项目初始化向导 | v0.16.0 | `waycoder --init` 创建配置目录和模板 |
| ✅ 输入历史 | v0.16.0 | ↑↓ 200 条，去重相邻重复 |
| ✅ 模型热键切换 | v0.16.0 | `Ctrl+M` 循环切换 4 个大模型 |
| ✅ Tab 路径补全 | v0.16.0 | 最长公共前缀 + 候选列表 |
| ✅ 自动 Test 循环 | v0.16.3 | `AppendTestFeedbackAsync` + 6 种构建系统 + 60s 防抖 |
| ✅ Prompt 缓存追踪 | v0.17.0 | SHA256 本地检测 + /stats 面板展示节省量 |
| ✅ 三级沙箱执行 | v0.17.1 | suggest/auto-edit/full-auto + 环境清理 + 内存监控 |
| ✅ 编辑器 Lint 诊断 | v0.17.2 | 保存时自动 lint + gutter 指示器 + 错误行高亮 + 状态栏 |
| ✅ MCP 协议完善 | v0.17.3 | HTTP/SSE 传输 + 传输抽象层 + 工具发现缓存 + 面板状态显示 |
| ✅ Terminal.Gui v2 TUI | v0.17.4 | 彩色聊天 + 侧栏面板 + 输入历史 + 会话恢复（v0.17.5 回退） |
| ✅ 恢复 AOT 编译 | v0.17.5 | 移除 Terminal.Gui v2 依赖，恢复 NativeAOT 单文件部署 |
| ✅ 子智能体递归 | v0.17.5 | 多层嵌套 + AsyncLocal 深度追踪 + 可配置深度（1-5） |
| ✅ `/undo` 按文件恢复 | v0.17.5 | filePath 参数 + 文件锁检查 + `/undo -l` 列出文件 |
| ✅ Lint/Tool 超时可配置 | v0.17.5 | ToolTimeoutSec (120s) + LintTimeoutSec (60s) 环境变量 |
| ✅ 并行子代理 | v0.19.0 | tasks 数组最多 4 并发 + 结果聚合，对齐 Claude Code Teams |
| ✅ SKILL.md 技能系统 | v0.19.0 | 标准技能格式发现 + 按需加载，对齐 Claude Code/Copilot/OpenCode |
| ✅ Git Worktree 隔离 | v0.19.0 | bash 自动检测 worktree 路径切换 cwd，对齐 Claude Code isolation |
| ✅ GitHub Actions CI | v0.19.0 | 自动构建 + 全量自测 |
| ✅ AutoGitCommit 质量校验 | v0.19.0 | conventional-commit 前缀强制 + 重试 + 兜底 |
| ✅ 结构化记忆 | v0.19.1 | `.corecoder/memory/*.md` frontmatter 多文件 + MEMORY.md 索引，首次使用自动迁移旧格式 |
| ✅ doc 文档查询工具 | v0.19.1 | 定向抓取官方文档（React/Next.js/Vue/DotNET 等 30+ 库），15 分钟会话缓存 |
| ✅ Diff 写前预览 | v0.19.1 | `WAYCODER_DIFF_PREVIEW=1` 开启逐 hunk 确认，非交互模式自动跳过 |
| ✅ 多 Agent 工作区 | v0.19.2 | F1-F10 切换 10 个独立会话槽位，各占各的屏幕 + 状态栏实时状态指示（独家） |
| ✅ 聊天显示风格 | v0.30.0 | detailed=全显示 auto=智能折叠(20行) concise=极简，设置界面可配 |
| ✅ 系统上限报告 | v0.30.1 | `--limits` 扫描 60 项系统上限，6 大类 + 4 级严重度 + ⚙可配/🔒硬编区分 (独家) |
| ✅ Markup 标记符号重构 | v0.30.0 | 方括号→书名号 `«»` 消除冲突，中文 `[]` 不再需双写 |
| ✅ TuiEditBase 键盘引擎 | v0.30.1 | 基类统一键盘分发（18 抽象原语），消除 TuiInput/TuiTextArea 335 行重复 OnKey |
| ✅ TuiTextArea 自动换行 | v0.30.1 | MaxColumnWidth（文字折行宽度）+ MaxLines（最大行数裁剪），可视区独立滚动 |
| ✅ Bash stderr 流式输出 | v0.30.2 | stderr 与 stdout 并行异步逐行读取，管道模式也支持 onToolOutput 流式 |
| ✅ 上限报告 60 项 | v0.30.2 | 新增 TuiEditBase/Bash 流式等 5 项上限探测 |
| ✅ 模式体系四轴正交 | v0.82~v0.83 | 权限/工作/边界/省钱四轴正交 + 行为轴三档（Chat/Plan/Build），`Shift+Tab` 切换，槽位实例级 |
| ✅ 模型配置三层重构 | v0.84.0 | connect / provider / connection 分层，`/connect <spec>` 统一入口，回退链开关 |
| ✅ 非 OpenAI 格式兼容 | v0.85.0 | Anthropic / Gemini 原生格式 + 本地模型 Ollama/LM Studio + 400 两级回退 |
| ✅ 模型能力显式标识 | v0.86.0 | SupportsThinking / SupportsTools / SupportsVision + `--model check` |
| ✅ 模型管理命令 | v0.87.0 | `--model report/free/clean` + API key 永不自动删除（删除需确认）|
| ✅ 跨工具会话桥接 | v0.87.11~v0.87.13 | `/join` 读取 Aider/Gemini CLI 会话历史，跨工具无缝续聊 |
| ✅ 竞品痛点九连击 | v0.87.14 | 可回滚/目标护栏/成本护栏/测试驱动/RAG/复现/可视化/离线/中文优化 |
| ✅ 代码级语义检索 | v0.87.16 | `CodeKnowledge` 扫描源码符号+文档注释分块，TF-IDF 语义召回代码段 |
| ✅ 子智能体依赖编排 | v0.87.16 | `depends_on` + DAG 拓扑分层调度，依赖输出注入，突破纯并行 4 并发 |
| ✅ MCP 生态目录 | v0.87.16 | 内置 18 个社区服务器目录 + `/mcp list` 浏览 + `/mcp add` 一键添加 |
| ✅ 子智能体失败重试 | v0.87.17 | `SubAgentRetryCount` 失败自动换方法重试，LLM 抽风给第二次机会 |
| ✅ 超大规模分批调度 | v0.87.17 | 批内并行批间串行，废除「超并行数即报错」，`SubAgentMaxTotalTasks` 硬上限防失控 |
| ✅ MCP 目录扩充 | v0.87.17~v0.87.20 | 内置 18→34 个，新增搜索/数据库/协作/服务四类生态服务器 |
| ✅ 环境变量精简 | v0.95.0 | 107→14（对齐竞品），config.json 权威源 |
| ✅ 内置编辑器三端 | v0.96.0 | TUI 完善（保存 lint Toast）+ Web 编辑器（透明 textarea 叠 pre + 文件树 + lint 标记）+ Avalonia GUI 编辑器（自定义 EditorView 绑 EditorCore）——共享 EditorCore 纯数据模型 |
| ✅ 修复 lint 全链路 | v0.96.0 | LintTool 读未启动 Process 的 ExitCode → 所有语言 lint 静默失效（潜伏 bug）；cs 改构建包含项目 + 截断 20000 + ⚠-跳过收窄 |
| ✅ CI 自测门禁 + GUI 构建 | v0.96.1 | ci.yml（push/PR）：主工程 + WayCoder.Gui 构建 + 4620 自测门禁（此前 CI 从不跑自测/不编译 GUI） |
| ✅ 编辑器补齐 | v0.96.1 | Web Ctrl+F/H 查找替换；GUI tab 展开 4 空格 + 横向滚动；LspTool 锁 3s 超时防死锁 |
| ✅ 修复源文件强加 BOM | v0.96.2 | 双代理测试发现 tetris.py 带 BOM；`Global.WriteAllTextPreserveBom`（原带 BOM 才保留）接入 5 处写文件点 |
| ✅ 工作模式 CLI 参数 | v0.96.3 | `--mode <build/plan/chat>` CLI 直接启动三模式（行为轴）；`--permit tiny/chat` 聊天别名仍兼容 |
| ✅ 系统提示词惰性生成 | v0.96.3 | 构造期按 Build 抢先生成被丢弃的昂贵工作消除：`EnsureSystemPrompt` 仅首次请求按当时模式/工具集生成并缓存；Chat 全程不生成 |

---

## 四、待实现路线图

### 📦 第一阶段：语义记忆 ✅ 已完成 (v0.25.0)

#### 1.1 语义记忆（⭐⭐ 体验增强 · 最后一项 P1）✅

> 结构化记忆已落地（v0.19.1：frontmatter 多文件 + MEMORY.md 索引），v0.25.0 实现向量语义搜索：

- ✅ TF-IDF 语义搜索（CJK bigram + 英文分词 + 时间新鲜度加权）替代关键词匹配
- ✅ 可选 Embedding API 向量嵌入（`/v1/embeddings`）+ 余弦相似度混合搜索
- ✅ 懒加载向量生成 + 原子写入 + 三层回退（Embedding → TF-IDF → 子串匹配）
- 小型本地嵌入模型（ONNX Runtime + all-MiniLM-L6-v2）
- 跨会话项目知识自动关联

---

### 📦 第二阶段：生态拓展 ✅ 全部完成

- **批量任务引擎** ✅ (v0.49.0)：`--batch`/`--batch-repo` 多仓库并行 + worktree 隔离 + 聚合报告，对标 Cursor 批量修复 / Aider 多仓库脚本
- **编译期插件系统** ✅ (v0.50.0)：`IPlugin` SDK + `[ModuleInitializer]` 自动注册，贡献工具与斜杠命令，AOT 无反射
- **IDE 桥接** ✅ (v0.51.0)：`--json` 一次性输出结构化 JSON，供 VS Code 扩展 / CI 脚本解析，对标 Codex `--json` / Claude Code `claude agents --json`
- **多模态支持** ✅ (v0.48.9)：图片（`view_image`）+ 音频（`transcribe`）+ 抓屏（`screenshot`），补齐 Codex CLI / Gemini CLI 的多模态短板
- **团队知识库共享** ✅ (v0.25.5)：多人项目共享 memory.md + git 同步
- **自动升级** ✅ (v0.48.7)：`UpdateChecker` 自替换，对标 Claude Code `claude update`

> 第二阶段原计划的「VS Code 扩展」以 `--json` 桥接的轻量方案落地（v0.51.0），完整 VS Code 扩展已交付 MVP（vscode-extension/，基于 --web SSE 流式对话）。

---

### 📦 第三阶段：跨设备接力（扫码接力 · 长期愿景）

> 「下班电脑出二维码，手机扫码接力改；到家再出码，家里电脑接着改」—— 工作现场在设备间无缝流转。

- **工作现场快照**：会话状态（`SessionManager` 已有）+ 代码（git 提交/补丁，依赖纯 C# git 子集）+ 记忆/上下文 + 未提交变更，打包为可传输的「接力包」
- **接力码**：二维码 / 共享码承载短 token，指向接力包（局域网直连 / 中转服务 / 自托管，三选一）
- **恢复接力**：扫码设备拉取接力包 → 恢复会话 + 工作目录 → 无缝继续编码
- **多端闭环**：桌面（TUI/CLI/GUI/Web）+ 移动端（MAUI）统一接力协议，五端互通

> 前置依赖：纯 C# git 子集（代码状态打包，进行中）+ 会话快照序列化（已有）+ 端到端传输通道（待建）。此阶段为长期愿景，需先完成移动端 git / MCP / import 等能力补齐。

---

## 五、实施优先级矩阵

```
                    高收益
                      │
                      │  1.1 语义记忆
                      │     (最后 P1)
                      │
  ─────────────────────┼──────────────────────
    低难度             │             高难度
                      │
                      │
                      │
                      │
                      │
                    低收益
```

---

## 六、建议执行顺序

```
Week 1-4 ─ 语义记忆（最后一项 P1）
  └── 本地嵌入模型（ONNX Runtime + all-MiniLM-L6-v2）

Week 5+ ─ 生态拓展（按需启动）
```

---

## 七、每月可衡量的里程碑

| 月份 | 交付物 | 考核指标 |
|------|--------|------|
| **M1** | 语义记忆 | 跨会话项目知识自动关联，ONNX 本地嵌入模型运行正常 |

---

## 八、核心差异化最终形态

```
         AOT 单文件部署 (0依赖, 双击即用)
         ┌──────────────────────────────┐
         │                              │
    中文原生 ── WayCoder v2.0 ── 终端 IDE
         │      差异化三角               │
         │                              │
         └──────────────────────────────┘
           Git 原生 (自动提交 + 自愈循环)

竞品做不到的:
  • AOT 单文件 → 不需要 Node/Python 运行时 ✅（注：Codex CLI 2026 已 Rust 单二进制，此项非独家）
  • 终端 IDE → 不需要外部编辑器 ✅ 14 语言 + 10 linter 诊断
  • 中文优先 → 唯一
  • 多 Agent 工作区 → F1-F10 十槽位独立会话 ✅ (v0.19.2)
  • 系统上限报告 → 60项6大类全量扫描 + ⚙可配/🔒硬编区分 ✅ (v0.30.1)

竞品做不好的:
  • 双模型省钱 → 用户无需手动切换
  • 四层安全 → 比沙箱更细粒度
  • 文件锁 → 多 Agent 并发安全
  • 彩色多角色 TUI → ANSI 全屏 ✅
  • 三层省钱体系 → Tiny + Economy + 双模型 ✅ (v0.43)

已补齐不输竞品的:
  • Git 自动提交 ✅ (v0.16.1)
  • Test 修复循环 ✅ (v0.16.3)
  • Prompt 缓存 ✅ (v0.17.0)
  • 沙箱执行 ✅ (v0.17.1)
  • Lint 诊断 ✅ (v0.17.2)
  • MCP HTTP/SSE ✅ (v0.17.3) + 资源/提示词 ✅ (v0.48.8)
  • 结构化记忆 ✅ (v0.19.1) + 语义搜索 ✅ (v0.25.0)
  • 多 Agent 工作区 ✅ (v0.19.2) + 真并行后台 ✅ (v0.48.6)
  • 聊天显示风格 + 性能测评 + 上限报告 ✅ (v0.30.0)
  • 多模态（图片+音频+抓屏）✅ (v0.48.9)
  • 自动升级自替换 ✅ (v0.48.7)
  • 批量任务引擎 ✅ (v0.49.0)
  • 编译期插件系统 ✅ (v0.50.0)
  • --json 结构化输出桥接 ✅ (v0.51.0)
  • 代码级语义检索 ✅ (v0.87.16)
  • 子智能体依赖编排 ✅ (v0.87.16)
  • MCP 生态目录 ✅ (v0.87.16)
  • 子智能体失败重试 ✅ (v0.87.17)
  • 超大规模分批调度 ✅ (v0.87.17，批内并行批间串行 + 总任务硬上限)
  • MCP 目录扩充 ✅ (v0.87.17~v0.87.20，18→34 个)

待补齐的（对标 2026 竞品新能力）:
  • 超大规模并行编排 → 已落地「分批调度」✅ (v0.87.17，几十~上百任务自动分批串行；跨仓库 worktree 隔离大规模编排仍待扩展)
  • 内核级沙箱（对标 Codex Seatbelt/Landlock）
  • VS Code 扩展 MVP ✅（vscode-extension/，--web SSE 流式对话 + 选中代码解释/修复）
  • MCP 生态目录自建 ✅ (v0.87.20，内置 34 个 + /mcp add 一键添加，Claude Code 有 800+ 仍在扩)

	第二阶段生态拓展全部补完 🎉（v0.49~v0.51）→ 第三阶段竞品短板补齐 🎉（v0.87.14~v0.87.17）
```

---

## 九、2026-08-25 竞品差异复核

| 维度 | WayCoder 现状 | 竞品参照 | 结论 |
|---|---|---|---|
| 模式体系 | 确认轴（`/permit`）、边界轴（`/perm`）、行为轴、省钱轴四轴正交 | Codex `sandbox_mode × approval_policy`、Claude permission modes | 已按竞品解耦，组合预设仍保留 `--yolo` / `--permission-mode bypassPermissions` |
| 代理连通 | LLM 代理读取 `HTTP(S)_PROXY` 时同时遵守 `NO_PROXY`，回环地址自动绕过 | curl / 现代 CLI 标准行为 | 修复本地 Ollama/LM Studio 与自测 mock 被代理劫持的问题 |
| 自测隔离 | 自测全局配置目录重定向到临时 home，不写真实用户目录 | CI 常见测试隔离 | 修复受限环境 `UnauthorizedAccessException` 崩溃 |
| MCP 生态 | 内置 87 个服务器 + `/mcp add`（npx/uvx 双启动）+ 零配置共用 Claude Code MCP | Claude Code 800+ 生态 | 目录数量仍与 800+ 有差距，但「共用 Claude Code 配置」抹平了生态壁垒，87 个精选目录已覆盖热门方向（含国内通讯微信/QQ + 国内搜索百度/SearXNG），下一步补 Docker 启动 |
| 安全层级 | 软件沙箱（cwd/环境/资源限制） | Codex Seatbelt/Landlock 内核级 | 短期保留“软件沙箱 + 文件锁 + SSRF + 命令防护”组合；内核级留作长期 |
| IDE 集成 | `--json` / `--output-format json` 桥接 | Claude Code beta、Codex VS Code 扩展 | 轻量桥接够用；完整扩展 MVP 已交付（vscode-extension/） |

本轮已落地：
- `LLM` 代理选择遵守 `NO_PROXY`，本地回环请求不再经过失效代理。
- 自测通过 `Global.HomeOverride` 隔离会话/检查点等全局写入。
- `SandboxManager.SetLevel` 与 `PermissionManager` 解耦；`--permission-mode` 三档映射补齐。
- `AllowedTools` 接入通用白名单决策链；`Ctrl+E` 切换经济档位后刷新并持久化。
- `ToolSafetyRegistry` 统一工具级风险数据，权限确认与智能分类不再维护两份名单。
- MCP 内置目录从 29 扩到 34：新增 GitLab、Redis、MySQL、PDF、AWS KB RAG、Google Drive。
- MCP 内置目录 35→40：新增「部署」分类（Netlify）+ 搜索（Perplexity/DuckDuckGo）+ 开发（Figma/Chrome DevTools），并引入 uvx 启动方式（DuckDuckGo 走 Python/uvx）。
- 零配置共用 Claude Code MCP：`ClaudeMcp` 读取三处配置源（`.mcp.json` / `~/.claude.json` user 级 / project 级），`type`→`transport` 自动映射，同名去重后追加到 WayCoder，`/mcp` 标注 `〔Claude〕` 来源。
- MCP 内置目录 40→47：补齐向量库四件套（Chroma/Qdrant/Elasticsearch/Weaviate）+ 云沙箱 E2B + 浏览器云 Browserbase + 邮件 Resend，包名均经 `npm view` 逐条核实。
- MCP 内置目录 47→83（达 80+ 目标）：新增数据仓库（Snowflake/DuckDB/ClickHouse）、云平台（AWS/Google Cloud/Firebase/DigitalOcean）、通讯（Discord/Telegram/WhatsApp/Twilio）、CRM/办公（Salesforce/HubSpot/Gmail/Calendar/Shopify/Zendesk/Mailchimp/Trello/ClickUp）、开发（Blender/K8s/Midscene/Magic/Composio/OpenRouter/PostHog）、服务（Weather/Spotify/Zapier/n8n/Datadog）等 36 个，包名均经 `npm view` 逐条核实。
- MCP 内置目录 83→87：通讯补微信（`weixin-mcp` 扫码即用）+ QQ（`qq-mcp` 经 HTTP API 发群消息，uvx 启动），搜索补国内百度（`baidu-search-mcp` 免费）+ SearXNG（`searxng-mcp` 自托管元搜索），包名均经 npm/PyPI 核实。
- 补竞品短板五连：上下文压缩预告/回看（`CompactionWarning`/`CompactionOccurred` + 有界 `CompactionHistory`）、修完必验证闭环门（`VerifyBeforeDone`，收尾前强制验证防「假修好了」）、子智能体明文审计日志（`.waycoder/audit/subagents.log`）、任务漂移护栏加强（逐级 goal_check + 文件清单）、状态栏显示 cwd + git 分支（`PathStatus` 支持 worktree/detached HEAD）。

下一优先项：
- MCP 内置目录补一键 Docker 启动模板（目录目前 0 个 docker 启动条目，`McpCatalog` 的 `Command="docker"` 能力已就绪待填充）。
- VS Code 扩展 MVP 已交付（vscode-extension/，--web SSE 流式对话；补齐 diff 预览/运行任务仍可后续增强）。
- 完整 VS Code 扩展复用 `--json` 桥接，补齐安装、运行任务、diff 预览三个核心界面。
