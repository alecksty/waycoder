# Python language compiler specification

> **Version**: v1.0 | **Date**: 2026-07-06 | **Revised by**: Shenzhen Tanso Intelligent Technology Co., Ltd.

## Specification standard

| Field | Value |
|:-----|:----|
| **Target standard** | Python 3.0 (2008) |
| **Release year** | 2008 |
| **Completeness** | ~90% |
| **Updates** | 2026-05-18: list/dict/tuple/set are all implemented with heap allocation |
| **Tests** | 6 passing |

> Version 1.0 | 2026-04-19 | Compiler path: VMLPrepares/PythonCompiler/

---

## 1. Goals and scope

Compile a subset of Python into VML assembly that runs on the VML virtual machine.
VML is a hybrid register-plus-stack architecture with 16 general-purpose registers (R0–R15), supporting integer and floating-point arithmetic.

**Not implemented**: class, async/await, decorators, type annotations, list comprehensions, the generator runtime

---

## 2. Reserved words

| Reserved word | Semantics | Parser | CodeGen |
|--------|------|--------|---------|
| False/True/None | literals | ✅ | ✅→0/1/0 |
| and/or/not | logical operators | ✅ short-circuit | ✅ jump chain |
| if/elif/else | conditionals | ✅ | ✅ conditional jumps |
| while/for | loops | ✅ with else | ✅ loop + else |
| break/continue | loop control | ✅ | ✅ JMP |
| def | function definition | ✅ | ✅ stack frame convention |
| return | return | ✅ | ✅ epilogue |
| pass | empty statement | ✅ | ✅ NOP |
| lambda | anonymous function | ✅ | ⚠️ stub |
| assert | assertion | ✅ | ⚠️ stub |
| del | delete | ✅ | ⚠️ stub |
| global/nonlocal | scope | ✅ | ⚠️ stub |
| import/from | module import | ✅ | ⚠️ stub |
| raise | raise an exception | ✅ | ⚠️ stub |
| try/except/finally | exceptions | ✅ | ⚠️ stub |
| with | context manager | ✅ | ⚠️ stub |
| yield | generators | ✅ | ⚠️ stub |
| match/case | pattern matching | ✅ constant patterns | ⚠️ stub (jumps already generated) |
| in/is | membership/identity | ✅ comparison | ⚠️ stub (→0) |
| as | alias | ✅ | — |
| // | division | ✅ | ✅ // |
| _ | wildcard | ✅ | ⚠️ →0 |

---

## 3. Data types

### 3.1 Literals

| Type | Syntax | Lexer Token | CodeGen |
|------|------|-------------|---------|
| Decimal integer | 42, 1_000 | INTEGER | LOAD RR0 #42 |
| Hexadecimal | 0xFF | INTEGER | LOAD RR0 #255 |
| Octal | 0o77 | INTEGER | LOAD RR0 #63 |
| Binary | 0b1010 | INTEGER | LOAD RR0 #10 |
| Float | 3.14, 1e10 | FLOAT | LOADF RR0 #3.14 |
| String | "hello", 'world' | STRING | dataSection + LOAD RR0 [addr] |
| Triple-quoted | """multi""" | STRING | dataSection |
| Boolean | True/False | TRUE/FALSE | LOAD RR0 #1/#0 |
| None | None | NONE | LOAD RR0 #0 |
| List | [1, 2, 3] | LBRACKET | ✅ heap allocated [length + elements] |
| Dictionary | {k:v} | LBRACE | ✅ heap allocated [pair count + key/value pairs] |
| Tuple | (1, 2) | LPAREN | ✅ heap allocated [length + elements] |
| Set | {1, 2, 3} | LBRACE | ✅ heap allocated [length + elements] |

**Dictionary vs set ambiguity**: `{expr}` is a set, `{expr:expr}` is a dictionary, `{}` is an empty dictionary.

### 3.2 Type compatibility

VML has no dynamic typing; at runtime every value is an integer (int) or a float (float). bool→int, None→0.
A string is a special data-segment reference (an address), and runtime string operations are not supported yet.

---

## 4. Operator precedence (lowest to highest)

