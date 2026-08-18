# WayCoder v0.79.15 发布说明

> 中文版易用编程智能体（C#/.NET 10 AOT 单文件 exe）。本版本（v0.79.4 → v0.79.15）完成**四端（CLI/TUI/Web/GUI）全部可用**，并将三端界面（TUI/Web/GUI）的**对话框与聊天格式全面对齐**。

## ✨ 亮点速览

- 🖥️ **四端全部可用**：CLI / TUI 全屏 / Web 浏览器 / GUI 桌面，另支持**管道输入**（`echo "任务" | waycoder`）
- 🎛️ **新增界面参数**：`--tui` 强制 TUI、`--cli` 强制 CLI 文本界面
- 🔄 **三端对话框全面对齐**：模型选择 / Diff 逐 hunk / 设置 Schema / 权限 / 单选多选 / 系统通知 / 附件上传 / 斜杠命令 / 推理内容独立气泡
- 🔒 **数据安全加固**：模型迁移防误删、文件并发原子化
- 🐛 **修复**：TUI 非交互崩溃、Web 忙碌状态丢失等

---

## 🎛️ 四端界面 + 管道输入

| 入口 | 命令 |
|---|---|
| TUI 全屏 | `waycoder` / `waycoder --tui` |
| CLI 文本 | `waycoder --cli`（`»` 提示符逐行交互，exit/quit 退出） |
| Web 浏览器 | `waycoder --web` |
| GUI 桌面 | `waycoder --gui` |
| 一次性 | `waycoder -p "任务"` / `--json` |
| 管道 | `echo "任务" \| waycoder` |

## 🔄 三端对话框/格式对齐（v0.79.6-12）

| 对话框 | 能力 |
|---|---|
| **模型选择** | 供应商分组 + 搜索 + 扫描 ✅/❌ + 自动导入 + OpenCode 在线 + 设置/清除 key + 大/小切换 + 槽位分配 |
| **Diff 预览** | 逐 hunk 确认（checkbox 勾选 / y-n-q），全部接受/拒绝/应用所选 |
| **设置** | `Config.SettingSchema()` 动态生成全量配置（分类 + toggle/select/secret/number/text 控件） |
| **权限确认** | 工具图标标题 + bash 命令绿/路径青着色 + 允许/全部允许/拒绝 |
| **单选/多选** | 多选 checkbox 列表 / 单选列表 |
| **系统通知** | Info/Success/Warn/Error 显示到聊天流 |
| **附件上传** | 图片入 vision 队列 / 音频转录（GUI + Web） |
| **斜杠命令** | /help /model /settings /theme /reset /todos /tokens /perm /slots |
| **聊天格式** | 气泡角色（含推理淡色独立气泡）+ Markdown 完整渲染（表格/代码高亮/链接） |

## 🔒 数据安全与健壮性（v0.79.4-5, 13-14）

- **模型迁移防误删**：`.migrated` 标记 + 全部分类写成功才删旧文件
- **模型文件并发原子化**：统一锁 + 临时文件原子替换 + 批量导入
- **Web 忙碌状态**：`/state` 上报 `slots[].busy`，前端切槽位不再卡死
- **TUI 非交互修复**：管道/CI 后台不再崩溃；管道 stdin 完整执行
- **GUI 健壮性**：对话框超时、空气泡、UI 线程隔离

## ⚙️ 自测

- 主项目自测 **3565 通过**，编译 0 告警
- 四端编译运行验证全部正常

---

**下载**：见本 Release 附件（Windows/macOS/Linux 单文件 exe）。

**源码**：https://gitee.com/aleckstygit/my-coder

**使用手册**：见仓库 `docs/使用手册.md`
