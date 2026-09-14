#param lib("math")

// 软双精度浮点模拟库 — C 实现 (兼容 VML C compiler)
// 格式: IEEE 754 双精度 (64-bit), 高位32位 = hi, 低位32位 = lo
// 高位: sign(1) | exponent(11) | mantissa_hi(20)
// 低位: mantissa_lo(32)
// 编译: dotnet run --project VMLPrepares/CCompiler Lib/shared/softdouble.c -o Lib/shared/softdouble.vml

// IEEE 754 双精度: (-1)^s * 2^(e-1023) * 1.m
// sign = (hi >> 31) & 1
// exponent = (hi >> 20) & 0x7FF
// mantissa_hi = hi & 0xFFFFF  (20 bits)
// mantissa_lo = lo            (32 bits)

// 偏置值
#define EXP_BIAS 1023
#define EXP_MASK 0x7FF00000
#define MANT_HI_MASK 0x000FFFFF
#define SIGN_MASK 0x80000000

// --- 简单操作 ---

// 取负: 翻转符号位
void __vml_double_neg(int* hi, int* lo) {
    *hi = *hi ^ SIGN_MASK;
}

// 绝对值: 清除符号位
void __vml_double_abs(int* hi, int* lo) {
    *hi = *hi & 0x7FFFFFFF;
}

// 比较: 返回 -1(a<b), 0(a==b), 1(a>b)
int __vml_double_cmp(int hi1, int lo1, int hi2, int lo2) {
    int s1 = (hi1 >> 31) & 1, s2 = (hi2 >> 31) & 1;
    // 符号不同, 正数大
    if (s1 != s2) return s2 - s1;
    // 同号, 先比高32位, 再比低32位
    if (hi1 != hi2) return (hi1 > hi2) ? 1 : -1;
    if (lo1 != lo2) return (lo1 > lo2) ? 1 : -1;
    return 0;
}

// --- 类型转换 ---

// 整数转双精度: int → double
void __vml_int2double(int x, int* hi, int* lo) {
    if (x == 0) { *hi = 0; *lo = 0; return; }
    int sign = 0;
    if (x < 0) { sign = 1; x = -x; }
    // 找到最高位
    int e = 0; int tmp = x;
    while (tmp > 0) { tmp >>= 1; e++; }
    e--; // 最高位位置
    // 实际指数 = 偏置 + 位置
    int exp = EXP_BIAS + e;
    // 移去最高位(隐含的1), 剩余52位
    int mant = x << (31 - e); // 取小数部分
    int mhi = (mant >> 12) & MANT_HI_MASK;
    int mlo = mant << 20;
    *hi = (sign << 31) | (exp << 20) | mhi;
    *lo = mlo;
}

// 双精度转整数 (截断)
int __vml_double2int(int hi, int lo) {
    int sign = (hi >> 31) & 1;
    int exp = ((hi >> 20) & 0x7FF) - EXP_BIAS;
    if (exp < 0) return 0; // 小于1
    if (exp > 30) return sign ? -2147483648 : 2147483647; // 溢出
    int mhi = hi & MANT_HI_MASK;
    // 组装完整mantissa: 隐含1 + mhi(20bit) + mlo(32bit) = 53bit
    // 右移 (52 - exp) 位得到整数部分
    if (exp <= 20) {
        // 整数部分完全在高32位
        int val = (1 << exp) | (mhi >> (20 - exp));
        return sign ? -val : val;
    } else {
        // exp > 20, 需要用到低位
        // 注意: VML C compiler 不支持 long long, 使用双 int 近似
        int shift = 52 - exp;
        // 简化: exp在20-30之间时, 用高bit+部分低位
        int val = (1 << exp) | (mhi << (exp - 20)) | (lo >> (52 - exp));
        return sign ? -val : val;
    }
}

// 浮点(32位IEEE 754)转双精度
void __vml_float2double(int f32, int* hi, int* lo) {
    int sign = f32 >> 31;
    int exp = (f32 >> 23) & 0xFF;
    int mant = f32 & 0x7FFFFF;
    if (exp == 0 && mant == 0) { *hi = 0; *lo = 0; return; } // 零
    if (exp == 255) { // Inf/NaN
        *hi = (sign << 31) | 0x7FF00000 | (mant << 20);
        *lo = mant << 12;
        return;
    }
    // 双精度指数 = 单精度指数 + (1023 - 127)
    int dexp = exp + (EXP_BIAS - 127);
    *hi = (sign << 31) | (dexp << 20) | (mant >> 3);
    *lo = mant << 29;
}

