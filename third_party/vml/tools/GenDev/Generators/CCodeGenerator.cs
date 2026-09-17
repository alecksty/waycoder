using GenDev.Models;
using System.Text;

namespace GenDev.Generators
{
    /// <summary>
    /// C 语言设备头文件生成器 (#define 宏风格).
    /// 继承 GeneratorBase 消除 ~180 行模板重复.
    /// </summary>
    public class CCodeGenerator : GeneratorBase
    {
        public override string Language => "c";
        public override string FileExtension => ".h";
        protected override string CommentPrefix => "//";

        protected override string GetTypeForSize(int size) => size switch
        {
            1 => "uint8_t",
            2 => "uint16_t",
            4 => "uint32_t",
            8 => "uint64_t",
            _ => "uint32_t"
        };

        protected override string FormatRegisterDef(string name, string type, string addr)
            => $"#define {name} (*(volatile {type}*){addr})";

        protected override string FormatBitFieldDef(string parent, string bitName, int bit)
            => $"#define {parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)} {bit}";

        protected override string FormatMemorySegment(string name, string start, string end, string size)
            => $"#define {name}_START {start}\n#define {name}_END {end}\n#define {name}_SIZE {size}";

        protected override string FormatPeripheralBase(string name, string baseAddr)
            => $"#define {name}_BASE {baseAddr}";

        protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
            => $"#define {reg} (*(volatile {type}*){absAddr})";

        protected override string FormatInterruptVector(string name, int vector, string desc)
            => $"#define {name}_VECTOR {vector}  // {desc}";

        protected override void EmitFunctions(StringBuilder code, DeviceModel device)
        {
            code.AppendLine("// 设备初始化函数");
            string initFuncName = CodeGeneratorHelper.SanitizeLower(device.Metadata.Name);
            if (char.IsDigit(initFuncName[0])) initFuncName = "device_" + initFuncName;
            code.AppendLine($"void {initFuncName}_init(void);");
            code.AppendLine();
        }
    }
}
