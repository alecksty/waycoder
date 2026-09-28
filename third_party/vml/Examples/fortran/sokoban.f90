! 推箱子 —— 用 **Fortran** 写的手机游戏
! Sokoban -- a mobile game written in **Fortran**
!
! 玩法：方向键推箱子，把两个箱子都推到目标点上即过关（回车重开、ESC 退出）。
! Gameplay: push the boxes with the arrow keys; get both boxes onto the target cells to win (Enter restarts, ESC exits).
! 箱子只能推、不能拉；推到墙或另一个箱子上就推不动。
! Boxes can only be pushed, never pulled; pushing into a wall or another box does not move them.
!
! 与前面几份刻意不同：这份是**双数组状态 + 移动规则 + 过关判定**，
! Deliberately different from the earlier files: this one is **two-array state + movement rules + win detection**,
! 一帧里没有任何连续位移 —— 照的是「离散格子 + 规则表」这条路径。
! with no continuous motion within a frame -- it exercises the "discrete cells + rule table" path.
! 也刻意不画数字：`ui_text` 要字符串，而 Fortran 没有现成的数字转字符串路径，
! It also deliberately draws no digits: `ui_text` needs a string, and Fortran has no ready-made number-to-string path,
! 所以状态一律用**颜色与形状**表达（目标=空心框、箱子=实心方块、箱子归位=变绿）。
! so state is expressed purely with **color and shape** (target = hollow frame, box = solid square, box in place = turns green).
!
! ◆ 手机那套 UI
! ◆ The mobile UI set
!
! 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
! Window opening / drawing / input / timers are written in C (`Lib/shared/src/vmlui.c` -> `vmlui.vml`),
! 由 `vmltool.config.xml` 的 `<Language Name="fortran" Libs="vmlui.vml">` 挂上来。
! pulled in by `<Language Name="fortran" Libs="vmlui.vml">` in `vmltool.config.xml`.
!
! ◆ 写法要求（沿用 corpus/fortran/skel.f90 的两条）
! ◆ Style requirements (the two from corpus/fortran/skel.f90)
!
!   · `call ui_rect(...)` 会被编成 `CALL sub_ui_rect`，而链接器只剥
!   · `call ui_rect(...)` compiles to `CALL sub_ui_rect`, and the linker strips only
!     func_/word_/method_/var_ 四种前缀、**不含 sub_** ⇒ 碰不到 UI 库。
!     the four prefixes func_/word_/method_/var_, **not sub_** => it never reaches the UI library.
!     所以一律写成**函数调用表达式** `k = ui_rect(...)`（编成 func_ui_rect）。
!     So always write a **function-call expression** `k = ui_rect(...)` (compiles to func_ui_rect).
!   · Fortran 数组 **1 基**：扁平下标 = (行-1)*8 + 列 + 1。
!   · Fortran arrays are **1-based**: flat index = (row-1)*8 + column + 1.

