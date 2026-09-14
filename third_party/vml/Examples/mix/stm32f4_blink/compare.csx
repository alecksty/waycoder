using System;
using System.IO;

var ours = File.ReadAllBytes(@"D:\Source\gitee\visualC\VML\vml\Examples\mix\stm32f4_blink\output\ours.bin");
var keil = File.ReadAllBytes(@"D:\Source\gitee\visualC\VML\vml\Examples\mix\stm32f4_blink\output\k2.bin");

Console.WriteLine($"Ours: {ours.Length} bytes, Keil: {keil.Length} bytes");
int diffs = 0;
for (int i = 0; i < Math.Min(ours.Length, keil.Length); i++)
{
    if (ours[i] != keil[i])
    {
        Console.WriteLine($"  DIFF @0x{i:X4}: ours=0x{ours[i]:X2} keil=0x{keil[i]:X2}");
        if (++diffs > 50) { Console.WriteLine("  ... (truncated)"); break; }
    }
}
if (diffs == 0) Console.WriteLine("IDENTICAL!");
