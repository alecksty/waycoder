#param lib("statistics64")

// 前向声明 —— **类型必须写全**：被调函数在别的模块里，没有声明时前端按默认
// `int` 猜形参 ⇒ `long`/`double` 被压成 4 字节，而被调方按 8 字节读，
// 实参整体错位（实测 print_long 打出空、64 位值丢高半字）。
__stdcall long lsum64(long* arr);
__stdcall long lmean64(long* arr);
__stdcall long lmax_arr64(long* arr);
__stdcall long lmin_arr64(long* arr);


// VML Shared CrossLang64 — 64-bit Cross-Language Arithmetic Bridge

__stdcall long ladd64(long a, long b) { return a + b; }
__stdcall long lsub64(long a, long b) { return a - b; }
__stdcall long lmul64(long a, long b) { return a * b; }
__stdcall long ldiv64(long a, long b) { if (b == 0) return 0; return a / b; }
__stdcall long lmod64(long a, long b) { if (b == 0) return 0; return a % b; }
__stdcall long lneg64(long x) { return -x; }

__stdcall long larr_sum64(long* arr) { return lsum64(arr); }
__stdcall long larr_avg64(long* arr) { return lmean64(arr); }
__stdcall long larr_max64(long* arr) { return lmax_arr64(arr); }
__stdcall long larr_min64(long* arr) { return lmin_arr64(arr); }
