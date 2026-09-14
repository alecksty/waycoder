# VML Console library — R

console_clear <- function() {
  asm("SYSCALL 5")
  return(0)
}

console_gotoxy <- function(x, y) {
  asm("SYSCALL 6")
  return(0)
}

console_textcolor <- function(fg, bg) {
  asm("SYSCALL 7")
  return(0)
}
