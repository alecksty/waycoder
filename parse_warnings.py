import re

warnings = {}
with open('D:/code-agents/WayCoder/warnings.txt', encoding='utf-8') as f:
    for line in f:
        m = re.match(r'(.+?)\((\d+),\d+\): warning (CS\d+|IL\d+): (.+)', line)
        if m:
            key = (m.group(1), m.group(3), int(m.group(2)))
            if key not in warnings:
                warnings[key] = m.group(4).strip()

by_type = {}
prefix = 'D:\\code-agents\\WayCoder\\WayCoder\\'
for (file, code, line_num), msg in sorted(warnings.items()):
    short = file.replace(prefix, '')
    by_type.setdefault(code, []).append((short, line_num, msg))

for code in sorted(by_type):
    items = by_type[code]
    print(f'=== {code} ({len(items)} unique) ===')
    for f, l, msg in sorted(items):
        print(f'  {f}:{l}  {msg}')
