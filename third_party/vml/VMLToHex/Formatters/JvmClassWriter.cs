using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VMLToHex.Formatters;

public static class JvmClassWriter
{
    public static byte[] Write(byte[] data, string className)
    {
        if (string.IsNullOrEmpty(className)) className = "VMLProgram";
        className = Sanitize(className);

        var hex = Convert.ToHexString(data);
        if (Encoding.UTF8.GetByteCount(hex) > 65535)
            throw new InvalidOperationException("数据超过32767字节，JVM格式不支持");

        var cp = new ConstPool();

        var utf = cp.Utf8;
        var cls = (string n) => cp.Class(utf(n));
        var str = (string n) => cp.String(utf(n));
        var nat = (ushort n, ushort d) => cp.Nat(n, d);
        var mr = (ushort c, ushort n) => cp.Mr(c, n);

        ushort cName     = cls(className),
               cObj      = cls("java/lang/Object"),
               cStr      = cls("java/lang/String"),
               cChar     = cls("java/lang/Character"),
               cFile     = cls("java/io/File"),
               cFos      = cls("java/io/FileOutputStream"),
               cRt       = cls("java/lang/Runtime"),
               cExc      = cls("java/lang/Exception"),

               uInit     = utf("<init>"),
               uVoid     = utf("()V"),
               uMain     = utf("main"),
               uMainD    = utf("([Ljava/lang/String;)V"),
               uLen      = utf("length"),
               uLenD     = utf("()I"),
               uCat      = utf("charAt"),
               uCatD     = utf("(I)C"),
               uDig      = utf("digit"),
               uDigD     = utf("(II)I"),
               uCrt      = utf("createTempFile"),
               uCrtD     = utf("(Ljava/lang/String;Ljava/lang/String;)Ljava/io/File;"),
               uGPa      = utf("getPath"),
               uGPaD     = utf("()Ljava/lang/String;"),
               uWrt      = utf("write"),
               uWrtD     = utf("([B)V"),
               uCls      = utf("close"),
               uGRt      = utf("getRuntime"),
               uGRtD     = utf("()Ljava/lang/Runtime;"),
               uExc      = utf("exec"),
               uExcD     = utf("([Ljava/lang/String;)Ljava/lang/Process;"),
               uCode     = utf("Code"),
               uExcA     = utf("Exceptions"),

               sVml      = str("vml"),
               sVmb      = str(".vmb"),
               sRun      = str("vmlrun"),
               sHex      = str(hex);

        ushort
            mObjInit = mr(cObj, nat(uInit, uVoid)),
            mStrLen  = mr(cStr, nat(uLen, uLenD)),
            mStrCat  = mr(cStr, nat(uCat, uCatD)),
            mChDig   = mr(cChar, nat(uDig, uDigD)),
            mFCrt    = mr(cFile, nat(uCrt, uCrtD)),
            mFGPa    = mr(cFile, nat(uGPa, uGPaD)),
            mFInit   = mr(cFos, nat(uInit, utf("(Ljava/io/File;)V"))),
            mFWrt    = mr(cFos, nat(uWrt, uWrtD)),
            mFCls    = mr(cFos, nat(uCls, uVoid)),
            mRGrt    = mr(cRt, nat(uGRt, uGRtD)),
            mRExc    = mr(cRt, nat(uExc, uExcD));

        using var ms = new MemoryStream();
        using var w = new BigEndianWriter(ms);

        w.U4(0xCAFEBABE); w.U2(0); w.U2(52);
        cp.Write(w);

        w.U2(0x0021);   // ACC_PUBLIC | ACC_SUPER
        w.U2(cName); w.U2(cObj);
        w.U2(0); w.U2(0); w.U2(2);

        // <init>()V
        w.U2(0x0001); w.U2(uInit); w.U2(uVoid); w.U2(1);
        w.U2(uCode); w.U4(12);
        w.U2(1); w.U2(1);
        var ic = new byte[] { 0x2A, 0xB7, 0, 0, 0xB1 };
        PutU2(ic, 2, mObjInit);
        w.U4(5); w.U2(1); w.U2(1); w.U4(5); w.Bytes(ic);
        w.U2(0); w.U2(0);

        // main([Ljava/lang/String;)V
        w.U2(0x0009); w.U2(uMain); w.U2(uMainD); w.U2(2);
        w.U2(uExcA); w.U4(4); w.U2(1); w.U2(cExc);

        var bc = BuildBytecode(sHex, sVml, sVmb, sRun, cStr,
            mStrLen, mStrCat, mChDig,
            mFCrt, mFGPa, mFInit, mFWrt, mFCls,
            mRGrt, mRExc,
            cFos, cStr);

        w.U2(uCode);
        w.U4(12 + (uint)bc.Length);
        w.U2(6); w.U2(8);
        w.U4((uint)bc.Length); w.Bytes(bc);
        w.U2(0); w.U2(0);
        w.U2(0);

        return ms.ToArray();
    }

