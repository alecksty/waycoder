/*
 * 图元体检 —— 把 C 绘图接口能画的每一样各画一次，一屏看完。
 * Primitive health check — draw each thing the C drawing API can draw once, all on one screen.
 *
 * ## 为什么需要这么个程序
 * ## Why such a program is needed
 *
 * 「绘图接口对不对」有两层，**构建、自测、桌面验证全绿也证明不了第二层**：
 * "Is the drawing API correct" has two layers, and **a green build, green self-tests and green desktop verification still prove nothing about the second**:
 *   ① DSL 文档 → 落笔指令的映射（自测覆盖，记录型落笔面）；
 *   1) the mapping from the DSL document to draw commands (covered by self-tests with a recording draw surface);
 *   ② 落笔指令 → 平台上真正画出来的像素（只有真机看一眼才算数）。
 *   2) the draw commands to the pixels actually painted on the platform (only looking on a real device counts).
 * v0.96.182 的两处缺陷就全在 ② 上：`polygon`/`polyline` 整格空白（宿主把「颜色」当成了
 * Both defects in v0.96.182 were entirely in layer 2: `polygon`/`polyline` came out blank (the host treated "color" as
 * 「填充开关」和「线宽」）、线性渐变渲染成纯红（平台要的是「相对刷子矩形」的归一化，
 * a "fill switch" and a "line width"), and the linear gradient rendered solid red (the platform wants normalization **relative to the brush rectangle**,
 * 我们给的是「相对整幅场景」）—— 而 DSL、几何、解析当时全是对的。
 * while we were handing it one **relative to the whole scene**) — yet the DSL, the geometry and the parsing were all correct at the time.
 *
 * 所以这个程序的用法是**肉眼看**，不是断言：
 * So this program is meant to be **read with your eyes**, not asserted on:
 *
 *   rect　方角矩形　　roundrect　圆角矩形（= rect 的 radius 参数）
 *   rect   square-corner rectangle    roundrect  rounded rectangle (= the radius parameter of rect)
 *   circle 正圆　　　　ellipse　  扁圆（不是正圆）
 *   circle a true circle              ellipse    an oval (not a true circle)
 *   line　 斜直线　　　polygon　  实心三角（**必须填满**）
 *   line   a slanted line             polygon    a solid triangle (**must be filled solid**)
 *   polyline 折线（**必须开口**，不能连回起点）
 *   polyline a polyline (**must stay open**, it must not close back to the start)
 *   path 挖洞（外框填 + 内框再填 ⇒ 奇偶规则，**正中间必须是空的**）
 *   path a hole (outer box filled plus inner box filled => even-odd rule, **the very middle must be empty**)
 *   curve　贝塞尔（**必须是弧线，不是直线**）
 *   curve  a Bezier (**must be a curve, not a straight line**)
 *   text　 三种锚点（居中/左/右 三行的**左端不在同一列**才对）
 *   text   three anchors (the centered/left/right lines must **not start at the same column**)
 *   grad 线性　**左红右蓝**（渲染成纯色 = 渐变没生效）
 *   grad linear **red on the left, blue on the right** (a solid color means the gradient did not take effect)
 *   grad 径向　圆心亮、往外暗
 *   grad radial bright at the center, darker outward
 *
 * 手机上直接跑：`vml run examples/draw_prims.c`
 * Run it straight on a phone: `vml run examples/draw_prims.c`
 * ⚠ C 前端 + 汇编 + 链接在手机上要一分多钟，别当成卡死。
 * ⚠ The C frontend plus assembler plus linker takes over a minute on a phone; do not mistake it for a hang.
 *
 * ## 覆盖范围 = C 包装真能画到的那些
 * ## Coverage = exactly what the C wrappers can really draw
 *
 * 星形 / 扇形 / 心形 / 环**没有 C 包装**（绘图 DSL 里有，VML 程序够不到）——
 * stars / pies / hearts / rings **have no C wrapper** (the drawing DSL has them, but VML programs cannot reach them) —
 * 它们不在这里，只由桌面的映射自测覆盖。
 * they are not here and are covered only by the desktop mapping self-tests.
 *
 * ## ⚠ 顶点数组必须写成具名局部数组，不能用复合字面量
 * ## ⚠ Vertex arrays must be written as named local arrays, not compound literals
 *
 * `ui_polygon((int[]){92,215,…}, …)` 这种 C99 复合字面量**本前端不支持，而且不报错**：
 * a C99 compound literal like `ui_polygon((int[]){92,215,…}, …)` is **unsupported by this frontend, and it reports no error**:
 * 它不会算出字面量的地址，把上一个寄存器的值（正好是"点数"）当成指针传下去 ⇒
 * it does not compute the address of the literal and passes the previous register value (exactly the "point count") down as a pointer =>
 * 宿主去地址 3 读坐标、越界就地停 ⇒ **多边形静默不画**。
 * the host reads coordinates at address 3, goes out of bounds and stops right there => **the polygon is silently not drawn**.
 * （`ArrayInitializer` 只挂在变量声明的初始化上，表达式位置没有这个节点。）
 * (`ArrayInitializer` is only attached to variable-declaration initializers; there is no such node in expression position.)
 * 真机 + 桌面汇编两处都验过，见 CHANGELOG v0.96.182。
 * Both the real device and the desktop assembly were verified, see CHANGELOG v0.96.182.
 */
