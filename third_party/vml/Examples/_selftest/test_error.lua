-- test_error.lua —— VML 跨语言「诊断」判据（**故意编不过**）
-- test_error.lua -- VML cross-language "diagnostics" probe (**deliberately does not compile**)
--
-- ⚠ 这是**刻意的诊断用例**，不参与任何「编译通过性」检查
-- ⚠ This is a **deliberate diagnostic case**; it takes part in no "compile-cleanliness" check
--   （`vml-out-probe` 那套逐字节比对跑的是同目录的 out.*，别把本文件混进去）。
--   (the byte-for-byte comparison in `vml-out-probe` runs against the out.* files in this same directory -- do not mix this file into it).
-- 用途：验证 error / warning 的报出与要素（文件 / 行 / 列 / 诊断码）+ 编辑器气泡。
-- Purpose: verify that error / warning are reported with their fields (file / line / col / diag-code) + the editor squiggle.
-- 期望产出：1 个 warning + 1 个 error。
-- Expected output: 1 warning + 1 error.
--
-- ⚠ **warning 刻意用「缺失头文件」造**：预处理器那条 `[Preprocessor_IncludeNotFound]`（1201）
-- ⚠ **The warning is deliberately made with a "missing header"**: the preprocessor's `[Preprocessor_IncludeNotFound]` (1201)
--   是**默认就开**的真诊断，自带 文件:行 + 诊断码；它只产 warning、不产 error ——
--   is a **default-on** real diagnostic carrying file:line + diag-code; it produces only a warning, not an error --
--   本文件的 error 仍是下面那句未声明引用。「未使用变量」那类警告本前端产不出
--   this file's error is still the undeclared reference below. The frontend cannot produce "unused variable" warnings
--   （`WarnUnused` 全仓**只有 CCompiler 在调**，CompilerBase/CodeGeneratorBase.cs:130），
--   (`WarnUnused` is called **only by CCompiler** in this whole repo, CompilerBase/CodeGeneratorBase.cs:130),
--   下面的 unusedCount 是**刻意留的探针**：前端哪天补上未使用诊断，它会立刻现形。
--   the unusedCount below is a **deliberately kept probe**: the day the frontend adds unused diagnostics, it will show up at once.
-- ⚠ **错误来自未声明的函数，不是变量**：本前端不检查未声明的变量（隐式全局是合法语义），
-- ⚠ **The error comes from an undeclared function, not a variable**: this frontend does not check undeclared variables (implicit globals are legal semantics),
--   错误只能由**调用未声明的函数**触发 —— 由链接器报出 `未定义的函数 'func_<名>'`，
--   the error can only be triggered by **calling an undeclared function** -- the linker reports `undefined function 'func_<name>'`,
--   位置只到 `文件:行`（**无列、无诊断码**）。
--   positioned only to `file:line` (**no column, no diag-code**).
#include <nosuchheader.h>
function main()
    local unusedCount = 5     -- 探针：期望 warning（当前前端产不出）
    -- probe: expected warning (the current frontend cannot produce it)
    print("test_error.lua")   -- 这一句是正常的
    -- this statement is normal
    undefined_func(1)         -- error：未声明的函数（链接期报出）
    -- error: undeclared function (reported at link time)
end
main()
