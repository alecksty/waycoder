' ═══════════════════════════════════════════════════════════════════════════
' WayCoder UI 接口 —— BASIC 侧的 `NATIVE` 声明表
'
' 用法（**一行即可**，写新版 UI 程序不用再抄一屏声明）：
'
'     '$INCLUDE: 'waycoder_ui.bi'
'
'     r = ui_win_open("演示", 360, 620)
'     DO WHILE ui_win_closed() = 0
'       ui_clear(0)
'       ui_text 20, 30, "你好", 15, 24, 0
'       ui_present
'     LOOP
'
' ⚠ 本文件是**生成物**：`python3 scripts/make-basic-ui-bi.py`
'   （真源 = `Lib/c/waycoder_ui.h`）。接口有改动请改那个头文件再重跑，
'   别在这里手改 —— 手改出来的是**第二份签名表**，迟早与宿主不一致。
'
' ⚠ `NATIVE SUB/FUNCTION` = 「这是外部的符号，别加 sub_/func_ 前缀」，
'   **不需要**再写 `END SUB` / `END FUNCTION`（NATIVE 是纯声明，本来就没有体）。
' ⚠ 形参**默认按值传递**（外部函数的约定；BASIC 自己的 SUB/FUNCTION 才是默认按引用）。
'   所以字符串实参传的是**首地址**、整数实参传的是**值**。
' ⚠ 参数类型说明：`char*` → `STRING`，`int`/`int*` → `INTEGER`，`float` → `SINGLE`，
'   `double` → `DOUBLE`。完整语义（每个号干什么）见 `docs/VML宿主接口.md`。
' ═══════════════════════════════════════════════════════════════════════════

