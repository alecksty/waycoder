#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
生成手机端「UI 开发」说明页（`Resources/Raw/help/vml/ui.md`）。

## 为什么要生成

那份文档要覆盖 **60 个宿主接口**，每个都带用法与例子。而**签名的权威来源是
`Lib/c/waycoder_ui.h`** —— 手抄一遍必然漂（改了参数忘了改文档，用户照着写就是编译不过）。

所以：**签名从头文件抓**，这里只写"说明 + 例子"；并且**两个方向都对账** ——
头文件里有而这里没写的、这里写了而头文件里没有的，都直接报错退出。
加了新接口不补文档 = 脚本跑不过去，而不是"文档悄悄少了几个函数"。

## 用法

    python3 scripts/make-ui-help.py            # 生成
    python3 scripts/make-ui-help.py --check    # 只核对不写
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HEADER = os.path.join(ROOT, "third_party", "vml", "Lib", "c", "waycoder_ui.h")
OUTDIR = os.path.join(ROOT, "WayCoder.Maui", "Resources", "Raw", "help", "vml")
OUT = os.path.join(OUTDIR, "ui.md")

# name -> (分组, 说明, 示例)
E = {
    # ── 窗口 ──
    "ui_win_open": ("窗口",
        "开一个绘图窗口。**先问 `ui_scr_w/h()` 拿可用绘图区，再按它开** —— 写死尺寸在小屏上会溢出。",
        'int w = ui_scr_w(), h = ui_scr_h();\nui_win_open("我的游戏", w, h);'),
    "ui_win_open_ex": ("窗口",
        "同上，另加两个**开窗前就生效**的声明：转屏策略、要不要手柄区。",
        '/* 五子棋：只竖屏 + 不要手柄 */\nui_win_open_ex("五子棋", w, h, VML_WIN_PORTRAIT, VML_WIN_NO_GAMEPAD);\n'
        '/* 赛车：锁横屏 + 要手柄 */\nui_win_open_ex("赛车", w, h, VML_WIN_LANDSCAPE, VML_WIN_NEED_GAMEPAD);'),
    "ui_win_close": ("窗口", "关掉窗口（程序自己结束用）。", "ui_win_close();"),
    "ui_win_closed": ("窗口", "用户是不是已经关窗了（主循环的退出条件）。",
        "while (ui_win_closed() == 0) { … }"),
    "ui_scr_w": ("窗口", "可用绘图区的宽（**开窗前也能问**）。", "int w = ui_scr_w();"),
    "ui_scr_h": ("窗口", "可用绘图区的高。", "int h = ui_scr_h();"),
    "ui_orientation": ("窗口",
        "设备方向：`VML_ORIENT_PORTRAIT`(0) / `VML_ORIENT_LANDSCAPE`(1)。\n"
        "**别拿 `scr_w > scr_h` 去推** —— 那是可用绘图区（会随手柄收起而变化），方向是设备本身的属性。",
        "if (ui_orientation() == VML_ORIENT_LANDSCAPE) { /* 横排 */ }"),

    # ── 收消息 ──
    "ui_wait": ("收消息",
        "**等**一条消息，参数是 `int msg[4]`。返回消息类型（见下表）；`timeout=0` 表示**一直等**。",
        "int m[4];\nint t = ui_wait(m, 0);   /* 一直等 */"),
    "ui_wait_ex": ("收消息",
        "同上，第三个参数决定读完之后**留不留**这条消息（`VML_MSG_KEEP` / `VML_MSG_CONSUME`）。",
        "int m[4];\nui_wait_ex(m, 0, VML_MSG_KEEP);   /* 读完不弹掉 */"),
    "ui_wait_msg": ("收消息",
        "**不碰指针**的等消息版本：等到就返回类型、超时返回 `VML_MSG_NONE`。",
        "if (ui_wait_msg(500) == VML_MSG_TIMER) { /* 一拍到了 */ }"),
    "ui_poll": ("收消息",
        "**不等**：没有消息就返回 `VML_MSG_NONE`。连续动画用这个（配自己的节拍）；\n"
        "事件驱动的用 `ui_wait`（常态省电）。\n"
        "⚠ 只有 `int* msg` 一个参数 —— **没有 timeout**，别照 `ui_wait` 写。",
        "int m[4];\nwhile (ui_win_closed() == 0) {\n    if (ui_poll(m) == VML_MSG_TOUCHDOWN) { /* 处理 */ }\n    /* 画一帧 */\n    ui_present();\n}"),
    "ui_poll_ex": ("收消息",
        "`ui_poll` 的带「读完后留不留」版本。",
        "int m[4];\nui_poll_ex(m, VML_MSG_CONSUME);"),
    "ui_poll_msg": ("收消息",
        "**不碰指针**的取消息版本（拿不到数组指针的语言用）：没有就返回 `VML_MSG_NONE`，\n"
        "参数用 `ui_msg_a()` / `ui_msg_b()` 读。",
        "if (ui_poll_msg() == VML_MSG_TOUCHDOWN) { int x = ui_msg_a(); int y = ui_msg_b(); }"),
    "ui_msg_type": ("收消息", "当前消息的类型（省得把 `msg[0]` 记在脑子里）。",
        "if (ui_msg_type() == VML_MSG_TOUCHDOWN) { /* 处理 */ }"),
    "ui_msg_a": ("收消息", "当前消息的**第一个参数**（触摸的 x、按键的键码、定时器的 id…）。",
        "int x = ui_msg_a();"),
    "ui_msg_b": ("收消息", "当前消息的**第二个参数**（触摸的 y…）。", "int y = ui_msg_b();"),
    "ui_msg_count": ("收消息", "队列里还积着几条（想丢掉积压时可以看一眼）。",
        "if (ui_msg_count() > 8) ui_msg_clear();"),
    "ui_msg_clear": ("收消息", "清空消息队列（切场景 / 重开一局时用，免得把上一局的按键吃进来）。",
        "ui_msg_clear();"),

    # ── 定时器 ──
    "ui_timer_set": ("定时器与随机数",
        "起一个**重复**定时器，每 N 毫秒发一条 `VML_MSG_TIMER`（`msg[1]` 是你给的 id）。",
        "ui_timer_set(1, 100);   /* 每 100ms 一条，id=1 */"),
    "ui_timer_kill": ("定时器与随机数",
        "停掉一个定时器。\n⚠ 它是**重复**的 —— 靠「一定会收到 KeyUp」来停是不可靠的，"
        "自己要有刹车（换键即接管 / 按了没动两次就停 / 总拍数上限）。",
        "ui_timer_kill(1);"),
    "ui_tick": ("定时器与随机数", "开机以来的毫秒数（自己算帧间隔、做动画用）。",
        "int now = ui_tick();"),
    "ui_rand": ("定时器与随机数",
        "随机数：`ui_rand(n)` → **0..n-1**（n ≤ 0 时返回 1，不会崩）。",
        "int n = ui_rand(6);      /* 0..5 */\nint side = ui_rand(2);   /* 0 或 1 */"),
    "ui_clear": ("绘图", "整屏填一个色（每帧开头调）。颜色一律 `0xAARRGGBB`。",
        "ui_clear(0xFF101020);   /* 深蓝底 */"),
    "ui_pixel": ("绘图", "画一个点。", "ui_pixel(10, 20, 0xFFFFFFFF);"),
    "ui_line": ("绘图", "画线，`lw` 是线宽。", "ui_line(0, 0, 100, 100, 0xFFFF0000, 2);"),
    "ui_rect": ("绘图", "画矩形。`fill` 非 0 填充、`radius` 是圆角半径。",
        "ui_rect(20, 30, 120, 60, 0xFF3366FF, 1, 2, 8);   /* 圆角填充 */"),
    "ui_rect_grad": ("绘图", "带渐变的矩形（渐变先用 `ui_gradient` 定义）。",
        "ui_rect_grad(0, 0, 200, 100, g, 8);"),
    "ui_circle": ("绘图", "画圆，`fill` 非 0 填充。",
        "ui_circle(100, 100, 30, 0xFF00FF00, 1, 2);"),
    "ui_circle_grad": ("绘图",
        "渐变的圆（渐变先用 `ui_gradient` 起个名字）。",
        "ui_gradient(\"ball\", 1, 0xFFFFFFFF, 0xFF2E6FC4, 500, 500, 500, 0);\nui_circle_grad(cx, cy, r, \"ball\");"),
    "ui_ellipse": ("绘图", "画椭圆（`rx` / `ry` 两个半径）。",
        "ui_ellipse(100, 100, 50, 30, 0xFFFFFF00, 1, 2);"),
    # ── 刷子 / 样式 / 形状（574–576）：一个号 + 操作码，加形状不必再占号 ──
    "ui_brush_solid": ("绘图",
        "造一个**纯色刷子**。返回句柄（≥1；0 = 失败），交给 `ui_set_fill` / `ui_set_pen` 用。\n"
        "⚠ 其实**直接传颜色**也行（颜色 = 只有一个色标的刷子），这个函数是给"
        "「先造一批刷子、之后换着用」的场合。",
        "int b = ui_brush_solid(0xFF2A3346);\nui_set_fill(b);"),
    "ui_brush_linear": ("绘图",
        "造一个**线性渐变刷子**。几何是**千分之一**（0..1000），相对**形状自己的包围盒**："
        "`0,0,1000,0` = 从左到右。",
        'int b = ui_brush_linear(0xFFFF3020, 0xFF2050FF, 0, 0, 1000, 0);\nui_set_fill(b);'),
    "ui_brush_radial": ("绘图",
        "造一个**径向渐变刷子**（中心 → 四周）。`cx cy r` 同样千分之一，`500,500,500` = 居中。",
        'int b = ui_brush_radial(0xFFFFE060, 0xFF204020, 500, 500, 500);\nui_set_fill(b);'),
    "ui_brush_named": ("绘图",
        "按**名字**引用一个已经 `ui_gradient` 定义过的渐变 → 句柄。\n"
        "⚠ 它是**引用型**：自己不定义渐变，所以依赖程序当帧先调过 `ui_gradient`，"
        "而 `ui_clear` 会把那条定义清掉。",
        'ui_gradient("sky", 0, 0xFF2E6FC4, 0xFFBEE3F7, 0, 0, 0, 1000);\n'
        'int b = ui_brush_named("sky");\nui_set_fill(b);'),
    "ui_set_fill": ("绘图",
        "设置**填充刷子**。传刷子句柄或颜色都行；传 0 = 不填充（空心）。",
        "ui_set_fill(0xFF2A3346);        /* 直接给颜色 */\nui_draw_rect(20, 20, 120, 60, 8);"),
    "ui_set_pen": ("绘图",
        "设置**画笔**（描边）= 刷子 + 线宽 + 线帽 + 虚线 + 箭头。传 0 = 不描边。\n"
        "线帽用 `VML_CAP_BUTT/ROUND/SQUARE`，箭头用 `VML_ARROW_*`。\n"
        "⚠ 本批**画笔只支持纯色**（渐变描边还没落地，给渐变句柄会记一次警告并退回它的起始色）。",
        "ui_set_fill(0xFF2A3346);\n"
        "ui_set_pen(0xFFF2F6FA, 3, VML_CAP_ROUND, 0, VML_ARROW_NONE);\nui_draw_rect(20, 20, 120, 60, 8);"),
    "ui_set_text_brush": ("绘图",
        "设置**文字刷子**（配合 `ui_set_font` + `ui_draw_text`）。传 0 = 回到 `ui_set_font` 给的颜色。",
        "ui_set_text_brush(0xFFFFD700);\nui_set_font(24, VML_FONT_BOLD, 0, VML_ANCHOR_CENTER);\n"
        "ui_draw_text(100, 40, \"标题\");"),
    "ui_draw_rect": ("绘图", "用**当前刷子**画矩形（`radius > 0` 即圆角）。",
        "ui_set_fill(0xFF4ADE80);\nui_draw_rect(20, 20, 120, 60, 8);"),
    "ui_draw_circle": ("绘图", "用当前刷子画圆。", "ui_draw_circle(100, 100, 40);"),
    "ui_draw_ellipse": ("绘图", "用当前刷子画椭圆。", "ui_draw_ellipse(100, 100, 50, 30);"),
    "ui_draw_line": ("绘图", "用当前**画笔**画直线。", "ui_set_pen(0xFFE06C50, 3, 0, 0, 0);\nui_draw_line(10, 10, 120, 60);"),
    "ui_draw_poly": ("绘图",
        "用当前刷子画多边形（`close=1` 自动闭合）或折线（`close=0`）。"
        "`pts` 每两个 int 一个点，`count` 是**点数**。",
        "int tri[6];\ntri[0]=60; tri[1]=10; tri[2]=90; tri[3]=70; tri[4]=30; tri[5]=70;\n"
        "ui_set_fill(0xFF4ADE80);\nui_draw_poly(tri, 3, 1);"),
    "ui_draw_path": ("绘图", "用当前刷子画 SVG 路径（`M L C Q A Z`，大小写区分绝对/相对；多子路径按奇偶规则挖洞）。",
        'ui_set_fill(0xFF4ADE80);\nui_draw_path("M 20 80 L 60 20 L 100 80 Z");'),
    "ui_draw_text": ("绘图", "用**当前文字属性**（`ui_set_font`）画一行字。", 'ui_set_font(24, 0, 0xFFFFFFFF, VML_ANCHOR_CENTER);\nui_draw_text(100, 40, "你好");'),
    "ui_draw_star": ("绘图", "用当前刷子画星形：外半径 / 内半径 / 角数 / 旋转角(度)。", "ui_set_fill(0xFFFFD700);\nui_draw_star(100, 100, 50, 22, 5, 0);"),
    "ui_draw_regular": ("绘图", "用当前刷子画正多边形：半径 / 边数 / 旋转角(度)。", "ui_set_fill(0xFF4ADE80);\nui_draw_regular(100, 100, 50, 6, 0);"),
    "ui_draw_ring": ("绘图", "用当前刷子画圆环（外半径 / 内半径，中间的洞靠奇偶规则挖）。", "ui_set_fill(0xFF4A90D9);\nui_draw_ring(100, 100, 50, 30);"),
    "ui_draw_pie": ("绘图", "用当前刷子画扇形：半径 / 起始角 / 结束角（度）。", "ui_set_fill(0xFFE06C50);\nui_draw_pie(100, 100, 50, 0, 120);"),
    "ui_draw_heart": ("绘图", "用当前刷子画心形。", "ui_set_fill(0xFFFF4D6D);\nui_draw_heart(100, 100, 60);"),
    "ui_ellipse_grad": ("绘图",
        "渐变填充的椭圆。与 `ui_rect_grad` / `ui_circle_grad` 是一组（那批接口当时**漏了椭圆**）。",
        'ui_gradient("ball", 1, 0xFFFFFFFF, 0xFF2E6FC4, 500, 500, 500, 0);\n'
        'ui_ellipse_grad(cx, cy, rx, ry, "ball");'),
    "ui_polygon": ("绘图",
        "画多边形（自动闭合）。`pts` 是 int 数组、**每两个 int 一个点**；`count` 是**点数**。\n"
        "`fill` / `stroke` 都是**颜色**（不是开关），`width` 是描边线宽，`grad` 传渐变名或空串。\n"
        "⚠ **顶点必须是具名数组** —— `ui_polygon((int[]){…})` 复合字面量在这条前端上不支持、也不报错。",
        'int pts[] = {10,10, 110,10, 60,90};\nui_polygon(pts, 3, 0xFFFF8800, 0xFFFFFFFF, 2, "");'),
    "ui_polyline": ("绘图",
        "折线（不闭合）。参数含义同 `ui_polygon`（`stroke` 是颜色、`width` 是线宽）。",
        'int pts[] = {10,10, 50,60, 90,20};\nui_polyline(pts, 3, 0xFFFFFFFF, 2, "");'),
    "ui_path": ("绘图",
        "按 **SVG 路径语法**画（`M L H V C S Q T A Z`，大小写区分绝对/相对）。\n"
        "`stroke` 描边色（0 = 不描边）、`width` 线宽、`fill` 填充色（0 = 不填充）、\n"
        "`grad` 传渐变名（给了就用它填充）、`cap` 0平/1圆/2方、`dash` 0/1 虚线。",
        'ui_path("M10,50 Q60,0 110,50 T210,50", 0xFFFF00FF, 3, 0, "", 1, 0);'),
    "ui_gradient": ("绘图",
        "定义一个渐变刷子并**起个名字**（字符串 id）；之后 `ui_rect_grad` / `ui_circle_grad` / `ui_path` 按名字引用它。\n"
        "几何是**归一化 0..1000 的整数**（千分之一）：线性给 x1,y1,x2,y2；径向给 cx,cy,r（第 4 个忽略）。\n"
        "⚠ 传像素会让渐变塌成纯色。",
        'ui_gradient("sky", 0, 0xFF2E6FC4, 0xFFBEE3F7, 0, 0, 0, 1000);\nui_rect_grad(0, 0, w, h, "sky", 0);'),
    "ui_image": ("绘图", "在指定位置画一张图（PNG / JPG / BMP），`w` / `h` 传 0 按原尺寸。",
        'ui_image(20, 20, "~/pics/logo.png", 0, 0);'),
    "ui_icon": ("绘图", "画一个内置图标（按名字取，省得自己画）。",
        'ui_icon(10, 10, "star", 24, 0xFFFFD700);'),
    "ui_present": ("绘图",
        "**这一帧画完了**。整个循环里最关键的一句 —— 不调它屏幕不更新。\n"
        "（宿主也是按它判断「可以出图了」，所以要放在每帧末尾、全部图元画完之后。）",
        'ui_clear(0xFF000000);\nui_text(10, 10, "一帧", 0xFFFFFFFF, 16, 0);\nui_present();'),

    # ── 文字 ──
    "ui_text": ("文字",
        "在 (x,y) 写一行字。`size` 是字号；`anchor` 决定 (x,y) 指文字的哪一边"
        "（`VML_ANCHOR_LEFT` / `CENTER` / `RIGHT`）。",
        'ui_text(180, 40, "得分: 120", 0xFFFFFFFF, 20, VML_ANCHOR_CENTER);'),
    "ui_text_styled": ("文字", "同上，另加样式（粗体 / 斜体 / 下划线）。",
        'ui_text_styled(10, 10, "标题", 0xFFFFFFFF, 22, VML_ANCHOR_LEFT, VML_FONT_BOLD);'),
    "ui_set_font": ("文字",
        "设一次字体，后面所有 `ui_text_cur` 都用它（省得每次重复传四个参数）。",
        'ui_set_font(18, VML_FONT_BOLD, 0xFFFFFFFF, VML_ANCHOR_CENTER);\nui_text_cur(180, 40, "按方向键退出");'),
    "ui_text_cur": ("文字", "用 `ui_set_font` 设好的字体写字。",
        'ui_text_cur(180, 300, "游戏结束");'),

    # ── 方块贴图 ──
    "ui_piece_init": ("方块贴图",
        "初始化棋子贴图表（无参版本；具体形态见 `Lib/shared/src/vmlui.c`）。",
        "ui_piece_init();"),
    "ui_piece_cell": ("方块贴图",
        "取某个棋子的某一格：`pid` 棋子号、`rot` 旋转、`which` 第几格。",
        "ui_piece_cell(0, 0, 3);   /* 0 号棋子、不旋转、第 3 格 */"),
    "ui_gclear": ("整数网格",
        "清空网格。棋盘 / 地图这种二维状态用它 —— **比语言自带的数组可靠**"
        "（有的前端数组写入读不回来，见「22 种语言」里各语言的坑）。",
        "ui_gclear();"),
    "ui_gset": ("整数网格", "写一格，`i` 是**一维下标**（`row * 宽 + col`）。",
        "ui_gset(y * 10 + x, 1);   /* 10 列的棋盘，(x,y) 落子 */"),
    "ui_gget": ("整数网格", "读一格，没写过返回 0。", "int v = ui_gget(y * 10 + x);"),

    # ── 对话框 ──
    "ui_dlg_msg": ("对话框",
        "弹一个提示框（`style` 传 0 即可）。**会阻塞到用户点掉** —— 游戏结束时报个结果正好。",
        'ui_dlg_msg("游戏结束", "得分 120", 0);'),
    "ui_dlg_select": ("对话框",
        "单选对话框：`opts` 是**选项串**、`n` 是选项个数、`def` 是默认选中项。返回选中下标（-1 = 取消）。",
        'int r = ui_dlg_select("游戏结束", "再来一局？", opts, 2, 0);'),
    "ui_dlg_multi": ("对话框",
        "多选对话框：`opts` 选项串、`n` 选项个数。返回选中的个数。",
        'int n = ui_dlg_multi("设置", "开哪些？", opts, 3);'),
    "ui_dlg_input": ("对话框",
        "要一行文字输入。结果写进你给的缓冲区。",
        'char buf[64];\nui_dlg_input("改名", "新名字：", buf, 64);'),
    "ui_beep": ("音效与触感",
        "**现场合成**一个音（不用带音频文件）：`freq` 赫兹、`ms` 毫秒。\n"
        "⚠ **单通道** —— 一次只响一个，连着发的只有最后一个听得见。"
        "所以用**音高**表达好坏：消一行 880Hz、四行 1568Hz，赢了盖过一切。",
        "ui_beep(880, 80);      /* 消一行 */\nui_beep(1568, 160);    /* 消四行，音更高 */"),
    "ui_vibrate": ("音效与触感",
        "震动：`ms` 毫秒，`strength` 强度。",
        "ui_vibrate(120, 0);"),
    "ui_keep_on": ("音效与触感", "屏幕常亮开关（玩游戏的都该开）。", "ui_keep_on(1);   /* 1 开 0 关 */"),

    # ── 本地存档 ──
    "ui_store_set": ("本地存档",
        "存一个值。**值也是字符串** —— 存数字要先自己转成字符串（这里没有 sprintf 可用）。\n"
        "键会自动加前缀，不会和 App 自己的设置打架。",
        'ui_store_set("high", "1200");'),
    "ui_store_get": ("本地存档",
        "读一个值：**写进你给的缓冲区**、返回长度（没有这条键返回 -1）。\n"
        "⚠ 它不是「返回那个整数」—— 字符串读回来，要数字自己转。",
        'char buf[16];\nif (ui_store_get("high", buf, 16) > 0) best = atoi(buf);'),
    "ui_call_json": ("全能接口",
        "**两个字符串进、一个 JSON 字符串出** —— 查设备信息、调宿主的杂项能力都走它，\n"
        "不必为每个小功能占一个 syscall 号。结果写进你给的缓冲区。\n"
        "⚠ **性能敏感的调用别走这里**（一次要过两趟 JSON + 一次内存拷贝）：绘图、输入仍旧走专用接口。",
        'char buf[512];\nui_call_json("sysinfo", "", buf, 512);\nputs(buf);   /* {"ok":true,"result":{…}} */'),
    "ui_call_json_s": ("全能接口",
        "同上，但**不用给缓冲区**（适合拿不到指针的语言），配 `_len` / `_at` 读结果。",
        'int n = ui_call_json_s("version", "");\nfor (int i = 0; i < n; i++) putchar(ui_call_json_at(i));'),
    "ui_call_json_len": ("全能接口", "上一次 `ui_call_json_s` 的结果有多长。",
        "int n = ui_call_json_len();"),
    "ui_call_json_at": ("全能接口", "取上一次结果的第 i 个字节。",
        "char c = (char)ui_call_json_at(i);"),
    "ui_call_json_print": ("全能接口",
        "把**上一次** `ui_call_json_s` 的结果整份打到 stdout（末尾补换行）—— 调试最省事，\n"
        "各语言的自测也都靠它把结果原样打出来。⚠ 无参：打印的是上一次的结果，不是带参数再调一次。",
        "ui_call_json_s(\"version\", \"\");\nui_call_json_print();"),
}

