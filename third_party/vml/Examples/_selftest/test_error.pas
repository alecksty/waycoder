{ test_error.pas —— VML 诊断自测用例（**故意含错，不参与编译通过性检查**）
  test_error.pas -- a VML diagnostics self-test case (**deliberately wrong; not part of any compile-cleanliness check**)

  用途：验证编译器的错误/警告输出、诊断列表、以及编辑器能否显示气泡。
  Purpose: check the compiler's error/warning output, the diagnostic list, and whether the editor can show hints.
  预期：1 个错误、**0 个警告**（详见下）。
  Expected: 1 error, **0 warnings** (see below).
  约束：确定性 —— 无随机数、不读文件、不交互、无死循环；错误来自源码本身。
  Constraint: deterministic -- no random numbers, no file reads, no interaction, no infinite loop; the error comes from the source itself.

  ⚠ `unused_var` 是刻意留的「未使用变量」探针。Pascal 前端不报未使用变量
  ⚠ `unused_var` is a deliberately left "unused variable" probe. The Pascal frontend does not report unused variables
    （WarnUnused 全仓只有 CCompiler 在调），因此本文件只产 1 条错误。
    (WarnUnused is only ever called by CCompiler in this repo), so this file produces exactly 1 error.
  ⚠ 本注释里**一个花括号都不能出现** —— Pascal 的块注释就是花括号，正文里写一个
  ⚠ **Not one brace may appear** inside this comment -- Pascal block comments are braces, so writing a
    右花括号会当场结束注释、把后面的散文当成源码（实测报过「未知字符: `」）。
    closing brace in the prose ends the comment right there and the rest of the prose is read as source (measured: it reported "unknown character: `").
    所以下面提到编译指令时只写美元号那半边（$ENDIF / $IFDEF），不写完整形式。
    That is why the directives below are written with only the dollar-sign half ($ENDIF / $IFDEF), never in full.
  ⚠ 正因为本文件不含任何编译指令，PascalCompiler.CompileFile 就不登记行号映射
  ⚠ Precisely because this file contains no directives, PascalCompiler.CompileFile registers no line-number mapping
    ⇒ 诊断的文件名显示为 <input> 而不是真实路径（实测：随便加一个多余的 $ENDIF
    => diagnostics show the file name as <input> instead of the real path (measured: adding one stray $ENDIF
    就会变回真实路径 —— 但那是坏语法，不值得为文件名往示例里塞）。
    makes the real path come back -- but that is bad syntax, not worth putting into an example just for the file name).
  ⚠ Pascal 的指令告警确实存在，但它进的是 CompileFile 本地那个诊断袋，只有
  ⚠ Pascal's directive warnings do exist, but they go into the local diagnostic bag inside CompileFile, and only
    **解析阶段的错误**才会让它一起被渲染；本文件的错误来自代码生成阶段（另一个袋）
    **parse-phase errors** make them get rendered together with it; this file's error comes from the code-generation phase (a different bag)
    ⇒ 告警会被丢掉。
    => the warnings get dropped. }

program test_error;
var
  unused_var: Integer;
begin
  unused_var := 5;
  WriteLn(undefined_thing);
end.
