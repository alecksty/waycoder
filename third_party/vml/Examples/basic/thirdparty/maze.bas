DECLARE SUB fadeout ()
DECLARE SUB fadein ()
DECLARE SUB waitkey ()
DECLARE SUB help ()
DECLARE FUNCTION addzeros$ (n!)
DECLARE SUB editor.editmaze (thefilename$)
DECLARE SUB editor ()
DECLARE SUB playmaze ()
'standard grey block
DATA 015,015,015,015,015,015,015,015,015,008
DATA 015,007,007,007,007,007,007,007,007,008
DATA 015,007,007,007,007,007,007,007,007,008
DATA 015,007,007,007,007,007,007,007,007,008
DATA 015,007,007,007,007,007,007,007,007,008
DATA 015,007,007,007,007,007,007,007,007,008
DATA 015,007,007,007,007,007,007,007,007,008
DATA 015,007,007,007,007,007,007,007,007,008
DATA 015,007,007,007,007,007,007,007,007,008
DATA 015,008,008,008,008,008,008,008,008,008
'left arrow
DATA 015,015,015,015,015,015,015,015,015,008
DATA 015,007,007,007,007,007,007,007,007,008
DATA 015,007,007,007,006,007,007,007,007,008
DATA 015,007,007,006,007,007,007,007,007,008
DATA 015,007,006,006,006,006,006,006,007,008
DATA 015,007,007,006,007,007,007,007,007,008
DATA 015,007,007,007,006,007,007,007,007,008
DATA 015,007,007,007,007,007,007,007,007,008
DATA 015,007,007,007,007,007,007,007,007,008
DATA 015,008,008,008,008,008,008,008,008,008
'right arrow
DATA 015,015,015,015,015,015,015,015,015,008
DATA 015,007,007,007,007,007,007,007,007,008
DATA 015,007,007,007,007,006,007,007,007,008
DATA 015,007,007,007,007,007,006,007,007,008
DATA 015,007,006,006,006,006,006,006,007,008
DATA 015,007,007,007,007,007,006,007,007,008
DATA 015,007,007,007,007,006,007,007,007,008
DATA 015,007,007,007,007,007,007,007,007,008
DATA 015,007,007,007,007,007,007,007,007,008
DATA 015,008,008,008,008,008,008,008,008,008
'finish block
DATA 015,000,015,000,015,000,015,000,015,000
DATA 000,015,000,015,000,015,000,015,000,015
DATA 015,000,015,000,015,000,015,000,015,000
DATA 000,015,000,015,000,015,000,015,000,015
DATA 015,000,015,000,015,000,015,000,015,000
DATA 000,015,000,015,000,015,000,015,000,015
DATA 015,000,015,000,015,000,015,000,015,000
DATA 000,015,000,015,000,015,000,015,000,015
DATA 015,000,015,000,015,000,015,000,015,000
DATA 000,015,000,015,000,015,000,015,000,015

'color data (color 1-15)
DATA 00,00,42
DATA 00,42,00
DATA 00,42,42
DATA 42,00,00
DATA 42,00,42
DATA 42,21,00
DATA 42,42,42
DATA 21,21,21
DATA 21,21,62
DATA 21,62,21
DATA 21,62,62
DATA 62,21,21
DATA 62,21,62
DATA 62,62,21
DATA 62,62,62
CLS
SCREEN 13
FOR dy = 1 TO 10
     FOR dx = 1 TO 10
          READ dc
          PSET (dx, dy), dc
     NEXT dx
NEXT dy
DIM SHARED block(25)
GET (1, 1)-(10, 10), block
CLS
SCREEN 13
FOR dy = 1 TO 10
     FOR dx = 1 TO 10
          READ dc
          PSET (dx, dy), dc
     NEXT dx
NEXT dy
DIM SHARED leftarr(25)
GET (1, 1)-(10, 10), leftarr

CLS
SCREEN 13
FOR dy = 1 TO 10
     FOR dx = 1 TO 10
          READ dc
          PSET (dx, dy), dc
     NEXT dx
NEXT dy
DIM SHARED rightarr(25)
GET (1, 1)-(10, 10), rightarr
CLS
SCREEN 13
FOR dy = 1 TO 10
     FOR dx = 1 TO 10
          READ dc
          PSET (dx, dy), dc
     NEXT dx
NEXT dy
DIM SHARED finishblock(25)
GET (1, 1)-(10, 10), finishblock

DIM SHARED colors(15, 3) AS INTEGER
FOR a = 1 TO 15
     FOR b = 1 TO 3
          READ colors(a, b)
     NEXT b
NEXT a
CLS
thebegin:
fadeout
CLS
SCREEN 13
FOR x = 6 TO 310 STEP 10
     FOR y = 1 TO 190 STEP 10
          PUT (x, y), block
     NEXT y
NEXT x
FOR x = 116 TO 196 STEP 10
     PUT (x, 11), block
     PUT (x, 21), block
NEXT x
COLOR 10
LOCATE 3, 19
PRINT "MAZE"
LINE (116, 11)-(205, 30), 14, B
FOR x = 96 TO 216 STEP 10
     FOR y = 61 TO 151 STEP 10
          PUT (x, y), block
     NEXT y
NEXT x
LINE (96, 61)-(225, 160), 13, B
LOCATE 10, 19
PRINT "PLAY" 'menuoption 1
LOCATE 13, 16
PRINT "MAZE EDITOR"  'menuoption 2
LOCATE 16, 19
PRINT "HELP"          'menuoption 3
LOCATE 19, 19
PRINT "EXIT"          'menuoption 4
'menu navigation stuff
seloption = 1   'var holding the selected menu option
DIM linevals(4)  AS INTEGER
linevals(1) = 10
linevals(2) = 13
linevals(3) = 16
linevals(4) = 19
fadein
DO
     FOR a = 1 TO 4
          LOCATE linevals(a), 14
          PRINT " "
          LOCATE linevals(a), 28
          PRINT " "
          NEXT a
     COLOR 12
     LOCATE linevals(seloption), 14: PRINT "["
     LOCATE linevals(seloption), 28: PRINT "]"
     
     DO
          key$ = INKEY$
     LOOP UNTIL key$ <> ""
     SELECT CASE key$
          '72,80,75,77 up down left right
          CASE CHR$(0) + CHR$(72)
               IF seloption = 1 THEN
                    seloption = 4
               ELSE
                    seloption = seloption - 1
               END IF
          CASE CHR$(0) + CHR$(80)
               IF seloption = 4 THEN
                    seloption = 1
               ELSE
                    seloption = seloption + 1
               END IF
          CASE CHR$(0) + CHR$(75)
               IF seloption = 1 THEN
                    seloption = 4
               ELSE
                    seloption = seloption - 1
               END IF
          CASE CHR$(0) + CHR$(77)
               IF seloption = 4 THEN
                    seloption = 1
               ELSE
                    seloption = seloption + 1
               END IF
          CASE CHR$(13)
               SELECT CASE seloption
                    CASE 1
                         playmaze
                         GOTO thebegin
                    CASE 2
                         editor
                         GOTO thebegin
                    CASE 3
                         help
                         GOTO thebegin
                    CASE 4
                         fadeout
                         END
               END SELECT
          CASE CHR$(27)
               fadeout
               END
     END SELECT
