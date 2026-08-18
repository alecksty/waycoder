# TUI 设计规范

> 让 WayCoder 的终端界面「漂亮且统一」的唯一依据。本文 + 代码里的设计令牌 + 自动校验闸门 `--ui-lint`，三者共同把「好看」固化成可执行、可回归的规则。

## 为什么要有这份规范

历史上界面分两套写法，观感割裂：

| 写法 | 载体 | 观感 |
|------|------|------|
| 走控件库 | `ChatScreen`/`EditorScreen`/`SettingsScreen`、`TuiDialog`、`UxHelper` | 统一、可切主题 |
| 手写自绘 | ~~`ModelPicker`/`DiffPreview` 等 8 个选择器：`StringBuilder` 拼 `\x1b` + `Console.Write` + `while(true) ReadKey`~~（v0.76.0 已全部标记化，此写法废弃） | 边框/间距/配色各写各的 |
| 声明式标记 | `tuidemo/**/*.tui` + code-behind（`MarkupChatScreen` 及全部窗口型界面） | 布局写标记、交互写代码，可 `--tui-preview` 预览 |

这就是「一部分满意、一部分丑陋」的根因。规范的落点只有一条：**所有界面都必须由控件库渲染，取色只走设计令牌，禁止任何控件直接碰 `Console` 或裸 ANSI 转义。**

## 0.5 声明式标记（`.tui` 资源文件）

新界面一律优先用 `.tui` 标记声明布局 + code-behind 写交互：

- **资源位置**：`tuidemo/**/*.tui`（主界面 `chat.tui`、对话框/选择器 `dialogs/*.tui`），发布时由 csproj 复制到输出，`TuiMarkupPaths.ResolveDemoFile(name)` 定位。
- **加载模式**：`TuiMarkup.LoadFile(path)` → `res.Find<T>(id)` 取控件 → 接线数据/事件。窗口型界面沿用 `screen.ShowWindow(win)` + `UxHelper.RenderWait`。
- **预览**：`--tui-preview <file.tui>` / `--tui-watch <file.tui>` 终端预览；**WPF 图形预览** `WayCoder.Preview/`（`dotnet run --project WayCoder.Preview -- <file.tui>`）——支持缩放、屏幕尺寸模拟（80x25~240x72 快选）、文件保存自动刷新、最近文件。
- **切分原则**：静态布局（容器/控件/层级/id/基础样式）写标记；动态内容（列表项、过滤、着色、快捷键、落盘）留 code-behind。
- **环境特性**：所有元素（`TuiBase`）带 `InDesign`（设计/预览模式）与 `SimulatedScreen`（true=模拟/离屏，false=物理终端），由 `TuiMarkup.InDesign`/`TuiMarkup.SimulatedScreen` 环境量注入。预览程序（`--tui-preview`/WPF）下为 true，正常 REPL 为 false——元素可据此在「设计态」与「运行态」渲染不同内容。
- **保持 code**：本质 code 驱动的部分不硬套标记——编辑器缓冲（`TuiRichEditor` 语法高亮/gutter/增量重绘）、`TuiMenu`（紧凑自包含+动态定位）、`InlinePermission`（已由 `ShowPermissionDialog` 取代）、`TuiToastQueue`（非控件，静态队列管理器）。

## 0. 三条铁律（不可违反）

1. **禁止硬编码写屏**：`UI/**` 目录内禁止直接 `Console.Write`/`ReadKey`/`SetCursorPosition`/`CursorVisible`，禁止裸 `\x1b`/``/`\033` 字面量。唯一写屏出口是 `Terminal/RenderBuffer.cs`（及其底层的 `Terminal/AnsiTty.cs`）。
2. **取色只走令牌**：颜色一律 `TuiTheme.Current.*` 或 `TuiColors.*`，禁止写死 `36`/`47`/`30` 等 ANSI 色码魔数。
3. **新界面必用控件**：没有的控件，先基于现有 TUI 架构（`TuiBase`→`TuiControl`→`TuiView`）打造一个，再组装界面，不自造 `ReadKey` 循环。

以上三条由 `dotnet run -- --test` 里的 `[UI Lint]` 段自动校验（扫描 `UI/**` 源码断言无裸 `Console.*`/裸转义），违规即红。

---

## 1. 设计令牌

### 1.1 唯一取色途径

```csharp
// ✅ 正确：走主题令牌
int fg = TuiTheme.Current.ListSelFg;   // 选中行前景
int bg = TuiTheme.Current.ListSelBg;   // 选中行背景
var accent = AnsiTty.Fg(36);           // 需要具体色时经 AnsiTty 语义化封装

// ❌ 错误：魔数 + 裸转义
var accent = "\x1b[36m";
int fg = 36;   // 切主题后不变色，就是这里
```

