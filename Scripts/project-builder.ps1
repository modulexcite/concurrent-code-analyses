# This script reads the file solutions.txt in the current working directory, and builds
# every solution (*.sln) or project (*.csproj) file with msbuild.

# The solutions.txt file should only contain filenames, one on each line.

# Log files can be found in logs/ in the current working directory.
# The log file prefixed with "0-" contains the general results of the build (i.e., success/failure per solution file.
# The rest of the files in the logs/ directory contain the output of msbuild for each solution file respectively.

# Build a solution, logging to a solution-specific log file in the logs/ directory.
Function Build-Solution ($solution)
{
    $solutionSplitted = $solution.Split("\")
    $solutionName = $solutionSplitted[$solutionSplitted.Length - 1]

    $solutionLogFile = [System.IO.Path]::GetFullPath($cwd + "\logs\" + $solutionName + ".log")

    Write-Host -NoNewline "${solution}: "

	SET EnableNuGetPackageRestore=true
	devenv $solution /rebuild "Debug" /out $solutionLogFile

    if ($?) {
        Write-Host "success"
        Write-Output "${solution}: success" | Out-File -Append -FilePath $logFile
    } else {
        Write-Host "failed"
        Write-Output "${solution}: failed" | Out-File -Append -FilePath $logFile
    }
}

###
### Actual start of the script
###

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

$solutions = Get-Content "solutions.txt"

foreach ($solution in $solutions) {
    Build-Solution $solution
}
