/* graphics.h —— **BGI（Borland Graphics Interface）兼容层**
 *
 * DOS 时代的老图形程序几乎都是这么开场的：
 *
 *     #include <graphics.h>
 *     int main(void) {
 *         int gd = DETECT, gm;
 *         initgraph(&gd, &gm, "");
 *         line(0, 0, 100, 100);
 *         circle(200, 200, 50);
 *         getch();
 *         closegraph();
 *     }
 *
 * 本头文件把**这一套名字**接过来，实现全部落在本平台的 `ui_*` 绘图接口上 ——
 * 老程序**一行不改**就能编译运行（这正是它存在的唯一理由）。
 *
 * ## 与「显存 / BGI 一整套不做」那条定案的关系（**不矛盾，是澄清**）
 *
 * 定案反对的是**直接操作内存**：往 `0xA0000` 显存直写、靠 BIOS 中断设模式、
 * `DEF SEG` 那类。它们在本平台上做不到，**也不该做**（见 docs/老程序兼容性.md 的 C 档）。
 *
 * 而这里做的是**转接**：BGI 的**函数名与语义**原样保留，落笔换成 `ui_*`
 * —— 程序看到的仍然是「画线 / 画圆 / 设色」，只是不再自己碰显存。
 * 换句话说：**要改的是"怎么画"，不改的是"程序怎么写"**。
 *
 * ## 落到哪个窗口
 *
 * `initgraph` 用 **`ui_win_open_pc`（第三种窗口：电脑屏）**：固定坐标系、不随旋转重排 ——
 * 正是为"按 640×480 排好版的老程序"准备的。老图形程序默认就是 VGA 640×480。
 *
 * ## ⚠ 颜色是**调色板索引**，不是 RGB
 *
 * BGI 的 `setcolor(4)` 是「红」而不是「RGB 0x000004」。老程序里到处是索引常量，
 * 所以这里按 **CGA/VGA 的 16 色标准调色板**翻译成 RGB（见下方 `_bgi_pal`）——
 * 不做这层翻译的话，老程序画面会**整片黑**（索引 0..15 当 RGB 用几乎全黑）。
 *
 * ## 覆盖范围
 *
 * 收的是**用得最多**的那些（用户定的规矩：只管大多数）。没做的
 * （`registerbgidriver`、字体文件、`setviewport` 那几样）**不假装支持** ——
 * 缺了就是编译期找不到符号，比"编得过、跑起来什么也没有"好排查。
 *
 * ⚠ `textwidth`/`textheight` 原先也在这张"不做"的名单里，**后来收进来了**：
 *   它们不是"做不了"，而是**必须与渲染同源**才算得对（老程序拿它居中排版）。
 *   做法是问宿主注册的 `text` JSON 函数，见下面「文字量测」那一节。
 *
 * ## 像素读回是**平台新做的**（v0.96.379）
 *
 * `floodfill` / `getimage` / `putimage` 一开始列为"不做"，理由是"场景没有像素缓冲"。
 * 但它们恰恰是老图形程序**填充**与**精灵**的两条命脉（实测语料里 `floodfill` 出现
 * 10 次），所以后来按用户的意见做成了三个 syscall（583–585）：
 * 宿主**当场光栅化一次**来回答"这个像素是什么颜色"，再把结果落回场景。
 * ⚠ 三者的实现代价不同：`floodfill` / `putimage(XOR)` 各要一次光栅化，
 *   `putimage(COPY)` 不用 —— 精灵动画每帧两次 putimage，这个差别是实打实的。
 */
#ifndef _GRAPHICS_H
#define _GRAPHICS_H

#include <vml_compat.h>      /* `far`/`near` 这类扩展关键字按空宏抹掉 */
#include <waycoder_ui.h>     /* 落笔走 ui_* */
/* `getch` / `kbhit` 从 conio 来 —— **本文件不再自己定义它们**。
 * ⚠ 这一行是**必须的**：老程序里既有 `#include <conio.h>` 的（多数），
 *   也有**只 include `<graphics.h>`** 的（如 `Hut.cpp`）—— 后者原先靠本文件
 *   自带的那份 getch，删掉之后不加这一行就**没有 getch 了**（编译不报错、
 *   链接期才炸，或者退化成隐式声明）。 */
#include <conio.h>

/* ── 模式与常量（老程序里到处在用）───────────────────────────── */
#define DETECT        0
#define VGA           9
#define EGA           5
#define CGA           3
#define IBM8514       6

/* ── 图形模式表（Borland BGI 的标准分辨率）────────────────────────────
 *
 * 老程序 `initgraph(&gd, &gm, "")` 里那个 `gm` 决定**分辨率**，而手机端画布是按
 * 可用区缩放的 ⇒ **拿到正确的宽高比**，按 640×350 排的版才不会被拉高变形
 * （塞进 640×480 会被纵向拉伸）。从前这里只实现了 VGA 640×480 一种。
 *
 * 模式号是**每驱动各自编号**的（CGA 的 0 与 EGA 的 0 不是一回事）⇒ 必须 (驱动, 模式) 成对查。
 * 查不到的组合退回 VGA 640×480（老程序不写模式时的默认）。 */
#define CGAC0    0
#define CGAC1    1
#define CGAC2    2
#define CGAC3    3
#define CGAHI    4
#define EGALO    0
#define EGAHI    1
#define VGALO    0
#define VGAMED   1
#define VGAHI    2

#define MAXCOLORS     15

/* 16 色调色板索引（BGI 的常量名就是数字，这里给几个常用的别名） */
#define BLACK         0
#define BLUE          1
#define GREEN         2
#define CYAN          3
#define RED           4
#define MAGENTA       5
#define BROWN         6
#define LIGHTGRAY     7
#define DARKGRAY      8
#define LIGHTBLUE     9
#define LIGHTGREEN   10
#define LIGHTCYAN    11
#define LIGHTRED     12
#define LIGHTMAGENTA 13
#define YELLOW       14
#define WHITE        15

