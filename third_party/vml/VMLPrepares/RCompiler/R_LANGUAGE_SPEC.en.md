# R language compiler specification

> **Version**: v1.0 | **Date**: 2026-07-06 | **Revised by**: Shenzhen Tanso Intelligent Technology Co., Ltd.

## Metadata

| Item | Description |
|------|------|
| **Standard** | R 4.x subset (S language compatible) |
| **Language birth year** | 1993 |
| **Completeness** | ~96% |
| **File extension** | `.r` |
| **Compiler entry point** | `RCompiler.RCompiler.Compile()` |
| **Plugin registration** | `RCompilerPlugin : CompilerPluginExBase` |
| **Language category** | statistical analysis language |

## Overview

R is a programming language and environment for statistical computing and graphics. This compiler implements the core syntax subset of R, supporting vectorized data operations, statistical functions, control flow and user-defined functions. The compilation output is VML assembly, which can be further translated to 16 target architectures.

The compiler architecture follows the standard VML front end pattern:

```
R source (.r) → Lexer (Lexer.cs) → Token stream
              → Parser (Parser.cs) → AST (ASTNode.cs)
              → CodeGenerator (CodeGenerator*.cs) → VML program
```

The entry file `RCompiler.cs` orchestrates the whole compilation pipeline, including preprocessing, lexing, parsing, code generation and standard library linking.

## Supported features

### Data types

| Type | R representation | Internal representation | Description |
|------|--------|----------|------|
| `numeric` | `42`, `3.14`, `1.5e-10` | `int` / `float` | Integers and floats. In R, integer can take an `L` suffix (such as `42L`); the `L` is stripped during parsing |
| `character` | `"hello"`, `'world'` | `string` (data section) | String literals, supporting three delimiters: double quote, single quote and backtick |
| `logical` | `TRUE`, `FALSE` | `int` (1 / 0) | Booleans map to integers |
| `NULL` | `NULL` | `int` (0) | Empty value; R0 is set to zero |
| `NA` | `NA` | `int` (`int.MinValue`) | Missing value, represented by a sentinel value |
| `Inf` | `Inf` | `float` (`float.PositiveInfinity`) | Infinity |

Note: in the current implementation, numeric integer and float are not strictly distinguished at run time. Scientific notation (such as `1.5e-10`, `2E+3`) is fully supported.

### Assignment operators

R supports three assignment forms, in descending order of precedence:

```r
# 1. Left assignment (recommended style)
x <- 42
y <- c(1, 2, 3)

# 2. Equals assignment (function-argument style)
x = 42

# 3. Super assignment (assign into the parent environment)
x <<- 42
```

- `<-` and `=` assign a variable in the current scope
- `<<-` is marked as super assignment, used to write into the parent scope (in the current compiler implementation it behaves the same as `<-`; scope levels are not fully implemented yet)
- An assignment expression returns the right-hand value, so chained assignment is supported: `a <- b <- 10`

### Arithmetic operators

| Operator | Syntax | Description |
|--------|------|------|
| Addition | `a + b` | Add numeric values |
| Subtraction | `a - b` | Subtract numeric values |
| Multiplication | `a * b` | Multiply numeric values |
| Division | `a / b` | Floating-point division |
| Exponentiation | `a ^ b` | Raise to a power |
| Modulo | `a %% b` | Integer remainder (note: it must be two `%` characters) |

### Comparison operators

| Operator | Syntax | Description |
|--------|------|------|
| Equal | `a == b` | Equality comparison |
| Not equal | `a != b` | Inequality comparison |
| Less than | `a < b` | Less-than comparison |
| Greater than | `a > b` | Greater-than comparison |
| Less than or equal | `a <= b` | Less than or equal to |
| Greater than or equal | `a >= b` | Greater than or equal to |

A comparison returns the integer `1` (TRUE) or `0` (FALSE).

### Logical operators

| Operator | Syntax | Description |
|--------|------|------|
| Logical AND | `a & b` | Short-circuit evaluation (mapped to `&&`) |
| Logical OR | `a \| b` | Short-circuit evaluation (mapped to `\|\|`) |
| Logical NOT | `!a` | Unary negation |

