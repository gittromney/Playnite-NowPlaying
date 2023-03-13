
#////////////////////////////////////////////////////////////////////////////////
#// Wrapper to robocopy.exe /MIR with graphical progress bar
#// -> Inspired by Copy-WithProgress written by Trevor Sullivan
#// -> Currently only supports 'copy' cases, where the destination is 
#//    either empty or has no files/directories that differ from the source. 
#//
function Robo_Copy([string] $Src, [string] $Dst) {
    
    # Use Powershell 5 style, multi-line progress bar
    if ((Get-Host).Version.Major -gt 5) { $PSStyle.Progress.View = 'Classic'; }

    # Robocopy.exe switches of interest:
    #  MIR = Mirror mode
    #  NDL = Don't log directory names
    #  NC  = Don't log file classes (existing, new file, etc.)
    #  BYTES = Show file sizes in bytes
    #  NJH = Do not display robocopy job header (JH)
    #  NJS = Do not display robocopy job summary (JS)
    #
    $rcArgs = """$Src"" ""$Dst"" /MIR /NDL /NC /BYTES /NJH /NJS";

    Write-Host "-I- Preparing to copy from '$Src' to '$Dst'...";

    $BytesToCopy, $FilesToCopy = RC_GetBytesFilesToCopy $rcArgs; 

    if ($BytesToCopy -gt 0) {    
        Write-Host ("-I- Ready to copy from '$Src' to '$Dst': {0} ($FilesToCopy files)" `
                     -f (RC_SmartBytes $BytesToCopy));

        RC_CopyWithProgress $rcArgs $BytesToCopy $FilesToCopy;

        Write-Host ("-I- Completed copy from '$Src' to '$Dst': {0} ($FilesToCopy files)" `
                     -f (RC_SmartBytes $BytesToCopy));
    }
    else {
        Write-Host "-I- No work to do: '$Src' and '$Dst' contain the same data.";
    }
}

function RC_GetBytesFilesToCopy([string] $rcArgs) {

    $BytesToCopy = 0;
    $FilesToCopy = 0;
   
    # Notes: 
    # 1. robocopy arg '/L' -> List only - don't copy, timestamp or delete any files.
    # 2. robocopy reports errors to STDOUT (ignore STDERR -> $null)
    #
    $rcExit, $rcOutput, $null = RunCmdWait "robocopy.exe" ($rcArgs + " /L");    # 1,2.
    RC_CheckExitCode $rcExit $rcOutput;                                         # 2.

    # File sizes (in bytes) are listed first, surrounded by white space
    $RegexBytes = '^\s*(\d+)\s+.*';

    # . Get the total number of files that will be copied
    # . Get the total number of bytes to be copied
    #
    $rcOutput.Split([Environment]::NewLine) -replace $RegexBytes, '$1' | Where-Object {$_} | Foreach-Object { `
        $BytesToCopy += $_;
        $FilesToCopy++; 
    }

    return $BytesToCopy, $FilesToCopy
}

function RC_CopyWithProgress([string] $rcArgs, [long] $BytesToCopy, [int] $FilesToCopy) {

    # constants / parameters:
    [int] $ReadLineDelay = 10;   # Delay(milliseconds) between reading lines of robocopy output
    [int] $ProgWaitCnt   = 5;    # Number of robocopy output lines to wait between progress bar updates
    
    # Copy job progress variables:
    $rcp = [RC_Progress]::new("Robocopy.exe /MIR from '$Src' to '$Dst':", $BytesToCopy, $FilesToCopy);
    [int] $ReadLineCnt = 0;
    
    # robocopy output parsing, regular expressions:
    [string] $RegexFileSize = '^\s*(\d+)\s+(.*)\s*$'; # e.g. ' 123 <file path> ' -> $1=123, $2=<file path>
    [string] $RegexPercent  = '^\s*([\d\.]+)%.*';     # e.g. ' 12.3%' -> $1=12.3
    
    # robocopy output parsing, line type
    enum RCLType {
        SIZE_PATH
        PROGRESS
        PROG_100
        OTHER
    }
    [RCLType] $lineType = [RCLType]::OTHER;
    
    # Start the Copy process
    [System.Diagnostics.Process] $rcProcess = RunCmd "robocopy.exe" $rcArgs;
    
    # while job is in progress: grab robocopy.exe output lines, one at a time...
    while (!$rcProcess.HasExited) {
        while ($line = $rcProcess.StandardOutput.ReadLine()) {

            # examine robocopy output line...
            switch -regex ($line) {
                '^\s*$'         { continue; } # ignore empty lines
                '^\s*100%'      { $lineType = [RCLType]::PROG_100; }
                $RegexFileSize  { $lineType = [RCLType]::SIZE_PATH; }
                $RegexPercent   { $lineType = [RCLTYPE]::PROGRESS; }
                Default         { $lineType = [RCLTYPE]::OTHER; }
            }

            switch ($lineType) {

                # We get a new size/path line...
                # 1. if we never saw 100% progress on the previous file, assume it completed; 
                #    update file/byte counters, accordingly.
                # 2. Grab the new file's size and path name
                SIZE_PATH {                     
                    if ($rcp.currFilePct -gt 0) {
                        $rcp.BytesCopied += $rcp.currFileSize;
                        $rcp.FilesCopied++;
                        $rcp.currFileSize = 0;
                        $rcp.currFilePct = 0;
                    }                   
                    $line -replace $RegexFileSize, '$1:$2' | Foreach-Object { `
                        $rcp.currFileSize, $rcp.currFileName = $_.split(':'); 
                    }
                }
                
                # We got 100% file progress... 
                # 1. update file/byte counters.
                # 2. clear current file size in case there are duplicate 100% lines
                PROG_100 { 
                    if ($rcp.currFileSize -gt 0) {
                        $rcp.BytesCopied += $rcp.currFileSize;
                        $rcp.FilesCopied++;
                        $rcp.currFileSize = 0;
                        $rcp.currFilePct = 0;
                    }
                }

                # We got a (non-100)% progress line... 
                # . update current file percentage
                PROGRESS {
                    $line -replace $RegexPercent, '$1' | Foreach-Object { $rcp.currFilePct = $_; }    
                }

                # We got an unexpected line -> ABORT 
                Default { 
                    Stop-Process $rcProcess;
                    Write-Error -Message "Unexpected robocopy output line: '$line'";
                    exit 1;
                }
            }

            # Update progress bar... but only on every ProgWaitCnt-th robocopy output line 
            if ( ($ReadLineCnt++ % $ProgWaitCnt) -eq 0 ) {
                RC_UpdateProgressBar $rcp
            }

            Start-Sleep -Milliseconds $ReadLineDelay
        }
    }

    # Check robocopy.exe exit code for success/ok status (throw on error)
    RC_CheckExitCode $rcProcess.ExitCode $rcProcess.StandardOutput.ReadToEnd(); 

    # Display final progress for a few seconds...
    RC_UpdateProgressBar $rcp
    Start-Sleep 2
}


# Invokes the given command w/ supplied arguments and redirected STDOUT|STDERR. 
# Returns the process object. 
#
function RunCmd([string] $cmd, [string] $cmdArgs) {
    $pInfo = New-Object System.Diagnostics.ProcessStartInfo
    $pInfo.FileName = $cmd
    $pInfo.RedirectStandardError = $true
    $pInfo.RedirectStandardOutput = $true
    $pInfo.UseShellExecute = $false
    $pInfo.Arguments = $cmdArgs
    $p = New-Object System.Diagnostics.Process
    $p.StartInfo = $pInfo
    $p.Start() | Out-Null
    return $p;
}

# Invokes and waits on the given command w/ supplied arguments. Returns a 
# list of command status & outputs: -> Exit code, STDOUT, STDERR
#
function RunCmdWait([string] $cmd, [string] $cmdArgs) {
    $p = RunCmd $cmd $cmdArgs;
    $cmdOut = $p.StandardOutput.ReadToEnd();
    $cmdErr = $p.StandardError.ReadToEnd();
    $p.WaitForExit();
    return $p.ExitCode, $cmdOut, $cmdErr;
}

function RC_CheckExitCode([int] $rcExit, [string] $rcError) {
    # Error codes can be combined... in general <codes> < 8 are not serious errors
    # 8  - Retry limit was exceeded.
    # 16 - Fatal error occurred.
    if( $rcExit -ge 8 ) {
        Write-Error -Message "-E- robocopy.exe returned error code $rcExit and message(s): $rcError";
        exit 1;
    }
}

# Returns a 'smart' representation in units of peta-/terra-/giga-/mega-/kilo-/bytes,
# as a string.  Also returns the 'unitScale' used for conversion, where
#   <smart value> = <bytes>/(1024^unitScale).
# 
function RC_GetSmartScale([long] $valueInBytes) {
    [int] $unitScale = 0;
    while( ($valueInBytes -ge 1024) -and ($unitScale -lt 5) ) {
        $valueInBytes = $valueInBytes -shr 10;  # /= 1024
        $unitScale++;
    }
    switch ($unitScale) {
        0 { $unitString = " Bytes"; Break }  # in Bytes
        1 { $unitString = " KB"; Break }  # in Kilobytes
        2 { $unitString = " MB"; Break }  # in Megabytes
        3 { $unitString = " GB"; Break }  # in Gigabytes
        4 { $unitString = " TB"; Break }  # in Terabytes
        5 { $unitString = " PB"; Break }  # in Petabytes
        Default { throw "This should never happen"; }
    }
    return @{
        unitScale  = $unitScale;
        unitString = $unitString
    };
}

function RC_SmartScaleBytes {
    param (
        [long] $valueInBytes,
         [int] $unitScale, 
         [string] $unitString = "", 
         [int] $decimalPlaces = 3
    )
    if ($unitScale -eq 0) { 
        return "${valueInBytes}${unitString}"; 
    }
    else {
        [double] $value = [double] $valueInBytes; 
        while( $unitScale-- -gt 0 ) { $value /= 1024.0; }
        return $value.ToString("n${decimalPlaces}") + $unitString;
    }
}

function RC_SmartBytes([long] $valueInBytes, [int] $decimalPlaces = 3) {
    $units = RC_GetSmartScale $valueInBytes;
    return RC_SmartScaleBytes $valueInBytes @units $decimalPlaces;
}

# Returns a representation of a progress pair of byte values
# in the form "<in-progress value> of <final value> <'smart' unit>".
# Both values are converted to the same 'smart' units which is 
# 
function RC_SmartProgressBytes([long] $inProgressBytes, [long] $finalBytes) {
    $units = RC_GetSmartScale $finalBytes;
    return "{0} of {1}" -f `
        (RC_SmartScaleBytes $inProgressBytes $units.unitScale), `
        (RC_SmartScaleBytes $finalBytes @units);
}

# Returns a 'smart' formatted time string with just the most
# relevant bits included.  For example, if a time-span is into
# the days, no need to include minutes/seconds.
#
function RC_SmartTimeSpan([Timespan] $ts) {
    if      ($ts.TotalDays -ge 1)    { $duration = "{0}d:{1}h"    -f $ts.Days, $ts.Hours; }
    elseif  ($ts.TotalHours -ge 1)   { $duration = "{0}h:{1:d2}m" -f $ts.Hours, $ts.Minutes; }
    elseif  ($ts.TotalMinutes -ge 1) { $duration = "{0}m:{1:d2}s" -f $ts.Minutes, $ts.Seconds; }
    else                             { $duration = "{0:n1}s"      -f $ts.TotalSeconds; }
    return $duration;
}

class RC_Progress {
    [string]    $Activity
    [DateTime]  $startTime
    [string]    $currFileName
    [long]      $currFileSize
    [float]     $currFilePct
    [long]      $FilesCopied
    [long]      $FilesToCopy
    [long]      $BytesCopied
    [long]      $BytesToCopy
    
    RC_Progress([string] $Activity, [long] $BytesToCopy, [long] $FilesToCopy) {
        $this.Activity = $Activity;
        $this.startTime = (Get-Date);
        $this.currFileName = "";
        $this.currFileSize = 0;
        $this.currFilePct = 0;
        $this.BytesCopied = 0;
        $this.BytesToCopy = $BytesToCopy;
        $this.FilesCopied = 0;
        $this.FilesToCopy = $FilesToCopy;
    }
}

function RC_UpdateProgressBar([RC_Progress] $rcp) {
    
    # . current file's bytes copied thus far
    $currFileBytesCopied = [long] ([double] $rcp.currFileSize * ($rcp.currFilePct/100.0));

    # . total bytes copied thus far
    $totBytesCopied = $currFileBytesCopied + $rcp.BytesCopied;

    # . copy job duration
    $timeSpan = New-TimeSpan -Start $rcp.startTime;
    $duration = RC_SmartTimeSpan $timeSpan;
    
    # . data rate
    $bytesPerSecond = (1000.0 * $totBytesCopied) / $timeSpan.TotalMilliseconds;

    # . estimated remaining time
    if ($bytesPerSecond -gt 0) {
        $ETA = RC_SmartTimeSpan (New-TimeSpan -Seconds (($BytesToCopy - $totBytesCopied) / $bytesPerSecond));
    }
    else {
        $ETA = "∞";
    }

    # . percentage done
    $percentDone = [float] ((100.0 * $totBytesCopied) / $rcp.BytesToCopy);

    # . update status text
    $status = "Copied {0} of {1} files, {2} *** Speed: {3}/s *** Duration: {4}, ETA: {5} *** {6:n2}% done" `
                -f  $rcp.FilesCopied, $rcp.FilesToCopy, `
                    (RC_SmartProgressBytes $totBytesCopied $rcp.BytesToCopy), `
                    (RC_SmartBytes $bytesPerSecond 1), $duration, $ETA, $percentDone;

    Write-Progress -Activity $rcp.Activity -Status $status -PercentComplete $percentDone;
}
