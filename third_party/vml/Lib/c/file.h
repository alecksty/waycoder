/* file.h - File operations (SYSCALL 110-114) */
#ifndef _FILE_H
#define _FILE_H
#param lib("file")
int fopen(const char *name, const char *mode);
int fclose(int handle);
int fread(int handle, void *buf, int count);
int fwrite(int handle, const void *buf, int count);
int fseek(int handle, int offset);
int ftell(int handle);
#endif
