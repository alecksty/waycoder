/* 栈漂移探针（C++）：循环里反复调外部库函数（ipow）。判据：`DRIFT=126`（2^1+…+2^6）。
 * 反序得 91（i^2 之和）、第 2 参丢得 6（ipow(2,0)=1）。
 * 与 abi.cpp 配成一对：那条只看一次两参调用，这条把「循环变量当实参」也纳进来。 */
int ipow(int base, int exp);
void print_str(char* s);
void print_int(int v);
void newline(void);

int main() {
    int i;
    int s;
    s = 0;
    for (i = 1; i <= 6; i = i + 1) {
        s = s + ipow(2, i);
    }
    print_str("DRIFT=");
    print_int(s);
    newline();
    return 0;
}
