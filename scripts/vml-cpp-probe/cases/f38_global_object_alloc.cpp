// F38：全局对象必须按类的大小分配
//   修前 var_gcp 只占 1 个 word ⇒ 字段写踩到相邻全局量
#include <waycoder_ui.h>
class Counter {
public:
    int a; int b; int c;
    void Set(int v) { a = v; b = v + 1; c = v + 2; }
    int Sum() { return a + b + c; }
};
static Counter gc;
static int guard1 = 111;
static int guard2 = 222;
int main(){
    int bad = 0;
    gc.Set(10);
    if (gc.Sum() != 33) { cout << "gc 自己坏了" << endl; bad = 1; }
    if (guard1 != 111) { cout << "guard1 被踩成 " << guard1 << endl; bad = 1; }
    if (guard2 != 222) { cout << "guard2 被踩成 " << guard2 << endl; bad = 1; }
    if (bad == 0) { cout << "ALL PASS" << endl; }
    return bad;
}
