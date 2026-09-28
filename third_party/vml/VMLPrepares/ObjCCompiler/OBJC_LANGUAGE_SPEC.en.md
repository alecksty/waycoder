# Objective-C Language Compiler Specification

> **Version**: v1.1 | **Date**: 2026-07-06 | **Revised by**: Shenzhen Tanso Intelligent Technology Co., Ltd.

| Attribute | Value |
|------|-----|
| **Standard** | Objective-C 2.0 subset |
| **Year** | 1984 (ObjC 2.0: 2006) |
| **Completeness** | ~99% |
| **Extensions** | `.m`, `.mm` |
| **Compiler entry points** | `ObjCCompiler.Compile()` / `ObjCCompiler.CompileFile()` |
| **Source files** | `Lexer.cs` (145 lines) / `Parser.cs` (440 lines) / `CodeGenerator*.cs` (399 lines) / `ASTNode.cs` (76 lines) |

---

## 1. Overview

This compiler implements a subset of Objective-C 2.0, compatible with C syntax and extended with object-oriented features. The core design idea is to compile C functions and ObjC message sends uniformly into VML IR, with a runtime library providing the message dispatch skeleton for base classes such as `NSObject`.

**Architecture layers**:
```
Objective-C source (.m, .mm)
    → Lexer (lexical analysis) → token stream
    → Parser (syntax analysis) → AST
    → CodeGenerator (code generation) → VMLProgram (VML IR)
    → VMLAssembler / VMLRuntime → execution
```

**Compilation pipeline**:
1. Preprocessing (`Preprocessor`) — expands `#import`/`#include`/`#define` and the predefined macros
2. Lexical analysis (`Lexer`) — splits the source into a token stream, recognizing the ObjC-specific `@` keywords and C operators
3. Syntax analysis (`Parser`) — recursive-descent parsing, building the AST node tree
4. Code generation (`CodeGenerator`) — translates the AST into a VML instruction sequence

---

## 2. Supported features

### 2.1 C-compatible types

The basic C type system is supported. Type keywords are decided centrally by `Lexer.IsTypeName()`.

| Type keyword | Description | Example |
|-----------|------|------|
| `int` | 32-bit signed integer | `int a = 10;` |
| `float` | 32-bit floating point | `float f = 3.14;` |
| `double` | 64-bit floating point | `double d = 2.718;` |
| `char` | 8-bit character | `char c = 'A';` |
| `void` | no return value / empty type | `void func() { }` |
| `short` | 16-bit integer | `short s = 100;` |
| `long` | 32-bit integer (`long long` supports 64-bit) | `long l = 99999;` |
| `signed` | signed modifier | `signed int x;` |
| `unsigned` | unsigned modifier | `unsigned int u;` |
| `struct` | struct declaration (**supported by the lexer; the parser can recognize `struct` as a type name, but parsing of the semantics of struct member access `.`/`->` is not yet fully implemented**) | `struct Point p;` |
| `enum` | enum declaration (**recognized by the lexer as a keyword and treated by the parser as a type name, but enum member definitions are not yet implemented**) | `enum Color c;` |
| `id` | ObjC generic object pointer, equivalent to `void *` | `id obj = nil;` |

**Type modifier combination rules**: `unsigned` / `signed` may be followed by a base type name (such as `unsigned int`); the parser concatenates the type string recursively.

### 2.2 ObjC keywords

All ObjC keywords carry a `@` prefix; the lexer reads the letter sequence after the `@` character and matches it.

#### 2.2.1 Core definition keywords

| Keyword | Purpose | Parsing status |
|--------|------|----------|
| `@interface` | declare a class interface (instance variables + method declarations) | **Fully parsed** — supports class name, superclass, instance variable block, method declaration list |
| `@implementation` | implement class methods | **Fully parsed** — supports method body implementations |
| `@end` | terminate an `@interface` / `@implementation` block | **Fully parsed** |
| `@protocol` | protocol declaration (**recognized by the lexer; the parser implements no parsing logic**) | token only |
| `@class` | forward-declare a class name | **recognized and skipped by the parser** (no code generated, no semantic effect) |
| `@property` | property declaration (**recognized by the lexer; the parser implements no parsing logic**) | token only |
| `@synthesize` | property accessor synthesis (**recognized by the lexer; the parser implements no parsing logic**) | token only |
| `@dynamic` | property accessors provided at runtime (**recognized by the lexer; the parser implements no parsing logic**) | token only |
| `@selector` | selector literal (**recognized by the lexer; the parser implements no parsing logic**) | token only |

