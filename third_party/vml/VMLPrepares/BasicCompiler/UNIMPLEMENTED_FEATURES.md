# BASIC 方言未实现特性清单

> v1.66.33 | 以下语句编译通过但无实际功能 (生成警告)

## FreeBasic (6项)

| 语句 | 用途 | 状态 |
|------|------|:--:|
| `PTR` | 指针类型声明 | 编译通过, 无代码 |
| `CAST` | 类型转换 | 编译通过, 无代码 |
| `EXTENDS` | 类继承 | 编译通过, 无代码 |
| `OPERATOR` | 运算符重载 | 编译通过, 无代码 |
| `ENUM` 值代入 | 枚举成员常量替换 | ✅ v1.66.33 已实现 |
| `CLASS` 方法调用 | 类方法链接 | ✅ obj.method(args)→ClassName_method |

## PureBasic (4项)

| 语句 | 用途 | 状态 |
|------|------|:--:|
| `PROTECTED` | 访问修饰符 | 编译通过, 跳过 |
| `INTERFACE` / `ENDINTERFACE` | 接口定义 | 编译通过, 无代码 |
| `NEW` | 对象分配 | 编译通过, 无分配代码 |
| `THREADED` | 线程存储 | 编译通过, 跳过 |

## TrueBasic (3项)

| 语句 | 用途 | 状态 |
|------|------|:--:|
| `MAT` | 矩阵操作 | 编译通过, 无代码 |
| `ZER` | 零值常量 | 编译通过, 无代码 |
| `CON` | 常量定义 | 编译通过, 无代码 |

## GW-BASIC (4项)

| 语句 | 用途 | 状态 |
|------|------|:--:|
| `BLOAD` | 二进制文件加载 | ⚠ 实为「生成 GPIO 调用」且**缺运行时库**（见 `Parser.Core.cs` 的 `ParseGpioStatement`）|
| `BSAVE` | 二进制文件保存 | ⚠ 同上 |
| `KEY` / `KEY(n) ON/OFF` | 功能键处理 | 编译通过, 无代码 |
| `DEF SEG` | 段地址定义 | 编译通过, 跳过 (x86特定) |

## PowerBASIC (2项)

| 语句 | 用途 | 状态 |
|------|------|:--:|
| `REGISTER` | 寄存器存储类 | 编译通过, 跳过 |
| `FASTPROC` | 快速过程调用 | 编译通过, 跳过 |

## VisualBasic (8项)

| 语句 | 用途 | 状态 |
|------|------|:--:|
| `Private` / `Public` / `Friend` | 访问修饰符 | 编译通过, 跳过 |
| `Optional` | 可选参数 | 编译通过, 跳过 |
| `ParamArray` | 可变参数 | 编译通过, 跳过 |
| `With` / `End With` | 对象引用简写 | 编译通过, 无代码 |
| `ReDim Preserve` | 数组保留重分配 | 编译通过, 无保留 |
| `Property` | 属性访问器 | 编译通过, 无代码 |

## ChipBasic (1项)

| 语句 | 用途 | 状态 |
|------|------|:--:|
| `PINMODE`/`DIGITALWRITE`/`DIGITALREAD` | GPIO操作 | 代码生成, 缺运行时库 |

## 跨方言通用 (3项)

| 特性 | 方言 | 状态 |
|------|------|:--:|
| `CLASS` 方法体执行 | FreeBasic | 解析完成, 方法未链接 |
| `DESTRUCTOR` 析构 | FreeBasic | 编译通过, 无代码 |
| `ENUM` 成员值代入 | FreeBasic | 值收集完成, 未代入 |

---

**总计: 29 项**（v0.96.506 重数 —— 此前写 31，与表里逐行数对不上）

⚠ **本文件与 `README.md`/`ALL_KEYWORDS.md` 口径不一致的地方已按代码核实过**：
- `PTR`/`CAST` 这里写"编译通过, 无代码"，而 README 列在「❌ 未实现（需类型系统）」——
  按代码是**报错**（不是"编过但没功能"）；
- `ENUM` 值代入 / `CLASS` 方法：上面表格标 ✅、下面「跨方言通用」又说未实现 ——
  按 `CodeGenerator.cs` 是**实现了**（会合成 `<Class>_<Method>` 并塞进 `subMap`），
  下面那段是没删掉的旧结论。
**逐条状态以 `ALL_KEYWORDS.md` 为准**（那张表 v0.96.506 按实测重算过）。
**原则**: 全部编译通过, 生成时输出警告 `; WARNING: {feature} not implemented`

**实现顺序（v0.96.506 更新）**：ENUM 值代入 ✅ → CLASS 方法 ✅ → **GPIO 库**（手机上无 GPIO，
不打算做）→ EXTENDS / OPERATOR / INTERFACE（都需类型系统，未做）。
