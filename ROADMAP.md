# CoreCoder 竞品分析与路线图

> 版本：v0.14.0 | 日期：2026-08-07

---

## 一、竞品全景对比（仅 CLI）

| 指标 | **CoreCoder** | Claude Code | Codex CLI | Aider | Goose | Gemini CLI |
|------|:---:|:---:|:---:|:---:|:---:|:---:|
| **开源** | ❓ | ❌ | ✅ | ✅ | ✅ | ✅ |
| **语言** | C# (.NET 10) | TypeScript | Rust | Python | Rust | TS/Go |
| **AOT 单文件** | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **内置 TUI 编辑器** | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **双模型架构** | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **模型灵活度** | OpenAI 兼容 | 仅 Anthropic | OSS 模式 | 100+ 模型 | 15+ 提供商 | 仅 Gemini |
| **Git 自动提交** | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ |
| **子智能体** | ✅ 单层 | ✅ Teams | ✅ V2 | ❌ | ✅ via MCP | ❌ |
| **沙箱执行** | ❌ | ❌ | ✅ 三级 | ❌ | ❌ | ❌ |
| **自动 Lint 反馈** | ✅ | ✅ | ✅ | ✅ lint+test | ❌ | ❌ |
| **自动 Test 循环** | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Prompt 缓存** | ❌ | ✅ | ✅ 75% | ❌ | ❌ | ❌ |
| **多模态** | ❌ | ❌ | ✅ 图片+音频 | ❌ | ❌ | ✅ |
| **IDE 集成** | ❌ | ✅ beta | ✅ | ✅ watch | ✅ 桌面 | ❌ |
| **上下文窗口** | 128K | 1M | 200K | 模型决定 | 模型决定 | 1M |
| **中文原生** | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **MCP 支持** | ✅ 基础 | ✅ 800+ | ✅ | ❌ | ✅ 原生 | ✅ |
| **自测** | 280 项 | 无 | 无 | 无 | 无 | 无 |
| **安装** | 单 exe | npm | npm/brew | pip | brew/cargo | npm/brew |

### 各竞品一句话总结

| 竞品 | 一句话 |
|------|--------|
| **Claude Code** | 最强自主性 + 最贵 + 闭源，2026 CLI Agent 标杆 |
| **Codex CLI** | Rust 高性能 + 开源 + ChatGPT Plus 免费带，增长最快 |
| **Aider** | 最成熟开源 + Git 原生 + 100+ 模型随意换，但无 MCP 无子智能体 |
| **Goose** | Linux 基金会治理 + Block 全员部署，但编码正确率行业最低 |
| **Gemini CLI** | 曾免费 1M 上下文，2026.6 停止免费后被 Antigravity 取代 |

---

## 二、CoreCoder 差异化优势

### 🟢 独有优势（所有 CLI 竞品都没有）

| # | 优势 | 技术实现 | 护城河深度 |
|---|------|----------|:---:|
| 1 | **AOT 单文件部署** | .NET 10 NativeAOT → 单个 exe，0 依赖 | ⭐⭐⭐ 极深 |
| 2 | **内置 TUI 编辑器** | 14 种语言语法高亮 + 光标编辑 + 撤销栈 | ⭐⭐⭐ 极深 |
| 3 | **双模型自动分工** | 大模型写代码，小模型做压缩/摘要，切换自动省钱 | ⭐⭐ 较深 |
| 4 | **全屏缓冲 TUI 控件库** | ScreenManager + 弹窗菜单 + 侧栏 + CJK 宽度感知 | ⭐⭐ 较深 |
| 5 | **中文原生** | 系统提示词 + 错误消息 + UI 全部中文 | ⭐⭐ 较深 |

### 🟡 相对优势（部分竞品有但不全）

| # | 优势 | 竞品情况 |
|---|------|------|
| 6 | **29 个内置工具** | Claude Code 相当，Aider 仅文件编辑，Goose 靠 MCP |
| 7 | **四层安全防护** | bash/git/rm/kill 四个工具各有独立拦截规则 |
| 8 | **模型回退链** | Claude Code 有，Codex/Aider/Goose 无自动回退 |
| 9 | **Hooks 生命周期** | Claude Code 更成熟，Codex 有，Aider/Goose 无 |
| 10 | **自定义命令** | Claude Code 有 slash commands，Codex 有 |
| 11 | **280 项自测** | 所有竞品均无内置自测 |

