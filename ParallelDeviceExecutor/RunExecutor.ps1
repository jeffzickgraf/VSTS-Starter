param([String] $assemblies = "", [String] $wherestatement="", [String] $baselocation="C:\a\1\s\")

$filePath = $baselocation + 'ParallelDeviceExecutor\bin\Release\ParallelDeviceExecutor.exe'
$workingDirectory = $baselocation + 'ParallelDeviceExecutor\bin\Release'
$arguments = "a:$assemblies" + "^w:" + $wherestatement
echo "aguments in ps are: " + $arguments
& C:\Users\jeffz\Source\Playground\arguments\EchoArgs.exe $arguments
#Start-Process -FilePath $filePath -Args $arguments -NoNewWindow -PassThru -Wait -WorkingDirectory $workingDirectory
