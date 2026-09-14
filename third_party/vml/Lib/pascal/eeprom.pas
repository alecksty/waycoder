{ VML EEPROM 扩展库 — Pascal }
{ 需显式 uses eeprom }

function EepromRead(offset: integer; var buffer; count: integer): integer;
function EepromWrite(offset: integer; var data; count: integer): integer;
