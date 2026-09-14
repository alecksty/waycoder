# Java 语言编译器规范说明

> **版本**：v1.0 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

## 规范标准

| 字段 | 值 |
|:-----|:----|
| **目标标准** | Java SE 8 子集 (2014) |
| **发布年份** | 2014 |
| **完成度** | ~92% |
| **MCU完成度** | ~90% |
| **测试** | 0 (测试目录待创建) |
| **更新** | 2026-05-18: 修正完成度和实现状态描述 |

## 关键字

`abstract` `assert` `boolean` `break` `byte` `case` `catch` `char` `class` `const`
`continue` `default` `do` `double` `else` `enum` `extends` `final` `finally` `float`
`for` `goto` `if` `implements` `import` `instanceof` `int` `interface` `long` `native`
`new` `package` `private` `protected` `public` `return` `short` `static` `strictfp`
`super` `switch` `synchronized` `this` `throw` `throws` `transient` `try` `void`
`volatile` `while` `true` `false` `null`

## 概述

本编译器支持 Java 语言的子集，将 Java 源代码编译为 VML (Virtual Machine Language) 汇编代码。编译器采用静态编译版架构，支持 Java 核心特性，包括类、方法、控制流和标准库。

## 支持的语言特性

### 1. 数据类型

#### 字面量
- **整数**: `42`（十进制）
- **十六进制**: `0xFF`, `0x40013804`（`0x`/`0X` 前缀）
- **长整数**: `42L`, `0xFFFFL`（`l`/`L` 后缀）
- **浮点数**: `3.14f`, `3.14`
- **双精度**: `3.14d`
- **布尔**: `true`, `false`
- **字符**: `'A'`
- **字符串**: `"hello"`
- **空**: `null`

#### 基本数据类型
- **整数类型**: `byte` (8位), `short` (16位), `int` (32位), `long` (64位)
- **浮点类型**: `float` (32位), `double` (64位)
- **字符类型**: `char` (16位 Unicode)
- **布尔类型**: `boolean` (true/false)
- **引用类型**: 类、接口、数组

#### 包装类支持
- `Integer`, `Double`, `Float`, `Boolean`, `Character`, `Byte`, `Short`, `Long`
- 自动装箱和拆箱支持

### 2. 类和对象

#### 类定义
```java
// 公共类
public class MyClass {
    // 字段
    private int value;
    public static final int CONSTANT = 100;
    
    // 构造方法
    public MyClass(int initialValue) {
        this.value = initialValue;
    }
    
    // 实例方法
    public int getValue() {
        return value;
    }
    
    // 静态方法
    public static void staticMethod() {
        System.out.println("Static method");
    }
}

// 内部类
class InnerClass {
    // 内部类定义
}
```

#### 继承和多态
```java
// 继承
public class ChildClass extends ParentClass {
    @Override
    public void method() {
        super.method();  // 调用父类方法
        System.out.println("Child method");
    }
}

// 接口实现
public class MyClass implements MyInterface {
    @Override
    public void interfaceMethod() {
        System.out.println("Interface method implementation");
    }
}
```

### 3. 控制流语句

#### 条件语句
```java
// if-else
if (condition) {
    // true分支
} else if (anotherCondition) {
    // else-if分支
} else {
    // false分支
}

// switch语句
switch (value) {
    case 1:
        System.out.println("Case 1");
        break;
    case 2:
        System.out.println("Case 2");
        break;
    default:
        System.out.println("Default case");
}
```

#### 循环语句
```java
// for循环
for (int i = 0; i < 10; i++) {
    System.out.println(i);
}

// for-each循环
for (String item : items) {
    System.out.println(item);
}

// while循环
while (condition) {
    // 循环体
}

// do-while循环
do {
    // 循环体至少执行一次
} while (condition);
```

### 4. 异常处理

```java
// try-catch-finally
try {
    // 可能抛出异常的代码
    riskyOperation();
} catch (IOException e) {
    // 处理IOException
    System.err.println("IO错误: " + e.getMessage());
} catch (Exception e) {
    // 处理其他异常
    System.err.println("错误: " + e.getMessage());
} finally {
    // 清理代码，总是执行
    cleanup();
}

// throw语句
if (error) {
    throw new RuntimeException("错误发生");
}

// throws声明
public void readFile() throws IOException {
    // 方法可能抛出IOException
}
```

### 5. 数组和集合

