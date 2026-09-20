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

## 文字

写字、字体、锚点 —— [`help:vml/ui/text`](help:vml/ui/text)

| 接口 | 一句话 |
|---|---|
| [ui_set_font](help:vml/ui/text) | 设一次字体，后面所有 `ui_text_cur` 都用它（省得每次重复传四个参数）。 |
| [ui_text](help:vml/ui/text) | 在 (x,y) 写一行字。`size` 是字号；`anchor` 决定 (x,y) 指文字的哪一边（`VML_ANCHOR_LEFT` / `CENTER` / `RIGHT`）。 |
| [ui_text_cur](help:vml/ui/text) | 用 `ui_set_font` 设好的字体写字。 |
| [ui_text_styled](help:vml/ui/text) | 同上，另加样式（粗体 / 斜体 / 下划线）。 |

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

合成音、震动、屏幕常亮 —— [`help:vml/ui/feel`](help:vml/ui/feel)

| 接口 | 一句话 |
|---|---|
| [ui_beep](help:vml/ui/feel) | 现场合成一个音（不用带音频文件）：`freq` 赫兹、`ms` 毫秒。 |
| [ui_keep_on](help:vml/ui/feel) | 屏幕常亮开关（玩游戏的都该开）。 |
| [ui_vibrate](help:vml/ui/feel) | 震动：`ms` 毫秒，`strength` 强度。 |

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
