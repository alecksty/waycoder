{

   Example of the vertical menu unit


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

Uses Crt, Menu;

Const
    TextColor = 15;   { Color Fore and Back color }
    BackColor = 01;

Var Choix, I : Byte;
    Lig      : String;

Begin

   { Draw the screen }

   TextAttr := TextColor + (BackColor Shl 4);

   ClrScr;

   Cadre  (02, 01, 78, 05, TextColor, BackColor, Double);

   WriteStrXy (04, 03, TextColor, BackColor, 'Vertical Unit Menu Sample.');
   WriteStrXy (04, 04, TextColor, BackColor, 'Try the '+
               'horizontal Menu Unit of AVONTURE Christophe -see in the SWAG- ');

   Cadre (41, 07, 78, 20, TextColor, BackColor, Double);

   WriteStrXy (43, 09, TextColor, BackColor,
              'This is a little example to show');

   WriteStrXy (43, 10, TextColor, BackColor, 'you the feature of the menu unit.');

   WriteStrXy (43, 13, TextColor, BackColor,
               'This menu unit has been written by');

   WriteStrXy (50, 14, TextColor, BackColor, 'AVONTURE Christophe');

   Cadre (02, 07, 38, 15, TextColor, BackColor, Double);

   WriteStrXy (00, 24, TextColor, BackColor, ' ');

   { Set the menu option.  The first index is the option and the second, the
     help line
   }

   TMenu [1,1] := 'This is the first option';
   TMenu [1,2] := 'This option can be used in order to ...    :=) ';
   TMenu [2,1] := 'Send a postcard';
   TMenu [2,2] := '... for me?  This idea is very good!  I wait news from you...';
   TMenu [3,1] := 'This unit is great, isn''t it?';
   TMenu [3,2] := 'Happy to see that you can find this unit usefull...';
   TMenu [4,1] := 'Nice to meet you';
   TMenu [4,2] := 'Hello, How are you?';
   TMenu [5,1] := 'Terminate this example';
   TMenu [5,2] := 'You can also use the Escape key';

   { Put the options on the screen: Column, line, option, help.
   }

   Prompt (04, 09, TMenu [1,1], TMenu [1,2]);
   Prompt (04, 10, TMenu [2,1], TMenu [2,2]);
   Prompt (04, 11, TMenu [3,1], TMenu [3,2]);
   Prompt (04, 12, TMenu [4,1], TMenu [4,2]);
   Prompt (04, 13, TMenu [5,1], TMenu [5,2]);

   { Show a little string animation...
   }

   Scroll (25, 01, 79, 'WOW!!!   What a cool vertical menu unit...'+
           '                                                      ');

   Repeat Until KeyPressed; ReadKey;

   { Clear the last line of the screen
   }

   GotoXy (1, 25);
   ClrEol;

   { Process the menu option selection
   }

   Repeat

        { Call the menu handler with three argument :
            the first  is the start column of the first option
            the second is the start line of the first option
            the last   is the number of option in our menu
        }

        Choix := MChoix (4,9,5);

        { Once the user has been pressed on a function key (F1 to F10), the
          escape key or the enter key, then the MChoix function will return
          a numeric value:

              59 to 68 if a function key has been pressed (59 = F1, ...)
              255 if the Escape key has been pressed
              a number from 1 to the option number; so if the user has
                 pressed the enter key on the third option, the return value
                 will be equal to 3.
        }

        Str (Choix, Lig);
        Lig := 'The selected option is : '+ Lig;
        WriteStrXy (43, 17, 15, 01, Lig);

        { I continue until the escape key is pressed or the 'Quit' option has
          been selected (this is the 5 option)
        }

   Until ((Choix = 5) or (Choix = 255));

   ClrScr;

End.
