using VMLAssembler;
using System.IO;
using VMLRuntime.Device;

namespace VMLRuntime
{
    public partial class VmRuntime : IDisposable
    {
        private int[] registers;
        private byte[] memory;
        private int sp;
        private int pc;
        private int memorySize;

        /// <summary>
        /// 本次运行的**超时源**（`TimeoutSeconds &gt; 0` 时才建）。
        ///
        /// ⚠ 提成字段（原来是 <see cref="Run"/> 里的局部变量）只为让 <see cref="ResetTimeout"/>
        /// 够得着它 —— 宿主每次"阻塞等待"返回后都要把截止时间推后，见那里的说明。
        /// </summary>
        private CancellationTokenSource? _timeoutCts;

        /// <summary>
        /// 超时的**截止时刻**（`Environment.TickCount64` 口径，毫秒）。
        ///
        /// <para>
        /// `CancellationTokenSource` 查不到"还剩多久"，而状态面板要显示这个数 ——
        /// 排查"游戏卡死"那一轮全靠拿日志时间戳反推超时时刻才定下案，面板上直接显示
        /// 「超时剩 3s」就不必再推。每次 <see cref="Run"/> 起表、<see cref="ResetTimeout"/>
        /// 续期时一起更新。
        /// </para>
        /// </summary>
        private long _timeoutDeadlineMs;

        /// <summary>
        /// **初始栈顶**（`LoadProgram` 里定下来那一刻的 `sp`）。
        ///
        /// 状态面板拿它当基准算"栈用了多少"：程序跑起来之后 `sp` 一路往下压，
        /// 而"压了多少"只有跟**起点**比才有意义（拿 R13 单独一个值看不出任何东西）。
        /// </summary>
        private int _initialSp;

        /// <summary>
        /// 调用方传进来的取消令牌（用户按「强制停止」/宿主取消）。
        ///
        /// **只用来区分两种结束**：它被取消了 ⇒ 用户自己停的；它没被取消而超时源响了 ⇒ 超时。
        /// 两种都打同一句话的话，用户看到"VM execution cancelled"根本分不出是程序坏了还是被超时杀了
        /// （这正是"游戏卡死"那轮排查里最花时间的一段）。
        /// </summary>
        private CancellationToken _externalCt;

        public int[] Registers
        {
            get { return registers; }
        }

        public float[] FloatRegisters
        {
            get { return floatRegisters; }
        }

        public double[] DoubleRegisters
        {
            get { return doubleRegisters; }
        }

        public long[] LongRegisters
        {
            get { return longRegisters; }
        } // L0-L7 (R24-R31)

        public byte[] Memory
        {
            get { return memory; }
        }

        public int PC
        {
            get { return pc; }
        }

        public int ExitCode { get; private set; }
        private bool zf;
        private bool cf;
        private bool sf;
        /// <summary>
        /// **无符号借位**（`a &lt; b` 按无符号比）—— 只由 `CMPU`/`CMPUL` 置位，
        /// 只有 `JA`/`JB`/`JAE`/`JBE` 读它。
        ///
        /// ⚠ **刻意与 `cf` 分开**：`cf` 被 `JG`/`JGE` 当**溢出位**用
        /// （`JG = !zf &amp;&amp; sf == cf`），而 `CMP` 刻意不传 carry ⇒ `cf` 恒 false。
        /// 把 `cf` 改成真借位会让有符号跳转全错（`3 > 5` 也会跳），
        /// 所以无符号比较另起一位，两边互不干扰。
        /// </summary>
        private bool uf;

        public bool ZF
        {
            get { return zf; }
        }

        public bool CF
        {
            get { return cf; }
        }

        public bool SF
        {
            get { return sf; }
        }

        public bool FZF
        {
            get { return fzf; }
        }

        public bool FSF
        {
            get { return fsf; }
        }

        public bool FCF
        {
            get { return fcf; }
        }

        public bool FOF
        {
            get { return fof; }
        }

        public bool FUF
        {
            get { return fuf; }
        }

        public bool FDF
        {
            get { return fdf; }
        }

        public bool FIF
        {
            get { return fif; }
        }

        public bool FES
        {
            get { return fes; }
        }

        public bool FIE
        {
            get => fie;
            set => fie = value;
        }

        public bool FZE
        {
            get => fze;
            set => fze = value;
        }

        public bool FDE
        {
            get => fde;
            set => fde = value;
        }

        public bool FOE
        {
            get => foe;
            set => foe = value;
        }

        public bool FPE
        {
            get => fpe;
            set => fpe = value;
        }

        public bool FPF
        {
            get { return fpf; }
        }

        private float[] floatRegisters;
        private double[] doubleRegisters;
        private long[] longRegisters; // L0-L7 64位整数寄存器 (R24-R31)
        private Stack<float> floatStack;
        private Stack<double> doubleStack;
        private Stack<long> longStack;
        private int floatCmpResult;
        private bool fzf;
        private bool fsf;
        private bool fcf;
        private bool fof;
        private bool fuf;
        private bool fdf;
        private bool fif;
        private bool fie;
        private bool fze;
        private bool fde;
        private bool foe;
        private bool fpe;
        private bool fpf;
        private bool fes;
        private int privilegeLevel = 1; // 0=OS模式, 1=MCU模式(默认)

        /// <summary>
        /// 设置工作模式: 0=OS模式(所有SYSCALL可用), 1=MCU模式(限制SYSCALL)
        /// </summary>
        public int PrivilegeLevel
        {
            get => privilegeLevel;
            set => privilegeLevel = value;
        }

        private readonly DeviceManager _deviceManager;
        private ISystemCallHandler? _systemCallHandler;
        private IConsoleIO? _consoleIO;
        private readonly Dictionary<int, IntPtr> _nativeHandles = new();
        private readonly Dictionary<int, IntPtr> _nativeFuncPtrs = new();
        private int _nextNativeHandle = 1;
        private readonly List<byte> _utf8OutputBuffer = new(4);

