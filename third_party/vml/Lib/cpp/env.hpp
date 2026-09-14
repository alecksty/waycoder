// VML 环境变量扩展库 — C++ (OS 模式)
// 需显式 #include <env.hpp>
#pragma once

namespace vml { namespace env {

inline const char* get_env(const char* name)   { asm("SYSCALL 360"); return ""; }
inline int set_env(const char* name, const char* value) { asm("SYSCALL 361"); return 0; }
inline int get_args(void* buffer)             { asm("SYSCALL 362"); return 0; }

}} // namespace vml::env
