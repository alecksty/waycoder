// 堆上的字符串过 `%s` 打成 `(null)`
//
// 内容本身是好的 —— `strlen` 数出 15、`p[0]` 取到 'A'（见 `04-mem.c`），
// **只有 `%s` 这一条路读不到**。栈上的缓冲区（`memcpy` 那个）过 `%s` 正常，
// 所以判据收在"堆指针"上，不是"字符串"上。
//
// 对 curses 库是要害：屏缓冲必然是 `malloc` 出来的，而调试/导出路径会拿它当字符串打印。
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

int main()
{
    char * p = malloc(16);
    strcpy(p, "heap-str");
    printf("H1=%s\n", p);
    printf("H2=%d\n", strlen(p));
    printf("H3=%c\n", p[0]);
    free(p);
    return 0;
}
// KNOWN-RED: 堆指针过 %s 打成 (null)（内容本身是对的，只有这条路读不到）
// EXPECT: H1=heap-str|H2=8|H3=h
