// sysinfo.cpp —— 全能接口 CALLJSON(#573) 自检：调用 sysinfo 并把返回的 JSON 整份打出来。
// sysinfo.cpp — self-check for the universal CALLJSON(#573) interface: call sysinfo and print the whole returned JSON.

#include <waycoder_ui.h>

int main() {
    ui_call_json_s("sysinfo", "");
    ui_call_json_print();
    return 0;
}
