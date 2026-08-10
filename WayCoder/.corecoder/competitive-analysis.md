# WayCoder（道码）v0.25.6 竞品对比分析

> 分析日期：2026-08-10

---

## 一、市场定位对比

| 维度 | WayCoder | Claude Code | Aider | Cursor | Cline | OpenCode |
|------|----------|-------------|-------|--------|-------|----------|
| **类型** | 终端 TUI | 终端 CLI | 终端 CLI | IDE (VS Code fork) | VS Code 扩展 | 终端 TUI |
| **技术栈** | C# .NET 10 AOT | TypeScript (Node) | Python | TypeScript (Electron) | TypeScript | Go |
| **部署** | **单文件 exe (30MB)** | npm install | pip install | 完整 IDE 安装 | VS Code 扩展 | go install / 单二进制 |
| **模型支持** | OpenAI 兼容 (15+模型) | Anthropic 独占 | **模型无关 (100+)** | 多模型 | **模型无关** | 75+ 提供商 |
| **许可证** | MIT | 专有 | Apache 2.0 | 专有 | Apache 2.0 | MIT |
| **安装量** | — | — | 高 | 极高 | 5M+ VS Code | 早期 |
| **中文支持** | **一等公民 (CJK 宽度计算)** | 有限 | 有限 | 有限 | 有限 | 有限 |

### 核心差异化

1. **单文件部署** — C# AOT 编译，零运行时依赖，下载即用。所有竞品需要 Node/Python/Go 运行时
2. **中文优先** — CJK 宽度精确计算、中文提示词、中文 TUI 界面
3. **全功能 TUI** — 内置编辑器 (14 语言语法高亮)、多槽位、设置界面、Diff 预览

### 推荐定位

**"最易部署的中文 AI 编程智能体"** — 对标 Claude Code + Aider 的终端市场，不与 Cursor (GUI IDE) 正面竞争。以"开箱即用"（单 exe + 内置编辑器 + LSP）超越它们。

---

## 二、WayCoder 相对领先的维度

### 2.1 部署与分发 ⭐⭐⭐⭐⭐
- **独有**：C# AOT → 单文件 exe，无运行时依赖
- 竞品：Claude Code 需 Node/npm，Aider 需 Python/pip，OpenCode 需 Go 工具链
- 对国内用户：无需配置 Python 虚拟环境，不需要 Node 版本管理

### 2.2 终端 TUI 完整度 ⭐⭐⭐⭐⭐
- 25+ 控件类型，自研 immediate-mode ANSI 渲染引擎
- 内置编辑器 (14 语言语法高亮 + 诊断集成) — 竞品无
- 设置界面 (Schema 自动布局) — 竞品无
- Diff 预览 (逐 hunk 确认 Y/N/A/Q) — 仅 WayCoder
- 多 Agent 槽位 F1-F10 + 状态栏指示 — 独有
- 弹窗菜单 + 侧栏 + Toast 通知 — 竞品无

### 2.3 上下文压缩 ⭐⭐⭐⭐
- 三层渐进压缩 (50%裁剪 → 70% LLM 摘要 → 90% 硬折叠)
- CJK 感知 token 估算 (±15%)
- SafeSplit 保证 tool/assistant 消息不孤立的约束
- 比 Claude Code 的单层压缩更精细

### 2.4 架构质量 ⭐⭐⭐⭐
- Config Schema 驱动 — 单一声明源，SettingSchema/FromEnv/SaveToEnvFile 自动推导
- SlashCommand 注册表 — 40 个命令统一管理，注册顺序即优先级
- SelfTest 844 项 — 多数竞品无系统性自测
- SectionModuleMap 字典化 — 替代 switch 语句

### 2.5 记忆系统 ⭐⭐⭐⭐
- 多文件 frontmatter 记忆 + TF-IDF + 向量嵌入混合搜索
- Wiki-link `[[交叉引用]]` 语法
- Git 同步团队记忆 (独有)
- 自动迁移旧格式

### 2.6 创新功能 ⭐⭐⭐⭐
- Watch 模式 (AI! 注释自动触发) — 独有
- 检查点/撤销 (文件级，比 Aider 的 git 更细粒度)
- 自动 lint 反馈循环 (write_file 后自动 lint)
- 自动测试反馈循环 (写代码后自动跑测试)
- 双模型架构 + 回退链 (大模型复杂任务，小模型省钱)

---

## 三、关键差距（按严重程度排序）

