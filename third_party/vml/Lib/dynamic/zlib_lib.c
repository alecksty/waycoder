// Lib/dynamic/zlib_lib.c — zlib VML 仿真库 (C 实现)
// 提供 zl_* 函数的桩实现
// 编译: dotnet run --project VMLTool -- Lib/dynamic/zlib_lib.c -o Lib/dynamic/zlib_lib.vml
#include <stdio.h>

int zl_compress(char* dest, int* destLen, const char* source, int sourceLen) {
    printf("[zlib] compress(%d bytes) -> Z_OK\n", sourceLen);
    *destLen = sourceLen + 16;
    return 0;
}

int zl_uncompress(char* dest, int* destLen, const char* source, int sourceLen) {
    printf("[zlib] uncompress(%d bytes) -> Z_OK\n", sourceLen);
    *destLen = sourceLen * 2;
    return 0;
}

int zl_compressbound(int sourceLen) {
    printf("[zlib] compressBound(%d)\n", sourceLen);
    return sourceLen + sourceLen / 10 + 12;
}

int zl_crc32(int crc, const char* buf, int len) {
    printf("[zlib] crc32(%d bytes) -> 0x12345678\n", len);
    return 0x12345678;
}

void zl_version() {
    printf("[zlib] version: 1.3.1 (VML stub)\n");
}
