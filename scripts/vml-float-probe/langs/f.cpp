// f.cpp
void println_str(char* s);
void println_int(int v);
int main() {
    float  a = 3.14f, b = 2.0f, h = 0x10;
    double d = -0.5;
    println_str("F-MUL="); println_int((int)(a * b * 100.0f));
    println_str("F-NEG="); println_int((int)(d * 100.0));
    println_str("F-HEX="); println_int((int)(h * 100.0f));
    return 0;
}
