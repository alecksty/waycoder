! testlib.f90 — VML Test Library Bindings (Fortran)
! 测试用静态库函数文档
!
! VML 测试库 (testlib) 提供基本的测试函数，用于验证
! 编译器 FFI / 外部调用功能。
!
! ============================================================
! 可用函数 (通过 FFI 调用或在 VML 汇编中 CALL):
!
!   int add(int a, int b)
!     两数相加，返回 a + b
!     参数: R0=a, R1=b (或通过参数缓冲区)
!     返回: R0 = a + b
!
!   int mul(int x, int y)
!     两数相乘，返回 x * y
!     参数: R0=x, R1=y
!     返回: R0 = x * y
!
!   void say_hello(void)
!     输出 "Hello from testlib!" 到控制台
!     无参数, 无返回值
! ============================================================

! ============================================================
! 使用方式:
!
! 方式一: VML 汇编直接 CALL (外链):
!   .linked  "../shared/testlib.vml"
!   ...
!   CALL add         ! R0=a, R1=b, 返回 R0
!   CALL mul         ! R0=x, R1=y, 返回 R0
!   CALL say_hello   ! 输出问候
!
! 方式二: Fortran asm() 内嵌调用:
!   call asm("CALL add")
!
! 方式三: FFI 动态加载 (如编译为动态库):
!   1. SYSCALL 370 (DLOpen) 加载 libtestlib
!   2. SYSCALL 371 (DLSym) 查找 "add"/"mul"/"say_hello"
!   3. SYSCALL 373 (NativeCall) 调用
!   4. SYSCALL 372 (DLClose) 关闭
! ============================================================

! ============================================================
! Fortran 调用示例 (概念性代码):
!
! program test_testlib
!   implicit none
!   ! 调用 add(3, 5)
!   ! 设置 R0=3, R1=5 然后 CALL
!   call asm("STORE 3, R0")
!   call asm("STORE 5, R1")
!   call asm("CALL add")
!   ! 结果在 R0
! end program test_testlib
! ============================================================
