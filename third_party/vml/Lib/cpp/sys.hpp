// VML 系统扩展库 — C++
// 需显式 #include <sys.hpp>
#pragma once

namespace vml { namespace sys {

inline void speaker_beep(int freq, int duration) {
    asm("SYSCALL 57");
}

inline int set_rtc(int timestamp) {
    asm("SYSCALL 58");
    return 0;
}

}} // namespace vml::sys
