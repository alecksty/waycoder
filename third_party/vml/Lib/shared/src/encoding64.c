#param lib("encoding")

// VML Shared Encoding64 Library — 64-bit Length Encoding Operations
// Wraps existing encoding functions with long len/max_bytes parameters

__stdcall long lutf8_strlen(const char* s) { return encoding_utf8_strlen(s); }

__stdcall long lutf8_decode(unsigned char* src, long len) {
    return encoding_utf8_decode(src, (int)(len > 0x7FFFFFFF ? 0x7FFFFFFF : len));
}

__stdcall long lutf8_encode(long codepoint, unsigned char* dst, long max_bytes) {
    return encoding_utf8_encode((int)codepoint, dst, (int)(max_bytes > 0x7FFFFFFF ? 0x7FFFFFFF : max_bytes));
}
