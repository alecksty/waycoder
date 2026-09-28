# JavaScript

Close in style, but you have to call the entry point yourself.

## How to run it on your phone

```
vml run examples/javascript/sysinfo.js
```

Compiling takes a while (one to two minutes for C, a few seconds for scripting languages).
Once the program is running, **the gamepad is at the bottom of the screen** - the D-pad
and the four action buttons are all there. Tap the back arrow in the top-right corner to
return to the Shell.

## What to know before you write

- After defining `function main()` «bold»remember to call it once«/»
- Call `ui_*` directly

## Examples

| File | What it shows |
|---|---|
| `bench.js` | Benchmark |
| `catch.js` | Catch the blocks |
| `file_io.js` | Reading and writing files |
| `sysinfo.js` | Device information |

Also in this folder: `demo_std.js`, `demo_tty.js`, `demo_ui.js`

---

The rest of this page is taken straight from the VML source (`third_party/vml/VMLPrepares/JavaScriptCompiler/`):
the `README` covers what this front end supports and how to compile it, and the
language reference covers the syntax itself. When upstream changes, regenerating
this page brings it up to date.

## Language reference

### JavaScript Language Compiler Specification

> «bold»Version«/»: v1.0 | «bold»Date«/»: 2026-07-06 | «bold»Revised by«/»: Shenzhen Tanso Intelligent Technology Co., Ltd.

#### Specification standard

| Field | Value |
|:-----|:----|
| «bold»Target standard«/» | ECMAScript 5.1 subset (2011) |
| «bold»Release year«/» | 2011 |
| «bold»Completeness«/» | ~94% |
| «bold»MCU completeness«/» | ~92% |
| «bold»Tests«/» | 0 (the test directory is to be created) |
| «bold»Update«/» | 2026-05-18: corrected the completeness and implementation-status descriptions |

#### Keywords

`break` `case` `catch` `continue` `debugger` `default` `delete` `do` `else` `finally`
`for` `function` `if` `in` `instanceof` `new` `return` `switch` `this` `throw`
`try` `typeof` `var` `void` `while` `with` `class` `const` `export` `import`
`let` `super` `yield` `true` `false` `null` `undefined` `NaN` `Infinity`

#### Overview

This compiler supports a subset of the JavaScript (ECMAScript 5) language and compiles JavaScript source code into VML (Virtual Machine Language) assembly. The compiler uses a statically-compiled architecture and supports the core JavaScript features, including functions, objects, arrays and asynchronous programming.

#### Supported language features

##### 1. Data types

###### Primitive types
- «bold»Numbers«/»: `Number` - double-precision floating point, such as `42`, `3.14`, `0xFF`
- «bold»Strings«/»: `String` - Unicode strings, such as `"hello"`, `'world'`
- «bold»Booleans«/»: `Boolean` - `true` or `false`
- «bold»Null«/»: `null` - null object reference
- «bold»Undefined«/»: `undefined` - undefined value
- «bold»Symbols«/»: `Symbol` (ES6) - unique identifier

###### Object types
- «bold»Objects«/»: `Object` - a collection of key-value pairs
- «bold»Arrays«/»: `Array` - ordered collection
- «bold»Functions«/»: `Function` - executable code block
- «bold»Dates«/»: `Date` - date and time
- «bold»Regular expressions«/»: `RegExp` - regular expression pattern

##### 2. Variable declarations

###### Declaration forms
```javascript
// var (function scope)
var x = 10;
var y, z = 20;

// let (block scope, ES6)
let counter = 0;
for (let i = 0; i < 10; i++) {
    console.log(i);
}

// const (constant, ES6)
const PI = 3.14159;
const MAX_SIZE = 100;

// Destructuring assignment (ES6)
const [a, b] = [1, 2];
const {name, age} = {name: "Alice", age: 30};
```

###### Scope
```javascript
// Global scope
var globalVar = "global";

function testScope() {
    // Function scope
    var functionVar = "function";
    
    if (true) {
        // Block scope (let/const)
        let blockVar = "block";
        console.log(blockVar); // accessible
    }
    // console.log(blockVar); // error: blockVar is not defined
}
```

##### 3. Functions

