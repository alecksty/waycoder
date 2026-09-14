// VML EEPROM 扩展库 — Java
// 需显式 import eeprom

public class Eeprom {
    public static int eepromRead(int offset, String buffer, int count) {
        asm("SYSCALL 106");
        return 0;
    }

    public static int eepromWrite(int offset, String data, int count) {
        asm("SYSCALL 107");
        return 0;
    }
}
