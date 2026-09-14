# JavaScript 语言编译器规范说明

> **版本**：v1.0 | **日期**：2026-07-06 | **修订者**：深圳市探索智能科技有限公司

## 规范标准

| 字段 | 值 |
|:-----|:----|
| **目标标准** | ECMAScript 5.1 子集 (2011) |
| **发布年份** | 2011 |
| **完成度** | ~94% |
| **MCU完成度** | ~92% |
| **测试** | 0 (测试目录待创建) |
| **更新** | 2026-05-18: 修正完成度和实现状态描述 |

## 关键字

`break` `case` `catch` `continue` `debugger` `default` `delete` `do` `else` `finally`
`for` `function` `if` `in` `instanceof` `new` `return` `switch` `this` `throw`
`try` `typeof` `var` `void` `while` `with` `class` `const` `export` `import`
`let` `super` `yield` `true` `false` `null` `undefined` `NaN` `Infinity`

## 概述

本编译器支持 JavaScript (ECMAScript 5) 语言的子集，将 JavaScript 源代码编译为 VML (Virtual Machine Language) 汇编代码。编译器采用静态编译版架构，支持 JavaScript 核心特性，包括函数、对象、数组和异步编程。

## 支持的语言特性

### 1. 数据类型

#### 原始类型
- **数字**: `Number` - 双精度浮点数，如 `42`, `3.14`, `0xFF`
- **字符串**: `String` - Unicode 字符串，如 `"hello"`, `'world'`
- **布尔值**: `Boolean` - `true` 或 `false`
- **空值**: `null` - 空对象引用
- **未定义**: `undefined` - 未定义的值
- **符号**: `Symbol` (ES6) - 唯一标识符

#### 对象类型
- **对象**: `Object` - 键值对集合
- **数组**: `Array` - 有序集合
- **函数**: `Function` - 可执行代码块
- **日期**: `Date` - 日期时间
- **正则表达式**: `RegExp` - 正则模式

### 2. 变量声明

#### 声明方式
```javascript
// var (函数作用域)
var x = 10;
var y, z = 20;

// let (块级作用域，ES6)
let counter = 0;
for (let i = 0; i < 10; i++) {
    console.log(i);
}

// const (常量，ES6)
const PI = 3.14159;
const MAX_SIZE = 100;

// 解构赋值 (ES6)
const [a, b] = [1, 2];
const {name, age} = {name: "Alice", age: 30};
```

#### 作用域
```javascript
// 全局作用域
var globalVar = "global";

function testScope() {
    // 函数作用域
    var functionVar = "function";
    
    if (true) {
        // 块级作用域 (let/const)
        let blockVar = "block";
        console.log(blockVar); // 可访问
    }
    // console.log(blockVar); // 错误：blockVar未定义
}
```

### 3. 函数

#### 函数定义
```javascript
// 函数声明
function add(a, b) {
    return a + b;
}

// 函数表达式
const multiply = function(x, y) {
    return x * y;
};

// 箭头函数 (ES6)
const divide = (x, y) => x / y;
const square = x => x * x;

// 立即执行函数表达式 (IIFE)
(function() {
    console.log("立即执行");
})();

// 生成器函数 (ES6)
function* generator() {
    yield 1;
    yield 2;
    yield 3;
}
```

#### 参数处理
```javascript
// 默认参数 (ES6)
function greet(name = "Guest") {
    console.log(`Hello, ${name}`);
}

// 剩余参数 (ES6)
function sum(...numbers) {
    return numbers.reduce((total, num) => total + num, 0);
}

// 参数解构 (ES6)
function printUser({name, age, city = "Unknown"}) {
    console.log(`${name}, ${age}岁, 来自${city}`);
}
```

### 4. 对象和类

#### 对象字面量
```javascript
// 对象创建
const person = {
    name: "Alice",
    age: 30,
    greet() {
        console.log(`Hello, I'm ${this.name}`);
    },
    // 计算属性名 (ES6)
    ["id_" + Date.now()]: "unique-id"
};

// 属性访问
console.log(person.name);      // 点表示法
console.log(person["age"]);    // 括号表示法

// 对象扩展 (ES6)
const extended = {
    ...person,
    city: "Beijing",
    job: "Developer"
};
```

#### 类定义 (ES6)
```javascript
// 类声明
class Animal {
    constructor(name) {
        this.name = name;
    }
    
    speak() {
        console.log(`${this.name} makes a sound`);
    }
    
    // 静态方法
    static isAnimal(obj) {
        return obj instanceof Animal;
    }
}

// 继承
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

// 使用类
const dog = new Dog("Buddy", "Golden Retriever");
dog.speak();
console.log(dog.description);
```

### 5. 数组和集合

#### 数组操作
```javascript
// 数组创建
const numbers = [1, 2, 3, 4, 5];
const empty = new Array(10);
const fromString = Array.from("hello");
const ofArray = Array.of(1, 2, 3);

