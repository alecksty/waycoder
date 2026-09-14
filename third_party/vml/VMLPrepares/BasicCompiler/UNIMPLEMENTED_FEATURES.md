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
| `BLOAD` | 二进制文件加载 | 编译通过, 无代码 |
| `BSAVE` | 二进制文件保存 | 编译通过, 无代码 |
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

**总计: 31 项未实现特性**
**原则**: 全部编译通过, 生成时输出警告 `; WARNING: {feature} not implemented`
**实现顺序**: ENUM值代入 → CLASS方法 → GPIO库 → EXTENDS → 其他
