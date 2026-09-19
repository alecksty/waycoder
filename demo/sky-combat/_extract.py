#!/usr/bin/env python3
# 从 headless Chrome 的 --dump-dom 输出里取出 <pre> 文本与标题（避免在 shell 里写正则被转义坑）
import sys, re, html

d = sys.stdin.read()
t = re.search(r"<title>(.*?)</title>", d, re.S)
print("TITLE:", t.group(1).strip() if t else "?")
m = re.search(r"<pre[^>]*>(.*?)</pre>", d, re.S)
print(html.unescape(m.group(1)) if m else "<<无输出>>")
