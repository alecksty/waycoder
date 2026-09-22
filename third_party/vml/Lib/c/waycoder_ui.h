/* waycoder_ui.h —— 手机端 UI syscall 的 C 语言**声明**（对话框 / 窗体 / 绘图 / 输入）
 *
 * ## 实现在哪
 *
 * 函数体在 **`Lib/shared/src/vmlui.c`**，编成 `Lib/shared/vmlui.vml` 后由
 * `vmltool.config.xml` 挂到各语言的 `Libs` 上 —— **C 与 Python 等前端共用同一份实现**。
 * 本头文件只提供声明与常量，`#include` 它不会让程序变大。
 *
 * （早先这层是写在头文件里自带实现的。挪出去的原因：Python 等前端**不支持内联 asm**，
 *   只能按标签调 C 函数 —— 实现留在 `.h` 里，那些语言就一个都调不到，
 *   于是同一套接口会被迫各写一遍。那是本仓库排第一的坑。）
 *
 * ## 号段
 * 500–599 是**宿主 UI 保留段**，只有手机 App（WayCoder）里能用；桌面 vmltool 不认识
 * 这些号。号段表见 `Lib/README-ui.md`。
 *
 * ## 用法（C）
 *
 *     #include <waycoder_ui.h>
 *
 *     int main(void) {
 *         int msg[4];
 *         ui_win_open("演示", 360, 620);
 *         ui_clear(0xFF101020);
 *         ui_set_font(18, VML_FONT_BOLD, 0xFFFFFFFF, VML_ANCHOR_CENTER);
 *         ui_text_cur(180, 40, "按方向键退出");
 *         ui_present();
 *         while (ui_win_closed() == 0) {
 *             if (ui_wait(msg, 0) != VML_MSG_TOUCHDOWN) continue;   // msg[1]=x msg[2]=y
 *         }
 *         return 0;
 *     }
 *
 * 文本按 **UTF-8**（NUL 结尾）传给宿主，源文件存成 UTF-8 即可直接写中文。
 */

/* ⚠ 本头文件要能被 **Objective-C** 源文件 `#include` —— 所以形式参数**不能叫 `id`**
 *   （`id` 在 ObjC 里是保留的**类型名**，形参位置写它会让 ObjC 前端报
 *   `expected ) (got IdType 'id')`）。原型里的形参名对 C 没有任何语义，改名零风险。
 *   实测（2026-09-19）：`Examples/objc/*.m` 引这个头一直是报错的，
 *   台账里那条「别引头文件、直接调用」的绕过就是这么来的。 */
#ifndef WAYCODER_UI_H
#define WAYCODER_UI_H

/* ── 消息类型（与 UI/Shared/VmlUiProtocol.cs 的 VmlMsgType 一一对应）── */
#define VML_MSG_NONE          0
#define VML_MSG_KEYDOWN       1
#define VML_MSG_KEYUP         2
#define VML_MSG_MOUSEMOVE     3
#define VML_MSG_MOUSEDOWN     4
#define VML_MSG_MOUSEUP       5
#define VML_MSG_TOUCHDOWN     6
#define VML_MSG_TOUCHMOVE     7
#define VML_MSG_TOUCHUP       8
#define VML_MSG_TIMER         9
#define VML_MSG_WINDOWCLOSE  10
#define VML_MSG_WINDOWRESIZE 11
/* 方向变了：msg[1] = 新方向（VML_ORIENT_*）。宿主**先发方向、后发尺寸**，
   所以处理 WINDOWRESIZE 时读到的方向已经是新的。 */
#define VML_MSG_WINDOWORIENT 12

/* ── 屏幕方向（ui_orientation() 的返回值）── */
#define VML_ORIENT_PORTRAIT   0
#define VML_ORIENT_LANDSCAPE  1

/* ── 键码（Win32 虚拟键值；手柄那一排见 VmlKeys）── */
#define VML_KEY_BACKSPACE  8
#define VML_KEY_ENTER     13
#define VML_KEY_SELECT    16    /* 手柄 SELECT */
#define VML_KEY_PAUSE     19    /* 手柄 PAUSE */
#define VML_KEY_ESCAPE    27
#define VML_KEY_SPACE     32
#define VML_KEY_LEFT      37
#define VML_KEY_UP        38
#define VML_KEY_RIGHT     39
#define VML_KEY_DOWN      40
#define VML_KEY_PAD_A     65    /* 'A' */
#define VML_KEY_PAD_B     66    /* 'B' */
#define VML_KEY_PAD_X     88    /* 'X' */
#define VML_KEY_PAD_Y     89    /* 'Y' */

