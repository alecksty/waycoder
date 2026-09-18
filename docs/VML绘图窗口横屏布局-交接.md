# VML 绘图窗口：横屏布局重做（交接说明）

> 2026-09-18 会话交接。**当前状态：竖屏可用、横屏布局未做**（我之前那版实现已停用并回滚）。
> 换会话继续时**先读这一份**，不用重新调查。

## 一、要做什么（用户原话）

> 「横屏时的游戏键盘太占空间，改横屏时键盘分左右两半，游戏画面在中间，键盘可以往两边伸缩，
> 横屏去掉 tab 栏，中间空间留大」
>
> 「横屏时 **SELECT 在左键盘区右下角，START 在右边键盘区左下角**」

## 二、实测数据（这是重做的依据，别再盲改）

探针：`DrawWindowPage.RefitCanvasIfNeeded()` 开头那行 `[WC-DRAW]`（`Android.Util.Log`，tag `WC-DRAW`）。
抓法：`adb -s <设备> logcat -d | grep WC-DRAW`。

**竖屏（正常）**：

```
page=392.7x700.7  host=392.7x524.4  req=323.2x524.4     ← 画布 323×524 ✅
```

**横屏（出问题）**：

```
page=872.7x220.7  host=828.7x28.4   req=17.5x28.4       ← 画布只剩 17.5×28.4 ❌
```

**根因**：横屏后**页面高度只有 220.7**，而当前布局是「Row0 `*` 画布 / Row1 Auto 折叠条(26) /
Row2 Auto 手柄区」。手柄区横着摊开的三块（十字键 3×38、XYAB 3×38、SELECT/START 28×2+间距）
**吃掉一百多**，留给画布的只剩 **28.4** → `FitSize` 算出 17×28。

**所以「画面小」不是画布适配算错了，是横屏下画布根本没空间**。
用户提的「键盘分左右两半」正是唯一解法 —— 否则横屏无论怎么调 `FitSize` 都没救。

## 三、当前代码状态

| 文件 | 状态 |
|---|---|
| `WayCoder.Maui/Pages/DrawWindowPage.xaml` | 已加 `x:Name`：`RootGrid` / `CollapseBar` / `PadArea` / `PadLeftArea` / `PadCenterArea` / `PadRightArea`（**纯命名，无行为**，可放心用） |
| `DrawWindowPage.xaml.cs` | `ApplyOrientation()` **方法保留但调用被注释掉**（见 `OnSizeAllocated` 里那段 ⛔ 注释）。`PutInGrid(row, column)` 辅助留着 |
| `RefitCanvasIfNeeded()` | 探针 `[WC-DRAW]` 在**所有守卫之前**（关键，保持这个位置） |
| `Attach()` | 里面清 `CanvasView.WidthRequest/HeightRequest = -1` + `Dispatcher.Dispatch(RefitCanvasIfNeeded)` —— 治「Shell 复用页面留下脏尺寸」，**保留** |

**回滚只停用了 `ApplyOrientation();` 这一行**，其余都在。

## 四、我踩过的三个坑（务必避开）

### 1. 搬控件会撞「已有父级」→ **直接闪退**

真机堆栈：

```
java.lang.IllegalStateException: The specified child already has a parent.
  You must call removeView() on the child's parent first.
  at android.view.ViewGroup.addViewInner → ContentViewGroup.n_onLayout
```

`ApplyOrientation` 首次被调用时（竖屏），控件**还挂在 XAML 定义的父子关系上**；
只摘手柄那几块、**漏了根级的 `CanvasHost`/`CollapseBar`/`PadArea`**，`Add` 就炸。

**根本教训**：**运行时搬控件这条路本身就不该走** —— 见下面的正解。

### 2. `Grid.Add(view, column, row)` 的参数是**列在前**

与 `Grid.SetRow/SetColumn` 的书写顺序**相反**，极易写反。已有 `PutInGrid(view, row, column)` 可复用。

