#!/usr/bin/env python3
"""合并 WayCoder/UI/WEB/www/*.html/css/js 为 WebAssets.Generated.cs。

编辑 www/index.html / style.css / app.js 后重新构建（dotnet build）即可，
无需手改 WebAssets.cs。要求 Python3。

占位符约定：
  - <link rel="stylesheet" href="style.css">  → <style>…css…</style>
  - <script src="app.js"></script>             → <script>…js…</script>
"""
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
WWW = ROOT / "WayCoder" / "UI" / "WEB" / "www"
OUT = ROOT / "WayCoder" / "UI" / "WEB" / "WebAssets.Generated.cs"

LINK = '<link rel="stylesheet" href="style.css">'
SCRIPT = '<script src="app.js"></script>'


def main() -> int:
    index = (WWW / "index.html").read_text(encoding="utf-8")
    css = (WWW / "style.css").read_text(encoding="utf-8")
    js = (WWW / "app.js").read_text(encoding="utf-8")

    if LINK not in index or SCRIPT not in index:
        print(f"错误：index.html 缺少占位符 {LINK} 或 {SCRIPT}", file=sys.stderr)
        return 1

    html = index.replace(LINK, f"<style>\n{css}\n</style>")
    html = html.replace(SCRIPT, f"<script>\n{js}\n</script>")

    # 安全：内容不应含三引号（否则破坏 raw string）
    if '"""' in html:
        print("错误：内容含三引号，无法生成 C# raw string", file=sys.stderr)
        return 1

    generated = (
        "// 由 scripts/merge_webassets.py 生成，勿手改。编辑 www/*.html/css/js 后重新构建。\n"
        "namespace WayCoder.UI.Web;\n\n"
        "internal static partial class WebAssets\n"
        "{\n"
        f'    internal const string Html = """\n{html}\n""";\n'
        "}\n"
    )
    OUT.write_text(generated, encoding="utf-8")
    print(f"已生成 {OUT}（{len(html)} 字符）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