        /// <summary>
        /// 输出字节流是不是**单字节老编码（CP437）**。
        ///
        /// <para>
        /// 开局是 <c>false</c>（按 UTF-8）。遇到**第一个不合法**的字节序列就置位，
        /// 此后一直按一个字节一个字符走 —— **不回头**。
        /// 为什么必须粘性、以及 `┌───┐` 那个 `C4 BF` 恰好是合法 UTF-8 的例子，
        /// 见 <c>VMLRuntime.Syscall.cs</c> 的 <c>TryFlushOneOutputUnit</c>。
        /// </para>
        /// </summary>
        private bool _legacyOutputEncoding;
        private static readonly HashSet<int> UserAllowedSyscalls = SyscallConstants.UserAllowed;
        public bool DebugMode { get; set; } = false;
        public bool TraceMode { get; set; } = false;
        public bool AutoInput { get; set; } = false;
        public bool KeyScriptActive { get; set; } = false;
        public int DebugScreenshotCounter { get; set; } = 0;
        public string DebugScreenshotPrefix { get; set; } = "debug_shot";
        public bool _interruptEnabled = true;
        private bool _nonRecoverableInterrupt;
        internal InterruptController? _interruptController;
        internal TimerInterruptSource? _timerSource;
        private Stack<CatchFrame> _catchStack = new();

        private struct CatchFrame
        {
            public int CatchAddress;
            public int FinallyAddress;

            public CatchFrame(int catchAddr, int finallyAddr)
            {
                CatchAddress = catchAddr;
                FinallyAddress = finallyAddr;
            }
        }

        public int StepLimit { get; set; } = 0;
        public int TimeoutSeconds { get; set; } = 30;
        private int _cpuSpeed = 0;
        private System.Diagnostics.Stopwatch _speedStopwatch = new();
        private long _speedBatchCount = 0;
        private const long SpeedBatchSize = 1000;
        private HashSet<int> breakpoints = new();
        public event Action<int>? OnBreakpoint;
        public event Action<string>? OnDebugLog;
        private VmlProgram? _program;
        private readonly Dictionary<string, int> labelAddresses;
        private readonly Dictionary<string, float> floatConstants;
        private int memoryAllocPtr = 0x400; // 从 IVT 后开始分配
        private Dictionary<int, int> memoryAllocations = new();
        private Dictionary<int, int> freeBlocks = new();

        public class MemoryRegion
        {
            public int Start { get; set; }
            public int Size { get; set; }
            public bool Readable { get; set; }
            public bool Writable { get; set; }
            public bool Executable { get; set; }
            public string Description { get; set; } = "";
        }

        internal List<MemoryRegion> memoryRegions = new();
        private static readonly int DefaultIvtSize = 0x400;
        private Random random = new Random();
        private int vgaStartAddress;
        private int vgaGraphicsAddress;
        private int vgaWidth;
        private int vgaHeight;
        private int vgaSize;
        private int vgaMode;
        public bool EnableLogging { get; set; } = false;
        public long InstructionsExecuted { get; private set; }
        public long SyscallsExecuted { get; private set; }
        private readonly DateTime _startTime = DateTime.UtcNow;
        private List<FileStream> _fileHandles = new();

        public ISystemCallHandler? SystemCallHandler
        {
            get => _systemCallHandler;
            set => _systemCallHandler = value;
        }

        public IConsoleIO? ConsoleIO
        {
            get => _consoleIO;
            set => _consoleIO = value;
        }

        /// <summary>宿主传进来的命令行参数（**不含** `argv[0]`；那一项由运行时补）</summary>
        public List<string> CommandLineArgs { get; set; } = new();

        /// <summary>
        /// 生效的 `argv`：**入口帧与 `#62`/`#63` 共用的唯一真源**。
        ///
        /// `[0]` 恒存在 —— 宿主没给参数时补一个（C 保证 `argc >= 1`，
        /// 而且老程序常拿 `argv[0]` 打用法/取程序名，给 NULL 就是替它们埋一个空指针）。
        /// 宿主给了参数时 `[0]` 由宿主决定（`vmlcli` 放源文件名）。
        ///
        /// **入口帧与 `#62`/`#63` 都读这一份** —— 两处各算一份就是
        /// "`main` 拿到的和 `ui_arg` 拿到的不是一回事"这种最难查的分叉。
        /// </summary>
        private List<string> _argvStrings = new();

