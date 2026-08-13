# 更新日志

## v0.45.0 (2026-08-13) — FileTracker 持久化：跨会话 stale-read 保护

### 💾 文件追踪持久化（P0-2，对标 Crush last_read_time）

- **问题**：`FileTracker` 哈希与读取时间仅存内存，程序重启后全部丢失，跨会话的 stale-read 检测与「先读后改」保护失效
- **修复**：追踪状态持久化到 `.waycoder/file-tracker.json`（纯 JSON，**零依赖、无数据库**），重启后自动恢复
  - `RecordRead` / `RecordWrite` / `CheckForChanges` / `Reset` 在状态变更后自动 `Save()`
  - `EnsureLoaded()` 惰性加载——首次使用时从磁盘读回，仅一次
  - **原子写**：先写 `.tmp` 再 `File.Move(overwrite:true)`，防止中断损坏缓存
  - 上限保护：磁盘数据超出 `MaxTracked=200` 时丢弃多余条目；损坏/不可读时静默退化为内存模式
- **体积零增长**：复用 `JsonNode` 手写序列化（与 `todos.json` 同模式），不引入 SQLite / 任何第三方库，AOT 兼容

### 🧪 自测 +5

- 新增 FileTracker 持久化往返：记录生成 JSON / 含路径与 hash 字段 / 模拟重启后仍追踪 / 检测到外部修改

## v0.44.0 (2026-08-13) — bash 前台超时自动迁移后台

### ⏱ 后台命令自动迁移（对标 Crush）

- **问题**：前台 `bash` 命令超时后直接 `Kill` 进程并返回「错误：超时」，长任务（build/test）已执行的工作白费，Agent 需重新跑
- **修复**：超时后不再杀死进程，自动转入后台继续执行并返回 `shell_id`，Agent 可用 `job_output` 轮询、`job_kill` 终止
  - **非流式路径**（`-p` 一次性模式 / benchmark）：`BackgroundTaskManager.Adopt` 接纳已运行进程 + `ReadToEndAsync` 任务
  - **流式路径**（交互 REPL 实时输出）：`BackgroundTaskManager.AdoptStreaming` 接纳逐行读取任务 + 共享输出缓冲
- **重构**：`RunTaskAsync` 提取 `WaitAndCollectAsync`（等待退出 + 收集 IO + 写状态），`Start` / `Adopt` / `AdoptStreaming` 三条路径共用，消除重复
- **沙箱模式例外**：仍直接终止，避免迁移绕过内存/CPU 资源上限
- **进程所有权**：迁移后由后台管理器负责 dispose，前台不再释放句柄（`migrated` 标志 + `finally`）

### 🧪 自测 +1

- 新增 `bash 超时自动迁移到后台`（慢命令 + 短超时 → 断言返回 `Shell ID`）

## v0.43.0 (2026-08-13) — 省 Token 模式：三态开关综合降 token

### 💰 省 Token 模式（`--economy [on|auto|off]` / `WAYCODER_ECONOMY`）

- **新增** `EconomyMode` 三态开关（默认 `off`），保持正常窗口不变：
  - **关（off）**：完整提示词 + 正常压缩阈值
  - **开（on）**：从四个方面综合降 token——
    - 系统提示词精简（`SystemPrompt.GenerateEconomy` 砍 RepoMap/Git/记忆/10 阶段流水线，保留完整工具描述 + 项目上下文 + 9 条核心规则）
    - 压缩更激进（snip/summarize/collapse 50/70/90 → 35/55/75）
    - 工具输出更早裁剪（4000 → 2000 字符）
    - 输出上限收紧（`max_tokens` 32768 → 8192）
  - **自动（auto）**：保持完整提示词，压缩阈值/裁剪阈值按**上下文占用率**动态插值——占用率 ≤30% 用正常阈值，≥90% 用全量省 token 阈值，中间线性过渡（越满越省）
- **与 Tiny 的区别**：Tiny = 极简提示词 + 4K 小窗口（面向本地小模型）；Economy = 保持正常窗口，仅综合省 token（面向云端大模型省钱）
- **`reasoning_effort` 不做最小化**：对不支持推理参数的非 DeepSeek/OpenAI 模型会 400，风险大于收益；reasoning 省 token 已由「reasoning_content 不存历史」覆盖

### 🧪 自测 +22（1591 → 1613）

- 新增 `TestEconomyMode`（三态默认值 / ResolveRatio 插值 / 提示词精简 / snip 阈值对照 / 常量）

## v0.42.0 (2026-08-13) — Tiny 模式增强：可指定窗口 + 自动探测 + 小模型自动进入

### 🐭 Tiny 模式窗口可指定 / 自动探测

- **`--tiny 8k` 指定窗口**：`TinyArg.ValueCount=-1` 支持可选值，`ModelCatalog.ParseWindowSpec` 解析 `8k`/`8192`/`4K` 等规格
- **`--tiny` 自动探测**：`ProbeModelWindow` 优先调 Ollama `/api/show` 读真实 `context_length`（解决目录对本地模型标称 128K 虚高问题），其次内置目录 `ContextWindow`，最后回退 4K
- **`<128K` 自动进入 tiny**：模型窗口低于 `TinyAutoThreshold=128_000` 时自动启用 tiny（精简提示词 + 对应窗口），本地小模型开箱即用，无需手动 `--tiny`

### 🔧 实现

- `Config.TinyWindow`（可运行时覆盖的窗口，默认 4K）+ `Config.TinyAutoThreshold=128_000`
- `ModelCatalog.ResolveTinyWindow` / `ProbeModelWindow` / `ParseWindowSpec` / `IsOllamaBaseUrl` / `QueryOllamaContextLength`（2s 超时，失败静默回退）
- `ResolveContextWindow` 在 Tiny 模式读 `Config.Instance.TinyWindow`（不再写死 4K）
- `Program` 在 base URL 解析后统一判定：显式 `--tiny` 或窗口 `<128K` 自动进入

### 🧪 自测 +17（1574 → 1591）

- 新增 `TestTinyWindow`（窗口规格解析 / 显式指定 / 自动探测目录与兜底 / ProbeModelWindow / 128K 阈值 / Ollama base url 识别）

## v0.41.0 (2026-08-13) — Tiny 模式：4K 上下文窗口也能写程序

### 🐭 Tiny 模式（`--tiny` / `WAYCODER_TINY=1`）

- **新增** `--tiny` CLI 参数 + `WAYCODER_TINY` 配置：4K 上下文窗口 + 极简系统提示词，省 token / 压力测试
- **窗口固定 4K**：`Config.TinyContextWindow=4096`，`ModelCatalog.ResolveContextWindow` 在 Tiny 模式下忽略模型窗口返回 4K
- **极简提示词** `SystemPrompt.GenerateTiny`：砍掉 RepoMap/记忆/技能/10 阶段流水线/冗长规则区块，只留身份+环境+工具+8 条核心规则，从 1 万+ token 压到 <3K 字符
- **依赖压缩 + 自动续跑**：4K 窗口下压缩更频繁、自动续跑接管，持续写程序不中断

### 🧪 自测 +8

- 新增 `TestTinyMode`（窗口常量 / 固定 4K / 提示词精简 / 核心规则保留）

## v0.40.0 (2026-08-13) — 自动续跑 + 压缩保真度 + 上下文窗口按模型切换

### 🔁 自动续跑（撞 MaxRounds 上限不再退出）

- **问题**：大任务撞 `MaxRounds=50` 就 `return` 提示「输入继续」，一次性模式（`-p`）无交互通道，进程直接退出，只能手动开新实例接力
- **修复**：撞上限且仍在写文件时，自动压缩 + 注入「继续 + 已完成文件清单」提示后重跑；`WAYCODER_MAX_REQUEUE` 控制次数（默认 3，0=关闭，上限 20）
- **提取** `InjectContinuePrompt`：压缩后自动继续与撞上限续跑共用同一注入模板

### 🧠 压缩保真度增强：无 LLM 回退摘要保留需求清单

- **问题**：无 LLM 的离线回退摘要 `ExtractKeyInfo` 只提取文件路径/命名空间/错误码，压缩后 Agent 会「忘记」还剩哪些需求没做
- **修复**：新增正则提取「需求 N：/Requirement N：/`- [ ]` 未完成勾选/TODO/待办」条目，摘要输出「待完成需求」段（去重取前 10）

### 📏 上下文窗口按模型切换（不再写死）

- **问题**：`MaxContextTokens` 全局写死 1M，切换模型不更新窗口 —— 切到 64K/128K 小窗口模型时压缩触发过晚，模型先报 `context length exceeded`
- **修复**：`ModelCatalog.ResolveContextWindow` 按模型目录 `ContextWindow` 解析窗口；`ContextManager.UpdateMaxTokens` 运行时重算三层压缩阈值；切模型入口（`/model`、ModelPicker、槽位切换、启动初始化）统一同步
- **修正**：Schema 默认串 `128000` → `1048576`（与代码默认一致）

### 🧪 自测 +22

- 新增 `TestCompressionFidelity`（30 需求压缩后保留路径/命名空间/错误码/需求清单）+ `TestContextWindowSwitch`（按模型解析 + 阈值重算 + 边界）+ `MaxAutoRequeue` 默认值

## v0.39.0 (2026-08-13) — 界面对标：`/` 补全接注册表 + 上下文用量 Gauge

### ✨ `/` 斜杠命令补全接 SlashCommandRegistry

- **问题**：`/` 补全用硬编码 14 条命令数组，新增斜杠命令不会自动出现在补全里
- **修复**：`ChatScreen.BuildPrefixHints` 的 `case '/'` 改为遍历 `SlashCommandRegistry.Commands`，`Name`/`Aliases` 参与匹配，`Usage` 作标签，`Value` 用主命令名

### ✨ 上下文用量彩色 Gauge（动态栏右段常驻）

- **新增**：`TuiDynamicBar.ContextPercent` 属性，右段常驻 `📊 {pct}%`，颜色绿(≤30%)→黄(≤70%)→红(>70%)
- **数据桥**：`ChatScreen.UpdateTokenDisplayFull` 计算 `_contextPercent`，`SyncDynamicBar()` 每帧同步；空闲态模型名与上下文% 并存

### 🧪 自测 + 修复 flaky 测试

- 新增 4 项 `SlashCommandRegistry` 测试（非空 / 含 `/help` / 含 `/model` / 数量 ≥14）
- 修复「日志包含堆栈信息」flaky 测试：`new` 出来的异常 `StackTrace` 为 null，改为 throw/catch 生成真实堆栈，不再依赖历史崩溃日志残留

## v0.38.0 (2026-08-12) — Agent 工具 tasks 数组解析 Bug 修复

### 🐛 修复：agent 工具 tasks 数组退化成字符串（16639 任务 bug）

- **问题**：调用 `agent` 工具传 `tasks` 数组时，3 个任务被拆成 16639 个「单字符任务」
- **根因**：`LLM.ParseArgs` 把 `JsonArray`/`JsonObject` 用 `ToJsonString()` 序列化成字符串，`AgentTool.ExecuteParallelAsync` 再把字符串当 `IEnumerable<char>` 逐字符遍历
- **修复**：新增 `JsonNodeToObject` 递归转换（数组→`List<object?>`、对象→`Dictionary<string, object?>`、标量→原生类型），`ParseArgs`/`TryParseCompleteJson` 两处解析入口统一走该转换

### 🐛 修复：JSON 数字类型保真（自测崩溃中断）

- **问题**：JSON 整数被解析成 `JsonElement` 或 `double`，`(long)x` 强转抛 `InvalidCastException`，导致自测进程 `AppDomain.UnhandledException` 崩溃中断
- **根因**：`JsonValue.GetValue<object>()` 返回 `JsonElement`；`TryGetValue<long>` 对整数 JSON 值也走 double 路径
- **修复**：新增 `JsonValueToNative` + `ParseJsonNumber`，基于原始文本 `long.TryParse`/`double.TryParse` 精确区分整数/小数，>2^53 的大整数也不丢精度

### 🔧 加固：AgentTool 字符串防御

- `IEnumerable` 分支加 `is not string` 守卫，杜绝字符串被逐字符遍历
- 新增 `string` 防御分支，兼容 LLM 直接返回单个字符串或旧版序列化 JSON 字符串

### 🧪 新自测（7 项）+ 修 3 项自测自身 bug

- ParseArgs 数组解析为 List、嵌套对象解析为 Dictionary、小数/负整数/大整数类型保真、混合数组保序保类型、嵌套数组递归解析
- AgentTool Schema 含 tasks 并行数组
- 修复：超时默认值测试改用 `new Config()` 测代码默认值（不再受 `.env` 600 覆盖影响）；仓库地图根路径测试支持 macOS 绝对路径

### 📊 评分

- v0.37.0: 80/100
- v0.38.0: **82/100** ✅（Agent 工具并行 bug 修复 + 数字类型保真 +2 分）

## v0.37.1 (2026-08-12) — 对话框首次显示 Bug 修复

### 🐛 修复：首次打开对话框尺寸/位置不计算 + 按钮不显示

- **问题**：启动后第一次打开对话框，窗口以默认 30×10 尺寸渲染在屏幕左上角 (0,0)，按钮全部堆叠不可见
- **根因**：`TuiScreen.AddWindow()` 只调用了 `win.OnCreate()`（生命周期），从未调用 `win.OnResize(TW, TH)`
  - `OnResize` 是唯一执行 XScale→Width、YScale→Height、WindowHAlign/WindowVAlign→居中定位、RootView.Layout()→Flex 按钮布局的地方
  - 终端 resize 事件会触发 `OnResize`，但首次创建时不会
- **修复**：`AddWindow()` 中 `win.OnCreate()` 之后添加一行 `win.OnResize(TW, TH)`
- **影响**：所有对话框（设置、模型选择、确认框、输入框等）首次打开即正确居中、比例缩放、按钮可见

## v0.37.0 (2026-08-12) — 文件先读后改保护 + Git 状态注入 + Agent 工具分层

### 🔧 改进 1：文件先读后改保护（对标 Crush last_read_time）

- **FileTracker.ValidatePreEdit**：文件写入/编辑前检查是否已先读取，防止 LLM 凭猜测编辑
- 未读取过 → 返回警告：必须先用 `read_file` 读取
- 读取后被外部修改 → 返回警告：文件已变更，需重新读取
- `FileTracker.RecordRead` 同步记录 `LastReadTimes`（时间戳字典）
- 集成工具：`WriteFileTool`（覆写已有文件时）、`EditFileTool`（编辑前）、`MultiEditTool`（编辑已有文件时）
- 新文件/不存在的文件不检查（无需先读）

### 🔧 改进 2：Git 状态注入系统提示词（对标 Crush git status）

- 系统提示词新增 `__GIT_STATUS__` 区块，每次启动自动注入当前 Git 状态
- 包含：当前分支名、工作区变更（git status --short，最多 15 项）、最近 3 次提交
- 让 LLM 感知当前 Git 上下文，减少误操作（如在不干净的工作区做提交）
- 非 Git 仓库时自动跳过（返回空字符串）

### 🔧 改进 3：Agent 工具集分层

- **子智能体工具白名单**：`ToolRegistry.SubAgentDeniedTools` — 禁止子智能体使用 bash/rm/kill/git 等危险工具
- **集中化过滤**：`ToolRegistry.GetSubAgentTools(parentTools, depth, maxDepth)` 统一管理子智能体工具集
- 子智能体仅保留安全工具：read_file、write_file、edit_file、grep、glob、ls 等读写/搜索工具 + agent（深度限制）
- 危险的 shell 命令/进程管理/Git 操作仅主智能体可用

### 🧪 新自测（20 项）

- FileTracker 先读后改：未读警告、已读通过、外部修改警告、新文件通过、Reset 清空
- Agent 工具分层：SubAgentDeniedTools 包含危险工具、子 Agent 不同深度工具集验证
- Git 状态注入：非 null、包含仓库信息

### 📊 评分

- v0.36.0: 72/100
- v0.37.0: **80/100** ✅（文件先读后改 +8 分）
- 达到可发布标准（80 分）

## v0.36.0 (2026-08-12) — 自编程稳定性修复 + 竞品对比驱动改进

### 🧪 macOS Web Desktop 自编程测试

- **WayCoder 成功自编程**：从零写出 1,251 行 / 52KB 的 macOS 风格 Web 桌面（`demo/index.html`）
- 包含菜单栏、Dock 栏、窗口系统（拖拽/缩放/动画）、6 个可交互应用（Finder/终端/计算器等）
- 过程中暴露 4 个关键缺陷，本轮全部修复

### 🐛 修复 1：ParseArgs 错误参数泄漏（12 分）

- **问题**：`ParseArgs()` JSON 解析失败时返回 `_parse_error`/`_parse_error_type`/`_raw_json_snippet` 伪参数，被当作真实工具参数传递
- **表现**：LLM 调用 `write_file(_parse_error=True, _parse_error_type=JsonReaderException, ...)`，文件路径丢失、工具调用幻觉
- **根因**：`LLM.cs` 第 729 行在解析异常时将错误标记写入参数字典，上层未清理直接传入工具
- **修复**：`ParsedToolCalls` 逻辑中检测并清除 `_parse_error*` 键，仅保留合法参数，将截断信息记录到调试日志

### 🐛 修复 2：推理（Reasoning）独占检测缺失（10 分）

- **问题**：DeepSeek V4 等模型将大量输出花在 `reasoning_content` 上（显示但不计入 `Content`），`_analysisOnlyStreak` 的 `contentLen > 100` 条件不触发
- **表现**：模型输出数千字推理内容、零工具调用，Agent 静默等待直到超时
- **根因**：推理内容被 `LLM.cs` 从 `contentParts` 剥离（设计决策：推理不存入对话历史），导致 Agent 层看不到
- **修复**：
  - `LLMResponse` 新增 `ReasoningTokens` 字段，传递推理内容长度
  - `Agent.cs` 新增检测：`ReasoningTokens > 300 && ToolCalls.Count == 0 && Content.Length < 80` → 渐进式催促
  - 与 `_analysisOnlyStreak` 共享计数器，三级递进 nudge

### 🔧 改进 3：文档细化（对标 Crush 竞品分析）

- 对 Crush（Go 版 Claude Code）v2.1.88 做了系统性 10 维竞品对比分析
- 识别出 WayCoder 的差异化优势（3 层上下文压缩、渐进式反循环检测、Schema 驱动配置）和竞品值得学习的模式（多供应商 SDK、后台命令自动迁移、基于事件总线的权限架构、多作用域配置）
- 详细对比记录在 `docs/waycoder-vs-crush-comparison.md`

### 🧪 新自测

- `ParseArgs` 错误标记清除：验证截断 JSON 不泄漏 `_parse_error` 到工具参数
- `LLMResponse.ReasoningTokens` 字段完整性：验证新字段存在且正确传递
- `_talksCodeStreak` + 推理独占检测端到端：模拟 3 轮推理独占 → 验证渐进式 nudge

### 📊 评分

- 自编程测试前 WayCoder 编程能力评分：**62/100**（初级工程师水平）
- 本轮修复后目标：**72/100**（接近中级工程师，改善了口述代码、错误参数泄漏、推理独占三大核心问题）
- 距离 80 分可发布目标的差距：**8 分**（需要进一步改进：文件读写时间戳保护、Agent 工具集分层、系统提示词动态化）

---

## v0.35.0 (2026-08-12) — Flex 弹性布局 + 窗口比例缩放 + 位置对齐 + 对话框标准化

### ✨ Flex 弹性布局系统

- **`TuiBase.Flex`**：所有 UI 元素新增 `Flex` 属性（默认 0=固定尺寸，>0=按比例分配父容器剩余空间）
- **`TuiHBox.Layout()`**：水平容器支持 Flex — Flex=0 固定宽度，Flex>0 按权重比例分配剩余空间
- **`TuiVBox.Layout()`**：垂直容器支持 Flex — 同算法垂直方向
- **后向兼容**：所有现有控件 Flex=0，行为完全不变

### ✨ 窗口比例缩放（XScale / YScale）

- **`TuiWindow.XScale`**：窗口宽度 = 终端宽度 × 比例（0=禁用，如 0.5=半屏宽）
- **`TuiWindow.YScale`**：窗口高度 = 终端高度 × 比例（0=禁用，如 0.4=40%屏高）
- **约束支持**：`MinWidth`/`MinHeight`/`MaxWidth`/`MaxHeight` 自动钳制缩放结果
- **手动拖拽清零**：鼠标拖拽缩放窗口后自动清零 XScale/YScale，切换到固定尺寸

### ✨ 窗口位置对齐（WindowHAlign / WindowVAlign）

