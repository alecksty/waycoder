# WayCoder v0.79.3 发布说明

> 中文版易用编程智能体（C#/.NET 10 AOT 单文件 exe）。本版本从 v0.71.29 起累计大量更新：**GUI 版全面完善**、**Web 版界面美化**、**模型按供应商分类存储**、**安全加固**。

## ✨ 亮点速览

- 🖥️ **GUI 版全面对齐 Web**：三栏布局 + 气泡聊天 + 右侧数据面板 + 模型弹窗 + 深/浅主题 + 完整 Markdown/表格/代码高亮渲染
- 🌐 **Web 版输入框重设计**：Composer 圆角卡片 + 模型工具栏内嵌 + 发送箭头（对标主流 AI 聊天页）
- 🗂️ **模型按供应商分类存储**：`provider/*.json` 分文件（opencode/deepseek/openai/locals…），旧 models.json 自动迁移
- 🚀 **OpenCode 在线导入**：一键拉取 `https://opencode.ai/zen/go/v1/models` 23+ 模型，按 id 前缀自动归类到各供应商
- 🔒 **安全加固**：修复 BashGuard 白名单绕过、PDF/CFB 解析器崩溃、SSRF DNS 重绑定、路径穿越等

---

## 🖥️ GUI 版（Avalonia）—— 全新完善

自 v0.79.3 起，GUI 版从单列 MVP 升级为与 Web 版同等完善：

| 功能 | 说明 |
|---|---|
| **三栏布局** | 左（槽位 F1-F10 + 历史会话列表）/ 中（气泡聊天 + Composer）/ 右（5 数据面板） |
| **消息气泡** | user 右 / assistant 左 / 工具 / 系统 分角色，流式合帧渲染（根治卡顿） |
| **完整渲染** | 表格（Grid）、代码语法高亮、Markdown（标题/粗体/列表/引用/链接/代码块）、文本段落 |
| **右侧面板** | 任务 / Token费用 / 修改文件 / MCP / LSP，2s 自动刷新 |
| **模型弹窗** | 搜索 + 供应商分组 7 列表格 + 扫描/自动导入/OpenCode 在线/设置 key/保存/切换 |
| **Composer** | 大/小模型 + 省钱 + 交互模式 + 发送箭头（busy 变停止）；输入自动增高 + Enter 发送 + 多槽位草稿 |
| **主题切换** | 深/浅色动态资源绑定，配置持久化 |
| **历史会话** | 列表预览 + 加载/重命名/删除/新建/清空（按槽位隔离） |

启动：`dotnet run --project WayCoder.Gui` 或 `waycoder --gui`

## 🌐 Web 版界面美化

- 输入区改为 **Composer 圆角卡片**（聚焦光晕 + 发送箭头 + 空输入禁用态），模型/省钱/交互模式工具栏内嵌输入框
- 底部整体上移、去横线，消息区与输入区呼吸感更佳

## 🗂️ 模型管理

- **按供应商分类存储**：全局 `~/.waycoder/provider/{供应商}.json`（本地模型归 `locals.json`），旧 `models.json` 首启自动迁移
- **OpenCode 在线导入**：模型弹窗「🌐 OpenCode 在线」一键导入 23+ 模型，按 id 前缀自动归类（minimax-*→minimax、kimi-*→moonshot、glm-*→zhipu…），baseUrl 保留 opencode 网关

## 🔒 安全与健壮性（v0.79.1）

- **BashGuard**：移除 `env`/`git config` 只读白名单，`find` 危险 flag（`-exec`/`-delete`）拦截
- **解析器护栏**：PDF 内容流递归深度、CFB DIFAT 循环、FlateDecode 解压上限（zip bomb）、xref count 钳制、JPEG 采样数组
- **SSRF**：ConnectCallback 原子「解析+校验+连接」，防 DNS 重绑定
- **路径防护** `PathSafety`：拦截 SSH 密钥 / shell 配置 / 云凭据读写
- **并发**：Messages 锁纪律、PendingImages 按 agentId 分队列、slowloris 读超时

## ⚙️ 自测

- 主项目自测 **3561 通过**，编译 0 警告 0 错误
- GUI 项目编译 0 错误

---

**下载**：见本 Release 附件（Windows/macOS/Linux 单文件 exe）。

**源码**：https://gitee.com/aleckstygit/my-coder

**使用手册**：见仓库 `docs/使用手册.md`