        /// <summary>
        /// 初始化虚拟机运行时
        /// </summary>
        /// <param name="memorySize">内存大小（字节）</param>
        /// <param name="config">虚拟机配置</param>
        /// <param name="deviceWhitelist">设备白名单</param>
        /// <param name="mode">工作模式</param>
        public VmRuntime(int memorySize = 0, VmConfig? config = null, List<string>? deviceWhitelist = null, string mode = "mcu")
        {
            _config = config ?? new VmConfig();
            config = _config; // 确保局部变量非 null
            _cpuSpeed = _config.CpuSpeed;
            memoryAllocPtr = _config.DataBase; // 数据段起始地址 (IBM-PC: 0x8000)

            // 设置工作模式
            privilegeLevel = mode?.ToLower() == "os" ? 0 : 1;
            if (mode?.ToLower() == "os")
            {
                _interruptController = new InterruptController();
                _timerSource = new TimerInterruptSource(_interruptController, _config.TimerInterruptInterval);
                System.Diagnostics.Debug.WriteLine("[VMRuntime] OS mode: 中断系统激活, 所有系统调用可用");
            }
            else
                System.Diagnostics.Debug.WriteLine("[VMRuntime] MCU mode: 中断系统禁用, 限制系统调用");

            if (memorySize <= 0) memorySize = config.MemorySize;
            else memorySize = Math.Clamp(memorySize, 64 * 1024, int.MaxValue);
            vgaStartAddress = config.VgaStartAddress;
            vgaGraphicsAddress = 0xA0000; // VGA图形帧缓冲标准地址
            vgaWidth = config.VgaDisplay.Width;
            vgaHeight = config.VgaDisplay.Height;
            vgaMode = config.VgaDisplay.Mode;
            vgaSize = config.VgaDisplay.Mode switch
            {
                1 => vgaWidth * vgaHeight * 3,
                2 => vgaWidth * vgaHeight * 1,
                _ => vgaWidth * vgaHeight * config.VgaDisplay.BytesPerCell
            };
            registers = new int[16];
            memory = new byte[memorySize];
            this.memorySize = memorySize;
            // Stack: use config.StackTop if set, else top of memory
            if (_config != null && _config.StackTop != 0)
                sp = _config.StackTop;
            else
                sp = memorySize - 4;
            registers[12] = sp;
            registers[13] = sp;
            registers[14] = sp;
            // Program start: use config.ProgramStart if set, else 0
            pc = _config?.ProgramStart ?? 0;
            zf = false;
            cf = false;
            sf = false;
            labelAddresses = new Dictionary<string, int>();
            floatConstants = new Dictionary<string, float>();
            floatRegisters = new float[16];
            doubleRegisters = new double[8];
            longRegisters = new long[8]; // L0-L7 (R24-R31)
            floatStack = new Stack<float>();
            doubleStack = new Stack<double>();
            longStack = new Stack<long>();
            floatCmpResult = 0;
            fzf = false;
            fsf = false;
            fcf = false;
            fof = false;
            fuf = false;
            fdf = false;
            fif = false;
            fes = false;
            fie = false;
            fze = false;
            fde = false;
            foe = false;
            fpe = false;
            fpf = false;
            _deviceManager = DeviceManager.Instance;

            // 根据型号白名单决定初始化哪些设备
            bool HasDevice(string name) => deviceWhitelist == null || deviceWhitelist.Contains(name, StringComparer.OrdinalIgnoreCase);

            if (HasDevice("vga"))
            {
                var bytesPerCell = config.VgaDisplay.Mode == 0 ? config.VgaDisplay.BytesPerCell : 1;
                var vgaDevice = new VmDisplayDevice(vgaWidth, vgaHeight, vgaMode, bytesPerCell);
                _deviceManager.RegisterOrReplaceDevice("vga", vgaDevice);
                vgaDevice.Open();
                VmDisplayDevice.InitDefaultPalette(memory);
                VmDisplayDevice.InitDefaultFont(memory);
            }
            else if (deviceWhitelist != null)
            {
                // 不在白名单中时取消注册，防止旧 VGA 设备拦截内存访问
                _deviceManager.UnregisterDevice("vga");
            }

            if (HasDevice("kbd"))
            {
                var kbdDevice = _deviceManager.FindDevice("kbd") as VmKeyboardDevice;
                if (kbdDevice == null)
                {
                    kbdDevice = new VmKeyboardDevice();
                    _deviceManager.RegisterDevice("kbd", kbdDevice);
                }

                // 计算状态寄存器偏移 (Apple II: 0xC010-0xC000=0x10, PC: 0x64-0x60=4)
                uint statusOffset = (uint)(config.KeyboardStatusAddress - config.KeyboardDataAddress);
                if (statusOffset == 0 || statusOffset > 0x20) statusOffset = 4;
                kbdDevice.SetMmioBase((uint)config.KeyboardDataAddress, statusOffset);
                _deviceManager.RegisterMmio(kbdDevice);
            }
            else if (deviceWhitelist != null)
            {
                _deviceManager.UnregisterDevice("kbd");
            }

            if (HasDevice("mouse"))
            {
                var mouseDevice = _deviceManager.FindDevice("mouse") as VmMouseDevice;
                if (mouseDevice == null)
                {
                    mouseDevice = new VmMouseDevice();
                    _deviceManager.RegisterDevice("mouse", mouseDevice);
                }

                mouseDevice.SetMmioBase((uint)config.MouseXAddress);
                _deviceManager.RegisterMmio(mouseDevice);
            }
            else if (deviceWhitelist != null)
            {
                _deviceManager.UnregisterDevice("mouse");
            }

            if (HasDevice("speaker"))
            {
                var spkDevice = _deviceManager.FindDevice("speaker") as VmSpeakerDevice;
                if (spkDevice == null)
                {
                    spkDevice = new VmSpeakerDevice();
                    _deviceManager.RegisterDevice("speaker", spkDevice);
                }

                spkDevice.Open();
            }

            if (HasDevice("printer"))
            {
                var prnDevice = _deviceManager.FindDevice("printer") as VmPrinterDevice;
                if (prnDevice == null)
                {
                    prnDevice = new VmPrinterDevice();
                    _deviceManager.RegisterDevice("printer", prnDevice);
                }

                prnDevice.Open();
            }
        }

