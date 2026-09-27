// f.c —— 浮点：常量、运算、负数、十六进制（判据见 run-langs.sh）
__stdcall void println_str(char* s);
__stdcall void println_int(int v);
int main() {
    float  a = 3.14f;
    float  b = 2.0f;
    float  h = 0x10;      /* 十六进制常量当浮点用 */
    double d = -0.5;
    println_str("F-MUL=");  println_int((int)(a * b * 100.0f));
    println_str("F-NEG=");  println_int((int)(d * 100.0));
    println_str("F-HEX=");  println_int((int)(h * 100.0f));
    return 0;
}