---

## 三、需要补齐的短板

### 🔴 P0 — 严重差距（影响核心竞争力）

| 差距 | 现状 | 目标 |
|------|------|------|
| **无 Git 自动提交** | 修改文件后无版本记录 | 每次 write/edit 自动 git add + commit |
| **无自动 Test 循环** | 只有 lint 反馈，不跑测试 | 自动跑测试 → 失败 → 修复 → 重测 |
| **无 Prompt 缓存** | 每次全量发送上下文 | 系统提示词 + 工具定义缓存命中 |

### 🟠 P1 — 中等差距

| 差距 | 现状 | 目标 |
|------|------|------|
| **Token 估算粗放** | `len/3` 误差 ~30% | 引入 tiktoken 等价物或 CIK 加权估算 |
| **无 IDE 集成** | 纯 CLI | Aider 式 `--watch-files` 监听外部编辑器改动 |
| **无 AGENTS.md 标准** | 仅 CLAUDE.md | 同时支持 AGENTS.md（60,000+ 项目在用） |
| **会话不自动保存** | 需手动 /save | 退出时自动保存，启动时恢复 |
| **Review 不跟踪 bash 修改** | 只跟踪 write/edit | 也跟踪 git diff 中的改动 |
| **Checkpoint 仅内存** | 重启丢失检查点列表 | 持久化到磁盘 |

### 🟡 P2 — 轻度差距

| 差距 | 说明 |
|------|------|
| `/undo` 不支持指定文件 | Aider 每次修改独立 commit，精确 revert |
| 设置改动不持久化 | 设置界面修改后关闭即丢失 |
| MCP 仅支持 stdio | 不支持 SSE / Streamable HTTP |
| 无用量统计面板 | 用户看不到总 token、总花费、会话数 |
| Lint/Tool 超时硬编码 | 不可配置，大项目可能需要更长时间 |

---

## 四、路线图：先易后难

> 规则：每个阶段聚焦 **3-5 项**，优先选择 **改动小、收益大** 的项目。

---

### 📦 第一阶段：Quick Wins（预计 2-3 天）

这些功能利用现有基础设施，每个只需改 1-2 个文件。

#### 1.1 Git 自动提交（⭐⭐⭐ 最高收益）

**改动文件**：`Agent.cs`（~10 行）、`GitTool.cs`（已有基础设施）

**方案**：在 `AppendLintFeedbackAsync` 同级位置，新增 `AutoGitCommit` 方法：

```csharp
// write_file / edit_file 成功后自动 git add + commit
private async Task<string> AutoGitCommit(ToolCall tc, string toolResult)
{
    if (tc.Name is not "write_file" and not "edit_file")
        return toolResult;

    var filePath = tc.Arguments.GetValueOrDefault("file_path")?.ToString();
    if (string.IsNullOrWhiteSpace(filePath)) return toolResult;

    try
    {
        // 生成 AI commit message（用当前会话上下文）
        var commitMsg = await GenerateCommitMessage(filePath);

        var git = new GitTool();
        await git.ExecuteAsync(new() { ["command"] = $"add \"{filePath}\"" });
        await git.ExecuteAsync(new() { ["command"] = $"commit -m \"{commitMsg}\"" });

        return toolResult + $"\n\n📦 已自动提交: {commitMsg}";
    }
    catch { return toolResult; }
}
```

**Commit message 生成**：复用小模型发一条 `Generate a conventional commit message for changes to {filePath}` 提示词。

**配置**：通过环境变量 `CORECODER_AUTO_COMMIT=true/false` 控制，默认关闭。

**预计工作量**：2-3 小时

---

#### 1.2 AGENTS.md 支持（⭐⭐ 高收益，极低改动）

**改动文件**：`ProjectContext.cs`（~3 行）

**方案**：在 `LoadInstructions` 中添加 `AGENTS.md` 作为备选：

```csharp
var instructionFiles = new[] { "CLAUDE.md", "AGENTS.md", ".cursorrules" };
```

**优先级**：AGENTS.md 已被 60,000+ 开源项目采用，不支持的损失是 LLM 看不到项目指令。

**预计工作量**：15 分钟

---

#### 1.3 会话自动保存与恢复（⭐⭐ 高收益）

