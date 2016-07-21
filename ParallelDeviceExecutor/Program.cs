using Newtonsoft.Json;
using SharedComponents;
using SharedComponents.Parameters;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelDeviceExecutor
{
	/// <summary>
	/// This program copies files and an nunit console test runner to a folder for each 
	/// device in a json config file that are to be tested and then kicks off the tests.
	/// </summary>
	public class Program
	{		
		private static ProcessObserver ParallelProcessObserver;
		private static Object LockObject = new Object();
		private static string RunIdentifier;
		[STAThread]
		public static void Main(string[] args)
		{
			try
			{
				//Allows us to group device runs for logging purposes
				RunIdentifier = string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now);

				ParallelProcessObserver = new ProcessObserver();

				Trace.Listeners.Add(new TextWriterTraceListener("ParallelDeviceOutput.log", "myListener"));


				/*Args Note:
				 * Having difficulty sending in separated args from Powershell
				 * when there are spaces in the arguments, they split up the where statment. 
				 * Need to do some special handling to get around this. Using an "^" to split incoming args
				 * 
				 * Expecting possiblity of: a:TheTestAssembly.dll^w:category==xyz AND category==123
				 */
				var joinedArgs = string.Join(" ", args);
				Console.WriteLine("arguments are: " + joinedArgs);
				var argsSplit = joinedArgs.Split('^');

				string assemblyArgs = "";
				string whereFilter = "";
				if (argsSplit.Length > 0)
				{
					foreach (string arg in argsSplit)
					{
						Console.WriteLine("Reading Arg:" + arg);

						if (arg.StartsWith("a:"))
						{
							assemblyArgs = arg.Substring(2);
						}

						if (arg.StartsWith("w:"))
						{
							whereFilter = arg.Substring(2);							
						}						
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

				if (testParams.Devices.Count < 0)
				{
					Console.WriteLine("No devices found from JSON config file.");
#if DEBUG
					Console.ReadKey();
#endif
					return;
				}

				//Now create a mutex so that this console app can only be run 1 at a time.

				// get application GUID as defined in AssemblyInfo.cs
				string appGuid = "PerfectoParallelRunner-82678dc5439649959b6e0b686efb1222";

				// unique id for global mutex - Global prefix means it is global to the machine
				string mutexId = string.Format("Global\\{{{0}}}", appGuid);

				// Need a place to store a return value in Mutex() constructor call
				bool createdNew;

				var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
				var securitySettings = new MutexSecurity();
				securitySettings.AddAccessRule(allowEveryoneRule);

				using (var mutex = new Mutex(false, mutexId, out createdNew, securitySettings))
				{
					var hasHandle = false;
					try
					{
						try
						{
							hasHandle = mutex.WaitOne(5000, false);
							if (hasHandle == false)
								throw new TimeoutException("Make sure another Test Process isn't already running.");
						}
						catch (AbandonedMutexException)
						{
							// Log the fact that the mutex was abandoned in another process, it will still get acquired
							hasHandle = true;
						}

						// Perform your work here.
						RunParallelExecutions(assemblyArgs, whereFilter, testParams);
					}
					finally
					{
						if (hasHandle)
							mutex.ReleaseMutex();
					}
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
#if DEBUG
				Console.ReadKey();
#endif
				throw;
			}
		}

		private static void RunParallelExecutions(string assemblyArgs, string whereFilter, PerfectoTestParams testParams)
		{
			//Execute in parallel for each device
			Parallel.ForEach(testParams.Devices, device =>
			{
				Console.WriteLine("Starting runner for " + device.DeviceDetails.Name);
				StartTestRunner(device, assemblyArgs, whereFilter);
			});

			//Don't want to exit the program until all devices have processed and then we can release mutex
			do
			{
				Thread.Sleep(10000);
				Console.WriteLine(" --> Processing ... NUnit Runner count is: {0}", ParallelProcessObserver.GetStillRunningProcessCount());
			}
			while (getParallelCount() > 0);
		}

		private static int getParallelCount()
		{
			lock (LockObject) {
				return ParallelProcessObserver.GetStillRunningProcessCount();
			}
		}

		private static void StartTestRunner(Device device, string assemblyArgs, string whereFilter)
		{
			string currentPath = Directory.GetCurrentDirectory();
			string baseProjectPath = Path.GetFullPath(Path.Combine(currentPath, @"..\..\..\"));
			string runnerPath = Path.GetFullPath(Path.Combine(currentPath, @"..\..\..\")) + @"\NunitConsole";

			DirectoryInfo testRunDirectory = CopyTestRunner(device, currentPath, runnerPath);

			CloneStripDeviceListForSingleDevice(device, baseProjectPath, testRunDirectory);

			//Use the nunit3 console to run our tests
			//arguments for nunit3 look like:
			//		your-assembly-with-test-cases.dll --result=TestRun.xml;format=nunit2 --where "cat != AppiumTests"
			//Using format=nunit2 for VSTS compatibility (it can't process nunit3 results)
			var arguments = assemblyArgs + " --result=TestRun.xml;format=nunit2";
				
			string toSkip = GetTestsToSkipForDevice(device);

			string whereOptions = string.Empty;

			if (!string.IsNullOrEmpty(toSkip))
			{
				whereOptions = toSkip;
			}

			if (!string.IsNullOrEmpty(whereFilter) && !string.IsNullOrEmpty(whereOptions))
			{
				whereOptions += " and " + whereFilter;
			}
			else if (!string.IsNullOrEmpty(whereFilter) && string.IsNullOrEmpty(whereOptions))
			{
				whereOptions += " " + whereFilter;
			}

			//if device config needs to skip or where filter was passed in -add a where clause to our console argument
			if (!string.IsNullOrEmpty(whereOptions))
			{
				const string quote = "\"";
				arguments += " --where " + quote + whereOptions + quote;
			}

			Console.WriteLine("About to start nunit3-console.exe with the following arguments: " + arguments);
			
			Trace.TraceInformation("About to start nunit3-console.exe with the following arguments: " + arguments);
			Trace.Flush();


			Process nunitRunnerProcess = new Process();
			ProcessStartInfo nunitStartInfo
				= new ProcessStartInfo(testRunDirectory.FullName
				+ @"\nunit3-console.exe", arguments);

			nunitStartInfo.WindowStyle = ProcessWindowStyle.Normal;
			//Must set WorkingDirectory to execute from the location of the testrunner or it will pull from this exe's config
			nunitStartInfo.UseShellExecute = false;
			nunitStartInfo.WorkingDirectory = testRunDirectory.FullName;
			nunitRunnerProcess.StartInfo = nunitStartInfo;
			nunitRunnerProcess.OutputDataReceived += (sender, args) => OnDataReceived(args.Data);
			
			nunitRunnerProcess.Start();

			lock (LockObject)
			{
				ParallelProcessObserver.AddProcess(nunitRunnerProcess);
			}
		}

		private static void CloneStripDeviceListForSingleDevice(Device device, string baseProjectPath, DirectoryInfo testRunDirectory)
		{
			//Get device group again as a separate instance as we will remove other devices
			//	and deserialize our JSON Device configuration that will be used by each test run
			ParameterRetriever retriever = new ParameterRetriever();
			PerfectoTestParams testParams = retriever.GetVSOExecParam(baseProjectPath, true);

			//drop other devices
			testParams.Devices.RemoveAll(d => d.DeviceDetails.DeviceID != device.DeviceDetails.DeviceID);

			//Add our run identifier to the device details
			testParams.Devices.FirstOrDefault(d => d.DeviceDetails.DeviceID == device.DeviceDetails.DeviceID).DeviceDetails.RunIdentifier = RunIdentifier;

			//Now save to disk
			JsonSerializer serializer = new JsonSerializer();
			
			Console.WriteLine("Writing config file for " + device.DeviceDetails.Name ?? device.DeviceDetails.DeviceID);
			var runConfigPath = testRunDirectory.FullName + "\\TestResources\\DevicesGroup\\" + SharedConstants.DeviceConfigFileName;
			Directory.CreateDirectory(Path.GetDirectoryName(runConfigPath));
			using (StreamWriter fileStream = new StreamWriter(runConfigPath,false))
			using(JsonWriter writer = new JsonTextWriter(fileStream))
			{
				serializer.Serialize(writer, testParams);				
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
			Trace.TraceInformation(data);
			Trace.Flush();
		}

		//Copies files needed for the testrunner into a device specific directory
		private static DirectoryInfo CopyTestRunner(Device device, string currentPath, string runnerPath)
		{
			//Make folder
			DirectoryInfo testRunDirectory = Directory.CreateDirectory(runnerPath + @"\TestRuns\" + device.DeviceDetails.Name + device.DeviceDetails.DeviceID);

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
