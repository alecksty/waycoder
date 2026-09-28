// sysinfo.m —— 全能接口 CALLJSON(#573) 自检：调用 sysinfo 并把返回的 JSON 整份打出来。
// sysinfo.m — self-check for the all-purpose interface CALLJSON(#573): call sysinfo and print the whole returned JSON.
//
// ⚠ **不 `#include <waycoder_ui.h>`** —— ObjC 前端解析不了那个头文件
// ⚠ **No `#include <waycoder_ui.h>`** — the ObjC frontend cannot parse that header
//   （`expected ) (got IdType 'id'`，报在头文件里，与源码无关）。同目录的 `snake.m`
//   (`expected ) (got IdType 'id'`, reported inside the header, unrelated to the source). The `snake.m`
//   也是靠"直接调用、由前端按库符号解析"这条路，不引头文件。
//   in the same directory also relies on "call directly and let the frontend resolve library symbols", with no header included.

int main() {
    ui_call_json_s("sysinfo", "");
    ui_call_json_print();
    return 0;
}
