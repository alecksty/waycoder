# D language compiler specification

> **Version**: v1.0 | **Date**: 2026-07-06 | **Revised by**: Shenzhen Tanso Intelligent Technology Co., Ltd.

## Metadata

| Property | Value |
|------|-----|
| **Language** | D |
| **Standard** | D 2.0 subset |
| **Year** | first released in 2001 |
| **Completeness** | ~97% |
| **Source file extension** | `.d` |
| **File encoding** | UTF-8 (default) |

## Overview

D is a systems programming language with C-like syntax and a modular design. This compiler implements a functional subset of the D 2.0 specification, running against the MCU-safe subset environment of the VML virtual machine. The compilation flow is:

```
source.d → [preprocessor] → [Lexer] → [Parser] → [CodeGenerator] → VML assembly (.vml)
```

### Compiler entry points

- Main compiler class: `DCompiler.DCompiler`
- Plugin registration: `DCompilerPlugin` (implements `CompilerPluginExBase`)
- Standalone run: `CompilerProgram.MainAsync()` — wrapped by `SimpleCompilerProgram`

### Calling convention

A C-like calling convention is used: arguments are pushed onto the stack from right to left and the caller is responsible for stack cleanup. Register R0 holds integer/boolean/pointer return values; floating-point return values use register F0.

---

## Supported features

### 1. Data types

| Type | Keyword | VML IR width | Description |
|------|--------|-------------|------|
| Void | `void` | — | function has no return value |
| Integer | `int` | 32-bit signed | supports decimal and hexadecimal (`0x`) literals |
| Single-precision float | `float` | 32-bit | IEEE 754 single precision |
| Double-precision float | `double` | 64-bit | currently stored downgraded to float |
| Boolean | `bool` | 32-bit | `true` = 1, `false` = 0 |
| String | `string` | pointer (32-bit) | immutable character array; literals go into the data section |
| Character | `char` | 8-bit | single-quoted literal, stored as an int |
| Class | `class` | reference type | members are accessed through R12-0 (this) |
| Struct | `struct` | value type | syntactically supported, parsed the same as class |
| Interface | `interface` | placeholder | inner declarations are skipped during parsing |
| Enum | `enum` | 32-bit | supports explicit assignment `= expr` |

**Literal syntax**:
- Integers: `42`, `0x2A`, `0xFF`
- Floats: `3.14`, `0.5`
- Booleans: `true`, `false`
- Null: `null`
- Strings: `"hello world"` (double quotes, escape characters supported)
- Characters: `'a'`, `'\n'` (single quotes, escape characters supported)
- this reference: `this` (points at the current object, stored at R12-0)

### 2. Module system

```d
module mymodule;                    // declare the current module name
import std.stdio;                   // import a module
import foo.bar.baz;                 // dotted import path
```

The `import` statement only supports the static import syntax; the module name is extracted during parsing and used for library linking. The `module` declaration is syntactically accepted but has no actual semantic effect.

### 3. Variable declarations

```d
int x = 10;                         // declaration + initialization
float pi;                           // declaration (default-initialized to 0)
string name = "hello";              // string variable
bool flag = true;                   // boolean variable
char c = 'A';                       // character variable
ClassName obj;                      // class-type variable (default-initialized to 0/null)
```

**Semantics**:
- Local variables are allocated in the stack frame (R12 relative offset, 4 bytes per allocation)
- Global variables are allocated in the data section (the `var_name` label)
- With no initializer given, the default initialization is 0
- The for-loop init clause supports declaring a new variable

### 4. Operators

#### 4.1 Arithmetic operators

| Operator | Operation | Example |
|--------|------|------|
| `+` | addition | `a + b` |
| `-` | subtraction | `a - b` |
| `*` | multiplication | `a * b` |
| `/` | division | `a / b` |
| `%` | modulo | `a % b` |
| `^^` | exponentiation | `a ^^ b` |

Note: `^^` exponentiation has no native power instruction at the VML level, so the compiler lowers `^^` to multiplication `*` as an approximation.

#### 4.2 Comparison operators

