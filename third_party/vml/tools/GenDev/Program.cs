using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using System.Text;
using GenDev.Models;
using GenDev.Generators;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }
        if (args[0] == "-v" || args[0] == "-V" || args[0] == "--version")
        {
            Console.WriteLine("GenDev v1.63.1");
            return;
        }

        try
        {
            var options = ParseArguments(args);
            if (options.ShowHelp)
            {
                PrintUsage();
                return;
            }

            if (string.IsNullOrEmpty(options.InputFile))
            {
                Console.WriteLine("错误: 必须指定输入文件");
                PrintUsage();
                return;
            }

            // 加载设备描述文件
            DeviceModel device = LoadXmlDevice(options.InputFile);

            // 创建代码生成器
            var generators = CreateGenerators(options);
            
            // 生成代码
            foreach (var generator in generators)
            {
                string code = generator.Generate(device);
                string outputFile = GetOutputFileName(options, device, generator);
                
                // 确保输出目录存在
                string? outputDir = Path.GetDirectoryName(outputFile);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                
                // 写入文件
                File.WriteAllText(outputFile, code, Encoding.UTF8);
                Console.WriteLine($"已生成: {outputFile}");
            }
            
            Console.WriteLine("代码生成完成!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"内部错误: {ex.InnerException.Message}");
            }
        }
    }

    [UnconditionalSuppressMessage("trim", "IL2026", Justification = "DeviceModel via Microsoft.XmlSerializer.Generator is AOT-safe")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "XmlSerializer requires dynamic code; GenDev is a build-time tool not published as AOT")]
    static DeviceModel LoadXmlDevice(string filePath)
    {
        var serializer = new XmlSerializer(typeof(DeviceModel));
        using var stream = new FileStream(filePath, FileMode.Open);
        return (DeviceModel?)serializer.Deserialize(stream) ?? new DeviceModel();
    }

    static List<ICodeGenerator> CreateGenerators(Options options)
    {
        var generators = new List<ICodeGenerator>();
        
        if (options.GenerateAllLanguages)
        {
            generators.Add(new CCodeGenerator());
            generators.Add(new BasicCodeGenerator());
            generators.Add(new PascalCodeGenerator());
            generators.Add(new VMLCodeGenerator());
            generators.Add(new LadderCodeGenerator());
            generators.Add(new PythonCodeGenerator());
            generators.Add(new ForthCodeGenerator());
            generators.Add(new LuaCodeGenerator());
            generators.Add(new GoCodeGenerator());
            generators.Add(new RustCodeGenerator());
            generators.Add(new JavaCodeGenerator());
            generators.Add(new JavaScriptCodeGenerator());
            generators.Add(new SwiftCodeGenerator());
            generators.Add(new CSharpCodeGenerator());
            generators.Add(new CppCodeGenerator());
            generators.Add(new KotlinCodeGenerator());
            generators.Add(new SchemeCodeGenerator());
            generators.Add(new RubyCodeGenerator());
            generators.Add(new DartCodeGenerator());
            generators.Add(new ObjCCodeGenerator());
            generators.Add(new RCodeGenerator());
            generators.Add(new DCodeGenerator());
            generators.Add(new FortranCodeGenerator());
        }
        else if (!string.IsNullOrEmpty(options.Language))
        {
            switch (options.Language.ToLower())
            {
                case "c":
                    generators.Add(new CCodeGenerator());
                    break;
                case "basic":
                    generators.Add(new BasicCodeGenerator());
                    break;
                case "pascal":
                    generators.Add(new PascalCodeGenerator());
                    break;
                case "vml":
                    generators.Add(new VMLCodeGenerator());
                    break;
                case "ladder":
                case "ld":
                    generators.Add(new LadderCodeGenerator());
                    break;
                case "python":
                case "py":
                    generators.Add(new PythonCodeGenerator());
                    break;
                case "forth":
                case "fs":
                    generators.Add(new ForthCodeGenerator());
                    break;
                case "lua":
                    generators.Add(new LuaCodeGenerator());
                    break;
                case "go":
                    generators.Add(new GoCodeGenerator());
                    break;
                case "rust":
                    generators.Add(new RustCodeGenerator());
                    break;
                case "java":
                    generators.Add(new JavaCodeGenerator());
                    break;
                case "javascript":
                case "js":
                    generators.Add(new JavaScriptCodeGenerator());
                    break;
                case "swift":
                    generators.Add(new SwiftCodeGenerator());
                    break;
                case "csharp":
                case "cs":
                    generators.Add(new CSharpCodeGenerator());
                    break;
                case "cpp":
                case "c++":
                    generators.Add(new CppCodeGenerator());
                    break;
                case "kotlin":
                case "kt":
                    generators.Add(new KotlinCodeGenerator());
                    break;
                case "scheme":
                case "scm":
                    generators.Add(new SchemeCodeGenerator());
                    break;
                case "ruby":
                    generators.Add(new RubyCodeGenerator());
                    break;
                case "dart":
                    generators.Add(new DartCodeGenerator());
                    break;
                case "objc":
                    generators.Add(new ObjCCodeGenerator());
                    break;
                case "r":
                    generators.Add(new RCodeGenerator());
                    break;
                case "d":
                    generators.Add(new DCodeGenerator());
                    break;
                case "fortran":
                    generators.Add(new FortranCodeGenerator());
                    break;
                default:
                    throw new ArgumentException($"不支持的语言: {options.Language}");
            }
        }
        else
        {
            // 默认生成C语言
            generators.Add(new CCodeGenerator());
        }
        
        return generators;
    }

    static string GetOutputFileName(Options options, DeviceModel device, ICodeGenerator generator)
    {
        if (!string.IsNullOrEmpty(options.OutputFile))
        {
            if (options.GenerateAllLanguages && string.IsNullOrEmpty(options.OutputDir))
            {
                // 如果生成所有语言但没有指定输出目录，使用输出文件作为基础名
                string baseName = Path.GetFileNameWithoutExtension(options.OutputFile);
                string? dir = Path.GetDirectoryName(options.OutputFile);
                return Path.Combine(dir ?? ".", $"{baseName}{generator.FileExtension}");
            }
            return options.OutputFile;
        }
        
        if (!string.IsNullOrEmpty(options.OutputDir))
        {
            string fileName = $"{device.Metadata.Name.ToLowerInvariant()}{generator.FileExtension}";
            return Path.Combine(options.OutputDir, fileName);
        }
        
        // 默认输出到当前目录
        return $"{device.Metadata.Name.ToLowerInvariant()}{generator.FileExtension}";
    }

    static Options ParseArguments(string[] args)
    {
        var options = new Options();
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    break;
                    
                case "--input":
                case "-i":
                    if (i + 1 < args.Length)
                    {
                        options.InputFile = args[++i];
                    }
                    break;
                    
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        options.OutputFile = args[++i];
                    }
                    break;
                    
                case "--output-dir":
                case "-d":
                    if (i + 1 < args.Length)
                    {
                        options.OutputDir = args[++i];
                    }
                    break;
                    
                case "--language":
                case "-l":
                    if (i + 1 < args.Length)
                    {
                        options.Language = args[++i];
                    }
                    break;
                    
                case "--all-languages":
                case "-a":
                    options.GenerateAllLanguages = true;
                    break;
                    
                default:
                    // 如果没有指定参数，假设是输入文件
                    if (string.IsNullOrEmpty(options.InputFile))
                    {
                        options.InputFile = args[i];
                    }
                    break;
            }
        }
        
        return options;
    }

    static void PrintUsage()
    {
        Console.WriteLine("VML设备代码生成器");
        Console.WriteLine("用法: GenDev [选项] <输入文件>");
        Console.WriteLine();
        Console.WriteLine("选项:");
        Console.WriteLine("  -h, --help            显示帮助信息");
        Console.WriteLine("  -i, --input <文件>    输入设备描述文件 (XML)");
        Console.WriteLine("  -o, --output <文件>   输出文件路径");
        Console.WriteLine("  -d, --output-dir <目录> 输出目录");
            Console.WriteLine("  -l, --language <语言>  生成的语言 (c, basic, pascal, vml, ladder, python, forth, lua, go, rust, java, javascript, swift, csharp, cpp, kotlin, scheme, ruby, dart, objc, r, d, fortran)");
        Console.WriteLine("  -a, --all-languages   生成所有支持的语言");
        Console.WriteLine();
        Console.WriteLine("示例:");
        Console.WriteLine("  GenDev -i ATmega328P.xml -o atmega328p.h -l c");
        Console.WriteLine("  GenDev -i ATmega328P.xml -d Generated -a");
        Console.WriteLine("  GenDev ATmega328P.xml -l basic");
    }

    class Options
    {
        public bool ShowHelp { get; set; }
        public string InputFile { get; set; } = string.Empty;
        public string OutputFile { get; set; } = string.Empty;
        public string OutputDir { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public bool GenerateAllLanguages { get; set; }
    }
}