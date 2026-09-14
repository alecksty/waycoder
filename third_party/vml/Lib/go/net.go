// VML 网络 Socket 扩展库 — Go
// OS 模式专用，需显式 import "net"

package net

func Create(domain, typ int) int {
    asm("SYSCALL 330")
    return 0
}
func Bind(fd, port int) int {
    asm("SYSCALL 331")
    return 0
}
func Listen(fd, backlog int) int {
    asm("SYSCALL 332")
    return 0
}
func Accept(fd int) int {
    asm("SYSCALL 333")
    return 0
}
func Connect(host string, port int) int {
    asm("SYSCALL 334")
    return 0
}
func Send(fd int, data []byte, length int) int {
    asm("SYSCALL 335")
    return 0
}
func Recv(fd int, buf []byte, maxLen int) int {
    asm("SYSCALL 336")
    return 0
}
func Close(fd int) int {
    asm("SYSCALL 337")
    return 0
}
func DnsResolve(hostname string) int {
    asm("SYSCALL 338")
    return 0
}
