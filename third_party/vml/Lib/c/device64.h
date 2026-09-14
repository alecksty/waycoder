/* device64.h - 64-bit Device I/O */

#ifndef _DEVICE64_H
#define _DEVICE64_H

#param lib("device64")

int ldev_read64(int handle, void* buf, long offset, long count);
int ldev_write64(int handle, const void* buf, long offset, long count);

#endif /* _DEVICE64_H */
