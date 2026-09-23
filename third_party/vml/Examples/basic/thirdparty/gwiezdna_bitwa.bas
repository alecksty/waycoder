' ==============================================================================
' =                                                                            =
' = Author: Sylwester Wysocki <sw143@wp.pl>                                    =
' = Created on: 1999-2000                                                      =
' =                                                                            =
' = This is free and unencumbered software released into the public domain.    =
' =                                                                            =
' = Anyone is free to copy, modify, publish, use, compile, sell, or            =
' = distribute this software, either in source code form or as a compiled      =
' = binary, for any purpose, commercial or non-commercial, and by any          =
' = means.                                                                     =
' =                                                                            =
' = In jurisdictions that recognize copyright laws, the author or authors      =
' = of this software dedicate any and all copyright interest in the            =
' = software to the public domain. We make this dedication for the benefit     =
' = of the public at large and to the detriment of our heirs and               =
' = successors. We intend this dedication to be an overt act of                =
' = relinquishment in perpetuity of all present and future rights to this      =
' = software under copyright law.                                              =
' =                                                                            =
' = THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,            =
' = EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF         =
' = MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.     =
' = IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR          =
' = OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,      =
' = ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR      =
' = OTHER DEALINGS IN THE SOFTWARE.                                            =
' =                                                                            =
' = For more information, please refer to <https://unlicense.org>              =
' =                                                                            =
' ==============================================================================

DECLARE SUB KONIEC ()
DECLARE SUB Konfiguracja ()
DECLARE SUB INTRO ()

INTRO

                              'ODCZYT KONFIGURACJI
OPEN "GB.CFG" FOR INPUT AS #1
INPUT #1, CPU, DZWIEK$
CLOSE

START2:

CLS
                                'DANE ETAPU
KOLOR.WROG1 = 28
SPEED = .25
ETAP$ = "PIERWSZY"
                                'DANE O GRACZU
PUNKTY = 0
ZYCIA = 3
SCREEN 13


START:
CLS
                                 'POCZATKOWY NAPIS
AA = 1
A = 19
DO
COLOR A
LOCATE 11, 15: PRINT "ETAP "; ETAP$
LOCATE 13, 8: PRINT "NACISNIJ SPACJE ABY ZACZAC!"
A = A + AA
IF A = 30 AND AA = 1 THEN AA = -1
IF A = 19 AND AA = -1 THEN AA = 1

FOR I = 1 TO CPU
NEXT

LOOP UNTIL INKEY$ = " "

CLS
COLOR 15




                           'DANE DLA GWIAZD
GG1 = 10        'PIONOWO
GG2 = 60
GG3 = 20
GG4 = 100
GG5 = 78
GG6 = 25
GG7 = 160
GG8 = 4
GG9 = 190
GG10 = 89
G1 = 10           'POZIOMO
G2 = 26
G3 = 50
G4 = 80
G5 = 120
G6 = 160
G7 = 200
G8 = 230
G9 = 260
G10 = 300

                               'DANE DLA STATKU
A = 150
AA = 199
B = 170
BB = 199
C = 160
CC = 180

                          'PIERWOTNY RYSUNEK STATKU
DO
LINE (A, AA)-(C, CC), 55
A = A + 1
LOOP UNTIL A = B

A = 150                   'PRZYWROCENIE PIERWOTNEJ WARTOSCI



SCREEN 13
DO

                       'GWIAZDY
GG1 = GG1 + .5        'PIONOWO
GG2 = GG2 + .5
GG3 = GG3 + .5
GG4 = GG4 + .5
GG5 = GG5 + .5
GG6 = GG6 + .5
GG7 = GG7 + .5
GG8 = GG8 + .5
GG9 = GG9 + .5
GG10 = GG10 + .5


