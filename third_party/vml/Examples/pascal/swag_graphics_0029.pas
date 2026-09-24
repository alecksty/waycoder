{$A-,B-,D+,E-,F-,G+,I-,L+,N-,O-,R-,S-,V-,X+} {TP 6.0 & 286 required!}
Unit x320x240;

{
 Sean Palmer, 1993
 released to the Public Domain
 in tweaked modes, each latch/bit plane contains the entire 8-bit pixel.
 the sequencer map mask determines which plane (pixel) to update, and, when
 reading, the read map select reg determines which plane (pixel) to read.
 almost exactly opposite from regular vga 16-color modes which is why I never
 could get my routines to work For BOTH modes. 8)

  # = source screen pixel
  Normal 16-color         Tweaked 256-color

      Bit Mask                Bit Mask
      76543210                33333333
 Map  76543210           Map  22222222
 Mask 76543210           Mask 11111111
      76543210                00000000

  Functional equivalents
      Bit Mask        =       Seq Map Mask
      Seq Map Mask    =       Bit Mask
}


Interface

Var
  color : Byte;

Const
 xRes    = 320;
 yRes    = 240;   {displayed screen size}
 xMax    = xRes - 1;
 yMax    = yRes - 1;
 xMid    = xMax div 2;
 yMid    = yMax div 2;
 vxRes   = 512;
 vyRes   = $40000 div vxRes; {virtual screen size}
 nColors = 256;
 tsx : Byte = 8;
 tsy : Byte = 8;  {tile size}


Procedure plot(x, y : Integer);
Function  scrn(x, y : Integer) : Byte;

Procedure hLin(x, x2, y : Integer);
Procedure vLin(x, y, y2 : Integer);
Procedure rect(x, y, x2, y2 : Integer);
Procedure pane(x, y, x2, y2 : Integer);

Procedure line(x, y, x2, y2 : Integer);
Procedure oval(xc, yc, a, b : Integer);
Procedure disk(xc, yc, a, b : Integer);
Procedure fill(x, y : Integer);

Procedure putTile(x, y : Integer; p : Pointer);
Procedure overTile(x, y : Integer; p : Pointer);
Procedure putChar(x, y : Integer; p : Word);

Procedure setColor(color, r, g, b : Byte);
{rgb vals are from 0-63}
Function  getColor(color : Byte) : LongInt;
{returns $00rrggbb format}
Procedure setPalette(color : Byte; num : Word; Var rgb);
{rgb is list of 3-Byte rgb vals}
Procedure getPalette(color : Byte; num : Word; Var rgb);

Procedure clearGraph;
Procedure setWriteMode(f : Byte);
Procedure waitRetrace;
Procedure setWindow(x, y : Integer);

{XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX}

Implementation

Const
  vSeg     = $A000;        {video segment}
  vxBytes  = vxRes div 4;  {Bytes per virtual scan line}
  seqPort  = $3C4;   {Sequencer}
  gcPort   = $3CE;    {Graphics Controller}
  attrPort = $3C0;   {attribute Controller}

  tableReadIndex    = $3C7;
  tableWriteIndex   = $3C8;
  tableDataRegister = $3C9;

  CrtcRegLen   = 10;
  CrtcRegTable : Array [1..CrtcRegLen] of Word =
    ($0D06, $3E07, $4109, $EA10, $AC11, $DF12, $0014, $E715, $0616, $E317);



