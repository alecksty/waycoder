// abi.m —— 实参顺序探针（ObjC）。判据：`ABI=8`（反序得 9、第二个实参读成 0 得 1）。
// 与 drift.m 分开：那条测的是「循环里反复调用后的累加」，这条只看一次两参调用。
int ipow(int base, int exp);
int main() {
    int r;
    r = ipow(2, 3);
    printf("ABI=%d\n", r);
    return 0;
}
