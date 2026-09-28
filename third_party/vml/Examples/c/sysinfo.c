/* sysinfo.c —— 全能接口 CALLJSON(#573) 自检：调用 sysinfo 并把返回的 JSON 整份打出来。
 * sysinfo.c — self-check for the all-in-one CALLJSON(#573) interface:
 * call the `sysinfo` function and print the whole JSON it returns.
 */
#include <waycoder_ui.h>

int main(void) {
    ui_call_json_s("sysinfo", "");
    ui_call_json_print();
    return 0;
}