Var
  CrtcPort   : Word;  {Crt controller}
  oldMode    : Byte;
  ExitSave   : Pointer;
  input1Port : Word;  {Crtc Input Status Reg #1=CrtcPort+6}
  fillVal    : Byte;

Type
 tRGB = Record
   r, g, b : Byte;
 end;

Var
 { ⚠ 本平台没有 VGA 硬件：这个量是原汇编里「硬件卷屏窗口」的软件替身（见 setWindow）。 }
 WinX, WinY : Integer;

{ 逻辑坐标（本单元是 320×240 屏）→ 本平台画布坐标。
  原汇编直接往 VGA 显存写（段 $A000、512 字节/行的虚拟屏），
  本平台画布的实际尺寸由 ui_scr_w()/ui_scr_h() 给出，故按比例换算。 }
function SX(x : Integer) : Integer;
begin
  SX := (x + WinX) * ui_scr_w() div xRes;
end;

function SY(y : Integer) : Integer;
begin
  SY := (y + WinY) * ui_scr_h() div yRes;
end;

{ 索引色（0..255）→ 本平台的 0xAARRGGBB 真彩色。
  原汇编的 setColor/setPalette 是往 VGA DAC 端口写 6 位/通道的寄存器，
  本平台没有端口 I/O、也没有调色板接口，故改用一张**固定**的默认调色板：
  与 Graphbegin 里 setpalette(0,256,p) 装的那张表同形 ——
  0..215 是 6×6×6 色立方（每级 (n*63) div 5），216..255 是 40 级灰阶。
  6 位 → 8 位通道按 ×4 换算（63→252；63 级满量程取整误差 3/255）。 }
function ArgbOf(idx : Byte) : LongInt;
var r, g, b, l : Integer;
begin
  if idx < 216 then begin
    r := (idx div 36) mod 6;
    g := (idx div 6) mod 6;
    b := idx mod 6;
    r := (r * 63) div 5;
    g := (g * 63) div 5;
    b := (b * 63) div 5;
  end else begin
    l := ((idx - 216) * 63) div 39;
    r := l;
    g := l;
    b := l;
  end;
  ArgbOf := -16777216 + ((r * 4) shl 16) + ((g * 4) shl 8) + (b * 4);
end;


{XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX}

Procedure clearGraph;
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：把段 $A000 的显存整屏
    （320×240、4 平面布局，共 $8000 个字）全部填成全局变量 color —— 即「用当前颜色清屏」。
    已改用本平台的 ui_clear 重新实现（颜色经 ArgbOf 由索引色换算成真彩色）；
    原汇编保留在下方注释里备查。 }
  ui_clear(ArgbOf(color));
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Procedure clearGraph; Assembler;
Asm
  mov ax, vSeg
  mov es, ax
  mov dx, seqPort
  mov ax, $0F02
  out dx, ax {enable whole map mask}
  xor di, di
  mov cx, $8000 {screen size in Words}
  cld
  mov al, color
  mov ah, al
  repz stosw {clear screen}
end;
────────────────────────────── *)

Procedure setWriteMode(f : Byte);
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：往 VGA 图形控制器端口 3CEh 的 03h 号寄存器
    （数据旋转/功能选择）写 f shl 3，选光栅操作模式（0=copy、1=AND、8=OR、24=XOR）。
    这是**纯端口 I/O 的硬件光栅操作开关**；本平台的 ui_* 只有直接覆盖一种画法、没有对应的
    光栅操作接口，无法实现，故以空过程代替；原汇编保留在下方注释里备查。
    退路：若将来 ui_* 提供「画法/合成模式」参数，此处应转接到它。 }
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Procedure setWriteMode(f : Byte); Assembler;
Asm {copy/and/or/xor modes}
  mov ah, f
  shl ah, 3
  mov al, 3
  mov dx, gcPort
  out dx, ax {Function select reg}
end;
────────────────────────────── *)

Procedure waitRetrace;
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：读 CRTC 输入状态寄存器（端口 CrtcPort+6）
    的第 3 位，先在「非垂直回扫」期等待、再到「垂直回扫」期返回 —— 把程序同步到显示器的
    垂直回扫以避免画面撕裂。本平台没有这个端口、也不知道帧何时提交，无法实现等回扫；
    **注意它不是 ui_present**（ui_present 是「提交这一帧」，在这里调它会把画了一半的画面提交出去），
    故以空过程代替；原汇编保留在下方注释里备查。 }
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Procedure waitRetrace; Assembler;
Asm
  mov  dx, CrtcPort
  add  dx, 6 {find Crt status reg (input port #1)}
 @L1:
  in   al, dx
  test al, 8
  jnz  @L1;  {wait For no v retrace}
 @L2:
  in   al, dx
  test al, 8
  jz   @L2 {wait For v retrace}
 end;
────────────────────────────── *)


{
 Since a virtual screen can be larger than the actual screen, scrolling is
 possible.  This routine sets the upper left corner of the screen to the
 specified pixel. Make sure 0 <= x <= vxRes - xRes, 0 <= y <= vyRes - yRes
}
Procedure setWindow(x, y : Integer);
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：算出「虚拟屏左上角」落在 (x,y) 时的
    显存偏移（vxBytes*y + x div 4），先等非回扫期、再用 CLI 关中断往 CRTC 的 0Ch/0Dh
    （显示起始地址高/低字节）写进去，最后设像素平移寄存器 —— 这是**硬件卷屏**：
    显存内容不动，只改从哪儿开始显示。
    本平台没有 CRTC 端口，也没有「比显示区大的虚拟显存」，改为**软件窗口原点**：
    把 (x,y) 记进 WinX/WinY，由本单元所有绘图/读点函数（见 SX/SY）加上这个偏移。
    ⚠ 语义差异：原版是「已画好的内容跟着窗口滑动」，这里是「后续绘制整体平移」——
    对「卷屏后重画」的用法等价，对「画完再平移视口」的用法不等价。 }
  WinX := x;
  WinY := y;
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Procedure setWindow(x, y : Integer); Assembler;
Asm
  mov  ax, vxBytes
  mul  y
  mov  bx, x
  mov  cl, bl
  shr  bx, 2
  add  bx, ax     {bx=Ofs of upper left corner}
  mov  dx, input1Port
 @L:
  in   al, dx
  test al, 8
  jnz  @L  {wait For no v retrace}
  sub  dx, 6  {CrtC port}
  mov  al, $D
  mov  ah, bl
  cli {these values are sampled at start of retrace}
  out  dx, ax  {lo Byte of display start addr}
  dec  al
  mov  ah, bh
  out  dx, ax    {hi Byte}
  sti
  add  dx, 6
 @L2:
  in   al, dx
  test al, 8
  jz   @L2  {wait For v retrace}
  {this also resets Attrib flip/flop}
  mov  dx, attrPort
  mov  al, $33
  out  dx, al   {Select Pixel Pan Register}
  and  cl, 3
  mov  al, cl
  shl  al, 1
  out  dx, al   {Shift is For 256 Color Mode}
