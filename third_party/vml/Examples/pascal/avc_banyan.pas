(*
  Category: SWAG Title: NOVELL/LANTASTIC NETWORK ROUTINES
  Original name: 0046.PAS
  Description: Functions to handle BANYAN Network
  Author: AVONTURE CHRISTOPHE
  Date: 03-04-97  13:18
*)

{

   Unit provided with some functions to handle a BANYAN network.

   All informations are been found in the Ralf Brown's Interrupt List.


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

UNIT BANYAN;

INTERFACE

{ Get somes informations about the session }

PROCEDURE Get_Session_Parameter;

{ Logout procedure without any warnings }

PROCEDURE Logout_Vines;

{ Send a message to the user connected: it's you }

PROCEDURE Send_A_Msg_To_Me (Msg : String);

{ Change your name by other any string }

PROCEDURE Set_User_Name (UserName : String);

{ Get the interrupt number used by the BANYAN network }

FUNCTION  Get_Banyan_Int_NO             : Word;

{ Get the name of the user logged in }

FUNCTION  Get_User_Full_Name            : String;
FUNCTION  GetTrueUser (sInput : String) : String;

VAR
   bBANYAN_Installed : Boolean;


IMPLEMENTATION

USES
   Dos;

VAR
   Reg : registers;

TYPE

   { Buffer of 64 Bytes in order to store the full user name }

   TName              = Array[1..64] of Byte;

   { Buffer of 64 Bytes in order to store the service name }

   TService_Name      = Array[1..64] of Byte;

   { Buffer of 64 Bytes in order to store the connection name }

   TConnection_Name   = Array[1..64] of Byte;

   { User record : function number and full user name }

   TUser = RECORD
      Sub_Function : Word;
      Name         : TName;
   END;

   { This record will be user to ask BANYAN to fill in an user name.  For
     example, I can set a true name and BANYAN will return to me the
     STREETALK address of this person. }

   TGetTrueUser = RECORD
     Sub_Function : Word;
     pInput_Name  : Word;      { Pointer (offset) to a 64 bytes buffer }
     pOutput_Name : Word;      { Pointer (offset) to a 64 bytes buffer }
   END;

   { Only to store the LOGOUT function number }

   TLogout = RECORD
      Sub_Function : Word;
   END;

   { Information on the ASYNC connection }

   TConnect_Info = RECORD
      Length_Of_Service_Name    : Word;
      pService_Name             : Pointer;
      Connection_Type           : Byte;
      Length_Of_Connection_Name : Word;
      pConnection_Name          : Pointer;
      Service_Line_Number       : Byte;
   END;

   { Current session informations }

   TSession = RECORD
      Session_ID        : Byte;
      Sub_Function      : Byte;
      Ofs_Connect_Info  : Word;
   END;

VAR
   User       : TUser;
   Session    : TSession;
   Connect    : TConnect_Info;
   Logout     : TLogout;
   Service    : TService_Name;
   Connection : TConnection_Name;
   S          : ^String;

{ Returns the interrupt number used by the BANYAN network }

FUNCTION Get_Banyan_Int_NO : Word;

{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：探测 Banyan VINES 网络是否
  已安装 —— 置 AX=$0D701h、BX=0 后调 int 2Fh（多路复用中断）；Banyan 已安装时
  AX 返回 0、否则返回它所占用中断号。随后 Not Al 把「AX=0」翻成
  bBANYAN_Installed 的布尔真值（0 → True），最后把 BX 里的中断号作为函数值返回。
  本平台没有 int 2Fh、也没有 Banyan VINES 网络环境，恒为「未安装」⇒ 以空函数
  返回 0（与「未安装时 AX=0」一致）。调用方据此把 bBANYAN_Installed 置 False，
  下面几个过程于是全部提前 Exit —— 正是本平台应有的行为。
  原汇编保留在下方注释里备查。 }
(*
ASM

     Mov  Ax, 0D701h
     Xor  Bx, Bx

     Int  2Fh

    { Because the return value 0000h in AX tell me that BANYAN is installed,
      I must take the NOT value of this for the Boolean value. }

     Not  Al
     Mov  bBANYAN_Installed, Al

     Mov  Ax, Bx

END;
*)

BEGIN

   Get_Banyan_Int_NO := 0;

END;

{ Get the STREETALK address of the actual logged in user }

FUNCTION Get_User_Full_Name : String;

BEGIN

    IF NOT bBANYAN_Installed THEN

       Get_User_Full_Name := ''

    ELSE
       BEGIN

          User.Sub_Function := $0005;

          WITH REG DO
             BEGIN
               AX := $0004;
               DX := Ofs (User);
               DS := Seg (User);
             END;

          INTR (Get_Banyan_Int_NO, Reg);

          S := Ptr (Seg(User.Name), Ofs(User.Name)-1);
          S^[0] := #64;

          Get_User_Full_Name := S^;

       END;

END;

{ This function will return the full STREETALK address of an user that I give
  only the familly name.  For example, if I get to this function the "GATES"
  name, the function will return "GATES Bill@PI.BOSS@MICROSOFT" (it's an
  example).

  The function will return <Unknown> if nobody has this name }

FUNCTION GetTrueUser (sInput : String) : String;

VAR
   User         : TGetTrueUser;
   sInput_Name  : TName;
   sOutput_Name : TName;
   pS           : ^String;
   sResul       : String;
   I            : Byte;

BEGIN

   IF NOT bBANYAN_Installed THEN Exit;

   User.Sub_Function := $012C;  { $0064 : Organization }
                                { $00C8 : Group        }
                                { $012C : Item         }

   { Pointers initialization }

   User.pInput_Name  := Ofs(sInput_Name);
   User.pOutput_Name := Ofs(sOutput_Name);

   pS := Ptr (Seg (sInput_Name), Ofs (sInput_Name)-1);
   pS^:= sInput;

   sInput_Name[Length(sInput)+1] := 0;

   WITH REG DO
      BEGIN
        AX := $0007;
        BX := $0007;
        DX := Ofs (User);
        DS := Seg (User);
      END;

   INTR (Get_Banyan_Int_NO, Reg);

   { AX equals ZERO if the user has been found }

   IF REG.AX = 0 THEN
      BEGIN

         pS := Ptr (Seg (sOutput_Name), Ofs (sOutput_Name)-1);

         sResul := '';
         I := 1;

         WHILE NOT (pS^[I] = #0) DO
           BEGIN

             sResul := sResul + pS^[I];
             Inc (I);

           END;

      END
   ELSE

      { The user is unknown }

      sResul := '<Unknown>';

   GetTrueUser := sResul;

END;

{ Allow to modify the user name of the user actually logged in on this
  station by any string. }

PROCEDURE Set_User_Name (UserName : String);

BEGIN

   IF NOT bBANYAN_Installed THEN Exit;

    { Test if this name exists }

    IF (GetTrueUser (UserName) = '<Unknown>') THEN
       Exit;

    S := Ptr (Seg(User.Name), Ofs(User.Name)-1);
    S^:= UserName;
    S^[64] := #0;

    User.Sub_Function := $0004;

    WITH REG DO
       BEGIN
         AX := $0004;
         DX := Ofs (User);
         DS := Seg (User);
       END;

    INTR (Get_Banyan_Int_NO, Reg);

END;

{ Get somes informations about the BANYAN session }

PROCEDURE Get_Session_Parameter;

BEGIN

    IF NOT bBANYAN_Installed THEN Exit;

    Session.Sub_Function     := $12;
    Session.Ofs_Connect_Info := Ofs(Connect);
    Connect.pService_Name    := @Service;
    Connect.pConnection_Name := @Connection;

    WITH REG DO
       BEGIN
         AX := $0003;
         BX := Ofs (Session);
         DS := Seg (Session);
       END;

    INTR (Get_Banyan_Int_NO, Reg);

END;

{ Send a message through the network to the actual user -you- }

PROCEDURE Send_A_Msg_To_Me (Msg : String);

BEGIN

   IF NOT bBANYAN_Installed THEN Exit;

   Msg := Msg + #0;

   WITH REG DO
      BEGIN
         AX := $0008;
         BX := $0002;
         CX := 2;
         DX := Ofs (Msg);
         DS := Seg (Msg);
      END;

   INTR (Get_Banyan_Int_NO, Reg);

END;

{ Logout procedure: terminates the actual BANYAN session }

PROCEDURE Logout_Vines;

BEGIN

   IF NOT bBANYAN_Installed THEN Exit;

   Logout.Sub_Function := $000c;

   WITH REG DO
      BEGIN
         AX := $0004;
         DX := Ofs (Logout);
         DS := Seg (Logout);
      END;

   INTR (Get_Banyan_Int_NO, Reg);

END;


BEGIN

    { Call first Get_Banyan_Int_NO for setting the bBANYAN_Installed }

    IF Get_Banyan_Int_NO = 0 THEN
       bBANYAN_Installed := False
    ELSE
       bBANYAN_Installed := True;

END.

{ -------------------------- cut here ------------------------------------ }
{ First Sample program }

{$A+,B-,D-,E-,F-,G+,I-,L-,N+,O-,P-,Q-,R-,S-,T-,V+,X+}
{$M 4000,0,8000}

USES
   Banyan;

VAR
   sParam  : String;
   sEmpty  : String;
   PSP_Seg : Word;
   pS      : ^String;
   I       : Byte;
   sResul  : String;

BEGIN

    IF (ParamCount = 0) THEN
       BEGIN
          Writeln ('');
          Writeln ('This little program has been written for, temporary, modify your user name');
          Writeln ('(under Banyan) to the name of an another user.');
          Writeln ('');
          Writeln ('');
          Writeln ('Example:  Chg_User VAN PIPERZEEL Dirk@TYOU.SYS');
          Writeln ('');
          Halt;
       END;

    { There is a problem with the paramstr() variable of Pascal: each time a
      space is found, Pascal think that there is another parameter. So, I
      will used interrupt in order to read the command line }

    { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：用 DOS 中断 int 21h 的
      AH=62h 功能读取当前程序的 PSP 段地址并存进 PSP_Seg；紧接着那两句
      pS := ptr (PSP_Seg, $80) / sParam := pS^ 就是靠它去 PSP:80h 拿原始命令行
      （绕开 Pascal 的 ParamStr 会把带空格的用户名拆成多个参数的问题）。
      本平台没有 PSP、也没有实模式段地址 ⇒ 无法实现，直接置 PSP_Seg := 0。
      原汇编保留在下方注释里备查。 }
(*
    Asm
       Mov  Ah, 62h               { Read PSP Segment }
       Int  21h
       Mov  PSP_Seg, Bx           { And Save it }
    End;
*)

    PSP_Seg := 0;

    pS := ptr (PSP_Seg, $80);    { Parameters are in PSP_SEG:80h }

    sParam := pS^;
    Delete (sParam, 1, 1);

    IF (GetTrueUser(sParam) = '<Unknown>') THEN
       BEGIN
           sResul := '';
           I := 1;

           WHILE NOT (sParam[I] = #0) DO
             BEGIN

               sResul := sResul + sParam[I];
               Inc (I);

             END;

          Writeln ('');
          Writeln ('Impossible to modify your user name because ',sResul,' isn''t valid.');
          Writeln ('');
          Writeln ('');
          Writeln ('');
          Exit;
       END;

    Writeln ('You are ',Get_User_Full_Name);

    Set_User_Name ('GATES Bill@pi.boss@MICROSOFT'+#0);

    Set_User_Name (GetTrueUser(sParam));
    Writeln ('Now, you are ',Get_User_Full_Name);

END.

{ -------------------------- cut here ------------------------------------ }
{ Second Sample program }

USES
   Crt, Dos, Banyan;

BEGIN

   ClrScr;

   Writeln ('');
   Writeln ('');

   IF bBANYAN_Installed THEN
      BEGIN

         Writeln ('BANYAN Interrupt number         : ',Byte2Hex(Get_Banyan_Int_NO),'h');
         Writeln ('My name (actual logged in user) : ',Get_User_Full_Name);

         Send_A_Msg_To_Me ('Hi, this is a test');

       END
    ELSE
       Writeln ('BANYAN network Not loaded');

END.
