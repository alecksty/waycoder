# 手机端 VML 游戏开发指南

> **给 AI 看的**：要写一个能在手机上跑的游戏时，**照本文的骨架抄**，别自己摸接口。
> 完整可运行的范例在 `Examples/c/`：`mario.c`（横版跳跃）、`tetris.c`（方块）、
> `gomoku.c`（回合制）、`snake.cpp` / `whack.bas` / `sokoban.f90`（其它语言同接口）。
>
> **先写能跑的骨架（开窗 + 定时器 + 消息循环 + 画个方块），跑通了再加玩法。**
> 一次性写完一大坨再调，是这个场景最慢的路。

---

## 0. 最短路径

```
1. 抄骨架  →  开窗、开定时器、进消息循环、每拍重绘
2. 跑起来  →  vml run xxx.c   （手机上「命令行」页，或文件页点「VML 运行」）
3. 加玩法  →  物理 / 碰撞 / 绘制，一次一样
```

⚠ **C 程序在手机上编译要一分多钟**（前端 + 汇编 + 链接 3.7 万条指令），
**别当成卡死**。等它出结果。

---

## 1. 骨架（复制这段就是能跑的游戏）

```c
#include <waycoder_ui.h>
#include <stdlib.h>

int W, H, TICK;          /* 画布宽高、格子边长 */
int x, y, vx;            /* 游戏状态放**文件级全局**（见 §3 约束 2）*/

void draw_all(void) {
    ui_clear(0xFF202030);              /* 背景 */
    ui_rect(x, y, TICK, TICK, 0xFF4ADE80, 1, 0, 4);
    ui_present();                      /* **一帧画完必须调它**，否则屏幕上什么都没有 */
}

int main(void) {
    int msg[4];
    int t;

    W = ui_scr_w();  if (W <= 0) W = 360;   /* **先问可用绘图区，再开窗** */
    H = ui_scr_h();  if (H <= 0) H = 620;
    ui_win_open("我的游戏", W, H);
    ui_keep_on(1);                          /* 玩的时候别熄屏 */

    TICK = 24;  x = 100;  y = 100;  vx = 2;
    draw_all();

    ui_timer_set(40, 0);                    /* ⚠ **漏了这行游戏一动不动**，见 §4 */

    while (ui_win_closed() == 0) {
        t = ui_wait(msg, 0);
        if (t == 0) continue;
        if (t == VML_MSG_WINDOWCLOSE) break;
        if (t == VML_MSG_TIMER) {
            x = x + vx;
            if (x < 0 || x > W - TICK) vx = -vx;
            draw_all();                     /* 每拍重绘 */
            continue;
        }
        if (t == VML_MSG_KEYDOWN) {
            if (msg[1] == VML_KEY_ESCAPE) break;
            /* 按键处理 */
        }
    }

    ui_keep_on(0);
    ui_win_close();
    return 0;
}
```

---

## 2. 接口清单（`#include <waycoder_ui.h>`）

### 窗口 / 循环
| 函数 | 说明 |
|---|---|
| `ui_scr_w()` / `ui_scr_h()` | 可用绘图区宽高 —— **必须在 `ui_win_open` 之前问**，窗口宽高就是画布坐标空间 |
| `ui_win_open(title, w, h)` | 开窗 |
| `ui_win_close()` / `ui_win_closed()` | 关窗 / 是否已请求关闭 |
| `ui_wait(msg, timeout_ms)` | 取一条消息，返回类型；`msg[1]` 起是参数 |
| `ui_timer_set(interval_ms, tag)` | **重复**定时器，到期投一条 `VML_MSG_TIMER` |
| `ui_timer_kill(id)` | 停掉定时器 |
| `ui_present()` | **提交这一帧** |