```
Level 1  lambda x: expr                    # loosest, right associative
Level 2  x if cond else y                 # ternary
Level 3  or                                # short-circuit
Level 4  and                               # short-circuit
Level 5  not x                             # unary
Level 6  in | is | < <= > >= != ==         # comparison (chained: a<b<c → (a<b)&(b<c))
Level 7  |                                 # bitwise or
Level 8  ^                                 # bitwise xor
Level 9  &                                 # bitwise and
Level 10 << >>                             # shifts
Level 11 + -                               # add/subtract
Level 12 * / // %                          # multiply/divide/floor-divide/modulo
Level 13 -x +x ~x                          # unary
Level 14 **                                # power (right associative)
Level 15 await x                           # unary (runtime not implemented yet)
Level 16 x[i] x.attr f(args)              # subscript/attribute/call (highest)
```

### VML mapping of the operators

| Operator | VML instruction | Notes |
|--------|----------|------|
| + | ADD RR0 RR0 RR1 | |
| - | SUB RR0 RR0 RR1 | |
| * | MUL RR0 RR0 RR1 | |
| / | DIV RR0 RR0 RR1 | float |
| // | DIV RR0 RR0 RR1 + truncation | floor division |
| % | MOD RR0 RR0 RR1 | |
| ** | loop MUL | simplified for now |
| & | AND RR0 RR0 RR1 | |
| \| | OR RR0 RR0 RR1 | |
| ^ | XOR RR0 RR0 RR1 | |
| ~ | NOT RR0 RR0 | |
| << | SHL RR0 RR0 RR1 | |
| >> | SHR RR0 RR0 RR1 | |
| -x (neg) | SUB RR0 #0 RR0 | |
| == | CMP + JE/JNE | |
| != | CMP + JNE/JE | |
| < | CMP + JL | |
| <= | CMP + JLE | |
| > | CMP + JG | |
| >= | CMP + JGE | |
| and | short-circuit jump chain | if A is 0, skip B |
| or | short-circuit jump chain | if A is non-zero, skip B |
| not | CMP RR0 #0 + invert | |

---

## 5. Statement and VML semantics mapping

### 5.1 Function definition

```python
def foo(a, b, c):
    return a + b + c
```

VML stack frame convention:
```
caller: PUSH c, PUSH b, PUSH a  (right to left)
       CALL foo                  (pushes the return address)
callee prologue: PUSH R15, PUSH R12, MOVE R12 R13
callee epilogue: MOVE R13 R12, POP R12, POP R15, RET
stack layout (R12 = frame pointer):
  arg0  ← R12+12
  arg1  ← R12+8
  arg2  ← R12+4
  ret   ← R12+0  (implicit)
  saved R15 ← R12-4
  saved R12 ← R12-8
  local0 ← R12-12
  local1 ← R12-16
```

### 5.2 if/elif/else

```python
if a > 0:
    x = 1
elif a == 0:
    x = 0
else:
    x = -1
```

VML:
```
    LOAD RR0 [R12-a]
    CMP RR0 #0
    JLE elif_0
    LOAD RR0 #1
    STORE [R12-x] RR0
    JMP endif_0
elif_0:
    LOAD RR0 [R12-a]
    CMP RR0 #0
    JNE else_0
    LOAD RR0 #0
    STORE [R12-x] RR0
    JMP endif_0
else_0:
    LOAD RR0 #-1
    STORE [R12-x] RR0
endif_0:
```

### 5.3 while loops

```python
while i < n:
    i = i + 1
else:
    result = -1
```

VML:
```
while_0:
    LOAD RR0 [R12-i]
    CMP RR0 [R12-n]
    JGE endwhile_0
    # body: i = i + 1
    LOAD RR0 [R12-i]
    PUSH RR0
    LOAD RR0 #1
    MOVE RR1 RR0
    POP RR0
    ADD RR0 RR0 RR1
    STORE [R12-i] RR0
    JMP while_0
endwhile_0:
    # else branch (runs when the while condition is False and no break left the loop)
    LOAD RR0 #-1
    STORE [R12-result] RR0
```

