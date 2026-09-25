#define N 4
#define M 6
class Box {
public:
    int cell[N];
    int more[M];
    Box(){ int i; for(i=0;i<N;i=i+1){ cell[i]=0; } for(i=0;i<M;i=i+1){ more[i]=0; } }
    void Set(int idx, int v){ cell[idx] = v; }
    void Shift(){ int i; for(i=0;i<N-1;i=i+1){ cell[i] = cell[i+1]; } }
    int Total(){ int s=0; int i; for(i=0;i<N;i=i+1){ s = s + cell[i]; } return s; }
    int Cross(){ int s=0; int i; for(i=0;i<M;i=i+1){ more[i] = i * 2; } for(i=0;i<M;i=i+1){ s = s + more[i]; } return s; }
};
int main(){
    Box b;
    b.Set(0,11); b.Set(1,22); b.Set(2,33); b.Set(3,44);
    int t1 = b.Total();
    b.Shift();
    int t2 = b.Total();
    int t3 = b.Cross();
    if (t1 == 110 && t2 == 143 && t3 == 30) { cout << "ALL PASS" << endl; } else { cout << "t1<>t2<>t3" << endl; }
    return 0;
}
