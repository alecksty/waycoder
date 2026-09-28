# Java Language Compiler Specification

> **Version**: v1.0 | **Date**: 2026-07-06 | **Revised by**: Shenzhen Tanso Intelligent Technology Co., Ltd.

## Specification standard

| Field | Value |
|:-----|:----|
| **Target standard** | Java SE 8 subset (2014) |
| **Release year** | 2014 |
| **Completeness** | ~92% |
| **MCU completeness** | ~90% |
| **Tests** | 0 (the test directory is still to be created) |
| **Updates** | 2026-05-18: corrected the completeness figures and the implementation-status description |

## Keywords

`abstract` `assert` `boolean` `break` `byte` `case` `catch` `char` `class` `const`
`continue` `default` `do` `double` `else` `enum` `extends` `final` `finally` `float`
`for` `goto` `if` `implements` `import` `instanceof` `int` `interface` `long` `native`
`new` `package` `private` `protected` `public` `return` `short` `static` `strictfp`
`super` `switch` `synchronized` `this` `throw` `throws` `transient` `try` `void`
`volatile` `while` `true` `false` `null`

## Overview

This compiler supports a subset of the Java language and compiles Java source code into VML (Virtual Machine Language) assembly. It uses a statically compiled architecture and supports the core Java features, including classes, methods, control flow and the standard library.

## Supported language features

### 1. Data types

#### Literals
- **Integers**: `42` (decimal)
- **Hexadecimal**: `0xFF`, `0x40013804` (the `0x`/`0X` prefix)
- **Long integers**: `42L`, `0xFFFFL` (the `l`/`L` suffix)
- **Floating point**: `3.14f`, `3.14`
- **Double precision**: `3.14d`
- **Boolean**: `true`, `false`
- **Character**: `'A'`
- **String**: `"hello"`
- **Null**: `null`

#### Basic data types
- **Integer types**: `byte` (8-bit), `short` (16-bit), `int` (32-bit), `long` (64-bit)
- **Floating-point types**: `float` (32-bit), `double` (64-bit)
- **Character type**: `char` (16-bit Unicode)
- **Boolean type**: `boolean` (true/false)
- **Reference types**: classes, interfaces, arrays

#### Wrapper class support
- `Integer`, `Double`, `Float`, `Boolean`, `Character`, `Byte`, `Short`, `Long`
- Autoboxing and unboxing are supported

### 2. Classes and objects

#### Class definition
```java
// public class
public class MyClass {
    // field
    private int value;
    public static final int CONSTANT = 100;
    
    // constructor
    public MyClass(int initialValue) {
        this.value = initialValue;
    }
    
    // instance method
    public int getValue() {
        return value;
    }
    
    // static method
    public static void staticMethod() {
        System.out.println("Static method");
    }
}

// inner class
class InnerClass {
    // inner class definition
}
```

#### Inheritance and polymorphism
```java
// inheritance
public class ChildClass extends ParentClass {
    @Override
    public void method() {
        super.method();  // call the parent method
        System.out.println("Child method");
    }
}

// interface implementation
public class MyClass implements MyInterface {
    @Override
    public void interfaceMethod() {
        System.out.println("Interface method implementation");
    }
}
```

### 3. Control-flow statements

#### Conditional statements
```java
// if-else
if (condition) {
    // true branch
} else if (anotherCondition) {
    // else-if branch
} else {
    // false branch
}

// switch statement
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

#### Loops
```java
// for loop
for (int i = 0; i < 10; i++) {
    System.out.println(i);
}

// for-each loop
for (String item : items) {
    System.out.println(item);
}

// while loop
while (condition) {
    // loop body
}

// do-while loop
do {
    // the loop body runs at least once
} while (condition);
```

### 4. Exception handling

```java
// try-catch-finally
try {
    // code that may throw
    riskyOperation();
} catch (IOException e) {
    // handle IOException
    System.err.println("IO错误: " + e.getMessage());
} catch (Exception e) {
    // handle other exceptions
    System.err.println("错误: " + e.getMessage());
} finally {
    // cleanup code, always runs
    cleanup();
}

// throw statement
if (error) {
    throw new RuntimeException("错误发生");
}

// throws declaration
public void readFile() throws IOException {
    // the method may throw IOException
}
```

### 5. Arrays and collections

#### Arrays
```java
// array declaration and initialization
int[] numbers = new int[10];
int[] initialized = {1, 2, 3, 4, 5};
String[] strings = new String[]{"a", "b", "c"};