**`@interface` syntax**:
```objc
@interface MyClass : SuperClass {
    // Instance variables (only type + name are supported; the pointer * syntax is not)
    int count;
    float value;
    id delegate;
}
// Method declarations
- (void)doSomething;
- (int)add:(int)a to:(int)b;
@end
```

**`@implementation` syntax**:
```objc
@implementation MyClass

// Method implementations
- (void)doSomething {
    count = count + 1;
    return;
}

// C functions may also be mixed into @implementation
int helperFunc(int x) {
    return x * 2;
}

@end
```

#### 2.2.2 Message expressions

The classic Objective-C square-bracket message send syntax is supported:

```objc
// Basic message send
[receiver methodName]

// Message send with parameters (parameters interleaved with selector labels)
[obj setX:10 y:20]

// Message with no parameters
[array count]

// Nested call (the receiver may be the result of another message expression)
[[self delegate] update]
```

**Code generation**: a message send compiles to a `CALL objc_<method>` instruction; the parameters and the receiver are `PUSH`ed onto the stack in order. The runtime must provide a function named `objc_<selectorName>` to perform message dispatch.

#### 2.2.3 NSString literals

The `@"string"` syntax for creating an ObjC string constant is supported:

```objc
id greeting = @"Hello, World!";
```

**Internal implementation**: the lexer recognizes `@"..."` as an `ObjCString` token (value = `@"...content..."`); the code generator treats it as a string literal, stores it in the data section and loads its address with `MOVE R0, label` (LEA semantics).

#### 2.2.4 Special values

| Value | Type | Compiles to |
|----|------|--------|
| `nil` | null object pointer | `MOVE R0, 0` |
| `YES` | boolean true | `MOVE R0, 1` |
| `NO` | boolean false | `MOVE R0, 0` |
| `self` | current object reference (implicit parameter inside a method) | stack frame offset 0 (R12 base) |
| `super` | superclass reference (**recognized by the lexer, semantics not implemented**) | token only |

### 2.3 Method declarations

Both instance methods (`-`) and class methods (`+`) are supported:

```objc
// Instance method — no parameters
- (int)getCount {
    return count;
}

// Instance method — one parameter
- (void)setCount:(int)newCount {
    count = newCount;
}

// Instance method — several parameters (interleaved selector labels)
- (int)add:(int)a to:(int)b {
    return a + b;
}

// Class method (+)
+ (id)sharedInstance {
    return self;
}
```

**Method name encoding**: colons in the selector are replaced with underscores. For example `add:to:` compiles to the label `objc_add_to_`.

### 2.4 Operators

#### 2.4.1 Fully supported operators (implemented in both the parser and the code generator)

| Category | Operators | Description |
|------|--------|------|
| Arithmetic | `+` `-` `*` `/` `%` | add, subtract, multiply, divide, modulo |
| Comparison | `==` `!=` `<` `>` `<=` `>=` | equal / not equal / ordering comparisons |
| Assignment | `=` `+=` `-=` `*=` `/=` `%=` | assignment and compound assignment |
| Unary | `-` `!` `++` `--` | negation / logical not / increment / decrement |
| Dereference | `*` | pointer dereference (**supported by the lexer + parser; code generation only performs a register-indirect load**) |
| Address-of | `&` | address-of (**supported by the lexer + parser; code generation only performs a register-indirect store**) |

#### 2.4.2 Lexer only (the parser implements no semantics)

| Operator | Token type | Description |
|--------|-----------|------|
| `&&` `\|\|` | `And`, `Or` | logical and/or (**short-circuit evaluation is not implemented**) |
| `&` `\|` `^` `~` | `Amp`, `Pipe`, `Caret`, `Tilde` | bitwise and/or/xor/not |
| `<<` `>>` | `LShift`, `RShift` | shift left/right |
| `->` | `Arrow` | struct pointer member access |
| `? :` | `Question`, `Colon` | ternary conditional operator |
| `...` | `Ellipsis` | varargs (recognized by the parser but has no semantics) |

### 2.5 C function declarations

```objc
// Standard function declaration
int add(int a, int b) {
    return a + b;
}

// Function with no parameters
void sayHello() {
    return;
}

// Multi-parameter function
float average(float a, float b, float c) {
    return (a + b + c) / 3.0;
}

// Forward declaration (the function body is replaced by a semicolon)
int factorial(int n);
```

**Calling convention**: parameters are `PUSH`ed onto the stack, called with `CALL func_<name>`, returned with `RET`, and the return value is in R0.

**Function overloading**: not supported. Function names must be unique across the whole program.

### 2.6 Control flow

#### 2.6.1 if / else

```objc
if (x > 0) {
    return 1;
} else if (x == 0) {
    return 0;
} else {
    return -1;
}
```