# 分组 → (子页 slug, 子页里的一句话标题)。**一页放 60 个接口太长**，按类别拆开，
# `vml/ui` 只当索引（这正是"层级用链接表达"的用法：加一类 = 加一个文件）。
GROUPS = [
    ("窗口", "window", "开窗、问尺寸、关窗、屏幕方向"),
    ("收消息", "messages", "触摸 / 按键 / 定时器消息怎么收"),
    ("定时器与随机数", "timer", "重复定时器、取时间、随机数"),
    ("绘图", "draw", "点线面、多边形、路径、渐变、贴图"),
    ("文字", "text", "写字、字体、锚点"),
    ("方块贴图", "piece", "把一张图切成小格反复贴"),
    ("整数网格", "grid", "棋盘 / 地图这种二维状态"),
    ("对话框", "dialog", "提示、单选、多选、输入"),
    ("音效与触感", "feel", "合成音、震动、屏幕常亮"),
    ("本地存档", "store", "存最高分这类小数据"),
    ("全能接口", "json", "两个字符串进、一个 JSON 出"),
]


def signatures():
    """从头文件抓 `ui_*` 的签名（参数表）——**权威来源**，文档里的签名直接用它。"""
    src = open(HEADER, encoding="utf-8").read()
    out = {}
    for m in re.finditer(r'^\s*(?:int|void)\s+(ui_[a-z_]+)\s*\(([^;]*)\)\s*;', src, re.M):
        out[m.group(1)] = re.sub(r'\s+', ' ', m.group(2)).strip()
    return out



