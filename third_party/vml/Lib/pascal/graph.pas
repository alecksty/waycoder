{ Graph —— BGI（Borland Graphics Interface）兼容单元的 **Pascal 侧声明**

  ## 这个文件是什么、不是什么

  它是**声明**，不是实现：`implementation` 段是空的，一个函数体都没有。

  ```
        Pascal 程序
             │  uses Graph;  →  CALL InitGraph / GetMaxX / …
             ▼
      Lib/pascal/graph.pas     ← 本文件：**声明**（interface 里有什么名字、什么形状）
             │  uses graph  →  AutoLinkUnit 链上 bgi.vml
             ▼
      Lib/shared/bgi.vml       ← 转发层（Lib/shared/src/bgi.c 生成）
             ▼
      Lib/c/graphics.h         ← **唯一的实现**（纯头文件的 BGI 垫层，全部落在 ui_* 上）

  ## 为什么不把实现写在这儿

  本仓铁律「一处实现」：真的那份在 `Lib/c/graphics.h`（45 个函数、走宿主 `ui_*` 图元，
  `Examples/c/test_bgi.c` 逐格体检过）。在 Pascal 里再写一份 = 第二份实现，
  而两份实现漂移的症状是"同一个程序 C 画得对、Pascal 画得不对"，最难查。
  VML 的 Pascal 前端调库是**按标签**的，所以转到 Pascal 只需要**声明**。

  ## 为什么要有这个文件（而不是在编译器里写一张 BGI 名字表）

  Pascal 里「无参函数」和「变量」的**语法形状完全一样**（都不带括号）：`GetMaxX div 2`
  到底是一次调用还是一个变量，只能靠**声明**分。把声明放在这里，编译器就只需要认
  「`uses` 单元 interface 里有什么」，而**不必认识 `GetMaxX` 这个名字** ——
  换个库只要加一个 `.pas`，前端一行都不用改。
  （前端那侧读这个文件的代码见 `PascalCompiler.RegisterUnitFunctions`。）

  ## 常量的值以谁为准

  **以 `Lib/c/graphics.h` 为准**（它是实现）。⚠ 别照 Turbo Pascal 手册抄驱动号：
  Borland 的 `CGA=1 / EGA=3`，而本垫层按自己的编号（`#define CGA 3` / `#define EGA 5`）
  在 `_bgi_mode_size` 里查分辨率 —— 抄手册的话 `CGA` 会落到"查不到"的分支、
  分辨率退回 640×480（不报错，只是版式不对）。老程序实际只用 `Detect` 和 `VGA`，
  这两个两边一致。
}

unit Graph;

interface

const
  { ── 驱动（`InitGraph` 的第一个参数）────────────────────────── }
  Detect   = 0;
  CGA      = 3;
  EGA      = 5;
  IBM8514  = 6;
  VGA      = 9;

  { ── 模式（`InitGraph` 的第二个参数 / `SetGraphMode`）─────────── }
  CGAC0 = 0;  CGAC1 = 1;  CGAC2 = 2;  CGAC3 = 3;  CGAHi = 4;
  EGALo = 0;  EGAHi = 1;
  VGALo = 0;  VGAMed = 1; VGAHi = 2;

  { ── 16 色调色板索引（= CGA/VGA 标准）──────────────────────────
     ⚠ 这一组与 Crt 的 `TextColor` 颜色常量**同值同名**，所以一个文件同时满足了
        `SetColor(Red)` 和 `TextColor(Red)` 两种老写法。 }
  Black = 0;  Blue = 1;  Green = 2;  Cyan = 3;
  Red = 4;    Magenta = 5;  Brown = 6;  LightGray = 7;
  DarkGray = 8;  LightBlue = 9;  LightGreen = 10;  LightCyan = 11;
  LightRed = 12; LightMagenta = 13; Yellow = 14;  White = 15;

  { ── 填充样式（`SetFillStyle`）──────────────────────────────── }
  EmptyFill = 0;  SolidFill = 1;  LineFill = 2;  LtSlashFill = 3;
  SlashFill = 4;  BkSlashFill = 5;  LtBkSlashFill = 6;  HatchFill = 7;
  XHatchFill = 8;  InterleaveFill = 9;  WideDotFill = 10;  CloseDotFill = 11;
  UserFill = 12;

  { ── 线型 / 线宽（`SetLineStyle`）────────────────────────────── }
  SolidLn = 0;  DottedLn = 1;  CenterLn = 2;  DashedLn = 3;  UserBitLn = 4;
  NormWidth = 1;  ThickWidth = 3;

  { ── `PutImage` 的运算模式 ─────────────────────────────────── }
  CopyPut = 0;  XOrPut = 1;

  { ── 字体 / 方向 / 对齐（`SetTextStyle` / `SetTextJustify`）──────
     ⚠ 本平台**只有一种字形**，字体号收下来只是为了"编得过"；字号（第三个参数）
       是认的，而且它是**放大倍数**（1 = 基本字形），换算在 graphics.h 里做。 }
  DefaultFont = 0;  TriplexFont = 1;  SmallFont = 2;  SansSerifFont = 3;
  GothicFont = 4;  ScriptFont = 5;  SimplexFont = 6;  TriplexScrFont = 7;
  ComplexFont = 8;  EuropeanFont = 9;  BoldFont = 10;  UserCharSize = 0;
  HorizDir = 0;  VertDir = 1;
  LeftText = 0;  CenterText = 1;  RightText = 2;  BottomText = 0;  TopText = 2;

  { ── 错误码（`GraphResult` / `GraphErrorMsg`）─────────────────
     本平台开窗不会失败 ⇒ `GraphResult` 恒回 `grOk`；收下其余码是为了
     老程序里那些 `case ErrorCode of` / 日志分支照样编得过。 }
  grOk = 0;              grNoInitGraph = -1;    grNotDetected = -2;
  grFileNotFound = -3;   grInvalidDriver = -4;  grNoLoadMem = -5;
  grNoScanMem = -6;      grNoFloodMem = -7;     grFontNotFound = -8;
  grNoFontMem = -9;      grInvalidMode = -10;   grError = -11;
  grIOerror = -12;       grInvalidFont = -13;   grInvalidFontNum = -14;
  grInvalidVersion = -18;

