# Ladder

梯形图（PLC）风格的前端 —— 它«bold»做不了手机界面程序«/»。

这一路能用的是「编译与运行」本身，UI 那整套接口（开窗/绘图/输入）没有对应语法（没有「带字符串参数的函数调用」）。

## 在手机上怎么跑

```
vml run examples/ladder/file_io.ld
```

编译要等一会儿（C 那种要一两分钟，脚本类语言几秒）。程序跑起来后**屏幕底部就是手柄**，
方向键 + 四个动作键都在；点右上角返回可以回到命令行。

## 写法要点

- 只能写声明与简单逻辑，«bold»不能调 UI 接口«/»

## 示例

| 文件 | 演示什么 |
|---|---|
| `file_io.ld` | 空测试（NOP） |
| `sysinfo.ld` | 占位 —— 这一路调不了 JSON 接口 |

## 实测踩过的坑

- 想做手机界面程序，换一门语言（C / Python / Lua 都可以）。

---

下面的内容是**从 VML 源码里直接带的**（`third_party/vml/VMLPrepares/LadderCompiler/`）：
`README` 讲这个前端支持什么、怎么编；`语言规范` 讲语法本身。
上游一改，这里重新生成就是最新的。

## 语言规范

### 梯形图语言编译器规范说明

> «bold»版本«/»：v1.0 | «bold»日期«/»：2026-07-06 | «bold»修订者«/»：深圳市探索智能科技有限公司

#### 规范标准

| 字段 | 值 |
|:-----|:----|
| «bold»目标标准«/» | IEC 61131-3 子集 (2003) |
| «bold»发布年份«/» | 2003 |
| «bold»完成度«/» | ~98% |
| «bold»MCU完成度«/» | ~97% |
| «bold»测试«/» | 0 (测试目录待创建) |
| «bold»更新«/» | 2026-05-18: 更新定时器实现为 SYSCALL #53 实时时钟；修正代码生成器状态 |

#### 关键字（指令）

«bold»触点«/»: `NO`(常开) `NC`(常闭) `POS`(上升沿) `NEG`(下降沿)
«bold»线圈«/»: `OUT`(输出) `SET`(置位) `RST`(复位)
«bold»定时器«/»: `TON`(接通延时) `TOF`(断开延时) `TP`(脉冲)
«bold»计数器«/»: `CTU`(加计数) `CTD`(减计数) `CTUD`(加减计数)
«bold»比较«/»: `EQ` `NE` `GT` `GE` `LT` `LE`
«bold»算术«/»: `ADD` `SUB` `MUL` `DIV` `MOD`
«bold»程序控制«/»: `JMP` `LBL` `RET` `END`
«bold»其他«/»: `MOVE` `SEL` `MUX` `LIMIT`

#### 概述

本编译器实现 IEC 61131-3 标准的梯形图（Ladder Diagram）语言到 VML (Virtual Machine Language) 汇编的编译。梯形图是工业控制领域广泛使用的图形化编程语言，用于可编程逻辑控制器（PLC）编程。

#### 语言特性

##### 1. 梯形图基本元素

###### 触点（Contacts）
- «bold»常开触点（Normally Open）«/»: ─┤ ├─
- «bold»常闭触点（Normally Closed）«/»: ─┤/├─
- «bold»上升沿触点（Positive Edge）«/»: ─┤P├─
- «bold»下降沿触点（Negative Edge）«/»: ─┤N├─

###### 线圈（Coils）
- «bold»输出线圈（Output Coil）«/»: ─( )─
- «bold»置位线圈（Set Coil）«/»: ─(S)─
- «bold»复位线圈（Reset Coil）«/»: ─(R)─
- «bold»保持线圈（Latch Coil）«/»: ─(L)─

###### 功能块（Function Blocks）
- «bold»定时器«/»: TON（接通延时）, TOF（断开延时）, TP（脉冲）
- «bold»计数器«/»: CTU（加计数器）, CTD（减计数器）, CTUD（加减计数器）
- «bold»比较器«/»: 等于、不等于、大于、小于等
- «bold»数学运算«/»: 加、减、乘、除等

