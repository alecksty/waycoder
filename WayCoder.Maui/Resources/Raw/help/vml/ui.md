# UI 开发

VML 程序不是只能打印文字 —— 它有一套完整的**手机界面接口**：开窗、绘图、收触摸与按键、
放声音、震动、存档。这一页把**全部接口**列出来，每个都带一句用法和一个例子。

## 最小程序

```c
#include <waycoder_ui.h>

int main(void) {
    int w = ui_scr_w(), h = ui_scr_h();     /* ① 先问可用绘图区 */
    ui_win_open("演示", w, h);              /* ② 再按它开窗 */
    ui_clear(0xFF101020);
    ui_text(20, 40, "Hello", 0xFFFFFFFF, 20, VML_ANCHOR_CENTER);
    ui_present();                           /* ③ 这一帧画完了 */

    int m[4];
    while (ui_win_closed() == 0) {          /* ④ 主循环：收消息 → 处理 → 重画 */
        if (ui_wait(m, 0) == VML_MSG_TOUCHDOWN) { /* 处理 m[1], m[2] */ }
    }
    return 0;
}
```

**四条骨架**：先问尺寸 → 开窗 → 主循环（收消息 / 处理 / `ui_present`）→ 用户关窗退出。
下面按类别列出全部接口；**不同语言调用方式一样**，只是语法不同（见「22 种语言」）。

> 签名的权威来源是 `Lib/c/waycoder_ui.h`，实现在 `Lib/shared/src/vmlui.c` ——
> **22 个前端共用同一份实现**。本页的签名就是从那个头文件取的，改了接口重跑生成器即可。

## 接口分几层

按**用途**分四层。写程序时按这张图找接口，比翻列表快：

```text
┌─ 窗口 / 设备 ────────────────────────────────────────────────┐
│  开窗 ui_win_open(_ex / _pc)   问尺寸 ui_scr_w/h             │
│  设备方向 ui_orientation       方向锁 ui_orient_lock         │
│  全屏 ui_immersive             别熄屏 ui_keep_on             │
└───────────────────────────┬──────────────────────────────────┘
                            │ 开好窗之后
┌─ 输入 ────────────────────▼──────────────────────────────────┐
│  事件式（主循环）ui_wait / ui_poll + ui_msg_a/b              │
│  轮询式（手感）  ui_touch_query / ui_key_down                │
└───────────────────────────┬──────────────────────────────────┘
                            │ 拿到输入 → 决定画什么
┌─ 绘图 ────────────────────▼──────────────────────────────────┐
│  图元   ui_rect / ui_circle / ui_path / ui_text …            │
│  绘图状态 ui_clip_* / ui_mask_* / ui_alpha                   │
│  资源   ui_create_block（精灵）/ ui_gradient / ui_brush_*     │
│  像素   ui_get_pixel / ui_flood_fill / ui_screenshot          │
└───────────────────────────┬──────────────────────────────────┘
                            │ 配合着用
┌─ 系统 ────────────────────▼──────────────────────────────────┐
│  对话框 ui_dlg_*    音效震动 ui_beep / ui_vibrate            │
│  存档 ui_store_*    参数 ui_argc / ui_arg                    │
└─────────────────────────────────────────────────────────────┘
```

**一句话记法**：**窗口**决定你能画多大，**输入**告诉你用户干了什么，
**绘图**把东西画上去，**系统**是游戏之外的那些事（弹框、出声、存档）。

### 精灵为什么要用「矢量图块」

会动的东西（玩家、飞鸟、香蕉…）**尽量做成图块**（`ui_create_block` → `ui_draw_block`）：

- 形状只写一遍，贴的时候能**旋转、缩放、镜像**（缩放取负值就是镜像）；
- 每一帧从"十几次绘图调用"压成"一次贴图块"。实测 300 个精灵 × 60 帧：
  直接画 **3531ms** → 贴图块 **2945ms**（快 **17%**），精灵越多差距越大。

