// nat.typedef.cpp —— C++ 前端对 **C 头文件里那三种 typedef 形态**的支持。
//
// 为什么单独立一条（而不是塞进 nat.cpp）：它压的是**解析 + 别名登记**这条链，
// 与「标准输出能不能用」是两件事 —— 混在一起，红了分不出是哪边坏的。
//
// 三种形态都必须能编过、且**别名当类型用要真的能用**（这一步才是判据的重点）：
//   ① typedef struct { ... } Alias;      匿名结构体 + 别名
//   ② typedef struct Tag { ... } Alias;  结构体定义 + 别名
//   ③ typedef struct Tag Alias;          **引用已声明的**结构体（`Lib/c/time.h:31` 就是它）
//
// ⚠ ③ 是用户真机报上来的那个：老前端把 `struct` 后面的**标签**当成别名吃掉，
//   紧接着撞在真正的别名上，于是 `#include <time.h>` 的 C++ 程序**一律编不过**，
//   而且报错的行号是预处理拼接后的（指到用户文件里另一行）。
//   ⇒ 这条探针**顺带**把 `#include <time.h>`（整份头文件 46 行）也压住了。
//
// 判据 = 编译成功 + 输出逐字节等于 nat.typedef.cpp.expect。
#include <time.h>

typedef struct point { int x; int y; } pt_t;   // ② 定义 + 别名
typedef struct tag_p p_alias_t;                 // ③ 前置：只声明别名，定义在后
struct tag_p { int a; int b; };

int main() {
    tm_t t;          // ③ 来自 <time.h>：`typedef struct tm tm_t;`
    t.tm_sec = 7;
    t.tm_min = 42;

    pt_t p;          // ②
    p.x = 3;
    p.y = 4;

    p_alias_t q;     // ③ 前置形态
    q.a = 5;
    q.b = 6;

    printf("TD-TM=%d,%d\n", t.tm_sec, t.tm_min);
    printf("TD-PT=%d,%d\n", p.x, p.y);
    printf("TD-PA=%d,%d\n", q.a, q.b);
    return 0;
}
