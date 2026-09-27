// i64.c —— **64 位整数**：常量、运算、负数、十六进制
// ⚠ 2026-09-27 实测：这一份**当前是崩的**（`MOVEL @13, R0 — 地址=FFFFFFFC` 越界），
//   连 `long small = 42L; println_long(small);` 都崩 ⇒ 与数值大小无关，是 64 位传参/取用那条链。
//   它是"支持 64 位"这件事的**待修清单**，留着当判据（修好之前 run-langs.sh 会一直报它 ✘）。
__stdcall void println_str(char* s);
__stdcall void println_long(long v);
int main() {
    long big   = 3000000000L;
    long hex64 = 0x100000000L;
    println_str("I64-BIG="); println_long(big * 2L);
    println_str("I64-SUB="); println_long(0L - 4294967296L);
    println_str("I64-HEX="); println_long(hex64);
    return 0;
}