/* ── 对话框样式 ── */
#define VML_DLG_INFO     0
#define VML_DLG_WARN     1
#define VML_DLG_ERROR    2
#define VML_DLG_QUESTION 3

/* ── 文字样式位 / 锚点 ── */
#define VML_FONT_BOLD    1
#define VML_FONT_ITALIC  2
#define VML_ANCHOR_LEFT   0
#define VML_ANCHOR_CENTER 1
#define VML_ANCHOR_RIGHT  2

/* ── 文字**竖对齐**（配 `ui_text_v`；老接口没有这一档）──
 *
 * 缺这一档时，程序写"横中"只能得到**横向居中、纵向顶着 y** —— 摆在方框/按钮正中看着偏上。
 * 修之前每个程序都得自己按字号估半个行高，还估不准。
 * 默认（TOP）就是老行为，所以老程序一个字都不用改。 */
#define VML_VANCHOR_TOP    0
#define VML_VANCHOR_MIDDLE 1
#define VML_VANCHOR_BOTTOM 2

/* ── 对话框 ── */
int  ui_dlg_msg(char* title, char* body, int style);
int  ui_dlg_select(char* title, char* body, char* opts, int n, int def);
int  ui_dlg_multi(char* title, char* body, char* opts, int n);
int  ui_dlg_input(char* title, char* prompt, char* buf, int cap);

/* ── 窗体 ── */
int  ui_win_open(char* title, int w, int h);
/* 开窗（带两个声明）。两条都在**开窗之前**就生效 —— 所以按 SCR_W/H 排的版一开始就是对的，
   不会"先按小画布排一次、再收到 resize 重排"。
   老程序照旧用 ui_win_open()（走 #520，宿主那边默认就是"支持旋转 + 要手柄"）。 */
int  ui_win_open_ex(char* title, int w, int h, int rotatable, int gamepad);

/* 第 4 个参数（转屏声明），三选一：
     VML_WIN_PORTRAIT  —— 只支持竖屏（棋盘类）。屏幕**锁在竖屏**，用户怎么转都不动。
     VML_WIN_ROTATABLE —— 支持旋转（默认）。两种排版都写好了 ⇒ 视口一变宿主就发
                          VML_MSG_WINDOWORIENT + VML_MSG_WINDOWRESIZE，并把**新的坐标空间**
                          整个给到窗口（程序按新尺寸重排版即可）。
     VML_WIN_LANDSCAPE —— 只支持横屏（赛车 / 横版过关）。锁在横屏。
   ⚠ 只有 ROTATABLE 那一档会换坐标系。窗口空间被换掉之后，不重新排版的程序会继续按
     老坐标画 ⇒ 内容被裁掉一大截 —— 所以"没声明"（老接口）保持"跟随旋转但坐标系不动"。 */
#define VML_WIN_PORTRAIT     0
#define VML_WIN_ROTATABLE    1
#define VML_WIN_LANDSCAPE    2

/* 第 5 个参数（要不要屏幕手柄区）：
     VML_WIN_NO_GAMEPAD  —— 整块手柄区连同折叠条一起不显示，画布吃满整屏
                            （画图表 / 放幻灯片那种不用手柄的程序）。
     VML_WIN_NEED_GAMEPAD —— 显示（默认）。 */
#define VML_WIN_NEED_GAMEPAD 1
#define VML_WIN_NO_GAMEPAD   0

