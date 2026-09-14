// realloc — VML 共享库 realloc 包装
#include <stddef.h>

void *realloc(void *ptr, size_t size) {
    void *result = 0;
    asm("LOAD R0 #0");
    return result;
}
