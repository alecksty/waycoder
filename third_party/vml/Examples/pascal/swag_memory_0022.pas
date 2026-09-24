{
FRANCOIS THUNUS

> Would it be possible to throw a [Ctrl-Alt-Del] into the keyboard buffer,
> causing Smartdrv to Write its data and warm boot the computer? if so, any
> ideal how a person would do this?

trap keyboard info
if ctr-alt-del then begin
            check For smrtdrv
            if smrtdrv then flush cache
            reboot
            end;

Flush cache: (was posted here but since it is more than a month old, i guess
it's ok to repost ?):
}

Unit SfeCache;
{

Max Maischein                                  Sunday,  7.03.1993
2:249/6.17                                         Frankfurt, GER

This  Unit  implements   an   automatic   flush   For   installed
Write-behind caches like SmartDrive and PC-Cache. It's  based  on
cache detection code by Norbert Igl, I added the calls  to  flush
the buffers. The stuff is only tested For SMARTDRV.EXE, the  rest
relies on Norbert and the INTERRUP.LST from Ralf Brown.

Al says : "Save early, save often !"

The Unit exports one  Procedure,  FlushCache,  this  flushes  the
first cache found. It  could  be  good  to  flush  everything  on
Program termination, since users are likely to switch  off  their
computers directly upon Exit from the Program.

This piece of code is donated to the public domain, but I request
that, if you use this code, you mention me in the DOCs somewhere.
                                                           -max
}
Interface

Implementation

Uses
  Dos;

Const
  AktCache : Byte = 0;

Type
  FlushProc = Procedure;

Var
  FlushCache : FlushProc;

Function SmartDrv_exe : Boolean;
Var
  Found : Boolean;
begin
  Found := False;
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 DOS 多路中断 int 2Fh 的
    4A10h 功能（SMARTDRV 驻留检测）—— 返回 AX=0BABEh 表示装了写后缓存。
    本平台没有 DOS 中断、也没有 SMARTDRV 这类写后缓存 ⇒ 以空实现代替：
    探测结果 = 未安装（Found 保持 False）。原汇编保留在下方注释里备查。 }
(*
  Asm
    push    bp
    stc
    mov     ax, 4A10h
    xor     bx, bx
    int     2Fh
    pop     bp
    jc      @NoSmartDrive
    cmp     ax, 0BABEh
    jne     @NoSmartDrive
    mov     Found, True
   @NoSmartDrive:
  end;
*)
  SmartDrv_exe := Found;
end;

Function SmartDrv_sys : Boolean;
Var
  F  : File;
  B  : Array[0..$27] of Byte; { return Buffer }
  OK : Boolean;
Const
  S = SizeOf( B );
begin
  SmartDrv_sys := False;
  OK := False;
  { -------Check For SmartDrv.SYS----------- }
  Assign(f,'SMARTAAR');
  {$I-}
  Reset( F );
  {$I+}
  if IoResult <> 0 then
    Exit; { No SmartDrv }
  { 这里原是 FillChar( B, Sizeof(B), 0 )：B 是下面那个 int 21h 4402h 的**返回缓冲区**，
    只为那次中断服务。中断已按上面的说明改成桩，缓冲区再无用途 ⇒ 一并去掉
    （本平台的 Pascal 库里也没有 FillChar）。 }
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 DOS 中断 int 21h 的
    4402h 功能（取设备控制信息 IOCTL）—— 用 TextRec(F).Handle 拿 SMARTAAR 设备的
    句柄，把 40 字节结果读进 B；成功（进位标志清）就把 OK 置 1。
    本平台没有 DOS 中断、也没有 SMARTDRV.SYS 设备（上面的 Reset('SMARTAAR') 本就
    打不开）⇒ 以空实现代替：探测结果 = 未安装（OK 保持 False）。
    原汇编保留在下方注释里备查。 }
(*
  Asm
    push    ds
    mov     ax, 4402h
    mov     bx, TextRec( F ).Handle
    mov     cx, S
    mov     dx, seg B
    mov     ds, dx
    mov     dx, offset B
    int     21h
    jc      @Error
    mov     OK, 1
   @Error:
    pop     ds
  end;
*)
  close(f);
  SmartDrv_sys := OK;
end;

Function CompaqPro : Boolean;
Var
  OK : Boolean;
begin
  CompaqPro := False;
  OK := False;
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 BIOS 键盘中断 int 16h 的
    0F400h 功能（Compaq 特有的「取缓存状态」）—— AH=0E2h 且 AL 在 1..2 之间
    表示装了 Compaq 的写后缓存。本平台没有 BIOS 中断、也没有 Compaq 缓存 ⇒
    以空实现代替：探测结果 = 未安装（OK 保持 False）。原汇编保留在下方注释里备查。 }
(*
  Asm
    mov     ax, 0F400h
    int     16h
    cmp     ah, 0E2h
    jne     @NoCache
    or      al, al
    je      @NoCache
    cmp     al, 2
    ja      @NoCache
    mov     OK, 1
   @NoCache:
  end;
*)
  CompaqPro := OK;
end;

Function PC6 : Boolean;   { PCTools v6, v5 }
Var
  OK : Boolean;
begin
  PC6 := False;
  OK := False;
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 BIOS 键盘中断 int 16h 的
    0FFA5h 功能（PC Tools v6 缓存检测）—— CH=0 表示装了 PC-Cache v6。
    本平台没有 BIOS 中断、也没有 PC Tools ⇒ 以空实现代替：探测结果 = 未安装
    （OK 保持 False）。原汇编保留在下方注释里备查。 }
