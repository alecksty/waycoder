/* ncurses.h —— 就是 `curses.h`（同一个实现，换个名字）
 *
 * ## 为什么需要它单独存在
 *
 * 老程序里 `#include <curses.h>` 与 `#include <ncurses.h>` **几乎是平分秋色**的
 * （GitHub 上分别 43,264 / 40,448 个文件）。而这两者本来就是**同一套 API** ——
 * `ncurses` 是 `curses` 的一个实现，头文件名不同纯粹是历史（System V 的 `curses.h`
 * vs 后来的 `ncurses.h`）。
 *
 * 所以这里**不复制任何声明**，只是转发过去 —— 两份声明表必然漂移，
 * 那是本仓排第一的坑（同一规则两处实现）。
 *
 * ⚠ 顺带一条容易踩的：真 ncurses 在 `ncurses.h` 里会把 `bool`、`attr_t`、
 * `cchar_t`、`mmask_t` 这些一起带出来，而 `curses.h` 不带。老程序如果只用
 * "`initscr`/`move`/`addstr`/`refresh`/`getch`"这一层（绝大多数都是），转发就够了。
 */
#ifndef _NCURSES_H
#define _NCURSES_H

#include <curses.h>

#endif /* _NCURSES_H */
