{
 STG>Does anyone know off hand if I can be in text mode and window in a
 STG>window and put the wondow only in graphics mode?
 STG>I have a program that I need to have a graph in.  Does anyone have some
 STG>code for using the PLOT procedure to plot variables.  The values for
 STG>the Y axis are from 1 - 2000, and for the X axis from 1 - 24.

        Yes, it's possible... sort of.   If you have a VGA (or
EGA) you can have 2 separate character sets on screen at once.
Use one character set for text, and redefine the other for your
graphics window.  The only problem is that your graphics window
can only be composed of 256 characters total.  So, a 16 X 16
character square would only give you a vertical resolution of 256
pixels and a horizontal resolution of 128 pixels.  The following
code is an example of how one would do this.

                                                Dave

}

Program GraphicsInTextModeExample;

{================================================

         Graphics In Text Mode Example
            Programmed by David Dahl
                    12/24/93
    This program and source are PUBLIC DOMAIN

 ------------------------------------------------

   This example uses a second font as a pseudo-
   graphics window.  This program requires VGA.

 ================================================}

Uses  CRT;

Const { Dimentions of The Graphics Window in Characters }
      ChrSizeX = 32;
      ChrSizeY = 256 DIV ChrSizeX;
      { Dimentions of The Graphics Window in Pixels }
      MaxX     = ChrSizeX * 8;
      MaxY     = ChrSizeY * 16;
      { VGA 16 色调色板 → 本平台的 ARGB 颜色。
        原程序向文本屏的属性字节写 VGA 调色板号（White=15 / Blue=1 …），
        本平台没有 VGA 调色板硬件，颜色的表达方式是 0xAARRGGBB 整数 ⇒ 按
        VGA 标准 16 色表逐项换算（行尾负数是 0xFFxxxxxx 的有符号写法）。 }
      VgaPal : array[0..15] of integer = (
        -16777216,   {  0 黑      000000 }
        -16777046,   {  1 蓝      0000AA }
        -16733696,   {  2 绿      00AA00 }
        -16733526,   {  3 青      00AAAA }
        -5636096,    {  4 红      AA0000 }
        -5635926,    {  5 洋红    AA00AA }
        -5614336,    {  6 棕      AA5500 }
        -5592406,    {  7 浅灰    AAAAAA }
        -11184811,   {  8 深灰    555555 }
        -11184641,   {  9 亮蓝    5555FF }
        -11141291,   { 10 亮绿    55FF55 }
        -11141121,   { 11 亮青    55FFFF }
        -43691,      { 12 亮红    FF5555 }
        -43521,      { 13 亮洋红  FF55FF }
        -171,        { 14 黄      FFFF55 }
        -1);         { 15 白      FFFFFF }

{ 图形窗口在本平台的状态：绘图窗口里的一块矩形区域（原平台是文本模式里的伪图形窗口，
  位置由 SetGraphicsWindow 的 (XCoord,YCoord) 字符格与 32×8 格尺寸换算而来）。 }
Var GWinX, GWinY, GWinW, GWinH  : integer;   { 窗口区域在本平台画布上的位置与大小 }
    GWinFG, GWinBG             : integer;   { 前景色（画像素用）/ 背景色 }

{-[ VGA 调色板号 → 本平台 ARGB 颜色 ]-------------------------------------}
Function VgaColor (c : integer) : integer;
Begin
     If (c < 0) OR (c > 15) Then c := 15;
     VgaColor := VgaPal[c];
End;

{-[ Set Character Width to 8 Pixels ]-------------------------------------}
{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：在 VGA 的 CRTC/定序器端口
  （$3CC/$3C2 时钟模式、$3C4/$3C5 定序器、$3DA 输入状态、$3C0 属性控制器）上
  把文本模式切成「640 点行 + 8 像素宽字模」，为后面的双字库伪图形窗口做准备。
  本平台没有 VGA 文本模式的字模硬件，也没有端口 I/O ⇒ 以空过程代替
  （该环境的「设置成功」在本平台无意义）；原汇编保留在下方注释里备查。 }