    static byte[] BuildBytecode(
        ushort sHex, ushort sVml, ushort sVmb, ushort sRun, ushort cStr,
        ushort mStrLen, ushort mStrCat, ushort mChDig,
        ushort mFCrt, ushort mFGPa,
        ushort mFInit, ushort mFWrt, ushort mFCls,
        ushort mRGrt, ushort mRExc,
        ushort cFos, ushort cStrIdx)
    {
        // Locals: 0=args, 1=hex, 2=data, 3=i, 4=hi, 5=lo, 6=tmp, 7=fos
        var b = new List<byte>();
        void U1(byte v) => b.Add(v);
        void U2(ushort v) { b.Add((byte)(v >> 8)); b.Add((byte)v); }

        // 0: String hex = "..."
        U1(0x12); U2(sHex);    // ldc
        U1(0x4C);               // astore_1

        // 4: byte[] data = new byte[hex.length()>>1]
        U1(0x2B);               // aload_1
        U1(0xB6); U2(mStrLen); // invokevirtual length
        U1(0x04);               // iconst_1
        U1(0x7A);               // ishr
        U1(0x08); U1(0x08);    // newarray byte
        U1(0x4D);               // astore_2

        // i = 0
        U1(0x03);               // iconst_0
        U1(0x3E);               // istore_3

        int lp = b.Count;       // loop start
        U1(0x1D);               // iload_3
        U1(0x2C);               // aload_2
        U1(0xBE);               // arraylength
        int jp = b.Count;
        U1(0xA2); U1(0); U1(0); // if_icmpge (patched)

        // hi = Character.digit(hex.charAt(i<<1), 16)
        U1(0x2B); U1(0x1D); U1(0x04); U1(0x78);
        U1(0xB6); U2(mStrCat);
        U1(0x10); U1(16);
        U1(0xB8); U2(mChDig);
        U1(0x36); U1(4);       // istore 4

        // lo = Character.digit(hex.charAt(i<<1|1), 16)
        U1(0x2B); U1(0x1D); U1(0x04); U1(0x78);
        U1(0x04); U1(0x60);
        U1(0xB6); U2(mStrCat);
        U1(0x10); U1(16);
        U1(0xB8); U2(mChDig);
        U1(0x36); U1(5);       // istore 5

        // data[i] = (byte)((hi<<4) | lo)
        U1(0x15); U1(4);       // iload 4
        U1(0x05);               // iconst_4
        U1(0x78);               // ishl
        U1(0x15); U1(5);       // iload 5
        U1(0x60);               // ior
        U1(0x91);               // i2b
        U1(0x2C); U1(0x1D);    // aload_2, iload_3
        U1(0x54);               // bastore

        U1(0x84); U1(3); U1(1);// iinc 3, 1
        int go = b.Count;
        U1(0xA7);
        U2((ushort)((lp - (go + 2)) & 0xFFFF));

        // patch if_icmpge
        int ol = b.Count;
        int jd = ol - jp - 3;
        b[jp + 1] = (byte)(jd >> 8);
        b[jp + 2] = (byte)jd;

        // File tmp = File.createTempFile("vml", ".vmb");
        U1(0x12); U2(sVml);
        U1(0x12); U2(sVmb);
        U1(0xB8); U2(mFCrt);
        U1(0x3A); U1(6);       // astore 6 (tmp)

        // FileOutputStream fos = new FileOutputStream(tmp);
        U1(0xBB); U2(cFos);    // new
        U1(0x59);               // dup
        U1(0x19); U1(6);       // aload 6 (tmp)
        U1(0xB7); U2(mFInit);  // invokespecial
        U1(0x3A); U1(7);       // astore 7 (fos)

        // fos.write(data);
        U1(0x19); U1(7);       // aload 7
        U1(0x2C);               // aload_2 (data)
        U1(0xB6); U2(mFWrt);

        // fos.close();
        U1(0x19); U1(7);
        U1(0xB6); U2(mFCls);

        // Runtime.getRuntime().exec(new String[]{"vmlrun", tmp.getPath()});
        U1(0xB8); U2(mRGrt);
        U1(0x05);               // iconst_2
        U1(0xBD); U2(cStrIdx); // anewarray String
        U1(0x59);               // dup
        U1(0x03);               // iconst_0
        U1(0x12); U2(sRun);    // ldc "vmlrun"
        U1(0x53);               // aastore
        U1(0x59);               // dup
        U1(0x04);               // iconst_1
        U1(0x19); U1(6);       // aload 6 (tmp)
        U1(0xB6); U2(mFGPa);   // invokevirtual getPath
        U1(0x53);               // aastore
        U1(0xB6); U2(mRExc);   // invokevirtual exec
        U1(0x57);               // pop
        U1(0xB1);               // return

        return b.ToArray();
    }