/* 填充样式 */
#define EMPTY_FILL    0
#define SOLID_FILL    1
#define LINE_FILL     2
#define LTSLASH_FILL  3
#define SLASH_FILL    4
#define BKSLASH_FILL  5
#define LTBKSLASH_FILL 6
#define HATCH_FILL    7
#define XHATCH_FILL   8
#define INTERLEAVE_FILL 9
#define WIDE_DOT_FILL 10
#define CLOSE_DOT_FILL 11
#define USER_FILL     12

/* 线型 / 线宽 */
#define SOLID_LINE    0
#define DOTTED_LINE   1
#define CENTER_LINE   2
#define DASHED_LINE   3
#define NORM_WIDTH    1
#define THICK_WIDTH   3

/* 位运算模式（`putimage` 的第四个参数）。
 * ⚠ 只实现这两个 —— BGI 还有 OR_PUT/AND_PUT/NOT_PUT 等，平台上没有对应物，
 *   **不假装支持**（做成"当 COPY 处理"会让程序画出错的东西还不报错）。 */
#define COPY_PUT      0
#define XOR_PUT       1

/* 文字对齐（settextjustify）*/
#define LEFT_TEXT     0
#define CENTER_TEXT   1
#define RIGHT_TEXT    2

/* 文字方向 */
#define HORIZ_DIR     0
#define VERT_DIR      1

/* 字体（settextstyle 的第一个参数）。
 * ⚠ 本平台**只有一种字形**，这些常量收下来只是为了"编得过"——
 *   按字体名切换字形的能力没有，字号（第三个参数）是**认的**（见 settextstyle）。
 *   老程序里 `settextstyle(SANS_SERIF_FONT, HORIZ_DIR, 2)` 这种写法极常见，
 *   不收常量的话整份源码一个字都编不过（实测：6 个经典 BGI 程序里有 2 个中招）。 */
#define DEFAULT_FONT        0
#define TRIPLEX_FONT        1
#define SMALL_FONT          2
#define SANS_SERIF_FONT     3
#define GOTHIC_FONT         4
#define SCRIPT_FONT         5
#define SIMPLEX_FONT        6
#define TRIPLEX_SCR_FONT    7
#define COMPLEX_FONT        8
#define EUROPEAN_FONT       9
#define BOLD_FONT           10

/* `NULL`：老程序普遍只 `#include <graphics.h>` 就拿它当空指针用（Turbo C 时代
 * 它由 BGI 的头**间接**带进来）。真去 `#include <stdlib.h>` 会连带牵进一大堆本平台
 * 用不着的东西，所以这里直接兜一个。
 * ⚠ 只兜"没定义过"的情形 —— 谁先定过就用谁的，避免重复定义（C 里 `#define` 重定义
 *   只在**展开后不同**时才报警，而 `((void*)0)` 与别的写法展开不同，会有警告刷屏）。 */
#ifndef NULL
#define NULL ((void *)0)
#endif

/* ── 内部状态（每个程序一份；本文件是头文件，用 static 隔离）────── */
static int _bgi_fg       = WHITE;   /* 当前前景（调色板索引）*/
static int _bgi_bg       = BLACK;   /* 当前背景 */
static int _bgi_fill_col = WHITE;   /* 当前填充色 */
static int _bgi_fill_pat = SOLID_FILL;
static int _bgi_line_w   = NORM_WIDTH;
static int _bgi_pen_x    = 0;       /* moveto/lineto 的当前点 */
static int _bgi_pen_y    = 0;
/* BGI 的 charsize 是**放大倍数**（1 = 8×8 基本字形），不是像素高度。
 * 本平台 `ui_text` 收的是**像素字号** ⇒ 两者之间要换算，基准就是"1 倍"对应的像素数。
 * ⚠ 这个常数与 `_bgi_txt_size` 的默认值必须是同一个 —— 默认字号 = 1 倍字号。 */
#define _BGI_CHAR_BASE 16
static int _bgi_txt_size = _BGI_CHAR_BASE;   /* 像素字号（= BGI 倍数 × _BGI_CHAR_BASE）*/
static int _bgi_txt_just = LEFT_TEXT;
static int _bgi_maxx     = 639;     /* getmaxx/getmaxy —— 跟着当前图形模式走 */
static int _bgi_maxy     = 479;
static int _bgi_opened   = 0;
static int _bgi_driver   = VGA;     /* 当前驱动（setgraphmode 要按它查表） */
static int _bgi_mode     = VGAHI;   /* 当前模式 */
static char *_bgi_title  = 0;       /* 开窗时定的标题 —— setgraphmode 重开窗要用同一个 */

static void _bgi_mode_size(int gd, int gm, int *pw, int *ph)
{
    int w = 640, h = 480;                 /* 默认 = VGA 640×480（老程序最熟的那一档） */

    if (gd == CGA) {
        if (gm == CGAHI) { w = 640; h = 200; }
        else             { w = 320; h = 200; }   /* CGAC0..C3：320×200 */
    } else if (gd == EGA) {
        if (gm == EGAHI) { w = 640; h = 350; }
        else             { w = 640; h = 200; }   /* EGALO */
    } else if (gd == VGA) {
        if (gm == VGAHI)      { w = 640; h = 480; }
        else if (gm == VGAMED){ w = 640; h = 350; }
        else                  { w = 640; h = 200; }   /* VGALO */
    }
    /* IBM8514 等其它驱动：保持 640×480（本平台只有 16 色调色板，高档模式也画不出更多色） */

    *pw = w;
    *ph = h;
}

/* CGA/VGA 标准 16 色调色板：BGI 的索引 → 本平台的 0xRRGGBB
 * ⚠ 不做这层翻译，`setcolor(4)` 会被当成 RGB 0x000004 ⇒ 画面几乎全黑。 */
static int _bgi_pal[16] = {
    0x000000,  /*  0 黑   */
    0x0000AA,  /*  1 蓝   */
    0x00AA00,  /*  2 绿   */
    0x00AAAA,  /*  3 青   */
    0xAA0000,  /*  4 红   */
    0xAA00AA,  /*  5 洋红 */
    0xAA5500,  /*  6 棕   */
    0xAAAAAA,  /*  7 浅灰 */
    0x555555,  /*  8 深灰 */
    0x5555FF,  /*  9 亮蓝 */
    0x55FF55,  /* 10 亮绿 */
    0x55FFFF,  /* 11 亮青 */
    0xFF5555,  /* 12 亮红 */
    0xFF55FF,  /* 13 亮洋红 */
    0xFFFF55,  /* 14 黄   */
    0xFFFFFF   /* 15 白   */
};

