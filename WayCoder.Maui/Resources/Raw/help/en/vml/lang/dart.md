# Dart

Close in style; external functions are declared `external`.

## How to run it on your phone

```
vml run examples/dart/sysinfo.dart
```

Compiling takes a while (one to two minutes for C, a few seconds for scripting languages).
Once the program is running, **the gamepad is at the bottom of the screen** - the D-pad
and the four action buttons are all there. Tap the back arrow in the top-right corner to
return to the Shell.

## What to know before you write

- The entry point is `void main()`
- Declare one `external` line for each `ui_*` you use

## Examples

| File | What it shows |
|---|---|
| `catch.dart` | Catch the blocks |
| `parserexpf_demo.dart` | Calling a shared-library expression parser |
| `sysinfo.dart` | Device information |

Also in this folder: `demo_std.dart`, `demo_tty.dart`, `demo_ui.dart`

---

The rest of this page is taken straight from the VML source (`third_party/vml/VMLPrepares/DartCompiler/`):
the `README` covers what this front end supports and how to compile it, and the
language reference covers the syntax itself. When upstream changes, regenerating
this page brings it up to date.

## Language reference

### Dart Language Compiler Specification

> «bold»Version«/»: v1.0 | «bold»Date«/»: 2026-07-06 | «bold»Revised by«/»: Shenzhen Tanso Intelligent Technology Co., Ltd.

#### Specification standard

| Field | Value |
|:-----|:----|
| «bold»Target standard«/» | Dart 2.x / 3.x syntax subset |
| «bold»Release year«/» | 2024 |
| «bold»Completeness«/» | ~97% |
| «bold»File extension«/» | `.dart` |
| «bold»MCU availability«/» | 🟠 Limited use |

#### Overview

This compiler compiles Dart source code into VML (Virtual Machine Language) assembly. It implements the core C-style syntax subset of Dart — variable declarations, control flow, and function/method and class definitions. The compiler follows the classic compilation flow: preprocessing -> lexical analysis -> syntax analysis -> code generation, and it supports linking the VML standard library through `import` statements.

#### Supported language features

##### 1. Data types

###### Basic data types

- «bold»`int`«/» — 32-bit signed integer
- «bold»`double`«/» — 64-bit double-precision floating point
- «bold»`String`«/» — string literal (double quotes), compiled to constant data
- «bold»`bool`«/» — boolean type; the literals `true` and `false` map to the integers 1 and 0
- «bold»`var`«/» — type-inferred declaration; the variable type is decided by the initializer expression
- «bold»`final`«/» — immutable variable declaration (read-only semantically; identical to `var` at runtime)
- «bold»`void`«/» — empty return type, used for functions that return nothing
- «bold»`null`«/» — null literal, mapped to the integer 0

Note: the compiler does not perform strict type checking. `bool`, `null` and the numeric types all map to VML 32-bit register values at runtime. `String` is stored as a data-section constant and its address is referenced with `MOVE R0, label`.

```dart
int x = 42;
double pi = 3.14159;
String msg = "Hello, Dart!";
bool flag = true;
var inferred = 100;       // inferred as int
final locked = "fixed";   // immutable
void emptyFunc() { }      // returns nothing
Object? nothing = null;   // null literal
```

###### Composite types (limited support)

Class types may be used as variable declaration types, but at runtime they are allocated on the stack and there is no complete object model:

```dart
MyClass obj;  // class-typed variable (syntax supported)
```

##### 2. Variable declaration and definition

Variable declarations follow the `type name [= initializer];` syntax. Local variables are allocated on the stack; global variables are stored in the VML data section.

```dart
// Local variables (inside a function/method)
int count;
int total = 0;
String label = "start";
bool done = false;

// Global variables (top-level scope)
int globalCounter = 0;
double scale = 1.5;

// Type inference
var name = "Dart";       // String
var size = 100;           // int
var ratio = 0.5;          // double

// Immutable declarations
final maxSize = 1024;
final String path = "/data";
```

##### 3. Operators

###### Arithmetic operators

```dart
+    // addition
-    // subtraction
*    // multiplication
/    // division
%    // modulo
-    // unary negation
```

###### Assignment operators

```dart
=    // assignment
+=   // add and assign
-=   // subtract and assign
*=   // multiply and assign
/=   // divide and assign
%=   // modulo and assign
```

###### Comparison operators

```dart
==   // equal
!=   // not equal
<    // less than
>    // greater than
<=   // less than or equal
>=   // greater than or equal
```

###### Logical operators

```dart
&&   // logical and (short-circuit evaluation)
||   // logical or (short-circuit evaluation)
!    // logical not
```

##### 4. Control flow statements

###### if-else

```dart
if (condition) {
  // statement block
} else if (condition2) {
  // statement block
} else {
  // statement block
}
```