### 绘制（都在画布坐标系里）
```c
void ui_clear(int color);
void ui_rect(int x, int y, int w, int h, int color, int fill, int lw, int radius);
void ui_circle(int cx, int cy, int r, int color, int fill, int lw);
void ui_line(int x1, int y1, int x2, int y2, int color, int lw);
void ui_ellipse(int cx, int cy, int rx, int ry, int color, int fill, int lw);
void ui_polygon(int* pts, int count, int fill, int stroke, int width, char* grad);
void ui_path(char* d, int stroke, int width, int fill, char* grad, int cap, int dash);  /* SVG 路径 */
void ui_text(int x, int y, char* s, int color, int size, int anchor);
void ui_set_font(int size, int style, int color, int anchor);   /* 配 ui_text_cur 用 */
void ui_text_cur(int x, int y, char* s);
```
- 颜色是 **`0xAARRGGBB`**（alpha 在前）。`0xFFRRGGBB` = 不透明。
- 锚点：`VML_ANCHOR_LEFT` / `VML_ANCHOR_CENTER`。
- 字体样式：`VML_FONT_BOLD`（无 = 0）。

### 手感 / 存档
```c
void ui_beep(int freq, int ms);          /* 音效 —— **单通道**：连发只听见最后一个（老语义，不变） */
/* 复音（v0.96.485）：**多个音同时响**（最多 VML_TONE_MAX_VOICES = 32 个声部）。
 * 音符号是真 MIDI 语义：A4 = 69 = 440Hz、中央 C = 60。
 * 按和弦就是连续 tone_on 几个通道、中间不 tone_off —— 这时 `ui_beep` 仍可照常打，
 * 两者互不干扰（蜂鸣走自己的专用声道，不吃掉和弦）。 */
int  ui_tone_on(int ch, int note, int vel);   /* 起一个音：通道 0–15、音符号 0–127、力度 0–127 */
int  ui_tone_off(int ch, int note);           /* 关一个音（note 传 -1 = 该通道全部） */
int  ui_tone_all_off(void);                   /* 全关（走淡出，不是硬切） */
int  ui_tone_voices(void);                    /* 此刻在响的声部数 —— 查"和弦叠起来没有"用它 */
int  ui_tone_panic(void);                     /* **立刻**全停（不进淡出）—— 强制停止 / 一键静音用；
                                               *   ⚠ 它只停声部，自己的记账要一并清，否则"还按着的手指"
                                               *   抬手时会去减一本已清空的账，那个键以后就起不来音 */
/* 音效音序器（**不是 syscall**，是共享库里的一段状态机）—— 做"轰/叮/警报"这类
 * 有音色的音效用它，别在事件点上裸调 ui_tone_on/off。怎么配见 §4.5。
 *     ui_sfx_add(ch, note, delay, dur, vel, wave);   塞一个音（delay 拍后响、响 dur 拍）
 *     ui_sfx_tick();                                 一拍推进（**放主循环**，见 §4.5）
 *     ui_sfx_panic();                                立刻静音 + 清表（退出/重开时）
 *     ui_sfx_active();                               还有几个槽占着（放完一轮应当回到 0）*/
void ui_sfx_add(int ch, int note, int delay, int dur, int vel, int wave);
void ui_sfx_tick(void);
void ui_sfx_reset(void);
void ui_sfx_panic(void);
int  ui_sfx_active(void);
/* 传感器（SENSOR #554，一个号 + 操作码）—— **做倾斜控制用第一个**：
 *     VML_SENS_ACCEL(0)     加速度（含重力），毫克（1000 = 1g）—— 静止时就是"往哪边歪"，**不漂**
 *     VML_SENS_GYRO(1)      角速度，千分之一度/秒 —— 要"角度"得自己积分，而积分**会漂**
 *     VML_SENS_ROTATION(2)  融合姿态（俯仰/翻滚/方位），千分之一度 —— 最准，但**有的设备没有**
 * 坐标：x = 屏幕向右、y = 屏幕向上、z = 屏幕朝外。 */
int  ui_sensor(int kind, int* out);       /* out[0..2]=x,y,z；1=有效 0=**没有该传感器** */
int  ui_sensor_available(int kind);       /* 开窗之前就能问 */
int  ui_sensor_rate(int kind, int ms);    /* 采样间隔，0=平台默认 */
int  ui_sensor_calibrate(int kind);       /* 把当前姿态当零点（玩家躺着玩时用）*/
/* 电量与省电（POWER #540）—— 玩到一半没电是最常见的"事故"，而程序自己就能防。
 * 不需要任何权限。单位：电量 0-100。 */
int  ui_battery(int* out);          /* out[0]=电量 out[1]=充电中；1=有效 0=**没有电池** */
int  ui_power_saver(void);          /* 系统省电模式（用户明确要求省电，与"电量低"不是一回事）*/
void ui_vibrate(int ms, int strength);   /* 震动 */
void ui_keep_on(int on);                 /* 别熄屏 */
void ui_store_set(char* key, char* value);
int  ui_store_get(char* key, char* buf, int cap);   /* 返回长度；没这条键返回 -1 */
int  ui_rand(int n);                     /* 0..n-1 */
```

