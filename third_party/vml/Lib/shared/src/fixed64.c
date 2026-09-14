// VML Shared Fixed64 Library — Q31.32 Fixed-Point Math (64-bit int representation)

__stdcall long lfixed_mul64(long a, long b) {
    // Q31.32 multiplication: (a * b) >> 32
    // Use decomposing to avoid 128-bit intermediate
    long ah = a >> 32, al = a & 0xFFFFFFFF;
    long bh = b >> 32, bl = b & 0xFFFFFFFF;
    long mid = ah * bl + al * bh;
    long result = ((ah * bh) << 32) + (unsigned long)(al * bl) >> 32 + mid;
    return result;
}

__stdcall long lfixed_div64(long a, long b) {
    if (b == 0) return 0;
    // (a << 32) / b with sign handling
    int sign = 1;
    if (a < 0) { a = -a; sign = -sign; }
    if (b < 0) { b = -b; sign = -sign; }
    long q = 0;
    int i;
    for (i = 0; i < 64; i++) { q <<= 1; if (a >= b) { a -= b; q |= 1; } a <<= 1; }
    return sign * q;
}

__stdcall long lfixed_from_int64(long value) {
    return value << 32;
}

__stdcall long lfixed_to_int64(long value) {
    return value >> 32;
}

__stdcall long lfixed_to_int_round64(long value) {
    return (value + (1L << 31)) >> 32;
}

__stdcall long lfixed_from_double(double f) {
    return (long)(f * (double)(1L << 32));
}

__stdcall double lfixed_to_double(long value) {
    return (double)value / (double)(1L << 32);
}
