{
  Retro programming in Borland Turbo Pascal

  Author: Johan Gardhage <johan.gardhage@gmail.com>
}

unit GFX;

INTERFACE

uses crt;

const VGA = $A000;

var vptr : pointer;                       { our virtual screen }
    vaddr : word;                         { segment of our virtual screen}
    screen_offset : array[0..199] of word;

procedure SetMCGA;
procedure SetText;
procedure setborder (col:byte);
procedure cls (where:word);
procedure setupvirtual;
procedure shutdownvirtual;
procedure flip (source,dest:Word);
procedure swapdisplay;
procedure Pal (Col,R,G,B : Byte);
procedure GetPal (Col : Byte; var R,G,B : Byte);
procedure WaitRetrace;
procedure Hline (x1,x2,y:word;col:byte;where:word);
procedure Line (a,b,c,d:integer;col:byte;where:word);
procedure Putpixel (X,Y : Integer; Col : Byte; where:word);
function Getpixel (X,Y : Integer; where:word) :Byte;
Procedure LoadPCX (filename:string;where:word;dopal:Boolean);

IMPLEMENTATION

{ ===== 本平台绘图适配层 ==================================================

  这个单元原来是**直写 VGA 显存**的：一切坐标都是 mode 13h 的 320x200 字节偏移
  （y*320+x），颜色是**调色板索引**，双缓冲靠两个段之间 rep movsd。

  本平台没有那块显存，绘图一律走宿主的 ui_* 接口（保留模式场景 + 0xAARRGGBB 真彩）。
  所以改写只做两件事，别的逻辑一字未动：

    1) 坐标系：setmcga 改用 **ui_win_open_pc** —— 那是宿主给老程序准备的「电脑屏」
       窗口，**坐标系固定为 320x200 且永不重排**，所以原程序的 x/y 可以**原样**
       传给 ui_*，不必自己缩放。
       —— 这比"按 ui_scr_w()/ui_scr_h() 手算缩放"更忠实地保住了原来的像素几何
          （含 320x200 的 4:3 长宽比）：放大交给宿主做，不会出现整数缩放留下的空隙。

    2) 颜色：宿主**没有**"索引色"接口（Lib/c/waycoder_ui.h 里没有 ui_palette
       这类号），所以这里自己维护一张 **256 项调色板表**：Pal/GetPal 读写它，
       绘图时把索引翻成真彩再交给 ui_*。（BASIC 前端做的是同一件事，见其
       _ui_palette_argb；本平台没有第二份调色板实现。）
}

var pal_rgb : array[0..255] of longint;   { 调色板索引 -> 0xAARRGGBB }
    pcxraw : array[0..65534] of byte;     { LoadPCX 读进来的原始 PCX 数据流 }
    pcxbuf : array[0..63999] of byte;     { LoadPCX 解压出来的像素（320x200） }
    pal_ready : boolean;                  { 默认调色板是否已铺开（见 InitPalette） }

procedure InitPalette;
var i : integer;
    v : longint;
begin
  if pal_ready then exit;      { 幂等：重复调用不覆盖用户已经设过的调色板 }
  pal_ready := true;

  { 默认调色板：前 16 项 = VGA/EGA 标准 16 色（mode 13h 开机默认就是这一套 --
    没调过 Pal 的 demo 用索引 10 = 浅绿），16..255 = 一条灰阶斜升。
    分量都存成"6 位 x 4"展开的 8 位，与 Pal 的存储口径一致（GetPal 除 4 精确还回）。

    —— 不写在单元初始化段里，而是由 setmcga 显式调用：本前端**不执行单元的
       初始化段**（实测段里的赋值在程序里读回来是 0），只写在那儿等于没铺。 }
  pal_rgb[0]  := $FF000000;  pal_rgb[1]  := $FF0000A8;
  pal_rgb[2]  := $FF00A800;  pal_rgb[3]  := $FF00A8A8;
  pal_rgb[4]  := $FFA80000;  pal_rgb[5]  := $FFA800A8;
  pal_rgb[6]  := $FFA85400;  pal_rgb[7]  := $FFA8A8A8;
  pal_rgb[8]  := $FF545454;  pal_rgb[9]  := $FF5454FC;
  pal_rgb[10] := $FF54FC54;  pal_rgb[11] := $FF54FCFC;
  pal_rgb[12] := $FFFC5454;  pal_rgb[13] := $FFFC54FC;
  pal_rgb[14] := $FFFCFC54;  pal_rgb[15] := $FFFCFCFC;
  for i := 16 to 255 do begin
    v := ((i - 16) * 63 div 239) * 4;
    pal_rgb[i] := $FF000000 or (v * 65536) or (v * 256) or v;
  end;
