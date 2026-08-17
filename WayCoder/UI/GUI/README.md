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
```

## 进度（MVP）

- [x] 窗口 + 聊天区 + 输入框 + 发送/停止（流式 onToken 回填）
- [ ] 模型下拉切换
- [ ] F1-F10 多槽位
- [ ] Markdown/中间格式富文本渲染（当前仅剥 `«»` 标记）
- [ ] 会话管理 / 设置 / 权限确认
