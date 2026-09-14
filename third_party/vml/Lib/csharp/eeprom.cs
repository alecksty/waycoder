// VML EEPROM 扩展库 — C#
// 需显式 using eeprom

static class Eeprom {
    public static int EepromRead(int offset, string buffer, int count) {
        asm("SYSCALL 106");
        return 0;
    }

    public static int EepromWrite(int offset, string data, int count) {
        asm("SYSCALL 107");
        return 0;
    }
}
