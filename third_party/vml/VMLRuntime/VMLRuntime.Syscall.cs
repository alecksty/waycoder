using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using VMLAssembler;
using System.IO;
using VMLRuntime.Device;

namespace VMLRuntime
{
    public partial class VmRuntime
    {
        private void ExecuteSyscall(List<Operand> operands)
        {
            var syscallNumber = operands[0];
            int syscallNum    = (int)syscallNumber.Value;
            // 权限检查：用户态只能执行允许的系统调用
            if (privilegeLevel > 0 && !UserAllowedSyscalls.Contains(syscallNum))
            {
                Console.WriteLine($"Permission denied: syscall {syscallNum} requires kernel mode");
                registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED;
                return;
            }

            // 首先尝试使用系统调用处理器
            if (_systemCallHandler != null && _systemCallHandler.HandleSyscall(syscallNum, registers, memory, ref pc))
            {
                return; // 处理器已处理该系统调用
            }

            SyscallsExecuted++;

            // 如果没有处理器或处理器未处理，使用默认实现
            switch (syscallNum)
            {
                case 1: Syscall_PrintString(); break;
                case 2: Syscall_ReadString(); break;
                case 3: ExitCode = registers[0]; pc = -1; break;     // Exit( R0 )
                case 4: Syscall_PrintChar(); break;
                case 5: Syscall_ReadChar(); break;
                case 6: Syscall_PrintInt(); break;
                case 7: Syscall_ReadInt(); break;
                case 8: Syscall_PrintFloat(); break;
                case 9: Syscall_ReadFloat(); break;
                case 10: // OutputHex(R0) — 输出 R0 的十六进制表示
                {
                    int val = registers[0];
                    string hex = val.ToString("X");
                    foreach (char c in hex) OutputChar(c);
                    break;
                }
                case 11: // MemCopy(R0=src, R1=dst, R2=len) -> R0=bytes copied
                {
                    int src = registers[0], dst = registers[1], len = registers[2];
                    if (src < 0 || dst < 0 || src >= memory.Length || dst >= memory.Length)
                    {
                        registers[0] = 0;
                        break;
                    }
                    int maxLen = Math.Min(len, Math.Min(memory.Length - src, memory.Length - dst));
                    if (maxLen > 0) Array.Copy(memory, src, memory, dst, maxLen);
                    registers[0] = maxLen;
                    break;
                }
                case 12: // MemFill(R0=addr, R1=value, R2=len, R3=srcLen) -> R0=0
                {
                    int addr = registers[0], val = registers[1], len = registers[2];
                    int srcLen = registers[3] > 0 ? registers[3] : 1;
                    if (addr < 0 || addr >= memory.Length) { registers[0] = 0; break; }
                    byte b0 = (byte)(val & 0xFF);
                    byte b1 = (byte)((val >> 8) & 0xFF);
                    byte b2 = (byte)((val >> 16) & 0xFF);
                    byte b3 = (byte)((val >> 24) & 0xFF);
                    int maxLen = Math.Min(len, memory.Length - addr);
                    for (int i = 0; i + srcLen <= maxLen; i += srcLen)
                    {
                        memory[addr + i] = b0;
                        if (srcLen >= 2) memory[addr + i + 1] = b1;
                        if (srcLen >= 3) memory[addr + i + 2] = b2;
                        if (srcLen >= 4) memory[addr + i + 3] = b3;
                    }
                    registers[0] = 0;
                    break;
                }
                case 13: // MemCompare(R0=addr1, R1=addr2, R2=len) -> R0=0(equal)/non-zero(diff)
                {
                    int a1 = registers[0], a2 = registers[1], len = registers[2];
                    if (a1 < 0 || a2 < 0 || a1 >= memory.Length || a2 >= memory.Length) { registers[0] = 1; break; }
                    int maxLen = Math.Min(len, Math.Min(memory.Length - a1, memory.Length - a2));
                    registers[0] = maxLen > 0 ? new ReadOnlySpan<byte>(memory, a1, maxLen)
                        .SequenceCompareTo(new ReadOnlySpan<byte>(memory, a2, maxLen)) : 1;
                    break;
                }
                case 40: // Alloc(size) -> addr
                    registers[0] = AllocateMemory(registers[0]);
                    break;
                case 41: // Free(addr)
                    FreeMemory(registers[0]);
                    break;
                case 50: // Random -> R0
                    registers[0] = random.Next();
                    break;
                case 51: // Seed(R0)
                    random = new Random(registers[0]);
                    break;
                case 52: // Sleep(ms)
                    Thread.Sleep(Math.Max(1, registers[0]));
                    break;
                case 53: // GetTick -> R0 = ms
                    registers[0] = (int)(DateTime.UtcNow - _startTime).TotalMilliseconds;
                    break;
                case 54: // GetDateTime -> R0 = unix timestamp
                    registers[0] = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                    break;
                case 55: registers[0] = AllocStr(DateTime.Now.ToString("yyyy-MM-dd")); break;
                case 60: // GetConfig(type) -> R0 = address/value
                    registers[0] = registers[0] switch
                    {
                        // 0-9: VGA 显示
                        0 => vgaStartAddress,       // VGA 文本模式帧缓冲 (0xB8000)
                        1 => vgaGraphicsAddress,    // VGA 图形模式帧缓冲 (0xA0000)
                        2 => 0x6FF4,                // 光标行寄存器地址
                        3 => 0x6FF8,                // 光标列寄存器地址
                        4 => 0x6FFC,                // 前景色寄存器地址
                        5 => 0x6FFD,                // 背景色寄存器地址
                        6 => 0x6FF0,                // VGA 模式寄存器地址
                        7 => memorySize,            // 总内存大小（字节）
                        8 => vgaWidth,              // 显示宽度
                        9 => vgaHeight,             // 显示高度

                        // 10-19: 系统信息
                        10 => 0,                     // 配置版本（当前 0）
                        11 => memorySize - Math.Min(_config?.StackSize ?? 0x10000, memorySize / 2),  // 栈区起始地址
                        12 => Math.Min(_config?.StackSize ?? 0x10000, memorySize / 2),               // 栈区大小
                        13 => 0x400,                 // 堆分配起始地址

                        // 20-29: 设备配置
                        20 => 0x60,                  // 键盘数据寄存器地址
                        21 => 0x64,                  // 键盘状态寄存器地址
                        22 => 0x300,                 // 鼠标 X 坐标地址
                        23 => 0x302,                 // 鼠标 Y 坐标地址
                        24 => 0x304,                 // 鼠标按钮地址

                        // 30-39: BASIC/QB 兼容
                        30 => 0x6FA0,                // BASIC Turtle 图形 X
                        31 => 0x6FA4,                // BASIC Turtle 图形 Y
                        32 => 0x6FA8,                // BASIC Turtle 颜色
                        33 => 0x6FAC,                // BASIC Turtle 缩放
                        34 => 0x6FB0,                // BASIC Turtle 角度
                        35 => 0x6F00,                // QB 16色调色板基址 (QB_PALETTE_ADDR)
                        36 => 0x7800,                // QB 256色调色板基址 (QB_PALETTE13_ADDR)
                        37 => 0x6FC0,                // BASIC 错误处理程序表
                        38 => 0x6FC4,                // BASIC 错误标志
                        39 => 0x6FD0,                // BASIC DATA 指针

                        _ => vgaStartAddress
                    };
                    break;
                case 56: registers[0] = AllocStr(DateTime.Now.ToString("HH:mm:ss")); break;
                case 57: // SpeakerBeep(R0=freq_hz, R1=duration_ms)
                    VmSpeakerDevice.Beep(registers[0], registers[1]);
                    break;
                case 58: // SetRTC(R0=unix_timestamp) -> R0=0
                    registers[0] = ErrorCodes.SUCCESS;
                    break;
                case 59: // GetInfo(TypeId) -> R0=string_addr
                    ExecuteGetInfo();
                    break;
                case 70: // DebugPrint(R0=string)
                {
                    int addr = registers[0];
                    while (addr < memory.Length && memory[addr] != 0)
                    {
                        Console.Error.Write((char)memory[addr]);
                        addr++;
                    }
                    break;
                }
                case 71: // DebugPrintInt(R0=int)
                    Console.Error.Write(registers[0]);
                    break;
                case 72: // Assert(R0=condition, R1=message)
                    if (registers[0] == 0)
                    {
                        int addr = registers[1];
                        Console.Error.Write("ASSERT FAILED: ");
                        while (addr < memory.Length && memory[addr] != 0)
                        {
                            Console.Error.Write((char)memory[addr]);
                            addr++;
                        }
                        Console.Error.WriteLine();
                        pc = -1;
                    }
                    break;
                case 100: // 设备操作：打开设备
                    ExecuteDeviceOpen();
                    break;
                case 101: // 设备操作：关闭设备
                    ExecuteDeviceClose();
                    break;
                case 102: // 设备操作：读取设备
                    ExecuteDeviceRead();
                    break;
                case 103: // 设备操作：写入设备
                    ExecuteDeviceWrite();
                    break;
                case 104: // 设备操作：控制设备
                    ExecuteDeviceControl();
                    break;
                case 106: // EEPROM_Read(R0=offset, R1=buf, R2=len) -> R0=bytes read
                    registers[0] = ErrorCodes.NOT_SUPPORTED;
                    break;
                case 107: // EEPROM_Write(R0=offset, R1=buf, R2=len) -> R0=bytes written
                    registers[0] = ErrorCodes.NOT_SUPPORTED;
                    break;
                case 110: // 文件操作：打开文件
                    ExecuteFileOpen();
                    break;
                case 111: // 文件操作：关闭文件
                    ExecuteFileClose();
                    break;
                case 112: // 文件操作：读取文件
                    ExecuteFileRead();
                    break;
                case 113: // 文件操作：写入文件
                    ExecuteFileWrite();
                    break;
                case 114: // 文件操作：控制文件
                    ExecuteFileControl();
                    break;

                // ======== Debug SYSCALL (200) ========
                case 200:   // DebugScreenshot — saves BMP immediately
                    {
                        string prefix = DebugScreenshotPrefix;
                        int n = ++DebugScreenshotCounter;
                        string path = $"{prefix}_{n:D4}.bmp";
                        // 读取 VGA SCREEN 模式号，自动查表获取分辨率和色深
                        int vgaMode = Memory != null && Memory.Length > 0x6FF0 ? Memory[0x6FF0] : 0;
                        var (w, h, imgMode, fbpp, indexed, _, _) = VmDisplayDevice.GetModeInfo(vgaMode);
                        // 通知 VGA 设备更新模式
                        if (_deviceManager.FindDevice("vga") is VmDisplayDevice vgaDev)
                            vgaDev.ApplyVgaMode(vgaMode);
                        try
                        {
                            byte[] vgaMem = GetVgaMemory(imgMode, w, h, fbpp);
                            byte[] pixels = new byte[w * h * 3];
                            for (int y = 0; y < h; y++)
                                for (int x = 0; x < w; x++)
                                {
                                    int src = (y * w + x) * fbpp;
                                    int dst = (y * w + x) * 3;
                                    if (imgMode == 1) // RGB
                                    {
                                        pixels[dst] = src + 2 < vgaMem.Length ? vgaMem[src + 2] : (byte)0;
                                        pixels[dst + 1] = src + 1 < vgaMem.Length ? vgaMem[src + 1] : (byte)0;
                                        pixels[dst + 2] = src < vgaMem.Length ? vgaMem[src] : (byte)0;
                                    }
                                    else if (imgMode == 2) // indexed
                                    {
                                        byte ci = src < vgaMem.Length ? vgaMem[src] : (byte)0;
                                        int pa = 0x7800 + ci * 3;
                                        pixels[dst] = pa + 2 < Memory.Length ? Memory[pa + 2] : (byte)0;
                                        pixels[dst + 1] = pa + 1 < Memory.Length ? Memory[pa + 1] : (byte)0;
                                        pixels[dst + 2] = pa < Memory.Length ? Memory[pa] : (byte)0;
                                    }
                                    else // text
                                    {
                                        byte ch = src < vgaMem.Length ? vgaMem[src] : (byte)0;
                                        byte gray = ch is >= 32 and <= 126 ? (byte)200 : ch == 0 ? (byte)0 : (byte)128;
                                        pixels[dst] = gray; pixels[dst + 1] = gray; pixels[dst + 2] = gray;
                                    }
                                }
                            int rowSize = (w * 3 + 3) & ~3;
                            using var fs = new FileStream(path, FileMode.Create);
                            using var bw = new BinaryWriter(fs);
                            bw.Write((byte)'B'); bw.Write((byte)'M');
                            bw.Write(14 + 40 + rowSize * h - 2);
                            bw.Write((short)0); bw.Write((short)0);
                            bw.Write(14 + 40);
                            bw.Write(40); bw.Write(w); bw.Write(-h);
                            bw.Write((short)1); bw.Write((short)24);
                            bw.Write(0); bw.Write(rowSize * h);
                            bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
                            for (int y = 0; y < h; y++)
                                bw.Write(pixels, y * w * 3, w * 3);
                            for (int y = 0; y < h; y++)
                                for (int p = w * 3; p < rowSize; p++)
                                    bw.Write((byte)0);
                            Console.Error.WriteLine($"[DBG-SHOT] {path} mode={vgaMode} {w}x{h} bpp={fbpp}");
                        }
                        catch (Exception ex) { Console.Error.WriteLine($"[DBG-SHOT] Error: {ex.Message}"); }
                    }
                    break;
                case 201: // PutImage(R0=x, R1=y, R2=w, R3=h, R4=pixels) -> R0=0/error
                {
                    int x = registers[0], y = registers[1], w = registers[2], h = registers[3], pxAddr = registers[4];
                    if (_deviceManager.FindDevice("vga") is VmDisplayDevice vga)
                    {
                        int rowBytes = w * 3;
                        for (int row = 0; row < h; row++)
                        {
                            int pxOff = row * rowBytes;
                            int vgaOff = ((y + row) * vga.Width + x) * 3;
                            for (int col = 0; col < w * 3 && vgaOff + col < vga.Memory.Length && pxOff + col < memory.Length; col++)
                                vga.Memory[vgaOff + col] = memory[pxAddr + pxOff + col];
                        }
                        registers[0] = ErrorCodes.SUCCESS;
                    }
                    else registers[0] = ErrorCodes.DEVICE_NOT_FOUND;
                    break;
                }
                case 202: // GetImage(R0=x, R1=y, R2=w, R3=h, R4=buf) -> R0=0/error
                {
                    int x = registers[0], y = registers[1], w = registers[2], h = registers[3], buf = registers[4];
                    if (_deviceManager.FindDevice("vga") is VmDisplayDevice vga)
                    {
                        int rowBytes = w * 3;
                        for (int row = 0; row < h; row++)
                        {
                            int vgaOff = ((y + row) * vga.Width + x) * 3;
                            int bufOff = row * rowBytes;
                            for (int col = 0; col < rowBytes && vgaOff + col < vga.Memory.Length && buf + bufOff + col < memory.Length; col++)
                                memory[buf + bufOff + col] = vga.Memory[vgaOff + col];
                        }
                        registers[0] = ErrorCodes.SUCCESS;
                    }
                    else registers[0] = ErrorCodes.DEVICE_NOT_FOUND;
                    break;
                }
                case 203: // Viewport(R0=x1, R1=y1, R2=x2, R3=y2) -> clip region
                    registers[0] = ErrorCodes.NOT_SUPPORTED;
                    break;

                // ======== OS模式扩展 SYSCALL (300+) ========
                // MCU模式(privilegeLevel>0)返回权限拒绝
                case 300: // ThreadCreate(fn_addr, stack_size) -> thread_id
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteThreadCreate();
                    break;
                case 301: // ThreadExit()
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteThreadExit();
                    break;
                case 302: // ThreadJoin(thread_id)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteThreadJoin();
                    break;
                case 303: // ThreadYield()
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    System.Threading.Thread.Yield();
                    registers[0] = ErrorCodes.SUCCESS;
                    break;
                // 304: ThreadSleep — 已删除，统一使用 Sleep(52)
                case 310: // MutexCreate() -> mutex_id
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteMutexCreate();
                    break;
                case 311: // MutexLock(mutex_id)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteMutexLock();
                    break;
                case 312: // MutexUnlock(mutex_id)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteMutexUnlock();
                    break;
                case 313: // CondCreate() -> cond_id
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteCondCreate();
                    break;
                case 314: // CondWait(cond_id, mutex_id)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteCondWait();
                    break;
                case 315: // CondSignal(cond_id)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteCondSignal();
                    break;
                case 316: // CondBroadcast(cond_id)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteCondBroadcast();
                    break;
                case 320: // Exec(path_addr) -> pid
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteExec();
                    break;
                // 321: ProcessExit — 已删除，统一使用 Exit(3)
                case 322: // GetPID() -> pid
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    registers[0] = System.Diagnostics.Process.GetCurrentProcess().Id;
                    break;
                case 330: // SocketCreate(domain, type) -> fd
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteSocketCreate();
                    break;
                case 331: // SocketBind(fd, port)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteSocketBind();
                    break;
                case 332: // SocketListen(fd, backlog)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteSocketListen();
                    break;
                case 333: // SocketAccept(fd) -> client_fd
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteSocketAccept();
                    break;
                case 334: // SocketConnect(fd, addr, port)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteSocketConnect();
                    break;
                case 335: // SocketSend(fd, buf, len) -> sent
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteSocketSend();
                    break;
                case 336: // SocketRecv(fd, buf, len) -> received
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteSocketRecv();
                    break;
                case 337: // SocketClose(fd)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteSocketClose();
                    break;
                case 338: // DnsResolve(hostname_addr) -> ip_addr
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteDnsResolve();
                    break;
                case 340: // MkDir(path_addr)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteMkDir();
                    break;
                case 341: // Remove(path_addr)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteRemove();
                    break;
                case 342: // Rename(old_addr, new_addr)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteRename();
                    break;
                case 343: // ReadDir(path_addr, buf_addr) -> count
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteReadDir();
                    break;
                case 344: // Stat(path_addr, buf_addr) -> 0/error
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteStat();
                    break;
                case 350: // Signal(signum, handler_addr)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    registers[0] = ErrorCodes.NOT_SUPPORTED;
                    break;
                case 360: // GetEnv(name_addr) -> value_addr
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteGetEnv();
                    break;
                case 361: // SetEnv(name_addr, value_addr)
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteSetEnv();
                    break;
                case 362: // GetArgs(buf_addr) -> count
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    ExecuteGetArgs();
                    break;
                case 370: // DLOpen(path_addr) -> handle
                    ExecuteDLOpen();
                    break;
                case 371: // DLSym(handle, name_addr) -> func_ptr
                    ExecuteDLSym();
                    break;
                case 372: // DLClose(handle) -> result
                    ExecuteDLClose();
                    break;
                case 373: // NativeCall(funcId, argsPtr, argCount, flags) -> result
                    ExecuteNativeCall();
                    break;
                case 374: // GetPlatform() -> platform_id
                    ExecuteGetPlatform();
                    break;
                case 375: // NativeCallF(funcId, floatArgsPtr, argCount, flags) -> result
                    ExecuteNativeCallF();
                    break;
                case 376: // NativeCallEx(funcId, argsPtr, typeDescPtr, argCount, retType) -> result
                    ExecuteNativeCallEx();
                    break;
                case 380: // TypeOf(addr) -> type_id
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    registers[0] = ErrorCodes.NOT_SUPPORTED;
                    break;
                case 381: // TypeName(type_id, buf_addr) -> result
                    if (privilegeLevel > 0) { registers[0] = ErrorCodes.SYSCALL_PERMISSION_DENIED; break; }
                    registers[0] = ErrorCodes.NOT_SUPPORTED;
                    break;

                case 391: // OutputWString(R0=wchar_t*addr) — 输出 16 位宽字符串
                    ExecuteSyscall391_OutputWString();
                    break;
                case 392: // InputWString(R0=wchar_t*buf, R1=max_chars) — 读入 16 位宽字符串
                    ExecuteSyscall392_InputWString();
                    break;
                case 393: // OutputUString(R0=char32_t*addr) — 输出 32 位 Unicode 字符串
                    ExecuteSyscall393_OutputUString();
                    break;
                case 394: // InputUString(R0=char32_t*buf, R1=max_chars) — 读入 32 位 Unicode 字符串
                    ExecuteSyscall394_InputUString();
                    break;

                case 400: // TTY_WriteChar(R0=char) — 直接写入终端, 绕过 stdout 重定向
                    Console.Write((char)registers[0]);
                    break;
                case 401: // TTY_WriteString(R0=addr) — 直接写入终端字符串
                {
                    int addr = registers[0];
                    while (addr < memory.Length && memory[addr] != 0)
                        Console.Write((char)memory[addr++]);
                    break;
                }
                case 402: // TTY_PrintInt(R0=int) — 直接写入终端整数
                    Console.Write(registers[0]);
                    break;

                default:
                    Console.WriteLine($"Unknown syscall: {syscallNum}");
                    registers[0] = ErrorCodes.UNKNOWN_SYSCALL;
                    break;
            }
        }
        /// <summary>
        /// 输出字符串（向后兼容）
        /// </summary>
        private void ExecuteSyscall1_OutputString()
        {
            // 简化: 逐字 OutputChar (统一输出原语)
            var address = registers[0];
            while (address < memory.Length && memory[address] != 0)
            {
                OutputChar((char)memory[address]);
                address++;
            }
        }

