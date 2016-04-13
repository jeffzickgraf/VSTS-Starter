
param([string] $assemblies = "")
$psi = New-object System.Diagnostics.ProcessStartInfo 
$psi.CreateNoWindow = $true 
$psi.UseShellExecute = $false 
$psi.WorkingDirectory = 'C:\a\1\s\MultiTestExecutor\bin\Release'
$psi.RedirectStandardOutput = $true 
$psi.RedirectStandardError = $true 
$psi.FileName = 'C:\a\1\s\MultiTestExecutor\bin\Release\MultiTestExecutor.exe'
$psi.Arguments = $assemblies
$process = New-Object System.Diagnostics.Process 
$process.StartInfo = $psi 
[void]$process.Start()
$output = $process.StandardOutput.ReadToEnd() 
$process.WaitForExit() 
$output