/* 调色板索引 → 本平台的颜色（**0xAARRGGBB**）。
 *
 * ⚠ **必须补上不透明的 alpha（0xFF）**。`_bgi_pal` 里存的是 24 位 RGB，
 *   高 8 位是 0，而平台的落笔是 `if (a == 0) return; // 全透明 = 不画`
 *   —— 不补的话**所有颜色都是全透明**：程序照常跑、`floodfill` 照常算、
 *   一个错误都不报，**屏幕上却什么都没有**（实测踩到：BGI 程序出帧全黑）。
 *   这也是"BGI 的屏幕是不透明的"这句常识在代码上的落点。 */
int _bgi_rgb(int idx)
{
    if (idx < 0) idx = 0;
    if (idx > 15) idx = 15;
    return _bgi_pal[idx] | 0xFF000000;
}

/* ── 生命周期 ──────────────────────────────────────────────── */

/* 取路径里的文件名部分（`examples/c/old/x.c` → `x.c`）。
 * 老程序不设标题，用源文件名当标题最好认 —— 而 `__FILE__` 给的是**整条路径**。 */
char *_bgi_basename(char *p)
{
    char *last = p;
    char *q    = p;
    if (p == 0) return "";
    while (*q) {
        if (*q == '/' || *q == '\\') last = q + 1;
        q++;
    }
    return last;
}

/* 老程序的固定开场。`gd`/`gm` 按 BGI 语义是入参+出参，
 * 但本平台只有一种图形模式，所以只读取 `*gd` 是否 DETECT 来决定要不要自动选。
 *
 * ⚠ **`file` 参数存在的原因**：老程序 `initgraph(&gd,&gm,"")` **不设标题**，
 *   于是拿源文件名当窗口标题。而 `__FILE__` **不能写在头文件里** —— 它是文本替换，
 *   写在这儿会展开成 `graphics.h`（头自己的名字）。必须由下面的 `initgraph` 宏
 *   在**调用点**把它传进来，展开的才是**调用者那个文件**。 */
void _bgi_initgraph(int *gd, int *gm, char *path, char *file)
{
    int w = 640, h = 480;          /* VGA 默认 */

    (void)path;
    /* ⚠ **`DETECT`(=0) 只是 `gd` 的取值，不是模式的**：BGI 里 `gm` 是**出参**
     *   （老程序的标准写法是 `int gd = DETECT, gm; initgraph(&gd, &gm, "");` ——
     *   `gm` 根本不初始化），而模式 0 是**合法模式**：`VGALO`/`CGAC0`/`EGALO` 都等于 0。
     *   把"模式 == 0"也当成 DETECT 去改写，会让 `initgraph(&VGA, &VGALO)` **静默变成
     *   640×480**（探针实测：VGALO 那条红）。 */
    if (gd != 0 && *gd == DETECT) {
        *gd = VGA;                       /* 驱动与模式都由库来挑（本平台只有 16 色调色板） */
        if (gm != 0) *gm = VGAHI;
    }

    _bgi_driver = (gd != 0) ? *gd : VGA;
    _bgi_mode   = (gm != 0) ? *gm : VGAHI;
    /* 回填给调用方的是**模式**。⚠ 从前这里错写成 `*gm = VGA`（把驱动号塞进模式出参）。 */
    if (gm != 0) *gm = _bgi_mode;

    /* **分辨率由 (驱动, 模式) 查表**（CGA 320×200 / EGA 640×350 / VGA 640×480 …）——
     * 从前只有 640×480 一种，按 640×350 排版的老程序会被拉高。 */
    _bgi_mode_size(_bgi_driver, _bgi_mode, &w, &h);

    /* ⚠ 用**电脑屏窗口**：固定坐标系、不随旋转重排 —— 老程序按固定分辨率排的版
     *   换空间就会画到框外。第 5 个参数是要不要屏幕键盘（老图形程序常要按键）。*/
    _bgi_title = _bgi_basename(file);
    ui_win_open_pc(_bgi_title, w, h, VML_WIN_ROTATABLE, VML_WIN_NEED_KEYBOARD);

    _bgi_maxx   = w - 1;
    _bgi_maxy   = h - 1;
    _bgi_opened = 1;
    cleardevice();
}

/* 包一层的唯一目的：让 `__FILE__` 在**调用点**展开（见上面的 ⚠） */
#define initgraph(gd, gm, path)  _bgi_initgraph((gd), (gm), (path), __FILE__)

/* `initwindow` 是 **WinBGIm**（BGI 的 Windows 移植）的入口，语义就是"开一个指定大小的
 * 图形窗口"，不收 `gd`/`gm`：
 *
 *     initwindow(640, 480, "标题");
 *
 * 老程序里两条路都有（DOS 时代的用 `initgraph`，Windows 时代的用 `initwindow`），
 * 收下它一条程序都不用改。⚠ 窗口标题照 WinBGIm 的**第三参**给，不再拿源文件名兜
 * —— 这条与 `initgraph` 不同（那个的确不设标题）。
 *
 * ⚠ 同样要包一层宏：`__FILE__` 写在头文件里会展开成 `graphics.h` 自己。 */
void _bgi_initwindow(int w, int h, char *title, char *file)
{
    if (w <= 0) w = 640;
    if (h <= 0) h = 480;
    _bgi_title = (title != 0 && title[0] != 0) ? title : _bgi_basename(file);
    ui_win_open_pc(_bgi_title, w, h, VML_WIN_ROTATABLE, VML_WIN_NEED_KEYBOARD);
    _bgi_maxx   = w - 1;
    _bgi_maxy   = h - 1;
    _bgi_opened = 1;
    cleardevice();
}

