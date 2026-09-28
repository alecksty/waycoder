{ 接方块 —— 用 **Pascal** 写的手机游戏
  Catch -- a mobile game written in Pascal

  玩法：左右方向键移动底部挡板，把落下来的球弹回去；没接住就结束。每接住一次 +10 分。
  Gameplay: move the bottom paddle with the left/right arrow keys and bounce the falling ball back; missing it ends the game. Each catch scores +10.

  ◆ 手机那套 UI
  * The mobile UI set

  开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
  Window / drawing / input / timers are written in C (Lib/shared/src/vmlui.c -> vmlui.vml),
  由 `vmltool.config.xml` 的 `<Language Name="pascal" Libs="vmlui.vml">` 挂上来。
  pulled in by the vmltool.config.xml entry Language Name="pascal" Libs="vmlui.vml".

  ◆ 为什么 Pascal 这份可以按「全局数组 + 无参过程」自然写
  * Why this Pascal version can be written naturally as global array + parameterless procedures

  别的语言（Kotlin / Swift / Go / Python）都有「模块级数组基址不对」或「函数看不见它」
  Other languages (Kotlin / Swift / Go / Python) all have problems like a wrong module-level array base address or
  这类问题，只能把状态塞进 main 或内联。Pascal 的 `var` 段全局量是语言原生语义、
  a function not seeing it, so state has to be pushed into main or inlined. Pascal's var-section globals are native language semantics and
  过程直接可见，所以这份保留了最自然的结构 —— 也正因为如此，它是**结构上最接近
  are directly visible to procedures, so this version keeps the most natural structure; because of that it is the structurally closest
  C 版**的一份，适合当对照。
  to the C version and makes a good reference.

  ◆ 写法要求
  * Coding rules

    · 裸调库函数不写声明（同 corpus/pascal/skel.pas）。
  . call library functions without declarations (same as corpus/pascal/skel.pas).
    · 整除用 `div`；颜色写负数十进制。
  . use div for integer division; write colours as negative decimals.
    · 过程名不叫 `step`：Pascal 标准过程里已有 `Inc()` 这类，避开常见名免得撞上。
  . do not name a procedure step: Pascal already has standard procedures like Inc(), so avoid common names to prevent collisions. }

program Catch;

var
  { 状态：0=挡板x 1=球x 2=球y 3=球dx 4=球dy 5=分数 6=最高 7=存活 8=屏宽 9=屏高 }
  // state: 0=paddle x 1=ball x 2=ball y 3=ball dx 4=ball dy 5=score 6=best 7=alive 8=screen w 9=screen h
  A: array[0..9] of integer;

  { 界面语言：0 = 中文 / 1 = 英文（ui_get_language 是 syscall，开局查一次存进 lang，
    文案也在这里一次算好；之后每帧绘制只用变量）
    UI language: 0 = Chinese / 1 = English (ui_get_language is a syscall: query it once at startup into lang
    and compute the strings here too; every later frame only uses the variables) }
  lang: integer;
  sScore, sBest, sRestart, sTitle, sOver: string;

procedure initLang();
begin
  lang := ui_get_language();
  if lang = 0 then sScore := '得分' else sScore := 'Score';
  if lang = 0 then sBest := '最高' else sBest := 'Best';
  if lang = 0 then sRestart := '按回车重开' else sRestart := 'Press Enter to restart';
  if lang = 0 then sTitle := '接方块' else sTitle := 'Catch';
  if lang = 0 then sOver := '没接住，这一局结束。再来一局？（选「否」退出）' else sOver := 'Missed - this round is over. Play again? (choose No to quit)';
end;

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
  ui_text(8, 8, sScore, -6643536, 13, 0);
  ui_rect(58, 11, A[5], 10, -11409298, 1, 0, 0);
  ui_text(A[8] div 2, 8, sBest, -6643536, 13, 1);
  ui_rect(A[8] div 2 + 46, 11, A[6], 10, -63488, 1, 0, 0);
  ui_rect(A[0], A[9] - 40, 80, 12, -63488, 1, 0, 6);
  ui_circle(A[1], A[2], 9, -131246, 1, 0);
  if A[7] = 0 then
  begin
    ui_text(A[8] div 2, A[9] div 2, sRestart, -131246, 16, 1);
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
  // caught: the ball reaches the paddle band and lands horizontally within the paddle span
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
      // 音效：单音 ui_beep（v0.96.509 从音序器换回来 ——
      // Sound effect: single-tone ui_beep (switched back from the sequencer in v0.96.509 —
      //   那一版多声部叠加 / 长音拖尾在真机上破音）
      //   that version's multi-voice layering / long sustained tones broke up on real devices)
      ui_beep(1047, 165);
    end;
  end;
  if A[2] > A[9] then
  begin
    A[7] := 0;
    // 音效：单音 ui_beep；**结局音取最低音**（接住 1047 / 没接住 131，差得开）
    // Sound effect: single-tone ui_beep; **the ending tone takes the lowest note** (caught 1047 / missed 131, far enough apart to tell)
    ui_beep(131, 320);
    draw();
    if ui_dlg_msg(sTitle, sOver, 0) <> 0 then begin ui_win_close(); exit; end;
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
  initLang();
  ui_win_open(sTitle, w, h);
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