Procedure SetCharWidthTo8;
Begin
End;

(*
Procedure SetCharWidthTo8; Assembler;
Asm
   { Change To 640 Horz Res }
   MOV DX, $3CC
   IN  AL, DX
   AND AL, Not(4 OR 8)
   MOV DX, $3C2
   OUT DX, AL
   { Turn Off Sequence Controller }
   MOV DX, $3C4
   MOV AL, 0
   OUT DX, AL
   MOV DX, $3C5
   MOV AL, 0
   OUT DX, AL
   { Reset Sequence Controller }
   MOV DX, $3C4
   MOV AL, 0
   OUT DX, AL
   MOV DX, $3C5
   MOV AL, 3
   OUT DX, AL
   { Switch To 8 Pixel Wide Fonts }
   MOV DX, $3C4
   MOV AL, 1
   OUT DX, AL
   MOV DX, $3C5
   IN  AL, DX
   OR  AL, 1
   OUT DX, AL
   { Turn Off Sequence Controller }
   MOV DX, $3C4
   MOV AL, 0
   OUT DX, AL
   MOV DX, $3C5
   MOV AL, 0
   OUT DX, AL
   { Reset Sequence Controller }
   MOV DX, $3C4
   MOV AL, 0
   OUT DX, AL
   MOV DX, $3C5
   MOV AL, 3
   OUT DX, AL
   { Center Screen }
   MOV DX, $3DA
   IN  AL, DX
   MOV DX, $3C0
   MOV AL, $13 OR 32
   OUT DX, AL
   MOV AL, 0
   OUT DX, AL
End;
*)
{-[ Turn On Dual Fonts ]--------------------------------------------------}
{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 BIOS 视频中断 10h 的
  「装载 8×8 字库」功能（AH=11h, AL=03h, BL=4）—— 把字库块 0/1 装成 512 个
  8×8 字符，于是文本模式能同时用两套字符集（第二套就是伪图形窗口的字形）。
  本平台没有 BIOS 中断、也没有可装载的文本字库 ⇒ 以空过程代替；
  原汇编保留在下方注释里备查。 }
Procedure SetDualFonts;
Begin
End;

(*
Procedure SetDualFonts; Assembler;
ASM
   { Set Fonts 0 & 1 }
   MOV BL, 4
   MOV AX, $1103
   INT $10
END;
*)
{-[ Turn On Access To Font Memory ]---------------------------------------}
{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：在 VGA 定序器（$3C4/$3C5）
  与图形控制器（$3CE/$3CF）上把寻址从「奇偶（odd/even）」改成「线性」、
  写平面切到平面 2、读映射也指向平面 2 —— 也就是打开「字体内存」（$B800:$4000
  起）的读写窗口，后面 PutPixel 就往这块内存里画字形位。
  本平台没有 VGA 平面显存与端口 I/O ⇒ 以空过程代替；原汇编保留在下方注释里备查。 }
Procedure SetAccessToFontMemory;
Begin
End;

(*
Procedure SetAccessToFontMemory; Assembler;
ASM
   { Turn Off Sequence Controller }
   MOV DX, $3C4
   MOV AL, 0
   OUT DX, AL
   MOV DX, $3C5
   MOV AL, 1
   OUT DX, AL
   { Reset Sequence Controller }
   MOV DX, $3C4
   MOV AL, 0
   OUT DX, AL
   MOV DX, $3C5
   MOV AL, 3
   OUT DX, AL
   { Change From Odd/Even Addressing to Linear }
   MOV DX, $3C4
   MOV AL, 4
   OUT DX, AL
   MOV DX, $3C5
   MOV AL, 7
   OUT DX, AL
   { Switch Write Access To Plane 2 }
   MOV DX, $3C4
   MOV AL, 2
   OUT DX, AL
   MOV DX, $3C5
   MOV AL, 4
   OUT DX, AL
   { Set Read Map Reg To Plane 2 }
   MOV DX, $3CE
   MOV AL, 4
   OUT DX, AL
   MOV DX, $3CF
   MOV AL, 2
   OUT DX, AL
   { Set Graphics Mode Reg }
   MOV DX, $3CE
   MOV AL, 5
   OUT DX, AL
   MOV DX, $3CF
   MOV AL, 0
   OUT DX, AL
   { Set Misc. Reg }
   MOV DX, $3CE
   MOV AL, 6
   OUT DX, AL
   MOV DX, $3CF
   MOV AL, 12
   OUT DX, AL
End;
*)
{-[ Turn On Access to Text Memory ]---------------------------------------}
{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：SetAccessToFontMemory 的
  逆操作 —— 把定序器/图形控制器改回「奇偶寻址 + 写平面 3」，也就是关掉字体内存
  窗口、把访问权还给文本内存（$B800:$0000 的字符/属性字节）。
  本平台没有 VGA 平面显存与端口 I/O ⇒ 以空过程代替；原汇编保留在下方注释里备查。 }
