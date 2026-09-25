// F25：类里能放数组字段，且字段偏移要对
#include <waycoder_ui.h>
class Holder {
public:
    int n;
    int data[4];
    Holder(){ n = 0; }
    void Set(int i, int v){ data[i] = v; }
    int Get(int i){ return data[i]; }
    int Sum(){ return data[0] + data[1] + data[2] + data[3]; }
};
int main(){
    Holder h;
    h.n = 5;
    h.Set(0, 11); h.Set(1, 22); h.Set(2, 33); h.Set(3, 44);
    if (h.n != 5) { cout << "n 被数组压坏" << endl; return 1; }
    if (h.Sum() != 110) { cout << "Sum=" << h.Sum() << endl; return 1; }
    cout << "ALL PASS" << endl;
    return 0;
}
