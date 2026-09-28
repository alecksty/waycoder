// demo_std.rs —— **标准输出**示范（Rust）
// demo_std.rs -- the **standard output** demo (Rust)
//
// 四层示范的第一层：只用这门语言自己的标准输出，**不读输入、不画图、不弹窗**，
// The first of the four demo layers: use only this language's own standard output, **no reading input, no drawing, no dialogs**,
// 输出**逐字节确定**（同样的二进制跑多少次都一样），能正常结束。
// the output is **byte-for-byte deterministic** (the same binary gives the same bytes on every run), and it exits normally.
//
// ◆ 判据
// ◆ The criterion
//
// 输出是确定的，所以这一份可以直接和期望文本逐字节比 —— 同目录另三份
// The output is deterministic, so this one can be compared byte-for-byte against the expected text -- the other three in this directory
// （`demo_tty` / `demo_bgi` / `demo_ui`）的判据都只能弱一档（编译零错误 +
// (`demo_tty` / `demo_bgi` / `demo_ui`) can only use a criterion one notch weaker (zero compile errors +
// 退出码 0 + 不挂死 / 出图不是全黑）。
// exit code 0 + no hang / the produced image is not all black).
//
// ◆ 写法（Rust 前端的两条实测约束）
// ◆ Style (two measured constraints of the Rust frontend)
//
//   · `println!` 的**第一个实参必须是字符串字面量**，格式化值走 `{}` 占位
//   · The **first argument of `println!` must be a string literal**; formatted values go through `{}` placeholders
//     （写 `println!(x)` 会打出 `[错误: println! 的第一个实参必须是字符串字面量]`）。
//     (writing `println!(x)` prints `[error: the first argument of println! must be a string literal]`).
//   · `\x1b` 这类转义**在本前端不解析**（会原样打出 `x1b` 四个字符）⇒ 要发控制字符
//   · Escapes like `\x1b` are **not parsed by this frontend** (it prints the four characters `x1b` verbatim) => to emit a control character
//     得走 `putchar(码)`（见 `demo_tty.rs`）。
//     go through `putchar(code)` (see `demo_tty.rs`).
//
// 跑法：命令行页输入  vml run examples/rust/demo_std.rs
// How to run: type this into the command-line page:  vml run examples/rust/demo_std.rs

fn main() {
    let lang = ui_get_language();

    println!("=== WayCoder demo_std (Rust) ===");

    // ① 字符串
    // ① String
    if lang == 0 { println!("[字符串] hello, world"); } else { println!("[string] hello, world"); }

    // ② 整数
    // ② Integer
    let n = 42;
    if lang == 0 { println!("[整数] n = {}", n); } else { println!("[int] n = {}", n); }

    // ③ 计算结果（字面量与变量都要能算）
    // ③ Computed results (both literals and variables must work)
    if lang == 0 { println!("[计算] 6 * 7 = {}", 6 * 7); } else { println!("[calc] 6 * 7 = {}", 6 * 7); }
    let a = 7;
    let b = 5;
    if lang == 0 { println!("[计算] a + b = {}", a + b); } else { println!("[calc] a + b = {}", a + b); }
    if lang == 0 { println!("[计算] a * b - 3 = {}", a * b - 3); } else { println!("[calc] a * b - 3 = {}", a * b - 3); }

    // ④ 循环里算斐波那契前 10 项（确定性）
    // ④ Compute the first 10 Fibonacci numbers in a loop (deterministic)
    if lang == 0 { println!("[循环] 斐波那契前 10 项："); } else { println!("[loop] first 10 Fibonacci numbers:"); }
    let mut x = 0;
    let mut y = 1;
    let mut i = 0;
    while i < 10 {
        let z = x + y;
        x = y;
        y = z;
        println!("  fib = {}", x);
        i = i + 1;
    }

    // ⑤ 累加：1+2+…+100
    // ⑤ Accumulation: 1+2+...+100
    let mut sum = 0;
    let mut k = 1;
    while k <= 100 {
        sum = sum + k;
        k = k + 1;
    }
    if lang == 0 { println!("[累加] 1+2+...+100 = {}", sum); } else { println!("[sum] 1+2+...+100 = {}", sum); }

    // ⑥ 阶乘
    // ⑥ Factorial
    let mut fact = 1;
    let mut m = 1;
    while m <= 5 {
        fact = fact * m;
        m = m + 1;
    }
    if lang == 0 { println!("[阶乘] 5! = {}", fact); } else { println!("[factorial] 5! = {}", fact); }

    if lang == 0 { println!("=== 结束 ==="); } else { println!("=== done ==="); }
}