PSET (G1, GG1)
PSET (G2, GG2)
PSET (G3, GG3)
PSET (G4, GG4)
PSET (G5, GG5)
PSET (G6, GG6)
PSET (G7, GG7)
PSET (G8, GG8)
PSET (G9, GG9)
PSET (G10, GG10)
PRESET (G1, GG1 - .5)
PRESET (G2, GG2 - .5)
PRESET (G3, GG3 - .5)
PRESET (G4, GG4 - .5)
PRESET (G5, GG5 - .5)
PRESET (G6, GG6 - .5)
PRESET (G7, GG7 - .5)
PRESET (G8, GG8 - .5)
PRESET (G9, GG9 - .5)
PRESET (G10, GG10 - .5)

IF GG1 = 201 THEN
GG1 = 1
G1 = INT(RND * 320)
END IF

IF GG2 = 201 THEN
GG2 = 1
G2 = INT(RND * 320)
END IF

IF GG3 = 201 THEN
GG3 = 1
G3 = INT(RND * 320)
END IF

IF GG4 = 201 THEN
GG4 = 1
G4 = INT(RND * 320)
END IF

IF GG5 = 201 THEN
GG5 = 1
G5 = INT(RND * 320)
END IF

IF GG6 = 201 THEN
GG6 = 1
G6 = INT(RND * 320)
END IF

IF GG7 = 201 THEN
GG7 = 1
G7 = INT(RND * 320)
END IF

IF GG8 = 201 THEN
GG8 = 1
G8 = INT(RND * 320)
END IF

IF GG9 = 201 THEN
GG9 = 1
G9 = INT(RND * 320)
END IF

IF GG10 = 201 THEN
GG10 = 1
G10 = INT(RND * 320)
END IF

                                'PUNKTACJA
LOCATE 1, 1: PRINT "PUNKTY "; PUNKTY
LOCATE 1, 15: PRINT "ZYCIA "; ZYCIA
LOCATE 1, 25: PRINT " BOMBY "; bomby
                              'RUCH STATKU

'    (C,CC)
'      /\
'     /  \
'    /    \
'   /------\
'(A,AA)   (C,CC)

LINE (A, AA)-(C, CC), KL
LINE (B, BB)-(C, CC), KP

PRESET (C, CC)             'USUNIECIE SLADOW PO WIERZCHOLKU "C"

A = A + ADD
B = B + ADD
C = C + ADD

                           'GRANICE STATKU
IF A <= 1 AND ADD = -1 THEN
ADD = 0                                   'Z LEWEJ
A = A + 1
B = B + 1
C = C + 1
END IF

IF B >= 319 AND ADD = 1 THEN
ADD = 0
A = A - 1                                'Z PRAWEJ
B = B - 1
C = C - 1
END IF

                                  'STRZAL GRACZA
IF STRZAL.GRACZ = 0 THEN GOTO DALEJ.2

STRZAL.GRACZ = 1
LINE (S, SS)-(S, SS - 1.5), 4         'ZAPALANIE

LINE (S, SS + 15)-(S, SS + 15 - 1.5), 0   'GASZENIE

SS = SS - 1.5


IF SS <= -15 THEN STRZAL.GRACZ = 0   'GRANICE STRZALU


DALEJ.2:

                               'WROG1
IF LINIE = 1 THEN GOTO NO
IF WROG1 = 1 THEN

LINE (W, WW)-(W + 15, WW), 0
LINE (W, WW + 15)-(W + 15, WW + 15), KOLOR.WROG1
WW = WW + SPEED
END IF

NO:
                           'LOSOWANIE POZYCJI 1 WROGA
IF LINIE = 1 THEN GOTO DALEJ.3
IF WROG1 = 1 THEN GOTO DALEJ.3

IF LINIE = 2 THEN
IF WROG1 = 1 THEN GOTO DALEJ.3
WROG1 = 1
W = INT(RND * X + 50)
WW = -15
ILOSC = ILOSC + 1
END IF

WROG1 = 1
W = INT(RND * 300)
WW = -15
ILOSC = ILOSC + 1

DALEJ.3:
                             'ZNIKANIE WROGA1
