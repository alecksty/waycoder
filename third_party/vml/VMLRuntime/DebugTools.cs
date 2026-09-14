using System;
using System.Text;

namespace VMLRuntime
{
    /// <summary>
    /// VML运行时调试工具
    /// </summary>
    public static class DebugTools
    {
        /// <summary>
        /// 格式化寄存器状态
        /// </summary>
        /// <param name="registers">寄存器数组</param>
        /// <param name="floatRegisters">浮点寄存器数组</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatRegisters(int[] registers, float[] floatRegisters)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 寄存器状态 ===");
            
            // 通用寄存器
            sb.AppendLine("通用寄存器:");
            for (int i = 0; i < registers.Length; i++)
            {
                sb.AppendLine($"  R{i:D2}: 0x{registers[i]:X8} ({registers[i]})");
            }
            
            // 特殊寄存器
            sb.AppendLine();
            sb.AppendLine("特殊寄存器:");
            sb.AppendLine($"  SP (R13): 0x{registers[13]:X8} ({registers[13]})");
            sb.AppendLine($"  BP (R14): 0x{registers[14]:X8} ({registers[14]})");
            sb.AppendLine($"  RA (R15): 0x{registers[15]:X8} ({registers[15]})");
            
            // 浮点寄存器
            if (floatRegisters != null && floatRegisters.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("浮点寄存器:");
                for (int i = 0; i < floatRegisters.Length; i++)
                {
                    sb.AppendLine($"  F{i}: {floatRegisters[i]:F6}");
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化内存区域
        /// </summary>
        /// <param name="memory">内存数组</param>
        /// <param name="address">起始地址</param>
        /// <param name="length">长度</param>
        /// <returns>格式化后的字符串</returns>
        public static string FormatMemory(byte[] memory, int address, int length)
        {
            if (address < 0 || address >= memory.Length)
                return $"无效地址: 0x{address:X8}";
            
            var sb = new StringBuilder();
            sb.AppendLine($"=== 内存转储 (0x{address:X8} - 0x{address + length - 1:X8}) ===");
            
            int end = Math.Min(address + length, memory.Length);
            for (int i = address; i < end; i += 16)
            {
                // 地址
                sb.Append($"0x{i:X8}: ");
                
                // 十六进制
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < end)
                        sb.Append($"{memory[i + j]:X2} ");
                    else
                        sb.Append("   ");
                }
                
                sb.Append(" ");
                
                // ASCII
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < end)
                    {
                        byte b = memory[i + j];
                        sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
                    }
                    else
                    {
                        sb.Append(' ');
                    }
                }
                
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化错误信息
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <param name="context">上下文信息</param>
        /// <returns>格式化后的错误信息</returns>
        public static string FormatError(int errorCode, string context = "")
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 错误信息 ===");
            sb.AppendLine($"错误码: {errorCode}");
            sb.AppendLine($"描述: {ErrorCodes.GetDescription(errorCode)}");
            sb.AppendLine($"类别: {ErrorCodes.GetCategory(errorCode)}");
            
            if (!string.IsNullOrEmpty(context))
            {
                sb.AppendLine($"上下文: {context}");
            }
            
