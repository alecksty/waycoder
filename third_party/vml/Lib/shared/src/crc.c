// VML Shared CRC / Hash Library
// MCU-compatible checksum and hash functions

// crc8: 8-bit CRC (CRC-8-ATM, poly=0x07)
__stdcall int crc8(const char* data, int len) {
    unsigned char crc = 0;
    int i, j;
    for (i = 0; i < len; i++) {
        crc ^= (unsigned char)data[i];
        for (j = 0; j < 8; j++)
            crc = (crc & 0x80) ? (unsigned char)((crc << 1) ^ 0x07) : (unsigned char)(crc << 1);
    }
    return crc;
}

// crc16: 16-bit CRC (CRC-16-IBM, poly=0x8005)
__stdcall int crc16(const char* data, int len) {
    unsigned short crc = 0;
    int i, j;
    for (i = 0; i < len; i++) {
        crc ^= (unsigned short)((unsigned char)data[i] << 8);
        for (j = 0; j < 8; j++)
            crc = (crc & 0x8000) ? (unsigned short)((crc << 1) ^ 0x8005) : (unsigned short)(crc << 1);
    }
    return crc;
}

// crc32: 32-bit CRC (CRC-32, poly=0xEDB88320)
__stdcall int crc32(const char* data, int len) {
    unsigned int crc = 0xFFFFFFFF;
    int i, j;
    for (i = 0; i < len; i++) {
        crc ^= (unsigned char)data[i];
        for (j = 0; j < 8; j++)
            crc = (crc & 1) ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
    }
    return (int)(crc ^ 0xFFFFFFFF);
}

// djb2_hash: 简单字符串哈希 (Bernstein)
__stdcall int djb2_hash(const char* str) {
    unsigned int hash = 5381;
    int c;
    while ((c = *str++))
        hash = ((hash << 5) + hash) + c; // hash * 33 + c
    return (int)hash;
}

// sdbm_hash: 字符串哈希 (SDBM)
__stdcall int sdbm_hash(const char* str) {
    unsigned int hash = 0;
    int c;
    while ((c = *str++))
        hash = c + (hash << 6) + (hash << 16) - hash;
    return (int)hash;
}

// fnv1a_hash: FNV-1a 32-bit 哈希
__stdcall int fnv1a_hash(const char* str) {
    unsigned int hash = 2166136261u;
    int c;
    while ((c = *str++)) {
        hash ^= (unsigned char)c;
        hash *= 16777619u;
    }
    return (int)hash;
}

// checksum8: 简单8位校验和 (所有字节相加)
__stdcall int checksum8(const char* data, int len) {
    unsigned char sum = 0;
    int i;
    for (i = 0; i < len; i++) sum += (unsigned char)data[i];
    return sum;
}

// checksum16: 16位校验和 (每2字节相加)
__stdcall int checksum16(const char* data, int len) {
    unsigned int sum = 0;
    int i;
    for (i = 0; i < len - 1; i += 2)
        sum += ((unsigned char)data[i] << 8) | (unsigned char)data[i + 1];
    if (len & 1) sum += (unsigned char)data[len - 1] << 8;
    while (sum >> 16) sum = (sum & 0xFFFF) + (sum >> 16);
    return (int)(~sum & 0xFFFF);
}
