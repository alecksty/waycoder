! opengl.f90 — VML OpenGL Bindings (Fortran)
! OpenGL 图形库 FFI 文档 (stub)
!
! VML 通过 glhelper 动态库 (libglhelper) 提供 OpenGL 访问。
! 使用 FFI SYSCALL (370-375) 调用。
!
! ============================================================
! 计划支持的操作 (通过 FFI):
!
! glh_init(width, height) -> window_handle
!   初始化 OpenGL 窗口
!   参数: 宽度, 高度 (int32)
!   返回: 窗口句柄
!
! glh_should_close(handle) -> int
!   检查窗口是否应关闭
!   返回: 1=关闭, 0=继续
!
! glh_swap() -> void
!   交换缓冲区 (双缓冲)
!
! glh_destroy() -> void
!   销毁窗口
!
! glh_set_title(title, handle)
!   设置窗口标题
!   参数: 标题字符串地址, 窗口句柄
!
! glh_get_time_i(handle) -> int
!   获取帧时间
!   返回: 毫秒数
!
! glh_draw_cube_i(handle)
!   绘制旋转立方体 (演示用)
!   内部包含完整渲染循环
! ============================================================

! ============================================================
! 使用流程:
!
!   1. 获取平台 (SYSCALL 374)
!   2. 加载 libglhelper 动态库 (SYSCALL 370)
!   3. 初始化窗口 (glh_init)
!   4. 渲染循环:
!      - 检查关闭 (glh_should_close)
!      - 绘制 (glh_draw_cube_i / 自定义 OpenGL)
!      - 交换缓冲 (glh_swap)
!   5. 销毁窗口 (glh_destroy)
!   6. 关闭库 (SYSCALL 372)
! ============================================================

! ============================================================
! 注意:
! - Fortran 编译器当前不支持返回值函数
! - 此文件为文档占位，需要 libglhelper 动态库支持
! - 实际调用通过 asm("SYSCALL 373") 或 asm("CALL native_call_ex")
! - 跨平台路径:
!   macOS:   libglhelper.dylib
!   Linux:   libglhelper.so
!   Windows: libglhelper.dll
! ============================================================
