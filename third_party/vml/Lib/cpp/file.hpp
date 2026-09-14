// VML 文件操作扩展库 — C++
#pragma once
namespace vml::file {
inline int fopen(const char* name, const char* mode)    { asm("SYSCALL 110"); return 0; }
inline int fclose(int h)                                { asm("SYSCALL 111"); return 0; }
inline int fread(int h, void* buf, int n)               { asm("SYSCALL 112"); return 0; }
inline int fwrite(int h, const void* buf, int n)        { asm("SYSCALL 113"); return 0; }
inline int fseek(int h, int off)                        { asm("SYSCALL 114"); return 0; }
inline int ftell(int h)                                 { asm("SYSCALL 114"); return 0; }
}
