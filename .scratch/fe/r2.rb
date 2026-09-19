def f0
  puts("no-arg")
end

def f1(a)
  puts("one-arg")
  puts(a)
end

def f2(a, b)
  puts("two-arg")
  puts(a + b)
end

f0()
f1(7)
f2(2, 3)
s = "VAR"
puts(s)
f1(s)
f1("LIT")
