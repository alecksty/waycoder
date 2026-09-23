using CompilerBase;
using System.Collections.Generic;

namespace BasicCompiler
{
    public partial class Parser : ParserBase<Token, TokenType>
    {
        /// <summary>
        /// 裸名（不带括号）能不能当"无参函数调用" —— 返回**登记时的原名**（查不到返回 null）。
        ///
        /// <para>
        /// 为什么不能直接 `declaredFunctions.Contains(名字)`：登记用的是**带类型后缀**的名字
        /// （`DECLARE FUNCTION CalcDelay! ()` ⇒ 表里是 `CalcDelay!`），而 QBasic 的调用点
        /// **可以不带后缀**写（GORILLA 的 `MachSpeed = CalcDelay` 就是这一档）⇒ 直接查必然落空、
        /// 退化成"同名变量"（= 0，**一个错都不报**）。
        /// </para>
        /// <para>
        /// 返回原名而不是布尔值：调用点要用它去生成 `CALL`，而标签是按原名算出来的
        /// （`!`/`#`/`%`/`&`/`$` 后缀在符号名换算里各有去处），拿裸名编出来的是另一个标签
        /// ⇒ 链接报「未定义的函数」。
        /// </para>
        /// </summary>
        private string? ResolveDeclaredFunction(string name)
        {
            if (declaredFunctions.Contains(name)) return name;
            foreach (var suffix in new[] { "!", "#", "%", "&", "$" })
                if (declaredFunctions.Contains(name + suffix)) return name + suffix;
            return null;
        }

        /// <summary>
        /// **这一行是不是已经结束了** —— 「逗号/分号分隔的语句列表」的收尾判据。
        ///
        /// <para>
        /// BASIC 的**换行就是语句分隔符**，语句不跨行（没有 PRINT 续行、没有 `_` 续行）。
        /// 而词法器**不产生换行 token**，于是那些"收一项、见到分隔符就继续收下一项"的
        /// 循环会把**下一行的语句**当成自己的下一个列表项吃掉 —— 而这种"吃掉"**不报错**：
        /// 赋值语句 `K$ = INKEY$` 落到"表达式"那一支，被编成 `K$ = INKEY$` 这个**比较表达式**
        /// 打印出来（看上去只像多打了一个 `0`），**赋值本身一次都没执行**。
        /// </para>
        /// <para>
        /// 实测最小复现（`.scratch/gor/mre_semi.bas`）：
        /// <code>
        /// K$ = ""
        /// PRINT "A1 K=["; K$; "]";     ' ← 结尾分号
        /// K$ = "9"                     ' ← 被吞进 PRINT，K$ 永远还是 ""
        /// </code>
        /// 输出是 `A1 K=[]0` —— 赋值没发生、比较结果被当列表项打了出来。
        /// GORILLA.BAS 的 `GetNum#` 正是这个形状（`PRINT Result$; CHR$(95); "    ";`
        /// 紧跟 `Kbd$ = INKEY$`）⇒ `Kbd$` 恒为空串、`Result$` 永远长不起来，
        /// 游戏卡在 `Angle:` 且**按键其实一直被读走**（读走的是 PRINT 里那次 INKEY$）。
        /// </para>
        /// <para>
        /// ⚠ 判据是 <b>行号</b>而不是"下一个 token 不认识"：`K$` 是完全合法的表达式开头，
        /// 只有行号能说明"这是另一条语句了"。原来的 `ParsePrintStatement` 里有一条
        /// **只认数字**的同款判断（当时是为了拦住下一行的行号），被这条通用判据取代。
        /// </para>
        /// </summary>
        /// <param name="startLine">语句首 token 的行号（`PRINT`/`READ`/`DATA` 自己那一行）</param>
        private bool LineEnded(int startLine) => Peek().Line > startLine;
    }
}
