using System.Text;
using VMLAssembler;
using VMLRuntime;

namespace WayCoder.Maui.Services;

/// <summary>
/// 把 VML（用户自己的编译器 + 翻译器 + 虚拟机）**在进程内**接进 App。
///
/// 关键决定：**不把它编成可执行文件丢进 shell 跑**，而是以库的形式链进来。
/// 三条理由，每一条单独都足够：① iOS 的 `fork/exec` 被沙箱物理拒绝 —— 进程外那条路永远上不了 iOS；
/// ② Android 10+ 的 W^X 让 app 私有目录里的文件不可 exec，得把二进制塞进 `jniLibs` 当 `.so`；
/// ③ 自包含 .NET 运行时几百 MB，而托管代码链进来会被链接器裁。
///
/// 代价是这里要自己接三样东西：设备重置、控制台 IO、超时 —— 见下面每一步的注释。
/// </summary>
internal static class MauiVml
{
    /// <summary>内建的自检程序（汇编形式）—— 用来验证「汇编 → 跑 → 收输出」这条链路是通的。</summary>
    public const string HelloWorldAsm = """
.entry main
.stack 256
.vectors 0x0
.data
msg: .string "Hello, VML!"
.text
main:
LEA R0 msg
SYSCALL #1
HALT
""";

    /// <summary>
    /// 汇编并运行一段 VML 源码，返回它写到控制台的东西。
    ///
    /// ⚠ 这是**同步阻塞**的（<c>VmRuntime.Run</c> 本身就是同步的），调用方要自己
    /// 放到后台线程上，别在 UI 线程直接调。
    /// </summary>
    public static string RunAssembly(string source, int timeoutSeconds = 10)
    {
        var prog = new VmlAssembler().Assemble(source);

        // **每次跑之前必须重置单例 DeviceManager**：它跨运行保留状态，
        // 不重置的话第二次运行的 MMIO 地址会和第一次串掉（这是 VML 自带测试里的做法）。
        VMLRuntime.Device.DeviceManager.Instance.Reset();

        // 设备地址全部归零 + 空白名单 = 关掉所有 MMIO 设备。
        // VGA 也压到 1x1 —— 手机上没有它的显示窗口，留着只是白占内存。
        var cfg = new VmConfig
        {
            KeyboardDataAddress = 0,
            KeyboardStatusAddress = 0,
            MouseXAddress = 0,
            MouseYAddress = 0,
            VgaStartAddress = 0,
        };
        cfg.VgaDisplay.Width = 1;
        cfg.VgaDisplay.Height = 1;

        var io = new CaptureIo();

        // ⚠ **永远只用 "mcu" 模式，这是手机端的安全边界，不是默认值凑巧。**
        //
        // 切到 "os"（privilegeLevel=0）会放开 syscall 300-376：线程/互斥量、Socket/DNS、
        // mkdir/stat/readdir、以及 **Exec（syscall 320 → System.Diagnostics.Process.Start）**。
        // 在手机上这些要么被沙箱挡、要么本就不该由一段 VML 程序触发（那处 Process.Start
        // 在 iOS 上还会直接抛）。MCU 模式下它们全部返回 SYSCALL_PERMISSION_DENIED，够不着。
        //
        // 模式是**宿主侧**参数、VML 程序自己改不了 —— 所以只要这里写死，那三层就是死代码。
        // （也正因如此，没去删 VMLRuntime.Syscall.OS.cs / .FFI.cs：删了只是删死代码，
        //   却要在 sync.sh 里加一步「同步后删文件」，那是最脆的一类本地适配。）
        using var vm = new VmRuntime(2 * 1024 * 1024, cfg, [], mode: "mcu")
        {
            // **超时必须设**：VML 程序里的死循环会把 App 挂死，手机上没有 Ctrl+C 可按。
            TimeoutSeconds = timeoutSeconds,
            ConsoleIO = io,
        };

        vm.LoadProgram(prog);
        vm.Run();
        return io.Text;
    }

    /// <summary>
    /// 把 VML 的输出收进内存。
    ///
    /// 不实现这个的话 <c>VmRuntime</c> 会回退到 <c>System.Console</c> ——
    /// 那在 MAUI 里等于「输出凭空消失」（没有可见的控制台）。
    /// 输入侧一律返回「没有输入」：命令行页是「敲一条、跑一条」的模型，不做交互式 stdin。
    /// </summary>
    private sealed class CaptureIo : IConsoleIO
    {
        private readonly StringBuilder _sb = new();

        public string Text => _sb.ToString();

        public void WriteString(string str) => _sb.Append(str);
        public void WriteChar(char ch) => _sb.Append(ch);
        public void WriteInt(int value) => _sb.Append(value);
        public void WriteFloat(float value) => _sb.Append(value);
        public void WriteHex(int value) => _sb.Append(value.ToString("X"));

        // 输入侧：没有交互式输入，全部回退成空值/0。
        // （若将来要做 `vml repl` 那种交互式，这里要换成能阻塞等待的可等待队列。）
        public string ReadString() => "";
        public char ReadChar() => '\0';
        public int ReadInt() => 0;
        public float ReadFloat() => 0f;
        public bool KeyAvailable() => false;
    }
}
