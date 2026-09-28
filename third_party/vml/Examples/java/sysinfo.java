// sysinfo.java —— 全能接口 CALLJSON(#573) 自检：调用 sysinfo 并把返回的 JSON 整份打出来。
// sysinfo.java — self-check for the all-purpose interface CALLJSON(#573): call sysinfo and print the whole returned JSON.

public class P {
    static native int ui_call_json_s(String fn, String args);
    static native void ui_call_json_print();
    public static void main(String[] a) {
        ui_call_json_s("sysinfo", "");
        ui_call_json_print();
    }
}