## 这些接口长在哪一层（出了问题去哪找）

```text
   你的程序（C / BASIC / Python / … 22 种语言）
        │  #include <waycoder_ui.h>
        ▼
   Lib/c/waycoder_ui.h          ← 签名的权威来源
   Lib/shared/src/vmlui.c       ← C 包装：一行 asm("SYSCALL #5xx")
        │  syscall 500–599
        ▼
   VmlHostRuntime（UI/Shared）  ← **逻辑全在这一份**
        │   手机与桌面**编同一份**：坐标钳位、消息队列、绘图状态机…
        │  IVmlHost（只有平台真的不一样的那十几件事）
        ▼
   手机 MauiVmlHost          │  桌面 CliVmlHost（scripts/vmlcli）
   弹原生框 / 存档 / 音频     │  脚本化输入 / 写 PNG
```

**知道这个有什么用**：

- **两端行为一致**（同一份实现）⇒ 桌面上验证过的，手机上就是那个行为；
- 万一出现「手机上对、桌面上错」，那一定在 **`IVmlHost`** 那一层（最下面那格），
  不用去翻绘图的代码；
- 桌面的 `scripts/vmlcli` 是**验收脚手架** —— 改完程序先在那儿跑（秒级），
  别一上来就打 APK（手机上编译要一分多钟）。

## 窗口

开窗、问尺寸、关窗、屏幕方向 —— [`help:vml/ui/window`](help:vml/ui/window)

| 接口 | 一句话 |
|---|---|
| [ui_orientation](help:vml/ui/window) | 设备方向：`VML_ORIENT_PORTRAIT`(0) / `VML_ORIENT_LANDSCAPE`(1)。 |
| [ui_scr_h](help:vml/ui/window) | 可用绘图区的高。 |
| [ui_scr_w](help:vml/ui/window) | 可用绘图区的宽（开窗前也能问）。 |
| [ui_win_close](help:vml/ui/window) | 关掉窗口（程序自己结束用）。 |
| [ui_win_closed](help:vml/ui/window) | 用户是不是已经关窗了（主循环的退出条件）。 |
| [ui_win_open](help:vml/ui/window) | 开一个绘图窗口。先问 `ui_scr_w/h()` 拿可用绘图区，再按它开 —— 写死尺寸在小屏上会溢出。 |
| [ui_win_open_ex](help:vml/ui/window) | 同上，另加两个开窗前就生效的声明：转屏策略、要不要手柄区。 |
| [ui_win_open_pc](help:vml/ui/window) | 开一个 “电脑屏”窗口：老 DOS/BGI 程序那种“字符网格 + 鼠标”的模型（要键盘/鼠标，不要手机手柄）。 |

```c
/* 例：ui_orientation */
if (ui_orientation() == VML_ORIENT_LANDSCAPE) { /* 横排 */ }
```

## 收消息

触摸 / 按键 / 定时器消息怎么收 —— [`help:vml/ui/messages`](help:vml/ui/messages)

| 接口 | 一句话 |
|---|---|
| [ui_msg_a](help:vml/ui/messages) | 当前消息的第一个参数（触摸的 x、按键的键码、定时器的 id…）。 |
| [ui_msg_b](help:vml/ui/messages) | 当前消息的第二个参数（触摸的 y…）。 |
| [ui_msg_clear](help:vml/ui/messages) | 清空消息队列（切场景 / 重开一局时用，免得把上一局的按键吃进来）。 |
| [ui_msg_count](help:vml/ui/messages) | 队列里还积着几条（想丢掉积压时可以看一眼）。 |
| [ui_msg_type](help:vml/ui/messages) | 当前消息的类型（省得把 `msg[0]` 记在脑子里）。 |
| [ui_poll](help:vml/ui/messages) | 不等：没有消息就返回 `VML_MSG_NONE`。连续动画用这个（配自己的节拍）； |
| [ui_poll_ex](help:vml/ui/messages) | `ui_poll` 的带「读完后留不留」版本。 |
| [ui_poll_msg](help:vml/ui/messages) | 不碰指针的取消息版本（拿不到数组指针的语言用）：没有就返回 `VML_MSG_NONE`， |
| [ui_wait](help:vml/ui/messages) | 等一条消息，参数是 `int msg[4]`。返回消息类型（见下表）；`timeout=0` 表示一直等。 |
| [ui_wait_ex](help:vml/ui/messages) | 同上，第三个参数决定读完之后留不留这条消息（`VML_MSG_KEEP` / `VML_MSG_CONSUME`）。 |
| [ui_wait_msg](help:vml/ui/messages) | 不碰指针的等消息版本：等到就返回类型、超时返回 `VML_MSG_NONE`。 |