**Implementation**: after the condition is evaluated, `CMP R0, 0` + `JE elseLabel`; chained `else if` is supported.

#### 2.6.2 while

```objc
while (i < 10) {
    i = i + 1;
}
```

**Implementation**: a loop-head label + a conditional `JE` to the exit + an unconditional `JMP` at the loop tail back to the loop head.

#### 2.6.3 for (C style)

```objc
for (int i = 0; i < 10; i = i + 1) {
    total = total + i;
}
```

**Implementation**: the initializer runs outside the loop, the condition is evaluated before each iteration, and the update runs after the loop body.

#### 2.6.4 Control flow not currently supported (the lexer already recognizes the keywords)

The following C control-flow keywords already have tokens defined in the lexer, but the parser implements no parsing logic for them:

- `switch` / `case` / `default` — switch multi-way selection
- `do` ... `while` — do-while loop
- `break` / `continue` — loop break/continue
- `goto` — unconditional jump

### 2.7 Comments

```objc
// single-line comment

/* 
   multi-line comment
   may span several lines
*/
```

Both comment styles are discarded directly during lexical analysis and never enter the token stream.

### 2.8 Preprocessing

The compiler supports the `#import`, `#include` and `#define` preprocessing directives. A line starting with `#` is recognized by the lexer as a `Hash` token, and the parser calls `SkipPreprocessor()` to skip to the end of the line. The actual preprocessing logic runs at the `ObjCCompiler.Compile()` entry point, handled by the `Preprocessor` class:

```objc
#import <Foundation/Foundation.h>
#include "common.h"
#define MAX_SIZE 100
```

---

## 3. Expression precedence

The function call chain of the recursive-descent parser defines the following precedence levels (from lowest to highest):

| Precedence | Operators | Associativity | Parse function |
|--------|--------|--------|----------|
| 1 (lowest) | `=` `+=` `-=` `*=` `/=` `%=` | right | `ParseAssignment()` |
| 2 | `==` `!=` `<` `>` `<=` `>=` | left | `ParseComparison()` |
| 3 | `+` `-` | left | `ParseAdditive()` |
| 4 | `*` `/` `%` | left | `ParseMultiplicative()` |
| 5 | `-` `!` `*` `&` `++` `--` (prefix) | right | `ParseUnary()` |
| 6 (highest) | literals, variables, function calls, message sends, parentheses | — | `ParsePrimary()` |

**Notes**:
- `&&` and `||` are not yet part of the expression parsing chain and cannot be used in expressions
- The bitwise operators `&` `|` `^` `<<` `>>` are recognized only at the lexical level
- The ternary operator `? :` is not implemented

---

## 4. Predefined macros

The compiler defines the following macros automatically at startup (see `ObjCCompiler.PredefinedMacros`):

| Macro | Value | Description |
|------|-----|------|
| `__VML__` | `1` | marks the VML compilation environment |
| `__VML_VERSION__` | `"1.65.32"` | VML toolchain version number |
| `__OBJC__` | `1` | marks the Objective-C compiler |
| `__DATE__` | `"Jun 02 2026"` (date at compile time) | compilation date (`MMM dd yyyy` format) |
| `__TIME__` | `"15:30:45"` (time at compile time) | compilation time (`HH:mm:ss` format) |

These macros are injected as the `PredefinedMacros` dictionary when they are handed to the `Preprocessor`, and take effect before the `#define`s in the source.

---

## 5. AST node types

| Node class | Purpose | Key fields |
|--------|------|----------|
| `ProgramNode` | top-level program root node | `Statements: List<ASTNode>` |
| `FuncDeclNode` | C function declaration/definition | `Name`, `ReturnType`, `Parameters`, `Body` |
| `VarDeclNode` | variable declaration | `Type`, `Name`, `Init` |
| `ObjCInterfaceNode` | `@interface` declaration | `Name`, `SuperClass`, `Members` |
| `ObjCImplNode` | `@implementation` definition | `Name`, `Methods` |
| `ObjCMethodNode` | ObjC method | `Name` (includes colons), `ReturnType`, `Parameters`, `Body` |
| `ReturnNode` | return statement | `Value` (may be null) |
| `IfNode` | if / else statement | `Condition`, `ThenBody`, `ElseBody` |
| `WhileNode` | while loop | `Condition`, `Body` |
| `ForNode` | for loop | `Init`, `Condition`, `Update`, `Body` |
| `LiteralNode` | literal | `Value` (int / float / string) |
| `VarNode` | variable reference | `Name` |
| `AssignNode` | simple assignment `=` | `Name`, `Value` |
| `BinaryNode` | binary expression | `Left`, `Op`, `Right` |
| `UnaryNode` | unary expression | `Op`, `Operand` |
| `CallNode` | C function call | `Name`, `Arguments` |
| `MsgSendNode` | ObjC message send | `Receiver`, `Method` (includes colons), `Arguments` |