        /// <summary>
        /// 输入字符串（向后兼容）
        /// </summary>
        private void ExecuteSyscall2_InputString()
        {
            string input;

            // KeyScript mode: read from keyboard buffer (supports key injection)
            if (KeyScriptActive)
            {
                input = ReadLineFromKeyBuffer();
            }
            else if (_consoleIO != null)
            {
                input = _consoleIO.ReadString();
            }
            else
            {
                input = Console.ReadLine() ?? string.Empty;
            }

            var address = registers[0];
            for (var i = 0; i < input.Length; i++)
            {
                if (address + i < memory.Length)
                {
                    memory[address + i] = (byte)input[i];
                }
            }
            if (address + input.Length < memory.Length)
            {
                memory[address + input.Length] = 0; // 字符串结束符
            }
        }

        /// <summary>
        /// SYSCALL 331: 输出 16 位宽字符串 (UTF-16LE) 到标准输出
        /// R0 = wchar_t* 字符串起始地址
        /// </summary>
        private void ExecuteSyscall391_OutputWString()
        {
            var address = registers[0];
            var bytes = new List<byte>();
            while (address + 1 < memory.Length)
            {
                ushort wch = (ushort)(memory[address] | (memory[address + 1] << 8));
                if (wch == 0) break;
                bytes.Add(memory[address]);
                bytes.Add(memory[address + 1]);
                address += 2;
            }
            var str = System.Text.Encoding.Unicode.GetString(bytes.ToArray());
            if (_consoleIO != null)
                _consoleIO.WriteString(str);
            else
                Console.Write(str);
        }

