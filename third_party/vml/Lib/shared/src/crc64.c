// VML Shared CRC64 Library — 64-bit Hashes & CRCs

// CRC-64-ECMA polynomial: 0x42F0E1EBA9EA3693
// crc64(data, len) — 64位CRC
__stdcall long crc64(const char* data, long len) {
    long i, bit;
    long crc = -1; /* 初始值全1 */
    for (i = 0; i < len; i++) {
        crc ^= ((long)data[i] & 0xFF) << 56;
        for (bit = 0; bit < 8; bit++) {
            if (crc < 0) crc = (crc << 1) ^ 0x42F0E1EBA9EA3693L;
            else crc = crc << 1;
        }
    }
    return crc ^ -1;
}

// fnv1a_hash64(str) — FNV-1a 64位哈希
__stdcall long fnv1a_hash64(const char* str) {
    long hash = 0xCBF29CE484222325L; /* FNV offset basis 64-bit */
    int i = 0;
    while (str[i]) {
        hash ^= (long)(unsigned char)str[i];
        hash *= 0x100000001B3L; /* FNV prime 64-bit */
        i++;
    }
    return hash;
}

// djb2_hash64(str) — DJB2 64位哈希
__stdcall long djb2_hash64(const char* str) {
    long hash = 5381;
    int c;
    while ((c = str[0]) != 0) {
        hash = ((hash << 5) + hash) + (long)c; /* hash * 33 + c */
        str++;
    }
    return hash;
}

// sdbm_hash64(str) — SDBM 64位哈希
__stdcall long sdbm_hash64(const char* str) {
    long hash = 0;
    int c;
    while ((c = str[0]) != 0) {
        hash = (long)(unsigned char)c + (hash << 6) + (hash << 16) - hash;
        str++;
    }
    return hash;
}

// crc32 with long length
__stdcall int crc32_64l(const char* data, long len) {
    long i, j;
    int crc = -1;
    for (i = 0; i < len; i++) {
        crc ^= (int)(data[i] & 0xFF);
        for (j = 0; j < 8; j++) {
            if (crc & 1) crc = (crc >> 1) ^ 0xEDB88320;
            else crc = crc >> 1;
        }
    }
    return crc ^ -1;
}
