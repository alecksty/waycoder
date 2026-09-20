// hdr-loc.cpp —— **头文件里的错必须指到头文件那一行**，不能指到用户文件上。
//
// 报错的位置来自前端的 `Preprocessor.LineMap`（预处理是把头文件内容**拼进同一个流**的，
// 所以词法/语法拿到的行号是拼接后的）。不映射的话，`#include` 一展开，用户文件里
// 第 N 行就会替头文件的错背锅 —— 真机上就是「第 112 行那句无害的 `/// <summary>` 被标红」。
//
// 判据（见 hdr-loc.cpp.sym）：stderr 里必须出现**头文件名 + 它的行号**。
#include "hdr-loc-bad.h"

int main() { return 0; }
