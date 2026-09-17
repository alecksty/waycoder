# 接方块 —— 用 **Ruby** 写的手机游戏
#
# 玩法：左右方向键移动底部挡板，把落下来的球弹回去；没接住就结束。每接住一次 +10 分。
#
# ◆ 手机那套 UI
#
# 开窗 / 绘图 / 输入 / 定时器是 C 写的（`Lib/shared/src/vmlui.c` → `vmlui.vml`），
# 由 `vmltool.config.xml` 的 `<Language Name="ruby" Libs="vmlui.vml">` 挂上来。
#
# ◆ 为什么这份是**完全平铺**的（没有 def、逻辑内联在主循环里）
#
# 两条本前端实测出来的限制（2026-09-17）：
#   ① **词法器不认 `&&`** —— `Unexpected char: &`。多个条件只能写成嵌套 `if`。
#   ② **`def` 的支持有问题** —— 一个最朴素的 `def resetGame … end` 会报
#      `Unexpected token: End(end)`（报在配对的 `end` 上，像是块没被正确打开）。
#      `corpus/ruby/skel.rb` 里**一个函数都没有**，所以这条从来没被骨架覆盖到。
#      ⇒ 本份例程不用任何方法，状态放顶层局部变量、draw/tick 内联进主循环。
#   这两条要修的话在前端（`RubyCompiler`），不是例程的问题。
#
# 另一处写法：颜色写负数十进制；裸调库函数不写声明（同 skel.rb）。

# 状态：0=挡板x 1=球x 2=球y 3=球dx 4=球dy 5=分数 6=最高 7=存活 8=屏宽 9=屏高
a0 = 0
a1 = 0
a2 = 0
a3 = 0
a4 = 0
a5 = 0
a6 = 0
a7 = 0

w = ui_scr_w()
h = ui_scr_h()
if w <= 0
  w = 360
end
if h <= 0
  h = 620
end
ui_win_open("接方块", w, h)
ui_keep_on(1)

a0 = w / 2 - 40
a1 = w / 2
a2 = 70
a3 = 3
a4 = 5
a5 = 0
a6 = 0
a7 = 1

tid = ui_timer_set(40, 0)

while ui_win_closed() == 0
  # ── draw ──
  ui_clear(-15724520)
  ui_text(8, 8, "得分", -6643536, 13, 0)
  ui_rect(58, 11, a5, 10, -11409298, 1, 0, 0)
  ui_text(w / 2, 8, "最高", -6643536, 13, 1)
  ui_rect(w / 2 + 46, 11, a6, 10, -63488, 1, 0, 0)
  ui_rect(a0, h - 40, 80, 12, -63488, 1, 0, 6)
  ui_circle(a1, a2, 9, -131246, 1, 0)
  if a7 == 0
    ui_text(w / 2, h / 2, "按回车重开", -131246, 16, 1)
  end
  ui_present()

  t = ui_wait_msg(0)
  if t == 10
    break
  end

  if t == 9
    if a7 != 0
      a1 = a1 + a3
      a2 = a2 + a4
      if a1 < 10
        a1 = 10
        a3 = 0 - a3
      end
      if a1 > w - 10
        a1 = w - 10
        a3 = 0 - a3
      end
      if a2 < 30
        a2 = 30
        a4 = 0 - a4
      end
      # ⚠ 不认 `&&` ⇒ 嵌套 if
      if a2 > h - 52
        if a2 < h - 30
          if a1 > a0 - 9
            if a1 < a0 + 89
              a4 = 0 - a4
              a2 = h - 52
              a5 = a5 + 10
              if a5 > a6
                a6 = a5
              end
              ui_beep(880, 30)
            end
          end
        end
      end
      if a2 > h
        a7 = 0
        ui_beep(220, 260)
        r = ui_dlg_msg("接方块", "没接住，这一局结束。
再来一局？（选「否」退出）", 0)
        if r != 0
            ui_win_close()
            break
        end
        a0 = w / 2 - 40
        a1 = w / 2
        a2 = 70
        a3 = 3
        a4 = 5
        a5 = 0
        a7 = 1
      end
    end
  end

  if t == 1
    k = ui_msg_a()
    if k == 27
      break
    end
    if k == 37
      a0 = a0 - 20
      if a0 < 4
        a0 = 4
      end
    end
    if k == 39
      a0 = a0 + 20
      if a0 > w - 84
        a0 = w - 84
      end
    end
    if k == 13
      a0 = w / 2 - 40
      a1 = w / 2
      a2 = 70
      a3 = 3
      a4 = 5
      a5 = 0
      a7 = 1
    end
  end
end

ui_timer_kill(tid)
ui_keep_on(0)
ui_win_close()
