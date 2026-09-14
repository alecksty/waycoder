{ VML 线程扩展库 — Pascal (OS 模式) }
function ThreadCreate(entry: pointer; stackSize: integer): integer;
procedure ThreadExit;
function ThreadJoin(tid: integer): integer;
function ThreadYield: integer;