##### 2. 数据类型（IEC 61131-3）

###### 基本数据类型
- «bold»BOOL«/»: 布尔值（1位）
- «bold»BYTE«/»: 无符号8位整数
- «bold»WORD«/»: 无符号16位整数
- «bold»DWORD«/»: 无符号32位整数
- «bold»INT«/»: 有符号16位整数
- «bold»DINT«/»: 有符号32位整数
- «bold»REAL«/»: 32位浮点数
- «bold»STRING«/»: 字符串
- «bold»TIME«/»: 时间类型

###### 派生数据类型
- «bold»数组«/»: `ARRAY[1..10] OF INT`
- «bold»结构体«/»: 
```iec
TYPE MotorControl :
STRUCT
    StartButton : BOOL;
    StopButton : BOOL;
    MotorRun : BOOL;
    Speed : INT;
END_STRUCT
END_TYPE
```

##### 3. 程序结构

###### 程序声明
```iec
PROGRAM MotorControl
VAR
    StartButton AT %IX0.0 : BOOL;
    StopButton AT %IX0.1 : BOOL;
    MotorRun AT %QX0.0 : BOOL;
    Timer1 : TON;
END_VAR
```

###### 变量声明区域
- «bold»VAR«/»: 局部变量
- «bold»VAR_INPUT«/»: 输入变量
- «bold»VAR_OUTPUT«/»: 输出变量
- «bold»VAR_IN_OUT«/»: 输入输出变量
- «bold»VAR_GLOBAL«/»: 全局变量
- «bold»VAR_TEMP«/»: 临时变量

###### I/O 映射
- «bold»输入«/»: `%IX0.0`（字节0，位0）
- «bold»输出«/»: `%QX0.0`（字节0，位0）
- «bold»内存«/»: `%MW0`（字0）
- «bold»保持寄存器«/»: `%MD0`（双字0）

##### 4. 梯形图梯级结构

###### 简单梯级
```
     Start    Stop     Motor
     ─┤ ├─────┤/├──────( )─
       I0.0    I0.1      Q0.0
```

###### 并联分支
```
     Button1     Lamp1
     ─┤ ├────┬───( )─
            │
     Button2│
     ─┤ ├────┘
```

###### 串联分支
```
     Start     Timer1.PT := T#5s
     ─┤ ├──────[TON]─────
       I0.0     Timer1
                IN  Q
                PT  ET
```

###### 功能块调用
```
     Start     MotorCtrl
     ─┤ ├──────[FB]─────
                EN  ENO
               Start Run
               Stop  Fault
               Speed ActualSpeed
```

##### 5. 定时器和计数器

###### 定时器（TON - 接通延时）
```iec
VAR
    Timer1 : TON;
    TimeValue : TIME := T#5s;
END_VAR

// 梯形图表示
     Start     Timer1
     ─┤ ├──────[TON]─────
                IN  Q
               PT  ET
```

«bold»功能«/»: 当IN为TRUE时开始计时，经过PT时间后Q变为TRUE。ET显示已过时间。

###### 计数器（CTU - 加计数器）
```iec
VAR
    Counter1 : CTU;
    PresetValue : INT := 10;
END_VAR

// 梯形图表示
     Pulse     Counter1
     ─┤P├──────[CTU]─────
                CU  Q
               PV  CV
                R
```

«bold»功能«/»: 在CU上升沿计数，当CV >= PV时Q为TRUE，R信号复位计数器。

##### 6. 指令列表（IL）支持

梯形图编译器也支持指令列表文本格式：

```iec
LD StartButton
ANDN StopButton
OUT MotorRun

LD MotorRun
TON Timer1, T#5s
```

##### 7. 结构化文本（ST）支持

在梯形图中可以嵌入结构化文本：

```iec
IF StartButton AND NOT StopButton THEN
    MotorRun := TRUE;
    Timer1(IN:=TRUE, PT:=T#5s);
END_IF;
```

##### 8. 标准函数