- **`TuiWindow.WindowHAlign`**：`Left`/`Center`/`Right`/`Stretch`(不定位)，resize 时自动重算 X
- **`TuiWindow.WindowVAlign`**：`Top`/`Middle`/`Bottom`/`Stretch`(不定位)，resize 时自动重算 Y
- **`TuiWindow.ScreenMargin`**：窗口与屏幕边缘偏移（Toast 右下角 + 偏移等场景）
- **移除 `AutoCenter`**：`WindowHAlign=Center + WindowVAlign=Middle` 等效替代

### ✨ 对话框全面标准化

- **全部 11 种对话框重构**（Info/Success/Warn/Error/Confirm/Confirm3/Input/Secret/Select/MultiSelect/Permission）
- 使用 `XScale` 替代手动 `CalcMaxMsgWidth()` + clamp 宽度计算
- 使用 `WindowHAlign`/`WindowVAlign` 替代 `win.Center()`
- 按钮使用 `Flex=1` 均分替代 `NormalizeButtons()` 手动统一宽度
- **移除 `OnResizeContent` 完全重建**：框架自动处理 resize → 窗口缩放 → Flex 重分配 → 重绘
- 代码量 914 行 → 338 行（-63%）

### 🧪 测试

- Flex 布局测试 16 项（含 HBox/VBox Flex 分配、Margin/Spacing 配合、后向兼容）
- 窗口比例缩放测试 10 项（XScale/YScale、Min/Max 约束、两维同时、手动保持）
- 窗口位置对齐测试 12 项（9 种对齐组合 + AutoCenter 兼容 + ScreenMargin Offset）
- 端到端集成测试 9 项（终端 resize → Screen → Window → Flex 全链路）
- 总计 **1499 通过 / 0 失败**

---

## v0.34.2 (2026-08-12) — 对话框 Resize 刷新 + 4 项 P0 修复

### 🐛 对话框 Resize 不刷新（修复）

- **根因**：`TuiWindow.OnResize()` 不调用 `RootView.MarkDirty()`，增量渲染可能跳过窗口重绘；窗口保持创建时尺寸，缩小终端时溢出、扩大终端时不利用额外空间
- **修复 1**：`TuiWindow.OnResize()` 新增 `RootView.MarkDirty()` 强制下一帧重绘窗口
- **修复 2**：`TuiWindow.OnResize()` 新增窗口位置 clamp（防止缩小终端时溢出）
- **修复 3**：新增 `TuiWindow.OnResizeContent` 回调，`TuiDialog` 各工厂方法设置此回调以在 resize 时重建控件
- **修复 4**：全部 `TuiDialog` 方法（Info/Success/Warn/Error/Confirm/Confirm3/Input/Secret/Select/MultiSelect/Permission）均已改造，状态型对话框（Input/Secret/Select/MultiSelect）保留用户输入/选择状态
- **设计**：每次 resize 重建控件树（`CalcMaxMsgWidth()` 重新按 `Tty.Cols` 计算宽度）→ 窗口尺寸自适应新的终端尺寸

### 🐛 P0-1：孤立工具调用/结果修复（Agent.cs）

- **根因**：Agent 中断（Ctrl+C）、会话恢复或 LLM 输出截断导致 assistant tool-call 无对应 tool-result，下轮 API 拒绝请求
- **修复**：实现 `RepairOrphanedToolPairs()`（对标 Crush `filterOrphanedToolResults` + `syntheticToolResultsForOrphanedCalls`）
  - 收集所有 assistant 消息的 tool_call ID → `callIds`
  - 收集所有 tool 消息的 tool_call_id → `resultIds`
  - 无结果的 tool-call → 注入合成错误 tool-result：`[工具执行被中断] 工具 "{name}" 的调用未能完成执行...`
  - 无对应 tool-call 的 tool-result → 从 Messages 中删除
- **效果**：中断后恢复的会话不再因孤例配对而 API 报错

### 🐛 P0-2：循环检测改 per-tool 级（Agent.cs）

- **根因**：旧方案对整轮做哈希，同轮中其他工具不同会掩盖某个工具的重复调用
- **修复**：重写 `DetectAndBreakLoop()` 为 per-tool-call 级
  - 每个工具单独哈希：`tool_name + args_json + output[..2000]`
  - 滑动窗口 10，阈值 5 → 循环警告；批量检测提示"共 N 个模式重复"
- **效果**：更精准检测 write→lint-error→rewrite 等单工具重复模式

### 🐛 P0-3：编辑前 mtime 检查（Agent.cs）

- **根因**：Agent 读文件后、编辑前，文件可能被 bash 外部修改，导致基于过期内容编辑
- **修复**：`ExecuteToolAsync()` 中 edit_file/write_file 前检查 `FileTracker.GetStatus()`
  - 文件 stale → 返回警告要求先 re-read（对标 Crush edit guard）
  - 第二次调用确认后放行；成功写入自动更新 FileTracker 哈希
- **效果**：防止 Agent 基于过期文件内容做编辑决策

### 🐛 P0-4：任务系统升级 — CRUD + 依赖（TodoTool.cs）

- **根因**：旧 TodoTool 无依赖、无描述、无持久化、int ID
- **修复**：重写为对标 Crush todos + Claude Code TaskCreate 的完整 CRUD 工具
  - string ID / description 字段 / deps 依赖列表 / 5 种状态含 blocked
  - 依赖检测：blocked→in_progress 需依赖完成；完成自动解除阻塞任务
  - 持久化 `.waycoder/todos.json`；兼容旧 `Items` API（侧栏、/todo 命令、自测）
- **效果**：Agent 可规划依赖型多步骤工作，任务跨会话持久保留

---

## v0.34.1 (2026-08-12) — 稳定性修复 8 项 + 竞品分析

### 🧪 Roguelike 稳定性测试

WayCoder 编写 10,418 行 Roguelike 游戏项目（35 文件），期间发现并修复 8 个问题。

### 🐛 P0 修复：工具参数静默丢失（LLM.cs）

- **根因**：`ParseArgs()` JSON 解析失败时静默返回空字典 `{}`，工具调用参数丢失
- **修复 1**：`ParseArgs` 失败时返回 `_parse_error` 标记字典 + 原始 JSON 片段，调用方可检测
- **修复 2**：`TryParseCompleteJson` 新增 `IsJsonProbablyComplete()` 完整性预检：花括号平衡、不以逗号/冒号结尾、引号成对
- **修复 3**：流式结束后检查 `[DONE]` 标记，未收到则记录截断警告
- **修复 4**：最终解析循环中检测 `_parse_error` 标记并记录日志

### 🐛 P0 修复：系统提示词强制探索模式（SystemPrompt.cs + Agent.cs）

- **根因**：`critical_rules` 第 1 条和 `workflow` 强制"先读后改"，与用户"不要读文件"指令冲突
- **修复**：新增快速模式 — 检测用户消息中的关键词（不要读文件/不要ls/不要规划/直接用write_file），自动替换工作流和规则 1 为"直接执行"版本
- `SystemPrompt.DetectFastMode()` 关键词检测 + `StandardWorkflow/FastModeWorkflow/StandardRule1/FastModeRule1` 公开属性
- `Agent.FullMessages()` 快速模式时替换工作流文本

### 🐛 P1 修复：思考流代码丢失（LLM.cs + SystemPrompt.cs）

- **根因**：`reasoning_content` 显示但不存储，模型在思考中生成 400 行代码 → 流截断后零落盘
- **修复 1**：新增 `_reasoningBuffer` StringBuilder 旁路缓冲区，累积推理文本
- **修复 2**：流结束后检测代码特征（`;` `{` 计数 > 20），警告可能丢失代码
- **修复 3**：推理内容保存到 `DebugLog.Log("reasoning", ...)` 供调试恢复
- **修复 4**：SystemPrompt `critical_rules` 新增第 16 条：不要在思考流中生成代码

### 🐛 P1 修复：最大轮次静默退出（Agent.cs）

- **根因**：达到 `_effectiveMaxRounds` 后返回固定消息，不报告完成状态
- **修复**：检测最近 10 条消息中的 `✅ 已写入/✅ 编辑完成` 标记，区分"正在写文件时退出"和"已完成退出"，输出差异化提示

### 🐛 P1 修复：ContinuePrompt 缩小已有文件（Agent.cs）

- **根因**：压缩后的继续提示不说"不要重写"，模型重新读取后可能用更短版本覆盖
- **修复 1**：ContinuePrompt 追加"不要重写或缩小已有文件"
- **修复 2**：自动收集已创建/修改的文件清单（从 write_file/edit_file 工具结果提取）注入继续提示

### 🐛 P1 修复：过度规划无渐进催促（Agent.cs）

- **根因**：首轮分析不行动只催促一次，模型继续分析时没有逐次加强的迫使
- **修复**：新增 `_analysisOnlyStreak` 计数器，第 1 次温和催促 → 第 2 次严肃要求 → 第 3+ 次严重警告。工具调用时自动重置

### 🐛 P1 修复：FallbackLLM 静默失败（LLM.cs + FallbackLLM.cs + Agent.cs）

- **根因**：所有回退模型失败后返回纯文本错误，Agent 当成正常回复退出
- **修复 1**：`LLMResponse` 新增 `IsFatalError` 标记
- **修复 2**：`FallbackLLM` 错误响应设置 `IsFatalError = true`
- **修复 3**：`Agent` 检测到致命错误时自动调用 `SessionManager.SaveSession()` 保存会话

### 📊 竞品对比分析

对比分析 Crush（Go）和 Claude Code（TypeScript）两大竞品，输出 15 项可借鉴改进，优先级排序：
- **P0**：孤立工具调用修复、per-tool 循环检测、编辑 mtime 检查、任务 CRUD 系统
- **P1**：流式工具执行、bash 自动后台、Agent 类型注册表、摘要保 todo、Hook 扩展
- **P2**：Worktree 隔离、工具描述缩短、工具结果磁盘持久化、文件建议、花费追踪

---

## v0.34.0 (2026-08-12) — 系统化流水线 + 渐进超时重试 + Hook 系统 + 动态栏

### 🐛 聊天列表不实时刷新（修复）

- **根因**：`AppendToLast()` 在流式 token/tool 输出时更新内容但未设脏标记，`TuiManager.Render()` 的 `IsDirty` 检查跳过渲染
- **修复**：`AppendToLast()` 尾部新增 `Manager.IsDirty = true`，折叠提示同步 `MarkDirty()`
- **原理**：只需标记 Manager — TuiView 子容器总是被遍历，`ChatList.OnRender` 渲染所有可见子项无需单独标记

### 🧠 10 阶段系统化流水线（`<systematic_phases>`）

SystemPrompt 新增 `<systematic_phases>` 区块，复杂任务（3+ 文件、多步骤、新建项目）强制按 10 阶段执行：
**调查→分析→规划→拆分→分工→执行→调试→审核→提交→总结**。每个阶段内部完成，不向用户叙述过程，只交付结果。

### ⏱ 渐进超时 + 自适应重试

- **逐次加长超时**：1x→1.5x→2x→3x→4x→6x→8x 倍率，每次重试独立 `CancellationTokenSource`
- `GetTimeoutMultiplier(attempt)` 计算当前尝试的超时倍率，超出内置数组后线性递增
- 超时日志记录当前超时秒数 + 下次尝试的超时秒数，便于诊断
- 每次 HTTP 调用入口恢复原始 `_http.Timeout`，确保并发请求不受影响
- 默认重试次数：3→5，HTTP 超时上限：900s→3600s

### 🎣 Hook 系统全面升级（对标 Claude Code Hooks）

**8 种事件类型**：
- `PreToolUse` — 工具调用前（可阻止/批准/拒绝）
- `PostToolUse` — 工具调用成功后
- `PostToolUseFailure` — 工具调用失败后
- `SessionStart` — 会话启动时
- `SessionEnd` — 会话结束时
- `Stop` — Agent 完成一轮后（可注入额外上下文）
- `PreCompact` — 上下文压缩前
- `Notification` — 通知事件（权限提示等）

**结构化输出协议**：
- `HookOutput` JSON 格式：`Continue`（继续/阻止）、`Decision`（approve/block）、`Reason`、`SystemMessage`、`AdditionalContext`
- `HookMatcherConfig` 匹配器：支持管道分隔 `"bash|git"`、正则 `"^Write"`、通配符 `"*"`
- 向后兼容纯文本 stdout（非 JSON 视为 SystemMessage）

### 📊 动态状态栏（对标 Claude Code Status Line）

- **`TuiDynamicBar`** 1 行控件：左段（模型状态 + 旋转动画）、中段（当前工具/任务）、右段（上下文压缩进度条）
- **6 种状态**：Idle / Thinking / ToolRunning / Compressing / WaitingPerm / Error
- **Braille 旋转动画**：⣾⣽⣻⢿⡿⣟⣯⣷ 基于 `DateTime.UtcNow` 计算帧（不依赖定时器）
- **上下文压缩进度条**：`████░░░░ 45%` 迷你 8 字符进度条
- 订阅 `ContextManager.CompressProgress` 事件，实时层号和进度百分比

### 💰 任务级花费追踪

- `LLM.TaskPromptTokens` / `TaskCompletionTokens`：当前任务的 token 消耗（从快照点计）
- `LLM.TaskCost`：当前任务的花费估算（美元），模型在定价表中时返回
- `LLM.SnapshotTaskCost()`：Agent 每轮对话开始时调用，建立快照
- `LLM.ResetTaskCost()`：任务取消或异常时重置

### 📝 工作总结报告 + 结构化 Todo

- **`WorkReporter`**：Agent 完成一轮后自动生成结构化摘要，包含新增/修改/删除文件、关键决策、下一步
- 报告保存到 `.waycoder/reports/latest.md`，失败不影响主流程
- **`ExportTool`**：对话导出工具（Markdown / JSON / HTML），Agent 可在用户请求时调用
- **`StructTodoTool`**：增强版 Todo 工具，支持优先级、依赖关系、状态追踪

### 📟 终端协议增强

- **Bracketed Paste**：启用 `\x1b[200~...\x1b[201~` 包裹粘贴内容，`ReadPasteContent()` 安全读取
- **Kitty 键盘协议**：`AnsiTty.EnableKittyKeyboard()` 启用修饰键完整报告
- **CSI 功能键解析器**：统一处理 Bracketed Paste + Kitty + xterm 功能键序列
- 粘贴内容通过 `InputType.Paste` 事件路由，自动过滤 ANSI 转义序列

### 🗂 槽位任务队列（`-p1`~`-p0`）

- **槽位专项任务**：`-p1 "提示词"` ~ `-p9`、`-p0`=F10，同一槽位多次 `-pN` 可排队
- **共享前缀**：`-pa "前缀"` 拼到每个 `-pN` 任务前面
- 槽位任务自动强制进入 REPL 交互模式（非一次性模式）
- `BuiltinArgs` 新增 11 个 `slot-prompt-N` 参数注册

### 🧠 跨会话记忆检索

- **`MemoryRetrieval`**：跨会话记忆检索，使用 TF-IDF + 时间衰减排序
- 系统提示词生成时自动加载匹配记忆（最多 5 条），与结构化记忆合并注入
- 防抖机制：相同查询 60 秒内不重复检索

### 🔧 上下文压缩改进

- **Snip 阈值**：1500→4000 字符（保留更多工具输出信息）
- **错误行保留**：裁剪时保留编译错误、异常堆栈等关键诊断信息
- **IsCompressing 状态**：静态属性标记压缩进行中，UI 可据此显示进度
- **`CompressProgress` 事件**：每层压缩完成时触发，包含层号、消息、百分比
- **`ProgressBar()`**：8 字符迷你进度条生成器

### 🏗 基础设施新增

| 文件 | 说明 |
|------|------|
| `Agent/TaskProgress.cs` | 任务进度追踪（并发安全） |
| `Agent/WorkReporter.cs` | 工作总结报告生成器 |
| `Infra/IdGenerator.cs` | 加密安全 ID 生成 |
| `Infra/LruCache.cs` | 线程安全 LRU 缓存（支持 TTL 过期） |
| `Infra/MemoryRetrieval.cs` | 跨会话记忆检索 |
| `Infra/RetryPolicy.cs` | 智能重试策略（指数退避 + 异常过滤） |
| `Infra/SnippetStore.cs` | 代码片段管理器 |
| `Infra/Logging/` (9 文件) | 结构化日志系统（ILogSink/File/Console/JSON + 指标） |
| `Tools/ExportTool.cs` | 对话导出工具 |
| `Tools/StructTodoTool.cs` | 结构化 Todo 工具 |
| `UI/TuiControls/TuiDynamicBar.cs` | 动态状态栏控件 |
| `UI/TuiControls/TuiKeybindHelp.cs` | 键盘快捷键帮助面板 |
| `UI/TuiControls/TuiToastQueue.cs` | Toast 通知队列 |

### ⚙ 配置变更

- `LlmMaxRetries`：3→5（更多重试机会）
- `LlmHttpTimeoutSec` 上限：900→3600（1 小时，适应深度思考模型）
- `WatchExtensions` + `WatchIgnoreDirs`：新注册到设置界面（67/67 全部可配）
- `ToolTimeout` 默认：300s
- `BackgroundTaskTimeoutSec` 默认：1200s

### 📋 修改文件清单

| 文件 | 变更 |
|------|------|
| `Agent/Agent.cs` | Stop hook + WorkReporter + CompressWithSmallModel 进度回调 |
| `Agent/ContextManager.cs` | CompressProgress 事件 + Snip 4000→4000 字符 + 错误行保留 + ProgressBar |
| `Agent/LLM.cs` | 渐进超时 + 任务花费追踪 + CallWithRetryAsync 重构 |
| `Agent/SystemPrompt.cs` | `<systematic_phases>` 10 阶段流水线 + MemoryRetrieval 整合 |
| `Arguments/BuiltinArgs.cs` | 新增 11 个 `slot-prompt-N` 参数 + `prompt-all` |
| `Arguments/CliArg.cs` | `GetAll` 静态方法 |
| `Arguments/CliArgRegistry.cs` | `GetAll` 多值获取 |
| `Commands/StatsCommand.cs` | 任务花费显示 |
| `Config/Config.cs` | MaxRetries 3→5 + TimeoutSec max 900→3600 + Watch 配置注册 |
| `Config/Global.cs` | v0.33.1 → v0.34.0 |
| `Infra/HooksManager.cs` | 全面重构：8 事件 + JSON 协议 + 匹配器 + 并发安全队列 |
| `Program.cs` | 槽位任务队列 + Bracketed Paste/Kitty 键盘启用 |
| `SelfTest.cs` | +23 项测试（系统化流水线 + 渐进超时 + 花费追踪 + 配置默认值） |
| `Terminal/AnsiTty.cs` | EnableBracketedPaste + EnableKittyKeyboard |
| `Terminal/Terminal.cs` | BracketedPaste + KittyKeyboard 封装 |
| `Tools/ToolRegistry.cs` | 注册 ExportTool + StructTodoTool |
| `TuiDemo.cs` | 动态栏演示 |
| `UI/TuiBase/BoxBuffer.cs` | 清理冗余代码 |
| `UI/TuiBase/InputManager.cs` | Bracketed Paste + Kitty Keyboard + CSI 统一解析 |
| `UI/TuiScreens/ChatScreen.cs` | 动态栏集成 + 状态同步 + CompressProgress 订阅 |

### 🧪 测试

- 1407 项自测全部通过（+23 项新增）

---

## v0.33.1 (2026-08-11) — 对话框刷新修复 + CSI 功能键解析 + TuiDemo 重构

### 🐛 对话框关闭 → 背景刷新修复

三处联动修复，解决关闭模态窗口后背景残留窗口残影的问题：

- **TuiManager `_needsFullRefresh`**：`RequestFullRefresh()` 设置标志，`Render()` 检测该标志时跳过增量渲染、发送 `ClearScreen` ANSI
- **TuiScreen `MarkDirtyInRect`**：关闭窗口时递归标记被遮挡区域的控件为脏，正确处理滚动容器坐标偏移
- **TuiView/TuiListView `EffectiveScrollOffset`**：虚拟属性抽象，`MarkDirtyInRect` 用于计算滚动容器中子控件的真实屏幕坐标

### ⌨️ CSI 功能键解析器（InputManager）

- **`TryParseCsiFunctionKey(char firstChar)`**：读取完整 CSI 参数串（`\x1b[num;modP/~`），委托给解析方法
- **`ParseCsiFuncKey(string, char)`**：解析 xterm 修饰键编码（2=Shift, 3=Alt, 4=Shift+Alt, 5=Ctrl, 6=Ctrl+Shift, 7=Ctrl+Alt, 8=Ctrl+Shift+Alt），支持 `P` 终止符（F1-F4）和 `~` 终止符（F5-F12 + 旧编码 F1-F24）
- 非 SGR 鼠标的 CSI 序列现在先尝试解析为功能键，失败后才吞掉序列
- **注**：macOS 终端（Terminal.app / iTerm2）默认不发送 `Shift+Fx` 序列，此功能为支持这些序列的终端提供正确解析

