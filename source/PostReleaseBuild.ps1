param($releaseDir)

$leanReleaseDir = $releaseDir.TrimEnd('\') + '_Lean' 
$packedReleaseDir = "C:\Dev\GitHub\Releases\"
$Toolbox = "C:\Users\miker\AppData\Local\Playnite\Toolbox.exe"

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
	echo "Release target is '$releaseDir'"
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