###### Function definitions
```javascript
// Function declaration
function add(a, b) {
    return a + b;
}

// Function expression
const multiply = function(x, y) {
    return x * y;
};

// Arrow function (ES6)
const divide = (x, y) => x / y;
const square = x => x * x;

// Immediately invoked function expression (IIFE)
(function() {
    console.log("立即执行");
})();

// Generator function (ES6)
function* generator() {
    yield 1;
    yield 2;
    yield 3;
}
```

###### Parameter handling
```javascript
// Default parameters (ES6)
function greet(name = "Guest") {
    console.log(`Hello, ${name}`);
}

// Rest parameters (ES6)
function sum(...numbers) {
    return numbers.reduce((total, num) => total + num, 0);
}

// Parameter destructuring (ES6)
function printUser({name, age, city = "Unknown"}) {
    console.log(`${name}, ${age}岁, 来自${city}`);
}
```

##### 4. Objects and classes

###### Object literals
```javascript
// Object creation
const person = {
    name: "Alice",
    age: 30,
    greet() {
        console.log(`Hello, I'm ${this.name}`);
    },
    // Computed property name (ES6)
    ["id_" + Date.now()]: "unique-id"
};

// Property access
console.log(person.name);      // dot notation
console.log(person["age"]);    // bracket notation

// Object spread (ES6)
const extended = {
    ...person,
    city: "Beijing",
    job: "Developer"
};
```

###### Class definitions (ES6)
```javascript
// Class declaration
class Animal {
    constructor(name) {
        this.name = name;
    }
    
    speak() {
        console.log(`${this.name} makes a sound`);
    }
    
    // Static method
    static isAnimal(obj) {
        return obj instanceof Animal;
    }
}

// Inheritance
class Dog extends Animal {
    constructor(name, breed) {
        super(name);
        this.breed = breed;
    }
    
    speak() {
        console.log(`${this.name} barks`);
    }
    
    // Getter/Setter
    get description() {
        return `${this.name} the ${this.breed}`;
    }
    
    set age(years) {
        this._age = years;
    }
}

// Using the class
const dog = new Dog("Buddy", "Golden Retriever");
dog.speak();
console.log(dog.description);
```

##### 5. Arrays and collections

###### Array operations
```javascript
// Array creation
const numbers = [1, 2, 3, 4, 5];
const empty = new Array(10);
const fromString = Array.from("hello");
const ofArray = Array.of(1, 2, 3);

// Array methods
numbers.push(6);           // add an element
numbers.pop();             // remove the last one
numbers.unshift(0);        // add to the front
numbers.shift();           // remove from the front

// Higher-order functions
const doubled = numbers.map(x => x * 2);
const evens = numbers.filter(x => x % 2 === 0);
const sum = numbers.reduce((total, x) => total + x, 0);
numbers.forEach(x => console.log(x));

// Array destructuring
const [first, second, ...rest] = numbers;
```

###### Set and Map (ES6)
```javascript
// Set - a collection of unique values
const set = new Set([1, 2, 3, 3, 4]); // {1, 2, 3, 4}
set.add(5);
set.delete(2);
console.log(set.has(3)); // true
set.forEach(value => console.log(value));

// Map - a collection of key-value pairs
const map = new Map();
map.set("name", "Alice");
map.set("age", 30);
console.log(map.get("name")); // "Alice"
console.log(map.size); // 2

// WeakSet and WeakMap
const weakSet = new WeakSet();
const weakMap = new WeakMap();
```

##### 6. Control flow

###### Conditionals
```javascript
// if-else
if (condition) {
    // true branch
} else if (anotherCondition) {
    // else-if branch
} else {
    // false branch
}

// Ternary operator
const result = condition ? "true" : "false";

// switch
switch (value) {
    case 1:
        console.log("One");
        break;
    case 2:
        console.log("Two");
        break;
    default:
        console.log("Other");
}
```

###### Loops
```javascript
// for loop
for (let i = 0; i < 10; i++) {
    console.log(i);
}

// for...of (ES6)
for (const item of array) {
    console.log(item);
}

// for...in (iterating over object properties)
for (const key in object) {
    console.log(key, object[key]);
}

// while loop
while (condition) {
    // loop body
}

// do...while loop
do {
    // the loop body runs at least once
} while (condition);
```

##### 7. Asynchronous programming

###### Promise
```javascript
// Creating a promise
const promise = new Promise((resolve, reject) => {
    setTimeout(() => {
        resolve("成功");
        // or reject(new Error("失败"));
    }, 1000);
});