| Operator | Operation | Description |
|--------|------|------|
| `==` | equal | returns 1/0 |
| `!=` | not equal | returns 1/0 |
| `<` | less than | returns 1/0 |
| `>` | greater than | returns 1/0 |
| `<=` | less than or equal | returns 1/0 |
| `>=` | greater than or equal | returns 1/0 |

A comparison returns an integer value: 1 for true, 0 for false.

#### 4.3 Logical operators

| Operator | Operation | Description |
|--------|------|------|
| `&&` | logical AND | CMP + JE short-circuit evaluation |
| `\|\|` | logical OR | short-circuit evaluation |
| `!` | logical NOT | unary negation |

Logical operations use a short-circuit strategy: `&&` skips the right operand when the left one is false; `||` skips the right operand when the left one is true.

#### 4.4 Assignment operators

| Operator | Operation |
|--------|------|
| `=` | direct assignment |
| `+=` | add and assign |
| `-=` | subtract and assign |
| `*=` | multiply and assign |
| `/=` | divide and assign |

Compound assignment operators (`+=`, `-=`, `*=`, `/=`) are expanded internally into the form `a = a op value`:
```d
a += b;    // equivalent to a = a + b;
a -= 5;    // equivalent to a = a - 5;
a *= 2;    // equivalent to a = a * 2;
a /= 3;    // equivalent to a = a / 3;
```

### 5. Control flow

#### 5.1 if / else statements

```d
if (condition) {
    // then body
}

if (condition) {
    // then body
} else {
    // else body
}

if (x > 0) {
    return 1;
} else if (x < 0) {
    return -1;
} else {
    return 0;
}
```

The condition is evaluated and compared with 0; when false the jump goes to the else label or the end label.

#### 5.2 while statements

```d
while (condition) {
    // loop body
}
```

#### 5.3 do-while statements

```d
do {
    // loop body
} while (condition);
```

Implementation: do-while is expanded at compile time into `{ body; while (cond) { body } }`.

#### 5.4 for statements (C style)

```d
for (init; condition; increment) {
    // loop body
}

// example
for (int i = 0; i < 10; i = i + 1) {
    x = x + i;
}
```

All three clauses are optional:
- `init`: a variable declaration or an expression, executed once
- `condition`: evaluated before every iteration; when false the loop exits
- `increment`: evaluated after the body of every iteration

#### 5.5 break / continue

The `break` and `continue` keywords are not implemented at present. They are absent from the lexer's keyword table, so they are treated as ordinary identifiers.

#### 5.6 return statements

```d
return;              // void return
return expression;   // return with a value
```

The return value is stored in register R0, R12 is restored and then RET is executed.

### 6. Functions

```d
// full declaration form
int add(int a, int b) {
    return a + b;
}

// void return type
void greet(string name) {
    // ...
}

// no parameters
int getAnswer() {
    return 42;
}
```

**Syntax**: `returnType functionName(paramType paramName, ...) { body }`

**Internal implementation**:
- Function label: `func_functionName`
- Class method label: `class_ClassName_methodName`
- Prologue: PUSH R15, PUSH R12, MOVE R12 R13 (saves the frame pointer and return address, establishes a new stack frame)
- Arguments are passed through the stack; the first argument is accessed at offset +4 bytes inside the stack frame
- Epilogue: POP R12, RET (restores the frame pointer, returns to the caller)
- Function definitions are skipped by a JMP in the main flow and only execute when called explicitly

### 7. Classes and structs

#### 7.1 class

```d
class Animal {
    void speak() {
        // member function
    }

    int getAge() {
        return this.age;   // the this keyword
    }
}
```

- `class` parses to a `ClassDeclNode`
- Member functions are generated in declaration order
- The `this` keyword accesses the current object pointer through R12-0
- Inheritance is not supported (the extends keyword is not implemented)
- Access control is not supported (public/private/protected are not implemented)
- Constructors and destructors are not supported

#### 7.2 struct

```d
struct Point {
    int x;
    int y;
}
```