def _count_args(call_text: str, name: str):
    """数例子里的实参个数。**先抠掉字符串字面量** —— 里面可能有逗号。"""
    src = re.sub(r'"[^"]*"', '""', call_text)
    i = src.find(name + "(")
    if i < 0:
        return None
    k = i + len(name) + 1
    depth, args, seen = 1, 0, False
    while k < len(src) and depth > 0:
        c = src[k]
        if c in "([{":
            depth += 1
        elif c in ")]}":
            depth -= 1
        elif c == "," and depth == 1:
            args += 1
        elif depth == 1 and not c.isspace():
            seen = True
        k += 1
    return args + 1 if seen else 0


def check_arity(sig: dict, table: dict):
    """
    例子里的调用**参数个数必须与头文件签名一致**。

    这道闸是补上的：原先只查了"每个函数都有条目"，**没查描述对不对** ——
    于是 `ui_polygon` 写成 7 个实参（真签名 6 个）、`ui_gradient` 写成有返回值，
    全都顺利发版了。参数个数是**机器能验的那一半**；措辞对不对仍要靠人读头文件，
    但至少"照着例子抄会编译不过"这类错，现在跑一次生成器就红。
    """
    bad = []
    for name, (_, _, ex) in table.items():
        got = _count_args(ex, name)
        if got is None:
            bad.append(f"{name}: 例子里没找到调用")
            continue
        params = sig[name].strip()
        want = 0 if params in ("", "void") else params.count(",") + 1
        if got != want:
            bad.append(f"{name}: 例子给了 {got} 个实参，签名是 {want} 个（{params}）")
    if bad:
        raise SystemExit("✘ 例子与头文件签名对不上：\n  " + "\n  ".join(bad))

