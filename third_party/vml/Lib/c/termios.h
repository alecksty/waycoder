/* termios.h —— 终端模式那套（raw/cbreak/echo…）。
   ⚠ 本平台没有真终端：输入是命令行页**按行**给的，输出走字符网格。
   所以这些函数一律**返回成功但不改任何行为** —— 老程序调它们只是为了
   "关掉回显/关掉行缓冲"，而这两件事在我们这儿本来就不存在。 */
#ifndef _TERMIOS_H
#define _TERMIOS_H

#param lib("util")

typedef unsigned int tcflag_t;
typedef unsigned char cc_t;
typedef unsigned int speed_t;

#define NCCS 32

struct termios {
    tcflag_t c_iflag;
    tcflag_t c_oflag;
    tcflag_t c_cflag;
    tcflag_t c_lflag;
    cc_t     c_line;
    cc_t     c_cc[NCCS];
    speed_t  c_ispeed;
    speed_t  c_ospeed;
};

/* lflag */
#define ECHO    0000010
#define ECHONL  0000100
#define ICANON  0000002
#define ISIG    0000001
#define IEXTEN  0100000
/* iflag */
#define ICRNL   0000400
#define IXON    0002000
/* 控制字符下标 */
#define VMIN 6
#define VTIME 5
#define VEOF 4
#define VERASE 3

#define TCSANOW 0
#define TCSADRAIN 1
#define TCSAFLUSH 2

int tcgetattr(int fd, struct termios *t);
int tcsetattr(int fd, int actions, struct termios *t);
void cfmakeraw(struct termios *t);
int tcflush(int fd, int queue);
speed_t cfgetospeed(struct termios *t);
int cfsetospeed(struct termios *t, speed_t speed);

#endif /* _TERMIOS_H */
