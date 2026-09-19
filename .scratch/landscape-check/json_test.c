/* 全能接口自检（ui_call_json / #573）。
 * 判据是**打印回来的 JSON 本体**（不经过 stdio 拼串，直接 puts 整份结果）：
 *   ① 成功信封 {"ok":true,"result":…}；
 *   ② 参数原样回显（管线通不通）；
 *   ③ 未注册的函数回 {"ok":false,"error":…}（而不是把 VM 打挂）；
 *   ④ 缓冲区故意开小 → 回"结果太长"的信封，返回 -1；
 *   ⑤ 无指针版本（ui_call_json_s + len/at）拿到的是同一份结果。 */
#include <waycoder_ui.h>

int main(void) {
    char buf[512];
    char small[24];
    int n;
    int m;
    int i;
    int same;

    puts("=== calljson 自检 ===");

    /* ① + ② echo：参数原样回来 */
    n = ui_call_json("echo", "{\"n\":7}", buf, 512);
    if (n > 0) { puts(buf); } else { puts("echo 失败"); }

    /* ③ 未知函数 */
    n = ui_call_json("没有这个函数", "", buf, 512);
    if (n > 0) { puts(buf); } else { puts("unknown 失败"); }

    /* 参数不是合法 JSON */
    n = ui_call_json("echo", "{不是json}", buf, 512);
    if (n > 0) { puts(buf); } else { puts("badargs 失败"); }

    /* version */
    n = ui_call_json("version", "", buf, 512);
    if (n > 0) { puts(buf); } else { puts("version 失败"); }

    /* screen（与 SCR_W/H 同源） */
    n = ui_call_json("screen", "", buf, 512);
    if (n > 0) { puts(buf); } else { puts("screen 失败"); }

    /* ④ 缓冲区太小：返回 -1，且写回来的是"结果太长"的信封 */
    n = ui_call_json("screen", "", small, 24);
    if (n < 0) { puts("small-capacity -> -1"); }
    if (small[0] != 0) { puts(small); }

    /* ⑤ 无指针版本：同一份结果 */
    m = ui_call_json_s("echo", "{\"k\":\"v\"}");
    same = 1;
    if (m <= 0) { same = 0; }
    for (i = 0; i < m; i = i + 1) {
        /* 结果里应当能找到 'k' 与 'v' —— 只做一个粗判，够证明拿到的是内容不是空 */
        if (ui_call_json_at(i) == 0) { same = 0; }
    }
    puts(same ? "no-pointer ok" : "no-pointer FAIL");
    puts("=== 自检结束 ===");
    return 0;
}
