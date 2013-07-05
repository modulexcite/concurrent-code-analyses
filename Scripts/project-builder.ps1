# Build all solutions in the directory tree under this directory, grouped per subdirectory.

$pwd = pwd
$cwd = $pwd.Path
echo "Current directory: ${cwd}"

$logdirExists = Test-Path -PathType Container -Path logs
if (!$logdirExists) {
    mkdir logs | Out-Null

    if (!$?) {
        Return 0
    }
}

$logFile = [System.IO.Path]::GetFullPath($cwd + "\logs\0-build.main.log")

echo "Logging to: ${logFile}"

$dirs = ls

foreach ($dir in $dirs) {
    cd $dir

    $solutions = Get-ChildItem -Recurse -Filter "*.sln" | foreach { echo $_.FullName }
    
    foreach ($solution in $solutions) {
        $solutionSplitted = $solution.Split("\")
        $solutionBase = $solutionSplitted[$solutionSplitted.Length - 1]
        $solutionName = $solutionBase.Remove($solutionBase.Length - 4, 4)
        $logFileName = $cwd + "\logs\" + $dir.Name + "." + $solutionName + ".log"

        $solutionLogFile = [System.IO.Path]::GetFullPath($cwd + "\logs\" + $dir + "." + $solutionName + ".log")

        Write-Host -NoNewline "${dir}: ${solution}: "
        
        msbuild /m $solution | Out-File -Append -FilePath $solutionLogFile

        if ($?) {
            Write-Host "success"
            Write-Output "${dir}: ${solution}: success" | Out-File -Append -FilePath $logFile
        } else {
            Write-Host "failed"
            Write-Output "${dir}: ${solution}: failed" | Out-File -Append -FilePath $logFile
        }
    }

    cd ..
}