// Using a promise
promise
    .then(result => {
        console.log(result);
        return result.toUpperCase();
    })
    .then(upper => console.log(upper))
    .catch(error => console.error(error))
    .finally(() => console.log("完成"));
```

###### async/await (ES2017)
```javascript
async function fetchData() {
    try {
        const response = await fetch("https://api.example.com/data");
        const data = await response.json();
        console.log(data);
        return data;
    } catch (error) {
        console.error("获取数据失败:", error);
        throw error;
    }
}

// Immediately invoke an async function
(async () => {
    const result = await fetchData();
    console.log("结果:", result);
})();
```

##### 8. Module system

###### ES6 modules
```javascript
// math.js - exporting module
export const PI = 3.14159;

export function add(a, b) {
    return a + b;
}

export default class Calculator {
    multiply(x, y) {
        return x * y;
    }
}

// app.js - importing module
import Calculator, { PI, add } from './math.js';
import * as math from './math.js';

const calc = new Calculator();
console.log(calc.multiply(2, 3));
console.log(add(PI, 1));
```

###### CommonJS modules (Node.js style)
```javascript
// Export
module.exports = {
    add: function(a, b) { return a + b; },
    subtract: function(a, b) { return a - b; }
};

// Or
exports.multiply = function(a, b) { return a * b; };

// Import
const math = require('./math.js');
console.log(math.add(2, 3));
```

##### 9. Error handling

```javascript
// try-catch-finally
try {
    // code that may throw
    riskyOperation();
    throw new Error("自定义错误");
} catch (error) {
    // handle the error
    console.error("捕获错误:", error.message);
    console.error("堆栈:", error.stack);
} finally {
    // cleanup code, always runs
    cleanup();
}

// Custom error type
class ValidationError extends Error {
    constructor(message, field) {
        super(message);
        this.name = "ValidationError";
        this.field = field;
        this.timestamp = new Date();
    }
}

// Using the custom error
try {
    throw new ValidationError("无效输入", "username");
} catch (error) {
    if (error instanceof ValidationError) {
        console.error(`验证错误在字段 ${error.field}: ${error.message}`);
    }
}
```

##### 10. Standard library support

###### Global objects
- `console` - console output
- `Math` - math functions
- `JSON` - JSON handling
- `Date` - date and time
- `RegExp` - regular expressions

###### Array methods
- `map`, `filter`, `reduce`, `forEach`
- `find`, `findIndex`, `some`, `every`
- `sort`, `reverse`, `slice`, `splice`
- `concat`, `join`, `includes`, `indexOf`

###### String methods
- `charAt`, `substring`, `slice`
- `toUpperCase`, `toLowerCase`
- `trim`, `replace`, `split`
- `startsWith`, `endsWith`, `includes`
- `padStart`, `padEnd` (ES6)

###### Object methods
- `Object.keys`, `Object.values`, `Object.entries`
- `Object.assign`, `Object.create`
- `Object.freeze`, `Object.seal`
- `Object.getPrototypeOf`, `Object.setPrototypeOf`

##### 11. Compilation target

###### Structure of the generated VML code
```vml
.entry main
.stack 1048572

.data
hello_string: .string "Hello from JavaScript!"

.text
main:
    MOVE R0, hello_string
    SYSCALL #1    ; output the string
    SYSCALL #3    ; exit the program
```

###### Function calling convention
- Parameters are passed through registers R0-R3
- The return value is returned through R0
- The closure environment is passed on the stack
- The this context is passed through R12

##### 12. Limitations and notes

###### Current limitations
1. «bold»Unsupported features«/»:
   - The eval() function
   - with statements
   - Dynamic property names (partially supported)
   - Proxy objects
   - The Reflect API

2. «bold»Simplified implementation«/»:
   - The prototype chain is a simplified version
   - The closure implementation has limitations
   - Asynchronous operations are simulated
   - Garbage collection is managed by the VML runtime

3. «bold»Performance considerations«/»:
   - Static compilation, no JIT optimization
   - Object layout is fixed
   - No dynamic code execution

###### Best practices
1. Use const and let instead of var
2. Use arrow functions to preserve the this context
3. Avoid deeply nested callbacks
4. Use Promise and async/await to handle asynchronous work
5. Use modules sensibly to organize code

##### 13. Example programs

###### Hello World
```javascript
console.log("Hello, World!");
```

###### Computing the Fibonacci sequence
```javascript
function fibonacci(n) {
    if (n <= 1) return n;
    return fibonacci(n - 1) + fibonacci(n - 2);
}