#### 数组
```java
// 数组声明和初始化
int[] numbers = new int[10];
int[] initialized = {1, 2, 3, 4, 5};
String[] strings = new String[]{"a", "b", "c"};

// 多维数组
int[][] matrix = new int[3][3];
int[][] jagged = {{1, 2}, {3, 4, 5}, {6}};

// 数组操作
int length = numbers.length;
numbers[0] = 100;
int first = numbers[0];
```

#### 集合框架（简化版）
```java
// List
List<String> list = new ArrayList<>();
list.add("item1");
list.add("item2");
String item = list.get(0);

// Map
Map<String, Integer> map = new HashMap<>();
map.put("key", 100);
int value = map.get("key");

// Set
Set<Integer> set = new HashSet<>();
set.add(1);
set.add(2);
boolean contains = set.contains(1);
```

### 6. 输入输出

#### 控制台输入输出
```java
// 输出
System.out.println("Hello, World!");
System.out.print("No newline");
System.out.printf("Formatted: %s %d %.2f", "text", 100, 3.14);

// 输入
Scanner scanner = new Scanner(System.in);
String input = scanner.nextLine();
int number = scanner.nextInt();
double decimal = scanner.nextDouble();
```

#### 文件输入输出
```java
// 读取文件
try (BufferedReader reader = new BufferedReader(new FileReader("file.txt"))) {
    String line;
    while ((line = reader.readLine()) != null) {
        System.out.println(line);
    }
}

// 写入文件
try (BufferedWriter writer = new BufferedWriter(new FileWriter("output.txt"))) {
    writer.write("Hello, File!");
    writer.newLine();
}
```

### 7. 标准库支持

#### java.lang 包
- `System` - 系统相关操作
- `String` - 字符串处理
- `Math` - 数学函数
- `Integer`, `Double` 等包装类
- `Object` - 所有类的基类

#### java.util 包
- `Scanner` - 输入扫描
- `ArrayList`, `LinkedList` - 列表
- `HashMap`, `TreeMap` - 映射
- `HashSet`, `TreeSet` - 集合
- `Date`, `Calendar` - 日期时间

#### java.io 包
- `File` - 文件操作
- `FileReader`, `FileWriter` - 文件读写
- `BufferedReader`, `BufferedWriter` - 缓冲读写

### 8. 高级特性

#### 泛型
```java
// 泛型类
public class Box<T> {
    private T content;
    
    public void set(T content) {
        this.content = content;
    }
    
    public T get() {
        return content;
    }
}

// 泛型方法
public static <T> T getFirst(List<T> list) {
    return list.get(0);
}
```

#### 注解
```java
// 使用注解
@Override
@Deprecated
@SuppressWarnings("unchecked")

// 自定义注解
@Target(ElementType.METHOD)
@Retention(RetentionPolicy.RUNTIME)
public @interface MyAnnotation {
    String value() default "";
    int count() default 1;
}
```

#### Lambda表达式和函数式接口
```java
// 函数式接口
@FunctionalInterface
interface MyFunction {
    void apply(String s);
}

// Lambda表达式
MyFunction func = (s) -> System.out.println(s);
func.apply("Hello Lambda");

// 方法引用
List<String> list = Arrays.asList("a", "b", "c");
list.forEach(System.out::println);
```

### 9. 编译目标

#### 生成的VML代码结构
```vml
.entry main
.stack 1048572

.data
hello_string: .string "Hello from Java!"

.text
main:
    MOVE R0, hello_string
    SYSCALL #1    ; 输出字符串
    SYSCALL #3    ; 退出程序
```

#### 方法调用约定
- 参数通过寄存器 R0-R3 传递
- 返回值通过 R0 返回
- 栈帧使用 R13 (SP) 和 R14 (BP)
- 局部变量在栈上分配

### 10. 限制和注意事项

#### 当前限制
1. **不支持的特性**:
   - 反射 API
   - 动态代理
   - 原生方法 (native)
   - 序列化
   - 并发包 (java.util.concurrent)

2. **简化实现**:
   - 垃圾回收由VML运行时管理
   - 异常处理为简化版本
   - 泛型擦除为原始类型

3. **性能考虑**:
   - 静态编译，无JIT优化
   - 内存布局固定
   - 无动态类加载

#### 最佳实践
1. 使用基本类型而非包装类以提高性能
2. 避免过度使用反射和动态特性
3. 使用 final 关键字帮助编译器优化
4. 合理使用静态方法和字段

### 11. 示例程序

#### Hello World
```java
public class HelloWorld {
    public static void main(String[] args) {
        System.out.println("Hello, World!");
    }
}
```

