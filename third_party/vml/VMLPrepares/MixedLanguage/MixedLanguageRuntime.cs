using System;
using System.Collections.Generic;
using System.Linq;

namespace VML.MixedLanguage
{
    /// <summary>
    /// 混合语言运行时接口
    /// 提供统一的函数调用接口，支持不同语言间的相互调用
    /// </summary>
    public static class MixedLanguageRuntime
    {
        /// <summary>
        /// 存储所有可调用函数的字典
        /// </summary>
        private static readonly Dictionary<string, Delegate> _functions = new();
        
        /// <summary>
        /// 注册函数
        /// </summary>
        /// <param name="name">函数名称</param>
        /// <param name="func">函数委托</param>
        public static void RegisterFunction(string name, Delegate func)
        {
            _functions[name] = func;
        }
        
        /// <summary>
        /// 调用函数
        /// </summary>
        /// <param name="name">函数名称</param>
        /// <param name="args">参数列表</param>
        /// <returns>函数返回值</returns>
        public static object CallFunction(string name, params object[] args)
        {
            if (_functions.TryGetValue(name, out var func))
            {
                return func.DynamicInvoke(args);
            }
            throw new InvalidOperationException($"Function '{name}' not found");
        }
        
        /// <summary>
        /// 获取所有注册的函数名称
        /// </summary>
        /// <returns>函数名称列表</returns>
        public static IEnumerable<string> GetRegisteredFunctions()
        {
            return _functions.Keys.ToList();
        }
        
        /// <summary>
        /// 获取函数信息
        /// </summary>
        /// <param name="name">函数名称</param>
        /// <returns>函数信息</returns>
        public static string GetFunctionInfo(string name)
        {
            if (_functions.TryGetValue(name, out var func))
            {
                return $"Function: {name}, Type: {func.GetType().Name}";
            }
            return $"Function '{name}' not found";
        }
    }
    
    /// <summary>
    /// 语言适配器基类
    /// </summary>
    public abstract class LanguageAdapter
    {
        /// <summary>
        /// 获取适配器名称
        /// </summary>
        public abstract string Name { get; }
        
        /// <summary>
        /// 调用指定语言的函数
        /// </summary>
        /// <param name="language">目标语言</param>
        /// <param name="functionName">函数名称</param>
        /// <param name="args">参数列表</param>
        /// <returns>调用结果</returns>
        public abstract object CallForeignFunction(string language, string functionName, params object[] args);
        
        /// <summary>
        /// 获取语言信息
        /// </summary>
        /// <returns>语言信息</returns>
        public virtual string GetLanguageInfo()
        {
            return $"Language: {Name}";
        }
    }
    
    /// <summary>
    /// C语言适配器
    /// </summary>
    public class CAdapter : LanguageAdapter
    {
        public override string Name => "C";
        
        public override object CallForeignFunction(string language, string functionName, params object[] args)
        {
            System.Console.WriteLine($"C calling {language}.{functionName} with args: {string.Join(", ", args)}");
            return $"C called {language}.{functionName}";
        }
        
        /// <summary>
        /// C语言标准库函数：输出整数
        /// </summary>
        public void PrintInt(int value)
        {
            System.Console.WriteLine($"C: print_int({value})");
        }
        
        /// <summary>
        /// C语言标准库函数：输出字符串
        /// </summary>
        public void PrintString(string str)
        {
            System.Console.WriteLine($"C: print_string(\"{str}\")");
        }
        
        /// <summary>
        /// C语言标准库函数：输出浮点数
        /// </summary>
        public void PrintFloat(float value)
        {
            System.Console.WriteLine($"C: print_float({value})");
        }
    }
    
    /// <summary>
    /// Python语言适配器
    /// </summary>
    public class PythonAdapter : LanguageAdapter
    {
        public override string Name => "Python";
        
        public override object CallForeignFunction(string language, string functionName, params object[] args)
        {
            System.Console.WriteLine($"Python calling {language}.{functionName} with args: {string.Join(", ", args)}");
            return $"Python called {language}.{functionName}";
        }
        
        /// <summary>
        /// Python语言标准库函数：输出整数
        /// </summary>
        public void PrintInt(int value)
        {
            System.Console.WriteLine($"Python: print_int({value})");
        }
        
        /// <summary>
        /// Python语言标准库函数：输出字符串
        /// </summary>
        public void PrintString(string str)
        {
            System.Console.WriteLine($"Python: print_string(\"{str}\")");
        }
        
