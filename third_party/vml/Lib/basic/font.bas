' VGA 8x16 Font Library
' Standard VGA ROM font bitmap data (ASCII 32-126)
' Each character is 16 bytes (8x16 pixels, 1 bit per pixel)
'
' Usage:
'   GOSUB FontInit          ' Initialize font data in memory
'   GOSUB DrawChar           ' Draw a character at cursor position
'                             ' Set CX=x, CY=y, CH$=char, CF=forecolor
'   GOSUB DrawString         ' Draw a string
'                             ' Set S$=string, CX=x, CY=y, CF=color
'
' Font data stored at FONT_ADDR (default 0x5000)
' Font bitmap for char c is at FONT_ADDR + (c-32)*16

DIM FONT_ADDR
DIM CX, CY, CH, CF, CS

FontInit:
    FONT_ADDR = &H5000
    ' Store font data in memory via POKE
    ' Space (32): all zeros
    I = 0
    WHILE I < 16
        POKE FONT_ADDR + 0 * 16 + I, 0
        I = I + 1
    WEND
    
    ' ! (33)
    POKE FONT_ADDR + 1 * 16 + 0, &H00
    POKE FONT_ADDR + 1 * 16 + 1, &H18
    POKE FONT_ADDR + 1 * 16 + 2, &H18
    POKE FONT_ADDR + 1 * 16 + 3, &H18
    POKE FONT_ADDR + 1 * 16 + 4, &H18
    POKE FONT_ADDR + 1 * 16 + 5, &H18
    POKE FONT_ADDR + 1 * 16 + 6, &H18
    POKE FONT_ADDR + 1 * 16 + 7, &H18
    POKE FONT_ADDR + 1 * 16 + 8, &H18
    POKE FONT_ADDR + 1 * 16 + 9, &H00
    POKE FONT_ADDR + 1 * 16 + 10, &H18
    POKE FONT_ADDR + 1 * 16 + 11, &H18
    POKE FONT_ADDR + 1 * 16 + 12, &H00
    POKE FONT_ADDR + 1 * 16 + 13, &H00
    POKE FONT_ADDR + 1 * 16 + 14, &H00
    POKE FONT_ADDR + 1 * 16 + 15, &H00
    
    ' " (34)
    POKE FONT_ADDR + 2 * 16 + 0, &H00
    POKE FONT_ADDR + 2 * 16 + 1, &H66
    POKE FONT_ADDR + 2 * 16 + 2, &H66
    POKE FONT_ADDR + 2 * 16 + 3, &H66
    POKE FONT_ADDR + 2 * 16 + 4, &H00
    POKE FONT_ADDR + 2 * 16 + 5, &H00
    POKE FONT_ADDR + 2 * 16 + 6, &H00
    POKE FONT_ADDR + 2 * 16 + 7, &H00
    POKE FONT_ADDR + 2 * 16 + 8, &H00
    POKE FONT_ADDR + 2 * 16 + 9, &H00
    POKE FONT_ADDR + 2 * 16 + 10, &H00
    POKE FONT_ADDR + 2 * 16 + 11, &H00
    POKE FONT_ADDR + 2 * 16 + 12, &H00
    POKE FONT_ADDR + 2 * 16 + 13, &H00
    POKE FONT_ADDR + 2 * 16 + 14, &H00
    POKE FONT_ADDR + 2 * 16 + 15, &H00

    ' # (35)
    POKE FONT_ADDR + 3 * 16 + 0, &H00
    POKE FONT_ADDR + 3 * 16 + 1, &H24
    POKE FONT_ADDR + 3 * 16 + 2, &H24
    POKE FONT_ADDR + 3 * 16 + 3, &H7E
    POKE FONT_ADDR + 3 * 16 + 4, &H24
    POKE FONT_ADDR + 3 * 16 + 5, &H24
    POKE FONT_ADDR + 3 * 16 + 6, &H7E
    POKE FONT_ADDR + 3 * 16 + 7, &H24
    POKE FONT_ADDR + 3 * 16 + 8, &H24
    POKE FONT_ADDR + 3 * 16 + 9, &H00
    POKE FONT_ADDR + 3 * 16 + 10, &H00
    POKE FONT_ADDR + 3 * 16 + 11, &H00
    POKE FONT_ADDR + 3 * 16 + 12, &H00
    POKE FONT_ADDR + 3 * 16 + 13, &H00
    POKE FONT_ADDR + 3 * 16 + 14, &H00
    POKE FONT_ADDR + 3 * 16 + 15, &H00

    ' $ (36)
    POKE FONT_ADDR + 4 * 16 + 0, &H00
    POKE FONT_ADDR + 4 * 16 + 1, &H18
    POKE FONT_ADDR + 4 * 16 + 2, &H3C
    POKE FONT_ADDR + 4 * 16 + 3, &H5A
    POKE FONT_ADDR + 4 * 16 + 4, &H58
    POKE FONT_ADDR + 4 * 16 + 5, &H3C
    POKE FONT_ADDR + 4 * 16 + 6, &H1A
    POKE FONT_ADDR + 4 * 16 + 7, &H5A
    POKE FONT_ADDR + 4 * 16 + 8, &H3C
    POKE FONT_ADDR + 4 * 16 + 9, &H18
    POKE FONT_ADDR + 4 * 16 + 10, &H00
    POKE FONT_ADDR + 4 * 16 + 11, &H00
    POKE FONT_ADDR + 4 * 16 + 12, &H00
    POKE FONT_ADDR + 4 * 16 + 13, &H00
    POKE FONT_ADDR + 4 * 16 + 14, &H00
    POKE FONT_ADDR + 4 * 16 + 15, &H00

    ' % (37)
    POKE FONT_ADDR + 5 * 16 + 0, &H00
    POKE FONT_ADDR + 5 * 16 + 1, &H62
    POKE FONT_ADDR + 5 * 16 + 2, &H64
    POKE FONT_ADDR + 5 * 16 + 3, &H08
    POKE FONT_ADDR + 5 * 16 + 4, &H10
    POKE FONT_ADDR + 5 * 16 + 5, &H26
    POKE FONT_ADDR + 5 * 16 + 6, &H46
    POKE FONT_ADDR + 5 * 16 + 7, &H00
    POKE FONT_ADDR + 5 * 16 + 8, &H00
    POKE FONT_ADDR + 5 * 16 + 9, &H00
    POKE FONT_ADDR + 5 * 16 + 10, &H00
    POKE FONT_ADDR + 5 * 16 + 11, &H00
    POKE FONT_ADDR + 5 * 16 + 12, &H00
    POKE FONT_ADDR + 5 * 16 + 13, &H00
    POKE FONT_ADDR + 5 * 16 + 14, &H00
    POKE FONT_ADDR + 5 * 16 + 15, &H00

    ' & (38)
    POKE FONT_ADDR + 6 * 16 + 0, &H00
    POKE FONT_ADDR + 6 * 16 + 1, &H1C
    POKE FONT_ADDR + 6 * 16 + 2, &H22
    POKE FONT_ADDR + 6 * 16 + 3, &H22
    POKE FONT_ADDR + 6 * 16 + 4, &H1C
    POKE FONT_ADDR + 6 * 16 + 5, &H28
    POKE FONT_ADDR + 6 * 16 + 6, &H46
    POKE FONT_ADDR + 6 * 16 + 7, &H3A
    POKE FONT_ADDR + 6 * 16 + 8, &H00
    POKE FONT_ADDR + 6 * 16 + 9, &H00
    POKE FONT_ADDR + 6 * 16 + 10, &H00
    POKE FONT_ADDR + 6 * 16 + 11, &H00
    POKE FONT_ADDR + 6 * 16 + 12, &H00
    POKE FONT_ADDR + 6 * 16 + 13, &H00
    POKE FONT_ADDR + 6 * 16 + 14, &H00
    POKE FONT_ADDR + 6 * 16 + 15, &H00

    ' ' (39)
    POKE FONT_ADDR + 7 * 16 + 0, &H00
    POKE FONT_ADDR + 7 * 16 + 1, &H18
    POKE FONT_ADDR + 7 * 16 + 2, &H18
    POKE FONT_ADDR + 7 * 16 + 3, &H18
    POKE FONT_ADDR + 7 * 16 + 4, &H00
    POKE FONT_ADDR + 7 * 16 + 5, &H00
    POKE FONT_ADDR + 7 * 16 + 6, &H00
    POKE FONT_ADDR + 7 * 16 + 7, &H00
    POKE FONT_ADDR + 7 * 16 + 8, &H00
    POKE FONT_ADDR + 7 * 16 + 9, &H00
    POKE FONT_ADDR + 7 * 16 + 10, &H00
    POKE FONT_ADDR + 7 * 16 + 11, &H00
    POKE FONT_ADDR + 7 * 16 + 12, &H00
    POKE FONT_ADDR + 7 * 16 + 13, &H00
    POKE FONT_ADDR + 7 * 16 + 14, &H00
    POKE FONT_ADDR + 7 * 16 + 15, &H00

    ' ( (40)
    POKE FONT_ADDR + 8 * 16 + 0, &H00
    POKE FONT_ADDR + 8 * 16 + 1, &H04
    POKE FONT_ADDR + 8 * 16 + 2, &H08
    POKE FONT_ADDR + 8 * 16 + 3, &H10
    POKE FONT_ADDR + 8 * 16 + 4, &H10
    POKE FONT_ADDR + 8 * 16 + 5, &H10
    POKE FONT_ADDR + 8 * 16 + 6, &H10
    POKE FONT_ADDR + 8 * 16 + 7, &H10
    POKE FONT_ADDR + 8 * 16 + 8, &H10
    POKE FONT_ADDR + 8 * 16 + 9, &H08
    POKE FONT_ADDR + 8 * 16 + 10, &H04
    POKE FONT_ADDR + 8 * 16 + 11, &H00
    POKE FONT_ADDR + 8 * 16 + 12, &H00
    POKE FONT_ADDR + 8 * 16 + 13, &H00
    POKE FONT_ADDR + 8 * 16 + 14, &H00
    POKE FONT_ADDR + 8 * 16 + 15, &H00

    ' ) (41)
    POKE FONT_ADDR + 9 * 16 + 0, &H00
    POKE FONT_ADDR + 9 * 16 + 1, &H10
    POKE FONT_ADDR + 9 * 16 + 2, &H08
    POKE FONT_ADDR + 9 * 16 + 3, &H04
    POKE FONT_ADDR + 9 * 16 + 4, &H04
    POKE FONT_ADDR + 9 * 16 + 5, &H04
    POKE FONT_ADDR + 9 * 16 + 6, &H04
    POKE FONT_ADDR + 9 * 16 + 7, &H04
    POKE FONT_ADDR + 9 * 16 + 8, &H04
    POKE FONT_ADDR + 9 * 16 + 9, &H08
    POKE FONT_ADDR + 9 * 16 + 10, &H10
    POKE FONT_ADDR + 9 * 16 + 11, &H00
    POKE FONT_ADDR + 9 * 16 + 12, &H00
    POKE FONT_ADDR + 9 * 16 + 13, &H00
    POKE FONT_ADDR + 9 * 16 + 14, &H00
    POKE FONT_ADDR + 9 * 16 + 15, &H00

    ' * (42)
    POKE FONT_ADDR + 10 * 16 + 0, &H00
    POKE FONT_ADDR + 10 * 16 + 1, &H00
    POKE FONT_ADDR + 10 * 16 + 2, &H24
    POKE FONT_ADDR + 10 * 16 + 3, &H18
    POKE FONT_ADDR + 10 * 16 + 4, &H7E
    POKE FONT_ADDR + 10 * 16 + 5, &H18
    POKE FONT_ADDR + 10 * 16 + 6, &H24
    POKE FONT_ADDR + 10 * 16 + 7, &H00
    POKE FONT_ADDR + 10 * 16 + 8, &H00
    POKE FONT_ADDR + 10 * 16 + 9, &H00
    POKE FONT_ADDR + 10 * 16 + 10, &H00
    POKE FONT_ADDR + 10 * 16 + 11, &H00
    POKE FONT_ADDR + 10 * 16 + 12, &H00
    POKE FONT_ADDR + 10 * 16 + 13, &H00
    POKE FONT_ADDR + 10 * 16 + 14, &H00
    POKE FONT_ADDR + 10 * 16 + 15, &H00

    ' + (43)
    POKE FONT_ADDR + 11 * 16 + 0, &H00
    POKE FONT_ADDR + 11 * 16 + 1, &H00
    POKE FONT_ADDR + 11 * 16 + 2, &H18
    POKE FONT_ADDR + 11 * 16 + 3, &H18
    POKE FONT_ADDR + 11 * 16 + 4, &H7E
    POKE FONT_ADDR + 11 * 16 + 5, &H18
    POKE FONT_ADDR + 11 * 16 + 6, &H18
    POKE FONT_ADDR + 11 * 16 + 7, &H00
    POKE FONT_ADDR + 11 * 16 + 8, &H00
    POKE FONT_ADDR + 11 * 16 + 9, &H00
    POKE FONT_ADDR + 11 * 16 + 10, &H00
    POKE FONT_ADDR + 11 * 16 + 11, &H00
    POKE FONT_ADDR + 11 * 16 + 12, &H00
    POKE FONT_ADDR + 11 * 16 + 13, &H00
    POKE FONT_ADDR + 11 * 16 + 14, &H00
    POKE FONT_ADDR + 11 * 16 + 15, &H00

    ' , (44)
    POKE FONT_ADDR + 12 * 16 + 0, &H00
    POKE FONT_ADDR + 12 * 16 + 1, &H00
    POKE FONT_ADDR + 12 * 16 + 2, &H00
    POKE FONT_ADDR + 12 * 16 + 3, &H00
    POKE FONT_ADDR + 12 * 16 + 4, &H00
    POKE FONT_ADDR + 12 * 16 + 5, &H00
    POKE FONT_ADDR + 12 * 16 + 6, &H18
    POKE FONT_ADDR + 12 * 16 + 7, &H18
    POKE FONT_ADDR + 12 * 16 + 8, &H08
    POKE FONT_ADDR + 12 * 16 + 9, &H10
    POKE FONT_ADDR + 12 * 16 + 10, &H00
    POKE FONT_ADDR + 12 * 16 + 11, &H00
    POKE FONT_ADDR + 12 * 16 + 12, &H00
    POKE FONT_ADDR + 12 * 16 + 13, &H00
    POKE FONT_ADDR + 12 * 16 + 14, &H00
    POKE FONT_ADDR + 12 * 16 + 15, &H00

    ' - (45)
    POKE FONT_ADDR + 13 * 16 + 0, &H00
    POKE FONT_ADDR + 13 * 16 + 1, &H00
    POKE FONT_ADDR + 13 * 16 + 2, &H00
    POKE FONT_ADDR + 13 * 16 + 3, &H00
    POKE FONT_ADDR + 13 * 16 + 4, &H7E
    POKE FONT_ADDR + 13 * 16 + 5, &H00
    POKE FONT_ADDR + 13 * 16 + 6, &H00
    POKE FONT_ADDR + 13 * 16 + 7, &H00
    POKE FONT_ADDR + 13 * 16 + 8, &H00
    POKE FONT_ADDR + 13 * 16 + 9, &H00
    POKE FONT_ADDR + 13 * 16 + 10, &H00
    POKE FONT_ADDR + 13 * 16 + 11, &H00
    POKE FONT_ADDR + 13 * 16 + 12, &H00
    POKE FONT_ADDR + 13 * 16 + 13, &H00
    POKE FONT_ADDR + 13 * 16 + 14, &H00
    POKE FONT_ADDR + 13 * 16 + 15, &H00

    ' . (46)
    POKE FONT_ADDR + 14 * 16 + 0, &H00
    POKE FONT_ADDR + 14 * 16 + 1, &H00
    POKE FONT_ADDR + 14 * 16 + 2, &H00
    POKE FONT_ADDR + 14 * 16 + 3, &H00
    POKE FONT_ADDR + 14 * 16 + 4, &H00
    POKE FONT_ADDR + 14 * 16 + 5, &H00
    POKE FONT_ADDR + 14 * 16 + 6, &H18
    POKE FONT_ADDR + 14 * 16 + 7, &H18
    POKE FONT_ADDR + 14 * 16 + 8, &H00
    POKE FONT_ADDR + 14 * 16 + 8, &H00
