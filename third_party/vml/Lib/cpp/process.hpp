// VML 进程扩展库 — C++ (OS 模式)
#pragma once
namespace vml { namespace process {
inline int exec(const char* path) { asm("SYSCALL 320"); return 0; }
inline int get_pid() { asm("SYSCALL 322"); return 0; }
}}