### 🎮 TuiDemo 重构

- **Slash 命令**：6 个全屏对话框改为 `/m /s /r /c /f /b` 输入框命令触发，跨平台兼容
- **PendingSubmissions 消费循环**：主循环新增 `TryDequeue` 逻辑，修复 Enter 提交的消息从未被 `OnSubmit` 处理的 bug
- **欢迎消息**：更新为 Slash 命令说明 + 全屏对话框列表

### 📋 文件清单

| 文件 | 变更 |
|------|------|
| `UI/TuiBase/TuiManager.cs` | 修改：`_needsFullRefresh` + `RequestFullRefresh()` 公共方法 |
| `UI/TuiBase/TuiScreen.cs` | 修改：`CloseWindow` dirty rects + `MarkDirtyInRect` 递归标记 |
| `UI/TuiBase/TuiView.cs` | 修改：`EffectiveScrollOffset` 虚拟属性 (default 0) |
| `UI/TuiBase/InputManager.cs` | 修改：`TryParseCsiFunctionKey` + `ParseCsiFuncKey` + `ConsumeCsi(char)` |
| `UI/TuiControls/TuiListView.cs` | 修改：`EffectiveScrollOffset` override |
| `UI/TuiControls/TuiDialog.cs` | 修改：try/finally `RequestFullRefresh` |
| `UI/TuiCust/ModelPicker.cs` | 修改：try/finally `RequestFullRefresh` |
| `UI/TuiCust/SessionPicker.cs` | 修改：try/finally `RequestFullRefresh` |
| `UI/TuiCust/ReasoningPicker.cs` | 修改：try/finally `RequestFullRefresh` |
| `UI/TuiCust/CommandPalette.cs` | 修改：try/finally `RequestFullRefresh` |
| `UI/TuiCust/FilePicker.cs` | 修改：try/finally `RequestFullRefresh` |
| `TuiDemo.cs` | 重构：Slash 命令 + PendingSubmissions 消费 + 欢迎消息更新 |
| `Config/Global.cs` | v0.33.0 → v0.33.1 |

### 🧪 测试
- 编译 0 错误

---

## v0.33.0 (2026-08-11) — 鼠标全面修复 + 推理深度 + 会话管理

### 🖱️ 鼠标全面修复（对标 Crush 鼠标系统）

- **启用鼠标追踪**：取消 `Tty.EnableMouse()` 的注释，终端可正常上报 SGR 鼠标事件
- **主循环路由**：鼠标事件不再被吞掉，通过 `TuiManager → TuiScreen → 控件树` 正常路由
- **窗口内控件路由**：`TuiWindow.HandleMouse` 新增子控件点击/滚动/悬停路由
- **事件冒泡**：`TuiView.HandleMouse` 支持从最深命中控件向上冒泡，确保父容器（如 TuiListView）可处理子控件未消费的事件
- **SGR 运动事件**：`InputEvent` 新增 `MouseMotion` 属性，解析 SGR 代码 35/36/39（鼠标移动追踪）
- **TuiListView 滚轮**：鼠标滚轮滚动列表（3 行/格），点击可选中列表项
- **TuiButton hover**：鼠标悬停自动高亮按钮（蓝底白字）

### 🧠 推理深度选择器（对标 Crush reasoning.go）

- **`ReasoningPicker`**：全屏 ANSI 对话框，5 级推理深度（Minimal / Low / Medium / High / Max）
- **实时搜索** / **当前级别 ✓ 标记** / **清除恢复默认**
- **`Config.ReasoningEffort`**：新增配置属性 + `WAYCODER_REASONING_EFFORT` 环境变量
- **`LLM.cs`**：请求体中自动携带 `reasoning_effort` 参数（DeepSeek V4 / OpenAI o-series）
- **快捷键**：`Ctrl+G` 打开推理深度选择器

### 📂 会话管理器（对标 Crush sessions.go）

- **`SessionPicker`**：全屏 ANSI 对话框，浏览 / 切换 / 重命名 / 删除历史会话
- **三种模式**：Normal（选择）→ Renaming（内联重命名）→ Deleting（确认删除）
- **实时搜索** / **当前会话 ✓ 标记** / **相对时间显示**
- **`SessionManager.RenameSession`**：新增重命名方法（更新 JSON 内 id + 文件重命名）
- **`SessionManager.CreateNewSessionId`**：公开的 ID 生成方法
- **快捷键**：`Ctrl+S` 打开会话管理器

### 📋 文件清单

| 文件 | 变更 |
|------|------|
| `UI/TuiCust/ReasoningPicker.cs` | 新增：推理深度选择对话框 |
| `UI/TuiCust/SessionPicker.cs` | 新增：会话管理对话框 |
| `Config/Config.cs` | 修改：新增 ReasoningEffort 属性 + Schema 项 |
| `Agent/LLM.cs` | 修改：请求体添加 reasoning_effort 参数 |
| `Memory/SessionManager.cs` | 修改：新增 RenameSession / CreateNewSessionId |
| `UI/TuiBase/InputManager.cs` | 修改：启用鼠标追踪 + MouseMotion 解析 |
| `UI/TuiBase/TuiManager.cs` | 修改：Enter() 中启用鼠标 |
| `UI/TuiBase/TuiView.cs` | 修改：HandleMouse 新增事件冒泡 |
| `UI/TuiBase/TuiWindow.cs` | 修改：HandleMouse 新增子控件路由 + 滚轮处理 |
| `UI/TuiControls/TuiListView.cs` | 修改：新增鼠标滚轮/点击处理 |
| `UI/TuiControls/TuiButton.cs` | 修改：新增鼠标悬停高亮 |
| `UI/TuiScreens/ChatScreen.cs` | 修改：新增 OnOpenSessions / OnReasoningEffort 回调 + Ctrl+S/Ctrl+G |
| `Program.cs` | 修改：启用鼠标路由 + 新对话框回调 |
| `Config/Global.cs` | v0.32.2 → v0.33.0 |

### 🧪 测试
- 1348 项自测全部通过

---

## v0.32.2 (2026-08-11) — 权限/输入/聊天 三大对标

### 🛡️ 权限行内渲染（对标 Crush inline permission）

- **`InlinePermission`**：在聊天流中直接嵌入 3 行黄色背景交互确认块，无需弹模态窗口
- **Y/N/A/D 快捷键**：Y=允许 N=拒绝 A=全允 D=展开详情，直接在消息列表中响应
- **工具参数着色**：bash 命令绿色高亮、write_file/edit_file 路径青色高亮
- **展开/折叠详情**：按 D 展开完整参数，再次按 D 折叠
- **已解决状态**：确认后自动变为灰色决议标记（✅/❌）
- `PermissionManager.ShowConfirmDialog` 更新：调用新的 `ChatScreen.ShowInlinePermission(toolName, summary, detail, isDangerous)`
- `TestCommand` 权限演示适配新接口

### ✏️ TuiDialog 多行输入升级（对标 Crush textarea）

- **`TuiDialog.Input()`** 从单行 `TuiInput` 升级为 3 行高 `TuiTextArea`，支持 Ctrl+Enter 硬换行
- **输入历史**：新建 `TuiInputHistory` 静态类，按字段名记录最近 50 条输入
- **自动预填**：对话框打开时自动填充该字段最近一次输入
- **AOT 安全持久化**：简单文本格式 `field|value` 保存到 `~/.waycoder/input_history.txt`
- **全局裁剪**：500 条全局上限 + 每字段 50 条上限 + 自动去重

### 💬 聊天输入增强

- **输入历史持久化**：`ChatScreen` Enter 发送时保存到 `TuiInputHistory`，重启后恢复
- **粘贴确认**：`ChatScreen.PasteAsync()` 超长(>500字符)或多行(>3行)弹出确认对话框
- **粘贴确认（旧）**：`TuiChatInput.Paste()` 同样增加 Y/N 确认
- **斜杠命令补全**：`BuildDefaultHints` 增加 `/model set`/`/model list`/`/model import`/`/perm ask`/`/perm auto` 等子命令提示

### 📋 文件清单

| 文件 | 变更 |
|------|------|
| `UI/TuiCust/InlinePermission.cs` | 新增：行内权限确认控件 |
| `UI/TuiInputHistory.cs` | 新增：输入历史管理器 + 持久化 |
| `Skills/PermissionManager.cs` | 修改：ShowConfirmDialog 使用新 InlinePermission |
| `UI/TuiScreens/ChatScreen.cs` | 修改：ShowInlinePermission 内嵌控件/粘贴确认/历史持久化/子命令提示 |
| `UI/TuiControls/TuiDialog.cs` | 修改：Input() 升级为 TuiTextArea 多行 + 历史预填 |
| `UI/TuiCust/TuiChatInput.cs` | 修改：Paste() 增加 Y/N 确认 |
| `Commands/TestCommand.cs` | 修改：权限演示适配新接口 |
| `Config/Global.cs` | v0.32.1 → v0.32.2 |

### 🧪 测试
- 1348 项自测全部通过

---

## v0.32.1 (2026-08-11) — 补齐 UI 控件与对话框

### 🎨 模型选择对话框（对标 Crush models.go）

- **`ModelPicker.Show()`**：全屏 ANSI 直写模式，21+ 模型按供应商分组（DeepSeek/OpenAI/Anthropic/Google/Qwen/Zhipu）
- **Tab 切换大/小模型**：标题栏实时显示当前选择类型，Tab 键切换并重置搜索
- **实时搜索过滤**：输入即过滤，按模型名/ID/供应商名匹配
- **键盘导航**：↑↓ 导航、Enter 确认、Esc 取消、Home/End/PgUp/PgDn
- **当前使用标记**：当前正在使用的模型带 ✓ 标记，自动定位到当前选择
- **Ctrl+M 接入**：由原来的 4 模型轮换改为打开 ModelPicker 对话框
- **Settings 接入**：设置页面的模型下拉打开 ModelPicker 而非基础 TuiList

### 🕹️ 按钮控件增强（对标 Crush button.go）

- **`TuiButton` 增强**：新增 `UnderlineIndex` 快捷键下划线、`IsSelected` 选中高亮、`IsHovered` 悬停状态、`MinWidth` 最小宽度
- **`TuiButtonGroup`**：按钮组容器，水平/垂直布局、Tab/Shift+Tab 切换、方向键导航、字母快捷键识别
- **`TuiButtonGroup.AddRange()`**：批量添加按钮，自动检测大写字母为快捷键

### 📜 TuiScrollbar 独立滚动条（对标 Crush scrollbar.go）

- **`TuiScrollbar`**：独立垂直滚动条，bar/dot/block 三种样式
- **滑块拖拽**：鼠标拖拽滑块精确定位，`OffsetFromMouse()` 坐标换算
- **鼠标滚轮**：`HandleMouse(InputEvent)` 支持 ScrollUp/ScrollDown
- **自动隐藏**：`AutoHide=true` 时内容无需滚动自动隐藏
- **键盘支持**：PgUp/PgDn/Home/End 控制滚动

### 📁 文件选择对话框（对标 Crush filepicker）

- **`FilePicker.Show()`**：全屏 ANSI 直写，目录浏览 + 文件选择
- **目录导航**：Enter 进入子目录、Backspace 返回上级、".." 快捷项
- **文件信息**：显示 📁/📄 图标、文件大小(K/M/G)、修改时间(MM-dd HH:mm)
- **搜索过滤**：输入文件名过滤，大小写不敏感
- **多级排序**：目录优先 → ".." 最前 → 字母序

### 🔍 命令面板（对标 Crush command palette）

- **`CommandPalette.Show()`**：通用命令面板框架，全屏 ANSI 直写
- **分类分组**：命令按 Category 分组，蓝色粗体类别标题分隔
- **模糊搜索**：搜索标签/类别/描述/快捷键，实时显示匹配数/总数
- **快捷键显示**：每个命令右侧黄色粗体显示快捷键

### 📋 文件清单

| 文件 | 变更 |
|------|------|
| `UI/TuiCust/ModelPicker.cs` | 新增：模型选择对话框 |
| `UI/TuiControls/TuiButton.cs` | 增强：UnderlineIndex/IsSelected/IsHovered/MinWidth |
| `UI/TuiControls/TuiButtonGroup.cs` | 新增：按钮组容器 |
| `UI/TuiControls/TuiScrollbar.cs` | 新增：独立滚动条 |
| `UI/TuiCust/FilePicker.cs` | 新增：文件选择对话框 |
| `UI/TuiCust/CommandPalette.cs` | 新增：命令面板 |
| `Program.cs` | 修改：Ctrl+M 接入 ModelPicker |
| `UI/TuiScreens/SettingsScreen.cs` | 修改：模型下拉接入 ModelPicker |
| `Config/Global.cs` | v0.32.0 → v0.32.1 |

### 🧪 测试
- 1348 项自测全部通过

---

## v0.32.0 (2026-08-11) — 对标 Crush TUI 四大模式

### 🔧 ToolRenderer 接口（对标 Crush ToolMessageItem）

- **`IToolRenderer`** 接口：每种工具类型独立渲染器，`FormatHeader` + `FormatOutput`
- **`ToolRendererFactory`**：按工具名分发（含 MCP 工具名解析），支持别名注册
- **6 个具体渲染器**：
  - `BashToolRenderer`：💻 图标 + 退出码着色（绿=成功/红=失败）+ stderr 红色标记
  - `EditToolRenderer`：✏️ 图标 + diff 着色（红底删除/绿底新增/青色 hunk 头）
  - `WriteToolRenderer`：📝 图标 + 成功绿色/错误红色
  - `AgentToolRenderer`：🤖 图标 + 深度标记蓝色 + 子任务分隔线黄色
  - `ReadFileToolRenderer`：📖 图标
  - `GlobGrepToolRenderer`：🔍 图标（glob + grep 共用）
- `ChatScreen.AddToolProgress` 集成：工具调用头自动使用对应 emoji 和格式

### 🪟 Dialog Overlay 栈（对标 Crush Overlay + typed Action）

- **`DialogOverlay`**：栈式对话框管理器，Push/Pop/Clear 操作
- **按 ID 管理**：同 ID 自动替换，支持嵌套对话框（确认→文件选择→权限）
- **类型化 Action 结果**：`DialogAction.Close` / `Confirm` / `Cancel` / `Select<T>` / `Permission` / `FilePicked` / `MultiSelect<T>` / `TextInput`
- Esc 自动关闭栈顶，焦点自动恢复
- 与现有 TuiScreen/TuiWindow 系统无缝兼容

### 📜 懒渲染列表（对标 Crush List + Item 接口）

- **`ILazyItem`** 接口：`MeasureHeight(width)` 预估高度 + `IsRenderCached` 缓存标记 + `InvalidateCache()`
- **`TuiMarkdown` 实现 `ILazyItem`**：已缓存时 O(1) 高度，未缓存时按字符折行估算
- **`TuiListView.FindFirstVisibleIndex()`**：二分查找 O(log n) 定位首个可见项
- **`OnRender` 优化**：从 firstVisibleIndex 开始遍历，`childScreenY >= screenBottom` 提前终止

### 💾 渲染缓存（对标 Crush cachedMessageItem）

- **已有实现**：`TuiMarkdown._parsed` + `_lastContent` + `_lastMaxWidth` 三级缓存
- `EnsureParsed()` 仅在内容或宽度变化时重新解析
- `ILazyItem.IsRenderCached` 对外暴露缓存状态

| 文件 | 变更 |
|------|------|
| `UI/TuiCust/ToolRenderers/IToolRenderer.cs` | 新增：接口 + 工厂（含 MCP 支持） |
| `UI/TuiCust/ToolRenderers/BashToolRenderer.cs` | 新增：bash 输出着色 |
| `UI/TuiCust/ToolRenderers/EditToolRenderer.cs` | 新增：diff 红绿着色 |
| `UI/TuiCust/ToolRenderers/WriteToolRenderer.cs` | 新增：文件创建摘要 |
| `UI/TuiCust/ToolRenderers/AgentToolRenderer.cs` | 新增：子智能体状态 |
| `UI/TuiCust/ToolRenderers/ReadFileToolRenderer.cs` | 新增：read/glob/grep 渲染 |
| `UI/TuiCust/ToolRenderers/DefaultToolRenderer.cs` | 新增：默认直通 |
| `UI/TuiCust/DialogAction.cs` | 新增：类型化 Action 结果 |
| `UI/TuiCust/DialogOverlay.cs` | 新增：栈式叠层管理器 |
| `UI/TuiControls/ILazyItem.cs` | 新增：懒渲染项接口 |
| `UI/TuiControls/TuiMarkdown.cs` | 修改：实现 ILazyItem + MeasureHeight |
| `UI/TuiControls/TuiListView.cs` | 修改：二分查找首个可见项 + 提前终止 |
| `UI/TuiScreens/ChatScreen.cs` | 修改：AddToolProgress 使用 ToolRenderer |
| `Config/Global.cs` | v0.31.11 → v0.32.0 |

### 🧪 测试
- 1348 项自测全部通过

## v0.31.11 (2026-08-11) — Diff 语法高亮

### 🎨 Diff 语法高亮

- **Token 级语法高亮**：Diff 中的代码行按语言（14 种）进行 token 级着色
- 关键字（蓝）、字符串（绿）、数字（黄）、注释（灰）等在 diff 背景上正确显示
- **Unified 模式**：`-`/`+` 行的代码在红/绿背景上保留语法颜色
- **Split 模式**：左右面板代码各自独立语法高亮
- 上下文行在蓝底（当前 hunk）上也语法高亮
- `GetSyntaxForFile()` 根据文件扩展名自动选择语法定义
- `AppendHighlightedCode()` 将 Tokenize 结果渲染到 diff 背景色上

| 文件 | 变更 |
|------|------|
| `UI/TuiCust/DiffPreview.cs` | GetSyntaxForFile + AppendHighlightedCode + unified/split 渲染改造 |
| `Config/Global.cs` | v0.31.10 → v0.31.11 |

### 🧪 测试
- 1348 项自测全部通过

## v0.31.10 (2026-08-11) — Diff 双模式（Unified/Split）

### 📊 Diff 双模式渲染

- **Split 侧边对照**：终端宽度 ≥ 120 时自动启用，删除（左红）↔ 新增（右绿）并排对照
- **Unified 统览**：窄屏自动回退传统 unified diff
- `BuildSplitRows()`：将 diff hunk 中的删除/新增行配对为 SplitRow（LeftText/RightText + LeftKind/RightKind）
- `RenderSplitRow()`：左面板 + ` │ ` 分隔符 + 右面板渲染，带行号偏移
- 滚动计算兼容两种模式（`totalVisualLines` = splitRows.Count 或 allLines.Count）

| 文件 | 变更 |
|------|------|
| `UI/TuiCust/DiffPreview.cs` | SplitRow 类 + BuildSplitRows/RenderSplitRow + useSplitMode 自动切换 |
| `Config/Global.cs` | v0.31.9 → v0.31.10 |

### 🧪 测试
- 1348 项自测全部通过

## v0.31.8 (2026-08-11) — 工具白名单/黑名单 + 配置完善

### 🔒 工具白名单/黑名单

- `Config.AllowedTools`：逗号分隔白名单，仅允许列表中的工具可用（空=全部允许）
- `Config.DisabledTools`：逗号分隔黑名单，禁止列表中工具（空=不禁用）
- 环境变量：`WAYCODER_ALLOWED_TOOLS` / `WAYCODER_DISABLED_TOOLS`
- 过滤发生在 Agent 构造函数（`FilterTools` 方法），对 Agent 和子 Agent 均生效
- 过滤后通过 `DebugLog.Log("tool-filter", ...)` 记录工具数量变化

### 🔧 配置完善

- Settings UI 新增"工具白名单"和"工具黑名单"两个配置项（`🔒 安全` 分类）

| 文件 | 变更 |
|------|------|
| `Config/Config.cs` | 新增 AllowedTools / DisabledTools 属性 + Settings 注册 |
| `Agent/Agent.cs` | 新增 `FilterTools()` 方法 |
| `Config/Global.cs` | v0.31.7 → v0.31.8 |

## v0.31.8-v0.31.9 (2026-08-11) — 工具白名单/黑名单 + FileTracker 集成 + 测试覆盖

### 🔒 工具白名单/黑名单

- `Config.AllowedTools`：白名单（环境变量 `WAYCODER_ALLOWED_TOOLS`）
- `Config.DisabledTools`：黑名单（环境变量 `WAYCODER_DISABLED_TOOLS`）
- Agent 构造函数中 `FilterTools()` 过滤，主 Agent 和子 Agent 均生效