        /// <summary>
        /// 加载VML程序
        /// </summary>
        /// <param name="prog">VML程序</param>
        public void LoadProgram(VmlProgram? prog)
        {
            if (prog == null)
            {
                Console.WriteLine("VML程序为空");
                return;
            }

            this._program = prog;
            if (prog.CpuSpeed > 0)
                _cpuSpeed = prog.CpuSpeed;
            PreDecodeMemoryOperands();
            ApplyDefaultMemoryProtection();

            // 处理 .skip 区域：标记为已分配，防止堆分配器覆盖寄存器/字库等固定地址
            foreach (var (addr, size) in _program.SkipRegions)
            {
                ReserveMemoryRegion(addr, size);
            }

            labelAddresses.Clear();
            foreach (var label in _program.Labels)
                labelAddresses[label.Key] = label.Value;
            /* ⚠ **标签引用的延迟解析表** —— 见循环末尾的补填那一段。
               数据段是**按遍历序**逐个分配内存的，而某个 `LabelRef` 指向的标签
               可能在**后面**才登记地址（`labelAddresses[data.Key] = address`
               在循环末尾）。顺序凑巧对的时候没事，但 `DataSection` 是字典，
               **不该依赖遍历序**。 */
            var deferredLabelRefs = new List<(int Address, LabelRef Ref)>();

            foreach (var data in _program.DataSection)
            {
                int address;
                if (data.Value is int intValue)
                {
                    address = AllocateMemory(4);
                    SetMemory(address, intValue);
                }
                else if (data.Value is LabelRef singleLabelRef)
                {
                    /* 单个**标签引用** —— `T *p = &x;` 这类全局指针的初始化。
                       与数组那条（`ResolveDataElement`）同一语义：写进去的是
                       **那个标签的地址**。
                       ⚠ 不加这一支它会落到最后的 else（"未初始化 ⇒ 0"）——
                       实测 `WINDOW *stdscr = &sc_win;` 因此恒为 NULL，
                       而 `stdscr` 是老程序最常用的全局对象。 */
                    address = AllocateMemory(4);
                    /* 查得到就当场填；**查不到就记下来稍后补** ——
                       它指向的标签可能在后面才登记地址（见 `deferredLabelRefs`）。 */
                    if (labelAddresses.TryGetValue(singleLabelRef.Name, out int lrAddr))
                        SetMemory(address, lrAddr);
                    else
                        deferredLabelRefs.Add((address, singleLabelRef));
                }
                else if (data.Value is int[] intArray)
                {
                    address = AllocateMemory(intArray.Length * 4);
                    for (var i = 0; i < intArray.Length; i++)
                    {
                        var bytes = BitConverter.GetBytes(intArray[i]);
                        for (var j = 0; j < 4; j++)
                            memory[address + i * 4 + j] = bytes[j];
                    }
                }
                /* 1 / 2 字节元素的数组：按**真实宽度**分配与铺字节。
                   ⚠ 少了这两支，`byte[]`/`short[]` 会掉到最后那个 `else`
                   （`ToString()` 当字符串写）—— 而 C 前端的下标算术是按 1/2 字节走的，
                   两边必须同源（判据 `scripts/vml-c-probe/cases/39-array-elem-width.c`）。 */
                else if (data.Value is byte[] byteArray)
                {
                    address = AllocateMemory(byteArray.Length);
                    for (var i = 0; i < byteArray.Length; i++)
                        memory[address + i] = byteArray[i];
                }
                else if (data.Value is short[] shortArray)
                {
                    address = AllocateMemory(shortArray.Length * 2);
                    for (var i = 0; i < shortArray.Length; i++)
                    {
                        var bytes = BitConverter.GetBytes(shortArray[i]);
                        memory[address + i * 2] = bytes[0];
                        memory[address + i * 2 + 1] = bytes[1];
                    }
                }
                else if (data.Value is object[] objArray)
                {
                    /* 对象数组：每个元素 4 字节，标签型元素写**标签地址**。
                       ⚠ **标签型元素必须和上面那条单个 `LabelRef` 一样延后补填**：
                         数据段是按遍历序逐个分配、并在**循环末尾**才 `labelAddresses[key] = address`，
                         而数组元素指向的标签（`char *rows[] = {"abc","def"}` 里的字符串、
                         多级指针表）完全可能排在**后面**才登记 ⇒ 边遍历边解析就读到 0。
                         实测：函数内 `static char *stat[2] = {"aa","bb"};` 两项恒 NULL，
                         而顶层同样形状的 `g[2]` 恰好顺序凑巧、正常 ——
                         「同一个形状，放函数里就坏、放顶层就好」正是**遍历序**的指纹。
                         判据 `scripts/vml-c-probe/cases/34-static-local-array.c`。 */
                    address = AllocateMemory(objArray.Length * 4);
                    for (var i = 0; i < objArray.Length; i++)
                    {
                        int slot = address + i * 4;
                        string? refName = objArray[i] switch
                        {
                            LabelRef lr => lr.Name,
                            string s when s.Length > 0 && !int.TryParse(s, out _) => s,
                            _ => null
                        };
                        if (refName is not null)
                            deferredLabelRefs.Add((slot, new LabelRef(refName)));
                        else
                            SetMemory(slot, ResolveDataElement(objArray[i]));
                    }
                }
                else if (data.Value is float floatValue)
                {
                    address = AllocateMemory(4);
                    var bytes = BitConverter.GetBytes(floatValue);
                    for (var i = 0; i < 4; i++) memory[address + i] = bytes[i];
                }
                else if (data.Value is double doubleValue)
                {
                    address = AllocateMemory(8);
                    var bytes = BitConverter.GetBytes(doubleValue);
                    for (var i = 0; i < 8; i++) memory[address + i] = bytes[i];
                }
                else if (data.Value is long longValue)
                {
                    address = AllocateMemory(8);
                    var bytes = BitConverter.GetBytes(longValue);
                    for (var i = 0; i < 8; i++) memory[address + i] = bytes[i];
                }
                else if (data.Value is DataString ds)
                {
                    // v1.65.171+: 根据 StringWidth 选择编码
                    if (ds.Width == StringWidth.Wide)
                    {
                        // UTF-16LE — 每个字符 2 字节，小端序
                        byte[] utf16Bytes = System.Text.Encoding.Unicode.GetBytes(ds.Value);
                        address = AllocateMemory(utf16Bytes.Length + 2);
                        for (var i = 0; i < utf16Bytes.Length; i++)
                            memory[address + i] = utf16Bytes[i];
                        memory[address + utf16Bytes.Length] = 0;
                        memory[address + utf16Bytes.Length + 1] = 0;
                    }
                    else if (ds.Width == StringWidth.Unicode)
                    {
                        // UTF-32LE — 每个字符 4 字节，小端序
                        byte[] utf32Bytes = System.Text.Encoding.UTF32.GetBytes(ds.Value);
                        address = AllocateMemory(utf32Bytes.Length + 4);
                        for (var i = 0; i < utf32Bytes.Length; i++)
                            memory[address + i] = utf32Bytes[i];
                        memory[address + utf32Bytes.Length] = 0;
                        memory[address + utf32Bytes.Length + 1] = 0;
                        memory[address + utf32Bytes.Length + 2] = 0;
                        memory[address + utf32Bytes.Length + 3] = 0;
                    }
                    else
                    {
                        // Char=8 — 使用 UTF-8
                        byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(ds.Value);
                        address = AllocateMemory(utf8Bytes.Length + 1);
                        for (var i = 0; i < utf8Bytes.Length; i++)
                            memory[address + i] = utf8Bytes[i];
                        memory[address + utf8Bytes.Length] = 0;
                    }
                }
                else if (data.Value is string strValue)
                {
                    byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(strValue);
                    address = AllocateMemory(utf8Bytes.Length + 1);
                    for (var i = 0; i < utf8Bytes.Length; i++)
                        memory[address + i] = utf8Bytes[i];
                    memory[address + utf8Bytes.Length] = 0;
                }
                else
                {
                    // 统一使用 UTF-8 编码存储 (避免 (byte)char 截断多字节字符)
                    var valueStr = data.Value.ToString() ?? "";
                    byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(valueStr);
                    address = AllocateMemory(utf8Bytes.Length + 1);
                    for (var i = 0; i < utf8Bytes.Length; i++)
                        memory[address + i] = utf8Bytes[i];
                    memory[address + utf8Bytes.Length] = 0;
                }

                // 数据段标签优先使用堆分配地址 (覆盖 code labels 中的同名字段)
                labelAddresses[data.Key] = address;
            }

            /* **补填延迟的标签引用** —— 走到这里，**所有**数据段槽位的地址都已
               登记进 `labelAddresses`，不再受 `DataSection` 遍历序的影响。
               （这就是"两趟"的等效做法：第一趟分配+登记，第二趟才解析跨槽位引用。） */
            foreach (var (refAddr, lr) in deferredLabelRefs)
            {
                int resolved = labelAddresses.TryGetValue(lr.Name, out int v) ? v : 0;
                SetMemory(refAddr, resolved);
            }

            // SP: prefer VML .stack directive, then config.StackTop, then config.StackSize, then 640K
            if (_program.StackTop != 0)
            {
                sp = _program.StackTop;
            }
            else if (_config != null && _config.StackTop != 0)
            {
                sp = _config.StackTop;
            }
            else if (_config != null && _config.StackSize > 0)
            {
                sp = _config.StackSize; // use stack size as stack top (e.g. 64K)
            }
            else
            {
                sp = 640 * 1024; // fallback default
            }

            /* ⚠⚠ **栈顶必须落在堆高水位之上** —— 数据段是逐个 `AllocateMemory` 分配的，
               堆从 `DataBase`（0x400）往上长，而上面那几条给的是**栈顶**（栈往下长）。
               两者重叠时最先脏掉的是**帧上方那几个槽**：C 的调用约定把参数放在
               `[R12+12]`/`[R12+16]`，而 `R12` 就在初始 SP 之下 —— 于是 `main` 的
               `argc`/`argv` 恰好落在初始 SP **之上**，被堆的数据写脏。
               实测（`sl`，heapTop=67342 > sp=65536）：
                 · `argc` 读出 **897988541** —— 那是 0x358637BD，某个浮点常量 `1e-6` 的位模式；
                 · `argv[1]` 指向数据段里的一段字符串，于是
                   `for (i = 1; i < argc; ++i) if (*argv[i] == '-')` **一进循环就崩**
                   （报"内存越界，地址＝MK1\r"这类**字符串正文**当地址的错误）。
               症状之所以难认：**同一个程序在小数据量下完全正常**（那个最小例 heapTop=22746），
               只有"数据段 + 堆超过 64K"的老程序才翻车 —— 而 `.stack 0`（C 前端默认发的就是它）
               会退到 `config.StackSize`（**64K**），越过它一点也不难
               （`sl` 的全部图案表 + curses 的 300×25 双缓冲 + 库自己的那些缓冲区）。
               判据 `scripts/vml-c-probe/cases/43-stack-heap-collision.c`。 */
            if (sp <= memoryAllocPtr)
                sp = memory.Length - 4;   // 退到内存顶端（构造函数本来就用这个默认值）

            // 记下**初始栈顶** —— 状态面板要拿它当"栈用了多少"的基准（`StackUsedBytes`）。
            // 程序跑起来之后 `sp` 一路往下压，而"压了多少"只有跟起点比才有意义。
            _initialSp = sp;

            registers[12] = sp;
            registers[13] = sp;
            registers[14] = sp;

            if (_program.EntryPoint != null && labelAddresses.ContainsKey(_program.EntryPoint))
            {
                pc = labelAddresses[_program.EntryPoint];
            }
            else
            {
                pc = 0;
            }
        }