Note: the current implementation does not distinguish the vectorized `&`/`|` from the short-circuit `&&`/`||`; all logical operations use short-circuit evaluation. R0 = 0 counts as FALSE, non-zero counts as TRUE.

### Functions

```r
# Function definition
add <- function(a, b) {
    return(a + b)
}

# Function with no parameters
greet <- function() {
    return("Hello")
}

# Implicit return: the value of the last expression in the body is the return value
square <- function(x) {
    x * x
}
```

- Anonymous functions are defined with the `function(params) { body }` syntax and assigned to a variable name with `<-`
- The parameter list may be empty
- `return(expr)` returns early; `return expr` (without parentheses) is also supported
- The value of the last expression in the function body is implicitly the return value
- Function calls: `name(arg1, arg2, ...)`; arguments are pushed onto the stack from right to left and the return value is in register R0
- A function has its own variable scope (parameters are pushed into the current stack frame and restored when the function exits)

### Control flow

#### if / else

```r
if (x > 0) {
    print("positive")
} else {
    print("non-positive")
}

# else if chain (parsed as nesting)
if (x > 0) {
    print("positive")
} else if (x < 0) {
    print("negative")
} else {
    print("zero")
}
```

The condition expression is evaluated and compared with 0: `CMP R0, 0; JE`. Non-zero is true.

#### while loop

```r
i <- 1
while (i <= 10) {
    print(i)
    i <- i + 1
}
```

A pre-test loop: when the condition is false the loop body is skipped.

#### for loop

```r
for (i in 1:5) {
    print(i)
}

for (x in c(10, 20, 30)) {
    print(x)
}
```

- `for (variable in sequence) { body }` syntax
- The iteration variable is given the current element of the sequence on every iteration
- `c()` vectors and range expressions are supported
- The sequence length is fixed at compile time (only constant-length sequences are supported)

#### repeat loop

```r
repeat {
    # Must contain a break condition, otherwise it is an infinite loop
    if (condition) {
        break
    }
}
```

An infinite loop; it must be exited explicitly with `break`.

#### break and next

```r
# break: exit the innermost loop
for (i in 1:10) {
    if (i > 5) {
        break
    }
}

# next: skip the current iteration and go to the next one
for (i in 1:10) {
    if (i %% 2 == 0) {
        next
    }
    print(i)  # prints odd numbers only
}
```

- `break` jumps to the loop end label
- `next` jumps to the loop condition check label (equivalent to `continue` in C)
- Using either outside a loop causes a compilation error

### Vectors

```r
# c() constructor
x <- c(1, 2, 3, 4, 5)
y <- c(TRUE, FALSE, TRUE)
z <- c()  # empty vector, returns NULL

# Range operator
1:10    # integer sequence, parsed as a special call
```

- Vectors are built with the `c()` function and are handled specially in the parser as a `SeqNode` AST node
- Vectors are allocated on the heap (`SYSCALL 2` = malloc), each element taking 4 bytes
- Elements are stored in order in contiguous memory
- The empty vector `c()` returns 0 (NULL)
- The range operator `start:end` is parsed as a call to the `:` function
- **Not supported**: named vectors, type coercion, vector recycling

### Indexing

```r
x <- c(10, 20, 30, 40)
a <- x[1]   # 10 (R indexing starts at 1)
b <- x[3]   # 30
```

- Uses the `[index]` syntax, 1-based indexing
- Internal implementation: subtract 1 from the index, multiply by 4 (word offset), load from the base address
- The index expression can be any expression: `x[i + 1]`
- **Not supported**: negative indexes (exclusion), logical indexing, multi-dimensional indexing, the `[[` extraction operator, slicing `x[2:4]`

### Comments

```r
# This is a single-line comment
x <- 1  # end-of-line comment
```

Only single-line comments starting with `#` are supported. Multi-line comments are not supported.

### Scientific notation