### 系统
```c
int ui_screenshot(char* path);   /* 把画布存成 PNG。返回写入字节数，-1 失败 */
```
- 存的是**你画的那张画面**（不含屏幕手柄与标题栏）—— 拿来当"战绩图 / 通关留念"正好。
- **传空串就行**：自动落在 `shot/` 下，名字带窗口标题与时间戳
  （`shot/五子棋_20260924_102801.png`）—— 连存几张不会互相覆盖，也不用自己拼名字。
- 想自己命名就给相对路径：没扩展名自动补 `.png`，子目录会自动建。
  含 `..` 或盘符的直接失败（**故意不宽容**：那是防写到你沙箱外面的唯一一道闸）。
- ⚠ **会阻塞几十毫秒**（光栅化 + 编码 PNG），**别放进每帧的循环里** ——
  放在"这一局结束/按了分享键"这种一拍一次的地方。

### 对话框（**阻塞**，返回用户选择）
```c
int ui_dlg_msg(title, body, style);                    /* style: VML_DLG_INFO/WARN/ERROR/QUESTION */
int ui_dlg_select(title, body, opts, n, def);          /* opts 用 '\n' 分隔 */
int ui_dlg_input(title, prompt, buf, cap);
```

---

## 3. C 前端的**三条硬约束**（踩过才写的）

1. ⚠ **`asm()` 的 `${}` 占位符只认局部变量。**
   所以**一律走 `waycoder_ui.h` 的包装函数**；**绝不要**
   `asm("SYSCALL #525, ${gx}, ...")` 去传全局变量 —— 会退化成 `R12+0`，静默画错。
2. **文件级全局变量是好的。** 游戏状态（棋盘、实体、分数）直接放全局，别为了"安全"到处传参。
   全局 `int` 数组的读写都正常。
3. ⚠ **`#define` 不支持反斜杠续行**（会让词法器在下一行报"未知字符"）。
   宏一行写完，多行说明另起一段块注释。

另两条非硬约束但省事：
- **整数物理**：位置/速度都用「像素·每拍」的整数。像素坐标最终也要取整，用浮点没收益还多担一份风险。
- **自己写 `num_str()` 转数字**，不依赖 `sprintf`/`printf`（本环境 printf 家族的格式化路径在桌面脚手架上有已知问题）。

---

## 4. 七个「写完才发现」的坑

| 坑 | 现象 | 原因 |
|---|---|---|
| **漏 `ui_clear()`** | **画面糊成一团**（每帧叠上一帧），十几秒后**闪退** | 所有绘制（含 `ui_gradient`）都往同一个图元表**追加**，只有 `ui_clear` 会清空。渐变铺满全屏**看着**像清了底 —— 那是「盖上去」不是「重置」 |
| **漏 `ui_timer_set`** | 窗口出来了，但**角色一动不动** | 所有逻辑都在 `VML_MSG_TIMER` 分支里，没定时器就只有按键消息 |
| **漏 `ui_present()`** | 屏幕全空 / 停在上一帧 | 绘制是画进后台缓冲的，要 `present` 才提交 |
| **按屏幕尺寸排版、却开了别的宽高** | 内容**右边被切掉** | 窗口宽高 = 画布坐标空间。必须先 `ui_scr_w/h()` 再 `ui_win_open` |
| **把「长按连发」当前提** | 松手后角色**一直往前跑** | KeyUp 会丢（手指滑出按键范围 / 系统吃掉 CANCEL）。**必须自带刹车**：换键即接管 + 连续按住超过 N 拍自动释放 |
| **`ui_keep_on(1)` 后忘了关** | 退出游戏后屏幕一直不熄 | 退出路径上补 `ui_keep_on(0)` |
| **音效连发一串** | 只听得见最后一个 | `ui_beep` 是**单通道**的，后一个音会把前一个停掉（**这是它的老语义，没变**）。⚠ 要**同时**响几个音、或者想让"轰"和"叮"听起来不是一回事，得用 `ui_tone_on/off`（复音）——做法见 **§4.5**；`ui_beep` 做不到，但它也不吃和弦 |
| **网格游戏一拍直接走 N 像素** | 角色**卡在半格上**：不转向、不吃道具，看着像"按键没反应" | 位置一越过格线 `col_of()` 就报**下一格**，"前面是墙"随即在**离格心半格**处把人钉死。见 §6.5 |
| **全局数组初始化里写负数** | 那个负数**变成 0**，正数全对 | C 前端曾有这个缺陷（`-3` 是 `UnaryOp` 而非 `NumberLiteral`，摊平初始化器时掉进兜底 `Add(0)`）。**已修**（`VMLPrepares/CCompiler/ConstFold.cs`），`vml-out-probe` 的 `nat.c` 钉着它 |