Syntactically the same as class, sharing the `ClassDeclNode` AST node. The difference is only at the semantic level (value type vs reference type) and the current implementation does not distinguish them.

#### 7.3 interface

```d
interface Drawable {
    void draw();
}
```

An `interface` declaration is parsed, its inner declaration body is skipped and a placeholder AST node is returned. Interface methods generate no code.

#### 7.4 enum

```d
enum Color {
    Red,           // implicit value 0
    Green,         // implicit value 1
    Blue = 5,      // explicit assignment
    Yellow         // implicit value 6
}
```

- Enum member names are recognized as `Identifier`
- `= expression` explicit assignment is supported
- Members are separated by commas (a trailing comma is optional)
- The enum declaration body ends with a semicolon (`};`)

### 8. Comments

| Type | Syntax | Description |
|------|------|------|
| Single-line comment | `// text` | from `//` to the end of the line |
| Block comment | `/* text */` | multi-line, cannot nest |
| Nested block comment | `/+ text +/` | D's native nested comment, not specially supported at present |

Inside the compiler `LexerBase.SkipBlockComment` handles `/* ... */` and `SkipLineComment` handles `// ...`. The `/+ ... +/` nested comment syntax is not implemented in the lexer at present.

**Note**: comment tokens are produced during tokenization but do not take part in parsing (they are skipped by `SkipComments()`).

### 9. String literals

```d
"hello world"                       // ordinary string
"line1\nline2"                      // with an escape sequence
"quote: \"inside\""                 // escaped double quote
```

- Delimited by double quotes
- Inner escape sequences are handled by `LexerBase.ReadStringLiteral` (supporting the standard C escapes such as `\n`, `\t`, `\\`, `\"`, `\0`)
- String literals are allocated in the data section and their address is loaded with `MOVE R0, label`
- Strings with identical contents are cached and deduplicated

### 10. Character literals

```d
'a'                                 // the character 'a'
'\n'                                // newline
'\t'                                // tab
'\\'                                // backslash
```