/* `setgraphmode(mode)`：切到**当前驱动的另一个模式**（BGI 的语义是"清屏 + 换模式"）。
 * 老程序用它在游戏内换分辨率（低分辨率跑得快 / 高分辨率看得清）。
 *
 * ⚠ 电脑屏窗口的坐标系是**开窗时定死**的（`VmlWinKind.PcScreen` 正是为"按固定分辨率
 *   排好版的老程序"准备的）⇒ 换模式只能**关掉重开**：新窗口 = 新坐标系。
 *   这与 `initgraph` 同一套语义，老程序看不出来（它本来也要清屏重画）。 */
void setgraphmode(int mode)
{
    int w, h;
    _bgi_mode = mode;
    _bgi_mode_size(_bgi_driver, _bgi_mode, &w, &h);
    if (_bgi_opened) ui_win_close();
    ui_win_open_pc(_bgi_title != 0 ? _bgi_title : "", w, h, VML_WIN_ROTATABLE, VML_WIN_NEED_KEYBOARD);
    _bgi_maxx   = w - 1;
    _bgi_maxy   = h - 1;
    _bgi_opened = 1;
    cleardevice();
}

#define initwindow(w, h, title)  _bgi_initwindow((w), (h), (title), __FILE__)

void closegraph(void)
{
    if (_bgi_opened) {
        ui_win_close();
        _bgi_opened = 0;
    }
}

void cleardevice(void)
{
    ui_clear(_bgi_rgb(_bgi_bg));
}

int getmaxx(void) { return _bgi_maxx; }
int getmaxy(void) { return _bgi_maxy; }

/* `getmaxcolor` 是 BGI 里**画随机色**的标准写法的一半：
 *
 *     setcolor(random(getmaxcolor() + 1));
 *
 * 收进来是因为此前**漏了**（语料里 8 处、`Examples/c/test_bgi.c` 也没盖到），
 * 而它的实现就是本平台唯一的答案：**只有 16 色调色板** ⇒ 恒 `MAXCOLORS`。
 * ⚠ 别按"当前视频模式"去查表 —— 本平台的模式表（`_bgi_mode_size`）只管**分辨率**，
 *   高档模式（EGA/VGA 的暗色档）并不真的多出颜色。 */
int getmaxcolor(void) { return MAXCOLORS; }

/* ── 错误码（`graphresult` / `grapherrormsg`）──────────────────
 *
 * 老程序的标准开场是**先开窗、再看错误**：
 *
 *     gd := Detect; InitGraph(gd, gm, '');
 *     ErrorCode := GraphResult;
 *     if ErrorCode <> grOk then begin Writeln(GraphErrorMsg(ErrorCode)); Halt; end;
 *
 * 语料里 `graphresult` 21 处、`grapherrormsg` 16 处 —— **这两个不做，几乎每一份
 * 老 Pascal 图形程序都编不过**（链接期"未解析标签"，而那是致命的）。
 *
 * 本平台上开窗**不会失败**（`ui_win_open_pc` 要么开出窗口、要么根本没有宿主），
 * 所以 `graphresult` 恒回 `grOk`(0) —— 如实表达"没有错误"，而不是编一个假错误码
 * （编一个的话老程序会去走它那条 `Halt` 分支，反而更难排查）。
 * `grapherrormsg` 仍然收下（老程序要拿它写日志），按 Borland 的原文回字符串。 */

#define grOk              0
#define grNoInitGraph    (-1)
#define grNotDetected    (-2)
#define grFileNotFound   (-3)
#define grInvalidDriver  (-4)
#define grNoLoadMem      (-5)
#define grNoScanMem      (-6)
#define grNoFloodMem     (-7)
#define grFontNotFound   (-8)
#define grNoFontMem      (-9)
#define grInvalidMode   (-10)
#define grError         (-11)
#define grIOerror       (-12)
#define grInvalidFont   (-13)
#define grInvalidFontNum (-14)
#define grInvalidVersion (-18)

int graphresult(void) { return grOk; }

char *grapherrormsg(int code)
{
    if (code == grOk)             return "No error";
    if (code == grNoInitGraph)    return "BGI graphics not installed (use InitGraph)";
    if (code == grNotDetected)    return "Graphics hardware not detected";
    if (code == grFileNotFound)   return "Device driver file not found";
    if (code == grInvalidDriver)  return "Invalid device driver file";
    if (code == grNoLoadMem)      return "Not enough memory to load driver";
    if (code == grNoScanMem)      return "Out of memory in scan fill";
    if (code == grNoFloodMem)     return "Out of memory in flood fill";
    if (code == grFontNotFound)   return "Font file not found";
    if (code == grNoFontMem)      return "Not enough memory to load font";
    if (code == grInvalidMode)    return "Invalid graphics mode for selected driver";
    if (code == grError)          return "Graphics error";
    if (code == grIOerror)        return "Graphics I/O error";
    if (code == grInvalidFont)    return "Invalid font file";
    if (code == grInvalidFontNum) return "Invalid font number";
    if (code == grInvalidVersion) return "Invalid version of file";
    return "Unknown graphics error";
}

/* ── 设置 ──────────────────────────────────────────────────── */

void setcolor(int c)       { _bgi_fg = c; }
void setbkcolor(int c)     { _bgi_bg = c; }
/* `getcolor`/`getbkcolor` 是上面两个的**读回**。老程序拿它做"画一笔再还原"：
 *
 *     old = getcolor(); setcolor(RED); …; setcolor(old);
 *
 * 收进来同 `getmaxcolor`：此前漏了（语料里 3 处），而实现就是"当前状态"本身。 */
int  getcolor(void)        { return _bgi_fg; }
int  getbkcolor(void)      { return _bgi_bg; }
void setfillstyle(int pattern, int color)
{
    (void)pattern;
    _bgi_fill_pat = pattern;
    _bgi_fill_col = color;
}
void setlinestyle(int style, unsigned pattern, int thickness)
{
    (void)style; (void)pattern;
    _bgi_line_w = (thickness == THICK_WIDTH) ? 3 : 1;
}
/* ⚠ 第三个参数是**字符放大倍数**，不是像素高度 —— 直接把倍数当像素传下去，
 * `settextstyle(0, HORIZ_DIR, 1)` 会把字号设成 **1 像素**，于是**一个字都看不见**。
 * 老程序里这一句极其常见（实测 `userinput2.cpp` 因此整屏空白，而它旁边不带这句的
 * 最小复现是好的），所以这里必须换算：像素 = 倍数 × `_BGI_CHAR_BASE`。 */
