# 栈漂移探针（Ruby）：`**` 被编成 CALL ipow，那条路径「压了必须由调用方清」。
# 判据：`DRIFT=126`（2^1+…+2^6）。2026-09-17 修复前实测 65528。
s = 0
for i in 1..6
  s = s + (2 ** i)
end
print("DRIFT=")
puts(s)