### 📁 Stale-Read 文件变更检测集成

- Agent 主循环集成 `FileTracker.GetChangeWarning()`（对标 Crush 的 stale-read 保护）
- 每轮工具执行后检查已读取文件是否被外部修改
- 检测到变更时注入 tool 消息警告 LLM 重新读取过期文件
- 通过 `DebugLog.Log("file-tracker", ...)` 记录

### 🧪 测试增强

- SystemPrompt 新增 9 项结构化区块检查（`critical_rules` / `workflow` / `editing_files` / `exact_matching` / `task_completion` / `error_handling` / `testing` / `code_conventions` / 15 条规则）

| 文件 | 变更 |
|------|------|
| `Agent/Agent.cs` | FileTracker 变更检测集成 + 工具过滤 `FilterTools()` |
| `Config/Config.cs` | AllowedTools / DisabledTools 属性 + Settings UI |
| `SelfTest.cs` | SystemPrompt 区块验证（9 项新检查） |
| `Config/Global.cs` | v0.31.7 → v0.31.9 |

### 🧪 测试
- 1348 项自测全部通过

## v0.31.7 (2026-08-11) — 对标 Crush：SystemPrompt 重写 + 循环检测 + 工具描述增强

### 🧠 SystemPrompt 重写（对标 Crush coder.md.tpl）

**从 107 行扩展到 ~350 行，15 个结构化 XML 区块**

- `<critical_rules>` — 15 条硬规则（先读后改 / 自主行动 / 每次修改后测试 / 极简输出 / 精确匹配）
- `<code_references>` — `file_path:line_number` 引用规范
- `<workflow>` — 行动前（搜索/读取/检查记忆）→ 行动中（编辑/测试/修复）→ 完成前（验证/对照需求/lint）
- `<decision_making>` — 自主决策原则 + 停止条件（能查到就不问 / 绝不因任务大而停下）
- `<editing_files>` — 编辑工具使用指南（edit_file / multi_edit / write_file 选择 + 7 步编辑流程 + 常见错误）
- `<exact_matching>` — 精确匹配避坑指南（空格 vs Tab / 花括号前空格 / 注释后空格 / 编辑失败修复流程）
- `<task_completion>` — 端到端完成检查清单（行动前思考 → 完整接线所有组件 → 逐项验证原始需求）
- `<error_handling>` — 错误恢复流程（读错误 → 隔离 → 3 种方案 → 修复 → 测试）
- `<testing>` — 测试规范（具体到宽泛 / 自我验证 / lint + 类型检查）
- `<tool_usage>` — 工具使用最佳实践 + bash 非交互命令优先
- `<code_conventions>` — 先读后写 / 匹配风格 / 野心 vs 精确
- `<proactiveness>` — 自主性平衡（被要求就做完 / 不描述直接做 / 被问"如何"只解释不实现）
- `<final_answers>` — 回复详细程度分级（默认 3 行 / 复杂任务 10-15 行 / 避免废话）

**技术修复**：使用无 `$` 前缀的 `"""` 原始字符串 + `.Replace()` 注入变量，避免代码示例中 `{` 花括号导致 C# 插值解析错误

### 🔄 SHA256 循环检测（Crush 风格）

- `Agent/Agent.cs` 新增 `DetectAndBreakLoop()` 方法
- 每轮对（assistant 消息 + 工具结果）做 SHA256 哈希，8 轮窗口内相同哈希出现 3+ 次触发
- 3 级递进式反循环提示（换方法 → 重新评估 → 严重警告重置）
- 触发后清空窗口给 Agent 几轮调整时间
- 通过 `DebugLog.Log("loop", ...)` 记录检测事件

### 📝 编辑工具描述增强

- `EditFileTool.Description` 重写：强调"先读后改"、"逐字符匹配（空格、Tab、换行）"、"3-5 行上下文确保唯一"
- `EditFileTool` 参数级描述增强：`old_string` 提示"从 read_file 输出精确复制，不要凭记忆或近似猜测"
- `WriteFileTool.Description` 重写：强调"仅用于新建或整体重写 / 局部编辑用 edit_file / 覆写前先 read_file"

### 🔧 Agent 架构更新

| 文件 | 变更 |
|------|------|
| `Agent/SystemPrompt.cs` | 完全重写，107→350 行，15 个结构化区块 |
| `Agent/Agent.cs` | 新增 SHA256 循环检测（`DetectAndBreakLoop`） |
| `Tools/EditFileTool.cs` | 描述 + 参数描述大幅增强 |
| `Tools/WriteFileTool.cs` | 描述 + 参数描述增强 |
| `Config/Global.cs` | v0.31.6 → v0.31.7 |

### 🧪 测试
- 1339 项自测全部通过

### 📄 文档读取增强

**PDF 文本提取**（PdfPig 库，开源 + AOT 兼容）
- `Infra/PdfExtractor.cs`：提取 PDF 纯文本，分页返回，压缩连续空行
- `ReadFileTool` 新增 `.pdf` 处理：自动调用 PdfExtractor，支持 `offset`（起始页）/ `limit`（最大页数）
- PDF 上限 50 MB，单次最多 20 页
- `FileIgnoreManager` 移除 `.pdf` 过滤（现在 PDF 可被 glob/grep 发现）

**Markdown 结构化渲染**
- `ReadFileTool` 新增 `.md` 处理：使用 `MarkdownParser` 解析 AST
- 输出结构化：标题层级 / 代码块（带语言标注）+ 80 行截断 / 表格（30 行截断） / 列表 / 段落
- Markdown 上限 500 KB

### 🤖 模型回退链增强

**跨供应商 API Key 自动解析**
- `FallbackLLM.ResolveKeyAndUrl()`：根据 ModelCatalog 供应商自动查找对应 API Key
- 14 个供应商映射：DEEPSEEK / OPENAI / GEMINI / ANTHROPIC / DASHSCOPE / ZHIPU / ARK / MOONSHOT / MISTRAL / XAI / SILICONFLOW / GROQ / TOGETHER / OPENROUTER
- 无 Key 时优雅跳过（不崩溃）+ 提示设置环境变量

**回退链新增免费模型**
- `gemini-2.0-flash`（Google 免费层 15 RPM）
- `qwen-turbo`（阿里超低价 $0.05/$0.15）
- `glm-4-flash`（智谱低价 $0.07/$0.14）
- 新链：`deepseek-v4-flash → deepseek-v4-pro → gemini-2.0-flash → qwen-turbo → glm-4-flash → gpt-5.4-mini`

### 🧪 测试脚本
- `scripts/` 目录 7 个脚本：bench-models.sh / bench-local.sh / bench-models.ps1 / bench-quick.bat / quick-test.sh / stress-test.sh / run-all-tests.sh
- 支持云端 + Ollama 本地模型一键基准测试

### 🗂️ 新增 + 修改文件
| 文件 | 说明 |
|------|------|
| `Infra/PdfExtractor.cs` | PDF 文本提取器（PdfPig，AOT 兼容） |
| `Tools/ReadFileTool.cs` | 重构：PDF + Markdown + 文本三模式 |
| `Agent/FallbackLLM.cs` | 跨供应商 Key 解析 + 优雅跳过 |
| `Config/Config.cs` | 回退链 3→6 模型 + 默认 V4 Flash |
| `Infra/FileIgnoreManager.cs` | 移除 .pdf 过滤 |
| `Program.cs` | API Key 提示增加 Gemini/DashScope |
| `SelfTest.cs` | +10 测试（PDF/MD/回退链） |
| `WayCoder.csproj` | 新增 PdfPig NuGet 依赖 |
| `.gitignore` | 添加 games/ chess-test/ |

### 🧪 测试
- 1331 项自测全部通过

## v0.31.5 (2026-08-11) — DeepSeek V4 推理修复 + Crush 上下文管理 + 安全/追踪/诊断/本地模型

### 🧠 DeepSeek V4 推理内容修复（关键 Bug）
- **reasoning_content 污染对话历史**：DeepSeek V4 的 `reasoning_content` + Ollama/qwen 的 `reasoning` 字段不再存入 `contentParts`
- `TryGetReasoningText()` 统一处理两种字段名（`reasoning_content` / `reasoning`）
- 推理内容以暗色（«dim»）实时显示给用户，但不存入对话历史，不送入下一轮 API 调用
- tool_calls 到达时自动关闭暗色样式
- 流结束后安全关闭推理标记（防御性代码）
- **根因**：推理 token 计入 `max_tokens` 预算 → V4 消耗全部预算在推理上 → 零正式输出 → 修复后 Snake2（292 行）生成成功

### 🔄 Crush 风格上下文管理
- **真实 token 追踪**：`ContextManager.AddUsage()` 累积每次 API 返回的 prompt/completion tokens
- **自动摘要触发**：`ShouldStopAndSummarize()` — 大窗口（>200K）用 20K buffer，小窗口用 20% 比例
- **Auto-Continue**：摘要后自动注入继续提示（`ContinuePromptInjected`），防止 Agent 丢失任务上下文
- **自动续写增强**：检测"口述代码"（content >300 字符 + 代码标记）→ 追问使其写文件
- **首轮停滞检测**：模型首轮只分析不调用工具 → 自动追问执行
- 新增配置：`ContextWindowLargeThreshold=200K`、`ContextWindowLargeBuffer=20K`、`ContextWindowSmallRatio=0.2`、`AutoContinueAfterSummarize=true`

### 🛡️ Bash 命令安全系统（对标 crush bannedCommands + safeCommands）
- `BashGuard` 三层防护：禁止命令名拦截 + 参数级拦截 + 安全只读白名单
- 70+ 禁止命令（网络下载/系统修改/包管理器/网络配置）
- 47+ 安全只读命令自动放行（免权限确认）
- 参数级规则：阻止 `pip install`（允许 `--user`）、`npm install -g` 等
- 管道中每个命令独立检查（`|`, `;`, `&` 分割）

### 📊 文件追踪 / Stale-Read 检测（对标 crush filetracker）
- `FileTracker`：SHA256 哈希记录 + 外部变更检测 + LRU 淘汰（200 文件）
- 集成到 `ReadFileTool`/`WriteFileTool`/`EditFileTool`/`MultiEditTool`/`BashTool`

### 🔍 LSP 诊断自动附加（对标 crush diagnostics auto-attachment）
- `DiagnosticManager.FormatForLLM()` + `TryRunLintWithTimeout()`（3s 超时）
- 编辑/创建文件后自动附加 lint 错误/警告

### 🖥️ Ollama 本地模型支持
- 新增 6 个 Ollama 模型 + 通用 BaseUrl 自动解析 + 本地模型免 API Key
- **实测结论**：`qwen3.x:4b`（thinking 内置，全部 token 耗尽）→ 不可用；`qwen2.5-coder:1.5b`（无 thinking，快但 1.5B 太小无法工具调用）→ 需 7B+ 模型

### 📝 统一错误日志系统
- `ErrorLog` 四级日志 + 按天轮转 + 内存缓冲 + 全局异常捕获 + 噪音过滤

### ⚙️ 配置变更
- **默认模型**：`deepseek-v4-flash` → `deepseek-chat`（V4 推理有缺陷）
- **SmallModel**：同改为 `deepseek-chat`
- **MaxTokens**：4096 → 32768（推理模型需要更多预算）
- **LlmHttpTimeoutSec**：60 → 300（大窗口请求慢）
- **回退链**：`deepseek-chat,deepseek-v4-flash,deepseek-v4-pro,gpt-5.4-mini`
- **超时参数集中管理**：9 个超时配置项（BackgroundTask/AutoTest/Git/Kill/Download/Hook/AskUser/Regex/Fetch）
- **SystemPrompt 规则 10/11**：复杂任务先列 todo 清单，不要输出思考过程

### 🗂️ 新增 + 修改文件
| 文件 | 说明 |
|------|------|
| `Agent/LLM.cs` | reasoning_content 显示不存 + TryGetReasoningText + Endpoint + ErrorLog |
| `Agent/Agent.cs` | token 追踪 + Crush 自动摘要 + 自动续写增强 + ErrorLog |
| `Agent/ContextManager.cs` | AddUsage() + ShouldStopAndSummarize() + ResetUsage() |
| `Agent/SystemPrompt.cs` | 规则 10（todo 清单）+ 规则 11（不输出思考过程） |
| `Agent/FallbackLLM.cs` | 回退链 ErrorLog + 模型更新 |
| `Agent/BackgroundTask.cs` | 输出缓冲区增强 + ErrorLog |
| `Infra/BashGuard.cs` | Bash 命令安全防护（三层拦截 + 安全白名单） |
| `Infra/FileTracker.cs` | 文件追踪器（SHA256 哈希 + 变更检测） |
| `Infra/ErrorLog.cs` | 统一错误日志系统（四级日志 + 自动轮转 + 全局异常） |
| `Config/Config.cs` | 默认模型/MaxTokens/超时 + ContextWindow 阈值 + 超时参数集中管理 |
| `Config/ModelCatalog.cs` | 新增 6 个 Ollama 模型 + deepseek-chat 优先 + BaseUrl 修正 |
| `Config/Global.cs` | 版本号 v0.31.5 |
| `Edit/DiagnosticManager.cs` | FormatForLLM() + TryRunLintWithTimeout() |
| `Program.cs` | ModelCatalog BaseUrl 解析 + 本地模型免 API Key + ErrorLog |
| `Tools/BashTool.cs` | BashGuard 集成 + FileTracker 变更警告 + ErrorLog |
| `Tools/EditFileTool.cs` | FileTracker + LSP 诊断自动附加 |
| `Tools/WriteFileTool.cs` | FileTracker + LSP 诊断自动附加 |
| `Tools/ReadFileTool.cs` | FileTracker + 缓存诊断附加 |
| `Tools/MultiEditTool.cs` | FileTracker + LSP 诊断自动附加 |
| `SelfTest.cs` | 测试更新（默认模型 deepseek-chat） |
| `.gitignore` | 添加 games/ chess-test/ |

### 🧪 测试
- 1321+ 项自测全部通过

## v0.31.4 (2026-08-11) — 竞品对标强化：Skills 升级 + 工具全面增强 + 温度/上下文默认调整

### ✨ 新增功能

**Agent Skills 标准升级**（对标 crush agentskills.io）
- SkillDef 新增字段：`License`、`Compatibility`、`Metadata`（key:value 字典）、`Builtin`
- 名称验证：正则 `/^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$/`，最大 64 字符，必须匹配目录名
- 新增内置 skill：`waycoder-config`（WayCoder 配置指南）
- 多发现路径：新增 `.cursor/skills/` 兼容
- 系统提示词注入格式：Markdown 列表 → `<available_skills>` XML 结构
- 技能加载追踪：`SkillsManager.LoadedSkills` + `MarkLoaded()`

**桌面通知**（对标 crush notify）
- `DesktopNotifier`：Agent 完成/权限等待时终端标题闪烁 + 响铃
- Windows Toast 通知支持（PowerShell 集成）
- 配置项：`WAYCODER_ENABLE_NOTIFICATIONS`（默认关闭）
- 集成点：`PermissionManager` 等待时 + Agent 轮次完成时

**文件忽略系统**（对标 crush gitignore/.crushignore）
- `FileIgnoreManager`：加载 `.gitignore` 和 `.waycoderignore` 规则
- 通用垃圾目录/文件扩展名自动忽略
- 集成到 `GlobTool` 和 `GrepTool` 自动过滤结果

### 🚀 工具增强

**EditFileTool**（对标 crush edit）
- 新增 `replace_all` 参数：一次替换所有匹配项
- CRLF 行尾保留：编辑后保持原始换行格式

**MultiEditTool** — 批量编辑工具（对标 crush multiedit）
- 参数：`file_path` + `edits: [{old_string, new_string, replace_all?}]`
- 首个编辑 old_string 为空 → 创建新文件
- CRLF 行尾保留

**ReadFileTool** 增强（对标 crush view）
- 文件不存在时 "Did you mean?" 建议（Levenshtein 编辑距离匹配）
- UTF-8 验证
- 图片文件识别与提示（jpg/png/gif/webp 等）
- 大文件保护（>100KB 拒绝，提示分段读取）
- 行号格式化（自适应宽度对齐）

**GrepTool** 增强（对标 crush grep）
- 新增 `literal_text` 参数：自动转义正则特殊字符
- FileIgnoreManager 过滤

**FetchTool** 升级（对标 crush fetch/web_fetch）
- HTML 净化：移除 script/style/nav/footer/header/aside 等噪音元素
- 多输出格式：`text`（纯文本）/ `markdown`（结构化）
- JSON 自动美化
- 真实浏览器 User-Agent（Chrome 131 Windows）

### 🔧 配置变更

**默认温度 0.0 → 0.1**
- `LLM.cs` 构造函数默认值 + `Config.Temperature` 默认值 + Schema 默认值
- 回退链自动继承温度

**MaxContextTokens 默认 128K → 1M**
- 匹配 DeepSeek V4 的 1M 上下文窗口

### 🗂️ 新增 + 修改文件
| 文件 | 说明 |
|------|------|
| `Skills/SkillsManager.cs` | 完全重写：名称验证 + XML 格式 + 内置 skill + `.cursor/skills` |
| `Skills/builtin/waycoder-config/SKILL.md` | 内置 WayCoder 配置指南 skill |
| `Infra/FileIgnoreManager.cs` | 文件忽略规则管理器（.gitignore + .waycoderignore） |
| `Infra/DesktopNotifier.cs` | 桌面通知系统（终端闪烁 + 响铃 + Windows Toast） |
| `Tools/MultiEditTool.cs` | 批量编辑工具 |
| `Tools/EditFileTool.cs` | 新增 replace_all + CRLF 保留 |
| `Tools/ReadFileTool.cs` | 增强：文件建议 + 图片检测 + UTF-8 验证 + 大文件保护 |
| `Tools/GrepTool.cs` | 新增 literal_text 模式 + FileIgnoreManager |
| `Tools/FetchTool.cs` | HTML 净化 + Markdown 格式 + JSON 美化 |
| `Tools/GlobTool.cs` | FileIgnoreManager 集成 |
| `Config/Config.cs` | 新增 DesktopNotifications 配置项 |
| `Agent/SystemPrompt.cs` | Skills XML 注入格式 |

### 🧪 测试
- 1312 项自测全部通过

## v0.31.3 (2026-08-11) — 竞品对标：后台任务工具 + Download 工具 + Bash 后台增强

### ✨ 新功能

**后台任务工具**（对标 Crush 竞品分析）
- `JobOutputTool` (`job_output`) — LLM 可读取后台运行 bash 任务的输出，支持 timeout 等待
- `JobKillTool` (`job_kill`) — LLM 可终止指定的后台 bash 任务
- `BackgroundTask.Kill()` — 新增进程终止方法（`entireProcessTree: true`）

**Download 工具**
- `DownloadTool` (`download`) — HTTP GET 下载文件到本地
  - 安全检查：拒绝 `file:///`、仅允许 http/https、最大 500MB、自动创建父目录
  - 超时：默认 60s，最大 600s
  - 权限：需要确认（危险操作）

**Bash 后台运行增强**
- `BashTool` 新增参数：`run_in_background` (bool)、`auto_background_after` (int)
- 后台模式：启动 BackgroundTask → 返回 shell_id → LLM 后续可通过 job_output/job_kill 管理

### 🔧 权限分类
- `download` 加入 PermissionManager.DangerousTools 列表
- `job_output` 加入 AutoModeClassifier.SafeTools（只读操作）
- `job_kill` 加入 AutoModeClassifier.DangerousTools（破坏性操作）

### 🗂 新增 + 修改文件
| 文件 | 说明 |
|------|------|
| `Tools/JobOutputTool.cs` | 后台任务输出读取工具 |
| `Tools/JobKillTool.cs` | 后台任务终止工具 |
| `Tools/DownloadTool.cs` | HTTP 下载工具 |
| `Agent/BackgroundTask.cs` | 新增 Kill 方法 + 增强输出缓冲 |
| `Tools/BashTool.cs` | 新增后台运行参数 |
| `Tools/ToolRegistry.cs` | 工具数量 33→36 |
| `Skills/AutoModeClassifier.cs` | 新工具风险分类 |
| `Skills/PermissionManager.cs` | download 确认规则 |
| `.gitignore` | 忽略 crush/ 和 test_game/ 参考目录 |

### 🧪 测试
- 1307 项自测全部通过

## v0.31.2 (2026-08-11) — TUI 增量渲染优化

### 🐛 修复

