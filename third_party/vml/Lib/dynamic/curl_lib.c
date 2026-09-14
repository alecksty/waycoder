// Lib/dynamic/curl_lib.c — libcurl VML 仿真库 (C 实现)
// 提供 cl_* 函数的桩实现
// 编译: dotnet run --project VMLTool -- Lib/dynamic/curl_lib.c -o Lib/dynamic/curl_lib.vml
#include <stdio.h>

int cl_init() {
    printf("[curl] easy_init() -> handle=1\n");
    return 1;
}

int cl_setopt_url(int curl, const char* url) {
    printf("[curl] setopt(%d, URL=%s)\n", curl, url);
    return 0;
}

int cl_setopt_write(int curl, char* buf) {
    printf("[curl] setopt(%d, WRITEDATA)\n", curl);
    return 0;
}

int cl_perform(int curl) {
    printf("[curl] perform(%d) -> HTTP 200 OK\n", curl);
    return 0;
}

void cl_cleanup(int curl) {
    printf("[curl] cleanup(%d)\n", curl);
}

void cl_version() {
    printf("[curl] version: 8.7.1 (VML stub)\n");
}
