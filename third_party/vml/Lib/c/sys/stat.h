/* sys/stat.h —— 文件状态。本平台的文件沙箱只有"存在/大小"这点信息，
   这里的 struct stat 是**占位**：字段齐（老程序要能编），值不保证有意义。
   ⚠ 拿它来判断"是不是字符设备"（`S_ISCHR`）的老程序，在本平台会得到"No" ——
   那正是我们要的（手机上没有 /dev/tty）。 */
#ifndef _SYS_STAT_H
#define _SYS_STAT_H

#include <sys/types.h>

struct stat {
    unsigned int st_dev;
    unsigned int st_ino;
    unsigned int st_mode;
    unsigned int st_nlink;
    unsigned int st_uid;
    unsigned int st_gid;
    unsigned int st_rdev;
    int st_size;
    int st_atime;
    int st_mtime;
    int st_ctime;
};

/* 文件类型位（照 POSIX 的值，够老程序判分支用） */
#define S_IFMT   0170000
#define S_IFREG  0100000
#define S_IFDIR  0040000
#define S_IFCHR  0020000
#define S_IFBLK  0060000
#define S_IFIFO  0010000

#define S_ISREG(m)  (((m) & S_IFMT) == S_IFREG)
#define S_ISDIR(m)  (((m) & S_IFMT) == S_IFDIR)
#define S_ISCHR(m)  (((m) & S_IFMT) == S_IFCHR)
#define S_ISBLK(m)  (((m) & S_IFMT) == S_IFBLK)
#define S_ISFIFO(m) (((m) & S_IFMT) == S_IFIFO)

int stat(const char *path, struct stat *buf);
int fstat(int fd, struct stat *buf);

#endif /* _SYS_STAT_H */