---

## 4.5 音效：**六个事件该有六种音色**，不是同一声哔的不同音高

`ui_beep` 能出声，但它只有一种方波 —— 命中、撞楼、警报听起来是**同一个东西**，
只是音高不同。复音（`ui_tone_on/off`）之后，音效才真的能"表达事情"。
三个 gorilla 与钢琴用的都是这一套。

### 先定一张「哪个事件配什么音色」的表

| 事件 | 怎么配 | 为什么 |
|---|---|---|
| 发射 | 两音**快速下行**（三角波） | 有方向感，像"嗖" |
| 命中得分 | **大三和弦上行** do–mi–sol（方波） | 这是重复最多的正反馈，就该是最好听的那个 |
| 打爆一个小东西 | 高音一「叮」+ 低音垫底 | **别用轰鸣** —— 与撞楼的份量不一样，听着就该不一样 |
| 撞楼 / 落地 | **几个不谐和低音叠一起**（如 36/48/54，锯齿） | 谐波丰富 + 音程"脏"（三全音）= 轰 |
| 警报（被惹毛） | **下行三音**（锯齿） | 一个音是"哔"，下行三音才是"我盯上你了" |
| 胜 / 负 | 上行大三和弦 / 小二度下行 | ⚠ **必须不看屏幕也分得出** |

⚠ **胜负一定要有声音**。这类"这一局唯一必须让玩家知道的事"，一行小字等于没交代 ——
而玩家输赢那一刻，视线通常都钉在自己那只猴子的手上。

### 音序器：**别在事件点上直接 `ui_tone_on/off`**

三个理由，任何一个都足以让"直接调"出问题：

1. **`ui_tone_on` 没有时长参数** —— 响多久全看自己什么时候 `ui_tone_off`。
   事件点上 on、忘了 off，声部就只涨不落（上限 32，满了以后新音**全哑，而且不报错**）。
2. 好听的音效常常是**几个音先后**（"叮—咚"、上行三音），而事件点只有一拍 ⇒
   需要"过几拍再响下一个"。
3. **同一个通道上后一个音会掐掉前一个**（这正是 `ui_beep` 的老语义）⇒
   "同时响"必须落在**不同通道**上，得有一处统一分配。

⚠ **这三条不用你自己绕 —— 机制在共享库里**（`Lib/shared/src/vmlui.c`，所有语言共用一份）：

```c
void ui_sfx_add(int ch, int note, int delay, int dur, int vel, int wave);
                 /* ↑delay 拍之后开始响、响 ↑dur 拍 */
void ui_sfx_tick(void);        /* 一拍推进：delay 到了 note_on、dur 响完 note_off */
void ui_sfx_reset(void);       /* 清表（**不发声**） */
void ui_sfx_panic(void);       /* 立刻静音：先把在响的全关掉、再清表 */
int  ui_sfx_active(void);      /* 还有几个槽占着 —— **放完一轮应当回到 0**，拿它做自检 */
```

**机制在库里，音色留在你的游戏里** —— 哪个事件配什么音是设计，不是机制
（同一个"爆炸"，机甲游戏和种田游戏要的不一样）。所以一个音效就是三五行：

