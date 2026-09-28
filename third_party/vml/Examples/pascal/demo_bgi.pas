(* demo_bgi.pas —— Pascal 第三层：**传统图形接口**（BGI）
  demo_bgi.pas -- Pascal layer 3: the classic graphics interface (BGI)

  这一层用的是 Borland 的 `Graph` 单元（BGI = Borland Graphics Interface）：
  This layer uses Borland's Graph unit (BGI = Borland Graphics Interface):
  `InitGraph` / `SetColor` / `Line` / `Rectangle` / `Circle` / `OutTextXY` …
  InitGraph / SetColor / Line / Rectangle / Circle / OutTextXY ...
  特征是**固定分辨率 + 索引色** —— `SetColor(Red)` 里的 `Red` 是 **4 号调色板索引**，
  It is characterised by fixed resolution + indexed colour -- the Red in SetColor(Red) is palette index 4,
  不是 RGB 0x000004。
  not RGB 0x000004.

  整条链是：
  The whole chain is:
      Pascal 程序
        │  uses Graph  →  CALL InitGraph / GetMaxX / …
        ▼
      Lib/pascal/graph.pas   ← **声明**（interface 里有什么名字、什么形状）
      Lib/pascal/graph.pas   <- the declarations (which names and shapes the interface has)
        │  uses graph  →  AutoLinkUnit 链上 bgi.vml
        uses graph  ->  AutoLinkUnit pulls in bgi.vml
        ▼
      Lib/shared/bgi.vml     ← 转发层
      Lib/shared/bgi.vml     <- forwarding layer
        ▼
      Lib/c/graphics.h       ← **唯一的实现**（45 个函数、走宿主 ui_* 图元）
      Lib/c/graphics.h       <- the single implementation (45 functions, built on host ui_* primitives)

  所以在 Pascal 里写 BGI 程序**不用写任何外部声明**（与同目录的 `catch.pas` 同一约定：
  So a BGI program in Pascal needs no external declarations at all (same convention as catch.pas beside it:
  `uses Graph` 之后裸调即可），实现只有一份，不会出现"同一个程序 C 画得对、
  after uses Graph just call them directly); there is only one implementation, so you never get "the same program right in C
  Pascal 画得不对"。
  but wrong in Pascal".

  跑法（桌面 vmlcli —— BGI 垫层开的是**电脑屏窗口**，坐标系固定 640x480 永不重排）：
  How to run (desktop vmlcli -- the BGI shim opens a computer-screen window, coordinates fixed at 640x480, never relaid out):
    dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/pascal/demo_bgi.pas \
        --screen 640x480 --frames out_frames/

  ⚠ 用 `--frames 目录` 而不是 `--frame 单文件`：本程序结尾调了 `CloseGraph`，
  WARNING: use --frames DIR and not --frame SINGLE-FILE: this program calls CloseGraph at the end,
  而关窗会把整个场景置空（`VmlHostRuntime.WinClose` 的 `_scene = null`），
  and closing the window clears the whole scene (VmlHostRuntime.WinClose sets _scene = null),
  跑完之后 `--frame` 已经没有场景可取。`--frames` 是每帧 present 当场拍快照，照常出图。
  so afterwards there is no scene left for --frame. --frames snapshots on each present, so images still come out.

  画面（从上到下，能一眼看出对错）：
  The picture (top to bottom, easy to check at a glance):
    ① 白底（`SetBkColor(White)` + `ClearDevice`）
    1) white background (SetBkColor(White) + ClearDevice)
    ② 一行 16 个色块 —— 0-15 号调色板索引各一条实心竖条（`Bar`）
    2) one row of 16 colour blocks -- one solid vertical bar (Bar) per palette index 0-15
    ③ 左边绿色空心矩形（`Rectangle`）、右边品红实心矩形（`Bar`）
    3) a green hollow rectangle (Rectangle) on the left, a magenta solid one (Bar) on the right
    ④ 中间一个黄圆（`Circle`）+ `FloodFill` 灌成青色
    4) a yellow circle (Circle) in the middle, flood-filled with cyan (FloodFill)
    ⑤ 左下角一条逐点画的对角线（`PutPixel`，看得到"点"的颗粒感）
    5) a diagonal drawn point by point (PutPixel) at the lower left, with visible dot grain
    ⑥ 右上角一个实心椭圆（`FillEllipse`）
    6) a solid ellipse (FillEllipse) at the upper right
    ⑦ 一行 `OutTextXY` 写的文字
    7) one line of text drawn with OutTextXY

  ◆ 两条用法事实
  * Two usage facts
    · `driver` / `mode` 是**入参+出参**：老程序的标准开场是
    . driver / mode are in-out parameters: the classic opening of old programs is
        gd := Detect;  InitGraph(gd, gm, '');
      `gm` 由库回填。`Detect` / `VGA` / `Red` 这些常量都由 `graph.pas` 登记。
      gm is filled in by the library. Constants like Detect / VGA / Red are all registered by graph.pas.
    · ⚠ **常量以 `Lib/c/graphics.h` 为准，别照 Turbo Pascal 手册抄驱动号** ——
    . WARNING: take the constants from Lib/c/graphics.h; do not copy driver numbers from the Turbo Pascal manual --
      Borland 的 `CGA=1 / EGA=3`，本垫层按自己的编号（`CGA=3` / `EGA=5`）
      Borland has CGA=1 / EGA=3, this shim uses its own numbering (CGA=3 / EGA=5)
      在 `_bgi_mode_size` 里查分辨率。老程序实际只用 `Detect` 和 `VGA`，这两个两边一致。
      and looks up the resolution in _bgi_mode_size. Old programs really only use Detect and VGA, which agree on both sides.
