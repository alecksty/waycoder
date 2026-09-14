! imgui_demo.f90 — VML Dear ImGui Bindings (Fortran)
! ImGui 即时模式 GUI 库 FFI 文档 (stub)
!
! Dear ImGui 是一个轻量级即时模式 GUI 库，适合调试和工具 UI。
!
! 目前 VML Fortran 编译器不支持函数返回值，Imgui 绑定
! 需要通过 FFI SYSCALL (370-375) 以原生调用的方式访问。
!
! ============================================================
! 计划支持的 ImGui 函数 (通过 FFI):
!
! 窗口:
!   igBegin(title)         — 开始窗口
!   igEnd()                — 结束窗口
!   igBeginChild(id, size) — 子窗口
!   igEndChild()           — 结束子窗口
!
! 控件:
!   igButton(label) -> bool        — 按钮
!   igCheckbox(label, value) -> bool — 复选框
!   igSliderFloat(label, v, min, max) -> float — 滑动条
!   igSliderInt(label, v, min, max) -> int
!   igInputText(label, buf) -> bool — 文本输入
!   igColorEdit3(label, col) -> bool — 颜色编辑
!
! 布局:
!   igSameLine()            — 同行排列
!   igSeparator()           — 分隔线
!   igSpacing()             — 间距
!
! 绘图:
!   igGetWindowDrawList() -> ptr    — 获取绘制列表
!   igAddLine(a, b, col)            — 绘制线条
!   igAddRect(a, b, col, rounding)  — 绘制矩形
!   igAddCircle(center, radius, col) — 绘制圆
!   igAddText(pos, text)            — 绘制文本
!
! 输入:
!   igIsMouseDown(button) -> bool
!   igGetMousePos() -> (x, y)
!   igIsKeyPressed(key) -> bool
! ============================================================

! ============================================================
! 注意:
! - Fortran 编译器当前不支持返回值函数
! - 此文件为文档占位，实际使用时需通过 asm("SYSCALL 373")
!   或 asm("CALL <native_call_ex>") 调用
! - 完整 ImGui 绑定需要编译 libimgui 动态库
! ============================================================