(*
  Asm
    mov     ax, 0FFA5h
    mov     cx, 01111h
    int     16h
    or      ch, ch
    jne     @NoCache
    mov     OK, 1
   @NoCache:
  end;
*)
  PC6 := OK;
end;

Function PC5 : Boolean;
Var
  OK : Boolean;
begin
  PC5 := False;
  OK := False;
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 DOS 中断 int 21h 的
    2BFFh 功能（未公开的 PC Tools v5 缓存检测）—— CX='CX'，AL=0 表示装了缓存。
    本平台没有 DOS 中断、也没有 PC Tools ⇒ 以空实现代替：探测结果 = 未安装
    （OK 保持 False）。原汇编保留在下方注释里备查。 }
(*
  Asm
    mov     ax, 02BFFh
    mov     cx, 'CX';
    int     21h
    or      al, al
    jne     @NoCache
    mov     ok, 1
   @NoCache:
  end;
*)
  PC5 := OK;
end;

Function HyperDsk : Boolean;   { 4.20+ ... }
Var
  OK : Boolean;
begin
  Hyperdsk := False;
  OK := False;
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 DOS 多路中断 int 2Fh 的
    0DF00h 功能（HyperDisk 4.20+ 检测）—— AL=0FFh 且 CX=05948h（'HY'）表示装了缓存。
    本平台没有 DOS 中断、也没有 HyperDisk ⇒ 以空实现代替：探测结果 = 未安装
    （OK 保持 False）。原汇编保留在下方注释里备查。 }
(*
  Asm
    mov     ax, 0DF00h
    mov     bx, 'DH'
    int     02Fh
    cmp     al, 0FFh
    jne     @NoCache
    cmp     cx, 05948h
    jne     @NoCache
    mov     OK, 1
   @NoCache:
  end;
*)
  HyperDSK := OK;
end;

Function QCache : Boolean;
Var
  OK : Boolean;
begin
  QCache := False;
  OK := False;
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 BIOS 磁盘中断 int 13h 的
    27h 功能（未公开的 QCache 检测）—— 返回 BX≠0 表示装了 QCache。
    本平台没有 BIOS 中断、也没有 QCache ⇒ 以空实现代替：探测结果 = 未安装
    （OK 保持 False）。原汇编保留在下方注释里备查。 }
(*
  Asm
    mov     ah, 027h
    xor     bx, bx
    int     013h
    or      bx, bx
    je      @NoCache
    mov     OK, 1
   @NoCache:
  end;
*)
  QCache := OK;
end;

Procedure FlushSD_sys;
Var
  F : File;
  B : Byte;
begin
  Assign(F, 'SMARTAAR');
  Reset(F);
  B := 0;
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 DOS 中断 int 21h 的
    4403h 功能（写设备控制信息 IOCTL）—— 让 SMARTDRV.SYS 立刻把缓存写盘。
    本平台没有 DOS 中断、也没有 SMARTDRV.SYS ⇒ 以空实现代替（刷新无事可做，
    也本来就没有缓存可刷）。原汇编保留在下方注释里备查。
    另：Turbo Pascal 的 `Far;`（段间调用约定）本平台无对应语义，一并去掉；
    过程名与参数签名不变。 }
(*
  Asm
    push    ds
    mov     ax, 04403h
    mov     bx, FileRec(F).Handle
    mov     cx, 1
    int     21h
    pop     ds
  end;
*)
end;

{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 DOS 多路中断 int 2Fh 的
  4A10h 功能，BX=1 = 让 SMARTDRV.EXE（驻留版）把缓存写盘。本平台没有 DOS 中断、
  也没有 SMARTDRV.EXE ⇒ 以空过程代替（空函数 = 无事可做）。
  另：`Far;` 段间调用约定本平台无对应语义，一并去掉；过程名与参数签名不变。
  原汇编保留在下方注释里备查。 }
Procedure FlushSD_exe;
begin
end;

(*
Procedure FlushSD_exe; Far; Assembler;
Asm
  mov  ax, 04A10h
  mov  bx, 1
  int  2Fh
end;
*)

{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 BIOS 键盘中断 int 16h 的
  0F5A5h 功能（PC Tools v6 缓存写盘），CX=-1 表示刷新全部。本平台没有 BIOS 中断、
  也没有 PC-Cache ⇒ 以空过程代替。`Far;` 同上去掉；原汇编保留在下方注释里备查。 }
Procedure FlushPC6;
begin
end;

(*
Procedure FlushPC6; Far; Assembler;
Asm
  mov  ax, 0F5A5h
  mov  cx, -1
  int  16h
end;
*)

{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 BIOS 磁盘中断 int 13h 的
  A1h 功能（PC Tools v5 的缓存写盘），SI=04358h 指 'CX' 签名。本平台没有 BIOS
  中断、也没有 PC Tools ⇒ 以空过程代替。`Far;` 同上去掉；
  原汇编保留在下方注释里备查。 }
Procedure FlushPC5;
begin
end;

(*
Procedure FlushPC5; Far; Assembler;
Asm
  mov  ah, 0A1h
  mov  si, 04358h
  int  13h
end;
*)

Procedure FlushNoCache;
begin
end;

begin
  if SmartDrv_exe then
    FlushCache := FlushSD_exe
  else
  if SmartDrv_sys then
    FlushCache := FlushSD_sys
  else
  if PC6 then
    FlushCache := FlushPC6
  else
  if PC5 then
    FlushCache := FlushPC5
  else
    FlushCache := FlushNoCache;

  FlushCache;
end.
