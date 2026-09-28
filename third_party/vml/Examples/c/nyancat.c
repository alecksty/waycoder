/*
 * Nyancat —— 移植自 klange/nyancat（原作者 Kevin Lange）
 * Nyancat — ported from klange/nyancat (original author Kevin Lange)
 *
 * 为什么拿它当**命令行彩色链路**的判据：
 * Why it serves as the test case for the **colorful CLI pipeline**:
 *   它是真实世界的彩色 TTY 程序 —— 一整屏都是 `\033[48;5;Nm` 这种 256 色背景转义，
 *   It is a real-world colorful TTY program — a whole screen of 256-color background escapes like `\033[48;5;Nm`,
 *   而且颜色**按字符查表**（帧数据是调色板索引网格，渲染时才替换成转义）。
 *   and the colors are looked up **per character** (the frame data is a grid of palette indices, replaced with escapes only when rendering).
 *   比手写的示例更能压住"转换 / 渲染"这两段。
 *   It stresses the "convert / render" stages harder than a hand-written sample would.
 *
 * ◆ 改了哪些（尽量少）
 * ◆ What was changed (as little as possible)
 *   ① 去掉 telnet 模式、termios 原始模式、signal 处理、命令行选项 —— 手机上都没有对应物
 *   ① telnet mode, termios raw mode, signal handling and command-line options removed — none of them exists on a phone
 *   ② 动画循环 `while(playing)` + `\033[H` 回到原点重画**改成打印固定几帧** ——
 *   ② the animation loop `while(playing)` + `\033[H` redraw from home **became printing a fixed few frames** —
 *      本平台会**吃掉光标定位序列**（`\033[H` 不生效），无限循环只会把帧一直往下堆；
 *      this platform **swallows cursor positioning sequences** (`\033[H` has no effect), so an endless loop only piles frames downward;
 *      而命令行页是"跑完给结果"，本来也不需要动画
 *      and the command-line page is "run it and hand back a result", which needs no animation anyway
 *   ③ `usleep(90000)` 去掉（没有动画就没有等待）
 *   ③ `usleep(90000)` removed (no animation, no waiting)
 *   ④ 帧数据只取前 3 帧（原文 12 帧 / 53KB，手机上编译时间敏感）
 *   ④ the frame data keeps only the first 3 frames (12 frames / 53KB originally, and compile time matters on a phone)
 *   **渲染循环本身一字未改**（连同它那个 `last` 同色优化），调色板照抄原版 256 色那一档。
 *   **The render loop itself is untouched** (including its `last` same-color optimization), and the palette is copied verbatim from the original 256-color branch.
 *
 * ◆ 许可：NCSA（BSD 式宽松，允许复制/修改/再分发，需保留版权声明）
 * ◆ License: NCSA (BSD-style permissive; copy/modify/redistribute allowed, keep the copyright notice)
 *   原始出处 http://github.com/klange/nyancat
 *   Original source: http://github.com/klange/nyancat
 *   Copyright (c) 2011 Kevin Lange.  All rights reserved.
 *   Permission is hereby granted, free of charge, to any person obtaining a copy of
 *   this software and associated documentation files (the "Software"), to deal with
 *   the Software without restriction, including without limitation the rights to use,
 *   copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 *   Software, and to permit persons to whom the Software is furnished to do so,
 *   subject to the following conditions: (1) Redistributions of source code must retain
 *   the above copyright notice, this list of conditions and the following disclaimers.
 *   (2) Redistributions in binary form must reproduce the above copyright notice, this
 *   list of conditions and the following disclaimers in the documentation and/or other
 *   materials provided with the distribution. (3) Neither the names of the Association
 *   for Computing Machinery, Kevin Lange, nor the names of its contributors may be used
 *   to endorse or promote products derived from this Software without specific prior
 *   written permission. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
 *
 * 跑法：命令行页输入  vml run examples/c/nyancat.c
 * How to run: type  vml run examples/c/nyancat.c  on the command-line page
 */

#include "nyancat_frames.h"

/* 调色板：字符 → ANSI 转义（`set_options` 里 256 色那一档，逐条照抄） */
/* Palette: char -> ANSI escape (the 256-color branch of `set_options`, copied item by item) */
char * nyancolor(char c)
{
    if (c == ',') return "\x1b[48;5;17m";   /* 蓝色背景 */
    /* Blue background. */
    if (c == '.') return "\x1b[48;5;15m";   /* 白色星星 */
    /* White stars. */
    if (c == '\'') return "\x1b[48;5;0m";   /* 黑色边框 */
    /* Black outline. */
    if (c == '@') return "\x1b[48;5;230m";  /* 焦糖色馅饼 */
    /* Caramel-colored pastry. */
    if (c == '$') return "\x1b[48;5;175m";  /* 粉色馅饼 */
    /* Pink pastry. */
    if (c == '-') return "\x1b[48;5;162m";  /* 红色馅饼 */
    /* Red pastry. */
    if (c == '>') return "\x1b[48;5;9m";    /* 红色彩虹 */
    /* Red rainbow. */
    if (c == '&') return "\x1b[48;5;202m";  /* 橙色彩虹 */
    /* Orange rainbow. */
    if (c == '+') return "\x1b[48;5;11m";   /* 黄色彩虹 */
    /* Yellow rainbow. */
    if (c == '#') return "\x1b[48;5;10m";   /* 绿色彩虹 */
    /* Green rainbow. */
    if (c == '=') return "\x1b[48;5;33m";   /* 浅蓝彩虹 */
    /* Light blue rainbow. */
    if (c == ';') return "\x1b[48;5;19m";   /* 深蓝彩虹 */
    /* Dark blue rainbow. */
    if (c == '*') return "\x1b[48;5;8m";    /* 灰色猫脸 */
    /* Grey cat face. */
    if (c == '%') return "\x1b[48;5;175m";  /* 粉色脸颊 */
    /* Pink cheek. */
    return "";
}

/* 渲染一帧 —— 循环结构与原版一致（含 `last` 同色不重发转义的优化） */
/* Render one frame — the loop structure matches the original (including the `last` optimization that skips re-sending an escape for the same color) */
void nyan_show(char ** fr)
{
    int y, x;
    char last = 0;
    printf("\x1b[H");                  /* 回到原点重画 —— 原版的动画就靠这一句 */
    /* Redraw from home — this one statement is what the original's animation rests on */
    for (y = 20; y < 43; y++) {
        for (x = 10; x < 50; x++) {
            if (fr[y][x] != last) {
                last = fr[y][x];
                printf("%s", nyancolor(fr[y][x]));
            }
            printf("  ");
        }
        printf("\n");
    }
    printf("\x1b[0m");
}

int main()
{
    int i;
    /* 原版是 `while (playing)` 无限循环 + `usleep(90000)`。这里跑**有限帧**：
       The original is an infinite `while (playing)` loop plus `usleep(90000)`; this version runs a **finite number of frames**:
       手机上"跑完给结果"，无限循环只能靠超时杀。帧数取 3 的倍数，看得到循环。
       On a phone it is "run it and hand back a result", so an infinite loop could only be killed by the timeout. The frame count is a multiple of 3, which makes the cycle visible. */
    for (i = 0; i < 12; i++) {
        if (i % 3 == 0) nyan_show(frame0);
        else if (i % 3 == 1) nyan_show(frame1);
        else nyan_show(frame2);
    }
    printf("\n");
    return 0;
}
