// lang_probe.c —— 判据：C 侧的 ui_get_language() 真的调到宿主 HOST_LANG（#568）并拿到 0/1。
//
// 三行输出，**合起来才构成判据**（单看 LANG=0 会被"没人写 R0"那种假绿骗过去）：
//   W=480         —— 自校验：`ui_*` 所在的 vmlui 模块链进来了、且 syscall 通道是活的
//                    （480 = `--screen` 给的宽度，只有宿主报得出来）。
//   LANG=0        —— 期望：中文系统 0；非中文系统 1。
//   UNKNOWN=-100  —— 对照：同号段里**没实现**的 #599 返回 -100（并打 `Unknown syscall: 599`）。
//                    ⇒ "没实现"永远不是 0 ⇒ LANG=0 是宿主**认领了 #568 并回答了 0**，
//                      而不是"没人写 R0、残留巧合是 0"。
//
// 跑法（在仓库根）：
//   dotnet run --project scripts/vmlcli -- scripts/vml-ui-lang-probe/lang_probe.c
//   LANG=en_US.UTF-8 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
//       dotnet run --project scripts/vmlcli -- scripts/vml-ui-lang-probe/lang_probe.c
//   （第二行是为了把 CurrentUICulture 按掉 —— 它在本机是 zh-CN，而判定规则里
//     **任一个标签是中文就算中文**，不清掉它就永远看不到 1。）
__stdcall void print_str(char* s);
__stdcall void println_int(int v);
int ui_scr_w(void);
int ui_get_language(void);

int probe_unknown(void) { return asm("SYSCALL #599"); }

int main() {
    print_str("W=");
    println_int(ui_scr_w());
    print_str("LANG=");
    println_int(ui_get_language());
    print_str("UNKNOWN=");
    println_int(probe_unknown());
    return 0;
}