```c
/* 例：ui_msg_a */
int x = ui_msg_a();
```

## 多点触控与设备

多指查询、按住判断、方向锁、全屏 —— [`help:vml/ui/device`](help:vml/ui/device)

| 接口 | 一句话 |
|---|---|
| [ui_audio_playing](help:vml/ui/device) | 后台 BGM 还在放吗 → 1/0（没放过、已放完、被停掉都是 0）。 |
| [ui_immersive](help:vml/ui/device) | 隐藏 / 恢复状态栏与导航栏（全屏游戏）。 |
| [ui_key_down](help:vml/ui/device) | 某个键此刻按住没有（“持续按住左移”不必自己维护状态表）。 |
| [ui_orient_lock](help:vml/ui/device) | 锁定屏幕方向：`VML_LOCK_PORTRAIT`(0) / `VML_LOCK_LANDSCAPE`(1) / `VML_LOCK_AUTO`(2)。 |
| [ui_touch](help:vml/ui/device) | 查第 slot 根手指（0..9）：写进 `out[0]=x out[1]=y out[2]=按下`，返回 1 有效（槽位越界 0）。 |
| [ui_touch_down](help:vml/ui/device) | 上一次 `ui_touch_query` 缓存里的“按下”（1/0）。 |
| [ui_touch_query](help:vml/ui/device) | 拿不到指针的语言用这个：查一次并缓存，下面三个读缓存（与 `ui_wait_msg` + `ui_msg_a/b` 同一套分工）。 |
| [ui_touch_x](help:vml/ui/device) | 上一次 `ui_touch_query` 缓存里的 x。 |
| [ui_touch_y](help:vml/ui/device) | 上一次 `ui_touch_query` 缓存里的 y。 |

```c
/* 例：ui_audio_playing */
ui_audio_play("bgm.mp3", 0);
while (ui_audio_playing()) { ui_wait(msg, 200); }
```

## 定时器与随机数

重复定时器、取时间、随机数 —— [`help:vml/ui/timer`](help:vml/ui/timer)

| 接口 | 一句话 |
|---|---|
| [ui_rand](help:vml/ui/timer) | 随机数：`ui_rand(n)` → 0..n-1（n ≤ 0 时返回 1，不会崩）。 |
| [ui_tick](help:vml/ui/timer) | 开机以来的毫秒数（自己算帧间隔、做动画用）。 |
| [ui_timer_kill](help:vml/ui/timer) | 停掉一个定时器。 |
| [ui_timer_set](help:vml/ui/timer) | 起一个重复定时器，每 N 毫秒发一条 `VML_MSG_TIMER`（`msg[1]` 是你给的 id）。 |

```c
/* 例：ui_rand */
int n = ui_rand(6);      /* 0..5 */
int side = ui_rand(2);   /* 0 或 1 */
```

## 绘图

点线面、多边形、路径、渐变、贴图 —— [`help:vml/ui/draw`](help:vml/ui/draw)

