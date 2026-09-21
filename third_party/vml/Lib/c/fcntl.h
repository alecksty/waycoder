/* fcntl.h —— 打开文件的那套标志位。本平台的文件沙箱只有"读/写/建"三件事，
   标志位收下（老程序要能编、要能判分支），实际语义由 file.* 那层决定。 */
#ifndef _FCNTL_H
#define _FCNTL_H

#param lib("file")

#define O_RDONLY  0
#define O_WRONLY  1
#define O_RDWR    2
#define O_ACCMODE 3
#define O_CREAT   0100
#define O_EXCL    0200
#define O_TRUNC   01000
#define O_APPEND  02000
#define O_NONBLOCK 04000
#define O_SYNC    010000

#define F_GETFL 3
#define F_SETFL 4

int open(const char *path, int flags, int mode);
int creat(const char *path, int mode);

#endif /* _FCNTL_H */
