(*
  Category: SWAG Title: CURSOR HANDLING ROUTINES
  Original name: 0031.PAS
  Description: Change the cursor aspect in textmode
  Author: AVONTURE CHRISTOPHE
  Date: 03-04-97  13:18
*)

{

   Change the cursor aspect in text mode


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

Type
   CursorType = (cNormal, cInsert);

PROCEDURE Set_Cursor (cType : CursorType);

{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 BIOS 中断 int 10h 的
  AH=01h 功能（设置文本模式光标的扫描线起止行）——若 cType = cInsert，则取
  CX = 0115h（起始行 01h、结束行 15h，即插入模式下那个方块状光标）；否则取
  CX = 0607h（正常的下划线光标）。
  本平台没有 BIOS / int 10h，也没有文本模式的硬件光标寄存器，该效果无处可施
  ⇒ 以空过程代替（对调用方而言就是「什么也不改」）。原汇编保留在下方注释里备查。 }
(*
ASM

    Cmp  cType, cNormal
    Je   @Normal

    Mov  Ah, 01h
    Mov  Cl, 15h
    Mov  Ch, 01h

    Jmp  @Call

@Normal:

    Mov  Ah, 01h
    Mov  Cx, 0607h

@Call:

    Int  10h

END;
*)

Begin

End;

Begin

   { Set the cursor normal }

   Set_Cursor (cNormal);

   { Set the cursor like a square -like used in an insert mode- }

   Set_Cursor (cInsert);

End;