**输入框输入时全屏闪烁**
- `TuiManager.Render()`：增量帧不再 `ClearScreen`，仅首帧/切屏/Resize 时全屏清除
- `TuiControl.MarkDirty()`：不再向 Parent 链传播脏标记，避免 RootView 脏导致所有控件重绘
- `TuiView.OnRender()`：始终遍历子视图容器以查找脏后代，但只渲染脏的叶子控件
- 效果：输入框打字时仅刷新输入区域，聊天区/状态栏等保持不变，消除闪烁

## v0.31.1 (2026-08-11) — CLI 参数注册系统 + DeepSeek V4 上下文修正

### ✨ 新功能

**📋 CLI 参数注册系统**
- 新建 `Parameters/` 目录，结构化参数定义：
  - `CliArg` 抽象基类 — 统一 Key / Names / Description / ValueCount / OnMatch 元数据
  - `CliArgRegistry` 注册表 — Register 自动重复检测（长短名冲突立即报错）、Parse 解析引擎、HelpText 自动生成
  - `BuiltinArgs` — 18 个内置参数子类，每个 5-10 行继承基类
- Program.cs Main：删除 ~50 行手动 switch，改为 15 行注册 + 解析
- `--help` 输出从注册表自动生成，排除内部/开发参数
- 支持 `--key=value` 等号格式
- 大小写敏感：`-b` (base-url) 和 `-B` (max-budget) 不冲突

**🏷 全部参数长短双名**
- 7 个参数新增强短名：`-b` (`--base-url`), `-k` (`--api-key`), `-i` (`--init`), `-d` (`--debug`), `-y` (`--yolo`), `-B` (`--max-budget-usd`)
- `--benchmark`/`--perf` 新增 `--bench` 别名
- 18 个参数全部支持短名或别名

### 🐛 修复

**DeepSeek V4 上下文窗口修正**
- `deepseek-v4-pro` / `deepseek-v4-flash` 上下文窗口：128K → 1M (1,048,576 tokens)
- 模型列表 `/model list` 显示正确：`1M ctx`

### 🔄 重构

- CLI 参数从手写 switch 重构为结构化注册表
- `BuiltinArgs.RegisterAll()` 幂等注册，重复名称自动检测报错

### 🗂️ 新增文件
| 文件 | 功能 |
|------|------|
| `Parameters/CliArg.cs` | CLI 参数抽象基类 |
| `Parameters/CliArgRegistry.cs` | 注册表 + 解析引擎 + 帮助生成 |
| `Parameters/BuiltinArgs.cs` | 18 个内置参数定义 |

### 🧪 测试
- 1306 项自测全部通过

## v0.31.0 (2026-08-11) — 统一配置系统 + 多模型管理 + 槽位独立记忆

### ✨ 新功能

**⚙ 统一配置系统**
- Config 改为单例模式 (`Config.Instance`)，全局唯一实例，支持 `Reload()`
- 新增 19 个可配置属性，均绑定环境变量 + Schema 驱动设置界面
  - 沙箱：`SandboxMaxMemoryMb`、`SandboxMaxCpuSeconds`、`SandboxAllowNetwork`
  - Agent：`MaxRounds`、`SubAgentMaxParallel`、`SubAgentOutputMaxChars`
  - LLM：`LlmHttpTimeoutSec`、`LlmMaxRetries`、`LlmConnectionTimeoutSec`、`LlmRateLimitMaxWaitSec`
  - 回退：`FallbackChain`（逗号分隔模型列表）
  - 文件锁：`FileLockTimeoutSec`
  - 上下文压缩：`ContextSnipRatio`、`ContextSummarizeRatio`、`ContextCollapseRatio`
- 9 个模块全量重构，从硬编码/分散静态字段改为 `Config.Instance` 读取
  - `SandboxManager`、`AgentTool`、`Agent`、`LLM`、`FallbackLLM`、`ContextManager`、`FileLockManager`、`BashTool`、`WatchMode`
- 双环境变量兼容：`WAYCODER_*`（新）+ `CORECODER_*`（旧）

**🤖 多模型目录与槽位独立选模型**
- `ModelCatalog` — 内置 50+ 模型，覆盖 12+ 提供商（OpenAI、Anthropic、DeepSeek、Google、Qwen、智谱、豆包、Moonshot、Mistral、xAI、Ollama 等）
- 每模型含：上下文窗口、输入/输出价格、默认 API 地址、分类标签
- 外部配置导入：OpenCode / Crush / Cline / Continue JSON 格式一键导入
- `ApiKeyStore` — 多模型 API Key 持久化（`~/.waycoder/api_keys.json`），按提供商独立存储，跨会话保留
- `AgentSlotConfig` — 10 槽位 (F1-F10) 各自独立选择大小模型
  - 统一模式：一键设置所有槽位为同一模型
  - 模型继承：未设置槽位默认使用 F1 的模型
  - API Key 三级优先：槽位直设 → ApiKeyStore → 全局 Config.ApiKey
  - BaseUrl 三级优先：槽位 → 模型默认 → 全局 Config.BaseUrl

**📟 `/model` 命令重写**
- 子命令：`list`（浏览目录）、`set`（当前槽位）、`uniform`（全槽位统一）、`import`（外部导入）、`keys`（管理 API Key）、`slot`（指定槽位）
- `#N` 快捷语法：`/model #1 deepseek-v4-pro sk-xxx` 一键配置
- 本地模型检测：`localhost:port` 自动转换为 `http://localhost:port/v1` BaseUrl，无需 API Key
- 向后兼容：`/model <id>` 快速切换大模型

**🧠 槽位独立记忆系统**
- 每个 Agent 槽位 (F1-F10) 独立记忆存储目录（`.waycoder/memory/slot_N/`）
- 共享记忆目录（`.waycoder/memory/`）所有槽位可见
- 切换槽位时记忆空间自动切换
- `StructuredMemory` 读写操作自动路由到当前槽位
- `ListAll` / `Search` / `GetRelevantContext` 合并共享 + 槽位独立记忆
- `Count` 合并计数（排除 MEMORY.md 索引文件）

### 🔄 重构

- `Config.FromEnv()` 调用点全部改为 `Config.Instance`（`BashTool` 4 处、`WatchMode` 2 处、`LintTool` 3 处、`WriteFileTool`、`EditFileTool`、`ChatScreen`、`SettingsScreen`、`TuiManager`、`SystemPrompt`）
- `SandboxManager` 静态字段改为 Config-backed getter（保留 setter 供测试）
- `AgentTool.MaxParallelTasks` 从 `const` 改为 Config 读取
- `FileLockManager.DefaultTimeout` 从硬编码改为 Config 读取
- `LLM` 超时/重试/连接超时全部从 Config 读取
- `FallbackLLM.FallbackChain` 从 Config 逗号分隔解析
- `ContextManager` 压缩比从 Config 读取
- `StructuredMemory` 目录结构重构为共享 + 槽位双层

### 🗂️ 新增文件
| 文件 | 功能 |
|------|------|
| `Config/ModelCatalog.cs` | 模型目录（50+ 内置 + 导入引擎 + 搜索） |
| `Config/ApiKeyStore.cs` | 多模型 API Key 持久化 |
| `Config/AgentSlotConfig.cs` | 10 槽位独立模型配置 + 统一模式 |

### 🧪 测试
- 1306 项自测全部通过

## v0.30.2 (2026-08-11) — Bash 流式输出增强 + 上限报告同步

### ✨ 新功能

**📟 Bash stderr 流式输出**
- 原仅 stdout 逐行流式，stderr 全缓冲到退出后一次性追加
- 现 stdout/stderr 并行异步逐行读取，stderr 行带 `[stderr]` 前缀
- UI 中通过 `IsErrorOutput` 自动标红错误行
- 管道模式（非 TUI）也新增 `onToolOutput` 回调，逐行输出到控制台

**📊 上限报告同步更新 (55→60 项)**
- 新增 5 项：TuiTextArea 最大行数、自动换行列宽、TuiEditBase 撤销栈、Tab 键行为标志、Bash 流式 stderr

## v0.30.1 (2026-08-11) — TuiEditBase 键盘引擎 + TuiTextArea 自动换行

### ✨ 新功能

**⌨ TuiTextArea 自动换行与行数限制**
- `MaxColumnWidth` 属性：文字自动折行宽度（按空格智能断词），可视区 `Width` 可小于此值实现水平滚动
- `MaxLines` 属性：最大行数限制，超出时从顶部自动裁剪旧行，同步调整光标和滚动偏移

### 🔄 重构

**🏗 TuiEditBase 统一键盘分发引擎**
- 基类新增 18 个抽象编辑原语（光标移动、文本编辑、选择管理、撤销重做、粘贴）
- 基类实现完整键盘分发：`OnKey` → `HandleCtrlKey` / `HandleShiftKey` / `HandleRegularKey`
- 子类只需实现数据模型相关的底层原语（每方法 1-5 行），不再需要重复编写键盘处理
- TuiInput：删除 ~160 行 OnKey，新增 12 个原语实现
- TuiTextArea：删除 ~175 行 OnKey + HandleCtrlKey，新增 18 个原语实现 + 多行光标移动
- TuiRichEditor：保留独立的 OnKey 覆写（委托 EditorCore），新增糖衣原语适配基类
- 消除 ~335 行重复代码

**🔀 按键冲突消除**
- `AcceptsTab` 虚属性：默认 `false`（Tab 切换焦点），TuiRichEditor 覆写为 `true`（Tab 输入缩进）
- `InsertNewLine()` 虚方法：默认提交（OnSubmit），TuiTextArea/TuiRichEditor 覆写为实际换行
- 基类 Tab 分发：`AcceptsTab ? InsertChar('\t') : return false`（交父容器切换焦点）

**🧪 自测**
- 1306 项自测全部通过（新增 6 项：MaxColumnWidth 折行、MaxLines 裁剪、Tab/Enter 按键差异）

### ✨ 新功能

**📊 系统上限报告 (`--limits`)**
- 扫描全代码库 55 项系统上限，分为 6 大类别：
  - 🤖 智能体（8 项）— 槽位数量、子Agent深度、并行上限、轮次/预算限制
  - 📨 上下文（8 项）— 三层压缩阈值、token 估算公式、会话消息列表
  - 📁 工具/文件（14 项）— Bash 截断、危险命令阻止、超时、各工具结果上限
  - 🔒 沙箱/资源（6 项）— 内存、CPU、网络、隔离深度
  - 🖥 TUI/编辑器（9 项）— 聊天消息、撤销历史、diff 预览、代码块宽度
  - ⚙ 配置/杂项（10 项）— 记忆注入、会话列表、commit 限制
- 每项标注：🔴硬阻断 / 🟡降级 / 🟢优雅 / ⚪无限制
- **⚙可配 vs 🔒硬编 区分**：标注哪些上限可在设置界面修改（8 项可配 + 环境变量名 + 设置路径）
- 输出包含源码位置（文件名:行号），方便代码审查和性能调优
- 发现 2 个潜在问题：`AgentTool.MaxDepth` 与 `Config.SubAgentMaxDepth` 不同步、沙箱 CPU 限制未实施

**💬 聊天显示风格设置**
- 三种显示模式：`detailed`（全显示）、`auto`（智能折叠 20 行）、`concise`（极简一行）
- 环境变量 `WAYCODER_CHAT_STYLE`，设置界面 → 🎨 界面 → 聊天显示风格
- 实时生效，保存设置后同步更新

### 🔄 重构 / 改进

**📝 Markup 标记符号重构**
- 清除 Spectre.Console 方括号 `[color]text[/]` 与中文 `[]` 的冲突
- 改用法语书名号 `«color»text«/»` 作为标记符号（U+00AB/00BB）
- 中文方括号不再需要双写 `[[选项]]`，直接写 `[选项]` 即可
- 涉及：`SpectreToAnsi` 解析器、37 处 `MarkupLine` 调用、`TuiHelper.Esc/StripMarkup`
- 同步更新 `CheckpointManager`、`FindReplaceTool`、`SelfTest`

**📊 性能测评新增 10K 聊天压力测试**
- 10000 条混合角色聊天消息的创建、渲染、滚动性能
- ChatMsg 创建 9ms、TuiListItem 解析 74ms、布局 258ms、滚动 <1ms

### 🐛 修复
- `AgentTool.MaxDepth` 与 `Config.SubAgentMaxDepth` 不同步问题已记录（上限报告中）

## v0.26.0 (2026-08-11) — 智能工作模式 + 跨槽位协作 + 光标修复

### ✨ 新功能

**🤖 智能 Auto Mode 分类器**
- 三级风险分级引擎：Safe（只读自动放行）→ Cautious（首次确认后记住）→ Dangerous（每次确认）
- 连续 3 次拒绝危险操作 → 自动退回 Ask 手动模式，防止误操作疲劳
- `/auto` 一键切换，别名 `/自动`

**🔨 工作模式系统（Shift+Tab 切换）**
- 四种模式：🔨Build 建造 / 🧠Plan 计划 / 🔍Review 审查 / 🤖Auto 自动
- 每个 Agent 槽位独立记忆工作模式
- **Plan 模式**：封锁 write/edit/bash/rm/git/agent，System Prompt 注入分析引导
- **Review 模式**：只读 + agent 可用，封锁写工具
- **Auto 模式**：全工具 + SmartAuto 分级确认
- 快捷键 **Shift+Tab** 循环切换，`/mode` 命令查看/直达
- 状态栏显示当前模式 emoji

**📦 自动 Git Commit 增强**
- `/autocommit` 开关命令，别名 `/自动提交`
- 精准暂存：只 `git add` AI 实际修改的文件（通过 `write_file`/`edit_file` 追踪）
- 提交正文含 `git diff --stat` 摘要
- 用户可见反馈：提交后在聊天区显示 `📦 自动提交 [N 文件]: msg`

**📨 跨槽位消息传递**
- `/send <槽位号> <消息>` —— 向其他 Agent 槽位发送消息
- `/broadcast <消息>` —— 向所有其他槽位广播
- 非活跃槽位的消息自动排队，切换回该槽位时投递
- 别名：`/发送` `/广播` `/to` `/bc`

**🧠 Architect 双模型模式**
- `/architect` 开关，大模型出计划 + 小模型执行
- 大模型不带工具，纯分析输出结构化计划

**📥 配置导入**
- `/import` 一键导入 Claude Code / OpenCode / Cursor / Cline 配置
- 支持：模型/API 配置、MCP 服务器、项目上下文、会话数据

**🗺️ Repository Map 升级**
- 新增 16 语言 import/include 引用图分析
- PageRank 风格核心文件评分 + ⭐ 标记

**💬 AskUserQuestion 用户交互工具**
- LLM 可主动弹窗向用户提问：单选（多选一）、多选（多选多）、文本输入
- 每次可问 1-4 个问题，依次模态弹窗，阻塞 Agent 等待响应
- 支持选项描述文本（如 `"React — Popular UI library"`）
- 非 TUI 模式自动回退到 Console I/O
- 分类为 Safe 工具，SmartAuto 下自动放行无需确认
- `UxHelper.RenderWait` 重构为公开方法，timeout 可配置，供工具层复用

### 🖥️ TUI 改进
- **状态栏心跳动画**：⣾⣽⣻⢿⡿⣟⣯⣷ braille 旋转，证明 UI 渲染循环存活
- **光标定位修复**：`EnsureCursorPosition()` 后备机制，增量渲染模式下光标位置不丢失
- **Shift+Tab 终端序列**：`\x1b[Z` → `InputType.ShiftTab`，模式切换
- **ProgramContext.Agent 同步**：槽位切换时自动更新，所有命令可用

### 🔄 重构
- `PermissionManager` 重构：提取 `ShowConfirmDialog()`，新增 `SmartAuto` 模式
- `SandboxManager` 新增 `smart-auto` 级别映射 + `IsSmartAuto` 属性
- `BackgroundTask` 完全异步化：`WaitForExit` → `WaitForExitAsync`，消除同步阻塞
- `AgentSlot` 新增：`WorkMode`、`PendingMessages`、`DeliverMessage()`、`FlushPendingMessages()`

### 🧪 测试
- 新增 95 项测试（总计 1299 项，从 1199 增长），全部通过
- 覆盖：AutoMode 分类器、工作模式约束/切换/事件、AutoCommit 属性/校验/清洗/EscArg、跨槽位投递/排队、光标状态、AskUserQuestion 工具 Schema/注册/安全分类

### 🗂️ 新增文件
| 文件 | 功能 |
|------|------|
| `Skills/AutoModeClassifier.cs` | 三级风险分类引擎 |
| `Agent/WorkModeManager.cs` | 四模式定义 + 约束 + Prompt |
| `Commands/AutoCommand.cs` | `/auto` 切换命令 |
| `Commands/ModeCommand.cs` | `/mode` 模式命令 |
| `Commands/AutoCommitCommand.cs` | `/autocommit` 开关 |
| `Commands/SendCommand.cs` | `/send` + `/broadcast` |
| `Commands/ArchitectCommand.cs` | `/architect` 双模型 |
| `Commands/ImportCommand.cs` | `/import` 导入 |
| `Infra/ImportHelper.cs` | 四源导入引擎 (~750行) |
| `Tools/AskUserQuestionTool.cs` | LLM 用户交互工具 (~360行) |

## v0.25.9 (2026-08-10) — TUI 控件完善 + 测试全覆盖 + Bug 修复

### ✨ 新功能
- **TuiDialog.Secret()**: 新增密码输入对话框，支持掩码回显
- **UxHelper.Secret()**: 新增密码输入辅助方法，TUI/控制台双模式适配

### 🧪 测试
- **29 个 TUI 组件测试全覆盖**: TuiButton, TuiCheckbox, TuiInput, TuiTextArea, TuiLabel, TuiIcon, TuiList, TuiListView, TuiProgress, TuiSpinner, TuiStatusBar, TuiTabs, TuiTitleBar, TuiBanner, TuiGrid, TuiWrapPanel, TuiSidePanel, TuiPromptBar, TuiDialog (11 工厂方法), TuiControl, TuiView, TuiScreen, BoxBuffer, TuiColors, TuiTheme, MarkdownRenderer, TuiTable, DiffPreview, UxHelper
- 新增 362 项测试（总计 1199 项），全部通过
- `/test ui` 可一键运行所有 UI 组件测试

### 🐛 Bug 修复
- **侧边栏背景色修复**: TuiSidePanel 默认背景从 WindowBg 改为 TerminalBg，与聊天区黑色背景统一
- **开机 Logo 间距**: 上方增加 3 行空白，视觉居中对齐
- **彩虹色偏移修复**: 横幅彩虹渐变基于视觉行号（非空行）而非绝对行索引，避免空白行消耗颜色锚点

### 🔄 重构
- Program.cs 中 `TuiInput.ReadInput()` → `TuiChatInput.ReadInput()`，统一输入入口

## v0.25.8 (2026-08-10) — 去 CoreCoder 品牌化

### 🔄 品牌重命名
为避免商标侵权，代码库中彻底移除 "CoreCoder" 品牌名称：

- **命名空间重命名**: `CoreCoderSharp` → `WayCoder`（~177 个 .cs 文件 + 项目文件 + CI）
  - `.csproj` `<RootNamespace>` / `<AssemblyName>` → `WayCoder` / `waycoder`
  - `.sln` 项目名和路径同步更新
  - CI workflows 路径更新
- **配置目录迁移**: `.corecoder/` → `.waycoder/`（~16 个文件）
  - 写操作一律用 `.waycoder/`，读操作先试新目录回退旧目录
  - `Global.cs` 新增集中式辅助方法: `WriteConfigPath` / `ReadConfigPath` / `GlobalConfigPath` / `ConfigDirSearchOrder`
  - 环境变量: 新增 `WAYCODER_*` 前缀，保留 `CORECODER_*` 兼容回退
  - 涉及文件: StructuredMemory, SessionManager, CheckpointManager, CustomCommands, ThemeConfig, Config, Program, WatchMode, SharedMemoryManager, ProjectContext, HooksManager, McpCache, McpClient, ExportCommand, MemoryTool, SkillsManager, SelfTest
- **注释与文档去 CoreCoder 化**:
  - 代码注释: Agent.cs, ContextManager.cs, SessionManager.cs, CheckpointManager.cs
  - 文档: CLAUDE.md, README.md, AGENTS.md, .gitignore
  - 二进制名: `corecoder.exe` → `waycoder.exe`（输出文件）

### ⚠️ 破坏性变更
- 新配置写入 `.waycoder/`，首次启动自动创建新目录
- 旧 `.corecoder/` 目录可安全保留或删除（读操作仍兼容）

### 🧪 测试
- 844 项自测全部通过，0 失败
- 编译 0 错误（仅 23 个预存在 nullability/AOT 警告）

