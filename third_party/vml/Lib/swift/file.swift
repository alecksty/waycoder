// VML 文件操作扩展库 — Swift
func fopen(_ name: String, _ mode: String) -> Int { asm("SYSCALL 110"); return 0 }
func fclose(_ handle: Int) -> Int { asm("SYSCALL 111"); return 0 }
func fread(_ handle: Int, _ buf: String, _ count: Int) -> Int { asm("SYSCALL 112"); return 0 }
func fwrite(_ handle: Int, _ buf: String, _ count: Int) -> Int { asm("SYSCALL 113"); return 0 }
func fseek(_ handle: Int, _ offset: Int) -> Int { asm("SYSCALL 114"); return 0 }
func ftell(_ handle: Int) -> Int { asm("SYSCALL 114"); return 0 }
