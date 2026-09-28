\ sysinfo.fth —— 全能接口 CALLJSON(#573) 自检：调用 sysinfo 并把返回的 JSON 整份打出来。
\ sysinfo.fth —— self-check of the all-purpose interface CALLJSON（#573）: call sysinfo and print the whole returned JSON.

\ ⚠ 两条 Forth 特有的规矩，写错了都**不报错、只是输出不对**：
\ ⚠ Two Forth-specific rules; getting either wrong **raises no error, it just produces wrong output**:
\   ① `S" ..."` 压的是 (地址, 长度) **两个单元**（标准 Forth 如此），而
\   ① `S" ..."` pushes **two cells** （address, length） （standard Forth behavior）, while
\      `ui_call_json_s(char* fn, char* args)` 只要一个指针 ⇒ 长度必须 `DROP` 掉。
\      `ui_call_json_s（char* fn, char* args）` wants only one pointer ⇒ the length must be `DROP`ped.
\      不 DROP 的话栈顶那个"长度"会顶到 fn 的位置上 —— 实测宿主收到的是空串，
\      Without the DROP, that "length" at the top of the stack lands in fn's position —— measured: the host receives an empty string,
\      报 `未实现该函数：`（名字后面**什么都没有**）。
\      and reports "function not implemented:" （**nothing at all** after the name）.
\   ② 实参顺序：**最后压的 = C 的第 1 个实参** ⇒ args 先压、fn 后压。
\   ② Argument order: **the last one pushed = C's first argument** ⇒ push args first, fn last.
\      （所以 C 里写 `ui_call_json_s("sysinfo","")`，这里写成 `S" " … S" sysinfo" …`）
\      （so where C writes `ui_call_json_s（"sysinfo",""）`, here it is `S" " … S" sysinfo" …`）
S" " DROP S" sysinfo" DROP ui_call_json_s DROP
ui_call_json_print