### 3. `Console.WriteLine` 在 VML 运行期间**会被吞掉**

`MauiVml.RunProgram` 跑之前把 `Console.Out/Error` 换成 `StringWriter`（为了把程序输出交回工具
返回值），而画布重算正是在运行期间触发的 ⇒ 日志进不了 logcat。**必须用 `Android.Util.Log`**
（需 `#if ANDROID`）。
另：应用未授权「所有文件访问」时 `ErrorLog` 写在**私有目录**，`adb` 读不到 —— 别拿"应用日志没内容"
当判据。

## 五、正解：**不搬控件，只改附加属性**

把 `PadArea` 这层**从 XAML 里去掉**，让三块直接成为 `RootGrid` 的子元素。这样横竖屏切换
**只改 `Grid.SetRow/SetColumn`**，父子关系自始至终不变 —— 没有"已有父级"、没有重新挂载、
没有时序问题。

```
竖屏：  CanvasHost(0,0)                     CollapseBar(1,0)
        PadLeftArea(2,0)  PadCenterArea(2,1)  PadRightArea(2,2)

横屏：  PadLeftArea(0,0)   CanvasHost(0,1)   PadRightArea(0,2)
        （CollapseBar 与 Shell TabBar 隐藏；SELECT/START 分别挪进左右区内侧角落）
```

- 根 `RootGrid` 的行列定义按方向重建（竖屏 `RowDefinitions="*,Auto,Auto"` + 单列；
  横屏单行 + `ColumnDefinitions="Auto,*,Auto"`）。
- SELECT/START 的横屏落点（**用户指定**）：左区**右下角**、右区**左下角**
  （两个键盘 Grid 都是 3×3，`Row=2,Col=2` 与 `Row=2,Col=0` 正好空着）。
- TabBar：`Shell.SetTabBarIsVisible(this, false/true)`。
- 「键盘可以往两边伸缩」= 沿用现有折叠条语义，横屏时改为收起左右手柄（收起后画布吃满整宽）。

### 实施顺序建议

1. **先改 XAML**（去掉 `PadArea` 包装层，三块直接挂 `RootGrid`，给 `RootGrid` 命名保留），
   编译并**确认竖屏表现与现在完全一致** —— 这一步不应有任何行为变化，先拿到一个稳定基线。
2. **再加切换逻辑**（只 `SetRow/SetColumn` + 行列定义），先只做「横屏隐藏 TabBar + 画布换到中间列」，
   跑通后再做 SELECT/START 归位与折叠条收起。
3. **每步都用 `[WC-DRAW]` 验证**：横屏时 `host` 高度应当显著变大（画布拿到中间整条），
   而不是现在的 28.4。

## 六、相关背景（避免重复踩）

- Shell **会复用页面实例**：`Attach` 时残留的 `WidthRequest` 会让 `ShowFrame` 里那句
  `if (CanvasView.WidthRequest <= 0) FitCanvas(scene)` 跳过兜底，而页面/容器尺寸又没变 ⇒
  `OnSizeAllocated`/`SizeChanged` 都不触发 ⇒ 画面定格在上一局的尺寸。**已修**（`Attach` 里清 -1 + 补一次重算）。
- 用户复现路径（横屏那个 bug）：**进游戏 → 横屏一下 → 转回 → 退出 → 再进**。
- 本仓 `third_party/vml/` **已与上游分家**（见 `third_party/vml/FORK.md`）：直接改，**不要加补丁**；
  `patches/` 只是历史记录。重生成共享库用 `dotnet run --project tools/GenLib -- -b`
  （`Lib/build_libs.sh` 已失效 —— 它依赖 Exe 形态的 `VMLTool`，而那个 csproj 为适配 MAUI 改成了 `Library`）。
- 本轮已完成并已验证：**消息队列清空接口** `ui_msg_clear()`（协议号 `#568`，`vmlui.c` + `waycoder_ui.h` +
  已重生成各语言绑定），五子棋已在「再来一局」处调用它。