// 数组方法
numbers.push(6);           // 添加元素
numbers.pop();             // 移除最后一个
numbers.unshift(0);        // 添加开头
numbers.shift();           // 移除开头

// 高阶函数
const doubled = numbers.map(x => x * 2);
const evens = numbers.filter(x => x % 2 === 0);
const sum = numbers.reduce((total, x) => total + x, 0);
numbers.forEach(x => console.log(x));

// 数组解构
const [first, second, ...rest] = numbers;
```

#### Set 和 Map (ES6)
```javascript
// Set - 唯一值集合
const set = new Set([1, 2, 3, 3, 4]); // {1, 2, 3, 4}
set.add(5);
set.delete(2);
console.log(set.has(3)); // true
set.forEach(value => console.log(value));

// Map - 键值对集合
const map = new Map();
map.set("name", "Alice");
map.set("age", 30);
console.log(map.get("name")); // "Alice"
console.log(map.size); // 2

// WeakSet 和 WeakMap
const weakSet = new WeakSet();
const weakMap = new WeakMap();
```

### 6. 控制流

#### 条件语句
```javascript
// if-else
if (condition) {
    // true分支
} else if (anotherCondition) {
    // else-if分支
} else {
    // false分支
}

// 三元运算符
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

#### 循环语句
```javascript
// for循环
for (let i = 0; i < 10; i++) {
    console.log(i);
}

// for...of (ES6)
for (const item of array) {
    console.log(item);
}

// for...in (遍历对象属性)
for (const key in object) {
    console.log(key, object[key]);
}

// while循环
while (condition) {
    // 循环体
}

// do...while循环
do {
    // 循环体至少执行一次
} while (condition);
```

### 7. 异步编程

#### Promise
```javascript
// Promise创建
const promise = new Promise((resolve, reject) => {
    setTimeout(() => {
        resolve("成功");
        // 或 reject(new Error("失败"));
    }, 1000);
});

// Promise使用
promise
    .then(result => {
        console.log(result);
        return result.toUpperCase();
    })
    .then(upper => console.log(upper))
    .catch(error => console.error(error))
    .finally(() => console.log("完成"));
```

#### async/await (ES2017)
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

// 立即执行async函数
(async () => {
    const result = await fetchData();
    console.log("结果:", result);
})();
```

### 8. 模块系统

#### ES6模块
```javascript
// math.js - 导出模块
export const PI = 3.14159;

export function add(a, b) {
    return a + b;
}

export default class Calculator {
    multiply(x, y) {
        return x * y;
    }
}

// app.js - 导入模块
import Calculator, { PI, add } from './math.js';
import * as math from './math.js';

const calc = new Calculator();
console.log(calc.multiply(2, 3));
console.log(add(PI, 1));
```

#### CommonJS模块 (Node.js风格)
```javascript
// 导出
module.exports = {
    add: function(a, b) { return a + b; },
    subtract: function(a, b) { return a - b; }
};

// 或
exports.multiply = function(a, b) { return a * b; };

// 导入
const math = require('./math.js');
console.log(math.add(2, 3));
```

### 9. 错误处理

```javascript
// try-catch-finally
try {
    // 可能抛出错误的代码
    riskyOperation();
    throw new Error("自定义错误");
} catch (error) {
    // 处理错误
    console.error("捕获错误:", error.message);
    console.error("堆栈:", error.stack);
} finally {
    // 清理代码，总是执行
    cleanup();
}

// 自定义错误类型
class ValidationError extends Error {
    constructor(message, field) {
        super(message);
        this.name = "ValidationError";
        this.field = field;
        this.timestamp = new Date();
    }
}

// 使用自定义错误
try {
    throw new ValidationError("无效输入", "username");
} catch (error) {
    if (error instanceof ValidationError) {
        console.error(`验证错误在字段 ${error.field}: ${error.message}`);
    }
}
```

### 10. 标准库支持

#### 全局对象
- `console` - 控制台输出
- `Math` - 数学函数
- `JSON` - JSON处理
- `Date` - 日期时间
- `RegExp` - 正则表达式

#### 数组方法
- `map`, `filter`, `reduce`, `forEach`
- `find`, `findIndex`, `some`, `every`
- `sort`, `reverse`, `slice`, `splice`
- `concat`, `join`, `includes`, `indexOf`

#### 字符串方法
- `charAt`, `substring`, `slice`
- `toUpperCase`, `toLowerCase`
- `trim`, `replace`, `split`
- `startsWith`, `endsWith`, `includes`
- `padStart`, `padEnd` (ES6)

#### 对象方法
- `Object.keys`, `Object.values`, `Object.entries`
- `Object.assign`, `Object.create`
- `Object.freeze`, `Object.seal`
- `Object.getPrototypeOf`, `Object.setPrototypeOf`

### 11. 编译目标

#### 生成的VML代码结构
```vml
.entry main
.stack 1048572

