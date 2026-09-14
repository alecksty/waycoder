#param lib("math")

// VML Shared Fixed-Point Math Library
// Q15.16 定点数运算 — MCU友好, 无浮点单元可用

// Q15.16 multiply: (a * b) >> 16
__stdcall int mul(int a, int b) {
    long long result = (long long)a * (long long)b;
    return (int)(result >> 16);
}

// Q15.16 divide: (a << 16) / b
__stdcall int div(int a, int b) {
    if (b == 0) return 0x7FFFFFFF; // max positive
    long long aa = (long long)a << 16;
    return (int)(aa / b);
}

// int to Q15.16: value << 16
__stdcall int from_int(int value) {
    return value << 16;
}

// Q15.16 to int: value >> 16
__stdcall int to_int(int value) {
    return value >> 16;
}

// Q15.16 to int with rounding: (value + 0.5) >> 16
__stdcall int to_int_round(int value) {
    return (value + 0x8000) >> 16;
}

// float to Q15.16: (int)(f * 65536.0f) — 简化实现
__stdcall int from_float(float f) {
    return (int)(f * 65536.0f);
}

// Q15.16 to float: value / 65536.0f
__stdcall float to_float(int value) {
    return (float)value / 65536.0f;
}

// Q15.16 sqrt: Newton's method
__stdcall int sqrt(int value) {
    if (value <= 0) return 0;
    int guess = value >> 1;
    if (guess < 1) guess = 1;
    int i;
    for (i = 0; i < 10; i++) {
        int div = guess > 0 ? ((value << 16) / guess) >> 16 : 0;
        guess = (guess + div) >> 1;
    }
    return guess;
}

// Q15.16 sin(radians) — Taylor series approximation
// Input: radians * 65536 (Q15.16)
__stdcall int sin(int rad_q16) {
    // Normalize to -PI..PI
    int pi = 205887; // PI * 65536
    int pi2 = 411775; // 2*PI * 65536
    while (rad_q16 > pi) rad_q16 -= pi2;
    while (rad_q16 < -pi) rad_q16 += pi2;
    // Taylor: sin(x) = x - x^3/3! + x^5/5! - x^7/7!
    int x = rad_q16;
    int x2 = mul(x, x);      // x^2
    int x3 = mul(x2, x);     // x^3
    int term1 = x;                  // x
    int term2 = -(x3 / 6);          // -x^3/6
    int x5 = mul(x3, x2);    // x^5
    int term3 = x5 / 120;           // x^5/120
    int x7 = mul(x5, x2);    // x^7
    int term4 = -(x7 / 5040);       // -x^7/5040
    return term1 + term2 + term3 + term4;
}

// Q15.16 cos(radians) = sin(rad + PI/2)
__stdcall int cos(int rad_q16) {
    int pi_half = 102944; // PI/2 * 65536
    return sin(rad_q16 + pi_half);
}

// Q15.16 atan2(y, x) — 简化逼近
__stdcall int atan2(int y, int x) {
    if (x == 0) {
        if (y > 0) return 102944;  // PI/2
        if (y < 0) return -102944; // -PI/2
        return 0;
    }
    // atan(y/x) approximation for Q15.16
    int abs_y = y >= 0 ? y : -y;
    int abs_x = x >= 0 ? x : -x;
    int ratio = (abs_x > 0) ? (abs_y << 16) / abs_x : 0x7FFFFFFF;
    // Fast atan approximation: z / (1 + 0.28*z^2) where z = ratio/65536
    int z = ratio; // already Q15.16
    int z2 = mul(z, z);
    int denom = 65536 + (z2 * 28) / 100; // 1 + 0.28*z^2 in Q15.16
    int atan = (denom > 0) ? ((z << 16) / denom) : 102944;
    // Adjust quadrant
    if (x < 0) atan = (y >= 0) ? 205887 - atan : -205887 + atan;
    return atan;
}