## v0.25.7 (2026-08-10) — TuiBase 统一基类 + Bug 修复

### 🏗️ 架构重构
- **TuiBase.cs** (NEW): 统一 UI 元素基类，提炼所有界面通用属性和方法
  - 公共属性: `X`, `Y`, `Width`(默认10), `Height`(默认1), `Name`, `Tag`, `IsDirty`
  - 脏标记管线: `MarkDirty()`(virtual), `ClearDirty()`, `Invalidate()`
  - 生命周期: `OnCreate()`, `OnDestroy()` (virtual)
  - 输入路由: `OnKey(ConsoleKeyInfo)`(virtual→bool), `HandleMouse(InputEvent)`(virtual→bool)
  - 尺寸变化: `OnResize(int newW, int newH)`(virtual)
- **TuiControl** → 继承 TuiBase，移除重复属性，virtual → override
  - 保留 `MarkDirty()` Parent 传播逻辑
  - 保留 KeyHook 模式 + Render 管线 + 颜色系统
- **TuiWindow** → 继承 TuiBase，构造函数默认 30×10，移除重复 X/Y/W/H
- **TuiScreen** → 继承 TuiBase
  - Namespace 统一: `CoreCoderSharp.UI.TuiBase` → `CoreCoderSharp.UI`
  - `MarkDirty()` override 增强: 同步标记 Manager.IsDirty + RootView.IsDirty
- **TuiView** → 无需改动，继承链自动传递 (TuiView→TuiControl→TuiBase)
- 5 个调用方移除旧 `using CoreCoderSharp.UI.TuiBase;` 导入

### 🐛 Bug 修复
- **Bug #1**: `/plan` 命令 — PlanModeAsync 加 try-finally 包裹 Enter/Exit 备用屏
- **Bug #2**: `!` shell 命令 — RunShellOnceAsync 加 try-finally 包裹 Enter/Exit 备用屏
- **Bug #8**: Agent.AutoCommitAsync 空 `catch { }` → `catch (Exception ex) { DebugLog.Log(...) }`
- **Bug #9**: Terminal.ExitAltScreenDirect 缺少 `MouseDisable` → 退出前先禁用鼠标

### 🧪 测试
- 844 项自测全部通过，0 失败
- 编译 0 错误（仅 23 个预存在 nullability/AOT 警告）

## v0.25.6 (2026-08-10) — NotebookEdit 工具 + 工具总数 32

### 🚀 P3 新功能
- **NotebookEditTool.cs** (NEW): Jupyter Notebook (.ipynb) 编辑工具
  - `replace`: 替换指定 cell 的源代码（自动清理旧 outputs）
  - `insert`: 在指定位置后插入新 cell（支持 code/markdown/raw 类型）
  - `delete`: 删除指定 cell
  - 基于 cell 索引（0-based），兼容 .ipynb v4 格式
  - AOT 兼容: 手写 JSON 节点操作，零反射依赖
- **ToolRegistry**: 工具总数 31 → 32
- **PermissionManager**: notebook_edit 加入写工具确认列表
- **SelfTest**: 9 项 NotebookEdit 测试（replace/insert/delete/边界）

## v0.25.5 (2026-08-10) — 团队知识库共享