###### 位操作函数
- «bold»AND«/», «bold»OR«/», «bold»XOR«/», «bold»NOT«/» - 逻辑运算
- «bold»SHL«/», «bold»SHR«/» - 移位运算
- «bold»ROL«/», «bold»ROR«/» - 循环移位

###### 数学函数
- «bold»ADD«/», «bold»SUB«/», «bold»MUL«/», «bold»DIV«/» - 算术运算
- «bold»MOD«/» - 取模
- «bold»ABS«/» - 绝对值
- «bold»SQRT«/» - 平方根
- «bold»LN«/», «bold»EXP«/» - 对数和指数
- «bold»SIN«/», «bold»COS«/», «bold»TAN«/» - 三角函数

###### 比较函数
- «bold»EQ«/» (=), «bold»NE«/» (<>), «bold»GT«/» (>), «bold»GE«/» (>=), «bold»LT«/» (<), «bold»LE«/» (<=)

###### 类型转换函数
- «bold»BOOL_TO_INT«/», «bold»INT_TO_REAL«/» 等类型转换
- «bold»TRUNC«/», «bold»ROUND«/» - 取整函数

###### 字符串函数
- «bold»CONCAT«/» - 字符串连接
- «bold»LEFT«/», «bold»RIGHT«/», «bold»MID«/» - 子字符串
- «bold»LEN«/» - 字符串长度
- «bold»FIND«/» - 查找子串

##### 9. VML 代码生成约定

###### 位操作映射
```
梯形图触点: ─┤ ├─
VML代码:    MOVE R0, [I0.0_address]
            CMP R0, #1
            JNE skip_branch

梯形图线圈: ─( )─
VML代码:    MOVE R0, #1
            MOVE [Q0.0_address], R0
```

###### 定时器实现（使用 SYSCALL #53 GetTick 实时时钟）

TON (接通延时定时器):
- 检测 IN 输入上升沿时记录 startTime = SYSCALL #53 (GetTick)
- ET = GetTick() - startTime
- 当 ET >= PT 时 Q 输出置为 TRUE
- IN 为 FALSE 时复位 ET = 0, Q = FALSE

TOF (断开延时定时器):
- 检测 IN 输入下降沿时记录 startTime
- ET = GetTick() - startTime
- IN 恢复 TRUE 时复位

TP (脉冲定时器):
- 检测 IN 输入上升沿触发脉冲
- ET = GetTick() - startTime
- 脉冲期间 Q 输出保持 TRUE

CTUD (加减计数器):
- CU 上升沿且 CV < 32767 时 CV++
- CD 上升沿且 CV > -32768 时 CV--
- R 复位 CV = 0, LD 加载 CV = PV
- QU: CV >= PV 时为 TRUE; QD: CV <= 0 时为 TRUE

###### 计数器实现（含溢出保护）

CTU (加计数器):
- 检测 CU 上升沿时计数
- CV 递增前检查 CV < 32767（防止上溢）
- CV >= PV 时 Q = TRUE
- R 信号复位 CV = 0

CTD (减计数器):
- 检测 CD 上升沿时计数
- CV 递减前检查 CV > -32768（防止下溢）
- CV <= 0 时 Q = TRUE
- LD 信号加载 CV = PV

CTUD (加减计数器):
- CU 上升沿且 CV < 32767 时 CV++
- CD 上升沿且 CV > -32768 时 CV--
- R 复位 CV = 0, LD 加载 CV = PV
- QU: CV >= PV 时为 TRUE; QD: CV <= 0 时为 TRUE

##### 10. I/O 内存映射

###### 输入映像区
```
地址范围: 0x8000 - 0x80FF (256字节)
用途: 存储数字输入状态
访问: MOVE R0, [0x8000]  ; 读取第一个输入字节
```

###### 输出映像区
```
地址范围: 0x8100 - 0x81FF (256字节)
用途: 存储数字输出状态
访问: MOVE [0x8100], R0  ; 写入第一个输出字节
```

###### 保持寄存器区
```
地址范围: 0x8200 - 0x83FF (512字节)
用途: 存储中间变量、定时器、计数器等
```