Procedure SetAccessToTextMemory;
Begin
End;

(*
Procedure SetAccessToTextMemory; Assembler;
ASM
   { Turn Off Sequence Controller }
   MOV DX, $3C4
   MOV AL, 0
   OUT DX, AL
   MOV DX, $3C5
   MOV AL, 1
   OUT DX, AL
   { Reset Sequence Controller }
   MOV DX, $3C4
   MOV AL, 0
   OUT DX, AL
   MOV DX, $3C5
   MOV AL, 3
   OUT DX, AL
   { Change To Odd/Even Addressing }
   MOV DX, $3C4
   MOV AL, 4
   OUT DX, AL
   MOV DX, $3C5
   MOV AL, 3
   OUT DX, AL
   { Switch Write Access }
   MOV DX, $3C4
   MOV AL, 2
   OUT DX, AL
   MOV DX, $3C5
   MOV AL, 3  {?}
   OUT DX, AL
   { Set Read Map Reg }
   MOV DX, $3CE
   MOV AL, 4
   OUT DX, AL
   MOV DX, $3CF
   MOV AL, 0
   OUT DX, AL
   { Set Graphics Mode Reg }
   MOV DX, $3CE
   MOV AL, 5
   OUT DX, AL
   MOV DX, $3CF
   MOV AL, $10
   OUT DX, AL
   { Set Misc. Reg }
   MOV DX, $3CE
   MOV AL, 6
   OUT DX, AL
   MOV DX, $3CF
   MOV AL, 14
   OUT DX, AL
End;
*)
{-[ Clear The Pseudo-Graphics Window by Clearing Font Definition ]--------}
{ ⚠ 原实现直接写显存（非汇编，但同样只在本平台不存在的环境里成立）：先切到字体内存，
  把 $B800:$4000 起 32*256 字节（第二字库全部字形）清零，再切回文本内存 ——
  效果 = 伪图形窗口整块变空。本平台不能直写 $B800 显存，改走绘图接口：
  用背景色铺满图形窗口区域（窗口还没建时什么都不做，等价于「清一块还不存在的东西」）。 }
Procedure ClearGraphicsWindow;
Begin
     If GWinW > 0 Then
     Begin
          ui_rect (GWinX, GWinY, GWinW, GWinH, GWinBG, 1, 0, 0);
          ui_present ();
     End;
End;
{-[ Turn The Cursor Off ]-------------------------------------------------}
{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：向 CRTC 的索引/数据端口
  （$3D4/$3D5）选中 $0A 号寄存器（光标起始扫描行），把它的 bit5 置 1 ——
  文本模式下 bit5 = 关闭光标。本平台没有 CRTC 端口，改用 CRT 库自带的光标
  开关函数（语义等价：隐藏文本光标）；原汇编保留在下方注释里备查。 }
Procedure TurnCursorOff;
Begin
     CursorOff;
End;

