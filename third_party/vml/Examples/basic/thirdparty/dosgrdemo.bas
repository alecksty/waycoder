DECLARE SUB Delay (TimeLimit!)
DECLARE SUB DrawBoxBorder ()
DECLARE SUB DrawBubble ()
DECLARE SUB DelayLoop (x)
DECLARE SUB ClearKeyboard ()
DECLARE SUB Demo1 ()
DECLARE SUB Demo2 ()
DECLARE SUB Demo3 ()
DECLARE SUB Demo4 ()
DECLARE SUB Demo5 ()
DECLARE SUB Demo6 ()
DECLARE SUB Demo7 ()
DECLARE SUB Demo8 ()
DECLARE SUB Demo9 ()
DECLARE SUB Demo10 ()

'** Cheking the Monitor **
DEF SEG = 0
IF (PEEK(&H410) AND &H30) = &H30 THEN
    COLOR 7, 0: CLS
    PRINT "Sorry! You must have EGA color, or VGA graphics to run GRAPHICS DEMO"
    PRINT
    SYSTEM
END IF
DEF SEG

'** Running Demo only on EGA or VGA **
ON ERROR GOTO GrErr
SCREEN 9
a1$ = "G r a p h i c s   D e m o"
a2$ = "by"
a3$ = "Farhan Ali Qureshi"
a4$ = "Press Enter to Start Demo"
a5$ = "Press Esc to Exit to Dos"

RANDOMIZE TIMER
Menu:
VIEW
CLS
CALL ClearKeyboard
DO
    COLOR INT(RND * 15) + 1, 0: LOCATE 5, (80 - LEN(a1$)) / 2: PRINT a1$;
    COLOR 2, 0: LOCATE 7, (80 - LEN(a2$)) / 2: PRINT a2$;
    COLOR 15, 0: LOCATE 10, (80 - LEN(a3$)) / 2: PRINT a3$;
    COLOR 11, 0: LOCATE 16, (80 - LEN(a4$)) / 2: PRINT a4$;
    COLOR 11, 0: LOCATE 18, (80 - LEN(a5$)) / 2: PRINT a5$;

    key$ = INKEY$
    IF key$ = CHR$(13) THEN GOTO StartDemo
    IF key$ = CHR$(27) THEN GOTO UserEnd
   
    CALL DrawBoxBorder
    CALL DrawBubble
LOOP

StartDemo:
CALL Demo1
CALL Demo2
CALL Demo3
CALL Demo4
CALL Demo5
CALL Demo6
CALL Demo7
CALL Demo8
CALL Demo9
CALL Demo10
GOTO Menu

UserEnd:
ClearKeyboard
SCREEN 0
COLOR 7, 0
CLS
PRINT "GoodBye!"
PRINT
SYSTEM

GrErr:
ClearKeyboard
SCREEN 0
COLOR 7, 0
CLS
PRINT "Graphics error"
PRINT "Error Code: "; ERR
PRINT
SYSTEM

SUB ClearKeyboard

WHILE INKEY$ <> ""
WEND

END SUB

SUB Delay (TimeLimit)

StartTime = TIMER
WHILE TIMER - StartTime < TimeLimit: WEND

END SUB

SUB DelayLoop (x)

FOR i = 1 TO x
NEXT

END SUB

SUB Demo1

VIEW
CLS
ClearKeyboard
a$ = "Press any key to continue"
COLOR 15, 0: LOCATE 25, (80 - LEN(a$)) / 2: PRINT a$;
COLOR 11: LOCATE 25, 1: PRINT "Circle Demo";
VIEW SCREEN (1, 1)-(638, 330), , 14

WHILE INKEY$ = ""
    x = INT(RND * 638)
    y = INT(RND * 330)
    r = INT(RND * 300)
    c = INT(RND * 15)
    CIRCLE (x, y), r, c
WEND

END SUB

SUB Demo10

VIEW
CLS
ClearKeyboard
a$ = "Press any key to continue"
COLOR 15, 0: LOCATE 25, (80 - LEN(a$)) / 2: PRINT a$;
COLOR 11: LOCATE 25, 1: PRINT "Pattern Demo";
VIEW SCREEN (1, 1)-(638, 330), , 14

PatternColor = 1
WHILE INKEY$ = ""
    COLOR PatternColor
    Pattern$ = ""
    FOR x = 1 TO 64
        Pattern$ = Pattern$ + CHR$(INT(RND * 255))
    NEXT
    PAINT (200, 200), Pattern$
    Delay .3
    CLS
    PatternColor = PatternColor + 1
    IF PatternColor = 16 THEN PatternColor = 1
WEND

END SUB

SUB Demo2

VIEW
CLS
ClearKeyboard
a$ = "Press any key to continue"
COLOR 15, 0: LOCATE 25, (80 - LEN(a$)) / 2: PRINT a$;
COLOR 11: LOCATE 25, 1: PRINT "Line Demo";
VIEW SCREEN (1, 1)-(638, 330), , 14

WHILE INKEY$ = ""
    x1 = INT(RND * 638)
    y1 = INT(RND * 330)
    x2 = INT(RND * 638)
    y2 = INT(RND * 330)
    c = INT(RND * 15)
    LINE (x1, y1)-(x2, y2), c
WEND

END SUB

SUB Demo3

VIEW
CLS
ClearKeyboard
a$ = "Press any key to continue"
COLOR 15, 0: LOCATE 25, (80 - LEN(a$)) / 2: PRINT a$;
COLOR 11: LOCATE 25, 1: PRINT "Rectangle Demo";
VIEW SCREEN (1, 1)-(638, 330), , 14

