// VML 网络 Socket 扩展库 — Pascal
// OS 模式专用，需显式 uses Net

unit Net;

interface

function NetCreate(domain, typ: integer): integer;
function NetBind(fd, port: integer): integer;
function NetListen(fd, backlog: integer): integer;
function NetAccept(fd: integer): integer;
function NetConnect(host: string; port: integer): integer;
function NetSend(fd: integer; var data; len: integer): integer;
function NetRecv(fd: integer; var buf; maxLen: integer): integer;
function NetClose(fd: integer): integer;
function DnsResolve(hostname: string): integer;

implementation

function NetCreate(domain, typ: integer): integer;
begin asm("SYSCALL 330"); NetCreate := 0 end;

function NetBind(fd, port: integer): integer;
begin asm("SYSCALL 331"); NetBind := 0 end;

function NetListen(fd, backlog: integer): integer;
begin asm("SYSCALL 332"); NetListen := 0 end;

function NetAccept(fd: integer): integer;
begin asm("SYSCALL 333"); NetAccept := 0 end;

function NetConnect(host: string; port: integer): integer;
begin asm("SYSCALL 334"); NetConnect := 0 end;

function NetSend(fd: integer; var data; len: integer): integer;
begin asm("SYSCALL 335"); NetSend := 0 end;

function NetRecv(fd: integer; var buf; maxLen: integer): integer;
begin asm("SYSCALL 336"); NetRecv := 0 end;

function NetClose(fd: integer): integer;
begin asm("SYSCALL 337"); NetClose := 0 end;

function DnsResolve(hostname: string): integer;
begin asm("SYSCALL 338"); DnsResolve := 0 end;

end.
