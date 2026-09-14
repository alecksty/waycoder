// VML 互斥锁扩展库 — C++ (OS 模式)
#pragma once
namespace vml { namespace mutex {
inline int create() { asm("SYSCALL 310"); return 0; }
inline int lock(int id) { asm("SYSCALL 311"); return 0; }
inline int unlock(int id) { asm("SYSCALL 312"); return 0; }
}}
