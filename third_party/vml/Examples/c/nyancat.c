/*
 * Nyancat —— 移植自 klange/nyancat（原作者 Kevin Lange）
 *
 * 为什么拿它当**命令行彩色链路**的判据：
 *   它是真实世界的彩色 TTY 程序 —— 一整屏都是 `\033[48;5;Nm` 这种 256 色背景转义，
 *   而且颜色**按字符查表**（帧数据是调色板索引网格，渲染时才替换成转义）。
 *   比手写的示例更能压住"转换 / 渲染"这两段。
 *
 * ◆ 改了哪些（尽量少）
 *   ① 去掉 telnet 模式、termios 原始模式、signal 处理、命令行选项 —— 手机上都没有对应物
 *   ② 动画循环 `while(playing)` + `\033[H` 回到原点重画**改成打印固定几帧** ——
 *      本平台会**吃掉光标定位序列**（`\033[H` 不生效），无限循环只会把帧一直往下堆；
 *      而命令行页是"跑完给结果"，本来也不需要动画
 *   ③ `usleep(90000)` 去掉（没有动画就没有等待）
 *   ④ 帧数据只取前 3 帧（原文 12 帧 / 53KB，手机上编译时间敏感）
 *   **渲染循环本身一字未改**（连同它那个 `last` 同色优化），调色板照抄原版 256 色那一档。
 *
 * ◆ 许可：NCSA（BSD 式宽松，允许复制/修改/再分发，需保留版权声明）
 *   原始出处 http://github.com/klange/nyancat
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
 */

#include "nyancat_frames.h"

/* 调色板：字符 → ANSI 转义（`set_options` 里 256 色那一档，逐条照抄） */
char * nyancolor(char c)
{
    if (c == ',') return "\x1b[48;5;17m";   /* 蓝色背景 */
    if (c == '.') return "\x1b[48;5;15m";   /* 白色星星 */
    if (c == '\'') return "\x1b[48;5;0m";   /* 黑色边框 */
    if (c == '@') return "\x1b[48;5;230m";  /* 焦糖色馅饼 */
    if (c == '$') return "\x1b[48;5;175m";  /* 粉色馅饼 */
    if (c == '-') return "\x1b[48;5;162m";  /* 红色馅饼 */
    if (c == '>') return "\x1b[48;5;9m";    /* 红色彩虹 */
    if (c == '&') return "\x1b[48;5;202m";  /* 橙色彩虹 */
    if (c == '+') return "\x1b[48;5;11m";   /* 黄色彩虹 */
    if (c == '#') return "\x1b[48;5;10m";   /* 绿色彩虹 */
    if (c == '=') return "\x1b[48;5;33m";   /* 浅蓝彩虹 */
    if (c == ';') return "\x1b[48;5;19m";   /* 深蓝彩虹 */
    if (c == '*') return "\x1b[48;5;8m";    /* 灰色猫脸 */
    if (c == '%') return "\x1b[48;5;175m";  /* 粉色脸颊 */
    return "";
}

/* 渲染一帧 —— 循环结构与原版一致（含 `last` 同色不重发转义的优化） */
void nyan_show(char ** fr)
{
    int y, x;
    char last = 0;
    printf("\x1b[H");                  /* 回到原点重画 —— 原版的动画就靠这一句 */
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
       手机上"跑完给结果"，无限循环只能靠超时杀。帧数取 3 的倍数，看得到循环。 */
    for (i = 0; i < 12; i++) {
        if (i % 3 == 0) nyan_show(frame0);
        else if (i % 3 == 1) nyan_show(frame1);
        else nyan_show(frame2);
    }
    printf("\n");
    return 0;
}
