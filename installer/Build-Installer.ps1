$ErrorActionPreference = "Stop"

$projectDirectory = Split-Path -Parent $PSScriptRoot
$publishDirectory = Join-Path $projectDirectory "artifacts\publish\win-x64"
$installerScript = Join-Path $PSScriptRoot "KeyboardMouseOverlay.iss"
$iscc = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue

if ($null -eq $iscc) {
    $compilerCandidates = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )
    $compilerPath = $compilerCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($null -eq $compilerPath) {
        throw "Inno Setup 6 was not found. Install it from https://jrsoftware.org/isinfo.php and run this script again."
    }
}
else {
    $compilerPath = $iscc.Source
}

& dotnet publish (Join-Path $projectDirectory "KeyboardMouseOverlay.csproj") `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    --output $publishDirectory

if ($LASTEXITCODE -ne 0) {
    throw "The self-contained application publish failed with exit code $LASTEXITCODE."
}

& $compilerPath $installerScript
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE."
}

Write-Host "Installer created at $(Join-Path $projectDirectory 'dist\KeyboardMouseOverlaySetup.exe')"
