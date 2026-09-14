#param lib("string")

// VML Shared Base64 Library
// Base64 编解码 — MCU兼容

static const char b64_table[] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

// base64_encode: 编码 data[len] → dst (调用者分配 dst, 大小 = (len+2)/3*4+1)
__stdcall int base64_encode(const char* data, int len, char* dst) {
    char* start = dst;
    int i;
    for (i = 0; i < len; i += 3) {
        int remaining = len - i;
        unsigned int group = ((unsigned char)data[i]) << 16;
        if (remaining > 1) group |= ((unsigned char)data[i + 1]) << 8;
        if (remaining > 2) group |= (unsigned char)data[i + 2];
        *dst++ = b64_table[(group >> 18) & 0x3F];
        *dst++ = b64_table[(group >> 12) & 0x3F];
        *dst++ = (remaining > 1) ? b64_table[(group >> 6) & 0x3F] : '=';
        *dst++ = (remaining > 2) ? b64_table[group & 0x3F] : '=';
    }
    *dst = 0;
    return (int)(dst - start);
}

static int _b64_decode_char(char c) {
    if (c >= 'A' && c <= 'Z') return c - 'A';
    if (c >= 'a' && c <= 'z') return c - 'a' + 26;
    if (c >= '0' && c <= '9') return c - '0' + 52;
    if (c == '+') return 62;
    if (c == '/') return 63;
    return -1;
}

// base64_decode: 解码 src → dst (调用者分配 dst, 大小 ≤ strlen(src))
// 返回解码后长度
__stdcall int base64_decode(const char* src, char* dst) {
    char* start = dst;
    while (*src) {
        int v0 = _b64_decode_char(src[0]);
        int v1 = _b64_decode_char(src[1]);
        if (v0 < 0 || v1 < 0) break;
        *dst++ = (char)((v0 << 2) | (v1 >> 4));
        if (src[2] == '=') break;
        int v2 = _b64_decode_char(src[2]);
        if (v2 < 0) break;
        *dst++ = (char)((v1 << 4) | (v2 >> 2));
        if (src[3] == '=') break;
        int v3 = _b64_decode_char(src[3]);
        if (v3 < 0) break;
        *dst++ = (char)((v2 << 6) | v3);
        src += 4;
    }
    return (int)(dst - start);
}
