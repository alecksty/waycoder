"""粗粒度括号平衡检查 —— 构建一次 4 分钟，先用它抓明显的括号失衡。

不追求完美：先剥掉行注释、块注释、字符串字面量，再数花括号/圆括号。
字符串剥除用逐字符扫描（比正则可靠，能处理转义与逐字字符串）。
"""
import sys

path = sys.argv[1]
src = open(path, encoding='utf-8').read()

out = []
i, n = 0, len(src)
state = None          # None / 'line' / 'block' / 'str' / 'char' / 'verb'
while i < n:
    c = src[i]
    nxt = src[i + 1] if i + 1 < n else ''
    if state is None:
        if c == '/' and nxt == '/':
            state = 'line'; i += 2; continue
        if c == '/' and nxt == '*':
            state = 'block'; i += 2; continue
        if c == '@' and nxt == '"':          # 逐字字符串 @"..."
            state = 'verb'; i += 2; continue
        if c == '"':
            state = 'str'; i += 1; continue
        if c == "'":
            state = 'char'; i += 1; continue
        out.append(c); i += 1; continue
    if state == 'line':
        if c == '\n':
            state = None; out.append(c)
        i += 1; continue
    if state == 'block':
        if c == '*' and nxt == '/':
            state = None; i += 2; continue
        i += 1; continue
    if state == 'verb':
        if c == '"' and nxt == '"':          # "" 是转义的双引号
            i += 2; continue
        if c == '"':
            state = None; i += 1; continue
        i += 1; continue
    if state in ('str', 'char'):
        if c == '\\':
            i += 2; continue
        if (state == 'str' and c == '"') or (state == 'char' and c == "'"):
            state = None
        i += 1; continue

code = ''.join(out)
pairs = {'{': '}', '(': ')', '[': ']'}
for op, cl in pairs.items():
    a, b = code.count(op), code.count(cl)
    flag = 'BALANCED' if a == b else '*** MISMATCH ***'
    print(f'{op}{cl}: {a} vs {b}  {flag}')

# 逐层扫描，报告第一个「提前闭合」的位置（最有用的一条线索）
depth = 0
line = 1
for ch in code:
    if ch == '\n':
        line += 1
    elif ch in '{([':
        depth += 1
    elif ch in '})]':
        depth -= 1
        if depth < 0:
            print(f'*** 第 {line} 行出现多余的闭合括号（提前归零）')
            break
print('结束深度:', depth, '(0 = 正常)' if depth == 0 else '*** 有未闭合的块 ***')
