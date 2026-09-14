// 软浮点模拟库 — C 实现 (兼容 VML C compiler)
// 格式: 定点数 Q15.16, 实际值 = 整数值 / 32768
// 编译: dotnet run --project VMLPrepares/CCompiler Lib/shared/softfloat.c -o Lib/shared/softfloat.vml

int __vml_float_add(int a, int b) { return a + b; }
int __vml_float_sub(int a, int b) { return a - b; }

int __vml_float_mul(int a, int b) {
    int a_lo = a & 0xFFFF, a_hi = a >> 16;
    int b_lo = b & 0xFFFF, b_hi = b >> 16;
    int mid = a_lo * b_hi + b_lo * a_hi;
    return (a_lo * b_lo >> 15) + (mid >> 15 << 16) + (mid << 17) + ((a_hi * b_hi) << 17);
}

int __vml_float_div(int a, int b) { return (a / b) << 15 | ((a % b) << 15) / b; }
int __vml_int2float(int x) { return x << 15; }
int __vml_float2int(int x) { return x >= 0 ? (x + 16384) >> 15 : (x - 16384) >> 15; }
int __vml_float_neg(int a) { return -a; }
int __vml_float_abs(int a) { return a >= 0 ? a : -a; }
int __vml_float_cmp(int a, int b) { if (a < b) return -1; if (a > b) return 1; return 0; }