- **`TuiTheme.Current`**（`UI/TuiBase/TuiTheme.cs`）是运行时唯一主题源，8 个预设：黄金甲（默认）/ 浅色 / 高对比度 / 海洋 / 森林 / 日落 / 单色 / 复古。切主题后**所有**窗口、选择器、对话框必须同步变色——做不到就说明某处写死了色值。
- **`TuiColors`**（`UI/TuiBase/TuiColors.cs`）是 ANSI 常量真源（`Black=30`…`White=37`、`BgBlack=40`…、`Bright*`、`BgBright*`）。
- **`AnsiTty`**（`Terminal/AnsiTty.cs`）是唯一允许出现 `\x1b` 的地方；`Fg/Bg/FgBg/BoldFg/SgrReset/Accent/Warn/Error/Success/DimText/BoldText` 等语义化封装覆盖常规场景。

### 1.2 前景/背景必须成对，对比度 ≥ 4.5:1

- 任何「文字 + 背景」组合都从主题里取成对令牌（如 `ControlFocusedFg`/`ControlFocusedBg`、`ListSelFg`/`ListSelBg`），禁止只设前景、背景裸奔（导致透明继承意外）。
- 对比度校验：`dotnet run -- --theme-verify`（`UI/TuiBase/ThemeVerify.cs`）会按 WCAG 相对亮度逐对算 8 主题全部关键配对的对比度，`<3:1` 报「同色/近色」、`<4.5:1` 报「偏低」。新增配色对后应补进 `ThemeVerify.CheckContrasts` 的 `pairs` 表。

### 1.3 选中态统一反白

列表/表格/标签页的「选中」一律反白高亮：`ListSelFg`（黑字）+ `ListSelBg`（青底，浅色主题自动换白字蓝底）。不要用「改前景色」这种弱信号表示选中。

---

## 2. 间距与布局

| 项 | 值 |
|----|----|
| 窗口边框内边距 | 1 格 |
| 控件间间距 | 1~2 格（`TuiVBox.Spacing`/`TuiHBox.Spacing`） |
| 窗口定位 | 一律居中（`TuiWindow` 的 X/Y 居中） |
| 窗口宽度 | 钳制 `min(目标宽, 终端宽 - 2)`（`ContentW` 钳制，窄终端不溢出） |
| 标题/状态栏 | 标题两侧各留空格 ` title `，标题栏用 `GradTitleBar` 渐变，状态栏用 `StatusBarFg`/`StatusBarBg` |

- **布局容器**：`TuiVBox`（纵向）/ `TuiHBox`（横向）/ `TuiScrollView`（滚动视口）位于 `UI/TuiBase/TuiView.cs`，是对话框组装的基础件，禁止手算坐标。
- **居中 + 钳制模板**见 `TuiDialog.NewDialog`（`UI/TuiControls/TuiDialog.cs`）：`TuiWindow{ Modal, HasMask, XScale, 居中 }` + `ContentW` 宽度钳制 + `TuiVBox`/`TuiHBox` 组装。
- **模态对话框**由 `DialogOverlay` 栈管理、`Esc` 关栈顶；浮动面板（建议列表等）用 `Floating=true` 定位，不参与流式布局，避免把内容挤出屏。

---

## 3. 边框与两态

- **边框风格全局统一**：由 `TuiHelper.GetBorderChars(WindowBorder)` 提供字符集（Single/Double/Thick/Rounded/…），不各自手绘 `+---`。
- **每个控件只定义两态**：`Normal`（默认 `Fg`/`Bg`）与 `Focused`（`FocusedFg`/`FocusedBg`）。禁用态用 `DisabledFg`（暗灰）表达。不引入第三、四态——选中态用反白（§1.3），与聚焦态区分开。
- **窗口边框两色**：聚焦 `WindowBorderFocused`（青），失焦 `WindowBorderUnfocused`（隐藏/弱化）。模态窗口 + `HasMask` 遮罩背景 `MaskBg`。

---

## 4. 组件外观

### 4.1 按钮

- 渐变底 `ApplyButtonGradient`（`TuiDialog.cs`）——比边框亮 30%，层次分明（`BtnCyanBlue`/`BtnGreenCyan`/`BtnOrangeYellow`/`BtnRedOrange`）。
- 快捷键字母下划线；按钮组（`TuiButtonGroup`）支持 `Tab` 导航 + 字母快捷键。

### 4.2 模态选择器统一模板

