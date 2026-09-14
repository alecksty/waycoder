{ VML 条件变量扩展库 — Pascal (OS 模式) }
function CondCreate: integer;
function CondWait(condId, mutexId: integer): integer;
function CondSignal(condId: integer): integer;
function CondBroadcast(condId: integer): integer;
