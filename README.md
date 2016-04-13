# Perfecto VSTS Digital Demo

This project requires the use of Visual Studio 2015 or Visual Studio 2013.

This project requires the consumer to add an appsettings.config file next to the app.config file that will contain your credentials and target Perfecto cloud. The filename appsettings.config has been added to the .gitignore file to prevent credentials from being accidently committed to source control.

```sh
<appSettings>
		<add key="PerfectoCloud" value="YOUR-CLOUD"/> 
		<add key="PerfectoUsername" value="YOUR-USERNAME"/>
		<add key="PerfectoPassword" value="YOUR-PASSWORD"/>		
</appSettings>
```

See the [Read-Me-For-Configuration.txt file for more details]VSTSDigitalDemoTests/Read-Me-For-Configuration.txt).


Additionally, you will need to provide your device identifiers in the SharedComponents/TestResources/DevicesGroup/ files.
```sh
 //TODO: Provide your device IDs eg. 125157DF53B1BA11F
 "devices": [    
    {
      "device": {
        "os": "windows",
        "osVersion": "7",
        "browserName": "Chrome",
        "browserVersion": "49",
        "name": "chrome",
        "isDesktopBrowser": "true"
      }
    },
    {
      "device": {
        "deviceID": "CA7EEEAADD92242C66A32807B538BDACFAA5A0DB",
        "os": "iOS",
        "name": "iPhone6s",
         "isDesktopBrowser": "false"
      }
    },
```

If you are using Visual Studio 2013, you must change the 'provider' parameter passed into the test runner under MultiTestExecutor/Program.cs to use 2013 instead of 2015

```sh
	//These are the arguments:
	//If using VS 2013 - change provider: to VSTEST_2013
	//provider:VSTEST_2015 root:TestResults threadcount:1 out:result.trx plevel:TestCase YOUR-DLL-WITH-TEST-CASES.dll
	Process myProcess = new Process();
	ProcessStartInfo myProcessStartInfo
		= new ProcessStartInfo(testRunDirectory.FullName 
		+ @"\ParallelTestRunner.exe", "provider:VSTEST_2015 root:TestResults threadcount:1 out:result.trx plevel:TestCase " + assemblyArgs);

```