(*
Procedure TurnCursorOff; Assembler;
ASM
   MOV DX, $3D4
   MOV AL, $0A
   OUT DX, AL
   MOV DX, $3D5
   IN  AL, DX
   OR  AL, 32
   OUT DX, AL
End;
*)
{-[ Turn The Cursor On ]--------------------------------------------------}
{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：同上，但把 $0A 号寄存器的
  bit5 清 0 —— 重新显示光标。本平台改用 CRT 库的光标开关函数（语义等价）；
  原汇编保留在下方注释里备查。 }
Procedure TurnCursorOn;
Begin
     CursorOn;
End;

(*
Procedure TurnCursorOn; Assembler;
ASM
   MOV DX, $3D4
   MOV AL, $0A
   OUT DX, AL
   MOV DX, $3D5
   IN  AL, DX
   AND AL, Not(32)
   OUT DX, AL
End;
*)
{-[ Set Up The Pseudo-Graphics Window ]-----------------------------------}
{ ⚠ 原实现直接写显存（非汇编，但同样只在本平台不存在的环境里成立）：把文本屏上从
  (XCoord,YCoord) 字符格起的 32×8 个字符格逐个写成「字形编号 = CounterX+CounterY*32」
  +「属性 = 前景色(带高亮) + 背景色<<4」—— 等于把第二字库的 256 个自定义字形拼成
  一整块伪图形窗口。本平台不能直写 $B800 显存，改走绘图接口：按原 VGA 640×480 的
  逻辑坐标（1 字符格 = 8×16 像素）换算出这块窗口在本平台画布上的位置与大小
  （画布实际尺寸取 ui_scr_w/ui_scr_h），开一扇绘图窗口并用背景色铺底；
  之后 PutPixel 画出的点就落在这块区域里（相当于原来的 256×128 个"伪像素"）。 }
Procedure SetGraphicsWindow (XCoord, YCoord    : Byte;
                             Color, BackGround : Byte);
Begin
     GWinFG := VgaColor (Color);
     GWinBG := VgaColor (BackGround);
     GWinX  := (XCoord * 8) * ui_scr_w () div 640;
     GWinY  := (YCoord * 16) * ui_scr_h () div 480;
     GWinW  := (ChrSizeX * 8) * ui_scr_w () div 640;
     GWinH  := (ChrSizeY * 16) * ui_scr_h () div 480;
     ui_win_open ('图形窗口', ui_scr_w (), ui_scr_h ());
     ui_rect (GWinX, GWinY, GWinW, GWinH, GWinBG, 1, 0, 0);
     ui_present ();
End;

(* 原实现（直写 $B800 文本屏，本平台无此环境，保留备查）：
Procedure SetGraphicsWindow (XCoord, YCoord    : Byte;
                             Color, BackGround : Byte);
Var CounterX,
    CounterY  : Byte;
Begin
     For CounterY := 0 to (ChrSizeY-1) do
         For CounterX := 0 to (ChrSizeX-1) do
             MEMW[$B800:CounterX*2 + XCoord*2 + (YCoord * 80 * 2) +
                 (CounterY * 80 * 2)] :=
                   (CounterX + CounterY * ChrSizeX) OR
                   (((Color OR 8) OR ((BackGround AND 15) SHL 4)) SHL 8);
End;
*)
{-[ Plot a Pixel in The Pseudo-Graphics Window ]--------------------------}
{ ⚠ 原实现直接写显存（非汇编，但同样只在本平台不存在的环境里成立）：先切到字体内存，
  再把字形位图里对应的那一位或上 1（地址 $B800:$4000 + RealX + RealY，
  位 = 128 SHR (Xin MOD 8)），最后切回文本内存 —— 一个"像素"其实是第二字库某个
  8×16 字形里的一个位。本平台不能直写 $B800 显存，改走绘图接口：把窗口内的像素坐标
  (Xin,Yin)（范围 MaxX×MaxY = 256×128）按窗口实际大小换算成画布坐标，
  用前景色 ui_pixel 画点（每个 PlotPixel 立即出图，与原平台的即时可见一致）。 }
Procedure PutPixel (Xin, Yin : Word);
Var RealY,
    RealX      : integer;
