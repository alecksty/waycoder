// VML 文件操作扩展库 — Go
package file
func Fopen(name, mode string) int { asm("SYSCALL 110"); return 0 }
func Fclose(handle int) int { asm("SYSCALL 111"); return 0 }
func Fread(handle int, buf []byte, count int) int { asm("SYSCALL 112"); return 0 }
func Fwrite(handle int, buf []byte, count int) int { asm("SYSCALL 113"); return 0 }
func Fseek(handle, offset int) int { asm("SYSCALL 114"); return 0 }
func Ftell(handle int) int { asm("SYSCALL 114"); return 0 }