**while-else semantics**: the else runs when the loop ends normally (the condition is false); it does not run when a break leaves the loop.
Implementation: break jumps to just after endwhile.

### 5.4 for loops

```python
for i in range(n):
    x = i * 2
```

VML:
```
    LOAD RR0 #0           # i = 0
    STORE [R12-i] RR0
for_0:
    LOAD RR0 [R12-i]
    CMP RR0 [R12-n]
    JGE endfor_0
    # body: x = i * 2
    LOAD RR0 [R12-i]
    PUSH RR0
    LOAD RR0 #2
    MOVE RR1 RR0
    POP RR0
    MUL RR0 RR0 RR1
    STORE [R12-x] RR0
    # increment
    LOAD RR0 [R12-i]
    PUSH RR0
    LOAD RR0 #1
    MOVE RR1 RR0
    POP RR0
    ADD RR0 RR0 RR1
    STORE [R12-i] RR0
    JMP for_0
endfor_0:
```

### 5.5 match/case

```python
match n:
    case 1:
        print(100)
    case 2:
        print(200)
    case _:
        print(999)
```

VML:
```
    LOAD RR0 [R12-n]
    PUSH RR0
    LOAD RR0 #1
    CMP RR0 RR1
    JE case_0_end
    LOAD RR0 #100
    PUSH RR0
    SYSCALL #4
    ADD RR13 RR13 #4
    JMP match_end
case_0_end:
    LOAD RR0 #2
    CMP RR0 RR1
    JE case_1_end
    LOAD RR0 #200
    PUSH RR0
    SYSCALL #4
    ADD RR13 RR13 #4
    JMP match_end
case_1_end:
    # _ wildcard: always matches
    LOAD RR0 #999
    PUSH RR0
    SYSCALL #4
    ADD RR13 RR13 #4
    JMP match_end
match_end:
    POP RR0
```

**Semantics**: push the matched value, then compare it in each case; the _ wildcard matches unconditionally. After a case body runs successfully, JMP match_end.

### 5.6 lambda

```python
f = lambda x, y: x + y
```

VML:
```
lambda_f_0:
    PUSH R15, PUSH R12, MOVE R12 R13
    # x = R12+12, y = R12+8
    LOAD RR0 [R12+12]
    PUSH RR0
    LOAD RR0 [R12+8]
    MOVE RR1 RR0
    POP RR0
    ADD RR0 RR0 RR1
    MOVE RR13 R12, POP R12, POP R15, RET
# f points at the address of lambda_f_0
```

### 5.7 assert

```python
assert x > 0, "x must be positive"
```

VML:
```
    # evaluate x > 0 → bool in RR0
    CMP RR0 #0
    JNE assert_ok_0
    # failure: print message, halt
    LOAD RR0 #-1
    SYSCALL #99          # abnormal exit
assert_ok_0:
```

### 5.8 Assignment and augmented assignment

```python
x = 10
x += 5
```

VML:
```
    LOAD RR0 #10
    STORE [R12-x] RR0
    LOAD RR0 [R12-x]
    PUSH RR0
    LOAD RR0 #5
    MOVE RR1 RR0
    POP RR0
    ADD RR0 RR0 RR1
    STORE [R12-x] RR0
```

### 5.9 Ternary expression

```python
result = 1 if a > 10 else 0
```

VML:
```
    LOAD RR0 [R12-a]
    CMP RR0 #10
    JG cmp_true_0
    LOAD RR0 #0
    JMP cmp_end_1
cmp_true_0:
    LOAD RR0 #1
cmp_end_1:
    STORE [R12-result] RR0
```

---

## 6. Built-in functions

| Function | VML | Arguments | Return value |
|------|-----|------|--------|
| print(x) | SYSCALL #4 | 1 int on the stack | none |
| input() | SYSCALL #7 | none | RR0 = integer |
| len(x) | stub | — | RR0 = 0 |
| range(n) | for loop unrolling | — | special-cased |
| abs(x) | inlined | RR0 | RR0 |
| int(x) | inlined | RR0 | RR0 |
| str(x) | stub | — | RR0 = 0 |

---

## 7. Indentation rules (Lexer)

