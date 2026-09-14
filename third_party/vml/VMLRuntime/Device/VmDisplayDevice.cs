using System;

namespace VMLRuntime.Device
{
    /// <summary>
    /// VGA显示设备 - 提供图形显示功能
    /// VGA 内存布局: 0xA0000-0xBFFFF (128KB)
    ///   0xA0000-0xAFFFF: 图形帧缓冲 (64KB, offset 0)
    ///   0xB8000-0xBFFFF: 文本模式帧缓冲 (32KB, offset 0x18000)
    /// </summary>
    public class VmDisplayDevice : IDevice, IMmioDevice
    {
        #region 属性
        private const uint VGA_BASE = 0xA0000;
        private const uint VGA_SIZE = 0x50000; // 320KB — 足够 640×480×1bpp=300KB + 文本缓冲区 4KB
        // MMIO 范围: 0xA0000-0xEFFFF, 0xF0000-0xFFFFF 留给 BIOS ROM 和 8x16 字库
        private const uint TEXT_MMIO_OFFSET = 0x18000; // VGA address 0xB8000 → MMIO offset
        private const uint TEXT_STORAGE = 0x18000;      // VGA text buffer at offset 0xB8000-0xA0000
        private const uint TEXT_SIZE = 0xFA0;            // 4000 bytes — exact text buffer (80 cols × 25 rows × 2 bytes)

        private uint MapOffset(uint offset) => offset;

        private EDeviceStatus _status;
        private byte[]        _memory;
        private int           _width;
        private int           _height;
        private int           _mode;

        /// <summary>MMIO 起始地址（VGA framebuffer 基址 0xA0000）</summary>
        public uint       MmioStart  => VGA_BASE;
        public uint       MmioSize   => VGA_SIZE;
        public MmioAccess MmioAccess => MmioAccess.ReadWrite;

        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name => "vga";

        /// <summary>
        /// 设备类型
        /// </summary>
        public EDeviceType Type => EDeviceType.Display;

        /// <summary>
        /// 设备状态
        /// </summary>
        public EDeviceStatus Status => _status;

        /// <summary>
        /// VGA内存
        /// </summary>
        public byte[] Memory => _memory;

        /// <summary>
        /// 显示宽度
        /// </summary>
        public int Width => _width;

        /// <summary>
        /// 显示高度
        /// </summary>
        public int Height => _height;

        /// <summary>
        /// 显示模式
        /// </summary>
        public int Mode => _mode;
        #endregion

        #region 基本方法
        private int _bytesPerCell;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="width">显示宽度</param>
        /// <param name="height">显示高度</param>
        /// <param name="mode">显示模式 (0=字符模式, 1=图形模式, 2=索引模式)</param>
        /// <param name="bytesPerCell">字符模式下每字符字节数（PC=2, Apple II/C64=1），默认2</param>
        public VmDisplayDevice(int width = 80, int height = 25, int mode = 0, int bytesPerCell = 2)
        {
            _width        = width;
            _height       = height;
            _mode         = mode;
            _bytesPerCell = bytesPerCell;
            _status       = EDeviceStatus.Closed;
            _memory       = new byte[VGA_SIZE];
        }

        public ulong ReadMmio(uint offset, int size)
        {
            offset = MapOffset(offset);
            if (offset + size > VGA_SIZE)
                throw new VmlMemoryException($"VGA MMIO 越界: offset=0x{offset:X8} size={size}", VGA_BASE + offset);
            return size switch
            {
                1 => _memory[offset],
                2 => (ushort)(_memory[offset] | (_memory[offset + 1] << 8)),
                4 => (ulong)(_memory[offset]  | (_memory[offset + 1] << 8) | (_memory[offset + 2] << 16) | (_memory[offset + 3] << 24)),
                _ => throw new VmlException($"VGA MMIO 不支持的位宽: {size}")
            };
        }

