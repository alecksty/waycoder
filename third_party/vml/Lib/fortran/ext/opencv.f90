! opencv.f90 — VML OpenCV Simple Wrapper (Fortran)
! 简化版 OpenCV FFI 文档，涵盖常用图像处理操作
!
! 使用前提:
!   1. libopencv 动态库已编译，放在可访问路径
!   2. OS 模式下运行 (--mode os)
!   3. 通过 FFI SYSCALL (370-375) 调用
!
! ============================================================
! 基本图像操作流程:
!
!  1. DLOpen -> 加载 libopencv 库
!  2. DLSym -> 查找 ocv_imread
!  3. NativeCall -> 读取图片，获得句柄
!  4. DLSym -> 查找处理函数 (resize/grayscale/blur/...)
!  5. NativeCall -> 执行处理
!  6. DLSym -> 查找 ocv_imwrite / ocv_imshow
!  7. NativeCall -> 保存或显示
!  8. DLSym -> 查找 ocv_release
!  9. NativeCall -> 释放图像内存
! 10. DLClose -> 关闭库
! ============================================================

! ============================================================
! 常用图像处理操作:
!
! 读取+显示:
!   imread("photo.jpg") -> 获得句柄
!   imshow("Window", handle) -> 显示
!   waitkey(0) -> 等待按键
!
! 颜色转换:
!   cvtcolor(src, dst, COLOR_BGR2GRAY) -> BGR 转灰度
!   cvtcolor(src, dst, COLOR_BGR2HSV)  -> BGR 转 HSV
!
! 图像缩放:
!   resize(src, dst, 320, 240) -> 缩放为 320x240
!
! 绘图:
!   rectangle(img, 10, 10, 100, 50, 255, 0, 0, 2) -> 蓝色矩形
!   circle(img, 50, 50, 30, 0, 255, 0, -1)         -> 绿色实心圆
!   line(img, 0, 0, 100, 100, 0, 0, 255, 1)        -> 红色线条
!   puttext(img, "Hello", 10, 30, 255, 255, 255)    -> 白色文字
!
! 图像处理:
!   blur(src, dst, 5)      -> 均值模糊 (5x5 核)
!   canny(src, dst, 50, 150) -> Canny 边缘检测
!   threshold(src, dst, 127, 255, 0) -> 二值化
!
! 人脸检测:
!   facedetect(img, "haarcascade_frontalface_default.xml") -> 检测结果
! ============================================================

! ============================================================
! 参数传递注意事项:
!
! 所有参数通过 NativeCall (SYSCALL 373) 的参数缓冲区传递:
!   R1 = 参数缓冲区地址 (int32 数组)
!   R2 = 参数个数
!
! 字符串参数:
!   将字符串写入 VML 内存，传递其地址
!
! 整数参数:
!   直接写入参数缓冲区 (每个参数占 4 字节, int32)
!
! 颜色参数 (r, g, b):
!   分别作为独立的 int32 参数传递
!   范围: 0-255
! ============================================================