void settextstyle(int font, int dir, int size)
{
    (void)font; (void)dir;
    if (size > 0) _bgi_txt_size = size * _BGI_CHAR_BASE;
}
void settextjustify(int horiz, int vert)
{
    (void)vert;
    _bgi_txt_just = horiz;
}

/* ── 画图 ──────────────────────────────────────────────────── */

void line(int x1, int y1, int x2, int y2)
{
    ui_line(x1, y1, x2, y2, _bgi_rgb(_bgi_fg), _bgi_line_w);
}

void moveto(int x, int y) { _bgi_pen_x = x; _bgi_pen_y = y; }

void lineto(int x, int y)
{
    line(_bgi_pen_x, _bgi_pen_y, x, y);
    _bgi_pen_x = x;
    _bgi_pen_y = y;
}

void linerel(int dx, int dy)
{
    lineto(_bgi_pen_x + dx, _bgi_pen_y + dy);
}

/* ⚠ BGI 的 `rectangle(l, t, r, b)` 收的是**右下角坐标**，
 *   而本平台 `ui_rect` 收的是**宽高** —— 这层换算必须在这里做掉。 */
void rectangle(int l, int t, int r, int b)
{
    ui_rect(l, t, r - l, b - t, _bgi_rgb(_bgi_fg), 0, _bgi_line_w, 0);
}

void bar(int l, int t, int r, int b)
{
    ui_rect(l, t, r - l, b - t, _bgi_rgb(_bgi_fill_col), 1, 1, 0);
}

void bar3d(int l, int t, int r, int b, int depth, int topflag)
{
    bar(l, t, r, b);
    rectangle(l, t, r, b);
    if (topflag) {
        line(l, t, l + depth, t - depth);
        line(r, t, r + depth, t - depth);
        line(l + depth, t - depth, r + depth, t - depth);
        line(r, b, r + depth, b - depth);
        line(r + depth, b - depth, r + depth, t - depth);
    }
}

void circle(int x, int y, int r)
{
    ui_circle(x, y, r, _bgi_rgb(_bgi_fg), 0, _bgi_line_w);
}

void fillellipse(int x, int y, int xr, int yr)
{
    ui_ellipse(x, y, xr, yr, _bgi_rgb(_bgi_fill_col), 1, 1);
}

void ellipse(int x, int y, int st, int en, int xr, int yr)
{
    (void)st; (void)en;      /* 本平台的椭圆是整圈；起始/终止角暂不支持 */
    ui_ellipse(x, y, xr, yr, _bgi_rgb(_bgi_fg), 0, _bgi_line_w);
}

/* ⚠ 顶点数组必须传**具名数组**的地址 —— `(int[]){…}` 复合字面量本前端不支持
 *   而且不报错（见 Examples/c/draw_prims.c 的说明）。老 BGI 程序本来就是这么写的。 */
void drawpoly(int numpoints, int *polypoints)
{
    ui_polyline(polypoints, numpoints, _bgi_rgb(_bgi_fg), _bgi_line_w, 0);
}

void fillpoly(int numpoints, int *polypoints)
{
    ui_polygon(polypoints, numpoints, _bgi_rgb(_bgi_fill_col), _bgi_rgb(_bgi_fg),
               _bgi_line_w, 0);
}

void putpixel(int x, int y, int color)
{
    ui_pixel(x, y, _bgi_rgb(color));
}

/* ── 扇形 / 弧（`arc` / `pieslice` / `sector`）──────────────────
 *
 * 老程序画**饼图**就靠这三个（`Examples` 语料里 `pieslice` 用了 3 次、`arc` 2 次）。
 *
 * ## 为什么这三个走的是**另一套**绘图接口
 *
 * 上面那些图元用的是"颜色当参数"的老接口（`ui_circle(x,y,r,色,…)`）—— 那一套
 * **画不了扇形**（只能画整圈）。平台侧能画扇形的是新的刷子接口
 * `ui_draw_pie(cx, cy, r, a0, a1)`，它的颜色来自**当前刷子**而不是参数。
 * 所以这里先 `ui_set_fill`/`ui_set_pen` 设好再画。
 *
 * ## 两条硬约定
 *
 * ① **`ui_set_fill(0)` = 不填充**（宿主侧 `_fill = brush == 0 ? null : …`）——
 *    `arc` 要的正是"只有轮廓"，靠它表达；`pieslice`/`sector` 才填色。
 * ② **刷子是全局状态，画完必须复位** —— 不复位的话，这把刷子会**漏给后面的图元**
 *    （上一条 `pieslice` 的填充色粘到下一个 `rectangle` 上），而这类串色在画面上
 *    看着像"某个颜色配错了"，极难反查到是扇形留下的。
 *
 * ⚠ `sector` 在 BGI 里收的是**两个半径**（可画椭圆扇形），而平台侧只有单半径的
 *   `ui_draw_pie` ⇒ 这里按 `xr` 画**圆**扇形，椭圆扇形没做。这条是**能力缺口**，
 *   不是"忘了"：真遇到了要往宿主加一个椭圆扇形图元。
 */
static void _bgi_pie(int x, int y, int st, int en, int r, int filled)
{
    ui_set_fill(filled ? _bgi_rgb(_bgi_fill_col) : 0);
    ui_set_pen(_bgi_rgb(_bgi_fg), _bgi_line_w, 0, 0, 0);
    ui_draw_pie(x, y, r, st, en);

    /* 复位：不留状态给后面的图元（见上面第 ② 条）*/
    ui_set_fill(0);
    ui_set_pen(_bgi_rgb(_bgi_fg), NORM_WIDTH, 0, 0, 0);
}

