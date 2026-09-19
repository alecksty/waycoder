# VML 编译期诊断判据

与 `../vml-out-probe` **极性相反**的一套：

| | 判什么 |
|---|---|
| `vml-out-probe` | 程序**必须编译成功**，输出逐字节正确 |
| **本套** | 程序**必须编译失败**，且 stderr 要点名出错的那个标识符 |

**为什么必须分开**（这是从 out-probe 自己的注释里学来的教训）：那一套早期把
「编译期抛异常」与「跑完输出不对」混成一档，于是 5 个编译失败被报成"输出为空"，
害得"修编译器"和"修探针"混在一起看不出来。反过来也一样 —— 把"必须失败"的用例
塞进 out-probe，它会把**正确行为判成 FAIL**。

## 用法

```bash
scripts/vml-diag-probe/run.sh                # 全部
scripts/vml-diag-probe/run.sh link-clean     # 只跑某组
scripts/vml-diag-probe/run.sh undef-fn c     # 某组里只跑 .c
```

## 三档判定

- **PASS** = 退出码非 0，stderr 里同时有「编译失败/error:」**和**用例点名的标识符
- **FAIL** = 编译"成功"了
- **CRASH** = `Unhandled exception` / `KeyNotFoundException` / `VmlLabelException`

**`CRASH` 必须单列**：那正是"修复没生效、只是换个地方崩"的伪装形态。
今天（修之前）的行为就是「编译通过 → 运行期 `KeyNotFoundException: 未找到标签: nosuch`」——
只能判"没编过"的话，那种形态会冒充 PASS。

每个用例旁边放一个 `<用例>.sym`，里面写**必须被点名**的标识符
（有些前端会改名前缀，比如 Rust 的 `func_nosuch`）。

## 各组

### `link-clean` —— 正常程序不该有未解析标签

判的是**用户档**，不是总数。链接器按**指令来源**分两档报（`LibraryLinker.ReportUnresolved`
的 `userEnd` 索引边界）：

- **用户档** = 前端为你这份源码产出的指令里的未解析调用 ⇒ 就是**你写错了一个函数名**。
  这一档必须恒为 0，也是"未定义函数升级成编译期硬错误"的前提。
- **库档** = 链接进来的库模块内部的未解析调用 ⇒ 历史遗留的**死包装器**。
  归 Tier 2 待办，不拦本次。

**2026-09-19 实测**：用户档 22 门全是 0；库档只有 4 门非零（bas 33 / cs 32 / ld 32 / pas 32）。

### `undef-fn` —— 未定义函数必须编译期报错

**当前 13/16 通过**。三门**红着的就是待办信号**，别把它们删掉：

| 用例 | 现状 | 说明 |
|---|---|---|
| `undef-fn.go` | ❌ 编译通过 | 前端**把整句调用丢掉了** —— 生成的汇编里连 `nosuch` 都没有（`grep -c nosuch` = 0） |
| `undef-fn.bas` | ❌ 编译通过 | 同上，静默丢弃 |
| `undef-fn.js` | ❌ 编译通过 | 未见 `call ...nosuch`；待查 |

⚠ 这三门是**另一类缺陷**（静默丢弃语句），比"发一个不存在的标签"更隐蔽 ——
后者至少会在链接期冒出来。修它们属逐门前端的工作（P6+），不是链接器能覆盖的。

### `dyn-global` —— 动态语言的隐式全局必须仍然合法

6 门动态语言（js/lua/py/r/rb/scm）的「未声明即隐式全局」是**合法语义**，不是缺陷。
本组是那道**反向护栏**：豁免改错了会在这里变红。

## 反证（判据必须能红）

仓库的规矩是「不响的自测比没有更糟」。四条反证都不需要造特殊输入 ——
**出问题的输入恰是修复本身**：

- `undef-fn` ← 把 `LibraryLinker` 里那句 `throw new UnresolvedSymbolException` 换回
  `Console.Error.WriteLine` ⇒ 用例立刻变回「编译通过、运行崩」
- `link-clean` ← 往 `Lib/modules.json` 的某个模块里加回一条虚构条目 →
  `dotnet GenLib.dll -m -r third_party/vml` ⇒ 那门语言立刻冒出未解析标签