Full multi-way nesting is supported. The condition expression may be any numeric expression; 0 and `false`/`null` count as false, and any non-zero value counts as true.

###### while loop

```dart
while (condition) {
  // loop body
}
```

The condition is evaluated before each iteration. The loop exits when the condition is false.

###### for loop (C style)

```dart
for (int i = 0; i < 10; i++) {
  // loop body
}

for (; ; ) {
  // infinite loop
}
```

The C-style three-clause `for` is supported: initialization, condition test, increment expression. All three clauses may be omitted. It is equivalent to `init; while (condition) { body; increment; }`.

###### return

```dart
return;            // returns nothing
return expression;  // returns the value of the expression
```

The `return` statement generates the function epilogue automatically (restore the frame pointer, `RET` instruction). A `return` with no argument is equivalent to `return 0`.

##### 5. Functions and methods

###### Top-level function definitions

```dart
// Function definition syntax
returnType functionName(paramType paramName, ...) {
  // function body
  return value;
}

// Examples
int add(int a, int b) {
  return a + b;
}

void greet(String name) {
  // returns nothing
}

double multiply(double x, double y) {
  return x * y;
}
```

###### Parameters

The parameter list is a comma-separated list of `type name` pairs. Named parameters and optional parameters are not supported. Parameters are passed on the stack (pushed right to left; the caller cleans up).

```dart
// Function with parameters
int sum(int a, int b, int c) {
  return a + b + c;
}

// Function without parameters
void reset() {
  // ...
}
```

###### Methods (inside a class)

```dart
class Calculator {
  int add(int a, int b) {
    return a + b;
  }

  int subtract(int a, int b) {
    return a - b;
  }
}
```

Methods are compiled internally as prefixed functions: `ClassName_methodName`. For example `Calculator.add` compiles to the function `func_Calculator_add`.

###### Function calls

```dart
int result = add(10, 20);
greet("World");
double area = multiply(3.0, 4.0);
```

Recursive function calls are fully supported.

##### 6. Classes

Basic class definitions are supported, including method and field declarations:

```dart
class Point {
  int x;
  int y;

  void move(int dx, int dy) {
    x += dx;
    y += dy;
  }

  int getX() {
    return x;
  }
}
```

«bold»Notes«/»:
- Class member fields are only parsed syntactically; no runtime object layout is generated
- Methods are compiled into standalone functions named `func_ClassName_methodName`
- Inheritance (`extends`), mixins (`with`) and interface implementation (`implements`) are not supported
- Constructors, the `this` keyword and the `new` keyword are not supported
- Class fields are simulated with function-scope variables / the global data section

##### 7. Comments

```dart
// single-line comment

/*
   multi-line block comment
   may span several lines
*/
```

Both comment styles match the Dart standard. Comments are removed during lexical analysis.

##### 8. Importing libraries

The Dart compiler resolves `import` statements during the preprocessing stage and links VML standard libraries:

```dart
import 'stdio.vml';         // import a VML library file
import 'dart:math';         // Dart-style import (the dart: prefix is stripped automatically)
import 'package:core';      // package-style import (the package: prefix is stripped automatically)
```

An `import` statement is only used to link VML runtime libraries; the full Dart module system is not implemented. The compiler strips non-VML prefixes (`dart:`, `package:`) and tries to resolve the library path.

#### Expression precedence table

Expressions are listed from the lowest precedence to the highest:

| Precedence | Operators | Associativity | Description |
|:------:|:-------|:------:|:-----|
| 1 | `=` `+=` `-=` `*=` `/=` `%=` | right | assignment and compound assignment |
| 2 | `\|\|` | left | logical or (short-circuit evaluation) |
| 3 | `&&` | left | logical and (short-circuit evaluation) |
| 4 | `==` `!=` | left | equality comparison |
| 5 | `<` `>` `<=` `>=` | left | relational comparison |
| 6 | `+` `-` | left | addition and subtraction |
| 7 | `*` `/` `%` | left | multiplication, division, modulo |
| 8 | `-` `!` | right | unary negation, logical not |
| 9 | `()` | — | function call, grouping parentheses |

##### Precedence example

```dart
// Arithmetic precedence
int a = 2 + 3 * 4;       // 2 + (3 * 4) = 14
int b = (2 + 3) * 4;     // 5 * 4 = 20

// Comparison precedence
bool c = a < b && b > 0; // (a < b) && (b > 0)

// Assignment precedence (right-associative)
int x = y = 10;          // x = (y = 10)
```

#### Predefined macros

The compiler provides the following built-in macros during the preprocessing stage:

| Macro | Value | Description |
|:-----|:---|:-----|
| `__VML__` | `"1"` | marks the VML toolchain compilation environment |
| `__VML_VERSION__` | `"1.65.32"` | VML toolchain version number |
| `__DART__` | `"1"` | marks the Dart language compiler |
| `__DATE__` | `"Jun 02 2026"` | date at compile time (generated dynamically) |
| `__TIME__` | `"HH:mm:ss"` | time at compile time (generated dynamically) |

