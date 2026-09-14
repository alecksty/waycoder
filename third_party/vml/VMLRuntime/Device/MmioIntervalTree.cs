using System.Collections.Generic;

namespace VMLRuntime.Device
{
    public class MmioIntervalTree
    {
        private readonly List<Entry> _entries = new();

        private struct Entry
        {
            public uint Start;
            public uint Size;
            public IMmioDevice Device;
        }

        public void Insert(uint start, uint size, IMmioDevice device)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (start < e.Start + e.Size && start + size > e.Start)
                {
                    throw new VmlException($"MMIO 地址冲突: 新设备 [0x{start:X8}+0x{size:X8}] 与已注册设备 [0x{e.Start:X8}+0x{e.Size:X8}] 重叠");
                }
            }
            _entries.Add(new Entry { Start = start, Size = size, Device = device });
        }

        public void Remove(IMmioDevice device)
        {
            _entries.RemoveAll(e => e.Device == device);
        }

        public IMmioDevice Find(uint address)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (address >= e.Start && address < e.Start + e.Size)
                    return e.Device;
            }
            return null;
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}
