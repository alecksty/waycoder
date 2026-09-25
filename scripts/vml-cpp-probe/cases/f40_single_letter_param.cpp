// F40（原 OPEN #17）：7 参 + 单字母形参不能让别的类解析失败
#include <waycoder_ui.h>
class G { public: int v; G(){ v = 1; } int Get(){ return v; } };
int p7(int a, int b, int c, int d, int e, int f, int g){ return a + b + c + d + e + f + g; }
int main(){
    G g;
    if (g.Get() != 1) { cout << "类解析失败" << endl; return 1; }
    if (p7(1,2,3,4,5,6,7) != 28) { cout << "p7 结果不对" << endl; return 1; }
    cout << "ALL PASS" << endl;
    return 0;
}
