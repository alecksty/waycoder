! demo_ui.f90 —— Fortran 第四层：**最新 UI 接口**（宿主 `ui_*` 图元 + 绘图窗口）
!
! 这一层和 BGI 那一路刻意不同。BGI 是"固定分辨率 + 索引色"的老世界，
! 这一层是**宿主窗口 + 0xAARRGGBB 真彩 + 统一消息队列**。四条关键差别：
!
!   ① **开窗显式**：`ui_win_open(标题, 宽, 高)`，宽高来自 `ui_scr_w()/ui_scr_h()`
!      （可用绘图区），不是写死的 640x480。
!   ② **屏幕方向要问**：`ui_orientation()` 返回 0=竖屏 / 1=横屏 —— 程序据此决定
!      "面板怎么摆"。这是**开窗前就能问**的（详见 `docs/VML宿主接口.md`）。
!   ③ **显式呈现**：图元只是追加进场景，`ui_present()` 才是"这一帧画完了"的帧边界。
!   ④ **输入是消息队列**：`ui_wait_msg(超时) / ui_msg_a() / ui_win_closed()` 收
!      键盘、触摸、定时器、窗口事件 —— 全部走同一个队列。
!      消息类型：1=KeyDown 2=KeyUp 3..5=鼠标 6..8=触摸 9=定时器 10=窗口关闭。
!
! 跑法（桌面 vmlcli）：
!   dotnet scripts/vmlcli/bin/Release/net10.0/vmlcli.dll Examples/fortran/demo_ui.f90 \
!       --screen 480x640 --frames out_frames/
!
! ⚠ **要看画面请用 `--frames 目录`，不要用 `--frame` 单个文件**：`ui_win_close()`（#521）
!   在宿主侧把整个场景置空（`VmlHostRuntime.WinClose` 的 `_scene = null`），
!   跑完之后 `--frame` 已经没场景可取；而 `--frames` 是每帧 present **当场拍快照**。
!
! ◆ **有界**（任务要求：按 N 帧或收到键就退出）—— 三条出口都写了：
!   · 收到**任意按键**（消息类型 1 = KeyDown）立刻退出
!   · 或画满 150 帧自动退出（每帧 `ui_wait_msg(30)` 最多等 30ms ⇒ 约 4.5 秒）
!   · 或用户点了标题栏的返回箭头（`ui_win_closed() /= 0`）
!   退出前 `ui_win_close()`，程序正常结束、退出码 0。
!
! ═══════════════════════════════════════════════════════════════════════
!  ⚠ 三条本前端的写法要求（每条都来自实测，不是风格偏好）
!
!  ① **调库函数一律写成函数调用表达式 `k = ui_rect(...)`**。
!     写成 `call ui_rect(...)` 会被编成 `CALL sub_ui_rect`，而**链接器只剥
!     `func_`/`word_`/`method_`/`var_` 四种前缀、不含 `sub_`** ⇒ 碰不到 UI 库。
!     （对照组：`call putchar(27)` 恰好能编成裸名 `CALL putchar`，所以那一句能跑
!      —— 见 `demo_tty.f90`。**别把某个 `call` 能跑推广到所有 `call`**。）
!
!  ② **一个子程序都不用**。`contains` 里的内部子程序**传不进实参**、
!     也**看不见宿主程序的变量**（实测 `call show(97)` 里 `v` 读到 0）；
!     外部子程序则直接「未定义的函数」。所以这份 demo 全部内联。
!
!  ③ **正文里不出现数字**。本前端没有整数转串（`Str()` 没有、`//` 拼串没有、
!     `character` 变量存不住字符串），而 `print` 浮点又会打出整数的位模式
!     （见 `demo_std.f90`）⇒ 状态一律用**形状与固定标签**表达：进度用 `ui_rect`
!     的宽度、方向用两个不同的词。这与同目录 `sokoban.f90` 的取舍一致。
!
!  附：颜色用的是**十进制负数**（`0xAARRGGBB` 作为 32 位有符号整数会溢出）。
!      每一条后面都标了它对应的十六进制值，改的时候照着改、别手算 ——
!      手算差了 1 个最低位，画出来"只差一点点"，肉眼根本看不出来。
! ═══════════════════════════════════════════════════════════════════════

