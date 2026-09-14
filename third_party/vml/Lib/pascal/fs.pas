{ VML 文件系统扩展库 — Pascal (OS 模式) }
{ 需显式 uses fs }

function FsMkdir(path: string): integer;
function FsRemove(path: string): integer;
function FsRename(oldPath, newPath: string): integer;
function FsReaddir(path: string; var buffer): integer;
function FsStat(path: string; var info): integer;
