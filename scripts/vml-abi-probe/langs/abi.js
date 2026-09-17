// abi.js —— 实参顺序探针（JavaScript）。判据：`ABI=8`（反序得 9）。
native function ipow(base, exp) {}
native function print_str(s) {}
native function print_int(v) {}
native function newline() {}

function main() {
    let r = ipow(2, 3);
    print_str("ABI=");
    print_int(r);
    newline();
}