Usage example:

```dart
#if __VML__
// VML platform-specific code
#endif

#if __DART__
// Dart compiler extension code
#endif
```

#### Compilation pipeline

##### 1. Preprocessing stage
- If the source contains `#` preprocessor directives, `Preprocessor` is called to expand macros and handle conditional compilation
- Comments are removed (`//` and `/* */` are handled during lexical analysis)

##### 2. Lexical analysis stage
- Converts the source code into a token stream
- Recognizes keywords, identifiers, numeric constants, string literals, operators and separators
- Keywords: `class` `void` `int` `double` `String` `bool` `var` `final` `if` `else` `for` `while` `return` `true` `false` `null`

##### 3. Syntax analysis stage
- Builds the abstract syntax tree (AST)
- Parses type definitions, variable declarations, control flow, expressions and function definitions
- Uses recursive-descent parsing (precedence climbing for expressions)

##### 4. Code generation stage
- Walks the AST and generates a VML instruction sequence
- Stack frame management: `R12` is the frame pointer (BP), `R13` is the stack pointer (SP), `R15` is the return address (RA)
- Function return values go through the `R0` register
- Class methods are compiled into standalone functions prefixed with the class name

#### Known limitations and unsupported features

The Dart compiler is currently 🟢 production ready and supports only the core subset of the Dart language. The following standard Dart features are not supported at present:

##### Unsupported features

| Feature category | Details | Planned priority |
|:---------|:---------|:----------:|
| «bold»Asynchrony«/» | `async` / `await` keywords | low |
| «bold»Async types«/» | `Future`, `Stream` types | low |
| «bold»Mixins«/» | the `mixin` keyword, `with` clauses | low |
| «bold»Generics«/» | parameterized types such as `List<int>`, `Map<K,V>` (the syntax is skipped and no code is generated for it) | medium |
| «bold»Collection literals«/» | `[1, 2, 3]` list literals, `{1, 2, 3}` set literals, `{"a": 1}` map literals | medium |
| «bold»Named parameters«/» | the `function({paramName: value})` named-parameter syntax | low |
| «bold»Optional parameters«/» | `[param]` positional optional parameters, default parameter values | low |
| «bold»Constructors«/» | class constructors, the `this` keyword, initializer lists | medium |
| «bold»Inheritance«/» | `extends`, `implements`, the `super` keyword | medium |
| «bold»Null safety«/» | `?` type suffixes, the `??` operator, the `?.` operator, the `late` keyword | low |
| «bold»Arrow functions«/» | the `=> expression` shorthand syntax | low |
| «bold»Cascades«/» | the `..` cascade operator | low |
| «bold»Enums«/» | `enum` type definitions | low |
| «bold»Extension methods«/» | the `extension on Type` syntax | low |
| «bold»Dynamic typing«/» | the `dynamic` keyword | low |
| «bold»Records«/» | `(int, String)` record types | low |
| «bold»Pattern matching«/» | `switch` expression pattern matching | low |
| «bold»String interpolation«/» | `"Hello $name"` string interpolation | low |
| «bold»Top-level constants«/» | `const` compile-time constants (`final` is only used for variable declarations) | low |
| «bold»Full module system«/» | `export`, `part`, `.dart` file resolution | low |
| «bold»Library prefixes«/» | `import '...' as prefix` library prefix aliases | low |
| «bold»Type aliases«/» | `typedef` function type aliases | low |

##### Partially supported features

| Feature | Level of support | Description |
|:-----|:---------|:-----|
| «bold»Classes«/» | basic | method definitions and field syntax are supported, but no complete object runtime layout is generated |
| «bold»Generic parameters«/» | syntax level | generic syntax such as `List<int>` is skipped and the type resolves to the `_gen` placeholder |
| «bold»Type system«/» | weak typing | no strict type checking is performed; all scalar types become VML 32-bit values at runtime |
| «bold»Single-precision floating point«/» | limited | `double` literals are parsed and stored, but may be converted before use at runtime |
| «bold»String operations«/» | limited | strings are stored as constants and referenced by address; there is no string concatenation or interpolation |

##### Differences from standard Dart

1. «bold»Memory model«/»: uses the VML virtual machine's stack + data section model; no heap allocation and no GC
2. «bold»Type system«/»: weakly typed at runtime; all values become 32-bit scalars, with no runtime type information
3. «bold»Object model«/»: class methods are compiled into global functions; there is no virtual method table and no dynamic dispatch
4. «bold»Standard library«/»: uses the VML runtime library rather than the Dart SDK's `dart:core` and other core libraries
5. «bold»Module system«/»: `import` is only used to link VML libraries; `.dart` source file dependencies are not resolved
6. «bold»Integer size«/»: `int` is VML's 32-bit signed integer (standard Dart uses arbitrary-precision integers)