    static string Sanitize(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s) if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
        if (sb.Length == 0 || char.IsDigit(sb[0])) sb.Insert(0, 'C');
        if (sb.Length == 0) sb.Append("VMLProgram");
        return sb.ToString();
    }

    static void PutU2(byte[] a, int i, ushort v) { a[i] = (byte)(v >> 8); a[i + 1] = (byte)v; }
}

internal class ConstPool
{
    readonly List<(byte tag, byte[] d)> _e = new();
    readonly Dictionary<string, ushort> _u = new(StringComparer.Ordinal);

    public ushort Add(byte t, byte[] d) { _e.Add((t, d)); return (ushort)_e.Count; }

    public ushort Utf8(string s)
    {
        if (_u.TryGetValue(s, out var i)) return i;
        i = Add(1, Encoding.UTF8.GetBytes(s));
        _u[s] = i; return i;
    }

    public ushort Class(ushort n) => Add(7, [(byte)(n >> 8), (byte)n]);
    public ushort String(ushort n) => Add(8, [(byte)(n >> 8), (byte)n]);
    public ushort Nat(ushort n, ushort d) => Add(12, [(byte)(n >> 8), (byte)n, (byte)(d >> 8), (byte)d]);
    public ushort Mr(ushort c, ushort n) => Add(10, [(byte)(c >> 8), (byte)c, (byte)(n >> 8), (byte)n]);

    public void Write(BigEndianWriter w)
    {
        w.U2((ushort)(_e.Count + 1));
        foreach (var (t, d) in _e)
        {
            w.U1(t);
            if (t == 1) { w.U2((ushort)d.Length); w.Bytes(d); }
            else w.Bytes(d);
        }
    }
}

internal class BigEndianWriter : IDisposable
{
    readonly BinaryWriter _w;
    public BigEndianWriter(Stream s) => _w = new BinaryWriter(s);
    public void Dispose() => _w.Dispose();
    public void U1(byte v) => _w.Write(v);
    public void U2(ushort v) { _w.Write((byte)(v >> 8)); _w.Write((byte)v); }
    public void U4(int v) { _w.Write((byte)(v >> 24)); _w.Write((byte)(v >> 16)); _w.Write((byte)(v >> 8)); _w.Write((byte)v); }
    public void U4(uint v) => U4((int)v);
    public void Bytes(byte[] v) => _w.Write(v);
}