NATIVE FUNCTION ui_dlg_msg(title AS STRING, body AS STRING, style AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_dlg_select(title AS STRING, body AS STRING, opts AS STRING, n AS INTEGER, p4 AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_dlg_multi(title AS STRING, body AS STRING, opts AS STRING, n AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_dlg_input(title AS STRING, prompt AS STRING, buf AS STRING, cap AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_win_open(title AS STRING, w AS INTEGER, h AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_win_open_ex(title AS STRING, w AS INTEGER, h AS INTEGER, rotatable AS INTEGER, gamepad AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_win_open_pc(title AS STRING, w AS INTEGER, h AS INTEGER, rotatable AS INTEGER, keyboard AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_win_close() AS INTEGER
NATIVE FUNCTION ui_win_closed() AS INTEGER
NATIVE FUNCTION ui_scr_w() AS INTEGER
NATIVE FUNCTION ui_scr_h() AS INTEGER
NATIVE FUNCTION ui_orientation() AS INTEGER
NATIVE SUB ui_clear(p0 AS INTEGER)
NATIVE SUB ui_pixel(x AS INTEGER, y AS INTEGER, p2 AS INTEGER)
NATIVE SUB ui_line(x1 AS INTEGER, y1 AS INTEGER, x2 AS INTEGER, y2 AS INTEGER, p4 AS INTEGER, lw AS INTEGER)
NATIVE SUB ui_rect(x AS INTEGER, y AS INTEGER, w AS INTEGER, h AS INTEGER, p4 AS INTEGER, fill AS INTEGER, lw AS INTEGER, radius AS INTEGER)
NATIVE SUB ui_circle(cx AS INTEGER, cy AS INTEGER, r AS INTEGER, p3 AS INTEGER, fill AS INTEGER, lw AS INTEGER)
NATIVE SUB ui_ellipse(cx AS INTEGER, cy AS INTEGER, rx AS INTEGER, ry AS INTEGER, p4 AS INTEGER, fill AS INTEGER, lw AS INTEGER)
NATIVE SUB ui_icon(x AS INTEGER, y AS INTEGER, name AS STRING, size AS INTEGER, p4 AS INTEGER)
NATIVE SUB ui_image(x AS INTEGER, y AS INTEGER, path AS STRING, w AS INTEGER, h AS INTEGER)
NATIVE SUB ui_present()
NATIVE FUNCTION ui_flood_fill(x AS INTEGER, y AS INTEGER, p2 AS INTEGER, border AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_get_image(x AS INTEGER, y AS INTEGER, w AS INTEGER, h AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_put_image(x AS INTEGER, y AS INTEGER, handle AS INTEGER, mode AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_screenshot(path AS STRING) AS INTEGER
NATIVE SUB ui_text(x AS INTEGER, y AS INTEGER, s AS STRING, p3 AS INTEGER, size AS INTEGER, anchor AS INTEGER)
NATIVE SUB ui_text_styled(x AS INTEGER, y AS INTEGER, s AS STRING, p3 AS INTEGER, size AS INTEGER, anchor AS INTEGER, style AS INTEGER)
NATIVE SUB ui_text_v(x AS INTEGER, y AS INTEGER, s AS STRING, p3 AS INTEGER, size AS INTEGER, anchor AS INTEGER, valign AS INTEGER, style AS INTEGER)
NATIVE SUB ui_set_font(size AS INTEGER, style AS INTEGER, p2 AS INTEGER, anchor AS INTEGER)
NATIVE SUB ui_set_valign(valign AS INTEGER)
NATIVE SUB ui_text_cur(x AS INTEGER, y AS INTEGER, s AS STRING)
NATIVE FUNCTION ui_call_json(p0 AS STRING, args_json AS STRING, out_buf AS STRING, cap AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_call_json_s(p0 AS STRING, args_json AS STRING) AS INTEGER
NATIVE FUNCTION ui_call_json_len() AS INTEGER
NATIVE FUNCTION ui_call_json_at(i AS INTEGER) AS INTEGER
NATIVE SUB ui_call_json_print()
NATIVE FUNCTION ui_wait_msg(timeout_ms AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_poll_msg() AS INTEGER
NATIVE FUNCTION ui_msg_type() AS INTEGER
NATIVE FUNCTION ui_msg_a() AS INTEGER
NATIVE FUNCTION ui_msg_b() AS INTEGER
NATIVE SUB ui_gclear()
NATIVE SUB ui_piece_init()
NATIVE FUNCTION ui_piece_cell(pid AS INTEGER, rot AS INTEGER, which AS INTEGER) AS INTEGER
NATIVE SUB ui_gset(idx AS INTEGER, val AS INTEGER)
NATIVE FUNCTION ui_gget(idx AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_poll(msg AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_wait(msg AS INTEGER, timeout_ms AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_poll_ex(msg AS INTEGER, keep AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_wait_ex(msg AS INTEGER, timeout_ms AS INTEGER, keep AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_msg_count() AS INTEGER
NATIVE FUNCTION ui_msg_clear() AS INTEGER
NATIVE FUNCTION ui_timer_set(interval_ms AS INTEGER, tag AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_timer_kill(timerId AS INTEGER) AS INTEGER
NATIVE SUB ui_gradient(gradId AS STRING, radial AS INTEGER, color_a AS INTEGER, color_b AS INTEGER, a1 AS INTEGER, a2 AS INTEGER, a3 AS INTEGER, a4 AS INTEGER)
NATIVE SUB ui_path(d AS STRING, stroke AS INTEGER, p2 AS INTEGER, fill AS INTEGER, grad AS STRING, cap AS INTEGER, dash AS INTEGER)
NATIVE SUB ui_polygon(pts AS INTEGER, count AS INTEGER, fill AS INTEGER, stroke AS INTEGER, p4 AS INTEGER, grad AS STRING)
NATIVE SUB ui_polyline(pts AS INTEGER, count AS INTEGER, stroke AS INTEGER, p3 AS INTEGER, grad AS STRING)
NATIVE SUB ui_rect_grad(x AS INTEGER, y AS INTEGER, w AS INTEGER, h AS INTEGER, grad AS STRING, radius AS INTEGER)
NATIVE SUB ui_circle_grad(cx AS INTEGER, cy AS INTEGER, r AS INTEGER, grad AS STRING)
NATIVE FUNCTION ui_brush_solid(p0 AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_brush_linear(color_a AS INTEGER, color_b AS INTEGER, x1 AS INTEGER, y1 AS INTEGER, x2 AS INTEGER, y2 AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_brush_radial(color_a AS INTEGER, color_b AS INTEGER, cx AS INTEGER, cy AS INTEGER, r AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_brush_named(gradId AS STRING) AS INTEGER
NATIVE FUNCTION ui_set_fill(brush AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_set_pen(brush AS INTEGER, p1 AS INTEGER, cap AS INTEGER, dash AS INTEGER, arrow AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_set_text_brush(brush AS INTEGER) AS INTEGER
NATIVE SUB ui_draw_rect(x AS INTEGER, y AS INTEGER, w AS INTEGER, h AS INTEGER, radius AS INTEGER)
NATIVE SUB ui_draw_circle(cx AS INTEGER, cy AS INTEGER, r AS INTEGER)
NATIVE SUB ui_draw_ellipse(cx AS INTEGER, cy AS INTEGER, rx AS INTEGER, ry AS INTEGER)
NATIVE SUB ui_draw_line(x1 AS INTEGER, y1 AS INTEGER, x2 AS INTEGER, y2 AS INTEGER)
NATIVE SUB ui_draw_poly(pts AS INTEGER, count AS INTEGER, p2 AS INTEGER)
NATIVE SUB ui_draw_path(d AS STRING)
NATIVE SUB ui_draw_text(x AS INTEGER, y AS INTEGER, s AS STRING)
NATIVE SUB ui_draw_star(cx AS INTEGER, cy AS INTEGER, r_out AS INTEGER, r_in AS INTEGER, points AS INTEGER, rot AS INTEGER)
NATIVE SUB ui_draw_regular(cx AS INTEGER, cy AS INTEGER, r AS INTEGER, n AS INTEGER, rot AS INTEGER)
NATIVE SUB ui_draw_ring(cx AS INTEGER, cy AS INTEGER, r_out AS INTEGER, r_in AS INTEGER)
NATIVE SUB ui_draw_pie(cx AS INTEGER, cy AS INTEGER, r AS INTEGER, a0 AS INTEGER, a1 AS INTEGER)
NATIVE SUB ui_draw_heart(cx AS INTEGER, cy AS INTEGER, size AS INTEGER)
NATIVE SUB ui_ellipse_grad(cx AS INTEGER, cy AS INTEGER, rx AS INTEGER, ry AS INTEGER, grad AS STRING)
NATIVE FUNCTION ui_rand(n AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_tick() AS INTEGER
NATIVE SUB ui_beep(freq AS INTEGER, ms AS INTEGER)
NATIVE SUB ui_vibrate(ms AS INTEGER, strength AS INTEGER)
NATIVE SUB ui_keep_on(p0 AS INTEGER)
NATIVE SUB ui_store_set(p0 AS STRING, value AS STRING)
NATIVE FUNCTION ui_store_get(p0 AS STRING, buf AS STRING, cap AS INTEGER) AS INTEGER
NATIVE FUNCTION ui_argc() AS INTEGER
NATIVE FUNCTION ui_arg(i AS INTEGER, buf AS STRING, cap AS INTEGER) AS INTEGER
