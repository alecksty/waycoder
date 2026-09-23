# `Examples/basic/thirdparty/` 第三方 QBasic 源码来源与许可登记

本目录下的 `.bas` 文件**不是本仓库原创**，是从上游公开仓库取来的第三方 QBasic 源码，
**原样保留、一个字节都没有改动**。

> ⚠ 关于署名，**如实说**：这些上游文件**多数没有文件头版权声明**（抽查 `bouncing_ball.bas`、
> `dosbism.bas` 等都是直接从 `SCREEN`/`DECLARE` 开始）。所以署名与许可**不是**靠文件内自带，
> 而是靠本目录的 `LICENSES.txt`（上游许可全文，含 Copyright 行）**随文件一并分发** ——
> 这正是 MIT/Apache-2.0 要求的「随副本附带版权声明与许可」的满足方式。

登记格式：`文件名` → 原作者 → 许可 → 来源 URL → 依据的原话。

> ⚠ **本目录只收「能溯源到明确宽松许可，且文件是该作者本人作品」的源码。**
> 微软随 QBasic 发行的官方样例（GORILLA / NIBBLES / MONEY / DONKEY / REMLINE）、
> 《BASIC Computer Games》原版书清单、微软 QuickBASIC 手册示例等，
> **一律不进本目录**（已在 `docs/` 之外的工作目录 `.scratch/qbasic-games/` 存档）。
>
> ⚠ **本目录里有 4 个文件是 QB64 方言**（用 `_DISPLAY` / `_LOADIMAGE` / `$TouchMouse` 这类
> QB64 专有关键字），**当前 QBasic 前端编不过**。它们的许可是干净的，留在这里是**当兼容层测试素材**，
> 每个都在下面标注了「QB64 方言」。当前能跑的只有 **QBasic / QuickBASIC 结构化**那一批。

许可原文的完整副本另存在工作目录 `.scratch/qbasic-games/_licenses/`（不进仓库）。

---

## 逐文件登记

### 1. `qtrek.bas`（74,316 字节，3176 行）
- **原作者**：Johann Philipp Strathausen
- **许可**：MIT License
- **来源**：https://github.com/strathausen/qtrek.bas
- **依据原话**（`LICENSE` 第 1–2 行）：
  > `The MIT License (MIT)`
  > `Copyright (c) 2014 Johann Philipp Strathausen`
- **说明**：星际迷航主题太空射击游戏。`SCREEN 13`，用到 `CIRCLE/LINE/PAINT/DRAW/PSET/PLAY`。

### 2. `futureblocks_tetris.bas`（45,739 字节，1967 行）
- **原作者**：Michael Fogleman
- **许可**：MIT License
- **来源**：https://github.com/fogleman/FutureBlocks （注：上游文件名是 `tetris.bas`，此处用 `futureblocks_tetris.bas` 避免与本目录已有的 `tetris.bas` 冲突）
- **依据原话**（`LICENSE.md` 第 1 行）：
  > `Copyright (C) 2013 Michael Fogleman`
  > `Permission is hereby granted, free of charge, to any person obtaining a copy`
- **说明**：俄罗斯方块（Future Blocks）。`SCREEN 12`，`LINE/GET/PUT/PLAY`。

### 3–7. `w84d_desert_rally.bas`、`w84d_spaceship.bas`、`w84d_radar.bas`、`w84d_mouse_cursor.bas`、`w84d_app_template.bas`
- **原作者**：Krzysztof Jankowski
- **许可**：MIT License
- **来源**：https://github.com/w84death/qbasic
- **依据原话**（`LICENSE` 第 1–2 行）：
  > `MIT License`
  > `Copyright (c) 2017 Krzysztof Jankowski`
- **说明**：`w84d_radar.bas` 是纯 QBasic（`SCREEN 7` 雷达扫描）；
  `w84d_desert_rally.bas` / `w84d_spaceship.bas` / `w84d_mouse_cursor.bas` / `w84d_app_template.bas`
  是 **QB64 方言**（用 `_DISPLAY` / `_LOADIMAGE` / `_MOUSEX` 等）。

