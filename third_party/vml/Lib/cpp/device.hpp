// VML 设备 I/O + 键盘/鼠标扩展库 — C++
// 需显式 #include "device.hpp"

#ifndef VML_DEVICE_HPP
#define VML_DEVICE_HPP

namespace vml::dev {

// 统一设备接口 (SYSCALL 100-104)
inline int open(const char* name)                       { asm("SYSCALL 100"); return 0; }
inline int close(int handle)                            { asm("SYSCALL 101"); return 0; }
inline int read(int h, void* buf, int off, int n)       { asm("SYSCALL 102"); return 0; }
inline int write(int h, const void* buf, int off, int n) { asm("SYSCALL 103"); return 0; }
inline int control(int h, int cmd, const void* data, int len) { asm("SYSCALL 104"); return 0; }

// 键盘 (SYSCALL 83-84)
inline int  kb_hit()                  { asm("SYSCALL 83"); return 0; }
inline char kb_getch()                { asm("SYSCALL 84"); return 0; }

// 鼠标 (SYSCALL 85-88)
inline int mouse_x()                  { asm("SYSCALL 85"); return 0; }
inline int mouse_y()                  { asm("SYSCALL 86"); return 0; }
inline int mouse_left()               { asm("SYSCALL 87"); return 0; }
inline int mouse_right()              { asm("SYSCALL 88"); return 0; }

} // namespace vml::dev

#endif
