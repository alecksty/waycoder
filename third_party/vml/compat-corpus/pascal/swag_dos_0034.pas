{
From: JON JASIUNAS
Subj: Share Multi-tasking
}

{**************************
 *     SHARE.PAS v1.0     *
 *                        *
 *  General purpose file  *
 *    sharing routines    *
 **************************

1992-93 HyperDrive Software
Released into the public domain.}

{$S-,R-,D-}
{$IFOPT O+}
  {$F+}
{$ENDIF}

unit Share;

{\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\}
                                   interface
{/////////////////////////////////////////////////////////////////////////////}

const
  MaxLockRetries : Byte = 10;

  NormalMode = $02; { ---- 0010 }
  ReadOnly   = $00; { ---- 0000 }
  WriteOnly  = $01; { ---- 0001 }
  ReadWrite  = $02; { ---- 0010 }
  DenyAll    = $10; { 0001 ---- }
  DenyWrite  = $20; { 0010 ---- }
  DenyRead   = $30; { 0011 ---- }
  DenyNone   = $40; { 0100 ---- }
  NoInherit  = $70; { 1000 ---- }

type
  Taskers = (NoTasker, DesqView, DoubleDOS, Windows, OS2, NetWare);

var
  MultiTasking: Boolean;
  MultiTasker : Taskers;
  VideoSeg    : Word;
  VideoOfs    : Word;

procedure SetFileMode(Mode: Word);
  {- Set filemode for typed/untyped files }

procedure ResetFileMode;
  {- Reset filemode to ReadWrite (02h) }

procedure LockFile(var F);
  {- Lock file F }

procedure UnLockFile(var F);
  {- Unlock file F }

procedure LockBytes(var F;  Start, Bytes: LongInt);
  {- Lock Bytes bytes of file F, starting with Start }

procedure UnLockBytes(var F;  Start, Bytes: LongInt);
  {- Unlock Bytes bytes of file F, starting with Start }

procedure LockRecords(var F;  Start, Records: LongInt);
  {- Lock Records records of file F, starting with Start }

procedure UnLockRecords(var F;  Start, Records: LongInt);
  {- Unlock Records records of file F, starting with Start }

function  TimeOut: Boolean;
  {- Check for LockRetry timeout }

procedure TimeOutReset;
  {- Reset internal LockRetry counter }

function  InDos: Boolean;
  {- Is DOS busy? }

procedure GiveTimeSlice;
  {- Give up remaining CPU time slice }

procedure BeginCrit;
  {- Enter critical region }

procedure EndCrit;
  {- End critical region }

{\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\}
                                 implementation
{/////////////////////////////////////////////////////////////////////////////}

uses
  Dos;

var
  InDosFlag: ^Word;
  LockRetry: Byte;

{=============================================================================}

procedure FLock(Handle: Word; Pos, Len: LongInt);
begin
  { ⚠ 本平台不支持内嵌汇编（Inline 机器码）。原汇编语义：调用 DOS
    INT 21h/AX=5C00h（FLOCK 的「锁定」子功能），BX=文件句柄、ES:DX=起始位置、
    ES:SI=长度，锁定文件的一段。本平台没有 DOS 中断与段式内存传参，
    无法实现，故以空过程代替；原汇编（Inline 机器码）保留在下方注释里备查。 }
end;

(* ── 原汇编（已停用，仅供参考）────────────────────────────────
procedure FLock(Handle: Word; Pos, Len: LongInt);
Inline(
  $B8/$00/$5C/    {  mov   AX,$5C00        ;DOS FLOCK, Lock subfunction}
  $8B/$5E/$04/    {  mov   BX,[BP + 04]    ;Place file handle in Bx register}
  $C4/$56/$06/    {  les   DX,[BP + 06]    ;Load position in ES:DX}
  $8C/$C1/        {  mov   CX,ES           ;Move ES pointer to CX register}
  $C4/$7E/$08/    {  les   DI,[BP + 08]    ;Load length in ES:DI}
  $8C/$C6/        {  mov   SI,ES           ;Move ES pointer to SI register}
  $CD/$21);       {  int   $21             ;Call DOS}
─────────────────────────────────────────────────────────────── *)

{-----------------------------------------------------------------------------}

procedure FUnlock(Handle: Word; Pos, Len: LongInt);
begin
  { ⚠ 本平台不支持内嵌汇编（Inline 机器码）。原汇编语义：调用 DOS
    INT 21h/AX=5C01h（FLOCK 的「解锁」子功能），BX=文件句柄、ES:DX=起始位置、
    ES:SI=长度，解除文件上的一段锁。本平台没有 DOS 中断与段式内存传参，
    无法实现，故以空过程代替；原汇编（Inline 机器码）保留在下方注释里备查。 }
end;

(* ── 原汇编（已停用，仅供参考）────────────────────────────────
procedure FUnlock(Handle: Word; Pos, Len: LongInt);
Inline(
  $B8/$01/$5C/    {  mov   AX,$5C01        ;DOS FLOCK, Unlock subfunction}
  $8B/$5E/$04/    {  mov   BX,[BP + 04]    ;Place file handle in Bx register}
  $C4/$56/$06/    {  les   DX,[BP + 06]    ;Load position in ES:DX}
  $8C/$C1/        {  mov   CX,ES           ;Move ES pointer to CX register}
  $C4/$7E/$08/    {  les   DI,[BP + 08]    ;Load length in ES:DI}
  $8C/$C6/        {  mov   SI,ES           ;Move ES pointer to SI register}
  $CD/$21);       {  int   $21             ;Call DOS}
─────────────────────────────────────────────────────────────── *)

{=============================================================================}

procedure SetFileMode(Mode: Word);
begin
  FileMode := Mode;
end;    { SetFileMode }

{-----------------------------------------------------------------------------}

procedure ResetFileMode;
begin
  FileMode := NormalMode;
end;    { ResetFileMode }

{-----------------------------------------------------------------------------}

procedure LockFile(var F);
begin
  If not MultiTasking then
    Exit;

  While InDos do
    GiveTimeSlice;

  FLock(FileRec(F).Handle, 0, FileSize(File(F)));
end;    { LockFile }

{-----------------------------------------------------------------------------}

procedure UnLockFile(var F);
begin
  If not MultiTasking then
    Exit;

  While InDos do
    GiveTimeSlice;

  FLock(FileRec(F).Handle, 0, FileSize(File(F)));
end;    { UnLockFile }

{-----------------------------------------------------------------------------}

procedure LockBytes(var F;  Start, Bytes: LongInt);
begin
  If not MultiTasking then
    Exit;

  While InDos do
    GiveTimeSlice;

  FLock(FileRec(F).Handle, Start, Bytes);
end;    { LockBytes }

{-----------------------------------------------------------------------------}

procedure UnLockBytes(var F;  Start, Bytes: LongInt);
begin
  If not MultiTasking then
    Exit;

  While InDos do
    GiveTimeSlice;

  FLock(FileRec(F).Handle, Start, Bytes);
end;    { UnLockBytes }

{-----------------------------------------------------------------------------}

procedure LockRecords(var F;  Start, Records: LongInt);
begin
  If not MultiTasking then
    Exit;

  While InDos do
    GiveTimeSlice;

  FLock(FileRec(F).Handle, Start * FileRec(F).RecSize, Records * FileRec(F).Rec
end;    { LockBytes }

{-----------------------------------------------------------------------------}

procedure UnLockRecords(var F;  Start, Records: LongInt);
begin
  If not MultiTasking then
    Exit;

  While InDos do
    GiveTimeSlice;

  FLock(FileRec(F).Handle, Start * FileRec(F).RecSize, Records * FileRec(F).Rec
end;    { UnLockBytes }

{-----------------------------------------------------------------------------}

function  TimeOut: Boolean;
begin
  GiveTimeSlice;
  TimeOut := True;

  If MultiTasking and (LockRetry < MaxLockRetries) then
    begin
      TimeOut := False;
      Inc(LockRetry);
    end;  { If }
end;    { TimeOut }

{-----------------------------------------------------------------------------}

procedure TimeOutReset;
begin
  LockRetry := 0;
end;    { TimeOutReset }

{-----------------------------------------------------------------------------}

function  InDos: Boolean;
begin   { InDos }
  InDos := InDosFlag^ > 0;
end;    { InDos }

{-----------------------------------------------------------------------------}

procedure GiveTimeSlice;
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：按全局 MultiTasker
    发对应的「让出时间片」中断——无多任务器用 INT 28h（DOS 空闲中断），
    DESQview INT 15h/AX=1000h，DoubleDOS INT 21h/AX=EE01h，Windows/OS2 INT 2Fh/AX=1680h，
    NetWare INT 7Ah。本平台没有 DOS/BIOS 中断与 8086 寄存器，也没有多任务器，
    无法实现，故以空过程代替；原汇编保留在下方注释里备查。 }
end;

(* ── 原汇编（已停用，仅供参考）────────────────────────────────
procedure GiveTimeSlice;  ASSEMBLER;
asm     { GiveTimeSlice }
  cmp   MultiTasker, DesqView
  je    @DVwait
  cmp   MultiTasker, DoubleDOS
  je    @DoubleDOSwait
  cmp   MultiTasker, Windows
  je    @WinOS2wait
  cmp   MultiTasker, OS2
  je    @WinOS2wait
  cmp   MultiTasker, NetWare
  je    @Netwarewait

@Doswait:
  int   $28
  jmp   @WaitDone

@DVwait:
  mov   AX,$1000
  int   $15
  jmp   @WaitDone

@DoubleDOSwait:
  mov   AX,$EE01
  int   $21
  jmp   @WaitDone

@WinOS2wait:
  mov   AX,$1680
  int   $2F
  jmp   @WaitDone

@Netwarewait:
  mov   BX,$000A
  int   $7A
  jmp   @WaitDone

@WaitDone:
end;    { TimeSlice }
─────────────────────────────────────────────────────────────── *)

{----------------------------------------------------------------------------}

procedure BeginCrit;
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：按全局 MultiTasker 进入临界区——
    DESQview INT 15h/AX=101Bh，DoubleDOS INT 21h/AX=EA00h，Windows INT 2Fh/AX=1681h。
    本平台没有 DOS/BIOS 中断与 8086 寄存器，也没有多任务器，无法实现，
    故以空过程代替；原汇编保留在下方注释里备查。 }
end;

(* ── 原汇编（已停用，仅供参考）────────────────────────────────
procedure BeginCrit;  ASSEMBLER;
asm     { BeginCrit }
  cmp   MultiTasker, DesqView
  je    @DVCrit
  cmp   MultiTasker, DoubleDOS
  je    @DoubleDOSCrit
  cmp   MultiTasker, Windows
  je    @WinCrit
  jmp   @EndCrit

@DVCrit:
  mov   AX,$101B
  int   $15
  jmp   @EndCrit

@DoubleDOSCrit:
  mov   AX,$EA00
  int   $21
  jmp   @EndCrit

@WinCrit:
  mov   AX,$1681
  int   $2F
  jmp   @EndCrit

@EndCrit:
end;    { BeginCrit }
─────────────────────────────────────────────────────────────── *)

{----------------------------------------------------------------------------}

procedure EndCrit;
begin
  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：按全局 MultiTasker 离开临界区——
    DESQview INT 15h/AX=101Ch，DoubleDOS INT 21h/AX=EB00h，Windows INT 2Fh/AX=1682h。
    本平台没有 DOS/BIOS 中断与 8086 寄存器，也没有多任务器，无法实现，
    故以空过程代替；原汇编保留在下方注释里备查。 }
end;

(* ── 原汇编（已停用，仅供参考）────────────────────────────────
procedure EndCrit;  ASSEMBLER;
asm     { EndCrit }
  cmp   MultiTasker, DesqView
  je    @DVCrit
  cmp   MultiTasker, DoubleDOS
  je    @DoubleDOSCrit
  cmp   MultiTasker, Windows
  je    @WinCrit
  jmp   @EndCrit

@DVCrit:
  mov   AX,$101C
  int   $15
  jmp   @EndCrit

@DoubleDOSCrit:
  mov   AX,$EB00
  int   $21
  jmp   @EndCrit

@WinCrit:
  mov   AX,$1682
  int   $2F
  jmp   @EndCrit

@EndCrit:
end;    { EndCrit }
─────────────────────────────────────────────────────────────── *)

{============================================================================}

begin { Share }
  {- Init }
  LockRetry:= 0;