void arc(int x, int y, int st, int en, int r)              { _bgi_pie(x, y, st, en, r, 0); }
void pieslice(int x, int y, int st, int en, int r)         { _bgi_pie(x, y, st, en, r, 1); }
void sector(int x, int y, int st, int en, int xr, int yr)  { (void)yr; _bgi_pie(x, y, st, en, xr, 1); }

/* ── 填充与图像块（老程序的"灌色"与"精灵"）────────────────────
 *
 * 这四个在**平台侧是新做的**（`ui_flood_fill` / `ui_get_image` / `ui_put_image`，
 * 583–585）—— 场景是保留模式的，宿主要先光栅化一次才知道"这个像素是什么颜色"。
 *
 * ## `floodfill(x, y, border)` —— 注意参数不是"填充色"
 *
 * BGI 的签名里**没有填充色**：填什么色由 `setfillstyle` 决定，第三个参数是**边界色**
 * （"碰到它就停"）。所以这里要把当前填充色从 `_bgi_fill_col` 翻成 RGB 传下去。
 */
void floodfill(int x, int y, int border)
{
    ui_flood_fill(x, y, _bgi_rgb(_bgi_fill_col), _bgi_rgb(border));
}

/* `imagesize` 返回**这块图像要多少字节** —— 老程序拿它去 `malloc`：
 *
 *     area = imagesize(l, t, r, b);
 *     p    = malloc(area);
 *     getimage(l, t, r, b, p);
 *
 * ⚠ 那块内存**我们不用**（句柄在宿主侧保管，见 `ui_get_image`），但**必须给个像样的数**：
 *   返回 0 的话 `malloc(0)` 可能返回 NULL，后面的 `putimage` 就被程序自己跳过了。
 *   按 BGI 的算法给（4 字节头 + 每像素 2 字节的位平面估算）。 */
int imagesize(int l, int t, int r, int b)
{
    int w = r - l + 1;
    int h = b - t + 1;
    if (w <= 0 || h <= 0) return 0;
    return 4 + w * h * 2;
}

/* 老程序的 `p`（那个 malloc 出来的缓冲区）我们**收下但不解引用** —— 真正的内容在
 * 宿主侧的句柄里。所以这里必须**按地址存一份映射**，好让 `putimage(p)` 找回来。
 *
 * ⚠ 为什么不能拿 `p` 当地址直接用：本平台下 `(int)p` 不是宿主的句柄，
 *   而句柄是**运行时才产生**的（每次 getimage 一个新的）。
 *   所以用一张**按指针索引**的小表把两者对上。表大小取常见的精灵数量（BGI 程序
 *   一般同时也就几个），满了就**覆盖最旧的一条**并且不再增长 —— 老程序里的
 *   `p` 是 malloc 出来的、地址稳定，覆盖最旧不影响正在用的那些。 */
#define BGI_IMG_SLOTS 16
static void *_bgi_img_ptr[BGI_IMG_SLOTS];
static int   _bgi_img_h[BGI_IMG_SLOTS];
static int   _bgi_img_next = 0;

void getimage(int l, int t, int r, int b, void *p)
{
    int h = ui_get_image(l, t, r - l + 1, b - t + 1);
    if (h <= 0 || p == 0) return;

    /* 先看这个指针是不是已经登记过（同一块缓冲区反复 getimage 很常见）*/
    int i;
    for (i = 0; i < BGI_IMG_SLOTS; i++) {
        if (_bgi_img_ptr[i] == p) { _bgi_img_h[i] = h; return; }
    }
    i = _bgi_img_next;
    _bgi_img_next = (i + 1) % BGI_IMG_SLOTS;
    _bgi_img_ptr[i] = p;
    _bgi_img_h[i] = h;
}

/* `op`：BGI 的 COPY_PUT / XOR_PUT（见上面的 #define）。 */
void putimage(int l, int t, void *p, int op)
{
    int i;
    int handle = 0;
    for (i = 0; i < BGI_IMG_SLOTS; i++) {
        if (_bgi_img_ptr[i] == p && _bgi_img_h[i] != 0) { handle = _bgi_img_h[i]; break; }
    }
    /* 没登记过（程序自己编了个 p 就调 putimage）⇒ 什么都不做。
       ⚠ **不要拿 p 当句柄碰运气** —— 那会贴出一块谁也说不清的像素。 */
    if (handle == 0) return;

    ui_put_image(l, t, handle, op == XOR_PUT ? 1 : 0);
}

/* ── 文字 ──────────────────────────────────────────────────── */

int _bgi_anchor(void)
{
    if (_bgi_txt_just == CENTER_TEXT) return VML_ANCHOR_CENTER;
    if (_bgi_txt_just == RIGHT_TEXT)  return VML_ANCHOR_RIGHT;
    return VML_ANCHOR_LEFT;
}

void outtextxy(int x, int y, char *s)
{
    ui_text(x, y, s, _bgi_rgb(_bgi_fg), _bgi_txt_size, _bgi_anchor());
}

void outtext(char *s)
{
    outtextxy(_bgi_pen_x, _bgi_pen_y, s);
}

/* ── 文字量测（`textwidth` / `textheight`）────────────────────
 *
 * 老程序拿它做**居中排版**，这是标准写法：
 *
 *     outtextxy((getmaxx() - textwidth(s)) / 2, y, s);
 *
 * ⚠ **宽度不在这里算** —— 问宿主注册的 `text` JSON 函数（`ui_call_json`）。
 *   它内部用的是全仓唯一的宽度判据（`AnsiString.CharWidth`：半角 1 列 / 全角 2 列 /
 *   列宽 = 字号÷2）。在本文件里另写一张 CJK 区间表就是"同一规则两处实现"，
 *   而居中最怕的就是"差几像素"—— 屏幕上看就是"没居中"，却谁也说不清差在哪。
 *
 * ⚠ 代价：一次调用要过两趟 JSON。**别放进每帧的循环里**
 *   （BGI 程序本来也几乎只在排版时调它一次）。
 */

/* 从 JSON 里读一个整数键。**只服务于下面那一个小信封**，不做通用解析：
 * 回应形如 `{"ok":true,"result":{"w":40,"h":16}}`，找 `"键":` 再读十进制数。
 * 找不到回 -1（调用方据此回退），不是 0 —— 0 是**合法宽度**，分不开。 */
