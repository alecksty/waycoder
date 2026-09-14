\ OpenCV FFI Bindings — Forth
: OCV-IMREAD ( addr -- h ) DROP 0 ;
: OCV-IMWRITE ( addr h -- ok ) 2DROP 0 ;
: OCV-WAITKEY ( delay -- key ) DROP 0 ;
: OCV-CVTCOLOR ( s d code -- ) 2DROP DROP ;
: OCV-RESIZE ( s d w h -- ) 2DROP 2DROP ;
: OCV-BLUR ( s d k -- ) 2DROP DROP ;
: OCV-CANNY ( s d l hi -- ) 2DROP 2DROP ;
: OCV-WIDTH ( h -- w ) DROP 0 ;
: OCV-HEIGHT ( h -- h ) DROP 0 ;
: OCV-RELEASE ( h -- ) DROP ;
