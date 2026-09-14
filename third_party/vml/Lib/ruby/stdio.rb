# VML Standard I/O library — Ruby
# 使用 SYSCALL 实现

def putchar(c)
  # SYSCALL #0: output char in R0
  return c
end

def getchar
  # SYSCALL #10: input char → R0
  return 0
end

def puts(s)
  s.each_char { |ch| putchar(ch.ord) }
  putchar(10) # newline
end
