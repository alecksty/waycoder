// `sprintf` 在**前面做过字符串操作之后**会多吐一个字符 —— 状态相关，不是单纯的格式解析错
//
// 发现经过：`06-string.c` 里先 `strcpy`/`strcat`/两次 `strcmp`，再
// `sprintf(buf, "%d-%d", 3, 7)` 得到 **`3-7d`**（末尾多一个 `d`）。
// 但**单独**写同一句 `sprintf` 却是对的（`[3-7]`）⇒ 判据必须带上前置上下文，
// 否则它会在孤立用例里"自己变绿"，把这条缺陷洗掉。
//
// 对 curses 库的影响：状态栏/调试输出大量用 `sprintf` 拼串，而库里字符串操作是连续的，
// 正好落在会犯的那种上下文里 —— 症状是静默多字。
#include <stdio.h>
#include <string.h>

int main()
{
    char buf[32];
    strcpy(buf, "ab");
    strcat(buf, "cd");
    printf("W1=%s\n", buf);
    printf("W2=%d\n", strcmp(buf, "abcd"));

    sprintf(buf, "%d-%d", 3, 7);
    printf("W3=[%s]\n", buf);
    printf("W4=%d\n", strlen(buf));
    return 0;
}
// 已修（v0.96.337）：**`sprintf` 根本不写结尾的 NUL**。
//   `vsnprintf` 返回的是长度（不含结尾符），实现里却直接 `return vsnprintf(...)` ——
//   目标缓冲区后面残留什么，串就"长"成什么。这里 `d` 正是上一句 `strcpy(buf,"abcd")`
//   留在 `buf[3]` 的残渣；**单独**写同一句是对的（缓冲区刚分配、后面本来就是 0），
//   所以这条用例**必须带着前置字符串操作**，否则它会在孤立上下文里自己变绿。
// EXPECT: W1=abcd|W2=0|W3=[3-7]|W4=3