  { ⚠ 原汇编（已停用）：单元初始化时检测当前多任务器——
    DESQview（INT 21h/AX=2B01h，CX/DX='DE''SQ'）、DoubleDOS（INT 21h/AX=E400h）、
    Windows（INT 2Fh/AX=1600h）、OS2（INT 21h/AX=3001h）、NetWare（INT 2Fh/AX=7A00h），
    都不在则 NoTasker；据此设 MultiTasking、设显存段:偏移（多任务时用
    INT 10h/AH=FEh 取当前视频缓冲区 ES:DI，否则 $B800:0000）、并用 INT 21h/AH=34h
    取 InDos 标志地址写进 InDosFlag。
    本平台没有 DOS/BIOS 中断、没有多任务器、也没有段式显存，无法实现，
    故等价地置「无多任务器」的初值。
    ⚠ InDosFlag 置 nil 后，function InDos 里的 InDosFlag^ > 0 会解引用
    空指针；但 MultiTasking=False 时，LockFile/LockBytes/... 都在 While InDos
    之前 Exit，实际走不到那一步（InDos 的函数体是非汇编代码，按规矩不动）。
    原汇编原文见下方注释。 }
  MultiTasker := NoTasker;
  MultiTasking := False;
  VideoSeg := $B800;
  VideoOfs := $0000;
  InDosFlag := nil;

(* ── 原汇编（已停用，仅供参考）────────────────────────────────
  asm
  @CheckDV:
    mov   AX, $2B01
    mov   CX, $4445
    mov   DX, $5351
    int   $21
    cmp   AL, $FF
    je    @CheckDoubleDOS
    mov   MultiTasker, DesqView
    jmp   @CheckDone

  @CheckDoubleDOS:
    mov   AX, $E400
    int   $21
    cmp   AL, $00
    je    @CheckWindows
    mov   MultiTasker, DoubleDOS
    jmp   @CheckDone

  @CheckWindows:
    mov   AX, $1600
    int   $2F
    cmp   AL, $00
    je    @CheckOS2
    cmp   AL, $80
    je    @CheckOS2
    mov   MultiTasker, Windows
    jmp   @CheckDone

  @CheckOS2:
    mov   AX, $3001
    int   $21
    cmp   AL, $0A
    je    @InOS2
    cmp   AL, $14
    jne   @CheckNetware
  @InOS2:
    mov   MultiTasker, OS2
    jmp   @CheckDone

  @CheckNetware:
    mov   AX,$7A00
    int   $2F
    cmp   AL,$FF
    jne   @NoTasker
    mov   MultiTasker, NetWare
    jmp   @CheckDone

  @NoTasker:
    mov   MultiTasker, NoTasker

  @CheckDone:
  {-Set MultiTasking }
    cmp   MultiTasker, NoTasker
    mov   VideoSeg, $B800
    mov   VideoOfs, $0000
    je    @NoMultiTasker
    mov   MultiTasking, $01
  {-Get video address }
    mov   AH, $FE
    les   DI, [$B8000000]
    int   $10
    mov   VideoSeg, ES
    mov   VideoOfs, DI
    jmp   @Done

  @NoMultiTasker:
    mov   MultiTasking, $00

  @Done:
  {-Get InDos flag }
    mov   AH, $34
    int   $21
    mov   WORD PTR InDosFlag, BX
    mov   WORD PTR InDosFlag + 2, ES
  end;  { asm }
─────────────────────────────────────────────────────────────── *)
end.  { Share }