console.log("斐波那契数列前10项:");
for (let i = 0; i < 10; i++) {
    console.log(`F(${i}) = ${fibonacci(i)}`);
}
```

###### Fetching data asynchronously
```javascript
async function fetchUserData(userId) {
    try {
        console.log(`开始获取用户 ${userId} 的数据...`);
        
        // simulate an asynchronous API call
        const user = await new Promise(resolve => {
            setTimeout(() => {
                resolve({
                    id: userId,
                    name: "Alice",
                    email: "alice@example.com"
                });
            }, 1000);
        });
        
        console.log("用户数据:", user);
        return user;
    } catch (error) {
        console.error("获取用户数据失败:", error);
        throw error;
    }
}

// Using the async function
(async () => {
    const user = await fetchUserData(123);
    console.log(`欢迎, ${user.name}!`);
})();
```

#### Floating point and 64-bit compilation modes

The VML toolchain controls the handling strategy for floating point and 64-bit integers through three compilation parameters:

| Parameter | Allowed values | Default | Description |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32-bit floating point handling mode |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64-bit floating point (Number) handling mode |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64-bit integer handling mode |

##### 32-bit floating point (float32)

In JavaScript every number is of type `Number` (IEEE 754 double precision). At VML compile time, 32-bit floating-point operations are handled in the following modes:

- «bold»`hard` mode (default)«/»: uses the native VML floating-point instructions `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`, operating directly through the sixteen floating-point registers F0-F15. Best performance; suitable for target platforms that support floating-point hardware.
- «bold»`soft` mode«/»: uses the Q15.16 fixed-point software emulation library `softfloat.c`, emulating floating-point operations through functions such as `__vml_float_add/sub/mul/div/neg/abs/cmp`. Suitable for MCU platforms without floating-point hardware.
- «bold»`none` mode«/»: disables all floating-point operations.

##### 64-bit floating point (double)

In JavaScript the `Number` type is natively IEEE 754 double-precision 64-bit floating point. At VML compile time it is handled in the following modes:

- «bold»`soft` mode (default)«/»: uses the IEEE 754 double-precision software emulation library `softdouble.c`, emulating through functions such as `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int` and `__vml_float2double/double2float`. Compatible with every platform (including MCU).
- «bold»`hard` mode«/»: uses the VML double-precision instructions `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`, operating through the eight double-precision registers D0-D7. Requires the target platform to support 64-bit operations.
- «bold»`none` mode«/»: disables all 64-bit floating-point operations.

##### 64-bit integers (int64)

JavaScript does not distinguish between integer and floating-point types; every number is a `Number`. The VML compilation layer reserves the `--int64` parameter for future BigInt extension support:

- «bold»`soft` mode (default)«/»: uses the two-register software emulation library `softint64.c`, emulating 64-bit integer operations through functions such as `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr`.
- «bold»`hard` mode«/»: reserved; a future VML version will support native 64-bit integer instructions.
- «bold»`none` mode«/»: disables the 64-bit integer extension.

##### Software emulation libraries

All of the software emulation libraries above live in the `Lib/shared/` directory, are written in C and are compiled to VML by the C compiler, and are shared by every language:

| Library file | Purpose | Core functions |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 fixed-point 32-bit floating-point emulation | `__vml_float_add/sub/mul/div/neg/abs/cmp`, `__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 double-precision 64-bit floating-point emulation | `__vml_double_add/sub/mul/div/neg/abs/cmp`, `__vml_int2double/double2int`, `__vml_float2double/double2float` |
| `softint64.c` | 64-bit integer two-register emulation | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

#### Compiler implementation status

##### Current completeness: ~94%
- ✅ Lexer (embedded in JavaScriptCompiler.cs, ~553 lines)
- ✅ Parser (Parser.cs, ~829 lines)
- ✅ AST node definitions (ASTNode.cs, ~395 lines)
- ✅ Code generator (CodeGenerator.cs, ~1204 lines)
- ⚠️ Functions/closures — basic support
- ⚠️ Objects — basic support
- ❌ Classes (ES6 class) — not implemented
- ❌ Arrow functions — not implemented
- ❌ Promise/async/await — not implemented
- ❌ Module system (import/export) — not implemented
- ❌ Standard library (Lib/javascript/) — to be created

