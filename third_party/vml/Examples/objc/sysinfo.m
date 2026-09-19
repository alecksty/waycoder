// sysinfo.m —— 全能接口 CALLJSON(#573) 自检：调用 sysinfo 并把返回的 JSON 整份打出来。
//
// ⚠ **不 `#include <waycoder_ui.h>`** —— ObjC 前端解析不了那个头文件
//   （`expected ) (got IdType 'id'`，报在头文件里，与源码无关）。同目录的 `snake.m`
//   也是靠"直接调用、由前端按库符号解析"这条路，不引头文件。

int main() {
    ui_call_json_s("sysinfo", "");
    ui_call_json_print();
    return 0;
}
