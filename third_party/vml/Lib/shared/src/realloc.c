// realloc — 重新分配内存
// VML 实现: 使用 vml_alloc (SYSCALL #40) + vml_free (SYSCALL #41) + memcpy
#include <stddef.h>

extern void *vml_alloc(int size);
extern void  vml_free(void *ptr);
extern void *memcpy(void *dest, const void *src, size_t n);

void *realloc(void *ptr, size_t size) {
    if (ptr == 0)
        return vml_alloc((int)size);
    if (size == 0) {
        vml_free(ptr);
        return 0;
    }
    void *new_ptr = vml_alloc((int)size);
    if (new_ptr != 0) {
        memcpy(new_ptr, ptr, size);
        vml_free(ptr);
    }
    return new_ptr;
}