            sb.AppendLine($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化系统调用信息
        /// </summary>
        /// <param name="syscallNum">系统调用号</param>
        /// <param name="registers">寄存器数组</param>
        /// <param name="result">结果</param>
        /// <returns>格式化后的系统调用信息</returns>
        public static string FormatSyscall(int syscallNum, int[] registers, int result)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== 系统调用 #{syscallNum} ===");
            sb.AppendLine($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"结果: {result} (0x{result:X8})");
            
            if (result < 0)
            {
                sb.AppendLine($"错误: {ErrorCodes.GetDescription(result)}");
            }
            
            // 显示相关寄存器
            sb.AppendLine("相关寄存器:");
            switch (syscallNum)
            {
                case 1: // 输出字符串
                    sb.AppendLine($"  R0 (字符串地址): 0x{registers[0]:X8}");
                    break;
                case 2: // 输入字符串
                    sb.AppendLine($"  R0 (缓冲区地址): 0x{registers[0]:X8}");
                    sb.AppendLine($"  R1 (缓冲区大小): {registers[1]}");
                    break;
                case 110: // 打开文件
                    sb.AppendLine($"  R0 (文件名地址): 0x{registers[0]:X8}");
                    sb.AppendLine($"  R1 (打开模式): {registers[1]}");
                    break;
                case 111: // 关闭文件
                    sb.AppendLine($"  R0 (文件句柄): {registers[0]}");
                    break;
                case 112: // 读取文件
                    sb.AppendLine($"  R0 (文件句柄): {registers[0]}");
                    sb.AppendLine($"  R1 (缓冲区地址): 0x{registers[1]:X8}");
                    sb.AppendLine($"  R2 (读取大小): {registers[2]}");
                    break;
                case 113: // 写入文件
                    sb.AppendLine($"  R0 (文件句柄): {registers[0]}");
                    sb.AppendLine($"  R1 (数据地址): 0x{registers[1]:X8}");
                    sb.AppendLine($"  R2 (写入大小): {registers[2]}");
                    break;
                case 114: // 文件定位
                    sb.AppendLine($"  R0 (文件句柄): {registers[0]}");
                    sb.AppendLine($"  R1 (偏移量): {registers[1]}");
                    break;
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 创建堆栈跟踪
        /// </summary>
        /// <param name="registers">寄存器数组</param>
        /// <param name="memory">内存数组</param>
        /// <param name="depth">深度</param>
        /// <returns>堆栈跟踪信息</returns>
        public static string CreateStackTrace(int[] registers, byte[] memory, int depth = 10)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 堆栈跟踪 ===");
            
            int sp = registers[13]; // 栈指针
            int bp = registers[14]; // 基址指针
            
            sb.AppendLine($"栈指针 (SP): 0x{sp:X8}");
            sb.AppendLine($"基址指针 (BP): 0x{bp:X8}");
            
            // 显示栈帧
            int currentBP = bp;
            int frameCount = 0;
            
            while (currentBP > 0 && currentBP < memory.Length && frameCount < depth)
            {
                // 读取返回地址（BP+4）
                if (currentBP + 4 < memory.Length)
                {
                    int returnAddress = BitConverter.ToInt32(memory, currentBP + 4);
                    sb.AppendLine($"帧 #{frameCount}: BP=0x{currentBP:X8}, RA=0x{returnAddress:X8}");
                    
                    // 读取前一个BP（BP+0）
                    currentBP = BitConverter.ToInt32(memory, currentBP);
                    frameCount++;
                }
                else
                {
                    break;
                }
            }
            
            if (frameCount == 0)
            {
                sb.AppendLine("无有效的堆栈帧");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 创建性能分析报告
        /// </summary>
        /// <param name="instructionCount">指令计数</param>
        /// <param name="syscallCount">系统调用计数</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>性能报告</returns>
        public static string CreatePerformanceReport(
            long instructionCount, 
            long syscallCount, 
            DateTime startTime, 
            DateTime endTime)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 性能分析报告 ===");
            
            TimeSpan duration = endTime - startTime;
            double instructionsPerSecond = instructionCount / duration.TotalSeconds;
            double syscallsPerSecond = syscallCount / duration.TotalSeconds;
            
            sb.AppendLine($"执行时间: {duration.TotalMilliseconds:F2} ms");
            sb.AppendLine($"指令总数: {instructionCount}");
            sb.AppendLine($"系统调用总数: {syscallCount}");
            sb.AppendLine($"指令执行速度: {instructionsPerSecond:F0} 指令/秒");
            sb.AppendLine($"系统调用频率: {syscallsPerSecond:F2} 次/秒");
            sb.AppendLine($"平均指令时间: {(duration.TotalMilliseconds * 1000) / instructionCount:F2} 微秒/指令");
            
            return sb.ToString();
        }
    }
}