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
