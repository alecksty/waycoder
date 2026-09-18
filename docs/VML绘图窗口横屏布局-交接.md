# VML 绘图窗口：横屏布局（交接说明）

> 2026-09-18 会话交接。**当前状态：已实现并在模拟器上验证通过**（竖屏原样、横屏手柄分左右两列、
> 画布居中、可收起、TabBar 隐藏）。
>
> ⚠ 上一版这里写的是「竖屏可用、横屏布局未做」，那份实现走的是**运行时搬控件**，已整条废弃 ——
> 踩过的两个坑留在下面第二节，别再走那条路。

## 一、用户要什么（原话）

> 「横屏时的游戏键盘太占空间，改横屏时键盘分左右两半，游戏画面在中间，键盘可以往两边伸缩，
> 横屏去掉 tab 栏，中间空间留大」
>
> 「横屏时 **SELECT 在左键盘区右下角，START 在右边键盘区左下角**」

## 二、做成了什么样

```
竖屏：  CanvasHost(0,0..2)                      CollapseBar(1,0..2)
        PadLeftArea(2,0)   PadCenterArea(2,1)   PadRightArea(2,2)

横屏：  PadLeftArea(0,0)   CanvasHost(0,1)      PadRightArea(0,2)
        CollapseBar(1,1)（只占画布那一列）
        SELECT → PadLeftArea(1,1)     START → PadRightArea(1,0)
        PadCenterArea 隐藏；Shell TabBar 隐藏
```

- **键盘往两边伸缩** = 折叠条那个箭头：横屏下收起的是左右两列手柄，收起后画布吃满整宽
  （实测 `host` 396 → 850）。
- **不搬控件**：XAML 里**去掉了 `PadArea` 包装层**，三块手柄直接挂在 `RootGrid` 上，
  切换只改 `Grid.SetRow/SetColumn/SetColumnSpan` + 重设行列定义 —— 父子关系自始至终不变。
  唯一跨父级的是 SELECT/START 两个按键（横屏要进左右键盘区的角落），走 `MoveBtn`：**先摘再挂**。

### 为什么不能"搬控件"（前一版的坟）

1. 全部摘下来再挂回去 ⇒ `IllegalStateException: The specified child already has a parent`
   （漏摘根级那三个，`ViewGroup.addViewInner` 抛，真机直接闪退）。
2. 补全摘除之后 ⇒ 竖屏看不见画面（`CanvasHost` 量到 0）。
   根因是**摘挂之间控件是没有父级的**，中途任何一次布局都能量到 0 并把 0 定格住。

## 三、这一轮同时修掉的三处「中间态被当成真值」

| 症状 | 真身 | 修法 |
|---|---|---|
| 横屏重开游戏，窗口开成 `544x46`、画面是花的 | `MeasuredViewport` 在 `OnSizeAllocated`（**布局期**回调）里记账，转屏时被夹在中间态、量到 `545x46` | 上报搬进 **40ms 定时器**（`PublishViewport`，布局落定之后）；有旧值才发 `WindowResize` |
| 横屏第一局开出来的窗又宽又扁 | `AvailableArea` 一律扣竖屏的 `262`，横屏只剩几十 ⇒ 算出 `898×149` | 横屏扣**宽度**（`LandscapeSideChromeDp=518`）不扣高度（`LandscapeChromeHeightDp=110`），常数照实际布局标定 |
| 画布下沿被「收起手柄」压住 | `FitSize` 用 `CanvasHost` 的尺寸，而 `GraphicsView` 自己有 `Margin="8"` | 新增 `CanvasBox()` 扣掉 `CanvasView.Margin`（从控件读，不写死） |

## 四、怎么验（模拟器，可复现）

探针：`DrawWindowPage.RefitCanvasIfNeeded()` 开头那行 `[WC-DRAW]`（走 `Android.Util.Log`，
**不能用 `Console.WriteLine`** —— VML 运行期间 `Console.Out` 被换成 `StringWriter`）。

```bash
adb -s emulator-5554 logcat -d -s WC-DRAW | tail
```

实测（模拟器 1080×2400 @2.625）：

```
横屏：page=914.3x327.2 host=396.4x301.0  req=374.9x285.0  scene=396x301  land=True
竖屏：page=411.4x726.5 host=411.4x525.3  req=395.4x300.6  scene=396x301  land=False
收起手柄（横屏）：host=850.5x301.0
```

驱动脚本在 `.scratch/landscape-check/`（**未入库**）：`final.py` 转屏取证，
`sender.py` 送命令（`input text` 从第一个空格就截断，要逐键码发；
逐键码发时**最后一个字符会丢** ⇒ 命令末尾补一个空格最省事）。

## 五、还没做的

- **真机复验**：模拟器已过，真机（`cd53cb14` 这台当时不在线）还没跑过，尤其是**横屏进游戏 →
  竖屏 → 退出 → 再进**这条用户报过的路径（模拟器上验过等价的两段，见 `LANDSCAPE` 那几行）。
- 横屏下**标题栏**（Shell 的 NavBar）仍占约 56dp —— 用户没要求去掉，留着还能看到游戏名与返回箭头。
- 游戏**场景尺寸**由程序按 `SCREEN_W/H` 自己定：横屏第一局的兜底估算已经能给出了（实测 396×301），
  但真正贴合仍要靠「开出过一次窗口」之后的实测值。
