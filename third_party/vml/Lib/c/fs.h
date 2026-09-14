/* fs.h - File system operations (OS mode)
 * SYSCALL 340-344
 */

#ifndef _FS_H
#define _FS_H

#param lib("file")

int fs_mkdir(const char *path);
int fs_remove(const char *path);
int fs_rename(const char *old_path, const char *new_path);
int fs_readdir(const char *path, void *buffer);
int fs_stat(const char *path, void *info);

#endif /* _FS_H */
