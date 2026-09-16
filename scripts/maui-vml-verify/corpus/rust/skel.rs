// skel.rs —— Rust 前端「能不能写游戏」最小骨架（期望输出恰好一行 SKEL-SUM=14）
//
// 取材 Examples/rust/stm32/f103/stm32f103_i2c.rs（`fn main()` + `println!`）；
// 前端实现 VMLPrepares/RustCompiler/。
//
// 故意踩的已知缺陷（跑不过=产品缺陷，不是语料写错）：
//  ① `a[i] = inc(a[i]);` 是标准 Rust，但前端只认 `IDENTIFIER(.IDENTIFIER)* =` 形式的
//     左值（Parser.IsAssignment，Parser.cs:456-475），下标左值会落回表达式语句并卡在
//     Expect(";") 上 ⇒ 整个文件解析失败。Rust 没有 extern/asm 可绕（asm 已移除），
//     已知唯一退路是 peek/poke 手工算地址。
//  ② docs/前端游戏能力评估.md 记「rust 数组读回 0」。
//  ③ ui_rect 会被编成 CALL func_ui_rect，靠链接器剥 `func_` 前缀解析到
//     lib_vmlui_ui_rect（VMLAssembler/LibraryLinker.cs:162-182）—— 各语法前端通例。
//      vmlui.vml 由宿主按 vmltool.config.xml 的 <Language Name="rust"> 挂上，无需 use。

fn inc(x: i32) -> i32 { return x + 1; }

fn main() {
    let mut a = [1, 2, 3, 4];
    let mut s = 0;
    let mut i = 0;
    while i <= 3 {
        a[i] = inc(a[i]);
        s = s + a[i];
        i = i + 1;
    }
    println!("SKEL-SUM={}", s);
    ui_rect(10, 10, 50, 50, 0xFFFF0000, 1, 0, 0);
    ui_present();
}