/* ── 第三种窗口：电脑屏（给老程序用）─────────────────────────────────────
 *
 *   int ui_win_open_pc(char* title, int w, int h, int rotatable, int keyboard);
 *
 * 与 ui_win_open_ex 的三点差别：
 *   ① **坐标系固定**为 (w,h)、永不重排 ⇒ 这里的 rotatable **没有"支持旋转"那一档**，
 *      它只决定锁不锁方向：VML_WIN_PORTRAIT 锁竖 / VML_WIN_LANDSCAPE 锁横 /
 *      其它（含 VML_WIN_ROTATABLE）= 不锁（跟着设备转，但坐标系不动）。
 *   ② 触摸**只当鼠标**（发 MouseDown/Move/Up，不发 Touch*）。
 *   ③ 带**屏幕键盘**而不是手柄区 ⇒ 第 5 个参数是 keyboard 而不是 gamepad：
 *        VML_WIN_NEED_KEYBOARD(1) 显示（默认） / VML_WIN_NO_KEYBOARD(0) 关掉、画布吃满整屏。
 *
 * 画图照旧走 ui_*（ui_clear / ui_rect / ui_line / ui_text …）—— **不碰显存**。 */
#define VML_WIN_NEED_KEYBOARD 1
#define VML_WIN_NO_KEYBOARD   0
int  ui_win_open_pc(char* title, int w, int h, int rotatable, int keyboard);

int  ui_win_close(void);
int  ui_win_closed(void);
int  ui_scr_w(void);
int  ui_scr_h(void);
/* 屏幕方向：VML_ORIENT_PORTRAIT(0) / VML_ORIENT_LANDSCAPE(1)。
   **开窗之前就能问** —— 程序据此决定"棋盘放左、面板放右"还是"上下排"。
   别拿 ui_scr_w() > ui_scr_h() 去推：那两个数是可用**绘图区**，
   会随宿主排版（手柄收起/展开）变，而方向是设备本身的属性。
   运行中方向变了，宿主会先发 VML_MSG_WINDOWORIENT（msg[1]=新方向）再发
   VML_MSG_WINDOWRESIZE（msg[1]=新宽 msg[2]=新高），收到后重新问一次本函数再重排版
   —— 前提是开窗时声明了 VML_WIN_ROTATABLE（老接口不换坐标系，见上）。 */
int  ui_orientation(void);

/* ── 绘图 ── */
void ui_clear(int color);
void ui_pixel(int x, int y, int color);
void ui_line(int x1, int y1, int x2, int y2, int color, int lw);
void ui_rect(int x, int y, int w, int h, int color, int fill, int lw, int radius);
void ui_circle(int cx, int cy, int r, int color, int fill, int lw);
void ui_ellipse(int cx, int cy, int rx, int ry, int color, int fill, int lw);
void ui_icon(int x, int y, char* name, int size, int color);
void ui_image(int x, int y, char* path, int w, int h);
void ui_present(void);

/* ── 像素读回（583–585）────────────────────────────────────────────────
 *
 * 场景是**保留模式**的（只有图元、没有像素缓冲），所以这三个号在宿主侧都会先
 * **光栅化一次**。老 graphics.h 程序要靠它们做填充与精灵。
 */

/* 从 (x,y) 灌色，**碰到 `border` 色就停**（四连通）。返回落笔的矩形条数，
   0 = 没填（种子点本身就在边界色上时是这样 —— 老程序「点在线上」很常见，不是错误）。 */
int ui_flood_fill(int x, int y, int color, int border);

/* 存一块画面 → **句柄**（≥1），失败 0。
   ⚠ 句柄由宿主保管，不是 VML 内存里的缓冲区（与 ui_brush/ui_gradient 同一套）。
   老程序的 `p = malloc(imagesize(...))` 照写不误，只是那块内存我们不用。 */
int ui_get_image(int x, int y, int w, int h);

/* 把句柄那块贴到 (x,y)。`mode`：0=COPY 直接贴 / 1=XOR 异或。返回 1 成功、0 失败。
   ⚠ XOR 必须**先读目的像素**（异或要拿它算），所以比 COPY 多一次光栅化 + 一次编码。 */
int ui_put_image(int x, int y, int handle, int mode);

/* ── 文字 ── */
void ui_text(int x, int y, char* s, int color, int size, int anchor);
void ui_text_styled(int x, int y, char* s, int color, int size, int anchor, int style);
/* 带**竖对齐**的文字（新号 #581）—— valign 见 VML_VANCHOR_*，style 见 VML_FONT_*。
 * ⚠ 与 `ui_text_styled` **参数序不同**（那个第 7 个是 style），别互相照抄。 */
void ui_text_v(int x, int y, char* s, int color, int size, int anchor, int valign, int style);
void ui_set_font(int size, int style, int color, int anchor);
void ui_text_cur(int x, int y, char* s);

