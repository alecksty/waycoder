// QBasic/Compiler/Chunk.cs
// 字节码块：指令序列 + 常量表 + 变量表 + 函数表 + 行号映射。
namespace QBasic.Compiler;

public sealed class Chunk
{
    public List<byte> Code { get; } = new();
    public List<object> Constants { get; } = new();
    public List<string> Variables { get; } = new();
    public List<string> Functions { get; } = new();
    // 每条指令对应的源行号（用于运行时错误定位）
    public List<int> Lines { get; } = new();

    public int AddConstant(object value)
    {
        // 复用已存在的常量
        for (int i = 0; i < Constants.Count; i++)
        {
            if (Equals(Constants[i], value)) return i;
        }
        Constants.Add(value);
        return Constants.Count - 1;
    }

    public int AddVariable(string name)
    {
        for (int i = 0; i < Variables.Count; i++)
        {
            if (Variables[i] == name) return i;
        }
        Variables.Add(name);
        return Variables.Count - 1;
    }

    public int AddFunction(string name)
    {
        for (int i = 0; i < Functions.Count; i++)
        {
            if (Functions[i] == name) return i;
        }
        Functions.Add(name);
        return Functions.Count - 1;
    }

    public void Emit(OpCode op, int line)
    {
        Code.Add((byte)op);
        Lines.Add(line);
    }

    public void Emit(OpCode op, int operand, int line)
    {
        Code.Add((byte)op);
        Code.Add((byte)operand);
        Lines.Add(line);
        Lines.Add(line);
    }

    public int Count => Code.Count;

    public int PatchJump(int offset, int target)
    {
        // offset 指向 Jump 指令的操作数字节位置
        Code[offset] = (byte)target;
        return target;
    }
}