// multi-dimensional arrays
int[][] matrix = new int[3][3];
int[][] jagged = {{1, 2}, {3, 4, 5}, {6}};

// array operations
int length = numbers.length;
numbers[0] = 100;
int first = numbers[0];
```

#### Collections framework (simplified)
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

### 6. Input and output

#### Console input and output
```java
// output
System.out.println("Hello, World!");
System.out.print("No newline");
System.out.printf("Formatted: %s %d %.2f", "text", 100, 3.14);

// input
Scanner scanner = new Scanner(System.in);
String input = scanner.nextLine();
int number = scanner.nextInt();
double decimal = scanner.nextDouble();
```

#### File input and output
```java
// read a file
try (BufferedReader reader = new BufferedReader(new FileReader("file.txt"))) {
    String line;
    while ((line = reader.readLine()) != null) {
        System.out.println(line);
    }
}

// write a file
try (BufferedWriter writer = new BufferedWriter(new FileWriter("output.txt"))) {
    writer.write("Hello, File!");
    writer.newLine();
}
```

### 7. Standard library support

#### The java.lang package
- `System` - system-related operations
- `String` - string handling
- `Math` - math functions
- wrapper classes such as `Integer`, `Double`
- `Object` - the base class of every class

#### The java.util package
- `Scanner` - input scanning
- `ArrayList`, `LinkedList` - lists
- `HashMap`, `TreeMap` - maps
- `HashSet`, `TreeSet` - sets
- `Date`, `Calendar` - date and time

#### The java.io package
- `File` - file operations
- `FileReader`, `FileWriter` - file reads and writes
- `BufferedReader`, `BufferedWriter` - buffered reads and writes

### 8. Advanced features

#### Generics
```java
// generic class
public class Box<T> {
    private T content;
    
    public void set(T content) {
        this.content = content;
    }
    
    public T get() {
        return content;
    }
}

// generic method
public static <T> T getFirst(List<T> list) {
    return list.get(0);
}
```

#### Annotations
```java
// using annotations
@Override
@Deprecated
@SuppressWarnings("unchecked")

// custom annotation
@Target(ElementType.METHOD)
@Retention(RetentionPolicy.RUNTIME)
public @interface MyAnnotation {
    String value() default "";
    int count() default 1;
}
```

#### Lambda expressions and functional interfaces
```java
// functional interface
@FunctionalInterface
interface MyFunction {
    void apply(String s);
}

// lambda expression
MyFunction func = (s) -> System.out.println(s);
func.apply("Hello Lambda");

// method reference
List<String> list = Arrays.asList("a", "b", "c");
list.forEach(System.out::println);
```

### 9. Compilation target

#### Structure of the generated VML code
```vml
.entry main
.stack 1048572

.data
hello_string: .string "Hello from Java!"

.text
main:
    MOVE R0, hello_string
    SYSCALL #1    ; write the string
    SYSCALL #3    ; exit the program
```

#### Method calling convention
- Arguments are passed in registers R0-R3
- The return value comes back in R0
- The stack frame uses R13 (SP) and R14 (BP)
- Local variables are allocated on the stack

### 10. Limitations and caveats

#### Current limitations
1. **Unsupported features**:
   - the reflection API
   - dynamic proxies
   - native methods
   - serialization
   - the concurrency package (java.util.concurrent)

2. **Simplified implementation**:
   - garbage collection is handled by the VML runtime
   - exception handling is a simplified version
   - generics are erased to their raw types

3. **Performance considerations**:
   - statically compiled, with no JIT optimization
   - the memory layout is fixed
   - no dynamic class loading

#### Best practices
1. Prefer primitive types over wrapper classes for performance
2. Avoid overusing reflection and dynamic features
3. Use the `final` keyword to help the compiler optimize
4. Use static methods and fields sensibly

### 11. Example programs

#### Hello World
```java
public class HelloWorld {
    public static void main(String[] args) {
        System.out.println("Hello, World!");
    }
}
```

#### Computing a factorial
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

#### File operations
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

## Floating-point and 64-bit compile modes

The VML toolchain controls how floating point and 64-bit integers are handled through three compile parameters:

| Parameter | Values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32-bit floating point (float) handling |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64-bit floating point (double) handling |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64-bit integers (long) handling |

### 32-bit floating point (float32)

The 32-bit single-precision type `float` compiles as follows:

- **`hard` mode (default)**: uses the native VML floating-point instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`, operating directly on the sixteen floating-point registers F0-F15. Best performance; suited to targets with floating-point hardware.
- **`soft` mode**: uses the Q15.16 fixed-point software emulation library `softfloat.c`, emulating floating-point operations through functions such as `__vml_float_add/sub/mul/div/neg/abs/cmp`. Suited to MCU platforms with no floating-point hardware.
- **`none` mode**: disables all 32-bit floating-point types; a `float` declaration is reported as a compile error.

