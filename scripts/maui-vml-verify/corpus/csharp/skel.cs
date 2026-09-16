// skel.cs —— VML 骨架程序（C#）。
//
// 习惯用法抄自 Examples/csharp/racer.cs、snake.cs：class + static 方法 + Main 入口
// （CSharpCompiler/CodeGenerator.cs:841 把 Main 注册成 main 入口），
// 数组在 Main 里显式赋初值，不依赖字段初始化顺序；颜色写负数十进制（示例里全是这么写的，
// 0xFFFF0000 = -65536）。
//
// 打印：System.Console.Write → PrintStr（无换行）；System.Console.WriteLine(整数) → PrintlnInt（含换行）。
// 两条都用 System. 全名 —— 只有 System_Console_Write/WriteLine 那条分支才会按参数类型选
// PrintInt/PrintlnInt（CodeGenerator.cs:996-1002），写成 Console.WriteLine(s) 会把整数当字符串指针。
// （ResolveMemberName 把 System.Console.WriteLine 这种嵌套成员名拼成 System_Console_WriteLine。）
class Skel
{
    static int inc(int x) { return x + 1; }

    static void Main()
    {
        int[] a = { 1, 2, 3, 4 };
        int s = 0;
        for (int i = 0; i < 4; i++)
        {
            a[i] = inc(a[i]);
            s = s + a[i];
        }
        ui_rect(10, 10, 50, 50, -65536, 1, 0, 0);
        ui_present();
        System.Console.Write("SKEL-SUM=");
        System.Console.WriteLine(s);
    }
}
