// abi.java —— 实参顺序探针（Java）。判据：`ABI=8`（反序得 9）。
class Abi {
    static native int ipow(int base, int exp);
    static native void print_str(String s);
    static native void print_int(int v);
    static native void newline();

    public static void main(String[] args) {
        int r = ipow(2, 3);
        print_str("ABI=");
        print_int(r);
        newline();
    }
}
