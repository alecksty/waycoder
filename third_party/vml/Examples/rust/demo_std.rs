// demo_std.rs —— **标准输出**示范（Rust）
//
// 四层示范的第一层：只用这门语言自己的标准输出，**不读输入、不画图、不弹窗**，
// 输出**逐字节确定**（同样的二进制跑多少次都一样），能正常结束。
//
// ◆ 判据
//
// 输出是确定的，所以这一份可以直接和期望文本逐字节比 —— 同目录另三份
// （`demo_tty` / `demo_bgi` / `demo_ui`）的判据都只能弱一档（编译零错误 +
// 退出码 0 + 不挂死 / 出图不是全黑）。
//
// ◆ 写法（Rust 前端的两条实测约束）
//
//   · `println!` 的**第一个实参必须是字符串字面量**，格式化值走 `{}` 占位
//     （写 `println!(x)` 会打出 `[错误: println! 的第一个实参必须是字符串字面量]`）。
//   · `\x1b` 这类转义**在本前端不解析**（会原样打出 `x1b` 四个字符）⇒ 要发控制字符
//     得走 `putchar(码)`（见 `demo_tty.rs`）。
//
// 跑法：命令行页输入  vml run examples/rust/demo_std.rs

fn main() {
    println!("=== WayCoder demo_std (Rust) ===");

    // ① 字符串
    println!("[字符串] hello, world");

    // ② 整数
    let n = 42;
    println!("[整数] n = {}", n);

    // ③ 计算结果（字面量与变量都要能算）
    println!("[计算] 6 * 7 = {}", 6 * 7);
    let a = 7;
    let b = 5;
    println!("[计算] a + b = {}", a + b);
    println!("[计算] a * b - 3 = {}", a * b - 3);

    // ④ 循环里算斐波那契前 10 项（确定性）
    println!("[循环] 斐波那契前 10 项：");
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
    let mut sum = 0;
    let mut k = 1;
    while k <= 100 {
        sum = sum + k;
        k = k + 1;
    }
    println!("[累加] 1+2+...+100 = {}", sum);

    // ⑥ 阶乘
    let mut fact = 1;
    let mut m = 1;
    while m <= 5 {
        fact = fact * m;
        m = m + 1;
    }
    println!("[阶乘] 5! = {}", fact);

    println!("=== 结束 ===");
}