static int _bgi_json_int(char *json, char *key)
{
    int i, j;
    for (i = 0; json[i] != 0; i++) {
        if (json[i] != '"') continue;
        for (j = 0; key[j] != 0 && json[i + 1 + j] == key[j]; j++) { }
        if (key[j] != 0) continue;                 /* 键名没对上 */
        if (json[i + 1 + j] != '"' || json[i + 2 + j] != ':') continue;
        j = i + 3 + j;
        while (json[j] == ' ') j++;
        {
            int sign = 1, v = 0, seen = 0;
            if (json[j] == '-') { sign = -1; j++; }
            while (json[j] >= '0' && json[j] <= '9') {
                v = v * 10 + (json[j] - '0'); j++; seen = 1;
            }
            if (seen) return v * sign;
        }
    }
    return -1;
}

/* 问宿主："这段字在当前字号下占多少像素"。返回 1 成功、0 失败（w/h 退回估算）。 */
static int _bgi_measure(char *s, int *pw, int *ph)
{
    char args[256];
    char out[160];
    char fn[] = "text";      /* 不用字面量直传：`ui_call_json` 收的是 `char*`，
                                而本垫层会被 C++ 程序 `#include`（老 BGI 程序多是 .cpp），
                                字面量转 `char*` 在 C++ 里是弃用转换。 */
    int n = 0, i, len, v, k;
    char num[12];

    *pw = 0;
    *ph = _bgi_txt_size;

    /* 组装 `{"s":"…","size":N}`。只转义 `"` 与 `\` —— 老程序传的是字面量，
       不做完整 JSON 转义也够用；真出了问题（字面量带反斜杠），宿主会回 ok:false。 */
    args[n++] = '{'; args[n++] = '"'; args[n++] = 's'; args[n++] = '"'; args[n++] = ':';
    args[n++] = '"';
    for (i = 0; s[i] != 0 && n < 200; i++) {
        if (s[i] == '"' || s[i] == '\\') args[n++] = '\\';
        args[n++] = s[i];
    }
    args[n++] = '"'; args[n++] = ',';
    args[n++] = '"'; args[n++] = 's'; args[n++] = 'i'; args[n++] = 'z'; args[n++] = 'e';
    args[n++] = '"'; args[n++] = ':';
    /* 自己拼十进制，**不用 `itoa`**：库里那份是 `itoa(int, char*)`（注意参数序与
       stdlib 的相反），而且链接期有一条按后缀匹配的重定向专门坑它（见 CHANGELOG v0.96.208）。 */
    v = _bgi_txt_size > 0 ? _bgi_txt_size : 1;
    k = 0;
    while (v > 0 && k < 11) { num[k++] = (char)('0' + v % 10); v /= 10; }
    while (k > 0) args[n++] = num[--k];
    args[n++] = '}';
    args[n] = 0;

    len = ui_call_json(fn, args, out, 160);
    if (len <= 0) return 0;

    *pw = _bgi_json_int(out, "w");
    v   = _bgi_json_int(out, "h");
    if (*pw < 0) { *pw = 0; return 0; }
    *ph = v > 0 ? v : _bgi_txt_size;
    return 1;
}

int textwidth(char *s)
{
    int w, h;
    _bgi_measure(s, &w, &h);
    return w;
}

int textheight(char *s)
{
    int w, h;
    _bgi_measure(s, &w, &h);        /* BGI 的签名收字符串，但高度只与字号有关；
                                       仍然走同一个入口，免得两条路各算各的 */
    return h;
}

/* ── `getpixel`：读一个像素的**颜色索引** ─────────────────────
 *
 * ⚠ 它放在这里（文字量测旁边）而不是紧挨着 `putpixel`，是因为它要复用上面那套
 *   JSON 垫层（`ui_call_json` + `_bgi_json_int`）—— C 不认"后面才定义"。
 *
 * ⚠ **分工**：宿主回 **0xRRGGBB**（场景里存的就是 RGB），
 *   "RGB → 调色板索引"这一步在**这里**做，因为 `_bgi_pal` 的真源就在本文件。
 *   在宿主再抄一张索引表就是"同一规则两处实现"。
 *
 * ⚠ 每次调用宿主都要**光栅化一次**（保留模式的场景没有像素缓冲，与 `getimage` 同源）
 *   ⇒ 别放进每帧的密集循环。老程序用它一般是碰撞检测 / 判断某格是否已占。
 */
int getpixel(int x, int y)
{
    char args[64];
    char out[96];
    char fn[] = "pixel";
    int n = 0, i, len, v;

    /* 拼 `{"x":X,"y":Y}`（十进制自己拼，理由同 `_bgi_measure`：绕开 itoa 那条链） */
    args[n++] = '{'; args[n++] = '"'; args[n++] = 'x'; args[n++] = '"'; args[n++] = ':';
    for (i = 0; i < 2; i++) {
        int val = (i == 0) ? x : y;
        char num[12];
        int k = 0, neg = 0;
        if (val < 0) { neg = 1; val = -val; }
        if (val == 0) num[k++] = '0';
        while (val > 0 && k < 11) { num[k++] = (char)('0' + val % 10); val /= 10; }
        if (neg) args[n++] = '-';
        while (k > 0) args[n++] = num[--k];
        if (i == 0) { args[n++] = ','; args[n++] = '"'; args[n++] = 'y'; args[n++] = '"'; args[n++] = ':'; }
    }
    args[n++] = '}';
    args[n] = 0;

    len = ui_call_json(fn, args, out, 96);
    if (len <= 0) return 0;

    v = _bgi_json_int(out, "rgb");
    if (v < 0) return 0;
    v = v & 0xFFFFFF;

    for (i = 0; i < 16; i++) {
        if (_bgi_pal[i] == v) return i;
    }
    /* 不在 16 色里的（渐变、抗锯齿边缘等）退回黑 —— BGI 本来也只有 16 色，
       给个确定的值比给垃圾好；老程序拿它多是判"这一格是不是背景色"。 */
    return 0;
}

/* ── 刷新与等待 ────────────────────────────────────────────── */

