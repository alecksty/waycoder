\ sysinfo.fth —— 全能接口 CALLJSON(#573) 自检：调用 sysinfo 并把返回的 JSON 整份打出来。

\ ⚠ 两条 Forth 特有的规矩，写错了都**不报错、只是输出不对**：
\   ① `S" ..."` 压的是 (地址, 长度) **两个单元**（标准 Forth 如此），而
\      `ui_call_json_s(char* fn, char* args)` 只要一个指针 ⇒ 长度必须 `DROP` 掉。
\      不 DROP 的话栈顶那个"长度"会顶到 fn 的位置上 —— 实测宿主收到的是空串，
\      报 `未实现该函数：`（名字后面**什么都没有**）。
\   ② 实参顺序：**最后压的 = C 的第 1 个实参** ⇒ args 先压、fn 后压。
\      （所以 C 里写 `ui_call_json_s("sysinfo","")`，这里写成 `S" " … S" sysinfo" …`）
S" " DROP S" sysinfo" DROP ui_call_json_s DROP
ui_call_json_print
