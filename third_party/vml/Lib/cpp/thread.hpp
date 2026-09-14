// VML 线程扩展库 — C++ (OS 模式)
#pragma once
namespace vml { namespace thread {
inline int create(void* entry, int stack_size) { asm("SYSCALL 300"); return 0; }
inline void exit() { asm("SYSCALL 301"); }
inline int join(int tid) { asm("SYSCALL 302"); return 0; }
inline int yield() { asm("SYSCALL 303"); return 0; }
}}
