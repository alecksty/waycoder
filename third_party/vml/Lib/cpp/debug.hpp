// VML 调试扩展库 — C++
// 需显式 #include <debug.hpp>
#pragma once

namespace vml { namespace debug {

inline void debug_print(const char* str) {
    asm("SYSCALL 70");
}

inline void debug_print_int(int n) {
    asm("SYSCALL 71");
}

inline void assert(int condition, const char* message) {
    asm("SYSCALL 72");
}

}} // namespace vml::debug