---

## 6. Code generation details

### 6.1 Register conventions

Same as the unified VML internal standard:

| Register | Purpose |
|--------|------|
| R0 | accumulator / function return value / first argument |
| R1-R3 | function arguments |
| R4-R11 | general-purpose registers (caller-saved) |
| R12 | frame pointer (BP) (callee-saved) |
| R13 | stack pointer (SP) |
| R14 | link register (LR) (callee-saved) |
| R15 | return address (RA) (managed automatically by CALL/RET) |
| F0-F15 | single-precision floating-point registers (caller-saved) |
| D0-D7 | double-precision floating-point registers (caller-saved) |
| L0-L7 | 64-bit long integer registers (caller-saved) |

### 6.2 Function/method naming rules

| Kind | Label prefix | Example |
|------|----------|------|
| C function | `func_` | `int main()` → `func_main` |
| ObjC method | `objc_` | `- (void)doIt:(int)x` → `objc_doIt_` |

A `JMP skip_<label>` instruction is generated before the function body to skip over it (so that execution does not fall into it); the function body ends with `RET`.

### 6.3 Variable storage

- **Local variables**: allocated inside the stack frame; a `symbolTable` dictionary maps the variable name to a stack offset `R12-{offset}`. 4 bytes are allocated automatically on the first reference
- **Global variables**: stored in the data section (`dataSection`), addressed through the `var_<name>` label
- **String constants**: stored in the data section, generating a `str_<GUID>` label, with the address loaded by `MOVE R0, label`

### 6.4 Message send code generation

```objc
[obj setX:10 y:20]
```

compiles to the following VML instruction sequence:
```asm
; push the arguments (no need to reverse them first; all arguments are PUSHed in order)
MOVE R0, 10
PUSH R0
MOVE R0, 20
PUSH R0
; push the receiver
MOVE R0, [R12-{obj_offset}]
PUSH R0
; call the method
CALL objc_setX_y_
```

### 6.5 `self` handling

Inside an ObjC method body, `self` is added to the symbol table automatically with offset 0 (that is, the top of the stack frame that the frame pointer R12 points to). The method body may use `self` like an ordinary variable reference.

### 6.6 Standard library linking

When compiling a file (`CompileFile`), the compiler automatically:
1. Extracts the dependency library names from the `#import` / `#include` directives
2. Resolves library files under the `Lib/` directory by the `objc` language type
3. Calls `LibraryLinker` to link the external symbols

The `extractImports` regular expression matches both `#import` and `#include`, and supports both angle brackets `<>` and double quotes `""`.

---

## 7. Known limitations

| Limitation | Details |
|--------|----------|
| **@property synthesis** | The lexer recognizes `@property`/`@synthesize`/`@dynamic`, but the parser implements no automatic accessor synthesis at all. Properties must be written by hand as getter/setter methods |
| **@protocol** | The lexer recognizes the `@protocol` token; the parser implements no protocol declaration, adoption or conformance checking. The type system does not support the `id<Protocol>` syntax |
| **Category** | Not implemented at all. The `@interface ClassName (CategoryName)` extension syntax is not supported |
| **Block / Closure** | Not implemented at all. The `^` block syntax and closure capture are not supported |
| **ARC memory management** | No automatic reference counting, and no ownership qualifiers such as `strong`/`weak`/`unsafe_unretained`. Object lifetime must be managed by hand |
| **Property attributes** | The property attribute syntax `@property (nonatomic, strong)` is not parsed at all |
| **Message forwarding** | `forwardInvocation:` / `methodSignatureForSelector:` are not implemented |
| **KVC/KVO** | Key-value coding and key-value observing are not implemented |
| **Exception handling** | `@try` `@catch` `@finally` `@throw` are not implemented at all (skipped in MCU mode) |
| **struct/enum/union definitions** | The lexer recognizes the keywords, but parsing and code generation for struct member access `.`/`->` are not implemented |
| **Logical operators && / \|\|** | Tokens are defined, but they are not part of the expression parsing chain, so short-circuit logic cannot be used |
| **Bitwise operators** | The tokens `&` `\|` `^` `~` `<<` `>>` are defined, but the expression parser only supports `&` and `*` as unary address-of/dereference; binary bitwise operations are not supported |
| **Ternary operator** | The `? :` token is defined, but the parser implements no conditional expression |
| **switch/case** | Tokens are defined, but the parser implements no multi-way selection |
| **break/continue** | Tokens are defined, but the parser implements no loop control |
| **do-while** | Token is defined, but the parser does not implement it |
| **goto / labels** | Token is defined, but the parser does not implement it |
| **Type casting** | The C-style explicit cast `(type)expr` is not supported |
| **Arrays / pointer arithmetic** | Array subscripting `[]` and pointer arithmetic are not supported |
| **Object allocation** | No compile-time support for the `alloc`/`init` pattern or `+new`; the runtime must provide it by hand |
| **super semantics** | The `super` keyword is a token only; there is no method-lookup semantics |
| **Multi-file compilation** | Each call compiles a single `.m` file; class hierarchy resolution across files at link time is not supported |