| 接口 | 一句话 |
|---|---|
| [ui_brush_linear](help:vml/ui/draw) | 造一个线性渐变刷子。几何是千分之一（0..1000），相对形状自己的包围盒：`0,0,1000,0` = 从左到右。 |
| [ui_brush_named](help:vml/ui/draw) | 按名字引用一个已经 `ui_gradient` 定义过的渐变 → 句柄。 |
| [ui_brush_radial](help:vml/ui/draw) | 造一个径向渐变刷子（中心 → 四周）。`cx cy r` 同样千分之一，`500,500,500` = 居中。 |
| [ui_brush_solid](help:vml/ui/draw) | 造一个纯色刷子。返回句柄（≥1；0 = 失败），交给 `ui_set_fill` / `ui_set_pen` 用。 |
| [ui_circle](help:vml/ui/draw) | 画圆，`fill` 非 0 填充。 |
| [ui_circle_grad](help:vml/ui/draw) | 渐变的圆（渐变先用 `ui_gradient` 起个名字）。 |
| [ui_clear](help:vml/ui/draw) | 整屏填一个色（每帧开头调）。颜色一律 `0xAARRGGBB`。 |
| [ui_draw_circle](help:vml/ui/draw) | 用当前刷子画圆。 |
| [ui_draw_ellipse](help:vml/ui/draw) | 用当前刷子画椭圆。 |
| [ui_draw_heart](help:vml/ui/draw) | 用当前刷子画心形。 |
| [ui_draw_line](help:vml/ui/draw) | 用当前画笔画直线。 |
| [ui_draw_path](help:vml/ui/draw) | 用当前刷子画 SVG 路径（`M L C Q A Z`，大小写区分绝对/相对；多子路径按奇偶规则挖洞）。 |
| [ui_draw_pie](help:vml/ui/draw) | 用当前刷子画扇形：半径 / 起始角 / 结束角（度）。 |
| [ui_draw_poly](help:vml/ui/draw) | 用当前刷子画多边形（`close=1` 自动闭合）或折线（`close=0`）。`pts` 每两个 int 一个点，`count` 是点数。 |
| [ui_draw_rect](help:vml/ui/draw) | 用当前刷子画矩形（`radius > 0` 即圆角）。 |
| [ui_draw_regular](help:vml/ui/draw) | 用当前刷子画正多边形：半径 / 边数 / 旋转角(度)。 |
| [ui_draw_ring](help:vml/ui/draw) | 用当前刷子画圆环（外半径 / 内半径，中间的洞靠奇偶规则挖）。 |
| [ui_draw_star](help:vml/ui/draw) | 用当前刷子画星形：外半径 / 内半径 / 角数 / 旋转角(度)。 |
| [ui_draw_text](help:vml/ui/draw) | 用当前文字属性（`ui_set_font`）画一行字。 |
| [ui_ellipse](help:vml/ui/draw) | 画椭圆（`rx` / `ry` 两个半径）。 |
| [ui_ellipse_grad](help:vml/ui/draw) | 渐变填充的椭圆。与 `ui_rect_grad` / `ui_circle_grad` 是一组（那批接口当时漏了椭圆）。 |
| [ui_gradient](help:vml/ui/draw) | 定义一个渐变刷子并起个名字（字符串 id）；之后 `ui_rect_grad` / `ui_circle_grad` / `ui_path` 按名字引用它。 |
| [ui_icon](help:vml/ui/draw) | 画一个内置图标（按名字取，省得自己画）。 |
| [ui_image](help:vml/ui/draw) | 在指定位置画一张图（PNG / JPG / BMP），`w` / `h` 传 0 按原尺寸。 |
| [ui_line](help:vml/ui/draw) | 画线，`lw` 是线宽。 |
| [ui_path](help:vml/ui/draw) | 按 SVG 路径语法画（`M L H V C S Q T A Z`，大小写区分绝对/相对）。 |
| [ui_pixel](help:vml/ui/draw) | 画一个点。 |
| [ui_polygon](help:vml/ui/draw) | 画多边形（自动闭合）。`pts` 是 int 数组、每两个 int 一个点；`count` 是点数。 |
| [ui_polyline](help:vml/ui/draw) | 折线（不闭合）。参数含义同 `ui_polygon`（`stroke` 是颜色、`width` 是线宽）。 |
| [ui_present](help:vml/ui/draw) | 这一帧画完了。整个循环里最关键的一句 —— 不调它屏幕不更新。 |
| [ui_rect](help:vml/ui/draw) | 画矩形。`fill` 非 0 填充、`radius` 是圆角半径。 |
| [ui_rect_grad](help:vml/ui/draw) | 带渐变的矩形（渐变先用 `ui_gradient` 定义）。 |
| [ui_set_fill](help:vml/ui/draw) | 设置填充刷子。传刷子句柄或颜色都行；传 0 = 不填充（空心）。 |
| [ui_set_pen](help:vml/ui/draw) | 设置画笔（描边）= 刷子 + 线宽 + 线帽 + 虚线 + 箭头。传 0 = 不描边。 |
| [ui_set_text_brush](help:vml/ui/draw) | 设置文字刷子（配合 `ui_set_font` + `ui_draw_text`）。传 0 = 回到 `ui_set_font` 给的颜色。 |

