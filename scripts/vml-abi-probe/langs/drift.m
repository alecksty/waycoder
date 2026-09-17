// 栈漂移探针（ObjC）：外部函数调用点「压了不清」（2026-09-17 已补调用方清栈）。
// 判据：`DRIFT=126`（2^1+…+2^6）。
// ⚠ 2026-09-17 实测：补清栈前 66、补后 2059、重生成前基线也是 2059 ——
//    说明 2059 是**另一个独立的既有多参缺陷**，不是栈漂移。见 README「已知失败」。
int ipow(int base, int exp);
int main() {
    int s;
    int i;
    s = 0;
    for (i = 1; i <= 6; i = i + 1) { s = s + ipow(2, i); }
    printf("DRIFT=%d\n", s);
    return 0;
}
