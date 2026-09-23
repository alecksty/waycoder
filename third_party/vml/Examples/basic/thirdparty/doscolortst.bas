10 REM *** A color table is given bellow ***
20 REM
30 REM Colors without blinking are from 0 to 15, which are listed below.
40 REM
50 REM 0 = Black                8 = Grey
60 REM 1 = Blue                 9 = Bright Blue
70 REM 2 = Green               10 = Bright Green
80 REM 3 = Cyan                11 = Bright Cyan
90 REM 4 = Red                 12 = Bright Red
100 REM 5 = Magenta             13 = Bright Magenta
110 REM 6 = Brown               14 = Yellow
120 REM 7 = White               15 = Bright White
130 REM
140 REM Colors with blinking are from 16 to 31, which are listed below.
150 REM
160 REM 16 = Black    (Blink)   24 = Grey            (Blink)
170 REM 17 = Blue     (Blink)   25 = Bright Blue     (Blink)
180 REM 18 = Green    (Blink)   26 = Bright Green    (Blink)
190 REM 19 = Cyan     (Blink)   27 = Bright Cyan     (Blink)
200 REM 20 = Red      (Blink)   28 = Bright Red      (Blink)
210 REM 21 = Magenta  (Blink)   29 = Bright Magenta  (Blink)
220 REM 22 = Brown    (Blink)   30 = Yellow          (Blink)
230 REM 23 = White    (Blink)   31 = Bright White    (Blink)
240 REM
250 REM *** Note ***
260 REM All color values non-blinking (0 to 15) and blinking (16 to 31)
270 REM can be set to foreground. It means that all colors 0 to 31 are
280 REM foreground color.
290 REM But to set background colors, use only non-blinking (0 to 7) color
300 REM values. Using color values from 8 to 15 for background will not work
310 REM and dark color is set corresponding to that color value.
320 REM Using color value for foreground less than 0 or greater than 31 will
330 REM cause GW-BASIC to produce an ERROR.
340 REM Using color value for background less than 0 or greater than 15 will
350 REM cause GW-BASIC to producs an ERROR.
360 REM
370 REM
380 REM *** How to use COLOR command ***
390 REM COLOR (foreground-color-value), (background-color-value)
400 REM
410 REM
420 REM *** Example of using colors ***
430 REM *** Set color to foreground 15 (Bright White) and background 1 (blue).
440 COLOR 15, 1
450 REM *** Clear the screen with color set back.
460 CLS
470 REM *** Print colored text on screen
480 PRINT "This is bright white color text on blue color"
490 REM *** End the program and return back to GW-BASIC.
500 END