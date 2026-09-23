# Pascal 例程 —— 来源与致谢（CREDITS）

本目录收录的 Pascal 源码**全部来自上游仓库、文件头一字未改**，仅重命名了文件名
（归一为「小写 + 下划线」并加来源前缀，见下表）。

每个来源的许可证据（作者 → 许可 → URL → 依据原话）如下，完整许可文本见同目录 `LICENSES.txt`。
B 类（许可不明 / 有版权）的候选**没有**放进本目录，只在 `.scratch/pascal-programs/` 留档。

---

## `ktp_*` —— lkesteloot/turbopascal

- **作者**：Lawrence Kesteloot
- **许可**：BSD-2-Clause
- **许可声明 URL**：https://github.com/lkesteloot/turbopascal/blob/master/LICENSE
- **依据原话**：`See the LICENSE file for the BSD 2-clause license.`（README.md 末行）；LICENSE 首行 `Copyright (c) 2013, Lawrence Kesteloot` / `All rights reserved.` / `Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:`
- **说明**：浏览器版 Turbo Pascal 5.5 子集编译器（含 crt / graph(BGI) 单元实现）自带的一批 .PAS 测试程序。
- **本目录文件（7）**：`ktp_bspline.pas`, `ktp_fastmand.pas`, `ktp_firework.pas`, `ktp_hello.pas`, `ktp_rose.pas`, `ktp_spider.pas`, `ktp_test.pas`

## `g7iles_*` —— gladir/7iles

- **作者**：Sylvain Maltais
- **许可**：MIT
- **许可声明 URL**：https://github.com/gladir/7iles/blob/main/LICENSE
- **依据原话**：`MIT License` / `Copyright (c) 2021 Sylvain Maltais` / `Permission is hereby granted, free of charge, to any person obtaining a copy of this software ...`
- **说明**：80+ 个 Pure Pascal 小游戏（Turbo Pascal 7 / Free Pascal 双分支）。
- **本目录文件（27）**：`g7iles_2048.pas`, `g7iles_7iles.pas`, `g7iles_asteroid.pas`, `g7iles_baby.pas`, `g7iles_berzerk.pas`, `g7iles_blox.pas`, `g7iles_breakout.pas`, `g7iles_csi.pas`, `g7iles_frogger.pas`, `g7iles_gomoku.pas`, `g7iles_hanois.pas`, `g7iles_invaders.pas`, `g7iles_life.pas`, `g7iles_loderunn.pas`, `g7iles_mahjong.pas`, `g7iles_mario.pas`, `g7iles_missile.pas`, `g7iles_monopoly.pas`, `g7iles_pacman.pas`, `g7iles_pegleap.pas`, `g7iles_pong.pas`, `g7iles_renju.pas`, `g7iles_sokoban.pas`, `g7iles_solitair.pas`, `g7iles_tetris.pas`, `g7iles_ttt.pas`, `g7iles_ttt3d.pas`

## `gmsdos_*` —— gladir/MSDOS-0

- **作者**：Sylvain Maltais
- **许可**：MIT
- **许可声明 URL**：https://github.com/gladir/MSDOS-0/blob/main/LICENSE
- **依据原话**：`MIT License` / `Copyright (c) 2021 Sylvain Maltais`
- **说明**：MS-DOS 命令克隆（每个命令一个独立 .PAS）。
- **本目录文件（9）**：`gmsdos_chkdsk.pas`, `gmsdos_choice.pas`, `gmsdos_command.pas`, `gmsdos_debug.pas`, `gmsdos_deltree.pas`, `gmsdos_dosshell.pas`, `gmsdos_edit.pas`, `gmsdos_edlin.pas`, `gmsdos_format.pas`

## `gnc_*` —— gladir/NORTONCOMMANDER-0

- **作者**：Sylvain Maltais
- **许可**：MIT
- **许可声明 URL**：https://github.com/gladir/NORTONCOMMANDER-0/blob/main/LICENSE
- **依据原话**：`MIT License` / `Copyright (c) 2024 Sylvain Maltais`
- **说明**：Norton Commander 克隆（单文件 81KB，本语料最大）。
- **本目录文件（2）**：`gnc_nc.pas`, `gnc_ncedit.pas`

## `gcorail_*` —— gladir/corail

- **作者**：Sylvain Maltais
- **许可**：MIT
- **许可声明 URL**：https://github.com/gladir/corail/blob/main/LICENSE
- **依据原话**：`MIT License` / `Copyright (c) 2021 Sylvain Maltais`
- **说明**：510+ 个命令行小工具中的若干代表。
- **本目录文件（8）**：`gcorail_ascii.pas`, `gcorail_base64.pas`, `gcorail_du.pas`, `gcorail_guid.pas`, `gcorail_iconv.pas`, `gcorail_number.pas`, `gcorail_printf.pas`, `gcorail_vdiag.pas`

## `avc_*` —— cavo789/swag

