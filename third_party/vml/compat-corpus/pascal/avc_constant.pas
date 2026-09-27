(*
  Category: SWAG Title: EXECUTION ROUTINES
  Original name: 0049.PAS
  Description: Change constants in EXE files
  Author: AVONTURE CHRISTOPHE
  Date: 11-29-96  08:17
*)

{

   The purpose of this program is to extract show how we can put a constant
   into our program and then, when the program is running, modify this
   constant in the EXE file.  So, on the next run, the constant will be
   modified.

   WARNING: A PROGRAM LIKE THAT CAN'T BE NEVER COMPRESSED BY PKLITE, LZEXE,
            DIET OR ANY OTHER  EXE COMPRESSOR.  THIS  PROGRAM CAN'T ALSO BE
            ENCRYPTED BY A PROGRAM!!!   SO BE CAREFULL!!!

   Try to  run  this program  severall times  from  the DOS prompt and NEVER
   FROM THE PASCAL IDE.



               ╔════════════════════════════════════════╗
               ║                                        ║░
               ║          AVONTURE CHRISTOPHE           ║░
               ║              AVC SOFTWARE              ║░
               ║     BOULEVARD EDMOND MACHTENS 157/53   ║░
               ║           B-1080 BRUXELLES             ║░
               ║              BELGIQUE                  ║░
               ║                                        ║░
               ╚════════════════════════════════════════╝░
               ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░

}

Uses Crt;

Const Taille_Psp : Integer = 256;

{ ⚠ 原码把 `Tour` 放在 `Const` 段却又给它赋值（`Tour := Tour+1`）—— 常量不可赋值，
  那是原程序自身的一处错误（老编译器宽容、本前端按语言规则报「未声明的变量」）。
  这里按本意把它挪成**变量**：它要被自增、被 `Ofs()` 取地址、被 `BlockWrite` 写出。 }
Var   Fich       : File;
      Taille_Hdr : Word;
      Tour       : Integer;

Begin

       Assign (Fich,ParamStr(0));
       Reset (Fich,1);
       Seek (Fich, 8);
       BlockRead (Fich, Taille_Hdr, 1);
       Tour := Tour+1;
       Writeln ('You have used this program ',tour,' times');
       Seek (Fich, LongInt(Taille_Hdr) Shl 4
           +(Dseg-PrefixSeg) shl 4-Taille_Psp+Ofs(Tour));
       BlockWrite (Fich, Tour, 1);

       If (Tour = 5) then
          Begin
             Writeln ('You have already used this program five times.  '+
                      'Is it cool?');

          End;
End.
