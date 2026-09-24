(* demo_ui.pas —— Pascal 第四层：**最新 UI 接口**（宿主 `ui_*` 图元 + 绘图窗口）

  这一层和第三层（BGI/`uses Graph`）刻意不同。BGI 是"固定分辨率 + 索引色"的老世界，
  这一层是**宿主窗口 + 0xAARRGGBB 真彩 + 统一消息队列**。四条关键差别：

    ① **开窗显式**：`ui_win_open(标题, 宽, 高)`，宽高来自 `ui_scr_w()/ui_scr_h()`
       （可用绘图区），不是写死的 640x480。
    ② **屏幕方向要问**：`ui_orientation()` 返回 0=竖屏 / 1=横屏 —— 程序据此决定
       "面板怎么摆"。这是**开窗前就能问**的（详见 `docs/VML宿主接口.md`）。
    ③ **显式呈现**：图元只是追加进场景，`ui_present()` 才是"这一帧画完了"的帧边界。
    ④ **输入是消息队列**：`ui_wait_msg(超时) / ui_msg_a() / ui_win_closed()` 收
       键盘、触摸、定时器、窗口事件 —— 全部走同一个队列。

  跑法（桌面 vmlcli）：
    dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/pascal/demo_ui.pas \
        --screen 480x640 --frames out_frames/

  ⚠ **要看画面请用 `--frames 目录`，不要用 `--frame 单个文件`**。原因不在本程序：
  `ui_win_close()`（#521）在宿主侧把**整个场景置空**（`VmlHostRuntime.WinClose`
  的 `_scene = null`），于是程序跑完之后 `--frame` 那一刀已经没有场景可取，
  只会打一行「没有可导出的帧（场景是空的）」。`--frames` 是**每帧 present 当场拍快照**，
  所以照常出图。这是桌面脚手架的行为，不是本程序写错了。

  ◆ **有界**（任务要求：按 N 帧或收到键就退出）—— 三条出口都写了：
    · 收到**任意按键**（消息类型 1 = KeyDown）立刻退出
    · 或画满 150 帧自动退出（每帧 `ui_wait_msg(30)` 最多等 30ms ⇒ 约 4.5 秒）
    · 或用户点了标题栏的返回箭头（`ui_win_closed() <> 0`）
  退出前 `ui_win_close()`，程序正常结束、退出码 0。

  ◆ 三条本前端的写法要求

    ① **裸调库函数不写声明**（同目录 `catch.pas` 的约定）：`Lib/shared/src/vmlui.c`
       编出来的 `vmlui.vml` 由 `vmltool.config.xml` 挂上来，直接写 `ui_rect(...)` 即可。

    ② **颜色用 Pascal 的十六进制字面量 `$FF101018`**（`$` 是 Pascal 的数字前缀）。
       文档给的记法是 `0xAARRGGBB`，而它作为 32 位有符号整数**会溢出** —— 写成
       十进制负数当然也行，但**极容易算错**：本文件第一版手算了 11 条，错了 5 条，
       画出来的颜色只是"差一点点"（比如该 `#2E7D32` 画成了 `#2F7D32`），
       肉眼根本看不出，是靠 `--frames` 出图后**逐像素比色**才发现的。

    ③ **正文里不用数字**。Pascal 的 `Str()`（整数转串）本前端没有，而字符串 `+`
       又不是拼接（见 `demo_std.pas` 的说明）⇒ **没有现成的"拼一串带数字的话"这条路**。
       所以状态一律用**形状与固定标签**表达（进度条用 `ui_rect` 的宽度、方向用
       两个不同的词）—— 这与同目录 `Examples/fortran/sokoban.f90` 的取舍一致。

    ⚠ 另外：**Pascal 的两种块注释都不嵌套**（花括号那种与括号星号那种都是），
      所以本文件里提到注释符号一律用文字说，不写真符号。
*)

program DemoUi;

var
  w, h, ori, frame, maxf, msg, kc, running: integer;
  bx, by, bw, bh: integer;
  label1, label2: string;

begin
  { ① 问可用绘图区；拿不到就给个兜底尺寸（桌面脚手架 / 某些后端会给 0） }
  w := ui_scr_w();
  h := ui_scr_h();
  if w <= 0 then
  begin
    w := 360;
  end;
  if h <= 0 then
  begin
    h := 620;
  end;

  { ② 开窗 —— 宽高直接取"可用绘图区"，于是手机上就是整页 }
  ui_win_open('Pascal UI demo', w, h);

  { ③ 问屏幕方向：0 = 竖屏，1 = 横屏 }
  ori := ui_orientation();
  if ori = 1 then
  begin
    label1 := 'orientation = LANDSCAPE (1)';
  end
  else
  begin
    label1 := 'orientation = PORTRAIT (0)';
  end;
  label2 := 'ui_win_open / ui_rect / ui_circle / ui_line / ui_text / ui_present';

  { ④ 方向决定面板怎么摆：横屏把方块放右上，竖屏放右下 }
  bx := 24;
  by := 24;
  bw := 140;
  bh := 100;
  if ori = 1 then
  begin
    bx := w - bw - 24;
    by := 120;
  end
  else
  begin
    by := h - bh - 110;
  end;

  maxf := 150;
  frame := 0;
  running := 1;

  while running = 1 do
  begin
    { ── 每帧重画（保留模式：场景每帧从头追加）────────────────── }
    ui_clear($FF101018);                                     { 深底 }

    { 标题 }
    ui_text(16, 16, 'Pascal UI demo', $FF9AD8FF, 20, 0);     { 浅蓝 }
    ui_text(16, 44, label1, $FFFFD34A, 15, 0);               { 黄 }
    ui_text(16, 66, label2, $FF8A9AA8, 11, 0);               { 灰 }

    { 图元三件套：线 / 矩形 / 圆 }
    ui_line(16, 92, w - 16, 92, $FF33506A, 1);               { 分隔线 }
    ui_rect(bx, by, bw, bh, $FF2E7D32, 1, 0, 8);             { 深绿实心圆角框 }
    ui_rect(bx + 12, by + 12, bw - 24, bh - 24, $FF66BB6A, 0, 2, 0);  { 浅绿空心框 }
    ui_circle(w div 2, h div 2, 44, $FFE74C3C, 1, 0);        { 红实心圆 }
    ui_circle(w div 2, h div 2, 20, $FFF1C40F, 1, 0);        { 黄内圆 }

    { 帧计数进度条（**不写字**：这里没有整数转串，见文件头第 ③ 条） }
    ui_rect(16, h - 62, w - 32, 14, $FF22303C, 1, 0, 3);     { 进度条底 }
    ui_rect(16, h - 62, (w - 32) * frame div maxf, 14, $FF00C2A8, 1, 0, 3);  { 进度条 }
    ui_text(16, h - 42, 'press any key to exit', $FF8A9AA8, 13, 0);
    ui_text(w - 16, h - 42, 'ui_present every frame', $FF8A9AA8, 13, 2);

    { ── 帧边界 ────────────────────────────────────────────────── }
    ui_present();
    frame := frame + 1;

    { 出口 A：画满 maxf 帧 }
    if frame >= maxf then
    begin
      running := 0;
    end;

    { 出口 B：收到任意按键（消息类型 1 = KeyDown） }
    if running = 1 then
    begin
      msg := ui_wait_msg(30);
      if msg = 1 then
      begin
        kc := ui_msg_a();
        running := 0;
      end;
    end;

    { 出口 C：用户点了标题栏的返回箭头 }
    if ui_win_closed() <> 0 then
    begin
      running := 0;
    end;
  end;

  { 正常收尾：关窗 }
  ui_win_close();
end.
