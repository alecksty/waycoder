// F43（原 OPEN #14）：`arr[i] = T(实参)` 要在**元素地址上**就地构造
//
// 症状：RHS 被当成普通自由函数调用，只拿到**一个 word** 的返回值，
//   而元素（`P` 两个 int）是 **8 字节** ⇒ 每个元素只写进去半个对象。
//   实测 `x` 求和得 60（应 6）、`y` 求和得 0（应 60）—— **静默**，一个错都不报。
//
// ⚠ 症状看着像"读的偏移多 4 字节"（读到的 `x` 是 `y` 的值），
//   很容易把人引去改**读取**路径；而读取一直是对的，是**写只写了一半**。
//   所以这个用例**同时钉两个方向的求和**，只坏一半时两边都不对、藏不住。
#include <waycoder_ui.h>
class P { public: int x; int y; P(){ x = 0; y = 0; } P(int a, int b){ x = a; y = b; } };
int main() {
    P arr[3];
    int i;
    for (i = 0; i < 3; i = i + 1) { arr[i] = P(i + 1, (i + 1) * 10); }
    int sx = arr[0].x + arr[1].x + arr[2].x;
    int sy = arr[0].y + arr[1].y + arr[2].y;
    if (sx != 6) { cout << "x 求和不对 " << sx << "（应 6）" << endl; return 1; }
    if (sy != 60) { cout << "y 求和不对 " << sy << "（应 60）" << endl; return 1; }
    cout << "ALL PASS" << endl;
    return 0;
}