IF SS <= WW THEN
IF S >= W AND S <= W + 15 THEN
IF DZWIEK$ = "T" OR DZWIEK$ = "t" THEN
SOUND 40, 1
SOUND 80, 1
SOUND 40, 1
END IF

PAINT (W + 1, WW + 1), 0
WROG1 = 0
STRZAL.GRACZ = 0
PRESET (S, SS)
PUNKTY = PUNKTY + 10

END IF
END IF

                                  'UTRATA ZYCIA
IF WW + 15 >= CC - 1 THEN
ZYCIA = ZYCIA - 1

IF DZWIEK$ = "T" OR DZWIEK$ = "t" THEN
SOUND 50, 1
SOUND 100, 1
SOUND 37, 1
END IF

PAINT (W + 1, WW + 1), 0
WROG1 = 0
END IF
IF ZYCIA = 0 THEN GOTO KONIEC.GRY

                                    'LINIE
IF LINIE <> 0 THEN
LINE (1, XX)-(X, XX), KOLOR.LINIA                 'XX - PIONOWE POL. LINI
LINE (X + 30, XX)-(320, XX), KOLOR.LINIA

LINE (1, XX - 2)-(X, XX - 2), 0
LINE (X + 30, XX - 2)-(320, XX - 2), 0

XX = XX + SPEED.LINE

IF DZIURA = 1 THEN GOTO OM
X = INT(RND * 290)
DZIURA = 1

OM:
                                   'UTRATA ZYCIA PRZEZ LINIE
IF XX >= CC AND XX < CC + .8 THEN
IF A >= X AND B <= X + 30 THEN GOTO OK
IF DZWIEK$ = "T" OR DZWIEK$ = "t" THEN
SOUND 37, 1
SOUND 60, 2
SOUND 37, 1
END IF
ZYCIA = ZYCIA - 1
END IF

OK:
                                    'ZAPETLENIE LINI
IF XX >= 202 THEN
PUNKTY = PUNKTY + 10
DZIURA = 0
XX = 0
ILOSC = ILOSC + 1


END IF
END IF


                                'STEROWANIE
SELECT CASE INKEY$
CASE IS = CHR$(0) + "M":                'PRAWO
ADD = 1
KL = 0
KP = 55

CASE IS = CHR$(0) + "K":                'LEWO
ADD = -1
KL = 55
KP = 0

CASE IS = CHR$(0) + "P":                 'STOP
ADD = 0

CASE IS = " ":                           'STRZAL
IF STRZAL.GRACZ = 1 THEN GOTO DALEJ
STRZAL.GRACZ = 1
S = C                                  'S - POZIOME ,SS -PIONOWE WSP. STRZALU
SS = CC - 1
IF DZWIEK$ = "T" OR DZWIEK$ = "t" THEN
SOUND 100, 1
END IF

DALEJ:

CASE IS = "B":                               'BOMBA
IF bomby = 0 THEN GOTO DALEJ.6
bomby = bomby - 1
IF DZWIEK$ = "T" OR DZWIEK$ = "t" THEN
SOUND 37, 1
SOUND 50, 1
SOUND 70, 1
SOUND 100, 1
END IF

PAINT (1, 1), 30
PAINT (W + 1, WW + 1), 0
Z = A
DO
LINE (A, AA)-(C, CC), 55
A = A + 1
LOOP UNTIL A = B
A = Z                   'PRZYWROCENIE PIERWOTNEJ WARTOSCI
WROG1 = 0
PUNKTY = PUNKTY + 10

CASE IS = CHR$(27):
INTRO

END SELECT
DALEJ.6:

                                     'WYLICZENIE ETAPU
IF ETAP$ = "DRUGI" THEN GOTO 1
IF ILOSC = 10 THEN
KOLOR.WROG1 = 70
ETAP$ = "DRUGI"
SPEED = .4
bomby = bomby + 1
GOTO START
END IF

