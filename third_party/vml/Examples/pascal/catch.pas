{ 接方块 —— 用 **Pascal** 写的手机游戏

  玩法：左右方向键移动底部挡板，把落下来的球弹回去；没接住就结束。每接住一次 +10 分。

  ◆ 手机那套 UI

  开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
  由 `vmltool.config.xml` 的 `<Language Name="pascal" Libs="vmlui.vml">` 挂上来。

  ◆ 为什么 Pascal 这份可以按「全局数组 + 无参过程」自然写

  别的语言（Kotlin / Swift / Go / Python）都有「模块级数组基址不对」或「函数看不见它」
  这类问题，只能把状态塞进 main 或内联。Pascal 的 `var` 段全局量是语言原生语义、
  过程直接可见，所以这份保留了最自然的结构 —— 也正因为如此，它是**结构上最接近
  C 版**的一份，适合当对照。

  ◆ 写法要求

    · 裸调库函数不写声明（同 corpus/pascal/skel.pas）。
    · 整除用 `div`；颜色写负数十进制。
    · 过程名不叫 `step`：Pascal 标准过程里已有 `Inc()` 这类，避开常见名免得撞上。 }

program Catch;

var
  { 状态：0=挡板x 1=球x 2=球y 3=球dx 4=球dy 5=分数 6=最高 7=存活 8=屏宽 9=屏高 }
  A: array[0..9] of integer;

procedure resetGame();
begin
  A[0] := A[8] div 2 - 40;
  A[1] := A[8] div 2;
  A[2] := 70;
  A[3] := 3;
  A[4] := 5;
  A[5] := 0;
  A[7] := 1;
end;

procedure draw();
begin
  ui_clear(-15724520);
  ui_text(8, 8, '得分', -6643536, 13, 0);
  ui_rect(58, 11, A[5], 10, -11409298, 1, 0, 0);
  ui_text(A[8] div 2, 8, '最高', -6643536, 13, 1);
  ui_rect(A[8] div 2 + 46, 11, A[6], 10, -63488, 1, 0, 0);
  ui_rect(A[0], A[9] - 40, 80, 12, -63488, 1, 0, 6);
  ui_circle(A[1], A[2], 9, -131246, 1, 0);
  if A[7] = 0 then
  begin
    ui_text(A[8] div 2, A[9] div 2, '按回车重开', -131246, 16, 1);
  end;
  ui_present();
end;

procedure tick();
begin
  if A[7] = 0 then
  begin
    exit;
  end;
  A[1] := A[1] + A[3];
  A[2] := A[2] + A[4];
  if A[1] < 10 then
  begin
    A[1] := 10;
    A[3] := 0 - A[3];
  end;
  if A[1] > A[8] - 10 then
  begin
    A[1] := A[8] - 10;
    A[3] := 0 - A[3];
  end;
  if A[2] < 30 then
  begin
    A[2] := 30;
    A[4] := 0 - A[4];
  end;
  { 接住：球落到挡板带上、且横向落在挡板范围内 }
  if (A[2] > A[9] - 52) and (A[2] < A[9] - 30) then
  begin
    if (A[1] > A[0] - 9) and (A[1] < A[0] + 89) then
    begin
      A[4] := 0 - A[4];
      A[2] := A[9] - 52;
      A[5] := A[5] + 10;
      if A[5] > A[6] then
      begin
        A[6] := A[5];
      end;
      ui_beep(880, 30);
    end;
  end;
  if A[2] > A[9] then
  begin
    A[7] := 0;
    ui_beep(220, 260);
    draw();
    if ui_dlg_msg('接方块', '没接住，这一局结束。再来一局？（选「否」退出）', 0) <> 0 then begin ui_win_close(); exit; end;
    resetGame();
  end;
end;

var
  w, h, tid, t, k: integer;

begin
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
  A[8] := w;
  A[9] := h;
  ui_win_open('接方块', w, h);
  ui_keep_on(1);
  resetGame();
  tid := ui_timer_set(40, 0);

  while ui_win_closed() = 0 do
  begin
    draw();
    t := ui_wait_msg(0);
    if t = 10 then
    begin
      break;
    end;
    if t = 9 then
    begin
      tick();
    end;
    if t = 1 then
    begin
      k := ui_msg_a();
      if k = 27 then
      begin
        break;
      end;
      if k = 37 then
      begin
        A[0] := A[0] - 20;
        if A[0] < 4 then
        begin
          A[0] := 4;
        end;
      end;
      if k = 39 then
      begin
        A[0] := A[0] + 20;
        if A[0] > w - 84 then
        begin
          A[0] := w - 84;
        end;
      end;
      if k = 13 then
      begin
        resetGame();
      end;
    end;
  end;

  ui_timer_kill(tid);
  ui_keep_on(0);
  ui_win_close();
end.
