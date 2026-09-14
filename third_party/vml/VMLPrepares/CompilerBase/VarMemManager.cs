using System;
using System.Collections.Generic;

namespace CompilerBase
{
    public enum VarScope { Global, Local, Param }

    public class VarInfo
    {
        public string Name;
        public VarScope Scope;
        public int Offset;
        public int Size;
        public int Alignment;
        public string? Label;

        // ====== 类型追踪 ======
        /// <summary>类型名 ("int", "float", "char*", "Pt", "std::vector")</summary>
        public string? TypeName;
        /// <summary>结构体/类/记录类型名（用于字段偏移查询）</summary>
        public string? StructType;
        /// <summary>是否是数组变量</summary>
        public bool IsArray;
        /// <summary>数组元素大小（字节），仅 IsArray 时有效</summary>
        public int ArrayElementSize;
        /// <summary>数组元素数量，仅 IsArray 时有效</summary>
        public int ElementCount;
        /// <summary>是否是引用变量</summary>
        public bool IsReference;

        /// <summary>数组第0元素的偏移（跳过 header）</summary>
        public int Element0Offset => IsArray ? Offset + 4 : Offset;

        public VarInfo(string name, VarScope scope, int offset, int size, int alignment, string? label = null)
        {
            Name = name;
            Scope = scope;
            Offset = offset;
            Size = size;
            Alignment = alignment;
            Label = label;
        }
    }

    /// <summary>
    /// 统一变量内存分配管理器 — 处理堆分配（全局变量）、栈分配（局部变量/参数）
    /// 和对齐管理，供所有编译器继承使用
    /// </summary>
    public class VarMemManager
    {
        private readonly int _baseReg;
        private int _localBottom;  // 局部变量底部偏移（从 0 向负方向增长）
        private int _paramTop;     // 参数区顶部偏移（默认正向增长，负向模式时向负方向增长）
        private readonly bool _paramsGrowPositive;
        private readonly int _paramStart; // 保存初始值用于 ResetLocals
        private readonly Dictionary<string, VarInfo> _vars = new Dictionary<string, VarInfo>();

        // ====== 统计追踪 ======
        private int _globalCount, _globalBytes;
        private int _peakLocalCount, _peakLocalBytes;
        private int _peakParamCount, _peakParamBytes;
        private int _currentLocalCount, _currentLocalBytes;
        private int _currentParamCount, _currentParamBytes;

        /// <summary>
        /// 创建 VarMemManager
        /// </summary>
        /// <param name="baseReg">帧指针寄存器号（C 用 12，Go 用 14）</param>
        /// <param name="paramStart">参数起始偏移（C=12 正向，Go=-4 负向）</param>
        public VarMemManager(int baseReg = 12, int paramStart = 12)
        {
            _baseReg = baseReg;
            _paramStart = paramStart;
            _paramsGrowPositive = paramStart > 0;
            _localBottom = 0;
            _paramTop = paramStart;
        }

        // ====== 查询 ======

        public VarInfo GetVar(string name) => _vars[name];
        public bool TryGetVar(string name, out VarInfo v) => _vars.TryGetValue(name, out v!);

        /// <summary>变量是否是数组</summary>
        public bool IsArrayVar(string name) => _vars.TryGetValue(name, out var v) && v.IsArray;

        /// <summary>变量是否是结构体/类类型（通过 StructType 或 TypeName 判断）</summary>
        public bool IsStructVar(string name, Func<string, bool>? isKnownStruct = null)
        {
            if (!_vars.TryGetValue(name, out var v)) return false;
            if (v.StructType != null) return true;
            if (isKnownStruct != null && v.TypeName != null && isKnownStruct(v.TypeName)) return true;
            return false;
        }

        /// <summary>变量是否是引用类型</summary>
        public bool IsRefVar(string name) => _vars.TryGetValue(name, out var v) && v.IsReference;

        /// <summary>获取变量类型名</summary>
        public string? GetVarType(string name) => _vars.TryGetValue(name, out var v) ? v.TypeName : null;

