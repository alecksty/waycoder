using GenDev.Models;

namespace GenDev.Generators
{
    /// <summary>
    /// Fortran 设备定义生成器 (integer, parameter 常量).
    /// 继承 GeneratorBase 消除 ~105 行模板重复.
    /// </summary>
    public class FortranCodeGenerator : GeneratorBase
    {
        public override string Language => "fortran";
        public override string FileExtension => ".f90";
        protected override string CommentPrefix => "!";

        protected override string GetTypeForSize(int size) => "integer";

        protected override string FormatRegisterDef(string name, string type, string addr)
            => $"  integer, parameter :: {name}_ADDR = {addr}";

        protected override string FormatBitFieldDef(string parent, string bitName, int bit)
            => $"  integer, parameter :: {parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)}_BIT = {bit}";

        protected override string FormatMemorySegment(string name, string start, string end, string size)
            => $"  integer, parameter :: {name}_START = {start}\n  integer, parameter :: {name}_END = {end}\n  integer, parameter :: {name}_SIZE = {size}";

        protected override string FormatPeripheralBase(string name, string baseAddr)
            => $"  integer, parameter :: {name}_BASE = {baseAddr}";

        protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
            => $"  integer, parameter :: {reg}_ADDR = {absAddr}";

        protected override string FormatInterruptVector(string name, int vector, string desc)
            => $"  integer, parameter :: INT_{name} = {vector}  ! {desc}";
    }
}