### 🚀 P3 新功能
- **SharedMemoryManager.cs** (NEW): 通过 git 同步 `.corecoder/memory/` 共享记忆
  - `IsGitRepo()`: 检测当前目录是否在 git 仓库中
  - `GetStatus()`: 获取本地共享记忆数 + 远程变更数 + 变更文件列表
  - `PullSharedAsync()`: 从远程拉取共享记忆（fetch + checkout 仅 memory 文件）
  - `PushSharedAsync()`: 推送本地共享记忆到远程（add + commit + push）
  - `ShareAsync()`: 标记记忆为共享 + 推送到远程
  - `Unshare()`: 取消记忆共享状态
  - 安全措施: 只操作 .corecoder/memory/*.md，不触碰其他 git 内容
- **StructuredMemory.cs**: 新增 `IsShared` / `SetShared()` / `ListShared()`
  - `shared: true` frontmatter 字段持久化
- **MemoryTool.cs**: 新增 `share` / `unshare` / `sync` 三个操作
  - `share`: 标记团队共享并推送
  - `unshare`: 取消共享
  - `sync`: 拉取（默认）或推送（sync push）远程共享记忆
- **Config.cs**: 新增 `TeamMemoryEnabled` + `TeamMemoryAutoSync` 配置项
  - 环境变量: `WAYCODER_TEAM_MEMORY` / `WAYCODER_TEAM_AUTO_SYNC`
- **Program.cs**: 启动时自动拉取远程共享记忆（需开启 TeamMemoryEnabled + TeamMemoryAutoSync）

## v0.25.4 (2026-08-10) — 核心 API 文档补全

### 📝 P2 改进
- **LLM.cs**: 8 个公开属性 + 2 个核心方法添加 XML 文档（`<param>`/`<returns>`）
- **Agent.cs**: 5 个公开属性 + 构造函数 + `ChatAsync` + `Reset` 添加 XML 文档

## v0.25.3 (2026-08-10) — 工具错误信息改进

### 🔧 P2 改进
- **26 个工具 catch 块添加异常类型**: `ex.Message` → `ex.GetType().Name: ex.Message`
  - 覆盖 Tools/ 下全部 26 处错误返回点
  - 区分 NullReferenceException / IOException / UnauthorizedAccessException 等

## v0.25.2 (2026-08-10) — 错误处理加固

### 🔧 P2 改进
- **空 catch 块添加日志**: 10+ 个静默异常捕获点增加 `DebugLog.Log()` 调用
  - `ProjectContext.cs` 6 处（指令文件/package.json/csproj/go.mod/Git/SafeGetFiles）
  - `SandboxManager.cs` 2 处（路径解析 + OperationCanceledException）
  - `RepoMapGenerator.cs` 3 处（.gitignore/目录扫描/LSP 符号提取）
  - `CheckpointManager.cs` 3 处（Git stash/Git status/变更文件）

## v0.25.1 (2026-08-10) — 代码质量修复

### 🐛 Bug 修复
- **Esc 转义方括号**: `TuiHelper.Esc` 中 `]` 被错误转义为 `[[]` 而非 `]]`，修复后 830/830 自测全过

### 🔧 P1 改进
- **SettingSchema 补全**: 新增 `DiffPreview` 和 `EmbeddingDimensions` 设置界面入口
- **PlanMode 接入**: `/plan` 命令升级使用 `PlanMode.GetPlanSystemPrompt()`（含项目上下文 + 仓库地图 + 两阶段确认）
- **MemoryStore 标记废弃**: 添加 `[Obsolete]` 属性，指向 `StructuredMemory`

## v0.25.0 (2026-08-10) — 语义记忆（P1 补全）

### 🔥 P1 语义记忆

**TF-IDF 语义搜索替换关键词匹配**
- `StructuredMemory.Search()` 和 `GetRelevantContext()` 升级为 CJK bigram + TF-IDF 评分
- `SemanticMemory` 新增 `SearchEntries(List<MemoryEntry>)` 重载，支持新结构化格式
- 旧式 `SearchRelevant(MemoryDocument)` 保持不变，向后兼容
- TF-IDF 无结果时自动兜底到原始子串匹配

**可选向量嵌入 (Embedding API)**
- 新增 `EmbeddingStore` 静态类：`.vec` 二进制向量文件 I/O + 余弦相似度 + 混合搜索
- `LLM.GetEmbeddingAsync()` 调用 `/v1/embeddings` 端点生成向量
- Hybrid 搜索：embedding 余弦相似度 ×0.7 + TF-IDF ×0.3
- 懒加载向量生成：搜索时 fire-and-forget，最多 3 并发
- 原子写入（临时文件 + 重命名）防并发冲突

**配置**
- `WAYCODER_EMBEDDING` 开关（默认 false，需 API 支持 `/v1/embeddings`）
- `WAYCODER_EMBEDDING_MODEL` 模型名（默认 `text-embedding-3-small`）
- `WAYCODER_EMBEDDING_DIMS` 维度（0=模型默认）

### 🔧 修复

- `SemanticMemory.SearchRelevant()` 时间新鲜度加权不再错误应用到零匹配文档

## v0.24.2 (2026-08-10) — 渐变增强 + 增量渲染

### 🔥 新增

**标题栏/状态栏金色渐变**
- `TuiTheme` 新增 `GradTitleBar`：暖金 `#FFD700` → 琥珀 `#FF8C00`
- `ControlRenderer` 新增 `DrawGradientBarFill` + `WriteGradientTextAt` 渐变条绘制
- `TuiTitleBar` / `TuiStatusBar` 整行金色渐变背景，黑字金底

**按钮焦点渐变差异**
- `AnsiTty` 新增 `DarkenRgb` 向黑色调暗
- `DrawButtonGradientLine` 非焦点时暗化渐变 55%，焦点保持完整亮色
- 焦点/非焦点视觉差异明显，Tab 切换即时响应

### 🔧 变更

**增量渲染（消除焦点切换闪烁）**
- `TuiView.FocusNext/FocusPrev` 标记丢失/获得焦点控件为脏
- `TuiManager` 条件全刷新：首帧/切屏/Resize → ClearScreen，增量时跳过
- `TuiScreen` 增量窗口渲染 `RenderWindowDirtyControls`：仅渲染脏控件，跳过背景/边框/遮罩
- `TuiWindow` 拖拽/缩放后 `RootView.MarkDirty()` 确保全量重绘

**边框背景修复**
- 竖边框/底角背景从 `bg=边框色` 改为 `bg=窗口底色`，不影响其他窗口外观
- Toast 文字背景跟随窗口底色，不再显示黑色

**CJK 修复**
- `WriteGradientTextAt` 改为 Rune 迭代 + `RuneWidth` 计算列偏移，修复汉字丢失

---

## v0.24.1 (2026-08-10) — 真彩渐变边框 + 按钮美化

### 🔥 新增

**真彩渐变边框**
- 对话框上下横边支持 24-bit TrueColor 渐变（青→蓝 / 绿→青 / 橙→黄 / 红→橙）
- 竖边框：左=起始色、右=终止色，纯色不断线
- `WriteGradientHLine` 逐字 Lerp 插值渲染
- `TuiTheme` 5 组渐变预设（`GradCyanBlue` / `GradGreenCyan` / `GradOrangeYellow` / `GradRedOrange` / `GradPurplePink`）

**按钮渐变背景**
- `DrawButtonGradientLine` 单次定位 + 逐字换背景色 + 末尾统一重置
- 按钮渐变独立于边框（比边框亮 30%），`TuiTheme` 4 组 `Btn*` 预设
- `TuiButton` 新增 `GradientBg` / `GradientBgStart` / `GradientBgEnd` 属性

**对话框美化**
- 边框默认改为 `Solid`（▀▄█ 半高块），粗线更显眼
- 四角改用全块 `█` 字符，防断线
- 竖边框 + 底角 bg=fg 防行间间隙
- 标题居中 + 渐变色（取 50% 位置）
- 内容文字 `TextAlign = HAlign.Center` 居中
- 按钮等宽居中（`NormalizeButtons` 统一取最宽者）
- 按钮 HBox 左右各留 1 字符间距，不贴边框

### 🔧 变更

- `AnsiTty` 新增 `RgbCode` / `DecodeRgb` / `LerpRgb` / `LightenRgb` TrueColor 工具
- `BorderChars` 支持 `HTop` / `HBottom` 独立字符
- `TuiWindow` 新增 `GradientBorder` / `GradientStart` / `GradientEnd` 属性
- `TuiScreen.RenderWindow` 渐变渲染逻辑重构

---

## v0.24.0 (2026-08-10) — 侧栏面板 + 按键架构简化

### 🔥 新增

**侧栏面板** (`UI/TuiControls/TuiSidePanel.cs`)
- 多分区同时显示：品牌、Todo、文件、MCP、LSP，无需标签切换
- `PanelSection` 数据模型：Title + Lines + Collapsed
- 左边框竖线分隔，每分区标题 + ─ 分隔线 + 内容行
- Ctrl+B 一键切换显示/隐藏

**标题栏 + 状态栏** (`UI/TuiControls/TuiTitleBar.cs`, `TuiStatusBar.cs`)
- 顶部 TitleBar：应用名 + Git 分支 + 版本号右对齐
- 底部 StatusBar：F1-F10 槽位指示（颜色区分状态）+ 提示文本 + Token
- 活跃槽位白底黑字，工作中绿色，等待权限黄色，出错红色

**输入区上下分隔线**
- InputTopBorder / InputBotBorder（━ 分隔线）包裹输入区
- BuildLayout 嵌套布局：VBox(外层) + HBox(ChatList+SidePanel 中间层)

### 🔧 变更

- **按键架构简化**：`HandleKey` → `OnKey` / `OnOwnKey` 统一模型
  - TuiControl 基类新增 `OnOwnKey`（控件自身按键），`OnKey` 负责路由
  - TuiView.OnKey 简化为：自己先处理 → 单焦点子节点路由
  - TuiWindow.HandleKey → OnKey，TuiScreen.HandleKey → OnKey + OnOwnKey
  - ChatScreen 6 个子方法合并到 OnOwnKey
  - 14 个控件 HandleKey override → OnOwnKey override
  - TuiPanel / TuiSeparator 删除 pass-through HandleKey override
- **LspTool**：新增 `SupportedServers` 公共属性供侧栏展示
- **AgentSlot**：`PanelTab ActivePanel` → `bool SidePanelVisible`
- **RenderBuffer.Write** 增强：精确 SGR 重置（SgrResetFg/SgrResetBg），避免冲掉底色
- **TitleBar/StatusBar 底色修复**：全部改用 `RenderBuffer.Write(fg, bg)` 传前景+背景色

### ❌ 移除

- `PanelTab` 枚举和标签切换系统
- 旧的 `TuiBanner.cs`、`TuiProgress.cs`（已迁移到 TuiControls 目录）
- `TuiView.OnSelfKey`（被 OnOwnKey 替代）

## v0.23.0 (2026-08-09) — TUI 控件系统增强

### 🔥 新增

**EdgeInsets 布局属性** (`UI/TuiControl.cs`)
- 新增 `EdgeInsets` 结构体（Top/Right/Bottom/Left），`Horizontal`/`Vertical` 快捷属性
- `TuiControl` 新增 `Margin` 和 `Padding` 属性（默认 0,0,0,0）
- `Padding` 自动内移渲染裁剪区 + 偏移 OnRender 原点
- VBox/HBox 布局自动计算 Margin 偏移

**Continuation 续接消息** (`UI/TuiControls/TuiListItem.cs`)
- `Continuation` 属性：同角色连续消息跳过头部（Icon + RoleLabel + TimeLabel）
- `IsPlainText` 模式：逐行渲染不走 Markdown 解析，避免系统消息行合并
- 正文 `Padding.Left = 2` 对齐标题文本

**OnClick sender 模式** (`UI/TuiControls/TuiButton.cs`)
- `OnClick` 从 `Action?` 改为 `Action<TuiButton>?`，按钮点击时传入自身引用
- 所有对话框按钮回调适配

### 🔧 变更

- **F1-F10 切换修复**：`Program.cs` 增加 `SwitchAgentSlot` 调用
- **角色图标**：所有图标从 emoji 改为 ● 纯色圆点（User=绿, Assistant=青, System=黄, Tool=灰）
- **聊天滚动**：`TuiListView` 新增 `ContentHeight` 属性，自动滚底 + PgUp/PgDn 手动滚动
- **IsAutoScrollToEnd**：`TuiScrollView` 和 `TuiListView` 重命名 `AutoScroll` → `IsAutoScrollToEnd`，旧属性标记 `[Obsolete]`
- **消息间距**：`ItemSpacing = 1`，所有消息之间自动空行

## v0.22.0 (2026-08-09) — Editor/Settings 迁移到新 TUI 架构

### 🔥 新增

**EditorCore 纯数据模型** (`Edit/EditorCore.cs`, ~280 行)
- 从旧 Editor.cs 提取纯数据层：文本缓冲区、光标、滚动、撤销栈、剪贴板、语法、诊断
- 零渲染依赖、零键盘依赖、零 TUI 依赖，可独立单测
- `InsertText/Backspace/Delete/NewLine/InsertTab` — 编辑操作
- `MoveCursor/MoveHome/MoveEnd/MovePageUp/MovePageDown/JumpToLine` — 光标导航
- `CopyLine/CutLine/PasteClipboard/DeleteLine` — 剪贴板操作
- `Undo` 完整撤销栈，`Save/SaveAsync` 文件持久化 + 异步 Lint 诊断触发
- `GetDiagnosticsAtLine/GetDiagSummary` — 诊断查询（委托 DiagnosticManager）

**TuiRichEditor 富文本编辑控件** (`UI/TuiControls/TuiRichEditor.cs`, ~277 行)
- 增强版源码编辑控件，TuiControl 子类，绑定 EditorCore 数据模型
- 行号列（右对齐 4 位）+ Gutter 诊断指示符（● 错误 / ▲ 警告 / · 无诊断）
- 语法高亮内容渲染（通过 Syntax.Tokenize），CJK 宽度感知截断
- 光标行整行高亮，诊断行背景色覆盖（错误红 41 / 警告黄 103）
- 完整键盘：↑↓←→ Home End PgUp PgDn / Backspace Delete Enter Tab
- Ctrl+Z/X/C/V/Y/G/S 组合键 → 事件回调（OnSaveRequested/OnJumpRequested/OnExitRequested）
- 自动滚动确保光标可见

**EditorScreen 编辑器屏幕** (`UI/TuiScreens/EditorScreen.cs`, ~275 行)
- 完整 TuiScreen 实现：TitleBar + TuiRichEditor + StatusBar1 + StatusBar2
- 全局键盘：Ctrl+S 保存 / Ctrl+G 跳行 / Escape 退出（脏文件三选一确认）
- 文件选择对话框：无 FilePath 时弹出 TuiDialog.Select 选择最近文件（最多 9 个）
- 动态状态栏：光标位置、行/字符统计、文件大小、语言、诊断摘要
- 保存后异步触发 Lint 诊断

**SettingsScreen 设置屏幕重写** (`UI/TuiScreens/SettingsScreen.cs`, ~360 行)
- 从旧 SettingsPage.Show() 重写为纯 TuiScreen + 控件树
- 左侧 TuiList 类别切换 + 右侧 VBox 设置项详情（TuiLabel + TuiDialog）
- select 类型 → TuiDialog.Select 下拉选择 / text/number/secret → TuiDialog.Input 输入
- Ctrl+S 保存写入 .env + Toast 通知 / Escape 退出 / ↑↓←→ Tab 导航
- 从 Config.SettingSchema() 自动生成布局，配置读写沿用原有 switch 逻辑

### 🔧 变更

- **Program.cs**: `/edit` `/edit <path>` `/settings` `/config` `Ctrl+O` → `TuiManager.PushScreen`
- **EditorCore Undo**: 修复 NewLine 撤销时删除错误行的问题（原来从末尾删除，改为从 Line+1 删除）
- **SettingsScreen.cs**: 移除旧的 EditorScreen 桩类（现在独立为 EditorScreen.cs）

### 📊 测试

- 自测：756 → 819 项（+63 项）

---

## v0.21.0 (2026-08-09) — 新增 6 个 TUI 控件

### 🔥 新增

**TuiTreeView 树形视图** (`UI/TuiControls/TuiTreeView.cs`, ~390 行)
- `TuiTreeNode` 树节点：Text/Icon/Children/Tag/Parent/IsLeaf/IsExpanded
- 递归构建可见节点列表（expand/collapse），≥200 个节点无障碍
- 树线渲染：`├─` `└─` `│` 缩进线 + `▼` `▶` 展开指示符
- 键盘全导航：↑↓ 移动焦点，←→ 展开/折叠/跳转父节点，Space 切换，Enter 激活
- 滚动：自动滚动使选中节点可见，`_scrollOffset` 偏移管理
- `ExpandToRoot()` 展开所有祖先使深层节点可见

**TuiRadioGroup 单选按钮组** (`UI/TuiControls/TuiRadioGroup.cs`, ~96 行)
- 互斥选项列表，`◉`/`○` 符号渲染选中/未选中
- 键盘：↑↓ Home End 导航，Enter/Spacebar 确认
- `OnSelectionChanged` 回调，自动装填选项数作为高度

**TuiComboBox 组合框** (`UI/TuiControls/TuiComboBox.cs`, ~177 行)
- 收起时显示选中项 + `▼`，展开时弹出下拉列表（最多 10 项可见）
- 键盘：Enter/Spacebar 展开，↑↓ Home End 在列表内导航，Enter 确认选择，Esc 收起
- `OnExpandedChanged` + `OnSelectionChanged` 双向回调
- RenderBuffer 背景填充，占位文本支持

**TuiSeekBar 滑块** (`UI/TuiControls/TuiSeekBar.cs`, ~163 行)
- `━●──` 风格滑块，比例计算滑块位置
- Value/MinValue/MaxValue/Step/LargeStep 可配置
- 键盘：←→ 步进微调，Home/End 跳边界，PgUp/PgDn 大步跳
- 可选数字标签 "50/100"，自定义字符（Thumb/TrackFilled/TrackEmpty）
- `OnValueChanged` 回调，值自动钳制

**TuiSeparator 分割线** (`UI/TuiControls/TuiSeparator.cs`, ~71 行)
- 水平/垂直两种方向，可选居中文本（`─── 标题 ───`）
- 自定义线字符和颜色

**TuiPanel 面板** (`UI/TuiControls/TuiPanel.cs`, ~139 行)
- 带边框 + 标题栏的嵌入式容器（TuiView 子类可选 12 种边框风格）
- 标题栏 `┤` 分隔线，内容区域 Padding，递归布局子控件

### ✨ 改进
- **TuiTreeView**: `MoveUp`/`MoveDown` 内部调用 `BuildFlatList()` 确保数据同步
- **TuiSeekBar**: 添加 `using CoreCoderSharp.Terminal` 修复 RenderBuffer 引用
- **TuiDemo**: F10=树形视图 F11=控件合集 F12=面板布局

### 📝 自测: 756/756

## v0.20.0 (2026-08-09) — 五层 TUI 架构全面接入 REPL

### 🔥 重大变更

**五层 TUI 架构** (`UI/TuiManager.cs` → `TuiScreen` → `TuiWindow` → `TuiView` → `TuiControl`)
- 树状场景图替代旧扁平 ScreenManager，每层职责清晰：Manager 管屏幕栈、Screen 管布局、Window 管浮层、View 管排版、Control 管交互
- 递归布局引擎（`VBox.Layout` / `HBox.Layout` 递归嵌套视图，精确 fillBg 背景色填充）
- 统一的 `MarkDirty` 脏标记 + 按需重绘，`HandleKey` 键盘事件沿树冒泡

**TUI 控件库** (`UI/TuiControls/`)
- `TuiButton` / `TuiInput` / `TuiTextArea` / `TuiLabel` / `TuiList` / `TuiListView` / `TuiMarkdown` / `TuiDialog` / `TuiGrid` / `TuiProgress` / `TuiSpinner` / `TuiBanner` / `TuiCheckbox` / `TuiTabs` / `TuiIcon`
- `TuiDialog` 静态工厂：`Info/Warn/Error/Success/Confirm/Confirm3/Select/MultiSelect/Permission/Input` 基于回调的 API，`ManualResetEventSlim` 阻塞包装器适配 REPL 同步流程
- `TuiListView` 可滚动列表 + 键盘/鼠标导航，`TuiMarkdown` 完整 Markdown 渲染，`TuiProgress` 进度条

**ChatScreen** (`UI/TuiScreens/ChatScreen.cs`)
- 完整 REPL 屏：欢迎横幅 + 聊天列表（TuiListView → TuiListItem → TuiMarkdown）+ 多行输入区（TuiTextArea）+ 状态栏（槽位/Token/Git）+ 建议面板
- 流式输出：`StartAgentMsg()` → `AppendToken()` → `FinishAgentMsg()` 实时追加
- 对话框包装器：`ShowInlinePermission()` / `ShowMenu()` / `RenderWait` 阻塞等待 + 渲染循环

**对话框增强**
- **键盘快捷键注册**：`TuiWindow.KeyShortcuts` 字典 + `RegisterShortcut(ConsoleKey, Action)` + `HandleKey` 优先快捷键拦截
- **对话框返回值**：`TuiWindow.Result` 属性，默认 -1（未选择），选择 ≥ 0
- 所有工厂方法（Permission/Confirm/Select/Input 等）自动注册 Y/N/A/Esc/Enter 快捷键并设置 Result
- **TuiScreen Esc 路由修复**：Esc 先路由到模态窗口快捷键（取消回调），未处理才关闭，防止 RenderWait 死锁

**TuiMenu 弹出菜单** (`UI/TuiControls/TuiMenu.cs`)
- 可滚动弹出菜单控件（~240 行），支持标题栏 + 窗口边框 + 滚动条指示器
- 键盘导航：↑↓ Home End PgUp/PgDn Enter Esc，1-9 数字键快速选择
- 分隔线（空字符串或 "---"），跳过键盘选中
- 位置自动 Clamp，屏幕边界不溢出
- `TuiDemo` F6=短菜单 F7=长滚动菜单(28项) F8=右键菜单

**Markdown 表格渲染**
- 完整 GFM 表格语法解析（`| cell | cell |`），Unicode 边框字符（┌┬┐├┼┤└┴┘）
- 列宽自适应内容 + 终端宽度不足时等比缩放
- **单元格内联格式**：`**加粗**` 和 `` `代码` `` 在表格单元格内正确渲染颜色
- `TuiDemo` F9=表格演示（语言对比 + 模型价格两张表）

### 🗑️ 移除
- **`UI/ScreenManager.cs`** (1458 行) — 旧全屏 TUI 管理器，功能全部迁移到新架构
- **`UI/WindowManager.cs`** (807 行) — 旧窗口管理器 + `ManagedWindow`/`UILabel`/`UIButton`/`UIInput` 旧控件体系

### ✨ 改进
- **Program.cs REPL 循环重写** — `ScreenManager.Instance` → `TuiManager.Instance` + `ChatScreen`，~100+ 处 `sm.` 调用迁移到新 API
- **PermissionManager** — `ShowInlinePermission` 改用 `ChatScreen.ShowInlinePermission()`（TuiDialog.Permission 包装器）
- **AgentSlot** — `SaveFrom`/`RestoreTo` 适配 `ChatScreen`（ChatMessages/InputArea/RecentFiles）
- **DiffPreview** — 移除无效 `ScreenManager.Instance` 引用
- **SettingsPage** — 独立全屏 Console UI，TUI 模式下临时 Exit/Enter
- **ThemeConfig** — `ApplyTo(ManagedWindow)` → `ApplyTo(TuiWindow)`
- **Editor/TuiTable/TuiBox** — `ScreenManager.Instance` 引用替换为 `TuiManager.Instance.ActiveScreen as ChatScreen`

### 📝 自测: 658/658

## v0.19.3 (2026-08-09) — 稳定性修复 + 控制台安全

### 🐛 修复
- **控制台无句柄崩溃**: `TTY.Cols`/`TTY.Rows` 属性在无真实控制台（管道/重定向/CI）时捕获 `IOException` 返回安全默认值 80×24，全局替换 8 个文件中 22 处直接 `Console.WindowWidth/Height` 调用
- **语义记忆文档拆分失败**: `ParseDocuments` 增加 `\r\n` → `\n` 规范化，修复 Windows 换行符下 `---` 分割线无法识别的问题
- **项目检测递归查找**: `DetectBuildTools`/`DetectFrameworks` 改用 `SafeGetFiles` 递归搜索 `.csproj`，修复 git 根目录下子目录项目无法检测到 dotnet/.NET SDK 的问题
- **自测参数越界崩溃**: 语义记忆测试访问 `docs[1]` 前增加 `Count >= 2` 守卫，避免拆分失败时 `ArgumentOutOfRangeException`

### 📝 自测: 705/705

## v0.19.2 (2026-08-09) — 多 Agent 工作区（F1-F10）

### 🔥 新增
- **多 Agent 工作区** (`AgentSlot.cs`): 10 个独立 Agent 槽位（F1-F10 一键切换），每个槽位拥有独立会话上下文 + 独立屏幕（聊天历史/输入草稿/状态栏/最近文件），懒创建 Agent，切换即保存/恢复 UI 状态
- **状态栏槽位指示条**: 状态栏左侧 10 个数字（1-9、0=10），底色白 = 当前显示屏幕；字色按状态——灰=空闲、绿=工作中、黄=等待权限、红=出错（LLM 全失败/超时自动标红）

### ✨ 改进
- **热键迁移**: F1/F2/F5/F10 让位给槽位切换，原功能迁移到 Ctrl 组合键——帮助 `Ctrl+H`、面板 `Ctrl+B`、设置 `Ctrl+O`、退出 `Ctrl+Q`
- **权限状态信号**: `PermissionManager` 新增 `PermissionPromptStarted`/`PermissionPromptResolved` 事件，权限确认框弹出时槽位指示条实时变黄
- Agent 运行时禁止切换槽位（提示等待），避免上下文撕裂

### 📝 自测: 704/704

## v0.19.1 (2026-08-09) — 三骨架接入 + 记忆系统升级

### 🔥 新增
- **文档查询工具接入** (`Tools/DocTool.cs`): 注册为第 31 个工具，`action='search'` 定向抓取官方文档（React/Next.js/Vue/DotNET/Rust/Go 等 30+ 库），`action='fetch'` 抓取指定页面，15 分钟会话级缓存
- **Diff 预览接入** (`UI/DiffPreview.cs`): `WAYCODER_DIFF_PREVIEW=1` 开启，`write_file`/`edit_file` 写前逐 hunk 确认（Y 接受/N 跳过/A 全接受/Q 取消），新增 `ApplyAccepted` 按接受集合重建内容；非交互模式（管道/重定向/测试）自动跳过

### ✨ 改进
- **记忆系统升级为结构化格式**: `MemoryTool` 从单文件 memory.md 切换到 `.corecoder/memory/*.md` frontmatter 多文件（read/write/search/delete 四操作，支持 name/description/type 参数）；`SystemPrompt` 记忆注入同步切换并自动迁移旧格式
- **自测隔离**: 记忆段与系统提示词段在临时目录运行，不再污染真实 `memory.md`；自测新增 Doc 校验、结构化记忆、Diff 纯函数测试

### 📝 自测: 694/694

## v0.19.0 (2026-08-09) — 技能系统 + 并行子代理 + CI

### 🔥 新增
- **技能系统** (`SkillsManager.cs` + `Tools/SkillTool.cs`): 标准 SKILL.md 格式发现与解析（`skills/<name>/SKILL.md`），SystemPrompt 注入精简技能列表，`skill` 工具按需加载完整 body 与打包文件
- **并行子代理** (`Tools/AgentTool.cs`): `tasks` 数组参数，最多 4 个并发，结果聚合返回；保留多层递归深度限制
- **GitHub Actions CI** (`.github/workflows/ci.yml`): 自动构建 + 全量自测
- **Git Worktree 隔离** (`WorktreeIsolation.cs`): 检测 worktree 路径并自动切换 bash cwd，`BashTool` 已接入
- **结构化记忆骨架** (`StructuredMemory.cs`): 对标 Claude Code frontmatter 记忆设计（未接入，预留）
- **Diff 预览骨架** (`UI/DiffPreview.cs`): 逐 Hunk 确认对话框（未接入，预留）
- **文档查询工具** (`Tools/DocTool.cs`): 搜索+抓取最新库/框架文档（未注册，预留）

### ✨ 改进
- **AutoGitCommit 质量校验**: conventional-commit 前缀强制 + 引号/代码围栏清理 + 不合格重试一次 + 兜底默认信息，杜绝乱提交

### 🔧 修复
- **SelfTest Checkpoint 段**（根因修复）: 原在仓库内执行 `git stash push` 会清空工作树且累积垃圾 stash（曾达 101 个），现隔离到临时非 git 目录执行
- **移除有风险的 Undo 测试**: FileBackup 还原路径会把备份拷回工作树，解析出错即覆盖真实文件，改为标注风险不测试

### 📝 自测: 676/676

## v0.18.5 (2026-08-09) — Plan 模式 + 稳定性修复

### 新增
- **Plan 模式** (`PlanMode.cs`): 14 个只读工具 + 规划提示词 + IsApproval 确认判断

### 修复
- **ReviewMode**: 始终列出文件名，git diff 优先
- **BashTool**: ReadToEndAsync 替代事件回调，避免管道死锁
- **CheckpointManager**: 路径 TrimStart('/') 修正 + ChangedFiles 回退
- **SelfTest**: Cwd 重置、缓存键断言修正、沙箱断言修正

### 操作
- Ctrl+C 闲时直接退，忙时中断后确认
- 自测: 656/657

## v0.18.4 (2026-08-08) — 终端抽象层 + 窗口系统 + 主题引擎

### 🔥 新增
- **终端抽象层** (`Terminal/`): TTY 屏幕控制 / RenderBuffer 零转义符渲染 / BoxChars 13种边框 / AnsiString 检测剥离 / Color 命名颜色 / AnsiText 快捷格式化
- **窗口管理器** (`UI/WindowManager.cs`): Z-order 层叠 / 裁剪渲染 / 模态对话框 / 弹出菜单 / Toast / UIControl 控件体系 (UILabel/UIButton/UIInput) / 11种边框风格 / 自定义边框
- **主题引擎** (`ThemeConfig.cs`): 6预设主题 / 自定义配色 / `~/.corecoder/theme.json` 持久化 / `/theme` 命令 / 设置页主题选择
- **Markdown 渲染**: 标题/代码块(14语言高亮)/表格/多级列表 / 内联格式
- **Diff 渲染**: 红绿背景 + 行号 + 语法高亮
- **InputManager**: 全键盘拦截 + 鼠标滚轮 + resize 即时重绘

### 🔧 修复
- **权限对话框**: `CheckAsync` 改用 `ShowInlinePermission` 行内渲染，不再卡死不显示
- **多级列表**: `MdListItem.Level` 字段，每2前导空格=1级缩进
- **减少空行**: 只在标题/代码块/表格后留空，段落间列表间不留
- **Emoji 宽度**: `cp >= 0xFEFF` → `cp == 0xFEFF`，修复所有 Emoji 被误判零宽
- **AnsiText 补全**: BoldFg/Reset/DimOn/BoldOn/FgCode/BoldFgCode/ClearLine/BorderOpen/Heading/Prompt
- **ClipboardHelper**: 跨平台剪贴板读取

### 📝 自测: 649/658

## v0.18.3 (2026-08-08) — 权限对话框行内渲染 + 表格显示修复 + Markdown 渲染缓存

### 🔧 修复
- **权限对话框卡死不显示**：`PermissionManager.CheckAsync` 从居中弹窗 `ShowMenu`（WindowManager 浮层 + 嵌套 ReadKey 循环）改为行内三行渲染 `ShowInlinePermission`，不依赖浮层机制，稳定可靠
- **表格输出导致聊天区卡死**：`TuiTable.Render()` 前缀 ANSI 哨兵 `\x1b[0m` 跳过 Markdown 重解析，逐行独立注入，添加后自动刷新屏幕
- **流式输出每帧重解析全文**：`BuildChatScreenLines` 新增按消息索引 + 内容缓存的渲染缓存，`AppendToken` / `FinishToolProgress` 精准失效，宽度变化或外部清空自动重置

### ⚡ 性能
- Markdown 渲染缓存：流式输出时只有最后一条消息每帧解析，其余消息走缓存 O(1) 命中，彻底解决大对话卡顿

## v0.18.2 (2026-08-08) — CJK/Emoji 全宽字符覆盖 + 方块光标

### 🔧 修复
- **RuneWidth 全面覆盖 CJK/全角/符号/Emoji**：
  - 修复 0x1F000-0x1F02F (麻将牌) 错误返回零宽的问题
  - 新增 0x2010-0x2027 (通用标点 — … " " ' ' ※ 等 East Asian Ambiguous)
  - 新增 0x2030-0x2043 (补充标点 ‰ ′ ″ ※ 等)
  - 新增 0x2600-0x27BF (杂项符号 + 装饰符号 ☀ ★ ❤ ➿ 等)
  - 扩展 Emoji 覆盖 0x1F000-0x1FAFF (含 Symbols & Pictographs Extended-A)

### ✨ 改进
- 光标改为大方块样式（`\x1b[2 q` DECSCUSR，对标竞品）

## v0.18.1 (2026-08-08) — 输入区分割线 + CJK 光标修复 + 聊天区简化

### ✨ 改进
- 输入区上下添加 dim 分割线（`─`），与聊天区视觉分离
- 状态栏紧跟下分割线，布局紧凑不重叠

### 🔧 修复
- CJK 中英文混合输入时光标逐渐错位（`InputHardToScreen` 换行边界反向遍历）
- 聊天区闪烁指示器移除（只有输入框需要光标/闪烁 `▊`）
- `works/**/*.cs` 编译排除，防止无关文件参与构建

### 📝 自测
- **61 项** Markdown 模块（通过全部 61 项）

## v0.18.0 (2026-08-08) — 窗口系统 + 主题引擎 + TUI 控件库 + 终端抽象层

### 🔥 重大变更

**窗口管理器** (`UI/WindowManager.cs`)
- Z-order 层叠窗口、裁剪渲染、模态对话框、弹出菜单、Toast 提示框
- UIControl 体系：UILabel / UIButton / UIInput，Tab/Shift+Tab 焦点切换
- 11 种边框风格：single/double/rounded/thick/solid/dotted/dashed/slash/triangle/ascii/custom
- 菜单满行高亮条、分隔线、滚动指示器 + 滚动条
- 窗口关闭自动还原背景

**终端抽象层** (`Terminal/`)
- `TTY` — 屏幕切换/清屏/光标/鼠标/颜色/样式快捷方式
- `RenderBuffer` — Write/Segment/Fill/SegmentBold/SegmentDim/Blink 零转义符渲染
- `BoxChars` — 13 种预设 + 自定义边框字符集
- `AnsiString` — ANSI 检测/剥离/截断/宽度计算
- `Color` — 命名颜色管理（Color.Cyan 替代数字 36）

**主题引擎** (`ThemeConfig.cs`)
- 6 个预设主题：default/ocean/forest/sunset/midnight/mono
- 自定义边框/背景/前景/选中色，持久化到 `~/.corecoder/theme.json`
- `/theme` 命令 + 设置页面主题选择
- ScreenManager 主题自动同步

**Markdown 渲染** (`UI/MarkdownRenderer.cs`, `UI/TuiMarkdown.cs`)
- 标题/段落/代码块(14 语言语法高亮)/表格/列表/内联格式

**Diff 渲染** (`UI/DiffRenderer.cs`)
- 红绿背景 + 行号 + 语法高亮

**InputManager** (`UI/InputManager.cs`)
- 全键盘拦截 + 鼠标滚轮 + resize 即时重绘

### ✨ 新增功能
- 子智能体递归：支持最多 5 层嵌套（`AgentTool.MaxDepth`）
- `/undo` 按文件精确 revert（`CheckpointManager.UndoAsync(id, filePath)`）
- TF-IDF 语义记忆（`SemanticMemory.cs`，零依赖纯 C#）
- Lint/Tool 超时可配置（`ToolTimeoutSec` / `LintTimeoutSec`）
- `/todo` 命令接入 UI
- Logo 补齐 "WAY**CODER**" 完整拼写

### 🔧 修复
- `DisplayWidth` 不计入 ANSI 转义码宽度
- 窗口裁剪 `Clip()` 修复顶框/左框被误裁
- 右框绝对定位，不再因 CJK 文字宽度错位
- 空窗口竖边始终渲染
- Damerau-Levenshtein 支持字符换位纠错
- 历史上限 while 循环修复
- 建议面板半宽+滚动+不溢出
- 滚动菜单选中项不被指示器挤出

### 📝 自测
- **636 项**（+35 新测试：主题/边框/Diff/InputManager/窗口）

## v0.17.5 (2026-08-07) — 移除 Terminal.Gui v2，恢复 AOT，三项短板清零

### 🔥 重大变更
- **移除 Terminal.Gui v2** — 回退到 ANSI 全屏 TUI（ScreenManager），Terminal.Gui v2 库体验不佳
- **恢复 AOT 编译** — `PublishAot` 重新启用，恢复零依赖单文件 exe 部署（~8MB）
- **子智能体递归** — 支持最多 5 层嵌套子智能体，深度可配置，自动移除 agent 工具防止无限递归
- **`/undo` 按文件精确 revert** — 支持 `/undo [N] <file>` 选择性恢复，`/undo -l` 列出检查点文件
- **工具/Lint 超时可配置** — 新增 `ToolTimeoutSec`（默认 120s）和 `LintTimeoutSec`（默认 60s）

### 🗑️ 移除
- 删除 `TerminalGuiRepl.cs` / `Controls.cs` / `ScrollMenu.cs`
- 删除 `--tui-v1` CLI 参数（仅保留 ANSI TUI）
- 删除 `TERMINAL_GUI` 编译常量和 Terminal.Gui NuGet 依赖
- 删除 `RunReplV2Async()` 方法

### ✨ 新增功能
- **Lint/Tool 超时可配置** — `WAYCODER_TOOL_TIMEOUT` / `WAYCODER_LINT_TIMEOUT` 环境变量
- **`/undo` 按文件恢复** — `CheckpointManager.UndoAsync(id, filePath)` + `GetCheckpointFiles()`
- **子智能体递归** — `AgentTool.MaxDepth` + `AsyncLocal<int>` 深度追踪
- **子智能体深度配置** — `WAYCODER_SUBAGENT_DEPTH`（默认 3，范围 1-5）
- **`/undo` / `/checkpoint` / `/checkpoints` 命令接入 UI** — 修复死代码问题

### 🔧 保留
- ScreenManager ANSI TUI 作为唯一交互式 REPL 实现
- 所有现有功能：侧栏面板、输入历史、彩色渲染、建议补全、弹窗菜单

## v0.17.4 (2026-08-07) — Terminal.Gui v2 默认 TUI

### 🔥 重大变更
- **Terminal.Gui v2 成为默认 TUI** — 替代手写 ANSI 转义码的 ScreenManager
  - 原 `--tui-v2` 标志改为 `--tui-v1`（回退到旧版 ANSI TUI）
  - 移除全部 `#if TERMINAL_GUI` 条件编译守卫
  - **AOT 编译暂时禁用**：Terminal.Gui v2 不支持 NativeAOT，待 v2 正式版后恢复
  - PublishAot 设为 false，仍保留单文件发布

