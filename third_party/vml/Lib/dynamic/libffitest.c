/* libffitest.c — FFI 参数传递验证 DLL
 * 每个函数将收到的参数写入 ffitest.log，用于验证 VML FFI 参数传递是否正确
 * 编译 (MSVC):
 *   cl /nologo /O2 /MD /LD /Fe:Lib/dynamic/libffitest.dll Lib/dynamic/libffitest.c
 */

#ifdef _WIN32
#include <windows.h>
#else
#include <dlfcn.h>
#endif
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

#ifdef _WIN32
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif
#define LOGFILE "ffitest.log"

static FILE* g_log = NULL;

static void log_open(void)
{
    if (g_log) return;
    g_log = fopen(LOGFILE, "w");
    if (g_log) {
        fprintf(g_log, "=== FFI Test Log ===\n");
        fflush(g_log);
    }
}

/* ====== int 参数测试 ====== */

DLLEXPORT void ffi_test_int(int a)
{
    log_open();
    if (g_log) { fprintf(g_log, "[INT]  a=%d (0x%08X)\n", a, a); fflush(g_log); }
}

DLLEXPORT void ffi_test_int2(int a, int b)
{
    log_open();
    if (g_log) { fprintf(g_log, "[INT2] a=%d (0x%08X)  b=%d (0x%08X)\n", a, a, b, b); fflush(g_log); }
}

DLLEXPORT void ffi_test_int3(int a, int b, int c)
{
    log_open();
    if (g_log) { fprintf(g_log, "[INT3] a=%d  b=%d  c=%d\n", a, b, c); fflush(g_log); }
}

/* ====== float 参数测试 (VML 传 float 按 int bits) ====== */

DLLEXPORT void ffi_test_float(int f_bits)
{
    float f;
    memcpy(&f, &f_bits, 4);
    log_open();
    if (g_log) { fprintf(g_log, "[FLT]  bits=0x%08X  float=%.6f\n", f_bits, f); fflush(g_log); }
}

DLLEXPORT void ffi_test_int_float(int a, int f_bits)
{
    float f;
    memcpy(&f, &f_bits, 4);
    log_open();
    if (g_log) { fprintf(g_log, "[I+F]  a=%d  bits=0x%08X  float=%.6f\n", a, f_bits, f); fflush(g_log); }
}

/* ====== string 参数测试 ====== */

DLLEXPORT void ffi_test_string(const char* s)
{
    log_open();
    if (g_log) {
        if (s) {
            fprintf(g_log, "[STR]  len=%d  s=\"%s\"\n", (int)strlen(s), s);
        } else {
            fprintf(g_log, "[STR]  NULL pointer!\n");
        }
        fflush(g_log);
    }
}

DLLEXPORT void ffi_test_int_string(int a, const char* s)
{
    log_open();
    if (g_log) {
        if (s) {
            fprintf(g_log, "[I+S]  a=%d  len=%d  s=\"%s\"\n", a, (int)strlen(s), s);
        } else {
            fprintf(g_log, "[I+S]  a=%d  NULL pointer!\n", a);
        }
        fflush(g_log);
    }
}

DLLEXPORT void ffi_test_string2(const char* s1, const char* s2)
{
    log_open();
    if (g_log) {
        fprintf(g_log, "[S+S]  s1=\"%s\"  s2=\"%s\"\n", s1 ? s1 : "NULL", s2 ? s2 : "NULL");
        fflush(g_log);
    }
}

DLLEXPORT int ffi_test_strlen(const char* s)
{
    log_open();
    int len = s ? (int)strlen(s) : -1;
    if (g_log) { fprintf(g_log, "[STRLEN] len=%d  s=\"%s\"\n", len, s ? s : "NULL"); fflush(g_log); }
    return len;
}