end;

{****************************************************************************}

{ 本平台不支持内嵌汇编（Assembler）。原汇编语义：BIOS int 10h, AX=0013h —— 切到
  VGA mode 13h（320x200 / 256 色）。
  已用 ui_win_open_pc 开一扇「固定 320x200 坐标系」的窗口重新实现相同功能
  （= 进入图形模式，之后的绘图都落在它的画布上）；原汇编保留在下方注释里备查。
  参数：标题, 宽, 高, 转屏(0=锁竖屏), 键盘(0=不要屏幕键盘)。 }
procedure setmcga;
begin
  ui_win_open_pc('TP Demo', 320, 200, 0, 0);
  InitPalette;   { 铺默认调色板 -- 不能只靠单元初始化段，见 InitPalette }
end;
(*
asm
  mov    ax, 0013h
  int    10h
end;
*)

{****************************************************************************}

{ 本平台不支持内嵌汇编（Assembler）。原汇编语义：BIOS int 10h, AX=0003h —— 切回
  80x25 文本模式（离开图形模式）。
  已用 ui_win_close 重新实现相同功能（本平台"离开图形模式"就是关上绘图窗口）；
  原汇编保留在下方注释里备查。 }
procedure settext;
begin
  ui_win_close();
end;
(*
asm
  mov    ax, 0003h
  int    10h
end;
*)

{****************************************************************************}

{ 本平台不支持内嵌汇编（Assembler）。原汇编语义：设 VGA 的**边框/overscan 色** --
  先读端口 3DAh 复位属性控制器的翻页触发器，再往 3C0h 写 0x31
  （0x20 = 允许访问调色板 | 0x11 = 边框色寄存器的索引），随后写入颜色。

  本平台**没有等价图元，而且不是"画不出来"、是"没有那块可画的区域"**：画布的
  最外圈就是画面本身，而原程序的边框色落在 320x200 图像**之外**的 overscan 区域，
  现代显示器上那块区域不存在。所以做空实现 —— 不在画面里凭空加一圈框，那会改动
  画面内容（叠加时会盖住 demo 自己的边缘），比不做更糟。
  退路：确实想要一圈边框，自己加一行
    ui_rect(0, 0, 320, 200, pal_rgb[col], 0, 2, 0);
  原汇编保留在下方注释里备查。 }
procedure setborder (col:byte);
begin
end;
(*
asm
  mov    dx, 3dah;
  in      al, dx
  mov    dx, 3c0h;
  mov    al, 11h+32;
  out    dx, al;
  mov    al, col;
  out    dx, al;
end;
*)

{****************************************************************************}

{ 本平台不支持内嵌汇编（Assembler）。原汇编语义：把目标段的 64000 字节（320x200）
  全部写成 0（rep stosd）—— 即把**整屏清成调色板索引 0** 的颜色。
  where 是段地址（vaddr 虚拟屏，或 $A000 显存）。

  已用 ui_clear 重新实现相同功能：本平台没有可寻址的显存段、也没有"离屏缓冲 +
  翻页"这一对，绘图只有一张画布，所以"清目标缓冲区"就落到那张画布上（清成索引 0
  的颜色）。where 无处可用、被忽略 —— 这不影响 swapdisplay：那里"翻页后再清虚拟屏"
  清的正是下一帧要用的画布，语义与原来一致。
  原汇编保留在下方注释里备查。 }
procedure cls (where:word);
begin
  ui_clear(pal_rgb[0]);
end;
(*
asm
  mov    es, [where]
  db     $66, $b9, $80, $3e, $00, $00   {mov     ecx, 16000}
  db     $66, $33, $c0                  {xor     eax, eax}
  db     $66, $33, $ff                  {xor     edi, edi}
  db     $f3, $66, $ab                  {rep     stosd}
end;
*)

{****************************************************************************}

procedure setupvirtual;
begin
  getmem (vptr,64000);
  vaddr := seg (vptr^);
end;

{****************************************************************************}

procedure shutdownvirtual;
begin
  freemem (vptr,64000);
end;

{****************************************************************************}

