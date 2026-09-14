! skia_demo.f90 — VML Skia Graphics Bindings (Fortran)
! Skia 2D 图形库 FFI 文档 (stub)
!
! Skia 是 Google 开发的跨平台 2D 图形库 (Chrome/Android/Flutter 使用)。
! VML 通过 FFI 接口可调用 Skia 进行矢量图形绘制。
!
! ============================================================
! 计划支持的操作 (通过 FFI):
!
! 画布:
!   sk_canvas_create(w, h) -> canvas_handle
!     创建指定大小的画布
!
!   sk_canvas_save(canvas) -> void
!     保存画布状态
!
!   sk_canvas_restore(canvas) -> void
!     恢复画布状态
!
!   sk_canvas_export_png(canvas, path) -> int
!     导出画布为 PNG 文件
!     返回: 0 成功, <0 失败
!
! 绘图:
!   sk_draw_rect(canvas, x, y, w, h, r, g, b, a)
!     绘制矩形
!
!   sk_draw_oval(canvas, cx, cy, rx, ry, r, g, b, a)
!     绘制椭圆
!
!   sk_draw_line(canvas, x1, y1, x2, y2, r, g, b, a, width)
!     绘制线条
!
!   sk_draw_text(canvas, text, x, y, size, r, g, b, a)
!     绘制文本
!
!   sk_draw_path(canvas, points, count, r, g, b, a, width)
!     绘制折线/路径
!
! 绘制属性:
!   sk_set_fill_color(r, g, b, a)
!   sk_set_stroke_color(r, g, b, a)
!   sk_set_stroke_width(width)
! ============================================================

! ============================================================
! 使用流程:
!
!   1. 加载 libskia 动态库 (SYSCALL 370)
!   2. 创建画布 (sk_canvas_create)
!   3. 绘制图形 (sk_draw_rect / sk_draw_oval / ...)
!   4. 导出或显示结果 (sk_canvas_export_png)
!   5. 释放画布
!   6. 关闭库 (SYSCALL 372)
! ============================================================

! ============================================================
! 注意:
! - Fortran 编译器当前不支持返回值函数
! - 此文件为文档占位，需要 libskia 动态库
! - 实际使用通过 asm("SYSCALL 373") 或 asm("CALL native_call_ex")
! - Skia 绑定主要用于生成 PNG/SVG 输出或配合 ImGui 显示
! ============================================================
