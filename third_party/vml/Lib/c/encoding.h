// encoding.h — 字符编码库 (简化 API: 解码返回 codepoint, 编码返回 bytes_written, 错误返回负数)

#ifndef ENCODING_H
#define ENCODING_H

#define ENC_ERR_INVALID  -1
#define ENC_ERR_TRUNCATED -2

#define ENC_UTF8     0
#define ENC_UTF16LE  1
#define ENC_UTF16BE  2
#define ENC_ASCII    3
#define ENC_LATIN1   4
#define ENC_GBK      5
#define ENC_BIG5     6

__stdcall int encoding_utf8_decode(unsigned char* src, int len);
__stdcall int encoding_utf8_encode(int codepoint, unsigned char* dst, int max_bytes);
__stdcall int encoding_utf8_strlen(const char* s);
__stdcall int encoding_utf8_seqlen(unsigned char lead_byte);

__stdcall int encoding_ascii_decode(unsigned char* src, int len);
__stdcall int encoding_ascii_isclean(const char* s);

__stdcall int encoding_latin1_decode(unsigned char* src, int len);
__stdcall int encoding_latin1_encode(int codepoint, unsigned char* dst, int max_bytes);

__stdcall int encoding_utf16le_decode(unsigned char* src, int len);
__stdcall int encoding_utf16be_decode(unsigned char* src, int len);

__stdcall int encoding_gbk_decode(unsigned char* src, int len);
__stdcall int encoding_gbk_encode(int codepoint, unsigned char* dst, int max_bytes);

__stdcall int encoding_big5_decode(unsigned char* src, int len);

__stdcall int encoding_detect_bom(unsigned char* src, int len, int* bom_len);
__stdcall int encoding_convert(unsigned char* src, int src_len, unsigned char* dst, int dst_max, int from_enc, int to_enc);
__stdcall const char* encoding_name(int enc);

#endif