### ✨ 新增功能
- **聊天区彩色多角色渲染** — `ChatView`（View + Label 组合）替代单色 `TextView`
  - User=亮青 `BrightCyan`、Assistant=白 `White`、System=灰 `Gray`、Tool=亮黄 `BrightYellow`、Welcome=青 `Cyan`
  - 每行独立 `ColorScheme`，手动 Y 坐标滚动
  - 流式输出实时追加到最后一行
- **侧边面板** — F2 切换 32 列右侧面板，4 个标签页：
  - 任务（Todo 列表）、文件（修改文件列表）、锁（活跃文件锁）、MCP（服务器状态）
- **输入历史导航** — ↑↓ 键浏览历史输入（单行模式），Ctrl+Enter 插入换行，Esc 清空
- **自动会话恢复** — 启动时检测 `_auto` 会话，提示 `/resume` 恢复
- **Scroll 快捷键** — PageUp/Down 滚动聊天区，Ctrl+Home/End 跳转首尾

### 🐛 修复
- 管道模式误判：`Console.IsInputRedirected` 在 bash 下始终为 true，修复为空 stdin 不触发一次性模式
- `Tab.View` 为 null 导致 NRE 崩溃（v2 API 变更：Tab 即 View）
- `Application.Top` 在 `Run()` 之前为 null 导致 NRE 崩溃
- `Colors.Base` / `Colors.TopLevel` / `Colors.Menu` 在 v2 不存在，改为视图级 `ColorScheme`
- `verify_build/` 目录混入 git 提交，已清理并加入 `.gitignore`

### 🔧 增强
- 状态栏快捷键：F1 帮助 / F2 面板 / F5 设置 / F6 编辑 / F10 退出
- 窗口标题实时显示当前模型名称
- CJK 字符在 Label 中正常显示（Terminal.Gui 内部处理）

## v0.17.3 (2026-08-07) — MCP 协议完善

### ✨ 新增功能
- **MCP 传输抽象层** — 解耦通信方式与协议层
  - 新增 `McpTransport` 抽象类：`SendRequestAsync` / `SendNotification` / `DisconnectAsync` / `IsConnected`
  - `StdioMcpTransport`：从 `McpConnection` 提取子进程管理代码（向后兼容）
  - `HttpMcpTransport`：HTTP POST + SSE 响应流解析，支持 Streamable HTTP 传输
  - `McpConnection` 重构为协议层，持有 `McpTransport` 实例委托通信
- **MCP 配置格式增强**
  - `mcp_servers.json` 新增 `transport` / `url` / `headers` 字段
  - 自动检测传输类型：有 `url` → HTTP，否则 → stdio（向后兼容）
  - `headers` 和 `url` 中的 `${VAR}` 语法自动展开为环境变量
  - `RunInit()` 模板添加 HTTP MCP 服务器注释示例
- **工具发现缓存** — 持久化 `tools/list` 结果，加速启动
  - 新增 `McpCache.cs`：SHA256 缓存键 + 24h TTL
  - `CachedMcpTool`：缓存命中时立即可用，后台异步刷新
  - 配置变更自动失效（基于命令/参数/URL 的 SHA256 哈希）
- **MCP 面板状态显示** — F2 面板 MCP 页显示服务器连接状态和工具数量
  - `McpManager.Info` 属性 + `ScreenManager` 自动读取渲染

### 🔧 增强
- MCP 客户端版本号更新为 0.17.3
- `DiscoveredTools` 支持按服务器前缀移除旧条目（缓存刷新时替换）

### 📝 自测
- 10+ 项 MCP HTTP 传输测试（传输检测、环境变量展开、headers 解析）
- 6 项 MCP 缓存测试（缓存键稳定性、规范标识符、格式验证）

## v0.17.2 (2026-08-07) — TUI 编辑器 Lint 诊断集成

### ✨ 新增功能
- **编辑器 Lint 诊断** — 保存文件时自动运行 lint 检查，行内标注错误和警告
  - 新增 `DiagnosticManager.cs`：解析 10+ 种 linter 输出格式（dotnet build / eslint / ruff / go vet / gcc / shellcheck / ruby / php / java / rust cargo）
  - 编辑器 gutter 指示器：有错误的行显示红色 `●`，有警告的行显示黄色 `▲`
  - 错误行红色背景、警告行黄色背景高亮
  - 状态栏显示错误/警告计数和当前行诊断消息
  - 配置开关：`WAYCODER_EDITOR_LINT`（默认开启）
- 新增 `ErrorBg` / `WarningBg` 颜色常量（ANSI 41 / 103）

### 📝 自测
- 10 组诊断解析测试（dotnet/eslint/ruff/go vet/gcc/shellcheck/ruby/php/java/rust）
- 3 组查询/配置/枚举测试

## v0.17.1 (2026-08-07) — 沙箱执行

### ✨ 新增功能
- **三级沙箱执行** — suggest（确认）/ auto-edit（自动编辑）/ full-auto（全自动沙箱）
  - 新增 `SandboxManager.cs`：环境变量清理、工作目录锁定、系统目录写保护、内存监控（1GB 限制）
  - bash 工具集成：沙箱模式下自动创建受保护的进程、异步内存监控
  - 权限系统联动：full-auto 模式 bash 直接放行
  - 配置开关：`WAYCODER_SANDBOX_LEVEL`（默认 suggest）

### 📝 自测
- 30 项沙箱管理测试

## v0.17.0 (2026-08-07) — Prompt 缓存 + 竞品 P0 清零

### ✨ 新增功能
- **Prompt 缓存追踪** — SHA256 本地检测系统提示词和工具定义是否重复发送
  - 新增 `PromptCache.cs`：静态追踪类，每次 LLM 请求前记录哈希值
  - `/stats` 面板展示缓存命中率、节省 Token 数和估算费用
  - 配置开关：`WAYCODER_PROMPT_CACHE`（默认开启）
  - 不影响实际 LLM 请求内容，仅在监控层面追踪
- **竞品 P0 差距全部清零** — 自动 Test 修复循环（v0.16.3 已有）+ Prompt 缓存（v0.17.0）

### 📝 文档更新
- ROADMAP.md：自动 Test 循环 ✅、Prompt 缓存 ✅、重新排布优先级
- competitor-analysis.md：P0 差距清零、功能矩阵更新、代码自愈评级提升

## v0.16.3 (2026-08-07) — 更名 WayCoder（道码）+ Watch 模式

### 🔥 重大变更
- **软件更名** — CoreCoder → **WayCoder（道码）**，因发现与现有商标名称冲突，规避侵权风险
  - 可执行文件：`corecoder.exe` → `waycoder.exe`
  - 环境变量前缀：`CORECODER_*` → `WAYCODER_*`（旧名仍兼容）
  - 配置目录：`.corecoder/` → `.waycoder/`（旧目录仍兼容）
  - 展示文本全部更新：UI 标题栏 / 帮助文本 / 测试输出 / 调试日志 / User-Agent / MCP 客户端名

### ✨ 新增功能
- **Watch 模式 (`--watch` / `-w`)** — 监听外部编辑器文件变更，检测代码注释中 `AI!` / `AI?` 标记自动触发 Agent
  - 兼容 Aider 的 AI 注释语法：`// AI!`、`# AI!`、`-- AI!`、`/* AI? */`、`<!-- AI! -->`
  - 支持 40+ 种编程语言（.cs .py .ts .js .go .rs .java .kt .swift .c .cpp .rb .php .vue .svelte 等）
  - 500ms 防抖合并快速连续修改 + 线程安全 `ConcurrentQueue` 队列
  - `/watch` REPL 命令切换 + `CORECODER_WATCH` 环境变量 + 设置界面集成
  - 3 处退出路径自动清理（F10 / quit / Ctrl+C）

## v0.16.2 (2026-08-07) — /stats 用量统计面板

### ✨ 新增功能
- **`/stats` 用量统计** — 表格面板展示模型/Token/花费/延迟/速度/消息/会话/权限等全维度用量

## v0.16.1 (2026-08-07) — 滚动修复 + /loop + /test + 界面主题

### 🐛 修复
- **滚动区域重构** — ChatScrollUp 下限 clamp + ChatScrollDown 延迟判底机制
- **ShowMenu 重写** — 长列表自动滚动 + PgUp/PgDn/Home/End 导航 + 选中项背景填充修复 + 滚动指示器
- **RenderSuggestions 越界修复** — 边框/填充改用 `chatW` 替代 `TW`，右侧面板激活时不再视觉错乱
- **ShowDialog 边距修正** — 标题/内容/提示行 fill 计算公式统一
- **箭头键聊天滚动** — 输入为空时 ↑↓ 滚动聊天区；Ctrl+↑/↓ 随时可滚动

### ✨ 新增功能
- **`/loop` 循环执行** — `/loop [最大轮次] 提示词` 重复执行 Agent 直到输出含成功标记（SUCCESS/✅/通过 等）
- **`/test` 分模块测试** — `/test all|tools|ui|git|config|memory|agent|review|mcp|system` 只跑相关模块
- **界面主题系统** — 6 套预设配色（default/ocean/forest/sunset/mono/cyberpunk）+ 4 种边框类型（rounded/single/double/bold）+ 独立边框色/强调色设置
- **SettingsPage 新增 `🎨 界面` 分类** — 配色方案/边框类型/边框颜色/强调色 四设置项，选色立即生效

### 🔧 增强
- 快捷键栏新增 `PgUp/Dn 滚动` 提示
- SelfTest 新增 Section/RunWithFilter 过滤机制
- ScreenManager 全面主题化：顶栏/分隔线/输入区/建议面板/菜单/对话框 均使用主题边框和颜色

## v0.16.0 (2026-08-07) — 20 项综合增强

### ✨ 新增功能
- **MCP 环境变量传递** — `mcp_servers.json` 支持 `env` 字段，可向 MCP 进程注入 API Key 等环境变量
- **标准输入管道模式** — `echo "prompt" | corecoder` 管道输入自动一次性模式 + yolo
- **记忆自动注入** — MemoryStore 内容自动注入系统提示词，跨会话项目知识持久化
- **Sub-Agent 增强** — 子智能体使用小模型（省钱）+ 注入父上下文（最近 6 条消息）
- **自定义提示词模板** — 扫描 `.corecoder/prompt.md` 及 `.corecoder/*.md`（排除 memory.md）自动注入
- **对话历史搜索** — `/history <关键词>` 搜索 + `Ctrl+R` 交互搜索，含上下文预览
- **Diff 预览确认增强** — write_file 显示行数/新建/覆盖 + 内容预览，edit_file 显示 +/- 对比
- **项目初始化向导** — `corecoder --init` 创建 `.corecoder/` + 模板文件（mcp_servers.json, prompt.md, memory.md）
- **命令别名** — `/c→/compact, /m→/model, /r→/reset, /h→/help, /t→/tokens, /d→/diff, /s→/save, /q→quit`
- **Token 性能统计** — 每次请求延迟 + tok/s 显示在 `/tokens` 和右下角状态栏
- **Agent 完成通知** — 耗时显示 + 终端响铃 `\a`
- **Agent 错误自恢复** — 工具报错时追加修正提示，引导 LLM 自我纠正
- **对话导出** — `/export` → `.corecoder/export_<date>.md`，Markdown 格式含角色和时间戳
- **输入历史** — `↑↓` 键浏览历史输入（单行模式），最多 200 条，去重相邻重复
- **模型热键切换** — `Ctrl+M` 循环切换大模型：deepseek-v4-flash → pro → gpt-5.4-mini → gpt-5.4
- **HTTP 代理支持** — 读取 `HTTPS_PROXY` / `HTTP_PROXY` / `ALL_PROXY` 环境变量配置代理
- **Tab 路径补全** — 输入像文件路径时 Tab 智能补全（最长公共前缀 + 候选列表）
- **Git 分支显示** — 启动时检测 `.git/HEAD`，顶栏显示当前分支名
- **最近文件列表** — `/recent` 显示最近修改文件（最多 50），按时间排序
- **快捷键完善** — `F1` 帮助，`F10` 自动保存 + 退出

### 🔧 增强
- CJK 感知 Token 估算（CJK ~1.5 tok/char，ASCII ~0.25 tok/char）
- Checkpoint 持久化加载（重启后 `/undo` 不丢失）
- Review 模式改为 git-diff-based（聚焦实际改动）
- 会话自动保存（退出时保存，启动时 `/resume` 恢复）
- 欢迎屏 ASCII Logo 注入聊天区
- 快捷键栏精简：`F1帮助 F2面板 F5设置 F6编辑 ↑↓历史 Ctrl+R搜索 Ctrl+M切模型`
- 版本号 v0.15.1 → v0.16.0

## v0.9.0 (2026-08-06) — 14 个内置工具 + 6 个新命令 + 进程管理

### ✨ 新增功能
- **仓库地图 (Repository Map)** — 自动扫描项目结构，17 种语言符号提取，ASCII 树状图
- **14 个纯 C# 内部工具** — 替代 Shell 依赖，可控缓冲区/超时/无转义问题：
  - 进程管理：`ps`（进程列表）、`kill`（终止进程，含系统进程保护）
  - 目录操作：`ls`（目录列表）、`mkdir`（创建目录）、`rm`（安全删除）、`cd`（切换目录）、`pwd`（打印路径）
  - 文件操作：`cp`（复制）、`mv`（移动）、`diff`（差异比对）、`stat`（元数据）
  - 文本统计：`wc`（行/词/字符计数）、`tree`（ASCII 目录树）、`find_replace`（跨文件查找替换）
- **6 个新 REPL 命令**：`/cost`（费用统计）、`/commit`（Git 提交）、`/doctor`（环境诊断）、`/config`（配置管理）、`/test`（运行测试）、`/status`（项目状态）
- **Git PR 工具** — `git_pr` 自动创建分支、推送、生成 GitHub/Gitee PR 链接
- **打包脚本** — `package.sh` / `package.bat` 一键 AOT 发布 + zip 打包
- **`/repomap` REPL 命令** — 手动刷新仓库地图

### 🔧 增强
- 仓库地图自动集成到系统提示词，LLM 时刻了解代码库布局
- `write_file`/`edit_file` 后自动使仓库地图缓存失效
- GitTool 异步输出读取，修复大输出死锁问题
- ITool Schema() 深克隆 Parameters，修复 "node already has a parent" 崩溃
- 工具总数：15 → 29
- 245 项自测（+49 项新工具/命令测试）

## v0.8.0 (2026-08-06) — 预算控制 + Hooks 生命周期 + MCP 协议

### ✨ 新增功能
- **硬预算上限** — `--max-budget-usd` CLI 标志 + `CORECODER_MAX_BUDGET_USD` 环境变量，超支自动停止
- **Hooks 生命周期** — PreToolUse / PostToolUse 事件，Shell 脚本处理器，退出码 2=阻止
- **MCP 协议支持** — Stdio 传输，自动发现 MCP 服务器工具，命名空间 `mcp__<server>__<tool>`
- **MCP 配置** — `.corecoder/mcp_servers.json` 配置服务器

### 🔧 增强
- `Config.MaxBudgetUsd` — 预算配置字段
- `ToolRegistry.AllTools` → 动态属性，自动合并 MCP 工具
- 166+ 项自测（+3 项预算/Hooks/MCP 测试）

## v0.7.0 (2026-08-06) — 自定义命令 + 自动 Lint 闭环 + YOLO 模式

### ✨ 新增功能
- **`--yolo` CLI 标志** — 一次性模式跳过所有权限确认，非交互环境可用
- **自动 Lint 反馈闭环** — `write_file`/`edit_file` 后自动运行 lint，错误注入 LLM 上下文
- **自定义斜杠命令** — `.corecoder/commands/*.md` 用户可扩展，支持 YAML frontmatter + `$ARGUMENTS`
- **示例命令** — `.corecoder/commands/review-code.md` 代码审查模板

### 🐛 Bug 修复
- **JsonNode Parent 冲突** — `FullMessages()` 深克隆消息，修复 "already has a parent" 崩溃

### 🔧 内部
- `CustomCommands.cs` — 自定义命令加载器
- 163+ 项自测（+3 项自定义命令测试）

## v0.6.1 (2026-08-06) — 三个新工具 + 增强语言支持

### ✨ 新增工具
- **Lint 工具** — 静态检查反馈闭环，支持 25+ 种编程语言（C#、Python、JS/TS、Go、Rust、Java、C/C++、Ruby、PHP、Swift、Kotlin、Lua、Shell、HTML/CSS、Vue、YAML、JSON、Markdown、Dart、R、SQL、Perl、Elixir、Haskell、Zig）
- **Web 搜索工具** — 通过 DuckDuckGo 进行网页搜索，无需 API 密钥
- **Checkpoint 系统** — Git stash + 文件备份双轨检查点，支持 `/checkpoint` `/undo` `/checkpoints` 命令

### 🔧 增强
- Lint 工具自动检测项目类型（通过扩展名和项目文件）
- AOT 安全的 JSON 序列化（无反射，手写 JSON 构建）
- 160 项自测全通过（+15 项新增测试）

## v0.6.0 (2026-08-06) — 纯 C# 版

### 🔥 重大变更
- **移除 Python 版** — 删除 `corecoder/`、`tests/`、`pyproject.toml` 等全部 Python 代码
- **C# 版成为唯一版本** — `CoreCoderSharp/` 是项目唯一实现
- **文档中文化** — README、CLAUDE.md、CHANGELOG 全部改为中文

## v0.5.0 (2026-08-05) — C# 重构版

### 🚀 C# 移植 (CoreCoderSharp)
- 完整移植 Python 版全部功能到 C# (.NET 10)
- AOT 原生编译为单文件 exe (7.8 MB)，无需运行时
- 12 个工具，44 项自测全通过

### ✨ 新增功能 (C# 版)
- **权限确认系统** — 危险操作前弹窗确认，支持 Ask/Auto/Yolo 三种模式
- **Git 智能集成** — git 工具 + `/git-status` `/git-log` `/git-diff` 命令
- **Web 抓取** — fetch 工具，自动提取网页纯文本
- **计划模式** — `/plan` 命令，Agent 先规划再执行
- **Todo 任务追踪** — Agent 可管理结构化任务列表
- **LSP 代码导航** — go-to-definition, find-references, hover, symbols
- **后台任务** — bash 命令后台执行，`/jobs` `/job-output` 查看
- **记忆系统** — Agent 可读写项目记忆 (`.corecoder/memory.md`)
- **流式工具执行** — LLM 边生成边执行工具，降低延迟
- **调试日志** — `--debug` 记录完整通信内容到 logs/
- **彩色 TUI** — Spectre.Console 美化界面 + ASCII Art 欢迎屏
- **项目指令加载** — 自动读取 CLAUDE.md/AGENTS.md
- **代码审查** — `/review` 命令多维度审查修改
- **模型回退** — LLM 失败时自动尝试备用模型
- **项目检测** — 自动识别语言/框架/构建工具

### 🔄 Python 版更新
- 默认模型改为 `deepseek-v4-flash`
- 新增 DeepSeek V4 Flash/Pro 定价
- CLI 自动识别 DeepSeek 模型并设置 base URL
- 所有注释和 docstring 翻译为中文

## v0.4.1 (2026-08-01)
- 添加 AGENTS.md 双语智能体指南
- 初始版本
