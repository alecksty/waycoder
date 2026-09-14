#param lib("file")
#param lib("os")
#param lib("convert")
#param lib("string")
#param lib("math")
#param lib("printf")
#param lib("io")
#param lib("ctype")
#param lib("bitops")
#param lib("util")
#param lib("float")
#param lib("memory")
// VML Kotlin Language-Specific Built-in Library
// 非共享函数 → builtins_kotlin.c (语言独享)

// ===== 数组分配 =====

__stdcall void* array_alloc(int count)
{
    int* arr;
    int size = count * 4 + 4;
    asm("SYSCALL #40");  // malloc(size) → R0
    arr[0] = count;       // STORE count → [arr+0]
    return arr;
}

// ===== 输入 =====

__stdcall char* read_line(void)
{
    char* buf;
    asm("SYSCALL #2");   // InputString → R0 = string pointer
    return buf;
}