LOOP

FUNCTION addzeros$ (n)
     strn$ = RTRIM$(LTRIM$(STR$(n)))
     SELECT CASE LEN(strn$)
          CASE 1
               strn$ = "00" + strn$
          CASE 2
               strn$ = "0" + strn$
          CASE ELSE
               strn$ = strn$
     END SELECT
     addzeros$ = strn$
END FUNCTION

SUB editor
     DIM linevals(3) AS INTEGER
begin.of.editor:
     fadeout
     CLS
     SCREEN 13
     FOR x = 6 TO 310 STEP 10
          FOR y = 1 TO 190 STEP 10
               PUT (x, y), block
          NEXT y
     NEXT x
     FOR x = 106 TO 206 STEP 10
          PUT (x, 11), block
          PUT (x, 21), block
     NEXT x
     COLOR 10
     LOCATE 3, 16
     PRINT "MAZE EDITOR"
     LINE (106, 11)-(215, 30), 14, B
     FOR x = 96 TO 216 STEP 10
          FOR y = 61 TO 131 STEP 10
               PUT (x, y), block
          NEXT y
     NEXT x
     LINE (96, 61)-(225, 140), 13, B
     LOCATE 10, 17: PRINT "NEW MAZE"
     LOCATE 13, 17: PRINT "EDIT MAZE"
     LOCATE 16, 19: PRINT "BACK"
     fadein
     seloption = 1
     linevals(1) = 10
     linevals(2) = 13
     linevals(3) = 16
     DO
          FOR a = 1 TO 3
               LOCATE linevals(a), 14
               PRINT " "
               LOCATE linevals(a), 28
               PRINT " "
          NEXT a
          COLOR 12
          LOCATE linevals(seloption), 14: PRINT "["
          LOCATE linevals(seloption), 28: PRINT "]"
          DO
               key$ = INKEY$
          LOOP UNTIL key$ <> ""
          SELECT CASE key$
               '72,80,75,77 up down left right
               CASE CHR$(0) + CHR$(72)
                    IF seloption = 1 THEN
                         seloption = 3
                    ELSE
                         seloption = seloption - 1
                    END IF
               CASE CHR$(0) + CHR$(80)
                    IF seloption = 3 THEN
                         seloption = 1
                    ELSE
                         seloption = seloption + 1
                    END IF
               CASE CHR$(0) + CHR$(75)
                    IF seloption = 1 THEN
                         seloption = 3
                    ELSE
                         seloption = seloption - 1
                    END IF
               CASE CHR$(0) + CHR$(77)
                    IF seloption = 3 THEN
                         seloption = 1
                    ELSE
                         seloption = seloption + 1
                    END IF
               CASE CHR$(13)
                    SELECT CASE seloption
                         CASE 1
                              'newmaze
                              GOSUB editor.newmaze

                              GOTO begin.of.editor
                         CASE 2
                              'editmaze
                              GOSUB editor.editexistingmaze
                              GOTO begin.of.editor
                         CASE 3 'back
                              EXIT SUB
                    END SELECT
               CASE CHR$(27)
                    EXIT SUB
          END SELECT
     LOOP
editor.newmaze:    '< < < < < < < < < < < - MAAKT NIEUWE MAZE
     fadeout
     CLS
     SCREEN 13
     FOR x = 6 TO 310 STEP 10
          FOR y = 1 TO 190 STEP 10
               PUT (x, y), block
          NEXT y
     NEXT x
     FOR x = 16 TO 296 STEP 10
          PUT (x, 11), block
          PUT (x, 21), block
     NEXT x
     COLOR 10
     LOCATE 3, 4
     PRINT "NAME YOUR MAZE (MAX 16 CHARACTERS)"
     LINE (16, 11)-(305, 30), 14, B
     FOR x = 96 TO 216 STEP 10
          PUT (x, 71), block
     NEXT x
     LOCATE 10, 13
     fadein
     INPUT "", mazname$
     IF LEN(mazname$) > 16 THEN mazname$ = LEFT$(mazname$, 16)

     fadeout
     CLS
     SCREEN 13
     FOR x = 6 TO 310 STEP 10
          FOR y = 1 TO 190 STEP 10
               PUT (x, y), block
          NEXT y
     NEXT x
     FOR x = 16 TO 296 STEP 10
          PUT (x, 11), block
          PUT (x, 21), block
     NEXT x
     COLOR 10
     LOCATE 3, 4
     PRINT "FILENAME FOR MAZE? (EXT. IS ADDED)"
     LINE (16, 11)-(305, 30), 14, B
     FOR x = 96 TO 216 STEP 10
          PUT (x, 71), block
     NEXT x
     LOCATE 10, 13
     fadein
     INPUT "", mazfilename$
     IF LEN(mazfilename$) > 8 THEN mazname$ = LEFT$(mazname$, 8)
     mazfilename$ = mazfilename$ + ".maz"
     OPEN mazfilename$ FOR OUTPUT AS #1
     PRINT #1, mazname$
     PRINT #1, "<startcoords>"
     PRINT #1, "1, 1"
     PRINT #1, "<endcoords>"
     PRINT #1, "31, 19"
     PRINT #1, "<wall>"
     PRINT #1, "015,015,015,015,015,015,015,015,015,008"
     PRINT #1, "015,007,007,007,007,007,007,007,007,008"
     PRINT #1, "015,007,007,007,007,007,007,007,007,008"
     PRINT #1, "015,007,007,007,007,007,007,007,007,008"
     PRINT #1, "015,007,007,007,007,007,007,007,007,008"
     PRINT #1, "015,007,007,007,007,007,007,007,007,008"
     PRINT #1, "015,007,007,007,007,007,007,007,007,008"
     PRINT #1, "015,007,007,007,007,007,007,007,007,008"
     PRINT #1, "015,007,007,007,007,007,007,007,007,008"
     PRINT #1, "015,008,008,008,008,008,008,008,008,008"
     PRINT #1, "<character>"
     PRINT #1, "000,000,000,000,000,000,000,000,000,000"
     PRINT #1, "000,000,000,000,015,015,000,000,000,000"
     PRINT #1, "000,000,000,015,007,007,015,000,000,000"
     PRINT #1, "000,000,000,015,007,007,015,000,000,000"
     PRINT #1, "000,000,000,000,015,015,000,000,000,000"
     PRINT #1, "000,000,000,007,007,007,007,000,000,000"
     PRINT #1, "000,000,015,000,015,015,000,015,000,000"
     PRINT #1, "000,000,000,000,015,015,000,000,000,000"
     PRINT #1, "000,000,000,007,015,015,007,000,000,000"
     PRINT #1, "000,000,007,015,008,008,015,007,000,000"
     PRINT #1, "<wallcoordinates>"
     CLOSE (1)
     OPEN "mazes.dat" FOR APPEND AS #9
     WRITE #9, mazfilename$
     editor.editmaze (mazfilename$)'< < < < << < < < < - Calls the main editor to edit the file
     RETURN
