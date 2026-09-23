' Constants
CONST ScreenWidth = 640
CONST ScreenHeight = 200
CONST LineColor = 2
CONST PenUpColor = 14
CONST PenDownColor = 5
CONST TextColor = 15

EscKey$ = CHR$(27)
UpKey$ = CHR$(0) + CHR$(72)
DownKey$ = CHR$(0) + CHR$(80)
LeftKey$ = CHR$(0) + CHR$(75)
RightKey$ = CHR$(0) + CHR$(77)

' Variables
isPenDown = 0
px = ScreenWidth / 2
py = ScreenHeight / 2
a$ = EscKey$

' Setup screen and display instructions
SCREEN 8, , 0, 3
CLS

COLOR TextColor
PRINT "Press escape to exit"
PRINT "Use arrow keys to move the cursor"
PRINT "Press space bar to toggle pen"

' Copy instructions to drawing
PCOPY 0, 1

DO
    ' Get keyboard input as ASCII character
    a$ = INKEY$
    SELECT CASE a$
    CASE UpKey$
        py = py - 1
    CASE DownKey$
        py = py + 1
    CASE LeftKey$
        px = px - 1
    CASE RightKey$
        px = px + 1
    CASE " "
        IF isPenDown = 1 THEN
            isPenDown = 0
        ELSE
            isPenDown = 1
        END IF
    END SELECT
    ' Copy drawing to active video page
    PCOPY 1, 0
    CursorColor = PenUpColor
    IF isPenDown = 1 THEN
        COLOR LineColor
        PSET (px, py)
        ' Copy updated drawing to drawing page
        PCOPY 0, 1
        CursorColor = PenDownColor
    END IF
    COLOR CursorColor
    PSET (px, py)
    ' Display active video page on screen
    PCOPY 0, 3
LOOP UNTIL a$ = EscKey$
' Reset color
COLOR 15

