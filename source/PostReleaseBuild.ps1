param(
    [Parameter(Mandatory = $true)]
    [string]$leanPackageDir,

    [Parameter(Mandatory = $true)]
    [string]$packedReleaseDir
)

$leanPackageDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($leanPackageDir)
$packedReleaseDir = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($packedReleaseDir)
$Toolbox = Join-Path $env:LOCALAPPDATA "Playnite\Toolbox.exe"

Write-Host "PostReleaseBuild.ps1:"
Write-Host "[leanPackageDir: $leanPackageDir]"
Write-Host "[packedReleaseDir: $packedReleaseDir]"
Write-Host "[Toolbox.exe path: $Toolbox]"

if (-not (Test-Path -Path $leanPackageDir)) {
    Write-Warning "Lean package directory not found: $leanPackageDir"
    exit 1
}

$requiredFiles = @(
    (Join-Path $leanPackageDir "NowPlaying.dll"),
    (Join-Path $leanPackageDir "extension.yaml"),
    (Join-Path $leanPackageDir "icon.png")
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path -Path $file)) {
        Write-Warning "Required release file not found: $file"
        exit 1
    }
}

$localizationDir = Join-Path $leanPackageDir "Localization"
if (-not (Test-Path -Path $localizationDir)) {
    Write-Warning "Required localization directory not found: $localizationDir"
    exit 1
}

if (-not (Test-Path -Path $Toolbox -PathType Leaf)) {
    Write-Warning "Toolbox.exe not found at: $Toolbox"
    exit 1
}

if (-not (Test-Path -Path $packedReleaseDir)) {
    $null = New-Item -Path $packedReleaseDir -ItemType Directory -Force
}

Write-Host "Packing lean release (w/Toolbox.exe) to '$packedReleaseDir'..."
& $Toolbox pack $leanPackageDir $packedReleaseDir
exit $LASTEXITCODE