```c
/* 命中得分：大三和弦上行（do–mi–sol）—— 重复最多的正反馈，就该最好听的那个 */
ui_sfx_add(3, 72, 0, 4, 95, VML_WAVE_SQUARE);
ui_sfx_add(4, 76, 1, 4, 85, VML_WAVE_SQUARE);
ui_sfx_add(5, 79, 2, 6, 85, VML_WAVE_SQUARE);
```

⚠ `ui_sfx_add` **接管同通道旧槽时会先把那个音关掉** —— 这是它替你处理的一件容易忘的事
（不关的话旧槽连同"它还在响"一起被丢掉，那个声部再也没人去关）。

现成的例子按"读起来最快"排序：`Examples/c/tetris.c`（消行用**和弦的丰满度**表达赚了多少，
不是把音高往上堆）、`Examples/c/pacman.c` / `mario.c` / `starfall.c`（几个 `sfx_*` 一眼看完）、
`Examples/cpp/gorilla.cpp`（七种音色最全）、`Examples/basic/gorilla.bas`（BASIC 怎么写）。

### 通道按「谁与谁**可能**同拍」分区

```
0–2 撞楼轰鸣   3–5 命中和弦   6–7 发射   8–9 空中爆炸   10–12 警报   13–15 胜负
```

⚠ **别只按"哪几个音本来就一起响"来分**。实测踩过：发射音与空中爆炸音原本共用 6/7，
它们本该相隔一整个飞行过程、不会同拍 —— 但凑到一起时**静默少了一个音**
（后者把前者的槽顶掉），日志上只是少两行，很容易当成"没做"。宁可多分几段。

### ⚠ 低音别写太低 —— **手机外放听不见**

手机的外放小喇叭在 **200Hz 以下衰减很快**：写 `36`（C2 = 65Hz）出来是"噗"的一声闷响，
玩家听着像**没响**而不是"低沉"。所以轰鸣的**基音落在 48（C3 = 130Hz）上下**，
低八度只当"配重"垫一层（三角波、谐波少）。

**桌面上听得到不代表手机听得到**（桌面走的是耳机/音箱）—— 这件事只能上真机判断。

### 推进要按**真实流逝时间**，而且**别挂在物理节拍上**

主循环的节奏常常是变的（gorilla 在"飞行 30ms"与"瞄准 120ms"之间切）——
按"绕一圈算一拍"会让同一段音效在两种状态下**快慢不一样**。

⚠⚠ 更阴的一条：**别把 `ui_sfx_tick()` 挂在物理节拍（那一支 `Tick()`）上**。
一局结束的那一刻，物理定时器往往就被杀掉了 —— 而胜负音正要开始放，
结果**只响出第一个音**。症状很像"音效没做"，实际是驱动源选错了。
挂在主循环里、按 `ui_tick()` 的真实流逝时间补拍，就一路活到程序退出：

```c
int sfx_last;
void sfx_pump(void) {                 /* 主循环每轮调一次 */
    int now = ui_tick();
    int n;
    if (sfx_last == 0) { sfx_last = now; return; }
    n = (now - sfx_last) / 33;        /* 一拍 33ms */
    if (n > 4) n = 4;                 /* 卡顿一下别"补跑"一串回来 */
    if (n > 0) { sfx_last = now; while (n > 0) { ui_sfx_tick(); n = n - 1; } }
}
```

⚠ 退出前记得 `ui_sfx_panic()` —— 声部是宿主的资源，不关就会一直响下去
（手机上表现为"切回桌面还有声音"）。

⚠ **BASIC 版另有一个**：`ui_dlg_msg` 这类对话框是**阻塞**的，一弹出来整个主循环就停了、
音序器跟着停。所以终局要"先放胜利音 → 等它放完（约 0.5 秒）→ 再弹框"。

---

## 4.6 倾斜控制：**用加速度计，不是陀螺仪**

想做"把手机歪向哪边、角色就往哪边走"，**绝大多数人第一反应是陀螺仪，而那是错的**：

