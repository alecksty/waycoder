(*
  Category: SWAG Title: TEXT WINDOWING ROUTINES
  Original name: 0030.PAS
  Description: Cool effects in Textmode
  Author: AVONTURE CHRISTOPHE
  Date: 11-29-96  08:22
*)

{
                  =======================================

                         CRT-DEMO (c) AVC Software
                               Cardware

                   Souce in Pascal to  show how we can
                   manipulate  EGA/VGA  register   for
                   obtain  some  cool  effects in text
                   mode 80*25.


                  =======================================

   The purpose of this program is to show how we can make some cools effect
   in text mode by manipulating the EGA/VGA registers.

   I have writte almost all procedure in assembler for the quick effect and
   for the creation of OBJ file if you want.

   Some code cames from severall books.

   Sorry for the French comments.




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

{$G+}

Uses Crt;

Var I  : Byte;
   Ch  : Char;
   Delai : Byte;

Procedure Wait_Retrace;

{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：轮询 VGA 输入状态端口
  3DAh 的 bit 3（垂直回扫标志）—— 先等它变高（进入回扫）、再等它变低
  （回扫结束），也就是「等两帧的垂直回扫都过去」再往下走，用来让下面的
  屏幕寄存器改写与显示器刷新的节拍对齐、避免撕裂。
  这是**端口 I/O**，本平台没有 VGA 端口、也没有垂直回扫这回事
  ⇒ 以空过程代替。原汇编保留在下方注释里备查。 }
(*
Asm

    Mov Dx, 3dah

  @Wait1:
    In   Al, Dx
    Test Al, 8h
    Jnz  @Wait1

   @Wait2:
    In   Al, Dx
    Test Al, 8h
    Jnz  @Wait2

End;
*)

Begin

End;

Procedure Wait_In_Retrace;

Begin

   { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：与上面 Wait_Retrace
     后半段相同 —— 轮询端口 3DAh 的 bit 3，等垂直回扫标志变低（回扫结束）。
     端口 I/O 在本平台不存在 ⇒ 整段注释掉（过程体因此为空）。
     原汇编保留在下方注释里备查。 }
(*
   Asm
           Mov Dx, 3dah
   @Wait2:
           In al, dx
           Test Al, 8h
           Jnz @Wait2
   End;
*)

End;

Procedure Deplace_par_Pixel;

{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：往属性控制器（ATC，端口
  3C0h）写两个字节 —— 先写索引 110011b（=33h：「像素平移 / Pel Panning」
  寄存器），再写值 10100b（=14h）：bit 5 置 0 表示不允许 CPU 访问调色板内存，
  低 4 位是水平细调量，于是整屏可以做**逐像素**的横向平移（而不只是按字符移）。
  这是**端口 I/O**，本平台没有 VGA 属性控制器、没有像素平移寄存器
  ⇒ 以空过程代替。原汇编保留在下方注释里备查。 }
(*
Asm

 { Je m'adresse au contrôleur d'attributs (Attribute Controller : ATC)
   qui s'adresse via le port 3c0h.  Je lui envoi la valeur 110011b
   qui lui indique que je ne désire pas que le CPU accède à la mémoire
   de la palette et que le déplacement doit se faire au pixel près }

      Mov Dx, 3c0h
      Mov Al, 110011b
      Out Dx, Al

      Mov Al, 10100b
      Out Dx, Al

End;
*)

Begin

End;


Procedure Smooth_Scrolling (Sens : Byte);

{ Sens = 0 => défilement vers le bas
         1 => Défilement vers le haut  (1 ou tout autre) }

Var Tempo : Word;

Begin

  If Sens = 0 then Tempo := 1 Else Tempo := 9;

  Repeat

     { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：每一拍把 CRTC 的
       寄存器 08h（Preset Row Scan，端口 3D4h 写索引 / 3D5h 读写）加 1、
       按 15 回绕（And 15）—— 它决定每一行从字符的哪一条扫描线开始显示，
       连续递增就得到**逐像素的平滑竖向滚动**（Smooth Scrolling），而不是
       按整行跳。回绕到 0 是为了避免扫到字符最后一条线时出杂点。
       这是**端口 I/O**，本平台没有 CRTC 寄存器 ⇒ 整段注释掉（循环体里
       还剩 Delay(Tempo)，不会变成空循环）。原汇编保留在下方注释里备查。 }
(*
     Asm
          Mov Al, 8
          Mov Dx, 3d4h
          Out Dx, Al
          Mov Dx, 3d5h
          In  Al, Dx
          Inc Al
          And Al, 15
          Out Dx, Al


     { Les bits 0 à 4 représente le Initial Row Adress : indiquent au CRTC
       la ligne de déclenchement du retour du balayage vertical, normalement
       0.  Si on augmente ce paramètre, le CRTC commence par une ligne située
       plus bas, ce qui déplace le contenu de l'écran vers le haut.
       Ce registre fonctionne de la même façon en mode texte qu'en mode
       graphique, de sorte que grâce à lui on peut réaliser un défilement
       continu vertical (qu'on appelle Smooth Scrolling).

       Si la ligne de départ est égale à 15, cela signifie que je vais traiter
       le dernier pixel du caractère, et l'apparition de parasites se fera
       sentir.  Je réinitalise donc à 0 grâce à un AND 15. }

     End;
*)

     Delay (Tempo);

  Until KeyPressed;

  Ch := ReadKey; If Ch = #0 then Ch := ReadKey;

  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：把 CRTC 寄存器 08h
    （Preset Row Scan，上一段一直在加 1 的那个）写回 0，也就是**复位还原**
    平滑滚动、以免影响后面的显示。端口 I/O ⇒ 整段注释掉。
    原汇编保留在下方注释里备查。 }
(*
  Asm
      xor al, al
      mov dx, 3d5h
      out dx, al
  End;
*)


  { Réinitialisation à sa valeur d'origine pour éviter les mauvaises surprises}

End;

Procedure EGA2VGA (OnOff : Byte);

{ 0 pour que les caractères aient 9 pixels ou 1 pour qu'il en aient 8 }

{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：改写定序器（Sequencer）的
  「时钟模式」寄存器（索引 01h：端口 3C4h 写索引、3C5h 读写数据）—— 先读出原值，
  再把 bit 0（8/9 点时钟选择）与 OnOff 做 **OR** 后写回：OnOff=1 时置上该位，
  每个字符占 8 像素宽（VGA 字形）；OnOff=0 时 OR 0 保持原值不变（注释里说的
  「9 像素」，EGA 字形）。
  这是**端口 I/O**，本平台没有定序器寄存器、字符宽度由字体本身决定
  ⇒ 以空过程代替。原汇编保留在下方注释里备查。 }
(*
Asm

          Mov Al, 1
          Mov Dx, 3c4h
          Out Dx, Al
          Mov Dx, 3c5h
          In  Al, Dx
          Or  Al, OnOff
          Out Dx, Al

End;
*)

Begin

End;


Procedure Minimize_Char;

Begin

    Repeat

       { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：改写 CRTC 寄存器 09h
         （Maximum Scan Line，端口 3D4h 写索引 / 3D5h 读写）—— 读出原值、加 1、
         与 14 相与（把最低位清掉，只保留偶数）、再把 bit 0 置 1，于是字符的
         扫描线数在 1..15 之间**逐条变小**，屏幕上的字符被一行行「压扁」，
         做出缩小动画。端口 I/O ⇒ 整段注释掉（循环体里还剩 Delay(100)）。
         原汇编保留在下方注释里备查。 }
(*
       Asm

          Mov Al, 9
          Mov Dx, 3d4h
          Out Dx, Al
          Mov Dx, 3d5h
          In  Al, Dx
          Inc Al
          And Al, 14
          Or  Al, 1
          Out Dx, Al

       End;
*)

       Delay (100);

    Until KeyPressed;

    Ch := ReadKey; If Ch = #0 then Ch := ReadKey;

    { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：往 3D5h 写 15，
      把 CRTC 寄存器（靠上一段循环留下的索引 09h —— 这里没有再写索引）
      恢复成 15 条扫描线，也就是还原成满高的正常字符。端口 I/O ⇒ 注释掉。
      原汇编保留在下方注释里备查。 }
(*
    Asm
           Mov Dx, 3d5h
           Mov Al, 15
           Out Dx, Al
    End;
*)

End;

Procedure Mazimize_Char;

Begin

    Repeat

       Delay (100);

       { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：改写 CRTC 寄存器 09h
         （Maximum Scan Line，端口 3D4h 写索引 / 3D5h 读写）—— 读出原值、加 1、
         再把 bit 7 置 1；bit 7 打开「行倍增」，让每条字符扫描线显示两遍，
         于是字符被「拉长」，做出放大动画。端口 I/O ⇒ 整段注释掉。
         原汇编保留在下方注释里备查。 }
(*
       Asm

            Mov Al, 9
            Mov Dx, 3d4h
            Out Dx, Al
            Mov Dx, 3d5h
            In  Al, Dx
            Inc Al
            Or  Al, 128
            Out Dx, Al

       End;
*)

    Until KeyPressed;

    Ch := ReadKey; If Ch = #0 then Ch := ReadKey;

    { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：往 CRTC 数据端口写 15，
      把寄存器 09h 恢复成 15 条扫描线（并关掉上一段打开的行倍增），还原成满高字符。
      端口 I/O ⇒ 注释掉。原汇编保留在下方注释里备查。 }
(*
    Asm Mov Al, 15; Out Dx, Al; End;
*)

End;

Procedure Dedouble;

Begin

  Port[$3d4] := $09;              { Je m'adresse au registre 08h du CRTC }

  Port[$3d5] := (Port[$3d5] or 128) ;

  { Positionne à 1 le bit 7 qui divise le rythme d'horloe vertical (clock
    rate) par deux, ce qui a pour effet de dédoubler l'affichage de chaque
    ligne.  Prévu pour la génération des modes 200 lignes dans une résolution
    physique de 400 lignes. }

  Ch := ReadKey; If Ch = #0 then Ch := ReadKey;

  Port[$3d5] := (Port[$3d5] and not 128) ;

  { Remet à 0 le bit 7 }

End;

Procedure Deplacement_Gauche;

{ Cette procédure décale tout l'écran vers la gauche pixel par pixel.

  Le début de la ligne qui commence à disparaitre fait place au début de la
  ligne suivante.  Par conséquent, la ligne n° 25 (non visible) doit contenir
  le même texte que la ligne 24 pour une question d'esthétique. }

Var Nbr : Word;

Begin

   Nbr := 0;

   Repeat

      Nbr := (Nbr + 1) mod 80;

      { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：改写 CRTC 的
        「起始地址」寄存器对（0Ch = 高位、0Dh = 低位，端口 3D4h 写索引、
        3D5h 写数据），值取自 Pascal 变量 Nbr：每拍把起始地址加 1，
        屏幕内容于是整体**向左移动一个像素**（每行开头消失的部分由下一行
        的行首补上，所以原注释要求第 25 行与第 24 行内容一致才好看）。
        端口 I/O ⇒ 整段注释掉（循环体里还剩 Delay(Delai)）。
        原汇编保留在下方注释里备查。 }
(*
      Asm

          Mov Dx, 3d4h                { Je m'adresse au port du CRTC : 3d4h }
          Mov Al, 0ch

          { Registre 0ch : Linear Starting Address : définit l'offset à
            l'intérieur de la mémoire d'écran où le CRTC commence à lire
            les données graphiques }

          Mov Ah, Byte Ptr Nbr + 1
          Out Dx, Ax

          { En manipulant cette adresse, on peut provoquer un défilement
            horizontal en incrémentant sans cesse cette valeur de une
            position supérieure }

          Mov Al, 0dh
          Mov Ah, Byte Ptr Nbr
          Out Dx, Ax

      End;
*)

      Delay (Delai);

  Until KeyPressed;

  Ch := ReadKey; If Ch = #0 then Ch := ReadKey;

End;

Procedure Split (Row, Mouv : Byte);

{ ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：改 CRTC（端口 3D4h 写索引、
  3D5h 读写数据）做**分屏**与**平滑竖向滚动**：
  ① 寄存器 07h（Overflow）清 bit 4、寄存器 09h（Maximum Scan Line）清 bit 6 ——
     这两位是「Line Compare 起始行」的最高两位（第 9、10 位），先按 Row 重算
     （Row*2 的高位）再 OR 回去；
  ② 寄存器 18h（Line Compare 低 8 位）写 Row*2 —— CRTC 扫到这一行就复位到
     起始地址，于是屏幕从 Row 处分成上下两半、下半屏显示另一个页面的内容；
  ③ 若 Mouv <> 0，再把寄存器 08h（Preset Row Scan）加 1 并按 15 回绕，
     让下半屏的分割线可以做**逐像素**的上下微调。
  这是**端口 I/O**（CRTC 寄存器），本平台没有这种文本模式分屏硬件
  ⇒ 以空过程代替。原汇编保留在下方注释里备查。 }
(*
Asm

          Mov Bl, Row
          Xor Bh, Bh
          Shl Bx, 1

          Mov Cx, BX

          Mov Dx, 3d4h
          Mov Al, 07h
          Out Dx, Al

          Inc Dx
          In Al, Dx
          And Al, 11101111b

          Shr Cx, 4
          And Cl, 16
          Or Al, Cl
          Out Dx, Al

          Dec Dx
          Mov Al, 09h
          Out Dx, Al
          Inc Dx
          In Al, Dx
          And Al, 10111111b

          Shr Bl, 3
          And Bl, 64
          Or Al, Bl
          Out Dx, Al

          Dec Dx
          Mov Al, 18h
          Mov Ah, Row
          Shl Ah, 1
          Out Dx, Ax

          Cmp Mouv, 0
          Je @Fin

          Mov Al, 8
          Mov Dx, 3d4h
          Out Dx, Al
          Mov Dx, 3d5h
          In  Al, Dx
          Inc Al
          And Al, 15
          Out Dx, Al

@Fin:

End;
*)

Begin

End;

Procedure CopyPage (Source, Cible : Byte);

{ Copy le contenu de la page Source dans la page Cible.  Permet de sauver
  une page avant sa modification et de la restaurer le moment venu}

Type VRam = Array  [0..7,0..4095] of byte;
     Vptr = ^VRam;

Var RVideo   : Vptr;
    i        : Word;

Begin

     RVideo := ptr ($B800,$0000);

     Move (RVideo^[Source, 0],RVideo^[Cible, 0], 4096);

End;

Procedure Superpose (fond, active, buffer : byte);

{ Affiche en superposition la page écran active sur la page écran fond.
  La page écran buffer servira de zone de stockage temporaire.

  Ces deux pages écran doivent être préparées à l'avance }

Begin

  CopyPage(Fond,   Buffer);
  CopyPage(Active, Fond);
  CopyPage(Buffer, Active);

  { Change the active page }

  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 BIOS int 10h 的
    AH=05h 功能（设置当前显示页）——AL=Active，把参数 Active 那一页设为屏幕上
    正在显示的那一页（前面三个 CopyPage 已经把页内容换好，这里只是切过去）。
    这是 **BIOS 中断**，本平台没有显示页这回事 ⇒ 整段注释掉。
    原汇编保留在下方注释里备查。 }
(*
  Asm

    Mov Ah, 05h
    Mov Al, Active

    Int 10h

  End;
*)

  Repeat

      For I := 200 downto 50 do Begin

        Wait_Retrace;
        Split(I,I Xor 1);
        Wait_Retrace;
        Delay (Delai);

        If KeyPressed then Delai := 0;

      End;

      For i := 50 to 200 do Begin

        Wait_Retrace;
        Split(I,I xor 1);
        Wait_Retrace;
        Delay (Delai);

        If KeyPressed then Delai := 0;

      End;

  Until KeyPressed;

  Ch := ReadKey; If Ch = #0 then Ch := ReadKey;

End;

var tempo, row : byte;

Begin

  Delai := 15;

  TextAttr:= 19;

  For I := 0 to 25 Do
     Writeln ('Wow!!!  What a cool effect!!!   Ha! Ha! Ha! Yes, it''s possible in text mode!!!  ');


  EGA2VGA(1);

  Smooth_Scrolling(0);
  Smooth_Scrolling(1);

  Dedouble;

  Minimize_Char;
  Mazimize_Char;

  Deplace_par_Pixel;

  Deplacement_Gauche;

  TextAttr := 45;

  For I := 0 to 24 Do
     Writeln ('Hello to you, Man.   This demo has been coded by AVONTURE Christophe');

  SuperPose (0,1,2);

  EGA2VGA(0);

  { Pour réinitialiser correctement les paramètres du port 3d4h }

  { ⚠ 本平台不支持内嵌汇编（Assembler）。原汇编语义：调 BIOS int 10h 置
    AX=0003h，把显示切回文本模式 3 —— 前面把 CRTC/ATC 寄存器改得乱七八糟，
    这里用它一次性还原成标准的 80*25 彩色文本模式。
    这是 **BIOS 中断**，本平台没有显示模式寄存器 ⇒ 整段注释掉。
    原汇编保留在下方注释里备查。 }
(*
  Asm
     Mov Ax, 0003h
     Int 10h
  End;
*)

  ClrScr;

  Writeln ('');
  Writeln ('Cette petite démo n''a aucune prétention si ce n''est celle de prouver');
  Writeln ('que le mode communément appelé texte n''est pas aussi idiot que cela.');
  Writeln ('');
  Writeln ('');
  Writeln ('Oui, on peut faire de jolies choses tout en restant dans le mode texte!!!');
  Writeln ('');


End.