**改动文件**：`Program.cs`（~15 行）、`SessionManager.cs`（~10 行）

**方案 A — 退出自动保存**：
在 REPL 退出前（Ctrl+C 或 `/quit`），自动调用 `SessionManager.SaveAsync`，存为 `session_auto_latest.json`。

**方案 B — 启动自动恢复**：
启动时检测 `session_auto_latest.json` 是否存在，存在则提示恢复。

**预计工作量**：1-2 小时

---

#### 1.4 设置持久化（⭐ 中收益）

**改动文件**：`SettingsPage.cs`（~20 行）、`Program.cs`（`/settings` 命令处理）

**方案**：在设置界面按 Ctrl+S 或退出时，将当前 Config 写回 `.env` 文件：

```csharp
static void SaveConfigToEnv(Config config)
{
    var envPath = Path.Combine(Environment.CurrentDirectory, ".env");
    var lines = new List<string>();
    lines.Add($"CORECODER_MODEL={config.Model}");
    lines.Add($"CORECODER_SMALL_MODEL={config.SmallModel}");
    // ... etc
    File.WriteAllText(envPath, string.Join("\n", lines));
}
```

**预计工作量**：1-2 小时

---

#### 1.5 改进 Token 估算（⭐ 中收益）

**改动文件**：`ContextManager.cs`（~30 行）

**方案**：从 `len/3` 改进为 CJK 感知估算：
- CJK 字符 ≈ 1.5 tokens/char
- ASCII 字符 ≈ 0.25 tokens/char（英文单词 ≈ 1.3 tokens/word）
- 混合文本：分别计数后加权

```csharp
static int EstimateTokens(string text)
{
    int cjk = 0, ascii = 0;
    foreach (var c in text)
    {
        if (c >= 0x4E00 && c <= 0x9FFF || c >= 0x3400 && c <= 0x4DBF) cjk++;
        else if (c > 127) cjk++;  // 其他非 ASCII
        else ascii++;
    }
    return (int)(cjk * 1.5 + ascii * 0.25);
}
```

备选：引入 `Microsoft.ML.Tokenizers` NuGet 包（支持 AOT），用 cl100k_base 编码器精确计数。但增加依赖，权衡后 CJK 感知估算已足够。

**预计工作量**：1 小时

---

### 📦 第二阶段：结构加固（预计 1-2 周）

这些需要跨文件改动，但风险可控。

#### 2.1 自动 Test 修复循环（⭐⭐⭐）

**改动文件**：`Agent.cs`（~50 行）、新增 `TestRunner.cs`（~80 行）

**方案**：在 lint 反馈后，自动检测并运行测试：

```
write_file → lint → 有错 → LLM 修复 → 重lint → 通过
                                          → 不通过 → 重试(最多3次)
                 → 通过 → 检测测试命令 → 运行测试
                                          → 失败 → LLM 修复 → 重测
```

**测试命令检测**：复用 `ProjectContext` 的 `BuildTool`：
- dotnet → `dotnet test`
- npm/yarn → `npm test`
- go → `go test ./...`
- cargo → `cargo test`
- pytest → `pytest`

```csharp
static string? DetectTestCommand(ProjectInfo project)
{
    return project.BuildTool switch
    {
        "dotnet" => "dotnet test --nologo -v q",
        "npm" or "yarn" or "pnpm" => $"{project.BuildTool} test",
        "go" => "go test ./...",
        "cargo" => "cargo test",
        "pip" or "poetry" or "hatch" => "python -m pytest",
        _ => null
    };
}
```

**配置**：`CORECODER_AUTO_TEST=true/false`，默认关闭（因为测试慢）。

**预计工作量**：4-6 小时

---

#### 2.2 Diff-based Code Review（⭐⭐）

**改动文件**：`ReviewMode.cs`（~40 行）、`Agent.cs`（~10 行）

**方案**：Review 时用 `git diff` 而非全文件内容：

```csharp
// 获取本次会话所有改动
var diff = RunGitCommand("git diff HEAD");
// 也包含新增的未跟踪文件
diff += RunGitCommand("git diff --cached");
```

**优势**：
- Review 聚焦实际改动，减少噪音
- 不需要 3000 字符截断
- 可以 review bash 命令修改的文件

**预计工作量**：2-3 小时

---