| 你要的 | 该用哪个 | 为什么 |
|---|---|---|
| **倾斜**（"现在往哪边歪"） | **`VML_SENS_ACCEL`** | 静止时它读到的是**重力方向**，也就是倾斜本身。**不漂移**，几乎所有设备都有 |
| **瞬时动作**（"甩一下"） | `VML_SENS_GYRO` | 它是**角速度**（转得多快）。想拿"当前角度"得自己积分，而**积分会漂**（几十秒就偏出可用范围） |
| **精确、长时间稳定的角度** | `VML_SENS_ROTATION` | 平台把加速度计+陀螺仪（+磁力计）融好的欧拉角。代价是**有的设备没有**（没磁力计时方位角会慢慢转）⇒ 先 `ui_sensor_available` |

**加速度计怎么用**：单位是**毫克**（`1000` = 1g）。

```c
int a[3];
if (ui_sensor(VML_SENS_ACCEL, a)) {
    /* 平放时 a ≈ (0, 0, 1000)；往右歪 30° 时 a[0] ≈ 500 */
    tilt_right = a[0];        /* -1000 .. +1000 */
}
```

⚠⚠ **球往低处滚，所以画面上的偏移要取「反」号** —— 这是最容易写反的一处：
加速度计读的是**"哪条轴朝上"**（世界"上"方向在设备坐标里的表示），
把右边抬起来 `x` 会**变大**，而物体应该往**低**的那边滑。
⇒ `屏幕偏移 = -加速度 / 1000 * 半径`，不是 `+`。

（写反的症状是"歪这边、东西往那边跑"，看起来完全像**传感器轴装反了**，
排查时容易一头扎进平台代码里 —— 其实就错在这一行。）

✅ **已真机确认（2026-09-26）**：把手机右边抬高、球往左滚，与上面这条推导一致。
桌面（vmlcli）注入的只是数字，验的是"给定读数、球走哪边"；**轴的正方向**那一半
是这么补上的 —— 这就是为什么平台相关的约定总要留一条"上真机看一眼"。

⚠ **读不到 ≠ 读到 0**：`ui_sensor` 返回 0 才是"这台设备没有这个传感器"
（返回 1 且三个数都是 0 才是"放平了"）。桌面（vmlcli）就属于前者 ——
它靠输入脚本注入：`accel 0 0 1`（单位是 g）、`gyro 0 0 90`（度/秒）。

⚠ **给一个"校准"入口**（`ui_sensor_calibrate`）：玩家躺在沙发上玩时"水平"是错的，
而且退出重进还是错的。点一下屏幕就把当前姿态当零点 —— 这比在设置里放个滑条好用得多。

现成例子：`examples/c/tilt.c`（水平仪 + 小球，把三个传感器的读数都画在屏幕上）。

---

## 5. 输入：**用系统手柄，别自己画**

绘图窗口底部**本来就有一排屏幕手柄**，程序只该收 `VML_MSG_KEYDOWN` / `VML_MSG_KEYUP`。

**不要在窗口里自绘一套十字键 + 触摸命中判定** —— 代价是三重的：
① 占掉约 140px 窗口高度（棋盘/场景矮一截）；
② 几何要在「画」与「命中判定」两处各算一遍（改个间距就"看着在键上、点下去没反应"）；
③ 每个游戏各画一套，用户还得重新学。

按键常量（就是 Win32 虚拟键值，接物理键盘同一套）：

| 手柄 | 常量 | 值 | 常见用途 |
|---|---|---|---|
| ← ↓ → ↑ | `VML_KEY_LEFT/DOWN/RIGHT/UP` | 37/40/39/38 | 移动 / 跳 |
| A / B | `VML_KEY_PAD_A` / `VML_KEY_PAD_B` | 65 / 66 | 主/副动作 |
| X / Y | `VML_KEY_PAD_X` / `VML_KEY_PAD_Y` | 88 / 89 | 其它动作 |
| START | `VML_KEY_ENTER` | 13 | 重开 / 确认 |
| SELECT | `VML_KEY_SELECT` | 16 | 暂停 / 切换 |
| 空格 | `VML_KEY_SPACE` | 32 | 跳 / 直落 |
| 返回箭头 | `VML_KEY_ESCAPE` | 27 | 退出（`break` 出主循环） |

**按住连发的正确写法**（`mario.c` / `tetris.c` 都是这套）：

