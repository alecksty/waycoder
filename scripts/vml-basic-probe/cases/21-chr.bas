' 收窄：CHR$ / ASC
PRINT "CHR65=["; CHR$(65); "]"
PRINT "CHR97=["; CHR$(97); "]"
PRINT "ASC="; ASC("A")
DIM code AS INTEGER
code = 66
PRINT "CHRV=["; CHR$(code); "]"
' EXPECT: CHR65=[A]|CHR97=[a]|ASC=65|CHRV=[B]
