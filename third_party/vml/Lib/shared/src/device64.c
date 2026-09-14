#param lib("device")

// VML Shared Device64 Library — 64-bit Device I/O (long offsets/counts)

__stdcall int ldev_read64(int handle, void* buf, long offset, long count) {
    return dev_read(handle, buf, (int)offset, (int)count);
}

__stdcall int ldev_write64(int handle, const void* buf, long offset, long count) {
    return dev_write(handle, buf, (int)offset, (int)count);
}
