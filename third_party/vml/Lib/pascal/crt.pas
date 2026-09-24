{ Crt —— Turbo Pascal `Crt` 单元的 **Pascal 侧声明**

  ## 为什么需要这个文件

  前端登记一个单元的**常量**（`UnitConstInts`）与**子程序头**（`UnitSubprograms`）时，
  读的是**该单元的 Pascal 源码**（`PascalCompiler.RegisterUnitFunctions`）。
  而 `Crt` 此前**只有 `crt.vml`（实现）、没有 `.pas`（声明）** ⇒ 一个常量都登记不上
  ⇒ 老程序里裸写的 `TextColor(White)` / `TextBackground(Blue)` 一律报
  「未声明的变量 'White'」（实测）。而 `uses graph` 的用户从来没这问题 ——
  因为 `graph.pas` 一直在（它的 16 色常量与这里的**同值同名**）。

  ## 这个文件是什么、不是什么

  它是**声明**，不是实现：`implementation` 段是空的，一个函数体都没有
  （与 `graph.pas` 同一约定）。实现全在 `Lib/shared/crt.vml`
  （由 `Lib/shared/src/crt.c` 生成，走 ANSI 转义、宿主侧已接终端配色）。

  ⚠ `implementation` **必须**是空的：一旦写上函数体，本单元就会自己生成同名标签，
  主程序里的 `CALL` 会解析到**这个空的本地版本**而不是库里的实现（返回垃圾、不报错）。

  ## 只登记常量，不登记子程序头

  `TextColor`/`GotoXY`/`ClrScr`… 这些名字前端**另有专门的名字表**（含"裸写"形态，
  见 `CodeGenerator.Core.CrtBareFunctions` 与 `CodeGenerator.Misc` 那两张表），
  本文件重复声明只会增加"同一件事两处实现"的漂移面。**改动前先看那两张表。** }

unit Crt;

interface

const
  { ── 16 色文本色（`TextColor` / `TextBackground` 的实参）──────────────
     ⚠ 与 `graph.pas` 的调色板索引**同值同名**（都是 CGA/VGA 标准），
       所以一个文件同时写 `SetColor(Red)` 与 `TextColor(Red)` 都能用。
       两处都改了才算改（本仓头号坑就是"同一份清单两处实现"）。 }
  Black = 0;  Blue = 1;  Green = 2;  Cyan = 3;
  Red = 4;    Magenta = 5;  Brown = 6;  LightGray = 7;
  DarkGray = 8;  LightBlue = 9;  LightGreen = 10;  LightCyan = 11;
  LightRed = 12; LightMagenta = 13; Yellow = 14;  White = 15;

  { ── 显示模式（`TextMode` 的实参）──────────────────────────────── }
  BW40 = 0;  CO40 = 1;  BW80 = 2;  CO80 = 3;  Mono = 7;

implementation

{ 空 —— 实现全在 `Lib/shared/crt.vml`，理由见文件头注释。 }

end.
