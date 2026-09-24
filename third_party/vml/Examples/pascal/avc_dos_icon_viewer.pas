(*
  Category: SWAG Title: GRAPHICS ROUTINES
  Original name: 0252.PAS
  Description: Dos Icon Viewer
  Author: AVONTURE CHRISTOPHE
  Date: 03-04-97  13:18
*)

{

   Dos icon viewer


               ╔════════════════════════════════════════╗
               ║                                        ║░
               ║          AVONTURE CHRISTOPHE           ║░
               ║              AVC SOFTWARE              ║░
               ║     BOULEVARD EDMOND MACHTENS 157/53   ║░
               ║           B-1080 BRUXELLES             ║░
               ║              BELGIQUE                  ║░
               ║                                        ║░
               ╚════════════════════════════════════════╝░
               ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░

}

Uses Crt, Dos;

CONST
   IcoSize = 32;  { An icon is a 32*32 square }

VAR
   arrNom : Array[1..255] of String[12]; { Store the full filename }

{ Display a pixel }

Procedure Put_Pixel (Colonne, Ligne, Couleur : Word);

{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 BIOS 中断 int 10h 的
  AH=0Ch 功能（写像素）——CX=列、DX=行、AL=颜色号、BH=0（第 0 显示页），在当时的
  VGA 640*480 256 色模式（模式 12h）下把那一个点设成调色板颜色 Couleur。
  这是**绘图**例程，本平台有等价的绘图接口 ⇒ 已用 ui_pixel 重新实现（原路是
  「往显存写一个点」，与 ui_pixel(x, y, color) 一一对应）。
  颜色号：图标文件里每个字节被拆成两个 4 位值，取值恒为 0..15，正是标准 16 色
  EGA/VGA 调色板 ⇒ 用下面的 Case 换算成 ui_pixel 要的 ARGB。
  ⚠ 两点差异：① 坐标系 —— 原程序画在 640*480 全屏上，本平台画在 ui_win_open
  开出的窗口里，故下面把 640*480 的坐标按窗口宽度（ui_scr_w）等比缩放，
  见 Begin 里的换算；② 调色板 —— 原程序在 256 色模式下调色板可被程序改写，
  这里取的是标准 16 色的默认值。
  原汇编保留在下方注释里备查。 }
(*
Asm
     Mov  Ah, 0Ch
     Mov  Al, Byte Ptr Couleur
     Mov  Cx, Colonne
     Mov  Dx, Ligne
     Xor  Bh, Bh
     Int  10h
End;
*)

{ 标准 16 色 EGA/VGA 调色板的 ARGB 值（下标 = 颜色号 0..15） }

Var sw, px, py, argb : Integer;

Begin

   { 颜色换算：图标文件里每个字节被拆成两个 4 位值，即颜色号 0..15，
     按标准 16 色 EGA/VGA 调色板换成 ui_pixel 要的 ARGB。

     ⚠ 这里刻意用 Case 逐个赋值，**不是**数组表 —— 本平台实测两件事：
     ① 「类型常量数组」（`Const X : Array[...] = (...)`）读出来是**垃圾值**
        （同一个下标三次读出的都不是写的那个数）；
     ② 过程内的**局部数组会压坏形参帧**（实测形参收到的值变成了数组元素自己的值）。
     只有逐个赋值这一条路是可靠的。 }

   Case Couleur And 15 Of
      0  : argb := -16777216;   {  0 黑      $000000 }
      1  : argb := -16777046;   {  1 蓝      $0000AA }
      2  : argb := -16733696;   {  2 绿      $00AA00 }
      3  : argb := -16733526;   {  3 青      $00AAAA }
      4  : argb := -5636096;    {  4 红      $AA0000 }
      5  : argb := -5635926;    {  5 品红    $AA00AA }
      6  : argb := -5614336;    {  6 棕      $AA5500 }
      7  : argb := -5592406;    {  7 浅灰    $AAAAAA }
      8  : argb := -11184811;   {  8 暗灰    $555555 }
      9  : argb := -11184641;   {  9 亮蓝    $5555FF }
      10 : argb := -11141291;   { 10 亮绿    $55FF55 }
      11 : argb := -11141121;   { 11 亮青    $55FFFF }
      12 : argb := -43691;      { 12 亮红    $FF5555 }
      13 : argb := -43521;      { 13 亮品红  $FF55FF }
      14 : argb := -171;        { 14 黄      $FFFF55 }
      15 : argb := -1;          { 15 白      $FFFFFF }
   End;

   { 坐标系换算：原程序画在 640*480 的 VGA 全屏上，本平台画在 ui_win_open
     开出的窗口里 ⇒ 按窗口宽度等比缩放（横竖用同一个系数，32*32 的图标方块
     才不会拉变形）。还没开窗时 ui_scr_w() 返回 0，此时保持原坐标 1:1 画。 }

   sw := ui_scr_w ();

   px := Colonne;
   py := Ligne;

   If sw > 0 Then Begin
      px := Colonne * sw Div 640;
      py := Ligne   * sw Div 640;
   End;

   ui_pixel (px, py, argb);

