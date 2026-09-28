# Ruby Language Compiler Specification

> **Version**: v1.0 | **Date**: 2026-07-06 | **Revised by**: Shenzhen Tanso Intelligent Technology Co., Ltd.

## Specification standard

| Field | Value |
|:-----|:----|
| **Target standard** | Ruby 1.9+ subset |
| **Release year** | 1995 (Ruby 1.9: 2007) |
| **Completeness** | ~96% |
| **File extension** | `.rb` |

## Overview

This compiler supports the core subset of the Ruby language and compiles Ruby source code into VML (Virtual Machine Language) assembly. Compiler flow: preprocessing → lexical analysis → syntax analysis → code generation.

## Supported language features

### 1. Data types

- **Integers**: `Integer` — integer type (4-byte signed)
- **Floats**: `Float` — single-precision floating point
- **Strings**: `String` — string literals, double-quoted `"..."` or single-quoted `'...'`
- **Symbols**: `Symbol` — starting with `:`, such as `:name`
- **Arrays**: `Array` — square-bracket literals `[1, 2, 3]`
- **nil**: `nil` — null value
- **Booleans**: `true` / `false`

### 2. Variables

```ruby
x = 10           # local variable
@x = 20          # instance variable (recognized but limited in function)
name = "hello"   # string variable
```

### 3. Operators

#### Arithmetic operators
```ruby
+    # addition
-    # subtraction
*    # multiplication
/    # division
%    # modulo
**   # exponentiation
```

#### Compound assignment
```ruby
+=   # add and assign
-=   # subtract and assign
*=   # multiply and assign
/=   # divide and assign
```

#### Comparison operators
```ruby
==   # equal
!=   # not equal
<    # less than
>    # greater than
<=   # less than or equal
>=   # greater than or equal
<=>  # spaceship operator (comparison result -1/0/1)
```

#### Logical operators
```ruby
!    # logical not (not)
```

### 4. Control flow

#### Conditionals
```ruby
if condition
  # then body
elsif other_condition
  # elsif body
else
  # else body
end

unless condition
  # then body (executed when the condition is false)
end
```

#### Loops
```ruby
while condition
  # loop body
end

until condition
  # loop body (repeats while the condition is false)
end
```

#### for loop
```ruby
for var in start..end
  # loop body
end
```

### 5. Method definitions

```ruby
def method_name(param1, param2)
  # method body
  return value
end

def method_name  # method without parameters
  # body
end
```

### 6. Class definitions

```ruby
class ClassName
  def method_name
    # method body
  end
end
```

Internally, classes are compiled into methods carrying a `self.` prefix, and `self` is stored as the first local variable.

### 7. Return values

```ruby
return value     # with a return value
return           # returns nil
```

### 8. Method calls

```ruby
method_name(arg1, arg2)
object.method_name(arg1)
method_name       # call without parameters
```

### 9. Comments

```ruby
# single-line comment
```

## Expression precedence

| Precedence | Type | Operators |
|:------:|:-----|:-------|
| 1 (lowest) | assignment | `=` `+=` `-=` `*=` `/=` |
| 2 | comparison | `==` `!=` `<` `>` `<=` `>=` `<=>` |
| 3 | addition | `+` `-` |
| 4 | multiplication | `*` `/` `%` `**` |
| 5 | unary | `-` `!` |
| 6 (highest) | primary | literals, variables, function calls, parentheses |

## Predefined macros

| Macro | Value |
|:-----|:---|
| `__VML__` | `1` |
| `__VML_VERSION__` | `"1.65.32"` |
| `__RUBY__` | `1` |
| `__DATE__` | compilation date |
| `__TIME__` | compilation time |

## Known limitations

- `module`, `yield`, `block` and `mixin` are not supported
- No exception handling (`begin`/`rescue`/`ensure`)
- No string interpolation (`"hello #{name}"`)
- No hash literals `{key => value}`
- No splat operator `*args`
- Class inheritance and polymorphic method dispatch are not yet implemented

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
