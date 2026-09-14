// VML 文件系统扩展库 — C++ (OS 模式)
// 需显式 #include <fs.hpp>
#pragma once

namespace vml { namespace fs {

inline int mkdir(const char* path)              { asm("SYSCALL 340"); return 0; }
inline int remove(const char* path)             { asm("SYSCALL 341"); return 0; }
inline int rename(const char* old_p, const char* new_p) { asm("SYSCALL 342"); return 0; }
inline int readdir(const char* path, void* buf) { asm("SYSCALL 343"); return 0; }
inline int stat(const char* path, void* info)   { asm("SYSCALL 344"); return 0; }

}} // namespace vml::fs