End;

{ Display the given icon file to the given coordinates }

Procedure Show_Ico   (sFileName : String; wColumn, wLine : Word);

Type
   TIconRec = Array[0..1023] of Byte;
   TIcon    = ^TIconRec;

Var
   Color    : Byte;
   fIcon    : File;
   I, J     : Word;
   Icon     : TIcon;

Begin

   Assign (fIcon, sFileName);
   FileMode := 0;                           { Read only }
   Reset  (fIcon, 1);

   GetMem (Icon, 1024);                    { Allocate memory for the icon }

   BlockRead (fIcon, Icon^, 126);

   For I := 0 to 511 Do                     { Process the icon file }
       BEGIN
          BlockRead (fIcon, Color, 1);
          Icon^[I shl 1]       := Color Shr 4;
          Icon^[(I shl 1) + 1] := Color And $0F;
       END;

   Close (fIcon);

   wLine := wLine + icoSize;

   { Display the icon. }

   For J := 31 Downto 0 do
       For I := 31 Downto 0 do
          Put_Pixel (wColumn+I, wLine-J, Icon^[I+J Shl 5]);

   Release (Icon);                          { Release icon memory }

End;

{ Load all icon files present in the specified directory }

PROCEDURE Load_Icons;

VAR
   DosFile    : SearchRec;
   OldX, OldY : Word;
   I          : Byte;
   wPos       : Word;

BEGIN

   OldX := IcoSize; OldY := IcoSize; wPos := 0;

   FindFirst (Paramstr(1)+'\*.Ico', AnyFile, DosFile);

   WHILE DosError = 0 DO
      BEGIN

        { List all icon file and display it }

        Inc (wPos);

        arrNom[wPos] := DosFile.Name;

        Show_Ico (Paramstr(1)+'\'+DosFile.Name, OldX, OldY);

        { Process the screen coordinates for the next icon }

        IF OldX < (640-(IcoSize Shl 1)) THEN
           OldX := OldX + IcoSize
        ELSE
           BEGIN
              OldX := IcoSize;
              OldY := OldY + IcoSize;
           END;

        IF OldY = 14*IcoSize THEN
          BEGIN
             OldX := IcoSize;
             OldY := IcoSize;
             ClrScr;
          END;

        FindNext (DosFile);

   END;

END;

{ Main program }

BEGIN

   GotoXy (0,0);
   TextAttr := 10;
   Write ('IconView (c) AVONTURE Christophe    February 1996');

   { If a parameter is specified, supposed that this parameter is a path
     name and try to display all icon files present in this directory }

   IF NOT (ParamCount = 0) THEN
      BEGIN

         { Initialize graphic mode 640*480 256 colors }

         { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 BIOS int 10h
           置 AX=0012h，把显示切到 VGA 模式 12h（640*480、256 色）—— 也就是
           「给我一块能画像素的画布」。
           本平台没有 BIOS 显示模式，取画布的对应做法是先 ui_win_open 开一个
           绘图窗口（下面 Put_Pixel 的 ui_pixel 就画在那个窗口里，画完要
           ui_present 才上屏）。此处**不代程序开窗**（那属于改主流程），只把
           原来这句汇编注释掉。原汇编保留在下方注释里备查。 }
(*
         Asm
            Mov Ax, 0012h
            Int 10h
         End;
*)

         Load_Icons;

         TextAttr := 15;

         REPEAT UNTIL KEYPRESSED; READKEY;

         { Restore 80*25 255 colors screen mode }

         { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 BIOS int 10h
           置 AX=0003h，把显示切回文本模式 3（80*25 彩色）—— 退图返回文本界面。
           本平台没有 BIOS 显示模式 ⇒ 对应的收尾是 ui_win_close 关掉绘图窗口，
           但那要配上面 ui_win_open 一起改主流程，此处只把原汇编注释掉。
           原汇编保留在下方注释里备查。 }
(*
         Asm
            Mov Ax, 0003h
            Int 10h
         End;
*)

       END
    ELSE
       BEGIN

           { No parameters has been given to the program.  So show a little
             help. }

           WriteLN ('');
           WriteLN ('');
           WriteLN ('');
           WriteLN ('You must specify the path where ICO files are stored.');
           WriteLN ('');
           WriteLN ('For instance, ICO_VIEW C:\WINDOWS\SYSTEM. ');
           WriteLN ('');
           WriteLN ('');

           REPEAT UNTIL KEYPRESSED; READKEY;

       END;

END.