*)

program DemoBgi;

uses
  Graph;

var
  gd, gm: integer;
  i: integer;

begin
  { ── 开场：老程序的标准写法（gd 是入参+出参，gm 由库回填）────────── }
  { -- opening: the standard old-program form (gd is in-out, gm is filled in by the library) ---------- }
  gd := Detect;
  InitGraph(gd, gm, '');

  { ── ① 白底清屏 ─────────────────────────────────────────────── }
  { -- 1) clear to a white background ------------------------------------------ }
  SetBkColor(White);
  ClearDevice;

  { ── ② 16 个调色板色块（Bar = 实心矩形）────────────────────── }
  { -- 2) 16 palette colour blocks (Bar = solid rectangle) --------------------- }
  for i := 0 to 15 do
  begin
    SetFillStyle(SolidFill, i);
    Bar(20 + i * 38, 20, 20 + i * 38 + 24, 60);
  end;

  { ── ③ 空心矩形（Rectangle）与实心矩形（Bar）────────────────── }
  { -- 3) hollow rectangle (Rectangle) and solid rectangle (Bar) ---------------- }
  SetColor(Green);
  Rectangle(40, 100, 220, 220);

  SetFillStyle(SolidFill, Magenta);
  Bar(260, 100, 440, 220);

  { ── ④ 圆 + 灌色（Circle + FloodFill）──────────────────────── }
  { -- 4) circle + flood fill (Circle + FloodFill) ----------------------------- }
  SetColor(Yellow);
  Circle(540, 160, 70);
  FloodFill(540, 160, Yellow);

  { ── ⑤ PutPixel 逐点画一条对角线 ─────────────────────────────── }
  { -- 5) draw a diagonal point by point with PutPixel ------------------------ }
  for i := 0 to 200 do
    PutPixel(60 + i, 260 + i, LightRed);

  { ── ⑥ 实心椭圆（FillEllipse）───────────────────────────────── }
  { -- 6) solid ellipse (FillEllipse) ----------------------------------------- }
  SetFillStyle(SolidFill, Cyan);
  FillEllipse(500, 340, 80, 50);

  { ── ⑦ 写字（BGI 的文字接口）───────────────────────────────── }
  { -- 7) draw text (the BGI text interface) ---------------------------------- }
  SetColor(Blue);
  SetTextStyle(DefaultFont, HorizDir, 2);
  OutTextXY(30, 400, 'BGI demo in Pascal: SCREEN 640x480, 16 palette colors');
  OutTextXY(30, 440, 'InitGraph / Line / Rectangle / Circle / FloodFill / PutPixel');

  { ── 收尾 ①：帧边界。BGI 这一层**自己不画帧边界**（老程序没有帧的概念），
     wrap-up 1: frame boundary. The BGI layer does not draw a frame boundary itself (old programs have no frames),
     但宿主那套 `ui_*` 是按帧渲染的 ⇒ 末尾补一次 `ui_present()` 告诉宿主
     but the host ui_* set renders per frame => add one ui_present() at the end to tell the host
     "这一帧画完了"。同目录的 `Examples/c/test_bgi.c` 也是这么收尾的。
     "this frame is done". Examples/c/test_bgi.c wraps up the same way.
     ⚠ 顺序不能反：`ui_present` 必须在 `CloseGraph` **之前** —— 关窗会把整个
     WARNING: order matters: ui_present must come before CloseGraph -- closing the window clears the
     场景置空，之后再 present 就没有东西可拍了（桌面 `--frames` 会一帧都导不出）。
     whole scene, and a later present has nothing to capture (desktop --frames would export no frame at all). }
  ui_present();

  { ── 收尾 ②：BGI 的收场白（关窗）─────────────────────────── }
  { -- wrap-up 2: the BGI closing (close the window) ------------------------- }
  CloseGraph;
end.
