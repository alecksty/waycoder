// 栈漂移探针（JavaScript）：循环里反复调外部库函数（ipow）。判据：`DRIFT=126`（2^1+…+2^6）。
// 反序得 91（i^2 之和）、第 2 参丢得 6（ipow(2,0)=1）。
// ⚠ §4.3 记的「未知函数当变量 + 正序压栈」若仍成立，这条会直接跑飞（间接 call 到野地址）——
//    与 abi.js 的区别是它把调用放进循环、并要求循环完好无损。
native function ipow(base, exp) {}
native function print_str(s) {}
native function print_int(v) {}
native function newline() {}

function main() {
    let s = 0;
    for (let i = 1; i <= 6; i = i + 1) {
        s = s + ipow(2, i);
    }
    print_str("DRIFT=");
    print_int(s);
    newline();
}
