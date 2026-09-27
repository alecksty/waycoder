// f2.cpp —— float/double/int64 运算（判据见 run-langs.sh 头部）
__stdcall void println_str(char* s);
__stdcall void println_int(int v);
int main() {
    float  a = 3.14f, b = 2.0f;
    double x = 3.14,  y = 2.0;
    long   big = 3000000000L, add = 1000000000L, mul = 123456789L, pos = 4294967296L;
    println_str("F-MUL="); println_int((int)(a * b * 100.0f));
    println_str("D-MUL="); println_int((int)(x * y * 100.0));
    println_str("F-NEG="); println_int((int)(-0.5f * 100.0f));
    println_str("D-NEG="); println_int((int)(-0.5 * 100.0));
    println_str("L-ADD="); println_int((int)((big + add) / 1000000000L));
    println_str("L-MUL="); println_int((int)((mul * 1000L) / 1000000000L));
    println_str("L-NEG="); println_int((int)((0L - pos) / 1000000000L));
    return 0;
}