/* ── 全能接口（#573）：两个字符串进、一个 JSON 字符串出 ──
 *
 * 给**不要求性能**的可扩展功能用：加一个能力 = 宿主侧注册一个函数，不占 syscall 号、
 * 不用重生成各语言绑定。绘图/输入这类每帧都发生的调用**别走这里**。
 *
 * 结果永远是对象：成功 {"ok":true,"result":…}，失败 {"ok":false,"error":"…"}。
 * 宿主侧已注册：echo（原样回显，自检用）/ version（App 名与版本）/ screen（可用绘图区
 * 与控制方向，与 SCR_W/H/SCR_ORIENT 同源）。 */
int  ui_call_json(char* fn, char* args_json, char* out_buf, int cap);
/* 拿不到缓冲区指针的前端用这两个：结果进静态缓冲，按字节读。 */
int  ui_call_json_s(char* fn, char* args_json);
int  ui_call_json_len(void);
int  ui_call_json_at(int i);
/* 把上一次 ui_call_json_s 的结果整份打到 stdout（末尾补换行）。
   22 种语言的自测都靠它把结果原样打出来，省得各自写一遍"逐字节取 + 单字符输出"。 */
void ui_call_json_print(void);
/* fn 是函数名、args_json 是参数（可为 "" 或 0）。例：
     char buf[256];
     ui_call_json("screen", "", buf, 256);      // {"ok":true,"result":{"w":395,…}}
     ui_call_json("echo", "{\"n\":7}", buf, 256); */

/* ── 不碰指针的消息读取 + 通用整数网格（非 C 语言用，C 也能用）── */
int  ui_wait_msg(int timeout_ms);
int  ui_poll_msg(void);
int  ui_msg_type(void);
int  ui_msg_a(void);
int  ui_msg_b(void);
void ui_gclear(void);
void ui_piece_init(void);
int  ui_piece_cell(int pid, int rot, int which);
void ui_gset(int idx, int val);
int  ui_gget(int idx);

/* ── 输入 ── */
int  ui_poll(int* msg);
int  ui_wait(int* msg, int timeout_ms);
/* 读一条，带"读完之后留不留"：keep=VML_MSG_KEEP 时**只看队头、不取走**
   （下一次读到的还是它，直到明确消费掉）；VML_MSG_CONSUME 与 ui_poll/ui_wait 同义。
   ⚠ 保留模式**别当循环条件用** —— 它永远返回同一条。 */
int  ui_poll_ex(int* msg, int keep);
int  ui_wait_ex(int* msg, int timeout_ms, int keep);
#define VML_MSG_CONSUME 0
#define VML_MSG_KEEP    1
int  ui_msg_count(void);
/* 丢掉队列里所有待处理消息 → 丢弃条数。重新开始/切关时调，
   防上一局没读完的输入（一次点击常有多条）被新一局读出来。 */
int  ui_msg_clear(void);
int  ui_timer_set(int interval_ms, int tag);
int  ui_timer_kill(int timerId);

/* ── 绘图增强（534–539，v0.96.176）：渐变刷子 / 路径与曲线 / 多边形 ──
 *
 * 这一组也是**本地加的**，同样要提给上游 VML 仓库（理由见文件末尾那段）。
 * 坐标与颜色约定与既有绘图接口一致（dp、0xAARRGGBB）。 */

/* 渐变刷子：id 之后用 ui_rect_grad / ui_circle_grad / ui_path 的 grad 参数按名引用。
 * 几何是**归一化 0..1000 的整数**（千分之一）：线性给 x1,y1,x2,y2；径向给 cx,cy,r（第 4 个忽略）。
 * 不想要自定义几何就传 0,0,1000,0（线性从左到右）或 500,500,500,0（径向居中）。 */
void ui_gradient(char* gradId, int radial, int color_a, int color_b,
                 int a1, int a2, int a3, int a4);

/* 路径：d 是 **SVG path 语法**（M L H V C S Q T A Z，大小写区分绝对/相对）。
 * stroke=描边色（0=不描边）、width=线宽、fill=填充色（0=不填充）、
 * grad=渐变 id（非 0 时用它填充、忽略 fill）、cap=0平/1圆/2方、dash=0/1 虚线。 */
