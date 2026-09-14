{ Pascal 标准库 - 系统函数和过程 (完整) }

{ ============================================================ }
{ 输出函数和过程 }
{ ============================================================ }
procedure Write(x: integer);
  begin asm("SYSCALL 6") end;

procedure Write(x: real);
  begin asm("SYSCALL 59") end;

procedure Write(x: boolean);
  begin if x then Write('true') else Write('false') end;

procedure Write(x: char);
  begin asm("SYSCALL 4") end;

procedure Write(x: string);
  begin asm("SYSCALL 1") end;

procedure WriteLn(x: integer);
  begin Write(x); asm("LOAD R0 #10") asm("SYSCALL 4") end;

procedure WriteLn(x: real);
  begin Write(x); asm("LOAD R0 #10") asm("SYSCALL 4") end;

procedure WriteLn(x: boolean);
  begin Write(x); asm("LOAD R0 #10") asm("SYSCALL 4") end;

procedure WriteLn(x: char);
  begin Write(x); asm("LOAD R0 #10") asm("SYSCALL 4") end;

procedure WriteLn(x: string);
  begin Write(x); asm("LOAD R0 #10") asm("SYSCALL 4") end;

procedure WriteLn;
  begin asm("LOAD R0 #10") asm("SYSCALL 4") end;

{ ============================================================ }
{ 输入函数和过程 }
{ ============================================================ }
procedure Read(var x: integer);
  begin asm("SYSCALL 7") end;

procedure Read(var x: real);
  begin asm("SYSCALL 8") end;

procedure Read(var x: char);
  begin asm("SYSCALL 5") end;

procedure Read(var x: string);
  begin asm("SYSCALL 2") end;

procedure ReadLn(var x: integer);
  begin Read(x); ReadLn end;

procedure ReadLn(var x: real);
  begin Read(x); ReadLn end;

procedure ReadLn(var x: char);
  begin Read(x); ReadLn end;

procedure ReadLn(var x: string);
  begin Read(x); ReadLn end;

procedure ReadLn;
  begin asm("SYSCALL 2") end;

function ReadChar: char;
  begin asm("SYSCALL 5") end;

{ ============================================================ }
{ 数学函数 }
{ ============================================================ }
function Abs(x: integer): integer;
  begin asm("SYSCALL 43") end;

function Abs(x: real): real;
  begin asm("SYSCALL 44") end;

function Sqr(x: integer): integer;
  begin result := x * x end;

function Sqr(x: real): real;
  begin result := x * x end;

function Sqrt(x: real): real;
  begin asm("SYSCALL 20") end;

function Sin(x: real): real;
  begin asm("SYSCALL 21") end;

function Cos(x: real): real;
  begin asm("SYSCALL 22") end;

function Tan(x: real): real;
  begin asm("SYSCALL 23") end;

function Arctan(x: real): real;
  begin asm("SYSCALL 32") end;

function Arcsin(x: real): real;
  begin asm("SYSCALL 24") end;

function Arccos(x: real): real;
  begin asm("SYSCALL 25") end;

function Exp(x: real): real;
  begin asm("SYSCALL 27") end;

function Ln(x: real): real;
  begin asm("SYSCALL 28") end;

function Pow(x, y: real): real;
  begin asm("SYSCALL 26") end;

function Floor(x: real): integer;
  begin asm("SYSCALL 29") end;

function Ceil(x: real): integer;
  begin asm("SYSCALL 30") end;

function Max(a, b: integer): integer;
  begin if a > b then result := a else result := b end;

function Min(a, b: integer): integer;
  begin if a < b then result := a else result := b end;

function Pi: real;
  begin result := 3.141592653589793 end;

{ ============================================================ }
{ 转换函数 }
{ ============================================================ }
function Chr(x: integer): char;
  begin result := char(x) end;

function Ord(x: char): integer;
  begin result := integer(x) end;

function Round(x: real): integer;
  begin result := Floor(x + 0.5) end;