        /// <summary>
        /// Python语言标准库函数：输出浮点数
        /// </summary>
        public void PrintFloat(float value)
        {
            System.Console.WriteLine($"Python: print_float({value})");
        }
    }
    
    /// <summary>
    /// Java语言适配器
    /// </summary>
    public class JavaAdapter : LanguageAdapter
    {
        public override string Name => "Java";
        
        public override object CallForeignFunction(string language, string functionName, params object[] args)
        {
            System.Console.WriteLine($"Java calling {language}.{functionName} with args: {string.Join(", ", args)}");
            return $"Java called {language}.{functionName}";
        }
        
        /// <summary>
        /// Java语言标准库函数：输出整数
        /// </summary>
        public void PrintInt(int value)
        {
            System.Console.WriteLine($"Java: print_int({value})");
        }
        
        /// <summary>
        /// Java语言标准库函数：输出字符串
        /// </summary>
        public void PrintString(string str)
        {
            System.Console.WriteLine($"Java: print_string(\"{str}\")");
        }
        
        /// <summary>
        /// Java语言标准库函数：输出浮点数
        /// </summary>
        public void PrintFloat(float value)
        {
            System.Console.WriteLine($"Java: print_float({value})");
        }
    }
    
    /// <summary>
    /// JavaScript语言适配器
    /// </summary>
    public class JavaScriptAdapter : LanguageAdapter
    {
        public override string Name => "JavaScript";
        
        public override object CallForeignFunction(string language, string functionName, params object[] args)
        {
            System.Console.WriteLine($"JavaScript calling {language}.{functionName} with args: {string.Join(", ", args)}");
            return $"JavaScript called {language}.{functionName}";
        }
        
        /// <summary>
        /// JavaScript语言标准库函数：输出整数
        /// </summary>
        public void PrintInt(int value)
        {
            System.Console.WriteLine($"JavaScript: print_int({value})");
        }
        
        /// <summary>
        /// JavaScript语言标准库函数：输出字符串
        /// </summary>
        public void PrintString(string str)
        {
            System.Console.WriteLine($"JavaScript: print_string(\"{str}\")");
        }
        
        /// <summary>
        /// JavaScript语言标准库函数：输出浮点数
        /// </summary>
        public void PrintFloat(float value)
        {
            System.Console.WriteLine($"JavaScript: print_float({value})");
        }
    }
    
    /// <summary>
    /// Basic语言适配器
    /// </summary>
    public class BasicAdapter : LanguageAdapter
    {
        public override string Name => "Basic";
        
        public override object CallForeignFunction(string language, string functionName, params object[] args)
        {
            System.Console.WriteLine($"Basic calling {language}.{functionName} with args: {string.Join(", ", args)}");
            return $"Basic called {language}.{functionName}";
        }
        
        /// <summary>
        /// Basic语言标准库函数：输出整数
        /// </summary>
        public void PrintInt(int value)
        {
            System.Console.WriteLine($"Basic: print_int({value})");
        }
        
        /// <summary>
        /// Basic语言标准库函数：输出字符串
        /// </summary>
        public void PrintString(string str)
        {
            System.Console.WriteLine($"Basic: print_string(\"{str}\")");
        }
        
        /// <summary>
        /// Basic语言标准库函数：输出浮点数
        /// </summary>
        public void PrintFloat(float value)
        {
            System.Console.WriteLine($"Basic: print_float({value})");
        }
    }
    
    /// <summary>
    /// Pascal语言适配器
    /// </summary>
    public class PascalAdapter : LanguageAdapter
    {
        public override string Name => "Pascal";
        
        public override object CallForeignFunction(string language, string functionName, params object[] args)
        {
            System.Console.WriteLine($"Pascal calling {language}.{functionName} with args: {string.Join(", ", args)}");
            return $"Pascal called {language}.{functionName}";
        }
        
        /// <summary>
        /// Pascal语言标准库函数：输出整数
        /// </summary>
        public void PrintInt(int value)
        {
            System.Console.WriteLine($"Pascal: print_int({value})");
        }
        
        /// <summary>
        /// Pascal语言标准库函数：输出字符串
        /// </summary>
        public void PrintString(string str)
        {
            System.Console.WriteLine($"Pascal: print_string(\"{str}\")");
        }
        
