using System;
using System.Collections.Generic;
using System.Text;

namespace VMLToHex.Assemblers
{
    /// <summary>
    /// Pass-through assembler for text-format targets (Wasm Wat).
    /// Just passes the text through as UTF-8 bytes.
    /// </summary>
    public class AssemblerWasm : BaseAssembler
    {
        public override string Name => "wasm";
        public override string Description => "WebAssembly 文本格式 (pass-through)";

        public override byte[] Assemble(string asmCode, out int baseAddr, out string architecture)
        {
            baseAddr = 0;
            architecture = Name;
            return Encoding.UTF8.GetBytes(asmCode);
        }
    }
}