        /// <summary>
        /// SYSCALL 332: 从标准输入读取一行到 16 位宽字符串缓冲区
        /// R0 = wchar_t* 缓冲区地址, R1 = 最大字符数（含终止符）
        /// R0 返回实际写入的字符数（不含终止符）
        /// </summary>
        private void ExecuteSyscall392_InputWString()
        {
            string input;
            if (_consoleIO != null)
                input = _consoleIO.ReadString();
            else
                input = Console.ReadLine() ?? string.Empty;

            int maxChars = registers[1];
            if (maxChars <= 0) maxChars = 256;
            int writeCount = Math.Min(input.Length, maxChars - 1);
            var address = registers[0];
            var utf16Bytes = System.Text.Encoding.Unicode.GetBytes(input);
            int byteCount = Math.Min(utf16Bytes.Length, writeCount * 2);
            for (int i = 0; i < byteCount && address + i < memory.Length; i++)
                memory[address + i] = utf16Bytes[i];
            // 写入终止符 0x0000
            if (address + byteCount + 1 < memory.Length)
            {
                memory[address + byteCount] = 0;
                memory[address + byteCount + 1] = 0;
            }
            registers[0] = writeCount;
        }

        /// <summary>
        /// SYSCALL 333: 输出 32 位 Unicode 字符串 (UTF-32LE) 到标准输出
        /// R0 = char32_t* 字符串起始地址
        /// </summary>
        private void ExecuteSyscall393_OutputUString()
        {
            var address = registers[0];
            var codePoints = new List<int>();
            while (address + 3 < memory.Length)
            {
                int cp = memory[address] | (memory[address + 1] << 8) | (memory[address + 2] << 16) | (memory[address + 3] << 24);
                if (cp == 0) break;
                codePoints.Add(cp);
                address += 4;
            }
            var str = new System.Text.StringBuilder();
            foreach (int cp in codePoints)
            {
                if (cp <= 0xFFFF)
                    str.Append((char)cp);
                else if (cp <= 0x10FFFF)
                {
                    int hi = 0xD800 + ((cp - 0x10000) >> 10);
                    int lo = 0xDC00 + ((cp - 0x10000) & 0x3FF);
                    str.Append((char)hi);
                    str.Append((char)lo);
                }
            }
            if (_consoleIO != null)
                _consoleIO.WriteString(str.ToString());
            else
                Console.Write(str.ToString());
        }

