{ VML 环境变量扩展库 — Pascal (OS 模式) }
{ 需显式 uses env }

function GetEnv(name: string): string;
function SetEnv(name, value: string): integer;
function GetArgs(var buffer): integer;