{ ── 生命周期 ──────────────────────────────────────────────── }
{ `driver`/`mode` 是**入参+出参**：老程序的标准开场是
  `gd := Detect; InitGraph(gd, gm, '');` —— `gm` 由库回填。 }
procedure InitGraph(var driver: integer; var mode: integer; path: string);
procedure InitWindow(w, h: integer; title: string);
procedure SetGraphMode(mode: integer);
procedure CloseGraph;
procedure ClearDevice;
function  GetMaxX: integer;
function  GetMaxY: integer;
function  GetMaxColor: integer;

{ ── 设置 ──────────────────────────────────────────────────── }
procedure SetColor(c: integer);
procedure SetBkColor(c: integer);
function  GetColor: integer;
function  GetBkColor: integer;
procedure SetFillStyle(pattern, color: integer);
procedure SetLineStyle(style, pattern, thickness: integer);
procedure SetTextStyle(font, direction, size: integer);
procedure SetTextJustify(horiz, vert: integer);

{ ── 画图 ──────────────────────────────────────────────────── }
procedure Line(x1, y1, x2, y2: integer);
procedure MoveTo(x, y: integer);
procedure LineTo(x, y: integer);
procedure LineRel(dx, dy: integer);
procedure Rectangle(l, t, r, b: integer);
procedure Bar(l, t, r, b: integer);
procedure Bar3D(l, t, r, b, depth, topflag: integer);
procedure Circle(x, y, r: integer);
procedure FillEllipse(x, y, xr, yr: integer);
procedure Ellipse(x, y, st, en, xr, yr: integer);
procedure PutPixel(x, y, color: integer);
procedure Arc(x, y, st, en, r: integer);
procedure PieSlice(x, y, st, en, r: integer);
procedure Sector(x, y, st, en, xr, yr: integer);
procedure FloodFill(x, y, border: integer);

{ ⚠ 这三个（还有 `DrawPoly`/`FillPoly`）收的是**数组的地址**，不是元素值 ——
  所以形参必须是 `var`：前端靠"形参是不是 var"决定压地址还是压值，
  写成值形参的话库那边拿到的是一个随机地址（程序照跑、画面全错、不报错）。 }
procedure DrawPoly(numPoints: integer; var polyPoints: integer);
procedure FillPoly(numPoints: integer; var polyPoints: integer);
function  ImageSize(l, t, r, b: integer): integer;
procedure GetImage(l, t, r, b: integer; var buffer: integer);
procedure PutImage(l, t: integer; var buffer: integer; op: integer);

{ ── 文字 ──────────────────────────────────────────────────── }
procedure OutTextXY(x, y: integer; s: string);
procedure OutText(s: string);
function  TextWidth(s: string): integer;
function  TextHeight(s: string): integer;

{ ── 像素读回 ──────────────────────────────────────────────── }
function  GetPixel(x, y: integer): integer;

{ ── 错误码 ────────────────────────────────────────────────── }
function  GraphResult: integer;
function  GraphErrorMsg(code: integer): string;

implementation

{ 空 —— 实现全在 `Lib/c/graphics.h`，由 `uses graph` 自动链接的 `bgi.vml` 提供。
  这里**必须**是空的：一旦写上函数体，本单元就会自己生成一个同名标签，
  主程序里的 `CALL` 会解析到**这个空的本地版本**而不是库里的实现
  （返回垃圾值、一个错都不报）。前端对此有一道闸：interface 里的声明
  （`IsForward`）不生成任何代码，见 `CodeGenerator.Core.cs` 的 `GenerateUnitCode`。 }

end.