/* BGI 是**立即出图**（每画一笔就上屏），本平台是**保留模式**（改完调 present 才上屏）。
 * 为了让老程序的行为对得上，这里在"一段画完"的几个常见位置自动 present；
 * 另外老程序结束前一般会 `getch()`，那里也 present 一次。 */
void _bgi_flush(void) { ui_present(); }

void delay(int ms)
{
    /* 老程序用 delay 做动画节拍；这里用 tick 轮询实现（不占宿主线程）*/
    int t0 = ui_tick();
    while (ui_tick() - t0 < ms) {
        /* ⚠ 判据是 `== 1`（**被关掉**），不是"非 0"：
           `ui_win_closed()` 现在是**三值**（0 开着 / 1 关掉了 / **2 从没开过窗口**，
           见 `VmlUi.WinClosed`）。写成"非 0 就 break"的话，**没开窗口的控制台程序**
           会让 `delay()` 立刻返回 —— 那正是 `25-dos-sleep-time` 压的东西。 */
        if (ui_win_closed() == 1) break;
    }
}

/* kbhit 是**非阻塞**查询 —— 必须用 `ui_poll_msg()`，
 * ⚠ 别顺手写成 `ui_wait_msg(0)`：那个 timeout 0 的语义是**无限等**（不是不阻塞），
 *   会把 kbhit 变成一个卡死的调用（本平台已经踩过一次，见 CHANGELOG）。 */
int kbhit(void) { return ui_poll_msg() > 0; }

/* ── 按键：`getch` ────────────────────────────────────────────
 *
 * ⚠⚠ **这个函数被定义了两次**（这里一份、`Lib/shared/src/conio.c` 一份），
 *   而这不是偷懒 —— 是**工具链逼出来的**，两条路各有各的用处：
 *
 *   · **C 程序**走 conio 那份：`Lib/c/conio.h` 里有 `#param lib("conio")`，
 *     前端会发 `.linked "conio.vml"` ⇒ `call getch` 解析到 `lib_conio_getch`（实测 ✓）。
 *   · **C++ 程序压根链不到 conio**：C++ 前端**不处理** `c/conio.h` 的 `#param`
 *     （实测：一个 .cpp 只 include `<conio.h>` 时，产物里的 `.linked` 列表
 *      没有任何 conio），而 `Lib/cpp/` 下**没有 conio.h**、`Lib/cpp/builtin.vml`
 *      里也没有 conio ⇒ `getch` 对 C++ 来说就是"未定义的函数"。
 *     而**老 BGI 程序几乎都是 .cpp**（Turbo C++ 时代）⇒ 这里这份是它们的唯一来源。
 *
 * ⚠ 两份同名实现同时存在时，**链接器会把 `call getch` 重定向到库里那份**
 *   （即已知红 `28-name-shadowed-by-lib` 那个"库函数静默劫持"）—— 所以
 *   **两份都必须是"对窗口友好"的实现**，否则用户看到的是"按了没反应"。
 *   conio 那份（`Lib/shared/src/conio.c`）已同步改成同一套语义。
 *
 * 真正的根治办法（还没做）：让 C++ 前端也认 `c/conio.h` 的 `#param`，
 * 或者往 `Lib/cpp/builtin.vml` 里补 `.linked "conio.vml"` —— 那之后这里就能删掉。
 *
 * ## 语义
 *
 * DOS 的 `getch()` 约定：**普通键返回 ASCII；扩展键先返回 0、下一次返回扫描码**：
 *
 *     ch = getch();
 *     if (ch == 0) { ch = getch(); switch (ch) { case 0x4B: …  左 … } }
 *
 * 而本平台的按键是 **Win32 虚拟键**（方向键 37–40、F1–F12 112–123），
 * 不翻译的话老程序里的方向键**永远按不动**（它们等的是 0 + 扫描码）。
 * 普通键不用翻译：A–Z / 0–9 的虚拟键码**就等于** ASCII。 */

static int _bgi_pending_scan = 0;   /* 非 0 = 下次 getch 立刻吐出的扫描码（两段式的后半段） */

static int _bgi_scan_code(int vk)
{
    if (vk == 0x25) return 0x4B;              /* ← */
    if (vk == 0x26) return 0x48;              /* ↑ */
    if (vk == 0x27) return 0x4D;              /* → */
    if (vk == 0x28) return 0x50;              /* ↓ */
    if (vk == 0x24) return 0x47;              /* Home */
    if (vk == 0x23) return 0x4F;              /* End  */
    if (vk == 0x21) return 0x49;              /* PgUp */
    if (vk == 0x22) return 0x51;              /* PgDn */
    if (vk == 0x2D) return 0x52;              /* Ins  */
    if (vk == 0x2E) return 0x53;              /* Del  */
    if (vk >= 0x70 && vk <= 0x79) return 0x3B + (vk - 0x70);   /* F1–F10 */
    if (vk == 0x7A) return 0x57;              /* F11 */
    if (vk == 0x7B) return 0x58;              /* F12 */
    return 0;
}

/* 等一个键。`ui_wait` 的 timeout 0 是**无限等**（不是不阻塞）。
 * 返回 0 表示窗口被关了 —— 老程序一般不看返回值，所以这里保证不返回负值。
 * ⚠ 只认 `KEYDOWN`：KeyUp / 触摸 / 尺寸变化一律跳过。 */
int getch(void)
{
    int msg[3];
    int vk;
    int sc;

    _bgi_flush();

    if (_bgi_pending_scan != 0)
    {
        sc = _bgi_pending_scan;
        _bgi_pending_scan = 0;
        return sc;
    }

    for (;;)
    {
        if (ui_wait(msg, 0) <= 0) return 0;
        if (msg[0] == VML_MSG_KEYDOWN) break;
    }

    vk = msg[1];
    sc = _bgi_scan_code(vk);
    if (sc != 0)
    {
        _bgi_pending_scan = sc;
        return 0;
    }
    return vk;
}

void getmouse(int *x, int *y, int *buttons)
{
    (void)x; (void)y;
    if (buttons) *buttons = 0;
}

#endif /* _GRAPHICS_H */
