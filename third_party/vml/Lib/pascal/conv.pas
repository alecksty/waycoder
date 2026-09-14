{ VML 全类型转换库 — Pascal 包装器 (v1.66.44) }
{ 用法: uses conv; 或直接调用 }

{ 整数 ↔ 字符串 }
function int_to_str(i: integer): string; external 'int_to_str';
function str_to_int(s: string): integer; external 'str_to_int';
function uint_to_str(u: longword): string; external 'uint_to_str';
function str_to_uint(s: string): longword; external 'str_to_uint';

{ 64位整数 ↔ 字符串 }
function long_to_str(l: int64): string; external 'long_to_str';
function str_to_long(s: string): int64; external 'str_to_long';
function ulong_to_str(l: qword): string; external 'ulong_to_str';
function str_to_ulong(s: string): qword; external 'str_to_ulong';

{ 浮点 ↔ 字符串 }
function float_to_str(f: real): string; external 'float_to_str';
function str_to_float(s: string): real; external 'str_to_float';
function double_to_str(d: double): string; external 'double_to_str';
function str_to_double(s: string): double; external 'str_to_double';

{ 布尔 ↔ 字符串 }
function bool_to_str(b: boolean): string; external 'bool_to_str';
function str_to_bool(s: string): boolean; external 'str_to_bool';

{ 字符 ↔ 字符串 }
function char_to_str(c: char): string; external 'char_to_str';
function str_to_char(s: string): char; external 'str_to_char';

{ 8/16位整数 }
function byte_to_str(b: byte): string; external 'byte_to_str';
function str_to_byte(s: string): byte; external 'str_to_byte';
function short_to_str(s: smallint): string; external 'short_to_str';
function str_to_short(s: string): smallint; external 'str_to_short';
