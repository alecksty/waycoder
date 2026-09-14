#param lib("statistics64")

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
