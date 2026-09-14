$ErrorActionPreference = "Stop"

# Auto-detect VS 2022 MSVC and Windows SDK via vswhere
$vsPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -property installationPath 2>$null
if (-not $vsPath) { Write-Error "Visual Studio 2022 not found."; exit 1 }

# Find latest MSVC toolset
$msvcRoot = Get-ChildItem "$vsPath\VC\Tools\MSVC" -Directory | Sort-Object Name -Descending | Select-Object -First 1
if (-not $msvcRoot) { Write-Error "MSVC toolset not found."; exit 1 }
$msvc = $msvcRoot.FullName

# Find latest Windows SDK
$sdkRoot = "${env:ProgramFiles(x86)}\Windows Kits\10"
$sdkVer = Get-ChildItem "$sdkRoot\Include" -Directory | Where-Object { $_.Name -match '^10\.' } | Sort-Object Name -Descending | Select-Object -First 1
if (-not $sdkVer) { Write-Error "Windows SDK not found."; exit 1 }
$sdk = $sdkRoot
$sdkVersion = $sdkVer.Name

$env:INCLUDE = "$msvc\include;$sdk\Include\$sdkVersion\ucrt;$sdk\Include\$sdkVersion\um;$sdk\Include\$sdkVersion\shared"
$env:LIB = "$msvc\lib\x64;$sdk\Lib\$sdkVersion\ucrt\x64;$sdk\Lib\$sdkVersion\um\x64"
$env:PATH = "$msvc\bin\Hostx64\x64;$env:PATH"

Set-Location $PSScriptRoot
& "$msvc\bin\Hostx64\x64\cl.exe" /nologo /O2 /MD /LD /Fe:libglhelper.dll libglhelper.c /link opengl32.lib user32.lib gdi32.lib

if ($LASTEXITCODE -eq 0) {
    Write-Host "BUILD SUCCESS: libglhelper.dll"
} else {
    Write-Host "BUILD FAILED (exit code $LASTEXITCODE)"
    exit $LASTEXITCODE
}
