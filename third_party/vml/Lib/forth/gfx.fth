\ VML VGA 图形扩展库 (QBASIC风格) — Forth
\ 需显式 include gfx.fth

\ Basic VGA
: VGA-CLEAR ( -- ) asm("SYSCALL 80") ;
: VGA-PUTCHAR ( x y c color -- ) asm("SYSCALL 81") ;
: VGA-PUTS ( x y addr len color -- ) asm("SYSCALL 82") ;

\ Screen mode & info
: GFX-SCREEN ( mode -- result ) ;
: GFX-WIDTH ( -- result ) ;
: GFX-HEIGHT ( -- result ) ;
: GFX-DEPTH ( -- result ) ;

\ Palette
: GFX-PALETTE ( idx r g b -- ) ;
: GFX-PALETTE-GET ( idx -- result ) ;

\ Pixel ops
: GFX-PSET ( x y color -- ) ;
: GFX-POINT ( x y -- result ) ;
: GFX-CLS ( -- ) ;
: GFX-CLS-COLOR ( color -- ) ;

\ Drawing
: GFX-LINE ( x1 y1 x2 y2 color -- ) ;
: GFX-RECT ( x1 y1 x2 y2 color -- ) ;
: GFX-RECT-FILL ( x1 y1 x2 y2 color -- ) ;
: GFX-CIRCLE ( cx cy r color -- ) ;
: GFX-CIRCLE-FILL ( cx cy r color -- ) ;
: GFX-ARC ( cx cy r sa ea color -- ) ;
: GFX-SECTOR ( cx cy r sa ea color -- ) ;

\ Text
: GFX-PRINT ( x y addr len color -- ) ;
: GFX-PRINT-SCALE ( x y addr len color scale -- ) ;

\ Fill
: GFX-FLOOD-FILL ( x y fc bc -- ) ;

\ Advanced (SYSCALL)
: GFX-SCREENSHOT ( -- result ) asm("SYSCALL 200") ;
: GFX-PUT-IMAGE ( x y w h addr -- result ) asm("SYSCALL 201") ;
: GFX-GET-IMAGE ( x y w h addr -- result ) asm("SYSCALL 202") ;
: GFX-VIEWPORT ( x1 y1 x2 y2 -- result ) asm("SYSCALL 203") ;