#### 2.3 会话管理增强（⭐⭐）

**改动文件**：`SessionManager.cs`（~50 行）、`Program.cs`（`/sessions` 命令）

**方案**：
- 会话删除：`/sessions delete <id>`
- 会话搜索：`/sessions search <keyword>`
- 会话列表显示 token 数、消息轮次
- 自动清理超过 30 天的会话

**预计工作量**：3-4 小时

---

#### 2.4 Checkpoint 持久化（⭐）

**改动文件**：`CheckpointManager.cs`（~30 行）

**方案**：启动时从 `~/.corecoder/checkpoints/` 读取已有检查点，重建内存列表。

```csharp
public static void LoadExisting()
{
    var dir = Path.Combine(home, ".corecoder", "checkpoints");
    if (!Directory.Exists(dir)) return;
    foreach (var ckptDir in Directory.GetDirectories(dir))
    {
        var metaPath = Path.Combine(ckptDir, "_checkpoint.json");
        if (File.Exists(metaPath))
        {
            // 解析并加入 _checkpoints 列表
            var meta = JsonNode.Parse(File.ReadAllText(metaPath));
            _checkpoints.Add(new Checkpoint { ... });
        }
    }
}
```

**预计工作量**：1-2 小时

---

#### 2.5 Aider 式 Watch 模式（⭐⭐）

**改动文件**：新增 `WatchMode.cs`（~100 行）、`Program.cs`（~10 行）

**方案**：`--watch` 参数启动文件监听。检测到外部编辑器保存文件时，自动将改动发送给 Agent：

```csharp
// 用 FileSystemWatcher 监听项目目录
var watcher = new FileSystemWatcher(projectRoot)
{
    IncludeSubdirectories = true,
    NotifyFilter = NotifyFilters.LastWrite,
};
watcher.Changed += async (_, e) =>
{
    // 检测 AI! / AI? 注释（兼容 Aider）
    var content = File.ReadAllText(e.FullPath);
    if (content.Contains("AI!") || content.Contains("AI?"))
    {
        var prompt = ExtractAiComment(content);
        await agent.ChatAsync(prompt);
    }
};
```

**预计工作量**：3-5 小时

---

### 📦 第三阶段：深度差异化（预计 2-4 周）

这些是较大的架构性改动，建立长期护城河。

#### 3.1 Prompt 缓存（⭐⭐⭐）

**方案**：利用 OpenAI/DeepSeek 兼容 API 的 prompt caching。

**关键洞察**：系统提示词（SystemPrompt）和工具定义（ToolSchemas）每次请求完全相同，是缓存的理想候选。

```csharp
// 计算系统提示词 + 工具定义的 SHA256
var cacheKey = ComputeCachePrefix(systemPrompt, toolSchemas);

// 在请求中加入缓存标记（如果提供商支持）
// 例如 Anthropic: cache_control: { type: "ephemeral" }
// DeepSeek: 文档待确认是否支持
```

由于 CoreCoder 使用 OpenAI 兼容 API，需要确认 DeepSeek 是否支持 prompt caching。若不支持，可以考虑：
- 自动断点续传：缓存最近 N 轮的消息，只发送新增部分
- 本地缓存命中检测：如果系统提示词未变，标记 "same as previous"

**预计工作量**：1-2 周（取决于提供商 API 支持程度）

---

#### 3.2 内置 TUI 编辑器升级（⭐⭐⭐）

**当前**：基础编辑器 + 语法高亮

**目标**：终端内微型 IDE

| 功能 | 说明 | 难度 |
|------|------|:---:|
| LSP 诊断集成 | 红色波浪线标注错误 | 中 |
| 文件树侧栏 | 常驻左侧，回车打开 | 低 |
| Git Blame 行内 | `git blame` 显示每行作者 | 中 |
| 多文件 Tab | Ctrl+Tab 切换 | 中 |
| 代码补全弹窗 | 小模型驱动的补全 | 高 |

**预计工作量**：2-3 周（分步做）

---

#### 3.3 语义记忆（⭐⭐）

**方案**：用简单向量相似度替代关键词搜索。

**技术选型**：.NET 生态中的轻量嵌入方案
- `Microsoft.ML.OnnxRuntime` + 小型嵌入模型（如 all-MiniLM-L6-v2）
- 或直接调用小模型 API 做嵌入（deepseek-v4-flash 可能支持）

