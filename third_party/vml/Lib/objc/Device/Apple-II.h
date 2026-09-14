// Apple-II 设备定义 - Objective-C 头文件
// 生成自: Apple Computer/Apple II/Apple-II
// 版本: 1.0
// 日期: 2026-04-17
// 作者: VML Team
// 描述: Apple II personal computer with MOS 6502 CPU, 48KB RAM, and color graphics
// CPU架构: MOS 6502
// 位宽: 8位
// 时钟频率: 1023000 Hz

#ifndef APPLE-II_DEVICE_H
#define APPLE-II_DEVICE_H

#import <Foundation/Foundation.h>

// 寄存器地址定义
#define A_ADDR 0  // Accumulator
#define X_ADDR 0  // Index Register X
#define Y_ADDR 0  // Index Register Y
#define SP_ADDR 0  // Stack Pointer
#define PC_ADDR 0  // Program Counter
#define P_ADDR 0  // Status Register

// 外设定义
// Apple II keyboard
#define KEYBOARD_BASE 
#define KEYBOARD_KBD_ADDR 0xC000
#define KEYBOARD_KBDSTRB_ADDR 0xC010
// Built-in speaker
#define SPEAKER_BASE 
#define SPEAKER_SPKR_ADDR 0xC030
// Cassette tape interface
#define CASSETTE_BASE 
#define CASSETTE_TAPEIN_ADDR 0xC060
#define CASSETTE_TAPEOUT_ADDR 0xC020
// Game controller port
#define GAMEPORT_BASE 
#define GAMEPORT_PADDLE0_ADDR 0xC064
#define GAMEPORT_PADDLE1_ADDR 0xC065
#define GAMEPORT_PADDLE2_ADDR 0xC066
#define GAMEPORT_PADDLE3_ADDR 0xC067
#define GAMEPORT_BUTTON0_ADDR 0xC061
#define GAMEPORT_BUTTON1_ADDR 0xC062
// Disk II controller
#define DISKCONTROLLER_BASE 
#define DISKCONTROLLER_DISKUNIT_ADDR 0xC0E0
#define DISKCONTROLLER_DISKCMD_ADDR 0xC0E8
#define DISKCONTROLLER_DISKSTAT_ADDR 0xC0E9
#define DISKCONTROLLER_DISKDATA_ADDR 0xC0EA

// 中断向量定义
#define INT_NMI 65526  // Non-maskable interrupt
#define INT_RESET 65528  // Reset vector
#define INT_IRQ 65530  // Interrupt request
#define INT_BRK 65532  // Break instruction

#endif /* APPLE-II_DEVICE_H */
