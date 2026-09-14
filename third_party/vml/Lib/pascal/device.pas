{ Pascal 设备接口库
  提供统一的设备操作函数
}

unit Device;

interface

{ 设备类型常量 }
const
  DEVICE_CONSOLE = 0;
  DEVICE_VGA = 1;
  DEVICE_KEYBOARD = 2;
  DEVICE_MOUSE = 3;
  DEVICE_TIMER = 4;
  DEVICE_RTC = 5;
  DEVICE_FILESYSTEM = 6;

{ 设备控制命令常量 }
const
  { 控制台设备命令 }
  CONSOLE_CMD_GET_INFO = 0;
  CONSOLE_CMD_CLEAR_SCREEN = 1;
  CONSOLE_CMD_SET_CURSOR = 2;
  CONSOLE_CMD_GET_CURSOR = 3;

  { VGA设备命令 }
  VGA_CMD_GET_INFO = 0;
  VGA_CMD_CLEAR_SCREEN = 1;
  VGA_CMD_SET_PIXEL = 2;
  VGA_CMD_DRAW_CHAR = 3;
  VGA_CMD_DRAW_STRING = 4;
  VGA_CMD_SET_MODE = 5;
  VGA_CMD_GET_MEMORY = 6;

  { 键盘设备命令 }
  KEYBOARD_CMD_GET_STATUS = 0;
  KEYBOARD_CMD_CHECK_KEY = 1;
  KEYBOARD_CMD_CLEAR_BUFFER = 2;
  KEYBOARD_CMD_SET_LEDS = 3;

  { 鼠标设备命令 }
  MOUSE_CMD_GET_STATUS = 0;
  MOUSE_CMD_SET_POSITION = 1;
  MOUSE_CMD_SET_BUTTONS = 2;
  MOUSE_CMD_SET_WHEEL = 3;
  MOUSE_CMD_SIMULATE_MOVE = 4;
  MOUSE_CMD_SIMULATE_CLICK = 5;

{ 设备操作函数 }
function DevOpen(name: string): integer;
function DevClose(handle: integer): integer;
function DevRead(handle: integer; var buffer; offset, count: integer): integer;
function DevWrite(handle: integer; const buffer; offset, count: integer): integer;
function DevControl(handle, command: integer; const data; length: integer): integer;

{ 便捷函数 }
procedure DevConsolePrint(text: string);
procedure DevVgaClear;
procedure DevVgaDrawChar(x, y: integer; c: char; color: integer);
procedure DevVgaDrawString(x, y: integer; text: string; color: integer);
function DevKeyboardCheck: integer;
procedure DevMouseGetPosition(var x, y: integer);
function DevRtcGetTimeString(var buffer: string; bufferSize: integer): integer;

{ 传统函数（使用新接口实现） }
procedure VgaClear;
procedure VgaPutChar(c: char);
procedure VgaPuts(s: string);
function KbHit: boolean;
function KbGetCh: char;
function MouseGetX: integer;
function MouseGetY: integer;
function MouseLeft: boolean;
function MouseRight: boolean;

implementation

{ 设备操作函数实现 }
function DevOpen(name: string): integer;
var
  handle: integer;
begin
  { 使用SYSCALL 100打开设备 }
  asm
    LOAD R0, name
    SYSCALL 100
    MOV handle, R1
  end;
  DevOpen := handle;
end;

function DevClose(handle: integer): integer;
var
  result: integer;
begin
  { 使用SYSCALL 101关闭设备 }
  asm
    LOAD R0, handle
    SYSCALL 101
    MOV result, R0
  end;
  DevClose := result;
end;

function DevWrite(handle: integer; const buffer; offset, count: integer): integer;
var
  result: integer;
begin
  { 使用SYSCALL 103写入设备 }
  asm
    LOAD R0, handle
    LOAD R1, buffer
    LOAD R2, count
    SYSCALL 103
    MOV result, R0
  end;
  DevWrite := result;
end;

function DevControl(handle, command: integer; const data; length: integer): integer;
var
  result: integer;
begin
  { 使用SYSCALL 104控制设备 }
  asm
    LOAD R0, handle
    LOAD R1, command
    LOAD R2, data
    LOAD R3, length
    SYSCALL 104
    MOV result, R0
  end;
  DevControl := result;
