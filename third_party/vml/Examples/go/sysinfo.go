// sysinfo.go —— 全能接口 CALLJSON(#573) 自检：调用 sysinfo 并把返回的 JSON 整份打出来。
// sysinfo.go -- self-check for the all-purpose interface CALLJSON(#573): call sysinfo and print the whole returned JSON.

package main
func main() {
	ui_call_json_s("sysinfo", "")
	ui_call_json_print()
}
