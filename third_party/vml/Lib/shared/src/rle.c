// VML Shared RLE Compression Library
// Run-Length Encoding — MCU兼容

// rle_encode: RLE 编码 data[len] → dst, 返回编码后长度
// 格式: [count byte][data byte] 重复对, count=0 标记结束
__stdcall int rle_encode(const char* data, int len, char* dst) {
    if (len <= 0) { dst[0] = 0; return 1; }
    char* start = dst;
    int i = 0;
    while (i < len) {
        char current = data[i];
        int count = 1;
        while (i + count < len && data[i + count] == current && count < 255) count++;
        *dst++ = (char)count;
        *dst++ = current;
        i += count;
    }
    *dst++ = 0; // terminator
    return (int)(dst - start);
}

// rle_decode: RLE 解码 src → dst, 返回解码后长度
__stdcall int rle_decode(const char* src, char* dst) {
    char* start = dst;
    while (1) {
        int count = (unsigned char)*src++;
        if (count == 0) break;
        char value = *src++;
        int i;
        for (i = 0; i < count; i++) *dst++ = value;
    }
    return (int)(dst - start);
}
