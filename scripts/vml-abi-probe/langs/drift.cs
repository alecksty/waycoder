// 栈漂移探针（C#）：GenerateConsoleWriteLine 那条路「压了不清」（另一条 stdlibLabel 路
// 本来就由调用方清，两条路约定此前互相矛盾）。判据：`DRIFT=190`（0+1+…+19）。
class Drift
{
    static void Main()
    {
        int s = 0;
        for (int i = 0; i < 20; i++) { System.Console.Write("."); s = s + i; }
        System.Console.Write("\nDRIFT=");
        System.Console.WriteLine(s);
    }
}