1 :
IF ETAP$ = "TRZECI" THEN GOTO 2
IF ILOSC = 25 THEN
KOLOR.WROG1 = 36
ETAP$ = "TRZECI"
SPEED = .6
bomby = bomby + 1
GOTO START
END IF

2 :
IF ETAP$ = "CZWARTY" THEN GOTO 3
IF ILOSC = 40 THEN
KOLOR.WROG1 = 89
ETAP$ = "CZWARTY"
SPEED = .7
bomby = bomby + 1
GOTO START
END IF

3 :
IF ETAP$ = "PIATY" THEN GOTO 4
IF ILOSC = 60 THEN
KOLOR.WROG1 = 80
ETAP$ = "PIATY"
SPEED = .75
bomby = bomby + 1
GOTO START
END IF

4 :
IF ILOSC = 70 THEN
LINIE = 1
KOLOR.LINIA = 2
SPEED.LINE = .7
END IF

IF ETAP$ = "SZOSTY" THEN GOTO 5
IF ILOSC = 85 THEN
ETAP$ = "SZOSTY"
KOLOR.WROG1 = 156
SPEED = .25
LINIE = 2
KOLOR.LINIA = 2
SPEED.LINE = .7
bomby = bomby + 1
GOTO START
END IF

5 :
IF ETAP$ = "SIODMY" THEN GOTO 6
IF ILOSC = 100 THEN
ETAP$ = "SIODMY"
KOLOR.WROG1 = 316
SPEED = .35
LINIE = 2
SPEED.LINE = .7
bomby = bomby + 1
GOTO START
END IF

6 :
IF ILOSC = 120 THEN
LINIE = 1
SPEED.LINE = .8
KOLOR.LINIA = 78
END IF

7 :
IF ILOSC = 140 THEN GOTO GRATULACJE

FOR I = 1 TO (33.33333 / 100) * CPU
NEXT

LOOP


KONIEC.GRY:
CLS
COLOR 34
PLAY "<CCC N0 CDEDCDD N0 C"
DO
LOCATE 11, 8: PRINT "TWOJA MISJA ZAKONCZYLA SIE"
LOCATE 13, 13: PRINT "NIEPOWODZENIEM "
LOOP WHILE INKEY$ = ""
DO

X = INT(RND * 320)
Y = INT(RND * 200)
X2 = INT(RND * 320)
Y2 = INT(RND * 200)


LINE (X, Y)-(X + 5, Y + 3), 4
LINE (X2, Y2)-(X2 + 5, Y2 + 3), 4

IF A = 10000 THEN LINE (0, 0)-(320, 200), 4, BF
A = A + 1

LOOP UNTIL A = 11000


                 'PYTANIE
CLS
COLOR 4
LOCATE 11, 10: PRINT "CZY CHCESZ GRAC DALEJ?"
LOCATE 13, 13: PRINT " T(tak) / N(nie)"

DO

SELECT CASE INKEY$
CASE IS = "T"
INTRO

CASE IS = "t"
INTRO

CASE IS = "n"
SYSTEM
CASE IS = "N":
SYSTEM


END SELECT
LOOP

GRATULACJE:
CLS
COLOR 34
PLAY "<CCC N0 DDEFEAB N0 CC"
DO
LOCATE 11, 11: PRINT "G R A T U L A C J E"
LOCATE 13, 13: PRINT "   K O N I E C  G R Y"
LOCATE 13, 13: PRINT "      CIAG DALSZY NASTAPI"
LOOP WHILE INKEY$ = ""
DO

X = INT(RND * 320)
Y = INT(RND * 200)
X2 = INT(RND * 320)
Y2 = INT(RND * 200)


LINE (X, Y)-(X + 5, Y + 3), 4
LINE (X2, Y2)-(X2 + 5, Y2 + 3), 4

IF A = 10000 THEN LINE (0, 0)-(320, 200), 4, BF
A = A + 1

LOOP UNTIL A = 11000


                 'PYTANIE
