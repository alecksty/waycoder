{ Dos —— Turbo Pascal `Dos` 单元的 **Pascal 侧声明**

  ## 为什么需要这个文件

  `uses Dos` 的**链接**一直是好的（`AutoLinkUnit` 把 `Lib/pascal/dos.vml` 链上，
  那份模块里有 `GetDate:` / `FindFirst:` 这些**裸的 Pascal 大小写标签**）。
  缺的是**声明**：`Registers` / `SearchRec` 这两个**记录类型**、
  以及 `Intr`/`MsDos`/`FindFirst` 这些**子程序头**。

  缺类型的后果最重，而且是"指不到根因"的那种：老程序写 `var Regs: Registers;`
  再用 `Regs.AX`，前端只认程序自己 `type` 段里建过的记录布局 ⇒
  报「**变量 'Regs' 不是 record类型，无法访问字段**」——
  实测这一条后来**同时卡住 12 份语料**（`Registers` / `SearchRec` / … 名字各不相同、
  根因是同一个），而错误信息里既没有 `Registers` 也没有 `Dos`，看不出是库缺了东西。
  （配套的前端改动：`CodeGenerator.UnitTypeDeclarations` + `ApplyUnitTypeDeclarations`。）

  ## 各条的真实状态（别照着声明就当它能用）

  · **有实现**（`Lib/shared/dos.vml` 里是裸 Pascal 名标签，按名解析）：
    `GetDate` / `GetTime` / `FindFirst` / `FindNext` / `FindClose` / `GetEnv` /
    `EnvCount` / `EnvStr` / `DiskFree` / `DiskSize` / `DosVersion` / `DosExitCode` /
    `Exec` / `SwapVectors` / `GetIntVec` / `SetIntVec`
    —— 签名以 `Lib/shared/dos.vml` 头部的 `; source :` 行为准，下面逐条对齐。

  · ⚠ **没有实现、按"空过程"处理**：`Intr` / `MsDos`。
    它们要的是 **x86 实模式中断**（`int 10h/13h/16h/21h`…），本平台是平坦内存模型、
    没有那套东西 —— 这是**有意接受的边界**（见 `docs/老程序兼容性.md` 的三条定案）。
    按用户定的口径「**实现不了的直接返 0 做个空函数**」，这里声明成普通过程，
    调用**不报错、什么都不做**。⚠ 也就是说：**靠 Intr 直写显存/BIOS 的老程序
    编得过、也能跑完，但画面上什么都不会发生** —— 这不是"跑对了"，是"没有崩"。

  ## 只声明，不实现

  `implementation` 段**必须**是空的：一旦写上函数体，本单元会自己生成同名标签，
  主程序里的 `CALL` 会解析到**这个本地版本**而不是库里的实现（与 `graph.pas`/`crt.pas` 同一约定）。
  `Intr`/`MsDos` 那两条的"空"不在这个文件里 —— 它们**根本不声明实现**，
  由前端按「未定义的函数」处理…… ⚠ 这一条与上面「空过程」的说法**冲突**，
  见下面的"现状"一节。 }

{ ⚠ 现状更正（写这份文件时核实到的，别被上面那句绕进去）：
  `Intr`/`MsDos` 目前**只是声明**，而 `Lib/` 里没有它们的实现 ⇒
  调用它们会**在链接期报「未定义的函数 'Intr'」**（响亮失败，不是静默空转）。
  要真按"空函数"落地，得往 `Lib/` 加实现并重跑 GenLib —— 那是**下一步**，
  本次先让"类型"这一层通（它才是 12 份语料的首错）。 }

unit Dos;

interface

{ ── 类型 ──────────────────────────────────────────────────────── }

