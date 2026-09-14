// VML FFI 动态库调用扩展库 — Pascal
// OS 模式专用，需显式 uses Ffi

unit Ffi;

interface

function DlOpen(path: string): integer;
function DlSym(handle: integer; name: string): integer;
function DlClose(handle: integer): integer;
function NativeCall(funcId: integer; var args; count, flags: integer): integer;
function NativeCallF(funcId: integer; var fargs; count, flags: integer): real;
function GetPlatform: integer;

implementation

function DlOpen(path: string): integer;
begin asm("SYSCALL 370"); DlOpen := 0 end;

function DlSym(handle: integer; name: string): integer;
begin asm("SYSCALL 371"); DlSym := 0 end;

function DlClose(handle: integer): integer;
begin asm("SYSCALL 372"); DlClose := 0 end;

function NativeCall(funcId: integer; var args; count, flags: integer): integer;
begin asm("SYSCALL 373"); NativeCall := 0 end;

function NativeCallF(funcId: integer; var fargs; count, flags: integer): real;
begin asm("SYSCALL 375"); NativeCallF := 0.0 end;

function GetPlatform: integer;
begin asm("SYSCALL 374"); GetPlatform := 0 end;

end.
