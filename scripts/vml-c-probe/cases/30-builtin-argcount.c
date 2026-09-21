// **内置函数少给参数 ⇒ 编译器崩掉**（一行 .NET 异常文本，没有文件/行号/诊断码）。
//
// ## 为什么单列一条
//
// `getenv`/`setenv`/`PEEK`/`POKE` 是**前端直接生成指令**的（不走普通调用那条路），
// 它们的实现里直接取 `Args[0]`/`Args[1]` —— **没有 `Args.Count` 守卫**。
// 而同文件里 `exit` 有守卫，另外几个内置函数也有各自的写法 ⇒ 这四条是漏的。
//
// 实测（修前）：
//
//     int main(){ (void)getenv(); }
//     ✘ 编译失败（c）：⚠️ 编译失败：Index was out of range. Must be non-negative
//       and less than the size of the collection. (Parameter 'index')
//
// 用户看到的就是这么一行 —— **既不知道是哪个文件哪一行，也不知道错在哪**。
//
// ## 判据是「报出**可读的**诊断」，不是「编译失败」
//
// 编译失败是**对的**（少参数确实编不出来），错的是**失败的方式**。
// 所以这里断言的是：stderr 里出现 `CodeGen_ArgCountMismatch`（诊断码）
// 与 `'getenv' 需要 1 个参数`（说清哪个函数、要几个、给了几个）。
//
// ⚠ **一次要报出两个**：下面 `POKE` 和 `setenv` 各缺一个参数。
//   诊断是**收集**起来最后一起抛的（`Diags`），所以两行都该出现 ——
//   "遇到第一个错就停"会让用户改一个再编一次，来回好几轮。
//
// ⚠ 普通的库函数（`strlen()`）与用户函数**仍然不做参数个数检查**（实测）。
//   这条只保证**内置函数这条路不再是崩溃** —— 要统一得先有"声明表"，那是另一件事。
#include <stdlib.h>

int main()
{
    (void)getenv();          /* getenv 需要 1 个 */
    POKE(1);                 /* POKE 需要 2 个 */
    setenv("A");             /* setenv 需要 2 个 */
    return 0;
}
// EXPECT-COMPILE-ERROR: CodeGen_ArgCountMismatch
// EXPECT-COMPILE-ERROR: 'getenv' 需要 1 个参数，这里只给了 0 个
// EXPECT-COMPILE-ERROR: 'POKE' 需要 2 个参数，这里只给了 1 个
// EXPECT-COMPILE-ERROR: 'setenv' 需要 2 个参数，这里只给了 1 个