```c
/* 例：ui_brush_linear */
int b = ui_brush_linear(0xFFFF3020, 0xFF2050FF, 0, 0, 1000, 0);
ui_set_fill(b);
```

## 绘图状态

裁剪、蒙版（含布尔运算）、透明度、资源计数 —— [`help:vml/ui/gfx`](help:vml/ui/gfx)

| 接口 | 一句话 |
|---|---|
| [ui_alpha](help:vml/ui/gfx) | 全局透明度 0..255，对之后画的图元生效。 |
| [ui_brush_reset](help:vml/ui/gfx) | 释放本窗口累积的全部画刷 / 渐变定义（只能整体重置 —— 句柄就是表的下标，删单个会让后面全部错位）。 |
| [ui_clip_pop](help:vml/ui/gfx) | 弹出一级裁剪（栈空了再弹是空操作）。 |
| [ui_clip_push](help:vml/ui/gfx) | 压入一级矩形裁剪：之后画的东西只在这个矩形里可见；可以嵌套（与上一级求交）。 |
| [ui_clip_reset](help:vml/ui/gfx) | 清空整个裁剪栈（不是弹一级）—— 给“出错恢复”用：一进主循环调一次，回到干净的整屏状态。 |
| [ui_layer_begin](help:vml/ui/gfx) | 开始图层收集：这期间画的图元先落在临时画布上，等 `ui_layer_end` 整层合成。 |
| [ui_layer_end](help:vml/ui/gfx) | 结束收集并整层按 `alpha`（0..255）合成上去。 |
| [ui_mask_begin](help:vml/ui/gfx) | 开始收集蒙版形状：这期间画的形状不上屏，只当蒙版用。配 `ui_mask_end` / `ui_mask_end2` 收尾。 |
| [ui_mask_clear](help:vml/ui/gfx) | 取消蒙版：之后的图元全部可见。 |
| [ui_mask_end](help:vml/ui/gfx) | 收下蒙版并取代当前蒙版。`inside`：1 = 只在形状里画 / 0 = 只在形状外画。 |
| [ui_mask_end2](help:vml/ui/gfx) | 收下蒙版并与当前蒙版按 `op` 做布尔运算 —— 老式“圆环”得画两遍，现在一次就够。 |
| [ui_mask_path](help:vml/ui/gfx) | 把第 seg 段第 idx 个形状导出成 SVG 路径写进 buf —— 程序能拿它描洞口的边、做外发光。 |
| [ui_mask_seg_count](help:vml/ui/gfx) | 当前蒙版有几段（没有蒙版返回 0）。 |
| [ui_mask_seg_op](help:vml/ui/gfx) | 第 seg 段用的运算符（越界 -1）。 |
| [ui_mask_shape_count](help:vml/ui/gfx) | 第 seg 段里有几个形状（越界 0）。 |
| [ui_mask_test](help:vml/ui/gfx) | 蒙版当碰撞体：这一点在不在当前蒙版里（1/0）。 |
| [ui_res_count](help:vml/ui/gfx) | 查当前占用：`0` 图元 / `1` 图像 / `2` 矢量图块 / `3` 画刷渐变；未知返回 -1。 |

