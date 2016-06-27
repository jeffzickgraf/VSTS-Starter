
param([String] $assemblies = "", [String] $wherestatement="", [String] $baselocation="C:\a\1\s\")
$psi = New-object System.Diagnostics.ProcessStartInfo 
$psi.CreateNoWindow = $true 
$psi.UseShellExecute = $false 
$psi.WorkingDirectory = $baselocation + 'ParallelDeviceExecutor\bin\Release'
$psi.RedirectStandardOutput = $true 
$psi.RedirectStandardError = $true 
$psi.FileName = $baselocation + 'ParallelDeviceExecutor\bin\Release\ParallelDeviceExecutor.exe'
$psi.Arguments = "a:" + $assemblies + " \`"w:" +  $wherestatement + "\`""
$process = New-Object System.Diagnostics.Process 
$process.StartInfo = $psi 
$process.Start()
$output = $process.StandardOutput.ReadToEnd() 
$process.WaitForExit() 
$output