end;
────────────────────────────── *)

{XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX}

Procedure plot(x, y : Integer);
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：往段 $A000 的显存里、按「每字节 4 像素、
    4 平面各存一个像素」的微调 320×240 布局，用序列器图掩码只打开 (x and 3) 号平面，
    再把全局 color 写进该字节 —— 也就是**画一个像素**。
    已改用本平台的 ui_pixel 重新实现（坐标经 SX/SY 换算到画布）；
    原汇编保留在下方注释里备查。 }
  ui_pixel(SX(x), SY(y), ArgbOf(color));
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Procedure plot(x, y : Integer); Assembler;
Asm
  mov   ax, vSeg
  mov   es, ax
  mov   di, x
  mov   cx, di
  shr   di, 2
  mov   ax, vxBytes
  mul   y
  add   di, ax
  mov   ax, $0102
  and   cl, 3
  shl   ah, cl
  mov   dx, seqPort
  out   dx, ax {set bit mask}
  mov   al, color
  stosb
end;
────────────────────────────── *)

Function scrn(x, y : Integer) : Byte;
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：先往图形控制器的「读图选择」寄存器
    写 (x and 3)，再读该字节 —— 取出 (x,y) 处像素的颜色**索引**。
    已改用本平台的 ui_get_pixel 重新实现；原汇编保留在下方注释里备查。
    ⚠ 两处语义差异（本单元只把返回值用于「颜色是否相同」的比较，见 fill/lineFill）：
      ① ui_get_pixel 返回的是 0xAARRGGBB 真彩色，而本函数签名是 Byte，只能取低 8 位
         （即蓝色通道）——**可能把两个蓝通道相同、红绿不同的颜色判成同色**；
      ② 画布外的点由宿主给值，本函数不做区分。
    ⚠ 性能：ui_get_pixel 每次调用宿主都要把整幅场景光栅化一次，**不要放进密集循环**；
      本单元的 fill（洪泛填充）恰好是密集调用 scrn 的地方，在大画布上会明显变慢。 }
  scrn := Byte(ui_get_pixel(SX(x), SY(y)) and $FF);
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Function scrn(x, y : Integer) : Byte; Assembler;
Asm
  mov ax, vSeg
  mov es, ax
  mov di, x
  mov cx, di
  shr di, 2
  mov ax, vxBytes
  mul y
  add di, ax
  and cl, 3
  mov ah, cl
  mov al, 4
  mov dx, gcPort
  out dx, ax      {Read Map Select register}
  mov al, es:[di]  {get the whole plane}
end;
────────────────────────────── *)

Procedure hLin(x, x2, y : Integer);
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：在微调 320×240 布局里画一条**水平线**
    （第 y 行的 x..x2）——按 (x and 3)/(x2 and 3) 算出左右端点的平面掩码，
    左端用序列器图掩码写第一字节、中间整字节用 $0F 一次写 4 个像素、右端再写最后一字节。
    已改用本平台的 ui_line 重新实现（线宽 1）；原汇编保留在下方注释里备查。 }
  ui_line(SX(x), SY(y), SX(x2), SY(y), ArgbOf(color), 1);
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Procedure hLin(x, x2, y : Integer); Assembler;
Asm
  mov   ax, vSeg
  mov   es, ax
  cld
  mov   ax, vxBytes
  mul   y
  mov   di, ax {base of scan line}
  mov   bx, x
  mov   cl, bl
  shr   bx, 2
  mov   dx, x2
  mov   ch, dl
  shr   dx, 2
  and   cx, $0303
  sub   dx, bx     {width in Bytes}
  add   di, bx     {offset into video buffer}
  mov   ax, $FF02
  shl   ah, cl
  and   ah, $0F {left edge mask}
  mov   cl, ch
  mov   bh, $F1
  rol   bh, cl
  and   bh, $0F {right edge mask}
  mov   cx, dx
  or    cx, cx
  jnz   @LEFT
  and   ah, bh                  {combine left & right bitmasks}
 @LEFT:
  mov   dx, seqPort
  out   dx, ax
  inc   dx
  mov   al, color
  stosb
  jcxz  @EXIT
  dec   cx
  jcxz  @RIGHT
  mov   al, $0F
  out   dx, al     {skipped if cx=0,1}
  mov   al, color
  repz  stosb   {fill middle Bytes}
 @RIGHT:
  mov   al, bh
  out   dx, al       {skipped if cx=0}
  mov   al, color
  stosb
 @EXIT:
end;
────────────────────────────── *)

Procedure vLin(x, y, y2 : Integer);
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：从 (x,y) 到 (x,y2) 逐行写同一个平面字节，
    画一条**竖线**（每行先读回原字节再写，是为了让未选中的平面保持不变）。
    已改用本平台的 ui_line 重新实现；原汇编保留在下方注释里备查。 }
  ui_line(SX(x), SY(y), SX(x), SY(y2), ArgbOf(color), 1);
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Procedure vLin(x, y, y2 : Integer); Assembler;
Asm
  mov ax, vSeg
  mov es, ax
  cld
  mov di, x
  mov cx, di
  shr di, 2
  mov ax, vxBytes
  mul y
  add di, ax
  mov ax, $102
  and cl, 3
  shl ah, cl
  mov dx, seqPort
  out dx, ax
  mov cx, y2
  sub cx, y
  inc cx
  mov al, color
 @DOLINE:
  mov bl, es:[di]
  stosb
  add di, vxBytes-1
  loop @DOLINE
end;
────────────────────────────── *)

Procedure rect(x, y, x2, y2 : Integer);
Var
  i : Word;
begin
  hlin(x, pred(x2), y);
  hlin(succ(x), x2, y2);
  vlin(x, succ(y), y2);
  vlin(x2, y, pred(y2));
end;

Procedure pane(x, y, x2, y2 : Integer);
Var
  i : Word;
begin
  For i := y2 downto y do
    hlin(x, x2, i);
end;

Procedure line(x, y, x2, y2:Integer);
Var
  d, dx, dy,
  ai, bi, xi, yi : Integer;
begin
  if(x < x2) then
  begin
    xi := 1;
    dx := x2 - x;
  end
  else
  begin
    xi := -1;
    dx := x - x2;
  end;
  if (y < y2) then
  begin
    yi := 1;
    dy := y2 - y;
  end
  else
  begin
    yi := -1;
    dy := y - y2;
  end;
  plot(x, y);
  if dx > dy then
  begin
    ai := (dy - dx) * 2;
    bi := dy * 2;
    d  := bi - dx;
    Repeat
      if (d >= 0) then
      begin
        inc(y, yi);
        inc(d, ai);
      end
      else
        inc(d, bi);
      inc(x, xi);
      plot(x, y);
    Until (x = x2);
  end
  else
  begin
    ai := (dx - dy) * 2;
    bi := dx * 2;
    d  := bi - dy;
    Repeat
      if (d >= 0) then
      begin
        inc(x, xi);
        inc(d, ai);
      end
      else
        inc(d, bi);
      inc(y, yi);
      plot(x, y);
    Until (y = y2);
  end;
end;

Procedure oval(xc, yc, a, b : Integer);
Var
  x, y      : Integer;
  aa, aa2,
  bb, bb2,
  d, dx, dy : LongInt;
begin
  x := 0;
  y := b;
  aa := LongInt(a) * a;
  aa2 := 2 * aa;
  bb := LongInt(b) * b;
  bb2 := 2 * bb;
  d := bb - aa * b + aa div 4;
  dx := 0;
  dy := aa2 * b;
  plot(xc, yc - y);
  plot(xc, yc + y);
  plot(xc - a, yc);
  plot(xc + a, yc);
  While (dx < dy) do
  begin
    if(d > 0) then
    begin
      dec(y);
      dec(dy, aa2);
      dec(d, dy);
    end;
    inc(x);
    inc(dx, bb2);
    inc(d, bb + dx);
    plot(xc + x, yc + y);
    plot(xc - x, yc + y);
    plot(xc + x, yc - y);
    plot(xc - x, yc - y);
  end;

  inc(d, (3 * (aa - bb) div 2 - (dx + dy)) div 2);

  While (y > 0) do
  begin
    if (d < 0) then
    begin
      inc(x);
      inc(dx, bb2);
      inc(d, bb + dx);
    end;
    dec(y);
    dec(dy, aa2);
    inc(d, aa - dy);
    plot(xc + x, yc + y);
    plot(xc - x, yc + y);
    plot(xc + x, yc - y);
    plot(xc - x, yc - y);
  end;
end;

Procedure disk(xc, yc, a, b:Integer);
Var
  x, y      : Integer;
  aa, aa2,
  bb, bb2,
  d, dx, dy : LongInt;