---

## 8. Grammar reference (EBNF summary)

```
program          := { top_level }

top_level        := objc_interface
                  | objc_implementation
                  | objc_forward_class
                  | function_decl
                  | statement

objc_interface   := "@interface" IDENT [ ":" IDENT ] "{" { ivar_decl } "}" { method_decl } "@end"

objc_implementation := "@implementation" IDENT { method_impl | function_decl } "@end"

objc_forward_class := "@class" IDENT ";"

ivar_decl        := type IDENT ";"

method_decl      := ("-" | "+") type method_selector [ ";"]
method_impl      := ("-" | "+") type method_selector "{" { statement } "}"

method_selector  := IDENT [ ":" "(" type ")" IDENT ] { IDENT ":" "(" type ")" IDENT }

function_decl    := type IDENT "(" [ param_list ] ")" ( ";" | block )

param_list       := type IDENT { "," type IDENT }

block            := "{" { statement } "}"

statement        := return_stmt
                  | if_stmt
                  | while_stmt
                  | for_stmt
                  | var_decl
                  | expression ";"

return_stmt      := "return" [ expression ] ";"

if_stmt          := "if" "(" expression ")" ( block | statement ) [ "else" ( block | statement ) ]

while_stmt       := "while" "(" expression ")" ( block | statement )

for_stmt         := "for" "(" [ var_decl | expression ] ";" [ expression ] ";" [ expression ] ")" ( block | statement )

var_decl         := type IDENT [ "=" expression ] ";"

type             := "int" | "char" | "float" | "double" | "short" | "long" | "long long"
                  | "void" | "id" | "struct" IDENT | "enum" IDENT
                  | "unsigned" type | "signed" type

expression       := assignment

assignment       := comparison { ( "=" | "+=" | "-=" | "*=" | "/=" | "%=" ) assignment }

comparison       := additive { ( "==" | "!=" | "<" | ">" | "<=" | ">=" ) additive }

additive         := multiplicative { ( "+" | "-" ) multiplicative }

multiplicative   := unary { ( "*" | "/" | "%" ) unary }

unary            := ( "-" | "!" | "*" | "&" | "++" | "--" ) unary | primary

primary          := NUMBER
                  | STRING
                  | OBJC_STRING
                  | "nil" | "YES" | "NO"
                  | "[" expression method_selector_exp "]"
                  | IDENT [ "(" [ expression { "," expression } ] ")" ]
                  | "(" expression ")"
```

---

## 9. Compilation command examples

```bash
# Compile a single .m file
dotnet run --project VMLTool -- input.m -o output.vml

# Compile a .mm file
dotnet run --project VMLTool -- app.mm -o app.vml

# Compile directly with ObjCCompiler (programmatic interface)
ObjCCompiler.Compile("int main() { return 0; }")
ObjCCompiler.CompileFile("MyClass.m", includePaths, libraryPaths)
```

---

## 10. Version history

| Version | Date | Changes |
|------|------|------|
| 1.0 | 2025-12 | Initial implementation — lexer + recursive-descent parser + VML code generation |
| 1.65.17+ | 2026-05 | Integrated `Preprocessor`, the `__OBJC__` predefined macro, and `ExtractImports` linking support |

---

## 🆕 String encoding (v1.65.19)

This language compiler uses the VML string system indirectly through the shared library (Lib/shared/).

| Pseudo-op | Width | Encoding | C type |
|:------|:----:|:-----|:--------|
| `.string` | 8-bit | UTF-8 | `char*` |
| `.wstring` | 16-bit | UTF-16LE | `wchar_t*` |
| `.ustring` | 32-bit | UTF-32LE | `char32_t*` |

**MCU mode** (default): strings are output as UTF-8 (`.string`)
**OS mode**: the encoding can be tested with the `VML_WSTRING` macro

The shared library already provides wide-string conversion functions (wchar.h/uchar.h); each language compiler may use them as needed.