        /// <summary>获取数组第0元素的偏移（跳过4字节header）</summary>
        public int GetArrayElement0Offset(string name)
        {
            if (_vars.TryGetValue(name, out var v) && v.IsArray)
                return v.Offset + 4;
            return 0;
        }

        // ====== 分配 ======

        public VarInfo AllocGlobal(string name, int size)
        {
            int alignment = AlignmentForSize(size);
            var info = new VarInfo(name, VarScope.Global, 0, size, alignment, name);
            _vars[name] = info;
            _globalCount++;
            _globalBytes += size;
            return info;
        }

        public VarInfo AllocLocal(string name, int size)
        {
            return AllocLocal(name, size, null, false, 0, 0, false);
        }

        /// <summary>带类型信息的局部变量分配</summary>
        public VarInfo AllocLocal(string name, int size, string? typeName,
            bool isArray = false, int elementSize = 0, int elementCount = 0, bool isRef = false)
        {
            int alignment = AlignmentForSize(size);
            int offset = Align(_localBottom - size, alignment);
            _localBottom = offset;
            var info = new VarInfo(name, VarScope.Local, offset, size, alignment)
            {
                TypeName = typeName,
                IsArray = isArray,
                ArrayElementSize = elementSize,
                ElementCount = elementCount,
                IsReference = isRef
            };
            _vars[name] = info;
            _currentLocalCount++;
            _currentLocalBytes += size;
            return info;
        }

        /// <summary>分配数组局部变量：header(4B长度) + elementCount * elementSize</summary>
        public VarInfo AllocLocalArray(string name, int elementSize, int elementCount, string? typeName = null)
        {
            int totalSize = 4 + elementCount * elementSize; // header + data
            var info = AllocLocal(name, totalSize, typeName, true, elementSize, elementCount);
            return info;
        }

        public VarInfo AllocParam(string name, int size)
        {
            return AllocParam(name, size, null, false);
        }

        /// <summary>带类型信息的参数分配</summary>
        public VarInfo AllocParam(string name, int size, string? typeName, bool isRef = false)
        {
            int alignment = AlignmentForSize(size);
            int offset;
            if (_paramsGrowPositive)
            {
                offset = Align(_paramTop, alignment);
                _paramTop = offset + size;
            }
            else
            {
                // 负向增长：参数从帧指针向负方向排列 (Go/R14 风格)
                offset = Align(_paramTop - size, alignment);
                _paramTop = offset;
            }
            var info = new VarInfo(name, VarScope.Param, offset, size, alignment)
            {
                TypeName = typeName,
                IsReference = isRef
            };
            _vars[name] = info;
            _currentParamCount++;
            _currentParamBytes += size;
            return info;
        }

        /// <summary>手动指定参数偏移（兼容 C 编译器隐式 struct 返回指针等场景）</summary>
        public VarInfo AllocParamAt(string name, int offset, int size)
        {
            int alignment = AlignmentForSize(size);
            var info = new VarInfo(name, VarScope.Param, offset, size, alignment);
            _vars[name] = info;
            _currentParamCount++;
            _currentParamBytes += size;
            if (_paramsGrowPositive)
            {
                if (offset + size > _paramTop)
                    _paramTop = offset + size;
            }
            else
            {
                if (offset < _paramTop)
                    _paramTop = offset;
            }
            return info;
        }

        // ====== 格式化 ======

        public string FormatOffset(string name)
        {
            var v = _vars[name];
            return FormatOffset(v.Offset);
        }

        public string FormatOffset(int offset)
        {
            return offset >= 0 ? $"R{_baseReg}+{offset}" : $"R{_baseReg}{offset}";
        }

        // ====== 生命周期 ======