#### Usage examples

##### Simple program

```dart
int main() {
  int x = 10;
  int y = 20;
  int sum = x + y;
  return sum;
}
```

##### Conditionals

```dart
int max(int a, int b) {
  if (a > b) {
    return a;
  } else {
    return b;
  }
}

void main() {
  int result = max(5, 10);
}
```

##### Loop computation

```dart
int factorial(int n) {
  int result = 1;
  for (int i = 1; i <= n; i++) {
    result *= i;
  }
  return result;
}

void main() {
  int f = factorial(5);  // 120
}
```

##### Classes and methods

```dart
class Counter {
  int value;

  void increment() {
    value += 1;
  }

  void decrement() {
    value -= 1;
  }

  int getValue() {
    return value;
  }
}

void main() {
  // Use the compiled prefixed functions
  // Class fields are stored as global variables
}
```

#### Performance considerations

1. «bold»Code size«/»: the generated VML code contains complete stack frame management logic (prologue/epilogue), so the code is larger than hand-written assembly
2. «bold»Execution speed«/»: it is interpreted inside the VML virtual machine, which is orders of magnitude slower than the native Dart VM
3. «bold»Memory use«/»: all variables use VML stack space, with no dynamic heap allocation overhead
4. «bold»Optimization level«/»: basic code generation only, with no intermediate optimization passes (such as constant folding or dead code elimination)

#### Related source files

| File | Description |
|:-----|:-----|
| `DartCompiler.cs` | compiler main entry; preprocessing dispatch, library linking |
| `Lexer.cs` | lexer: token stream generation |
| `Parser.cs` | parser: recursive-descent AST construction |
| `ASTNode.cs` | AST node type definitions |
| `Token.cs` | token type enum and token data structure |
| `CodeGenerator.cs` | code generation entry: stack frame management, class handling |
| `CodeGenerator.Expressions.cs` | expression code generation: literals, variables, binary/unary operations, function calls |
| `CodeGenerator.Statements.cs` | statement code generation: variable declarations, assignment, control flow |
| `DartCompilerPlugin.cs` | plugin interface implementation |
| `DartCompiler.csproj` | .NET project file |

---

#### 🆕 String encoding (v1.65.19)

This language compiler uses the VML string system indirectly through the shared library (Lib/shared/).

| Pseudo-op | Width | Encoding | C type |
|:------|:----:|:-----|:--------|
| `.string` | 8-bit | UTF-8 | `char*` |
| `.wstring` | 16-bit | UTF-16LE | `wchar_t*` |
| `.ustring` | 32-bit | UTF-32LE | `char32_t*` |

«bold»MCU mode«/» (default): strings are output as UTF-8 (`.string`)
«bold»OS mode«/»: the encoding can be tested with the `VML_WSTRING` macro

The shared library already provides wide-string conversion functions (wchar.h/uchar.h); each language compiler may use them as needed.

## Compiler README

### Dart Compiler

«bold»Path«/»: `VMLPrepares/DartCompiler/`
«bold»Completeness«/»: ~97% | 🟢 Production ready
«bold»Standard library«/»: `Lib/dart/`

#### Features
- ✅ Lexer + parser + code generator (Lexer/Parser/CodeGenerator)
- ✅ Control flow (if/else/while/for/do-while/break/continue)
- ✅ Variable declarations (var/final/const/type annotations)
- ✅ Functions: return types/parameters/recursion
- ✅ Class: class definitions/members/methods
- ✅ Operators: ++/--/ternary/bitwise/short-circuit logic
- ✅ Compound assignment (+= -= *= /= %=)
- ✅ import statements
- ✅ 30 tests, 0 failures
- ⚠️ async/await/Future — skipped on MCU
- ⚠️ mixin/extension — not implemented
- ✅ C-style preprocessing (#ifdef/#ifndef/#define/#include)

#### Compilation modes

##### MCU mode (default `--mode mcu`)
MCU mode is optimized for microcontroller/bare-metal environments.

«bold»Skipped«/» (not supported on MCU):
- async/await, Future, Stream
- mixin

«bold»Retained«/»:
- Basic type arithmetic, control flow, function calls
- print maps to UART

##### OS mode (`--mode os`)
OS mode supports the full set of language features.

##### RAM levels
- `--ram k`: kilobyte level
- `--ram m`: megabyte level (default)
- `--ram g`: gigabyte level

#### Usage
```bash
dotnet run --project VMLTool -- input.dart -o output.vml
```

#### Tests
- `VMLTests/NewCompilerTests.cs` — 30 tests (0 failures)
- `VMLTests/FullPipelineTests.cs` — full-pipeline tests
- `Examples/dart/` — example programs (start/factorial/file_io/info)
