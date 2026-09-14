using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VMLToHex.Formatters;

/// <summary>
/// .NET PE 文件生成器 — 将 VMB 数据嵌入到有效的 .NET PE 程序集中
/// </summary>
public static class DotNetPeWriter
{
    public static byte[] Write(byte[] vmbData, string moduleName, bool isExe)
    {
        if (string.IsNullOrEmpty(moduleName)) moduleName = "VMLProgram";
        var w = new PeBuilder(moduleName, vmbData, isExe);
        return w.Build();
    }

    class PeBuilder
    {
        readonly string _name;
        readonly byte[] _vmb;
        readonly bool _isExe;

        public PeBuilder(string name, byte[] vmb, bool isExe)
        {
            _name = name;
            _vmb = vmb;
            _isExe = isExe;
        }

        // --- Heaps ---
        readonly List<(string s, int offset)> _strs = new() { ("", 0) };
        readonly List<(byte[] data, int offset)> _blobs = new();
        int _strOff = 1, _blobOff;

        int S(string s)
        {
            var f = _strs.FirstOrDefault(x => x.s == s);
            if (f.s != null) return f.offset;
            int o = _strOff;
            _strs.Add((s, o));
            _strOff += Encoding.UTF8.GetByteCount(s) + 1;
            return o;
        }

        int B(byte[] d)
        {
            int o = _blobOff;
            _blobs.Add((d, o));
            _blobOff += Cl(d.Length).Length + d.Length;
            return o;
        }

        // --- Compressed int ---
        static byte[] Cl(int v)
        {
            if (v <= 0x7F) return [(byte)v];
            if (v <= 0x3FFF) return [(byte)(0x80 | (v >> 8)), (byte)v];
            return [(byte)(0xC0 | (v >> 24)), (byte)(v >> 16), (byte)(v >> 8), (byte)v];
        }

        static byte[] U2(ushort v) => [(byte)(v >> 8), (byte)v];
        static byte[] U4(int v) => [(byte)v, (byte)(v >> 8), (byte)(v >> 16), (byte)(v >> 24)];

        // Index size: 2 bytes if heap < 0x10000, else 4
        byte[] I(int off, int size) => size < 0x10000 ? U2((ushort)off) : U4(off);

        // Coded index: TypeDefOrRef (2-bit tag: 0=TypeDef,1=TypeRef,2=TypeSpec)
        byte[] TOR(int idx, int tag) => U2((ushort)((idx << 2) | tag));

        // Coded index: ResolutionScope (2-bit tag: 0=Module,1=ModuleRef,2=AssemblyRef,3=TypeRef)
        byte[] RS(int idx, int tag) => U2((ushort)((idx << 2) | tag));

        // Coded index: HasCustomAttribute (5-bit tag)
        byte[] HCA(int idx, int tag) => U2((ushort)((idx << 5) | tag));

        // Coded index: CustomAttributeType (3-bit tag: 2=MemberRef)
        byte[] CAT(int idx, int tag) => U2((ushort)((idx << 3) | tag));