.data
hello_string: .string "Hello from JavaScript!"

.text
main:
    MOVE R0, hello_string
    SYSCALL #1    ; 输出字符串
    SYSCALL #3    ; 退出程序
```

#### 函数调用约定
- 参数通过寄存器 R0-R3 传递
- 返回值通过 R0 返回
- 闭包环境通过栈传递
- this 上下文通过 R12 传递

### 12. 限制和注意事项

#### 当前限制
1. **不支持的特性**:
   - eval() 函数
   - with 语句
   - 动态属性名（部分支持）
   - Proxy 对象
   - Reflect API

2. **简化实现**:
   - 原型链为简化版本
   - 闭包实现有限制
   - 异步操作为模拟实现
   - 垃圾回收由VML运行时管理

3. **性能考虑**:
   - 静态编译，无JIT优化
   - 对象布局固定
   - 无动态代码执行

#### 最佳实践
1. 使用 const 和 let 替代 var
2. 使用箭头函数保持 this 上下文
3. 避免深度嵌套的回调
4. 使用 Promise 和 async/await 处理异步
5. 合理使用模块化组织代码

### 13. 示例程序

#### Hello World
```javascript
console.log("Hello, World!");
```

#### 计算斐波那契数列
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

#### 异步数据获取
```javascript
async function fetchUserData(userId) {
    try {
        console.log(`开始获取用户 ${userId} 的数据...`);
        
        // 模拟异步API调用
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

// 使用async函数
(async () => {
    const user = await fetchUserData(123);
    console.log(`欢迎, ${user.name}!`);
})();
```

## 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点 (Number) 处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数处理模式 |

### 32位浮点 (float32)

JavaScript 中所有数字为 `Number` 类型（IEEE 754 双精度），VML 编译时按以下模式处理 32 位浮点运算：

- **`hard` 模式（默认）**: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。性能最佳，适合支持浮点硬件的目标平台。
- **`soft` 模式**: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。适合无浮点硬件的 MCU 平台。
- **`none` 模式**: 禁用所有浮点运算。

### 64位浮点 (double)

JavaScript 的 `Number` 类型原生为 IEEE 754 双精度 64 位浮点，VML 编译时按以下模式处理：

- **`soft` 模式（默认）**: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`，通过 `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` 等函数模拟。兼容所有平台（含 MCU）。
- **`hard` 模式**: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`，通过 D0-D7 八个双精度寄存器运算。需要目标平台支持 64 位运算。
- **`none` 模式**: 禁用所有 64 位浮点运算。

### 64位整数 (int64)

JavaScript 不区分整数与浮点类型，所有数字统一为 `Number`。VML 编译层预留 `--int64` 参数用于未来 BigInt 扩展支持：

- **`soft` 模式（默认）**: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- **`hard` 模式**: 预留，未来 VML 版本将支持原生 64 位整数指令。
- **`none` 模式**: 禁用 64 位整数扩展。

### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

## 编译器实现状态

### 当前完成度: ~94%
- ✅ 词法分析器 (内嵌于 JavaScriptCompiler.cs, ~553行)
- ✅ 语法分析器 (Parser.cs, ~829行)
- ✅ AST节点定义 (ASTNode.cs, ~395行)
- ✅ 代码生成器 (CodeGenerator.cs, ~1204行)
- ⚠️ 函数/闭包 — 基本支持
- ⚠️ 对象 — 基本支持
- ❌ 类 (ES6 class) — 未实现
- ❌ 箭头函数 — 未实现
- ❌ Promise/async/await — 未实现
- ❌ 模块系统 (import/export) — 未实现
- ❌ 标准库 (Lib/javascript/) — 待创建

### 开发路线图
1. **阶段1**: 基础语法支持 (var、函数、控制流) — ✅ 完成
2. **阶段2**: 对象和数组支持 — ⚠️ 基本完成
3. **阶段3**: 类和模块支持 — ❌ 待实现
4. **阶段4**: 异步编程支持 — ❌ 待实现
5. **阶段5**: 性能优化和标准库完善 — ❌ 待实现

## 编译和运行

### 使用JavaScript编译器
```bash
# 编译JavaScript程序
dotnet run --project VMLPrepares/JavaScriptCompiler -L Lib/javascript app.js -o output.vml

# 运行生成的VML程序
dotnet run --project VMLEmulators/FullDevicesEmulator output.vml
```

### 使用VMLTool (插件架构)
```bash
# 自动识别JavaScript文件
vmltool app.js -o output.vml

# 指定JavaScript语言
vmltool app.js -o output.vml --lang javascript
```

## 相关文档
- [VML工具链架构](ARCHITECTURE.md)
- [编译器完成度仪表板](../../COMPLETION_DASHBOARD.md)
- [JavaScript标准库文档](Lib/javascript/README.md)
- [测试用例说明](Test/javascript/README.md)
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
