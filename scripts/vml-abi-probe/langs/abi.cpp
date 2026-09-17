/* abi.cpp —— 实参顺序探针（C++）。判据：`ABI=8`（反序得 9）。
 * 打印只用**单参**的 print_str / print_int —— 本前端的打印路径就是这样规避反序的，
 * 探针要孤立地测 `ipow` 这个两参调用。 */
int ipow(int base, int exp);
void print_str(char* s);
void print_int(int v);
void newline(void);

int main() {
    int r = ipow(2, 3);
    print_str("ABI=");
    print_int(r);
    newline();
    return 0;
}