void ui_path(char* d, int stroke, int width, int fill, char* grad, int cap, int dash);

/* 多边形（自动闭合）/ 折线：pts 是 int 数组，**每两个 int 一个点**（x,y）；count 是**点数**。 */
void ui_polygon(int* pts, int count, int fill, int stroke, int width, char* grad);
void ui_polyline(int* pts, int count, int stroke, int width, char* grad);

/* 渐变填充的矩形 / 圆（渐变按钮、渐变背景这类最常用） */
void ui_rect_grad(int x, int y, int w, int h, char* grad, int radius);
void ui_circle_grad(int cx, int cy, int r, char* grad);

/* ── 刷子 / 样式 / 形状（574–576）────────────────────────────────────
 *
 * 一条总原则：**颜色 = 只有一个色标的刷子** —— `ui_set_fill(0xFF2A3346)` 直接
 * 传颜色是合法的，不必先 `ui_brush_solid` 一下。
 *
 * 旧的 `ui_rect` / `ui_circle` / … **全部原样保留**（各走各的老号）；这一套是另加的：
 * 样式来自"当前刷子"，绘制调用本身**不带颜色**。
 *
 * ⚠ 本批**画笔与文字只支持纯色**：渐变描边/渐变文字要等引擎侧做出来。
 *   给渐变句柄时宿主会记一次警告并退回该渐变的起始色（不静默）。 */
int  ui_brush_solid(int color);
int  ui_brush_linear(int color_a, int color_b, int x1, int y1, int x2, int y2);
int  ui_brush_radial(int color_a, int color_b, int cx, int cy, int r);
int  ui_brush_named(char* gradId);
int  ui_set_fill(int brush);
int  ui_set_pen(int brush, int width, int cap, int dash, int arrow);
int  ui_set_text_brush(int brush);

void ui_draw_rect(int x, int y, int w, int h, int radius);
void ui_draw_circle(int cx, int cy, int r);
void ui_draw_ellipse(int cx, int cy, int rx, int ry);
void ui_draw_line(int x1, int y1, int x2, int y2);
void ui_draw_poly(int* pts, int count, int close);
void ui_draw_path(char* d);
void ui_draw_text(int x, int y, char* s);
void ui_draw_star(int cx, int cy, int r_out, int r_in, int points, int rot);
void ui_draw_regular(int cx, int cy, int r, int n, int rot);
void ui_draw_ring(int cx, int cy, int r_out, int r_in);
void ui_draw_pie(int cx, int cy, int r, int a0, int a1);
void ui_draw_heart(int cx, int cy, int size);
void ui_ellipse_grad(int cx, int cy, int rx, int ry, char* grad);

/* ui_set_pen 的线帽 */
#define VML_CAP_BUTT    0
#define VML_CAP_ROUND   1
#define VML_CAP_SQUARE  2
/* ui_set_pen 的箭头。⚠ 本批只做**末端**箭头，且只对 ui_draw_line 生效
 *   （折线/路径的箭头要各自的几何支持，还没做）—— START/BOTH 与 END 同义 */
#define VML_ARROW_NONE  0
#define VML_ARROW_END   1
#define VML_ARROW_START 2
#define VML_ARROW_BOTH  3

/* ── 随机数 / 计时（游戏用；实现走 VM 的 #50/#53，各语言共用一份）── */
int  ui_rand(int n);      /* 0..n-1 */
int  ui_tick(void);       /* VM 启动至今毫秒 */

/* ── 手感：音效 / 震动 / 持久化 / 常亮 ──
   音效走 **VM 内置的 #57 蜂鸣**（宿主把它接到真实音频，零素材、不必打包音频文件）。
   这一组同样要提给上游 VML 仓库（Lib/ 会被 sync.sh 覆盖）。 */
void ui_beep(int freq, int ms);              /* 合成音：频率 Hz + 时长 ms */
void ui_vibrate(int ms, int strength);                     /* 震动一下（Android 需 VIBRATE 权限，normal 级） */
void ui_keep_on(int on);                     /* 玩游戏时别熄屏：0 关 / 1 开 */
void ui_store_set(char* key, char* value);   /* 写持久化键值（最高分/进度/设置） */
int  ui_store_get(char* key, char* buf, int cap);  /* 读；返回长度，没有这条键返回 -1 */