// 双精度转浮点(32位IEEE 754) — 可能有精度损失
int __vml_double2float(int hi, int lo) {
    int sign = hi >> 31;
    int exp = ((hi >> 20) & 0x7FF) - EXP_BIAS + 127; // 转为单精度偏置
    int mhi = hi & MANT_HI_MASK;
    if (exp <= 0) return 0; // 下溢
    if (exp >= 255) { // 上溢为Inf
        return (sign << 31) | 0x7F800000;
    }
    int fmant = (mhi << 3) | (lo >> 29);
    return (sign << 31) | (exp << 23) | fmant;
}

// --- 算术运算 (简化实现, 处理常规数) ---

// 双精度加法
void __vml_double_add(int* hi, int* lo, int bhi, int blo) {
    int s1 = (*hi >> 31) & 1, s2 = (bhi >> 31) & 1;
    int e1 = (*hi >> 20) & 0x7FF, e2 = (bhi >> 20) & 0x7FF;
    int m1_hi = *hi & MANT_HI_MASK, m2_hi = bhi & MANT_HI_MASK;
    int m1_lo = *lo, m2_lo = blo;

    // 隐含1 (除非指数为0: 非规格化数, 这里简化处理)
    if (e1 != 0) m1_hi |= 0x100000; // 加隐含1
    if (e2 != 0) m2_hi |= 0x100000;

    // 对齐指数: 让e1 >= e2
    if (e1 < e2) {
        int t; t = e1; e1 = e2; e2 = t;
        t = s1; s1 = s2; s2 = t;
        t = m1_hi; m1_hi = m2_hi; m2_hi = t;
        t = m1_lo; m1_lo = m2_lo; m2_lo = t;
    }
    int shift = e1 - e2;
    if (shift > 52) shift = 52;

    // 右移m2 (对齐)
    // m2是53-bit值 (m2_hi:21bit + m2_lo:32bit)
    if (shift > 0) {
        if (shift < 32) {
            m2_lo = (m2_lo >> shift) | (m2_hi << (32 - shift));
            m2_hi >>= shift;
        } else {
            m2_lo = m2_hi >> (shift - 32);
            m2_hi = 0;
        }
    }

    // 加法/减法
    int r_hi, r_lo;
    if (s1 == s2) {
        // 同号相加
        r_lo = m1_lo + m2_lo;
        r_hi = m1_hi + m2_hi;
        if ((unsigned int)r_lo < (unsigned int)m1_lo) r_hi++; // 进位
    } else {
        // 异号相减
        r_lo = m1_lo - m2_lo;
        r_hi = m1_hi - m2_hi;
        if (m1_lo < m2_lo) r_hi--; // 借位
        // 如果结果为负, 取绝对值并翻转符号
        if (r_hi < 0) {
            r_hi = -r_hi; r_lo = -r_lo;
            if (r_lo > 0) { r_lo = 0; r_hi--; } // 简化
            s1 = s2; // 取减数的符号
        }
    }

    // 规格化: 移除前置零
    // r_hi:21bit, r_lo:32bit, 隐含最高位在bit 52
    // 正常化: 确保最高位在bit 52位置
    if (r_hi & 0x200000) { // 进位到bit 53
        r_lo = (r_lo >> 1) | ((r_hi & 1) << 31);
        r_hi = (r_hi >> 1) | 0x100000;
        e1++;
    } else if (r_hi & 0x100000) {
        // 已经正常
        // 移除隐含1, 取低20bit
    } else {
        // 需要左移
        while (e1 > 0 && !(r_hi & 0x100000)) {
            r_hi = (r_hi << 1) | (r_lo >> 31);
            r_lo <<= 1;
            e1--;
        }
        if (e1 == 0) { *hi = 0; *lo = 0; return; } // 下溢为零
    }

    // 去除隐含1, 组合结果
    r_hi = (r_hi & MANT_HI_MASK);
    *hi = (s1 << 31) | (e1 << 20) | r_hi;
    *lo = r_lo;
}

