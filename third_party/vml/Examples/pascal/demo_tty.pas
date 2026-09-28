(* demo_tty.pas —— Pascal 第二层：**彩色控制台**（tty）
  demo_tty.pas -- Pascal layer 2: colour console (tty)

  这一层用的是 Turbo Pascal 的 `Crt` 单元：`ClrScr` / `TextColor` / `TextBackground`
  This layer uses the Turbo Pascal Crt unit: ClrScr / TextColor / TextBackground
  / `GotoXY`。它们由前端翻译成 **ANSI 转义序列**（走 `SYSCALL 400-402` 那条
  / GotoXY. The frontend translates them into ANSI escape sequences (through the SYSCALL 400-402
  "绕过 stdout 重定向"的 TTY 通道），桌面 vmlcli 的终端里直接可见，手机端
  TTY channel that bypasses stdout redirection); they are directly visible in a desktop vmlcli terminal,
  「命令行」页也会被 `UI/Shared/AnsiMarkup.cs` 翻成彩色富文本 —— 同一份源码。
  and the mobile command-line page turns them into coloured rich text via UI/Shared/AnsiMarkup.cs -- the same source.

  跑法（桌面 vmlcli）：
  How to run (desktop vmlcli):
    dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/pascal/demo_tty.pas

  画面（从上到下）：
  The picture (top to bottom):
    ① 清屏（`ClrScr`）
    1) clear the screen (ClrScr)
    ② 8 个暗色前景（色号 0-7），背景 = 7 浅灰，便于看清暗色
    2) 8 dark foregrounds (colour numbers 0-7) on background 7 light grey, so the dark colours are visible
    ③ 8 个亮色前景（色号 8-15），背景 = 0 黑
    3) 8 bright foregrounds (colour numbers 8-15) on background 0 black
    ④ `GotoXY` 定位：同一行先写右半段、再回到左端写左半段
    4) GotoXY positioning: write the right half of a line first, then go back to the left end for the left half
       —— 证明是**定位**不是顺序输出
       -- proving it is positioning and not sequential output
    ⑤ 复位成白字黑底，正常结束
    5) reset to white on black and end normally

  ◆ `Crt` 单元的三条事实（本文件用到的）
  * Three facts about the Crt unit (the ones used here)

    ① 颜色常量（`Black` / `Blue` / … / `White` / `Yellow`）由 `Lib/pascal/crt.pas`
    1) The colour constants (Black / Blue / ... / White / Yellow) are registered by Lib/pascal/crt.pas
       登记，**与 `Graph` 的调色板索引同值同名** —— 所以一个文件里
       with the same values and names as the Graph palette indexes -- so inside one file both
       `TextColor(Red)` 与 `SetColor(Red)` 都能用。
       TextColor(Red) and SetColor(Red) can be used.
    ② `TextColor` 的高位可以加 `Blink`（=128）—— 本平台画不出闪烁，但值按 TP 给对。
    2) The high bit of TextColor can carry Blink (=128) -- this platform cannot blink, but the value is given as TP defines it.
    ③ ⚠ 用 `Crt` 之后 **`writeln` 仍然走 stdout 那条路**（实测中文正常），
    3) WARNING: after using Crt, writeln still goes through the stdout path (Chinese works, measured),
       与 BASIC 不同 —— BASIC 一旦 `COLOR` 就切进 `TTY_WriteChar` 那条**按字节**
       unlike BASIC -- once BASIC does COLOR it switches to the TTY_WriteChar path that sends
       送字符的路，非 ASCII 会碎（见 `Examples/basic/demo_tty.bas` 的说明）。
       characters byte by byte, and non-ASCII breaks (see Examples/basic/demo_tty.bas).
       所以这里的正文可以照常写中文。
       So the body text here can contain Chinese as usual.

  ◆ 一个实测踩到的小坑：**Pascal 的两种块注释都不嵌套**。
  * One small trap found by measurement: neither of Pascal's two block comment forms nests.
    花括号那种就不必说了；而括号星号那种同样会被内层的那对符号提前收掉 ——
    The brace form needs no explanation; the paren-star form is also closed early by an inner pair of those symbols --
    本文件的头注释本来想拿它举例，结果**举例子本身就把注释收掉了**，
    this header comment meant to use it as an example, but the example itself closed the comment,
    后面正文全被当成代码（实测报「未知字符: `」）。所以这类符号一律用文字描述。
    and the rest of the text was parsed as code (it reports an unknown character). So such symbols are always described in words.
*)

program DemoTty;

uses
  Crt;

var
  c, row: integer;

begin
  { ① 先清屏 }
  { 1) clear the screen first }
  ClrScr;

  { 标题：亮白字 + 蓝底 }
  { title: bright white on a blue background }
  TextColor(White);
  TextBackground(Blue);
  GotoXY(1, 1);
  writeln('=== Pascal 彩色控制台 demo ===  (TextColor(White) + TextBackground(Blue))');

  { ② 暗色 0-7（背景 7 浅灰） }
  { 2) dark colours 0-7 (background 7 light grey) }
  row := 3;
  for c := 0 to 7 do
  begin
    GotoXY(3, row + c);
    TextColor(c);
    TextBackground(LightGray);
    write('前景色 ', c, '  暗色，背景 = 7 浅灰');
  end;

  { ③ 亮色 8-15（背景 0 黑） }
  { 3) bright colours 8-15 (background 0 black) }
  row := 12;
  for c := 8 to 15 do
  begin
    GotoXY(3, row + (c - 8));
    TextColor(c);
    TextBackground(Black);
    write('前景色 ', c, '  亮色，背景 = 0 黑');
  end;

  { ④ GotoXY 定位：先写右半段，再回到左端写左半段 }
  { 4) GotoXY positioning: write the right half first, then go back to the left end for the left half }
  GotoXY(34, 21);
  TextColor(Yellow);
  TextBackground(Red);
  write('<- 先写的（第 34 列）');

  GotoXY(1, 21);
  TextColor(LightCyan);
  TextBackground(Black);
  write('后写的（第 1 列）-> ');

  { ⑤ 收尾：复位成白字黑底 }
  { 5) wrap-up: reset to white on black }
  GotoXY(1, 23);
  TextColor(LightGray);
  TextBackground(Black);
  writeln('=== done（已复位为白字黑底）===');
end.