        public void ResetLocals()
        {
            // 更新峰值统计
            if (_currentLocalCount > _peakLocalCount) _peakLocalCount = _currentLocalCount;
            if (_currentLocalBytes > _peakLocalBytes) _peakLocalBytes = _currentLocalBytes;
            if (_currentParamCount > _peakParamCount) _peakParamCount = _currentParamCount;
            if (_currentParamBytes > _peakParamBytes) _peakParamBytes = _currentParamBytes;

            var globals = new List<VarInfo>();
            foreach (var v in _vars.Values)
                if (v.Scope == VarScope.Global)
                    globals.Add(v);
            _vars.Clear();
            foreach (var g in globals)
                _vars[g.Name] = g;
            _localBottom = 0;
            _paramTop = _paramStart;
            _currentLocalCount = 0;
            _currentLocalBytes = 0;
            _currentParamCount = 0;
            _currentParamBytes = 0;
        }

        public void Reset()
        {
            _vars.Clear();
            _localBottom = 0;
            _paramTop = _paramStart;
            _globalCount = 0;
            _globalBytes = 0;
            _peakLocalCount = 0;
            _peakLocalBytes = 0;
            _peakParamCount = 0;
            _peakParamBytes = 0;
            _currentLocalCount = 0;
            _currentLocalBytes = 0;
            _currentParamCount = 0;
            _currentParamBytes = 0;
        }

        // ====== 属性 ======

        public int LocalFrameSize => -_localBottom;
        public int ParamRegionSize => _paramsGrowPositive ? _paramTop - _paramStart : _paramStart - _paramTop;

        /// <summary>总栈帧大小 = 局部变量区 + 参数区（用于 ENTER/SUB 指令）</summary>
        public int TotalFrameSize => LocalFrameSize + ParamRegionSize;

        // ====== 统计查询 ======

        public int GlobalVarCount => _globalCount;
        public int GlobalVarBytes => _globalBytes;
        public int PeakLocalVarCount => _peakLocalCount;
        public int PeakLocalVarBytes => _peakLocalBytes;
        public int PeakParamCount => _peakParamCount;
        public int PeakParamBytes => _peakParamBytes;
        public int TotalTrackedBytes => _globalBytes + _peakLocalBytes + _peakParamBytes;
        public int TotalTrackedVars => _globalCount + _peakLocalCount + _peakParamCount;

        /// <summary>是否启用变量统计日志输出（仅编译调试时启用）</summary>
        public static bool EnableStatsLog { get; set; }

        /// <summary>输出变量分配统计到控制台</summary>
        public void LogStats()
        {
            // 先保存当前函数统计到峰值
            if (_currentLocalCount > _peakLocalCount) _peakLocalCount = _currentLocalCount;
            if (_currentLocalBytes > _peakLocalBytes) _peakLocalBytes = _currentLocalBytes;
            if (_currentParamCount > _peakParamCount) _peakParamCount = _currentParamCount;
            if (_currentParamBytes > _peakParamBytes) _peakParamBytes = _currentParamBytes;

            if (EnableStatsLog)
                Console.Error.WriteLine($"[VarMem] 变量统计: 全局={_globalCount}({_globalBytes}B)  局部峰值={_peakLocalCount}({_peakLocalBytes}B)  参数峰值={_peakParamCount}({_peakParamBytes}B)  总计={TotalTrackedVars}变量/{TotalTrackedBytes}B");
        }

        // ====== 静态工具 ======

        /// <summary>对齐 offset。正数向正无穷对齐，负数向负无穷对齐</summary>
        public static int Align(int offset, int alignment)
        {
            if (alignment <= 1) return offset;
            if (offset >= 0)
                return ((offset + alignment - 1) / alignment) * alignment;
            else
                return ((offset - alignment + 1) / alignment) * alignment;
        }

        /// <summary>根据类型大小返回对齐要求：1→1, 2→2, 4→4, 8→4, 其他→4</summary>
        public static int AlignmentForSize(int size)
        {
            return size switch
            {
                1 => 1,
                2 => 2,
                4 => 4,
                8 => 4,
                _ => 4
            };
        }
    }
}
