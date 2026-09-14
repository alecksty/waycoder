// VML EEPROM 持久存储扩展库 — Objective-C
// SYSCALL 106-107

int eeprom_read(int offset, void *buffer, int count) {
    asm("SYSCALL 106");
    return 0;
}

int eeprom_write(int offset, const void *data, int count) {
    asm("SYSCALL 107");
    return 0;
}
