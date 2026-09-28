! sysinfo.f90 —— 全能接口 CALLJSON(#573) 自检：调用 sysinfo 并把返回的 JSON 整份打出来。
! sysinfo.f90 -- self-check for the all-purpose interface CALLJSON(#573): call sysinfo and print the whole returned JSON.

program p
  call ui_call_json_s('sysinfo', '')
  call ui_call_json_print()
end program p