// 双精度减法: a - b = a + (-b)
void __vml_double_sub(int* hi, int* lo, int bhi, int blo) {
    int nbhi = bhi ^ SIGN_MASK;
    __vml_double_add(hi, lo, nbhi, blo);
}

// 双精度乘法 (简化)
void __vml_double_mul(int* hi, int* lo, int bhi, int blo) {
    int s1 = (*hi >> 31) & 1, s2 = (bhi >> 31) & 1;
    int e1 = (*hi >> 20) & 0x7FF, e2 = (bhi >> 20) & 0x7FF;
    int m1_hi = (*hi & MANT_HI_MASK) | 0x100000;
    int m2_hi = (bhi & MANT_HI_MASK) | 0x100000;
    int m1_lo = *lo, m2_lo = blo;

    // 指数相加, 减去一个偏置
    int e = e1 + e2 - EXP_BIAS;

    // 乘法: 53-bit * 53-bit = 106-bit, 取高53位
    // 简化: 只取高32-bit * 高32-bit + 交叉项
    // m1_hi(21bit) * m2_hi(21bit) = 42bit
    // 取高21bit作为新mantissa的高位
    long long product = (long long)m1_hi * m2_hi;
    // C编译器不支持long long, 用32-bit乘法分解
    int a_lo = m1_hi & 0xFFFF, a_hi = m1_hi >> 16;
    int b_lo = m2_hi & 0xFFFF, b_hi = m2_hi >> 16;
    int mid = a_lo * b_hi + b_lo * a_hi;
    int r_hi = a_hi * b_hi + (mid >> 16);
    int r_lo = (a_lo * b_lo) + (mid << 16);

    // 规格化: 确保最高位在bit 52位置
    // r_hi(21bit)需要最高位对齐到bit 52
    // 101-bit乘积, 取高53位
    while (!(r_hi & 0x100000) && e > 0) {
        r_hi = (r_hi << 1) | (r_lo >> 31);
        r_lo <<= 1;
        e--;
    }
    if (e <= 0) { *hi = 0; *lo = 0; return; } // 下溢

    r_hi &= MANT_HI_MASK;
    *hi = ((s1 ^ s2) << 31) | (e << 20) | r_hi;
    *lo = r_lo;
}

// 双精度除法 (简化)
void __vml_double_div(int* hi, int* lo, int bhi, int blo) {
    int s1 = (*hi >> 31) & 1, s2 = (bhi >> 31) & 1;
    int e1 = (*hi >> 20) & 0x7FF, e2 = (bhi >> 20) & 0x7FF;
    int m1_hi = (*hi & MANT_HI_MASK) | 0x100000;
    int m2_hi = (bhi & MANT_HI_MASK) | 0x100000;
    int m1_lo = *lo, m2_lo = blo;

    // 除零检查
    if ((bhi & 0x7FFFFFFF) == 0 && blo == 0) {
        *hi = s1 ? 0xFFF00000 : 0x7FF00000; // ±Inf
        *lo = 0;
        return;
    }

    int e = e1 - e2 + EXP_BIAS;

    // 长除法: 53-bit / 53-bit → 53-bit
    // 简化: 只取高32bit进行除法
    // dividend = m1(m1_hi:21bit + m1_lo:32bit) << 52
    // divisor = m2(m2_hi:21bit + m2_lo:32bit)
    // 用高32bit近似
    int num = m1_hi; // 简化: 只使用高21bit
    int den = m2_hi; // 简化
    if (den == 0) { *hi = 0; *lo = 0; return; }
    int quot = (num << 15) / den;
    if (quot == 0) { *hi = 0; *lo = 0; return; }

    // 规格化
    while (!(quot & 0x100000) && e > 0) {
        quot <<= 1; e--;
    }
    if (e <= 0) { *hi = 0; *lo = 0; return; }

    int r_hi = quot & MANT_HI_MASK;
    int r_lo = 0;
    *hi = ((s1 ^ s2) << 31) | (e << 20) | r_hi;
    *lo = r_lo;
}
