(* demo_tty.ld —— Ladder 第二层：**彩色控制台**（tty）

   Ladder 是 IEC 61131-3 的**工业控制**语言（PLC 梯形图），它**没有** conio / crt /
   ANSI 辅助库：`Lib/ladder/gfx.ld` 里 `VGA_*` 那几个块用的是 DOS 时代的 syscall
   号（80/81/82），`GFX_*` 那几个块**整个块体就是一个 `RET`**（空实现）。
   而更要紧的是 —— 那些块名是 `[GFX_LINE]` 这种**裸标签**，前端调函数时会编成
   `CALL LADDER_<名字>`，两边**对不上**，所以从 Ladder 里**根本调不到任何库函数**
   （实测：`R := ui_win_open("x", 320, 240);` ⇒ 「未定义的函数 'LADDER_UI_WIN_OPEN'」）。

   于是这一层只能**直接发 ANSI 转义序列**，一个字节一个字节地送 ——
   与 `Examples/c/ansi_colors.c` 同一个做法、同一套效果
   （手机端「命令行」页由 `UI/Shared/AnsiMarkup.cs` 翻成彩色富文本）。

   跑法（桌面 vmlcli）：
     dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/ladder/demo_tty.ld

   画面（从上到下）：
     ① 清屏（ESC[2J + ESC[H）+ 复位属性
     ② 8 个暗色前景（色号 0-7），背景 = 7 浅灰
     ③ 8 个亮色前景（色号 8-15），背景 = 0 黑
     ④ 光标定位：同一行先写右半段、再回到左端写左半段
     ⑤ 复位成白字黑底，正常结束

   ◆ 写法：**ESC 用 `PRINT_CHAR 27` 发，序列的其余部分用 `PRINT_STR` 写**
     （两行一条序列，字符串里全是可见字符，不必往源码里塞控制字节）：
         PRINT_CHAR 27;            (* ESC *)
         PRINT_STR "[31;47m";      (* 前景 31 / 背景 47 *)
     另一种写法是把 ESC 直接**嵌进字符串字面量**（`PRINT_STR "<0x1B>[31m"`）——
     实测也能跑通，但源码里会出现一个不可见字节，编辑器里极易看漏或改坏，所以不采用。

   ◆ ⚠ **这一层写不出循环**，所以下面是 16 条手工展开的彩色行。
     原因是本前端的两个缺陷（都是实测的最小复现，本文件刻意绕开）：
       ① **标签 `LBL_x:` 会吃掉它后面的语句**：
            S := 7; PRINT_INT S;              ⇒ 打出 7      ✅
            S := 7; LBL_A: PRINT_INT S;       ⇒ 什么都不打  ❌
       ② **`IF <条件> THEN … END_IF` 的块体不执行**（条件写 `S = 1` 或布尔量都试过，
          块内语句一条没跑，块后的照常跑）。
     两条合起来 ⇒ 没有可用的跳转/分支 ⇒ **没有循环**。
     好在彩色控制台本来就是「固定几行」的活儿，手工展开不影响结果。
*)

(* 本程序一个变量都不用，所以 VAR 段也省了（实测 `PROGRAM … BEGIN … END_PROGRAM`
   不带 VAR 完全合法）。 *)
PROGRAM DemoTty
BEGIN
  (* ① 清屏 + 复位属性 *)
  PRINT_CHAR 27;
  PRINT_STR "[0m";
  PRINT_CHAR 27;
  PRINT_STR "[2J";
  PRINT_CHAR 27;
  PRINT_STR "[H";

  (* 标题：亮白字(97) + 蓝底(44) *)
  PRINT_CHAR 27;
  PRINT_STR "[97;44m";
  PRINT_STR "=== Ladder color console demo ===  (ANSI 97;44 = bright white on blue)";
  PRINT_CHAR 10;

  (* ② 暗色 0-7（前景 30-37 / 背景 47 浅灰），定位到第 3-10 行第 3 列 *)
  PRINT_CHAR 27;
  PRINT_STR "[3;3H";
  PRINT_CHAR 27;
  PRINT_STR "[30;47m";
  PRINT_STR "前景色 0  暗色，背景 = 7 浅灰";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[4;3H";
  PRINT_CHAR 27;
  PRINT_STR "[31;47m";
  PRINT_STR "前景色 1  暗色，背景 = 7 浅灰";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[5;3H";
  PRINT_CHAR 27;
  PRINT_STR "[32;47m";
  PRINT_STR "前景色 2  暗色，背景 = 7 浅灰";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[6;3H";
  PRINT_CHAR 27;
  PRINT_STR "[33;47m";
  PRINT_STR "前景色 3  暗色，背景 = 7 浅灰";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[7;3H";
  PRINT_CHAR 27;
  PRINT_STR "[34;47m";
  PRINT_STR "前景色 4  暗色，背景 = 7 浅灰";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[8;3H";
  PRINT_CHAR 27;
  PRINT_STR "[35;47m";
  PRINT_STR "前景色 5  暗色，背景 = 7 浅灰";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[9;3H";
  PRINT_CHAR 27;
  PRINT_STR "[36;47m";
  PRINT_STR "前景色 6  暗色，背景 = 7 浅灰";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[10;3H";
  PRINT_CHAR 27;
  PRINT_STR "[37;47m";
  PRINT_STR "前景色 7  暗色，背景 = 7 浅灰";
  PRINT_CHAR 10;

  (* ③ 亮色 8-15（前景 90-97 / 背景 40 黑），定位到第 12-19 行第 3 列 *)
  PRINT_CHAR 27;
  PRINT_STR "[12;3H";
  PRINT_CHAR 27;
  PRINT_STR "[90;40m";
  PRINT_STR "前景色 8  亮色，背景 = 0 黑";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[13;3H";
  PRINT_CHAR 27;
  PRINT_STR "[91;40m";
  PRINT_STR "前景色 9  亮色，背景 = 0 黑";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[14;3H";
  PRINT_CHAR 27;
  PRINT_STR "[92;40m";
  PRINT_STR "前景色 10  亮色，背景 = 0 黑";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[15;3H";
  PRINT_CHAR 27;
  PRINT_STR "[93;40m";
  PRINT_STR "前景色 11  亮色，背景 = 0 黑";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[16;3H";
  PRINT_CHAR 27;
  PRINT_STR "[94;40m";
  PRINT_STR "前景色 12  亮色，背景 = 0 黑";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[17;3H";
  PRINT_CHAR 27;
  PRINT_STR "[95;40m";
  PRINT_STR "前景色 13  亮色，背景 = 0 黑";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[18;3H";
  PRINT_CHAR 27;
  PRINT_STR "[96;40m";
  PRINT_STR "前景色 14  亮色，背景 = 0 黑";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[19;3H";
  PRINT_CHAR 27;
  PRINT_STR "[97;40m";
  PRINT_STR "前景色 15  亮色，背景 = 0 黑";
  PRINT_CHAR 10;

  (* ④ 光标定位：第 21 行第 34 列先写，再回第 21 行第 1 列写 *)
  PRINT_CHAR 27;
  PRINT_STR "[21;34H";
  PRINT_CHAR 27;
  PRINT_STR "[93;41m";
  PRINT_STR "<- 先写的（第 34 列）";
  PRINT_CHAR 10;

  PRINT_CHAR 27;
  PRINT_STR "[21;1H";
  PRINT_CHAR 27;
  PRINT_STR "[96;40m";
  PRINT_STR "后写的（第 1 列）-> ";
  PRINT_CHAR 10;

  (* ⑤ 收尾：第 23 行第 1 列，复位成白字黑底（37 / 40） *)
  PRINT_CHAR 27;
  PRINT_STR "[23;1H";
  PRINT_CHAR 27;
  PRINT_STR "[37;40m";
  PRINT_STR "=== done（已复位为白字黑底）===";
  PRINT_CHAR 10;
END_PROGRAM
