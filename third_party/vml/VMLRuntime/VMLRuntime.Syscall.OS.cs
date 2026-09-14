using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

namespace VMLRuntime
{
    public partial class VmRuntime
    {
        // ======== OS模式扩展 SYSCALL 实现 ========

        private readonly object _executionLock = new();
        private readonly object _dictLock = new(); // 保护 _threads/_mutexes/_conds/_sockets 并发访问
        private readonly Dictionary<int, System.Threading.Thread> _threads = new();
        private int _nextThreadId = 1;
        private readonly Dictionary<int, System.Threading.Mutex> _mutexes = new();
        private int _nextMutexId = 1;
        private readonly Dictionary<int, System.Threading.EventWaitHandle> _conds = new();
        private int _nextCondId = 1;
        private readonly Dictionary<int, System.Net.Sockets.Socket> _sockets = new();
        private int _nextSocketFd = 1;

        private readonly Dictionary<int, bool> _threadExited = new(); // threadId → exited flag

        private void ExecuteThreadCreate()
        {
            int fnAddr = registers[0];
            int stackSize = Math.Max(1024, registers[1]);
            int threadId = _nextThreadId++;
            // 每个线程需要独立的 VM 上下文以避免竞态条件
            // 当前实现克隆内存和寄存器快照给子线程使用
            var threadMemory = new byte[memory.Length];
            Array.Copy(memory, threadMemory, memory.Length);
            var threadRegs = (int[])registers.Clone();
            _threadExited[threadId] = false;
            var t = new System.Threading.Thread(() => {
                lock (_executionLock)
                {
                    var origMem = memory;
                    var origRegs = registers;
                    memory = threadMemory;
                    registers = threadRegs;
                    pc = fnAddr;
                    try { Run(); }
                    catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"Thread crash: {ex.Message}"); /* mark exited */ }
                    memory = origMem;
                    registers = origRegs;
                }
                lock (_threadExited) { _threadExited[threadId] = true; System.Threading.Monitor.PulseAll(_threadExited); }
            });
            t.IsBackground = true;
            t.Start();
            _threads[threadId] = t;
            registers[0] = threadId;
        }

        private void ExecuteThreadExit()
        {
            int threadId = registers[0];
            if (threadId == 0) threadId = GetCurrentThreadId();
            lock (_threadExited) { _threadExited[threadId] = true; System.Threading.Monitor.PulseAll(_threadExited); }
            // 从活动线程列表中移除
            if (_threads.TryGetValue(threadId, out var t))
            {
                _threads.Remove(threadId);
            }
            registers[0] = ErrorCodes.SUCCESS;
        }

        private void ExecuteThreadJoin()
        {
            int threadId = registers[0];
            if (_threads.TryGetValue(threadId, out var t))
            {
                try { t.Join(); registers[0] = ErrorCodes.SUCCESS; }
                catch { registers[0] = ErrorCodes.INVALID_PARAMETER; }
            }
            else
            {
                // 如果线程已退出并被移除, 检查 exited 标志
                bool exited = false;
                lock (_threadExited) { exited = _threadExited.ContainsKey(threadId) && _threadExited[threadId]; }
                registers[0] = exited ? ErrorCodes.SUCCESS : ErrorCodes.INVALID_PARAMETER;
            }
        }

        private int GetCurrentThreadId()
        {
            int currentManagedId = System.Environment.CurrentManagedThreadId;
            foreach (var kv in _threads)
            {
                if (kv.Value.ManagedThreadId == currentManagedId) return kv.Key;
            }
            return -1;
        }

        private void ExecuteMutexCreate()
        {
            int id = _nextMutexId++;
            _mutexes[id] = new System.Threading.Mutex();
            registers[0] = id;
        }

        private void ExecuteMutexLock()
        {
            int id = registers[0];
            if (_mutexes.TryGetValue(id, out var m))
            {
                m.WaitOne();
                registers[0] = ErrorCodes.SUCCESS;
            }
            else
                registers[0] = ErrorCodes.INVALID_PARAMETER;
        }

        private void ExecuteMutexUnlock()
        {
            int id = registers[0];
            if (_mutexes.TryGetValue(id, out var m))
            {
                m.ReleaseMutex();
                registers[0] = ErrorCodes.SUCCESS;
            }
            else
                registers[0] = ErrorCodes.INVALID_PARAMETER;
        }

        private void ExecuteCondCreate()
        {
            int id = _nextCondId++;
            _conds[id] = new System.Threading.AutoResetEvent(false);
            registers[0] = id;
        }

        private void ExecuteCondWait()
        {
            int condId = registers[0];
            int mutexId = registers[1];
            if (_conds.TryGetValue(condId, out var cond) && _mutexes.TryGetValue(mutexId, out var mtx))
            {
                // 释放互斥锁, 等待条件变量, 重新获取互斥锁
                try { mtx.ReleaseMutex(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"CondWait mutex release failed: {ex.Message}"); }
                // 单线程 VM 中 cond 可能永远不会被 signal，用超时防止永久阻塞
                try { cond.WaitOne(100); registers[0] = ErrorCodes.SUCCESS; }
                catch { registers[0] = ErrorCodes.INVALID_PARAMETER; }
                try { mtx.WaitOne(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"CondWait mutex reacquire failed: {ex.Message}"); }
            }
            else registers[0] = ErrorCodes.INVALID_PARAMETER;
        }

        private void ExecuteCondSignal()
        {
            int condId = registers[0];
            if (_conds.TryGetValue(condId, out var cond))
            {
                cond.Set();
                registers[0] = ErrorCodes.SUCCESS;
            }
            else registers[0] = ErrorCodes.INVALID_PARAMETER;
        }

        private void ExecuteCondBroadcast()
        {
            int condId = registers[0];
            if (_conds.TryGetValue(condId, out var cond))
            {
                // 条件变量 Broadcast: 尽可能唤醒所有等待线程
                // 注: AutoResetEvent 只唤醒一个线程, 此处多轮 Set 尽力模拟 broadcast
                // TODO: 用 ManualResetEventSlim 重构 (需同时修改 Create/Wait/Signal)
                for (int i = 0; i < 10; i++)
                {
                    cond.Set();
                    System.Threading.Thread.Sleep(1);
                }
                registers[0] = ErrorCodes.SUCCESS;
            }
            else registers[0] = ErrorCodes.INVALID_PARAMETER;
        }

        private void ExecuteExec()
        {
            string path = ReadStringFromMemory(registers[0]);
            try
            {
                var p = System.Diagnostics.Process.Start(path);
                registers[0] = p?.Id ?? -1;
            }
            catch
            {
                registers[0] = -1;
            }
        }

        private void ExecuteSocketCreate()
        {
            // domain: R0 (2=AF_INET), type: R1 (1=SOCK_STREAM)
            try
            {
                var sock = new System.Net.Sockets.Socket(
                    registers[0] == 2 ? System.Net.Sockets.AddressFamily.InterNetwork : System.Net.Sockets.AddressFamily.Unspecified,
                    registers[1] == 1 ? System.Net.Sockets.SocketType.Stream : System.Net.Sockets.SocketType.Dgram,
                    System.Net.Sockets.ProtocolType.Tcp);
                int fd = _nextSocketFd++;
                _sockets[fd] = sock;
                registers[0] = fd;
            }
            catch
            {
                registers[0] = -1;
            }
        }

        private void ExecuteSocketBind()
        {
            int fd = registers[0];
            int port = registers[1];
            if (_sockets.TryGetValue(fd, out var sock))
            {
                try
                {
                    sock.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));
                    registers[0] = ErrorCodes.SUCCESS;
                }
                catch { registers[0] = -1; }
            }
            else registers[0] = ErrorCodes.INVALID_PARAMETER;
        }

        private void ExecuteSocketListen()
        {
            int fd = registers[0];
            int backlog = Math.Max(1, registers[1]);
            if (_sockets.TryGetValue(fd, out var sock))
            {
                try { sock.Listen(backlog); registers[0] = ErrorCodes.SUCCESS; }
                catch { registers[0] = -1; }
            }
            else registers[0] = ErrorCodes.INVALID_PARAMETER;
        }

        private void ExecuteSocketAccept()
        {
            int fd = registers[0];
            if (_sockets.TryGetValue(fd, out var sock))
            {
                try
                {
                    var client = sock.Accept();
                    int clientFd = _nextSocketFd++;
                    _sockets[clientFd] = client;
                    registers[0] = clientFd;
                }
                catch { registers[0] = -1; }
            }
            else registers[0] = ErrorCodes.INVALID_PARAMETER;
        }

        private void ExecuteSocketConnect()
        {
            int fd = registers[0];
            int addr = registers[1]; // 内存中的IP字符串地址
            int port = registers[2];
            if (_sockets.TryGetValue(fd, out var sock))
            {
                try
                {
                    string ip = ReadStringFromMemory(addr);
                    sock.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ip), port));
                    registers[0] = ErrorCodes.SUCCESS;
                }
                catch { registers[0] = -1; }
            }
            else registers[0] = ErrorCodes.INVALID_PARAMETER;
        }

        private void ExecuteSocketSend()
        {
            int fd = registers[0];
            int bufAddr = registers[1];
            int len = Math.Min(registers[2], memory.Length - bufAddr);
            if (_sockets.TryGetValue(fd, out var sock) && bufAddr >= 0 && len > 0)
            {
                try
                {
                    byte[] data = new byte[len];
                    for (int i = 0; i < len; i++) data[i] = memory[bufAddr + i];
                    registers[0] = sock.Send(data);
                }
                catch { registers[0] = -1; }
            }
            else registers[0] = ErrorCodes.INVALID_PARAMETER;
        }

        private void ExecuteSocketRecv()
        {
            int fd = registers[0];
            int bufAddr = registers[1];
            int len = Math.Min(registers[2], memory.Length - bufAddr);
            if (_sockets.TryGetValue(fd, out var sock) && bufAddr >= 0 && len > 0)
            {
                try
                {
                    byte[] buffer = new byte[len];
                    int received = sock.Receive(buffer);
                    for (int i = 0; i < received; i++) memory[bufAddr + i] = buffer[i];
                    registers[0] = received;
                }
                catch { registers[0] = -1; }
            }
            else registers[0] = ErrorCodes.INVALID_PARAMETER;
        }

        private void ExecuteSocketClose()
        {
            int fd = registers[0];
            if (_sockets.TryGetValue(fd, out var sock))
            {
                try { sock.Close(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Socket close failed: {ex.Message}"); }
                _sockets.Remove(fd);
                registers[0] = ErrorCodes.SUCCESS;
            }
            else registers[0] = ErrorCodes.INVALID_PARAMETER;
        }

        private void ExecuteDnsResolve()
        {
            int addr = registers[0];
            string hostname = ReadStringFromMemory(addr);
            try
            {
                var hostEntry = System.Net.Dns.GetHostEntry(hostname);
                if (hostEntry.AddressList.Length > 0)
                {
                    string ip = hostEntry.AddressList[0].ToString();
                    // 写入内存作为字符串
                    int ipAddr = AllocateMemory(ip.Length + 1);
                    for (int i = 0; i < ip.Length; i++)
                        memory[ipAddr + i] = (byte)ip[i];
                    memory[ipAddr + ip.Length] = 0;
                    registers[0] = ipAddr;
                }
                else registers[0] = -1;
            }
            catch
            {
                registers[0] = -1;
            }
        }

        private void ExecuteMkDir()
        {
            string path = ReadStringFromMemory(registers[0]);
            try
            {
                System.IO.Directory.CreateDirectory(path);
                registers[0] = ErrorCodes.SUCCESS;
            }
            catch
            {
                registers[0] = ErrorCodes.FILE_ERROR;
            }
        }

        private void ExecuteRemove()
        {
            string path = ReadStringFromMemory(registers[0]);
            try
            {
                if (System.IO.Directory.Exists(path))
                    System.IO.Directory.Delete(path);
                else
                    System.IO.File.Delete(path);
                registers[0] = ErrorCodes.SUCCESS;
            }
            catch
            {
                registers[0] = ErrorCodes.FILE_ERROR;
            }
        }

        private void ExecuteRename()
        {
            string oldPath = ReadStringFromMemory(registers[0]);
            string newPath = ReadStringFromMemory(registers[1]);
            try
            {
                System.IO.File.Move(oldPath, newPath);
                registers[0] = ErrorCodes.SUCCESS;
            }
            catch
            {
                registers[0] = ErrorCodes.FILE_ERROR;
            }
        }

        private void ExecuteGetEnv()
        {
            string name = ReadStringFromMemory(registers[0]);
            string? val = Environment.GetEnvironmentVariable(name);
            if (val != null)
            {
                int addr = AllocateMemory(val.Length + 1);
                for (int i = 0; i < val.Length; i++)
                    memory[addr + i] = (byte)val[i];
                memory[addr + val.Length] = 0;
                registers[0] = addr;
            }
            else
                registers[0] = 0;
        }

        private void ExecuteSetEnv()
        {
            string name = ReadStringFromMemory(registers[0]);
            string val = ReadStringFromMemory(registers[1]);
            Environment.SetEnvironmentVariable(name, val);
            registers[0] = ErrorCodes.SUCCESS;
        }

        private void ExecuteReadDir()
        {
            string path = ReadStringFromMemory(registers[0]);
            int bufAddr = registers[1];
            try
            {
                var entries = System.IO.Directory.GetFileSystemEntries(path);
                int count = Math.Min(entries.Length, 256); // 最多 256 个条目
                int offset = 0;
                for (int i = 0; i < count && offset + 4 <= memory.Length - bufAddr; i++)
                {
                    string name = System.IO.Path.GetFileName(entries[i]);
                    int strAddr = AllocateMemory(name.Length + 1);
                    for (int j = 0; j < name.Length; j++)
                        memory[strAddr + j] = (byte)name[j];
                    memory[strAddr + name.Length] = 0;
                    SetMemory(bufAddr + offset, strAddr);
                    offset += 4;
                }
                registers[0] = count;
            }
            catch
            {
                registers[0] = ErrorCodes.FILE_ERROR;
            }
        }

        private void ExecuteStat()
        {
            string path = ReadStringFromMemory(registers[0]);
            int bufAddr = registers[1];
            try
            {
                if (bufAddr < 0 || bufAddr + 20 > memory.Length)
                {
                    registers[0] = ErrorCodes.MEMORY_OUT_OF_BOUNDS;
                    return;
                }
                if (System.IO.Directory.Exists(path))
                {
                    var di = new System.IO.DirectoryInfo(path);
                    SetMemory(bufAddr, 0);                                          // size = 0 (目录)
                    SetMemory(bufAddr + 4, 1);                                      // mode = 1 (目录)
                    SetMemory(bufAddr + 8, (int)((di.LastWriteTime - new System.DateTime(1970, 1, 1)).TotalSeconds));
                    SetMemory(bufAddr + 12, (int)((di.CreationTime - new System.DateTime(1970, 1, 1)).TotalSeconds));
                    SetMemory(bufAddr + 16, (int)((di.LastAccessTime - new System.DateTime(1970, 1, 1)).TotalSeconds));
                }
                else if (System.IO.File.Exists(path))
                {
                    var fi = new System.IO.FileInfo(path);
                    SetMemory(bufAddr, (int)fi.Length);                              // size
                    SetMemory(bufAddr + 4, 0);                                      // mode = 0 (文件)
                    SetMemory(bufAddr + 8, (int)((fi.LastWriteTime - new System.DateTime(1970, 1, 1)).TotalSeconds));
                    SetMemory(bufAddr + 12, (int)((fi.CreationTime - new System.DateTime(1970, 1, 1)).TotalSeconds));
                    SetMemory(bufAddr + 16, (int)((fi.LastAccessTime - new System.DateTime(1970, 1, 1)).TotalSeconds));
                }
                else
                {
                    registers[0] = ErrorCodes.FILE_NOT_FOUND;
                    return;
                }
                registers[0] = ErrorCodes.SUCCESS;
            }
            catch
            {
                registers[0] = ErrorCodes.FILE_ERROR;
            }
        }

        private void ExecuteGetArgs()
        {
            int bufAddr = registers[0];
            var args = CommandLineArgs.Count > 0 ? CommandLineArgs : new List<string>(Environment.GetCommandLineArgs());
            int count = Math.Min(args.Count, 256);
            // 写入参数数量
            SetMemory(bufAddr, count);
            // 写入每个参数
            int offset = 4;
            for (int i = 0; i < count && offset + 256 < memory.Length; i++)
            {
                int strAddr = AllocateMemory(args[i].Length + 1);
                for (int j = 0; j < args[i].Length; j++)
                    memory[strAddr + j] = (byte)args[i][j];
                memory[strAddr + args[i].Length] = 0;
                SetMemory(bufAddr + offset, strAddr);
                offset += 4;
            }
            registers[0] = count;
        }

    }
}
