using System;
using System.Collections.Generic;
using VML.MixedLanguage;

namespace MixedLanguageDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== VML 混合语言编程演示 (增强版) ===\n");
            
            // 创建各种语言适配器
            var cAdapter = new CAdapter();
            var pythonAdapter = new PythonAdapter();
            var javaAdapter = new JavaAdapter();
            var jsAdapter = new JavaScriptAdapter();
            var basicAdapter = new BasicAdapter();
            var pascalAdapter = new PascalAdapter();
            var luaAdapter = new LuaAdapter();
            var goAdapter = new GoAdapter();
            var csharpAdapter = new CSharpAdapter();
            var swiftAdapter = new SwiftAdapter();
            
            Console.WriteLine("1. 注册语言适配器到混合运行时:");
            MixedLanguageRuntime.RegisterFunction("c_print_int", new Action<int>(cAdapter.PrintInt));
            MixedLanguageRuntime.RegisterFunction("c_print_string", new Action<string>(cAdapter.PrintString));
            MixedLanguageRuntime.RegisterFunction("python_print_int", new Action<int>(pythonAdapter.PrintInt));
            MixedLanguageRuntime.RegisterFunction("python_print_string", new Action<string>(pythonAdapter.PrintString));
            MixedLanguageRuntime.RegisterFunction("java_print_int", new Action<int>(javaAdapter.PrintInt));
            MixedLanguageRuntime.RegisterFunction("java_print_string", new Action<string>(javaAdapter.PrintString));
            
            Console.WriteLine("   已注册 C, Python, Java 标准库函数\n");
            
            Console.WriteLine("2. 演示语言间函数调用:");
            
            // C调用Python函数
            Console.WriteLine("   C调用Python函数:");
            var result1 = cAdapter.CallForeignFunction("Python", "PrintInt", 42);
            Console.WriteLine($"   结果: {result1}\n");
            
            // Python调用C函数
            Console.WriteLine("   Python调用C函数:");
            var result2 = pythonAdapter.CallForeignFunction("C", "PrintString", "Hello from Python!");
            Console.WriteLine($"   结果: {result2}\n");
            
            // Java调用Python函数
            Console.WriteLine("   Java调用Python函数:");
            var result3 = javaAdapter.CallForeignFunction("Python", "PrintInt", 100);
            Console.WriteLine($"   结果: {result3}\n");
            
            Console.WriteLine("3. 直接调用注册函数:");
            
            // 直接调用已注册的函数
            Console.WriteLine("   调用C打印整数函数:");
            MixedLanguageRuntime.CallFunction("c_print_int", 123);
            
            Console.WriteLine("   调用Python打印字符串函数:");
            MixedLanguageRuntime.CallFunction("python_print_string", "Hello from registered function!");
            
            Console.WriteLine("\n4. 语言适配器信息:");
            Console.WriteLine($"   {cAdapter.GetLanguageInfo()}");
            Console.WriteLine($"   {pythonAdapter.GetLanguageInfo()}");
            Console.WriteLine($"   {javaAdapter.GetLanguageInfo()}");
            Console.WriteLine($"   {jsAdapter.GetLanguageInfo()}");
            Console.WriteLine($"   {basicAdapter.GetLanguageInfo()}");
            Console.WriteLine($"   {pascalAdapter.GetLanguageInfo()}");
            Console.WriteLine($"   {luaAdapter.GetLanguageInfo()}");
            Console.WriteLine($"   {goAdapter.GetLanguageInfo()}");
            Console.WriteLine($"   {csharpAdapter.GetLanguageInfo()}");
            Console.WriteLine($"   {swiftAdapter.GetLanguageInfo()}");
            
            Console.WriteLine("\n5. 函数信息查询:");
            Console.WriteLine($"   {MixedLanguageRuntime.GetFunctionInfo("c_print_int")}");
            Console.WriteLine($"   {MixedLanguageRuntime.GetFunctionInfo("python_print_string")}");
            
            Console.WriteLine("\n6. 所有已注册函数:");
            foreach (var func in MixedLanguageRuntime.GetRegisteredFunctions())
            {
                Console.WriteLine($"   - {func}");
            }
            
            Console.WriteLine("\n=== 演示完成 ===");
        }
    }
}