### 🔴 P0 — 严重影响用户体验或安全性

#### 1. 终端 Resize 无响应 ⚠️ 已修复 + 流式场景仍需处理 (Bug 7)
- **现象**：Ctrl+Plus/Ctrl+Minus 缩放终端 → 不重新布局
- **根因**：`Program.cs` resize 事件只调 `mgr.Render()`，未调 `mgr.OnResize()`。`Render()` 因 `IsDirty=false` 直接返回
- **修复**：已在 `Program.cs:371` 添加 `mgr.OnResize()` 调用 + 在 `ChatScreen.cs:OnResize` 添加消息保存/恢复
- **遗留**：LLM 流式输出期间 resize 仍然会渲染错乱——token 回调直接调 `screen.Render()` 不经过 REPL 的 resize 检测

#### 2. 无 LSP 实时诊断集成（最大功能差距）
- **竞品**：OpenCode 自动安装 30+ LSP 服务器，TUI 内实时红色波浪线
- **WayCoder**：`LspTool` 仅一次性查询 (hover/references/goto-def)，**每次查询启动新进程**（500ms 冷启动），用完即销毁。实现了 LSP 规范的 4/30+ 方法，无 `textDocument/completion`、`textDocument/publishDiagnostics`、`textDocument/codeAction`
- **影响**：写代码时看不到实时错误，LLM 也无法利用诊断上下文自我纠正
- **预估**：5-7 天（~800-1200 行），需要 LSP 服务器生命周期管理（长连接保活）、增量文档同步、自动下载二进制、跨平台服务器路径

#### 3. 子智能体无 Worktree 隔离
- **2026 最佳实践**：Claude Code 子智能体在独立 git worktree 中运行，避免并发修改冲突
- **WayCoder**：`AgentTool` 直接在当前目录操作，仅靠 `FileLockManager` (30s 超时) 防并发。**关键发现**：项目内已有完整的 `WorktreeIsolation.cs`（创建 worktree、隔离 BashTool cwd、自动清理），但 `AgentTool.RunSubAgentAsync` 完全没有调用它——worktree 模块是死代码！
- **影响**：多个子智能体并发时可能互相覆盖修改，`edit_file` 的 old_string 可能失效
- **预估**：2-3 天（~300-500 行），主要是**连线工作**：将 AgentTool 与 WorktreeIsolation 对接

#### 4. 编辑器只有单层撤销
- **现象**：`Editor.cs` 撤销栈仅一层 (line snapshot)，Ctrl+Z 只能回退一步
- **影响**：多步操作后无法回退到初始状态
- **预估**：1 天工作量，改为 `Stack<EditorSnapshot>`

