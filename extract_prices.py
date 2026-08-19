#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""从 OpenRouter /models 导出中提取内置 ModelCatalog 各模型的最新价格（$/1M tokens）。"""
import json, sys

with open(r"D:\code-agents\WayCoder\openrouter_models.json", encoding="utf-8") as f:
    data = json.load(f)

# id -> (input$/1M, output$/1M)
prices = {}
for m in data.get("data", []):
    mid = m.get("id", "")
    p = m.get("pricing", {}) or {}
    inp = p.get("prompt")
    out = p.get("completion")
    if inp is None or out is None:
        continue
    prices[mid] = (float(inp) * 1e6, float(out) * 1e6)

# 内置目录模型 id 与 OpenRouter 的候选匹配：直接 id / 常见 prefix
def find_candidates(pid):
    cands = []
    lower = pid.lower()
    # 精确 id 或 以 provider/model 形式
    for mid in prices:
        ml = mid.lower()
        if ml == lower or ml.endswith("/" + lower):
            cands.append(mid)
    return cands

builtin = [
    # (内置Id, 注释/OpenRouter可能名)
    ("gpt-5.5","gpt-5.5"),("gpt-5.4","gpt-5.4"),("gpt-5.4-mini","gpt-5.4-mini"),
    ("gpt-5.4-nano","gpt-5.4-nano"),("o4-mini","o4-mini"),("gpt-4.1","gpt-4.1"),
    ("gpt-4.1-mini","gpt-4.1-mini"),("gpt-4.1-nano","gpt-4.1-nano"),
    ("gpt-4o","gpt-4o"),("gpt-4o-mini","gpt-4o-mini"),
    ("claude-opus-5","claude-opus-5"),("claude-sonnet-5","claude-sonnet-5"),
    ("claude-haiku-4-5","claude-haiku-4.5"),("claude-opus-4-6","claude-opus-4.6"),
    ("claude-sonnet-4-6","claude-sonnet-4.6"),
    ("deepseek-v4-pro","deepseek-v4-pro"),("deepseek-v4-flash","deepseek-v4-flash"),
    ("deepseek-chat","deepseek-chat"),("deepseek-reasoner","deepseek-reasoner"),
    ("gemini-2.5-pro","gemini-2.5-pro"),("gemini-2.5-flash","gemini-2.5-flash"),
    ("gemini-2.0-flash","gemini-2.0-flash"),
    ("qwen3-max","qwen3-max"),("qwen3-plus","qwen3-plus"),
    ("qwen-max","qwen-max"),("qwen-plus","qwen-plus"),("qwen-turbo","qwen-turbo"),
    ("kimi-k2.5","kimi-k2.5"),
    ("glm-4-plus","glm-4-plus"),("glm-4-flash","glm-4-flash"),
    ("doubao-pro-1.5","doubao-pro-1.5"),("doubao-lite-1.5","doubao-lite-1.5"),
    ("yi-large","yi-large"),
    ("grok-3","grok-3"),
    ("mistral-large","mistral-large"),("mistral-small","mistral-small"),
    ("codestral","codestral"),
]

print("=== 内置模型 -> OpenRouter 价格 ($/1M, in/out) ===")
for bid, key in builtin:
    cands = find_candidates(key)
    if not cands:
        # 再试模糊：搜索 id 包含 key 或 name 包含
        cands = [mid for mid in prices if key.lower() in mid.lower()]
    if not cands:
        print(f"{bid:22s}  <未找到>")
        continue
    # 取第一个
    mid = cands[0]
    inp, out = prices[mid]
    print(f"{bid:22s} <- {mid:45s} {inp:.4f} / {out:.4f}")
