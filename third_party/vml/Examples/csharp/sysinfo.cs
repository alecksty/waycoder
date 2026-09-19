// sysinfo.cs —— 全能接口 CALLJSON(#573) 自检：调用 sysinfo 并把返回的 JSON 整份打出来。

class P {
  static void Main() {
    ui_call_json_s("sysinfo", "");
    ui_call_json_print();
  }
}
