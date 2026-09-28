# Ruby

Usable, but this front end supports the fewest features of all: «bold»write everything flat«/».

## How to run it on your phone

```
vml run examples/ruby/sysinfo.rb
```

Compiling takes a while (one to two minutes for C, a few seconds for scripting languages).
Once the program is running, **the gamepad is at the bottom of the screen** - the D-pad
and the four action buttons are all there. Tap the back arrow in the top-right corner to
return to the Shell.

## What to know before you write

- Put all the logic at the top level; do not wrap it in functions and do not nest
- Call `ui_*` directly

## Examples

| File | What it shows |
|---|---|
| `catch.rb` | Catch the blocks (a completely flat version; this path cannot do functions) |
| `file_io.rb` | Reading and writing files |
| `sysinfo.rb` | Device information |

Also in this folder: `demo_std.rb`, `demo_tty.rb`, `demo_ui.rb`

## Pitfalls we hit on real devices

- ⚠ «bold»`&&` is not recognised«/» (use `and`, or write separate `if` statements).
- ⚠ «bold»`def` reports `Unexpected token: End`«/» - in other words «bold»you cannot write a single function«/» on this path,
  only flat code (`examples/ruby/catch.rb` is written that way).

---

The rest of this page is taken straight from the VML source (`third_party/vml/VMLPrepares/RubyCompiler/`):
the `README` covers what this front end supports and how to compile it, and the
language reference covers the syntax itself. When upstream changes, regenerating
this page brings it up to date.

## Language reference

### Ruby Language Compiler Specification

> «bold»Version«/»: v1.0 | «bold»Date«/»: 2026-07-06 | «bold»Revised by«/»: Shenzhen Tanso Intelligent Technology Co., Ltd.

#### Specification standard

| Field | Value |
|:-----|:----|
| «bold»Target standard«/» | Ruby 1.9+ subset |
| «bold»Release year«/» | 1995 (Ruby 1.9: 2007) |
| «bold»Completeness«/» | ~96% |
| «bold»File extension«/» | `.rb` |

#### Overview

This compiler supports the core subset of the Ruby language and compiles Ruby source code into VML (Virtual Machine Language) assembly. Compiler flow: preprocessing → lexical analysis → syntax analysis → code generation.

#### Supported language features

##### 1. Data types

- «bold»Integers«/»: `Integer` — integer type (4-byte signed)
- «bold»Floats«/»: `Float` — single-precision floating point
- «bold»Strings«/»: `String` — string literals, double-quoted `"..."` or single-quoted `'...'`
- «bold»Symbols«/»: `Symbol` — starting with `:`, such as `:name`
- «bold»Arrays«/»: `Array` — square-bracket literals `[1, 2, 3]`
- «bold»nil«/»: `nil` — null value
- «bold»Booleans«/»: `true` / `false`

##### 2. Variables

```ruby
x = 10           # local variable
@x = 20          # instance variable (recognized but limited in function)
name = "hello"   # string variable
```

##### 3. Operators

###### Arithmetic operators
```ruby
+    # addition
-    # subtraction
*    # multiplication
/    # division
%    # modulo
**   # exponentiation
```

###### Compound assignment
```ruby
+=   # add and assign
-=   # subtract and assign
*=   # multiply and assign
/=   # divide and assign
```

###### Comparison operators
```ruby
==   # equal
!=   # not equal
<    # less than
>    # greater than
<=   # less than or equal
>=   # greater than or equal
<=>  # spaceship operator (comparison result -1/0/1)
```

###### Logical operators
```ruby
!    # logical not (not)
```

##### 4. Control flow

###### Conditionals
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

###### Loops
```ruby
while condition
  # loop body
end

until condition
  # loop body (repeats while the condition is false)
end
```

###### for loop
```ruby
for var in start..end
  # loop body
end
```

##### 5. Method definitions

```ruby
def method_name(param1, param2)
  # method body
  return value
end

def method_name  # method without parameters
  # body
end
```

##### 6. Class definitions

```ruby
class ClassName
  def method_name
    # method body
  end
end
```

Internally, classes are compiled into methods carrying a `self.` prefix, and `self` is stored as the first local variable.

##### 7. Return values

```ruby
return value     # with a return value
return           # returns nil
```

##### 8. Method calls

```ruby
method_name(arg1, arg2)
object.method_name(arg1)
method_name       # call without parameters
```

##### 9. Comments

```ruby
# single-line comment
```

#### Expression precedence

| Precedence | Type | Operators |
|:------:|:-----|:-------|
| 1 (lowest) | assignment | `=` `+=` `-=` `*=` `/=` |
| 2 | comparison | `==` `!=` `<` `>` `<=` `>=` `<=>` |
| 3 | addition | `+` `-` |
| 4 | multiplication | `*` `/` `%` `**` |
| 5 | unary | `-` `!` |
| 6 (highest) | primary | literals, variables, function calls, parentheses |

#### Predefined macros

| Macro | Value |
|:-----|:---|
| `__VML__` | `1` |
| `__VML_VERSION__` | `"1.65.32"` |
| `__RUBY__` | `1` |
| `__DATE__` | compilation date |
| `__TIME__` | compilation time |

#### Known limitations

- `module`, `yield`, `block` and `mixin` are not supported
- No exception handling (`begin`/`rescue`/`ensure`)
- No string interpolation (`"hello #{name}"`)
- No hash literals `{key => value}`
- No splat operator `*args`
- Class inheritance and polymorphic method dispatch are not yet implemented

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

### Ruby Compiler

«bold»Path«/»: `VMLPrepares/RubyCompiler/`
«bold»Completeness«/»: ~96% | 🟢 Production ready
«bold»Standard library«/»: `Lib/ruby/`

#### Features
- ✅ Lexer + parser + code generator (Lexer/Parser/CodeGenerator)
- ✅ Control flow (if/elsif/else/unless/while/until/for/in)
- ✅ Functions: def/return/parameter passing
- ✅ Class: class definitions/methods/instance variables
- ✅ Operators: assignment/comparison/arithmetic/unary/<=>/and/or/not
- ✅ Literals: integer/float/string/boolean/array/Symbol/`**` power/`..` range
- ✅ Compound assignment (+= -= *= /= %=)
- ✅ case/when/else branching
- ✅ 34 tests, 0 failures
- ✅ module/mixin — include is inlined
- ⚠️ yield/block/Proc — not implemented
- ✅ require library import
- ✅ C-style preprocessing (#ifdef/#ifndef/#define/#include)

#### Compilation modes

##### MCU mode (default `--mode mcu`)
MCU mode is optimized for microcontroller/bare-metal environments and automatically skips features that are incompatible with an operating system.

«bold»Skipped«/» (not supported on MCU):
- require dynamic loading, yield/block

«bold»Retained«/» (implemented by the BIOS):
- Basic type arithmetic, control flow, function calls
- print/puts map to UART

##### OS mode (`--mode os`)
OS mode supports the full set of language features (file system, threads, etc.).

##### RAM levels
- `--ram k`: kilobyte level (2 KB~64 KB)
- `--ram m`: megabyte level (64 KB~1 MB, default)
- `--ram g`: gigabyte level

#### Usage
```bash
dotnet run --project VMLTool -- input.rb -o output.vml
```

#### Tests
- `VMLTests/NewCompilerTests.cs` — 34 tests (0 failures)
- `VMLTests/FullPipelineTests.cs` — full-pipeline tests
- `Examples/ruby/` — example programs (start/factorial/file_io/info)
