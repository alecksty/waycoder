using GenDev.Models;

namespace GenDev.Generators
{
    public class LadderCodeGenerator : ICodeGenerator
    {
        public string Language => "ladder";
        public string FileExtension => ".ld";

        public string Generate(DeviceModel device)
        {
            var code = new System.Text.StringBuilder();
            
            // 梯形图文件头
            code.AppendLine($"(*");
            code.AppendLine($" * {device.Metadata.Name}梯形图定义");
            code.AppendLine($" * 生成自: {device.Metadata.Manufacturer}/{device.Metadata.Family}/{device.Metadata.Name}");
            code.AppendLine($" * 版本: {device.Metadata.Version}");
            code.AppendLine($" * 日期: {device.Metadata.Date:yyyy-MM-dd}");
            code.AppendLine($" * 作者: {device.Metadata.Author}");
            if (!string.IsNullOrEmpty(device.Metadata.Description))
                code.AppendLine($" * 描述: {device.Metadata.Description}");
            code.AppendLine($" * CPU架构: {device.Cpu.Architecture}");
            code.AppendLine($" * 位宽: {device.Cpu.Bits}位");
            code.AppendLine($" * 时钟频率: {device.Cpu.Clock.Default} Hz");
            code.AppendLine($" *)");
            code.AppendLine();
            
            // IEC 61131-3梯形图程序开始
            code.AppendLine($"PROGRAM {device.Metadata.Name.Replace("-", "_")}");
            code.AppendLine($"VAR");
            code.AppendLine();
            
            // 输入变量定义 (I/O映射)
            code.AppendLine($"    (* 输入变量 *)");
            if (device.Pins?.PinList.Count > 0)
            {
                foreach (var pin in device.Pins.PinList)
                {
                    if (pin.Type.ToLower().Contains("input") || pin.Type.ToLower().Contains("in"))
                    {
                        code.AppendLine($"    {CodeGeneratorHelper.SanitizeUpper(pin.Name)} AT %I{pin.Number} : BOOL; (* {pin.Description} *)");
                    }
                }
            }
            code.AppendLine();
            
            // 输出变量定义
            code.AppendLine($"    (* 输出变量 *)");
            if (device.Pins?.PinList.Count > 0)
            {
                foreach (var pin in device.Pins.PinList)
                {
                    if (pin.Type.ToLower().Contains("output") || pin.Type.ToLower().Contains("out"))
                    {
                        code.AppendLine($"    {CodeGeneratorHelper.SanitizeUpper(pin.Name)} AT %Q{pin.Number} : BOOL; (* {pin.Description} *)");
                    }
                }
            }
            code.AppendLine();
            
            // 内存变量定义 (寄存器映射)
            code.AppendLine($"    (* 内存变量 *)");
            if (device.Cpu.Registers.RegisterList.Count > 0)
            {
                foreach (var reg in device.Cpu.Registers.RegisterList)
                {
                    string type = GetLadderTypeForSize(reg.Size);
                    code.AppendLine($"    {CodeGeneratorHelper.SanitizeUpper(reg.Name)} AT %M{reg.Address} : {type}; (* {reg.Description} *)");
                    
                    // 位域变量
                    if (reg.BitFields.Count > 0)
                    {
                        foreach (var bit in reg.BitFields)
                        {
                            string bitName = $"{CodeGeneratorHelper.SanitizeUpper(reg.Name)}_{CodeGeneratorHelper.SanitizeUpper(bit.Name)}";
                            code.AppendLine($"    {bitName} AT %M{reg.Address}.{bit.Bit} : BOOL; (* {bit.Description} *)");
                        }
                    }
                }
            }
            code.AppendLine();
            
            // 外设变量定义
            if (device.Peripherals?.PeripheralList.Count > 0)
            {
                code.AppendLine($"    (* 外设变量 *)");
                foreach (var peripheral in device.Peripherals.PeripheralList)
                {
                    code.AppendLine($"    (* {peripheral.Description} *)");
                    
                    // 外设基地址变量
                    code.AppendLine($"    {CodeGeneratorHelper.SanitizeUpper(peripheral.Name)}_BASE AT %MW{peripheral.Base} : WORD;");
                    
                    // 外设寄存器变量
                    foreach (var reg in peripheral.Registers)
                    {
                        string type = GetLadderTypeForSize(reg.Size);
                        string regName = $"{CodeGeneratorHelper.SanitizeUpper(peripheral.Name)}_{CodeGeneratorHelper.SanitizeUpper(reg.Name)}";
                        code.AppendLine($"    {regName} AT %MW{reg.Address} : {type};");
                        
                        // 位域变量
                        if (reg.BitFields.Count > 0)
                        {
                            foreach (var bit in reg.BitFields)
                            {
                                string bitName = $"{regName}_{CodeGeneratorHelper.SanitizeUpper(bit.Name)}";
                                code.AppendLine($"    {bitName} AT %MW{reg.Address}.{bit.Bit} : BOOL; (* {bit.Description} *)");
                            }
                        }
                    }
                }
                code.AppendLine();
            }
            
            // 中断变量定义
            if (device.Interrupts?.InterruptList.Count > 0)
            {
                code.AppendLine($"    (* 中断变量 *)");
                foreach (var interrupt in device.Interrupts.InterruptList)
                {
                    code.AppendLine($"    INT_{CodeGeneratorHelper.SanitizeUpper(interrupt.Name)} : BOOL; (* {interrupt.Description} *)");
                }
                code.AppendLine();
            }
            
            code.AppendLine($"END_VAR");
            code.AppendLine();
            
            // 梯形图程序体
            code.AppendLine($"BEGIN");
            code.AppendLine($"    (* {device.Metadata.Name}初始化梯形图 *)");
            code.AppendLine($"    (* 网络 1: 设备初始化 *)");
            code.AppendLine($"    [初始化完成]");
            code.AppendLine();
            
            // 生成简单的梯形图逻辑示例
            code.AppendLine($"    (* 网络 2: 输入监控示例 *)");
            if (device.Pins?.PinList.Count > 0)
            {
                int network = 2;
                foreach (var pin in device.Pins.PinList)
                {
                    if (pin.Type.ToLower().Contains("input"))
                    {
                        code.AppendLine($"    (* 网络 {network++}: 监控{pin.Name} *)");
                        code.AppendLine($"    --|{CodeGeneratorHelper.SanitizeUpper(pin.Name)}|--( )--");
                        code.AppendLine();
                    }
                }
            }
            
            // 生成寄存器监控逻辑
            if (device.Cpu.Registers.RegisterList.Count > 0)
            {
                code.AppendLine($"    (* 网络 3: 寄存器监控 *)");
                int regCount = 0;
                foreach (var reg in device.Cpu.Registers.RegisterList)
                {
                    if (regCount < 5) // 只显示前5个寄存器
                    {
                        code.AppendLine($"    --|{CodeGeneratorHelper.SanitizeUpper(reg.Name)}>0|--(监控灯)--");
                        regCount++;
                    }
                }
                code.AppendLine();
            }
            
            // 生成中断处理逻辑
            if (device.Interrupts?.InterruptList.Count > 0)
            {
                code.AppendLine($"    (* 网络 4: 中断处理 *)");
                foreach (var interrupt in device.Interrupts.InterruptList)
                {
                    code.AppendLine($"    --|INT_{CodeGeneratorHelper.SanitizeUpper(interrupt.Name)}|--[中断处理]--");
                }
                code.AppendLine();
            }
            
            code.AppendLine($"END_PROGRAM");
            
            return code.ToString();
        }
        
        private string GetLadderTypeForSize(int size)
        {
            return size switch
            {
                1 => "BYTE",
                2 => "WORD",
                4 => "DWORD",
                8 => "LWORD",
                _ => $"ARRAY[0..{size-1}] OF BOOL"
            };
        }
    }
}