editor.editexistingmaze:
     CLOSE
     a = 0
     OPEN "mazes.dat" FOR INPUT AS #2
     DO
          INPUT #2, filename$
          a = a + 1
     LOOP UNTIL EOF(2)
     CLOSE (2)
     DIM mazelist(a, 2)  AS STRING
     OPEN "mazes.dat" FOR INPUT AS #2
     nrmazes = a
     IF a <> 0 THEN
          FOR b = 1 TO a
               INPUT #2, filename$
               OPEN filename$ FOR INPUT AS #3
               mazelist(b, 1) = filename$
               INPUT #3, mazelist(b, 2)
               CLOSE (3)
          NEXT b
     END IF
     CLOSE (2)
     'here comes some nice layout stuff, like beginmenu
     fadeout
     CLS
     SCREEN 13
     FOR x = 6 TO 310 STEP 10
          FOR y = 1 TO 190 STEP 10
               PUT (x, y), block
          NEXT y
     NEXT x
     FOR x = 66 TO 246 STEP 10
          PUT (x, 11), block
          PUT (x, 21), block
     NEXT x
     COLOR 10
     LOCATE 3, 11
     PRINT "SELECT A MAZE TO EDIT"
     LINE (66, 11)-(255, 30), 14, B
     FOR x = 96 TO 216 STEP 10
          PUT (x, 71), block
     NEXT x
     PUT (86, 71), block
     PUT (86, 71), leftarr
     PUT (226, 71), block
     PUT (226, 71), rightarr
     FOR x = 126 TO 186 STEP 10
          PUT (x, 101), block
          PUT (x, 111), block
          PUT (x, 121), block
          PUT (x, 131), block
     NEXT x
     selmaze = 1
     LOCATE 10, 13: PRINT mazelist(selmaze, 2)
     LINE (126, 101)-(195, 140), 17, BF
     OPEN mazelist(selmaze, 1) FOR INPUT AS #4
     FOR l = 1 TO 28
          LINE INPUT #4, l$
     NEXT l
     DO
          INPUT #4, a, b
          PSET (a + 144, b + 110), 7
     LOOP UNTIL EOF(4)
     CLOSE (4)
     fadein
     DO
          DO
               key$ = INKEY$
          LOOP UNTIL key$ <> ""
          SELECT CASE key$
               CASE CHR$(0) + CHR$(75) 'left arrow
                    IF selmaze = 1 THEN
                         selmaze = nrmazes
                    ELSE
                         selmaze = selmaze - 1
                    END IF
               CASE CHR$(0) + CHR$(77) 'right arrow
                    IF selmaze = nrmazes THEN
                         selmaze = 1
                    ELSE
                         selmaze = selmaze + 1
                    END IF
               CASE CHR$(13)
                    mazefile$ = mazelist(selmaze, 1)
                    GOSUB editit
                    GOTO begin.of.editor
               CASE CHR$(27)
                    RETURN
          END SELECT
          LOCATE 10, 13: PRINT "                "
          LOCATE 10, 13: PRINT mazelist(selmaze, 2)
          LINE (126, 101)-(195, 140), 17, BF
          OPEN mazelist(selmaze, 1) FOR INPUT AS #4
          FOR l = 1 TO 28
               LINE INPUT #4, l$
          NEXT l
          DO UNTIL EOF(4)
               INPUT #4, a, b
               PSET (a + 144, b + 110), 7
          LOOP
          CLOSE (4)
     LOOP
editit:
editor.editmaze (mazefile$)
RETURN
END SUB

SUB editor.editmaze (thefilename$)
'let's make clear what variables we're going to use here:
'thefilename$ is already used, it holds the filename of the maze
'beginx is the x-component of the begincoordinates (integer)
'beginy is the y-component of the begincoordinates (integer)
'endx is the x-component of the endcoordinates (integer)
'endy is the y-component of the endcoordinates (integer)
'wallsprite(10,10) will hold the pixel color data for the wall sprite (integer)
'charsprite(10,10) will hold the pixel color data for the character sprite (integer)
'mazecoords(31,19) will hold the wall data (1 if wall at coord, 0 if no wall) (integer)
'selcolor is the current selected color in sprite editing (integer)
'curx is the x-component of the cursor (in 10x10) (integer)
'cury is the y-component of the cursor (in 10x10) (integer)
'mazx is the x-component of the cursor (in 31x19) (integer)
'mazy is the y-component of the cursor (in 31x19) (integer)
'Now that that's said, let's go and declare them, then load them with data
DIM beginx, beginy, endx, endy, curx, cury, mazx, mazy, selcolor AS INTEGER
DIM wallsprite(10, 10) AS INTEGER
DIM charsprite(10, 10) AS INTEGER
DIM mazecoords(31, 19) AS INTEGER
OPEN thefilename$ FOR INPUT AS #7
INPUT #7, b$
INPUT #7, b$
INPUT #7, beginx, beginy
INPUT #7, b$
INPUT #7, endx, endy
INPUT #7, b$
'load the wall sprite
FOR y = 1 TO 10
     FOR x = 1 TO 10
          INPUT #7, wallsprite(x, y)
     NEXT x
NEXT y
INPUT #7, b$
'load the character sprite
FOR y = 1 TO 10
     FOR x = 1 TO 10
          INPUT #7, charsprite(x, y)
     NEXT x
NEXT y
INPUT #7, b$
'set 0 in every coordinate of mazecoords (clear it)
FOR a = 1 TO 31
     FOR b = 1 TO 19
          mazecoords(a, b) = 0
     NEXT b