Begin
     If (Xin < MaxX) AND
        (Yin < MaxY)
     Then
     Begin
          RealX := GWinX + (Xin * GWinW) DIV MaxX;
          RealY := GWinY + (Yin * GWinH) DIV MaxY;
          ui_pixel (RealX, RealY, GWinFG);
     End;
End;

(* 原实现（直写 $B800 字体内存，本平台无此环境，保留备查）：
Procedure PutPixel (Xin, Yin : Word);
Var RealY,
    RealX      : Word;
Begin
     If (Xin < MaxX) AND
        (Yin < MaxY)
     Then
     Begin
          RealX := (Xin DIV 8) * 32;
          RealY := (Yin MOD 16) + ((Yin DIV 16) * (32 * ChrSizeX));
          SetAccessToFontMemory;
          MEM[$B800:$4000 + RealX + RealY] :=
              MEM[$B800:$4000 + RealX + RealY] OR (128 SHR (Xin MOD 8));
          SetAccessToTextMemory;
     End;
End;
*)
{-[ Draw A Line ]---------------------------------------------------------}
{ OCTANT DDA Subroutine converted from the BASIC listing on pages 26 - 27 }
{ from the book _Microcomputer_Displays,_Graphics,_ And_Animation_ by     }
{ Bruce A. Artwick                                                        }
Procedure Line (XStart, YStart, XEnd, YEnd : Word);
Var StartX,
    StartY,
    EndX,
    EndY    : Word;
    DX,
    DY      : Integer;
    CNTDWN  : Integer;
    Errr    : Integer;
    Temp    : Integer;
    NotDone : Boolean;
Begin
     NotDone := True;
     StartX := XStart;
     StartY := YStart;
     EndX   := XEnd;
     EndY   := YEnd;
     If EndX < StartX Then
     Begin
          { Mirror Quadrants 2,3 to 1,4 }
          Temp   := StartX;
          StartX := EndX;
          EndX   := Temp;
          Temp   := StartY;
          StartY := EndY;
          EndY   := Temp;
     End;
     DX := EndX - StartX;
     DY := EndY - StartY;
     If DY < 0 Then
     Begin
          If -DY > DX Then
          Begin
               { Octant 7 Line Generation }
               CntDwn := -DY + 1;
               ERRR   := -(-DY shr 1);   {Fast Divide By 2}
               While NotDone do
               Begin
                    PutPixel (StartX, StartY);
                    Dec (CntDwn);
                    If CntDwn <= 0
                    Then NotDone := False
                    Else
                    Begin
                         Dec(StartY);
                         Inc(Errr, DX);
                         If Errr >= 0 Then
                         Begin
                              Inc(StartX);
                              Inc(Errr, DY);
                         End;
                    End;
               End;
          End
          Else
          Begin
               { Octant 8 Line Generation }
               CntDwn := DX + 1;
               ERRR   := -(DX shr 1);   {Fast Divide By 2}
               While NotDone do
               Begin
                    PutPixel (StartX, StartY);
                    Dec (CntDwn);
                    If CntDwn <= 0
                    Then NotDone := False
                    Else
                    Begin
                         Inc(StartX);
                         Dec(Errr, DY);
                         If Errr >= 0 Then
                         Begin
                              Dec(StartY);
                              Dec(Errr, DX);
                         End;
                    End;
               End;
          End;
     End
     Else If DY > DX Then
          Begin
               { Octant 2 Line Generation }
               CntDwn := DY + 1;
               ERRR   := -(DY shr 1);   {Fast Divide By 2}
               While NotDone do
               Begin
                    PutPixel (StartX, StartY);
                    Dec (CntDwn);
                    If CntDwn <= 0
                    Then NotDone := False
                    Else
                    Begin
                         Inc(StartY);
                         Inc(Errr, DX);
                         If Errr >= 0 Then
                         Begin
                              Inc(StartX);
                              Dec(Errr, DY);
                         End;
                    End;
               End;
          End
          Else
          { Octant 1 Line Generation }
          Begin
               CntDwn := DX + 1;
               ERRR   := -(DX shr 1);   {Fast Divide By 2}
               While NotDone do
               Begin
                    PutPixel (StartX, StartY);
                    Dec (CntDwn);
                    If CntDwn <= 0
                    Then NotDone := False
                    Else
                    Begin
                         Inc(StartX);
                         Inc(Errr, DY);
                         If Errr >= 0 Then
                         Begin
                              Inc(StartY);
                              Dec(Errr, DX);
                         End;
                    End;
               End;
          End;
     { 本平台是「保留模式」渲染：原平台每写一次显存就即时可见，这里要显式标记一帧
        画完（ui_present），否则画好的线不会刷到画布上。 }
     ui_present ();