        /// <summary>
        /// 解析数据段元素值：整数直接返回，字符串解析为标签地址
        /// </summary>
        private int ResolveDataElement(object element)
        {
            if (element is int intVal)
                return intVal;
            /* **标签引用**（`.word <标签名>`，即 `T *p = &x;` 落成的形态）——
               与下面那条 `string` 分支的"解析为标签地址"是**同一件事**，
               只是这个形态**明确知道自己是标签**（不必先试 int.Parse）。
               ⚠ 不加这一支它会落到函数末尾的 `return 0` —— 实测那让
               `char *rows[] = {"abc","def"}` 的元素全变 NULL（回归），
               也让 `WINDOW *stdscr = &sc_win;` 恒为 NULL。 */
            if (element is LabelRef labelRef)
                return labelAddresses.TryGetValue(labelRef.Name, out int lrAddr) ? lrAddr : 0;
            if (element is string strVal)
            {
                // 先尝试解析为整数
                if (int.TryParse(strVal, out int parsed))
                    return parsed;
                // 尝试解析为标签地址
                if (labelAddresses.TryGetValue(strVal, out int addr))
                    return addr;
                return 0;
            }

            if (element is float floatVal)
                return BitConverter.SingleToInt32Bits(floatVal);
            return 0;
        }

        public void LoadVmbProgram(byte[] vmbData)
        {
            var program = VmlProgram.FromVmbBytes(vmbData);
            LoadProgram(program);
        }

        public void LoadVmbProgramFromFile(string filePath)
        {
            var data = File.ReadAllBytes(filePath);
            LoadVmbProgram(data);
        }

        public void Run(CancellationToken cancellationToken = default)
        {
            if (_program == null)
            {
                Console.WriteLine("错误: 未加载程序");
                return;
            }

            // 超时控制：TimeoutSeconds=0 不限时，>0 则创建超时 Token
            //
            // ⚠ **这是"连续执行"的超时，不是墙钟** —— 宿主每次阻塞等待（等消息 / 等弹框 /
            //   等用户输入）返回后都会调 `ResetTimeout()` 把截止时间推后（见那里的说明）。
            //   按墙钟走的话，"用户在开场对话框上读说明、瞄准时思考"的时间全被算进超时：
            //   实测 `gorilla.bas`（编辑器运行 = 120 秒）里玩家实际只玩了 46 秒就被杀掉，
            //   而**程序被杀后窗口停在最后一帧**，用户看到的就是"游戏卡死、触摸没反应"。
            _externalCt = cancellationToken;
            _timeoutCts = null;
            if (TimeoutSeconds > 0 && cancellationToken == default)
            {
                _timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
                cancellationToken = _timeoutCts.Token;
            }
            else if (TimeoutSeconds > 0 && cancellationToken != default)
            {
                _timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _timeoutCts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));
                cancellationToken = _timeoutCts.Token;
            }
            // 截止时刻自己记一笔（CTS 查不到"还剩多久"，见 `_timeoutDeadlineMs`）
            _timeoutDeadlineMs = _timeoutCts is null ? 0 : Environment.TickCount64 + TimeoutSeconds * 1000L;

