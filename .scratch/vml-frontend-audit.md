# VML 22 前端横向体检（下标读写 / 操作数方向）

> 2026-09-17 只读审计。目的是给「至少 20 种语言能写游戏」这个目标排出修复顺序。
> 验收语料：`scripts/maui-vml-verify/corpus/<lang>/skel.*`，期望输出恰好 `SKEL-SUM=14`。
> 桌面迭代工具：`dotnet build .scratch/vmlcli -c Release` 然后
> `dotnet .scratch/vmlcli/bin/Release/net10.0/vmlcli.dll <源文件>`（与手机端逐字等价，已 5/5 实测）。

## 一、结果表

| 语言 | 下标赋值 | 下标复合赋值 | 下标读取 | 证据 |
|---|---|---|---|---|
| Basic | ✅有 | N/A（无 `+=`）| ✅ | `Expressions.cs:617-619` `MOVE @R0, R3` |
| C | ✅有 | ✅有（脱糖）| ✅ | `Expressions.cs:621-631` |
| C++ | ✅有（含 +4）| ❌缺（算完就丢）| ✅ | 写 `:1650-1674`；复合 `:1543` 走 `WrapTargetExpr`（`[]` 给不出真左值）|
| C# | ✅有 | ⚠️只存右值 | ✅ | 写 `CodeGenerator.cs:605-634`；`IndexExpression` 分支 `:605` 早于复合分支 `:659` |
| Dart | ❌缺（静默丢）| ❌缺 | ❌**无下标语法** | `Parser.cs:561-564` `if (left is VarNode v) …; return right;`；`:566-574` 同款 |
| D | ❌缺（写到错地方）| ❌缺 | ⚠️无 +4 头 | `Parser.cs:570-573` → `new AssignNode(idx.Name, right)`（给数组变量本身赋值）|
| Forth | ✅有 | N/A | ❌**自赋值** | 读 `Operations.cs:565-566` **`MOVE R0, R0`**、`:585-586` `MOVEB R0, R0` |
| Fortran | ✅有 | N/A | ✅ | `Statements.cs:414-447` |
| Go | ✅有（0015 已修）| ⚠️只存右值 | ✅（0015 已修）| 复合 `Statements.cs:519` 只认 `Identifier` |
| Java | ❌缺（无分支）| ❌缺 | ❌错（读到数组头）| 见下「三个独立缺陷」|
| JavaScript | ✅有（含 +4）| ❌缺 | ✅ | 复合 `Expressions.cs:215` 只认 `VariableExpression` |
| Kotlin | ❌缺（**load 当 store**）| ❌缺 | ⚠️0016 已修但仍不全 | `CodeGenerator.cs:256` `MOVE R1, [MEMORY "R0"]` 是 load |
| Ladder | ❌缺（解析吞掉）| ❌缺 | ⚠️只支持常量下标 | `Parser.cs:1422-1432` 只接受 `IDENT = expr`；`Core.cs:627-629` 与 `LoadVariable` 同形 |
| Lua | ⚠️调**不存在的**运行库 | N/A | ⚠️同 | 写 `Statements_A.cs:205-207` `CALL lua_table_set`；读 `Statements_B.cs:1033-1035` `CALL lua_table_get`。**全仓只有调用、没有定义** |
| ObjC | ✅有 | ❌缺（无 store）| ✅ | 复合 `Expressions.cs:133-137` 结果留 R0 不落盘 |
| Pascal | ✅有 | N/A | ✅ | `Statements.cs:531-597` |
| Python | ✅有（**本次已修**）| ✅有（已修）| ✅ | 写 `Statements_A.cs:506-523`；地址 `Expressions.cs:480-522` |
| R | ✅有 | N/A | ✅ | 布局**无数组头**、读写两侧都是 `(idx-1)*4`，**自洽** |
| Ruby | ❌缺（静默写进 `_`）| ❌缺 | ❌无下标语法 | `Parser.cs:278-279` `return new AssignNode("_", right)` |
| Rust | ❌缺（**解析失败**）| ❌缺 | ❌全错 | `Parser.cs:456-475` 只认 `IDENT(.IDENT)* =`；读 `NewFeatures.cs:347/349/350/351` 四句 `MOVE R0, label` 方向全反 |
| Scheme | ❌缺（**load 当 store**）| N/A | ✅ | `Expressions.cs:587` `MOVE R0, [R1+4]` 应为 `MOVE [R1+4], R0` |
| Swift | ✅有 | ⚠️只存右值 | ✅ | 复合 `CodeGenerator.cs:589` 只认 `VariableExpression` |

## 二、Java 的三个独立缺陷（足以解释「输出 0」）

1. `CodeGenerator.Expressions.cs:533-583` —— `GenerateAssignment` 只有 `Left is VariableExpression`，
   `ArrayAccessExpression` 落空且**没有 else** ⇒ 下标赋值整段无代码生成。
