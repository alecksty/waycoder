using GenDev.Models;

namespace GenDev.Generators
{
    public class VMLCodeGenerator : GeneratorBase
    {
        public override string Language => "vml";
        public override string FileExtension => ".vml";
        protected override string CommentPrefix => ";";

        protected override string GetTypeForSize(int size) => "word";

        protected override string FormatRegisterDef(string name, string type, string addr)
            => $".equ {name}, {addr}";

        protected override string FormatBitFieldDef(string parent, string bitName, int bit)
            => $".equ {parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)}, {bit}";

        protected override string FormatMemorySegment(string name, string start, string end, string size)
            => $".equ {name}_START, {start}\n.equ {name}_END, {end}\n.equ {name}_SIZE, {size}";

        protected override string FormatPeripheralBase(string name, string baseAddr)
            => $".equ {name}_BASE, {baseAddr}";

        protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
            => $".equ {reg}, {absAddr}";

        protected override string FormatInterruptVector(string name, int vector, string desc)
            => $".equ INT_{name}, {vector}  ; {desc}";
    }
}