```c
/* 例：ui_alpha */
ui_alpha(120);
ui_rect(10, 10, 100, 60, 0xFF000000, 1, 0, 0);   /* 半透明黑 */
ui_alpha(255);
```

## 像素读回

读像素、灌色、抓图贴图、截屏 —— [`help:vml/ui/pixel`](help:vml/ui/pixel)

| 接口 | 一句话 |
|---|---|
| [ui_flood_fill](help:vml/ui/pixel) | 从 (x,y) 灌色，碰到 border 色就停（四连通）。返回落笔的矩形条数，0 = 没填 |
| [ui_get_image](help:vml/ui/pixel) | 存一块画面 → 句柄（≥1），失败 0。老程序的 `malloc(imagesize(...))` 照写不误，只是那块内存我们不用。 |
| [ui_get_pixel](help:vml/ui/pixel) | 读一个像素的颜色（`0xRRGGBB`；越界 -1）。 |
| [ui_put_image](help:vml/ui/pixel) | 把句柄那块贴到 (x,y)。`mode`：0 = COPY 直接贴 / 1 = XOR 异或（异或要先读目的像素，慢一些）。 |
| [ui_screenshot](help:vml/ui/pixel) | 把当前窗口存成 PNG（路径相对工作区；不给路径就自动取名落在 `shot/` 下）。返回 1 成功。 |

```c
/* 例：ui_flood_fill */
ui_flood_fill(50, 50, 0xFF00FF00, 0xFFFF0000);   /* 在红框里灌绿 */
```

## 矢量图块

录一次、带旋转缩放地反复贴（精灵用这个） —— [`help:vml/ui/block`](help:vml/ui/block)

| 接口 | 一句话 |
|---|---|
| [ui_create_block](help:vml/ui/block) | 开始录制一个图块。录的是绘图指令、不是像素 —— 贴的时候能旋转缩放，放大也不糊。 |
| [ui_draw_block](help:vml/ui/block) | 贴一个图块。(x,y) 是块的中心、绕中心旋转；缩放是千分比（1000 = 原尺寸）、角度是度。 |
| [ui_draw_block_at](help:vml/ui/block) | 同上，但 (x,y) 是块的左上角、绕左上角转。 |
| [ui_end_block](help:vml/ui/block) | 结束录制，返回图块句柄（≥1；0 = 没录成）。 |

```c
/* 例：ui_create_block */
bid = ui_create_block(30, 20, 0);   /* 30×20 的块 */
```

## 文字

写字、字体、锚点 —— [`help:vml/ui/text`](help:vml/ui/text)

| 接口 | 一句话 |
|---|---|
| [ui_set_font](help:vml/ui/text) | 设一次字体，后面所有 `ui_text_cur` 都用它（省得每次重复传四个参数）。 |
| [ui_set_valign](help:vml/ui/text) | 设置默认垂直对齐（之后所有文字生效）。 |
| [ui_text](help:vml/ui/text) | 在 (x,y) 写一行字。`size` 是字号；`anchor` 决定 (x,y) 指文字的哪一边（`VML_ANCHOR_LEFT` / `CENTER` / `RIGHT`）。 |
| [ui_text_cur](help:vml/ui/text) | 用 `ui_set_font` 设好的字体写字。 |
| [ui_text_styled](help:vml/ui/text) | 同上，另加样式（粗体 / 斜体 / 下划线）。 |
| [ui_text_v](help:vml/ui/text) | 带垂直对齐的文字（`VML_VALIGN_*`）—— `ui_text` 的 y 是基线，这个可以按顶/中/底对齐。 |