CLS
COLOR 4
LOCATE 11, 10: PRINT "CZY CHCESZ GRAC DALEJ?"
LOCATE 13, 13: PRINT " T(tak) / N(nie)"

DO

SELECT CASE INKEY$
CASE IS = "T"
INTRO

CASE IS = "t"
INTRO

CASE IS = "n"
SYSTEM
CASE IS = "N":
SYSTEM


END SELECT
LOOP

SUB INTRO
POCZATEK:                'DANE DLA GWIAZD
GG1 = 10        'PIONOWO
GG2 = 60
GG3 = 20
GG4 = 100
GG5 = 78
GG6 = 25
GG7 = 160
GG8 = 4
GG9 = 190
GG10 = 89
G1 = 10           'POZIOMO
G2 = 26
G3 = 50
G4 = 80
G5 = 120
G6 = 160
G7 = 200
G8 = 230
G9 = 260
G10 = 300

SCREEN 13
                               'ODCZYT KONFIGURACJI
OPEN "GB.CFG" FOR INPUT AS #1
INPUT #1, CPU, DZWIEK$
CLOSE

'GOTO menu
ADD = .1
X = 16
CLS
DO
COLOR X
LOCATE 12, 4: PRINT "S Y L W E S T E R   W Y S O C K I "
X = X + ADD
IF X >= 30 THEN
ADD = -.1
END IF

IF X = 16 AND ADD = -.1 THEN GOTO PRZED
FOR I = 1 TO CPU / 2
NEXT

IF INKEY$ = " " THEN GOTO MENU
LOOP

PRZED:

ADD = .1
X = 16
DO
COLOR X
LOCATE 12, 6: PRINT "P R Z E D S T A W I A   G R E "
X = X + ADD
IF X >= 30 THEN
ADD = -.1
END IF

IF X = 16 AND ADD = -.1 THEN GOTO POD
FOR I = 1 TO CPU / 2
NEXT
IF INKEY$ = " " THEN GOTO MENU
LOOP

POD:
ADD = .1
X = 16
DO
COLOR X
LOCATE 12, 10: PRINT "P O D   T Y T U L E M"
X = X + ADD
IF X >= 30 THEN
ADD = -.1
END IF

IF X = 16 AND ADD = -.1 THEN GOTO GWIEZDNA
FOR I = 1 TO CPU / 2
NEXT
IF INKEY$ = " " THEN GOTO MENU
LOOP

GWIEZDNA:
ADD = .1
X = 35
DO
COLOR 1
LOCATE 12, 3: PRINT "G W I E Z D N A   B I T W A  WERSJA 1.2"
X = X + ADD

IF X >= 45 THEN
ADD = -.1
END IF

IF X = 35 AND ADD = -.1 THEN
DO
LOOP UNTIL INKEY$ = " "
GOTO MENU
END IF

FOR I = 1 TO CPU / 2
NEXT
IF INKEY$ = " " THEN GOTO MENU
LOOP

MENU:
PLAY "L6"
CLS
X = 20
ADD = 1

SCREEN 13
DO
DO

                       'GWIAZDY
GG1 = GG1 + .5        'PIONOWO
GG2 = GG2 + .5
GG3 = GG3 + .5
GG4 = GG4 + .5
GG5 = GG5 + .5
GG6 = GG6 + .5
GG7 = GG7 + .5
GG8 = GG8 + .5
GG9 = GG9 + .5
GG10 = GG10 + .5


PSET (G1, GG1)
PSET (G2, GG2)
PSET (G3, GG3)
PSET (G4, GG4)
PSET (G5, GG5)
PSET (G6, GG6)
PSET (G7, GG7)
PSET (G8, GG8)
PSET (G9, GG9)
PSET (G10, GG10)
PRESET (G1, GG1 - .5)
PRESET (G2, GG2 - .5)
PRESET (G3, GG3 - .5)
PRESET (G4, GG4 - .5)
PRESET (G5, GG5 - .5)
PRESET (G6, GG6 - .5)
PRESET (G7, GG7 - .5)
PRESET (G8, GG8 - .5)
PRESET (G9, GG9 - .5)
PRESET (G10, GG10 - .5)