```r
x <- 1.5e-10   # 1.5 × 10^-10
y <- 2.0E+5    # 200000
z <- 3e8        # 300000000
```

The lexer fully parses scientific notation: mantissa, decimal point, `e`/`E`, optional sign, exponent.

### Identifier rules

- Starts with a letter, underscore or dot (a dot must be followed by a letter or underscore)
- Following characters: letters, digits, underscores, dots
- Case sensitive: `x` and `X` are different variables
- Keywords cannot be used as identifiers
- Examples of legal identifiers: `x`, `_private`, `.hidden`, `my.var`, `data_1`

### source() import

```r
source("lib/utils.r")
```

The `source()` call is extracted from the source with a regular expression (parsed before the preprocessing stage) and links the contents of the given `.r` file into the current program. This is a compile-time mechanism, not a run-time function call.

## Expression precedence table

Listed from highest to lowest precedence (a smaller number means higher precedence):

| Precedence | Operators | Associativity | Description |
|--------|--------|--------|------|
| 1 | `()` `[]` `$` `:` `f()` | left | Grouping, indexing, member access, range, function call |
| 2 | `-` (unary) `!` `+` (unary) | right | Unary minus, logical NOT, unary plus |
| 3 | `^` | right | Exponentiation |
| 4 | `*` `/` `%%` | left | Multiply, divide, modulo |
| 5 | `+` `-` | left | Add, subtract |
| 6 | `==` `!=` `<` `>` `<=` `>=` | left | Comparison operators |
| 7 | `&` | left | Logical AND |
| 8 | `\|` | left | Logical OR |
| 9 | `<-` `<<-` `=` | right | Assignment |

## Predefined macros

The compiler injects the following macros during the preprocessing stage (`Preprocessor`):

| Macro name | Typical value | Description |
|--------|--------|------|
| `__VML__` | `1` | Indicates the code is being compiled in the VML toolchain |
| `__VML_VERSION__` | `"1.65.32"` | VML toolchain version number |
| `__R__` | `1` | Indicates the language being compiled is R |
| `__DATE__` | `"Jun 02 2026"` | Compilation date |
| `__TIME__` | `"15:30:00"` | Compilation time |

These macros can be used through the preprocessor directives `#ifdef`, `#ifndef`, `#if` and so on, enabling conditional compilation.

## Limitations and unsupported features

The current implementation is 🟢 production-ready level. The following core R language features are not supported yet:

### Object system

- **S3 objects**: classes and generic functions (`class<-`, `UseMethod`, `NextMethod`)
- **S4 objects**: formal class definitions and method dispatch (`setClass`, `setMethod`)
- **RC (Reference Classes)**
- **R6 classes**: the modern OOP system

### Data structures

- **`list()`**: recursive lists (only the `c()` atomic vector is implemented)
- **data.frame**: the table data structure
- **matrix**: multi-dimensional matrices (`matrix()`, `%*%` matrix multiplication)
- **array**: multi-dimensional arrays (`array()`, `dim<-`)
- **factor**: categorical variables
- **names / attributes**: the attribute system (`names<-`, `attr<-`, `attributes<-`)
- **environment**: environment objects and lexical scope levels

### Functional programming

- **apply family**: `apply()`, `lapply()`, `sapply()`, `tapply()`, `mapply()` and so on
- **Closures**: functions capturing free variables in a lexical scope
- **`...` (dots)**: variadic arguments
- **`on.exit()`**: function exit hook
- **`invisible()`**: invisible return

### Formula interface

- **Formula objects**: `y ~ x1 + x2` (the core syntax of statistical modelling)
- Related functions: `lm()`, `glm()`, `terms()`, `model.frame()`

### Vectorized operations

- **Vector recycling**: automatic extension when operating on vectors of unequal length
- **Element-wise operations**: all operators are currently scalar operations and do not broadcast over vectors automatically
- **Vectorized ifelse()**: conditional vector selection

### Packages and libraries

- **library() / require()**: the package loading mechanism
- **CRAN packages**: any third-party package
- **Namespaces**: the `::` and `:::` operators
- **base/recommended packages**: the statistics and graphics packages shipped with R