```c
/* 例：ui_set_font */
ui_set_font(18, VML_FONT_BOLD, 0xFFFFFFFF, VML_ANCHOR_CENTER);
ui_text_cur(180, 40, "按方向键退出");
```

## 方块贴图

把一张图切成小格反复贴 —— [`help:vml/ui/piece`](help:vml/ui/piece)

| 接口 | 一句话 |
|---|---|
| [ui_piece_cell](help:vml/ui/piece) | 取某个棋子的某一格：`pid` 棋子号、`rot` 旋转、`which` 第几格。 |
| [ui_piece_init](help:vml/ui/piece) | 初始化棋子贴图表（无参版本；具体形态见 `Lib/shared/src/vmlui.c`）。 |

```c
/* 例：ui_piece_cell */
ui_piece_cell(0, 0, 3);   /* 0 号棋子、不旋转、第 3 格 */
```

## 整数网格

棋盘 / 地图这种二维状态 —— [`help:vml/ui/grid`](help:vml/ui/grid)

| 接口 | 一句话 |
|---|---|
| [ui_gclear](help:vml/ui/grid) | 清空网格。棋盘 / 地图这种二维状态用它 —— 比语言自带的数组可靠（有的前端数组写入读不回来，见「22 种语言」里各语言的坑）。 |
| [ui_gget](help:vml/ui/grid) | 读一格，没写过返回 0。 |
| [ui_gset](help:vml/ui/grid) | 写一格，`i` 是一维下标（`row * 宽 + col`）。 |

```c
/* 例：ui_gclear */
ui_gclear();
```

## 对话框

提示、单选、多选、输入 —— [`help:vml/ui/dialog`](help:vml/ui/dialog)

| 接口 | 一句话 |
|---|---|
| [ui_dlg_input](help:vml/ui/dialog) | 要一行文字输入。结果写进你给的缓冲区。 |
| [ui_dlg_msg](help:vml/ui/dialog) | 弹一个提示框（`style` 传 0 即可）。会阻塞到用户点掉 —— 游戏结束时报个结果正好。 |
| [ui_dlg_multi](help:vml/ui/dialog) | 多选对话框：`opts` 选项串、`n` 选项个数。返回选中的个数。 |
| [ui_dlg_select](help:vml/ui/dialog) | 单选对话框：`opts` 是选项串、`n` 是选项个数、`def` 是默认选中项。返回选中下标（-1 = 取消）。 |

```c
/* 例：ui_dlg_input */
char buf[64];
ui_dlg_input("改名", "新名字：", buf, 64);
```

## 音效与触感

合成音、音频文件、震动、屏幕常亮 —— [`help:vml/ui/feel`](help:vml/ui/feel)

| 接口 | 一句话 |
|---|---|
| [ui_beep](help:vml/ui/feel) | 现场合成一个音（不用带音频文件）：`freq` 赫兹、`ms` 毫秒。 |
| [ui_audio_play](help:vml/ui/feel) | 播放一个音频**文件**（mp3/wav…），`loop` 非 0 = 循环（BGM 用）。 |
| [ui_audio_stop](help:vml/ui/feel) | 停掉正在播的音频。 |
| [ui_audio_volume](help:vml/ui/feel) | 整体音量 0–100（对**之后**播放的音生效）。 |
| [ui_audio_playing](help:vml/ui/device) | 后台 BGM 还在放吗 → 1/0。 |
| [ui_keep_on](help:vml/ui/feel) | 屏幕常亮开关（玩游戏的都该开）。 |
| [ui_vibrate](help:vml/ui/feel) | 震动：`ms` 毫秒，`strength` 强度。 |
| [ui_vibrate_pattern](help:vml/ui/feel) | 按节奏震动（int 数组：奇数下标静、偶数下标动）。 |

