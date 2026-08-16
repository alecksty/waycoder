# WayCoder（道码）竞品分析与路线图

> 版本：v0.69.0 | 日期：2026-08-16

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
| **子智能体** | ✅ 并行 4 并发 | ✅ Teams | ✅ V2 | ❌ | ✅ via MCP | ❌ |
| **沙箱执行** | ✅ 三级¹ | ❌ | ✅ 三级 | ❌ | ❌ | ❌ |
| **自动 Lint 反馈** | ✅ | ✅ | ✅ | ✅ lint+test | ❌ | ❌ |
| **自动 Test 循环** | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Prompt 缓存** | ✅ | ✅ | ✅ 75% | ❌ | ❌ | ❌ |
| **Watch 模式** | ✅ AI! 注释 | ❌ | ❌ | ✅ watch | ❌ | ❌ |
| **多模态** | ✅ 图片+音频 | ❌ | ✅ 图片+音频 | ❌ | ❌ | ✅ |
| **IDE 集成** | ✅ --json 桥接 | ✅ beta | ✅ | ✅ watch | ✅ 桌面 | ❌ |
| **上下文窗口** | 128K | 1M | 200K | 模型决定 | 模型决定 | 1M |
| **中文原生** | ✅² | ❌ | ❌ | ❌ | ❌ | ❌ |
| **MCP 支持** | ✅ HTTP+SSE | ✅ 800+ | ✅ | ❌ | ✅ 原生 | ✅ |
| **彩色聊天 TUI** | ✅ v2 | ❌³ | ❌ | ❌ | ❌ | ❌ |
| **侧栏面板** | ✅ 4 标签页 | ❌ | ❌ | ❌ | ❌ | ❌ |
| **自测** | 3070 项 | 无 | 无 | 无 | 无 | 无 |
| **智能工作模式** | ✅ 4 模式 Shift+Tab | ❌ | ❌ | ❌ | ❌ | ❌ |
| **跨槽位消息** | ✅ F1-F10 | ❌ | ❌ | ❌ | ❌ | ❌ |
| **用户交互对话框** | ✅ AskUserQuestion 工具 | ✅ | ❌ | ❌ | ❌ | ❌ |
| **安装** | 单 exe | npm | npm/brew | pip | brew/cargo | npm/brew |
| **用量统计面板** | ✅ /stats | ✅ | ❌ | ❌ | ❌ | ❌ |
| **设置持久化** | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Checkpoint 持久化** | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

> ¹ WayCoder 为软件级沙箱（环境清理 + 内存监控），Codex CLI 为内核级沙箱（Seatbelt / Landlock），安全层级不同。
> ² 中文原生渐成标配（通义灵码、Trae 等亦中文优先），已非独家护城河。
> ³ Claude Code 支持 ANSI 富文本聊天渲染，但非 WayCoder 级的全屏缓冲 TUI 控件库体系。

### 各竞品一句话总结

| 竞品 | 一句话 |
|------|--------|
| **Claude Code** | 最强自主性 + 最贵 + 闭源，2026 CLI Agent 标杆 |
| **Codex CLI** | Rust 高性能 + 开源 + ChatGPT Plus 免费带，增长最快 |
| **Aider** | 最成熟开源 + Git 原生 + 100+ 模型随意换，但无 MCP 无子智能体 |
| **Goose** | Linux 基金会治理 + Block 全员部署，但编码正确率行业最低 |
| **Gemini CLI** | 曾免费 1M 上下文，2026.6 停止免费后被 Antigravity 取代 |

---

## 二、WayCoder 差异化优势

### 🟢 独有优势（所有 CLI 竞品都没有）

| # | 优势 | 技术实现 | 护城河深度 |
|---|------|----------|:---:|
| 1 | **AOT 单文件部署** | .NET 10 NativeAOT → 单个 exe，0 依赖 | ⭐⭐⭐ 极深 |
| 2 | **内置 TUI 编辑器** | 14 种语言语法高亮 + 光标编辑 + 撤销栈 | ⭐⭐⭐ 极深 |
| 3 | **双模型自动分工** | 大模型写代码，小模型做压缩/摘要，自动切换省钱 | ⭐⭐ 较深 |
| 4 | **全屏缓冲 TUI 控件库** | ScreenManager + 弹窗菜单 + 侧栏 + CJK 宽度感知 | ⭐⭐ 较深 |
| 5 | **中文原生** | 系统提示词 + 错误消息 + UI + 竞品分析全部中文 | ⭐⭐ 较深 |
| 6 | **文件锁机制** | 多 Agent 并发修改冲突防护，30s 超时自动释放 | ⭐⭐ 较深 |

### 🟡 相对优势（部分竞品有但不全）

| # | 优势 | 竞品情况 |
|---|------|------|
| 7 | **44 个内置工具** | Claude Code 相当，Aider 仅文件编辑，Goose 靠 MCP |
| 8 | **四层安全防护** | bash/git/rm/kill 四个工具各有独立拦截规则 |
| 9 | **模型回退链** | Claude Code 有，Codex/Aider/Goose 无自动回退 |
| 10 | **Hooks 生命周期** | Claude Code 更成熟，Codex 有，Aider/Goose 无 |
| 11 | **自定义命令** | Claude Code 有 slash commands，Codex 有 |
| 12 | **3070 项自测** | 所有竞品均无内置自测 |

---

## 三、已完成功能清单（v0.17.4）

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

> 第二阶段原计划的「VS Code 扩展」以 `--json` 桥接的轻量方案落地（v0.51.0），完整 VS Code 扩展留作 P1。

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

待补齐的（对标 2026 竞品新能力）:
  • 上下文窗口突破 128K（对标 Claude Code/Gemini/Cline 1M）
  • 超大规模并行编排（对标 Claude Code Dynamic Workflows 几十~几百子代理）
  • 内核级沙箱（对标 Codex Seatbelt/Landlock）
  • VS Code 扩展（复用 --json 桥接）

	第二阶段生态拓展全部补完 🎉（v0.49~v0.51）
```