/* ── 命令行参数 ──
   参数由宿主喂（桌面 `vmlcli prog.c --arg -l`、手机 `vml run prog.c -l`）。
   `argv[0]` 是程序名、恒存在 ⇒ 用户给的第一个参数是 `ui_arg(1,…)`。
   ⚠ `int main(int argc, char **argv)` 的程序**不需要**它们（入口帧直接给），
      这两个是给 `int main(void)` 的程序和非 C 语言绑定用的。 */
int  ui_argc(void);                          /* 参数个数（含程序名，恒 ≥ 1） */
int  ui_arg(int i, char* buf, int cap);      /* 拷第 i 个进 buf；返回长度，越界 -1 */

/* ── 通用宿主调用口（577–580，v0.96.326）──────────────────────────────
 *
 * 这四个函数让程序**按数字 id 调宿主的函数**：参数直接躺在寄存器里、返回值直接
 * 写回第 0 号寄存器，**一次调用零编解码**（对比 `ui_call_json` —— 那条路要
 * 序列化 + 解析两趟 JSON，好处是加能力不用占号）。
 *
 * 用法（四步）：
 *
 *     int v[8];
 *     v[0] = VML_CALL_ECHO_INT;   ① 调用号，见下面的宏
 *     v[1] = 1; v[2] = 2; …       ② 参数（不够的位留 0）
 *     int r = callwithint8(v);    ③ 发出去，返回值覆盖 v[0] —— ④ **拿不到原来的 v[0]**
 *
 * ## 寄存器约定（跨语言契约，改不得）
 *
 *     callwithint8  : v[0..7] → R0..R7   返回值 → R0
 *     callwithfloat8: v[0..7] → F0..F7   返回值 → F0
 *     callwithlong4 : v[0..3] → L0..L3   返回值 → L0
 *     callwithdouble4: v[0..3] → D0..D3  返回值 → D0
 *
 * 第 0 个元素**既是调用号（进）又是返回值（出）** —— 所以"调用方拿不到 val[0]"
 * 是设计的一部分，不是缺陷。四个函数的调用号都是 `(int)v[0]`
 * （float 那个也一样：调用号必须能被 `float` 精确表示，id < 2²⁴ 就安全）。
 *
 * ## 失败长什么样
 *
 * **不崩**。没注册过的 id、种类对不上、实现体自己抛异常，都写回一个**负数失败码**：
 * `-2` 参数非法、`-6` 没有这个调用号、`-9` 宿主内部错误（正数才是正常返回值）。
 * 宿主同时会记一条可读日志。
 *
 * ## 实现与注册
 *
 * 实现在 `Lib/shared/src/vmlui.c`（编成 `.vml` 后各语言共用），
 * 调用号 → 函数的对应表在宿主里（手机 `WayCoder.Maui/Services/VmlUiCalls.cs`、
 * 桌面 `scripts/vmlcli`，两边共用 `WayCoder/UI/Shared/VmlCallRegistry.cs`）。
 */
int    callwithint8(int* v);
float  callwithfloat8(float* v);
long   callwithlong4(long* v);
double callwithdouble4(double* v);

/* 调用号 —— **跨语言契约，只能末尾追加、不能改值**。
 * 本批是"自检族"：返回值是每个参数按位权拼出来的可分辨数
 * （传 1,2,3,4,5,6,7 得 7654321），哪个参数没到、串没串位一眼看得出。 */
#define VML_CALL_ECHO_INT     1   /* callwithint8：7 个 int 的位权拼接 */
#define VML_CALL_ECHO_FLOAT   2   /* callwithfloat8：同上，浮点域 */
#define VML_CALL_ECHO_LONG    3   /* callwithlong4：3 个 long 的位权拼接（×1 / ×1000 / ×10⁶） */
#define VML_CALL_ECHO_DOUBLE  4   /* callwithdouble4：同上，浮点域 */
#define VML_CALL_HOST_INFO    5   /* callwithint8：宿主种类（VML_CALL_HOST_*） */

/* VML_CALL_HOST_INFO 的返回值 */
#define VML_CALL_HOST_DESKTOP 1   /* 桌面脚手架（scripts/vmlcli） */
#define VML_CALL_HOST_MOBILE  2   /* 手机 App */

#endif /* WAYCODER_UI_H */