### Other

- **Pipe operators**: `|>` (native pipe) and `%>%` (magrittr)
- **switch()**: multi-branch selection
- **tryCatch()**: exception handling
- **R complex type**: `complex`
- **R raw type**: `raw`
- **String manipulation functions**: `paste()`, `sprintf()`, `gsub()` and so on (they would need to be implemented manually through a C shared library)
- **Graphics system**: graphics device interfaces
- **S4 generics**: `setGeneric`, `setMethod`
- **Lazy evaluation**: R's lazy argument evaluation semantics
- **R's `[` `[[` `$` as functions**: the function form of operators
- **Negative and logical indexing**: `x[-1]`, `x[x > 5]`
- **Named list elements**: `x[["name"]]`
- **formals() / body()**: function object introspection

## Compiler implementation details

### File structure

```
VMLPrepares/RCompiler/
├── Token.cs                      # Token type definitions and value carrier
├── Lexer.cs                      # Lexer (inherits LexerBase)
├── ASTNode.cs                    # Abstract syntax tree node definitions
├── Parser.cs                     # Recursive-descent parser
├── CodeGenerator.cs              # Code generation entry point
├── CodeGenerator.Expressions.cs  # Expression code generation
├── CodeGenerator.Statements.cs   # Statement code generation
├── RCompiler.cs                  # Compiler entry point and pipeline orchestration
├── RCompilerPlugin.cs            # Plugin registration
├── Program.cs                    # CLI entry point
└── RCompiler.csproj              # Project file
```

### Register conventions

Follows the standard VML calling convention:

| Register | Purpose |
|--------|------|
| R0 | accumulator / function return value |
| R12 | frame pointer (BP) |
| R13 | stack pointer (SP) |
| R15 | return address (RA) |

### Memory model

- Local variables: allocated inside the stack frame, addressed through `R12-offset`
- Global variables: allocated in the data section, addressed through the label `var_name`
- An unknown variable gets space allocated on the stack automatically at its first assignment (4-byte aligned)
- Vector elements: heap allocated (`SYSCALL 2` / malloc), returning the base address pointer

### Function calling convention

- Arguments are pushed onto the stack from right to left
- `CALL func_<name>` jumps to the function body label
- In the callee:
  - prologue: `PUSH R15; PUSH R12; MOVE R12, R13`
  - body: executes statements. Arguments are accessed inside the stack frame through `R12-<offset>`
  - epilogue: `POP R12; RET`
- The return value is passed through R0

### String handling

- String literals are stored in the data section with a unique label `str_<guid>`
- `MOVE R0, label` loads the string address into R0
- Repeated strings share the same label (deduplicated through the `_stringCache` dictionary)

### Standard output

The current compiler has no built-in `print()` implementation. Output has to go through VML SYSCALLs or BIOS calls, or by linking the `printf` function from the C shared library.

### Typical R to VML mappings

| R construct | VML instruction sequence |
|--------|-------------|
| `x <- 10` | `MOVE R0, 10; MOVE [R12-x], R0` |
| `a + b` | `MOVE R0, [R12-a]; MOVE R0, [R12-b]; ADD R0, R0, R0` |
| `if (x) { ... }` | `MOVE R0, [R12-x]; CMP R0, 0; JE else_label; ...; LABEL else_label` |
| `while (x) { ... }` | `LABEL while_start; MOVE R0, [R12-x]; CMP R0, 0; JE wend; ...; JMP while_start; LABEL wend` |
| `for (i in c(1,2,3)) { ... }` | index initialization + condition check loop + loading the sequence element value on every iteration |
| `return(x)` | `MOVE R0, [R12-x]; POP R12; RET` |
| `f(x, y)` | `MOVE R0, [R12-y]; PUSH R0; MOVE R0, [R12-x]; PUSH R0; CALL func_f` |
| `v[i]` | compute base address + (i-1)*4, indirect load |
| `c(1, 2, 3)` | `SYSCALL 2` (malloc 12) + store elements in turn + restore the base address into R0 |

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
