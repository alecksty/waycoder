{ VML VGA 图形扩展库 (QBASIC风格) — Pascal }
{ 需显式 uses gfx }

{ 基本 VGA }
procedure VgaClear;
procedure VgaPutchar(x, y: integer; c: char; color: integer);
procedure VgaPuts(x, y: integer; s: string; color: integer);

{ 屏幕模式与信息 }
function GfxScreen(mode: integer): integer;
function GfxWidth: integer;
function GfxHeight: integer;
function GfxDepth: integer;

{ 调色板 }
procedure GfxPalette(idx, r, g, b: integer);
function GfxPaletteGet(idx: integer): integer;

{ 像素操作 }
procedure GfxPset(x, y, color: integer);
function GfxPoint(x, y: integer): integer;
procedure GfxCls;
procedure GfxClsColor(color: integer);

{ 绘图原语 }
procedure GfxLine(x1, y1, x2, y2, color: integer);
procedure GfxRect(x1, y1, x2, y2, color: integer);
procedure GfxRectFill(x1, y1, x2, y2, color: integer);
procedure GfxCircle(cx, cy, r, color: integer);
procedure GfxCircleFill(cx, cy, r, color: integer);
procedure GfxArc(cx, cy, r, sa, ea, color: integer);
procedure GfxSector(cx, cy, r, sa, ea, color: integer);

{ 文字 }
procedure GfxPrint(x, y: integer; text: string; color: integer);
procedure GfxPrintScale(x, y: integer; text: string; color, scale: integer);

{ 填充 }
procedure GfxFloodFill(x, y, fc, bc: integer);

{ 高级图形 (SYSCALL) }
function GfxScreenshot: integer;
function GfxPutImage(x, y, w, h: integer; var data): integer;
function GfxGetImage(x, y, w, h: integer; var buffer): integer;
function GfxViewport(x1, y1, x2, y2: integer): integer;
