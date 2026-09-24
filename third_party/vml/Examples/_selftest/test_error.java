// test_error.java —— VML 诊断自测用例（**故意含错，不参与编译通过性检查**）
//
// 用途：验证编译器的错误/警告输出、诊断列表、以及编辑器能否显示气泡。
// 预期：1 个错误、**0 个警告**（详见下）。
// 约束：确定性 —— 无随机数、不读文件、不交互、无死循环；错误来自源码本身。
//
// ⚠ `unused` 是刻意留的「未使用变量」探针。java 前端不报未使用变量
//   （该检测只在 CCompiler 里实现），因此本文件只产 1 条错误。
//   java 前端确实有「MCU 模式: synchronized/volatile 被忽略」两条警告，但走的是
//   VMLPlugins.WarningEmitter，被 WarningLevel 门控、默认 0 ⇒ vmlcli 与手机端
//   都不开启（且该通道不带 文件:行:列 与诊断码，编辑器无法定位）。
// ⚠ 本条错误的文件名显示为 `<input>` 而不是真实路径。
public class P {
  public static void main(String[] a) {
    int unused = 5;
    System.out.println("java diag probe");
    int x = undefinedValue;
    System.out.println(x);
  }
}
