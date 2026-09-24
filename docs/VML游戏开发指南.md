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
void ui_beep(int freq, int ms);          /* 合成音 —— **单通道，一次只发一个音** */
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
| **音效连发一串** | 只听得见最后一个 | 合成音是**单通道**的，后一个音会把前一个停掉。一次事件只发一个音，要用**音高**表达情绪（赢=高、输=低） |
| **网格游戏一拍直接走 N 像素** | 角色**卡在半格上**：不转向、不吃道具，看着像"按键没反应" | 位置一越过格线 `col_of()` 就报**下一格**，"前面是墙"随即在**离格心半格**处把人钉死。见 §6.5 |
| **全局数组初始化里写负数** | 那个负数**变成 0**，正数全对 | C 前端曾有这个缺陷（`-3` 是 `UnaryOp` 而非 `NumberLiteral`，摊平初始化器时掉进兜底 `Add(0)`）。**已修**（`VMLPrepares/CCompiler/ConstFold.cs`），`vml-out-probe` 的 `nat.c` 钉着它 |

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
| `Examples/c/draw_prims.c` | 绘图图元逐个体检（矩形/圆/多边形/路径/渐变各一格） |
| `Examples/c/snake.cpp`、`Examples/basic/whack.bas`、`Examples/fortran/sokoban.f90` | 其它语言前端，**同一套 `ui_*` 接口** |

> 换语言只换语法，**接口与骨架完全一样** —— 22 门前端都编到同一个 VML 后端。
