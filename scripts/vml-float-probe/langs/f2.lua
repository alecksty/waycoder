-- f2.lua —— float/double/int64 运算
-- Lua 只有一种数值类型（`number` = IEEE 双精度）⇒ 这三组运算本质同一套指令，
-- 但值域不同：`3.14` 走小数、`3000000000` 走"双精度里的大整数"（2^53 以内精确）。
print("F-MUL=")
print(3.14 * 2.0 * 100)
print("D-MUL=")
print(3.14 * 2.0 * 100)
print("F-NEG=")
print(-0.5 * 100)
print("D-NEG=")
print(-0.5 * 100)
-- ⚠ 「int64 运算」在 Lua 里只能用**双精度**表达（`number` 就是 IEEE double，
--   没有独立的整数类型）⇒ 大整数在 2^53 以内精确。要把它**变成整数**打出来，
--   得显式 `math.floor`（Lua 的 print 对整数值本来就是不带小数点的 `%.14g` 口径，
--   但除法的结果不是整数 ⇒ 不转换会打出 `-4.294967`）。
print("L-ADD=")
print(math.floor((3000000000 + 1000000000) / 1000000000))
print("L-MUL=")
print(math.floor(((5000000000 / 5) * 2) / 1000000000))
print("L-NEG=")
print(math.floor((0 - 4294967296) / 1000000000))
