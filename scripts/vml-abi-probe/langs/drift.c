/* 栈漂移探针（C）：循环里反复调外部库函数（ipow），循环变量与累加器必须一字未动。
 * 判据：`DRIFT=126`（2^1+…+2^6）。
 *
 * 与 drift.f90 / drift.d / drift.rb 同族，但把「循环变量当实参」也压进了判据 ——
 * 一次量出四件事：循环会不会提前退出、实参顺序对不对、第 2 个实参有没有丢、栈漂没漂。
 * 反序得 91（i^2 之和）、第 2 参丢得 6（ipow(2,0)=1）、只跑 2 轮则远小于 126。
 *
 * C 是这套判据的历史基准（run.sh 的 6 条探针全绿），这里等于把基准也纳入跨语言那张表。 */
__stdcall int ipow(int base, int exp);
__stdcall void print_str(char* s);
__stdcall void print_int(int v);
__stdcall void newline(void);

int main(void) {
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
