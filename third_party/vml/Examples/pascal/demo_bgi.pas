(* demo_bgi.pas —— Pascal 第三层：**传统图形接口**（BGI）

  这一层用的是 Borland 的 `Graph` 单元（BGI = Borland Graphics Interface）：
  `InitGraph` / `SetColor` / `Line` / `Rectangle` / `Circle` / `OutTextXY` …
  特征是**固定分辨率 + 索引色** —— `SetColor(Red)` 里的 `Red` 是 **4 号调色板索引**，
  不是 RGB 0x000004。

  整条链是：
      Pascal 程序
        │  uses Graph  →  CALL InitGraph / GetMaxX / …
        ▼
      Lib/pascal/graph.pas   ← **声明**（interface 里有什么名字、什么形状）
        │  uses graph  →  AutoLinkUnit 链上 bgi.vml
        ▼
      Lib/shared/bgi.vml     ← 转发层
        ▼
      Lib/c/graphics.h       ← **唯一的实现**（45 个函数、走宿主 ui_* 图元）

  所以在 Pascal 里写 BGI 程序**不用写任何外部声明**（与同目录的 `catch.pas` 同一约定：
  `uses Graph` 之后裸调即可），实现只有一份，不会出现"同一个程序 C 画得对、
  Pascal 画得不对"。

  跑法（桌面 vmlcli —— BGI 垫层开的是**电脑屏窗口**，坐标系固定 640x480 永不重排）：
    dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/pascal/demo_bgi.pas \
        --screen 640x480 --frames out_frames/

  ⚠ 用 `--frames 目录` 而不是 `--frame 单文件`：本程序结尾调了 `CloseGraph`，
  而关窗会把整个场景置空（`VmlHostRuntime.WinClose` 的 `_scene = null`），
  跑完之后 `--frame` 已经没有场景可取。`--frames` 是每帧 present 当场拍快照，照常出图。

  画面（从上到下，能一眼看出对错）：
    ① 白底（`SetBkColor(White)` + `ClearDevice`）
    ② 一行 16 个色块 —— 0-15 号调色板索引各一条实心竖条（`Bar`）
    ③ 左边绿色空心矩形（`Rectangle`）、右边品红实心矩形（`Bar`）
    ④ 中间一个黄圆（`Circle`）+ `FloodFill` 灌成青色
    ⑤ 左下角一条逐点画的对角线（`PutPixel`，看得到"点"的颗粒感）
    ⑥ 右上角一个实心椭圆（`FillEllipse`）
    ⑦ 一行 `OutTextXY` 写的文字

  ◆ 两条用法事实
    · `driver` / `mode` 是**入参+出参**：老程序的标准开场是
        gd := Detect;  InitGraph(gd, gm, '');
      `gm` 由库回填。`Detect` / `VGA` / `Red` 这些常量都由 `graph.pas` 登记。
    · ⚠ **常量以 `Lib/c/graphics.h` 为准，别照 Turbo Pascal 手册抄驱动号** ——
      Borland 的 `CGA=1 / EGA=3`，本垫层按自己的编号（`CGA=3` / `EGA=5`）
      在 `_bgi_mode_size` 里查分辨率。老程序实际只用 `Detect` 和 `VGA`，这两个两边一致。
*)

program DemoBgi;

uses
  Graph;

var
  gd, gm: integer;
  i: integer;

begin
  { ── 开场：老程序的标准写法（gd 是入参+出参，gm 由库回填）────────── }
  gd := Detect;
  InitGraph(gd, gm, '');

  { ── ① 白底清屏 ─────────────────────────────────────────────── }
  SetBkColor(White);
  ClearDevice;

  { ── ② 16 个调色板色块（Bar = 实心矩形）────────────────────── }
  for i := 0 to 15 do
  begin
    SetFillStyle(SolidFill, i);
    Bar(20 + i * 38, 20, 20 + i * 38 + 24, 60);
  end;

  { ── ③ 空心矩形（Rectangle）与实心矩形（Bar）────────────────── }
  SetColor(Green);
  Rectangle(40, 100, 220, 220);

  SetFillStyle(SolidFill, Magenta);
  Bar(260, 100, 440, 220);

  { ── ④ 圆 + 灌色（Circle + FloodFill）──────────────────────── }
  SetColor(Yellow);
  Circle(540, 160, 70);
  FloodFill(540, 160, Yellow);

  { ── ⑤ PutPixel 逐点画一条对角线 ─────────────────────────────── }
  for i := 0 to 200 do
    PutPixel(60 + i, 260 + i, LightRed);

  { ── ⑥ 实心椭圆（FillEllipse）───────────────────────────────── }
  SetFillStyle(SolidFill, Cyan);
  FillEllipse(500, 340, 80, 50);

  { ── ⑦ 写字（BGI 的文字接口）───────────────────────────────── }
  SetColor(Blue);
  SetTextStyle(DefaultFont, HorizDir, 2);
  OutTextXY(30, 400, 'BGI demo in Pascal: SCREEN 640x480, 16 palette colors');
  OutTextXY(30, 440, 'InitGraph / Line / Rectangle / Circle / FloodFill / PutPixel');

  { ── 收尾 ①：帧边界。BGI 这一层**自己不画帧边界**（老程序没有帧的概念），
     但宿主那套 `ui_*` 是按帧渲染的 ⇒ 末尾补一次 `ui_present()` 告诉宿主
     "这一帧画完了"。同目录的 `Examples/c/test_bgi.c` 也是这么收尾的。
     ⚠ 顺序不能反：`ui_present` 必须在 `CloseGraph` **之前** —— 关窗会把整个
     场景置空，之后再 present 就没有东西可拍了（桌面 `--frames` 会一帧都导不出）。 }
  ui_present();

  { ── 收尾 ②：BGI 的收场白（关窗）─────────────────────────── }
  CloseGraph;
end.