begin
  x   := 0;
  y   := b;
  aa  := LongInt(a) * a;
  aa2 := 2 * aa;
  bb  := LongInt(b) * b;
  bb2 := 2 * bb;
  d   := bb - aa * b + aa div 4;
  dx  := 0;
  dy  := aa2 * b;

  vLin(xc, yc - y, yc + y);

  While (dx < dy) do
  begin
    if (d > 0) then
    begin
      dec(y);
      dec(dy, aa2);
      dec(d, dy);
    end;
    inc(x);
    inc(dx, bb2);
    inc(d, bb + dx);
    vLin(xc - x, yc - y, yc + y);
    vLin(xc + x, yc - y, yc + y);
  end;

  inc(d, (3 * (aa - bb) div 2 - (dx + dy)) div 2);

  While (y >= 0) do
  begin
    if (d < 0) then
    begin
      inc(x);
      inc(dx, bb2);
      inc(d, bb + dx);
      vLin(xc - x, yc - y, yc + y);
      vLin(xc + x, yc - y, yc + y);
    end;
    dec(y);
    dec(dy, aa2);
    inc(d, aa - dy);
  end;
end;

{This routine only called by fill}
Function lineFill(x, y, d, prevXL, prevXR : Integer) : Integer;
Var
  xl, xr, i : Integer;
Label
  _1, _2, _3;
begin
  xl := x;
  xr := x;

  Repeat
    dec(xl);
  Until (scrn(xl, y) <> fillVal) or (xl < 0);

  inc(xl);

  Repeat
    inc(xr);
  Until (scrn(xr, y) <> fillVal) or (xr > xMax);

  dec(xr);
  hLin(xl, xr, y);
  inc(y, d);

  if Word(y) <= yMax then
  For x := xl to xr do
    if (scrn(x, y) = fillVal) then
    begin
      x := lineFill(x, y, d, xl, xr);
      if Word(x) > xr then
        Goto _1;
    end;

  _1 :

  dec(y, d + d);
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：neg d —— 把局部变量 d 取反
    （纯计算，没有任何硬件依赖）。已用等价的 Pascal 语句重新实现；
    原汇编保留在下方注释里备查。 }
  d := -d;
(* ── 原汇编（已停用，仅供参考）──────────────────────────────
  Asm
    neg d;
  end;
────────────────────────────── *)
  if Word(y) <= yMax then
  begin
  For x := xl to prevXL do
    if (scrn(x, y) = fillVal) then
    begin
      i := lineFill(x, y, d, xl, xr);
      if Word(x) > prevXL then
        Goto _2;
    end;

    _2 :

    for x := prevXR to xr do
      if (scrn(x, y) = fillVal) then
      begin
        i := lineFill(x, y, d, xl, xr);
        if Word(x) > xr then
          Goto _3;
      end;

      _3 :

      end;

  lineFill := xr;
end;

Procedure fill(x, y : Integer);
begin
  fillVal := scrn(x, y);
  if fillVal <> color then
    lineFill(x, y, 1, x, x);
end;


{XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX}

Procedure putTile(x, y : Integer; p : Pointer);
Var
  q : ^Byte;
  row, col, k : Integer;
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：把 p 指向的 8×8 图块**原样覆盖**贴到 (x,y)。
    源数据是「微调模式」的顺序：每个目标字节地址连续 4 个字节依次属于平面 0..3
    （源码里 movsb 每 4 次才 inc di、图掩码 ah 循环 1→2→4→8 就是这个意思），
    所以 1 行 8 个像素 = 8 个字节（2 个字节地址 × 4 平面）。
    已改用本平台的 ui_pixel 逐点重画（坐标经 SX/SY、颜色经 ArgbOf 换算）；
    原汇编保留在下方注释里备查。 }
  For row := 0 to tsy - 1 do
    For col := 0 to tsx - 1 do begin
      k := (col div 4) * 4 + (col mod 4);
      q := p;
      Inc(q, row * tsx + k);
      ui_pixel(SX(x + col), SY(y + row), ArgbOf(q^));
    end;
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Procedure putTile(x, y : Integer; p : Pointer); Assembler;
Asm
  push  ds
  lds   si, p
  mov   ax, vSeg
  mov   es, ax
  mov   di, x
  mov   cx, di
  shr   di, 2
  mov   ax, vxBytes
  mul   y
  add   di, ax
  mov   ax, $102
  and   cl, 3
  shl   ah, cl      {make bit mask}
  mov   dx, seqPort
  mov   bh, tsy
 @DOLINE:
  mov   cl, tsx
  xor   ch, ch
  push  ax
  push  di    {save starting bit mask}
 @LOOP:
  {mov al, 2}
  out   dx, ax
  shl   ah, 1       {give it some time to respond}
  mov   bl, es:[di]
  movsb
  dec   di
  test  ah, $10
  jz    @SAMEByte
  mov   ah, 1
  inc   di
 @SAMEByte:
  loop  @LOOP
  pop   di
  add   di, vxBytes
  pop   ax {start of next line}
  dec   bh
  jnz   @DOLINE
  pop   ds
