// VML FFI 动态库调用扩展库 — C++
// OS 模式专用，需显式 #include "ffi.hpp"

#ifndef FFI_HPP
#define FFI_HPP

namespace vml {
namespace ffi {

inline int dl_open(const char* path)                 { asm("SYSCALL 370"); return 0; }
inline int dl_sym(int handle, const char* name)      { asm("SYSCALL 371"); return 0; }
inline int dl_close(int handle)                      { asm("SYSCALL 372"); return 0; }
inline int native_call(int func_id, const int* args, int count, int flags) { asm("SYSCALL 373"); return 0; }
inline float native_call_f(int func_id, const float* args, int count, int flags) { asm("SYSCALL 375"); return 0.0f; }
inline int get_platform()                            { asm("SYSCALL 374"); return 0; }

} // namespace ffi
} // namespace vml

#endif // FFI_HPP
