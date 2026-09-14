namespace CCompiler
{
    public enum CallingConvention
    {
        /// <summary>cdecl (默认): 全部参数从右到左压栈，调用者清理</summary>
        Cdecl,
        /// <summary>CCv2: 前4参数 R0-R3，第5+ 从右到左压栈，调用者清理</summary>
        C,
        /// <summary>Pascal: 参数从左到右压栈，被调用者清理</summary>
        Pascal,
        /// <summary>BASIC: 参数通过 R0-R3 传递（最多4个），无栈清理</summary>
        Basic,
        /// <summary>stdcall: 全部参数从右到左压栈，被调用者清理</summary>
        Stdcall,
        /// <summary>fastcall: 前2参数 R0-R1，其余从右到左压栈，调用者清理</summary>
        Fastcall,
    }
}
