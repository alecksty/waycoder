// F44（原 OPEN #13）：结构体整体赋值要按**字节大小**搬，不能按"字段数"
//
// 症状：搬运侧按 `Members.Count`（扁平字段数）、访问侧按 `ClassSizeDeep`（字节数）⇒
//   有**内嵌对象**或**基类**时两者不等，**尾巴上的字段搬不过去**（静默）。
//
// ⚠ 用例要**两件事都钉**：
//   ① 结构体整体赋值 `o2 = o1`；
//   ② 按值传参 `f(Outer o)`。
//   两条走的是**不同的函数**（`EmitStructFieldCopy` / `GetStructSlotCount`），
//   只测一条会漏掉另一条 —— 实测修之前正是「赋值坏、传参好」。
#include <waycoder_ui.h>
class Inner { public: int a; int b; };
class Outer { public: Inner in; int c; };
int tsum(Outer o) { return o.in.a + o.in.b + o.c; }
int main() {
    Outer o1; Outer o2;
    o1.in.a = 1; o1.in.b = 2; o1.c = 3;
    o2 = o1;
    int sc = o2.in.a + o2.in.b + o2.c;
    if (sc != 6) { cout << "结构体赋值不对 copy=" << sc << "（应 6）" << endl; return 1; }
    int sp = tsum(o1);
    if (sp != 6) { cout << "按值传参不对 param=" << sp << "（应 6）" << endl; return 1; }
    cout << "ALL PASS" << endl;
    return 0;
}
