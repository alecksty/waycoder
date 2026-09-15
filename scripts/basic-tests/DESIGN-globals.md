# BASIC 全局变量区 —— 设计与实施清单

> 起因：`tetris.bas` 跑不起来，一路查到 BASIC 前端**没有任何可用的跨帧变量机制**。
> 语料（本目录 `run.sh`）把现状钉死了：`t4`（SUB 读模块级变量）与 `t7`（`DIM SHARED` 跨 SUB）
> 都读出 **0**。本文是"真要修该怎么修"的设计与逐点清单。

## 一、现状（都已实测）

| 事实 | 证据 |
|---|---|
| 模块级变量按 **`R12 + 8 + byteOffset`** 寻址 | `CodeGenerator.VarMemRef()` 的 `return $"R12+{8 + offset}"` |
| 进 SUB 后 R12 变成**子帧** | 所以同一个偏移读到的是子帧里的别的东西 → `t4` 读出 0 |
| `DIM SHARED` 走 **`StaticBase + 8 + offset`** | 而 `StaticBase` 只是 `get => STATIC_BASE_ADDR`（常量 `0x6FD4`）——它**不是动态基址**，算出来是个指向"存基址那个槽"的裸数字地址 |
| 静态区是**动态分配**的 | `AddRI(MOVE, 0, STATIC_TOTAL_SIZE); EmitAlloc(); EmitStaticBase(1)`，基址存在 `0x6FD4` |
| 取静态区地址的正确姿势已有现成实现 | `EmitStaticAddr(reg, offset)` = `MOVE reg,#0x6FD4; MOVE reg,[reg]; ADD reg,#offset` |

**结论**：`_sharedVariables` 那条路从来没成立过 —— 不能照它改。

## 二、设计

**给全局变量划一块独立的静态区段，读写一律经 `EmitStaticAddr` 间接寻址。**

1. **区段**：`STATIC_GLOBALS_OFFSET = 0x5000`（**追加在现有分区之后**，不动 0x0000 DATA /
   0x1000 文件句柄 / 0x3000 调色板 / 0x3800 VGA 调色板 / 0x4000 声音 这些硬编码分区，
   免得重新编号引入新错），`STATIC_TOTAL_SIZE` 从 `0x5000` 加到 `0x7000`（留 8KB ≈ 2048 个 int）。
2. **归属**：`currentSubName == null` 时创建的变量记为全局（`HashSet<string> _globalVars`），
   含模块级 `DIM`、模块级自动创建的变量、以及 `DIM SHARED`。
   SUB 的局部/参数在 `currentLocalVars` 里，本来就不进 `variables` ✓ 不受影响。
3. **取址**：全局变量的地址用 `EmitStaticAddr(reg, STATIC_GLOBALS_OFFSET + GetVarByteOffset(name))`
   —— **只需要一个临时寄存器**（目标的那个），不需要专用寄存器
   （已确认 R7–R11 全被大量使用，33~123 处，没有空闲的）。
4. **初始化**：启动时把这段清 0（"未初始化的全局变量是 0"是 BASIC 的既定语义）。
5. **两个 helper**（新增，替换裸 `VarMemRef`）：
   ```csharp
   void EmitLoadVar(int reg, string name)     // 全局：EmitStaticAddr(reg, off); MOVE reg,[reg]
                                              // 其它：MOVE reg,[VarMemRef(name)]
   void EmitStoreVar(string name, int srcReg) // 全局：EmitStaticAddr(tmp, off); MOVE [tmp],srcReg
                                              // 其它：MOVE [VarMemRef(name)], srcReg
   ```

## 三、实施清单（逐点，改完逐个跑语料）

- [ ] `CodeGenerator.cs`：加 `STATIC_GLOBALS_OFFSET`；`STATIC_TOTAL_SIZE` 0x5000 → 0x7000
- [ ] `CodeGenerator.cs`：加 `HashSet<string> _globalVars`；在 `GetOrCreateVariable` 里按
      `currentSubName == null` 打标
- [ ] `CodeGenerator.cs`：新增 `EmitLoadVar` / `EmitStoreVar`
- [ ] `CodeGenerator.cs:~905`（静态区分配处）：追加"把全局区清 0"的循环
- [ ] **写**：`CodeGenerator.Statements.cs:553`、`:566`（模块级赋值）改走 `EmitStoreVar`
- [ ] **读**：`CodeGenerator.Sub.cs:1143`、`:1150`（SUB 读）改走 `EmitLoadVar`
- [ ] **写**：`CodeGenerator.Sub.cs:1358`（SUB 写）改走 `EmitStoreVar`
- [ ] **主程序侧的读**：`CodeGenerator.Expressions.cs:117-120`。它**不走 `VarMemRef`**，
      而是就地拼 `R12+{8 + varOffset}` —— 漏了这处就会"写进全局区、自己却从栈上读"，只修一半
- [ ] ⚠ 同一处还有个**既有隐患**要一并处理：那里 `varOffset = GetOrCreateVariable(name) * 4`
      是**按索引×4**，而 `VarMemRef` 用的是 `GetVarByteOffset`（按类型宽度累加，Double/Long 算 8 字节）
      —— **同一件事两处算法**，程序里只要有 Double/Long 就会漂。统一到 `GetVarByteOffset`
- [ ] 之后 `VarMemRef` 对全局变量应当**不再被调用**；留着它并用注释说明"全局走 EmitLoadVar"

## 四、验证

```bash
cd scripts/basic-tests && bash run.sh
```
- `t4` 应从 `sub=0` 变成 `sub=10`
- `t7` 应从 `shared=0` 变成 `shared=77`
- 其余 5 项**必须保持不变**（这是"没打坏别的"的唯一凭据）

## 五、动手时才会发现的一件事：**五个站点形状各不相同**

写到一半才确认：这些站点**不是一个模子**，不能用一个 helper 一把替换 ——

| 站点 | 现在的形状 | 改成全局后 |
|---|---|---|
| `Statements.cs:553/566`（模块级写） | 直接拼 `MEMORY(VarMemRef(x))` + `storeSrcReg` | 得先要一个**地址临时寄存器**（值在 storeSrcReg 里，不能借它） |
| `Sub.cs:1143/1150`（SUB 读） | `MOVE reg, [VarMemRef]` | 可走 `EmitLoadVar`（就地用 reg 算地址） ✓ 最规整 |
| `Sub.cs:1358`（SUB 写） | **已经把地址算进 `reg`**（`MOVE reg,#off; ADD reg,R12`），再按 `memRef.StartsWith("R12")` 分支 | 天然契合：把那段换成 `EmitStaticAddr(reg, …)`，后续存 `[reg]` |
| `Expressions.cs:117`（主程序读） | 就地拼 `R12+{8+index*4}`（还带"索引×4 vs 字节偏移"那个隐患） | 同 `EmitLoadVar` 的形状 ✓ |

所以实施顺序建议：**先做两个"形状天然契合"的读点**（`Sub.cs:1143/1150`、`Expressions.cs:117`），
跑一次语料看 `t4` 是否转绿；**再处理两个写点**（`Statements.cs:553/566`、`Sub.cs:1358`），
每步一跑。写点要额外挑一个"当下确定不活"的临时寄存器 —— 这一点在各站点要分别确认，
不能想当然（R7–R11 都被大量占用正是这个原因）。

## 六、为什么单开一轮

这改的是**所有 BASIC 程序的变量布局**，一处漏了就是"静默算错"（比现在的"读成 0"更难查）。
所以：先有语料（已就位）→ 再改 → 每改一点跑一次。**不要在一次会话末尾顺手改掉。**