{ 本平台不支持内嵌汇编（Assembler）。原汇编语义：把 64000 字节（320x200）从 source
  段整块拷到 dest 段（rep movsd）—— 在本程序里就是"把虚拟屏内容送上显存"，
  也就是**显示这一帧**。

  已用 ui_present 重新实现相同功能（把当前场景作为一帧交给宿主渲染上屏）；
  原汇编保留在下方注释里备查。 }
procedure flip(source,dest:Word);
begin
  ui_present();
end;
(*
asm
  push    ds                              {must be here!!}
  mov     es, [Dest]
  mov     ds, [Source]
  db      $66, $b9, $80, $3e, $00, $00    {mov     ecx, 16000}
  db      $66, $33, $f6                   {xor     esi, esi}
  db      $66, $33, $ff                   {xor     edi, edi}
  db      $f3, $66, $a5                   {rep     movsd}
  pop     ds                              {must be here!!}
end;
*)

{****************************************************************************}

procedure swapdisplay;
begin
  waitretrace;
  flip (vaddr,VGA);
  cls (vaddr);
end;

{****************************************************************************}

{ 本平台不支持内嵌汇编（Assembler）。原汇编语义：写 VGA DAC（调色板）的一项 --
  往 3C8h 写索引，随后往 3C9h 依次写 R,G,B（每个分量 6 位）。

  已用等价 Pascal 重新实现相同功能：本平台没有 DAC，也没有"索引色"绘图接口
  （宿主 ui_* 收的是 0xAARRGGBB 真彩）⇒ 写上面那张**软件调色板表**，
  绘图时再把索引翻成真彩。两处按硬件约定截断 —— 不截断就会溢出到相邻分量：
    · 索引取低 8 位：demos 里确有 pal(256,...)（硬件的索引寄存器就是 8 位，会绕回 0）；
    · 每个分量取低 6 位：demos 里有传 >63 的（如 pal(loop1,loop1,loop1,loop1) 到 256）。
  存的是"6 位分量 x 4"展开出的 8 位，GetPal 再除回来，所以"设了再读"精确往返。
  原汇编保留在下方注释里备查。 }
procedure Pal (Col,R,G,B : Byte);
var i : integer;
    v : longint;
begin
  { ⚠ 下标先算进变量：本前端的**数组赋值**在下标写成表达式时
    （如 pal_rgb[Col and 255] := ...）会把**下标本身**存进去、而不是右边那个值
    （实测 pal[1] 被存成 1）。下标用简单变量就正确 —— 别把这两句"化简"回去。 }
  { ⚠ 分量换算**用位移、别用 `* 4 * 65536`**：实测在带大数组的单元里那一项会
    静默算成 0（红色通道全黑），而同样的表达式在没有大数组的小单元里是对的
    —— 数据布局相关的前端 miscompile，位移写法在两种情形下都验过。 }
  i := Col and 255;
  v := $FF000000
      or ((R and 63) shl 18)
      or ((G and 63) shl 10)
      or ((B and 63) shl 2);
  pal_rgb[i] := v;
end;
(*
asm
  mov     dx, 3c8h
  mov     al, [col]
  out     dx, al
  inc     dx
  mov     al, [r]
  out     dx, al
  mov     al, [g]
  out     dx, al
  mov     al, [b]
  out     dx, al
end;
*)

{****************************************************************************}

{ 本平台不支持内嵌汇编（Assembler）。原汇编语义：读回 VGA DAC 的一项 --
  往 3C7h 写索引，随后从 3C9h 依次读 R,G,B（每个分量 6 位）。

  已用等价 Pascal 重新实现相同功能：本平台没有 DAC，改成从上面那张软件调色板表
  读回（与 Pal 互为逆运算，所以"设了再读"能原样拿回），分量按硬件约定取 6 位
  （表里存的是 6 位 x 4，这里除 4 还回去，精确往返）。
  原汇编保留在下方注释里备查。 }
procedure GetPal (Col : Byte; var R,G,B : Byte);
var v : longint;
begin
  v := pal_rgb[Col and 255];
  R := ((v shr 16) and 255) div 4;
  G := ((v shr 8) and 255) div 4;
  B := (v and 255) div 4;
end;
(*
  asm
    mov     dx, 3c7h
    mov     al, [col]
    out     dx, al

    add     dx, 2

    in      al, dx
    mov     [rr], al
    in      al, dx
    mov     [gg], al
    in      al, dx
    mov     [bb], al
  end;
*)

{****************************************************************************}