```c
/* KeyDown：换键即接管 */
if (d != 0) { if (held != d) holdTicks = 0; held = d; }
/* KeyUp：只认"抬起的是当前按住的键" */
if (d != 0 && d == held) { held = 0; holdTicks = 0; }
/* 每拍：刹车（丢 KeyUp 时不至于永远跑下去）*/
if (held != 0) { holdTicks++; if (holdTicks > 150) held = 0; }
```

---

## 6. 关卡怎么摆：**用矩形列表，别做瓦片地图**

瓦片地图要一张 `int map[200*16]` 的大表（3200 项），编成 `.word` 数据段又长又难改。
平台游戏真正需要的「碰撞体」本来就是**一组矩形**：

```c
/* 每 4 项一个平台：(列, 行, 宽, 高) */
int PLAT[120] = { 0,14,24,2,  26,14,16,2,  10,10,4,1, /* … */ };
int PLATN = 3;

int solid(int c, int r) {          /* 碰撞与绘制**共用这一份**，不会两处不一致 */
    int i = 0;
    while (i < PLATN) {
        if (c >= PLAT[i*4] && c < PLAT[i*4] + PLAT[i*4+2] &&
            r >= PLAT[i*4+1] && r < PLAT[i*4+1] + PLAT[i*4+3]) return 1;
        i = i + 1;
    }
    return 0;
}
```

**碰撞用「先位移、再按格推出」**，比"预测下一步"简单且不会穿模：
水平移动后检查左右两列、竖直移动后检查上下两行，命中就把位置顶回格边、速度清零。

---

## 6.5 走格子（吃豆人 / 推箱子 / 坦克大战这类）

**一句话：一拍走多少像素都行，但"转向 / 停下 / 吃道具"只在格心做，所以要一像素一判。**

这一条在 `Examples/c/pacman.c` 上连错两次，两次都不是笔误，是**位置模型**没想清楚：

```c
/* ✗ 错法一：格心判据说成"坐标是 TILE 的整数倍" —— 那是**格线** */
/* ✗ 错法二：判据对了（离格心 ≤3px），但一拍直接走 spd 像素 */
c = col_of(pxp);                       /* 一越过格线就报**下一格** */
if (!can_go(c, r, pdir)) { /* 前面是墙 */ return; }   /* ← 把人钉在格线上 */
pxp = pxp + DX[pdir] * spd;
```

错法二的现场（自测打印出来的）：`pdir` 恒为 1、`pxp` 恒为 **316**，
而那一格的格心是 **325** —— 人在离格心 9px 的格线上停住，**不吸附、不转向、再也不动**。
用户看到的就是「方向键有反应（`pwant` 确实在变），但人不动」。

```c
/* ✓ 正解：逐像素推进，只在**正落在格心**时做判定 */
int at_center(int v, int origin) {      /* 精确判定，**不给容差** */
    int r = (v - origin) % TILE;
    if (r < 0) r = r + TILE;
    return r == TILE / 2;
}

void move_pac(void) {
    int spd = 3 + level / 3, i = 0;
    while (i < spd) {
        if (at_center(pxp, OX) && at_center(pyp, OY)) {
            pxp = cx_of(col_of(pxp));   /* 吸附，把偏差归零 */
            pyp = cy_of(row_of(pyp));
            eat_at(col_of(pxp), row_of(pyp));
            if (can_go(c, r, pwant)) pdir = pwant;
            if (!can_go(c, r, pdir)) return;   /* 停在格心 */
        }
        pxp = pxp + DX[pdir];               /* 一次 1px */
        pyp = pyp + DY[pdir];
        i = i + 1;
    }
}
```

**为什么这样就对了（两点都是构造保证的）**：

- 从格心出发、每次走 1px ⇒ **必然精确经过格心**（整数坐标，1px 步长不可能跨过它），
  与速度是多少无关。旧写法在 `spd` 不整除 `TILE` 的关卡（4/5/7…）还会**累积漂移**。
- 判定只在格心做 ⇒ 停下来的位置也必然是格心 ⇒ 下一拍还在格心上，不会卡在格线上。