- Delimited by single quotes
- Parsed into an integer value (the character's ASCII/Unicode code point)
- Handled by the same `ReadStringLiteral` method as string literals

### 11. Expression grammar

```
Expression := Assignment
Assignment := LogicalOr ('=' | '+=' | '-=' | '*=' | '/=') Assignment | LogicalOr
LogicalOr  := LogicalAnd ('||' LogicalAnd)*
LogicalAnd := Comparison ('&&' Comparison)*
Comparison := Additive (('==' | '!=' | '<' | '>' | '<=' | '>=') Additive)*
Additive   := Multiplicative (('+' | '-') Multiplicative)*
Multiplicative := Unary (('*' | '/' | '%' | '^^') Unary)*
Unary      := ('-' | '!') Unary | Postfix
Postfix    := Primary ('(' Arguments? ')')*
Primary    := Integer | Float | String | Char | true | false | null
            | this | Identifier | '(' Expression ')'
```

### 12. Expression precedence

Listed from lowest to highest precedence:

| Precedence | Operators | Associativity |
|--------|--------|--------|
| 1 (lowest) | `=` `+=` `-=` `*=` `/=` | right |
| 2 | `\|\|` | left |
| 3 | `&&` | left |
| 4 | `==` `!=` `<` `>` `<=` `>=` | left |
| 5 | `+` `-` | left |
| 6 | `*` `/` `%` `^^` | left |
| 7 | `-` (unary) `!` (unary) | right |
| 8 (highest) | `()` (function call) | left |

### 13. Preprocessor support

The compiler integrates a C-style preprocessor (`Preprocessor`) that runs before lexing:

#### Predefined macros

| Macro name | Value | Description |
|------|-----|------|
| `__VML__` | `1` | VML target platform marker |
| `__VML_VERSION__` | `"1.65.32"` | VML toolchain version |
| `__D__` | `1` | D language source file marker |
| `__DATE__` | `"MMM dd yyyy"` | compilation date |
| `__TIME__` | `"HH:mm:ss"` | compilation time |

#### Preprocessor directives

Standard C preprocessor directives are supported; the preprocessor is only activated when the source file contains a `#` character:
- `#include "file"` / `#include <file>`
- `#define NAME value` / `#define NAME`
- `#undef NAME`
- `#if` / `#ifdef` / `#ifndef` / `#else` / `#elif` / `#endif`
- `#pragma` (passed through to the compile pipeline)
- `#error "message"`
- the line continuation character `\`

### 14. Entry point and library linking

- The program entry point is the `main` label (not a `main` function but the VML program's starting point)
- The compiler automatically links the D standard library (directory `Lib/d/`) through `CompilerHelper.LinkStandardLibrary`
- Modules declared with `import` are automatically resolved to library file paths and linked

---

## Limitations and unimplemented features

The following features belong to the full D language specification but are currently **not supported**:

### Metaprogramming

- **Templates**: `template T(T)` — the core metaprogramming feature, not implemented
- **Mixins**: `mixin("string")` — compile-time code generation, not implemented
- **CTFE (compile-time function evaluation)**: executing functions at compile time is not supported
- **Constraints**: `if (is(T == int))` template constraints, not implemented

### Contract programming

- **in contracts**: `in { assert(x > 0); }` — preconditions, not implemented
- **out contracts**: `out (result) { assert(result > 0); }` — postconditions, not implemented
- **invariant**: `invariant() { ... }` — class invariants, not implemented

### Memory management

- **GC (garbage collection)**: D's default memory management strategy, skipped because of MCU-safe subset limits
- **new expressions**: `new ClassName()` — the keyword is recognized but no allocation code is generated
- **delete**: explicit destruction, not implemented

### Control flow

- **switch / case**: multi-branch selection, not implemented
- **break**: loop break, not implemented (the keyword is not in the lexer's table)
- **continue**: continue to the next iteration, not implemented (the keyword is not in the lexer's table)
- **foreach / foreach_reverse**: range iteration, not implemented
- **goto**: jump statement, not implemented
- **scope statements**: `scope(exit)`, `scope(success)`, `scope(failure)` — scope guards, not implemented
- **with statements**: `with (expr) { ... }` — scope shortening, not implemented
- **synchronized**: multi-thread synchronization, not implemented
- **try / catch / finally**: exception handling, skipped because of MCU-safe subset limits

### Type system

- **auto**: type inference, not implemented
- **const / immutable**: type modifiers, not implemented
- **alias**: type aliases, not implemented
- **typeof**: type retrieval, not implemented
- **delegate / function**: delegates and function pointers, not implemented
- **enum as a manifest type**: `enum E : string { ... }` — only basic integer enums are supported
- **union**: unions, not implemented

### Composite types

- **Associative arrays**: `int[string] aa;` — key/value container, not implemented
- **Dynamic arrays**: `int[] arr;` — slices and dynamic arrays, not implemented
- **Static arrays**: `int[10] arr;` — fixed-size arrays, not implemented
- **Slice operations**: `arr[0..$]`, `arr[1..3]` — not implemented

### Other

- **unittest blocks**: `unittest { ... }` — unit test blocks, not implemented
- **version / debug**: conditional compilation blocks, not implemented (the preprocessor `#ifdef` can be used instead)
- **Properties**: `@property T name()` — not implemented
- **Operator overloading**: `T opBinary(string op)(T rhs)` — not implemented
- **UFCS (Uniform Function Call Syntax)**: `obj.method()` equivalent to `method(obj)`, not implemented
- **Module constructors/destructors**: `static this()`, `static ~this()` — not implemented
- **Nested functions/closures**: defining a function inside a function, not implemented
- **Ranges**: lazy evaluation abstractions such as `std.range`, not implemented
- **list (linked-list literals)**: such as `[1, 2, 3]`, not implemented

---

## Compilation options

Compilation options supported when invoking through the CLI tool `VMLTool`:

| Option | Meaning |
|------|------|
| `-o <file>` | output file path |
| `-mode mcu` | MCU-safe subset mode (default) |
| `-mode os` | full OS mode |
| `--std=<std>` | specify the language standard version |
| `-D <macro>` | define a preprocessor macro |
| `-U <macro>` | undefine a preprocessor macro |
| `-I <path>` | add an include search path |
| `-L <path>` | add a library search path |
| `-static` | static linking |
| `-shared` | dynamic linking |
| `-target <arch>` | specify the target architecture |

---

## Full grammar reference (EBNF)

```ebnf
Program       := { TopLevelDecl }

TopLevelDecl  := ModuleDecl | ImportDecl | FuncDef | VarDecl
               | ClassDecl | StructDecl | InterfaceDecl | EnumDecl

ModuleDecl    := "module" Identifier ";"
ImportDecl    := "import" Identifier { "." Identifier } ";"

Type          := "void" | "int" | "float" | "double" | "bool"
               | "string" | "char" | Identifier

VarDecl       := Type Identifier [ "=" Expression ] ";"

FuncDef       := Type Identifier "(" [ ParamList ] ")" Block
ParamList     := Type Identifier { "," Type Identifier }

ClassDecl     := "class" Identifier "{" { Declaration } "}" ";"
StructDecl    := "struct" Identifier "{" { Declaration } "}" ";"
InterfaceDecl := "interface" Identifier "{" { Declaration } "}" ";"
EnumDecl      := "enum" Identifier "{" EnumMember { "," EnumMember } [ "," ] "}" ";"
EnumMember    := Identifier [ "=" Expression ]

Block         := "{" { Statement } "}"
Statement     := ReturnStmt | IfStmt | WhileStmt | DoWhileStmt
               | ForStmt | VarDecl | ExpressionStmt

ReturnStmt    := "return" [ Expression ] ";"
IfStmt        := "if" "(" Expression ")" Block [ "else" (IfStmt | Block) ]
WhileStmt     := "while" "(" Expression ")" Block
DoWhileStmt   := "do" Block "while" "(" Expression ")" ";"
ForStmt       := "for" "(" [ForInit] ";" [ Expression ] ";" [ Expression ] ")" Block
ForInit       := VarDecl | Expression
ExpressionStmt:= Expression ";"

Expression    := Assignment
Assignment    := LogicalOr [ ("=" | "+=" | "-=" | "*=" | "/=") Assignment ]
LogicalOr     := LogicalAnd { "||" LogicalAnd }
LogicalAnd    := Comparison { "&&" Comparison }
Comparison    := Additive [ ("==" | "!=" | "<" | ">" | "<=" | ">=") Additive ]
Additive      := Multiplicative { ("+" | "-") Multiplicative }
Multiplicative:= Unary { ("*" | "/" | "%" | "^^") Unary }
Unary         := ["-" | "!"] Postfix
Postfix       := Primary { "(" [ Expression { "," Expression } ] ")" }

Primary       := Integer | FloatLiteral | StringLiteral | CharLiteral
               | "true" | "false" | "null" | "this"
               | Identifier
               | "(" Expression ")"

Identifier    := Letter { Letter | Digit | "_" }
Integer       := Digit { Digit } | "0x" HexDigit { HexDigit }
FloatLiteral  := Digit { Digit } "." Digit { Digit }
StringLiteral := '"' { Character } '"'
CharLiteral   := "'" Character "'"

Comment       := "//" { Character }
BlockComment  := "/*" { Character } "*/"
```

---

## 🆕 String encoding (v1.65.19)

This language compiler uses the VML string system indirectly through the shared library (Lib/shared/).

| Directive | Width | Encoding | C type |
|:------|:----:|:-----|:--------|
| `.string` | 8-bit | UTF-8 | `char*` |
| `.wstring` | 16-bit | UTF-16LE | `wchar_t*` |
| `.ustring` | 32-bit | UTF-32LE | `char32_t*` |

**MCU mode** (default): string output is UTF-8 (`.string`)
**OS mode**: the encoding can be determined through the `VML_WSTRING` macro

The shared library already provides wide-string conversion functions (wchar.h/uchar.h) that each language compiler can use as needed.
