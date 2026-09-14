/*
 * VML System Call Constants — C 源码版本
 * 替代 Lib/shared/syscall.inc.vml (手写VML)
 * 编译为 syscall.vml，由 builtins.c 的 #param lib("syscall") 链接
 *
 * 使用: 在 C 代码中用 asm("SYSCALL #N") 或调用 vmlsys.c 的包装函数
 * 常量名供参考/文档用途
 */

// ======== 基础 I/O (1-10) ========
const int SYS_OUTPUTSTR = 1;   // R0=string addr → output string
const int SYS_INPUTSTR = 2;    // R0=buffer → input string
const int SYS_EXIT = 3;        // exit program
const int SYS_OUTPUTCHAR = 4;  // R0=char → output char (+VGA text buffer)
const int SYS_INPUTCHAR = 5;   // → R0=char
const int SYS_OUTPUTINT = 6;   // R0=int → output decimal
const int SYS_INPUTINT = 7;    // → R0=int
const int SYS_OUTPUTFLOAT = 8; // F0=float → output float
const int SYS_INPUTFLOAT = 9;  // → F0=float
const int SYS_OUTPUTHEX = 10;  // R0=int → output hex (0x....)

// ======== 内存管理 (40-41) ========
const int SYS_ALLOC = 40;      // R0=size → R0=addr
const int SYS_FREE = 41;       // R0=addr → free

// ======== 随机数 (50-51) ========
const int SYS_RANDOM = 50;     // → R0=random int
const int SYS_SEED = 51;       // R0=seed → set seed

// ======== 时间 (52-56) ========
const int SYS_SLEEP = 52;      // R0=ms → sleep
const int SYS_GETTICK = 53;    // → R0=ms since boot
const int SYS_GETDATETIME = 54; // → R0=Unix timestamp (seconds)
const int SYS_GETDATE = 55;    // → R0=date string "YYYY-MM-DD"
const int SYS_GETTIME = 56;    // → R0=time string "HH:mm:ss"

// ======== 系统配置 (60) ========
const int SYS_GETCONFIG = 60;  // R0=type → R0=addr/value

// GetConfig 类型码
// VGA/显示 (0-9)
const int CFG_VGA_FB = 0;      // VGA framebuffer base
const int CFG_VGA_TEXTFB = 1;  // text framebuffer (same as 0)
const int CFG_CURSOR_ROW = 2;  // cursor row register addr
const int CFG_CURSOR_COL = 3;  // cursor column register addr
const int CFG_FG_COLOR = 4;    // foreground color register addr
const int CFG_BG_COLOR = 5;    // background color register addr
const int CFG_VGA_MODE = 6;    // VGA mode register addr
const int CFG_MEM_SIZE = 7;    // total memory size (bytes)
const int CFG_DISP_WIDTH = 8;  // display width
const int CFG_DISP_HEIGHT = 9; // display height
// 系统信息 (10-19)
const int CFG_CONFIG_VER = 10; // config interface version
const int CFG_STACK_BASE = 11; // stack area start addr
const int CFG_STACK_SIZE = 12; // stack area size
const int CFG_HEAP_START = 13; // heap allocation start addr
// 外设地址 (20-24)
const int CFG_KBD_DATA = 20;   // keyboard data register
const int CFG_KBD_STATUS = 21; // keyboard status register
const int CFG_MOUSE_X = 22;    // mouse X coord
const int CFG_MOUSE_Y = 23;    // mouse Y coord
const int CFG_MOUSE_BTN = 24;  // mouse button
// BASIC/QB compatible (30-39)
const int CFG_TURTLE_X = 30;   // turtle X
const int CFG_TURTLE_Y = 31;   // turtle Y
const int CFG_TURTLE_COLOR = 32; // turtle color
const int CFG_TURTLE_SCALE = 33; // turtle scale
const int CFG_TURTLE_ANGLE = 34; // turtle angle
const int CFG_QB_PAL = 35;     // QB 16-color palette
const int CFG_QB_PAL256 = 36;  // QB 256-color palette
const int CFG_BASIC_ERRTB = 37; // BASIC error table
const int CFG_BASIC_ERRFL = 38; // BASIC error flag
const int CFG_BASIC_DPTR = 39;  // BASIC DATA pointer

// ======== 调试 (70-72) ========
const int SYS_DEBUGPRINT = 70;  // R0=string → output to stderr
const int SYS_DEBUGINT = 71;    // R0=int → output to stderr
const int SYS_ASSERT = 72;      // R0=cond, R1=message → assert

// ======== 设备操作 (100-104) ========
const int SYS_DEVOPEN = 100;    // R0=device name → R0=handle
const int SYS_DEVCLOSE = 101;   // R0=handle → close device
const int SYS_DEVREAD = 102;    // R0=handle, R1=buf, R2=size → R0=bytes read
const int SYS_DEVWRITE = 103;   // R0=handle, R1=data, R2=size → R0=bytes written
const int SYS_DEVCONTROL = 104; // R0=handle, R1=cmd, R2=data → R0=result

// ======== 文件操作 (110-114) ========
const int SYS_FILEOPEN = 110;   // R0=filename, R1=mode → R0=handle
const int SYS_FILECLOSE = 111;  // R0=handle → close
const int SYS_FILEREAD = 112;   // R0=handle, R1=buf, R2=size → R0=bytes read
const int SYS_FILEWRITE = 113;  // R0=handle, R1=data, R2=size → R0=bytes written
const int SYS_FILECONTROL = 114; // R0=handle, R1=cmd, R2=data → R0=result
// 文件控制命令:
const int FCTL_GETSIZE = 0;     // get file size
const int FCTL_SEEK = 1;        // seek R2=offset
const int FCTL_GETPOS = 2;      // get position
const int FCTL_TRUNCATE = 3;    // truncate R2=new size

// ======== 打印机 (200-208) ========
const int SYS_PRNOPEN = 200;    // open printer
const int SYS_PRNCLOSE = 201;   // close printer
const int SYS_PRNWRITESTR = 202; // R1=string → print
const int SYS_PRNWRITECHAR = 203; // R1=char → print
const int SYS_PRNSTATUS = 204;  // → R0=status
const int SYS_PRNNL = 205;      // newline
const int SYS_PRNFF = 206;      // form feed
const int SYS_PRNCLRBUF = 207;  // clear buffer
const int SYS_PRNGETCONT = 208; // R1=buf → R0=byte count