        /// <summary>
        /// SYSCALL 334: 从标准输入读取一行到 32 位 Unicode 字符串缓冲区
        /// R0 = char32_t* 缓冲区地址, R1 = 最大字符数（含终止符）
        /// R0 返回实际写入的字符数（不含终止符）
        /// </summary>
        private void ExecuteSyscall394_InputUString()
        {
            string input;
            if (_consoleIO != null)
                input = _consoleIO.ReadString();
            else
                input = Console.ReadLine() ?? string.Empty;

            int maxChars = registers[1];
            if (maxChars <= 0) maxChars = 256;
            var address = registers[0];
            int writeCount = 0;
            for (int i = 0; i < input.Length && i < maxChars - 1 && address + 3 < memory.Length; i++)
            {
                int cp = char.ConvertToUtf32(input, i);
                if (char.IsSurrogatePair(input, i)) i++; // skip low surrogate
                memory[address] = (byte)(cp & 0xFF);
                memory[address + 1] = (byte)((cp >> 8) & 0xFF);
                memory[address + 2] = (byte)((cp >> 16) & 0xFF);
                memory[address + 3] = (byte)((cp >> 24) & 0xFF);
                address += 4;
                writeCount++;
            }
            // 写入终止符 0x00000000
            if (address + 3 < memory.Length)
            {
                memory[address] = 0;
                memory[address + 1] = 0;
                memory[address + 2] = 0;
                memory[address + 3] = 0;
            }
            registers[0] = writeCount;
        }

        /// <summary>
        /// 输出字符（向后兼容）
        /// 支持两种模式：
        /// - 值 &lt; 256：UTF-8 字节流，缓冲并解码完整多字节序列后输出
        /// - 值 &gt;= 256：Unicode 码点（如 Pascal writeln），直接输出字符
        /// </summary>
        private void ExecuteSyscall4_OutputChar()
        {
            var ch = registers[0];

            // BEL (0x07) triggers PC speaker
            if (ch == 7)
            {
                VmSpeakerDevice.Beep();
                return;
            }

            // 值 >= 256 是 Unicode 码点直接输出 (如 Pascal compiler)
            if (ch >= 256)
            {
                if (_utf8OutputBuffer.Count > 0)
                {
                    foreach (byte b in _utf8OutputBuffer)
                        OutputChar((char)b);
                    _utf8OutputBuffer.Clear();
                }
                OutputChar((char)ch);
                return;
            }

            // 积累 UTF-8 字节（值 < 256）
            _utf8OutputBuffer.Add((byte)ch);

            int utf8Len = ExpectedUtf8Bytes(_utf8OutputBuffer[0]);
            if (_utf8OutputBuffer.Count < utf8Len)
                return;

            // 解码 UTF-8 序列并输出到控制台
            string decoded;
            try
            {
                decoded = System.Text.Encoding.UTF8.GetString(_utf8OutputBuffer.ToArray());
            }
            catch
            {
                decoded = "?";
            }
            _utf8OutputBuffer.Clear();

            foreach (char c in decoded)
                OutputChar(c);
        }

        private void OutputChar(char c)
        {
            if (_consoleIO != null)
                _consoleIO.WriteChar(c);
            else
                Console.Write(c);
        }

        private void OutputSingleByte(byte b)
        {
            OutputChar((char)b);
        }

        private void WriteVgaChar(char c)
        {
            if (c >= 32 && c < 127)
            {
                try
                {
                    int vgaRow = GetMemory(0x6FF4);
                    int vgaCol = GetMemory(0x6FF8);
                    // Clamp to valid ranges (0-24 rows, 0-79 cols for standard VGA)
                    if (vgaRow < 0 || vgaRow > 200) return;
                    if (vgaCol < 0 || vgaCol > 200) return;
                    int addr = vgaStartAddress + (vgaRow * vgaWidth + vgaCol) * 2;
                    if (addr >= vgaStartAddress && addr + 1 < vgaStartAddress + vgaSize)
                    {
                        SetMemoryByte(addr, (byte)c);
                        vgaCol++;
                        SetMemory(0x6FF8, vgaCol);
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"VGA write error: {ex.Message}"); }
            }
        }

        private static int ExpectedUtf8Bytes(byte first)
        {
            if ((first & 0x80) == 0) return 1;
            if ((first & 0xE0) == 0xC0) return 2;
            if ((first & 0xF0) == 0xE0) return 3;
            if ((first & 0xF8) == 0xF0) return 4;
            return 1; // 无效字节，当作单字节处理
        }

