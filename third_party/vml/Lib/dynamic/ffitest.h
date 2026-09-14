/* ffitest.h — FFI parameter test functions */
extern void ffi_test_int(int a);
extern void ffi_test_int2(int a, int b);
extern void ffi_test_int3(int a, int b, int c);
extern void ffi_test_float(int f_bits);
extern void ffi_test_int_float(int a, int f_bits);
extern void ffi_test_string(const char* s);
extern void ffi_test_int_string(int a, const char* s);
extern int ffi_test_strlen(const char* s);