type
  { `Intr`/`MsDos` 的入参/出参结构。
    ⚠ Turbo Pascal 里它是**变体记录**（`case Integer of 0:(AX,BX,…); 1:(AL,AH,…)`），
    为的是让 `AL`/`AH` 与 `AX` 的**高低字节重叠**。本前端没有变体记录，
    这里按**平铺的 10 个 Word** 声明 —— 于是 `Regs.AX := …` 这类主寄存器访问是对的，
    而 `Regs.AL := …` 那种**依赖重叠布局**的写法不会生效（值写进去了，但与 AX 不重叠）。
    若要精确支持，得先给前端加变体记录，届时这里再改成变体形式即可。 }
  Registers = record
    AX, BX, CX, DX, BP, SI, DI, DS, ES, Flags: Word;
    { ⚠ 这 8 个在 TP 里是**与 AX/BX/CX/DX 重叠**的（变体记录的另一支），
      这里**平铺在后面**（本前端没有变体记录）⇒ 老程序里 `Regs.AH := …`
      会写进一个**独立**的槽、`Regs.AX` 读不到它。
      实测语料里确实有这种写法（`g7iles_life` 卡在「REGISTERS 中不存在字段 'AH'」），
      平铺至少让它**编得过**；但**依赖重叠的语义是错的**，别把这当成支持了变体记录。 }
    AL, AH, BL, BH, CL, CH, DL, DH: Byte;
  end;

  { `FindFirst`/`FindNext` 的查找结果。
    ⚠ 字段的**偏移**与 TP 的 `SearchRec` 一致（21 字节保留 + Attr/Time/Size/Name），
    本前端按 4 字节槽对齐 ⇒ 偏移不是逐字节的 —— 老程序只读 `Name`/`Size`/`Attr`，
    按字段名访问即可，**不要**依赖 `Fill` 的字节数。 }
  SearchRec = record
    Fill: array[1..21] of Byte;
    Attr: Byte;
    Time: LongInt;
    Size: LongInt;
    Name: string;
  end;

{ ── 版本与错误码 ──────────────────────────────────────────────── }

const
  { ⚠ `DosError` 在 TP 里是个**变量**（Dos 单元每次调用后置位），而本前端的
    "单元可见符号"只支持**整数常量**与**子程序头**（`UnitConstInts`/`UnitSubprograms`），
    单元里的 `var` **传不到主程序**（会报"未声明的变量"）。
    所以这里退化成**常量桩 = 0**，与 `ParamCount`/`Dseg` 的处置同一套。

    后果要看清楚：`FindFirst(...); if DosError <> 0 then <处理没找到>` 这类分支
    会**一直走"成功"那一支**。这是"能编过、不崩"与"语义正确"之间有意选的前者
    （用户对实现不了的接口定的口径是「直接返 0 做个空函数」）。
    真要把它做对，得给前端加"单元 interface 变量"（与本次给类型加的
    `UnitTypeDeclarations` 是同一类缺口）。 }
  DosError = 0;

  { `DosError` 的取值（TP 的 Dos 单元）—— 老程序拿它判"文件没找到"这类分支。 }
  dosErrorNone = 0;
  dosErrorFileNotFound = 2;
  dosErrorPathNotFound = 3;
  dosErrorTooManyOpenFiles = 4;
  dosErrorAccessDenied = 5;

  { `FindFirst`/`FindNext` 的 attr 参数 }
  ReadOnly = $01;
  Hidden   = $02;
  SysFile  = $04;
  VolumeID = $08;
  Directory = $10;
  Archive  = $20;
  AnyFile  = $3F;

function DosVersion: integer;
function DosExitCode: integer;

{ ── 磁盘 ──────────────────────────────────────────────────────── }

function DiskFree(drive: byte): LongInt;
function DiskSize(drive: byte): LongInt;

{ ── 目录查找 ──────────────────────────────────────────────────── }

procedure FindFirst(path: string; attr: word; var sr: SearchRec);
procedure FindNext(var sr: SearchRec);
procedure FindClose(var sr: SearchRec);

{ ── 日期时间 ──────────────────────────────────────────────────── }

procedure GetDate(var year, month, day, dayOfWeek: word);
procedure GetTime(var hour, minute, second, sec100: word);

{ ── 环境变量 ──────────────────────────────────────────────────── }

function EnvCount: integer;
function EnvStr(index: integer): string;
function GetEnv(name: string): string;

{ ── 执行与中断向量 ────────────────────────────────────────────── }

function Exec(path, args: string): integer;
procedure SwapVectors;
procedure GetIntVec(intno: byte; var vector: integer);
procedure SetIntVec(intno: byte; vector: integer);

{ ── 实模式中断：**本平台没有对应物**，调用会报「未定义的函数」─────────
   留着声明是为了让程序的**类型**能解析（`Registers` 要有人引用），
   也让"这里缺一块"这件事在源码里看得见。 }
procedure Intr(intno: byte; var regs: Registers);
procedure MsDos(var regs: Registers);

implementation

{ 空 —— 实现全在 `Lib/shared/dos.vml`（裸 Pascal 名标签），理由见文件头。 }

end.
