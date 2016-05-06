﻿using SharedComponents;
using SharedComponents.Parameters;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

//This code copies files and an nunit console test runner to a folder for each device 
//that is to be tested and then kicks off the tests

namespace MultiTestExecutor
{
	public class Program
	{
		public static void Main(string[] args)
		{
			try
			{
				string assemblyArgs = "";
				if (args.Length > 0)
				{
					foreach (string arg in args)
					{
						assemblyArgs += arg + " ";
					}
				}
				else
				{
					Console.WriteLine("No assembly passed in so using VSTSDigitalDemoTests.dll");
					assemblyArgs = "VSTSDigitalDemoTests.dll";
				}

				//Get the devices to run for 
				string currentPath = Directory.GetCurrentDirectory();
				string baseProjectPath = Path.GetFullPath(Path.Combine(currentPath, @"..\..\..\"));
				ParameterRetriever retriever = new ParameterRetriever();
				PerfectoTestParams testParams = retriever.GetVSOExecParam(baseProjectPath, true);

				//Execute in parallel for each device
				Parallel.ForEach(testParams.Devices, device => {
					Console.WriteLine("Starting runner for " + device.DeviceDetails.Name);
					StartTestRunner(device, assemblyArgs);
				});

			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				throw;
			}
		}

		private static void StartTestRunner(Device device, string assemblyArgs)
		{
			string currentPath = Directory.GetCurrentDirectory();
			string baseProjectPath = Path.GetFullPath(Path.Combine(currentPath, @"..\..\..\"));
			string runnerPath = Path.GetFullPath(Path.Combine(currentPath, @"..\..\..\")) + @"\NunitConsole";

			DirectoryInfo testRunDirectory = CopyTestRunner(device, currentPath, runnerPath);

			CloneDeviceListFile(device, baseProjectPath, testRunDirectory);

			//Use the nunit3 console to run our tests
			//arguments for nunit3 look like:
			//		your-assembly-with-test-cases.dll --result=TestRun.xml;format=nunit2 --where "cat != AppiumTests"
			//Using format=nunit2 for VSTS compatibility (it can't process nunit3 results)
			var arguments = assemblyArgs + " --result=TestRun.xml;format=nunit2";

			string toSkip = GetTestsToSkipForDevice(device);

			//if device config needs to skip -add a where clause to our console argument
			if (!string.IsNullOrEmpty(toSkip))
			{
				arguments += " --where \"" + toSkip + "\"";
			}

			Console.WriteLine("About to start nunit3-console.exe with the following arguments: " + arguments);

			Process myProcess = new Process();
			ProcessStartInfo myProcessStartInfo
				= new ProcessStartInfo(testRunDirectory.FullName
				+ @"\nunit3-console.exe", arguments);

			myProcessStartInfo.WindowStyle = ProcessWindowStyle.Normal;
			//Must set WorkingDirectory to execute from the location of the testrunner or it will pull from this exe's config
			myProcessStartInfo.UseShellExecute = false;
			myProcessStartInfo.WorkingDirectory = testRunDirectory.FullName;
			myProcess.StartInfo = myProcessStartInfo;
			myProcess.OutputDataReceived += (sender, args) => OnDataReceived(args.Data);
			myProcess.Start();
		}

		private static void CloneDeviceListFile(Device device, string baseProjectPath, DirectoryInfo testRunDirectory)
		{
			//Get device group again as a separate instance as we will remove other devices
			//	and deserialize our JSON Device configuration that will be used by each test run
			ParameterRetriever retriever = new ParameterRetriever();
			PerfectoTestParams testParams = retriever.GetVSOExecParam(baseProjectPath, true);

			//drop other devices
			testParams.Devices.RemoveAll(d => d.DeviceDetails.DeviceID != device.DeviceDetails.DeviceID);

			//Now save to memory and then a filestream to disk
			MemoryStream memoryStream = new MemoryStream();
			DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(PerfectoTestParams));
			serializer.WriteObject(memoryStream, testParams);

			Console.WriteLine("Writing config file for " + device.DeviceDetails.Name ?? device.DeviceDetails.DeviceID);
			var runConfigPath = testRunDirectory.FullName + "\\TestResources\\DevicesGroup\\" + SharedConstants.DeviceConfigFileName;
			Directory.CreateDirectory(Path.GetDirectoryName(runConfigPath));
			using (FileStream fileStream = new FileStream(runConfigPath, FileMode.Create, FileAccess.Write))
			{
				memoryStream.WriteTo(fileStream);
			}
		}

		private static string GetTestsToSkipForDevice(Device device)
		{
			//See if we are supposed to skip Appium or Web tests for this device
			string toSkip = string.Empty;

			//If both 
			if (!device.DeviceDetails.RunWeb && !device.DeviceDetails.RunNative)
			{
				toSkip = "cat != WebTests and cat != AppiumTests";
			}
			else if (!device.DeviceDetails.RunWeb)
			{
				toSkip = "cat != WebTests";
			}
			else if (!device.DeviceDetails.RunNative || device.DeviceDetails.IsDesktopBrowser)
			{
				toSkip = "cat != AppiumTests";
			}

			return toSkip;
		}

		//output to the console any output from the runner
		private static void OnDataReceived(string data)
		{
			if (string.IsNullOrEmpty(data))
			{
				return;
			}

			Console.WriteLine(data);			
		}

		//Copies files needed for the testrunner into a device specific directory
		private static DirectoryInfo CopyTestRunner(Device device, string currentPath, string runnerPath)
		{
			//Make folder
			DirectoryInfo testRunDirectory = Directory.CreateDirectory(runnerPath + @"\TestRuns\" + device.DeviceDetails.Name);

			//Copy only the test runner first - there are possibly previous run directories so don't copy that folder
			DirectoryCopy(runnerPath, testRunDirectory.FullName, false);
			//Now need the addins for the nunit2 compatability
			DirectoryCopy(runnerPath + "\\addins", testRunDirectory.FullName + "\\addins", true);
			//Now the test cases
			DirectoryCopy(currentPath, testRunDirectory.FullName, false);
				
			return testRunDirectory;
		}

		private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);
			}

			DirectoryInfo[] dirs = dir.GetDirectories();
			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, true);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs)
			{
				foreach (DirectoryInfo subdir in dirs)
				{
					string temppath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, copySubDirs);
				}
			}
		}
	}
}
