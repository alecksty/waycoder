/* conv.h — VML 全类型转换库 C 头文件 (v1.66.44)
 * 所有 C/C++/ObjC 程序通过 #include 使用 */

#ifndef VML_CONV_H
#define VML_CONV_H

/* --- 整数 → 字符串 --- */
__stdcall const char*  int_to_str(int val);
__stdcall const wchar_t* int_to_wstr(int val);
__stdcall const char32_t* int_to_ustr(int val);
__stdcall int str_to_int(const char* s);
__stdcall int wstr_to_int(const wchar_t* s);
__stdcall int ustr_to_int(const char32_t* s);

/* --- int64 → 字符串 --- */
__stdcall const char*  long_to_str(long long val);
__stdcall const wchar_t* long_to_wstr(long long val);
__stdcall const char32_t* long_to_ustr(long long val);
__stdcall long long str_to_long(const char* s);
__stdcall long long wstr_to_long(const wchar_t* s);
__stdcall long long ustr_to_long(const char32_t* s);

/* --- 无符号 --- */
__stdcall const char* uint_to_str(unsigned int val);
__stdcall unsigned int str_to_uint(const char* s);
__stdcall const char* ulong_to_str(unsigned long long val);
__stdcall unsigned long long str_to_ulong(const char* s);

/* --- 布尔 --- */
__stdcall const char* bool_to_str(int b);
__stdcall int str_to_bool(const char* s);

/* --- 字符 --- */
__stdcall const char* char_to_str(char c);
__stdcall char str_to_char(const char* s);

/* --- 8-bit --- */
__stdcall const char* byte_to_str(unsigned char b);
__stdcall unsigned char str_to_byte(const char* s);
__stdcall const char* sbyte_to_str(signed char b);
__stdcall signed char str_to_sbyte(const char* s);

/* --- 16-bit --- */
__stdcall const char* short_to_str(short s);
__stdcall short str_to_short(const char* s);
__stdcall const char* ushort_to_str(unsigned short s);
__stdcall unsigned short str_to_ushort(const char* s);

/* --- 浮点 --- */
__stdcall const char*  float_to_str(float f);
__stdcall float str_to_float(const char* s);
__stdcall const char*  double_to_str(double d);
__stdcall double str_to_double(const char* s);

/* --- 宽字符 / Unicode 包装器 --- */
__stdcall const wchar_t* float_to_wstr(float f);
__stdcall const wchar_t* double_to_wstr(double d);
__stdcall const wchar_t* bool_to_wstr(int b);
__stdcall const char32_t* float_to_ustr(float f);
__stdcall const char32_t* double_to_ustr(double d);
__stdcall const char32_t* bool_to_ustr(int b);

#endif /* VML_CONV_H */
