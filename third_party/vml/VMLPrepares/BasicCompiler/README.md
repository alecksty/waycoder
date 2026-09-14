# BASIC 编译器

**路径**: `VMLPrepares/BasicCompiler/` | **版本**: v1.66.33
**完成度**: 10方言 ≥90% | 🟢 生产可用 | **测试**: 78 (Lang_BASIC)
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

## 关键字实现 (81/99, 82%)

### 完全实现
- ✅ 控制流: IF/THEN/ELSE/ELSEIF/SELECT CASE/FOR/NEXT/WHILE/WEND/DO/LOOP/GOTO/GOSUB
- ✅ 子程序: SUB/END SUB/FUNCTION/CALL/DECLARE/BYVAL/BYREF/PROCEDURE
- ✅ 变量: DIM/LET/CONST/SWAP/ERASE/REDIM/SHARED/COMMON/STATIC/LOCAL/GLOBAL
- ✅ I/O: PRINT/INPUT/OPEN/CLOSE/WRITE/LPRINT/PRINT USING/FREEFILE
- ✅ OOP (FreeBasic): CLASS/CONSTRUCTOR/DESTRUCTOR/METHOD/NEW/ENUM
- ✅ 数学: ABS/SGN/SQR/SIN/COS/TAN/INT/FIX/RND/RANDOMIZE
- ✅ 字符串: LEN/CHR$/ASC/STR$/VAL/LEFT$/RIGHT$/MID$
- ✅ MCU: CHIPASM/ASM/BLOAD/BSAVE
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
├── ASTNode.cs                 # AST 节点 (141 TokenType)
├── Lexer.cs                   # 词法分析 (116+ 关键字)
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
