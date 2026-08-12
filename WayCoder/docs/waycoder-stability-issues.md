# WayCoder 稳定性测试问题记录

> 测试场景：让 WayCoder 写一个 ~10000 行的 Roguelike 游戏项目
> 日期：2026-08-12

---

## 1. FATAL 崩溃

### 1.1 HttpClient.Timeout 动态修改
- **严重程度**: FATAL（进程崩溃）
- **现象**: `_http` 是 `static readonly HttpClient`，渐进超时重试每次尝试都改 `_http.Timeout`，第一次请求后 `Timeout` 属性不可写，抛 `InvalidOperationException`
- **位置**: `Agent/LLM.cs` — `CallWithRetryAsync()` 中 `_http.Timeout = TimeSpan.FromSeconds(thisTimeoutSec)`
- **状态**: ✅ 已修复 — 改为 `Timeout.InfiniteTimeSpan` + CTS 控制超时

### 1.2 JSON 流式截断 → 空路径 → 工具崩溃
- **严重程度**: ERROR（不崩进程但工具返回异常）
- **链条**: LLM 流式返回被截断 → `JsonReaderException` → 工具调用 JSON 不完整 → `file_path` 字段为空 → `Path.GetFullPath("")` 抛 `ArgumentException`
- **位置**: `Tools/WriteFileTool.cs:44`, `Tools/EditFileTool.cs:66`, `Tools/ReadFileTool.cs:67` 等
- **状态**: ✅ 已修复 — WriteFileTool, EditFileTool, ReadFileTool, MultiEditTool, DownloadTool, NotebookEditTool 加了空值校验

---

## 2. 上下文管理问题

### 2.1 过度阅读已有代码
- **现象**: 每次指令"不要读已有代码"，WayCoder 仍然先 ls/tree/read_file 所有现有文件
- **后果**: 读完 6-10 个文件后上下文耗尽，一个文件都没写出来
- **复现率**: ~40%（7 次运行中 3 次失败）
- **严重性**: 高 — 导致任务完全无法推进

### 2.2 过度规划（Over-planning）
- **现象**: 第一次运行花了整个上下文窗口设计架构（~50 个文件的目录结构、每文件行数估算），零代码产出
- **后果**: 上下文耗尽退出，退出码 0（非崩溃，静默失败）
- **严重性**: 中 — 任务无法推进，但不崩溃

### 2.3 代码在思考流中生成但未写入磁盘
- **现象**: WayCoder 在 `«dim»` 思考中逐行生成代码，但上下文在写完代码前耗尽，`write_file` 工具调用从未发出
- **后果**: 代码全部丢失
- **复现**: CombatSystem.cs 第一次尝试（379 行输出全是思考中的代码）
- **严重性**: 高 — 无声失败，用户以为在写但实际什么都没落盘

### 2.4 "继续"后重写已有文件且缩小
- **现象**: 第二次让它"继续"，它读取了已有文件后重新思考，然后用更短的版本覆盖了 Room.cs 和 Entity.cs
- **后果**: Room.cs 从 54 行变成 23 行，Entity.cs 从 87 行变成 44 行，总行数从 616 降到 602
- **严重性**: 中 — 丢失已有进度

---

## 3. 工具调用问题

### 3.1 write_file 缺少 file_path 参数
- **现象**: 模型输出 `write_file()` 不带 file_path，工具返回错误，需要重试
- **复现**: Player.cs 生成时出现 3 次
- **严重性**: 低 — 能自恢复，但浪费上下文

### 3.2 文件路径错误
- **现象**: 写到 `D:\code-agents\WayCoder\games\roguelike\` 而不是 `D:\code-agents\WayCoder\WayCoder\games\roguelike\`（少了一层目录）
- **原因**: 理解相对路径 `WayCoder/games/` 时出错
- **严重性**: 低 — 文件能找回，但用户不可见

### 3.3 工具调用 JSON 截断
- **现象**: LLM 输出的工具调用 JSON 不完整（被流式截断），解析失败
- **日志**: `JsonReaderException: Expected end of string, but instead reached end of data`（字节位置 371/12789/17578）
- **后果**: 工具调用失败，模型需要重试
- **严重性**: 中 — 频繁发生，浪费 token 和上下文

---

## 4. 代码质量问题

### 4.1 类型/命名空间冲突
- **现象**: Player.cs 重新定义了已有的 `Item` 类（Actor.cs 中已定义 `abstract class Item`）
- **原因**: 未读已有代码导致 API 不匹配
- **后果**: 编译失败（或需要后续手动修复）
- **严重性**: 中 — "不要读代码"和"代码需编译"是矛盾的需求

### 4.2 命名空间不统一
- **现象**: Core 目录用 `Roguelike.Core`，Entities 目录用 `WayCoder.Games.Roguelike.Entities`
- **原因**: 第一次运行创建立的文件用了不同约定
- **严重性**: 低 — 不影响运行但混乱

### 4.3 跨文件 API 不一致
- **现象**: 每个文件因为"不读已有代码"而猜测 API，导致不同文件使用不同的属性名/方法签名
- **后果**: 项目无法编译，需要大量手动修复
- **严重性**: 高 — 批量生成的文件互不兼容

---

## 5. 效率问题

### 5.1 每次对话只能写 1 个文件
- **现象**: 每写 1 个文件（~350 行）需要一次完整的对话（上下文满载）
- **效率**: 10,000 行需要约 30 次独立对话
- **严重性**: 中 — 能工作但极慢，且每次对话后上下文丢失

### 5.2 上下文利用率低
- **现象**: 380 行输出的对话中，约 80 行是实际代码思考，其余 300 行是读文件/分析/纠结
- **严重性**: 低 — 对话资源浪费

---

## 6. 已知但未修复的问题

| # | 问题 | 严重度 | 建议 |
|---|------|--------|------|
| 1 | "不要读文件"指令被忽略 | 高 | 可能需要系统提示词层面支持"skip exploration"模式 |
| 2 | 思考流中代码未写入磁盘 | 高 | write_file 应在代码生成前调用，而非之后 |
| 3 | 上下文压缩静默退出（exit 0） | 中 | 压缩后应主动提示用户"需要继续"而非静默退出 |
| 4 | "继续"后可能缩小已有文件 | 中 | 继续时应追加而非重写 |
| 5 | JSON 流式截断 | 中 | 可能是 LLM max_tokens 不够或上下文裁剪 |
| 6 | LintTool/TreeTool/WcTool 等缺少空路径校验 | 低 | 补齐所有 Path.GetFullPath 调用点的校验 |