### 8–12. `ex_calc.bas`、`ex_message.bas`、`ex_numgame.bas`、`ex_print.bas`、`ex_rnddots.bas`
- **原作者**：Ercan Ersoy
- **许可**：MIT License（**文件头自己也写了一遍**）
- **来源**：https://github.com/ercanersoy/QBasic-Code-Examples
- **依据原话**（`LICENSE` 第 1 行 + 文件头第 2–3 行）：
  > `Copyright (C) 2020-2021 Ercan Ersoy (http://ercanersoy.net)`
  > `Permission is hereby granted, free of charge, to any person obtaining a copy of this software ...`
  >
  > 文件头：`' Copyright (C) 2020-2021 Ercan Ersoy` / `' This code licensed by MIT License.`
- **说明**：计算器 / 消息框 / 猜数字 / PRINT 演示 / 随机点。计算器与消息框会自己打印版权行。

### 13. `qbjc_guess.bas`（1,625 字节）
- **原作者**：jichu4n（qbjc 项目）
- **许可**：Apache License 2.0
- **来源**：https://github.com/jichu4n/qbjc （`playground/examples/guess.bas`）
- **依据原话**（`LICENSE` 第 1–3 行）：
  > `Apache License`
  > `Version 2.0, January 2004`
- **说明**：猜数字，qbjc playground 自带示例。
  ⚠ 同仓库的 `cal.bas` / `check.bas` / `strtonum.bas` **没有收进来** ——
  那三个文件头写着 `Example from "Microsoft QuickBASIC: Programming in BASIC"`（微软手册示例），
  版权不属于 qbjc 作者。

### 14. `maze.bas`（47,656 字节，1542 行）
- **原作者**：Michael Duerinckx
- **许可**：MIT License
- **来源**：https://github.com/michd/maze.bas （上游文件名 `MAZE.BAS`）
- **依据原话**（`LICENSE` 第 1–2 行）：
  > `MIT License`
  > `Copyright (c) 2021 Michael Duerinckx`
- **说明**：迷宫游戏 + 内置迷宫编辑器。`SCREEN 13`。

### 15. `solitaire.bas`（37,733 字节，1306 行）
- **原作者**：Elod P. Csirmaz
- **许可**：MIT License
- **来源**：https://github.com/csirmaz/BasicSolitaire （上游文件名 `solit.bas`）
- **依据原话**（`LICENSE` 第 1–2 行 + 文件头第 1 行）：
  > `The MIT License (MIT)`
  > `Copyright (c) 2014 Elod Csirmaz`
  >
  > 文件头：`'CLASSIC SOLITAIRE by Elod P Csirmaz 2004`
- **说明**：经典纸牌（Klondike）接龙。

### 16. `bouncing_ball.bas`（2,816 字节）
- **原作者**：Sebastian（GitHub 用户 `Sebastian-gthb`）
- **许可**：MIT License
- **来源**：https://github.com/Sebastian-gthb/bouncing_ball.bas （上游文件名 `BALL.BAS`）
- **依据原话**（`LICENSE` 第 1–2 行）：
  > `MIT License`
  > `Copyright (c) 2024 Sebastian`
- **说明**：弹跳球物理演示，`SCREEN 13`（也演示了 0/9）。

### 17. `turtle.bas`（1,454 字节）
- **原作者**：Bernard Igiri
- **许可**：MIT License
- **来源**：https://github.com/BernardIgiri/TurtleBas （上游文件名 `TURTLE.BAS`）
- **依据原话**（`LICENSE` 第 1–2 行）：
  > `MIT License`
  > `Copyright (c) 2021 Bernard Igiri`
- **说明**：海龟绘图，`SCREEN 8`。

### 18. `gwiezdna_bitwa.bas`（15,141 字节，899 行）
- **原作者**：Sylwester Wysocki `<sw143@wp.pl>`
- **许可**：Unlicense / public domain —— **文件头自己写了完整声明**（比仓库的 LICENSE 更直接）
- **来源**：https://github.com/dzik143/gwiezdna-bitwa （`src/GB.BAS`）
- **依据原话**（**文件头第 3–6 行，就在源码里**）：
  > `' = Author: Sylwester Wysocki <sw143@wp.pl>`
  > `' = This is free and unencumbered software released into the public domain.`
  > `' = Anyone is free to copy, modify, publish, use, compile, sell, or`
  > `' = binary, for any purpose, commercial or non-commercial, and by any`
- **说明**：「星际之战」2D 射击游戏（波兰语注释）。`SCREEN 0/13`。

