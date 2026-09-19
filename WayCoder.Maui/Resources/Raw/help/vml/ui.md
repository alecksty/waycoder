# UI 开发

VML 程序能画界面、能收输入、能出声。这一篇是接口全表 + 几条容易踩的规矩。

> 完整清单（含每个号段的入参/返回）在仓库的 `docs/VML宿主接口.md`。
> 这里讲**怎么用**。

## 最小骨架

```c
#include <waycoder_ui.h>

int main(void) {
    int w = ui_scr_w();          /* 先问可用绘图区 */
    int h = ui_scr_h();
    ui_win_open("我的程序", w, h); /* 按它开窗 —— 单位与屏幕 1:1 */

    ui_clear(0xFF101020);
    ui_rect(20, 20, w - 40, h - 40, 0xFF00C8FF, 0, 4, 12);
    ui_text(40, 60, "你好，VML", 0xFFFFFFFF, 20, 0);
    ui_present();                 /* **这一帧画完了** */

    while (ui_win_closed() == 0) {
        int msg[4];
        if (ui_wait(msg, 0) == 0) continue;
        /* 处理输入… */
    }
    return 0;
}
```

跑：`vml run mygame.c`

**先问后开**（`ui_scr_w/h` → `ui_win_open`）很重要：这样绘图单位与屏幕 1:1，
不缩放也不出界。

## 开窗时可以先声明两件事

```c
ui_win_open_ex("五子棋", w, h, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);
```

| 第 4 个参数 | 含义 |
|---|---|
| `VML_WIN_PORTRAIT` | **只支持竖屏**（棋盘类）：屏幕锁竖屏，怎么转都不动 |
| `VML_WIN_ROTATABLE` | **支持旋转**（默认）：两种排版都写好了，视口变了宿主会通知你 |
| `VML_WIN_LANDSCAPE` | 只支持横屏（赛车 / 横版过关） |

| 第 5 个参数 | 含义 |
|---|---|
| `VML_WIN_NEED_GAMEPAD` | 显示屏幕手柄区（默认） |
| `VML_WIN_NO_GAMEPAD` | **整块手柄区连同折叠条都不显示，画布吃满整屏** |

两条都在**开窗之前**生效 ⇒ 你按 `ui_scr_w/h` 排的版一开始就是对的。

## 绘图（保留模式）

你不是"画上去"，而是**往场景里追加图元**，宿主每帧把整份场景渲染出来。

```c
ui_clear(0xFF101020);                    /* 清屏：颜色 0xAARRGGBB */
ui_rect(x, y, w, h, 颜色, 填充, 线宽, 圆角);
ui_circle(cx, cy, r, 颜色, 填充, 线宽);
ui_line(x1, y1, x2, y2, 颜色, 线宽);
ui_text(x, y, "文字", 颜色, 字号, 锚点);   /* 锚点 0左 1中 2右 */
ui_present();                            /* 提交这一帧 */
```

颜色一律 **`0xAARRGGBB`**（alpha 在最前）。`0x00xxxxxx` 是全透明 = 看不见。

还有渐变、路径（SVG 语法）、多边形、贴图、图标等，见 `waycoder_ui.h`。

⚠ **每帧画完必须 `ui_present()`** —— 它是"这帧画完了"的标记，宿主据此出图。
不调的话画面可能停在一半（先 `ui_clear` 完、棋子还没画）。

## 收输入

```c
int msg[4];
while (ui_win_closed() == 0) {
    int t = ui_wait(msg, 0);     /* 0 = 无限等；返回 0 = 超时 */
    if (t == VML_MSG_TOUCHDOWN) { x = msg[1]; y = msg[2]; ... }
    if (t == VML_MSG_KEYDOWN)   { k = msg[1]; ... }
    if (t == VML_MSG_TIMER)     { ... }
    if (t == VML_MSG_WINDOWCLOSE) break;
}
```

| 消息 | 含义 |
|---|---|
| `VML_MSG_TOUCHDOWN/MOVE/UP` | 触摸，`msg[1]=x msg[2]=y`（**画布坐标**） |
| `VML_MSG_KEYDOWN/KEYUP` | 按键（含屏幕手柄：方向键 / A B X Y / SELECT / START） |
| `VML_MSG_TIMER` | 定时器到期，`msg[1]` 是定时器 id |
| `VML_MSG_WINDOWRESIZE` | 画布尺寸变了（转屏、收起手柄），`msg[1]=宽 msg[2]=高` |
| `VML_MSG_WINDOWORIENT` | 屏幕方向变了，`msg[1]` = 0 竖屏 / 1 横屏 |
| `VML_MSG_WINDOWCLOSE` | 用户点了返回箭头 |

⚠ `ui_wait(msg, 0)` 的 `0` 是**无限等**，不是"不阻塞"。要轮询用 `ui_poll(msg)`。

⚠ 一次点击往往产生**多条**消息（按下/抬起/移动）。重开一局前先 `ui_msg_clear()`
把上一局的残留清掉，否则新一局会立刻读到旧输入。

## 定时器

```c
int tid = ui_timer_set(300, 7);   /* 每 300ms 发一条 Timer，msg[2] = 7 */
...
ui_timer_kill(tid);
```

⚠ **重复**定时器，记得在暂停/结束时 `kill`，否则它会一直往队列里投消息。

## 音效 / 震动 / 存档

```c
ui_beep(880, 120);          /* 现场合成的音：频率 Hz + 时长 ms，不用带音频文件 */
ui_vibrate(50);             /* 震动毫秒 */
ui_keep_on(1);              /* 玩的时候别熄屏 */
ui_store_set("best", 100);  /* 本地存档（键自动加前缀，不会和 App 自己的设置打架） */
int best = ui_store_get("best", 0);
```

音是**单通道**的：一次只响一个。所以要"用音高表达意思"，
连发一串琶音只有最后一个听得见。

## 屏幕方向

```c
int land = ui_orientation();   /* 0 竖屏 / 1 横屏；开窗前就能问 */
```

⚠ **别拿 `ui_scr_w() > ui_scr_h()` 去推方向** —— 那两个数是"可用绘图区"，
会随宿主的排版（手柄收起/展开）变，而方向是设备本身的属性。

## 手感三条

1. **先问后开**：`ui_scr_w/h` → `ui_win_open`，不然内容会超出画布（被裁掉）
2. **帧边界要标**：`ui_present()` 别忘
3. **声明比事后调整省事**：不要手柄就开窗时说，别等用户去点"收起手柄"
