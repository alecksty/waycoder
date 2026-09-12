using System.Text;

namespace WayCoder.UI.Shared.Terminal;

    /// <summary>
    /// 一个 rows×cols 的文本网格，逐帧 Apply 增量 ANSI（CursorPos/SGR/ClearScreen）后保持累积状态，
    /// 避免增量渲染只输出 delta 时抓不到完整画面。SGR 颜色被剥离，仅保留字符与位置。
    /// </summary>
    public sealed class FrameBuffer
    {
        readonly int _rows, _cols;
        readonly string[][] _cell;
        readonly bool[][] _cont;
        readonly int[][] _fg, _bg;   // 每个格子的前景/背景 ANSI 色码（0=默认）
        int _curR, _curC;
        int _savedR, _savedC;        // 保存/恢复光标（ESC [ s / ESC [ u）
        int _curFg, _curBg;          // 当前 SGR 状态（用于颜色采集）

        public FrameBuffer(int rows, int cols)
        {
            // 尺寸保底**必须收口在这里**，不能留给调用方：`Apply` 里的 CUP/擦除都走
            // Math.Clamp(x-1, 0, _cols-1)，而 Math.Clamp 在 min > max 时抛 ArgumentException；
            // rows<0 更早一步在 new string[rows][] 就抛 OverflowException。三条调用路径
            // （TuiAudit.AnsiToGrid、SelfTest.Chunk12、SelfTest.Chunk8）都无 try/catch，
            // 一旦抛出就是整个 --test / --tui-audit 中断，而不是报一条 ❌。
            // 此前 TuiAudit 那份手写解析自己带了这两行保底，改用它之后保底随之丢失（code-review #1）。
            rows = Math.Max(rows, 1);   // 钳制参数本身：下面的数组分配用的是它，只改字段不够
            cols = Math.Max(cols, 1);
            _rows = rows;
            _cols = cols;
            _cell = new string[rows][];
            _cont = new bool[rows][];
            _fg = new int[rows][];
            _bg = new int[rows][];
            for (int r = 0; r < rows; r++)
            {
                _cell[r] = new string[cols];
                _cont[r] = new bool[cols];
                _fg[r] = new int[cols];
                _bg[r] = new int[cols];
                for (int c = 0; c < cols; c++) _cell[r][c] = " ";
            }
        }

        public void Apply(string ansi)
        {
            int i = 0, len = ansi.Length;
            while (i < len)
            {
                char ch = ansi[i];
                if (ch == AnsiString.AnsiCharPrefix && i + 1 < len && ansi[i + 1] == '[')
                {
                    int j = i + 2;
                    while (j < len && !(ansi[j] >= '@' && ansi[j] <= '~')) j++;
                    if (j >= len) break;
                    char final = ansi[j];
                    string param = ansi.Substring(i + 2, j - (i + 2));
                    i = j + 1;

                    // 私有模式（?25h 光标显隐 / ?1049h 备用屏）与扩展协议（>q 等）不影响字符网格
                    if (param.Length > 0 && (param[0] == '?' || param[0] == '>')) continue;

                    var p = param.Split(';');
                    int n1 = 1;
                    if (p.Length >= 1 && p[0].Length > 0 && int.TryParse(p[0], out var v1)) n1 = Math.Max(1, v1);

                    switch (final)
                    {
                        case 'H': case 'f': // CUP / HVP：行;列，1-based
                            {
                                int row = 1, col = 1;
                                if (p.Length >= 1 && p[0].Length > 0 && int.TryParse(p[0], out var rr)) row = Math.Max(1, rr);
                                if (p.Length >= 2 && p[1].Length > 0 && int.TryParse(p[1], out var cc)) col = Math.Max(1, cc);
                                _curR = Math.Clamp(row - 1, 0, _rows - 1);
                                _curC = Math.Clamp(col - 1, 0, _cols - 1);
                            }
                            break;
                        case 'A': _curR = Math.Max(0, _curR - n1); break;             // CUU 光标上移
                        case 'B': _curR = Math.Min(_rows - 1, _curR + n1); break;     // CUD 光标下移
                        case 'C': _curC = Math.Min(_cols - 1, _curC + n1); break;     // CUF 光标右移
                        case 'D': _curC = Math.Max(0, _curC - n1); break;             // CUB 光标左移
                        case 'G': _curC = Math.Clamp(n1 - 1, 0, _cols - 1); break;    // CHA 列绝对定位
                        case 'd': _curR = Math.Clamp(n1 - 1, 0, _rows - 1); break;    // VPA 行绝对定位
                        case 's': _savedR = _curR; _savedC = _curC; break;            // 保存光标
                        case 'u': _curR = _savedR; _curC = _savedC; break;            // 恢复光标
                        case 'K':                                                   // EL 行内清除
                            {
                                int mode = p[0].Length > 0 && int.TryParse(p[0], out var km) ? km : 0;
                                EraseLine(mode);
                            }
                            break;
                        case 'J':                                                   // ED 屏幕清除
                            {
                                int mode = p[0].Length > 0 && int.TryParse(p[0], out var em) ? em : 0;
                                EraseDisplay(mode);
                            }
                            break;
                        case 'm': ApplySgr(param); break;                           // SGR
                        // 其余（h/l 私有模式、@/L/M/P 插入删除等）——对话框不使用，忽略
                    }
                    continue;
                }

                if (ch == '\r') { _curC = 0; i++; continue; }
                if (ch == '\n') { _curR++; _curC = 0; i++; continue; }
                if (ch == '\t') { _curC += 4 - (_curC % 4); i++; continue; }

                var rune = Rune.GetRuneAt(ansi, i);
                int w = AnsiString.CharWidth(rune);
                string s = rune.ToString();
                i += rune.Utf16SequenceLength;

                if (w == 0) continue; // 零宽字符（组合标记/变体选择器）不占列

                if (_curR >= 0 && _curR < _rows && _curC >= 0 && _curC < _cols)
                {
                    // 落点若是**前一个宽字符的延续格** → 那个宽字符被打断。真实终端（xterm）会把它
                    // 的首格清成空格，否则会留下「半个汉字 + 新字符」挤在 2 列跨度里（code-review #5：
                    // `\x1b[1;1H中\x1b[1;2HX` 此前得到 "中X"，而真终端给 " X"）。
                    if (_cont[_curR][_curC] && _curC > 0)
                    {
                        _cell[_curR][_curC - 1] = " ";
                        _fg[_curR][_curC - 1] = 0;
                        _bg[_curR][_curC - 1] = 0;
                    }
                    _cell[_curR][_curC] = s;
                    _fg[_curR][_curC] = _curFg;
                    _bg[_curR][_curC] = _curBg;
                    _cont[_curR][_curC] = false;   // 写新字符时清除本格的宽字符延续标记
                    if (w == 2 && _curC + 1 < _cols) _cont[_curR][_curC + 1] = true;
                }
                _curC += w;
            }
        }

        void ClearAll()
        {
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                {
                    _cell[r][c] = " "; _cont[r][c] = false;
                    _fg[r][c] = 0; _bg[r][c] = 0;
                }
            _curR = 0; _curC = 0;
        }

        /// <summary>EL：清除当前行（mode 0=光标到行尾，1=行首到光标，2=整行）。</summary>
        void EraseLine(int mode)
        {
            if (_curR < 0 || _curR >= _rows) return;
            // 列界必须在这里钳死：写字符的路径 `_curC += w` **不**钳到 _cols-1，所以满行写满后
            // _curC == _cols（甚至因宽字符越过末列）。此时 EL1（`\x1b[1K`）/ ED1 会把 to==_cols，
            // 下面的 _cell[_curR][_cols] 直接越界抛 IndexOutOfRangeException —— 而 TuiAudit
            // 改接本模拟器后，K/J 从「一律忽略」变成「走这条路径」，于是这条潜在崩溃变得可达。
            // 钳 to/from 是最小修法：不动 `_curC += w` 的推进语义（那会影响宽字符在末列的落位）。
            int last = _cols - 1;
            int from = 0, to = last;
            if (mode == 0) from = Math.Clamp(_curC, 0, last);
            else if (mode == 1) to = Math.Clamp(_curC, 0, last);
            for (int c = from; c <= to; c++)
            {
                _cell[_curR][c] = " "; _cont[_curR][c] = false;
                _fg[_curR][c] = 0; _bg[_curR][c] = 0;
            }
        }

        /// <summary>ED：清除屏幕（mode 0=光标到末尾，1=开头到光标，2=整屏）。</summary>
        void EraseDisplay(int mode)
        {
            if (mode == 2) { ClearAll(); return; }
            if (mode == 0)
            {
                EraseLine(0);
                for (int r = _curR + 1; r < _rows; r++)
                    for (int c = 0; c < _cols; c++) { _cell[r][c] = " "; _cont[r][c] = false; _fg[r][c] = 0; _bg[r][c] = 0; }
            }
            else if (mode == 1)
            {
                for (int r = 0; r < _curR; r++)
                    for (int c = 0; c < _cols; c++) { _cell[r][c] = " "; _cont[r][c] = false; _fg[r][c] = 0; _bg[r][c] = 0; }
                EraseLine(1);
            }
        }

        /// <summary>解析 SGR 参数串（如 "37;47" / "0" / "38;5;N" / "38;2;R;G;B"），更新当前 fg/bg。</summary>
        void ApplySgr(string param)
        {
            if (string.IsNullOrEmpty(param)) param = "0";
            var parts = param.Split(';');
            for (int k = 0; k < parts.Length; k++)
            {
                if (!int.TryParse(parts[k], out var code)) continue;
                if (code == 0) { _curFg = 0; _curBg = 0; }
                else if (code >= 30 && code <= 37) _curFg = code;
                else if (code >= 90 && code <= 97) _curFg = code;
                else if (code >= 40 && code <= 47) _curBg = code;
                else if (code >= 100 && code <= 107) _curBg = code;
                else if (code == 39) _curFg = 0;
                else if (code == 49) _curBg = 0;
                else if (code == 38 || code == 48)
                {
                    // 38;5;N / 48;5;N / 38;2;R;G;B —— 跳过后续参数，不做精确记录
                    if (k + 1 < parts.Length && parts[k + 1] == "5") k += 2;
                    else if (k + 1 < parts.Length && parts[k + 1] == "2") k += 4;
                }
                // 其余（1 粗体 / 2 淡化 / 22 取消粗体 等）不参与 fg/bg 采集
            }
        }

        public List<string> Dump()
        {
            var lines = new List<string>();
            for (int r = 0; r < _rows; r++)
            {
                var sb = new StringBuilder();
                for (int c = 0; c < _cols; c++)
                {
                    if (_cont[r][c]) continue; // 宽字符延续格
                    sb.Append(_cell[r][c]);
                }
                lines.Add(sb.ToString().TrimEnd());
            }
            while (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
            return lines;
        }

        /// <summary>
        /// 带颜色的帧转储：按行重新合成最小化 SGR 序列，真实终端可直接渲染出颜色。
        /// 用于「肉眼看到颜色」——排查白底白字/黄底黄字等前景背景同色问题。
        /// </summary>
        public List<string> DumpAnsi()
        {
            var lines = new List<string>();
            for (int r = 0; r < _rows; r++)
            {
                var sb = new StringBuilder();
                int lastFg = -1, lastBg = -1;
                for (int c = 0; c < _cols; c++)
                {
                    if (_cont[r][c]) continue; // 宽字符延续格
                    int fg = _fg[r][c], bg = _bg[r][c];
                    if (fg != lastFg || bg != lastBg)
                    {
                        sb.Append(AnsiString.AnsiCharPrefix + "[0m");
                        if (fg > 0) sb.Append($"{AnsiString.AnsiCharPrefix}[{fg}m");
                        if (bg > 0) sb.Append($"{AnsiString.AnsiCharPrefix}[{bg}m");
                        lastFg = fg; lastBg = bg;
                    }
                    sb.Append(_cell[r][c]);
                }
                lines.Add(sb.ToString().TrimEnd());
            }
            while (lines.Count > 0 && lines[^1].TrimEnd(AnsiString.AnsiCharPrefix).Length == 0) lines.RemoveAt(lines.Count - 1);
            return lines;
        }

        /// <summary>把标准 ANSI 色码映射为可读名称（供颜色图例/诊断输出）。</summary>
        static string ColorName(int code)
        {
            if (code == 0) return "默认";
            return code switch
            {
                30 => "黑字", 31 => "红字", 32 => "绿字", 33 => "黄字", 34 => "蓝字", 35 => "紫字", 36 => "青字", 37 => "白字",
                40 => "黑底", 41 => "红底", 42 => "绿底", 43 => "黄底", 44 => "蓝底", 45 => "紫底", 46 => "青底", 47 => "白底",
                90 => "亮黑字", 91 => "亮红字", 92 => "亮绿字", 93 => "亮黄字", 94 => "亮蓝字", 95 => "亮紫字", 96 => "亮青字", 97 => "亮白字",
                100 => "亮黑底", 101 => "亮红底", 102 => "亮绿底", 103 => "亮黄底", 104 => "亮蓝底", 105 => "亮紫底", 106 => "亮青底", 107 => "亮白底",
                _ => $"#{code}",
            };
        }

        // ── 截屏（SHOT 命令）：渲染字符网格为 PNG ──

        /// <summary>把当前帧渲染为 PNG 保存。用 TrueTypeFont 矢量渲染（CJK 可读），
        /// 背景按 _bg 填色、字符按 _fg 前景色绘制。</summary>
        public void RenderPng(string path)
        {
            const int cellW = 11, cellH = 22; // 等宽格子（字号 ≈ cellH）
            var font = TrueTypeFont.Resolve(null)
                ?? TrueTypeFont.Load(FontFinder.Find().FirstOrDefault()?.Path ?? "");
            if (font == null)
                throw new InvalidOperationException("未找到可用系统字体");

            var canvas = new Canvas(_cols * cellW, _rows * cellH, 0xFF0B0B0B);
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    if (_cont[r][c]) continue; // 宽字符延续格跳过
                    int bg = _bg[r][c];
                    if (bg > 0)
                        canvas.FillRect(c * cellW, r * cellH, cellW, cellH, AnsiToRgba(bg));
                    string ch = _cell[r][c];
                    if (ch.Length == 0 || ch == " " || ch == "\0") continue;
                    int fg = _fg[r][c];
                    font.Render(canvas, ch, c * cellW, r * cellH, cellH, AnsiToRgba(fg), "start", false, false);
                }
            }
            File.WriteAllBytes(path, canvas.ToPng());
        }

        /// <summary>ANSI 色码 → RGBA。支持标准 16 色 / 256 色（48;5 已并入 _bg 前剥离？此处覆盖标准色；256 码尽量映射）。</summary>
        static uint AnsiToRgba(int code)
        {
            if (code == 0) return 0xFFBBBBBB; // 默认前景
            if (code >= 0x1000000) return (uint)(code & 0xFFFFFF) | 0xFF000000; // TrueColor
            uint rgb = code switch
            {
                30 or 40 => 0x1E1E1E, 31 or 41 => 0xAA2E2E, 32 or 42 => 0x2E7D32, 33 or 43 => 0xB07A1E,
                34 or 44 => 0x2E5FAA, 35 or 45 => 0x8E3AA8, 36 or 46 => 0x2E8E8E, 37 or 47 => 0xC8C8C8,
                90 or 100 => 0x555555, 91 or 101 => 0xE05A5A, 92 or 102 => 0x5EB26E, 93 or 103 => 0xE0B84A,
                94 or 104 => 0x6E8FE0, 95 or 105 => 0xC06EC8, 96 or 106 => 0x6EC8C8, 97 or 107 => 0xFFFFFF,
                _ => Xterm256(code),
            };
            return rgb | 0xFF000000;
        }

        /// <summary>xterm 256 调色板映射（0-15 标准色，16-231 立方体，232-255 灰度）。</summary>
        static uint Xterm256(int code)
        {
            if (code is >= 0 and <= 15)
                return AnsiToRgba(code is >= 8 ? code - 8 + 90 : code is >= 30 ? code - 30 + 40 : code);
            if (code is >= 16 and <= 231)
            {
                int v = code - 16;
                int r = v / 36, g = (v / 6) % 6, b = v % 6;
                uint Comp(int x) => x == 0 ? 0u : (uint)(55 + x * 40);
                return (Comp(r) << 16) | (Comp(g) << 8) | Comp(b);
            }
            int gray = code - 232;
            uint gv = (uint)(8 + gray * 10);
            return (gv << 16) | (gv << 8) | gv;
        }
}