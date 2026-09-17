// 栈漂移探针（Java）：循环里反复调外部库函数（ipow）。判据：`DRIFT=126`（2^1+…+2^6）。
// 反序得 91（i^2 之和）、第 2 参丢得 6（ipow(2,0)=1）。
// ⚠ 本前端「被调方 param0 读 R0」那条审计项若仍成立，这里会先露馅（ipow 的第 1 参是常量 2，
//    恰好是 R0 里最可能残留的值 ⇒ 可能假绿）；真要看 param0，得让第 1 个实参也来自变量。
class Drift {
    static native int ipow(int base, int exp);
    static native void print_str(String s);
    static native void print_int(int v);
    static native void newline();

    public static void main(String[] args) {
        int s = 0;
        for (int i = 1; i <= 6; i = i + 1) {
            s = s + ipow(2, i);
        }
        print_str("DRIFT=");
        print_int(s);
        newline();
    }
}
