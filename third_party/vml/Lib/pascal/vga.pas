{ VGA 图形库 - 用于 VML 虚拟机的图形和输入输出 }

{ 文本模式函数 }
procedure VgaClear;
procedure VgaPutChar(c: char);
procedure VgaPuts(s: string);
procedure VgaSetCursor(x, y: integer);
procedure VgaGetCursor(var x, y: integer);

{ 键盘输入函数 }
function KbHit: boolean;
function KbGetCh: char;

{ 鼠标输入函数 }
function MouseGetX: integer;
function MouseGetY: integer;
function MouseLeft: boolean;
function MouseRight: boolean;

{ 图形模式函数 }
procedure VgaPutPixel(x, y, color: integer);
function VgaGetPixel(x, y: integer): integer;

procedure VgaDrawLine(x1, y1, x2, y2, color: integer);
procedure VgaDrawCircle(cx, cy, radius, color: integer);
procedure VgaFillCircle(cx, cy, radius, color: integer);
procedure VgaDrawRect(x, y, width, height, color: integer);
procedure VgaFillRect(x, y, width, height, color: integer);

procedure VgaDrawText(x, y: integer; text: string; color: integer);
procedure VgaDrawBitmap(x, y: integer; bitmap: array of integer; width, height: integer);

procedure VgaClearScreen(color: integer);
function VgaGetWidth: integer;
function VgaGetHeight: integer;

{ 颜色常量 }
const
  VGA_BLACK = 0;
  VGA_BLUE = 1;
  VGA_GREEN = 2;
  VGA_CYAN = 3;
  VGA_RED = 4;
  VGA_MAGENTA = 5;
  VGA_BROWN = 6;
  VGA_LIGHT_GRAY = 7;
  VGA_DARK_GRAY = 8;
  VGA_LIGHT_BLUE = 9;
  VGA_LIGHT_GREEN = 10;
  VGA_LIGHT_CYAN = 11;
  VGA_LIGHT_RED = 12;
  VGA_LIGHT_MAGENTA = 13;
  VGA_YELLOW = 14;
  VGA_WHITE = 15;