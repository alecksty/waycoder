// OpenCV FFI Bindings — Pascal
unit Opencv;
interface
function OcvImread(path: String): Integer;
function OcvImwrite(path: String; h: Integer): Integer;
function OcvWaitkey(delay: Integer): Integer;
procedure OcvCvtcolor(s, d, code: Integer);
procedure OcvResize(s, d, w, h: Integer);
procedure OcvBlur(s, d, k: Integer);
procedure OcvCanny(s, d, l, hi: Integer);
function OcvWidth(h: Integer): Integer;
function OcvHeight(h: Integer): Integer;
procedure OcvRelease(h: Integer);
implementation
function OcvImread(path: String): Integer; begin OcvImread := 0; end;
function OcvImwrite(path: String; h: Integer): Integer; begin OcvImwrite := 0; end;
function OcvWaitkey(delay: Integer): Integer; begin OcvWaitkey := 0; end;
procedure OcvCvtcolor(s, d, code: Integer); begin end;
procedure OcvResize(s, d, w, h: Integer); begin end;
procedure OcvBlur(s, d, k: Integer); begin end;
procedure OcvCanny(s, d, l, hi: Integer); begin end;
function OcvWidth(h: Integer): Integer; begin OcvWidth := 0; end;
function OcvHeight(h: Integer): Integer; begin OcvHeight := 0; end;
procedure OcvRelease(h: Integer); begin end;
end.