任何「弹窗选一项/选多项」的界面都按同一模板组装（`TuiDialog.Select/MultiSelect/InputLine/FindReplace/Confirm3/Permission` 是现成实现，可展开复用）：

```
┌ 标题栏（渐变 GradTitleBar，居中/居左）──────────────┐
│ 搜索框（TuiInput，OnTextChanged 实时过滤）          │
│ ┌ 可滚动列表（TuiList / TuiListView / TuiTableList）┐ │
│ │ ...                                              │ │
│ └──────────────────────────────────────────────────┘ │
│ [确认按钮] [取消按钮]                                │
│ ↑↓ 选择  Enter 确认  Esc 取消  （快捷键提示行）      │
└──────────────────────────────────────────────────────┘
```

- **单列**用 `TuiList`（内置滚动条 + 键盘导航 + `OnSelect`）；**任意控件行**用 `TuiListView`；**多列对齐**用 `TuiTableList`（§6）。
- **快捷键提示**是选择器美观的隐性一半：底部固定一行，列出 `↑↓`/`Enter`/`Esc` 等键位，用 `RegisterShortcut` 注册而非硬编码按键判断。

### 4.3 阻塞 → 事件桥接

所有 `static Show()` 返回结果的模态界面，统一走 `UxHelper.RenderWait`（`UI/TuiCust/UxHelper.cs:309`）：

```csharp
screen.ShowWindow(win);                 // 弹出模态窗口
RenderWait(screen, evt, timeoutMs, win); // 阻塞轮询渲染 + 按键，直到回调 Set()
```

不自己写 `while(true) Console.ReadKey()`。这是控件库与「手写循环」的分水岭。

---

## 5. 排版

- **宽度真源**：`Terminal/AnsiString.cs` 是 CJK/emoji 显示宽度的唯一真源；`TuiHelper.DisplayWidth`（CJK=2、ASCII=1）与其对齐，禁止两套宽度算法。
- **截断**：一律 `TuiHelper.TruncateByWidth(text, width)`——末尾追加全角省略号 `…` 并预留其宽度，禁止 `text.Substring(0, n)` 按字符数硬截（会截断半个中文字符）。
- **对齐填充**：`TuiHelper.PadRightByWidth`/`PadLeftByWidth` 按显示宽度补齐，禁止 `new string(' ', n)` 按字符数补（CJK 下错位）。
- **折行**：`TuiHelper.WrapText`（优先空格断行，CJK 按字符边界）。
- **标记转义**：内部标记用书名号 `«color»text«/»`，与方括号 `[ ]` 不冲突、无需双写；`TuiHelper.Esc` 负责转义用户文本中的 `« »`。

---

## 6. 新控件规范（以 TuiTableList 为例）

缺失控件时按此流程打造，`UI/TuiControls/TuiTableList.cs` 是完整范例：

1. **继承**：可交互表格继承 `TuiControl`（非容器则不用 `TuiView`），参照 `TuiList.cs`（焦点/滚动/键盘）与 `TuiListView.cs`（任意行）。
2. **数据与渲染分离**：纯数据模型（列/行）+ 格式化纯函数（`FormatCell`/`RenderHeader` 返回纯文本、无 ANSI）供自测直接断言；`OnRender` 只做「拼字符串 + 写缓冲」。
3. **颜色只走主题**：选中行 `ListSelFg`/`ListSelBg`，列头 `MdHeadingFg`，滚动条滑块 `SeekBarThumbFg`/轨道 `SeparatorFg`。
4. **键盘**：`↑↓`/`Home`/`End`/`PgUp`/`PgDn`/`Enter`/`Space`，边界钳制不越界；`EnsureSelectedVisible()` 保证选中行始终可见。
5. **AOT 安全**：纯数据 + `TuiHelper`，无反射。
6. **单测**：`Test/SelfTest.Chunk11.cs` 里补 `[TuiTableList]` 段——数据、截断、键盘边界、回调触发、列头渲染字符串。

### 6.1 自定义单元格（cell 模板 + 占位符绑定）

三个列表控件（`TuiDataList`/`TuiTreeView`/`TuiTableList`）支持用 `.tui` 文件片段做单元格模板，声明式渲染每行/每列，向「布局写 `.tui`、逻辑写 code-behind」架构靠拢。