End;
{-[ Draw A Circle ]-----------------------------------------------------}
{ Algorithm based on the Pseudocode from page 83 of the book _Advanced  }
{ Graphics_In_C_ by Nelson Johnson                                      }
Procedure Circle (XCoord, YCoord, Radius : Integer);
Var   d     : Integer;
      X, Y  : Integer;
    Procedure Symmetry (xc, yc, x, y : integer);
    Begin
         PutPixel ( X+xc,  Y+yc);
         PutPixel ( X+xc, -Y+yc);
         PutPixel (-X+xc, -Y+yc);
         PutPixel (-X+xc,  Y+yc);
         PutPixel ( Y+xc,  X+yc);
         PutPixel ( Y+xc, -X+yc);
         PutPixel (-Y+xc, -X+yc);
         PutPixel (-Y+xc,  X+yc);
    End;
Begin
     x := 0;
     y := abs(Radius);
     d := 3 - 2 * y;
     While (x < y) do
     Begin
          Symmetry (XCoord, YCoord, x, y);
          if (d < 0) Then
             inc(d, (4 * x) + 6)
          else
          Begin
               inc (d, 4 * (x - y) + 10);
               dec (y);
          End;
          inc(x);
     End;
     If x = y then
        Symmetry (XCoord, YCoord, x, y);
     { 同 Line：保留模式渲染要显式标记一帧画完 }
     ui_present ();
End;
{-[ Draw A Rectangle ]----------------------------------------------------}
Procedure Rectangle (X1, Y1, X2, Y2 : Word);
Begin
     { Draw Top Of Box }
     Line (X1, Y1, X2, Y1);
     { Draw Right Side Of Box }
     Line (X2, Y1, X2, Y2);
     { Draw Left Side Of Box }
     Line (X1, Y1, X1, Y2);
     { Draw Botton Of Box }
     Line (X1, Y2, X2, Y2);
End;
{=[ Main Program ]========================================================}

Var C : Word;
    Key : Char;
Begin

     TextMode (C80);
     TurnCursorOff;
     SetCharWidthTo8;
     SetDualFonts;
     ClearGraphicsWindow;
     TextColor(LightGray);
     ClrScr;

     SetGraphicsWindow (40, 0, White, Blue);   {X, Y, Color, BGColor}

     Writeln ('Graphics In Text Mode Example');
     Writeln ('Programmed by David Dahl');
     Writeln ('This is PUBLIC DOMAIN');
     Writeln;
     Writeln ('The graphics window to the right is');
     Writeln ('made up of custom characters of the');
     Writeln ('second font.');
     Writeln;
     Writeln ('There are four graphics primitives');
     Writeln ('available in this example program.');
     Writeln ('Circle, Line, PutPixel, and ');
     Writeln ('Rectangle are avaiable for your own');
     Writeln ('use.');
     Writeln;

     Randomize;
     For C := 1 to 10 do
     Begin
          Line (Random(MaxX), Random(MaxY),
                Random(MaxX), Random(MaxY));

          Circle (Random(MaxX), Random(MaxY), Random(30));

          Rectangle (Random(MaxX), Random(MaxY),
                     Random(MaxX), Random(MaxY));
     End;

     Writeln ('Press [RETURN] to exit.');
     Readln;
     TurnCursorOn;
     TextMode (C80);
End.
