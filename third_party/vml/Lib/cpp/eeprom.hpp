// VML EEPROM 扩展库 — C++
// 需显式 #include <eeprom.hpp>
#pragma once

namespace vml { namespace eeprom {

inline int eeprom_read(int offset, void* buffer, int count) {
    asm("SYSCALL 106");
    return 0;
}

inline int eeprom_write(int offset, const void* data, int count) {
    asm("SYSCALL 107");
    return 0;
}

}} // namespace vml::eeprom
