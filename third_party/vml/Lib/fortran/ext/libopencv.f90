! libopencv.f90 — VML libopencv FFI Bindings (Fortran)
! 通过 VML FFI 系统调用加载并调用 OpenCV 动态库
!
! 使用前提:
!   1. 确保 libopencv.dylib / libopencv.so / libopencv.dll 已编译
!   2. OS 模式下可用 (--mode os)
!   3. 通过 SYSCALL 370-375 加载和调用
!
! 使用示例:
!   ! 1. 加载库
!   call asm("STORE <库路径地址>, R0")
!   call asm("SYSCALL 370")   ! DLOpen -> R0 = handle
!
!   ! 2. 查找函数
!   call asm("SYSCALL 371")   ! DLSym -> R0 = func_id
!
!   ! 3. 调用函数
!   call asm("SYSCALL 373")   ! NativeCall -> R0 = result
!
!   ! 4. 关闭库
!   call asm("SYSCALL 372")   ! DLClose

! ============================================================
! OpenCV 可用函数列表 (通过 FFI 调用):
!
! ocv_imread(path) -> int (图像句柄)
!   参数: 文件路径字符串地址
!   返回: 图像句柄 (>=0 成功)
!
! ocv_imwrite(path, handle) -> int
!   参数: 文件路径, 图像句柄
!   返回: 0 成功, <0 失败
!
! ocv_imshow(title, handle) -> int
!   参数: 窗口标题, 图像句柄
!   返回: 0 成功
!
! ocv_waitkey(delay_ms) -> int
!   参数: 等待毫秒数 (0=无限等待)
!   返回: 按键码
!
! ocv_cvtcolor(src, dst, code)
!   参数: 源句柄, 目标句柄, 颜色转换码
!   (void 函数, 无返回值)
!
! ocv_resize(src, dst, w, h)
!   参数: 源句柄, 目标句柄, 宽度, 高度
!
! ocv_rectangle(img, x1, y1, x2, y2, r, g, b, thickness)
!   参数: 图像句柄, 坐标, RGB 颜色, 线条粗细
!
! ocv_circle(img, cx, cy, radius, r, g, b, thickness)
!
! ocv_line(img, x1, y1, x2, y2, r, g, b, thickness)
!
! ocv_puttext(img, text, x, y, r, g, b)
!   参数: 图像句柄, 文本字符串地址, 坐标, RGB 颜色
!
! ocv_width(handle) -> int
!   返回: 图像宽度
!
! ocv_height(handle) -> int
!   返回: 图像高度
!
! ocv_channels(handle) -> int
!   返回: 颜色通道数
!
! ocv_blur(src, dst, ksize)
!   参数: 源句柄, 目标句柄, 核大小
!
! ocv_canny(src, dst, low, high)
!   参数: 源句柄, 目标句柄, 低阈值, 高阈值
!
! ocv_threshold(src, dst, thresh, maxval, type)
!   参数: 源/目标句柄, 阈值, 最大值, 类型
!
! ocv_facedetect(img, cascade_path) -> int
!   参数: 图像句柄, 级联分类器路径
!   返回: 检测到的人脸数
!
! ocv_release(handle)
!   参数: 图像句柄 (释放内存)
!
! 颜色转换码常量:
!   COLOR_BGR2GRAY       = 6
!   COLOR_BGR2RGB        = 4
!   COLOR_BGR2HSV        = 40
!   COLOR_GRAY2BGR       = 8
!   COLOR_RGB2BGR        = 3
! ============================================================

! ============================================================
! 完整使用示例 (概念性代码):
!
! program opencv_demo
!   implicit none
!
!   ! 1. 加载 OpenCV 库
!   ! macOS: libopencv.dylib
!   ! Linux: libopencv.so
!   ! Windows: libopencv.dll
!   call asm("SYSCALL 370")   ! DLOpen
!
!   ! 2. 读取图片
!   call asm("SYSCALL 374")   ! DLSym "ocv_imread"
!   call asm("SYSCALL 373")   ! NativeCall
!
!   ! 3. 显示图片
!   call asm("SYSCALL 374")   ! DLSym "ocv_imshow"
!   call asm("SYSCALL 373")   ! NativeCall
!
!   ! 4. 等待按键
!   call asm("SYSCALL 374")   ! DLSym "ocv_waitkey"
!   call asm("SYSCALL 373")   ! NativeCall
!
!   ! 5. 释放资源
!   call asm("SYSCALL 374")   ! DLSym "ocv_release"
!   call asm("SYSCALL 373")   ! NativeCall
!   call asm("SYSCALL 372")   ! DLClose
! end program opencv_demo
! ============================================================