```c
/* 例：ui_beep */
ui_beep(880, 80);      /* 消一行 */
ui_beep(1568, 160);    /* 消四行，音更高 */
```

## 本地存档

存最高分这类小数据 —— [`help:vml/ui/store`](help:vml/ui/store)

| 接口 | 一句话 |
|---|---|
| [ui_store_get](help:vml/ui/store) | 读一个值：写进你给的缓冲区、返回长度（没有这条键返回 -1）。 |
| [ui_store_set](help:vml/ui/store) | 存一个值。值也是字符串 —— 存数字要先自己转成字符串（这里没有 sprintf 可用）。 |
| [ui_store_del](help:vml/ui/store) | 删掉一条存档（例如"清空最高分"）。 |

```c
/* 例：ui_store_get */
char buf[16];
if (ui_store_get("high", buf, 16) > 0) best = atoi(buf);
```

## 全能接口

两个字符串进、一个 JSON 出 —— [`help:vml/ui/json`](help:vml/ui/json)

| 接口 | 一句话 |
|---|---|
| [ui_call_json](help:vml/ui/json) | 两个字符串进、一个 JSON 字符串出 —— 查设备信息、调宿主的杂项能力都走它， |
| [ui_call_json_at](help:vml/ui/json) | 取上一次结果的第 i 个字节。 |
| [ui_call_json_len](help:vml/ui/json) | 上一次 `ui_call_json_s` 的结果有多长。 |
| [ui_call_json_print](help:vml/ui/json) | 把上一次 `ui_call_json_s` 的结果整份打到 stdout（末尾补换行）—— 调试最省事， |
| [ui_call_json_s](help:vml/ui/json) | 同上，但不用给缓冲区（适合拿不到指针的语言），配 `_len` / `_at` 读结果。 |

```c
/* 例：ui_call_json */
char buf[512];
ui_call_json("sysinfo", "", buf, 512);
puts(buf);   /* {"ok":true,"result":{…}} */
```

## 调用宿主

按数字 id 调宿主函数（带类型、零编解码） —— [`help:vml/ui/call`](help:vml/ui/call)

| 接口 | 一句话 |
|---|---|
| [callwithdouble4](help:vml/ui/call) | 同上，参数是 3 个 `double`（走 `D1..D3`），返回覆盖 `D0`。 |
| [callwithfloat8](help:vml/ui/call) | 同上，参数是 7 个 `float`（走 `F1..F7`），返回覆盖 `F0`。 |
| [callwithint8](help:vml/ui/call) | 按数字 id 调宿主的函数：`v[0]` 是调用号（见 `VML_CALL_*` 宏）、`v[1..7]` 是参数， |
| [callwithlong4](help:vml/ui/call) | 同上，参数是 3 个 `long`（走 `L1..L3`），返回覆盖 `L0`。 |

```c
/* 例：callwithdouble4 */
double v[4];
v[0] = VML_CALL_ECHO_DOUBLE;
v[1] = 1.0; v[2] = 2.0;
double r = callwithdouble4(v);
```

## 命令行参数

程序自己的 -l 这类开关 —— [`help:vml/ui/args`](help:vml/ui/args)

| 接口 | 一句话 |
|---|---|
| [ui_arg](help:vml/ui/args) | 把第 i 个参数拷进 buf，返回长度（越界 -1）。`argv[0]` 是程序名 ⇒ 用户给的第一个是 `ui_arg(1,…)`。 |
| [ui_argc](help:vml/ui/args) | 参数个数（含程序名，恒 ≥ 1）。 |

```c
/* 例：ui_arg */
char buf[64];
if (ui_arg(1, buf, sizeof(buf)) > 0) { /* 用了 -l 这类开关 */ }
```