program sokoban
  implicit none
  integer :: map(64)     ! 0=地板 1=墙 2=目标点
  ! 0=floor 1=wall 2=target cell
  integer :: obj(64)     ! 0=空 1=箱子 2=玩家
  ! 0=empty 1=box 2=player
  integer :: i
  integer :: k
  integer :: w
  integer :: h
  integer :: cell
  integer :: ox
  integer :: oy
  integer :: pr
  integer :: pc
  integer :: done
  integer :: t
  integer :: key
  integer :: tid
  integer :: nr
  integer :: nc
  integer :: ni
  integer :: over
  integer :: goals
  integer :: lang

  ! 界面语言：开局问一次宿主要中文还是英文（0=中文 1=英文），之后整局按它分支。
  ! UI language: ask the host once at start whether Chinese or English (0=Chinese 1=English), then branch on it for the whole round.
  ! ⚠ 别在每帧里调 —— 那是一次 syscall。
  ! ⚠ Do not call it every frame -- that is a syscall.
  ! ⚠ 本前端**没有三元/merge**，界面文字只能靠单行 `if` 二选一
  ! ⚠ This frontend has **no ternary/merge**, so UI text can only pick between two options with a single-line `if`
  !   （sokoban 本来就在用这个写法，见下面的 `if (key == 37) nc = pc - 1`）。
  !   (sokoban already used this style, see `if (key == 37) nc = pc - 1` below).
  lang = ui_get_language()

  w = ui_scr_w()
  h = ui_scr_h()
  if (w <= 0) then
    w = 360
  end if
  if (h <= 0) then
    h = 620
  end if
  if (lang == 0) k = ui_win_open('推箱子', w, h)
  if (lang /= 0) k = ui_win_open('Sokoban', w, h)

  cell = (w - 40) / 8
  ! ⚠ 本前端在 `if` 条件里解析不了「紧跟括号的除法」（`Unexpected token: Div(/)`）⇒ 先算进变量
  ! ⚠ This frontend cannot parse "a division right after a parenthesis" in an `if` condition (`Unexpected token: Div(/)`) => compute it into a variable first
  k = (h - 150) / 8
  if (k < cell) then
    cell = k
  end if
  if (cell < 8) then
    cell = 8
  end if
  ox = (w - cell * 8) / 2
  oy = 90

  ! ── 关卡：边框是墙，两个目标点、两个箱子 ──
  ! -- Level: the border is walls, with two targets and two boxes --
  ! 地图用代码建（不用 64 个字面量），内部障碍摆两块
  ! The map is built in code (not with 64 literals), with two internal obstacles placed in it
  do i = 1, 64
    map(i) = 0
    obj(i) = 0
  end do

  ! 边框：第 1/8 行、第 1/8 列
  ! Border: rows 1/8, columns 1/8
  do i = 1, 8
    map(i) = 1                 ! 顶行
    ! top row
    map(56 + i) = 1            ! 底行
    ! bottom row
    map((i - 1) * 8 + 1) = 1   ! 左列
    ! left column
    map((i - 1) * 8 + 8) = 1   ! 右列
    ! right column
  end do
  ! 两块内部障碍
  ! Two internal obstacles
  map(3 * 8 + 4 + 1) = 1
  map(5 * 8 + 4 + 1) = 1

  ! 目标点：(行,列) = (3,3) 与 (5,6)
  ! Targets: (row,column) = (3,3) and (5,6)
  map((3 - 1) * 8 + 3 + 1) = 2
  map((5 - 1) * 8 + 6 + 1) = 2
  ! 箱子：(2,2) 与 (3,6)
  ! Boxes: (2,2) and (3,6)
  obj((2 - 1) * 8 + 2 + 1) = 1
  obj((3 - 1) * 8 + 6 + 1) = 1
  ! 玩家：(5,3)
  ! Player: (5,3)
  pr = 5
  pc = 3
  obj((pr - 1) * 8 + pc + 1) = 2

  k = ui_keep_on(1)
  done = 0
  tid = ui_timer_set(120, 0)

  do while (ui_win_closed() == 0)

    ui_sfx_tick()
    ! ── 过关判定：目标点上都有箱子 ──
    ! -- Win detection: every target cell has a box --
    goals = 0
    do i = 1, 64
      if (map(i) == 2) then
        if (obj(i) == 1) then
          goals = goals + 1
        end if
      end if
    end do
    if (goals == 2) then
      done = 1
    end if

    ! ── draw ──
    k = ui_clear(-15723504)
    ! 用显式两层循环画（比一维反解行号清楚）
    ! Draw with an explicit two-level loop (clearer than reverse-solving row numbers from a flat index)
    do nr = 1, 8
      do nc = 1, 8
        ni = (nr - 1) * 8 + nc
        if (map(ni) == 1) then
          k = ui_rect(ox + (nc - 1) * cell, oy + (nr - 1) * cell, cell, cell, -11841664, 1, 0, 0)
        else
          k = ui_rect(ox + (nc - 1) * cell, oy + (nr - 1) * cell, cell, cell, -15719856, 1, 0, 1)
        end if
        if (map(ni) == 2) then
          k = ui_rect(ox + (nc - 1) * cell + cell / 4, oy + (nr - 1) * cell + cell / 4, cell / 2, cell / 2, -11513776, 0, 3, 0)
        end if
        if (obj(ni) == 1) then
          if (map(ni) == 2) then
            k = ui_rect(ox + (nc - 1) * cell + 3, oy + (nr - 1) * cell + 3, cell - 6, cell - 6, -131246, 1, 0, 2)
          else
            k = ui_rect(ox + (nc - 1) * cell + 3, oy + (nr - 1) * cell + 3, cell - 6, cell - 6, -11409298, 1, 0, 2)
          end if
        end if
        if (obj(ni) == 2) then
          k = ui_circle(ox + (nc - 1) * cell + cell / 2, oy + (nr - 1) * cell + cell / 2, cell / 2 - 6, -63488, 1, 0)
        end if
      end do
    end do

    if (done == 0) then
      if (lang == 0) k = ui_text(8, 8, '推箱子：把两个箱子推到空心格', -6643536, 13, 0)
      if (lang /= 0) k = ui_text(8, 8, 'Push boxes onto the hollow targets', -6643536, 13, 0)
    else
      if (lang == 0) k = ui_text(8, 8, '过关！回车再来一次', -131246, 16, 0)
      if (lang /= 0) k = ui_text(8, 8, 'Cleared! Press Enter to restart', -131246, 16, 0)
    end if
    k = ui_present()

    t = ui_wait_msg(0)
    if (t == 10) then
      exit
    end if

    if (t == 1) then
      key = ui_msg_a()
      if (key == 27) then
        exit
      end if
      if (key == 13) then
        ! 重开：把箱子与玩家放回原位
        ! Restart: put the boxes and the player back in their original places
        obj((2 - 1) * 8 + 2 + 1) = 1
        obj((3 - 1) * 8 + 6 + 1) = 1
        obj((pr - 1) * 8 + pc + 1) = 0
        pr = 5
        pc = 3
        obj((pr - 1) * 8 + pc + 1) = 2
        done = 0
      end if

      ! 方向键 → 目标格
      ! Arrow key -> target cell
      nr = pr
      nc = pc
      if (key == 37) nc = pc - 1
      if (key == 39) nc = pc + 1
      if (key == 38) nr = pr - 1
      if (key == 40) nr = pr + 1
      if (nr /= pr .or. nc /= pc) then
        ni = (nr - 1) * 8 + nc
        if (map(ni) /= 1) then
          if (obj(ni) == 0) then
            ! 空地：走过去
            ! Empty floor: step onto it
            obj((pr - 1) * 8 + pc + 1) = 0
            pr = nr
            pc = nc
            obj((pr - 1) * 8 + pc + 1) = 2
          else
            if (obj(ni) == 1) then
              ! 箱子：看它后面那格能不能放
              ! A box: check whether the cell behind it is free
              over = (nr - pr) + (nc - pc)
              if (over == -1) then
                if (map((nr - 1) * 8 + nc - 1) == 1) over = 0
                if (obj((nr - 1) * 8 + nc - 1) /= 0) over = 0
                if (over /= 0) then
                  obj((nr - 1) * 8 + nc - 1) = 1
                end if
              end if
              if (over == 1) then
                if (map((nr - 1) * 8 + nc + 1) == 1) over = 0
                if (obj((nr - 1) * 8 + nc + 1) /= 0) over = 0
                if (over /= 0) then
                  obj((nr - 1) * 8 + nc + 1) = 1
                end if
              end if
              if (over == -8) then
                if (map((nr - 2) * 8 + nc) == 1) over = 0
                if (obj((nr - 2) * 8 + nc) /= 0) over = 0
                if (over /= 0) then
                  obj((nr - 2) * 8 + nc) = 1
                end if
              end if
              if (over == 8) then
                if (map(nr * 8 + nc) == 1) over = 0
                if (obj(nr * 8 + nc) /= 0) over = 0
                if (over /= 0) then
                  obj(nr * 8 + nc) = 1
                end if
              end if
              ! 推得动才走
              ! Only move if it can be pushed
              if (over /= 0) then
                obj(ni) = 0
                obj((pr - 1) * 8 + pc + 1) = 0
                pr = nr
                pc = nc
                obj((pr - 1) * 8 + pc + 1) = 2
                call ui_sfx_add(0, 84, 0, 2, 75, 1)      ! 推到位：上行两音
                ! Pushed into place: two rising notes
                call ui_sfx_add(1, 91, 1, 4, 70, 1)
              else
                call ui_sfx_add(2, 48, 0, 2, 80, 2)   ! 推不动：低闷
                ! Cannot push: low and muffled
              end if
            end if
          end if
        else
          call ui_sfx_add(2, 48, 0, 2, 80, 2)   ! 推不动：低闷
          ! Cannot push: low and muffled
        end if
      end if
    end if
  end do

  k = ui_timer_kill(tid)
  k = ui_keep_on(0)
  k = ui_win_close()
end program sokoban