### 64-bit floating point (double)

The 64-bit double-precision type `double` compiles as follows:

- **`soft` mode (default)**: uses the IEEE 754 double-precision emulation library `softdouble.c`, through `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` and similar. Works on every platform, MCU included.
- **`hard` mode**: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`, operating on the eight double-precision registers D0-D7. Requires the target platform to support 64-bit operations.
- **`none` mode**: disables all 64-bit floating-point types; a `double` declaration is reported as a compile error.

### 64-bit integers (int64)

The 64-bit integer type `long` compiles as follows:

- **`soft` mode (default)**: uses the two-register software emulation library `softint64.c`, through functions such as `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr`.
- **`hard` mode**: reserved; a future VML version will support native 64-bit integer instructions.
- **`none` mode**: disables the 64-bit integer type; a `long` declaration is reported as a compile error.

### Software emulation libraries

These emulation libraries all live in `Lib/shared/`, are written in C, are compiled to VML by the C compiler, and are shared by every language:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point 32-bit floating-point emulation | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 double-precision 64-bit floating-point emulation | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | 64-bit integer two-register emulation | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

## Compiler implementation status

### Current completeness: ~92%
- ✅ Lexer (Lexer.cs, ~487 lines)
- ✅ Parser (Parser.cs, ~1188 lines)
- ✅ AST node definitions (ASTNode.cs, ~603 lines)
- ✅ Code generator (CodeGenerator.cs, ~1463 lines)
- ⚠️ Classes/objects — basic support
- ⚠️ Interfaces — basic support
- ❌ Generics — parsed but no code is generated
- ❌ Exception handling (try/catch/throw) — not implemented
- ❌ Lambda/method references — not implemented
- ❌ Annotations — not implemented
- ❌ Standard library (Lib/java/) — to be created

### Development roadmap
1. **Stage 1**: basic syntax support (if, for, while, method calls) — ✅ done
2. **Stage 2**: class and object support — ⚠️ mostly done
3. **Stage 3**: exception handling — ❌ to do
4. **Stage 4**: standard library — ❌ to do
5. **Stage 5**: generics / advanced features — ❌ to do

## Compiling and running

### Using the Java compiler
```bash
# compile a Java program
dotnet run --project VMLPrepares/JavaCompiler -L Lib/java HelloWorld.java -o output.vml

# run the generated VML program
dotnet run --project VMLEmulators/FullDevicesEmulator output.vml
```

### Using VMLTool (plugin architecture)
```bash
# recognise the Java file automatically
vmltool HelloWorld.java -o output.vml

# specify the Java language explicitly
vmltool HelloWorld.java -o output.vml --lang java
```

## Related documents
- [VML toolchain architecture](ARCHITECTURE.md)
- [Compiler completeness dashboard](../../COMPLETION_DASHBOARD.md)
- [Java standard library documentation](Lib/java/README.md)
- [Test case notes](Test/java/README.md)
---

## 🆕 String types (v1.65.19)

This language's compiler uses `.wstring` (UTF-16LE) as its internal string storage by default.

| Mode | Default encoding | VML directive | SYSCALL output |
|:-----|:--------|:----------|:------------|
| MCU (default) | `.string` (UTF-8) | `.string` | #1 |
| OS | `.wstring` (UTF-16LE) | `.wstring` | #391 |

**Predefined macro**: `VML_WSTRING` — defined automatically in OS mode, undefined in MCU mode
**Output function**: OS mode automatically uses `shared_print_wstr` (converts UTF-16LE→UTF-8 for you)

```c
// user code can test the macro to tell which encoding is in use
#ifdef VML_WSTRING
  // strings are wstring (UTF-16LE) by default
#else
  // strings are UTF-8 by default
#endif
```
