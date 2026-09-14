{ VML 互斥锁扩展库 — Pascal (OS 模式) }
function MutexCreate: integer;
function MutexLock(id: integer): integer;
function MutexUnlock(id: integer): integer;
