# BASIC 编译器

**路径**: `VMLPrepares/BasicCompiler/` | **版本**: v1.66.33
**完成度**: 10方言 ≥90% | 🟢 生产可用（图形/声音/文件已通手机端）
⚠ **测试数字（78 / Lang_BASIC、`VMLTests/`、`test/basic_examples/`）是上游仓库的** ——
  本仓（分家后的移动端副本）**没有** `VMLTests/` 与 `test/` 这两个目录，那些路径与数字在这里复现不了。
  本仓的判据是 `scripts/vml-basic-probe/`（54 例 + `fileio.sh` 5 例）。
**标准库**: `Lib/basic/`

## 多方言支持

通过 `--basictype` 选项支持 10 种 BASIC 方言:

| 方言 | CLI 值 | 派系 | 完成度 |
|------|--------|------|:--:|
| QBasic (默认) | `qbasic` | Microsoft | 92% |
| GW-BASIC | `gwbasic` | Microsoft | 90% |
| VisualBasic 6 | `visualbasic` | Microsoft | 90% |
| TurboBasic | `turbobasic` | Borland | 90% |
| PowerBASIC | `powerbasic` | Borland | 90% |
| FreeBasic | `freebasic` | 开源 | 90% |
| PureBasic | `purebasic` | 商业 | 90% |
| TrueBasic | `truebasic` | ANSI/ISO | 90% |
| ChipBasic | `chipbasic` | MCU | 90% |
| MiniBasic | `minibasic` | 教学 | 90% |

```bash
vmltool -x basic --basictype freebasic test.bas -o test.vml
vmltool -x basic --basictype gwbasic test.bas -o test.vml
```

## 关键字实现

⚠ **这里的数字此前对不上代码**（README 写 81/99、同文件下面又写 116+，SPEC 又写 116/141）。
按 v0.96.506 实测重算：**词法表 140 条关键字、`TokenType` 171 个**；
`ALL_KEYWORDS.md` 那张 99 项的表按实测是 **89 实现 / 10 未实现**。
**以 `ALL_KEYWORDS.md` 为准**，它逐条标了实测状态。

### 完全实现
- ✅ 控制流: IF/THEN/ELSE/ELSEIF/SELECT CASE/FOR/NEXT/WHILE/WEND/DO/LOOP/GOTO/GOSUB
- ✅ 子程序: SUB/END SUB/FUNCTION/CALL/DECLARE/BYVAL/BYREF/PROCEDURE
- ✅ 变量: DIM/LET/CONST/SWAP/ERASE/REDIM/SHARED/COMMON/STATIC/LOCAL/GLOBAL
- ✅ I/O: PRINT/INPUT/OPEN/CLOSE/WRITE/LPRINT/PRINT USING/FREEFILE/LINE INPUT #
  （⚠ 文件那一族是 v0.96.503/504 才真做通的：此前 OPEN 的句柄根本没存、
    `PRINT #`/`INPUT #` 走的是**设备**号而不是**文件**号、`FREEFILE` 只有词法条目。
    现在全部改走吃沙箱的 `#110-113`，判据见 `scripts/vml-basic-probe/fileio.sh`）
- ⚠️ OOP (FreeBasic): CLASS/METHOD/ENUM ✅；**CONSTRUCTOR 只到"解析"、DESTRUCTOR 无代码**
- ✅ 数学: ABS/SGN/SQR/SIN/COS/TAN/INT/FIX/RND/RANDOMIZE
- ✅ 字符串: LEN/CHR$/ASC/STR$/VAL/LEFT$/RIGHT$/MID$
- ⚠️ MCU: CHIPASM/ASM/BLOAD/BSAVE —— **`ASM` 已从词法表移除**（注释写着"仅限 C/ObjC/C++"）；
  `BLOAD`/`BSAVE` 会生成 GPIO 调用但**缺运行时库**，且手机上没有 GPIO ⇒ 实际不可用
- ✅ 其他: REM/DATA/READ/RESTORE/ON ERROR/RESUME/SLEEP/SYSTEM/BEEP/SOUND

### 部分实现
- ⚠️ PROPERTY, CONSTRUCTOR (框架就绪, 待完善)

### 未实现 (需类型系统)
- ❌ PTR, CAST, EXTENDS, OPERATOR, INTERFACE

## 方言特有功能

| 功能 | 适用方言 | 说明 |
|------|------|------|
| CLASS/OOP | FreeBasic | 类/构造/析构/方法/枚举 |
| GPIO | ChipBasic | PINMODE/DIGITALWRITE/DIGITALREAD |
| GLOBAL | PureBasic | → SHARED 映射 |
| PROCEDURE | PureBasic | → SUB 映射 |
| ENUM 值代入 | FreeBasic | 编译时常量 (RED+GREEN=30) |
| NEW | PureBasic | → EmitAlloc 堆分配 |
| Private/Public | VisualBasic | 修饰符识别 |

## 文件结构

```
BasicCompiler/
├── ASTNode.cs                 # AST 节点 (实测 171 个 TokenType)
├── Lexer.cs                   # 词法分析 (实测 140 条关键字)
├── Parser.cs / Parser.*.cs    # 语法分析 (10方言支持)
├── CodeGenerator.cs / *.cs    # 代码生成 (TypedCodeGen<BasicType>)
├── BasicCompilerPlugin.cs     # 插件入口 + 编译流水线
├── TokenType.cs               # Token 类型枚举
├── BASIC_LANGUAGE_SPEC.md     # 语言规范 v2.1
├── ALL_KEYWORDS.md            # 99关键字×10方言清单
├── UNIMPLEMENTED_FEATURES.md  # 未实现特性清单
└── README.md                  # 本文件
```

## 使用

```bash
# QBasic (默认)
vmltool input.bas -o output.vml

# 指定方言
vmltool -x basic --basictype freebasic input.bas -o output.vml
vmltool -x basic --basictype visualbasic input.bas -o output.vml

# 编译 + 运行
vmltool input.bas -o output.vml && vmltool -r output.vml
```

## 测试

- **78 单元测试** (`VMLTests/Lang_BASIC.cs`): 方言关键字 + TYPE/CLASS + ENUM
- **22 方言测试**: 7方言特有关键字 + 跨方言兼容性
- **14 FreeBasic 示例** (`test/basic_examples/freebasic/`): class/constructor/method/pointers等
- **QBasic 游戏**: gorillas.bas, nibbles.bas

```bash
# 运行所有 BASIC 测试
dotnet test VMLTests/VMLTests.csproj --filter "ClassName~Lang_BASIC"

# 运行现代特性测试
dotnet test VMLTests/VMLTests.csproj --filter "ClassName~Lang_ModernFeatures"
```