WHILE INKEY$ = ""
    x1 = INT(RND * 638)
    y1 = INT(RND * 330)
    x2 = INT(RND * 638)
    y2 = INT(RND * 330)
    c = INT(RND * 15)
    LINE (x1, y1)-(x2, y2), c, B
WEND

END SUB

SUB Demo4

VIEW
CLS
ClearKeyboard
a$ = "Press any key to continue"
COLOR 15, 0: LOCATE 25, (80 - LEN(a$)) / 2: PRINT a$;
COLOR 11: LOCATE 25, 1: PRINT "Ellipse Demo";
VIEW SCREEN (1, 1)-(638, 330), , 14

WHILE INKEY$ = ""
    x = INT(RND * 638)
    y = INT(RND * 330)
    r = INT(RND * 50)
    c = INT(RND * 15)
    i = INT(RND * 50) + 1
    j = INT(RND * 50) + 1
    CIRCLE (x, y), r, c, , , i / j
WEND

END SUB

SUB Demo5

VIEW
CLS
ClearKeyboard
a$ = "Press any key to continue"
COLOR 15, 0: LOCATE 25, (80 - LEN(a$)) / 2: PRINT a$;
COLOR 11: LOCATE 25, 1: PRINT "Thin Dot Demo";
VIEW SCREEN (1, 1)-(638, 330), , 14

WHILE INKEY$ = ""
    x = INT(RND * 638)
    y = INT(RND * 330)
    c = INT(RND * 15)
    PSET (x, y), c
WEND

END SUB

SUB Demo6

VIEW
CLS
ClearKeyboard
a$ = "Press any key to continue"
COLOR 15, 0: LOCATE 25, (80 - LEN(a$)) / 2: PRINT a$;
COLOR 11: LOCATE 25, 1: PRINT "Thick Dot Demo";
VIEW SCREEN (1, 1)-(638, 330), , 14

WHILE INKEY$ = ""
    x = INT(RND * 638)
    y = INT(RND * 330)
    c = INT(RND * 15)
    PSET (x, y), c
    PSET (x + 1, y), c
    PSET (x, y + 1), c
    PSET (x + 1, y + 1), c
WEND

END SUB

SUB Demo7

'DIM Box%(2000)
DIM Box%(883)
PI = 3.141593

VIEW
CLS
ClearKeyboard
a$ = "Press any key to Continue"
COLOR 15, 0: LOCATE 25, (80 - LEN(a$)) / 2: PRINT a$;
COLOR 11: LOCATE 25, 1: PRINT "Get/Put Image Demo";
VIEW SCREEN (1, 1)-(638, 330), , 14

' Outline of face
CIRCLE (320, 100), 30, 14
CIRCLE (320, 100), 31, 4
CIRCLE (320, 100), 32, 14
' Fill with color
PAINT (320, 100), 2, 14
' Mouth
CIRCLE (321, 109), 10, 11, PI, 0, 100 / 300
CIRCLE (321, 109), 11, 11, PI, 0, 100 / 300
' Left eye
CIRCLE (309, 93), 5, 9
CIRCLE (309, 93), 6, 1
' Right eye
CIRCLE (331, 93), 5, 9
CIRCLE (331, 93), 6, 1

GET (287, 76)-(353, 124), Box%
Delay .29
PUT (287, 76), Box%, XOR

WHILE INKEY$ = ""
    x = INT(RND * (638 - 66))
    y = INT(RND * (330 - 48))
    PUT (x, y), Box%
    Delay .3
    PUT (x, y), Box%, XOR
WEND

END SUB

SUB Demo8

VIEW
CLS
ClearKeyboard
a$ = "Press any key to continue"
COLOR 15, 0: LOCATE 25, (80 - LEN(a$)) / 2: PRINT a$;
COLOR 11: LOCATE 25, 1: PRINT "Solid Line Demo";
VIEW SCREEN (1, 1)-(638, 330), , 14

DO
    FOR x = 1 TO 330
        a = INT(RND * 7) + 9
        LINE (0, x)-(640, x), a
        IF INKEY$ <> "" THEN EXIT DO
    NEXT
    FOR x = 1 TO 638
        a = INT(RND * 7) + 9
        LINE (x, 0)-(x, 640), a
        IF INKEY$ <> "" THEN EXIT DO
    NEXT
LOOP

END SUB

SUB Demo9

VIEW
CLS
ClearKeyboard
a$ = "Press any key to continue"
COLOR 15, 0: LOCATE 25, (80 - LEN(a$)) / 2: PRINT a$;
COLOR 11: LOCATE 25, 1: PRINT "Solid Box Demo";
VIEW SCREEN (1, 1)-(638, 330), , 14

WHILE INKEY$ = ""
    x1 = INT(RND * 638)
    y1 = INT(RND * 330)
    x2 = INT(RND * 638)
    y2 = INT(RND * 330)
    c = INT(RND * 15)
    LINE (x1, y1)-(x2, y2), c, BF
WEND

END SUB

SUB DrawBoxBorder

BoxColor = INT(RND * 15)
x1 = 180: y1 = 41
x2 = 442: y2 = 263
j1 = 160: k1 = 21
j2 = 462: k2 = 283
FOR i = 0 TO 10
    LINE (x1 - i, y1 - i)-(x2 + i, y2 + i), BoxColor, B
    LINE (j1 + i, k1 + i)-(j2 - i, k2 - i), BoxColor, B
    DelayLoop 100
NEXT

END SUB

SUB DrawBubble

x = INT(RND * 640)
y = INT(RND * 320)
CircleColor = INT(RND * 7) + 9
FOR Radius = 1 TO 50
    CIRCLE (x, y), Radius, CircleColor
    DelayLoop 10
NEXT
FOR Radius = 1 TO 50
    CIRCLE (x, y), Radius, 0
NEXT

END SUB

