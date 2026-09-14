#if STATIC_LINK
using VMLPlugins;
using VMLPlugins.Interfaces;

namespace VMLTool.StaticLink
{
    /// <summary>
    /// 静态链接初始化器 — 将所有编译器和转译器直接注册到插件管理器，
    /// 无需从外部 DLL 加载，实现单文件发布。
    /// </summary>
    public static class StaticLinkInitializer
    {
        public static void RegisterAll(PluginManager manager)
        {
            RegisterFrontendCompilers(manager);
            RegisterBackendTranslators(manager);
        }

        private static void RegisterFrontendCompilers(PluginManager manager)
        {
            // C
            manager.RegisterFrontendCompiler(new CCompiler.CCompilerPlugin());
            // BASIC
            manager.RegisterFrontendCompiler(new BasicCompiler.BasicCompilerPlugin());
            // Pascal
            manager.RegisterFrontendCompiler(new PascalCompiler.PascalCompilerPlugin());
            // Python
            manager.RegisterFrontendCompiler(new PythonCompiler.PythonCompilerPlugin());
            // Lua
            manager.RegisterFrontendCompiler(new LuaCompiler.LuaCompilerPlugin());
            // Forth
            manager.RegisterFrontendCompiler(new ForthCompiler.ForthCompilerPlugin());
            // Rust
            manager.RegisterFrontendCompiler(new RustCompiler.RustCompilerPlugin());
            // Go
            manager.RegisterFrontendCompiler(new GoCompiler.GoCompilerPlugin());
            // Ladder
            manager.RegisterFrontendCompiler(new LadderCompiler.LadderCompilerPlugin());
            // CSharp
            manager.RegisterFrontendCompiler(new CSharpCompiler.CSharpCompilerPlugin());
            // Java
            manager.RegisterFrontendCompiler(new JavaCompiler.JavaCompilerPlugin());
            // JavaScript
            manager.RegisterFrontendCompiler(new JavaScriptCompiler.JavaScriptCompilerPlugin());
            // Swift
            manager.RegisterFrontendCompiler(new SwiftCompiler.SwiftCompilerPlugin());
            // C++
            manager.RegisterFrontendCompiler(new CppCompiler.CppCompilerPlugin());
            // Kotlin
            manager.RegisterFrontendCompiler(new KotlinCompiler.KotlinCompilerPlugin());
            // Scheme
            manager.RegisterFrontendCompiler(new SchemeCompiler.SchemePlugin());
            // Ruby
            manager.RegisterFrontendCompiler(new RubyCompiler.RubyCompilerPlugin());
            // Dart
            manager.RegisterFrontendCompiler(new DartCompiler.DartCompilerPlugin());
            // Objective-C
            manager.RegisterFrontendCompiler(new ObjCCompiler.ObjCCompilerPlugin());
            // R
            manager.RegisterFrontendCompiler(new RCompiler.RCompilerPlugin());
            // D
            manager.RegisterFrontendCompiler(new DCompiler.DCompilerPlugin());
            // Fortran
            manager.RegisterFrontendCompiler(new FortranCompiler.FortranCompilerPlugin());
        }

        private static void RegisterBackendTranslators(PluginManager manager)
        {
            // 8-bit
            manager.RegisterBackendTranslator(new VMLTranslators.Translator6502Plugin());
            manager.RegisterBackendTranslator(new VMLTranslators.TranslatorZ80Plugin());
            manager.RegisterBackendTranslator(new VMLTranslators.Translator8051Plugin());
            manager.RegisterBackendTranslator(new VMLTranslators.TranslatorAVRPlugin());
            manager.RegisterBackendTranslator(new VMLTranslators.TranslatorPICPlugin());
            // 16-bit
            manager.RegisterBackendTranslator(new VMLTranslators.TranslatorMSP430Plugin());
            manager.RegisterBackendTranslator(new VMLTranslators.TranslatorPIC24Plugin());
            // 32-bit
            manager.RegisterBackendTranslator(new VMLTranslators.TranslatorX86Plugin());
            manager.RegisterBackendTranslator(new VMLTranslators.TranslatorARMCMPlugin());
            manager.RegisterBackendTranslator(new VMLTranslators.TranslatorRISCVPlugin());
            manager.RegisterBackendTranslator(new VMLTranslators.TranslatorMIPSPlugin());
            manager.RegisterBackendTranslator(new VMLTranslators.Translator68000Plugin());
            manager.RegisterBackendTranslator(new VMLTranslators.TranslatorPowerPCPlugin());
            manager.RegisterBackendTranslator(new VMLTranslators.TranslatorSPARCPlugin());
            // VM targets
            manager.RegisterBackendTranslator(new VMLTranslators.TranslatorJVMPlugin());
            manager.RegisterBackendTranslator(new VMLTranslators.TranslatorDotNETPlugin());
            manager.RegisterBackendTranslator(new VMLTranslators.TranslatorWasmPlugin());
        }
    }
}
#endif