{ 本平台不支持内嵌汇编（Assembler）。原汇编语义：轮询 VGA 输入状态寄存器 1
  （端口 3DAh）的第 3 位（垂直回扫标志）—— 先等它变 0（离开回扫），再等它变 1
  （进入回扫），即"等到下一场垂直回扫开始"。软件双缓冲靠它避免画面撕裂。

  本平台没有 CRT 电子束、画面由宿主合成，没有垂直回扫可等 ⇒ 空实现。
  （要限帧用 ui_timer_set / ui_tick；撕裂由宿主处理。）
  原汇编保留在下方注释里备查。 }
procedure WaitRetrace;
begin
end;
(*
asm
  mov     dx, 3DAh
@l1:
  in      al, dx
  and     al, 08h
  jnz     @l1
@l2:
  in      al, dx
  and     al, 08h
  jz      @l2
end;
*)

{****************************************************************************}

{ 本平台不支持内嵌汇编（Assembler）。原汇编语义：往显存画**一行里的连续像素** --
  地址 = screen_offset[y]（= y*320）+ x1，然后用 rep stosw 一次写两个像素、
  奇数个时先用一个 stosb 补上。画的像素范围是 x1 .. x2-1（**右端不含**，共 x2-x1 个）。

  已用 ui_rect 填一条 1 行高的实心矩形重新实现相同功能：宽度取 x2-x1，
  "右端不含"的语义因此精确保住（若改用 ui_line 会多画一个像素）。
  原汇编保留在下方注释里备查。 }
procedure Hline (x1,x2,y:word;col:byte;where:word);
begin
  if x2 <= x1 then exit;
  ui_rect(x1, y, x2 - x1, 1, pal_rgb[col], 1, 0, 0);
end;
(*
asm
  mov     es, [where]
  mov     bx, [y]
  add     bx, bx
  mov     di, word ptr [screen_offset + bx]
  add     di, [x1]
  mov     al, col
  mov     ah, al
  mov     cx, x2
  sub     cx, x1
  shr     cx, 1
  jnc     @start
  stosb
@Start:
  rep     stosw
end;
*)

{****************************************************************************}

procedure Line(a,b,c,d:integer;col:byte;where:word);
  { This draws a solid line from a,b to c,d in colour col }
  function sgn(a:real):integer;
  begin
       if a>0 then sgn:=+1;
       if a<0 then sgn:=-1;
       if a=0 then sgn:=0;
  end;
var i,s,d1x,d1y,d2x,d2y,u,v,m,n:integer;
begin
     u:= c - a;
     v:= d - b;
     d1x:= SGN(u);
     d1y:= SGN(v);
     d2x:= SGN(u);
     d2y:= 0;
     m:= ABS(u);
     n := ABS(v);
     IF NOT (M>N) then
     begin
          d2x := 0 ;
          d2y := SGN(v);
          m := ABS(v);
          n := ABS(u);
     end;
     s := m shr 1;
     FOR i := 0 TO m DO
     begin
          putpixel(a,b,col,where);
          s := s + n;
          IF not (s<m) THEN
          begin
               s := s - m;
               a:= a + d1x;
               b := b + d1y;
          end
          ELSE
          begin
               a := a + d2x;
               b := b + d2y;
          end;
     end;
end;

{****************************************************************************}

{ 本平台不支持内嵌汇编（Assembler）。原汇编语义：往显存写**一个像素** --
  地址 = screen_offset[y]（= y*320）+ x，写入一个字节（调色板索引）。

  已用 ui_pixel 重新实现相同功能（320x200 的坐标与窗口坐标系 1:1，直传；
  颜色由本单元的调色板表把索引翻成真彩）。
  原汇编保留在下方注释里备查。 }
procedure Putpixel (X,Y : Integer; Col : Byte; where:word);
begin
  if (X < 0) or (X > 319) or (Y < 0) or (Y > 199) then exit;
  ui_pixel(X, Y, pal_rgb[Col]);
end;
(*
asm
  mov     es, [where]
  mov     bx, [y]
  add     bx, bx
  mov     di, word ptr [screen_offset + bx]
  add     di, [x]
  mov     al, [col]
  mov     es:[di],al
end;
*)

{****************************************************************************}

{ 本平台不支持内嵌汇编（Assembler）。原汇编语义：从显存读**一个字节** = 该像素的
  **调色板索引**（地址算法同 Putpixel）。

  已用 ui_get_pixel（号 #587）+ 反查调色板表重新实现相同功能：ui_get_pixel 返回
  该点的 0xRRGGBB（**注意不带 alpha**），拿它到软件调色板表里反查索引后返回，
  与原来"返回索引"的语义一致；表里找不到（宿主自己画的颜色、不在表内）返回 0，
  不假装。
  原汇编保留在下方注释里备查。

  代价提醒：ui_get_pixel 每次调用宿主都要**重光栅化整幅场景**（场景是保留模式的），
  实测约 0.4ms/次 ⇒ 别放进密集大循环（320 像素一行就要 128ms）。 }
