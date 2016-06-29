param([String] $assemblies = "", [String] $wherestatement="", [String] $baselocation="C:\a\1\s\")

$filePath = $baselocation + 'ParallelDeviceExecutor\bin\Release\ParallelDeviceExecutor.exe'
$workingDirectory = $baselocation + 'ParallelDeviceExecutor\bin\Release'
Start-Process -FilePath $filePath -Args "a:$assemblies", "w:$wherestatement" -NoNewWindow -PassThru -Wait -WorkingDirectory $workingDirectory