- Leading spaces > currentIndent → emit an INDENT token, update currentIndent
- Leading spaces < currentIndent → emit DEDENT tokens in a loop, currentIndent -= 4
- No leading spaces and currentIndent > 0 → emit DEDENT tokens until 0
- The indent unit is fixed at 4 spaces, tab = 4 spaces
- At end of file, every unclosed INDENT is matched with a DEDENT

---

## Floating-point and 64-bit compilation modes

The VML toolchain controls how floating point and 64-bit integers are handled through three compilation flags:

| Flag | Possible values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32-bit float handling mode |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64-bit float (float) handling mode |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64-bit integer (int) handling mode |

### 32-bit float (float32)

Python's `float` type is mapped to a 64-bit double-precision float when VML compiles it. 32-bit float arithmetic is used for internal intermediate results:

- **`hard` mode (default)**: uses the VML native float instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`, operating directly through the sixteen float registers F0-F15.
- **`soft` mode**: uses the Q15.16 fixed-point software emulation library `softfloat.c`, emulating float arithmetic through the `__vml_float_add/sub/mul/div/neg/abs/cmp` functions and so on.
- **`none` mode**: floating-point arithmetic is disabled.

### 64-bit float (double)

Python's `float` type is natively an IEEE 754 double-precision 64-bit float and compiles as follows:

- **`soft` mode (default)**: uses the IEEE 754 double-precision software emulation library `softdouble.c`, emulating through the `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` functions and so on. Compatible with every platform (MCU included).
- **`hard` mode**: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`, operating through the eight double-precision registers D0-D7.
- **`none` mode**: floating-point arithmetic is disabled.

### 64-bit integer (int64)

Python 3's `int` type is an arbitrary-precision integer. When VML compiles it, integers within the 64-bit range are handled as follows:

- **`soft` mode (default)**: uses the dual-register software emulation library `softint64.c`, emulating 64-bit integer arithmetic through the `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` functions and so on.
- **`hard` mode**: reserved; a future VML version will support native 64-bit integer instructions.
- **`none` mode**: degrades to 32-bit integers.

### Software emulation libraries

The software emulation libraries above all live in the `Lib/shared/` directory, are written in C, are compiled to VML by the C compiler, and are shared by every language:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point 32-bit float emulation | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 double-precision 64-bit float emulation | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | 64-bit integer dual-register emulation | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

## 8. Unsupported Python features

| Feature | Reason |
|------|------|
| class / __init__ / inheritance | the class system is too complex |
| async / await | needs an event loop |
| decorators @ | low-priority syntactic sugar |
| type annotations x: int | compile-time checking is not needed yet |
| f-string | string interpolation is complex |
| list comprehensions [x*2 for x in ...] | needs iterators |
| the yield generator runtime | needs a state machine |
| with __enter__/__exit__ | simplified to an empty implementation |
| full match patterns (destructuring/guards) | constants + wildcards only |
| *args/**kwargs | variadic parameters |
| multiple assignment a, b = 1, 2 | unpacking |
| the walrus operator := | not implemented yet |

---

## 9. Change log

| Date | Version | Change |
|------|------|------|
| 2026-04-19 | 0.1 | initial version |
| 2026-04-19 | 1.0 | added the VML semantics mapping, the stack frame convention, the indentation rules, and while/for-else semantics |

---

## 🆕 String type (v1.65.19)

This language compiler uses `.wstring` (UTF-16LE) by default as its internal string storage.

| Mode | Default encoding | VML directive | SYSCALL output |
|:-----|:--------|:----------|:------------|
| MCU (default) | `.string` (UTF-8) | `.string` | #1 |
| OS | `.wstring` (UTF-16LE) | `.wstring` | #391 |

**Predefined macro**: `VML_WSTRING` — defined automatically in OS mode, undefined in MCU mode
**Output function**: OS mode automatically uses `shared_print_wstr` (automatic UTF-16LE→UTF-8 conversion)

```c
// user code can determine the encoding through the macro
#ifdef VML_WSTRING
  // the default string is a wstring (UTF-16LE)
#else
  // the default string is UTF-8
#endif
```
