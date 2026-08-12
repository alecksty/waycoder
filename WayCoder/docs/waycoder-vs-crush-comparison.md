# WayCoder vs Crush 竞品对比分析

> 2026-08-12, v0.36.0 调研

对 Crush v2.1.88（Go 版 Claude Code 开源实现，347 个 .go 文件）做系统性对比分析。

---

## 1. Agent 主循环

| 维度 | Crush | WayCoder |
|---|---|---|
| 循环机制 | 框架代理（`agent.Stream()` 单次调用） | 显式 `for` 循环，硬上限 `_effectiveMaxRounds` |
| 阶段感知 | 无 | 10 阶段流水线（提示词层面） |
| 续写 | 消息队列触发递归 `Run()` | 单循环直到文字结束或耗尽回合 |

**WayCoder 优势**: 硬上限防无限消耗、逐轮 lint/test 反馈注入。
**Crush 优势**: 统一流生命周期（`OnReasoningStart/Delta/End` 回调）。

---

## 2. 工具调用 JSON 解析

| 维度 | Crush | WayCoder |
|---|---|---|
| 解析 | Fantasy SDK 框架层完成 | 手动 SSE 解析 + JSON 片段拼接 |
| 截断检测 | N/A（框架内部） | `IsJsonProbablyComplete()` 花括号平衡试探 |
| 异常处理 | `{}`（空 JSON） | `ParseArgs` 返回空字典 + 日志（v0.36.0 修复） |

**WayCoder 优势**: `IsJsonProbablyComplete()` 流式完整性检测器，可提前执行工具。
**WayCoder 风险**: 花括号试探可能被字符串内含代码误导。v0.36.0 已修复错误参数泄漏。

---

## 3. 推理/思维链处理

| 维度 | Crush | WayCoder |
|---|---|---|
| 存储策略 | **存入消息历史**（`AppendReasoningContent`） | **丢弃**（仅显示 + 侧缓冲区调试） |
| 签名保留 | 多供应商（Anthropic/Google/OpenAI） | 无 |
| 代码检测 | 无 | 检测推理中代码（>20 +;或 {）防丢失 |

**Crush 优势**: 存储推理允许 LLM 后续引用思考过程。
**WayCoder 优势**: 代码检测捕获 DeepSeek V4 在推理中生成代码的静默失败。

---

## 4. 反循环/停滞检测

| 维度 | Crush | WayCoder |
|---|---|---|
| 检测粒度 | 按 step（同一轮所有工具调用合并） | 按 tool-call（单个指纹） |
| 响应方式 | **硬停止**（StopWhen 返回 true） | **渐进式催促**（3 级递进） |
| 额外检测 | 无 | 分析不动手 + 口述代码 + v0.36.0 推理独占 |

**WayCoder 优势**: 3 种检测轴 × 3 级递进 = 9 种防御组合。Crush 的纯哈希检测漏掉"变种文字无工具"的停滞模式。
**Crush 优势**: 硬停止 + 摘要后重入队 = 干净重启。

---

## 5. 上下文压缩

| 维度 | Crush | WayCoder |
|---|---|---|
| 层数 | 1（仅 LLM 摘要） | **3 层**（裁剪 → 摘要 → 硬折叠） |
| 触发方式 | 流内 StopWhen（单点） | 每轮 `ShouldStopAndSummarize()` |
| Token 计数 | **真实 API 报告** | 估算（CJK 感知试探） |
| 摘要模型 | 同大模型 | **小模型**（省钱） |

**WayCoder 优势**: 第 1 层（工具输出裁剪）常避免 LLM 摘要调用，省 token + 省钱。
**WayCoder 风险**: 估算 Token 计数偏差 ±15%。

---

## 6. 文件追踪

| 维度 | Crush | WayCoder |
|---|---|---|
| 存储 | 持久化（SQLite） | 内存（Dictionary） |
| 检测方式 | 时间戳比较 | SHA256 哈希 |
| 容量上限 | 无（DB 支撑） | 200 条目（LRU 淘汰） |

**Crush 优势**: 持久化 = 跨会话保护。会话隔离 = 多会话并发安全。
**WayCoder 优势**: 哈希检测能发现不改变时间戳的内容变化（如 git checkout）。事后 bash 变更警告（Crush 无）。

---

## 7. Bash 工具

| 维度 | Crush | WayCoder |
|---|---|---|
| 后台命令 | **自动迁移**（超时后自动转后台） | 手动 `run_in_background` |
| 安全层数 | 2 层（命令名 + 参数匹配） | **3 层**（命令名 + 安全白名单 + 危险正则） |
| 输出保留 | 8 小时（`CompletedJobRetentionMinutes`） | 一次性捕获 |
| 沙箱 | 无 | `SandboxManager`（内存/CPU 监控） |

**WayCoder 优势**: 3 层防御、正则检测 `rm -rf /` `/dev/`、沙箱隔离。
**Crush 优势**: 自动后台迁移是核心 UX 模式，避免阻塞 agent。

---

## 8. 权限系统

| 维度 | Crush | WayCoder |
|---|---|---|
| 架构 | **发布/订阅事件总线**（工具发出 PermissionRequest，UI 订阅） | 直接调用确认 |
| 持久性 | 会话级（`GrantPersistent` vs `Grant`） | 模式级（Yolo/Ask/SmartAuto） |
| 安全命令绕过 | 在工具内部 | **集成到权限检查**（`BashGuard.IsSafeReadOnly()`） |

**Crush 优势**: 事件总线解耦工具执行与 UI。WayCoder 可考虑对标改造。
**WayCoder 优势**: 4 种权限模式 + 智能自动分类 + 安全命令无感。

---

## 9. 配置系统

| 维度 | Crush | WayCoder |
|---|---|---|
| 作用域 | 多作用域（全局 + 工作区 JSON） | 单文件 `.env` |
| 自动重载 | 文件快照 + 陈旧检测 | 手动 `Reload()` |
| 配置项定义 | 手动赋值 | **Schema 驱动**（一行添加，自动生成 FromEnv / SettingSchema / SaveToEnvFile） |

**WayCoder 优势**: Schema 驱动是 AOT 安全的创新——Crush 没有等效机制。
**Crush 优势**: 多作用域配置更灵活。

---

## 总结

### WayCoder 的差异化优势（14 项）
1. 3 层上下文压缩（优于 Crush 的 1 层）
2. 3 轴 × 3 级反循环检测（优于 Crush 的纯哈希）
3. Schema 驱动配置（Crush 无等效）
4. 3 层 Bash 安全防御 + 危险正则
5. 沙箱集成（内存/CPU 监控）
6. 4 种权限模式 + 智能自动
7. 逐轮 lint/test 反馈闭环
8. `IsJsonProbablyComplete()` 流式执行
9. 推理中代码丢失检测
10. 安全命令无感绕过
11. 流式工具输出 TUI 显示
12. 工作区隔离（worktree 检测）
13. cd 追踪（AsyncLocal）
14. Token 花费独立追踪

### Crush 值得 WayCoder 学习的模式（6 项）
1. **后台命令自动迁移**（超时自动转后台）- 提升体验
2. **文件读取时间戳保护**（工具层强制先读后改）- 提升安全性
3. **事件总线权限架构**（pub/sub 解耦）- 更优雅
4. **多供应商原生支持**（Fantasy SDK）- Anthropic/Google 原生 API
5. **Git 状态注入提示词**（分支/status/最近提交）- 更多上下文
6. **多作用域配置**（全局 + 工作区）- 更灵活