IF GG1 = 201 THEN
GG1 = 1
G1 = INT(RND * 320)
END IF

IF GG2 = 201 THEN
GG2 = 1
G2 = INT(RND * 320)
END IF

IF GG3 = 201 THEN
GG3 = 1
G3 = INT(RND * 320)
END IF

IF GG4 = 201 THEN
GG4 = 1
G4 = INT(RND * 320)
END IF

IF GG5 = 201 THEN
GG5 = 1
G5 = INT(RND * 320)
END IF

IF GG6 = 201 THEN
GG6 = 1
G6 = INT(RND * 320)
END IF

IF GG7 = 201 THEN
GG7 = 1
G7 = INT(RND * 320)
END IF

IF GG8 = 201 THEN
GG8 = 1
G8 = INT(RND * 320)
END IF

IF GG9 = 201 THEN
GG9 = 1
G9 = INT(RND * 320)
END IF

IF GG10 = 201 THEN
GG10 = 1
G10 = INT(RND * 320)
END IF



SELECT CASE INKEY$
 CASE IS = CHR$(0) + "P":
  IF MENU < 2 THEN MENU = MENU + 1
 CASE IS = CHR$(0) + "H":
  IF MENU > 0 THEN MENU = MENU - 1
 CASE IS = CHR$(13):
  IF MENU = 0 THEN CLS : GOTO KONIEC
  IF MENU = 1 THEN GOTO Konfiguracja
  IF MENU = 2 THEN END

END SELECT

SELECT CASE MENU
 CASE IS = 0:
  AX = 30: BX = 25: CX = 25
 CASE IS = 1:
  AX = 25: BX = 30: CX = 25
 CASE IS = 2:
  AX = 25: BX = 25: CX = 30
END SELECT

COLOR 69: LOCATE 1, 9: PRINT "GWIEZDNA BITWA WERSJA 1.2"
COLOR AX: LOCATE 10, 10: PRINT "  N O W A  G R A  "
COLOR BX: LOCATE 12, 10: PRINT "  K O N F I G U R A C J A"
COLOR CX: LOCATE 14, 10: PRINT "  W Y J S C I E  "

LOOP



COLOR X
X = X + ADD
IF X = 30 THEN ADD = -1
IF X = 20 AND ADD = -1 THEN ADD = 1
LOCATE 1, 9: PRINT "GWIEZDNA BITWA WERSJA 1.1"
IF C > 8 AND C < 15 THEN

IF DZWIEK$ = "T" OR DZWIEK$ = "t" THEN
PLAY "<D"
END IF

GOTO D
END IF
IF C = 16 THEN C = 0

IF DZWIEK$ = "T" OR DZWIEK$ = "t" THEN
PLAY "<C"
END IF

D:
C = C + 1

SELECT CASE INKEY$
CASE IS = "3":
END

CASE IS = "1":
GOTO KONIEC

CASE IS = "2":
GOTO Konfiguracja

END SELECT

FOR I = 1 TO CPU
NEXT
LOOP

Konfiguracja:
SCREEN 0
WIDTH 80, 25
CLS
A:
CLS
INPUT "Jak szybki jest twoj komputer? (0-2.14 mld)? ", CPU
PRINT
B:
CLS
INPUT "Dzwiek (T/N)?", DZWIEK$
IF DZWIEK$ = "T" OR DZWIEK$ = "t" THEN
    GOTO PISZ
      ELSE GOTO B
 END IF
PISZ:
OPEN "GB.CFG" FOR OUTPUT AS #1
WRITE #1, CPU, DZWIEK$
CLOSE
CLS
SCREEN 13
GOTO POCZATEK

KONIEC:
END SUB

SUB KONIEC
CLS
END
END SUB
