param([String] $assemblies = "", [String] $wherestatement="", [String] $baselocation="C:\a\1\s\")

$filePath = $baselocation + 'ParallelDeviceExecutor\bin\Release\ParallelDeviceExecutor.exe'
$workingDirectory = $baselocation + 'ParallelDeviceExecutor\bin\Release'
$arguments = "a:" + $assemblies + " w:" + $wherestatement
#& C:\Users\jeffz\Source\Playground\arguments\EchoArgs.exe "a:$assemblies", "w:$wherestatement" 
& -FilePath $filePath -Args "a:$assemblies", "w:$wherestatement" -NoNewWindow -PassThru -Wait -WorkingDirectory $workingDirectory
