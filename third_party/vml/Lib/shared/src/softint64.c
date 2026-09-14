// 64位整数模拟库
void __vml_i64_add(int* l, int* h, int bl, int bh) {
    int rl = *l + bl; int rh = *h + bh;
    if (rl < *l) rh = rh + 1;
    *l = rl; *h = rh;
}

void __vml_i64_sub(int* l, int* h, int bl, int bh) {
    int rl = *l - bl; int rh = *h - bh;
    if (rl > *l) rh = rh - 1;
    *l = rl; *h = rh;
}

void __vml_i64_neg(int* l, int* h) {
    *l = ~(*l) + 1; *h = ~(*h);
    if (*l == 0) *h = *h + 1;
}

void __vml_i64_and(int* l, int* h, int bl, int bh) { *l = *l & bl; *h = *h & bh; }
void __vml_i64_or(int* l, int* h, int bl, int bh)  { *l = *l | bl; *h = *h | bh; }
void __vml_i64_xor(int* l, int* h, int bl, int bh) { *l = *l ^ bl; *h = *h ^ bh; }
void __vml_i64_not(int* l, int* h) { *l = ~(*l); *h = ~(*h); }

void __vml_i64_shl(int* l, int* h, int n) {
    if (n >= 32) { *h = *l << (n - 32); *l = 0; }
    else if (n > 0) { *h = (*h << n) | (*l >> (32 - n)); *l = *l << n; }
}

void __vml_i64_shr(int* l, int* h, int n) {
    if (n >= 32) { *l = *h >> (n - 32); *h = *h >> 31; }
    else if (n > 0) { *l = (*l >> n) | (*h << (32 - n)); *h = *h >> n; }
}
