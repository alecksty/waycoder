# 更新日志

## v0.71.20 (2026-08-17) — Infra 层确定性 bug 修复（Hooks 前缀碰撞 + FileIgnore 未转义 [ + RetryPolicy 边界）

Explore 代理系统扫 `Infra/`（BashGuard/FileTracker/RetryPolicy/LruCache/HooksManager/FileIgnoreManager/ErrorLog/IdGenerator）后人工验证，本轮修复 4 个可复现问题。BashGuard 的 `rm`/`mv`/`cp` 未列入禁用集合经核实为**有意设计**（走 PermissionManager 确认层，非禁用层），不修。

### 🐛 修复

- **`HooksManager` session hook 前缀碰撞**：hook ID 格式为 `"{eventType}_{guid}"`，`RunEventAsync` 用裸 `kv.Key.StartsWith(eventName)` 判断事件归属，导致 `"PostToolUseFailure_xxx".StartsWith("PostToolUse")` 为真——失败专属 hook 在成功路径上被误触发。改为 `eventName + "_"` 前缀 + `Ordinal` 精确匹配
- **`FileIgnoreManager` 未转义 `[`/`]` 生成非法正则**：`GlobSegmentToRegex` 转义了 `. + ( ) ^ $ { } | \` 却漏了 `[`/`]`，`.gitignore` 出现不成对方括号（如 `foo[`、`[abc`）时 `new Regex` 抛 `ArgumentException`（`Match` 无 try/catch，向上传播到文件过滤/搜索工具）。补上 `[`/`]` 转义为字面量
- **`RetryPolicy` `MaxRetries` 负数不执行 action**：`for (attempt = 0; attempt <= cfg.MaxRetries; attempt++)` 在负数时条件立即为 false，action 一次都不执行却落到「不可达终点」抛误导性的 `InvalidOperationException`。改为 `maxRetries = Math.Max(0, cfg.MaxRetries)` 钳制
- **`RetryPolicy` `NoRetryExceptions` null 无保护**：`if (NoRetryExceptions.Contains(...))` 直接调用，而 `RetryableExceptions` 有 `is { Count: > 0 }` 保护。调用方显式置 null 时 `ShouldRetry` 抛 NRE（且位于异常过滤器内会逃出 catch）。改为 `?.Contains(typeName) == true`

### ✅ 测试

新增 `TestV0720InfraDeterministic`（6 项断言）：PostToolUse 不误触发 PostToolUseFailure hook / PostToolUseFailure 正确触发 / 含未闭合 `[` 的 .gitignore 不崩溃且字面匹配 / MaxRetries 负数钳制为 0 执行一次 / NoRetryExceptions null 不抛 NRE。测试总数 3270 → 3276。

## v0.71.19 (2026-08-17) — 语义记忆扩展 B 区汉字召回修复（Tokenize 代理对）

`SemanticMemory.IsCJK` 只覆盖 BMP 内 CJK 区间，`Tokenize` 按 `char`（UTF-16 码元）迭代，导致 CJK 扩展 B 区汉字（U+20000–U+2A6DF，UTF-16 代理对，如 𠮷、𩸽 等常见于人名/地名）落入 `i++` 被**静默丢弃**——既不参与 bigram 也不成为单 token，含扩展 B 的记忆按扩展 B 关键词查询时召回失败。属召回率缺陷而非崩溃/数据损坏。

### 🐛 修复

- **`SemanticMemory.Tokenize` 扩展 B 代理对处理**：在跳过空白/标点后、CJK 判断前，识别 `高代理项+低代理项` 并解出完整码点；若落在扩展 B 区间则作为**单个 token**加入（扩展 B 罕见字按单字索引、无需 bigram），其余代理对（emoji 等）成对跳过——同时避免旧代码把 emoji 逐 `char` 丢弃时的低效迭代。BMP 基本区汉字 bigram 逻辑不变。

### ✅ 测试

新增 `TestV0719CjkExtB`（5 项断言）：`Tokenize("𠮷野家")` 保留扩展 B 字「𠮷」为单 token + BMP「野家」bigram 正常 + 无孤立代理项；emoji 代理对成对跳过不产生 token；`SearchRelevant` 用扩展 B 查询命中含扩展 B 的记忆。测试总数 3265 → 3270。

## v0.71.18 (2026-08-17) — 共享记忆按名查找修复（StructuredMemory.Get 双目录回退）

记忆系统的共享记忆在 `ListAll()`/`Search()` 里能被加载（`ListAll` 遍历 `SharedMemoryDir` + `SlotMemoryDir` 两个目录），但 `Get(name)` 只经 `NameToPath` 解析到槽位独立目录（`MemoryDir => SlotMemoryDir`），导致共享记忆（如 `SharedMemoryManager.PullSharedAsync` 拉取的团队记忆、或 `GetRelevantContext` 里 `[[wiki-link]]` 交叉引用指向的共享记忆）**按名查不到，返回 null**。而 `Update`/`SetShared`/`GetRelevantContext` 均先 `Get(name)`，于是团队共享记忆无法被更新、无法展开交叉引用描述。

### 🐛 修复

- **`StructuredMemory.Get` 共享目录回退**：查找顺序改为「槽位独立目录优先 → 共享目录回退」，与 `ListAll` 双目录行为一致。抽出 `NameToPathIn(dir, name)` 辅助方法，`NameToPath`（供 Create/Delete 写槽位）委派给 `NameToPathIn(MemoryDir, ...)`，`Get` 额外回退 `NameToPathIn(SharedMemoryDir, ...)`
- **同名冲突槽位优先**：个人槽位记忆可覆盖同名共享记忆（个人覆盖团队，符合直觉）；`Get` 找到共享记忆后 `Update`/`SetShared` 会经 `existing.FilePath` 回写共享文件，行为正确

### ✅ 测试

新增 `TestV0718SharedMemoryGet`（4 项断言）：临时目录隔离 cwd + `CurrentSlotIndex=7`，直接向 `SharedMemoryDir` 根目录写一个共享记忆文件（模拟 `PullSharedAsync` 拉取），断言 `Get` 按名查到共享记忆、`ListAll` 双目录均列出、同名冲突时槽位优先。测试总数 3261 → 3265。

## v0.71.17 (2026-08-17) — UI 层确定性 bug 修复（撤销栈方向 + 菜单空序列 + BoxBuffer 负宽 + WrapLine 码点）

继续清扫 UI 层确定性 bug。Explore 代理系统扫 `UI/TUI/`（编辑器/控件/共享缓冲）后人工验证，本轮修复 4 个可复现问题。

### 🐛 修复

- **`EditorCore.TrimBottom` 撤销栈修剪方向反**：`Stack<T>.ToArray()` 返回栈顶在前（`arr[0]`=最新、`arr[^1]`=最旧），原循环 `i = arr.Length-1 .. arr.Length-max` 保留的是**最旧**的 max 条、丢弃最新若干条——编辑超过 `MaxUndo=100` 后第 101 次编辑被立即丢弃、撤销历史整体错位。改为 `i = max-1 .. 0` 保留最新 max 条，并提为 `internal` 便于自测
- **`TuiMenu` 全分隔线菜单空序列崩溃**：`.Max(i => DisplayWidth(i))` 只检查 `items.Count > 0` 未检查过滤后是否为空，列表全为 `""`/`"---"` 时 `Max()` 抛 `InvalidOperationException`。改为 `.Select(...).DefaultIfEmpty(10).Max()`
- **`BoxBuffer.Fill` 负宽度负参异常**：`new string(ch, ContentWidth)` 未钳制 `Width-2`，带边框且 `Width<2` 时抛 `ArgumentOutOfRangeException`（同文件 `Render` 已有 `Math.Max(0, ...)` 保护）。补上钳制
- **`TuiHelper.WrapLine` 首字符 emoji 切半**：`FindBreakIndex` 返回 0（首字符宽度即超 `maxWidth`）时原兜底 `breakIdx=1` 按 UTF-16 码元切半代理对，`maxWidth=1` 且首字符为 emoji/扩展区汉字时产出孤立代理项。改为取第一个完整码点的字符长度

### ✅ 测试

新增 `TestV0717RuneSafeWrap`（3 项）+ `TestV0717UiDeterministic`（3 项）：WrapText 首字符 emoji 不切半 / CJK 不越界 / 英文折行正常；TrimBottom 保留最新 2 条 / 全分隔线菜单不崩溃 / 窄边框 BoxBuffer.Fill 不抛。测试总数 3255 → 3261。

## v0.71.16 (2026-08-17) — 数据路径 UTF-16 切片代理对修复（6 处）

继续清扫「按 UTF-16 码元任意切片」这一 CLAUDE.md 明文禁止的确定性数据损坏源。系统性审查发现 6 处仍用 `str[..N]` 截断**发往 LLM/系统提示词/落盘**的数据，当截断点落在 emoji/扩展区汉字（代理对）中间时会切出孤立代理项，UTF-8 编码后成为 U+FFFD 替换符混入提示词与文件内容。统一改走 `ContextManager.TruncateByRunes`（按码点截断）。

### 🐛 数据路径

- **`Agent.Feedback.cs`（2 处）**：lint 结果 `lintResult[..1500]` 与自动测试失败输出 `fullOutput[..2000]` 注入工具结果前改按码点截断——测试输出常含 ✔/✘/❌ 等 emoji，切半后污染自动修复闭环的上下文
- **`Memory/SemanticMemory.cs`**：检索记忆摘要 `snippet[..300]` 注入系统提示词前改按码点截断
- **`Memory/EmbeddingStore.cs`**：embedding 输入 `text[..8000]` 改按码点截断，避免切半文本生成错误向量（原静默 catch 吞掉）
- **`Infra/OfficeExtractor.cs`（3 处）**：DOCX/XLSX/PPTX 提取结果 `result[..maxChars]` 改按码点截断（对比 `FetchTool` 已正确用 `TruncateByRunes`），文档内 emoji/扩展区汉字不再损坏
- **`Agent.Commit.cs`**：LLM 生成的提交信息 `msg[..72]` 改按码点截断，避免 `feat: add 🎉` 之类消息落成带 `�` 的 commit

### ✅ 测试

新增 `TestV0716RuneSafeTruncation`（3 项断言）：构造最小 DOCX（段落含 emoji），`maxChars=3` 恰好落在代理对中间，断言提取结果无孤立代理项、emoji 完整保留、截断说明正常追加。测试总数 3252 → 3255。

## v0.71.15 (2026-08-17) — 上下文压缩界面指示（Web + TUI 动画）

上下文压缩此前在 Web 版只有聊天流里的一行 `🔄 [1/3] ...` 文本、TUI 版动态栏的进度条在压缩结束后残留陈旧标签，用户感知弱。本轮补齐两端「压缩中」的界面指示：动画 + 完成后消失。

### ✨ 界面

- **Web 版压缩指示条**：`WebAssets.cs` 在聊天区顶部新增浮动胶囊指示条——旋转 spinner + 阶段文案（裁剪工具输出 / 正在摘要旧对话 / 紧急压缩）+ 进度条，`@keyframes cspin` 旋转动画 + 进度条宽度过渡，收到 `done` 后淡出消失（`opacity` 过渡 + `translateY` 上浮）
- **Web 版事件链路**：`ContextManager` 新增静态 `CompressFinished` 事件（`MaybeCompressAsync` 的 `finally` 触发，无论是否实际压缩）；`WebChat.Start` 订阅 `CompressProgress`/`CompressFinished`，经 `AsyncLocal<int> _currentSlot` 把进度按槽位路由 `BroadcastTo(slot, "compress", ...)`，新增纯函数 `SerializeCompress(layer, label, percent, done)`；前端 `compress` SSE 监听 → `showCompress()`
- **TUI 版修复残留标签**：`ChatScreen.SyncDynamicBar` 压缩完成清理 `ProgressPercent` 时同步清空 `ProgressLabel`，避免压缩结束后动态栏右段残留 `[L3] 压缩完成` 覆盖常驻上下文占用 `%`（原 `OnCompressProgress` 只在 `IsCompressing` 时写标签，结束时无事件清空）

### ✅ 测试

新增 `TestV0715CompressIndicator`（8 项断言）：`SerializeCompress` 纯函数载荷（done/layer/label/percent 透传）+ `CompressFinished` 事件触发 + `IsCompressing` 复位 + 极小上下文不压缩。测试总数 3244 → 3252。

## v0.71.14 (2026-08-17) — Retry-After 头解析负数回退

继续清扫 LLM 客户端边界。本轮修复一个 429 限流重试的确定性边界 bug：`Retry-After` 响应头为负数时，退避延迟计算为负，`Task.Delay` 抛异常。

### 🐛 LLM 重试

- **`ParseRetryAfter` 负数秒未回退**：`LLM.cs` 解析纯数字 `Retry-After` 头时直接 `(long)seconds * 1000` 再 `Math.Min`，负数会得到负延迟——调用方 `Task.Delay(负)` 抛 `ArgumentOutOfRangeException`（`Retry-After: -1` 更会让 `Task.Delay(-1000)` 直接抛、而非「无限等待」语义），整次 429 重试链被异常打断而非回退默认退避；改为 `seconds < 0` 时返回 `null` 走默认指数退避，与 HTTP-date 分支的 `delay > 0` 判断保持一致

### ✅ 测试

新增 `TestV0714RetryAfter`（8 项断言）：正整数→正延迟、`0`→立即重试、负数/`-1`→回退 null、无头→null、非数字→null、过去时间→null、未来时间→正延迟。`ParseRetryAfter` 由 `private` 提为 `internal` 便于自测。测试总数 3236 → 3244。

## v0.71.13 (2026-08-17) — 槽位 Cts 清理顺序竞态修复

继续清扫多 Agent 并发场景的数据竞态。本轮修复一个确定性竞态：后台槽位任务结束时，`IsBusy=false` 与 `Cts` 原子摘除的顺序错误，会让新任务的取消令牌被旧任务误释放。

### 🐛 并发

- **`StartSlotTask` finally 清理顺序竞态**：`Program.Repl.cs` 后台任务 finally 原先把 `slot.IsBusy = false` 写在 `Interlocked.Exchange(ref slot.Cts, null)?.Dispose()` **之前**。二者之间 UI 线程读到 `IsBusy == false` 会启动新任务写入新的 `Cts`，此时旧任务的 `Interlocked.Exchange` 会把新任务的 `Cts` 摘走并 Dispose——新任务从此无法被 Esc 中断（Esc 读到 null 即 no-op）。改为先摘除 `Cts` 再置 `IsBusy=false`，使「检查 IsBusy 启动新任务」必然发生在旧任务 Cts 清理完成之后，杜绝误释放

### ✅ 测试

新增 `TestV0713CtsLifecycle`（6 项断言）：`AgentSlot.Cts`/`IsBusy` 独立字段语义 + `Interlocked.Exchange` 原子摘除（并发摘除恰好一个取到非 null、字段归 null、取到者是原实例）。测试总数 3230 → 3236。

## v0.71.12 (2026-08-17) — Agent.Messages 线程安全（锁内读写 + 快照读）

继续清扫多 Agent 并发场景的数据竞态。本轮聚焦 `Agent.Messages`（`List<JNode>`）的并发访问：主循环线程流式追加消息，与 Web 序列化 / 退出自动保存 / 会话命令 / 历史命令等外部线程的遍历并存，非线程安全的 `List` 在「遍历中并发 Add」时抛 `InvalidOperationException`。

### 🐛 并发

- **`Agent.Messages` 非线程安全访问**：`Agent.cs` 把 `Messages` 从公开字段改为 `_messages` 后备字段 + 属性，新增 `MessagesLock` + 封装方法 `AddMessage`/`RemoveMessageAt`/`InsertMessage`/`SnapshotMessages`/`ReplaceMessages`/`ClearMessages`（锁内读写，快照 `ToList` 供外部只读遍历）
- **内部写点统一走锁**：`Agent.cs`/`Agent.Loop.cs`/`Agent.Tools.cs` 的 `Messages.Add/Insert/RemoveAt/Clear` 全部替换为封装方法
- **外部写点改封装**：`Program.cs`（恢复会话）/`Program.Repl.cs`（`/resume`）/`Program.Commands.cs`（`/session`、新建会话）/`WebChat.Commands.cs`（`/reset`、`/session load`）/`WebChat.cs`（`/sessions/new|load`、`/fileref`）/`SessionCommand.cs`（load/resume）的 `Clear+AddRange` 全部合并为 `ReplaceMessages`、`Clear` 改 `ClearMessages`、`Add` 改 `AddMessage`
- **外部读点改快照**：`WebChat.Serialization.cs`（`SerializeHistory`/`HasHistory`）、`Program.Repl.cs`（退出自动保存/崩溃保存/紧急退出/token 估算）、`Program.Commands.cs`（`/history` 索引遍历、`/loop` 尾消息）、`WebChat.Commands.cs`（`/session save`）、`SessionCommand.cs`（save）、`HistoryCommand`/`ExportCommand`/`CompactCommand`/`StatsCommand`/`AgentTool.BuildParentContext` 全部改为 `SnapshotMessages()` 锁内快照，杜绝遍历中并发 Add 抛异常

### ✅ 测试

新增 `TestV0712MessagesThreadSafety`（7 项断言）：封装方法功能正确性（追加/快照副本/插入/删除/整体替换/清空）+ 并发压力（写线程 `AddMessage` ×5000 与读线程 `SnapshotMessages` ×5000 并发不抛异常）。测试总数 3223 → 3230。

## v0.71.11 (2026-08-17) — 并发安全 4 项（锁一致性/volatile/定时器/原子累加）

继续清扫多 Agent 并发场景的数据竞态，修复 4 个确定性并发 bug。

### 🐛 并发

- **`_allSessionFiles` 锁不一致**：`Agent.Tools.cs` 里 `_allSessionFiles.Add(path)` 在 `lock (_modifiedFiles)` 内，而 `Agent.cs:620` 读取时用 `lock (_allSessionFiles)` —— 两个不同锁对象无法互斥，文件清单读写竞态；改为 `_allSessionFiles` 的写入也单独用 `lock (_allSessionFiles)`
- **`_activeSlot` 非 volatile**：`Program.cs` 的 `_activeSlot` 被主线程写、后台槽位/命令线程读（`ActiveSlotIndex`），非 volatile 存在可见性风险；改为 `volatile int`
- **`WatchMode` 定时器 Dispose 竞态**：`Stop()`/`Dispose()` 在锁外操作 `_debounceTimer`，与 `OnFileChanged` 的锁内访问不互斥，Stop 后回调仍可能重建 Timer 泄漏；`Stop()` 的 Timer 清理移入 `lock (_lock)`，删掉 `Dispose()` 里冗余的锁外 `Dispose()`，`_disposed` 改为 `volatile bool`
- **`FallbackLLM.TotalSpent` 非原子累加**：`TotalSpent += cost` 是读-改-写，多槽位 Agent 并发回退时丢增量、预算判断失真；引入 `_stateLock` + `AddSpent`/`BudgetExceeded` 辅助，累加与预算检查原子化，`Reset()` 同锁置零

### ✅ 测试

新增 `TestV0711Concurrency`（3 项断言）：`FallbackLLM` 并发累加 10000×0.5 无丢失、Reset 归零、`WatchMode` 重复 Stop/Dispose 幂等不抛异常。测试总数 3220 → 3223。

## v0.71.10 (2026-08-17) — 输入控件代理对安全（光标移动/删除）

继续清扫编辑原语层 UTF-16 代理对拆半问题：输入控件的光标移动与字符删除仍逐 `char` 操作，emoji/CJK 扩展 B 会拆半成 U+FFFD。

### 🐛 输入控件

- **`TuiInput` 光标移动拆半代理对**：`MoveCursorLeft`/`MoveCursorRight` 逐 `char` 前进/后退，光标会落在代理对中间；改为检测 `char.IsHighSurrogate(Text[i-1]) && char.IsLowSurrogate(Text[i])` 跳过中间码元
- **`TuiInput` 删除拆半代理对**：`DeleteCharBefore`/`DeleteCharAfter` 固定删 1 个 `char`，退格/Delete 只删半个 emoji；改为按代理对边界算 `delLen`（1 或 2）
- **`TuiTextArea` 光标移动拆半代理对**：`MoveCursorCol` 在 `Math.Clamp` 后仍可能落在代理对中间；改为检测边界后 `newCol += delta > 0 ? 1 : -1` 跳过
- **`TuiTextArea` 删除拆半代理对**：`DeleteCharBefore`/`DeleteCharAfter` 固定删 1 个 `char`；改为按代理对边界算 `delLen`
- **`TuiChatInput` 光标移动/删除拆半代理对**：`MoveLeft`/`MoveRight`/`Backspace`/`DeleteFwd` 同样逐 `char` 操作；改为 `char.IsHighSurrogate`/`char.IsLowSurrogate` 边界检测（`private static` 原语，与 `EditorCore.MoveCursor` 已正确的代理对跳过模式一致）

### ✅ 测试

新增 `TestV0710EditPrimitives`（8 项断言）：TuiInput/TuiTextArea 左右移动跳过代理对中间、退格/Delete 整删 emoji 不产生 `�`。测试总数 3212 → 3220。

## v0.71.9 (2026-08-17) — 全仓 UTF-16 代理对截断清扫 + ANSI/JPEG/边框 确定性修复

继续清扫全仓字符串截断与 UI 渲染边界，修复约 30 个确定性 bug，补齐单元测试。

### 🐛 UTF-16 代理对截断（续）

任意索引 `[..N]`/`[^N..]`/`Substring` 切片在 emoji/CJK 扩展 B 处拆半代理对产生 U+FFFD。本轮把 Program 层（Commands/Repl/Output）、Watch 模式、文件操作工具（ReadFile/EditFile/MultiEdit/Lint/FindReplace/Agent）、记忆与会话（StructuredMemory/SessionManager/MemoryRetrieval/ProjectContext）等约 23 处切片统一改为 `ContextManager.TruncateByRunes`/`TruncateTailByRunes`。

### 🐛 LSP 客户端

- **JSON-RPC `Content-Length` 按字符数而非字节数**：`ReadResponse` 逐字符读正文，多字节 UTF-8（中文/emoji）内容长度错位导致粘包/丢包；改为从 `BaseStream` 按字节读头 + 正文，`Encoding.UTF8.GetString` 解码
- **`initialized` 通知后误读响应**：`initialized` 是单向通知、无响应体，却 `await ReadResponse` 阻塞到超时；移除该次读取
- **参数拼接 + 跨平台路径**：`string.Join(" ", args)` 遇含空格路径被拆散，改用 `ArgumentList` 逐参数；`file://` URI 改用 `new Uri(Path.GetFullPath()).AbsoluteUri` 跨平台

### 🐛 渲染 / 终端

- **`AnsiString.Strip`/`TruncateByWidth` CSI 终止符只认 `m/H/J/K`**：`\x1b[?25l`（隐藏光标）等序列未在 `m/H/J/K` 终止，把后续真实文本一并吞掉；改为按 CSI 最终字节区间 0x40–0x7E 判定，并跳过 `ESC[` 引入符
- **`BoxBuffer` 负宽度崩溃**：`new string(h[0], Width - 2)` 在 `Width < 2` 时抛 `ArgumentOutOfRangeException`；改为 `Math.Max(0, Width - 2)`
- **`BoxBuffer` 省略号 off-by-one**：`TruncateByVW(text, maxLen-1) + "…"` 未预留「…」两列，改为 `maxLen-2`
- **双省略号**：`TuiHelper.TruncateByWidth` 已自带省略号，`TuiToastQueue`/`TuiDynamicBar` 又 `+ "…"` 产生「……」；去掉多余省略号
- **省略号未预留宽度**：`InlinePermission`/`SessionPicker`/`DiffPreview` 私有 `TruncateByVW` 追加「…」但不预留两列，改为先判 `DisplayWidth` 快速返回 + 循环预留 2 列
- **`TuiRichEditor` 宽度判据 `>127`**：重音字符（é/ñ）被判为宽字符，改用 `TuiHelper.DisplayWidth`（委托 `AnsiString.CharWidth` 唯一真源）

### 🐛 图像解码 / 配置 / 杂项

- **`JpegCodec` DHT 表越界**：`dcTables`/`acTables` 长度 4，但 DHT 表 id 字段 0–15，构造表 id≥4 的 JPEG 触发 `IndexOutOfRangeException`；扩到 16
- **`--max-requeue` 配置丢失**：命令行参数在 `_config = Config.FromEnv()` 之前写入 `_config` 字段、随后被单例覆盖；改为先解析为局部变量、在 `FromEnv()` 后落回 `Config.Instance`
- **`LLM` Retry-After 整数溢出**：`seconds * 1000` 用 `int` 相乘溢出，改为 `long` 中间量后钳制
- **`ProjectContext` `.git` 仅识别目录**：worktree/submodule 的 `.git` 是文件而非目录，漏检版本库；同时识别 `File.Exists`

## v0.71.8 (2026-08-17) — 工具/TUI/UTF-16 代理对 26 项确定性修复

系统性审查工具、TUI 控件、基础设施与全仓字符串截断，修复 26 个确定性 bug，补齐单元测试。

### 🐛 工具（Tools）

- **`StructTodoTool` 解除阻塞污染标题**：每次解锁都在 `Title` 后追加 `[解除阻塞: id]`，反复累加；移除污染行，并为 `TodoItem` 补 `Description` 字段（读/写与 `TodoTool` schema 对齐）
- **`TodoTool` 状态硬编码**：列出时 `状态=pending` 写死，改为 `状态={todo.Status}` 反映真实状态
- **`FetchTool` 截断长度谎报**：截断后消息引用已截断的 `text.Length`，改为截断前记录 `originalLen` 再引用
- **`DocTool` 缓存非线程安全**：`Dictionary` 缓存多 Agent 并发读写竞态，改为 `ConcurrentDictionary`

### 🐛 TUI 控件 / UI

- **`TuiProgress` 负宽度越界**：`barW < 0` 时 `Math.Clamp(filled, 0, barW)` 抛异常，先钳 `barW = 0`
- **`TuiControl` 字符推进按 UTF-16 码元**：`charIdx++` 遇 emoji 拆半，改为 `charIdx += rune.Utf16SequenceLength`
- **`AnsiString.TruncateByWidth` 拆半代理对**：逐 `char` 拼接切半 emoji → U+FFFD，改为按 `Rune` 拼接 + 码元补齐
- **`BoxBuffer` 宽度判定与真源分叉**：`VW`/`TruncateByVW` 用 `r.Value > 127` 判宽，与 `AnsiString.CharWidth` 真源不一致，统一委托真源
- **`TuiScrollbar` 拖拽坐标未换算**：`ev.MouseY` 直接当相对坐标，缺 `GetAbsoluteY()` 偏移
- **`TuiMarkdown` 彩虹段拆半代理对**：`BuildRainbowSegments` 逐 `char` 遍历，改为 `EnumerateRunes`
- **`TuiToastQueue` 跨线程可见性**：`_current` 静态字段非 volatile，Toast 在 UI/后台线程间可能读到旧值，加 `volatile`
- **`TuiComboBox` 过滤后索引错乱**：渲染/Home/End 用未过滤索引，过滤后选中错位，新增 `ActiveIndices` 统一
- **`TuiTable` 单元格溢出错位**：超宽单元格不截断破坏表格对齐，新增按列宽截断（含 ANSI/Spectre 标记感知的 `TruncateMarkup`）

### 🐛 基础设施（Infra）

- **`Logger.FlushAll` 锁重入死锁**：外层 `lock(_lock)` 内再进 `FlushLocked` 触发 `LockRecursionException`，去掉外层锁
- **`LogMetrics` 缩容残留写指针**：`RingCapacity` 缩小只删元素不重置 `_ringIndex`，后续 `Record` 越界写，补 `_ringIndex = 0`
- **`BmpCodec` int.MinValue 溢出**：`Math.Abs((int)height)` 遇 `int.MinValue` 溢出/回绕，改为 `long` 取绝对值 + 超界抛 `FormatException`
- **`TrueTypeFont` 拆半代理对**：`Measure`/`DrawString` 逐 `char` 取字形，改为 `EnumerateRunes`
- **`IdGenerator` 取模偏差**：`_rng.GetInt32` 实例调用误用（静态方法）+ 模运算取模偏差，改为 `RandomNumberGenerator.GetInt32(len)`

### 🐛 UTF-16 代理对截断（全仓清扫）

任意索引处 `[..N]`/`[^N..]` 切片在 emoji/CJK 扩展 B 处拆半代理对，产生 U+FFFD。新增 `ContextManager.TruncateTailByRunes`，并把约 20 处切片（Tree/Export/Fetch/Doc/Git/Ps/Bash/GitPR/Memory/AskUserQuestion/NotebookEdit/TranscribeAudio/Trajectory/BackgroundTask/WorkReporter 等）改为按码点截断。

### 🐛 共享记忆

- **`SharedMemoryManager` 快照目录与检出目标不一致**：拉取前快照用 `MemoryDir`（槽位子目录）、检出目标却是共享目录，导致新增文件误判为「更新」；快照统一用 `SharedMemoryDir`

## v0.71.7 (2026-08-17) — 基础设施/文件工具/会话/批处理/编辑器 21 项修复

系统性审查基础设施、文件操作工具、会话/辅助模式、批处理与 TUI 编辑器，修复 21 个确定性 bug，补齐单元测试。

### 🐛 基础设施（Infra）

- **`.gitignore` 目录规则产生 `//` 双斜杠**：`FileIgnoreManager.BuildRegex` 对 `logs/` 这类尾斜杠目录规则先拼接再补 `/`，正则出现 `//`，导致 `logs/` 规则既匹配不到目录本身、也匹配不到目录内容；拆分「锚定/中间目录段/尾斜杠后缀」逐段拼接修复，`logs/output.txt`、`logs/deep/file.txt` 现在正确命中且不误伤 `catalog.txt`
- **Hook exit 2 + JSON 无 decision 不阻断**：`HooksManager.ParseHookOutput` 在 JSON 分支下若 `exitCode == 2` 且 `Decision` 为空，未落回 block；补上 `Continue=false + Decision="block"`，同时不覆盖显式 `approve`/`deny`

### 🐛 文件操作工具

- **`RmTool` 非 Windows 空 `SpecialFolder` 前缀**：`Environment.GetFolderPath(Windows/System)` 在非 Windows 返回 `""`，`ProtectedPaths` 空串 `StartsWith` 恒真 → 一切路径都被判为受保护；跳过空路径
- **`GlobTool` `*.*` 漏掉无扩展名文件**：`Directory.GetFiles(root, "*.*")` 在 Unix 不匹配无点文件，改为 `"*"`
- **`FindReplaceTool` 替换串被当正则替换模式**：`regex.Replace(content, replacement)` 会把 `$1`/`${name}` 解释为捕获组引用，改用 `MatchEvaluator` 字面替换
- **`EditFileTool`/`MultiEditTool` 三处**：① `Encoding.UTF8`（`throwOnInvalidBytes:false`）静默吞非法字节，改为 `new UTF8Encoding(false,true)` 严格校验；② 写回时 `Replace("\n","\r\n")` 在已有 CRLF 时产生 `\r\r\n`，改为先归一化 LF 再转 CRLF；③ CRLF 文件编辑保持换行符
- **`CpTool`/`MvTool` 目录复制/移动进自身子树**：目标落在源目录内部时 `Directory.Move`/递归复制无限循环，加前缀检测提前拒绝
- **`ReadFileTool` 尾随换行行数虚增**：`text.Split('\n')` 对 `"a\nb\n"` 产生 3 元素、行数报 3 实为 2；去掉末尾空元素
- **`GrepTool` 单目录不可访问导致整树漏搜**：`Directory.GetFiles(..., AllDirectories)` 一个异常整棵放弃，改为逐目录递归、每目录独立 try/catch

### 🐛 会话/辅助模式（Watch / Fallback / Session）

- **`WatchMode` AI? 注释 off-by-one**：`trimmed[2..]` 漏掉 `AI?` 第 3 字符 `?`，指令前缀残留问号
- **`WatchMode` 忽略目录按绝对路径段匹配误伤**：用绝对路径 `dir` 逐段比对，祖先目录名（如 `/Users/x/target/...`）被误判为忽略目录；改为相对监视根的路径段
- **`WatchMode` 块注释在不适配语言误判**：`.py` 等文件里含 `/*` 的字符串/URL 被当 C 块注释吞掉后续行；`/* */`、`<!-- -->` 检测改为仅适用语言开启
- **`FallbackLLM.MaxBudget` setter 死代码**：setter 写私有静态 `_maxBudget`、读走 `Config`，`Config.FallbackMaxBudget = value` 永不生效；setter 直接落到 `Config`
- **`SessionManager.ListSessions` 跨目录分页错序**：先按目录取 `Skip/Take` 再拼接，跨 `sessions/` 与旧目录时排序/去重失效；改为全量收集 → 按 `saved_at` 降序 → `seen` 去重 → 统一分页

### 🐛 批处理 / 编辑器

- **`BatchRunner.CloneRepo` 无超时**：同步 `GitRunner.Run` 的 `WaitForExit` 无超时，`git clone` 因网络/认证问题永久挂起卡死整个批任务；改为 `CloneRepoAsync` + `RunAsync` 可取消，超时杀进程树
- **`DiagnosticManager` 并发访问非线程安全字典**：后台 lint 写、UI 线程读共享 `Dictionary`，改为 `ConcurrentDictionary`
- **`DiagnosticManager` exit 0 时丢弃 warning**：`StartsWith("✅")` 把「检查通过」整体跳过，但 stderr 里的 warning 会拼进 `combined`；改为只跳过「无法运行 linter」，继续解析 warning
- **`DiagnosticManager` PHP 严重级按整段输出判 warning**：`output.Contains("warning")` 会让一条 error 之外的无关 warning 把 error 也标成 warning；改为按每条匹配的 `error|warning` 前缀判定
- **`DiagnosticManager` Rust 错误定位按索引配对错位**：`note`/`help` 注解也带 `-->`，`errMatches[i]` 配 `locMatches[i]` 会让后续错误错位到注解位置；改为取该 error 之后最近的 `-->`
- **`EditorCore` 光标/删除切半 emoji 代理对**：`Backspace`/`Delete`/`MoveCursor` 按单 `char` 操作会在 emoji/CJK 扩展 B 中间切断；改为代理对感知，删除/移动整码点
- **`Syntax` 高亮忽略字符串转义**：`line.IndexOf('"', i+1)` 把 `\"` 当结束引号提前截断；新增 `FindStringEnd` 跳过反斜杠转义

## v0.71.6 (2026-08-17) — 上下文压缩/LLM 流式/Web 并发 11 项修复

系统性审查 `ContextManager`/`LLM`/`WebChat`，修复 11 个确定性 bug：上下文压缩省略行 off-by-one、UTF-16 切片切半代理对、LLM 400 回退死代码、流式工具调用重复触发、Web 版断连泄漏/槽位竞态。

### 🐛 上下文压缩（ContextManager）

- **裁剪省略行 off-by-one**：`SnipToolOutputs` 的 `lastWritten` 初值 `-2` 导致每条被裁剪输出开头多一句虚假的「省略 1 行」，改为 `-1`
- **UTF-16 切片切半代理对**：`flat[..20000]`/`text[..1000]`/`trimmed[..200]` 按码元切片会在 emoji/扩展区汉字（代理对）中间切断，经 JSON 编码后变成 U+FFFD 污染发往 LLM 的文本；新增 `TruncateByRunes` 按码点截断，与 `EstimateTokensText` 的 rune 感知一致
- **裁剪完成提示语义**：进度消息把「剩余容量」当「节省量」上报（`-(MaxTokens - current)`），改为上报实际节省量（裁剪前 − 裁剪后）

### 🐛 LLM 流式解析（LLM）

- **400 回退死代码**：`catch (HttpRequestException) when (StatusCode == BadRequest)` 永不命中——`CallWithRetryAsync` 对 4xx 返回响应而非抛异常，导致不支持 `stream_options.include_usage` 的端点返回 400 时被当成 SSE 流解析、静默返回空响应。改为在返回后检查状态码、400 则去掉 `stream_options` 重试一次（并顺带清理未使用的 `request` 变量）
- **流式工具调用去重**：`onToolCall` 在参数形成完整 JSON 后每次 delta 都会触发，无「该 index 已触发」守卫；新增 `firedToolCalls` 去重，防止同一工具调用被重复执行
- **日志预览切半代理对**：`ParseArgs` 失败日志的 `json[..200]` 改为 `TruncateForLog` 按码点截断

### 🐛 Web 并发（WebChat）

- **SSE 断连无检测**：服务端从不读流，`Closed` 仅在写失败时置位，客户端关标签页后若无后续广播，连接永久阻塞在 `Closed.Task` 上泄漏线程/连接槽位；改为 `Task.WhenAny` 同时监听底层流 EOF
- **`_clientSlot` 永不清理**：客户端断开只移除 `_clients` 不清 `_clientSlot`，字典无界增长 + 旧 clientId 永久占用槽位，反复刷新后新客户端回退槽位 0 串扰；断开时按「无其他连接复用该 clientId」条件清理
- **Interrupt 启动窗口丢失**：`IsBusy=true` 与 `slot.Cts` 赋值非原子，窗口内到达的中断被 `Exchange` 取到 null 丢弃；`Interrupt` 与 `StartSlotTask` 改为共享 `StartLock`
- **EnsureSlot 异常卡死槽位**：`IsBusy=true` 先于 `EnsureSlot` 且无回滚，`EnsureSlot` 抛异常时槽位永久 busy；改为异常时广播失败且不置 `IsBusy`
- **EnsureSlot 无锁双建**：多路由并发首建产生双 Agent 相互覆盖；加 `AgentLock` double-checked locking
- **BindClientSlot 不校验占用**：两个页面可绑到同一槽位互看对方对话；改为校验占用、被占用时拒绝并报错

## v0.71.5 (2026-08-17) — 提问多选布尔解析修复 + 聊天角色标识中文化

修复 `ask_user_question` 多选参数在 JNode 路径下永远解析为 false 的问题，并把聊天/导出里的角色标识（User/Assistant/System/Tool）统一改为中文。

### 🐛 修复

- **`AskUserQuestionTool` 多选布尔解析**：`ParseOneQuestion(JNode)` 用 `AsString() == "true"` 判断 `multiSelect`，但 JSON 布尔值（`"multiSelect": true`）的 `AsString()` 返回 null（`JKind.Bool` ≠ `JKind.String`），导致多选永远解析为单选。改为优先 `AsBool()`、兜底字符串 `"true"`（对齐 `MultiEditTool` 的处理）

### ✨ 聊天角色标识中文化

- **TUI 聊天角色标签**：`TuiListItem` 的角色头从 `You`/`Assistant`/`System`/`Tool` 改为 `用户`/`智能体`/`系统`/`工具`
- **对话导出角色名**：`ExportTool` 的 Markdown/HTML 导出标题中的英文 role（`user`/`assistant`/`system`/`tool`）统一改为中文

## v0.71.4 (2026-08-17) — 并发竞态修复 + LLM 提问对话框 + diff 滚动

修复多槽位/并行子智能体下的几处竞态，新增 LLM 提问对话框（`ask_user_question`）与 diff 对话框代码区滚动，并清理冗余文档。

### 🐛 并发竞态修复

- **AgentTool 独立实例**：每个 Agent 构造时持有独立的 `AgentTool` 实例（不再共享单例），避免 `ParentAgent` 被后构造的子智能体覆写（AgentId 继承失效、花费归并到错误实例、跨槽位重绑竞态）
- **WebChat 双启动锁**：`StartSlotTask` 加 `StartLock` 串行化 check-then-act，杜绝同槽位两个并发请求同时通过 `IsBusy` 检查导致双 Agent 并发 + 第二个 CTS 覆盖第一个（泄漏）
- **Esc 中断 CTS 原子摘除**：`Program.Repl` 中断槽位改用 `Interlocked.Exchange` 摘除 CTS，与后台 `finally` 的 `Dispose` 对齐，消除「读到非空 → 后台 Dispose → Cancel 抛 ObjectDisposedException」竞态
- **LLM token/请求计数原子累加**：`TotalPromptTokens`/`TotalCompletionTokens`/`TotalRequests` 改 `Interlocked` 累加，并行子智能体（`Task.WhenAll`）并发归并到同一父实例不丢增量；新增 `AddUsage` 供自测/归并原子注入

### 🛡️ 健壮性

- **鼠标输入默认关闭**：`TuiManager.MouseEnabled` 默认 false（鼠标定位尚未调好），设 `WAYCODER_MOUSE=1` 即可重新启用；`InputManager`/`TuiManager.Enter`/`Program.Repl` 统一按此开关
- **WrapText 省略号修复**：超行截断时正确预留省略号宽度并在末行补「…」，不再出现省略号被截断或吞掉最后一行
- **权限确认改模态弹框**：`ShowInlinePermission` 迁移为 `ShowPermissionDialog`（`TuiDialog.Permission` 模态框，Y=允许 A=全允 N/Esc=拒绝），替代旧行内权限块，详情超 800 字符自动截断

### ✨ LLM 提问对话框 + diff 滚动

- **`TuiDialog.Ask`**：标题独占一行（粗体）→ 消息正文（1~5 行，超出省略号）→ 选项列表（单选 ▶ / 多选 ☑，最多 9 行可滚动）→ 底部按钮，高度按内容精确计算；`UxHelper.Ask` 统一 TUI 弹框 / 非 TUI 编号菜单回退，`AskUserQuestionTool` 接入
- **diff 对话框代码区滚动**：`DiffPreview` 的代码对比区支持鼠标滚轮（每格 3 行）+ 右侧滚动条，内容超出屏幕时可滚动查看

### 🧹 文档清理

- 删除 `AGENTS.md`（旧 Agent Guide，已由 `CLAUDE.md` 取代）与误提交的临时文件

## v0.71.3 (2026-08-16) — TUI 会话记录按槽位隔离

把 Web 版「会话记录按槽位隔离」同步到终端 TUI：每个槽位（F1-F10）各自保存/加载/列出/删除自己的会话记录，互不串扰。

### ✨ 会话记录按槽位隔离（TUI）

- **会话管理器按当前槽位**：`SessionPicker.Show` 增加 `slot` 参数，`ListSessions`/`RenameSession` 按当前槽位作用；Ctrl+S 打开时只看到/操作本槽位会话
- **`/session` 命令按当前槽位**：`SessionCommand` 的 `list`/`save`/`load`/`resume` 全部加 `Program.ActiveSlotIndex`，各槽位独立保存/加载/恢复
- **退出自动保存按槽位**：`AutoSaveSession`/`AutoSaveException`/`PanicExit` 从 `_auto`/`_auto_slotN` 后缀改为 `SaveSession(..., "_auto", slot)`，物理隔离到 `sessions/slot{N}/` 子目录
- **当前会话 ID per-slot 化**：`_currentSessionId` 单值改为 `_currentSessionIds[10]` 数组，每槽位各自标记当前会话（Ctrl+S 切换/删除只影响本槽位）
- **切换会话只改本槽位模型**：Ctrl+S 切换会话从改全局 `_config.Model`/`_llm.Model` 改为 `_agent.LlmClient.Model`，不再污染其他槽位的默认模型
- **恢复向后兼容**：`TryRestoreSession`、`--resume`、`-c`、`/session resume` 恢复 `_auto` 时「槽位 0 优先，回退全局目录」，旧版本存全局的会话仍可恢复

### 🧪 自测

- 新增 `SessionSlot` 断言：`_auto` 存槽位 2 可恢复、跨槽位（0/3）不可见

## v0.71.2 (2026-08-16) — Web 会话记录按槽位隔离

每个浏览器页面（= 一个槽位 = 一个「虚拟用户 + 智能体」）拥有独立的会话记录：单端口下各页面只看到、保存、加载、删除自己槽位的会话，互不串扰。

### ✨ 会话记录按槽位隔离

- **`SessionManager` 增加槽位维度**：`SaveSession`/`LoadSession`/`ListSessions`/`DeleteSession`/`RenameSession`/`DeleteAllSessions` 均新增可选 `slot` 参数（默认 -1 = 全局共享，终端 TUI 沿用旧行为）；传 0-9 时记录写入 `~/.waycoder/sessions/slot{N}/` 子目录，各槽位物理隔离
- **槽位隔离模式不回退旧目录**：`LoadSession`/`ListSessions` 在 slot≥0 时只读该槽位子目录，不扫描全局目录或 `.corecoder` 旧目录，杜绝跨槽位读取
- **`DeleteAllSessions(slot)` 只清空该槽位**：清空按钮按当前页面槽位作用，不影响其他页面的会话记录
- **Web 层贯穿槽位**：`HandleCommand`/`WebSessionText` 增加 `slot` 参数；`/session save|load|list`、`/sessions`（GET 列表）、`/sessions/load|delete|rename|clear` 全部按当前客户端绑定的槽位作用
- **会话广播按槽位路由**：`BroadcastAll("sessions")` 改为 `BroadcastTo(slot, "sessions", SerializeSessions(slot))`，会话列表 SSE 事件只发给本槽位页面；前端 `fetchSessions`/删除/重命名/清空请求统一走 `?client=` 标识槽位

### 🧪 自测

- 新增 `SessionSlot` 断言组：槽位 0/1 各自只列自己会话、跨槽位加载返回 null、同槽位加载命中、`SerializeSessions(0|1)` 各自隔离、`DeleteAllSessions(0)` 只清空槽位 0 而槽位 1 保留

## v0.71.1 (2026-08-16) — Web 停止按钮按页面隔离 + 发送按钮改版

Web 版停止按钮修复为「每个浏览器页面只作用于自己的 agent」：后端从单活动槽位 + 单取消令牌 + 单顺序循环，改为每页面（SSE 客户端）绑定一个槽位、各槽位独立并发执行；发送按钮改为圆形 + 纸飞机图标。

### 🐛 停止按钮按页面隔离（根因修复）

- **客户端槽位绑定**：前端生成随机 `clientId`，所有请求走 `?client=<id>`（含 `/events` SSE 连接）；后端 `ResolveSlot` 把新客户端分配到空闲槽位（0-9）并复用
- **槽位分配跳过已绑定槽位**：`ResolveSlot` 分配空闲槽位时同时排除「已被其他客户端绑定」的槽位（不能只看 `Agent`/`IsBusy`——新客户端分配后不会立刻建 Agent，否则多个页面被分到同一个槽位互相干扰）
- **切换槽位同步 SSE 客户端槽位**：`BindClientSlot` 除更新 `_clientSlot` 字典外，同步改写该页面已建 `SseClient.SlotIndex`，否则切换后 `BroadcastTo(新槽位)` 匹配不到它 → 收不到 token/停止态、停止按钮失效
- **每槽位独立执行**：`WebSlot{Agent,IsBusy,Cts}` + `StartSlotTask` 后台 `Task.Run` 并发跑 `ChatAsync`（镜像终端 `Program.Repl.cs` 槽位模型），删掉全局 `_activeSlot`/`_roundCts`/`_input`/`MainLoopAsync`
- **作用域广播**：`BroadcastTo(slot)` 只写绑定该槽位的客户端；`BroadcastAll` 保留给全局事件（sessions）；`BroadcastStateForAll` 按各客户端自己的槽位刷新 state
- **交互桥 `ask` 按槽位路由**：`AsyncLocal<int> _currentSlot`（AOT 安全，项目已用于 bash cwd 跟踪）在 `StartSlotTask` 内 set，`WaitAnswerAsync` 据此只把提问发给发起该轮任务的页面
- **`OnSse` 委托加 `HttpRequest`**：SSE 连接建立时能读到 query 里的 `client` 标识

### ✨ 槽位切换保留状态

- **输入草稿按槽位记忆**：前端 `slotDrafts` 缓存每个槽位未发送的输入，切换槽位时保存当前、恢复目标，输入框内容互不串扰（切换时也重置 `isBusy`，避免残留旧槽位的停止态拦截发送）
- **聊天内容按槽位隔离**：每槽位独立 `Agent.Messages`，切换时从后端回放该槽位历史

### ✨ 发送按钮改版

- **圆形按钮**：`#send` 的 `border-radius` 13px → `50%`（46×46 成圆）
- **纸飞机图标**：`✈️` emoji 换成 Material「send」内联 SVG 纸飞机（`fill=currentColor` 随主题着色），忙态仍为 ⏹ 圆钮

### 🧪 自测

- 新增 `ParseClientQuery`（query 取 client）/ `PickFreeSlot`（槽位分配）纯函数断言
- 新增端点冒烟：两个 `?client=` 分配不同槽位、`/slot` 切槽后互不影响

## v0.71.0 (2026-08-16) — 安全加固 + 多槽位真并行 + 渲染一致性 + 健壮性

一轮系统性审查后的四批修复落地：Web 安全加固、修复「切换槽位后任务停止」的根因（10 个 agent 真正并行）、三端渲染一致性收敛、补齐非 bash 工具取消令牌与资源泄漏。

### 🔒 安全加固

- **XSS 修复**：`renderSuggest` 对文件名等提示框字段做 `escapeHtml` 转义，杜绝恶意文件名注入 HTML/脚本
- **`/shell` 权限确认**：Web 端执行 Shell 命令前走 `PermissionManager.CheckAsync`，非 YOLO 模式下不再无条件放行
- **`/fileref` `/filelist` 路径穿越限制**：`ResolveWithinRoot` 钳制路径于项目根目录内，越界返回「路径超出项目根目录」而非读取任意文件
- **CSRF 纵深防御**：状态变更请求（非 GET）校验 `Origin`（空 Origin 放行 curl/SSE）+ `Sec-Fetch-Site: cross-site` 兜底拦截漏带 Origin 的跨站请求

### 🐛 核心修复：多槽位真并行

- **槽位独立 LLM 克隆**（根因修复）：`GetSlotLlm` 对 `UseGlobal` 槽位返回 `_llm.Clone()` 而非共享 `_llm`，消除并发读写 `ModelOverride`/`_reasoningBuffer`/`_reasoningShown` 的竞态——修复「F1 任务跑到一半，切 F2 再切回 F1 就停了」的问题，10 个 agent 真正互不干扰并行
- **跨槽位文件锁冲突检测**：新增 `Agent.AgentId`（F1-F10）+ `ExecuteToolAsync` 注入 `_agent_id`，`FileLockManager` 按槽位归属识别跨 agent 资源锁定并报错提醒（此前始终按 "main" 判定，跨槽位冲突被误作同源续期静默吞掉）
- **WebChat 并发写安全**：`SseClient` 加 `WriteLock` 串行化 `Broadcast` 写入；`Agent.Messages` 序列化前防御性快照（`ToList`）
- **`_roundCts` 原子化**：`Stop`/`Interrupt`/`MainLoopAsync` 用 `Interlocked.Exchange`/`CompareExchange` 协调取消与释放，杜绝 dispose/cancel 竞态

### 🎨 渲染一致性（三端收敛）

- **裸标记修复**：`Program.Output.cs` 管道输出改用 `MarkupLine`，`«dim»` 标记不再裸写到终端
- **`SpectreToAnsi` 标签集补齐**：新增 `«underline»`/`«italic»`/`«strike»`/`«blue»`/`«magenta»`/`«white»` 等，与 `MapMarkupTag` 对齐；`«bold X»` 复合标签真正带粗体（此前粗体被丢弃）
- **Markdown 表格**：`SplitTableCells` 支持 `\|` 转义竖线（单元格字面 `|` 不误拆）；「先窥探再消费」避免单行 `| 文本 |` 被静默吞掉；无分隔行（表头+数据）也能解析

### 🛡️ 健壮性

- **非 bash 工具取消令牌**：`fetch`/`web_search`/`download`/`git`/`agent` 实现 `ICancellableTool`，中断时真正终止在途 HTTP 请求/子进程/子智能体，并区分「中断」（`OperationCanceledException` 向上抛）与「超时」（返回超时文案）
- **资源泄漏修复**：槽位 `Cts` 用 `Interlocked.Exchange` 原子摘除并 `Dispose`；`BackgroundTaskManager` 完成态任务保留上限 50 自动清除；spinner CTS 释放

### 🧪 自测

- 新增表格转义竖线 / 无分隔行 / 单行竖线不吞行测试
- 新增 `ICancellableTool` 接口实现断言（fetch/web_search/download/git/agent）
- 全量自测通过（3137 通过 / 0 失败）

## v0.70.0 (2026-08-16) — Web 特殊前缀输入 + 停止真中断 + 中间格式渲染

一轮 Web 交互补强：标题栏只显示智能体名、`!` Shell 与 `#` 文件引用两大前缀输入落地、停止按钮真正杀掉 bash 子进程，并确立「中间格式 → 各平台渲染」的着色架构（`«tag»…«/»` 统一表达，CLI/TUI→ANSI、Web→HTML）。

### ✨ Web 交互增强

- **标题栏只显示智能体名**：中间标签从 `智能体: 智能体N` 精简为 `智能体N`（随槽位切换更新）
- **`!` Shell 前缀**：输入 `! <命令>` 直接执行 bash 并显示输出（新增 `/shell` 路由，对标 Claude Code `!`）
- **`#` 文件引用前缀**：输入 `# <路径>` 读取文件注入当前对话上下文；`#` 后实时列出文件/目录补全（新增 `/fileref` `/filelist` 路由）
- **全角/半角归一化**：`／`→`/`、`！`→`!`、`＃`→`#`，中文输入法下前缀照常识别
- **命令提示框**：斜杠命令模糊匹配、`!` Shell 提示、`#` 文件补全，Tab/方向键选中回车确认
- **Shell 输出 tty 配色**：`ansiToHtml` 把 Shell 命令产生的裸 ANSI SGR 转 HTML span（XSS 安全转义），`.shell-output` 独立块等宽显示

### 🎨 中间格式渲染架构

- **`markupToHtml`**（Web 端）：对标后端 `SpectreToAnsi`，把 `«red»`/`«bold»`/`«dim»`/`«underline»`/`«italic»` 等中间格式转 HTML span，颜色值与 `ANSI_FG` 同源 `TuiColors`，三端观感一致
- **接入渲染管线**：`mdToHtml` 行内与 `renderToolOutput` 纯文本分支改用 `markupToHtml`，正文/表格单元格/工具输出里的 `«»` 标记正确着色
- **`/test markup`**：中间格式样例（颜色/文字特征/复合标签/表格内联/代码块原样），验证跨平台渲染；`/test ansi` 保留为「Shell 裸 ANSI 解码」测试

### 🐛 修复

- **停止按钮真中断**：`ICancellableTool` 接口 + 取消令牌贯穿 `Agent`→`BashTool`，中断时杀掉 bash 子进程并抛 `OperationCanceledException`（不吞），修复「停止后 shell 仍在跑」
- **Markdown 表格渲染**：修复转义竖线 `\|` 误拆列（`/perm [ask\|auto\|…]` 拆成 5 列）、`.md-table` 布局；分隔行对齐冒号解析（`:---` 左 / `---:` 右 / `:---:` 居中）应用 `text-align`

### 🧪 自测

- 新增「bash 取消令牌中断长命令」测试（`sleep` 被 800ms 令牌杀掉，耗时 < 5s）
- `TestUiLint` 跳过 `UI/WEB` 目录（HTTP/SSE 层不适用「禁止硬编码 Console/ANSI」约束）
- 全量自测通过（3094 通过 / 0 失败）

## v0.69.0 (2026-08-16) — Web 聊天界面完善

一轮 Web 聊天界面体验收尾：修复设置保存并发写 `.env` 导致的「保存失败」，补全代码块语法高亮、大/小模型双下拉、无 key 弹框、标题栏（智能体 + 版本号）、修改文件增删行统计，并新增 `/stats` `/recent` `/model` 斜杠命令。

### 🐛 修复

- **设置保存失败**：`Config.SaveToEnvFile()` 加 `SaveLock` 串行化读改写，修复 Web 设置面板 `Promise.all` 并发 POST 多项设置时并发写 `.env` 抛 `IOException` → 连接被服务端丢弃（无响应、`fetch` 拒绝）的竞态
- **模型对话框按钮被折叠**：`.model-card` 改 flex 三段布局（标题 + 搜索框固定、列表区 `flex:1; overflow-y:auto`、底部按钮固定），66 个模型不再把「确认/取消」顶出可视区
- **点击遮罩关闭对话框**：backdrop click-to-close（`e.target === e.currentTarget` 才关闭）

### ✨ Web 界面增强

- **大/小模型双下拉**：标题栏拆成「大模型:」+「小模型:」两个 `select`，各自 `onchange` 即时切换；小模型下拉仅列 5 个常用小模型（`deepseek-chat`/`deepseek-v4-flash`/`gpt-5.4-mini`/`gpt-4o-mini`/`deepseek-v4-pro`），下拉加宽 1.5 倍避免长名称截断
- **无 key 弹框**：切换模型时若该供应商无 API Key（且非 local/custom）→ 回退选中并弹 key 输入框，提交后写 `ApiKeyStore` + 持久化并完成切换
- **标题栏三分区**：左=APP 标题，中=当前智能体（`智能体: 智能体N` 随槽位切换更新），右=软件版本号（`Global.Version` 注入）
- **修改文件增删统计**：`EditFileTool.RecordChange` 记录 `+新增/-删除` 行数（`ChangedFileStats`），右栏文件面板显示 `+N/-M`
- **代码块语法高亮**：`highlightCode` 按语言 token 着色（`--tok-*` CSS 变量，明暗主题自适应）

### 🎛 Web 斜杠命令

- 新增 `/stats`（token/费用/请求统计）、`/recent`（最近修改文件）、`/model <名称>`（模糊匹配换模型），与终端命令对齐
- `WebServer` 响应加 `Cache-Control: no-store, no-cache, must-revalidate` 防页面缓存
- Web 模式强制开启 diff 预览（`Config.Instance.DiffPreview = true`）
- `WayCoder.csproj` 排除 `WayEngine/**/*` 构建

### 🧪 自测

- 全量自测通过（0 失败）

## v0.68.0 (2026-08-16) — UI 分类重构 + 大文件拆分

一次清偿两项工程债：UI 代码按 `UI/{Shared,CLI,TUI,GUI,WEB}` 五层分类归档（命名空间对齐目录），并把 4 个 1500+ 行的超大文件拆成 `partial class` 多文件，降低维护成本。附带修复 GitRunner 经典死锁。

### 🗂 UI 分类重构

- 目录五层归档：`UI/Shared`（Terminal/BoxBuffer/MarkdownRenderer/TuiColors/TuiHelper）、`UI/CLI`（Arguments/Commands）、`UI/TUI`（Base/Controls/Custom/Screens/Edit/ToolRenderers）、`UI/GUI`（预留）、`UI/WEB`（Web 服务器）
- 命名空间对齐目录：`WayCoder.Terminal`/`Arguments`/`Commands`/`UI.TuiBase` 等 → `WayCoder.UI.Shared.*`/`Cli.*`/`Tui.*`/`Web.*`
- 130+ 文件 `git mv` 归位，外部调用点经 `using` 同步更新

### ✂️ 大文件拆分（partial class）

- `Program.cs` 2536 行 → 4 文件（`Program` + `Repl`/`Commands`/`Output`）
- `WebChat.cs` 2272 行 → 5 文件（`WebChat` + `WebAssets` + `Serialization`/`Commands`/`Interaction`）
- `ChatScreen.cs` 2237 行 → 5 文件（`ChatScreen` + `Input`/`Dialogs` + `SlotState` + `ChatMsg`）
- `Agent.cs` 1563 行 → 5 文件（`Agent` + `Tools`/`Feedback`/`Commit`/`Loop`）
- 独立类型抽取：`WebAssets.Html`（~1000 行纯 HTML 常量）、`SlotState` 枚举、`ChatMsg` 类

### 🐛 GitRunner 死锁修复

- `Run`/`RunAsync` 并发读取 stdout/stderr，修复「同步先读 stdout、stderr 缓冲写满 → 进程阻塞 → stdout 永不 EOF」的经典死锁
- 此前脏工作树下 `--test` 因大量 git 换行警告填充 stderr 缓冲而挂起

### 🧪 自测

- `SelfTest.Chunk11`（UI Lint + TuiTableList）接入 runner（此前被遗漏未执行）
- UI Lint 白名单更新：终端 ANSI 底层原语（AnsiString/AnsiTty/RenderBuffer/Terminal）+ CLI 参数层（BuiltinArgs）+ ChatScreen.Dialogs
- 自测 13 partial 文件、3070 项全部通过（0 失败）

## v0.67.0 (2026-08-16) — Web 多模态上传（图片/音频）

补齐 Web 端多模态输入短板：输入栏新增 📎 上传按钮，图片入 vision 队列、音频转录为文字后自动发送，对标终端 `view_image` / `transcribe` 工具。

### 📎 Web 多模态上传

- 输入栏新增 📎 按钮 + 隐藏 `<input type="file" accept="image/*,audio/*">`，选中即上传
- 后端 `POST /upload?kind=image|audio`：图片走 `LLM.QueueImage`（vision 模型门控，非 vision 模型友好报错）、音频走 `TranscribeAudioTool`（Whisper 转录，成功后自动作为 user 消息发送）
- 二进制正文支持：`HttpRequest` 新增 `RawBody` 字节数组（避免 UTF-8 解码损坏图片/音频），`HttpServer` 两阶段读取（头 64KB 上限 / 正文普通 1MB、`/upload` 32MB）
- 纯函数辅助：`ParseUploadKind` / `IsImageExtension` / `SafeExtension` / `IsTranscribeError` / `ParsePath`（便于自测）
- 大小限制：图片 ≤5MB、音频 ≤25MB；扩展名白名单校验 + 安全落盘（`UploadDir` 临时目录）

### 🧪 自测

- 新增：`ParseUploadKind`/`SafeExtension`/`IsTranscribeError`/`IsImageExtension` 纯函数 + `ParseHttpRequest(byte[])` 二进制正文 + `ParsePath`
- 全量自测通过（0 失败）

## v0.66.0 (2026-08-16) — Web Diff 预览（写前逐 hunk 确认）

对标终端 `WAYCODER_DIFF_PREVIEW=1`：Web 模式下 write_file/edit_file/multi_edit 写文件前，把 diff 逐 hunk 推送到浏览器确认，不再因无 Console 而跳过。

### 🔍 Web Diff 预览

- `IWebInteraction` 新增 `DiffConfirmAsync(filePath, hunks, timeoutMs)` + `DiffConfirmResult` 结果类型
- `DiffPreview.Show` 增加 Web 分支：`UxHelper.WebInteraction != null` 时经桥弹浏览器 diff 对话框（`GetAwaiter().GetResult()` 阻塞等待，无 SynchronizationContext 死锁风险；超时/取消 → 拒绝）
- `WebChatServer` 实现 `DiffConfirmAsync` + `SerializeHunks` + `ParseDiffAnswer`（纯函数，便于自测）
- 前端 `ask` 事件新增 `kind:"diff"`：每 hunk 一个复选框（默认勾选）+ 行级红/绿/灰着色（明暗主题自适应）+ 「全部接受 / 应用选中 / 全部拒绝」三按钮
- 复用现有 `/answer` 路由回传结构化决策（`{"decision":"accept|reject|partial","accepted":[索引]}`）

### 🧪 自测

- 新增：`ParseDiffAnswer` 纯函数（accept/reject/partial/null/非法）+ `SerializeHunks` + `DiffPreview.Show` Web 分支（mock 交互桥）
- 全量自测通过（0 失败）

## v0.65.0 (2026-08-16) — Web 斜杠命令路由

Web 输入框支持斜杠命令，对标终端 REPL。未识别命令回退为普通 Agent 消息，纯 UI 命令前端直接拦截。

### ⌨ Web 斜杠命令

- 后端 `HandleCommand` 纯函数 + `POST /command` 端点，覆盖 Web 有意义命令子集
- 前端 `/` 开头输入路由到 `/command`，未识别回退 `/chat`；纯 UI 命令（`/theme`、`/settings`、`/model`）前端直接拦截
- 命令输出用 `.msg.cmd` 独立样式（accent 左边框 + 满宽 + Markdown 渲染）

### 命令清单

| 命令 | 说明 |
|---|---|
| `/help` | 命令帮助 |
| `/perm [ask\|auto\|smartauto\|yolo]` | 权限模式 |
| `/model` | 打开模型选择窗口（前端） |
| `/model list` | 列出模型 |
| `/theme` | 切换明暗主题（前端） |
| `/settings` | 打开设置（前端） |
| `/reset` | 清空当前会话 |
| `/session [list\|save\|load <id>]` | 会话管理 |
| `/tokens` | Token 统计 |
| `/mcp` | MCP 服务器状态 |
| `/todo` | 任务列表 |
| `/interrupt` | 中断当前任务 |

### 🧪 自测

- 新增 19 条：`HandleCommand` 纯函数 + `/command` 端点冒烟 + HTML 结构
- 全量自测通过（0 失败）

## v0.64.0 (2026-08-16) — Web UI 完善：Markdown 渲染 + 权限模式开关 + 模型选择窗口

在 v0.63.0 三栏改版基础上继续完善 `--web` 浏览器界面：聊天消息 Markdown 渲染、权限模式从「强制 YOLO」改为用户可选、模型选择独立窗口、设置窗口居中。全程零第三方依赖、手搓渲染器、AOT 安全。

### 📝 Markdown 渲染（手搓 XSS 安全渲染器）

- 聊天 assistant 消息从纯文本升级为 Markdown：代码块（```` ```lang ````）、行内代码、标题 `#`~`######`、无序/有序列表、引用、表格、水平线、粗体/斜体、链接
- 安全策略：**先 `escapeHtml` 转义再结构化**，链接仅允许 `http/https`（`javascript:` 直接拒绝），代码块内容不解析内部 Markdown
- 流式体验：流式期间用 `textContent` 追加（快、不闪烁），`done/interrupted/failed` 时 `finalizeAssistant()` 一次性转 Markdown；`.streaming` class 区分流式/完成态，避免 `white-space` 冲突
- 修复表格分隔行被误当数据行的 bug（`i++` → `i += 2`）

### 🛡 权限模式开关（Web 从「强制 YOLO」改为用户可选）

- 后端 `SerializeState` 新增 `permMode` 字段 + `POST /perm` 端点
- 前端顶栏新增 `🛡 Ask / ✅ Auto / 🧭 SmartAuto / ⚡ YOLO` 下拉，切到 Ask 后权限确认框真正经交互桥弹浏览器对话框（此前 Web 模式强制 YOLO，确认框永不触发）

### 🧠 模型选择窗口 + 设置居中

- 顶栏模型下拉改为独立 `model-modal`：搜索过滤 + 按供应商分组 + 上下文窗口/价格元数据 + 需 key 标记（点选自动弹 key 输入）
- 设置抽屉从贴边弹出改为**居中弹出**（`top:50%; left:50%; translate(-50%,-50%)`，缩放过渡）

### 🧪 自测

- 新增 9 条：Markdown 结构/`finalizeAssistant`/流式态样式/权限模式下拉/`/perm` 端点冒烟
- `mdToHtml` 渲染器另用 node 单独 21 条单测覆盖（代码块/标题/列表/表格/引用/XSS 转义等）
- 全量自测通过（0 失败）

## v0.63.0 (2026-08-16) — Web 聊天界面三栏改版 + 交互桥

`--web` 浏览器聊天界面从单栏升级为三栏，并修复「工具输出不滚动」与「网页版无提问对话框」两个致命问题。全程零第三方依赖、AOT 安全、跨平台、手搓 HTTP+SSE。

### 🖥 三栏布局

- **左栏 · 会话记录**：上半 F1-F10 槽位条（白底=当前、有历史=高亮、点击切换），下半历史持久化会话列表（预览 + 模型 + 时间 + 消息数，悬停删除/重命名，底部「新建会话」按钮）
- **中栏 · 聊天**：保留消息流 + 输入框 + 发送/停止
- **右栏 · 信息面板**：📋 任务（含状态色点）、💰 Token/费用（本轮 + 累计 + 速率）、🔧 修改文件、🔌 MCP 服务器、🧠 LSP 会话；自动实时刷新（SSE 事件触发 + 2 秒轮询，页面隐藏时跳过）
- **设置页两列**：左列类别导航（首个默认高亮），右列当前类别的详细设置项，点击类别切换右侧内容

### 🐛 滚动 bug 修复

- `tool_output` 此前被追加到过期的 assistant 流元素（位于 tool 卡片上方隐藏区），导致工具输出「一直在下方不上滚」。现改为**两个独立流指针**：`assistantStreamEl`（assistant 文本流）+ `toolOutputEl`（独立 `.tool-output` 等宽可折叠块），按状态机正确分离，工具输出独立成块、按到达顺序显示在底部并自动滚动

### 🆘 Web 交互桥（致命问题修复）

- 此前 Web 模式下 `ask_user_question` 工具与权限确认框走 `Console.ReadLine()` 阻塞在后台线程，浏览器用户看不到任何对话框。现抽象「交互模式」：`UxHelper.IWebInteraction` 接口 + `WebInteraction` 注入点
- `WebChatServer` 实现 `IWebInteraction`（`Start` 注入 / `Stop` 移除）：提问/确认经 SSE `ask` 事件弹浏览器对话框（select 选项按钮 / multi 复选 / text 输入 / confirm 允许+总是允许+拒绝），`POST /answer` 回填应答，`Task.WhenAny` 超时兜底
- `AskUserQuestionTool` 与 `PermissionManager` 改异步走桥；`WebInteraction == null` 时仍走原 TUI/Console 路径，终端模式零影响

### 🔌 后端新增端点与访问器

- `GET /panel` — 右栏六类数据（`SerializePanel` 纯静态函数）
- `GET /sessions`、`POST /sessions/new|load|delete|rename` — 历史会话管理（`SerializeSessions` 纯静态函数）
- `POST /answer` — Web 交互桥应答
- `LspTool.ActiveSessions` 公开访问器 + `ActiveLspInfo` record（右栏 LSP 会话展示）

### 🧪 自测

- 新增 `SerializePanel`/`SerializeSessions`/`LspTool.ActiveSessions`/交互桥/端点冒烟测试
- 全量自测通过（0 失败）

## v0.62.2 (2026-08-16) — P0-P4 健壮性与安全加固

对全仓库做系统性安全审计与压力测试，修复命令注入、RCE、权限绕过、SSRF、资源泄漏、整数溢出、OOM、并发竞态与 Web 资源滥用等一批硬伤。全程零反射、AOT 安全、跨平台、零新依赖。

### 🔒 安全加固（P0-P2）

- **P0 `test` 工具 RCE**：`test` 工具经 shell 执行命令却绕过权限确认与 `BashGuard` 黑名单（`/perm yolo` 之外仍可 `test "curl ...|sh"`）。现加入 `PermissionManager.DangerousTools` + `TestTool.ExecuteAsync` 前置 `BashGuard.CheckBanned`
- **P1 `git` 命令注入**：`git -c alias.x='!cmd'` / `core.pager` / `core.sshCommand` 可使 git 内部经 shell 执行任意命令，完全绕过 `BashGuard`。新增 `GitTool.HasDangerousGitArgs` 拦截 `-c/--config/--config-env/--upload-pack/--receive-pack/--exec`（含 `-c=` 前缀）
- **P1 `/checkpoint` 命令注入**：`description` 未经清洗拼进 `git stash push -m "..."` 经 shell 执行，`/checkpoint x"; rm -rf ~; #` 可注入。新增 `SanitizeCheckpointLabel` 清除 shell 元字符
- **P1 `cp`/`mv`/`find_replace` 权限绕过**：三个文件操作工具不在确认名单，Agent 可无确认覆盖/移动文件。现加入 `DangerousTools`
- **P2 `doc` 工具 SSRF**：`action=fetch` 的 URL 未做 SSRF 校验，可诱导访问云元数据（169.254.169.254）与内网服务。现复用 `SsgfGuard.CheckUrl`/`CheckDns` 拦截
- **P2 `RasterImage` 整数溢出**：`width * height * 4` 按 int 溢出为负绕过长度检查 → 改 `long` + 超 2GB 拒绝；`AnsiString.TruncateByWidth` 悬空 ESC 序列 `text[i..(j+1)]` 越界 → 终止符钳制
- **P2 `LspTool.ReadResponse` 挂起**：同步阻塞读 + 未使用的超时 token，服务器不响应时永久挂起。现异步读 + token 超时 + EOF 保护 + `Content-Length` 长度上限（防恶意巨大缓冲区分配）
- **P2 `CheckpointManager` stderr 死锁**：只读 stdout，命令大量写 stderr 时管道缓冲区写满阻塞子进程。现并行排空 stdout/stderr
- **P2 `ErrorLog` 锁内 IO + `_dirty` 不复位**：缓冲区满时在 `lock` 内 `File.AppendAllLines` 阻塞其他线程；抽出 `AppendToFile` 锁外刷盘并修正 `_dirty` 复位

### 🛡 健壮性加固（P1：OOM / 死循环 / 栈溢出防护）

- **不可信尺寸字段护栏**：`PdfParser`（深层嵌套数组深度护栏）、`PngDecoder`（负数 chunk 长度）、`BmpCodec`/`JpegCodec`、`OfficeExtractor`（zip bomb 解压上限）、`CfbParser`（流 Size 校验）对图片宽高、CFB 流尺寸等不可信字段加防御护栏
- **绘图引擎护栏**：`DrawCanvas` 防 NaN/Inf 半径、超大半径钳制到画布对角线、退化椭圆除零；`DrawEngine` 画布像素数上限 25MP（`canvas W H` 超大尺寸防 OOM）+ 超大画布自动跳过 3× 超采样

### 🔀 并发与资源安全（P3）

- **`ModelOverride` 竞态**：`Agent.WithModelOverrideAsync` 用 try/finally 保证临时切换的小模型恢复，异常不再把 `ModelOverride` 永久污染导致后续请求静默降级
- **线程安全集合**：新增 `ThreadSafeStringSet` 替代无锁 `HashSet`（`EditFileTool.ChangedFiles` / `PermissionManager.AutoAllowed`）；`FileTracker` 7 处、`BackgroundTask.Output`、`LruCache`（读写锁 + 回调锁外调用防 `NoRecursion`）加锁；`FileLockManager` 过期强占改 `TryUpdate` CAS 原子更新
- **响应体 / 进程泄漏**：`LLM` 5xx/429 重试前 `Dispose` 响应体、`doc`/`fetch` 响应体 `using`、`LspTool` 进程 Kill 后 `Dispose`（`KillAndDispose`）

### 🌐 跨平台与 Web（P4 / P4-2）

- **`CrossPlatform` 统一运行器**：shell（`cmd.exe` vs `/bin/bash`）+ python（`python` vs `python3`）按平台选择；`HooksManager`/`LintTool` 的 `.py` 脚本改用 `CrossPlatform.PythonExecutable`，消除硬编码导致的跨平台失效
- **Web 资源上限 + XSS**：`WebServer` 请求正文 1MB 上限（413）、连接数 32 上限（`SemaphoreSlim`）；`WebChat` SSE 客户端 16 / 待处理输入队列 100 上限（429）；`HtmlEscape` 转义工具名/参数防 XSS

### 🧪 自测

- 新增 P1/P3/P4/P4-2/P0-P2 各批次共 **164** 项测试（`TestP1Hardening`/`TestP3Concurrency`/`TestCrossPlatform`/`TestP4WebResource`/`TestP0P2Hardening`）：命令注入拦截、权限名单、SSRF、整数溢出、画布护栏、线程安全集合、Web 资源上限、HTML 转义、跨平台运行器等
- 总计 **2965** 项自测全部通过（0 失败）

## v0.62.1 (2026-08-16) — 老式 Office/WPS 提取器真实文件修复

对着 WPS 自带真实模板文件端到端验证后，修复 `LegacyOffice` 三个提取器对真实文件的解析缺陷（此前单元测试夹具与解析器共享同一套错误假设，2795 项自测全绿但真实文件仍解析失败）。

### 🐛 修复

- **DOC FIB 布局 off-by-4**：`cslw` 偏移应为 `34 + csw*2`（原误为 34）、`fibRgLwOff` 应为 `36 + csw*2`（原误为 32 + csw*2）、漏读 `cbRgFcLcb` 2 字节，导致真实 `.doc` 报「无效 DOC：FIB 截断」。修复后 `secdoctemplate.doc`/`Austere.doc` 正确提取中文正文
- **XLS 空白表格 dump 元数据**：BIFF8 空白表格（空 SST）此前退化到 UTF-16 扫描，把字体名/数字格式/表名当正文输出；现按 BOF 版本（≥0x0500）判定现代格式，无文本直接返回「无文本内容」
- **XLS 加密检测**：新增 `FILEPASS`（0x002F）记录检测，密码保护文件返回「已加密」而非「无文本内容」
- **PPT 分层嵌套解析**：PowerPoint Document 流是分层容器结构（容器 `recVer=0xF`），文本 atom 嵌在容器内部，此前平铺扫描跳过容器内所有子记录导致「PPT 无文本内容」；现递归下降进容器，`newfile.dps` 正确提取母版文本
- **PPT 加密检测**：按 `Current User` 流 `CurrentUserAtom.headerToken` 高 16 位（0xF3D1）判定标准加密

### 🧪 自测

- 新增 6 项测试：XLS 空白表格不 dump 元数据、XLS 加密文件（FILEPASS）、PPT 嵌套容器文本、PPT 加密分支、端到端 CFB 加密检测（headerToken）、端到端未加密正常提取
- 总计 **2801** 项自测全部通过（0 失败）

## v0.62.0 (2026-08-16) — 老式二进制 Office / WPS 文档读取

补齐 `.doc/.xls/.ppt` 老式二进制 Office 文档与 WPS 老后缀 `.wps/.et/.dps` 的文本读取。这些格式本质都是 CFB（Compound File Binary / OLE2）复合文档，此前 `read_file` 只能读 docx/xlsx/pptx，遇到二进制 Office 会报「无法识别」。全程零第三方依赖、零反射、跨平台，延续「手搓」原则。

### ✨ 新增

- **手搓 CFB 解析器 `CfbParser`**（`WayCoder/Infra/CfbParser.cs`）：按扇区 + FAT + DIFAT + 目录组织解析复合文档，支持常规扇区链（512/4096 字节）与 mini 扇区链（<4096 小流），按名取流（`GetStream`/`HasStream`/`StreamNames`）；FAT 链遍历带 100 万次防御上限，损坏输入返回 `null` 不崩
- **老式格式文本提取器 `LegacyOffice`**（`WayCoder/Infra/LegacyOffice.cs`）：
  - **二进制 DOC**：FIB 解析（wIdent/flags/csw/cslw → fibRgLw/fibRgFcLcb）+ piece table 提取文本。正确实现 MS-DOC 规范——`fcClx` 指向**表流**（0Table/1Table 由 `fWhichTblStm` 选择）、`Pcd.fc` 指向 **WordDocument 流**（bit31 为保留位 r1，bit30 为 fCompressed）、压缩文本字节偏移 = `fc/2`（非压缩 = `fc`）、cp1252 单字节映射（0x80–0x9F → € ‚ ƒ „ … 等）
  - **二进制 XLS**（BIFF8）：SST 共享字符串表（`0x00FC`）+ LABEL 内联标签（`0x0204`）+ LABELSST（`0x00FD`）+ STRING/RSTRING；BIFF 字符串 flags（fHighByte/fExtSt/fRichSt）解析
  - **二进制 PPT**：RecordHeader 遍历 + `TextCharsAtom`（0x0FA0 UTF-16）/ `TextBytesAtom`（0x0FA8 ANSI）文本 atom
  - **RTF 剥离**：控制字（`\word`/`\wordN`）、控制符号（`\'hh`/`\~`/`\_`/`\-`）、`\*` 跳过目标组、`\uN` Unicode 转义
  - **容器识别**：按文件头魔数区分 CFB / ZIP / RTF / HTML / 纯文本，扩展名不可靠时仍正确路由
- **`read_file` 分发**：`.doc/.wps` → DOC、`.xls/.et` → XLS、`.ppt/.dps` → PPT（WPS 老后缀与 Office 老后缀共用同一套解析器）

### 🧪 自测

- 新增 22 项测试（`TestWps`）：CFB 解析 round-trip（小流走 mini 链、大流走常规扇区、未知名返回 null）、容器识别（CFB/ZIP/RTF/HTML/纯文本）、二进制 DOC 提取（Hello World + 中文 + **压缩文本折半定位**）、XLS（SST + LABEL）、PPT、RTF 剥离、端到端 `.wps → read_file`
- 测试夹具 `BuildCfb`/`BuildDocWordStream`/`BuildDocTableStream`/`BuildXlsWorkbook`/`BuildPptStream` 手搓构造最小合法 CFB/BIFF/PPT 结构，piece table 按规范落在表流
- 总计 **2795** 项自测全部通过（0 失败）

## v0.61.0 (2026-08-16) — Web 界面完整化（对标 DeepSeek Harness）

把 `--web` 浏览器聊天界面从「单 Agent + 固定模型」扩展为完整的 Web UI，对标 DeepSeek Harness 的 `dsh web`（黑白主题、圆角、换模型、输 key、设置、多槽位）。全程延续零第三方依赖 + AOT 安全 + 跨平台 + 手搓原则。

### ✨ 新增

- **黑白双主题 + 圆角风格**：`data-theme="dark"|"light"` 两套 CSS 变量，`localStorage` 持久化，默认深色；全局圆角（消息卡片 14px / 按钮 10px / 输入框 14px）
- **模型下拉**（按 provider 分组）：`GET /models` 返回 `ModelCatalog.All`（含 `hasKey` 字段），`POST /model` 换当前槽位模型
- **输入 API Key**：`POST /key` 按供应商存 `ApiKeyStore`（`~/.waycoder/api_keys.json`），换到无 key 供应商时前端弹 key 输入框；`secret` 类型设置项返回 masked 不泄露明文
- **设置面板**：`GET /settings` 返回 `Config.SettingSchema()` 按 Category 分组（text/number/select/secret/toggle 对应控件），`POST /settings` 走 `TrySetPropValue` + `SaveToEnvFile`，右滑抽屉交互
- **槽位切换（F1-F10）**：`POST /slot` 切换多 Agent 工作区槽位，每槽位独立 LLM + 历史（惰性创建 `EnsureSlot`），顶栏胶囊指示条（当前=高亮、有历史=实线）
- **LLM 运行时重配置**：`LLM.Reconfigure(apiKey, baseUrl)` 让 `ApiKey`/`BaseUrl` 改为 `{ get; private set; }`，换供应商无需重建 Agent、不丢对话历史；`ApplyModel` 换模型流程（模型目录 Url 优先 + `UpdateContextWindow` + 持久化）

### 🧪 自测

- 新增 25 项测试（`TestWebFull`）：`LLM.Reconfigure`（key/baseUrl/Endpoint/Model）、`SerializeModels`（分组 + hasKey）、`SerializeSettings`（分组 + secret 字段）、`SerializeState`/`SerializeHistory`、`ApplyModel` 非法模型报错、`ProviderHasKey`（local/custom 无需 key）、端点冒烟（`GET /models` `/state` `/settings`、`POST /slot` `/model` `/settings` 成功与错误分支）
- 端到端原生进程验证：10 个端点全部正常（换模型/换 key/设值/槽位切换返回 `{"ok":true}`，非法输入返回结构化错误），`.env` 验证后恢复
- 总计 **2774** 项自测全部通过（0 失败）

## v0.60.0 (2026-08-15) — 浏览器聊天界面（--web）

对标 deepseek-harness 的 `--web`，新增本地 HTTP 服务 + 浏览器聊天界面：`waycoder --web [端口]` 启动服务并自动打开浏览器，在网页里与 Agent 流式对话，摆脱终端环境限制（远程服务器、无 TTY 场景），获得更友好的 Markdown 渲染、流式输出与工具调用可视化。全程零新依赖、零反射，符合「跨平台 + 手搓」原则。

### ✨ 新增

- **手搓 HTTP 服务端 `HttpServer`**（`WayCoder/Web/WebServer.cs`，纯 BCL，AOT 安全）：`TcpListener` 监听 `127.0.0.1:<端口>`（默认 9527，`WAYCODER_WEB_PORT` / `--web 端口` 覆盖，仅回环不暴露公网）；HTTP/1.1 请求解析（请求行 + 头 + `Content-Length` 正文）；SSE 长连接（`text/event-stream`，`event:`/`data:` + `\n\n`）。纯函数 `ParseHttpRequest`/`SseEvent`/`FindHeaderEnd`/`ParseContentLength` 便于自测
- **Agent 桥接 `WebChatServer`**（`WayCoder/Web/WebChat.cs`）：把 `Agent.ChatAsync` 的三个流式回调（onToken/onTool/onToolOutput）转 SSE 事件广播（token/tool/tool_output/done/interrupted/failed）；`ConcurrentQueue` 输入队列 + 可重置 `CancellationTokenSource` 支持 `/interrupt` 中断；`/history` 回放 `Agent.Messages` 中 user/assistant 消息
- **内嵌前端**（单 `const string Html`，无构建、无外部 CDN、离线可用）：深色主题聊天界面，`EventSource('/events')` 收流式 token、`fetch POST /chat` 发消息、`fetch POST /interrupt` 中断，工具调用卡片 + 流式追加 + 自动滚动
- **CLI 接入 `WebArg`**：`BuiltinArgs.RegisterAll()` 注册 `--web`（`ValueCount -1` 可选端口）；`Program.RunWebAsync` 强制 YOLO（web 无终端弹权限框）→ 启动服务 → `OpenBrowser`（macOS `open`/Windows `start`/Linux `xdg-open`，`WAYCODER_WEB_NO_OPEN=1` 禁用）→ Ctrl+C 优雅退出自动保存会话
- **保持单文件发布**：HTML 内嵌为 `const string`，无外部文件、无新依赖，AOT 单文件 exe 不变

### 🧪 自测

- 新增 Web 17 项测试：`ParseHttpRequest`（GET/POST/查询串/畸形请求不崩）、`SseEvent` 格式化、`FindHeaderEnd`/`ParseContentLength`、HTML 含关键标记（EventSource//chat//interrupt）、端到端 `HttpClient` GET `/` 冒烟
- 本地原生二进制端到端验证：`--web 9999` → `GET /` 返回 HTML、`GET /history` 返回 `[]`、`POST /interrupt` 返回 `ok`、YOLO 权限切换与 URL 打印正确
- 总计 **2749** 项自测全部通过（0 失败）

## v0.59.0 (2026-08-15) — 手搓 PDF 解析器替代 PdfPig

彻底移除最后一个重依赖第三方库 PdfPig，手写纯 BCL 的 PDF 文本提取器，消除其编译警告与不可修复/安全泄漏隐患，符合「优先开源、无开源则手搓」的第三方库选型原则。

### ✨ 新增

- **手搓 PDF 解析器 `PdfParser`**（`WayCoder/Infra/PdfParser.cs`，约 900 行纯 BCL，AOT 安全、零反射、零依赖）：
  - 文件结构解析：`%PDF` 头校验 → `startxref` 定位 → xref 表 / xref 流（PDF 1.5+）双路 + `/Prev` 增量链回溯 → 间接对象（字典/数组/名字/字面字符串/十六进制字符串/数字/引用/流）递归解析，带对象缓存与 `<<` 字典→`stream` 流内联识别
  - 流解压：`FlateDecode`（复用 `ZLibStream`）+ `ASCIIHexDecode` + `ASCII85Decode` + `Filter` 数组链式过滤，无 filter 原样返回
  - 页面树遍历：Catalog → Pages → Kids 递归收集，`/Count` 校验
  - 内容流文本提取：`BT`/`ET` 文本块 + `Tj`/`TJ`/`'`/`"` 显示文本 + `Tf` 字体切换 + `Td`/`TD`/`T*`/`Tm` 换行判定（负位移/绝对 y 下降），`TJ` 大负 gap 插空格；**内容流用独立静态字节级解析器**（与文件结构解析解耦）
  - 字体编码：`/ToUnicode` CMap（bfchar 单码 + bfrange 范围）> Type0 `/Identity-H`（UTF-16BE）> 简单字体 `/WinAnsiEncoding`（CP1252）+ `/Differences` 字形映射 + Latin-1 近似 + UTF-16BE/LE BOM 自动识别；`/Info /Title` 元数据解码
- **`PdfExtractor` 重写**：公开 API 完全不变（`Extract`/`GetMeta`/`PdfExtractResult`/`PdfPageContent`/`PdfMeta`/`ToMarkdown`），内部改用 `PdfParser`；空行压缩逻辑保留；损坏/加密/不支持结构（object stream）优雅报错不崩溃
- **移除 PdfPig 依赖**：`WayCoder.csproj` 删除 `PackageReference Include="PdfPig"`，编译警告随之消失

### 🧪 自测

- 新增 PdfParser 19 项测试：最小 PDF 构造（xref 表 + 页树 + 内容流 + 标题）→ 页数/标题/两行文本提取；非 PDF/空数据返回 null；`PdfExtractor` 公共 API（Extract/GetMeta/ToMarkdown/不存在文件/损坏文件）错误分支
- 本地用 `cupsfilter` 生成的真实 PDF（含中文）端到端验证：文本完整提取（68 字符无乱码）
- 总计 **2732** 项自测全部通过（0 失败）

## v0.58.1 (2026-08-15) — 运行轨迹记录 + OpenClaw 竞品分析

对标 OpenClaw 的 trajectory JSONL 回放，新增运行轨迹记录器，把每次 Agent 运行的完整过程落盘为版本化 JSONL 事件流，为调试 agent 行为、评估模型质量、复现 bug 提供可观测基石。

### ✨ 新增

- **运行轨迹 `Trajectory`**：版本化 JSONL 事件流（`traceSchema`/`schemaVersion`/`runId`/`sessionId`/`type`/`ts`/`seq`/`data`），四类事件——`run_start`/`llm_turn`（每轮 token+内容长度+工具数+推理长度）/`tool_call`（工具名+入参/结果摘要+成败+耗时）/`run_end`（轮次+累计 token 汇总）；落盘 `.waycoder/trajectory/<runId>.jsonl`（已被 `.gitignore` 覆盖）；`WAYCODER_TRAJECTORY=0` 关闭；纯手搓 JSONL 追加（`File.AppendAllText` + lock + Interlocked 序列号），AOT 安全、零依赖
- **`ChatAsync` 薄包装重构**：主循环抽为 `ChatAsyncCore`，外层 try/finally 统一落 `run_end`——无论正常完成/异常/取消/提前返回都不漏（轨迹记录失败静默降级，不影响主流程）
- **竞品分析文档** `docs/openclaw-analysis.md`：OpenClaw 架构对比 + 4 个可借鉴点（轨迹回放/上下文降级原因码/工具声明式元数据/安全自审计），轨迹回放已落地

### 🧪 自测

- 新增 Trajectory 21 项测试：截断纯函数（头尾保留/标记/极小 maxChars）、Enabled 标志、JSONL 事件流落盘/读回（事件类型顺序、schema 字段、run_end 汇总、tool_call 成败）
- 总计 **2713** 项自测全部通过（0 失败）

## v0.58.0 (2026-08-15) — 对标 deepseek-harness：持久 shell + 环境清理 + 进程树终止 + 调度器

对照 deepseek-harness 源码逐项借鉴，补齐一批执行层的健壮性能力：`bash` 支持 `session_id` 持久 shell 会话（跨命令共享 cwd/env/shell 状态）；子进程启动前清理凭据形状的环境变量（防密钥经 env 泄漏）；全部子进程终止统一走 `entireProcessTree` 进程树终止（父进程被杀子进程一并清理）；`RetryPolicy` 增加对称 jitter（±10%）打破多客户端同时重试的惊群；工具调用改为按 `ExecutionMode`（Parallel/Exclusive）分批并行调度（批内有界并发 4 + 独占串行 + 按模型声明顺序提交）；并新增 `ToolResultClassifier` 统一区分「真实错误 vs 用户取消/安全阻止」，自恢复提示只对真实错误注入。

### 🚀 增强

- **`bash` 持久 shell 会话**：新增 `session_id` 参数，复用同一 shell 进程，`export`/`alias`/`cd` 跨命令生效；唯一 GUID marker 界定输出边界 + 回读退出码；进程崩溃/超时自动重建，空闲 5 分钟自动回收（沙箱模式不支持）
- **环境变量清理**：`EnvScrubber` 在子进程启动前移除 KEY/PASSWORD/SECRET/TOKEN 形状及 `WAYCODER_*` 环境变量，防止密钥经 `env`/输出泄漏（对标 harness `scrubbedParentEnv`）
- **进程树终止对齐**：`HooksManager`/`LspTool`/`LintTool`/`McpClient`/`Agent` 自动测试等全部子进程 `Kill()` 统一改为 `Kill(entireProcessTree: true)`，父进程被杀时子进程一并终止
- **`RetryPolicy` 对称 jitter**：新增 `JitterRatio`（默认 0.1），重试延迟在 ±10% 内随机抖动；`ComputeJitteredDelay` 纯逻辑可自测（对标 harness jitterRatio）
- **工具调用并行调度**：`ITool.ExecutionMode`（Parallel/Exclusive）+ `ToolCallScheduler.Partition` 把一轮工具调用切分为「并行批 + 独占批」，批内有界并发（`MaxParallelism=4`）、批间串行，结果按模型声明顺序回填；bash/write_file/edit_file/agent/lsp/rm 等 14 个有副作用工具标注为 Exclusive
- **统一工具错误格式**：`ToolResultClassifier` 统一识别「错误/Error/❌/失败/运行命令时出错」等真实错误前缀，与「用户取消/Hook 阻止/沙箱阻止/危险命令阻止」等中止类区分——只有真实错误才注入「修正参数后重试」自恢复提示

### 🧪 自测

- 新增持久 shell 命令包装/cwd/env/退出码、环境变量敏感名判定与清理、进程树终止（父杀子随）、jitter 上下限/禁用、调度器分批、14 个工具 ExecutionMode 标注、错误分类器 17 项等测试
- 总计 **2692** 项自测全部通过（0 失败）

## v0.57.0 (2026-08-15) — 工具完善：新增 sqlite/test 工具 + 6 个工具增强 + LSP 会话缓存

响应「所有工具还有什么欠缺」的系统性排查，补齐工具短板：新增 `sqlite` 查询、`test` 测试运行两个工具（内置工具数 44→46）；`fetch` 支持 HTTP 方法/headers/body、`web_search` 增加 Bing 备用引擎与节流、`read_file` 支持 tail/二进制识别/JSON/INI 结构化、`write_file` 支持 append 与编码、网络工具接入 `RetryPolicy`；并给 `lsp` 加会话缓存复用，避免每次导航都重启服务器 + 重新初始化。

### ✨ 新增

- **`sqlite` 工具**（第 45 个）：通过系统 `sqlite3` 命令行执行 SQL（SELECT/INSERT/UPDATE/DELETE），`-header -column` 列式输出；零依赖、跨平台、AOT 安全，未安装时给出 macOS/Linux/Windows 各平台安装提示
- **`test` 工具**（第 46 个）：封装「跑测试 → 统计通过/失败 → 定位失败用例」闭环，支持 dotnet test / pytest / npm test / cargo test / go test 等，自动解析 pass/fail 计数并提取 FAILED/Error 失败用例行

### 🚀 增强

- **`fetch`**：支持 HTTP 方法（GET/POST/PUT/DELETE/PATCH/HEAD/OPTIONS）、自定义 headers、请求 body；接入 `RetryPolicy` 网络重试
- **`web_search`**：DuckDuckGo 失败自动回退 Bing 解析；2 秒最小间隔节流防封
- **`read_file`**：新增 `tail` 读取末尾 N 行；二进制内容识别（NUL 字节 + 严格 UTF-8 校验）；`.json` 美化输出、`.ini/.cfg/.conf` 结构化解析
- **`write_file`**：新增 `append` 追加模式；`encoding` 参数（utf8/utf8bom/ascii/utf16/utf16be/utf32，AOT 内置编码）
- **`download`**：接入 `RetryPolicy` 网络重试
- **`lsp`**：会话缓存复用——按（项目根, 命令）缓存 LSP 服务器进程，空闲 5 分钟自动回收、进程崩溃自动重建；顺带修复 didOpen 通知误读响应导致的 10 秒阻塞

### 🧪 自测

- 新增 fetch 方法/headers 解析、sqlite 查询、test 结果解析（pytest/dotnet 格式）、lsp 项目根查找与会话清理等测试
- 工具数量断言 44 → 46
- 总计 **2638** 项自测全部通过（0 失败）

## v0.56.0 (2026-08-15) — TUI 控件库统一 + 编辑器增强 + 对话框布局修复

响应「编辑器输入/刷新闪烁」「表格控件」「对话框标题栏错位」等一批 TUI 体验问题，新增 `TuiTableList` 表格列表控件并把 8 个界面迁移到统一控件库；编辑器补齐正则查找/替换、括号匹配、鼠标支持、状态栏行列信息，`TuiRichEditor` 改为逐行脏渲染消除整屏闪烁；修复确认框/权限框等 TitleBold 对话框标题栏覆盖上边框的错行 bug，并对全部 13 种对话框 + 行内权限控件（InlinePermission）建立端到端渲染自测。

### ✨ 新增

- **`TuiTableList` 控件**：表格列表（列头/列分隔线/选中高亮/滚动钳制/`ActivateSelected` 回调），8 个界面迁移到统一控件库
- **编辑器能力补齐**：
  - 正则查找/替换（捕获组 + 整词匹配 + 大小写开关）
  - 括号匹配 + 光标处词搜索
  - 鼠标点击定位光标 + 滚轮滚动
  - 状态栏显示行列/总行数/字节数
  - Tab 缩进可配置（默认 `\t`，设置里选 tab/space）
  - 退出未保存时弹保存确认对话框
- **`docs/TUI设计规范.md`**：设计令牌 + 规范文档 + 校验闸门

### 🐛 修复

- **对话框标题栏错行**：`TuiScreen` 两处 TitleBold 分支误用 1 基 `CursorPos`，导致标题覆盖上边框、与边框挤同一行；改为 0 基 `CursorPos0` 后标题独立成行、上边框为纯净渐变线（确认框/权限框等所有带标题对话框受益）
- **编辑器标点输入**：中文/全角标点无法正常输入的解析问题修复
- **`TuiRichEditor` 逐行脏渲染**：输入时只刷新光标所在行，未变化行不再整屏闪烁；滚动时全量刷新

### 🛠 重构

- **`Test/` 目录重组**：SelfTest 12 partial + Benchmark/Keypad/TuiAudit/TuiDemo 归入 `WayCoder/Test/`
- **对话框体系收敛**：移除 `DialogOverlay`/`DialogAction`，并入统一对话框管线

### 🧪 自测

- 新增全部 13 个 `TuiDialog` 工厂方法（Info/Success/Warn/Error/Confirm/Confirm3/Input/InputLine/Secret/FindReplace/Select/MultiSelect/Permission）布局 + 渲染断言（宽高 ≤ 屏 3/4、TitleBold 标记、标题独立成行）
- 新增 `InlinePermission` 行内权限控件测试（3 行黄色块渲染、危险命令忽略 A、D 展开、Y/N/A 三种结果）
- 总计 **2622** 项自测全部通过（0 失败）

## v0.55.0 (2026-08-15) — 绘图引擎增强 + 图片编解码 + JNode 手搓 JSON 迁移

响应「丰富画图功能」与「图片互转/贴图/裁剪/应用图标」需求，绘图引擎从 10 条指令扩展到 20+ 条（变换/新形状/描边/渐变/贴图/裁剪/图标模板/抗锯齿），并手搓 PNG/JPG/BMP 编解码，新增第 44 个内置工具 `convert_image` 实现格式互转。同时完成 JNode 手搓 JSON 全量迁移，彻底告别 `System.Text.Json` 反射。

### ✨ 新增

**图片编解码（零依赖、零反射、AOT 安全、跨平台）**

- **`Infra/RasterImage.cs`** — 统一像素缓冲（RGBA + 宽高 + 单像素读写/采样）
- **`Infra/BmpCodec.cs`** — BMP 编解码；**`Infra/JpegCodec.cs`** — JPEG 基线编解码
- **`Infra/PngDecoder.cs`** — 手搓 PNG 解码（灰度/索引/RGB/RGBA + 5 种行滤波）
- **`Infra/ImageLoader.cs`** — 魔数格式检测 + 编解码分发 + 按扩展名转格式
- **`Tools/ImageConvertTool.cs`** — `convert_image` 工具（第 44 个内置工具）：PNG/JPG/BMP 互转

**绘图引擎增强**

- **变换**：`translate/rotate/scale/push/pop`（`Affine` 仿射矩阵，SVG `<g transform>` + PNG 逆变换填充）
- **5 个新形状**：`star`（n 尖星）/`regular`（正 n 边形）/`ring`（圆环）/`pie`（扇形）/`heart`（心形）
- **描边**：所有填充形状支持 `stroke` 轮廓 + 线宽 + 线头 butt/round/square
- **渐变**：`gradient` 定义 + 线性/径向渐变填充（`@id` 引用，SVG `<defs>` + PNG 参数插值）
- **贴图**：`image x y w h "路径"` 把 PNG/JPG/BMP 图片贴入画布
- **裁剪**：`crop sx sy sw sh` 裁源图子矩形、`round r` 目标圆角、`rect` 直角矩形
- **图标模板**：`icon mac|ios|android|windows [颜色] [字形]` 一键生成应用图标（预设尺寸/圆角/安全区）
- **字体 + 抗锯齿**：`TrueTypeFont` 手搓 TrueType 解析 + `FontFinder` 系统字体探测（跨平台）；`antialias` 线条/字体消除锯齿

### 🛠 重构

- **JNode 手搓 JSON 迁移**：43 个工具的 `Parameters`/`Schema` 及非工具文件（Agent/Config/Infra/Memory/Batch）从 `System.Text.Json.Nodes` 全量迁移到手搓 `JNode`，移除残留 `JsonNode`/`JsonSerializer` 反射（承接 v0.53.10/11，实现「完全禁止反射」）

### 🧪 自测

- 新增图片/绘图断言（Image.Loader/Convert/Paste/Crop + Draw.Icon 等）
- 工具数量 43 → 44，总计 **2343** 项自测全部通过（0 失败）

## v0.54.0 (2026-08-15) — 新增 draw 绘图工具（文本 DSL → SVG/PNG）

响应「是否需要加个绘图工具」需求，新增第 43 个内置工具 `draw`：用文本指令画图，支持主流格式（SVG 矢量 + PNG 位图）双输出，指令可经编译期插件系统扩展。全程手搓零依赖、零反射、AOT 安全、跨平台。

### ✨ 新增

- **`Tools/DrawTool.cs` — draw 工具**：`code`（绘图指令文本）+ `format`（svg/png）+ `output`（文件路径），SVG 缺省返回内容、PNG 写文件返回路径
- **`Infra/DrawEngine.cs` — 绘图引擎核心**：`ColorUtil`（#rgb/#rrggbb/#rrggbbaa + 20 命名色）、`DrawTokenizer`（引号/逗号分词）、`DrawFigure` 图元、`IDrawCommand` 指令接口 + `DrawCommandRegistry` 注册表、`DrawDocument`/`DrawRunner`（DSL 解析 + SVG/PNG 编排）
- **`Infra/DrawCanvas.cs` — 光栅化器**：`Canvas` 像素画布（Bresenham 线 + 中点圆 + 扫描线 even-odd 多边形填充 + 圆角矩形/椭圆）+ 内置 5×7 点阵字体（ASCII 32–126，非 ASCII 实心块占位）
- **`Infra/DrawCommands.cs` — 10 条内置指令**：`rect`/`roundrect`/`circle`/`ellipse`/`line`/`arrow`/`polygon`/`polyline`/`path`/`text`（另有 `canvas` 画布），经 `[ModuleInitializer]` 自动注册
- **`Infra/PngEncoder.cs` — 手搓 PNG 编码器**：RGBA → PNG（ZLibStream DEFLATE + CRC32 + chunk 布局），对标 ScreenshotTool 的手写 PNG 解析

### 🔌 扩展

- 指令可扩展：实现 `IDrawCommand` 并 `DrawCommandRegistry.Register`，插件 `[ModuleInitializer]` 里注册即可贡献自定义绘图指令（满足「自己增加指令」）

### 🧪 自测

- 新增 `SelfTest.Chunk10.cs`（50 项）：ColorUtil 解析往返、分词器、注册表、DSL 解析（含错误分支）、SVG 标签、PNG 签名/IHDR 尺寸/IEND、光栅化像素、工具端到端
- 工具数量 42 → 43，总计 **2228** 项自测全部通过（0 失败）

## v0.53.11 (2026-08-15) — 彻底移除 JsonSerializer 反射（AgentSlotConfig / FetchTool）

承接 v0.53.10 的手搓 JSON/XML 库，把代码库中**最后一处**反射型 `JsonSerializer.Deserialize<T>`/`Serialize<T>` 全部替换为手搓 `JNode`，实现「完全禁止反射」——AOT 下不再有任何 `JsonSerializerIsReflectionDisabled` 隐患。

### 🛠 重构

- **`Config/AgentSlotConfig.cs`**：`Load`/`Save` 改用 `Json.Parse` + 新增 `SlotFromNode`/`SlotToNode` 手搓映射（键名保持 PascalCase，兼容历史 `agent_slots.json`），移除 4 处 `JsonSerializer.Deserialize<SlotConfig>`/`Serialize<SlotConfig>` 反射调用
- **`Tools/FetchTool.cs`**：`PrettyPrintJson` 改用 `Json.Parse` + `Json.Serialize(indent: true)`，移除 `JsonDocument`+`JsonSerializer` 反射路径

### ✅ 核查

- **`Tools/ScreenshotTool.cs`** 确认为跨平台（Windows PowerShell / macOS screencapture / Linux grim→import→scrot→maim 回退）+ 零反射（PNG 尺寸手搓解析 IHDR、OCR 走外部 tesseract），无需改动

### 🧪 自测

- 新增 10 项断言：`SlotConfig` 手搓往返（PascalCase 键名、字段往返、null 字段、`UseGlobal` 缺省为 true）、嵌套缩进美化
- 总计 **2178** 项自测全部通过（0 失败）

## v0.53.10 (2026-08-14) — 手搓 AOT 安全 JSON/XML 库

响应「不使用反射、自己可控」需求，新增两个零依赖、零反射的手写序列化库，替代散落各处的 `JsonNode.Parse` 手写解析与 `System.Text.Json` 反射序列化。

### ✨ 新增

- **`Infra/JsonLib.cs` — 手搓 JSON 库**（约 400 行）：
  - `JNode` DOM（Object/Array/String/Number/Bool/Null）+ 工厂/增删查/取值/深拷贝
  - `Json.Parse`/`TryParse` 递归下降解析器（手写 tokenizer），支持全部转义（含 `\uXXXX` 代理对）、严格数字语法、错误定位
  - `Json.Serialize`（紧凑 + 缩进两模式）+ `SerializeValue`（无反射，对齐 JsonHelper）
  - 数字往返保真（保留原始文本），非有限值安全回退 `null`
  - 类名 `JNode`/`JKind`/`Json` 避开 `global using System.Text.Json.Nodes` 冲突
- **`Infra/XmlLib.cs` — 手搓 XML 库**（约 400 行）：
  - `XNode` DOM（Element/Text）+ 属性保序 + 子节点/查询/InnerText
  - `Xml.Parse`/`TryParse` 手写解析器：声明/注释/DOCTYPE/CDATA/处理指令跳过、单双引号属性、预定义实体 + 数字字符引用、自闭合标签、标签匹配校验
  - `Xml.Serialize`（紧凑 + 缩进）+ 文本/属性转义

### 🧪 自测

- 新增 `TestJsonLib`（36 项）+ `TestXmlLib`（24 项）共 60 项断言：标量/对象/数组/嵌套解析、转义与代理对、非法输入拒绝、序列化往返、DOM 操作、实体/CDATA、错误分支
- 总计 2127 项自测全部通过（0 失败）

## v0.53.9 (2026-08-14) — 修复 HooksManager AOT 反射 bug

修复一个 NativeAOT 下的真实缺陷：`HooksManager.ParseHookOutput` 与 `LoadMatchers` 使用 `JsonSerializer.Deserialize<T>` 反射序列化，在 `PublishAot=true` 下会抛 `JsonSerializerIsReflectionDisabled`，导致 hook JSON 输出协议与 `hooks.json` matcher 系统在 AOT 发布版中完全失效。

### 🐛 修复

- **`ParseHookOutput`**：改用 `JsonNode.Parse` + `GetJsonString`/`GetJsonBool` 手写提取（AOT 安全），支持 `continue`/`decision`/`reason`/`systemMessage`/`additionalContext` 字段
- **`LoadMatchers`**：改用 `JsonNode.Parse` 手写解析 `matchers` 数组（matcher/events/hooks），移除 `HookMatchersWrapper` 反射类与 `using System.Text.Json`

### 🧪 自测

- 新增 `TestHooksManager`（20 项）：会话 hook 注册/注销/清空、`MatchesPattern` 通配匹配、`SnakeCase` 转换、`ParseHookOutput` 纯文本/JSON/exitCode 2 分支
- 其中 JSON 解析断言此前因反射异常而失败，现随修复通过
- 总计 2067 项自测全部通过（0 失败）

## v0.53.8 (2026-08-14) — 文件锁/文件追踪/Prompt 缓存单元测试补齐

继续代码质量维度：补齐三个核心安全/成本特性的零覆盖纯逻辑类的单元测试（39 项），均为 CLAUDE.md 强调的关键设计。

### 🧪 自测

- 新增 39 项断言覆盖三个类：
  - **`FileLockManager`**（14 项）：首次获取、同 agent 续期、异 agent 拒绝、`IsLockedByOther` 判定、Release 归属校验、过期锁强占（负 timeout 立即过期）、`ReleaseAll` 清空、`GetSummary` 空/非空、`WaitForLockAsync` 无锁成功
  - **`FileTracker`**（12 项）：未追踪/已追踪状态、外部修改 stale 检测（哈希变更）、`CheckForChanges` 检出、`RecordWrite` 更新、删除检测并移除追踪、`ValidatePreEdit` 未读取警告/已读取通过、`GetChangeWarning`、`Enabled` 短路、`Reset` 清空
  - **`PromptCache`**（13 项）：首次未命中、相同请求命中、节省 token 累计、system/tools 任一变更未命中、`HitRate` 计算、`Reset` 后未命中、`Enabled` 短路、`Summary` 命中率与 K 格式

- 总计 2047 项自测全部通过（0 失败）

## v0.53.7 (2026-08-14) — 长方法拆分 + ImportHelper 纯逻辑 + 压力测试脚本修复

代码质量维度继续推进：消除两处重复样板（`AgentTool` 并行解析三分支、`ContextManager` 三层压缩进度报告），将 `ImportHelper` 两个私有纯函数改为 `internal` 并补齐单元测试，同时修复压力测试脚本的跨平台与路径 bug。

### 🛠 重构

- **`AgentTool.ExecuteParallelAsync` 三分支合并**：`JsonArray` / `IEnumerable` / string-JSON 三处重复的「`ExtractTaskText` + 非空判断 + `Add`」样板提取为 `CollectTaskTexts` 单方法，消除重复
- **`ContextManager.MaybeCompressAsync` 进度报告合并**：三层压缩（裁剪/摘要/硬折叠）共 6 处重复的「百分比 + 进度条 + 事件」样板提取为 `ReportProgress`，行为不变
- **`ImportHelper` 纯逻辑暴露**：`StripJsonComments`（JSONC 注释剥离）与 `FormatSize`（文件大小格式化）由 `private` 改 `internal`，成为可测的零依赖纯函数

### 🧪 自测

- 新增 14 项断言覆盖 `ImportHelper`：
  - **`StripJsonComments`**（7 项）：行注释/块注释移除、注释后仍可解析、字符串内 `//` 与 `/* */` 不误删、字符串内转义引号保留
  - **`FormatSize`**（7 项）：B/KB/MB 三档 + 0/1023/1024/1MB 边界

- 总计 2008 项自测全部通过（0 失败）

### 🐛 修复

- **`scripts/stress-test.sh` 跨平台 + 路径 bug**：① AOT 产物名硬编码 `WayCoder.exe`（Windows），macOS/Linux 实为 `waycoder`，改为平台感知探测；② 编译验证 `cd "$WORK_DIR"` 后直接 `dotnet build`，但 Agent 在子目录（`MiniKanban/`）建项目导致 MSB1003，改为 `find` 定位 `.csproj`/`.sln` 所在目录后编译

### 🚀 端到端验证

- 压力测试通过：`deepseek-v4-flash` 生成 MiniKanban（9 文件 312 行），Agent 自测循环实测 `dotnet build` 0 错误 0 警告，CLI `add/list/move/delete` 全部通过，确认本轮重构无回归

## v0.53.6 (2026-08-14) — SnippetStore 可测试性重构 + 单元测试补齐

`SnippetStore` 此前 `Get`/`Search`/`List`/`Delete`/`Add` 硬编码 `DefaultDir`（`Environment.CurrentDirectory/.waycoder/snippets`），无法在隔离目录下测试——「不可测试 = 不可维护」的设计缺陷。

### 🛠 重构

- **`SnippetStore` 五个方法增加可选 `dir` 参数**：`Add`/`Search`/`List`/`Delete`/`Get` 均新增 `string? dir = null`（默认行为不变，向后兼容），内部 `EnsureLoaded(dir)` + 文件操作统一走 `dir ?? DefaultDir`，使测试可用临时目录隔离

### 🧪 自测

- 新增 9 项断言覆盖 `SnippetStore`：frontmatter 解析（name/language/tags/body）、`Add`→`Get` 往返、`Search` 多词 OR 按名称/标签命中、无命中返回空、`List` 全量、`Delete` 命中/未命中

- 总计 1994 项自测全部通过（0 失败）

## v0.53.5 (2026-08-14) — 工具层单元测试补齐（find_replace/diff/tree）

从基础设施类转向工具层：补齐三个纯 C# 实现工具的单元测试（16 项），覆盖编辑/对比/目录树核心行为。

### 🧪 自测

- 新增 16 项断言覆盖三个工具：
  - **`FindReplaceTool`**（6 项）：空 pattern 报错、预览模式不写文件、实际替换写入、无效正则回退纯文本匹配、目录不存在报错
  - **`DiffTool`**（5 项）：差异行 `-`/`+` 输出、相同文件提示、空文件提示、文件不存在错误
  - **`TreeTool`**（5 项）：树生成含子目录/文件、隐藏文件跳过、深度限制不展开、目录不存在错误

- 总计 1985 项自测全部通过（0 失败）

## v0.53.4 (2026-08-14) — 文件忽略规则 + 记忆检索补齐单元测试

继续代码质量维度改进：再补齐两个零覆盖的基础设施类 `FileIgnoreManager` 与 `MemoryRetrieval` 的单元测试（27 项）。

### 🧪 自测

- 新增 27 项断言覆盖两个类：
  - **`FileIgnoreManager`**（20 项）：`node_modules`/`dist`/`.git` 等始终忽略目录、`.pyc`/`.dll`/`.jpg` 等扩展名、`.gitignore` 规则匹配（`*.log` 任意深度、`!` 否定反转、`/rootfile.txt` 锚定、`*.tmp`）、`FilterIgnored` 批量过滤、`ShouldSkipDirectory` 隐藏/忽略目录跳过
  - **`MemoryRetrieval`**（7 项）：frontmatter 记忆加载 + `GetRelevant` 关键词匹配（英文标识符 + CJK 双字词）、无关关键词不误命中、`FormatForPrompt` 标题/类型/描述超 200 字符截断/空列表返回空

- 总计 1969 项自测全部通过（0 失败）

## v0.53.3 (2026-08-14) — 基础设施纯逻辑类补齐细粒度单元测试

代码质量维度（68/100）改进：此前三个零覆盖的基础设施纯逻辑类（`RetryPolicy` / `LruCache` / `IdGenerator`）从未被自测触达，属于「写了但没人验证」的死角。

### 🧪 自测

- 新增 `[基础设施]` 测试段，42 项断言覆盖三个类：
  - **`RetryPolicy`**（12 项）：黑名单/白名单异常过滤、首次成功不重试、失败 N 次后成功、耗尽重试、指数退避 100→200→400、无返回值版本
  - **`LruCache`**（16 项）：基本读写、容量淘汰最旧、`Get` 提升 LRU、TTL 过期、`Remove`/`Clear`/`OnEvicted` 事件、`TryGet`、命中/未命中/淘汰统计、容量 ≤0 校验
  - **`IdGenerator`**（14 项）：`NewId` 长度与去歧义字符集、100 个唯一性、`NewSlug` 格式与词数 clamp、`NewPrefixed` 前缀

### 🐛 修复

- **`RetryPolicy` 死代码**：重试循环末尾的 `throw new AggregateException` 实为不可达死代码——`catch (Exception ex) when (...)` 过滤器在最后一次尝试（`attempt == MaxRetries`）已原样放行异常向外抛出，`lastEx` 变量与 `AggregateException` 从未执行，且误导读者「耗尽重试抛 AggregateException」（实际抛最后一次原始异常，与文档一致）。移除死代码，替换为明确的「不可达终点」哨兵，行为不变

- 总计 1942 项自测全部通过（0 失败）

## v0.53.2 (2026-08-14) — 修复上下文压缩误触发（累计用量 → 最近 prompt）

端到端验证暴露的第二个缺陷：主智能体累计用量到 169 万 tokens、触发 5 次「上下文压缩」，但消息估算却只有 8 万左右——压缩根本没发生，只是在空转刷屏。

### 🐛 修复

- **压缩判断度量错误**：`ContextManager.ShouldStopAndSummarize()` 此前用 `CumulativePromptTokens + CumulativeCompletionTokens`（会话累计用量，单调递增）判断「剩余窗口是否不足」，导致上下文远未满时（真实上下文 ~12 万 vs 窗口 1M）就误触发压缩；而压缩层（`MaybeCompressAsync`）用消息估算（~8 万，远低于 50% 阈值）判断，三层压缩全部不触发，累计值也不重置，形成「每轮误触发 → 实际不压缩 → 累计继续涨 → 再误触发」的死循环
- **新增 `ContextManager.LastPromptTokens`**：`AddUsage` 里覆盖记录最近一次真实 prompt（代表当前上下文大小），`ShouldStopAndSummarize` 改用其判断——只有真实上下文真正接近窗口（剩余 ≤ buffer）才触发压缩，触发后压缩层用校准估算（≈ 真实 prompt）判断会真正执行裁剪/摘要，`ResetUsage` 同步重置

### 🧪 自测

- 新增断言 9 项（`LastPromptTokens` 记录最近一次非累加、累计超窗口但最近 prompt 小不触发、最近 prompt 接近窗口触发、大小窗口阈值、`ResetUsage` 重置），总计 1900 项全部通过（0 失败）

## v0.53.1 (2026-08-14) — 并行子智能体 tasks 数组对象元素乱码修复

端到端验证暴露的缺陷：`agent(tasks=[{"description": "..."}, ...])` 结构化传参时，对象元素被解析成 `Dictionary<string, object?>` 后直接 `ToString()`，子智能体收到 `System.Collections.Generic.Dictionary...` 乱码，3 个子智能体 2 个直接失败（只有纯字符串元素碰巧成功）。

### 🐛 修复

- **tasks 数组对象元素提取**：新增 `AgentTool.ExtractTaskText()`，对元素为对象（`JsonObject` / `Dictionary<string, object?>`）时提取 `description`/`task`/`name`/`title`/`text`/`prompt`/`instruction` 字段（对齐 schema 的 items 结构），纯字符串透传，无已知字段时兜底取第一个字符串值，杜绝类型名乱码

### 🧪 自测

- 新增断言 5 项（纯字符串透传、对象提取 description/task、JsonObject 提取、null 返回），总计 1891 项全部通过（0 失败）

## v0.53.0 (2026-08-14) — 子智能体健壮性加固（修复并行竞态 + 上下文爆炸防线）

压力测试暴露的两个「短板」从代码层根治：子智能体并行时的共享可变状态竞态、多路输出累加撑爆主智能体上下文。

### 🐛 修复

- **并行子智能体 ModelOverride 竞态**：子智能体改用独立 LLM 实例（`LLM.Clone()`），不再共享父 `LlmClient` 的小模型切换。此前并行模式下最后完成的子智能体会把 `ModelOverride` 恢复成小模型，污染主智能体后续请求降级
- **BashGuard 参数拦截语义 bug**：纯子命令禁止（如 `dotnet new`、`cargo install`）此前因 `MatchArgs` 无兜底而漏拦；`exceptFlags` 白名单（如 `pip install --user`）此前未命中时也放行。重写 `Match`/`MatchArgs`，白名单（exceptFlags=默认拦）与黑名单（flags/blockArgs=默认放）语义分离

### 🛡️ 健壮性

- 新增 `SubAgentParallelTotalMaxChars` 配置（`WAYCODER_SUBAGENT_PARALLEL_TOTAL_MAX_CHARS`，默认 15000）：并行子智能体聚合结果总限长，防止 N 个输出累加撑爆主智能体上下文（压力测试第五轮 8 路并行 3.8M tokens 的根因）
- 子智能体输出截断改「保尾」（头 70% + 尾 25%），保留末尾结论（如「Automata 7→0」），不再把关键结论截掉
- BashGuard 新增拦截 `dotnet new`（生成 csproj/Program.cs 污染多项目构建，压力测试第五轮 `MSB1011` 的根因）

### 🤖 自主性

- 新增 `SystemPrompt.SubAgentDiscipline`：子智能体纪律固化到每个子任务注入（不建 scratch/csproj、自测到通过、精简回报、不越界改模块），主智能体不必每次在 task 里重写外部铁律
- `LLM.MergeUsageFrom()`：子智能体 clone 实例的花费统计回收累加到父智能体，隔离不丢花费追踪

### 🧪 自测

- 新增断言 6 项（`dotnet new` 拦截、`dotnet build` 不误伤、纪律非空、`LLM.Clone` 独立、配置默认值等），并顺手修复 Snapshot 路径假设（兼容 cwd=仓库根或 `WayCoder/`），1879 项全部通过（0 失败）

## v0.52.0 (2026-08-14) — 子智能体 shell 权限（YOLO 放行 / 非 YOLO 提问确认）

### 🤖 多智能体

- 子智能体获得 `bash`（shell）权限：从「工具层禁令」转为「确认层管控」——移除 `SubAgentDeniedTools` 中的 `bash` 条目，shell 能力交给既有的 `PermissionManager.CheckAsync` 统一裁决
- **YOLO 模式**（`/perm yolo`）：子智能体 `bash` 直接放行，可跑 `dotnet build`/`dotnet run` 自测，不再「盲写」代码（修复压力测试中失败数从 7 暴涨到 129 的根因）
- **非 YOLO 模式**：子智能体 `bash` 属危险工具，逐条弹行内确认框「提问申请」；`ls`/`find`/`wc` 等只读命令仍由 `BashGuard.IsSafeReadOnly` 自动放行
- 新增 `PermissionManager.ConfirmLock`（`SemaphoreSlim`）串行化确认弹框：并行子智能体（`Task.WhenAll`）并发请求 shell 权限时逐个排队，消除抢键盘/渲染竞态
- 保留 `rm`/`kill`/`git` 等危险/管理类工具禁令（主智能体统一管理）

### 🔧 工具回显优化

- `Agent.FormatValue` 递归序列化集合/字典/JsonNode（而非 `ToString()` 泄漏 `System.Collections...`）
- `JsonHelper.SerializeValue` 改 `public` 供 FormatValue 复用
- `AgentTool.Description` 并发数硬编码「4 个」改为动态 `MaxParallelTasks`（配置调整后描述同步）

### 🧪 自测

- 自测数 1867→1872（上次 5 项回归）；`SubAgentDeniedTools` 断言反转（不再包含 `bash` / 子 Agent 深度 0 保留 `bash`），1872 项全部通过（0 失败）

## v0.52.0 (2026-08-14) — TUI 交互与渲染增强 + 反白配色统一 + 一键发布（master 并行线）

### 🎨 反白配色统一

- 统一「选中/光标行」反白惯例：菜单/列表选中行、单选组选中项、输入框光标行全部为「高亮底 + 黑字」，与终端反白（前景/背景互换）语义一致
- 修复 `TuiRadioGroup` 选中项前景色误用背景色代码（`SelFg=ControlFocusedBg` 经 `AnsiTty.FgCode(47)` 渲染成 256 色亮绿）的 bug：新增 `SelBg`，选中项整行「白底 + 黑字」
- `TuiTextArea` 新增 `CursorLineFg`（光标行黑字），光标移动/翻页/滚动时补 `MarkDirty`，修复光标行高亮残留

### 📝 Markdown 渲染增强

- 引用块 `>`：连续多行合并为 `MdBlockQuote`，左侧 `│` 竖线 + 缩进渲染
- 任务清单 `- [x]` / `- [ ]`：解析勾选状态，渲染 ☑（已完成·绿）/ ☐（未完成·弱化）
- 链接 `[文字](url)`：青色链接文字 + 弱化显示 URL；删除线 `~~text~~` 弱化显示

### ⌨️ 对话框 / 菜单 / 列表交互

- 所有 `TuiDialog` 对话框统一注册 `Esc` = 取消/关闭；单行输入框回车 = 确定；多选列表 Enter = 确认
- `TuiMenu`：`Space` 激活当前项；快捷键编号只计入非分隔线项（1-9 连续，分隔线不占编号）；菜单高度按终端内容区收拢；弹出菜单按调用点定位（不再居中）
- `TuiList` 单选 `Space` = 激活（等同回车）；`TuiButtonGroup` 支持上下键导航，按钮自行渲染（不依赖父 Children 树）

### 🔐 权限确认与问答统一

- `PermissionManager` 统一走 `UxHelper.Confirm`（TUI 黄底 Y/N/A 弹框 / 非 TUI 行内编号菜单），删除重复的 `ShowInlinePermission` 分支（注：本次合并保留 mac 线 `ShowInlinePermission` 行内权限块方案）
- `AskUserQuestionTool` 单选/多选/文本输入统一 `UxHelper.Select/MultiSelect/Ask`，删除约 125 行重复的 `ShowAndWait` 事件循环，并透传 `AskUserTimeoutSec` 超时

### 📎 工具消息缩进嵌套

- `tool` 消息缩进嵌套在所属 `assistant` 消息下（`indent=1`），`TuiListItem.Indent` 新增左缩进、续接无角色头；AgentSlot / Program / SessionCommand 全链路透传

### 🛠️ 工具增强

- 抓屏跨平台实现补全：Windows（PowerShell `CopyFromScreen`）/ macOS（`screencapture`）/ Linux（`grim`→`import`→`scrot`→`maim` 依次回退）
- 自动升级源切换：**Gitee 优先**（国内快）→ GitHub 回退；GitHub 仓库名更正为 `alecksty/waycoder`（配合商标更名，brew/winget/docs 同步更新）

### 🧪 TUI 测试审计工具

- `--keypad`：按键脚本回放驱动 TUI（KEY/TEXT/DELAY/SNAP/DIALOG/FOCUS/MSG/FILL），任意节点抓帧核对排版
- `--tui-audit`：对话框/控件渲染审计（输出纯文本帧，剥离 ANSI）

### 🎨 选择器统一外框 + 发布自动化

- 新增 `DialogFrame`：居中带边框外框（橙→黄渐变 + 暗化背景），统一 ModelPicker / FilePicker / SessionPicker / CommandPalette 外观
- 新增 `scripts/release.ps1` / `scripts/release.sh` 一键发布：编译 6 平台包 → 算 SHA256 → 生成 winget manifest → 更新 brew formula → apt 打包 → 打印提交命令

### 🧪 自测

- 新增反白配色 / Markdown 引用块·任务清单·链接 / 菜单快捷键·收拢 / 对话框 Esc / UxHelper 多选·确认等自测，1867 → 1909 项全部通过（0 失败）

## v0.51.0 (2026-08-14) — JSON 输出模式（--json，IDE / 脚本桥接，对标 Claude Code --output-format json）

### 🔌 IDE 桥接

- 新增 `--json`（`-j`）一次性输出模式：`waycoder --json -p "任务"`（或 `echo "任务" | waycoder --json`）静默执行 Agent，stdout 只输出一个结构化 JSON 对象，供 VS Code 扩展、CI 脚本、外部工具直接 `JsonNode.Parse` 解析——无需剥离 ANSI 动画/Spinner/权限块
- 结果字段：`schema`（版本 1.0）、`success`、`answer`（最终回答）、`error`（失败原因）、`model`、`usage{prompt_tokens/completion_tokens/total_tokens}`、`cost_usd`（模型无定价时为 null）、`duration_ms`、`changed_files[]`（本次会话修改文件清单，复用 `EditFileTool.ChangedFiles`）
- 退出码约定：0 = 成功，1 = 中断/超时/异常；与现有 `-p` 一次性模式共用同一条 Agent 执行链（`-y` 自动放行、非交互）
- `JsonResult.Build` 为纯函数构建器（输入原始值 → 输出 JsonObject），便于 AOT 自测，不依赖 `_llm`/`_agent` 静态态

### 🧪 自测

- 新增 JSON 模式自测 14 项（成功/失败结果、usage 汇总、cost null 兜底、空 changed_files、序列化可解析），1853→1867 项全部通过（0 失败）

## v0.50.0 (2026-08-14) — 编译期插件系统（IPlugin SDK，对标 Claude Code/Crush 插件扩展）

### 🔌 插件系统

- 新增 `IPlugin` 接口 + `Plugin` 抽象基类 + `PluginRegistry` 注册表——编译期插件（C# 源码随主程序一起 AOT 编译进单文件）可贡献两类扩展：
  - **工具**（`ITool`）：自动并入 `ToolRegistry.AllTools`，大模型直接通过 function calling 调用
  - **斜杠命令**（`ISlashCommand`）：自动并入 `SlashCommandRegistry`，REPL 输入 `/xxx` 触发
- 三步接入、零启动代码改动：`WayCoder/Plugins/` 目录新建 `.cs` → 继承 `Plugin` 覆写贡献项 → `[ModuleInitializer]` 自动注册（AOT 安全，无反射）
- 与已有扩展机制互补：SKILL.md（Markdown 技能）、Hooks（外部脚本）、MCP（外部服务器）之外，插件提供「原生性能、复杂逻辑、可复用工具/命令」的第四类扩展
- 注册表健壮性：同名插件（忽略大小写）覆盖不重复、`null` 注册/`null` 返回防御、按名卸载
- 完整文档见 [docs/插件系统.md](docs/插件系统.md)（含 `ITool` + `ISlashCommand` + `[ModuleInitializer]` 完整示例）

### 🧪 自测

- 新增插件系统自测 11 项（注册/收集/集成到 AllTools/同名覆盖/null 防御/卸载），1842→1853 项全部通过（0 失败）

## v0.49.0 (2026-08-14) — 批量任务引擎（多仓库并行 + worktree 隔离，对标 Cursor/Aider 多仓库批处理）

### 🚀 批量任务引擎

- 新增 `--batch` 一次性模式：多仓库并行处理，每个任务在独立克隆副本中隔离执行，跑完输出聚合报告（Markdown + 退出码），对标 Cursor 的批量修复、Aider 的多仓库脚本
- 两种用法：
  - `--batch <JSON文件|内联JSON>`：`{ "maxParallel": 4, "timeoutSec": 1800, "keepResults": false, "tasks": [{ "repo": "URL或本地路径", "task": "任务描述", "name"?, "branch"? }] }`
  - `--batch-repo <仓库> --batch-task "任务"`：快速给多个仓库跑同一个共享任务（`--batch-repo` 可重复）
- 隔离与安全：每个任务 `git clone` 到 `.waycoder/batch/jobs/<名>_<随机>` 独立副本，子进程以 `-p` 一次性模式 + `-y` 放行执行，进程级隔离 cwd 与状态；默认执行后清理副本，`--batch-keep` 保留
- 子进程复用父进程已解析的模型/BaseUrl/API Key/预算（`--model`/`--base-url`/`--api-key`/`--max-budget-usd` 显式传入），避免 clone 目录无 `.env` 丢失配置
- 并行度受控（`SemaphoreSlim`，1–16 可配，默认 4），单任务超时可配（默认 1800s），超时终止整个进程树（含 bash 子进程）
- 报告落盘 `.waycoder/batch/batch-report.md`：总计/成功/失败/总耗时 + 每个任务的摘要与错误详情

### 🧪 自测

- 新增批量任务引擎自测 26 项（JSON 解析/钳制/错误场景/名称消毒/远程判断/FromRepos/报告渲染/端到端克隆+并行+清理），1816→1842 项全部通过（0 失败）

## v0.48.9 (2026-08-14) — 多模态音频输入（transcribe 转录，对标 Codex CLI / Gemini CLI）

### 🎙️ 音频转录

- 新增 `transcribe` 工具：把本地音频文件上传到 Whisper 兼容端点（`/v1/audio/transcriptions`，multipart）转成文字，补齐多模态的「音频输入」短板——至此 WayCoder 同时支持图片（`view_image`）与音频（`transcribe`）两种多模态输入
- 支持 mp3/wav/m4a/flac/ogg/webm/aac/opus 等 19 种音频格式，自动映射 MIME 类型，25MB 大小上限
- 可选 `language`（ISO 语言代码）与 `prompt`（术语引导词）参数提高转录准确率
- 配置三件套（设置界面自动生成）：`WAYCODER_WHISPER_MODEL`（默认 whisper-1）、`WAYCODER_WHISPER_BASE_URL`（空=默认 api.openai.com）、`WAYCODER_WHISPER_API_KEY`（空=回退主 API Key）；支持 OpenAI Whisper / Groq / faster-whisper 任意兼容服务
- 归入 AutoModeClassifier「Safe」级（只读外部查询，自动放行，无需确认）

### 🧪 自测

- 新增 transcribe 自测 11 项（注册 + 格式支持 + MIME 映射 + 路径校验 + 配置默认值），内置工具 41→42，1816 项全部通过（0 失败）

## v0.48.8 (2026-08-14) — MCP 资源/提示词支持（对标 Claude Code resources/prompts）

### 🔌 MCP 能力补全

- 新增 **资源（resources）** 支持：`resources/list` 发现 + `resources/read` 读取，注册为 `mcp__<server>__resources` 工具——省略 `uri` 参数列出全部资源，传入 `uri` 读取指定资源内容（text/blob/嵌套资源统一格式化）
- 新增 **提示词模板（prompts）** 支持：`prompts/list` 发现 + `prompts/get` 调用，每个模板注册为 `mcp__<server>__prompt__<name>` 工具，参数从模板 `arguments` 数组自动生成 inputSchema
- 修复 MCP 发现结果解析 bug：`tools/list`/`resources/list`/`prompts/list` 的响应数据此前从响应顶层读取，实际位于 JSON-RPC `result` 字段下——导致工具发现一直为空（此前 `tools/call` 正确读 `result`、发现却读顶层，二者不一致）；本次统一改为 `result` 下读取
- 状态模型扩展：`McpServerState`/`McpServerInfo` 新增 `ResourceCount`/`PromptCount`，`/mcp` 命令与侧栏 MCP 区显示「N 工具 · M 资源 · K 提示词」

### 🧪 自测

- 新增 MCP 资源/提示词自测 15 项（资源工具名称/描述/参数/读取/列表 + 提示词工具名称/参数生成/调用 + `BuildParameters`/`ExtractContentText` 纯逻辑 + 状态计数），1804 项全部通过（0 失败）

## v0.48.7 (2026-08-14) — 内置自动升级 + winget/brew/apt 分发（对标 Claude Code `claude update`）

### ⬆️ 自动升级

- 新增 `UpdateChecker`：语义版本比较 + 当前平台 RID 探测 + release 资产匹配（纯逻辑与网络/文件操作分离，可确定性自测）
- 版本检查优先 **GitHub Releases**、失败回退 **Gitee Releases**（`WAYCODER_GITHUB_REPO` / `WAYCODER_GITEE_REPO` 环境变量可覆盖）
- 完整自替换：下载匹配平台的 `.tar.gz`/`.zip` → 解压单文件二进制 → 覆盖当前可执行文件；Windows 落 `.new` + `upgrade.bat` 重试脚本（退出后自动替换并重启），Unix 原子 `rename` 覆盖运行中二进制
- 极简 tar.gz 单文件解压（仅 `GZipStream` + `FileStream`，AOT 零风险，不引入 `System.Formats.Tar`）
- `/update` 命令（检查 + 更新日志详情）、`/update now`（自替换）、启动后台静默检查（有新版本才提示）
- `--update` CLI 一次性升级标志（幂等，已最新则提示后退出）

### 📦 分发渠道

- `packaging/winget/`：winget manifest（portable 类型，x64/arm64）
- `packaging/brew/`：Homebrew formula（osx-arm64 / osx-x64）
- `packaging/apt/`：`build-deb.sh` 打包脚本 + reprepro 仓库配置说明
- `.github/workflows/release.yml`：推送 `v*` 标签自动 NativeAOT 编译 4 平台并创建 Release + 上传资产
- 新增 `docs/安装与升级.md` 完整安装/升级/发布指南

### 🧪 自测

- 新增 `TestUpdateChecker` 14 项（版本比较 7 + RID 探测 2 + 资产名/URL 匹配 5），1789 项全部通过（0 失败）

## v0.48.6 (2026-08-14) — 工作模式下沉到 Agent 实例（修复混合模式并行污染）

### 🔧 实例级工作模式

- `Agent.WorkMode` 实例字段替代全局 `WorkModeManager.CurrentMode`（全局仅作 UI 镜像），每个槽位 Agent 持有自己的模式
- 修复混合模式并行污染：此前后台槽位会读到活跃槽位的模式（如 A 槽 Plan + B 槽 Build 并行时 B 槽被误判为 Plan 而阻止写文件）
- `Agent.OnWorkModeChanged` 回调携带槽位索引——后台槽位批准计划后自动切回 Build 只通知正确槽位，不再经全局 `ModeChanged` 事件污染活跃槽位
- Agent 主循环三处读取（模式提示 / 计划审批门 / 工具约束检查）+ 计划批准后切回 Build 全部改为读写实例字段
- `Program.cs` 新增 `WireSlotWorkMode` 绑定槽位时灌入模式 + 接线回调；Shift+Tab、`/mode` 命令同步更新活跃槽位 Agent 实例模式

### 🧪 自测

- 新增 `TestWorkModePerAgent` 10 项（默认 Build + 双实例独立 + 实例不影响全局 + 工具约束跟随实例 + 回调 + 审批门纯逻辑），1775 项全部通过（0 失败）

## v0.48.5 (2026-08-14) — 多会话真并行执行（对标 Claude Code 多窗口）

### 🧵 多槽位后台并行执行

- F1-F10 槽位从「切换」升级为「并行」：Agent 任务在后台线程运行，主循环不再阻塞，运行中可自由切换槽位查看/投递其他任务
- 输出按槽位路由：活跃槽位实时流式写屏（复用 ChatScreen 流式方法），非活跃槽位缓冲到槽位自身 `ChatMessages`，切换回时由 `RestoreTo` 完整展示
- `AgentSlot` 新增线程安全缓冲输出（`BufferedStartStream`/`BufferedAppendToken`/`BufferedFinishStream`/`BufferedAppendToLast`/`BufferedAddMsg`）+ 运行状态（`IsBusy`/`Cts`/`Sync` 互斥锁）
- 切换与输出路由共享槽位 `Sync` 锁，原子完成「检查活跃 + 写入」与「快照 + 改活跃槽位」，杜绝切换瞬间丢 token
- Esc 中断当前活跃槽位 Agent / Ctrl+Z 优雅暂停（空闲时正常下发），Ctrl+Q 仍为全量紧急退出
- 退出/崩溃自动保存全部非空槽位会话（`_auto` / `_auto_slotN`），后台任务的进度不丢
- 槽位内已有任务运行时拒绝重复投递，避免同槽并发冲突

### 🧪 自测

- 新增 `TestMultiSlotParallel` 12 项（槽位运行状态 + 缓冲流式输出 + 自动新建流式消息 + 追加），1765 项全部通过（0 失败）

## v0.48.4 (2026-08-14) — LSP 语言扩充 5→14

### 🔍 LSP 语言服务器扩充

- `LspTool` 从 5 种语言扩充到 **14 种**，对标 Cursor/Claude Code 的覆盖
- 新增 9 种：C/C++（`clangd`）、Java（`jdtls`）、Kotlin（`kotlin-language-server`）、Ruby（`solargraph`）、PHP（`intelephense`）、Lua（`lua-language-server`）、Bash（`bash-language-server`）、Swift（`sourcekit-lsp`）、Zig（`zls`）
- `ExtToLang` 扩展映射补齐（.c/.cpp/.h/.java/.kt/.rb/.php/.lua/.sh/.swift/.zig 等）
- `GetLanguageId` 同步补齐 LSP 语言 ID（c/cpp/java/kotlin/ruby/php/lua/shellscript/swift/zig）

### 🧪 自测

- 新增 `[LSP]` 语言覆盖 10 项（14 种语言 + 9 个服务器命令断言），1753 项全部通过（0 失败）

## v0.48.3 (2026-08-14) — /mcp 管理面板 + MCP 状态模型（对标 Claude Code /mcp）

### 🔌 `/mcp` 管理面板

- 新增 `/mcp` 命令：列出所有 MCP 服务器（名称/传输/状态/工具数），`/mcp reload [name]` 重连（省略 name 重连全部）
- `McpManager` 引入结构化状态模型：`McpServerStatus`（Connecting/Connected/Failed）+ `McpServerInfo`（不可变快照）+ `McpServerState`（内部运行时状态）
- `ReloadAsync` 重连：断开旧连接 → 移除旧工具 → 重新解析配置 → 重连 → 更新状态
- `McpManager.Servers` 暴露排序后的服务器状态快照，侧栏面板（Ctrl+B）MCP 区改用结构化显示（状态图标 + transport + 工具数）
- 连接状态由拼凑字符串改为状态机：握手失败/无工具/连接失败均记录精确状态与错误信息

### 🧪 自测

- 新增 `[MCP 状态]` 区块 12 项（枚举值 + Info 快照 + ToInfo 映射 + Reload 非空），1743 项全部通过（0 失败）

## v0.48.2 (2026-08-14) — /init 项目初始化（对标 Claude Code /init）

### 🚀 `/init` 项目初始化

- 新增 `/init [force]` 命令：扫描项目（语言/框架/构建工具/Git）→ 生成中文 `CLAUDE.md` 指导文件（对标 Claude Code /init）
- `ProjectInitializer.GenerateClaudeMd()` 纯逻辑生成：项目概述 / 常用命令 / 架构 / 开发规范 / 注意事项 五区块
- 命令检测复用 `ProjectContext.DetectProject()` 结果，补充构建/测试/lint 命令精确探测（.NET / Node / Go / Rust / Python / Makefile）
- 已存在 `CLAUDE.md` 时弹确认框（覆盖/取消），`force` 参数跳过确认
- 生成后下次启动自动注入系统提示词（复用现有 `ProjectContext.LoadInstructions()`）

### 🧪 自测

- 新增 `TestProjectInit`：生成结构 6 项 + 命令检测 11 项，1731 项全部通过（0 失败）

## v0.48.1 (2026-08-14) — MCP SSE 传输 + 竞品资源复用指南

### 🔌 MCP 三传输补齐（stdio / HTTP / SSE）

- 新增 legacy HTTP+SSE 双端点传输（`SseMcpTransport`）：GET `/sse` 事件流 + POST `/message` 发请求
- `McpTransportType` 枚举 + `DetectTransport` 自动探测（`sse` / `http` / `stdio`），配置字段 `transport` 优先
- `SseMcpTransport` 后台 SSE 读循环解析 `event:`/`data:` 行，`endpoint` 事件解析消息端点、`message` 事件按 `id` 匹配 pending 请求
- `ResolveEndpointUrl` 相对→绝对 URL 解析，空白/非法 data 安全返回 null
- 自测新增 `[MCP SSE]` 区块 8 项（DetectTransport ×4 + ResolveEndpointUrl ×4），1714 项全部通过

### 📚 竞品资源复用指南

- 新增 `docs/竞品资源复用.md`：按「协议是否开放」给竞品（Claude Code / Crush / Cursor / Aider / Cline）的插件/MCP/LSP/Skill 分级
- MCP 完全共用、LSP server 生态完全共用、SKILL 高度共用（容错 frontmatter）、插件外壳不可共用但内部资源可拆
- 附 `.waycoder/mcp_servers.json` 三 transport 配置示例 + 复用优先级建议

## v0.48.0 (2026-08-14) — 计划审批门 + 抓屏/多模态视觉 + SelfTest 拆分

### 🧠 计划审批门（对标 Claude Code Plan Mode）

- `计划` 模式（Shift+Tab）下模型产出计划后不再自动催促执行，而是就地弹出审批框
- 批准 → 自动切回 `建造` 模式继续执行；拒绝 → 停止并返回计划
- `Agent.ShouldPromptPlanApproval(mode, contentLen)` 纯逻辑判定 + `ChatScreen.ShowPlanApproval` 审批对话框（Y/N 快捷键）
- `WorkModeManager.ModeChanged` 统一同步槽位持久模式与状态栏，修复批准后状态栏仍显示"计划"的错位
- 非 TUI 环境（一次性模式/管道/测试）自动批准，不阻塞

### 📸 抓屏工具（`screenshot`）

- 新增 `screenshot` 工具：终端文本抓屏（去除 ANSI）+ 桌面 PNG 抓屏（macOS `screencapture`）+ 区域抓屏
- 桌面截图可选 OCR（检测到 tesseract 时自动提取文本）

### 👁️ 多模态视觉（`view_image`）

- 新增 `view_image` 工具：把本地图片附加到下一轮请求，让支持 vision 的模型直接"看图"
- `LLM.ModelSupportsVision` 门控：仅 gpt-4o/gpt-5/claude/gemini 等 vision 模型注入，DeepSeek 等文本模型自动跳过避免 400
- 配合 `screenshot` 实现「抓屏 → 看图修 bug」闭环

### 🧪 SelfTest 拆分

- 5879 行单文件 `SelfTest.cs` 拆为 11 个 partial 文件（`SelfTest.cs` 核心 + `Chunk1-9` + `Helpers`），单文件最大 863 行

### 🧪 自测

- 1706 项自测全部通过（0 失败）

## v0.47.12 (2026-08-13) — /pause 优雅暂停 + 主循环稳健性修复 + Windows 打包脚本

### ⏸️ 优雅暂停（Ctrl+Z）

- 新增 **Ctrl+Z 优雅暂停**：Agent 运行时按 Ctrl+Z，当前批次完成后自动「git commit → 写检查点 → 存会话」再停机，与 Esc 立即中断互补
- 只提交 Agent 自己改过的文件（`AutoCommitAsync(fallbackToGitStatus: false)`），不卷入与本任务无关的未提交改动
- 新增 `/pause` 命令（提示用法）；会话存到 `_auto`，重启后 `/resume` 可恢复

### 🐛 LLM 参数解析健壮性

- `ParseArgs` 改用 `JsonDocument` 解析（替代 `JsonNode`），容忍重复 JSON 键（后者覆盖，不再抛 `ArgumentException`）

### 🔧 主循环稳健性修复

- 修复主循环中途停滞被误判为完成而提前退出
- 任务进行中无工具调用不再误判为完成
- 自动续跑 `wasWriting` 标记与实际工具输出对齐（`已写入 / 已编辑 / 已创建`）

### 📦 Windows 打包脚本

- 新增 `scripts/package.ps1`（与 `package.sh` 功能对等，Windows 原生 PowerShell 打包）

### 🧪 自测

- 1676 项自测全部通过（0 失败）

## v0.47.11 (2026-08-13) — 输入框完善：光标定位 + 复制粘贴 + 双输入对话框 + 补全钩子 + 渲染残影修复

### 🖱️ 光标/选区错位修复

- **根因**：`GetAbsoluteX/Y()` 沿 `Parent` 链累加，无法反映窗口内容区的 `ContentLeft/ContentTop` 偏移（`RootView` 不设 `Parent`），导致对话框内输入控件光标定位错位
- **修复**：`TuiControl.Render()` 记录渲染时的绝对原点 `_lastAbsX/_lastAbsY`（在裁剪早退前设置），三个输入控件（`TuiInput`/`TuiTextArea`/`TuiRichEditor`）的 `GotoCursorPos()` 改用该坐标，窗口内光标不再跑偏/消失

### 📋 双输入对话框 + 复制粘贴

- **新增 `TuiDialog.InputLine()`**：单行输入对话框（`TuiInput`），与已有的多行 `Input()`（`TuiTextArea`）、密码 `Secret()` 并列，均带输入历史（`TuiInputHistory`）与 OK/Cancel 按钮
- **复制粘贴快捷键**：`Ctrl+C` 被全局保留为退出，故复制改用 **Ctrl+Insert**、粘贴改用 **Shift+Insert**（Linux/Win 通用），`TuiEditBase` 统一分发

### 🎣 补全输入钩子（触发提示框）

- `ChatScreen.RegisterPrefixHint(prefix, provider)` / `UnregisterPrefixHint`：允许外部注册任意前缀符号的提示项生成器，`BuildPrefixHints` 优先走钩子，`IsKnownPrefix` 统一判定内置（`/ @ ! #`）与自定义前缀

### ✨ 渲染闪烁/残影修复

- **建议面板浮层化**：`TuiControl` 新增 `Floating` 属性，`TuiVBox`/`TuiHBox` 布局跳过浮动子控件（Flex/尺寸/位置都不计入）——Tab 补全/前缀提示面板不再把输入区挤出屏幕
- **脏区补绘**：`TuiScreen.MarkDirtyRect()` + `ChatScreen` 记录建议面板上一帧矩形，移动/缩放/隐藏时补绘被遮挡的聊天内容
- **底色擦除修复**：脏区擦除先 `SGR 复位` 再填空格，`bg=0` 的空格不再残留浮层底色（如建议面板的 Bg=47）

### 🧪 自测

- 1671 项自测全部通过（0 失败）

## v0.47.10 (2026-08-13) — 一键多平台打包脚本（`scripts/package.sh` / `scripts/package.ps1`）

### 📦 多平台打包

- 新增 `scripts/package.sh`（bash）与 `scripts/package.ps1`（PowerShell/Windows 原生）：一条命令打包 6 个平台（win-x64 / win-arm64 / linux-x64 / linux-arm64 / osx-x64 / osx-arm64）
- **当前平台走 NativeAOT**（零依赖原生单文件），**其他平台走非 AOT**（自包含单文件 JIT，跨平台交叉发布）
- Windows AOT 自动把 VS Installer 目录加入 PATH（定位 vswhere.exe / MSVC 链接器）
- 每次打包前清理 `obj/bin`，避免不同 RID 之间的还原状态污染（误报 Cross-OS）
- 产物统一为 `dist/waycoder-<版本>-<RID>.zip`（Windows）或 `.tar.gz`（Linux/macOS），排除 `.pdb` 调试符号
- 版本号自动从 `Global.cs` 提取；支持命令行指定平台（如 `./scripts/package.sh win-x64 linux-x64`）

## v0.47.9 (2026-08-13) — 一键清理失效供应商（`--model prune`）

### 🧹 剪除失效供应商（`--model prune` / `/model prune`，别名 `clean`）

- 一条指令自动清理：逐一测试所有已存 API key，分三种失效情形处理
  - **仅 key 无效（401/403）**：供应商真实可达 → 只删 key、**模型保留**
  - **无法连接（超时/拒绝/写错地址）**：供应商本身不可用 → 删 key + 该供应商下所有自定义模型
  - **无端点（供应商不存在/拼错供应商）**：删 key + 该供应商下所有自定义模型
- 内置供应商/模型不删（仅删 key）；本地端点（Ollama/LM Studio）不参与
- 输出逐项报告：✅ 保留 / 🗑️ 已删除，末尾给出「删除 N 个 key + M 个自定义模型」结论
- 复用连通性探测（401/403=密钥无效、超时/拒绝=无法连接），与 `--model test` 同一套判定

## v0.47.8 (2026-08-13) — 连通性测试覆盖全部已存 key + 补充供应商端点

### 🔑 全量 key 扫描

- `--model test` 由「只测有目录模型的服务商」升级为「逐一测试**所有已存 API key** + 所有本地端点」——目录内无模型的供应商（如 Gitee / Bailian / OpenCode / MiniMax / AIHubMix）也会被扫描
- 新增供应商端点注册表：`gitee`（ai.gitee.com/v1）、`bailian`（dashscope 百炼）、`opencode`（opencode.ai/zen/v1）、`minimax`（api.minimaxi.com/v1）、`aihubmix`（aihubmix.com/v1）
- 探测结果细化：区分「密钥无效（401/403）」「端点可达但无 /models 接口（可能非 OpenAI 兼容）」「无法连接（超时/拒绝）」，避免误报
- 报告分「API Key」与「本地端点」两节，末尾给出「N/M 个端点可连接」结论

### 🧪 自测 +5

- 新增供应商注册表端点断言（gitee/bailian/opencode/minimax/aihubmix）

## v0.47.7 (2026-08-13) — 模型连通性测试 + 手动增删模型/服务商/API key

### 🔌 模型连通性测试（`--model test` / `/model test`）

- 测试所有「有 key 的服务商」+「所有本地模型」（Ollama / LM Studio / localhost）能否连上
- 按端点（服务商 + base_url）分组探测 `GET /models`：401/403=密钥无效、404 回退 `/v1/models`、超时/拒绝=无法连接
- 输出报告：每个端点 ✅/❌ + 所属模型 + 最终「N/M 个端点可连接」结论
- 有效 base_url 解析：显式 > 服务商默认 > 本地默认 `localhost:11434`

### ➕➖ 手动增删（`--model add` / `--model remove`，`/model` 同理）

- `add model <id> <供应商ID> [baseUrl]`、`add provider <供应商ID> [baseUrl]`、`add key <供应商ID> <key>`
- `remove model <id>` / `remove provider <供应商ID>` / `remove key <供应商ID>`（`remove <id>` 向后兼容删模型）
- 均写入全局模型库 / key 库并持久化；内置模型/供应商不可删

### 🧪 自测 +8

- 手动添加模型/服务商、按服务商删除、删除 key、连通性端点分组

## v0.47.6 (2026-08-13) — 模型库外置化：内置兜底 + 多来源导入（OpenCode/OpenClaw/Crush/Claude Code/Codex）

### 📚 模型库外置化（内置兜底）

- 模型目录拆为「内置精选 + 自定义库」：`~/.waycoder/models.json`（全局）优先，项目 `.waycoder/models.json`（本地）覆盖，找不到外置库时内置目录兜底——开箱即用
- `ModelCatalog.All` = 内置 + 自定义合并（自定义按 Id 覆盖内置、新增追加）；`--model list`、`/model`、`/provider`、模型选择框全部切换为合并目录

### 📥 外部模型库导入（`--model import` / `/provider import`）

- 支持 `opencode`（`~/.config/opencode/opencode.json`/`.jsonc`）、`openclaw`（`~/.openclaw/openclaw.json`）、`crush`（`~/.config/crush/config.json`）、`claude`/`claudecode`（`~/.claude/settings.json` env 中 `*_MODEL` + `BASE_URL`）、`codex`（`~/.codex/config.toml` 的 `[model_providers.*]` + `[profiles.*]`），或任意 JSON/TOML 文件路径
- `import` 无参 / `all` = 自动探测全部来源；导入内容持久化到全局 `models.json`；内置已有自动跳过；同一 Id 去重

### 🧭 /provider 命令

- `/provider`（当前服务商概览）、`/provider list`（服务商列表 + 模型数/key 状态/base-url）、`/provider <pid>`（该服务商模型列表）、`/provider apikey [set <pid> <key>]`、`/provider import [...]`

### 🐛 修复往返损坏

- 导入后写回 `models.json` 时错误复用外部格式解析器（`ParseModelNode` 会从 provider 显示名推断 providerId、硬编码 description），二次写回污染数据；新增专用 `FromJson`（精确往返）读回，`ParseModelNode` 仅用于外部导入

### 🧪 自测 +14

- Claude/Codex 导入解析、模型库序列化往返、自定义库合并/删除

## v0.47.5 (2026-08-13) — API key 统一走服务商 + 小模型服务商跟踪 + 命令行 key 自动入库

### 🔑 全局 key 库格式定版：`[{ "provider": ..., "apikey": ... }]`

- **问题**：上一版 `api_keys.json` 还是扁平 `{ "服务商": "key" }`，与「一个服务商一个 key、一个服务商多个模型」的心智不符
- **修复**：定版为数组 `[{ "provider": "deepseek", "apikey": "sk-..." }, ...]`（对标 OpenCode/Crush 多 key 全局存储），读写均按数组；兼容旧扁平格式与 OpenCode `{ pid: { key } }` 格式自动迁移
- **key 跟服务商走，不跟模型走**：`Config.ApiKey` 解析链 = 全局 JSON（按当前服务商）> 全局 JSON（按模型）> `.env WAYCODER_API_KEY` > 各家环境变量

### 🧩 大/小模型服务商独立跟踪

- **问题**：只有 `Provider` 跟踪大模型服务商，小模型切服务商后 key 解析会错
- **修复**：新增 `SmallProvider`（`WAYCODER_SMALL_PROVIDER`）字段；`--model small <id>` 子命令选中/持久化小模型并同步小模型服务商

### 🐛 修复 `.env` 污染

- **问题**：切换服务商后 `SaveToEnvFile` 会把旧服务商的 key 写成新模型的 `WAYCODER_API_KEY`（污染 `.env`，切 key 错乱）
- **修复**：`secret` 类型设置项永不写入 `.env`——key 只存全局 `api_keys.json`，`.env` 只存 `MODEL/PROVIDER/SMALL_*` 等非敏感项

### ⌨️ 命令行 key 自动入库

- `--api-key <key>` / `-k <key>` 自动保存到全局 `~/.waycoder/api_keys.json`（按当前服务商，或 `--model` 指定模型所属服务商），无需再手动 `--model key <供应商> <key>`

## v0.47.4 (2026-08-13) — 全局 JSON 多 key 存储：多服务商/模型丝滑切换，无需重输 key

### 🔑 API key 全局 JSON 回退（对标 OpenCode/Crush）

- **问题**：key 只能存 `.env`（单一 key），切换服务商/模型要手动改 `.env` 重输 key
- **修复**：`.env` 无 `WAYCODER_API_KEY` 时，按当前模型供应商自动到全局 JSON（`~/.waycoder/api_keys.json`）找 key——
  - `.env` 只存「当前模型名 + 当前 key」（单 key：`WAYCODER_MODEL` + `WAYCODER_API_KEY`）
  - `api_keys.json` 存「多服务商多 key」（`{ "deepseek": "...", "openai": "...", ... }`）
  - 切换即用：`--model name gpt-5.5` 自动匹配 openai 的 key，无需重输、无需改 .env
- **实现**：`ApiKeyStore.ForModel(modelId)` 按模型目录解析供应商再查 JSON；`Config.Instance.ApiKey` 加载链末尾追加该回退；「API 密钥未设置」报错补 `--model key <供应商> <key>` 引导

### 🧪 自测 +3

- 模型→供应商解析 deepseek/openai + `ApiKeyStore.ForModel` 可调用

## v0.47.3 (2026-08-13) — --model 模型管理：列表 / 选中 / API key / 端点，全程命令行

### 🤖 --model 子命令（对标 /model 斜杠命令）

- **问题**：模型管理（列表/选中/API key）只能在 REPL 内用 `/model`，CLI 只有一个 `--model <名称>` 会话级选择
- **修复**：`--model` 升级为贪长子命令分发器，与 `/model` 共享同一份模型目录（`ModelCli`）——
  - `waycoder --model` → 显示当前大/小模型 + BaseUrl
  - `--model list [关键词]` → 列出模型目录（按供应商分组，当前项标注）
  - `--model name <id>` → 选中并持久化（自动解析供应商 + 默认 BaseUrl + 写 .env）
  - `--model key <供应商> <key>` → 保存 API key（无参列出已存 keys，打码）
  - `--model connect <base-url>` → 设置连接端点（写 .env）
  - `--model <id>` → 快捷选中（仅本次会话，不持久化，向后兼容）
- **实现**：抽 `ModelCli` 静态助手（Current/List/Select/Connect/ListKeys/SetKey），`ModelArg` 变贪长子命令分发；`CliArg.Greedy` 语义改为「吞到下一个以 `-` 开头的旗标为止」，使 `--model gpt-5.5 -y` / `-p` 等组合仍正常解析

### 🧪 自测 +3

- `ModelCli.List` 含标题 / 过滤 deepseek / `ListKeys` 可读

## v0.47.2 (2026-08-13) — --config 命令行参数：启动即配置，无需进 REPL

### ⌨ --config 命令行配置（对标 /config 斜杠命令）

- **问题**：`/config` 只能在 REPL 内使用，脚本/批处理/远程部署场景无法在启动时读写配置
- **修复**：新增 `--config`（短名 `-C`）命令行参数，与 `/config` 共享同一份 Schema 数据源（`ConfigCli`）——
  - `waycoder --config` 或 `--config list` → 列出全部设置项（按分类，含当前值，secret 打码）
  - `--config get <key>` → 读取单项（含描述 / 环境变量 / 可选项）
  - `--config set <key> <value>` → 设置并**立即写入 .env**
  - 简写：`--config <key> <value>` = set，`--config <key>` = get
- **实现**：抽 `ConfigCli` 静态助手（List/Get/Set 返回纯文本），`ConfigCommand`（→屏幕）与 `ConfigArg`（→控制台）共用，消除重复；`CliArg` 新增 `Greedy` 标志 + 解析器贪婪吞参，支持 `--config set <key> <value>` 变长参数

### ⌨ Tiny 模式并入 test 前缀分组

- `-T` / `--tiny` → `-tt` / `--test-tiny`（保留 `--tiny` 别名），与 `-t` / `-tb` / `-tl` 统一为 `-t` 测试族前缀

## v0.47.1 (2026-08-13) — CLI 参数全部补齐短命令

### ⌨ 命令行参数补短选项（频率排序 + 同类前缀分组）

- **问题**：`--tiny` / `--economy` / `--bench` / `--limits` / `--sessions` 只有长名，无短命令
- **命名规则**：
  - 高频参数：首字母小写（`-p` prompt / `-m` model / `-h` help / `-k` api-key / `-t` test / `-e` economy / `-w` watch / `-y` yolo / `-r` resume / `-s` sessions / `-b` base-url / `-v` version / `-i` init / `-d` debug）
  - 低频冲突：首字母大写（`-B` max-budget-usd）
  - 同类测试参数：统一 `-t` 前缀分组（`-t` test / `-tb` test-benchmark / `-tl` test-limits / `-tt` test-tiny）
  - 内部开发参数：`-x` screenshot / `-u` tui-demo / `-z` theme-verify
- 使用手册 CLI 参数表同步更新

## v0.47.0 (2026-08-13) — /config 命令行配置：所有设置项无需进界面

### ⌨ /config 命令行配置（对标 Claude Code /config）

- **问题**：所有配置项只能通过 `/settings` 图形界面逐项点选，脚本/批处理/远程场景无法设置
- **修复**：新增 `/config` 命令，全部设置项（Model/SmallModel/ApiKey/BaseUrl/超时/压缩/沙箱/界面主题…）均可在命令行读写——
  - `/config` 或 `/config list` → 按分类列出全部设置项与当前值（secret 打码）
  - `/config get <key>` → 读取单项（含描述 / 环境变量 / 可选项）
  - `/config set <key> <value>` → 设置并**立即写入 .env**（`SaveToEnvFile`）
  - 简写：`/config <key> <value>` = set，`/config <key>` = get
  - key 大小写不敏感，也可用环境变量名（`/config set WAYCODER_MODEL x`）
  - select 类型校验可选项（错误时列出合法值），number 类型自动钳制（复用 Schema Setter）
  - 主题类设置（ThemePreset/ColorScheme/Border*）改后即时 `SyncTheme` 生效
- **实现**：`Config` 新增 Schema 驱动的 `FindProp`/`GetPropValue`/`TrySetPropValue`，复用同一份 `_schema` 数据源，**消除 SettingsScreen 手写 switch 的重复**；`/settings` 保留图形界面，`/config` 专注命令行
- **兼容**：语法对齐 Claude Code 的 `get`/`set`/`list`，老用户零学习成本

### 🧪 自测 +9

- 新增 `/config` 读写 API 用例：FindProp 按 Key/大小写/环境变量、GetPropValue、TrySetPropValue 成功、非法 select 拒绝、未知项拒绝

## v0.46.0 (2026-08-13) — Token 计数切真实 API 报告：校准消除系统性低估

### 📊 压缩触发切真实 API 校准（P1，对标 Crush 纯 API 报告）

- **问题**：`ContextManager.MaybeCompressAsync` 的三层压缩阈值（裁剪/摘要/折叠）用 `EstimateTokens`（CJK 感知估算）判断，但估算只统计消息内容，**漏掉 system prompt + 工具定义 + 消息元数据**（固定开销约 8–12K），导致压缩整体触发偏晚
- **修复**：用真实 API 报告的 `prompt_tokens` 校准估算——
  - `AddUsage(promptTokens, completionTokens, estimatedTokens)` 新增可选参数，计算 `固定开销 = 真实 prompt − 估算`，移动平均平滑收敛
  - 新增 `EstimateCalibratedTokens()` = 原始估算 + 固定开销（加性模型：system prompt/工具定义固定，不随内容增长，比比例模型更准）
  - `MaybeCompressAsync` 三层判断全部改用校准值，`PreCompact` hook 报告同步校准
  - 未采集到真实用量时（首轮/自测）退化为原始估算，零风险
- **收益**：压缩触发时机更准（校准前 50% 阈值实际 59% 才触发，校准后对齐真实窗口占用），少误压/漏压

### 🧪 自测 +4

- 新增 `TestTokenEstimation` 校准用例：无真实数据退化 / 含固定开销 / 开销平滑收敛 / 校准值 > 估算

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

## v0.43.0 (2026-08-13) — 省 Token 模式：三态开关 + 任务复杂度自适应

### 💰 省 Token 模式（`--economy [on|auto|off]` / `WAYCODER_ECONOMY` + `WAYCODER_ECONOMY_PRIORITY`）

- **新增** `EconomyMode` 三态开关（默认 `off`），保持正常窗口不变：
  - **关（off）**：完整提示词 + 正常压缩阈值
  - **开（on）**：从四个方面综合降 token——
    - 系统提示词精简（`SystemPrompt.GenerateEconomy` 砍 RepoMap/Git/记忆/10 阶段流水线，保留完整工具描述 + 项目上下文 + 9 条核心规则）
    - 压缩更激进（snip/summarize/collapse 50/70/90 → 35/55/75）
    - 工具输出更早裁剪（4000 → 2000 字符）
    - 输出上限收紧（`max_tokens` 32768 → 8192）
  - **自动（auto）**：保持完整提示词，压缩阈值/裁剪阈值按**任务轮数复杂度**动态插值——任务越复杂（轮数越多）越少省，先保质量、再省费用
- **新增** `EconomyPriority` 优先级偏好（仅 Auto 生效，默认 `quality`）：
  - `quality` 质量优先：简单任务省、复杂任务几乎不省
  - `balanced` 均衡：始终保留一半省钱力度
  - `cost` 费用优先：尽量省，弱化复杂度影响
- **与 Tiny 的区别**：Tiny = 极简提示词 + 4K 小窗口（面向本地小模型）；Economy = 保持正常窗口，仅综合省 token（面向云端大模型省钱）
- **`reasoning_effort` 不做最小化**：对不支持推理参数的非 DeepSeek/OpenAI 模型会 400，风险大于收益；reasoning 省 token 已由「reasoning_content 不存历史」覆盖

### 🧪 自测 +26（1591 → 1617）

- 新增 `TestEconomyMode`（三态默认值 / 优先级偏好 / ResolveRatio 复杂度插值 / 提示词精简 / snip 阈值对照 / 常量）

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