#include <waycoder_ui.h>

int main(void)
{
    int msg[4];
    int i;
    int tri[6];                                    /* 顶点一律用具名数组，别用 (int[]){…} */
    /* vertices are always named arrays, never (int[]){…} */
    int zig[8];
    int lang;                                      /* 界面语言：开局查一次（ui_get_language 是 syscall，别每帧调） */
/* UI language: queried once at start (ui_get_language is a syscall, do not call it every frame) */

    lang = ui_get_language();
    ui_win_open(lang == 0 ? "图元体检" : "Primitive health check", 320, 460);
    ui_clear(0xFF101018);

    tri[0] = 92;  tri[1] = 215;  tri[2] = 148;      /* 实心三角 */
    /* solid triangle */
    tri[3] = 215; tri[4] = 120;  tri[5] = 172;
    zig[0] = 162; zig[1] = 212;  zig[2] = 182;      /* 折线（开口） */
    /* polyline (open) */
    zig[3] = 172; zig[4] = 202;  zig[5] = 212;
    zig[6] = 222; zig[7] = 182;

    for (i = 0; i < 12; i++)                       /* 4 列 × 3 行 的格子底 */
        /* the 4-column by 3-row cell background */
        ui_rect((i % 4) * 80 + 2, (i / 4) * 150 + 2, 76, 146, 0xFF1A1A24, 1, 0, 4);

    /* ── 第 0 行：四个基本形 ── */
    /* ── Row 0: four basic shapes ── */
    ui_rect(10, 20, 60, 40, 0xFFE06C50, 1, 0, 0);
    ui_text(40, 80, "rect", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    ui_rect(90, 20, 60, 40, 0xFF50C878, 1, 0, 14);          /* 圆角 = rect 的 radius */
    /* rounded corners = the radius parameter of rect */
    ui_text(120, 80, "roundrect", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    ui_circle(200, 42, 24, 0xFF4A90D9, 1, 0);
    ui_text(200, 80, "circle", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    ui_ellipse(280, 42, 30, 18, 0xFFD9B44A, 1, 0);
    ui_text(280, 80, "ellipse", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    /* ── 第 1 行：线 / 多边形 / 折线 / 路径挖洞 ── */
    /* ── Row 1: line / polygon / polyline / path hole ── */
    ui_line(12, 170, 70, 215, 0xFFE06C50, 3);
    ui_text(40, 230, "line", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    ui_polygon(tri, 3, 0xFF50C878, 0, 0, "");
    ui_text(120, 230, "polygon", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    ui_polyline(zig, 4, 0xFFD9B44A, 3, "");
    ui_text(200, 230, "polyline", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    /* 外框填 + 内框再填 ⇒ 奇偶规则应把中间挖空 */
    /* outer box filled plus inner box filled => the even-odd rule should punch out the middle */
    ui_path("M245 170 L312 170 L312 216 L245 216 Z M264 184 L293 184 L293 202 L264 202 Z",
            0xFF4A90D9, 2, 0xFF4A90D9, "", 1, 0);
    ui_text(280, 230, lang == 0 ? "path 挖洞" : "path hole", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    /* ── 第 2 行：曲线 / 文字 / 两种渐变 ── */
    /* ── Row 2: curve / text / two kinds of gradient ── */
    ui_path("M12 420 C 40 320, 60 320, 76 420", 0xFFE06C50, 3, 0, "", 1, 0);
    ui_text(40, 440, "curve", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    ui_text(120, 330, lang == 0 ? "中ABC" : "ABC", 0xFFE8E8F0, 16, VML_ANCHOR_CENTER);
    ui_text(120, 360, lang == 0 ? "左对齐" : "left", 0xFF9AA0B0, 12, VML_ANCHOR_LEFT);
    ui_text(120, 380, lang == 0 ? "右对齐" : "right", 0xFF9AA0B0, 12, VML_ANCHOR_RIGHT);
    ui_text(120, 440, "text", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    ui_gradient("lin", 0, 0xFFFF3020, 0xFF2050FF, 0, 0, 1000, 0);
    ui_rect_grad(165, 330, 70, 60, "lin", 0);
    ui_text(200, 440, lang == 0 ? "grad 线性" : "grad linear", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    ui_gradient("rad", 1, 0xFFFFE060, 0xFF204020, 500, 420, 500);
    ui_circle_grad(280, 360, 36, "rad");
    ui_text(280, 440, lang == 0 ? "grad 径向" : "grad radial", 0xFF9AA0B0, 12, VML_ANCHOR_CENTER);

    ui_present();

    while (!ui_win_closed())
        ui_wait(&msg, 0);

    ui_win_close();
    return 0;
}
