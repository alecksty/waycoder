// VML 全类型转换库 — C++ 包装器 (v1.66.63)
#pragma once

extern "C" {
    __stdcall const char* int_to_str(int val);
    __stdcall int str_to_int(const char* s);
    __stdcall const char* long_to_str(long long val);
    __stdcall long long str_to_long(const char* s);
    __stdcall const char* uint_to_str(unsigned int val);
    __stdcall unsigned int str_to_uint(const char* s);
    __stdcall const char* ulong_to_str(unsigned long long val);
    __stdcall unsigned long long str_to_ulong(const char* s);
    __stdcall const char* float_to_str(float f);
    __stdcall float str_to_float(const char* s);
    __stdcall const char* double_to_str(double d);
    __stdcall double str_to_double(const char* s);
    __stdcall const char* bool_to_str(int b);
    __stdcall int str_to_bool(const char* s);
    __stdcall const char* char_to_str(char c);
    __stdcall char str_to_char(const char* s);
    __stdcall const char* byte_to_str(unsigned char b);
    __stdcall unsigned char str_to_byte(const char* s);
    __stdcall const char* sbyte_to_str(signed char b);
    __stdcall signed char str_to_sbyte(const char* s);
    __stdcall const char* short_to_str(short s);
    __stdcall short str_to_short(const char* s);
    __stdcall const char* ushort_to_str(unsigned short s);
    __stdcall unsigned short str_to_ushort(const char* s);
}