- **模板来源**：`CellMarkup` 属性放 `.tui` 片段字符串（如 `&lt;HBox spacing='1'&gt;&lt;Icon glyph='▸'/&gt;&lt;Label text='{text}'/&gt;&lt;/HBox&gt;`），在 `.tui` 布局文件里写成 `cell="…"` 属性（`<`/`>` 需转义为 `&lt;`/`&gt;`）。
- **占位符替换**：`TuiMarkup.LoadCell(markup, vars)` 把 `{key}` 替换为数据值，文本与颜色属性通用；`AsyncLocal` 保证并发安全（同一模板并发渲染不同行）。不同控件提供的变量：
  - `TuiDataList`：`{text}`（整行）/`{index}`（行号）/+ 数据源附加 key
  - `TuiTreeView`：`{text}`（节点文本）/`{icon}`（展开符）/`{depth}`（缩进层）
  - `TuiTableList`：`{value}`（当前列值）/`{col0..N}`（各列值）/`{text}`（整行拼接）/`{index}`（行号）
- **叶子根自动包装**：`LoadCell` 根为叶子控件（Label/Button 等）时自动包装进 `TuiVBox` 返回——任意控件都能当 cell 模板，不必显式写容器。
- **宽度约束**：渲染前 `OnResize(avail, 1)` 触发布局 + `ClampCellWidths` 递归把子控件宽度钳到 ≤ 列宽。**必须钳制**——`DrawLine` 直接写屏不裁剪，cell 子控件超宽会溢出串列。选中反白先画整行背景，cell 内透明处透出。
- **容错兜底**：模板解析失败时回退 `FormatCell` 纯文本截断渲染，不崩渲染循环。
- **TreeView `items` 路径语法**：`items="文档>概览,文档>入门,文档>进阶>性能"` 用 `>` 表示层级自动建树，中间节点自动展开；cell 模板可替换默认的 `缩进+树线+展开符` 前缀之后的文本部分。

---

## 7. 校验闸门 `--ui-lint`

`dotnet run -- --test` 内的 `[UI Lint]` 段（`Test/SelfTest.Chunk11.cs`）扫描 `UI/**` 全部 `.cs`，断言非白名单文件不含：

- `Console.Write`/`WriteLine`/`ReadKey`/`KeyAvailable`/`SetCursorPosition`/`CursorLeft`/`CursorTop`/`CursorVisible`/`Clear`/`OpenStandardOutput`/`Out.Write`/`ForegroundColor`/`BackgroundColor`/`ResetColor`
- 裸转义字面量 `\x1b`/``/`\033`（大小写）

迁移未完成前，现存违规文件以白名单放行；**每迁移一个界面就从白名单移除一项，最终收紧到 0**。白名单只允许两类例外：纯逻辑待迁移的界面（阶段三目标），以及 `ThemeVerify.cs` 这类 stdout 诊断工具（非 TUI 界面）。

---

## 附录 A：迁移清单（阶段三）

8 个手写界面 → 控件化，签名不变、调用点不变：

| 界面 | 迁移载体 |
|------|----------|
| `ReasoningPicker`（最简，先做模板） | `TuiWindow` + `TuiList` + 按钮行 |
| `CommandPalette` | `TuiWindow` + `TuiInput` + `TuiListView`/`TuiTableList` |
| `FilePicker` | `TuiWindow` + `TuiInput` + `TuiTableList`（名称/大小/日期） |
| `ModelPicker` | `TuiWindow` + `TuiInput` + `TuiTableList` + 底部槽位条 |
| `SessionPicker` | `TuiWindow` + `TuiList` + 按钮行（打开/重命名/删除） |
| `TuiKeybindHelp` | `TuiWindow` + `TuiTableList`（按键/说明两列） |
| `DiffPreview` | `TuiWindow`/`TuiScreen` 内嵌只读 `TuiControl` 渲染 diff |
| `TuiChatInput` | `TuiTextArea` + `TuiList`（建议面板） |

## 附录 B：参考实现索引

- 对话框模板：`TuiDialog.NewDialog` + `ContentW` + `TuiVBox`/`TuiHBox` + `ApplyButtonGradient` + `RegisterShortcut`（`UI/TuiControls/TuiDialog.cs`）
- 阻塞桥接：`UxHelper.RenderWait`（`UI/TuiCust/UxHelper.cs:309`）
- 单列选择：`TuiList`；结构化行：`TuiListView`；多列：`TuiTableList`
- 文本输入：`TuiInput`/`TuiTextArea`（`TuiEditBase` 已封装光标/撤销/剪贴板）
- 截断/CJK：`TuiHelper.TruncateByWidth`/`DisplayWidth`/`PadRightByWidth`；宽度真源 `Terminal/AnsiString.cs`
- diff 高亮：`Edit/Syntax.cs` + `DiffRenderer`，仅换载体为 `TuiControl.OnRender`
- 主题校验：`dotnet run -- --theme-verify`（WCAG 对比度）