        /// <summary>
        /// 输入字符（向后兼容）
        /// </summary>
        private void ExecuteSyscall5_InputChar()
        {
            // R0=0 (or unset): non-blocking (INKEY$/KBGETCH)
            // R0=1: blocking (INPUT) — wait until key available
            bool blocking = registers[0] == 1;

            // KeyScript mode: poll keyboard buffer.
            // Blocking: wait until key arrives. Non-blocking: return 0 if no key.
            if (KeyScriptActive)
            {
                var kbdDev2 = DeviceManager.Instance.FindDevice("kbd") as VmKeyboardDevice;
                int keyWaitMs = 0;
                while (true)
                {
                    if (kbdDev2 != null && kbdDev2.HasKey())
                    {
                        byte key = kbdDev2.ReadKey();
                        if (key == 0x0D || key == 0x0A) Console.WriteLine();
                        else if (key >= 0x20 && key <= 0x7E) Console.Write((char)key);
                        registers[0] = key;
                        return;
                    }
                    if (!blocking) { registers[0] = 0; return; }
                    if (TimeoutSeconds > 0)
                    {
                        keyWaitMs += 10;
                        if (keyWaitMs >= TimeoutSeconds * 1000) { registers[0] = 0; return; }
                    }
                    System.Threading.Thread.Sleep(10);
                }
            }

            var kbdDev = DeviceManager.Instance.FindDevice("kbd") as VmKeyboardDevice;
            if (kbdDev != null && kbdDev.HasKey())
            {
                registers[0] = kbdDev.ReadKey();
                return;
            }

            if (AutoInput)
            {
                registers[0] = 0x0A;
                return;
            }

            char ch;
            int waitMs = 0;
            while (true)
            {
                if (_consoleIO != null && _consoleIO.KeyAvailable())
                {
                    ch = _consoleIO.ReadChar();
                    break;
                }

                try
                {
                    if (Console.KeyAvailable)
                    {
                        ch = (char)Console.Read();
                        break;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Console input redirected
                    if (!blocking) { registers[0] = 0; return; }
                }

                if (!blocking) { registers[0] = 0; return; }

                // 超时保护: 与 KeyScript 循环一致
                if (TimeoutSeconds > 0)
                {
                    waitMs += 10;
                    if (waitMs >= TimeoutSeconds * 1000) { registers[0] = 0; return; }
                }

                System.Threading.Thread.Sleep(10);
            }
            registers[0] = ch;
        }

        /// <summary>
        /// 输出整数（R0 = 整数值）— 简化为 itoa + 逐字 OutputChar
        /// </summary>
        private void ExecuteSyscall6_OutputInt()
        {
            var val = registers[0];
            var str = val.ToString();
            foreach (char c in str)
                OutputChar(c);
            registers[0] = val;
        }

        /// <summary>
        /// 从键盘缓冲区读取一行（用于 KeyScript 模式），阻塞直到 Enter
        /// 回显字符到控制台，支持退格
        /// </summary>
        private string ReadLineFromKeyBuffer()
        {
            var kbdDev = DeviceManager.Instance.FindDevice("kbd") as VmKeyboardDevice;
            var chars = new List<char>();
            while (true)
            {
                byte keyByte = 0;
                // Blocking read from keyboard buffer
                if (kbdDev != null)
                {
                    while (!kbdDev.HasKey())
                        System.Threading.Thread.Sleep(10);
                    keyByte = kbdDev.ReadKey();
                }

                if (keyByte == 0x0D || keyByte == 0x0A) // Enter
                {
                    Console.WriteLine(); // echo newline
                    break;
                }
                else if (keyByte == 0x08) // Backspace
                {
                    if (chars.Count > 0)
                    {
                        chars.RemoveAt(chars.Count - 1);
                        Console.Write("\b \b"); // erase last char
                    }
                }
                else if (keyByte >= 0x20 && keyByte <= 0x7E) // Printable ASCII
                {
                    chars.Add((char)keyByte);
                    Console.Write((char)keyByte); // echo
                }
                // Ignore other control chars (tab, escape, etc.)
            }
            return new string(chars.ToArray());
        }

        /// <summary>
        /// 输入整数（从控制台或键盘缓冲区读取整数到R0）
        /// </summary>
        private void ExecuteSyscall7_InputInt()
        {
            int val;

            // KeyScript mode: read from keyboard buffer (supports key injection)
            if (KeyScriptActive)
            {
                var input = ReadLineFromKeyBuffer();
                if (!int.TryParse(input, out val))
                    val = 0;
                registers[0] = val;
                return;
            }

            // 使用控制台接口或默认控制台
            if (_consoleIO != null)
            {
                val = _consoleIO.ReadInt();
            }
            else
            {
                var input = Console.ReadLine();
                if (!int.TryParse(input, out val))
                {
                    val = 0;
                }
            }

            registers[0] = val;
        }

        /// <summary>
        /// 输出浮点数（F0 = 浮点数值）
        /// </summary>
        // v1.65.213: 统一走OutputChar — 所有输出原语收敛为单一字符输出
        private void ExecuteSyscall8_OutputFloat()
        {
            foreach (char c in floatRegisters[0].ToString())
                OutputChar(c);
        }

        private void ExecuteSyscall9_InputFloat()
        {
            float val;

            // KeyScript mode: read from keyboard buffer (supports key injection)
            if (KeyScriptActive)
            {
                var input = ReadLineFromKeyBuffer();
                if (!float.TryParse(input, out val))
                    val = 0.0f;
                floatRegisters[0] = val;
                return;
            }

            if (_consoleIO != null)
            {
                val = _consoleIO.ReadFloat();
            }
            else
            {
                var input = Console.ReadLine();
                if (!float.TryParse(input, out val))
                {
                    val = 0.0f;
                }
            }

            floatRegisters[0] = val;
        }

        private void ExecuteDeviceOpen()
        {
            // R0: 设备名称地址
            // R1: 返回设备句柄
            var nameAddress = registers[0];
            
            // 参数验证
            if (nameAddress < 0 || nameAddress >= memory.Length)
            {
                registers[0] = ErrorCodes.INVALID_PARAMETER;
                return;
            }
            
            // 从内存中读取设备名称
            var nameBytes = new List<byte>();

            while (nameAddress < memory.Length && memory[nameAddress] != 0)
            {
                nameBytes.Add(memory[nameAddress]);
                nameAddress++;
            }

            var deviceName = System.Text.Encoding.UTF8.GetString(nameBytes.ToArray());
            
            // 设备名验证
            if (string.IsNullOrEmpty(deviceName))
            {
                registers[0] = ErrorCodes.INVALID_PARAMETER;
                return;
            }

            // 打开设备
            var handle = _deviceManager.OpenDevice(deviceName);
            
            // 错误处理
            if (handle < 0)
            {
                registers[0] = ErrorCodes.DEVICE_NOT_FOUND;
                return;
            }

            registers[0] = handle; // 返回设备句柄
            registers[1] = handle; // 返回设备句柄
        }

        /// <summary>
        /// 关闭设备
        /// </summary>
        private void ExecuteDeviceClose()
        {
            // R0: 设备句柄
            var handle = registers[0];
            
            // 参数验证
            if (handle < 0)
            {
                registers[0] = ErrorCodes.INVALID_DEVICE_HANDLE;
                return;
            }

            var success = _deviceManager.CloseDevice(handle);
            registers[0] = success ? ErrorCodes.SUCCESS : ErrorCodes.DEVICE_ERROR; // 返回执行结果
        }

        private void ExecuteDeviceRead()
        {
            // R0: 设备句柄
            // R1: 数据缓冲区地址
            // R2: 要读取的字节数
            // R0: 返回实际读取的字节数
            int handle        = registers[0];
            int bufferAddress = registers[1];
            int count         = registers[2];

            // 参数验证
            if (handle < 0)
            {
                registers[0] = ErrorCodes.INVALID_DEVICE_HANDLE;
                return;
            }
            
            if (count <= 0)
            {
                registers[0] = ErrorCodes.INVALID_PARAMETER;
                return;
            }
            
            if (bufferAddress < 0 || bufferAddress + count > memory.Length)
            {
                registers[0] = ErrorCodes.MEMORY_OUT_OF_BOUNDS;
                return;
            }

            var buffer    = new byte[count];
            var bytesRead = _deviceManager.ReadDevice(handle, buffer, 0, count);

            // 错误处理
            if (bytesRead < 0)
            {
                registers[0] = ErrorCodes.DEVICE_ERROR;
                return;
            }

            if (bytesRead > 0)
            {
                // 将读取的数据复制到内存
                Array.Copy(buffer, 0, memory, bufferAddress, bytesRead);
            }

            registers[0] = bytesRead;
        }

        /// <summary>
        /// 写入数据到设备
        /// </summary>
        private void ExecuteDeviceWrite()
        {
            // R0: 设备句柄
            // R1: 数据缓冲区地址
            // R2: 要写入的字节数
            // R0: 返回实际写入的字节数
            int handle        = registers[0];
            int bufferAddress = registers[1];
            int count         = registers[2];

            // 参数验证
            if (handle < 0)
            {
                registers[0] = ErrorCodes.INVALID_DEVICE_HANDLE;
                return;
            }
            
            if (count <= 0)
            {
                registers[0] = ErrorCodes.INVALID_PARAMETER;
                return;
            }
            
            if (bufferAddress < 0 || bufferAddress + count > memory.Length)
            {
                registers[0] = ErrorCodes.MEMORY_OUT_OF_BOUNDS;
                return;
            }

            var buffer = new byte[count];
            Array.Copy(memory, bufferAddress, buffer, 0, count);

            var bytesWritten = _deviceManager.WriteDevice(handle, buffer, 0, count);
            
            // 错误处理
            if (bytesWritten < 0)
            {
                registers[0] = ErrorCodes.DEVICE_ERROR;
                return;
            }

            registers[0] = bytesWritten;
        }

        private void ExecuteDeviceControl()
        {
            // R0: 设备句柄
            // R1: 控制命令
            // R2: 命令数据地址
            // R3: 数据长度
            // R0: 返回执行结果
            int handle      = registers[0];
            int command     = registers[1];
            int dataAddress = registers[2];
            int dataLength  = registers[3];

            // 参数验证
            if (handle < 0)
            {
                registers[0] = ErrorCodes.INVALID_DEVICE_HANDLE;
                return;
            }
            
            if (dataLength < 0)
            {
                registers[0] = ErrorCodes.INVALID_PARAMETER;
                return;
            }
            
            if (dataLength > 0 && (dataAddress < 0 || dataAddress + dataLength > memory.Length))
            {
                registers[0] = ErrorCodes.MEMORY_OUT_OF_BOUNDS;
                return;
            }

            byte[] data = null;
            if (dataLength > 0 && dataAddress >= 0 && dataAddress + dataLength <= memory.Length)
            {
                data = new byte[dataLength];
                Array.Copy(memory, dataAddress, data, 0, dataLength);
            }

            int result = _deviceManager.ControlDevice(handle, command, data);

            // 将数据拷回 VML 内存（支持双向数据交换, 如 GetCursor / TCGETATTR）
            if (data != null && dataLength > 0)
            {
                Array.Copy(data, 0, memory, dataAddress, dataLength);
            }

            // 错误处理
            if (result < 0)
            {
                registers[0] = ErrorCodes.DEVICE_ERROR;
                return;
            }

            registers[0] = result;
        }
        
        /// <summary>
        /// 打开文件
        /// SYSCALL 110: 打开文件
        /// R0: 文件名地址
        /// R1: 打开模式 (0=只读, 1=只写, 2=读写)
        /// 返回: 文件句柄 (>=0) 或错误码 (<0)
        /// </summary>
        private void ExecuteFileOpen()
        {
            try
            {
                var fileNameAddr = registers[0];
                var mode = registers[1];
                
                if (TraceMode) Console.WriteLine($"DEBUG ExecuteFileOpen: 文件名地址: 0x{fileNameAddr:X} ({fileNameAddr}), 模式: {mode}");
                
                // 从内存读取文件名
                string fileName = ReadStringFromMemory(fileNameAddr);
                if (TraceMode) System.Diagnostics.Debug.WriteLine($"ExecuteFileOpen: 文件名: '{fileName}'");
                
                // 根据模式打开文件
                FileMode fileMode;
                FileAccess fileAccess;
                
                switch (mode)
                {
                    case 0: // 只读
                        fileMode = FileMode.Open;
                        fileAccess = FileAccess.Read;
                        break;
                    case 1: // 只写
                        fileMode = FileMode.Create;
                        fileAccess = FileAccess.Write;
                        break;
                    case 2: // 读写
                        fileMode = FileMode.OpenOrCreate;
                        fileAccess = FileAccess.ReadWrite;
                        break;
                    default:
                        registers[0] = ErrorCodes.INVALID_PARAMETER; // 无效模式
                        return;
                }
                
                // 打开文件
                if (TraceMode) Console.WriteLine($"DEBUG ExecuteFileOpen: 文件模式: {fileMode}, 访问权限: {fileAccess}");
                try
                {
                    var stream = new FileStream(fileName, fileMode, fileAccess);
                    int handle = _fileHandles.IndexOf(null);
                    if (handle == -1) { handle = _fileHandles.Count; _fileHandles.Add(stream); }
                    else _fileHandles[handle] = stream;
                    if (TraceMode) Console.WriteLine($"DEBUG ExecuteFileOpen: 文件打开成功，句柄: {handle}");
                    registers[0] = handle;
                }
                catch (FileNotFoundException)
                {
                    if (TraceMode) Console.WriteLine($"DEBUG ExecuteFileOpen: 文件不存在");
                    registers[0] = ErrorCodes.FILE_NOT_FOUND;
                }
                catch (UnauthorizedAccessException)
                {
                    if (TraceMode) Console.WriteLine($"DEBUG ExecuteFileOpen: 文件访问被拒绝");
                    registers[0] = ErrorCodes.FILE_ACCESS_DENIED;
                }
                catch (IOException)
                {
                    registers[0] = ErrorCodes.FILE_FULL;
                }
                catch (Exception ex)
                {
                    if (TraceMode) Console.WriteLine($"DEBUG ExecuteFileOpen: 文件打开失败: {ex.Message}");
                    registers[0] = ErrorCodes.FAILURE; // 通用失败
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"文件打开错误: {ex.Message}");
                registers[0] = ErrorCodes.INTERNAL_ERROR;
            }
        }
        
        /// <summary>
        /// 关闭文件
        /// SYSCALL 111: 关闭文件
        /// R0: 文件句柄
        /// 返回: 0=成功, -1=失败
        /// </summary>
        private void ExecuteFileClose()
        {
            try
            {
                var handle = registers[0];
                if (TraceMode) Console.WriteLine($"DEBUG ExecuteFileClose: 句柄: {handle}, 文件句柄数量: {_fileHandles.Count}");
                
                if (handle >= 0 && handle < _fileHandles.Count && _fileHandles[handle] != null)
                {
                    if (TraceMode) Console.WriteLine($"DEBUG ExecuteFileClose: 关闭文件句柄 {handle}");
                    _fileHandles[handle].Close();
                    _fileHandles[handle] = null;
                    // Trim trailing null entries to prevent unbounded list growth
                    while (_fileHandles.Count > 0 && _fileHandles[_fileHandles.Count - 1] == null)
                        _fileHandles.RemoveAt(_fileHandles.Count - 1);
                    registers[0] = 0;
                    if (TraceMode) Console.WriteLine($"DEBUG ExecuteFileClose: 关闭成功");
                }
                else
                {
                    if (TraceMode) Console.WriteLine($"DEBUG ExecuteFileClose: 无效句柄");
                    registers[0] = ErrorCodes.INVALID_FILE_HANDLE;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"文件关闭错误: {ex.Message}");
                registers[0] = ErrorCodes.FILE_CLOSE_ERROR;
            }
        }
        
        /// <summary>
        /// 读取文件
        /// SYSCALL 112: 读取文件
        /// R0: 文件句柄
        /// R1: 缓冲区地址
        /// R2: 要读取的字节数
        /// 返回: 实际读取的字节数 (>=0) 或错误码 (<0)
        /// </summary>
        private void ExecuteFileRead()
        {
            try
            {
                var handle = registers[0];
                var bufferAddr = registers[1];
                var count = registers[2];
                
                // 参数验证
                if (handle < 0 || handle >= _fileHandles.Count || _fileHandles[handle] == null)
                {
                    registers[0] = ErrorCodes.INVALID_FILE_HANDLE;
                    return;
                }
                
                if (count <= 0)
                {
                    registers[0] = ErrorCodes.INVALID_PARAMETER;
                    return;
                }
                
                if (bufferAddr < 0 || bufferAddr + count > memory.Length)
                {
                    registers[0] = ErrorCodes.MEMORY_OUT_OF_BOUNDS;
                    return;
                }
                
                var stream = _fileHandles[handle];
                byte[] buffer = new byte[count];
                int bytesRead = stream.Read(buffer, 0, count);
                if (bytesRead > 0)
                    Array.Copy(buffer, 0, memory, bufferAddr, bytesRead);
                
                registers[0] = bytesRead;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"文件读取错误: {ex.Message}");
                registers[0] = ErrorCodes.FILE_READ_ERROR;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"文件读取错误: {ex.Message}");
                registers[0] = ErrorCodes.INTERNAL_ERROR;
            }
        }
        
