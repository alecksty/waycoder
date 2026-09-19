' sysinfo.bas —— 全能接口 CALLJSON(#573) 自检：调用 sysinfo 并把返回的 JSON 整份打出来。

NATIVE FUNCTION ui_call_json_s(fn AS STRING, args AS STRING) AS INTEGER
NATIVE SUB ui_call_json_print()
ui_call_json_s "sysinfo", ""
ui_call_json_print