- **作者**：Christophe Avonture (AVC Software)
- **许可**：MIT
- **许可声明 URL**：https://github.com/cavo789/swag/blob/master/LICENSE
- **依据原话**：`MIT License` / `Copyright (c) 2020 Christophe Avonture`；文件头另有作者自署 `(c) AVC Software` / `Cardware`
- **说明**：作者本人投给 SWAG 归档的 DOS 底层程序（端口 / 中断 / CMOS / EXE 头 / 视频内存）。
- **本目录文件（27）**：`avc_ani2ico.pas`, `avc_banyan.pas`, `avc_cmos.pas`, `avc_constant.pas`, `avc_convert_hex.pas`, `avc_crt_demo.pas`, `avc_cursor.pas`, `avc_disk_serial.pas`, `avc_dos_icon_viewer.pas`, `avc_exe_head.pas`, `avc_exe_size.pas`, `avc_fast_output_video.pas`, `avc_file_select.pas`, `avc_flush_keyboard.pas`, `avc_ico2inc.pas`, `avc_inline_com.pas`, `avc_menu.inc`, `avc_menu.pas`, `avc_mouse_unit.pas`, `avc_read_car.pas`, `avc_segment_offset.pas`, `avc_set_etat_led.pas`, `avc_swap_bin.pas`, `avc_tspeedbutton.pas`, `avc_vertical_menu.pas`, `avc_vertical_menu_sample.pas`, `avc_word2hex.pas`

## `tpdem_*` —— johangardhage/dos-tpdemos

- **作者**：Johan Gardhage
- **许可**：MIT
- **许可声明 URL**：https://github.com/johangardhage/dos-tpdemos/blob/master/LICENSE
- **依据原话**：`MIT License` / `Copyright (c) 2018 Johan Gardhage`
- **说明**：Borland Turbo Pascal 7 复古编程教程的 20 个递进小 demo + 两个自带单元。
- **本目录文件（11）**：`tpdem_demo01.pas`, `tpdem_demo02.pas`, `tpdem_demo03.pas`, `tpdem_demo05.pas`, `tpdem_demo07.pas`, `tpdem_demo10.pas`, `tpdem_demo13.pas`, `tpdem_demo17.pas`, `tpdem_demo20.pas`, `tpdem_gfx.pas`, `tpdem_vector.pas`

## `swag_*` —— nickelsworth/swag

- **作者**：多名（SWAG 捐赠者，逐条见 INVENTORY「许可依据原话」）
- **许可**：public domain / freeware（文件头作者自述）+ 仓库默认 BSD-2
- **许可声明 URL**：https://github.com/nickelsworth/swag/blob/master/LICENSE
- **依据原话**：仓库 LICENSE 原文：`The code snippets in this archive are generally freeware or public domain as defined by their accompanying comments and copyrights. Any content without a specific license attached to it is considered to be licensed under the 2-clause BSD license`；**本次入选的 20 个文件每一个的文件头都另有作者自述**（如 `Released into the public domain 1989`、`PUBLIC DOMAIN 1993 Peter M. Gruhn`、`FreeWare`），逐条原话见 .scratch/pascal-programs/INVENTORY.md
- **说明**：SWAG（SourceWare Archive Group，1997 终版）里**文件头自己就写着 public domain / freeware** 的片段。⚠ 另有 3 个 SWAG 片段（`crt/0033.pas` TurboPower 版权+「with the aid of the commercial product」、`dos/0059.pas` 含 Borland RTL 源码部分、`timing/0014.pas` 「is copyrighted but may be used ... as long as」）**因为许可带条件或含他人版权，已降级为 B 类**，不在本目录，见 .scratch/pascal-programs/INVENTORY.md 的 `swagcond_*`。
- **本目录文件（20）**：`swag_crt_0009.pas`, `swag_dos_0034.pas`, `swag_dos_0051.pas`, `swag_egavga_0024.pas`, `swag_entry_0012.pas`, `swag_exec_0017.pas`, `swag_graphics_0029.pas`, `swag_graphics_0038.pas`, `swag_graphics_0046.pas`, `swag_graphics_0069.pas`, `swag_hardware_0057.pas`, `swag_interrupt_0012.pas`, `swag_memory_0022.pas`, `swag_memory_0034.pas`, `swag_memory_0038.pas`, `swag_memory_0054.pas`, `swag_mouse_0007.pas`, `swag_sound_0025.pas`, `swag_textwndw_0012.pas`, `swag_tsr_0007.pas`

---

## 再分发注意事项

1. MIT / BSD-2 都要求**保留版权声明与许可文本**。本目录同时保留 `CREDITS.md`（本文件）
   与 `LICENSES.txt`，`make-vml-lib.sh` 打包 `Examples/` 时必须把这两个文件一并带上。
2. `swag_*` 这批的许可依据是**每个文件自己文件头里作者的原话**（仓库级 LICENSE 只是兜底默认）。
   逐条原话见 `.scratch/pascal-programs/INVENTORY.md` 对应条目；改这几个文件等于改许可依据，
   所以**必须保持文件头原样**。
3. 本目录中 `catch.pas`、`sysinfo.pas`、`stm32/` 是本项目自己写的例子，不在本登记的范围内。