#### 计算阶乘
```java
public class Factorial {
    public static int factorial(int n) {
        if (n <= 1) {
            return 1;
        }
        return n * factorial(n - 1);
    }
    
    public static void main(String[] args) {
        int result = factorial(5);
        System.out.println("5! = " + result);
    }
}
```

#### 文件操作
```java
import java.io.*;

public class FileExample {
    public static void main(String[] args) {
        try {
            FileWriter writer = new FileWriter("output.txt");
            writer.write("Hello, File!");
            writer.close();
            System.out.println("文件写入成功");
        } catch (IOException e) {
            System.err.println("文件错误: " + e.getMessage());
        }
    }
}
```

## 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点 (float) 处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点 (double) 处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数 (long) 处理模式 |

### 32位浮点 (float32)

本语言中的 32 位单精度浮点类型 `float` 按以下模式编译：

- **`hard` 模式（默认）**: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。性能最佳，适合支持浮点硬件的目标平台。
- **`soft` 模式**: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。适合无浮点硬件的 MCU 平台。
- **`none` 模式**: 禁用所有 32 位浮点类型，遇到 `float` 声明时报告编译错误。

### 64位浮点 (double)

本语言中的 64 位双精度浮点类型 `double` 按以下模式编译：

- **`soft` 模式（默认）**: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`，通过 `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` 等函数模拟。兼容所有平台（含 MCU）。
- **`hard` 模式**: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`，通过 D0-D7 八个双精度寄存器运算。需要目标平台支持 64 位运算。
- **`none` 模式**: 禁用所有 64 位浮点类型，遇到 `double` 声明时报告编译错误。

### 64位整数 (int64)

本语言中的 64 位整数类型 `long` 按以下模式编译：

- **`soft` 模式（默认）**: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- **`hard` 模式**: 预留，未来 VML 版本将支持原生 64 位整数指令。
- **`none` 模式**: 禁用 64 位整数类型，遇到 `long` 声明时报告编译错误。

### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

## 编译器实现状态

### 当前完成度: ~92%
- ✅ 词法分析器 (Lexer.cs, ~487行)
- ✅ 语法分析器 (Parser.cs, ~1188行)
- ✅ AST节点定义 (ASTNode.cs, ~603行)
- ✅ 代码生成器 (CodeGenerator.cs, ~1463行)
- ⚠️ 类/对象 — 基本支持
- ⚠️ 接口 — 基本支持
- ❌ 泛型 — 解析但未生成代码
- ❌ 异常处理 (try/catch/throw) — 未实现
- ❌ Lambda/方法引用 — 未实现
- ❌ 注解 — 未实现
- ❌ 标准库 (Lib/java/) — 待创建

### 开发路线图
1. **阶段1**: 基础语法支持 (if, for, while, 方法调用) — ✅ 完成
2. **阶段2**: 类和对象支持 — ⚠️ 基本完成
3. **阶段3**: 异常处理 — ❌ 待实现
4. **阶段4**: 标准库完善 — ❌ 待实现
5. **阶段5**: 泛型/高级特性 — ❌ 待实现

## 编译和运行

### 使用Java编译器
```bash
# 编译Java程序
dotnet run --project VMLPrepares/JavaCompiler -L Lib/java HelloWorld.java -o output.vml

# 运行生成的VML程序
dotnet run --project VMLEmulators/FullDevicesEmulator output.vml
```

### 使用VMLTool (插件架构)
```bash
# 自动识别Java文件
vmltool HelloWorld.java -o output.vml

# 指定Java语言
vmltool HelloWorld.java -o output.vml --lang java
```

## 相关文档
- [VML工具链架构](ARCHITECTURE.md)
- [编译器完成度仪表板](../../COMPLETION_DASHBOARD.md)
- [Java标准库文档](Lib/java/README.md)
- [测试用例说明](Test/java/README.md)
---

## 🆕 字符串类型 (v1.65.19)

该语言编译器默认使用 `.wstring` (UTF-16LE) 作为内部字符串存储。

| 模式 | 默认编码 | VML 伪指令 | SYSCALL 输出 |
|:-----|:--------|:----------|:------------|
| MCU (默认) | `.string` (UTF-8) | `.string` | #1 |
| OS | `.wstring` (UTF-16LE) | `.wstring` | #391 |

**预定义宏**: `VML_WSTRING` — OS 模式下自动定义，MCU 模式未定义
**输出函数**: OS 模式自动使用 `shared_print_wstr` (UTF-16LE→UTF-8 自动转换)

```c
// 用户代码可通过宏判断编码
#ifdef VML_WSTRING
  // 默认字符串为 wstring (UTF-16LE)
#else
  // 默认字符串为 UTF-8
#endif
```
