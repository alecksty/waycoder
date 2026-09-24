-- test_error.lua —— VML 跨语言「诊断」判据（**故意编不过**）
--
-- ⚠ 这是**刻意的诊断用例**，不参与任何「编译通过性」检查
--   （`vml-out-probe` 那套逐字节比对跑的是同目录的 out.*，别把本文件混进去）。
-- 用途：验证前端是否报出 error / warning、诊断带上哪些要素（文件 / 行 / 列 / 诊断码）、
--   以及编辑器能否据此画气泡。
-- 期望产出：1 个 error、0 个 warning。
--
-- ⚠ **0 个警告不是漏写**：`WarnUnused` 全仓**只有 CCompiler 在调**
--   （CompilerBase/CodeGeneratorBase.cs:130），lua 前端没有任何警告出口。
--   下面的 unusedCount 是**刻意留的探针**：前端哪天补上未使用诊断，它会立刻现形。
-- ⚠ **错在函数名而不是变量名**：本前端不检查未声明的变量（查不到就发一个 0 继续生成），
--   所以「未定义变量」这类探针在这里静默无事。错误只能由**调用未声明的函数**触发 ——
--   编译期不报，靠链接器报 `未定义的函数 'func_<名>'（引用 1 次）`（名字前会加 `func_`），
--   位置只到 `文件:行`（**无列、无诊断码**）。
function main()
    local unusedCount = 5     -- 探针：期望 warning（当前前端产不出）
    print("test_error.lua")   -- 这一句是正常的
    undefined_func(1)         -- error：未声明的函数（链接期报出）
end
main()
