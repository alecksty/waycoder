{ VML 文件操作扩展库 — Pascal }
function Fopen(name, mode: string): integer;
function Fclose(handle: integer): integer;
function Fread(handle: integer; var buf; count: integer): integer;
function Fwrite(handle: integer; var buf; count: integer): integer;
function Fseek(handle, offset: integer): integer;
function Ftell(handle: integer): integer;
