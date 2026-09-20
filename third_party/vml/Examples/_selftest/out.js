// out.js —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
//
// ⚠ **必须先把库函数声明成 native**，否则本前端对「不认识的函数名」会先找 func_<名>，
//   都没有就把名字当**变量**、编成「MOVE R1, var_<名>；CALL R0」⇒ 运行期跳野地址、
//   **一个字都不输出，而且编译链接全绿**（87224 条指令、跑 5.6 秒、零输出）。
//   这一步是骨架里逐字记着的（corpus/javascript/skel.js 的 ⚠ 段，
//   对应 JavaScriptCompiler/CodeGenerator.Calls.cs:823-855）。
//   ⚠ 顺带：本前端实参是从左到右压栈，与库包装读 [R12+12] 的约定相反 ——
//   单参函数（println_str/println_int）不受影响。
native function println_str(s) {}
native function println_int(n) {}

function main() {
    println_str("OUT-STR=abc");
    print("OUT-INT=");
    println_int(42);
    println_str("OUT-PUN=hello, world");
}