        // --- Table building ---
        public byte[] Build()
        {
            // String indices
            int sEmpty = S("");
            int sModule = S(_name);
            int sProgram = S("Program");
            int sMain = S("Main");
            int sArgs = S("args");
            int sObject = S("Object");
            int sSystem = S("System");
            int sRuntime = S("System.Runtime");

            // Signatures
            // Main: static void(string[])
            int sigMain = B([0x00, 0x01, 0x01, 0x1D, 0x0E]);
            // AssemblyRef PKT for System.Runtime
            int sigPkt = B([0x7C, 0xEC, 0x85, 0xD7, 0xBE, 0xA7, 0x79, 0x8E]);

            // Row builders (all columns are 2 bytes in our minimal case)
            byte[] Row(params byte[][] cols) => cols.SelectMany(c => c).ToArray();

            // Module: Generation(2)=0, Name, Mvid(GuidIdx), EncId, EncBaseId
            var modRow = Row(U2(0), I(sModule, _strOff), U2(0), U2(0), U2(0));

            // TypeRef: ResolutionScope, Name, Namespace (for System.Object)
            // AssemblyRef[1] tag=2 in ResolutionScope → (1<<2)|2 = 6
            var trRow = Row(RS(1, 2), I(sObject, _strOff), I(sSystem, _strOff));

            // TypeDef: Flags(4), Name, Namespace, Extends(TypeRef[1]), FieldList, MethodList
            // Extends = TypeRef[1] → (1<<2)|1 = 5
            // Flags: public(0x00000001) | auto(0x00010000) | ansi(0x00020000) | class(0x00100000) = 0x00130001
            var tdRow = Row(U4(0x00100001), I(sProgram, _strOff), I(sEmpty, _strOff),
                TOR(1, 1), U2(1), U2(1));

            // MethodDef: RVA(4), ImplFlags(2), Flags(2), Name, Signature, ParamList
            // RVA: will be patched after we know the IL code offset
            int methodRva = 0x2000 + 0x48 + 200; // rough estimate
            var mdRow = Row(U4(methodRva), U2(0), U2(0x0096),
                I(sMain, _strOff), I(sigMain, _blobOff), U2(1));

            // Param: Flags(2), Sequence(2), Name
            var prRow = Row(U2(0), U2(1), I(sArgs, _strOff));

            // Assembly: HashAlg(4), Major(2), Minor(2), Build(2), Rev(2), Flags(4), PubKey, Name, Culture
            var asRow = Row(U4(0x8004), U2(1), U2(0), U2(0), U2(0), U4(0),
                I(0, _blobOff), I(sModule, _strOff), I(0, _strOff));

            // AssemblyRef: Major(2), Minor(2), Build(2), Rev(2), Flags(4), PKT, Name, Culture, HashValue
            var arRow = Row(U2(10), U2(0), U2(0), U2(0), U4(0),
                I(sigPkt, _blobOff), I(sRuntime, _strOff), I(0, _strOff), I(0, _blobOff));

            // Build heap data
            var strHeap = _strs.OrderBy(x => x.offset).Select(x =>
            {
                var b = Encoding.UTF8.GetBytes(x.s);
                return (byte[])[.. b, 0];
            }).SelectMany(x => x).ToArray();

            var blbHeap = _blobs.OrderBy(x => x.offset).Select(x =>
            {
                var h = Cl(x.data.Length);
                return (byte[])[.. h, .. x.data];
            }).SelectMany(x => x).ToArray();

            // Build #~ stream
            using var ts = new MemoryStream();
            using var tw = new BinaryWriter(ts);
            tw.Write(0u); // Reserved
            tw.Write((byte)2); tw.Write((byte)0); // Version
            // HeapSizes: 0 if str < 65536
            byte hs = 0;
            if (strHeap.Length >= 0x10000) hs |= 1;
            if (blbHeap.Length >= 0x10000) hs |= 4;
            tw.Write(hs);
            tw.Write((byte)1); // Reserved

            // Valid tables
            long valid = 0;
            valid |= 1L << 0x00; // Module
            valid |= 1L << 0x01; // TypeRef
            valid |= 1L << 0x02; // TypeDef
            valid |= 1L << 0x06; // MethodDef
            valid |= 1L << 0x08; // Param
            valid |= 1L << 0x20; // Assembly
            valid |= 1L << 0x23; // AssemblyRef
            tw.Write(valid);
            tw.Write(0L); // Sorted

            // Row counts (all compressed int, small so 1 byte each)
            void Rc(int c) => tw.Write(Cl(c));
            Rc(1); // Module
            Rc(1); // TypeRef
            Rc(1); // TypeDef
            Rc(1); // MethodDef
            Rc(1); // Param
            Rc(1); // Assembly
            Rc(1); // AssemblyRef

            // Write rows
            tw.Write(modRow);
            tw.Write(trRow);
            tw.Write(tdRow);
            tw.Write(mdRow);
            tw.Write(prRow);
            tw.Write(asRow);
            tw.Write(arRow);

            var tblStream = ts.ToArray();

            // GUID heap (just 16 zero bytes - Mvid will point here)
            var guidHeap = new byte[16];
            for (int i = 0; i < 16; i++) guidHeap[i] = 0;

            // Metadata root
            using var mm = new MemoryStream();
            using var mw = new BinaryWriter(mm);
            mw.Write(0x424A5342u); // BSJB
            mw.Write((ushort)2); mw.Write((ushort)5); // version
            mw.Write((ushort)0); // reserved
            var v = "v4.0.30319\0";
            mw.Write((ushort)v.Length);
            mw.Write(Encoding.UTF8.GetBytes(v));
            int vp = (4 - (v.Length % 4)) % 4;
            mw.Write(new byte[vp]);
            mw.Write((ushort)0); // flags
            mw.Write((ushort)2); // 2 streams: #~, #Strings

            // Calculate metadata root header size
            int rootSize = 4 + 2 + 2 + 2 + 2 + v.Length + vp + 2 + 2;

            // #~ stream
            int so1 = rootSize;
            int sz1 = tblStream.Length;

            // #Strings stream
            int so2 = so1 + sz1;
            int sz2 = strHeap.Length;

            // Stream header: #~
            mw.Write(so1); mw.Write(sz1);
            mw.Write(Encoding.ASCII.GetBytes("#~\0")); mw.Write((byte)0);

            // Stream header: #Strings
            mw.Write(so2); mw.Write(sz2);
            mw.Write(Encoding.ASCII.GetBytes("#Strings\0"));

            // Write streams
            mw.Write(tblStream);
            mw.Write(strHeap);

            var meta = mm.ToArray();

            // === IL code: just `ret` ===
            var ilCode = new byte[] { 0x06, 0x2A }; // tiny header + ret

            // Calculate section layout
            int cliSize = 72;
            int metaOff = cliSize;
            int metaAligned = (meta.Length + 3) & ~3;
            int ilOff = metaOff + metaAligned;
            int vmbOff = ilOff + ilCode.Length;
            int secCont = vmbOff + _vmb.Length;
            int secFile = (secCont + 511) & ~511;
            int secVirt = (secCont + 4095) & ~4095;

            // Fix MethodDef RVA
            int rva = 0x2000 + ilOff;

            // === Build PE ===
            int dos = 128;
            int secTblOff2 = dos + 4 + 20 + 224; // PE sig + COFF + Opt
            int hdrFile = (secTblOff2 + 40 + 511) & ~511;
            int secRva = 0x2000;

            using var pe = new MemoryStream();
            using var pw = new BinaryWriter(pe);

            // DOS header
            pw.Write(new byte[64]);
            pe.Seek(0, SeekOrigin.Begin);
            pw.Write((byte)0x4D); pw.Write((byte)0x5A);
            pe.Seek(60, SeekOrigin.Begin);
            pw.Write(dos);

            // DOS stub at offset 64 (after 64-byte DOS header)
            pe.Seek(64, SeekOrigin.Begin);
            pw.Write(Encoding.ASCII.GetBytes("This program requires .NET Runtime.\r\n\0"));
            pe.Seek(dos, SeekOrigin.Begin);

            // PE signature
            pw.Write(0x00004550u);

            // COFF header
            pw.Write((ushort)0x014C); // I386
            pw.Write((ushort)1); // sections
            pw.Write(0u); pw.Write(0u); pw.Write(0u);
            pw.Write((ushort)224); pw.Write((ushort)(_isExe ? 0x0102 : 0x2102));

            // Optional header PE32
            pw.Write((ushort)0x010B);
            pw.Write((byte)10); pw.Write((byte)0);
            pw.Write(secFile); // SizeOfCode
            pw.Write(0); pw.Write(0);
            pw.Write(0); // AddressOfEntryPoint 由 CLI Header 管理
            pw.Write(secRva); pw.Write(0);
            pw.Write(0x00400000u); // ImageBase
            pw.Write(0x1000u); pw.Write(0x0200u); // alignments
            pw.Write((ushort)4); pw.Write((ushort)0);
            pw.Write((ushort)0); pw.Write((ushort)0);
            pw.Write((ushort)6); pw.Write((ushort)0);
            pw.Write(0u);
            pw.Write(secVirt); // SizeOfImage
            pw.Write(hdrFile); // SizeOfHeaders
            pw.Write(0u); // CheckSum
            pw.Write((ushort)(_isExe ? 3 : 2)); // Subsystem
            pw.Write((ushort)0x0500); // IL_ONLY
            pw.Write(0x100000u); pw.Write(0x1000u); pw.Write(0x100000u); pw.Write(0x1000u);
            pw.Write(0u); pw.Write(16u);

            // Data directories (16)
            for (int i = 0; i < 16; i++)
            {
                if (i == 14) { pw.Write(secRva); pw.Write(cliSize); }
                else { pw.Write(0); pw.Write(0); }
            }

            // Fix: CLI header RVA should be secRva + cliOff, not just secRva
            // Let me rebuild the data directory correctly.
            // I'll seek back and fix it.
            long ddPos = pe.Position;
            // Actually, CLI header is at secRva + 0 (beginning of section)
            // Let me fix by seeking back to the right directory entry

            // Section: .text
            pe.Seek(secTblOff2, SeekOrigin.Begin);
            pw.Write(Encoding.ASCII.GetBytes(".text\0\0\0"));
            pw.Write(secVirt);
            pw.Write(secRva);
            pw.Write(secFile);
            pw.Write(hdrFile);
            pw.Write(0); pw.Write(0); pw.Write((ushort)0); pw.Write((ushort)0);
            pw.Write(0x60000020u);

            pe.Seek(hdrFile, SeekOrigin.Begin);

            // CLI header
            pw.Write(72u); // Cb
            pw.Write((ushort)2); pw.Write((ushort)5);
            pw.Write(secRva + metaOff); // Metadata RVA
            pw.Write(meta.Length);
            pw.Write(0u); // Flags
            pw.Write(0x06000001); // EntryPointToken: MethodDef[1]

            // Resources (8 zero dwords)
            for (int i = 0; i < 8; i++) pw.Write(0L);

            // Fix Data Directory[14] (CLI Header entry)
            long saved = pe.Position;
            // Optional header PE32 starts at dos + 4 + 20 = dos + 24
            // DataDir[14] starts at optOff + 96 + 14*8
            int optStart = dos + 4 + 20;
            int dd14Off = optStart + 96 + 14 * 8;
            pe.Seek(dd14Off, SeekOrigin.Begin);
            pw.Write(secRva); // RVA of CLI Header = secRva + 0
            pw.Write(cliSize);
            pe.Seek(saved, SeekOrigin.Begin);

            // Metadata
            pw.Write(meta);

            // Align
            int pad = metaAligned - meta.Length;
            pw.Write(new byte[pad]);

            // IL code
            pw.Write(ilCode);

            // VMB data
            pw.Write(_vmb);

            // Pad to file alignment
            int fin = hdrFile + secFile;
            pe.SetLength(fin);

            return pe.ToArray();
        }
    }
}
