// VML 条件变量扩展库 — C++ (OS 模式)
#pragma once
namespace vml { namespace cond {
inline int create() { asm("SYSCALL 313"); return 0; }
inline int wait(int cond_id, int mutex_id) { asm("SYSCALL 314"); return 0; }
inline int signal(int cond_id) { asm("SYSCALL 315"); return 0; }
inline int broadcast(int cond_id) { asm("SYSCALL 316"); return 0; }
}}