end;
────────────────────────────── *)

Procedure overTile(x, y : Integer; p : Pointer);
Var
  q : ^Byte;
  row, col, k, src, dst : Integer;
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：同 putTile 的 8×8 贴图，但**带两个键**：
    ① 源像素为 0 时跳过（透明，or al,al / jz @SKIP）；
    ② 目标像素的当前值 >= $C0 时跳过（原码用 GC 读图选择读回目标字节，cmp bl,$C0 / jae @SKIP）。
    已改用本平台的 ui_get_pixel + ui_pixel 重新实现；原汇编保留在下方注释里备查。
    ⚠ 目标值取自 ui_get_pixel 的低 8 位（同 scrn 的语义差异）。
    ⚠ 性能：每个像素一次 ui_get_pixel（宿主每次都要光栅化整幅场景），一块 8×8 就是 64 次，偏慢。 }
  For row := 0 to tsy - 1 do
    For col := 0 to tsx - 1 do begin
      k := (col div 4) * 4 + (col mod 4);
      q := p;
      Inc(q, row * tsx + k);
      src := q^;
      If src <> 0 then begin
        dst := ui_get_pixel(SX(x + col), SY(y + row)) and $FF;
        If dst < $C0 then
          ui_pixel(SX(x + col), SY(y + row), ArgbOf(src));
      end;
    end;
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Procedure overTile(x, y : Integer; p : Pointer); Assembler;
Asm
  push  ds
  lds   si, p
  mov   ax, vSeg
  mov   es, ax
  mov   di, x
  mov   cx, di
  shr   di, 2
  mov   ax, vxBytes
  mul   y
  add   di, ax
  mov   ax, $102
  and   cl, 3
  shl   ah, cl      {make bit mask}
  mov   bh, tsy
  mov   dx, seqPort
 @DOLINE:
  mov   ch, tsx
  push  ax
  push  di    {save starting bit mask}
 @LOOP:
  mov   al, 2
  mov   dx, seqPort
  out   dx, ax
  shl   ah, 1
  xchg  ah, cl
  mov   al, 4
  mov   dl, gcPort and $FF
  out   dx, ax
  xchg  ah, cl
  inc   cl
  and   cl, 3
  lodsb
  or    al, al
  jz    @SKIP
  mov   bl, es:[di]
  cmp   bl, $C0
  jae   @SKIP
  stosb
  dec   di
 @SKIP:
  test  ah, $10
  jz    @SAMEByte
  mov   ah, 1
  inc   di
 @SAMEByte:
  dec   ch
  jnz   @LOOP
  pop   di
  add   di, vxBytes
  pop   ax {start of next line}
  dec   bh
  jnz   @DOLINE
  pop   ds
end;
────────────────────────────── *)

{won't handle Chars wider than 1 Byte}
Procedure putChar(x, y : Integer; p : Word);
Var
  row, col, bits : Integer;
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：把地址 p 处的 8 字节 1bpp 点阵
    （每字节 8 个像素、高位在左）用全局 color 画成 8×8 字符
    （原注释即写明「不支持宽度超过 1 字节的字符」）。
    已改用本平台的 ui_pixel 重新实现；原汇编保留在下方注释里备查。
    ⚠ 地址口径差异：原汇编的 p 是 **DS 段内偏移**（mov si, p），本平台没有段式内存，
    这里用 mem[p + row] 按本平台的 mem[] 语义取字节 —— 调用方必须传一个当前平台
    有效的地址（例如某个 Byte 数组的首地址），传裸偏移量没有意义。 }
  For row := 0 to tsy - 1 do begin
    bits := mem[p + row];
    For col := 0 to tsx - 1 do begin
      If (bits and $80) <> 0 Then
        ui_pixel(SX(x + col), SY(y + row), ArgbOf(color));
      bits := bits shl 1;
    end;
  end;
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Procedure putChar(x, y : Integer; p : Word); Assembler;
Asm
  mov   si, p  {offset of Char in DS}
  mov   ax, vSeg
  mov   es, ax
  mov   di, x
  mov   cx, di
  shr   di, 2
  mov   ax, vxBytes
  mul   y
  add   di, ax
  mov   ax, $0102
  and   cl, 3
  shl   ah, cl      {make bit mask}
  mov   dx, seqPort
  mov   cl, tsy
  xor   ch, ch
 @DOLINE:
  mov   bl, [si]
  inc   si
  push  ax
  push  di    {save starting bit mask}
 @LOOP:
  mov   al, 2
  out   dx, ax
  shl   ah, 1
  shl   bl, 1
  jnc   @SKIP
  mov   al, color
  mov   es:[di], al
 @SKIP:
  test  ah, $10
  jz    @SAMEByte
  mov   ah, 1
  inc   di
 @SAMEByte:
  or    bl, bl
  jnz   @LOOP
  pop   di
  add   di, vxBytes
  pop   ax {start of next line}
  loop  @DOLINE