### 19–25. `dosbism.bas`、`dosbismkey.bas`、`doscolortst.bas`、`dosgrdemo.bas`、`dosgrbism.bas`、`dosmathtbl.bas`、`dosrandom.bas`
- **原作者**：Farhan Ali Qureshi
- **许可**：MIT License
- **来源**：https://github.com/FarhanAliQureshi/dos_basic
- **依据原话**（`LICENSE` 第 1–2 行；另有文件内的自我署名）：
  > `MIT License`
  > `Copyright (c) Farhan Ali Qureshi. All rights reserved.`
  >
  > 文件内署名（`dosbism.bas` 第 29 行）：`PRINT "  Program by FARHAN ALI QURESHI ..."`
- **说明**：`SCREEN 0` 彩色文字界面演示 / 键盘输入 / 16 色色彩表 / 图形演示合集（`SCREEN 9`）/
  图形+文字混合（`SCREEN 0/12`）/ 乘法表 / 随机数演示。

### 26. `qbjs_paint.bas`（4,620 字节）— **QB64 方言**
- **原作者**：boxgaming（qbjs 项目）
- **许可**：MIT License
- **来源**：https://github.com/boxgaming/qbjs （`samples/apps/paint.bas`）
- **依据原话**（`LICENSE` 第 1–2 行）：
  > `MIT License`
  > `Copyright (c) 2022-2026 boxgaming`
- **说明**：画图程序。用 `$TouchMouse` / `_NewImage` / `_Width` ⇒ **QB64 方言，当前前端编不过**。
  ⚠ 同仓库的 `samples/games/trfbird.bas`（Flappy Bird）**没有收进来** ——
  文件头写着 `QB64 FlappyBird Clone by Terry Ritchie`，是第三方作品，仓库的 MIT 盖不住它。

---

## 许可全文在哪

| 上游仓库 | 许可文件 | 本地副本 |
|---|---|---|
| strathausen/qtrek.bas | `LICENSE` | `.scratch/qbasic-games/_licenses/strathausen_qtrek_bas.LICENSE` |
| fogleman/FutureBlocks | `LICENSE.md` | `.scratch/qbasic-games/_licenses/fogleman_FutureBlocks.LICENSE.md` |
| w84death/qbasic | `LICENSE` | `.scratch/qbasic-games/_licenses/w84death_qbasic.LICENSE` |
| ercanersoy/QBasic-Code-Examples | `LICENSE` | `.scratch/qbasic-games/_licenses/ercanersoy_QBasic-Code-Examples.LICENSE` |
| jichu4n/qbjc | `LICENSE` | `.scratch/qbasic-games/_licenses/jichu4n_qbjc.LICENSE` |
| michd/maze.bas | `LICENSE` | `.scratch/qbasic-games/_licenses/michd_maze_bas.LICENSE` |
| csirmaz/BasicSolitaire | `LICENSE` | `.scratch/qbasic-games/_licenses/csirmaz_BasicSolitaire.LICENSE` |
| Sebastian-gthb/bouncing_ball.bas | `LICENSE` | `.scratch/qbasic-games/_licenses/Sebastian-gthb_bouncing_ball_bas.LICENSE` |
| BernardIgiri/TurtleBas | `LICENSE` | `.scratch/qbasic-games/_licenses/BernardIgiri_TurtleBas.LICENSE` |
| dzik143/gwiezdna-bitwa | `LICENSE.txt`（+ 文件头） | `.scratch/qbasic-games/_licenses/dzik143_gwiezdna-bitwa.LICENSE.txt` |
| FarhanAliQureshi/dos_basic | `LICENSE` | `.scratch/qbasic-games/_licenses/FarhanAliQureshi_dos_basic.LICENSE` |
| boxgaming/qbjs | `LICENSE` | `.scratch/qbasic-games/_licenses/boxgaming_qbjs.LICENSE` |

> MIT 与 Apache-2.0 都要求「保留版权声明与许可声明」。本目录**保留了原作者的文件头**（一字未改，
> 全部 26 个文件的 md5 与上游逐字节一致），并且**上游许可全文已经随包分发** ——
> 就在本目录的 `LICENSES.txt`（把上表 12 份许可按仓库顺序拼在一起，原样未改）。
> 工作目录 `.scratch/qbasic-games/_licenses/` 里另存的是逐个仓库的原始文件，便于比对。
