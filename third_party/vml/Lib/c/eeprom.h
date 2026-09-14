/* eeprom.h - EEPROM persistent storage
 * SYSCALL 106-107
 */

#ifndef _EEPROM_H
#define _EEPROM_H

// eeprom_read/write 通过 SYSCALL 实现，无需额外库

int eeprom_read(int offset, void *buffer, int count);
int eeprom_write(int offset, const void *data, int count);

#endif /* _EEPROM_H */