NEXT a
'load the wall data into mazecoords
DO UNTIL EOF(7)
     INPUT #7, a, b
     mazecoords(a, b) = 1
LOOP
'everything is loaded in our vars, let's close the file
CLOSE (7)
'Now we can start the stuff for the user:
     DIM linevals(4)  AS INTEGER
     linevals(1) = 10
     linevals(2) = 13
     linevals(3) = 16
     linevals(4) = 19
menuagain:
     fadeout
     CLS
     SCREEN 13
     FOR x = 6 TO 310 STEP 10
          FOR y = 1 TO 190 STEP 10
               PUT (x, y), block
          NEXT y
     NEXT x
     FOR x = 116 TO 196 STEP 10
          PUT (x, 11), block
          PUT (x, 21), block
     NEXT x
     COLOR 10
     LOCATE 3, 19
     PRINT "EDIT:"
     LINE (116, 11)-(205, 30), 14, B
     FOR x = 96 TO 216 STEP 10
          FOR y = 61 TO 151 STEP 10
               PUT (x, y), block
          NEXT y
     NEXT x
     LINE (96, 61)-(225, 160), 13, B
     LOCATE 10, 16
     PRINT "WALL IMAGE" 'menuoption 1
     LOCATE 13, 16
     PRINT "CHAR IMAGE"  'menuoption 2
     LOCATE 16, 16
     PRINT "MAZE COORDS"          'menuoption 3
     LOCATE 19, 19
     PRINT "BACK"          'menuoption 4
     'menu navigation stuff
     seloption = 1   'var holding the selected menu option
     fadein
     DO
          FOR a = 1 TO 4
               LOCATE linevals(a), 14
               PRINT " "
               LOCATE linevals(a), 28
               PRINT " "
          NEXT a
          COLOR 12
          LOCATE linevals(seloption), 14: PRINT "["
          LOCATE linevals(seloption), 28: PRINT "]"
          DO
               key$ = INKEY$
          LOOP UNTIL key$ <> ""
          SELECT CASE key$
               '72,80,75,77 up down left right
               CASE CHR$(0) + CHR$(72)
                    IF seloption = 1 THEN
                         seloption = 4
                    ELSE
                         seloption = seloption - 1
                    END IF
               CASE CHR$(0) + CHR$(80)
                    IF seloption = 4 THEN
                         seloption = 1
                    ELSE
                         seloption = seloption + 1
                    END IF
               CASE CHR$(0) + CHR$(75)
                    IF seloption = 1 THEN
                         seloption = 4
                    ELSE
                         seloption = seloption - 1
                    END IF
               CASE CHR$(0) + CHR$(77)
                    IF seloption = 4 THEN
                         seloption = 1
                    ELSE
                         seloption = seloption + 1
                    END IF
               CASE CHR$(13)
                    SELECT CASE seloption
                         CASE 1
                              GOSUB editwall
                              GOTO menuagain
                         CASE 2
                              GOSUB editcharacter
                              GOTO menuagain
                         CASE 3
                              GOSUB editmaze
                              GOTO menuagain
                         CASE 4
                              EXIT SUB
                    END SELECT
               CASE CHR$(27)
                    EXIT SUB
          END SELECT
     LOOP
'--------------------------------------------<WALL SPRITE EDITOR>
editwall:
     fadeout
     CLS
     SCREEN 13
     FOR x = 6 TO 310 STEP 10
          FOR y = 1 TO 190 STEP 10
               PUT (x, y), block
          NEXT y
     NEXT x
     FOR x = 86 TO 226 STEP 10
          PUT (x, 11), block
          PUT (x, 21), block
     NEXT x
     COLOR 10
     LOCATE 3, 14
     PRINT "EDIT WALL IMAGE"
     LINE (86, 11)-(235, 30), 14, B
     FOR x = 26 TO 286 STEP 10
          FOR y = 151 TO 171 STEP 10
               PUT (x, y), block
          NEXT y
     NEXT x
     'LINE (26, 151)-(295, 180), 13, B
     LOCATE 20, 5: PRINT "S=Save ESC=Back C=Next color"
     LOCATE 21, 5: PRINT "V=Prev. color Arrows=Move"
     LOCATE 22, 5: PRINT "ENTER=apply new color"
     FOR x = 126 TO 186 STEP 10
          FOR y = 61 TO 101 STEP 10
               PUT (x, y), block
          NEXT y
     NEXT x
     PUT (86, 61), block
     PUT (96, 61), block
     PUT (86, 71), block
     PUT (96, 71), block
     LINE (154, 79)-(167, 92), 15, B
     curx = 1: cury = 1: selcolor = 0
     fadein
     DO
          LINE (156, 81)-(165, 90), 0, BF
          LINE (87, 62)-(104, 79), selcolor, BF
          FOR x = 1 TO 10
               FOR y = 1 TO 10
                    PSET (155 + x, 80 + y), wallsprite(x, y)
               NEXT y
          NEXT x
          PSET (155 + curx, 80 + cury), selcolor
          DO
               key$ = INKEY$
          LOOP UNTIL key$ <> ""
          SELECT CASE key$
               CASE CHR$(0) + CHR$(72)   'up arrow
                    IF cury > 1 THEN
                         curx = curx
                         cury = cury - 1
                    ELSE
                         curx = curx
                         cury = cury
                    END IF
               CASE CHR$(0) + CHR$(80)   'down arrow
                    IF cury < 10 THEN
                         curx = curx
                         cury = cury + 1
                    ELSE
                         curx = curx
                         cury = cury
                    END IF
               CASE CHR$(0) + CHR$(75)   'left arrow
                    IF curx > 1 THEN
                         curx = curx - 1
                         cury = cury
                    ELSE
                         curx = curx
                         cury = cury
                    END IF
               CASE CHR$(0) + CHR$(77)   'right arrow
                    IF curx < 10 THEN
                         curx = curx + 1
                         cury = cury
                    ELSE
                         curx = curx
                         cury = cury
                    END IF
               CASE CHR$(13)            'enter
                         wallsprite(curx, cury) = selcolor
               CASE "c"
                    IF selcolor = 255 THEN
                         selcolor = 0
                    ELSE
                         selcolor = selcolor + 1
                    END IF
               CASE "C"
                    IF selcolor = 255 THEN
                         selcolor = 0
                    ELSE
                         selcolor = selcolor + 1
                    END IF
               CASE "v"
                    IF selcolor = 0 THEN
                         selcolor = 255
                    ELSE
                         selcolor = selcolor - 1
                    END IF
               CASE "V"
                    IF selcolor = 0 THEN
                         selcolor = 255
                    ELSE
                         selcolor = selcolor - 1
                    END IF
               CASE "s"
                    GOSUB saveroutine
               CASE "S"
                    GOSUB saveroutine
               CASE CHR$(27)
                    RETURN
          END SELECT
     LOOP
'--------------------------------------------</WALL SPRITE EDITOR>
'--------------------------------------------<CHARACTER SPRITE EDITOR>
editcharacter:
fadeout
CLS
     SCREEN 13
     FOR x = 6 TO 310 STEP 10
          FOR y = 1 TO 190 STEP 10
               PUT (x, y), block
          NEXT y
     NEXT x
     FOR x = 86 TO 226 STEP 10
          PUT (x, 11), block
          PUT (x, 21), block
     NEXT x
     COLOR 10
     LOCATE 3, 14
     PRINT "EDIT CHAR IMAGE"
     LINE (86, 11)-(235, 30), 14, B
     FOR x = 26 TO 286 STEP 10
          FOR y = 151 TO 171 STEP 10
               PUT (x, y), block
          NEXT y
     NEXT x
     'LINE (26, 151)-(295, 180), 13, B
     LOCATE 20, 5: PRINT "S=Save ESC=Back C=Next color"
     LOCATE 21, 5: PRINT "V=Prev. color Arrows=Move"
     LOCATE 22, 5: PRINT "ENTER=apply new color"
     FOR x = 126 TO 186 STEP 10
          FOR y = 61 TO 101 STEP 10
               PUT (x, y), block
          NEXT y
     NEXT x
     PUT (86, 61), block
     PUT (96, 61), block
     PUT (86, 71), block
     PUT (96, 71), block
     LINE (154, 79)-(167, 92), 15, B
     curx = 1: cury = 1: selcolor = 0
     fadein
     DO
          LINE (156, 81)-(165, 90), 0, BF
          LINE (87, 62)-(104, 79), selcolor, BF
          FOR x = 1 TO 10
               FOR y = 1 TO 10
                    PSET (155 + x, 80 + y), charsprite(x, y)
               NEXT y
          NEXT x
          PSET (155 + curx, 80 + cury), selcolor
          DO
               key$ = INKEY$
          LOOP UNTIL key$ <> ""
          SELECT CASE key$
               CASE CHR$(0) + CHR$(72)   'up arrow
                    IF cury > 1 THEN
                         curx = curx
                         cury = cury - 1
                    ELSE
                         curx = curx
                         cury = cury
                    END IF
               CASE CHR$(0) + CHR$(80)   'down arrow
                    IF cury < 10 THEN
                         curx = curx
                         cury = cury + 1
                    ELSE
                         curx = curx
                         cury = cury
                    END IF
               CASE CHR$(0) + CHR$(75)   'left arrow
                    IF curx > 1 THEN
                         curx = curx - 1
                         cury = cury
                    ELSE
                         curx = curx
                         cury = cury
                    END IF
               CASE CHR$(0) + CHR$(77)   'right arrow
                    IF curx < 10 THEN
                         curx = curx + 1
                         cury = cury
                    ELSE
                         curx = curx
                         cury = cury
                    END IF
               CASE CHR$(13)            'enter
                    charsprite(curx, cury) = selcolor
               CASE "c"
                    IF selcolor = 255 THEN
                         selcolor = 0
                    ELSE
                         selcolor = selcolor + 1
                    END IF
               CASE "C"
                    IF selcolor = 255 THEN
                         selcolor = 0
                    ELSE
                         selcolor = selcolor + 1
                    END IF
               CASE "v"
                    IF selcolor = 0 THEN
                         selcolor = 255
                    ELSE
                         selcolor = selcolor - 1
                    END IF
               CASE "V"
                    IF selcolor = 0 THEN
                         selcolor = 255
                    ELSE
                         selcolor = selcolor - 1
                    END IF
               CASE "s"
                    GOSUB saveroutine
               CASE "S"
                    GOSUB saveroutine
               CASE CHR$(27)
                    RETURN
          END SELECT
     LOOP
'--------------------------------------------</CHARACTER SPRITE EDITOR>
'--------------------------------------------<MAZE ITSELF EDITOR>
editmaze:
fadeout
CLS
SCREEN 13
mazx = 1
mazy = 1
DO
LINE (1, 1)-(319, 195), 0, BF
FOR x = 1 TO 31
     FOR y = 1 TO 19
          LINE (((x - 1) * 10) + 1, ((y - 1) * 10) + 1)-(((x - 1) * 10) + 11, ((y - 1) * 10) + 11), 8, B
          IF mazecoords(x, y) = 1 THEN
               LINE (((x - 1) * 10) + 1, ((y - 1) * 10) + 1)-(((x - 1) * 10) + 11, ((y - 1) * 10) + 11), 14, BF
          END IF
     NEXT y
NEXT x
LINE (((beginx - 1) * 10) + 1, ((beginy - 1) * 10) + 1)-(((beginx - 1) * 10) + 11, ((beginy - 1) * 10) + 11), 10, BF
LINE (((endx - 1) * 10) + 1, ((endy - 1) * 10) + 1)-(((endx - 1) * 10) + 11, ((endy - 1) * 10) + 11), 12, BF
LINE (((mazx - 1) * 10) + 1, ((mazy - 1) * 10) + 1)-(((mazx - 1) * 10) + 11, ((mazy - 1) * 10) + 11), 13, B
     fadein
     DO
          key$ = INKEY$
     LOOP UNTIL key$ <> ""
     SELECT CASE key$
               CASE CHR$(0) + CHR$(72)   'up arrow
                    IF mazy > 1 THEN
                         mazx = mazx
                         mazy = mazy - 1
                    ELSE
                         mazx = mazx
                         mazy = mazy
                    END IF
               CASE CHR$(0) + CHR$(80)   'down arrow
                    IF mazy < 19 THEN
                         mazx = mazx
                         mazy = mazy + 1
                    ELSE
                         mazx = mazx
                         mazy = mazy
                    END IF
               CASE CHR$(0) + CHR$(75)   'left arrow
                    IF mazx > 1 THEN
                         mazx = mazx - 1
                         mazy = mazy
                    ELSE
                         mazx = mazx
                         mazy = mazy
                    END IF
               CASE CHR$(0) + CHR$(77)   'right arrow
                    IF mazx < 31 THEN
                         mazx = mazx + 1
                         mazy = mazy
                    ELSE
                         mazx = mazx
                         mazy = mazy
                    END IF
               CASE CHR$(13)            'enter
                    IF mazecoords(mazx, mazy) = 1 THEN
                         mazecoords(mazx, mazy) = 0
                    ELSE
                         IF mazx = beginx AND mazy = beginy THEN
                         ELSEIF mazx = endx AND mazy = endy THEN
                         ELSE
                              mazecoords(mazx, mazy) = 1
                         END IF
                    END IF
               CASE "b"
                    IF mazecoords(mazx, mazy) <> 1 THEN
                         beginx = mazx
                         beginy = mazy
                    END IF
               CASE "B"
                    IF mazecoords(mazx, mazy) <> 1 THEN
                         beginx = mazx
                         beginy = mazy
                    END IF
               CASE "e"
                    IF mazecoords(mazx, mazy) <> 1 THEN
                         endx = mazx
                         endy = mazy
                    END IF
               CASE "E"
                    IF mazecoords(mazx, mazy) <> 1 THEN
                         endx = mazx
                         endy = mazy
                    END IF
               CASE "s"
                    GOSUB saveroutine
               CASE "S"
                    GOSUB saveroutine
               CASE CHR$(27)
               RETURN
     END SELECT
LOOP
'--------------------------------------------</MAZE ITSELF EDITOR>
'--------------------------------------------<MAZEFILE SAVER>
saveroutine:
OPEN thefilename$ FOR INPUT AS #8
INPUT #8, thamazename$
CLOSE (8)
OPEN thefilename$ FOR OUTPUT AS #8
PRINT #8, thamazename$
PRINT #8, "<startcoords>"
PRINT #8, STR$(beginx) + "," + STR$(beginy)
PRINT #8, "<endcoords>"
PRINT #8, STR$(endx) + "," + STR$(endy)
PRINT #8, "<wall>"
FOR a = 1 TO 10
myvar$ = ""
     FOR b = 1 TO 10
          thenumber = wallsprite(b, a)
          myvar$ = myvar$ + addzeros$(thenumber) + ","
     NEXT b
     myvar$ = LEFT$(myvar$, 39)
     PRINT #8, myvar$
NEXT a
PRINT #8, "<character>"
FOR a = 1 TO 10
myvar$ = ""
     FOR b = 1 TO 10
          thenumber = charsprite(b, a)
          myvar$ = myvar$ + addzeros$(thenumber) + ","
     NEXT b
     myvar$ = LEFT$(myvar$, 39)
     PRINT #8, myvar$
NEXT a
PRINT #8, "<wall coordinates>"
FOR a = 1 TO 19
     FOR b = 1 TO 31
          IF mazecoords(b, a) = 1 THEN
               PRINT #8, LTRIM$(RTRIM$(STR$(b))) + "," + LTRIM$(RTRIM$(STR$(a)))
          END IF
     NEXT b
NEXT a
CLOSE (8)
RETURN
'--------------------------------------------</MAZEFILE SAVER>
END SUB

SUB fadein
DIM tempcolors(15, 3) AS INTEGER
FOR a = 1 TO 15
     FOR b = 1 TO 3
          tempcolors(a, b) = 0
     NEXT b
NEXT a

DO
b = 15 * 3
FOR a = 1 TO 15
     FOR c = 1 TO 3
          tempcolors(a, c) = tempcolors(a, c) + 1
          IF tempcolors(a, c) = colors(a, c) + 1 THEN
               tempcolors(a, c) = colors(a, c)
               b = b - 1
          END IF
     NEXT c
NEXT a
FOR a = 1 TO 15
PALETTE a, (tempcolors(a, 1) + (tempcolors(a, 2) * 256) + (tempcolors(a, 3) * 65536))
NEXT a

LOOP UNTIL b = 0

END SUB

SUB fadeout
DIM tempcolors(15, 3) AS INTEGER
FOR a = 1 TO 15
     FOR b = 1 TO 3
          tempcolors(a, b) = colors(a, b)
     NEXT b
NEXT a

DO
b = 15 * 3
FOR a = 1 TO 15
     FOR c = 1 TO 3
          tempcolors(a, c) = tempcolors(a, c) - 1
          IF tempcolors(a, c) = -1 THEN
               tempcolors(a, c) = 0
               b = b - 1
          END IF
     NEXT c
     NEXT a
FOR a = 1 TO 15
PALETTE a, (tempcolors(a, 1) + (tempcolors(a, 2) * 256) + (tempcolors(a, 3) * 65536))
NEXT a

LOOP UNTIL b = 0
END SUB

SUB help
DIM linevals(3)  AS INTEGER
thebegin1:
fadeout
CLS
SCREEN 13
FOR x = 6 TO 310 STEP 10
     FOR y = 1 TO 190 STEP 10
          PUT (x, y), block
     NEXT y
NEXT x
FOR x = 116 TO 196 STEP 10
     PUT (x, 11), block
     PUT (x, 21), block
NEXT x
COLOR 10
LOCATE 3, 19
PRINT "HELP"
LINE (116, 11)-(205, 30), 14, B
FOR x = 96 TO 216 STEP 10
     FOR y = 61 TO 131 STEP 10
          PUT (x, y), block
     NEXT y
NEXT x
LINE (96, 61)-(225, 140), 13, B
LOCATE 10, 16
PRINT "HOW TO PLAY" 'menuoption 1
LOCATE 13, 16
PRINT "MAZE EDITOR"  'menuoption 2
LOCATE 16, 19
PRINT "BACK"          'menuoption 3
'menu navigation stuff
seloption = 1   'var holding the selected menu option

linevals(1) = 10
linevals(2) = 13
linevals(3) = 16
fadein
DO
     FOR a = 1 TO 3
          LOCATE linevals(a), 14
          PRINT " "
          LOCATE linevals(a), 28
          PRINT " "
          NEXT a
     COLOR 12
     LOCATE linevals(seloption), 14: PRINT "["
     LOCATE linevals(seloption), 28: PRINT "]"
     DO
          key$ = INKEY$
     LOOP UNTIL key$ <> ""
     SELECT CASE key$
          '72,80,75,77 up down left right
          CASE CHR$(0) + CHR$(72)
               IF seloption = 1 THEN
                    seloption = 3
               ELSE
                    seloption = seloption - 1
               END IF
          CASE CHR$(0) + CHR$(80)
               IF seloption = 3 THEN
                    seloption = 1
               ELSE
                    seloption = seloption + 1
               END IF
          CASE CHR$(0) + CHR$(75)
               IF seloption = 1 THEN
                    seloption = 3
               ELSE
                    seloption = seloption - 1
               END IF
          CASE CHR$(0) + CHR$(77)
               IF seloption = 3 THEN
                    seloption = 1
               ELSE
                    seloption = seloption + 1
               END IF
          CASE CHR$(13)
               SELECT CASE seloption
                    CASE 1
                         GOSUB playhelp
                         GOTO thebegin1
                    CASE 2
                         GOSUB editorhelp
                         GOTO thebegin1
                    CASE 3
                         EXIT SUB
               END SELECT
          CASE CHR$(27)
               EXIT SUB
     END SELECT
LOOP
playhelp:
fadeout
CLS
SCREEN 13
FOR x = 6 TO 310 STEP 10
     FOR y = 1 TO 190 STEP 10
          PUT (x, y), block
     NEXT y
NEXT x
FOR x = 76 TO 236 STEP 10
     PUT (x, 11), block
     PUT (x, 21), block
NEXT x
COLOR 10
LOCATE 3, 12
PRINT "HELP : HOW TO PLAY"
LINE (76, 11)-(245, 30), 14, B
FOR x = 16 TO 296 STEP 10
     FOR y = 41 TO 161 STEP 10
          PUT (x, y), block
     NEXT y
NEXT x
LINE (16, 41)-(305, 170), 13, B

LINE (17, 42)-(304, 169), 0, BF 'use this to clear the window
COLOR 15
LOCATE 7, 4: PRINT "The goal is to get your character"
LOCATE 9, 4: PRINT "to the finish. The finish is a "
LOCATE 11, 4: PRINT "black & white flag. Use the arrow"
LOCATE 13, 4: PRINT "keys to move the character through"
LOCATE 15, 4: PRINT "the maze. You can press ESC at "
LOCATE 17, 4: PRINT "any time you if want to quit during"
LOCATE 19, 4: PRINT "a game. (Press enter to go back.)"
fadein
waitkey
RETURN

editorhelp:
fadeout
CLS
SCREEN 13
FOR x = 6 TO 310 STEP 10
     FOR y = 1 TO 190 STEP 10
          PUT (x, y), block
     NEXT y
NEXT x
FOR x = 76 TO 236 STEP 10
     PUT (x, 11), block
     PUT (x, 21), block
NEXT x
COLOR 10
LOCATE 3, 12
PRINT "HELP : MAZE EDITOR"
LINE (76, 11)-(245, 30), 14, B
FOR x = 16 TO 296 STEP 10
     FOR y = 41 TO 161 STEP 10
          PUT (x, y), block
     NEXT y
NEXT x
LINE (16, 41)-(305, 170), 13, B

LINE (17, 42)-(304, 169), 0, BF 'use this to clear the window
COLOR 15
LOCATE 7, 4: PRINT "You can create your own mazes"
LOCATE 9, 4: PRINT "using the maze editor. Here's "
LOCATE 11, 4: PRINT "some quick help."
LOCATE 13, 4: PRINT "Editing the wall image and the"
LOCATE 15, 4: PRINT "character image works the same"
LOCATE 17, 4: PRINT "way."
LOCATE 19, 4: PRINT "(Press ENTER to continue)"
fadein
DO
key$ = INKEY$
LOOP UNTIL key$ <> ""
SELECT CASE key$
CASE CHR$(13)
GOTO verder1
CASE CHR$(27)
RETURN
END SELECT
verder1:
LINE (17, 42)-(304, 169), 0, BF 'use this to clear the window
COLOR 15
LOCATE 7, 4: PRINT "Controls for image editing"
LOCATE 9, 4: PRINT "Arrow keys move the cursor."
LOCATE 11, 4: PRINT "C chooses next color."
LOCATE 13, 4: PRINT "V chooses previous color."
LOCATE 15, 4: PRINT "S saves your work "
LOCATE 16, 4: PRINT "ESC goes back to the menu."
LOCATE 17, 4: PRINT "Make sure to save before ESC!"
LOCATE 19, 4: PRINT "(Press ENTER to continue)"
DO
key$ = INKEY$
LOOP UNTIL key$ <> ""
SELECT CASE key$
CASE CHR$(13)
GOTO verder2
CASE CHR$(27)
RETURN
END SELECT
verder2:
LINE (17, 42)-(304, 169), 0, BF 'use this to clear the window
LOCATE 7, 4: PRINT "Controls for editing the maze"
LOCATE 9, 4: PRINT "Arrow keys move the purple cursor."
LOCATE 11, 4: PRINT "ENTER toggles wall"
LOCATE 13, 4: PRINT "B places the beginning point."
LOCATE 15, 4: PRINT "E places the finish."
LOCATE 16, 4: PRINT "S saves your work."
LOCATE 17, 4: PRINT "ESC returns to menu."
LOCATE 18, 4: PRINT "Make sure to save before ESC!"
LOCATE 19, 4: PRINT "Press any key to go back."
waitkey
RETURN
     





END SUB

SUB playmaze
     CLOSE
     a = 0
     OPEN "mazes.dat" FOR INPUT AS #2
     DO
          INPUT #2, filename$
          a = a + 1
     LOOP UNTIL EOF(2)
     CLOSE (2)
     DIM mazelist(a, 2)  AS STRING
     OPEN "mazes.dat" FOR INPUT AS #2
     nrmazes = a
     IF a <> 0 THEN
          FOR b = 1 TO a
               INPUT #2, filename$
               OPEN filename$ FOR INPUT AS #3
               mazelist(b, 1) = filename$
               INPUT #3, mazelist(b, 2)
               CLOSE (3)
          NEXT b
     END IF
     CLOSE (2)
     'here comes some nice layout stuff, like beginmenu
     fadeout
     CLS
     SCREEN 13
     FOR x = 6 TO 310 STEP 10
          FOR y = 1 TO 190 STEP 10
               PUT (x, y), block
          NEXT y
     NEXT x
     FOR x = 96 TO 216 STEP 10
          PUT (x, 11), block
          PUT (x, 21), block
     NEXT x
     COLOR 10
     LOCATE 3, 15
     PRINT "SELECT A MAZE"
     LINE (96, 11)-(225, 30), 14, B
     FOR x = 96 TO 216 STEP 10
          PUT (x, 71), block
     NEXT x
     PUT (86, 71), block
     PUT (86, 71), leftarr
     PUT (226, 71), block
     PUT (226, 71), rightarr
     FOR x = 126 TO 186 STEP 10
          PUT (x, 101), block
          PUT (x, 111), block
          PUT (x, 121), block
          PUT (x, 131), block
     NEXT x
     selmaze = 1
     LOCATE 10, 13: PRINT mazelist(selmaze, 2)
     
     OPEN mazelist(selmaze, 1) FOR INPUT AS #4
     FOR l = 1 TO 28
          LINE INPUT #4, l$
     NEXT l
     LINE (126, 101)-(195, 140), 17, BF
     DO
          INPUT #4, a, b
          PSET (a + 144, b + 110), 7
     LOOP UNTIL EOF(4)
     CLOSE (4)
     fadein
     DO
          DO
               key$ = INKEY$
          LOOP UNTIL key$ <> ""
          SELECT CASE key$
               CASE CHR$(0) + CHR$(75) 'left arrow
                    IF selmaze = 1 THEN
                         selmaze = nrmazes
                    ELSE
                         selmaze = selmaze - 1
                    END IF
               CASE CHR$(0) + CHR$(77) 'right arrow
                    IF selmaze = nrmazes THEN
                         selmaze = 1
                    ELSE
                         selmaze = selmaze + 1
                    END IF
               CASE CHR$(13)
                    mazefile$ = mazelist(selmaze, 1)
                    GOTO playthemaze
               CASE CHR$(27)
                    EXIT SUB
          END SELECT
          LOCATE 10, 13: PRINT "                "
          LOCATE 10, 13: PRINT mazelist(selmaze, 2)
          OPEN mazelist(selmaze, 1) FOR INPUT AS #4
          FOR l = 1 TO 28
               LINE INPUT #4, l$
          NEXT l
          LINE (126, 101)-(195, 140), 17, BF
          DO WHILE NOT EOF(4)
               INPUT #4, a
               INPUT #4, b
               PSET (a + 144, b + 110), 7
               'END IF
          LOOP
          CLOSE (4)
     LOOP

    
playthemaze:
fadeout
     OPEN mazefile$ FOR INPUT AS #1
     INPUT #1, b$
     INPUT #1, b$
     DIM charx, chary, prevcharx, prevchary, endx, endy AS INTEGER'x-coord and y-coord for character and previous character location
     INPUT #1, charx, chary
     INPUT #1, b$
     INPUT #1, endx, endy
     INPUT #1, b$
     prevcharx = charx: prevchary = chary
     SCREEN 13      'Set the screenmode to 13 (320x200)
     '-wall
     FOR daty = 1 TO 10   'y-coord 1 to 10
          FOR datx = 1 TO 10  'x-coord 1 to 10
               INPUT #1, datc      'read the color for the current pixel from the listings
               PSET (datx, daty), datc   'set a point in the color just read
          NEXT datx         ' continue for x
     NEXT daty              'continue for y
     DIM wall(25)           'make a variable to store the sprite in
     GET (1, 1)-(10, 10), wall   'store the graphics in the variable
     '-character    (see info up here, it's all the same)
     INPUT #1, b$
     FOR daty = 1 TO 10
          FOR datx = 1 TO 10
               INPUT #1, datc
               PSET (datx, daty), datc
          NEXT datx
     NEXT daty
     DIM char(25)
     GET (1, 1)-(10, 10), char
     DIM maze(32, 20) AS INTEGER 'declare an array to store the maze data in.
     FOR a = 1 TO 32
          FOR b = 1 TO 19
               maze(a, b) = 0  'fill the maze array with zero's (no maze data)
          NEXT b
     NEXT a
     INPUT #1, b$
     '----Now, let's load a maze file, here 'c:\maze.dat' is used
     CLS
     fadeout
     CLS          'clear the screen
     DO WHILE NOT EOF(1)                                'start a loop
          INPUT #1, x, y                'read x and y coordinates from opened file
          maze(x, y) = 1                'fill the maze array, 1 if wall
          PUT (((x - 1) * 10) + 1, ((y - 1) * 10) + 1), wall 'using the read coord, place a wall sprite on the screen
     LOOP 'UNTIL EOF(1)                  'keep doing this until end of file
     CLOSE (1)                          'close the file

     '---declare the variables for the character position
     'keyactions:
     
     PUT (((charx - 1) * 10) + 1, ((chary - 1) * 10) + 1), char
     PUT (((endx - 1) * 10) + 1, ((endy - 1) * 10) + 1), finishblock
     DIM arrowpressed AS INTEGER
     fadein
     DO
          DO
               key$ = INKEY$
          LOOP UNTIL key$ <> ""
          SELECT CASE key$
               CASE CHR$(0) + CHR$(72) 'up arrow
                    arrowpressed = 1
                    IF maze(charx, chary - 1) = 0 AND chary <> 1 THEN
                         prevcharx = charx: prevchary = chary
                         charx = charx
                         chary = chary - 1
                    ELSE
                         charx = charx
                         chary = chary
                         prevcharx = charx: prevchary = chary
                    END IF
               CASE CHR$(0) + CHR$(80) 'down arrow
                    arrowpressed = 1
                    IF maze(charx, chary + 1) = 0 AND chary <> 19 THEN
                         prevcharx = charx: prevchary = chary
                         charx = charx
                         chary = chary + 1
                    ELSE
                         charx = charx
                         chary = chary
                         prevcharx = charx: prevchary = chary
                    END IF
               CASE CHR$(0) + CHR$(75) 'left arrow
                    arrowpressed = 1
                    IF maze(charx - 1, chary) = 0 AND charx <> 1 THEN
                         prevcharx = charx: prevchary = chary
                         charx = charx - 1
                         chary = chary
                    ELSE
                         charx = charx
                         chary = chary
                         prevcharx = charx: prevchary = chary
                    END IF
               CASE CHR$(0) + CHR$(77) 'right arrow
                    arrowpressed = 1
                    IF maze(charx + 1, chary) = 0 AND charx <> 31 THEN
                         prevcharx = charx: prevchary = chary
                         charx = charx + 1
                         chary = chary
                    ELSE
                         charx = charx
                         chary = chary
                         prevcharx = charx: prevchary = chary
                    END IF
                         CASE CHR$(27)
                    EXIT SUB
               CASE ELSE
                    arrowpressed = 0
          END SELECT
          IF arrowpressed = 1 THEN
               PUT (((charx - 1) * 10) + 1, ((chary - 1) * 10) + 1), char
               PUT (((prevcharx - 1) * 10) + 1, ((prevchary - 1) * 10) + 1), char
               IF charx = endx AND chary = endy THEN
                    CLS
                    LOCATE 5, 3
                    PRINT "Congratulations!"
                    LOCATE 6, 3
                    PRINT "You completed the maze!"
                    waitkey
                    EXIT DO
               END IF
          END IF
     LOOP
END SUB

SUB waitkey
DO: LOOP UNTIL INKEY$ <> ""
END SUB