def build() -> str:
    sig = signatures()

    check_arity(sig, E)

    missing = sorted(set(sig) - set(E))
    extra = sorted(set(E) - set(sig))
    if missing or extra:
        raise SystemExit(
            "✘ 头文件与说明表对不上：\n"
            f"  头文件里有、说明表里没有: {missing}\n"
            f"  说明表里有、头文件里没有: {extra}\n"
            "  （加了新接口就必须在这里补一条用法说明，文档不会悄悄变少）")

    parts = ["""# UI 开发

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
"""]

    for g, slug, blurb in GROUPS:
        rows = []
        for name in sorted(k for k, v in E.items() if v[0] == g):
            _, desc, _ = E[name]
            first = desc.split("\n")[0].replace("**", "")
            rows.append(f"| [{name}](help:vml/ui/{slug}) | {first} |")
        ex = next(E[n][2] for n in sorted(k for k, v in E.items() if v[0] == g))
        first_name = sorted(k for k, v in E.items() if v[0] == g)[0]
        parts.append(f"""
## {g}

{blurb} —— [`help:vml/ui/{slug}`](help:vml/ui/{slug})

| 接口 | 一句话 |
|---|---|
""" + "\n".join(rows) + f"""

```c
/* 例：{first_name} */
{ex}
```
""")

    return "".join(parts)