**代价是零**：`spd` 最多 9，一拍最多 9 次循环。**追人的 AI（鬼、敌人）必须用同一套走法** ——
两边算法不同就会出现"鬼能穿墙/鬼卡住"这类只在特定格子暴露的怪事。

> **诊断这类问题不要靠看**：打印 `pdir / pwant / pxp / 格心坐标`，一眼就能看出
> "意图变了但位置没变" —— 那是位置模型的问题，不是按键没收到。

---

## 7. 调试

```bash
# 手机上（App 的「命令行」页）
vml run examples/c/mario.c        # 编译并运行
vml build examples/c/mario.c      # 只编译，出 .vml

# 电脑上（仓库里，编译快得多，用来先验证语法与逻辑）
dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/c/mario.c --timeout 20
```

- **编译报错信息是中文的**，直接看行号。
- **运行期报错**（内存错误 / 标签错误 / 未预期崩溃 + 16 个寄存器 dump）会打进输出 ——
  手机上这些原本落在看不见的流里，现在命令行页会显示出来。
- **别拿 `puts`/`printf` 当判据**调试：本环境的 `puts` 在字面量多的程序里输出会**串行/重复**，
  会把 stdio 的毛病看成代码的毛病。**用 `ui_beep` 的频率报数**或 `ui_dlg_msg` 显示字符串。
- **「源码看着对、跑起来不对」时，把程序\*\*实际读到的数据\*\*打出来** —— 别盯着源码推理。
  这条抓到过一个真身：`int DX[4] = {0,1,0,-1}` 在程序里的值曾是 `{0,1,0,0}`
  （前端把初始化器里的**负数**静默编成了 0，正数全对，所以只看源码永远看不出来）。
  一段循环把数组逐元素 `print_int` 出来，比读十遍源码有用。
- **自测要跑"行为"而不只是"数据"**：`pacman.c` 里有一条正式的**泛洪验证**
  （从出生点 BFS，断言每颗豆子都走得到）+ 一条"跑 400 拍、断言人真的在动、豆子真的在少"。
  前者抓布局死角，后者抓"按键有反应但人不动" —— 两类问题都**只有跑一遍才暴露**。

---

## 8. 参考实现

| 文件 | 看点 |
|---|---|
| `Examples/c/mario.c` | 横版卷轴：摄像机、整数物理、矩形碰撞、踩敌人、金币、终点、三条命 |
| `Examples/c/starfall.c` | **视觉最丰富**：渐变星云背景、三层视差星空、发光叠层、粒子爆炸、震屏、机身倾斜；**触摸拖动操控**（不用手柄） |
| `Examples/c/tetris.c` | 网格游戏：形状表、消行、等级加速、最高分存档、暂停 |
| `Examples/c/gomoku.c` | 回合制：点棋盘落子（这批用**触摸**，不是手柄）、AI 落子、胜负弹框 |
| `Examples/c/piano.c` | **多点触控 + 复音音频**：轮询 `ui_touch(slot)` 做多指边沿检测（事件消息只有槽位 0 会投）、按和弦、急停清音、自适应键位布局 |
| `Examples/c/audio_test.c` | **音频**四段：音阶（音准）/ 和弦同时响（复音）/ 旋律 / 与旧式 `ui_beep` 共存 |
| `Examples/cpp/gorilla.cpp` | **音效最完整的一个**：发射 / 命中 / 空中爆炸 / 轰（撞楼）/ 警报（飞碟被惹毛）/ 胜 / 负，七种各是一种音色；附带一个 16 槽音序器（§4.5）。另有弹道 + 风 + 会塌的楼、飞过的鸟/飞机/飞碟、天黑天亮 |
| `Examples/basic/gorilla.bas`、`basic/gorilla_pro.bas` | 同上玩法的 BASIC 两版，**音效与 C++ 版同一套设计** —— 想看音序器在另一门语言里怎么写，对照着读最快 |
| `Examples/c/draw_prims.c` | 绘图图元逐个体检（矩形/圆/多边形/路径/渐变各一格） |
| `Examples/c/snake.cpp`、`Examples/basic/whack.bas`、`Examples/fortran/sokoban.f90` | 其它语言前端，**同一套 `ui_*` 接口** |

> 换语言只换语法，**接口与骨架完全一样** —— 22 门前端都编到同一个 VML 后端。
