{
  Retro programming in Borland Turbo Pascal

  Flat shaded cube on fire with z light source demo. Drawn with quads.

  Author: Johan Gardhage <johan.gardhage@gmail.com>
}

program demonstration;

uses crt,gfx,vector;

const border:boolean=false;
      xst=1; yst=1; zst=1;

{$I cube.vec}

{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：对 320×200 的虚拟屏幕逐像素做
  「火焰渐隐」—— 取当前像素与「下一行左、下两行、下一行右」三个邻像素按字节相加后
  `shr ax,2`（除 4）取平均，非零再减一（`dec al`），写回当前像素。底部画上去的亮色
  因此逐帧向上传递并衰减，形成燃烧渐隐。
  已用 ui_get_pixel / ui_pixel 重新实现同一套邻域运算（本平台没有可直写的显存，
  画布尺寸也不固定 ⇒ 逻辑坐标仍按原 VGA 320×200，落到画布时按 ui_scr_w/ui_scr_h 换算；
  邻域用线性偏移 di+319/di+640/di+321 表达，x=0 时 di+319 在原汇编里落到上一行末列，
  这里照同样方式回绕）。原汇编保留在下方注释里备查。 }
procedure firefadeout;
var
  x, y, c, lx : integer;
  sx, sy, sw, sh : integer;
begin
  sw := ui_scr_w();
  sh := ui_scr_h();
  if (sw <= 0) or (sh <= 0) then exit;
  { 原汇编外层循环到 cx=198 为止（读 di+640 会越过 64000 字节的屏幕缓冲），
    这里收敛到 0..197，避免越界读取。 }
  for y := 0 to 197 do
  begin
    sy := (y * sh) div 200;
    for x := 0 to 319 do
    begin
      sx := (x * sw) div 320;
      lx := x - 1;
      if lx < 0 then lx := 319;                 { di+319 在 x=0 时绕到上一行末列 }
      c := ( ui_get_pixel (sx, sy)
           + ui_get_pixel ((lx * sw) div 320, ((y + 1) * sh) div 200)
           + ui_get_pixel (sx, ((y + 2) * sh) div 200)
           + ui_get_pixel (((x + 1) * sw) div 320, ((y + 1) * sh) div 200) ) div 4;
      if c <> 0 then c := c - 1;                { 非零才减一（原汇编的 jz @skip） }
      ui_pixel (sx, sy, c);
    end;
  end;
end;

(*
procedure firefadeout; assembler;
asm
  mov   es, vaddr
  xor   di, di

  xor   dx, dx
  xor   cx, cx          { cx = y }
 @yloop:
  xor   bx, bx          { bx = x }
 @xloop:
  mov   dl, es:[di]     { dl = get color }
  mov   ax, dx          { ax = color }
  mov   dl, es:[di+319]
  add   ax, dx
  mov   dl, es:[di+640]
  add   ax, dx
  mov   dl, es:[di+321]
  add   ax, dx
  shr   ax, 2           { ax = average color }
  jz    @skip
  dec   al              { if col > 0 => dec col }
 @skip:
  stosb                 { store new col }
  inc   bx              { next x }
  cmp   bx, 320
  jne   @xloop
  inc   cx              { next y }
  cmp   cx, 199
  jne   @yloop
end;
*)

procedure mainloop;
var loop1:integer;
begin
  cls(vaddr);
  repeat
    waitretrace;
    flip(vaddr,VGA);
    if border then setborder(5);

    for loop1:=1 to maxpoints do begin
      x:=points[loop1,1]; y:=points[loop1,2]; z:=points[loop1,3];
      rotate(x,y,z,phix,phiy,phiz,newx,newy,newz);
      conv3dto2d(newx,newy,newz,xp[loop1],yp[loop1]);
      zp[loop1]:=newz;
    end;

    phix:=(phix+xst) and (maxdegrees-1);
    phiy:=(phiy+yst) and (maxdegrees-1);
    phiz:=(phiz+zst) and (maxdegrees-1);

    for loop1:=1 to maxpolygons do begin
      polyz[loop1]:=(zp[polygons[loop1,1]]+zp[polygons[loop1,2]]+zp[polygons[loop1,3]]+zp[polygons[loop1,4]]) div 4;
      pind[loop1]:=loop1;
    end;

    quicksort(maxpolygons);

    for loop1:=maxpolygons-maxvisible to maxpolygons do
      draw_quad(xp[polygons[pind[loop1],1]],yp[polygons[pind[loop1],1]],
                xp[polygons[pind[loop1],2]],yp[polygons[pind[loop1],2]],
                xp[polygons[pind[loop1],3]],yp[polygons[pind[loop1],3]],
                xp[polygons[pind[loop1],4]],yp[polygons[pind[loop1],4]],polyz[loop1]+200);

    firefadeout;

    if border then setborder(0);
  until keypressed;
end;

var i:integer;
begin
  setmcga;
  setupvirtual;
  for i:=0 to 63 do pal(i,0,0,0);
  for i:=0 to 63 do pal(64+i,0,0,i shr 1);
  for i:=0 to 63 do pal(128+i,i,i shr 1,31-i shr 1);
  for i:=0 to 63 do pal(192+i,63,32+i shr 1,0);
  mainloop;
  shutdownvirtual;
  settext;
end.