def build_group(group: str, slug: str, blurb: str) -> str:
    """一个分类一页。"""
    sig = signatures()
    head = f"""# {group}

{blurb}。

> 完整清单见「[UI 开发](help:vml/ui)」。下面每个接口都带一句用法和一个例子；
> 签名取自 `Lib/c/waycoder_ui.h`（权威来源）。

"""
    out = [head]
    for name in sorted(k for k, v in E.items() if v[0] == group):
        _, desc, ex = E[name]
        out.append(f"### `{name}({sig[name]})`\n")
        out.append(desc + "\n")
        out.append("```c\n" + ex + "\n```\n")
    return "".join(out)


def _unused_tail():
    parts = []
    parts.append("""
## 消息类型

`ui_wait` / `ui_poll` 的返回值；参数从 `msg[1]` / `msg[2]` 取（也可以 `ui_msg_a()` / `ui_msg_b()`）。

| 值 | 名字 | `msg[1]` / `msg[2]` |
|---|---|---|
| 0 | `VML_MSG_NONE` | 没消息 |
| 1 / 2 | `VML_MSG_KEYDOWN` / `KEYUP` | 键码（Win32 虚拟键：方向键 37-40、回车 13、空格 32、Esc 27） |
| 3 / 4 / 5 | `VML_MSG_MOUSEMOVE` / `MOUSEDOWN` / `MOUSEUP` | x, y |
| 6 / 7 / 8 | `VML_MSG_TOUCHDOWN` / `TOUCHMOVE` / `TOUCHUP` | x, y（**手机上的主要输入**） |
| 9 | `VML_MSG_TIMER` | 定时器 id（`ui_timer_set` 给的） |
| 10 | `VML_MSG_WINDOWCLOSE` | 用户关窗了 |
| 11 | `VML_MSG_WINDOWRESIZE` | 新宽, 新高 |
| 12 | `VML_MSG_WINDOWORIENT` | 新方向（宿主**先发方向、后发尺寸**） |

⚠ **转屏要声明**：只有开窗时写了 `VML_WIN_ROTATABLE` 才会收到 11 / 12，
而且**只有这一档会换坐标系**（换成新的可用绘图区）—— 收到后要按新尺寸重排版。
老接口（`ui_win_open`）跟随旋转但**坐标系不动**，所以老程序不会被转屏打乱。

## 常见坑

- **颜色一律 `0xAARRGGBB`**（alpha 在最前）—— 写成 `0xRRGGBBAA` 会把透明度当成别的颜色
- **每帧末尾必须 `ui_present()`**，否则屏幕不更新
- **`ui_wait(msg, 0)` 是「一直等」**，不是「不阻塞」；要轮询用 `ui_poll`
- **音效是单通道的**：一次事件只发一个音，用音高表达好坏
- **棋盘这类二维状态用 `ui_gset` / `ui_gget`**（整数网格），比语言自带的数组可靠
- 消息积压时 `ui_msg_clear()` 清一下，免得新一局吃到上一局的按键
- 完整可跑的例子在 `examples/` 下（`vml run examples/c/gomoku.c` 之类），
  每门语言一个目录
""" + "\n")
    return "".join(parts)


def main():
    files = [(OUT, build())]
    for g, slug, blurb in GROUPS:
        files.append((os.path.join(OUTDIR, "ui", slug + ".md"), build_group(g, slug, blurb)))

    if "--check" in sys.argv:
        bad = [f for f, t in files
               if (open(f, encoding="utf-8").read() if os.path.exists(f) else None) != t]
        print("✔ 一致" if not bad else "✘ 与生成结果不一致：" + "、".join(os.path.basename(b) for b in bad))
        return

    os.makedirs(os.path.join(OUTDIR, "ui"), exist_ok=True)
    for f, t in files:
        open(f, "w", encoding="utf-8", newline="").write(t)
    print(f"✔ 生成 {len(files)} 页（{len(signatures())} 个接口）→ {OUTDIR}/ui*/")


if __name__ == "__main__":
    main()