            try
            {
                /* ── `main` 的帧要**自己搭好**，不论有没有命令行参数 ────────────────────
                   C 的调用约定里 `call` 会把**返回地址**压栈，参数落在它**之上** —— 被调用方
                   读参数的位置因此是 `[R12+12]`（第一个）、`[R12+16]`（第二个）……
                   （`main` 的序言是 `push R15 / push R12 / move R12 R13` ⇒ R12 在初始 SP 之下 8 字节。）
                   此前这段**两处都不对**：
                     · 只在 `CommandLineArgs.Count > 0` 时才搭 ⇒ 无参数时 `main` 的 `[R12+12]`
                       落在**初始 SP 之上**、读到没写过的内存。实测 `sl`：`argc` 读出
                       **897988541**（= 0x358637BD，某个浮点常量 `1e-6` 的位模式），
                       于是 `for (i = 1; i < argc; ++i) if (*argv[i] == '-')` 一进循环就崩
                       （报"内存越界，地址＝MK1\r"这类**字符串正文当指针**的错误）。
                     · 少压了**返回地址槽** ⇒ 有参数时 `argc` 读到的是 argv、`argv` 读到垃圾。
                   所以无条件搭三格，**顺序 argv、argc、返回地址**（返回地址最后压、落在最低
                   地址，正好对上"`call` 压 RA"的布局）。
                   ⚠ 返回地址给 **0**：`ExecuteRet` 把 `returnAddress <= 0` 当"从入口返回"⇒
                     结束程序、退出码取 R0 —— 这正是 `main` 正常 `return` 的语义。
                   ⚠ `argc` **至少 1**、`argv[0]` 一定可读（没有参数时给空串）：C 保证 `argc >= 1`，
                     老程序常拿 `argv[0]` 打用法/取程序名，给 NULL 就是替它们埋一个空指针。
                   判据 `scripts/vml-c-probe/cases/43-stack-heap-collision.c`。
                   ⚠⚠ **不要**给这段加 `privilegeLevel == 0` 的门 —— 这里原来就有，而
                     `privilegeLevel` **默认是 1**（MCU 模式）⇒ 整段**从来没执行过**。
                     帧布局是 **ABI** 的事、与特权级无关：前端不论哪种模式都按 `[R12+12]` 读 `argc`。
                     （特权级管的是**系统调用**能不能用，见 `DeniedByPrivilege`。） */
                {
                    /* 生效的 argv（**唯一真源**：入口帧照它铺，`#62`/`#63` 按序号读它）。 */
                    _argvStrings = CommandLineArgs.Count > 0
                        ? new List<string>(CommandLineArgs)
                        : new List<string> { "" };
                    int argc = _argvStrings.Count;
                    int argvPtr = AllocateMemory((argc + 1) * 4);
                    for (int i = 0; i < argc; i++)
                    {
                        string arg = _argvStrings[i];
                        int strAddr = AllocateMemory(arg.Length + 1);
                        for (int j = 0; j < arg.Length; j++)
                            memory[strAddr + j] = (byte)arg[j];
                        memory[strAddr + arg.Length] = 0;
                        SetMemory(argvPtr + i * 4, strAddr);
                    }
                    SetMemory(argvPtr + argc * 4, 0); // NULL 终止

                    sp -= 4;
                    SetMemory(sp, argvPtr); // argv
                    sp -= 4;
                    SetMemory(sp, argc);    // argc
                    sp -= 4;
                    SetMemory(sp, 0);       // 返回地址槽：0 = 从 main 返回即结束程序
                    registers[13] = sp;
                    // R0 = argc, R1 = argv (方便直接访问)
                    registers[0] = argc;
                    registers[1] = argvPtr;
                }

                int stepCount = 0;
                int traceInterval = 1000;
                if (_cpuSpeed > 0)
                {
                    _speedStopwatch.Restart();
                    _speedBatchCount = 0;
                }

                while (pc >= 0 && pc < _program.Instructions.Count)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        // 两种"停"必须分得出来：外部令牌被取消 = 用户自己按的停止；
                        // 外部没取消而这里响了 = **连续执行**超时。原来两种都打
                        // "VM execution cancelled"，用户根本分不出是程序坏了还是被超时杀了。
                        Console.WriteLine(_externalCt.IsCancellationRequested
                            ? "VM execution cancelled"
                            : $"运行超时（连续执行 {TimeoutSeconds} 秒未等待，已停止）");
                        return;
                    }

                    if (StepLimit > 0 && stepCount >= StepLimit)
                    {
                        Console.WriteLine($"Step limit ({StepLimit}) reached");
                        return;
                    }