        public void WriteMmio(uint offset, ulong value, int size)
        {
            offset = MapOffset(offset);
            if (offset + size > VGA_SIZE)
                throw new VmlMemoryException($"VGA MMIO 越界: offset=0x{offset:X8} size={size}", VGA_BASE + offset);
            switch (size)
            {
                case 1:
                    _memory[offset] = (byte)value;
                    break;
                case 2:
                    _memory[offset]     = (byte)(value        & 0xFF);
                    _memory[offset + 1] = (byte)((value >> 8) & 0xFF);
                    break;
                case 4:
                    _memory[offset]     = (byte)(value         & 0xFF);
                    _memory[offset + 1] = (byte)((value >> 8)  & 0xFF);
                    _memory[offset + 2] = (byte)((value >> 16) & 0xFF);
                    _memory[offset + 3] = (byte)((value >> 24) & 0xFF);
                    break;
                default:
                    throw new VmlException($"VGA MMIO 不支持的位宽: {size}");
            }
        }

        /// <summary>获取当前模式 framebuffer 副本（使用设备初始模式）</summary>
        public byte[] GetFramebuffer() => GetFramebuffer(_mode, _width, _height, _bytesPerCell);

        /// <summary>获取指定模式 framebuffer 副本（用于运行时模式切换后的渲染）</summary>
        /// <param name="mode">0=文本, 1=RGB, 2=索引色(SCREEN 13)</param>
        /// <param name="width">显示宽度</param>
        /// <param name="height">显示高度</param>
        /// <param name="bytesPerCellOrBpp">文本模式=每字符字节数, 索引模式=1, RGB模式=3</param>
        public byte[] GetFramebuffer(int mode, int width, int height, int bytesPerCellOrBpp)
        {
            int offset, size;
            if (mode == 0)
            {
                offset = (int)TEXT_STORAGE;
                size   = width * height * bytesPerCellOrBpp;
            }
            else if (mode == 2)
            {
                offset = 0;
                size   = width * height; // bpp=1, each pixel = 1 byte
            }
            else
            {
                offset = 0;
                size   = width * height * 3;
            }
            if (offset + size > _memory.Length) size = _memory.Length - offset;
            var buf = new byte[size];
            Array.Copy(_memory, offset, buf, 0, size);
            return buf;
        }

        /// <summary>
        /// 打开设备
        /// </summary>
        /// <returns>是否成功</returns>
        public bool Open()
        {
            _status = EDeviceStatus.Open;
            Clear();
            return true;
        }

        /// <summary>
        /// 关闭设备
        /// </summary>
        public void Close()
        {
            _status = EDeviceStatus.Closed;
        }

        /// <summary>
        /// 读取数据（VGA显示设备通常不支持读取）
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">要读取的字节数</param>
        /// <returns>实际读取的字节数</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (_status != EDeviceStatus.Open)
                return -1;

            try
            {
                int fbOffset = _mode == 0 ? (int)TEXT_STORAGE : 0;
                int fbSize   = _mode == 0 ? _width * _height * _bytesPerCell
                           : _mode == 2 ? _width * _height
                           : _width * _height * 3;
                var bytesToRead = Math.Min(count, fbSize);
                Array.Copy(_memory, fbOffset + offset, buffer, 0, bytesToRead);
                return bytesToRead;
            }
            catch
            {
                return -1;
            }
        }

