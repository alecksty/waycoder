using VMLAssembler;

namespace VMLTranslators
{
    /// <summary>
    /// 虚拟机转译器中间层 (JVM, DotNET, Wasm).
    /// 共用特性: 栈机模型, 局部变量槽, 方法/函数包装声明.
    /// </summary>
    public abstract class TranslatorVM : BaseTranslator
    {
        protected TranslatorVM(VmlProgram vmlProgram) : base(vmlProgram) { }

        /// <summary>方法声明开始格式: ".method ..." 或 "(func ..."</summary>
        protected virtual string MethodDeclOpen => ".method";
        /// <summary>方法声明结束格式: ".end method" 或 ")"</summary>
        protected virtual string MethodDeclClose => ".end method";
        /// <summary>局部变量声明格式: ".local R{N}" 或 "(local $r{N} i32)"</summary>
        protected virtual string LocalDecl(int index) => $".local R{index}";
        /// <summary>操作数引用格式（栈机使用不同的操作数表示）</summary>
        protected virtual string GetStackOperand(Operand op) => MapRegister(SafeToInt(op));

        /// <summary>发出方法开始</summary>
        protected void EmitMethodOpen(string name)
        {
            Emit(MethodDeclOpen.Replace("{name}", name));
        }

        /// <summary>发出方法结束</summary>
        protected void EmitMethodClose()
        {
            Emit($"        {MethodDeclClose}");
        }

        /// <summary>发出所有 VML 虚拟寄存器的局部变量声明</summary>
        protected void EmitLocals(int count = 16)
        {
            for (int i = 0; i < count; i++)
                Emit($"        {LocalDecl(i)}");
        }

        /// <summary>栈机二元运算: POP src2, POP src1, OP, PUSH result</summary>
        protected void EmitStackBinary(string op)
        {
            Emit($"        {op}");
        }

        /// <summary>标签名清理：替换 VM 不允许的特殊字符</summary>
        protected static string SanitizeLabel(string name)
            => name.Replace("-", "_").Replace(".", "_").Replace("@", "_at_").Replace(" ", "_");
    }
}
