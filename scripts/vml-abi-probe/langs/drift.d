// 栈漂移探针（D）：`^^` 被编成 CALL ipow，那条路径「压了必须由调用方清」。
// 判据：`DRIFT=126`（2^1+…+2^6）。2026-09-17 修复前实测 66。
void main() {
    int i = 0;
    int s = 0;
    for (i = 1; i <= 6; i = i + 1) {
        s = s + (2 ^^ i);
    }
    writeln("DRIFT=", s);
}