program demo_ui
  implicit none
  integer :: w, h
  integer :: ori
  integer :: frame, maxf
  integer :: tid, msg, kc
  integer :: running
  integer :: k
  integer :: bx, by, bw, bh

  ! ① 问可用绘图区；拿不到就给个兜底尺寸（桌面脚手架 / 某些后端会给 0）
  w = ui_scr_w()
  h = ui_scr_h()
  if (w <= 0) then
    w = 360
  end if
  if (h <= 0) then
    h = 620
  end if

  ! ② 开窗 —— 宽高直接取"可用绘图区"，于是手机上就是整页
  k = ui_win_open('Fortran UI demo', w, h)

  ! ③ 问屏幕方向：0 = 竖屏，1 = 横屏
  !    ⚠ 这里本来想先把文字存进 `character(len=40) :: label1` 再画 —— **做不到**：
  !    本前端的 `character(len=…)` 声明直接编译错（「期望变量名（得到 LParen）」），
  !    而不带长度的 `character :: s` 虽然编得过、却**存不住字符串**（`s = 'AB'` 读回来是 0）。
  !    ⇒ 文字只能以**字面量**形式直接写在 `ui_text(...)` 那一句里，所以下面的
  !    if/else 是两份重复的绘制调用（而不是"选一句话再画一次"）。
  ori = ui_orientation()

  ! ④ 方向决定面板怎么摆：横屏把方块放右上，竖屏放右下
  bx = 24
  by = 24
  bw = 140
  bh = 100
  if (ori == 1) then
    bx = w - bw - 24
    by = 120
  else
    by = h - bh - 110
  end if

  maxf = 150
  frame = 0
  running = 1

  do while (running == 1)
    ! ── 每帧重画（保留模式：场景每帧从头追加）────────────────────
    k = ui_clear(-15724520)                                   ! 0xFF101018 深底

    ! 标题（方向那行只能按 if/else 各写一份字面量 —— 理由见上面第 ③ 条）
    k = ui_text(16, 16, 'Fortran UI demo', -6629121, 20, 0)   ! 0xFF9AD8FF 浅蓝
    if (ori == 1) then
      k = ui_text(16, 44, 'orientation = LANDSCAPE (1)', -11446, 15, 0)   ! 0xFFFFD34A
    else
      k = ui_text(16, 44, 'orientation = PORTRAIT (0)', -11446, 15, 0)    ! 0xFFFFD34A
    end if
    k = ui_text(16, 66, 'ui_win_open / ui_rect / ui_circle / ui_line / ui_text / ui_present', -7693656, 11, 0)  ! 0xFF8A9AA8

    ! 图元三件套：线 / 矩形 / 圆
    k = ui_line(16, 92, w - 16, 92, -13414294, 1)             ! 0xFF33506A 分隔线
    k = ui_rect(bx, by, bw, bh, -13730510, 1, 0, 8)           ! 0xFF2E7D32 深绿实心圆角框
    k = ui_rect(bx + 12, by + 12, bw - 24, bh - 24, -10044566, 0, 2, 0)  ! 0xFF66BB6A 浅绿空心
    k = ui_circle(w / 2, h / 2, 44, -1618884, 1, 0)           ! 0xFFE74C3C 红实心圆
    k = ui_circle(w / 2, h / 2, 20, -932849, 1, 0)            ! 0xFFF1C40F 黄内圆

    ! 帧计数进度条（不写字：这里没有整数转串，见文件头第 ③ 条）
    k = ui_rect(16, h - 62, w - 32, 14, -14536644, 1, 0, 3)   ! 0xFF22303C 进度条底
    k = ui_rect(16, h - 62, (w - 32) * frame / maxf, 14, -16727384, 1, 0, 3)  ! 0xFF00C2A8
    k = ui_text(16, h - 42, 'press any key to exit', -7693656, 13, 0)
    k = ui_text(w - 16, h - 42, 'ui_present every frame', -7693656, 13, 2)

    ! ── 帧边界 ────────────────────────────────────────────────────
    k = ui_present()
    frame = frame + 1

    ! 出口 A：画满 maxf 帧
    if (frame >= maxf) then
      running = 0
    end if

    ! 出口 B：收到任意按键（消息类型 1 = KeyDown）
    if (running == 1) then
      msg = ui_wait_msg(30)
      if (msg == 1) then
        kc = ui_msg_a()
        running = 0
      end if
    end if

    ! 出口 C：用户点了标题栏的返回箭头
    if (ui_win_closed() /= 0) then
      running = 0
    end if
  end do

  ! 正常收尾：关窗（定时器没用到就不必 kill）
  k = ui_win_close()
end program demo_ui
