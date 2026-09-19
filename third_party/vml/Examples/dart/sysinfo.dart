// sysinfo.dart —— 全能接口 CALLJSON(#573) 自检：调用 sysinfo 并把返回的 JSON 整份打出来。

external int ui_call_json_s(String fn, String args);
external void ui_call_json_print();
void main() {
  ui_call_json_s("sysinfo", "");
  ui_call_json_print();
}