###### 特殊寄存器
```
地址: 0x8400 - 系统时间（毫秒）
地址: 0x8404 - 扫描周期
地址: 0x8408 - 错误代码
```

##### 11. 扫描周期模型

梯形图程序按照扫描周期执行：

```
开始扫描
├── 读取物理输入到输入映像区
├── 执行用户程序（梯形图逻辑）
├── 写入输出映像区到物理输出
└── 处理系统任务（通信、诊断等）
```

VML实现：
```vml
LABEL main_scan_cycle
    ; 1. 读取输入
    CALL read_physical_inputs
    
    ; 2. 执行用户程序
    CALL user_program
    
    ; 3. 写入输出
    CALL write_physical_outputs
    
    ; 4. 更新系统时间
    CALL update_system_timer
    
    ; 5. 检查扫描周期时间
    MOVE R0, [current_time]
    SUB R0, [scan_start_time]
    CMP R0, [max_scan_time]
    JLE scan_ok
    
    ; 扫描超时错误
    MOVE R0, #1
    MOVE [scan_timeout_error], R0
    
scan_ok:
    ; 等待下一个扫描周期
    CALL delay_scan_cycle
    JMP main_scan_cycle
```

##### 12. 示例程序

###### 电机启停控制
```iec
PROGRAM MotorControl
VAR
    StartButton AT %IX0.0 : BOOL;
    StopButton AT %IX0.1 : BOOL;
    MotorRun AT %QX0.0 : BOOL;
    Overload AT %IX0.2 : BOOL;
    Timer1 : TON;
END_VAR

// 梯形图逻辑
     Start    Stop    Overload   Motor
     ─┤ ├─────┤/├─────┤/├──────( )─
       I0.0    I0.1    I0.2      Q0.0
     
     Motor     Timer1
     ─┤ ├──────[TON]─────
                IN  Q
               PT  ET
              T#5s
     
     Timer1.Q    Alarm
     ─┤ ├────────( )─
                     Q0.1
```

###### 流水线控制
```iec
PROGRAM ConveyorControl
VAR
    Sensor1 AT %IX0.0 : BOOL;
    Sensor2 AT %IX0.1 : BOOL;
    Motor1 AT %QX0.0 : BOOL;
    Motor2 AT %QX0.1 : BOOL;
    Counter1 : CTU;
    PartsCount : INT;
END_VAR

// 产品计数
     Sensor1    Counter1
     ─┤P├──────[CTU]─────
                CU  Q
               PV  CV
               10
                R
     
     Counter1.Q   PartsCount := Counter1.CV
     ─┤ ├─────────────────[MOV]─────
     
// 流水线控制
     Sensor1    Sensor2    Motor1
     ─┤ ├───────┤/├────────( )─
     
     Sensor2           Motor2
     ─┤ ├──────────────( )─
```

##### 浮点与64位编译模式

VML 工具链通过三个编译参数控制浮点和 64 位整数的处理策略：

| 参数 | 可选值 | 默认值 | 说明 |
|------|--------|:------:|------|
| `--float32` | `hard` / `soft` / `none` | `hard` | 32位浮点 (REAL) 处理模式 |
| `--float64` | `hard` / `soft` / `none` | `soft` | 64位浮点处理模式 |
| `--int64` | `hard` / `soft` / `none` | `soft` | 64位整数处理模式 |

###### 32位浮点 (float32)

本语言中的 `REAL` 类型（32位单精度浮点）按以下模式编译：

- «bold»`hard` 模式（默认）«/»: 使用 VML 原生浮点指令 `MOVEF`/`FADD`/`FSUB`/`FMUL`/`FDIV`/`FCMP`/`FNEG`，通过 F0-F15 十六个浮点寄存器直接运算。性能最佳，适合支持浮点硬件的目标平台。
- «bold»`soft` 模式«/»: 使用 Q15.16 定点数软件模拟库 `softfloat.c`，通过 `__vml_float_add/sub/mul/div/neg/abs/cmp` 等函数模拟浮点运算。适合无浮点硬件的 MCU 平台。
- «bold»`none` 模式«/»: 禁用所有 32 位浮点类型，遇到 `REAL` 声明时报告编译错误。