        /// <summary>
        /// 写入文件
        /// SYSCALL 113: 写入文件
        /// R0: 文件句柄
        /// R1: 数据地址
        /// R2: 要写入的字节数
        /// 返回: 实际写入的字节数 (>=0) 或错误码 (<0)
        /// </summary>
        private void ExecuteFileWrite()
        {
            try
            {
                var handle = registers[0];
                var dataAddr = registers[1];
                var count = registers[2];
                
                if (TraceMode) Console.WriteLine($"DEBUG ExecuteFileWrite: 句柄: {handle}, 数据地址: 0x{dataAddr:X}, 字节数: {count}");
                if (TraceMode) Console.WriteLine($"DEBUG ExecuteFileWrite: 文件句柄数量: {_fileHandles.Count}");
                
                // 参数验证
                if (handle < 0 || handle >= _fileHandles.Count || _fileHandles[handle] == null)
                {
                    if (TraceMode) Console.WriteLine($"DEBUG ExecuteFileWrite: 无效句柄");
                    registers[0] = ErrorCodes.INVALID_FILE_HANDLE;
                    return;
                }
                
                if (count <= 0)
                {
                    registers[0] = ErrorCodes.INVALID_PARAMETER;
                    return;
                }
                
                if (dataAddr < 0 || dataAddr + count > memory.Length)
                {
                    registers[0] = ErrorCodes.MEMORY_OUT_OF_BOUNDS;
                    return;
                }
                
                var stream = _fileHandles[handle];
                byte[] buffer = new byte[count];
                Array.Copy(memory, dataAddr, buffer, 0, count);
                
                stream.Write(buffer, 0, count);
                if (TraceMode) Console.WriteLine($"DEBUG ExecuteFileWrite: 写入成功，写入字节数: {count}");
                registers[0] = count;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"文件写入错误: {ex.Message}");
                registers[0] = ErrorCodes.FILE_WRITE_ERROR;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"文件写入错误: {ex.Message}");
                registers[0] = ErrorCodes.INTERNAL_ERROR;
            }
        }
        
        /// <summary>
        /// 控制文件
        /// SYSCALL 114: 控制文件
        /// R0: 文件句柄
        /// R1: 控制命令
        /// R2: 命令数据地址
        /// 返回: 执行结果 (>=0) 或错误码 (<0)
        /// </summary>
        private void ExecuteFileControl()
        {
            try
            {
                var handle = registers[0];
                var command = registers[1];
                var dataAddr = registers[2];
                
                // 参数验证
                if (handle < 0 || handle >= _fileHandles.Count || _fileHandles[handle] == null)
                {
                    registers[0] = ErrorCodes.INVALID_FILE_HANDLE;
                    return;
                }
                
                var stream = _fileHandles[handle];
                
                switch (command)
                {
                    case 0: // 获取文件大小
                        registers[0] = (int)stream.Length;
                        break;
                    case 1: // 设置文件位置
                        if (dataAddr >= 0)
                        {
                            stream.Seek(dataAddr, SeekOrigin.Begin);
                            registers[0] = ErrorCodes.SUCCESS;
                        }
                        else
                        {
                            registers[0] = ErrorCodes.INVALID_PARAMETER;
                        }
                        break;
                    case 2: // 获取文件位置
                        registers[0] = (int)stream.Position;
                        break;
                    case 3: // 截断文件
                        if (dataAddr >= 0)
                        {
                            stream.SetLength(dataAddr);
                            registers[0] = ErrorCodes.SUCCESS;
                        }
                        else
                        {
                            registers[0] = ErrorCodes.INVALID_PARAMETER;
                        }
                        break;
                    default:
                        registers[0] = ErrorCodes.NOT_SUPPORTED; // 无效命令
                        break;
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"文件控制错误: {ex.Message}");
                registers[0] = ErrorCodes.FILE_CONTROL_ERROR;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"文件控制错误: {ex.Message}");
                registers[0] = ErrorCodes.INTERNAL_ERROR;
            }
        }
        
        /// <summary>
        /// 从内存读取字符串
        /// </summary>
        private string ReadStringFromMemory(int address)
        {
            if (address < 0 || address >= memory.Length)
                return string.Empty;
                
            int length = 0;
            while (address + length < memory.Length && memory[address + length] != 0)
            {
                length++;
            }
            
            if (length == 0)
                return string.Empty;
                
            return System.Text.Encoding.UTF8.GetString(memory, address, length);
        }
        
        // === v1.65.212: IO统一方法 (简化自8个独立方法) ===
        private void Syscall_PrintString() { int a=registers[0]; while(a<memory.Length&&memory[a]!=0) { registers[0]=memory[a]; ExecuteSyscall4_OutputChar(); a++; } }
        private void Syscall_ReadString() => ExecuteSyscall2_InputString();
        private void Syscall_PrintChar() => ExecuteSyscall4_OutputChar();
        private void Syscall_ReadChar() => ExecuteSyscall5_InputChar();
        private void Syscall_PrintInt() => ExecuteSyscall6_OutputInt();
        private void Syscall_ReadInt() => ExecuteSyscall7_InputInt();
        private void Syscall_PrintFloat() => ExecuteSyscall8_OutputFloat();
        private void Syscall_ReadFloat() => ExecuteSyscall9_InputFloat();
        private int AllocStr(string s) { s+="\0"; int a=AllocateMemory(s.Length); for(int i=0;i<s.Length;i++)memory[a+i]=(byte)s[i]; return a; }

        /// <summary>
        /// SYSCALL #59 — GetInfo(TypeId) → string_addr
        /// 返回系统/硬件/平台信息字符串。TypeId:
        ///   0=VM版本, 1=CPU架构, 2=内存映射(实时), 3=平台/OS,
        ///   4=用户名, 5=主机名, 6=平台-架构, 7=运行时,
        ///   8=MachineUUID, 9=MAC地址, 10=CPU型号, 11=设备标识,
        ///   12=网络接口列表, 13=CPU核心数, 14=进程ID
        /// </summary>
        private void ExecuteGetInfo()
        {
            int typeId = registers[0];

            // TypeId=2: 内存映射 — 全部使用运行时真实值
            if (typeId == 2)
            {
                int stackSize = _config?.StackSize ?? 0x10000;
                int heapBase = _config?.DataBase ?? memoryAllocPtr; // 运行时堆起始地址
                // 计算已分配字节 (从 memoryAllocations 字典累加)
                long totalAlloc = 0;
                foreach (var kv in memoryAllocations) totalAlloc += kv.Value;
                // 空闲块总大小
                long totalFree = 0;
                foreach (var kv in freeBlocks) totalFree += kv.Value;
                // 未使用的堆空间 = memorySize - stackSize - heapBase - totalAlloc
                long unusedHeap = memorySize - stackSize - heapBase - totalAlloc;
                if (unusedHeap < 0) unusedHeap = 0;
                string memInfo = $"RAM={memorySize}B({memorySize/1048576}MB) " +
                                 $"Stack={stackSize}B({stackSize/1024}KB) " +
                                 $"HeapBase=0x{heapBase:X} " +
                                 $"AllocPtr=0x{memoryAllocPtr:X} " +
                                 $"Used={totalAlloc}B({totalAlloc/1024}KB) " +
                                 $"Free={totalFree}B({totalFree/1024}KB) " +
                                 $"Avail≈{unusedHeap}B({unusedHeap/1024}KB)";
                registers[0] = AllocStr(memInfo);
                return;
            }

            string info = typeId switch
            {
                0 => $"VML v{typeof(VmRuntime).Assembly.GetName().Version?.ToString() ?? "?.?"}",
                1 => System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                3 => System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                4 => Environment.UserName,
                5 => Environment.MachineName,
                6 => $"{GetOsShortName()}-{System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}",
                7 => System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                8 => GetMachineUuid(),
                9 => GetMacAddress(),
                10 => GetCpuIdentifier(),
                11 => GetDeviceSerial(),
                12 => GetAllNetworkInfo(),
                13 => $"{Environment.ProcessorCount} cores / {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}",
                14 => $"{Environment.ProcessId}",
                _ => ""
            };
            registers[0] = string.IsNullOrEmpty(info) ? 0 : AllocStr(info);
        }

        private static string GetOsShortName()
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) return "windows";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)) return "macOS";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)) return "linux";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.FreeBSD)) return "freebsd";
            return "unknown";
        }

        /// <summary>读取 Machine UUID (跨平台)</summary>
        private static string GetMachineUuid()
        {
            try
            {
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
                {
                    using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "/usr/sbin/ioreg",
                        Arguments = "-d2 -c IOPlatformExpertDevice -r",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    if (p != null)
                    {
                        p.WaitForExit(3000);
                        string output = p.StandardOutput.ReadToEnd();
                        int idx = output.IndexOf("IOPlatformUUID");
                        if (idx >= 0)
                        {
                            int eq = output.IndexOf('=', idx);
                            if (eq >= 0)
                            {
                                int q1 = output.IndexOf('"', eq);
                                int q2 = q1 >= 0 ? output.IndexOf('"', q1 + 1) : -1;
                                if (q1 >= 0 && q2 > q1)
                                    return output.Substring(q1 + 1, q2 - q1 - 1);
                            }
                        }
                    }
                    return "macOS-uuid-not-found";
                }
                else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                {
                    if (File.Exists("/etc/machine-id"))
                        return File.ReadAllText("/etc/machine-id").Trim();
                    if (File.Exists("/sys/class/dmi/id/product_uuid"))
                        return File.ReadAllText("/sys/class/dmi/id/product_uuid").Trim();
                    return "linux-uuid-not-found";
                }
                else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                    if (key != null)
                    {
                        var guid = key.GetValue("MachineGuid")?.ToString();
                        key.Close();
                        if (!string.IsNullOrEmpty(guid)) return guid;
                    }
                    return "windows-uuid-not-found";
                }
            }
            catch { /* fall through */ }
            return "uuid-unavailable";
        }

        /// <summary>返回首个物理网卡的 MAC 地址</summary>
        private static string GetMacAddress()
        {
            try
            {
                var nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                foreach (var nic in nics)
                {
                    if (nic.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback
                        && nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                    {
                        return BitConverter.ToString(nic.GetPhysicalAddress().GetAddressBytes()).Replace('-', ':');
                    }
                }
                foreach (var nic in nics)
                {
                    if (nic.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                        return BitConverter.ToString(nic.GetPhysicalAddress().GetAddressBytes()).Replace('-', ':');
                }
            }
            catch { }
            return "00:00:00:00:00:00";
        }

        /// <summary>返回 CPU 标识符 (跨平台: 型号+架构+核心数)</summary>
        private static string GetCpuIdentifier()
        {
            var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
            int cores = Environment.ProcessorCount;
            try
            {
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
                {
                    using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "/usr/sbin/sysctl",
                        Arguments = "-n machdep.cpu.brand_string",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    if (p != null) { p.WaitForExit(2000); return $"{p.StandardOutput.ReadToEnd().Trim()} ({cores}c/{arch})"; }
                }
                else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                {
                    if (File.Exists("/proc/cpuinfo"))
                    {
                        foreach (var line in File.ReadAllLines("/proc/cpuinfo"))
                        {
                            if (line.StartsWith("model name", StringComparison.OrdinalIgnoreCase))
                            {
                                int colon = line.IndexOf(':');
                                if (colon >= 0) return $"{line.Substring(colon + 1).Trim()} ({cores}c/{arch})";
                            }
                        }
                    }
                }
            }
            catch { }
            return $"{arch} ({cores} cores)";
        }

        /// <summary>返回设备标识: platform-hostname-osversion</summary>
        private static string GetDeviceSerial()
        {
            try
            {
                return $"{GetOsShortName()}-{Environment.MachineName}-{Environment.OSVersion.VersionString}";
            }
            catch { return "unknown-device"; }
        }

        /// <summary>返回所有非回环网络接口 (名称:MAC:类型)</summary>
        private static string GetAllNetworkInfo()
        {
            try
            {
                var nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                var parts = new System.Collections.Generic.List<string>();
                foreach (var nic in nics)
                {
                    if (nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                        continue;
                    string mac = BitConverter.ToString(nic.GetPhysicalAddress().GetAddressBytes()).Replace('-', ':');
                    parts.Add($"{nic.Name}:{mac}:{nic.NetworkInterfaceType}");
                }
                return parts.Count > 0 ? string.Join("; ", parts) : "no-network";
            }
            catch { return "network-error"; }
        }

        public DeviceManager GetDeviceManager()
        {
            return _deviceManager;
        }

    }
}