2. `CodeGenerator.cs:583-603` —— `GenerateArrayAccess`：
   - `:598` `SHL [#2, R0]` 目标写成了**立即数** ⇒ 空操作（应 `SHL R0, #2`）
   - `:602` `MOVE R0, [MEMORY "R1"]` 读的是**基址寄存器**，不是算出来的 R0
   ⇒ **永远读回数组头（count）**
3. `CodeGenerator.Expressions.cs:565-568` 局部标量赋值 `storeOp [R0, MEMORY off]` 操作数反了
   （应 `[MEMORY off, R0]`），且 `:227` 变量读取**逐字同形** ⇒ **连 `x = 5` 都不落盘**。

## 三、另 10 处「操作数写反 / 地址当值」（同族，均不在现有补丁里）

| 位置 | 现状 | 应为 |
|---|---|---|
| `JavaCompiler/CodeGenerator.Expressions.cs:565-568` | `storeOp [R0, MEMORY off]` | `[MEMORY off, R0]` |
| `JavaCompiler/CodeGenerator.cs:591-593` | `MOVE R0, R13` | 存进槽 |
| `JavaCompiler/CodeGenerator.cs:598` | `SHL [#2, R0]` | `SHL R0, #2` |
| `JavaCompiler/CodeGenerator.cs:602` | `MOVE R0, [R1]` | `MOVE R0, [R0]` |
| `KotlinCompiler/CodeGenerator.cs:256` | `MOVE R1, [R0]` | `MOVE [R0], R1` |
| `RustCompiler/CodeGenerator.VisitExpr.cs:196` | `MOVE R1, (R0)` | `MOVE (R0), R1` |
| `RustCompiler/CodeGenerator.NewFeatures.cs:347/349/350/351` | `MOVE R0, label` ×4 | `MOVE label, R0` |
| `SchemeCompiler/CodeGenerator.Expressions.cs:587` | `MOVE R0, [R1+4]` | `MOVE [R1+4], R0` |
| `ForthCompiler/CodeGenerator.Operations.cs:565 / 585` | `MOVE R0, R0` / `MOVEB R0, R0` | `MOVE R0, [R0]` |
| `LadderCompiler/CodeGenerator.Core.cs:627-629` | `storeOp [R0, MEMORY var]` | `[MEMORY var, R0]` |

外加 **Ruby 数组字面量** `Expressions.cs:217-233`：循环里 `PUSH R0` 但 `R0` 第一轮后已是**上一个元素的值**，
`POP R1` 又冲掉正在用的 R1 游标 ⇒ 元素 1..n-1 写到「上一个元素值」那个地址。
（R 的同名函数 `Expressions.cs:361-377` 用 R1 当游标、循环内不 push/pop，是**对的** —— 两份长得像，只有 Ruby 坏。）

## 四、元素宽度与 `+4` 数组头（别误改）

- **按元素类型走 MOVEB/MOVEH/MOVE**：C、C++、ObjC、Pascal；Forth 是 `@`/`!` 与 `C@`/`C!`。
- **硬编码 4 字节**：C#、Java、JavaScript、Swift、Python、Fortran、Basic、R、Scheme、Go/Kotlin（数组）。
- **`+4` 头只有用了 `AllocateVmlArray`（`[count, e0, …]`）的语言才需要**：
  C 系/ObjC/Swift/JS/C#/Kotlin/Java/Basic/Python/Go 有头；
  **R / Fortran / Pascal 是自建扁布局且读写两侧同式，没有 `+4` 是对的**。

## 五、修复顺序（按「改动最小 × 收益最大」）

1. **Java** —— 三条独立缺陷，都是几行；修完「输出 0」应当消失
2. **Forth** —— 两行（`@`/`C@` 自赋值），且 `65536` = `0x10000` 与「返回地址」精确吻合
3. **Scheme** —— 一行（`vector-set!` 的 load→store）
4. **Kotlin** —— 一处（load→store），另需查 `array_alloc` 的 `PUSH` 约定
5. **Rust** —— 读路径四句方向 + 解析器补下标赋值
6. **Swift / C# / Go** —— `a[i] += v` 退化成 `a[i] = v`，各一处分支顺序/条件
7. **C++ / JavaScript** —— 只差下标复合赋值
8. **Lua** —— 根因是**运行库缺失**，要补 `Lib/lua` 辅助函数（`Lib/` 会被 `sync.sh` 覆盖 ⇒ 必须走 patch）
9. **Ladder** —— `StoreVariable` 与 `LoadVariable` 同形 + 解析器吞掉 `a[0]=5`
10. **Dart / Ruby / D** —— 解析器层**新增**下标语法，工作量最大，排最后

**待单独查**：R 的「内存越界」（读写两侧同式、无 +4 是对的，本表四问解释不了）
——更像是**没有边界检查**（`GenerateIndex`/`GenerateIndexAssign` 都不做范围校验，Python 反而做了）。**⚠️ 存疑**。
