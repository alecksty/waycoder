using GenDev.Models;
using System.Text;

namespace GenDev.Generators
{
    /// <summary>
    /// C++ 设备头文件生成器 (constexpr 风格).
    /// 继承 GeneratorBase 消除 ~180 行模板重复.
    /// </summary>
    public class CppCodeGenerator : GeneratorBase
    {
        public override string Language => "cpp";
        public override string FileExtension => ".hpp";
        protected override string CommentPrefix => "//";

        protected override string ComputeGuardName(DeviceModel device)
        {
            string name = CodeGeneratorHelper.SanitizeUpper(device.Metadata.Name) + "_HPP";
            if (char.IsDigit(name[0])) name = "DEVICE_" + name;
            return name;
        }

        protected override string GetTypeForSize(int size) => size switch
        {
            1 => "uint8_t",
            2 => "uint16_t",
            4 => "uint32_t",
            8 => "uint64_t",
            _ => "uint32_t"
        };

        protected override string FormatRegisterDef(string name, string type, string addr)
            => $"constexpr auto {name} = reinterpret_cast<volatile {type}*>({addr});";

        protected override string FormatBitFieldDef(string parent, string bitName, int bit)
            => $"constexpr int {parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)} = {bit};";

        protected override string FormatMemorySegment(string name, string start, string end, string size)
            => $"constexpr auto {name}_START = {start};\nconstexpr auto {name}_END = {end};\nconstexpr auto {name}_SIZE = {size};";

        protected override string FormatPeripheralBase(string name, string baseAddr)
            => $"constexpr auto {name}_BASE = {baseAddr};";

        protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
            => $"constexpr auto {reg} = reinterpret_cast<volatile {type}*>({absAddr});";

        protected override string FormatInterruptVector(string name, int vector, string desc)
            => $"constexpr int {name}_VECTOR = {vector};  // {desc}";

        protected override void EmitFunctions(StringBuilder code, DeviceModel device)
        {
            code.AppendLine("// 设备初始化函数");
            string initFuncName = CodeGeneratorHelper.SanitizeLower(device.Metadata.Name);
            if (char.IsDigit(initFuncName[0])) initFuncName = "device_" + initFuncName;
            code.AppendLine($"void {initFuncName}_init();");
            code.AppendLine();
        }
    }
}
