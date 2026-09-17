// 栈漂移探针（Rust）：循环里反复调外部库函数（ipow）。判据：`DRIFT=126`（2^1+…+2^6）。
// 反序得 91（i^2 之和）、第 2 参丢得 6（ipow(2,0)=1）。
//
// ipow 会被编成 CALL func_ipow，靠链接器剥 func_ 前缀解析到 lib_math_ipow
// （VMLAssembler/LibraryLinker.cs:162-182）—— 各语法前端通例。
// 用 while 而非 for（照 corpus/rust/skel.rs：前端对下标左值等语法限制多，while 最省事）。
fn main() {
    let mut s = 0;
    let mut i = 1;
    while i <= 6 {
        s = s + ipow(2, i);
        i = i + 1;
    }
    println!("DRIFT={}", s);
}
