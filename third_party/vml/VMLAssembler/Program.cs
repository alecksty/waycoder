using System;

namespace VMLAssembler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("用法: VMLAssembler <输入文件.vml>");
                return;
            }

            string inputFile = args[0];

            try
            {
                Console.WriteLine($"汇编文件: {inputFile}");
                string source = File.ReadAllText(inputFile);

                var assembler = new VmlAssembler();
                var program = assembler.Assemble(source);

                Console.WriteLine($"汇编成功!");
                Console.WriteLine($"指令数: {program.Instructions.Count}");
                Console.WriteLine($"入口点: {program.EntryPoint}");
                Console.WriteLine($"栈顶: 0x{program.StackTop:X}");

                // 显示前10条指令
                Console.WriteLine("\n前10条指令:");
                for (int i = 0; i < Math.Min(10, program.Instructions.Count); i++)
                {
                    Console.WriteLine($"  {i}: {program.Instructions[i]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"汇编失败: {ex.Message}");
            }
        }
    }
}