end;
────────────────────────────── *)

{XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX}

Procedure setColor(color, r, g, b : Byte);
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：往 VGA DAC 端口 3C8h 写颜色索引，
    再往 3C9h 依次写 r/g/b —— 设置索引 color 的调色板颜色（每通道 6 位，0..63）。
    本平台没有端口 I/O，也没有调色板接口（无 ui_palette），无法实现，故以空过程代替；
    本单元的颜色改由固定的默认调色板给出（见 ArgbOf）；
    原汇编保留在下方注释里备查。退路：若将来宿主提供 ui_palette，此处应转接到它。 }
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Procedure setColor(color, r, g, b : Byte); Assembler;
Asm {set DAC color}
  mov  dx, tableWriteIndex
  mov  al, color
  out  dx, al
  inc  dx
  mov  al, r
  out  dx, al
  mov  al, g
  out  dx, al
  mov  al, b
  out  dx, al
end; {Write index now points to next color}
────────────────────────────── *)

Function getColor(color : Byte) : LongInt;
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：往 3C7h 写颜色索引、再从 3C9h 连读三次
    得到该索引的 r/g/b，拼成 $00rrggbb 返回（每通道 6 位）。
    本平台没有端口 I/O，无法读硬件 DAC；但本单元的颜色就是固定的默认调色板
    （见 ArgbOf），所以直接返回那张表里同索引的颜色，语义自洽
    （6 位/通道 → 8 位的换算与 ArgbOf 一致）；原汇编保留在下方注释里备查。 }
  getColor := ArgbOf(color) and $00FFFFFF;
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Function getColor(color : Byte) : LongInt; Assembler;
Asm {get DAC color}
  mov  dx, tableReadIndex
  mov  al, color
  out  dx, al
  add  dx, 2
  cld
  xor  bh, bh
  in   al, dx
  mov  bl, al
  in   al, dx
  mov  ah, al
  in   al, dx
  mov  dx, bx
end; {read index now points to next color}
────────────────────────────── *)

Procedure setPalette(color : Byte; num : Word; Var rgb);
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：把 rgb 指向的 num 组 3 字节 r/g/b
    依次写进 VGA DAC（先往 3C8h 写起始颜色索引）—— 批量设置调色板。
    本平台没有端口 I/O、也没有调色板接口，无法实现，故以空过程代替；
    本单元的颜色由固定的默认调色板给出（见 ArgbOf），因此这次调用不产生效果；
    原汇编保留在下方注释里备查。
    另：本函数的 Var rgb 是无类型 var 形参，本前端也不支持（报「期望 :」）。 }
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Procedure setPalette(color : Byte; num : Word; Var rgb); Assembler;
Asm
  mov   cx, num
  jcxz  @X
  mov   ax, cx
  shl   cx, 1
  add   cx, ax {mul by 3}
  push  ds
  lds   si, rgb
  cld
  mov   dx, tableWriteIndex
  mov   al, color
  out   dx, al
  inc   dx
 @L:
  lodsb
  out   dx, al
  loop  @L
  pop   ds
 @X:
end;
────────────────────────────── *)

Procedure getPalette(color : Byte; num : Word; Var rgb);
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：先往 3C7h 写起始索引、再从 3C9h 连读 num*3 个字节，
    把 num 组 r/g/b 写进 rgb 指向的缓冲区 —— 批量读调色板。
    本平台没有端口 I/O，也没有调色板接口；而且 Var rgb 是无类型 var 形参，
    本前端也不支持（报「期望 :」），无法实现，故以空过程代替；
    原汇编保留在下方注释里备查。 }
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Procedure getPalette(color : Byte; num : Word; Var rgb); Assembler;
Asm
  mov   cx, num
  jcxz  @X
  mov   ax, cx
  shl   cx, 1
  add   cx, ax {mul by 3}
  les   di, rgb
  cld
  mov   dx, tableReadIndex
  mov   al, color
  out   dx, al
  add   dx, 2
 @L:
  in    al, dx
  stosb
  loop  @L
 @X:
end;
────────────────────────────── *)

{XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX}

Function vgaPresent : Boolean;
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：先用 BIOS INT 10h/AH=0Fh 存下当前显示模式，
    再用 INT 10h/AX=1A00h 问「有没有 VGA BIOS」——AL=1Ah 且 BL>=7 才算有 VGA。
    本平台没有 BIOS，也没有 VGA 硬件，无从探测；
    但这个函数在本单元里的用途是「能不能进 256 色图形模式」，而本平台**有绘图画布**
    （ui_* 直接可用），故返回 True —— 否则本单元的初始化段会立刻
    Writeln('VGA required.') + halt(1)，整个单元都不可用。
    （若希望严格按「探测不到就返回未安装」处理，把这里改成 False 即可。）
    原汇编保留在下方注释里备查。 }
  vgaPresent := True;