###### 64位浮点 (double)

VML 编译层支持 64 位双精度浮点运算。梯形图语言本身未定义 `LREAL` 对应类型，但编译器预留扩展支持。

- «bold»`soft` 模式（默认）«/»: 使用 IEEE 754 双精度软件模拟库 `softdouble.c`。
- «bold»`hard` 模式«/»: 使用 VML 双精度指令 `MOVED`/`DADD`/`DSUB`/`DMUL`/`DDIV`/`DCMP`/`DNEG`。
- «bold»`none` 模式«/»: 禁用双精度浮点扩展。

###### 64位整数 (int64)

梯形图语言 IEC 61131-3 中 `DLONG` / `LINT` 等 64 位整数类型按以下模式编译：

- «bold»`soft` 模式（默认）«/»: 使用双寄存器软件模拟库 `softint64.c`，通过 `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` 等函数模拟 64 位整数运算。
- «bold»`hard` 模式«/»: 预留，未来 VML 版本将支持原生 64 位整数指令。
- «bold»`none` 模式«/»: 禁用 64 位整数类型。

###### 软件模拟库

以上软件模拟库均位于 `Lib/shared/` 目录，使用 C 语言编写并由 C 编译器编译为 VML，所有语言共享：

| 库文件 | 用途 | 核心函数 |
|:-------|:-----|:---------|
| `softfloat.c` | Q15.16 定点数 32 位浮点模拟 | `__vml_float_add/sub/mul/div/neg/abs/cmp`、`__vml_int2float/float2int` |
| `softdouble.c` | IEEE 754 双精度 64 位浮点模拟 | `__vml_double_add/sub/mul/div/neg/abs/cmp`、`__vml_int2double/double2int`、`__vml_float2double/double2float` |
| `softint64.c` | 64 位整数双寄存器模拟 | `__vml_i64_add/sub/neg/and/or/xor/not/shl/shr` |

##### 13. 编译限制和注意事项

###### 当前实现状态
- «bold»词法分析器«/»: 完整实现，支持IEC 61131-3关键字
- «bold»语法分析器«/»: 完整实现，支持梯形图和结构化文本
- «bold»代码生成器«/»: 基本实现 (CodeGenerator.Elements.cs)，支持触点/线圈/定时器/计数器
- «bold»定时器«/»: TON/TOF/TP 使用 SYSCALL #53 (GetTick) 实时时钟
- «bold»计数器«/»: CTU/CTD/CTUD 含溢出保护
- «bold»标准库«/»: 需要创建梯形图标准库（stdlib.vml）

###### 待实现功能
1. 梯形图到VML指令的代码生成 — ⚠️ 基本完成
2. 定时器、计数器功能块 — ✅ 已实现 (TON/TOF/TP/CTU/CTD/CTUD)
3. I/O内存映射管理
4. 扫描周期调度
5. 标准函数库实现 (MOVE/SEL/MUX/LIMIT)

###### 技术挑战
1. «bold»图形到文本转换«/»: 梯形图是图形化语言，使用文本指令列表作为中间表示
2. «bold»实时性要求«/»: PLC 程序有严格的实时性要求 — 定时器使用 SYSCALL #53 毫秒时钟
3. «bold»位操作«/»: 触点/线圈需要高效位操作和边沿检测
4. «bold»保持性变量«/»: 需要保持断电后的变量值

##### 14. 与VML运行时集成

梯形图程序通过以下方式与VML运行时交互：

- «bold»I/O访问«/»: 通过内存映射I/O地址访问物理设备
- «bold»定时器服务«/»: 使用系统定时器实现TON/TOF/TP
- «bold»中断处理«/»: 支持硬件中断事件
- «bold»诊断功能«/»: 扫描周期监控、错误处理

###### 系统调用
- «bold»SYSCALL #3«/»: 程序退出
- «bold»SYSCALL #53«/»: GetTick — 获取毫秒时间戳 (定时器计时基准)
- «bold»I/O 访问«/»: 通过内存映射 I/O 地址访问物理设备 (触点/线圈)

