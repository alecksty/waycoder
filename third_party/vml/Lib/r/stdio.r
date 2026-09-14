# VML Standard I/O library — R
# 使用内联 VML 汇编 SYSCALL

putchar <- function(c) {
  asm("SYSCALL 0")
  return(c)
}

getchar <- function() {
  asm("SYSCALL 10")
  return(0)
}

puts <- function(s) {
  for (i in 1:nchar(s)) {
    ch <- substr(s, i, i)
    putchar(utf8ToInt(ch))
  }
  putchar(10)
  return(0)
}
