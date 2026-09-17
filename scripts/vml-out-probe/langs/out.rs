// out.rs —— VML 跨语言「输出」判据（期望恰好三行，见 run-langs.sh）
// 写法照 corpus/rs/skel.* —— 共享库同时提供 println_str / println_int。
fn main() {
    println!("OUT-STR=abc");
    println!("OUT-INT={}", 42);
    println!("OUT-PUN=hello, world");
}