##### 15. 应用领域

梯形图编译器主要应用于：
- 工业自动化控制系统
- 楼宇自动化
- 过程控制
- 机械设备控制
- 能源管理系统

通过将梯形图编译为VML代码，可以在VML虚拟机上运行传统的PLC程序，实现工业控制程序的跨平台移植和仿真测试。
---

#### 🆕 字符串编码 (v1.65.19)

该语言编译器通过共享库 (Lib/shared/) 间接使用 VML 字符串体系。

| 伪指令 | 宽度 | 编码 | C 类型 |
|:------|:----:|:-----|:--------|
| `.string` | 8-bit | UTF-8 | `char*` |
| `.wstring` | 16-bit | UTF-16LE | `wchar_t*` |
| `.ustring` | 32-bit | UTF-32LE | `char32_t*` |

«bold»MCU 模式«/» (默认): 字符串输出为 UTF-8 (`.string`)
«bold»OS 模式«/»: 可通过 `VML_WSTRING` 宏判断编码

共享库已提供宽字符串转换函数 (wchar.h/uchar.h)，各语言编译器可按需使用。

## 编译器 README

### 梯形图 (IEC 61131-3 子集) 编译器

«bold»路径«/»: `VMLPrepares/LadderCompiler/`
«bold»完成度«/»: ~98% | 🟢 生产可用
«bold»标准库«/»: `Lib/ladder/`

#### 功能
- ✅ 语法分析 + 代码生成（Lexer/Parser/CodeGenerator）
- ✅ 触点: NO(常开)/NC(常闭)/POS(上升沿)/NEG(下降沿)
- ✅ 线圈: OUT(输出)/SET(置位)/RST(复位)
- ✅ 定时器: TON(接通延时)/TOF(断开延时)/TP(脉冲) — SYSCALL #53 实时时钟
- ✅ 计数器: CTU(加)/CTD(减)/CTUD(加减) — 含溢出保护
- ✅ 比较: EQ/NE/GT/GE/LT/LE
- ✅ 算术: ADD/SUB/MUL/DIV/MOD
- ✅ POKE/PEEK 内存操作 (MMIO)
- ✅ 共享内置函数库 (builtins.vml)


#### 编译模式

##### MCU 模式（默认 `--mode mcu`）
MCU 模式针对单片机/裸机环境（Arduino/STM32/8051 等）优化，自动跳过不兼容操作系统的特性。

«bold»跳过«/»（遇到这些语法不生成代码）:
- 无（PLC 语言天然 MCU 兼容）

«bold»保留«/»（由 BIOS 实现底层）:
- 定时器 (TON/TOF/TP) — 使用 SYSCALL #53 GetTick
- 计数器 (CTU/CTD/CTUD) — 16位溢出保护
- POKE/PEEK 内存映射 I/O (MMIO)
- I/O 内存映射 (触点/线圈)
- 基本算术/比较/控制流

##### OS 模式（`--mode os`，预留）
OS 模式针对带操作系统环境（如 Linux 嵌入式、RTOS 等），届时支持全部语言特性（文件系统、多线程、异步、异常、反射等）。

##### RAM 级别
- `--ram k`：KB级别（2KB~64KB，如 8051/PIC/AVR）
- `--ram m`：MB级别（64KB~1MB，如 ARM Cortex-M，«bold»默认«/»）
- `--ram g`：GB级别（如 x86/DDR 系统）
- `--stack-size <bytes>`：手动指定栈大小（默认自动根据 --ram 分配）

##### MCU 安全编码提示
- PLC 扫描周期需满足实时性要求
- 定时器精度取决于 SYSCALL #53 时钟分辨率
- 计数器注意 16 位范围 (-32768 ~ 32767)
- 浮点运算可能需软浮点库

#### 使用
```bash
dotnet run --project VMLTool -- input.lad -o output.vml
```

#### 测试
`Test/La/` — 0 测试文件（测试目录待创建）