function Trunc(x: real): integer;
  begin result := Floor(x) end;

function Int(x: real): real;
  begin result := Floor(x) end;

function Frac(x: real): real;
  begin result := x - Floor(x) end;

function IntToStr(x: integer): string;
  begin asm("SYSCALL 42") end;

function StrToInt(s: string): integer;
  begin asm("SYSCALL 40") end;

function FloatToStr(x: real): string;
  begin asm("SYSCALL 58") end;

function StrToFloat(s: string): real;
  begin asm("SYSCALL 41") end;

function Str(x: integer; var s: string);
  begin s := IntToStr(x) end;

function Str(x: real; var s: string);
  begin s := FloatToStr(x) end;

procedure Val(s: string; var v: integer; var code: integer);
  begin v := StrToInt(s); code := 0 end;

procedure Val(s: string; var v: real; var code: integer);
  begin v := StrToFloat(s); code := 0 end;

function HexStr(x: integer): string;
  begin asm("SYSCALL 10") end;

{ ============================================================ }
{ 序数函数 }
{ ============================================================ }
function Pred(x: integer): integer;
  begin result := x - 1 end;

function Pred(x: char): char;
  begin result := Chr(Ord(x) - 1) end;

function Succ(x: integer): integer;
  begin result := x + 1 end;

function Succ(x: char): char;
  begin result := Chr(Ord(x) + 1) end;

{ ============================================================ }
{ 布尔函数 }
{ ============================================================ }
function Odd(x: integer): boolean;
  begin result := (x mod 2) <> 0 end;

{ ============================================================ }
{ 随机数函数 }
{ ============================================================ }
procedure Randomize;
  begin asm("SYSCALL 51") end;

function Random: real;
  begin asm("SYSCALL 50") end;

function Random(range: integer): integer;
  begin result := trunc(Random) mod range end;

{ ============================================================ }
{ 字符串函数 }
{ ============================================================ }
function Length(s: string): integer;
  begin asm("SYSCALL 60") end;

function Copy(s: string; index, count: integer): string;
  begin
    if index < 1 then index := 1;
    if index + count - 1 > Length(s) then count := Length(s) - index + 1;
    if count <= 0 then result := '' else asm("SYSCALL 71") end;

function Pos(substr, s: string): integer;
  begin
    for result := 1 to Length(s) - Length(substr) + 1 do
      if Copy(s, result, Length(substr)) = substr then exit;
    result := 0;
  end;

procedure Delete(var s: string; index, count: integer);
  begin
    if index < 1 then index := 1;
    if index > Length(s) then exit;
    if index + count > Length(s) then count := Length(s) - index + 1;
    s := Copy(s, 1, index - 1) + Copy(s, index + count, Length(s));
  end;

procedure Insert(source: string; var dest: string; index: integer);
  begin
    if index < 1 then index := 1;
    if index > Length(dest) + 1 then index := Length(dest) + 1;
    dest := Copy(dest, 1, index - 1) + source + Copy(dest, index, Length(dest));
  end;

function Concat(s1, s2: string): string;
  var i: integer;
  begin
    result := '';
    for i := 1 to Length(s1) do result := result + s1[i];
    for i := 1 to Length(s2) do result := result + s2[i];
  end;

function Concat(s1, s2, s3: string): string;
  begin result := Concat(Concat(s1, s2), s3) end;

function LowerCase(s: string): string;
  var i: integer; c: char;
  begin
    result := s;
    for i := 1 to Length(s) do begin
      c := s[i];
      if (c >= 'A') and (c <= 'Z') then result[i] := Chr(Ord(c) + 32);
    end;
  end;

function UpperCase(s: string): string;
  var i: integer; c: char;
  begin
    result := s;
    for i := 1 to Length(s) do begin
      c := s[i];
      if (c >= 'a') and (c <= 'z') then result[i] := Chr(Ord(c) - 32);
    end;
  end;

