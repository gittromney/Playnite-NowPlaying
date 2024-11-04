param($releaseDir, $packedReleaseDir)

# collapse up-level relative paths ('\xyz\..' -> ''), if applicable
$up1Pattern = '\\[^\\\.]+\\\.\.'
$packedReleaseDir = $packedReleaseDir -replace $up1Pattern, ''
$leanReleaseDir = ($releaseDir.TrimEnd('\') -replace $up1Pattern, '') + '_Lean\' 
$Toolbox = $Env:LOCALAPPDATA + "\Playnite\Toolbox.exe"

echo "PostReleaseBuild.ps1:"
echo "[-releaseDir: $releaseDir]"
echo "[lean release dir: $leanReleaseDir]"
echo "[-packedReleaseDir: $packedReleaseDir]"
echo "[Toolbox.exe path: $Toolbox]"

if (Test-Path -Path $releaseDir) 
{
	# clean | create lean release area 
	if (Test-Path -Path $leanReleaseDir) 
	{
		Remove-Item -LiteralPath $leanReleaseDir -Force -Recurse
	}
	$null = New-Item -Path $leanReleaseDir -ItemType Directory

	# copy lean release files/subdirs to the lean release dir:
	# . Localization\
	# . extension.yaml
	# . icon.png
	# . NowPlaying.dll
    echo "Creating 'lean' release in '$leanReleaseDir' ..."

	Copy-Item -Path $releaseDir\Localization -Destination $leanReleaseDir -Recurse -Container
	Copy-Item -Path $releaseDir\extension.yaml -Destination $leanReleaseDir
	Copy-Item -Path $releaseDir\icon.png -Destination $leanReleaseDir
	Copy-Item -Path $releaseDir\NowPlaying.dll -Destination $leanReleaseDir

	echo "Done copying files to lean release area"

	# use Playnite's Toolbox.exe to create packed release, .pext
	$test1 = Test-Path -Path $Toolbox -Type Leaf
	$test2 = Test-Path -Path $packedReleaseDir
	if ($test1 -and $test2) 
	{
		echo "Packing lean release (w/Toolbox.exe) to '$packedReleaseDir'..."
		$command = "$Toolbox pack $leanReleaseDir $packedReleaseDir"
		Invoke-Expression $command
	}
}
exit 0;