                    stepCount++;
                    InstructionsExecuted++;
                    Instruction crashingInstr = null;
                    try
                    {
                        if (DebugMode && HasBreakpoint(pc))
                        {
                            OnBreakpoint?.Invoke(pc);
                            return;
                        }

                        crashingInstr = _program.Instructions[pc];
                        if (TraceMode && stepCount % traceInterval == 0)
                        {
                            var entryLabel = _program.EntryPoint ?? "main";
                            Console.WriteLine($"VML Runtime: entry={entryLabel}@{pc}, stack_top={sp} (0x{sp:X}), vectors=0x{_program.VectorTable:X}");
                        }

                        if (crashingInstr.Opcode == OpCode.HALT)
                        {
                            return;
                        }

                        ExecuteInstruction();
                        if (_cpuSpeed > 0)
                        {
                            _speedBatchCount++;
                            if (_speedBatchCount >= SpeedBatchSize)
                            {
                                double targetMs = _speedBatchCount * 1000.0 / _cpuSpeed;
                                double elapsedMs = _speedStopwatch.Elapsed.TotalMilliseconds;
                                if (elapsedMs < targetMs)
                                {
                                    int sleepMs = (int)(targetMs - elapsedMs);
                                    if (sleepMs > 1)
                                        Thread.Sleep(sleepMs);
                                    while (_speedStopwatch.Elapsed.TotalMilliseconds < targetMs)
                                        Thread.Sleep(0);
                                }

                                _speedBatchCount = 0;
                                _speedStopwatch.Restart();
                            }
                        }

                        // 定时器中断源 — 每条指令后通知
                        _timerSource?.OnInstructionExecuted();

                        // 硬件中断检查 — 仅 OS 模式且中断使能时
                        if (privilegeLevel == 0 && _interruptEnabled &&
                            _interruptController != null && _interruptController.HasPending)
                        {
                            var req = _interruptController.Dequeue();
                            if (req != null)
                                DispatchInterrupt(req);
                        }
                    }
                    catch (VmlMemoryException ex)
                    {
                        string instrStr = crashingInstr != null
                            ? $" {crashingInstr.Opcode} {string.Join(", ", crashingInstr.Operands)}"
                            : "";
                        string dmaStr = "";
                        if (crashingInstr != null)
                        {
                            foreach (var op in crashingInstr.Operands)
                            {
                                if (op.Value is DecodedMemAddr dma)
                                {
                                    dmaStr = $" [AddrKind={dma.AddrKind} BaseReg={dma.BaseReg} Offset={dma.Offset} RegVal={(dma.BaseReg >= 0 && dma.BaseReg < registers.Length ? registers[dma.BaseReg].ToString("X8") : "OOB")}]";
                                    break;
                                }
                            }
                        }

                        Console.Error.WriteLine($"内存错误(PC={pc:X8}):{instrStr}{dmaStr} — {ex.Message}");
                        DumpRegisters();
                        return;
                    }
                    catch (VmlLabelException ex)
                    {
                        Console.Error.WriteLine($"标签错误(PC={pc:X8}): {ex.Message}");
                        DumpRegisters();
                        return;
                    }
                    catch (VmlException ex)
                    {
                        Console.Error.WriteLine($"VML 错误(PC={pc:X8}): {ex.Message}");
                        if (DebugMode) DumpRegisters();
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"未预期的运行时崩溃(PC={pc:X8}): {ex.GetType().Name}: {ex.Message}");
                        Console.Error.WriteLine(ex.StackTrace);
                        DumpRegisters();
                        throw;
                    }
                }

                // 确保所有缓冲输出在程序退出前刷新 (修复自执行 EXE 中输出丢失)
                Console.Out.Flush();
            }
            finally
            {
                _timeoutCts?.Dispose();
                _timeoutCts = null;   // `ResetTimeout` 据此判"这次运行已经结束"
            }
        }

        /// <summary>
        /// 把超时截止时间**推后** <see cref="TimeoutSeconds"/> —— 宿主每次"阻塞等待"返回后调用。
        ///
        /// <para>
        /// 语义因此从「从开始跑起满 N 秒」变成「**连续执行 N 秒未阻塞**」：等消息、等弹框、
        /// 等用户思考**都不计时**。失控程序（死循环里压根不碰宿主）照旧会在 N 秒被杀掉 ——
        /// 那才是超时该管的唯一情形。
        /// </para>
        ///
        /// <para>
        /// ⚠ **只在"真的阻塞等过"的路径上调用**（`ui_wait_msg` / `ui_dlg_*` / 读输入）。
        /// 非阻塞的 `ui_poll` **绝不能调**：程序每帧都调它，续期等于超时永不触发。
        /// </para>
        /// <para>
        /// ⚠ 只在 VM 线程调用（宿主的 syscall 处理就跑在 VM 线程上），与 <see cref="Run"/>
        /// 是同一条线程，无并发问题。
        /// </para>
        /// </summary>
        public void ResetTimeout()
        {
            if (TimeoutSeconds <= 0 || _timeoutCts is null) return;
            try
            {
                _timeoutCts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));
                _timeoutDeadlineMs = Environment.TickCount64 + TimeoutSeconds * 1000L;
            }
            catch (ObjectDisposedException) { /* 这次运行刚结束（与 Run 的 finally 赛跑） */ }
        }

        /// <summary>
        /// 这次运行**还剩多少秒**超时；`0` = 不限时（`TimeoutSeconds &lt;= 0`）或这次运行已经结束。
        ///
        /// <para>
        /// 状态面板靠它显示「超时剩 Ns」。它是"程序是不是马上要被超时杀掉"的**唯一直接读数** ——
        /// 排查"游戏卡死"那一轮，最后是靠日志时间戳反推出"08:22:05 + 120s = 08:24:05"才定案的；
        /// 面板上直接有这个数就不必再推。
        /// </para>
        /// </summary>
        public int TimeoutRemainingSeconds
        {
            get
            {
                if (TimeoutSeconds <= 0 || _timeoutCts is null) return 0;
                var left = _timeoutDeadlineMs - Environment.TickCount64;
                return left <= 0 ? 0 : (int)(left / 1000);
            }
        }

        /// <summary>
        /// **内存用了多少**（字节）= 堆高水位（数据段 + 已分配的动态内存）。
        ///
        /// 这是"还装得下多少"的那个读数：它逼近 <see cref="MemoryTotalBytes"/> 时，
        /// `AllocateMemory` 就开始失败，而程序那边只会看到"内存错误"。
        /// </summary>
        public int MemoryUsedBytes => memoryAllocPtr;

        /// <summary>内存总量（字节）—— 就是 `memory` 数组的长度。</summary>
        public int MemoryTotalBytes => memory?.Length ?? 0;

        /// <summary>
        /// **栈压了多少**（字节）= 初始栈顶 − 当前 `sp`。
        ///
        /// ⚠ 分母（<see cref="StackTotalBytes"/>）用**配置的栈大小**，不用"到堆高水位的距离"：
        /// 后者会随程序自己的动态分配涨落，用户看到的百分比就会莫名其妙地跳 ——
        /// 而他心里的"栈"就是设置页里填的那个数。
        /// </summary>
        public int StackUsedBytes => Math.Max(0, _initialSp - sp);

        /// <summary>栈容量（字节）：配置的栈大小；拿不到配置就退回"初始栈顶"。</summary>
        public int StackTotalBytes => _config is { StackSize: > 0 } c ? c.StackSize : _initialSp;

        public string FormatOperand(Operand operand)
        {
            var valStr = operand.Value?.ToString() ?? "";
            return operand.Type switch
            {
                OperandType.REGISTER => $"R{valStr}",
                OperandType.IMMEDIATE => $"#{valStr}",
                OperandType.MEMORY => $"[{valStr}]",
                OperandType.LABEL => valStr,
                OperandType.INDIRECT => $"@{valStr}",
                _ => valStr,
            };
        }

        /// <summary>保存中断上下文并跳转到 ISR。INT 指令和硬件中断共用此逻辑。</summary>
        internal void SaveInterruptContext(int vector, bool isRecoverable)
        {
            int ivtBase = _program?.VectorTable ?? 0;

            // 压栈 R0-R11
            for (int i = 0; i < 12; i++)
            {
                registers[13] -= 4;
                sp = registers[13];
                SetMemory(registers[13], registers[i]);
            }

            // 压栈 BP(R12), LR(R14), RA(R15) — SP(R13) 由 handler 管理
            registers[13] -= 4;
            sp = registers[13];
            SetMemory(registers[13], registers[12]);
            registers[13] -= 4;
            sp = registers[13];
            SetMemory(registers[13], registers[14]);
            registers[13] -= 4;
            sp = registers[13];
            SetMemory(registers[13], registers[15]);

            // 压栈标志位 (ZF/SF/CF/IF)
            int flags = (zf ? 0x01 : 0) | (sf ? 0x80 : 0) | (cf ? 0x40 : 0) | (_interruptEnabled ? 0x100 : 0);
            registers[13] -= 4;
            sp = registers[13];
            SetMemory(registers[13], flags);

            // 压栈返回地址 (当前 PC，已指向下一条指令)
            registers[13] -= 4;
            sp = registers[13];
            SetMemory(registers[13], pc);

            // 自动关中断，防止嵌套
            _interruptEnabled = false;
            _nonRecoverableInterrupt = !isRecoverable;

            // 从向量表读取 ISR 地址并跳转
            int isrAddr = GetMemory(ivtBase + vector * 4);
            if (isrAddr != 0)
                pc = isrAddr;
        }

        /// <summary>恢复中断上下文。IRET 指令调用。</summary>
        internal void RestoreInterruptContext()
        {
            // 弹栈返回地址
            pc = GetMemory(registers[13]);
            registers[13] += 4;
            sp = registers[13];

            // 弹栈标志位
            int flags = GetMemory(registers[13]);
            registers[13] += 4;
            sp = registers[13];
            zf = (flags & 0x01) != 0;
            sf = (flags & 0x80) != 0;
            cf = (flags & 0x40) != 0;
            _interruptEnabled = (flags & 0x100) != 0;

            // 弹栈 RA(R15), LR(R14), BP(R12)
            registers[15] = GetMemory(registers[13]);
            registers[13] += 4;
            sp = registers[13];
            registers[14] = GetMemory(registers[13]);
            registers[13] += 4;
            sp = registers[13];
            registers[12] = GetMemory(registers[13]);
            registers[13] += 4;
            sp = registers[13];

            // 弹栈 R11-R0
            for (int i = 11; i >= 0; i--)
            {
                registers[i] = GetMemory(registers[13]);
                registers[13] += 4;
                sp = registers[13];
            }
        }

        /// <summary>分发硬件中断请求。</summary>
        private void DispatchInterrupt(InterruptRequest req)
        {
            SaveInterruptContext(req.Vector, req.IsRecoverable);
        }

        private void DumpRegisters()
        {
            Console.Error.WriteLine($"R0={registers[0]:X8}  R1={registers[1]:X8}  R2={registers[2]:X8}  R3={registers[3]:X8}");
            Console.Error.WriteLine($"R4={registers[4]:X8}  R5={registers[5]:X8}  R6={registers[6]:X8}  R7={registers[7]:X8}");
            Console.Error.WriteLine($"R8={registers[8]:X8}  R9={registers[9]:X8}  R10={registers[10]:X8} R11={registers[11]:X8}");
            Console.Error.WriteLine($"R12(BP)={registers[12]:X8} R13(SP)={registers[13]:X8} R14(LR)={registers[14]:X8} R15(RA)={registers[15]:X8}");
            Console.Error.WriteLine($"ZF={zf} SF={sf} CF={cf}  PC={pc:X8}  SP={registers[13]:X8}");
        }

        public void Step()
        {
            if (_program == null) return;
            if (pc < 0 || pc >= _program.Instructions.Count)
            {
                // First step or reset: start from entry point
                if (_program.EntryPoint != null && labelAddresses.ContainsKey(_program.EntryPoint))
                    pc = labelAddresses[_program.EntryPoint];
                else
                    pc = 0;
                sp = memorySize - 4;
                registers[12] = sp;
                registers[13] = sp;
            }

            ExecuteInstruction();
        }

        public byte[] GetVgaMemory()
        {
            if (_deviceManager.FindDevice("vga") is VmDisplayDevice vgaDevice)
                return vgaDevice.GetFramebuffer();
            return Array.Empty<byte>();
        }

        /// <summary>获取指定模式的 VGA framebuffer（运行时模式切换后使用）</summary>
        public byte[] GetVgaMemory(int mode, int width, int height, int bpp)
        {
            if (_deviceManager.FindDevice("vga") is VmDisplayDevice vgaDevice)
                return vgaDevice.GetFramebuffer(mode, width, height, bpp);
            return Array.Empty<byte>();
        }

        private void ApplyDefaultMemoryProtection()
        {
            memoryRegions.Clear();

            // 1. Profile 定义的 memoryMap
            if (_config != null)
            {
                foreach (var entry in _config.MemoryMap)
                {
                    memoryRegions.Add(new MemoryRegion
                    {
                        Start = entry.Start,
                        Size = entry.Size,
                        Readable = entry.Readable,
                        Writable = entry.Writable,
                        Executable = entry.Executable,
                        Description = entry.Description
                    });
                }
            }

            // 2. 中断向量表保护（只读）。仅当程序显式设置了 .vectors 时生效
            if (_program != null && _program.VectorTable > 0)
            {
                memoryRegions.Add(new MemoryRegion
                {
                    Start = _program.VectorTable,
                    Size = DefaultIvtSize,
                    Readable = true,
                    Writable = false,
                    Executable = false,
                    Description = $"中断向量表"
                });
            }

            // 3. 栈区不可执行（始终在 profile 之上追加，不检查覆盖）
            int stackSize = Math.Min(_config?.StackSize ?? 0x10000, memorySize / 2);
            int stackBottom = Math.Max(0, memorySize - stackSize);
            memoryRegions.Add(new MemoryRegion
            {
                Start = stackBottom,
                Size = memorySize - stackBottom,
                Readable = true,
                Writable = true,
                Executable = false,
                Description = $"栈区"
            });

            // 4. VGA 图形区可读写 (0xA0000-0xCFFFF, 192KB)
            if (memorySize > 0xA0000)
            {
                memoryRegions.Add(new MemoryRegion
                {
                    Start = 0xA0000,
                    Size = 0x30000,
                    Readable = true,
                    Writable = true,
                    Executable = false,
                    Description = "VGA Graphics"
                });
            }
        }

        private bool IsRegionCovered(int start, int size)
        {
            foreach (var r in memoryRegions)
                if (start >= r.Start && start + size <= r.Start + r.Size)
                    return true;
            return false;
        }

        public string GetStats()
        {
            var elapsed = DateTime.UtcNow - _startTime;
            double ips = elapsed.TotalSeconds > 0 ? InstructionsExecuted / elapsed.TotalSeconds : 0;
            return $"指令: {InstructionsExecuted}  |  系统调用: {SyscallsExecuted}  |  耗时: {elapsed.TotalSeconds:F1}s  |  速度: {ips:F0} 指令/秒";
        }

        private VmConfig? _config;
        public VmConfig? Config => _config;

        public void Dispose()
        {
            foreach (var fh in _fileHandles)
            {
                try
                {
                    fh.Close();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"FileHandle close error: {ex.Message}");
                }
            }

            _fileHandles.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