function Trim(s: string): string;
  var start, finish: integer;
  begin
    start := 1;
    while (start <= Length(s)) and (s[start] = ' ') do Inc(start);
    finish := Length(s);
    while (finish >= start) and (s[finish] = ' ') do Dec(finish);
    result := Copy(s, start, finish - start + 1);
  end;

function StringReplace(s, oldPattern, newPattern: string): string;
  var i: integer;
  begin
    result := '';
    i := 1;
    while i <= Length(s) do begin
      if Copy(s, i, Length(oldPattern)) = oldPattern then begin
        result := result + newPattern;
        i := i + Length(oldPattern);
      end else begin
        result := result + s[i];
        Inc(i);
      end;
    end;
  end;

function StringOfChar(c: char; count: integer): string;
  var i: integer;
  begin
    result := '';
    for i := 1 to count do result := result + c;
  end;

function CompareStr(s1, s2: string): integer;
  begin asm("SYSCALL 62") end;

function CompareText(s1, s2: string): integer;
  begin result := CompareStr(LowerCase(s1), LowerCase(s2)) end;

{ ============================================================ }
{ 文件操作 (OS 模式) }
{ ============================================================ }
type TFileHandle = integer;

procedure AssignFile(var f: TFileHandle; filename: string);
  begin f := -1 end;

procedure Reset(var f: TFileHandle);
  begin asm("SYSCALL 110") end;

procedure Rewrite(var f: TFileHandle);
  begin asm("SYSCALL 111") end;

procedure Append(var f: TFileHandle);
  begin asm("SYSCALL 112") end;

procedure CloseFile(var f: TFileHandle);
  begin asm("SYSCALL 113") end;

function Eof(var f: TFileHandle): boolean;
  begin asm("SYSCALL 114"); result := false end;

function Eoln(var f: TFileHandle): boolean;
  begin result := false end;

function Seek(var f: TFileHandle; pos: integer): integer;
  begin asm("SYSCALL 115") end;

function FilePos(var f: TFileHandle): integer;
  begin asm("SYSCALL 116") end;

function FileSize(var f: TFileHandle): integer;
  begin asm("SYSCALL 117") end;

{ ============================================================ }
{ 内存操作 }
{ ============================================================ }
function Malloc(size: integer): pointer;
  begin asm("SYSCALL 40") end;

procedure Free(p: pointer);
  begin asm("SYSCALL 41") end;

function MemCmp(var a, b; n: integer): integer;
  begin asm("SYSCALL 13") end;

procedure Move(var source, dest; count: integer);
  begin asm("SYSCALL 71") end;

procedure FillChar(var x; count: integer; value: char);
  begin asm("SYSCALL 70") end;

function SizeOf(x: integer): integer;
  begin result := 4 end;

{ ============================================================ }
{ 日期与时间 }
{ ============================================================ }
function GetDateTime: longint;
  begin asm("SYSCALL 54") end;

function Now: real;
  begin asm("SYSCALL 53"); result := 0.0 end;

function Date: string;
  begin asm("SYSCALL 55") end;

function Time: string;
  begin asm("SYSCALL 56") end;

function DateTimeToStr(dt: longint): string;
  begin result := IntToStr(dt) end;

function DateToStr(d: real): string;
  begin result := Date end;

function TimeToStr(t: real): string;
  begin result := Time end;

function GetTickCount: longint;
  begin asm("SYSCALL 53") end;

procedure Delay(ms: integer);
  begin asm("SYSCALL 52") end;

{ ============================================================ }
{ 系统函数 }
{ ============================================================ }
procedure Halt;
  begin Halt(0) end;

procedure Halt(exitcode: integer);
  begin asm("SYSCALL 3") end;

function ParamCount: integer;
  begin result := 0 end;

function ParamStr(index: integer): string;
  begin result := '' end;

function GetConfig(key: integer): integer;
  begin asm("SYSCALL 60") end;
