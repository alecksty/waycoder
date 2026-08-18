# GUI（图形界面）

Avalonia 12 图形界面，对标 Web 版（`WayCoder/UI/WEB/`）的聊天交互。

命名空间：`WayCoder.UI.Gui`（实现在 `WayCoder.Gui/` 项目）。

## 架构

- **独立 JIT 进程**：`WayCoder.Gui.csproj` 是 Avalonia（JIT）可执行文件，与主程序 AOT 单文件分离——Avalonia 依赖反射，无法 NativeAOT。
- **核心抽库（shared-source）**：GUI 项目通过 `<Compile Include="../WayCoder/**/*.cs">` 复用主项目核心源码（Agent/LLM/Tools/Infra/Memory 等），排除 CLI 入口（Program*.cs）、CLI 命令层（UI/CLI）、测试（Test）、插件（Plugins）、斜杠命令（SlashCommand）。
- **占位桩**：`CoreStubs.cs` 提供核心源码引用的 CLI 专属类型（ISlashCommand/SlashCommandRegistry/ProgramContext/PluginRegistry）的空实现，使核心在 GUI 进程编译通过。

## 启动

```bash
dotnet build WayCoder.Gui
dotnet run --project WayCoder.Gui          # 直接启动 GUI
waycoder --gui                              # 主程序拉起 GUI 进程

# 发布自包含可执行（独立分发，无需 .NET 运行时）
dotnet publish WayCoder.Gui -c Release -r osx-arm64 --self-contained true
```

## 进度

- [x] 窗口 + 聊天区 + 输入框 + 发送/停止（流式 onToken 回填）
- [x] 模型下拉切换
- [x] F1-F10 多槽位（独立 Agent + 各自历史）
- [x] 权限确认/提问对话框（GuiInteraction 交互桥）
- [x] Markdown + «» 标记富文本渲染（代码块/标题/列表/引用/颜色/粗体/行内代码）
- [x] 会话持久化（退出保存 _auto/_auto_slotN，启动恢复）
- [x] diff 预览确认（接受/拒绝全部）
- [x] 设置对话框（API Key / Temperature / MaxTokens / 自动提交）

## v0.79.2 界面完善（对齐 Web 版）

- [x] **三栏布局**：左（槽位 F1-F10 + 历史会话列表）/ 中（聊天 + composer）/ 右（5 数据面板），顶栏含 logo + 主题切换 + 模型按钮
- [x] **消息气泡化**：`ChatMessage`/`MessageBubble` 结构化消息，逐气泡渲染 + 流式合帧（根治全量重渲染 O(n²)）
- [x] **右侧 5 面板**：任务 / Token费用 / 修改文件 / MCP / LSP，`DispatcherTimer` 2s 刷新，数据直连主项目静态类
- [x] **模型选择对话框** `ModelWindow`：搜索 + 供应商分组 7 列表格 + 扫描/自动导入/OpenCode在线/设置key/保存/切换
- [x] **Composer 工具栏**：大/小模型按钮（开弹窗）+ 💰省钱 + 🛡交互模式 + 发送箭头（busy 变 ⏹ 停止）
- [x] **深/浅主题切换**：`App.ToggleTheme` + 动态资源绑定，`GuiTheme` 配置持久化
- [x] **历史会话管理**：列表预览/元信息 + 加载/重命名/删除/新建/清空（按槽位隔离）

## v0.79.2 渲染完善（块级 Markdown）

- [x] **块级渲染 `MarkdownBlocks`**：消息气泡改为多 block 容器，完整支持段落/标题/引用/列表/分隔线/代码块/表格
- [x] **表格**：`| a | b |` 解析为 Grid（表头加粗 + 边框）
- [x] **代码语法高亮 `SimpleHighlight`**：注释/字符串/数字/关键字着色（对齐 Web tok 配色）
- [x] **Markdown**：粗体/行内代码/链接/列表/引用
- [x] **主题联动**：block 文字色从主题资源读取，切主题重建气泡

## v0.79.6-v0.79.12 三端对话框/格式对齐

- [x] **聊天格式**：气泡角色（user右/assistant左/工具/系统/**推理淡色独立气泡**）+ Markdown/表格/代码高亮/链接（`MarkdownBlocks`）+ 工具输出代码块
- [x] **附件上传**：📎 接 `OpenFileDialog`，图片入 vision 队列 / 音频转录，槽位 `AgentId` 隔离
- [x] **斜杠命令**：`/help /model /settings /theme /reset /todos /tokens /perm /slots`（未知命令回退普通消息）
- [x] **模型选择**：`ModelWindow` 搜索 + 供应商分组 7 列表格 + 扫描/导入/OpenCode/设置key/保存/切换
- [x] **Diff 逐 hunk**：`DiffConfirmAsync` 每 hunk CheckBox 勾选 + 全部接受/拒绝/应用所选
- [x] **设置 Schema 驱动**：`Config.SettingSchema()` 动态生成全量配置（分类 + 各类型控件）
- [x] **权限确认**：工具图标标题 + bash 命令绿/路径青着色 + 三键
- [x] **单选/多选**：多选 CheckBox 列表 / 单选 ListBox
- [x] **系统通知**：`UxHelper.OnNotify` 显示 Info/Success/Warn/Error 到聊天流
