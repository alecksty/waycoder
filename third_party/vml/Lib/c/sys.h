/* sys.h - System control functions
 * SYSCALL 57-58
 */

#ifndef _SYS_H
#define _SYS_H

#param lib("os")

void speaker_beep(int freq, int duration);
int set_rtc(int timestamp);

#endif /* _SYS_H */