```csharp
// MemoryStore 升级
class SemanticMemory
{
    List<(string Text, float[] Embedding)> _entries;

    async Task AddAsync(string text)
    {
        var emb = await GetEmbeddingAsync(text);
        _entries.Add((text, emb));
    }

    async Task<List<string>> SearchAsync(string query, int topK = 5)
    {
        var qEmb = await GetEmbeddingAsync(query);
        return _entries
            .OrderByDescending(e => CosineSimilarity(qEmb, e.Embedding))
            .Take(topK)
            .Select(e => e.Text)
            .ToList();
    }
}
```

**预计工作量**：3-5 天

---

#### 3.4 沙箱执行（⭐⭐）

**方案**：借鉴 Codex CLI 的三级沙箱设计，在 CoreCoder 已有的四层安全防护基础上增加进程隔离。

```yaml
沙箱级别:
  suggest: 所有 bash 需确认（当前 ask 模式）
  auto-edit: 文件编辑自动，bash 需确认
  full-auto: 全部自动，但 bash 运行在网络禁用 + 目录隔离的沙箱中
```

**技术方案**：Windows 用 `runas /trustlevel` 或 Job Objects，Linux 用 `bwrap`/`firejail`。

**预计工作量**：1-2 周

---

## 五、实施优先级矩阵

```
                    高收益
                      │
      1.1 Git自动提交  │  2.1 自动Test循环
      1.2 AGENTS.md    │  3.1 Prompt缓存
      1.3 会话自动保存 │  3.2 TUI编辑器升级
      1.4 设置持久化   │
                      │
  ─────────────────────┼──────────────────────
    低难度             │             高难度
                      │
      1.5 Token估算    │  2.5 Watch模式
      2.3 会话管理增强 │  3.3 语义记忆
      2.4 Checkpoint持久化│
      2.2 Diff Review  │  3.4 沙箱执行
                      │
                    低收益
```

---

## 六、建议执行顺序

```
Week 1 ─ 第一阶段 Quick Wins
  ├── Day 1: 1.2 AGENTS.md (15min) + 1.3 会话自动保存 (2h) + 1.5 Token估算 (1h)
  ├── Day 2: 1.1 Git 自动提交 (3h) + 1.4 设置持久化 (2h)
  └── Day 3: 2.4 Checkpoint 持久化 (2h) + 2.3 会话管理增强 (4h)

Week 2 ─ 第二阶段 结构加固
  ├── Day 1-2: 2.2 Diff-based Review (3h) + 2.5 Watch 模式 (5h)
  └── Day 3-5: 2.1 自动 Test 修复循环 (6h)

Week 3-6 ─ 第三阶段 深度差异化
  ├── 3.1 Prompt 缓存 (1-2周)
  ├── 3.2 TUI 编辑器升级 (2-3周)
  └── 3.3 语义记忆 (穿插)
```

---

## 七、每月可衡量的里程碑

| 月份 | 交付物 | 考核指标 |
|------|--------|------|
| **M1** | Quick Wins 全部完成 | Git 自动提交可用、AGENTS.md 识别、会话不丢失、Token 估算误差 <15% |
| **M2** | 自动 Test 循环 + Watch 模式 | `--watch` 可用、改代码自动跑测试、Review 基于 diff |
| **M3** | Prompt 缓存 + TUI 编辑器 V2 | 系统提示词缓存命中率 >90%、编辑器支持 LSP 诊断 |

---

## 八、核心差异化最终形态

```
         AOT 单文件部署 (0依赖, 双击即用)
         ┌──────────────────────────────┐
         │                              │
    中文原生 ── CoreCoder v2.0 ── 终端IDE
         │      差异化三角               │
         │                              │
         └──────────────────────────────┘
           Git 原生 (自动提交 + 自愈循环)

竞品做不到的:
  • AOT 单文件 → 不需要 Node/Python/Rust
  • 终端 IDE → 不需要外部编辑器
  • 中文优先 → 唯一

竞品做不好的:
  • 双模型省钱 → 用户无需手动切换
  • 四层安全 → 比沙箱更细粒度

补齐后不输竞品的:
  • Git 自动提交 (追平 Aider)
  • Test 修复循环 (追平 Claude Code)
  • Prompt 缓存 (追平 Codex CLI)
```
