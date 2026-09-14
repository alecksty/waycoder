using System;

namespace VMLPlugins
{
    /// <summary>
    /// 多语言字符串 — 委托给 Localization 系统。
    /// 默认中文，VML_LANG=en 或系统语言为英文时自动切换。
    /// </summary>
    public static class Strings
    {
        [Obsolete("Use Localization.Get() instead")]
        public static bool IsEnglish => Localization.CurrentLang == "en";

        // ===== 通用 =====
        public static string Error(string msg) => Localization.Get("compile.error", msg);
        public static string Warning(string msg) => $"[{(Localization.CurrentLang == "en" ? "Warning" : "警告")}] {msg}";
        public static string Info(string msg) => $"[{(Localization.CurrentLang == "en" ? "Info" : "信息")}] {msg}";

        // ===== VMLTool =====
        public static string ToolDescription => Localization.Get("tool.description");
        public static string Usage(string tool) => Localization.Get("tool.usage", tool);
        public static string HelpOption => Localization.Get("help.option");
        public static string VersionOption => Localization.Get("version.option");
        public static string DebugOption => Localization.Get("debug.option");
        public static string OutputOption => Localization.Get("output.option");
        public static string TargetArchOption => Localization.Get("target.option");
        public static string FormatOption => Localization.Get("format.option");
        public static string IncludeOption => Localization.Get("include.option");
        public static string LibOption => Localization.Get("lib.option");
        public static string LangOption => Localization.Get("lang.option");
        public static string TargetModeOption => Localization.Get("mode.option");
        public static string RamOption => Localization.Get("ram.option");
        public static string StackSizeOption => Localization.Get("stack.option");
        public static string Compiling(string file) => Localization.Get("compiling", file);
        public static string Compiled(string output, int count) => Localization.Get("compiled", output, count);
        public static string CompileError(string msg) => Localization.Get("compile.error", msg);
        public static string FileNotFound(string path) => Localization.Get("file.notfound", path);
        public static string UnknownFormat => Localization.Get("unknown.format");
        public static string Running => Localization.Get("running");
        public static string ProgramCompleted => Localization.Get("program.completed");
        public static string NoCompiler(string ext) => Localization.Get("nocompiler", ext);
        public static string Recording(string path) => Localization.Get("recording", path);

        // ===== 编译器通用错误 =====
        public static string SyntaxError(int line, string msg) => Localization.Get("syntax.error", line, msg);
        public static string SyntaxErrorAt(int line, int col, string msg) => $"{Localization.Get("syntax.error", line, msg)} (col {col})";
        public static string UndefinedVariable(string name) => Localization.Get("undefined.var", name);
        public static string FunctionRedefined(string name) => Localization.Get("func.redefined", name);
        public static string TypeMismatch(string expected, string got) => Localization.Get("type.mismatch", expected, got);
        public static string ExpectedToken(string expected, string got) => Localization.Get("expect.token", expected, got);
        public static string ExpectedIdentifier(string context) => Localization.Get("expect.identifier", context);
        public static string UnsupportedType(string type) => Localization.Get("unsupported.type", type);
        public static string UnsupportedExpression(string expr) => Localization.Get("unsupported.expr", expr);
        public static string NotImplemented(string feature) => Localization.Get("not.implemented", feature);
        public static string McuSkipped(string compiler, string feature, string reason) => Localization.Get("mcu.skipped", feature, reason);

        // ===== VMLToHex =====
        public static string V2hDescription => Localization.Get("v2h.description");
        public static string V2hUsage => Localization.Get("v2h.usage");
        public static string ArchOption => Localization.Get("arch.option");
        public static string BaseAddrOption => Localization.Get("baseaddr.option");
        public static string V2hBiosOption => Localization.Get("bios.option");
        public static string Assembling(string arch) => Localization.Get("assembling", arch);
        public static string OutputFormat(string format, int size) => Localization.Get("output.format", format, size);

        // ===== ConsoleEmulator =====
        public static string EmulatorTitle => Localization.Get("emulator.title");
        public static string Loading(string file) => Localization.Get("loading", file);
        public static string LoadSuccess => Localization.Get("load.success");
        public static string DeviceInfo(string name, string mode, int memKb, string display) => Localization.Get("device.info", name, mode, memKb, display);
        public static string VgaMode(int mode) => Localization.Get("vga.mode", mode);
        public static string StepMode => Localization.Get("step.mode");
        public static string RecordingStarted(string path) => Localization.Get("recording.started", path);
        public static string RecordingFinished(string path, double seconds) => Localization.Get("recording.finished", seconds, path);
        public static string ScreenshotSaved(string path) => Localization.Get("screenshot.saved", path);
        public static string KeyScriptStarted(string file) => Localization.Get("key.script.started", file);
        public static string UnknownCommand(string cmd) => Localization.Get("unknown.cmd", cmd);
    }
}