        public int Write(byte[] buffer, int offset, int count)
        {
            if (_status != EDeviceStatus.Open)
                return -1;

            try
            {
                int fbOffset = _mode == 0 ? (int)TEXT_STORAGE : 0;
                int fbSize   = _mode == 0 ? _width * _height * _bytesPerCell
                           : _mode == 2 ? _width * _height
                           : _width * _height * 3;
                var bytesToWrite = Math.Min(count, fbSize);
                Array.Copy(buffer, offset, _memory, fbOffset, bytesToWrite);
                return bytesToWrite;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 控制设备
        /// </summary>
        /// <param name="command">控制命令</param>
        /// <param name="data">命令数据</param>
        /// <returns>命令执行结果</returns>
        public int Control(int command, byte[] data)
        {
            if (_status != EDeviceStatus.Open)
                return -1;

            switch (command)
            {
                case 0: // 获取VGA信息
                    return GetDisplayInfo(data);

                case 1: // 清屏
                    Clear();
                    return 0;

                case 2: // 设置像素（图形模式）
                    return SetPixel(data);

                case 3: // 绘制字符（字符模式）
                    return DrawChar(data);

                case 4: // 绘制字符串（字符模式）
                    return DrawString(data);

                case 5: // 设置显示模式
                    return SetMode(data);

                case 6: // 获取VGA内存
                    return GetMemory(data);

                default:
                    return -1;
            }
        }
        #endregion

        #region 特有方法
        /// <summary>
        /// 获取VGA信息
        /// </summary>
        /// <param name="data">数据缓冲区</param>
        /// <returns>执行结果</returns>
        private int GetDisplayInfo(byte[] data)
        {
            if (data == null || data.Length < 12)
                return -1;

            BitConverter.GetBytes(_width).CopyTo(data, 0);
            BitConverter.GetBytes(_height).CopyTo(data, 4);
            BitConverter.GetBytes(_mode).CopyTo(data, 8);
            return 0;
        }

        /// <summary>
        /// 设置像素（图形模式）
        /// </summary>
        /// <param name="data">像素数据 [x, y, r, g, b]</param>
        /// <returns>执行结果</returns>
        private int SetPixel(byte[] data)
        {
            if (data == null) return -1;

            if (_mode == 2)
            {
                if (data.Length < 3) return -1;
                int x = data[0];
                int y = data[1];
                if (x < 0 || x >= _width || y < 0 || y >= _height)
                    return -1;
                int index = y * _width + x;
                if (index >= _memory.Length)
                    return -1;
                _memory[index] = data[2]; // color index
                return 0;
            }
            else if (_mode == 1)
            {
                if (data.Length < 5) return -1;
                int x = data[0];
                int y = data[1];
                if (x < 0 || x >= _width || y < 0 || y >= _height)
                    return -1;
                int index = (y * _width + x) * 3;
                if (index + 2 >= _memory.Length)
                    return -1;
                _memory[index]     = data[2]; // R
                _memory[index + 1] = data[3]; // G
                _memory[index + 2] = data[4]; // B
                return 0;
            }
            return -1;
        }

        /// <summary>
        /// 绘制字符（字符模式，写入文本帧缓冲 0xB8000 区域）
        /// </summary>
        private int DrawChar(byte[] data)
        {
            if (_mode != 0 || data == null || data.Length < 4)
                return -1;

            int x = data[0];
            int y = data[1];

            if (x < 0 || x >= _width || y < 0 || y >= _height)
                return -1;

            int index = (int)TEXT_STORAGE + (y * _width + x) * 2;
            if (index + 1 >= _memory.Length)
                return -1;

            _memory[index]     = data[2]; // 字符
            _memory[index + 1] = data[3]; // 属性

            return 0;
        }

        /// <summary>
        /// 绘制字符串（字符模式，写入文本帧缓冲 0xB8000 区域）
        /// </summary>
        private int DrawString(byte[] data)
        {
            if (_mode != 0 || data == null || data.Length < 4)
                return -1;

            int  x    = data[0];
            int  y    = data[1];
            byte attr = data[2];

            if (x < 0 || x >= _width || y < 0 || y >= _height)
                return -1;

            int strLength    = data.Length - 3;
            int charsWritten = 0;

            for (int i = 0; i < strLength; i++)
            {
                int currentX = x + i;
                if (currentX >= _width)
                    break;

                int index = (int)TEXT_STORAGE + (y * _width + currentX) * 2;
                if (index + 1 >= _memory.Length)
                    break;

                _memory[index] = data[3 + i];        // 字符
                _memory[index           + 1] = attr; // 属性
                charsWritten++;
            }

            return charsWritten;
        }

        /// <summary>
        /// 设置显示模式
        /// </summary>
        private int SetMode(byte[] data)
        {
            if (data == null || data.Length < 3)
                return -1;

            int newMode   = data[0];
            int newWidth  = data[1];
            int newHeight = data[2];

            if (newWidth <= 0 || newHeight <= 0)
                return -1;

            _mode   = newMode;
            _width  = newWidth;
            _height = newHeight;

            // 不在此处清空帧缓冲，保留已有数据用于截图
            // Clear();
            return 0;
        }

        /// <summary>
        /// 根据 VGA SCREEN 模式号自动配置分辨率、色深和内部模式
        /// 14 种标准 QBASIC/VGA 模式
        /// </summary>
        /// <param name="vgaScreenMode">VGA SCREEN 模式号 (0-13)</param>
        /// <returns>(width, height, internalMode, bpp, isIndexed, paletteBase, paletteSize)</returns>
        public static (int w, int h, int mode, int bpp, bool indexed, int palAddr, int palEntries) GetModeInfo(int vgaScreenMode)
        {
            return vgaScreenMode switch
            {
                0  => (80,  25,  0, 2, false,  0,      0),    // 文本模式 (无调色板)
                1  => (320, 200, 2, 1, true,   0x6F00, 16),   // CGA 4色
                2  => (640, 200, 2, 1, true,   0x6F00, 16),   // CGA 2色
                3  => (720, 400, 2, 1, true,   0x6F00, 16),   // 黑白
                7  => (320, 200, 2, 1, true,   0x6F00, 16),   // EGA 16色
                8  => (640, 200, 2, 1, true,   0x6F00, 16),   // EGA 16色
                9  => (640, 350, 2, 1, true,   0x6F00, 16),   // EGA 16色
                10 => (640, 350, 2, 1, true,   0x6F00, 16),   // EGA 4色
                11 => (640, 480, 2, 1, true,   0x6F00, 16),   // VGA 2色
                12 => (640, 480, 2, 1, true,   0x6F00, 16),   // VGA 16色
                13 => (320, 200, 2, 1, true,   0x7800, 256),  // VGA 256色
                _  => (320, 200, 2, 1, true,   0x7800, 256),  // 默认
            };
        }

        /// <summary>
        /// 根据 VGA SCREEN 模式号重新配置设备
        /// </summary>
        public void ApplyVgaMode(int vgaScreenMode)
        {
            var (w, h, m, _, _, _, _) = GetModeInfo(vgaScreenMode);
            _mode   = m;
            _width  = w;
            _height = h;
        }

        /// <summary>8x8 font ROM base address (legacy)</summary>
        public const int FONT_ROM_ADDR = 0x6000;
        /// <summary>Font size: 95 chars × 8 rows, each stored as 32-bit word</summary>
        public const int FONT_ROM_SIZE = 95 * 8 * 4;

        // 字库配置寄存器 (MMIO)
        public const int FONT_MODE_MMIO   = 0x6FE8; // 字库模式: 0=8x8,32bit/row; 1=8x16,byte/row
        public const int FONT_WIDTH_MMIO  = 0x6FE9; // 字宽(像素)
        public const int FONT_HEIGHT_MMIO = 0x6FEA; // 字高(像素)
        public const int FONT_ADDR_MMIO   = 0x6FEC; // 字库基址(32-bit)

        /// <summary>8x8 font bitmap (95 printable ASCII chars, 8 bytes each)</summary>
        public static readonly byte[] Font8x8 = new byte[95 * 8];

        static VmDisplayDevice()
        {
            string bits =
                "00000000 00000000 00000000 00000000 00000000 00000000 00000000 00000000 " + // 32
                "00010000 00010000 00010000 00010000 00010000 00000000 00010000 00000000 " + // 33 !
                "00101000 00101000 00101000 00000000 00000000 00000000 00000000 00000000 " + // 34
                "00101000 00101000 01111100 00101000 01111100 00101000 00101000 00000000 " + // 35 #
                "00010000 00111100 01010000 00111000 00010100 01111000 00010000 00000000 " + // 36 $
                "01100010 01100100 00001000 00010000 00100000 01001100 01000110 00000000 " + // 37 %
                "00110000 01001000 01001000 00110000 01001010 01000100 00111010 00000000 " + // 38 &
                "00010000 00010000 00100000 00000000 00000000 00000000 00000000 00000000 " + // 39
                "00001000 00010000 00100000 00100000 00100000 00010000 00001000 00000000 " + // 40 (
                "00100000 00010000 00001000 00001000 00001000 00010000 00100000 00000000 " + // 41 )
                "00000000 00010000 01010100 00111000 01010100 00010000 00000000 00000000 " + // 42 *
                "00000000 00010000 00010000 01111100 00010000 00010000 00000000 00000000 " + // 43 +
                "00000000 00000000 00000000 00000000 00011000 00011000 00001000 00010000 " + // 44 ,
                "00000000 00000000 00000000 01111100 00000000 00000000 00000000 00000000 " + // 45 -
                "00000000 00000000 00000000 00000000 00000000 00011000 00011000 00000000 " + // 46 .
                "00000000 00000100 00001000 00010000 00100000 01000000 00000000 00000000 " + // 47 /
                "00111000 01000100 01001100 01010100 01100100 01000100 00111000 00000000 " + // 48 0
                "00010000 00110000 00010000 00010000 00010000 00010000 00111000 00000000 " + // 49 1
                "00111000 01000100 00000100 00001000 00010000 00100000 01111100 00000000 " + // 50 2
                "00111000 01000100 00000100 00011000 00000100 01000100 00111000 00000000 " + // 51 3
                "00001000 00011000 00101000 01001000 01111100 00001000 00001000 00000000 " + // 52 4
                "01111100 01000000 01111000 00000100 00000100 01000100 00111000 00000000 " + // 53 5
                "00111000 01000100 01000000 01111000 01000100 01000100 00111000 00000000 " + // 54 6
                "01111100 00000100 00001000 00010000 00100000 00100000 00100000 00000000 " + // 55 7
                "00111000 01000100 01000100 00111000 01000100 01000100 00111000 00000000 " + // 56 8
                "00111000 01000100 01000100 00111100 00000100 01000100 00111000 00000000 " + // 57 9
                "00000000 00011000 00011000 00000000 00011000 00011000 00000000 00000000 " + // 58 :
                "00000000 00011000 00011000 00000000 00011000 00011000 00001000 00010000 " + // 59 ;
                "00001000 00010000 00100000 01000000 00100000 00010000 00001000 00000000 " + // 60 <
                "00000000 00000000 01111100 00000000 01111100 00000000 00000000 00000000 " + // 61 =
                "00100000 00010000 00001000 00000100 00001000 00010000 00100000 00000000 " + // 62 >
                "00111000 01000100 00000100 00001000 00010000 00000000 00010000 00000000 " + // 63 ?
                "00111000 01000100 01011100 01010100 01011100 01000000 00111000 00000000 " + // 64 @
                "00010000 00101000 01000100 01000100 01111100 01000100 01000100 00000000 " + // 65 A
                "01111000 01000100 01000100 01111000 01000100 01000100 01111000 00000000 " + // 66 B
                "00111000 01000100 01000000 01000000 01000000 01000100 00111000 00000000 " + // 67 C
                "01111000 01000100 01000100 01000100 01000100 01000100 01111000 00000000 " + // 68 D
                "01111100 01000000 01000000 01111000 01000000 01000000 01111100 00000000 " + // 69 E
                "01111100 01000000 01000000 01111000 01000000 01000000 01000000 00000000 " + // 70 F
                "00111000 01000100 01000000 01001100 01000100 01000100 00111000 00000000 " + // 71 G
                "01000100 01000100 01000100 01111100 01000100 01000100 01000100 00000000 " + // 72 H
                "00111000 00010000 00010000 00010000 00010000 00010000 00111000 00000000 " + // 73 I
                "00011100 00001000 00001000 00001000 00001000 01001000 00110000 00000000 " + // 74 J
                "01000100 01001000 01010000 01100000 01010000 01001000 01000100 00000000 " + // 75 K
                "01000000 01000000 01000000 01000000 01000000 01000000 01111100 00000000 " + // 76 L
                "01000100 01101100 01010100 01010100 01000100 01000100 01000100 00000000 " + // 77 M
                "01000100 01100100 01010100 01001100 01000100 01000100 01000100 00000000 " + // 78 N
                "00111000 01000100 01000100 01000100 01000100 01000100 00111000 00000000 " + // 79 O
                "01111000 01000100 01000100 01111000 01000000 01000000 01000000 00000000 " + // 80 P
                "00111000 01000100 01000100 01000100 01010100 01001000 00110100 00000000 " + // 81 Q
                "01111000 01000100 01000100 01111000 01010000 01001000 01000100 00000000 " + // 82 R
                "00111000 01000100 01000000 00111000 00000100 01000100 00111000 00000000 " + // 83 S
                "01111100 00010000 00010000 00010000 00010000 00010000 00010000 00000000 " + // 84 T
                "01000100 01000100 01000100 01000100 01000100 01000100 00111000 00000000 " + // 85 U
                "01000100 01000100 01000100 01000100 00101000 00101000 00010000 00000000 " + // 86 V
                "01000100 01000100 01000100 01010100 01010100 01101100 01000100 00000000 " + // 87 W
                "01000100 01000100 00101000 00010000 00101000 01000100 01000100 00000000 " + // 88 X
                "01000100 01000100 00101000 00010000 00010000 00010000 00010000 00000000 " + // 89 Y
                "01111100 00000100 00001000 00010000 00100000 01000000 01111100 00000000 " + // 90 Z
                "00111000 00100000 00100000 00100000 00100000 00100000 00111000 00000000 " + // 91 [
                "00000000 01000000 00100000 00010000 00001000 00000100 00000000 00000000 " + // 92 backslash
                "00111000 00001000 00001000 00001000 00001000 00001000 00111000 00000000 " + // 93 ]
                "00010000 00101000 01000100 00000000 00000000 00000000 00000000 00000000 " + // 94 ^
                "00000000 00000000 00000000 00000000 00000000 00000000 01111100 00000000 " + // 95 _
                "00100000 00010000 00001000 00000000 00000000 00000000 00000000 00000000 " + // 96 `
                "00000000 00000000 00111000 00000100 00111100 01000100 00111100 00000000 " + // 97 a
                "01000000 01000000 01111000 01000100 01000100 01000100 01111000 00000000 " + // 98 b
                "00000000 00000000 00111000 01000100 01000000 01000100 00111000 00000000 " + // 99 c
                "00000100 00000100 00111100 01000100 01000100 01000100 00111100 00000000 " + // 100 d
                "00000000 00000000 00111000 01000100 01111100 01000000 00111000 00000000 " + // 101 e
                "00001100 00010000 00111000 00010000 00010000 00010000 00010000 00000000 " + // 102 f
                "00000000 00111100 01000100 01000100 00111100 00000100 00111000 00000000 " + // 103 g
                "01000000 01000000 01111000 01000100 01000100 01000100 01000100 00000000 " + // 104 h
                "00010000 00000000 00110000 00010000 00010000 00010000 00111000 00000000 " + // 105 i
                "00001000 00000000 00011000 00001000 00001000 01001000 00110000 00000000 " + // 106 j
                "01000000 01000000 01001000 01010000 01100000 01010000 01001000 00000000 " + // 107 k
                "00110000 00010000 00010000 00010000 00010000 00010000 00111000 00000000 " + // 108 l
                "00000000 00000000 01101100 01010100 01010100 01000100 01000100 00000000 " + // 109 m
                "00000000 00000000 01111000 01000100 01000100 01000100 01000100 00000000 " + // 110 n
                "00000000 00000000 00111000 01000100 01000100 01000100 00111000 00000000 " + // 111 o
                "00000000 01111000 01000100 01000100 01111000 01000000 01000000 00000000 " + // 112 p
                "00000000 00111100 01000100 01000100 00111100 00000100 00000100 00000000 " + // 113 q
                "00000000 00000000 01011000 01100100 01000000 01000000 01000000 00000000 " + // 114 r
                "00000000 00000000 00111100 01000000 00111000 00000100 01111000 00000000 " + // 115 s
                "00010000 00010000 01111100 00010000 00010000 00010000 00001100 00000000 " + // 116 t
                "00000000 00000000 01000100 01000100 01000100 01000100 00111100 00000000 " + // 117 u
                "00000000 00000000 01000100 01000100 00101000 00101000 00010000 00000000 " + // 118 v
                "00000000 00000000 01000100 01000100 01010100 01010100 00101000 00000000 " + // 119 w
                "00000000 00000000 01000100 00101000 00010000 00101000 01000100 00000000 " + // 120 x
                "00000000 01000100 01000100 00101000 00010000 00100000 01000000 00000000 " + // 121 y
                "00000000 00000000 01111100 00001000 00010000 00100000 01111100 00000000 " + // 122 z
                "00001100 00010000 00010000 01100000 00010000 00010000 00001100 00000000 " + // 123 {
                "00010000 00010000 00010000 00010000 00010000 00010000 00010000 00000000 " + // 124 |
                "01100000 00010000 00010000 00001100 00010000 00010000 01100000 00000000 " + // 125 }
                "00000000 00000000 00110010 01001100 00000000 00000000 00000000 00000000 " + // 126 ~
                "";
            var parts = bits.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length && i < Font8x8.Length; i++)
                Font8x8[i] = Convert.ToByte(parts[i], 2);
        }

        /// <summary>Write 8x8 font to main memory at FONT_ROM_ADDR (32-bit words)</summary>
        public static void InitDefaultFont(byte[] mainMemory)
        {
            if (mainMemory == null || mainMemory.Length < FONT_ROM_ADDR + FONT_ROM_SIZE) return;
            for (int i = 0; i < Font8x8.Length; i++)
            {
                int addr = FONT_ROM_ADDR + i * 4;
                // Store each font byte as a 32-bit word (little-endian)
                byte val = Font8x8[i];
                mainMemory[addr] = val;
                mainMemory[addr + 1] = 0;
                mainMemory[addr + 2] = 0;
                mainMemory[addr + 3] = 0;
            }

            // 写入字库配置寄存器 (默认 8x8@0x6000, mode=0)
            if (mainMemory.Length > FONT_ADDR_MMIO + 4)
            {
                // 检查是否已有 8x16 字库加载 (VgaFont 之后调用)
                bool has8x16 = mainMemory.Length > 0xFF000 + 4096 && mainMemory[0xFF000 + 65 * 16 + 2] != 0;

                mainMemory[FONT_MODE_MMIO]   = (byte)(has8x16 ? 1 : 0);
                mainMemory[FONT_WIDTH_MMIO]  = 8;
                mainMemory[FONT_HEIGHT_MMIO] = (byte)(has8x16 ? 16 : 8);

                int fontAddr = has8x16 ? 0xFF000 : FONT_ROM_ADDR;
                byte[] addrBytes = BitConverter.GetBytes(fontAddr);
                for (int j = 0; j < 4; j++)
                    mainMemory[FONT_ADDR_MMIO + j] = addrBytes[j];
            }
        }

        /// <summary>刷新字库配置寄存器 — 在 VgaFont.LoadFontRom 后调用以切换到 8x16 字库</summary>
        public static void RefreshFontConfig(byte[] mainMemory)
        {
            if (mainMemory == null || mainMemory.Length <= FONT_ADDR_MMIO + 4) return;
            bool has8x16 = mainMemory.Length > 0xFF000 + 4096 && mainMemory[0xFF000 + 65 * 16 + 2] != 0;
            mainMemory[FONT_MODE_MMIO]   = (byte)(has8x16 ? 1 : 0);
            mainMemory[FONT_HEIGHT_MMIO] = (byte)(has8x16 ? 16 : 8);
            int fontAddr = has8x16 ? 0xFF000 : FONT_ROM_ADDR;
            byte[] addrBytes = BitConverter.GetBytes(fontAddr);
            for (int j = 0; j < 4; j++)
                mainMemory[FONT_ADDR_MMIO + j] = addrBytes[j];
        }

        /// <summary>
        /// 初始化默认调色板到主内存 (0x6F00 16色 + 0x7800 256色)
        /// </summary>
        public static void InitDefaultPalette(byte[] mainMemory)
        {
            if (mainMemory == null || mainMemory.Length < 0x7900) return;

            // 标准 VGA 16 色调色板 → 0x6F00 (每色 3 字节 RGB, 0-63)
            var pal16 = new byte[] {
                0,0,0,     0,0,42,    0,42,0,    0,42,42,
                42,0,0,    42,0,42,   42,42,0,   42,42,42,
                21,21,21,  21,21,63,  21,63,21,  21,63,63,
                63,21,21,  63,21,63,  63,63,21,  63,63,63,
            };
            for (int i = 0; i < 48; i++)
                mainMemory[0x6F00 + i] = pal16[i];

            // 256 色调色板 → 0x7800
            // 前 16 色映射到标准 VGA 16 色
            for (int i = 0; i < 16; i++)
            {
                mainMemory[0x7800 + i * 3 + 0] = pal16[i * 3 + 0];
                mainMemory[0x7800 + i * 3 + 1] = pal16[i * 3 + 1];
                mainMemory[0x7800 + i * 3 + 2] = pal16[i * 3 + 2];
            }
            // 颜色 16-31: 灰阶
            for (int i = 16; i < 32; i++)
            {
                byte g = (byte)((i - 16) * 4);
                mainMemory[0x7800 + i * 3 + 0] = g;
                mainMemory[0x7800 + i * 3 + 1] = g;
                mainMemory[0x7800 + i * 3 + 2] = g;
            }
            // 颜色 32-247: 6x6x6 色彩立方 (216色)
            for (int r = 0; r < 6; r++)
                for (int g = 0; g < 6; g++)
                    for (int b = 0; b < 6; b++)
                    {
                        int idx = 32 + (r * 36 + g * 6 + b);
                        mainMemory[0x7800 + idx * 3 + 0] = (byte)(r * 12); // 0,12,24,36,48,60
                        mainMemory[0x7800 + idx * 3 + 1] = (byte)(g * 12);
                        mainMemory[0x7800 + idx * 3 + 2] = (byte)(b * 12);
                    }
            // 颜色 248-255: 更多灰阶
            for (int i = 248; i < 256; i++)
            {
                byte g = (byte)((i - 248) * 32 + 8);
                mainMemory[0x7800 + i * 3 + 0] = g;
                mainMemory[0x7800 + i * 3 + 1] = g;
                mainMemory[0x7800 + i * 3 + 2] = g;
            }
        }

        /// <summary>
        /// 获取VGA内存（当前模式的帧缓冲区域）
        /// </summary>
        private int GetMemory(byte[] data)
        {
            int fbOffset = _mode == 0 ? (int)TEXT_STORAGE : 0;
            int fbSize   = _mode == 0 ? _width * _height * _bytesPerCell
                       : _mode == 2 ? _width * _height
                       : _width * _height * 3;

            if (data == null || data.Length < fbSize)
                return -1;

            Array.Copy(_memory, fbOffset, data, 0, fbSize);
            return fbSize;
        }

        /// <summary>
        /// 清屏
        /// </summary>
        private void Clear()
        {
            Array.Clear(_memory, 0, _memory.Length);
        }
        #endregion
    }
}