        /// <summary>
        /// Pascal语言标准库函数：输出浮点数
        /// </summary>
        public void PrintFloat(float value)
        {
            System.Console.WriteLine($"Pascal: print_float({value})");
        }
    }
    
    /// <summary>
    /// Lua语言适配器
    /// </summary>
    public class LuaAdapter : LanguageAdapter
    {
        public override string Name => "Lua";
        
        public override object CallForeignFunction(string language, string functionName, params object[] args)
        {
            System.Console.WriteLine($"Lua calling {language}.{functionName} with args: {string.Join(", ", args)}");
            return $"Lua called {language}.{functionName}";
        }
        
        /// <summary>
        /// Lua语言标准库函数：输出整数
        /// </summary>
        public void PrintInt(int value)
        {
            System.Console.WriteLine($"Lua: print_int({value})");
        }
        
        /// <summary>
        /// Lua语言标准库函数：输出字符串
        /// </summary>
        public void PrintString(string str)
        {
            System.Console.WriteLine($"Lua: print_string(\"{str}\")");
        }
        
        /// <summary>
        /// Lua语言标准库函数：输出浮点数
        /// </summary>
        public void PrintFloat(float value)
        {
            System.Console.WriteLine($"Lua: print_float({value})");
        }
    }
    
    /// <summary>
    /// Go语言适配器
    /// </summary>
    public class GoAdapter : LanguageAdapter
    {
        public override string Name => "Go";
        
        public override object CallForeignFunction(string language, string functionName, params object[] args)
        {
            System.Console.WriteLine($"Go calling {language}.{functionName} with args: {string.Join(", ", args)}");
            return $"Go called {language}.{functionName}";
        }
        
        /// <summary>
        /// Go语言标准库函数：输出整数
        /// </summary>
        public void PrintInt(int value)
        {
            System.Console.WriteLine($"Go: print_int({value})");
        }
        
        /// <summary>
        /// Go语言标准库函数：输出字符串
        /// </summary>
        public void PrintString(string str)
        {
            System.Console.WriteLine($"Go: print_string(\"{str}\")");
        }
        
        /// <summary>
        /// Go语言标准库函数：输出浮点数
        /// </summary>
        public void PrintFloat(float value)
        {
            System.Console.WriteLine($"Go: print_float({value})");
        }
    }
    
    /// <summary>
    /// C#语言适配器
    /// </summary>
    public class CSharpAdapter : LanguageAdapter
    {
        public override string Name => "C#";
        
        public override object CallForeignFunction(string language, string functionName, params object[] args)
        {
            System.Console.WriteLine($"C# calling {language}.{functionName} with args: {string.Join(", ", args)}");
            return $"C# called {language}.{functionName}";
        }
        
        /// <summary>
        /// C#语言标准库函数：输出整数
        /// </summary>
        public void PrintInt(int value)
        {
            System.Console.WriteLine($"C#: print_int({value})");
        }
        
        /// <summary>
        /// C#语言标准库函数：输出字符串
        /// </summary>
        public void PrintString(string str)
        {
            System.Console.WriteLine($"C#: print_string(\"{str}\")");
        }
        
        /// <summary>
        /// C#语言标准库函数：输出浮点数
        /// </summary>
        public void PrintFloat(float value)
        {
            System.Console.WriteLine($"C#: print_float({value})");
        }
    }
    
    /// <summary>
    /// Swift语言适配器
    /// </summary>
    public class SwiftAdapter : LanguageAdapter
    {
        public override string Name => "Swift";
        
        public override object CallForeignFunction(string language, string functionName, params object[] args)
        {
            System.Console.WriteLine($"Swift calling {language}.{functionName} with args: {string.Join(", ", args)}");
            return $"Swift called {language}.{functionName}";
        }
        
        /// <summary>
        /// Swift语言标准库函数：输出整数
        /// </summary>
        public void PrintInt(int value)
        {
            System.Console.WriteLine($"Swift: print_int({value})");
        }
        
        /// <summary>
        /// Swift语言标准库函数：输出字符串
        /// </summary>
        public void PrintString(string str)
        {
            System.Console.WriteLine($"Swift: print_string(\"{str}\")");
        }
        
        /// <summary>
        /// Swift语言标准库函数：输出浮点数
        /// </summary>
        public void PrintFloat(float value)
        {
            System.Console.WriteLine($"Swift: print_float({value})");
        }
    }
}