end;

{ 便捷函数实现 }
procedure DevConsolePrint(text: string);
var
  handle: integer;
begin
  handle := DevOpen('console');
  if handle >= 0 then
  begin
    DevWrite(handle, text[1], 0, Length(text));
    DevClose(handle);
  end;
end;

procedure DevVgaClear;
var
  handle: integer;
begin
  handle := DevOpen('vga');
  if handle >= 0 then
  begin
    DevControl(handle, VGA_CMD_CLEAR_SCREEN, nil, 0);
    DevClose(handle);
  end;
end;

procedure DevVgaDrawChar(x, y: integer; c: char; color: integer);
var
  handle: integer;
  data: array[0..3] of byte;
begin
  handle := DevOpen('vga');
  if handle >= 0 then
  begin
    data[0] := x;
    data[1] := y;
    data[2] := Ord(c);
    data[3] := color;
    DevControl(handle, VGA_CMD_DRAW_CHAR, data, 4);
    DevClose(handle);
  end;
end;

procedure DevVgaDrawString(x, y: integer; text: string; color: integer);
var
  handle: integer;
  data: array[0..255] of byte;
  i, len: integer;
begin
  handle := DevOpen('vga');
  if handle >= 0 then
  begin
    len := Length(text);
    if len > 253 then len := 253;
    
    data[0] := x;
    data[1] := y;
    data[2] := color;
    
    for i := 1 to len do
      data[2 + i] := Ord(text[i]);
    
    DevControl(handle, VGA_CMD_DRAW_STRING, data, 3 + len);
    DevClose(handle);
  end;
end;

function DevKeyboardCheck: integer;
var
  handle: integer;
  status: byte;
  result: integer;
begin
  handle := DevOpen('kbd');
  if handle >= 0 then
  begin
    status := 0;
    result := DevControl(handle, KEYBOARD_CMD_CHECK_KEY, status, 1);
    DevClose(handle);
    if result = 0 then
      DevKeyboardCheck := status
    else
      DevKeyboardCheck := -1;
  end
  else
    DevKeyboardCheck := -1;
end;

procedure DevMouseGetPosition(var x, y: integer);
var
  handle: integer;
  data: array[0..9] of byte;
  result: integer;
begin
  handle := DevOpen('mouse');
  if handle >= 0 then
  begin
    result := DevControl(handle, MOUSE_CMD_GET_STATUS, data, 10);
    DevClose(handle);
    if result = 0 then
    begin
      x := data[0] + data[1] * 256;
      y := data[4] + data[5] * 256;
    end
    else
    begin
      x := 0;
      y := 0;
    end;
  end
  else
  begin
    x := 0;
    y := 0;
  end;
end;

function DevRtcGetTimeString(var buffer: string; bufferSize: integer): integer;
var
  handle: integer;
begin
  handle := DevOpen('rtc');
  if handle >= 0 then
  begin
    DevRtcGetTimeString := DevControl(handle, RTC_CMD_GET_TIME_STRING, buffer[1], bufferSize);
    DevClose(handle);
  end
  else
    DevRtcGetTimeString := -1;
end;

{ 传统函数实现 }
procedure VgaClear;
begin
  DevVgaClear;
end;

procedure VgaPutChar(c: char);
begin
  { 简化实现：在默认位置输出字符 }
  DevConsolePrint(c);
end;

procedure VgaPuts(s: string);
begin
  DevConsolePrint(s);
end;

function KbHit: boolean;
begin
  KbHit := DevKeyboardCheck() > 0;
end;

function KbGetCh: char;
begin
  { 简化实现：返回空字符 }
  KbGetCh := #0;
end;

function MouseGetX: integer;
var
  x, y: integer;
begin
  DevMouseGetPosition(x, y);
  MouseGetX := x;
end;

function MouseGetY: integer;
var
  x, y: integer;
begin
  DevMouseGetPosition(x, y);
  MouseGetY := y;
end;

function MouseLeft: boolean;
begin
  { 简化实现：返回false }
  MouseLeft := false;
end;

function MouseRight: boolean;
begin
  { 简化实现：返回false }
  MouseRight := false;
end;

end.
procedure Exit(code: Integer);
begin
  asm('SYSCALL 3');
end;
