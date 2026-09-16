// skel.js —— VML 骨架程序（JavaScript）。
//
// 骨架形状抄自 Examples/javascript/file_io.js（function main() {...}），但那个示例里的
// asm(...) 已经**不成立**：JavaScriptCompiler/CodeGenerator.Calls.cs:34 注明「asm() 已移除」，
// 它会被当成普通调用编成间接 CALL R0。
//
// ⚠ 本前端对**不认识的函数名**会先找 func_<名>，都没有就把名字当**变量**、
//   编成「MOVE R1, var_<名>；CALL R0」（同文件 :823-855）⇒ 运行期跳野地址。
//   唯一能拿到裸标签的写法是先声明 native（CodeGenerator.Statements.cs:74-81），所以下面声明了。
//   实参是**从左到右**压栈（:823），与库包装读 [R12+12] 的约定相反 ⇒ 8 参的 ui_rect 参数会整体反序；
//   print_str / print_int 只有一个参数，不受影响。
// 打印：print("...") 走 print_str（不换行）；console.log(整数变量) 走 print_int + 换行（:88-102）。
// 颜色写负数十进制（-65536 = 0xFFFF0000）。
native function ui_rect(x, y, w, h, color, fill, lw, radius) {}
native function ui_present() {}

function plus1(x) { return x + 1; }

function main() {
    let a = [1, 2, 3, 4];
    let s = 0;
    for (let i = 0; i < 4; i = i + 1) {
        a[i] = plus1(a[i]);
        s = s + a[i];
    }
    ui_rect(10, 10, 50, 50, -65536, 1, 0, 0);
    ui_present();
    print("SKEL-SUM=");
    console.log(s);
}