Function Getpixel (X,Y : Integer; where:word):byte;
var i : integer;
    c : longint;
begin
  if (X < 0) or (X > 319) or (Y < 0) or (Y > 199) then begin Getpixel := 0; exit; end;
  c := ui_get_pixel(X, Y) and $FFFFFF;
  Getpixel := 0;
  for i := 0 to 255 do
    if (pal_rgb[i] and $FFFFFF) = c then begin Getpixel := i; exit; end;
end;
(*
asm
  mov     es, [where]
  mov     bx, [y]
  add     bx, bx
  mov     di, word ptr [screen_offset + bx]
  add     di, [x]
  mov     al, es:[di]
end;
*)

{****************************************************************************}

Procedure LoadPCX (filename:string;where:word;dopal:Boolean);
VAR f:file;
    res,loop1:word;
    pallette: Array[0..767] Of Byte;
    di,p,b,cnt,v : integer;
BEGIN
  assign (f,filename);
  reset (f,1);
  if dopal then BEGIN
    Seek(f,FileSize(f)-768);
    BlockRead(f,pallette,768);
    For loop1:=0 To 255 Do
      pal (loop1,pallette[loop1*3] shr 2,pallette[loop1*3+1] shr 2,pallette[loop1*3+2] shr 2);
  END;
  seek (f,128);

  blockread (f,pcxraw,65535,res);

  { 本平台不支持内嵌汇编（Assembler）。原汇编语义：**PCX 扫描线 RLE 解压** --
    逐字节读 PCX 数据流：字节高两位为 11b 时是"重复包"（低 6 位 = 重复次数，
    下一字节 = 要写的值），否则该字节就是字面量；解出来的像素连续写进目标段，
    直到写满 64000 字节（320x200）为止。

    已用等价的 Pascal 循环重新实现同一套解压（含"重复 0 次不写"与"写满 64000
    即停"两条边界）；原汇编保留在下方注释里备查。

    一处**做不到**的地方（不假装等价）：原汇编的落笔目标是**段寄存器**给出的地址
    （调用方传 where），而本平台是平坦内存模型、没有段:偏移，where 无法解引用。
    所以解出来的像素落在本单元的 pcxbuf 里。真要在画布上显示一张图，本平台的正路是
    ui_image(x,y,path,w,h)（直接按路径贴图）。
    另外这里加了两个边界检查（原汇编没有）：数据流读完就停、写满 64000 就停 --
    防的是畸形 PCX 把 65535 字节的读缓冲读穿。 }
(*
  asm
    push ds
    mov  ax,where
    mov  es,ax
    xor  di,di
    xor  ch,ch
    lds  si,temp
@Loop1 :
    lodsb
    mov  bl,al
    and  bl,$c0
    cmp  bl,$c0
    jne  @Single

    mov  cl,al
    and  cl,$3f
    lodsb
    rep  stosb
    jmp  @Fin
@Single :
    stosb
@Fin :
    cmp  di,63999
    jbe  @Loop1
    pop  ds
  end;
*)
  di := 0;
  p := 0;
  while di <= 63999 do begin
    if p > 65534 then break;
    b := pcxraw[p];
    p := p + 1;
    if (b and $C0) = $C0 then begin
      cnt := b and $3F;
      if p <= 65534 then begin
        v := pcxraw[p];
        p := p + 1;
      end else v := 0;
      while (cnt > 0) and (di <= 63999) do begin
        pcxbuf[di] := v;
        di := di + 1;
        cnt := cnt - 1;
      end;
    end else begin
      pcxbuf[di] := b;
      di := di + 1;
    end;
  end;
  close (f);
END;

{****************************************************************************}

var loop1:integer;
begin
  For loop1 := 0 to 199 do
    screen_offset[loop1] := loop1 * 320;

  { ⚠ 本前端**不执行单元的初始化段**（实测：段里赋的值在程序里读回来是 0），
    所以默认调色板真正的入口是 setmcga 里那次 InitPalette 调用。
    这里再调一次只是为了让"会执行初始化段"的前端也照样正确（InitPalette 幂等）。 }
  InitPalette;
end.
