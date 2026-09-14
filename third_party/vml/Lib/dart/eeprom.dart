// VML EEPROM 扩展库 — Dart

int eepromRead(int offset, String buffer, int count) {
    asm("SYSCALL 106");
    return 0;
}

int eepromWrite(int offset, String data, int count) {
    asm("SYSCALL 107");
    return 0;
}