#### 附：TUI 显示完整性 Bug（调查发现 10 个）
| # | 严重度 | 问题 | 影响 |
|---|--------|------|------|
| 1 | 🔴 HIGH | `/plan` 绕过 TUI 直接 `Console.WriteLine` → 画面撕裂 | 每次用 /plan |
| 2 | 🔴 HIGH | `!` shell 命令直接 `Console.WriteLine(result)` → 画面撕裂 | 每次用 ! |
| 3 | 🟡 MED | `\a` 响铃字符混入 ANSI 流 → 部分终端乱码 | Agent 完成时 |
| 4 | 🟡 MED | Editor lint `Task.Run` + `Render()` 后台线程写 Console → 乱码 | 编辑器保存时 |
| 5 | 🟡 MED | Editor 未正确挂起 TUI alt-screen → 内容残留 | 编辑器退出后 |
| 6 | 🟡 MED | Ctrl+C `Environment.Exit(0)` 跳过所有清理 → 鼠标追踪残留 + 会话丢失 | 每次 Ctrl+C |
| 7 | 🟡 MED | LLM 流式 resize 渲染错乱 (见 P0 #1 遗留) | 流式输出时缩放 |
| 8 | 🟢 LOW | `AutoCommitAsync` 空 catch 吞掉 git 部分失败 | 低概率 |
| 9 | 🟢 LOW | 崩溃退出时鼠标追踪残留 | 崩溃时 |
| 10 | 🟢 LOW | `AutoSaveSession` 在 Ctrl+C/崩溃时不触发 | 非正常退出时 |

### 🟡 P1 — 功能缺失，高频场景受限

#### 5. 无 Agent Skills 渐进式加载
- **竞品**：Claude Code 的 `SKILL.md` 按需加载，只在 LLM 决定使用技能时才注入 prompt
- **WayCoder**：`CustomCommands` 从 .md 加载但只是简单 prompt 模板，`SkillTool` 有 skill 目录加载
- **差距**：不支持渐进式披露 (progressive disclosure)，所有 skill 描述全量注入系统提示词
- **影响**：skill 数量增加后系统提示词膨胀
- **预估**：3-4 天

#### 6. Git 提交消息无描述性
- **竞品**：Aider 每次 AI 编辑自动生成描述性 conventional commit message
- **WayCoder**：`Agent.cs` 中有 `AutoGitCommit`，用 small model 生成 conventional commit，已有基础
- **差距**：实际消息质量待验证，语言限制 "English only, <70 chars, no Chinese"。且**默认关闭** (`autoCommit = false`)，多数用户不会发现
- **影响**：Git 历史可追溯性不如 Aider
- **预估**：0.5-1 天优化

#### 7. Token 效率偏低
- **数据参考**：Aider 比 Claude Code 少用 4.2 倍 token
- **WayCoder 现状**：RepoMapGenerator 全量生成 ASCII tree，注入系统提示词。符号提取是正则而非 AST (可能捕获假符号)
- **可优化点**：
  - PageRank 排序文件重要性 (参考 Aider)
  - 按需注入而非全量 (当前 100 条目上限已有限制但无优先级排序)
  - 增量更新而非全量重建 (当前有 2 分钟 TTL 缓存)
- **预估**：3-4 天

#### 8. 跨平台 AOT 仅 Windows + LSP URI Bug
- **AOT**：仅 `win-x64` RID，需 `linux-x64`、`osx-arm64`
- **LSP Bug**：`LspTool.FileToUri` 在 Linux 上 `TrimStart('/')` 会吃掉根斜杠 → `file:///home/...` 变成 `file:///home/...`（缺少 `/` → LSP 全功能不可用）
- **影响**：Mac/Linux 用户无法使用
- **预估**：0.5 天 (AOT 改 csproj + 修复 URI 逻辑)

#### 9. 编辑器内无搜索
- **现象**：`Editor.cs` 无 Ctrl+F 搜索、无替换功能
- **影响**：编辑大文件效率低
- **预估**：1-2 天

### 🟢 P2 — 锦上添花

#### 10. 会话无法分享
- **竞品**：OpenCode `/share` 生成只读公开链接
- **预估**：2 天

#### 11. 无外部 $EDITOR 集成
- **竞品**：OpenCode `Ctrl+E` → 打开系统 $EDITOR 写多行提示词
- **WayCoder**：仅有内置 TUI 编辑器
- **预估**：0.5 天

#### 12. MCP 连接无自动重连
- **现状**：`McpManager.Init()` 一次性连接，无 `Process.Exited` 事件，无健康检查，无自动重启。stdio 传输也不支持流式响应。整个 MCP 协议只实现了 `tools/list`，未实现 `resources/list` 和 `prompts/list`
- **影响**：MCP 服务器崩溃后工具消失，需重启 WayCoder
- **预估**：1-2 天（~500-700 行）

#### 13. WorktreeIsolation 模块是死代码
- **现状**：`WorktreeIsolation.cs` 完整实现了 git worktree 创建/隔离/清理，但 `AgentTool.RunSubAgentAsync` 完全没有调用它
- **影响**：P0 的第 3 项修复实际上只需要**连线**，不需要重新实现
- **预估**：已在 P0 #3 中涵盖

#### 14. CustomCommands 过于简陋
- **现状**：仅 `$1` (所有参数合并)，无 `$2`、`$3`、`argv`，无条件执行。YAML 解析是手写正则
- **预估**：1 天

#### 15. 无 Benchmark 系统
- **竞品**：Aider polyglot benchmark (225 题)，Claude Code SWE-bench
- **预估**：2-3 天

#### 16. VS Code 扩展
- **预估**：5-7 天 (可延后到 v1.0)

---

## 四、架构层面改进建议

基于"统一提炼"的实践经验，可继续推进：

| # | 改进项 | 描述 | 节省 |
|---|--------|------|------|
| 1 | **Tools 参数声明式注册** | 31 个工具各手写 JSON Schema + 参数提取，用 `[ToolParam]` 属性统一 | ~300 行 |
| 2 | **SystemPrompt 分段模型** | 80 行字符串拼接 → `IPromptSection` 注册模型，按需注入 | ~100 行 |
| 3 | **ReviewMode/PlanMode 基类** | 两个模式共享项目上下文加载逻辑，提取 `AgentMode` 基类 | ~150 行 |
| 4 | **Memory 系统三合一** | `MemoryStore` + `StructuredMemory` + `SemanticMemory` → 统一 `IMemoryStore` | 复杂度降 |

---

## 五、改进路线图建议

### 第一阶段：体验修复 (1 周)
1. ✅ Resize 响应修复 (1 行)
2. 编辑器撤销栈 (1 天)
3. 跨平台 AOT (0.5 天)
4. 编辑器内搜索 (1 天)
5. 外部 $EDITOR 集成 (0.5 天)

### 第二阶段：能力追赶 (3 周)
6. LSP 实时诊断 (5-7 天)
7. 子智能体 Worktree 隔离 (2-3 天)
8. Git 提交消息优化 (1 天)
9. Token 效率优化 (3-4 天)

### 第三阶段：差异化增强 (3 周)
10. Agent Skills 渐进式加载 (3-4 天)
11. MCP 自动重连 (1-2 天)
12. 会话分享 (2 天)
13. Benchmark 系统 (2-3 天)

### 第四阶段：架构深化 (2 周)
14. Tools 参数声明式注册
15. SystemPrompt 分段模型
16. Memory 系统三合一
17. VS Code 扩展 (可选)

---

## 六、竞品功能矩阵 (详细)

| 功能 | WayCoder | Claude Code | Aider | OpenCode |
|------|----------|-------------|-------|----------|
| **终端 UI** | 全功能 TUI (25 控件) | 基础 CLI | 纯文本行 | Bubble Tea TUI |
| **内置编辑器** | ✅ 14 语言高亮 | ❌ | ❌ | ❌ |
| **多 Agent 槽位** | ✅ F1-F10 | ❌ (单会话) | ❌ | ❌ |
| **子智能体** | ✅ AgentTool | ✅ 子智能体 | ❌ | ✅ 自定义 Agent |
| **Worktree 隔离** | ❌ (仅 FileLock) | ✅ | 不适用 | ❌ |
| **LSP 实时诊断** | ❌ (仅一次性查询) | ❌ | ❌ | ✅ 30+ 服务器 |
| **Auto-commit** | ✅ (small model) | ❌ | ✅ (描述性) | ❌ |
| **检查点/撤销** | ✅ 文件级 | ❌ | ✅ (git) | ❌ |
| **权限系统** | ✅ 三级沙箱 | ✅ | ❌ | ✅ |
| **MCP** | ✅ stdio+HTTP | ✅ 完整 | ❌ | ✅ |
| **Watch 模式** | ✅ (独有) | ❌ | ❌ | ❌ |
| **团队记忆** | ✅ Git 同步 | ❌ | ❌ | ❌ |
| **向量嵌入** | ✅ 混合搜索 | ❌ | ❌ | ❌ |
| **Settings UI** | ✅ Schema 驱动 | ❌ | ❌ | ❌ |
| **会话分享** | ❌ | ❌ | ❌ | ✅ /share |
| **Skills 系统** | ⚠️ 基础 | ✅ SKILL.md | ❌ | ✅ 兼容 |
| **Diff 预览** | ✅ 逐 hunk | ❌ | ❌ | ❌ |
| **自测** | ✅ 844 项 | ❌ | ✅ 225 项 | ❌ |
| **Token 效率** | ⚠️ 全量 RepoMap | ⚠️ | ✅ PageRank | ⚠️ |
| **跨平台** | ⚠️ 仅 Windows | ✅ | ✅ | ✅ |
| **VS Code 集成** | ❌ | ✅ | ❌ | ❌ |
| **多模态** | ❌ | ✅ (图片) | ❌ | ❌ |

---

## 七、总结

WayCoder 的**核心优势**在于：
1. **单文件部署** — 竞品无人能及
2. **全功能 TUI** — 终端体验在竞品中最佳
3. **中文优先** — 国内市场差异化

**最大短板**：
1. LSP 实时诊断 — 现代编程工具的基础设施
2. 子智能体 Worktree 隔离 — 2026 年行业标配
3. 终端 Resize 不响应 — 已修复

**一句话建议**：聚焦"开箱即用"体验，用 LSP + Worktree 补齐基础设施，保持在终端 AI 编程工具的第一梯队。