##### Development roadmap
1. «bold»Stage 1«/»: basic syntax support (var, functions, control flow) — ✅ done
2. «bold»Stage 2«/»: object and array support — ⚠️ basically done
3. «bold»Stage 3«/»: class and module support — ❌ to be implemented
4. «bold»Stage 4«/»: asynchronous programming support — ❌ to be implemented
5. «bold»Stage 5«/»: performance optimization and standard library completion — ❌ to be implemented

#### Compiling and running

##### Using the JavaScript compiler
```bash
# Compile a JavaScript program
dotnet run --project VMLPrepares/JavaScriptCompiler -L Lib/javascript app.js -o output.vml

# Run the generated VML program
dotnet run --project VMLEmulators/FullDevicesEmulator output.vml
```

##### Using VMLTool (plugin architecture)
```bash
# Automatically recognize a JavaScript file
vmltool app.js -o output.vml

# Specify the JavaScript language
vmltool app.js -o output.vml --lang javascript
```

#### Related documents
- [VML toolchain architecture](ARCHITECTURE.md)
- [Compiler completeness dashboard](../../COMPLETION_DASHBOARD.md)
- [JavaScript standard library documentation](Lib/javascript/README.md)
- [Test case notes](Test/javascript/README.md)
---

#### 🆕 String types (v1.65.19)

This language compiler uses `.wstring` (UTF-16LE) as its internal string storage by default.

| Mode | Default encoding | VML pseudo-op | SYSCALL output |
|:-----|:--------|:----------|:------------|
| MCU (default) | `.string` (UTF-8) | `.string` | #1 |
| OS | `.wstring` (UTF-16LE) | `.wstring` | #391 |

«bold»Predefined macro«/»: `VML_WSTRING` — defined automatically in OS mode, undefined in MCU mode
«bold»Output function«/»: OS mode uses `shared_print_wstr` automatically (UTF-16LE to UTF-8 automatic conversion)

```c
// User code can test the encoding through the macro
#ifdef VML_WSTRING
  // the default string is wstring (UTF-16LE)
#else
  // the default string is UTF-8
#endif
```

## Compiler README

### JavaScript (ECMAScript 5) Compiler

«bold»Path«/»: `VMLPrepares/JavaScriptCompiler/`
«bold»Completeness«/»: ~94% | 🟢 Production ready
«bold»Standard library«/»: `Lib/javascript/` (to be created)

#### Features
- ✅ Full syntax analysis + code generation
- ✅ Control flow (if/while/for/switch)
- ✅ Function/procedure definitions and calls
- ✅ Standard library support
- ✅ POKE/PEEK memory operations (MMIO)
- ✅ Hexadecimal literals
- ✅ Shared built-in function library (builtins.vml)


#### Compilation modes

##### MCU mode (default `--mode mcu`)
MCU mode is optimized for microcontroller/bare-metal environments (Arduino/STM32/8051 etc.) and automatically skips features that are incompatible with an operating system.

«bold»Skipped«/» (no code is generated for these):
- async/await, Promise, eval

«bold»Retained«/» (implemented at the BIOS level):
- Closures, objects, arrays
- POKE/PEEK memory-mapped I/O (MMIO)
- Basic type arithmetic, control flow, function calls
- printf/puts map to UART

##### OS mode (`--mode os`, reserved)
OS mode targets environments that have an operating system (such as embedded Linux, RTOS). It will then support the full set of language features (file system, threads, async, exceptions, reflection, etc.).

##### RAM levels
- `--ram k`: kilobyte level (2 KB~64 KB, e.g. 8051/PIC/AVR)
- `--ram m`: megabyte level (64 KB~1 MB, e.g. ARM Cortex-M, «bold»default«/»)
- `--ram g`: gigabyte level (e.g. x86/DDR systems)
- `--stack-size <bytes>`: set the stack size manually (by default it is allocated automatically from --ram)

##### MCU-safe coding tips
- Limited stack space (256-4096 bytes typical), avoid deep recursion
- No deep recursion (beyond 10 levels, evaluate the stack)
- No dynamic loading (import/dofile/eval)
- Floating-point arithmetic may need a software floating-point library

#### Usage
```bash
dotnet run --project VMLPrepares/JavaScriptCompiler input.Js -o output.vml
```

#### Tests
`Test/Js/` — 0 test files (the test directory is to be created; use Js to avoid clashing with the Java Test/Jv)
