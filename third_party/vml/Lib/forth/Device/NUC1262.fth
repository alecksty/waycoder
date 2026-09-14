\ NUC1262SE设备定义 - Forth文件
\ 生成自: Nuvoton/NuMicro/NUC1262SE
\ 版本: 1.0
\ 日期: 2026-04-29
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-M4F MCU with 512KB Flash, 96KB SRAM, 72MHz, USB
\ CPU架构: ARM-Cortex-M4F
\ 位宽: 32位
\ 时钟频率: 72000000 Hz

\ =========================================
\ NUC1262SE设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" NUC1262SE" ;
: MANUFACTURER  S" Nuvoton" ;
: FAMILY        S" NuMicro" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M4F" ;
32 CONSTANT BITS
72000000 CONSTANT CLOCK-FREQ

\ =========================================
\ 寄存器访问字
\ =========================================

\ =========================================
\ 设备初始化
\ =========================================

: NUC1262SE-INIT ( -- )
  \ 初始化NUC1262SE设备
  ." 初始化NUC1262SE..." CR



  ." NUC1262SE初始化完成" CR
;

\ =========================================
\ 设备信息显示
\ =========================================

: .DEVICE-INFO ( -- )
  CR
  ." 设备: " DEVICE-NAME TYPE CR
  ." 厂商: " MANUFACTURER TYPE CR
  ." 系列: " FAMILY TYPE CR
  ." 版本: " VERSION TYPE CR
  ." 架构: " ARCHITECTURE TYPE CR
  ." 位宽: " BITS . CR
  ." 时钟: " CLOCK-FREQ . ." Hz" CR
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  NUC1262SE-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )
