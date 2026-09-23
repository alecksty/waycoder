DECLARE SUB andersrum ()
DECLARE SUB keinegrav ()
SCREEN 13
DIM SHARED Felda(1 TO 841)  AS INTEGER
DIM SHARED Feldb(1 TO 598)  AS INTEGER
DIM SHARED snd AS INTEGER
j = 16
FOR i = 1 TO 25
PALETTE j, 0
j = j + 1
NEXT i
c = 31
r = 1
FOR i = 1 TO 16
CIRCLE (100, 100), r, c
r = r + 1
c = c - 1
NEXT i
PSET (100, 100), 15
c = 31
r = 1
FOR i = 1 TO 16
CIRCLE (100, 101), r, c
r = r + 1
c = c - 1
NEXT i
r = 13
FOR i = 1 TO 15
CIRCLE (103, 104), r, 0
r = r + 1
NEXT i
PSET (92, 91), 0
PSET (99, 91), 0
PSET (96, 91), 0
PSET (107, 91), 0
PSET (111, 94), 0
PSET (112, 96), 0
PSET (94, 96), 0
PSET (95, 94), 0
PSET (93, 93), 0
PSET (92, 95), 0
PSET (90, 93), 0
PSET (89, 94), 0
PSET (90, 96), 0
PSET (87, 95), 0
PSET (87, 98), 0
PSET (87, 101), 0
PSET (87, 107), 0
PSET (94, 112), 0
GET (90, 89)-(116, 118), Felda
GET (90, 93)-(116, 114), Feldb
CLS
PALETTE
snd = 0
20
x = 1
y = 10
a = 10
wx = 1
wa = 1
PLAY "T180L64O0"
DO
Esc$ = INKEY$
'tim = TIMER     ' this is for the fixed time delay described below
PUT (x, y), Felda, PSET
x = x + wx
a = a + wa
y = (a / 5) ^ 2 + 10
IF y <= 10 THEN wa = 1
IF y >= 168 THEN wa = -1: IF snd = 1 THEN PLAY "g"
IF x >= 290 THEN wx = -1: IF snd = 1 THEN PLAY "g"
IF x <= 1 THEN wx = 1: IF snd = 1 THEN PLAY "g"
IF Esc$ = "s" AND snd = 0 THEN snd = 1: GOTO 40
IF Esc$ = "s" AND snd = 1 THEN snd = 0: GOTO 40
40
IF Esc$ = "g" THEN andersrum: GOTO 20
' this is a test for a fixed time delay for all CPU speeds but its to slow
'DO
'zeit = TIMER - tim
'IF zeit > .01 THEN fr = 1
'LOOP UNTIL fr = 1
'fr = 0
LOOP UNTIL Esc$ = CHR$(27)
CLS
SCREEN 9
SCREEN 0
END

SUB andersrum
CLS
x = 1
y = 170
a = 10
wx = 1
wa = 1
PLAY "T180"
PLAY "L64"
PLAY "O0"
DO
Esc$ = INKEY$
PUT (x, y), Felda, PSET
x = x + wx
a = a + wa
y = 180 - ((a / 5) ^ 2 + 10)
IF y >= 170 THEN wa = 1
IF y <= 1 THEN wa = -1: IF snd = 1 THEN PLAY "g"
IF x >= 290 THEN wx = -1: IF snd = 1 THEN PLAY "g"
IF x <= 1 THEN wx = 1: IF snd = 1 THEN PLAY "g"
IF Esc$ = "s" AND snd = 0 THEN snd = 1: GOTO 50
IF Esc$ = "s" AND snd = 1 THEN snd = 0: GOTO 50
50
IF Esc$ = "g" THEN keinegrav: GOTO 10
LOOP UNTIL Esc$ = CHR$(27)
CLS : SCREEN 9: SCREEN 0
END
10
END SUB

SUB keinegrav
CLS
x = 1
y = 1
wx = 1
wy = 1
PLAY "T180"
PLAY "L64"
PLAY "O0"
DO
Esc$ = INKEY$
PUT (x, y), Feldb, PSET
x = x + wx
y = y + wy
IF x >= 293 THEN wx = -1: IF snd = 1 THEN PLAY "g"
IF y >= 178 THEN wy = -1: IF snd = 1 THEN PLAY "g"
IF x <= 0 THEN wx = 1: IF snd = 1 THEN PLAY "g"
IF y <= 0 THEN wy = 1: IF snd = 1 THEN PLAY "g"
IF Esc$ = "g" THEN GOTO 30
IF Esc$ = "s" AND snd = 0 THEN snd = 1: GOTO 60
IF Esc$ = "s" AND snd = 1 THEN snd = 0: GOTO 60
60
LOOP UNTIL Esc$ = CHR$(27)
CLS : SCREEN 9: SCREEN 0
END
30
CLS
END SUB