end;

(* ── 原汇编（已停用，仅供参考）──────────────────────────────
Function vgaPresent : Boolean; Assembler;
Asm
  mov ah, $F
  int $10
  mov oldMode, al  { save old Gr mode}
  mov ax, $1A00
  int $10          { check For VGA}
  cmp al, $1A
  jne @ERR         { no VGA Bios}
  cmp bl, 7
  jb @ERR          { is VGA or better?}
  cmp bl, $FF
  jnz @OK
 @ERR:
  xor al, al
  jmp @EXIT
 @OK:
  mov al, 1
 @EXIT:
end;
────────────────────────────── *)

Procedure Graphbegin;
Var
  p     : Array [0..255] of tRGB;
  i, j,
  k, l  : Byte;
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：BIOS INT 10h/AX=0013h 切到
    VGA 模式 13h（320×240、256 色）—— 也就是「进入图形模式」。
    本平台的「图形模式」= 开一扇绘图窗口（画布由 ui_* 提供），故改用 ui_win_open 重新实现
    （先问 ui_scr_w/ui_scr_h 拿画布尺寸，与 examples/pascal/catch.pas 的写法一致）；
    原汇编保留在下方注释里备查。 }
  ui_win_open('x320x240', ui_scr_w(), ui_scr_h());
(* ── 原汇编（已停用，仅供参考）──────────────────────────────
  Asm
    mov ax, $0013
    int $10
  end;   {set BIOS mode}
────────────────────────────── *)

  l := 0;
  For i := 0 to 5 do
    For j := 0 to 5 do
      For k := 0 to 5 do
      With p[l] do
      begin
        r := (i * 63) div 5;
        g := (j * 63) div 5;
        b := (k * 63) div 5;
        inc(l);
      end;

  For i := 216 to 255 do
  With p[i] do
  begin
    l := ((i - 216) * 63) div 39;
    r := l;
    g := l;
    b := l;
  end;

  setpalette(0, 256, p);
  color := 0;

  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：逐条配置 VGA 寄存器以进入「微调
    320×240 模式」——往序列器 3C4h 写 $0604（关链 4）与 $0100（同步复位）、往杂项输出
    端口 3C2h 写 $E3（25MHz 点时钟、480 行）、重启序列器 $0300；再从 CrtcRegTable 用
    repz outsw 把 10 个 CRTC 寄存器一次写完、用 $7F 清掉 CR11 的写保护；
    最后按 vxBytes 半宽设 CRTC 偏移寄存器。
    这些都是**纯 VGA 硬件端口编程**，只为上面那种显存布局服务；
    本平台的画布由 ui_* 管理，既没有这些端口也不需要这种配置，故整段停用；
    原汇编保留在下方注释里备查。 }
(* ── 原汇编（已停用，仅供参考）──────────────────────────────
  Asm
   mov  dx, seqPort
   mov  ax, $0604
   out  dx, ax            { disable chain 4}
   mov  ax, $0100
   out  dx, ax            { synchronous reset asserted}
   dec  dx
   dec  dx
   mov  al, $E3
   out  dx, al            { misc output port at $3C2}
                          { use 25mHz dot clock,  480 lines}
   inc  dx
   inc  dx
   mov  ax, $0300
   out  dx, ax            { restart sequencer}
   mov  dx, CrtcPort
   mov  al, $11
   out  dx, al            { select cr11}
   inc  dx
   in   al, dx
   and  al, $7F
   out  dx, al
   dec  dx                { remove Write protect from cr0-cr7}
   mov  si, offset CrtcRegTable
   mov  cx, CrtcRegLen
   repz outsw             { set Crtc data}
   mov  ax, vxBytes
   shr  ax, 1             { Words per scan line}
   mov  ah, al
   mov  al, $13
   out  dx, ax            { set CrtC offset reg}
  end;
────────────────────────────── *)

  clearGraph;
end;

Procedure Graphend; Far;
begin
  ExitProc := exitSave;
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：BIOS INT 10h/AH=00h 把显示模式恢复成
    进入图形模式前存下的 oldMode —— 退出时回到文本模式。
    对应的本平台动作是关掉绘图窗口，故改用 ui_win_close 重新实现；
    原汇编保留在下方注释里备查。 }
  ui_win_close();
(* ── 原汇编（已停用，仅供参考）──────────────────────────────
  Asm
    mov al, oldMode
    mov ah, 0
    int $10
  end;
────────────────────────────── *)
end;

begin
  CrtcPort   := memw[$40 : $63];
  input1Port := CrtcPort + 6;
  if vgaPresent then
  begin
    ExitSave := exitProc;
    ExitProc := @Graphend;
    Graphbegin;
  end
  else
  begin
    Writeln(^G + 'VGA required.');
    halt(1);
  end;
end.
