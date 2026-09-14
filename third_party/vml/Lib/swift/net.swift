// VML 网络 Socket 扩展库 — Swift
// OS 模式专用，需显式 import Net

func netCreate(_ domain: Int, _ type: Int) -> Int {
    asm("SYSCALL #330")
    return 0
}
func netBind(_ fd: Int, _ port: Int) -> Int {
    asm("SYSCALL #331")
    return 0
}
func netListen(_ fd: Int, _ backlog: Int) -> Int {
    asm("SYSCALL #332")
    return 0
}
func netAccept(_ fd: Int) -> Int {
    asm("SYSCALL #333")
    return 0
}
func netConnect(_ host: String, _ port: Int) -> Int {
    asm("SYSCALL #334")
    return 0
}
func netSend(_ fd: Int, _ data: String, _ len: Int) -> Int {
    asm("SYSCALL #335")
    return 0
}
func netRecv(_ fd: Int, _ buf: String, _ maxLen: Int) -> Int {
    asm("SYSCALL #336")
    return 0
}
func netClose(_ fd: Int) -> Int {
    asm("SYSCALL #337")
    return 0
}
func dnsResolve(_ hostname: String) -> Int {
    asm("SYSCALL #338")
    return 0
}
