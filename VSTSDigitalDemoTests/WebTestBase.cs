using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using SharedComponents.Parameters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VSTSDigitalDemoTests.Utility;
using NUnit.Framework;
using System.Collections;

namespace VSTSDigitalDemoTests
{
	public abstract class WebTestBase
	{
		protected static PerfectoTestParams PerfectoTestingParameters;
		protected static string TestCaseName;
		protected static Device CurrentDevice;
		protected static string TestRunLocation;
		protected static string BaseProjectPath;
		protected static string Host;
		protected static string Username;
		protected static string Password;
		protected static RemoteWebDriverExtended DriverInstance;		
		protected static List<ExecutionError> ExecutionErrors;
		private static ArrayList startedVitals = new ArrayList();

		/// <summary>
		/// To be called from concrete test fixtures to initialize the test run.
		/// </summary>		
		protected static RemoteWebDriverExtended InitializeDriver()
		{
			string model = "Unknown device model";
			try
			{
				Trace.Listeners.Add(new TextWriterTraceListener("webTestCaseExecution.log", "webTestCaseListener"));

				ExecutionErrors = new List<ExecutionError>();
				BaseProjectPath = Path.GetFullPath(Path.Combine(TestRunLocation, @"..\..\..\"));
				
				SensitiveInformation.GetHostAndCredentials(BaseProjectPath, out Host, out Username, out Password);

				ParameterRetriever testParams = new ParameterRetriever();
				PerfectoTestingParameters = testParams.GetVSOExecParam(BaseProjectPath, false);

				CurrentDevice = PerfectoTestingParameters.Devices.FirstOrDefault();
				if (string.IsNullOrEmpty(CurrentDevice.DeviceDetails.RunIdentifier))
					CurrentDevice.DeviceDetails.RunIdentifier = string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now);

				CheckForValidConfiguration();

				model = CurrentDevice.DeviceDetails.Name ?? "Unknown device Model";

				Trace.Listeners.Add(new ConsoleTraceListener());
				Trace.AutoFlush = true;

				var browserName = "mobileOS";

				if (CurrentDevice.DeviceDetails.IsDesktopBrowser)
				{
					browserName = CurrentDevice.DeviceDetails.BrowserName;
				}

				DesiredCapabilities capabilities = new DesiredCapabilities(browserName, string.Empty, new Platform(PlatformType.Any));
				capabilities.SetCapability("user", Username);
				capabilities.SetCapability("password", Password);
				capabilities.SetCapability("newCommandTimeout", "120");
				capabilities.SetPerfectoLabExecutionId(Host);

				capabilities.SetCapability("scriptName", "Parallel-" + TestCaseName);

				if (CurrentDevice.DeviceDetails.IsDesktopBrowser)
				{
					capabilities.SetCapability("platformName", CurrentDevice.DeviceDetails.OS);
					capabilities.SetCapability("platformVersion", CurrentDevice.DeviceDetails.OSVersion);
					capabilities.SetCapability("browserName", CurrentDevice.DeviceDetails.BrowserName);
					capabilities.SetCapability("resolution", "1366x768");
					capabilities.SetCapability("version", CurrentDevice.DeviceDetails.BrowserVersion);
				}
				else
				{
					capabilities.SetCapability("deviceName", CurrentDevice.DeviceDetails.DeviceID);

					//WindTunnel only for devices and only when available
					if (AppSettingsRetriever.IsWindTunnelEnabled())
						capabilities.SetCapability("windTunnelPersona", "Georgia");
				}

				var url = new Uri(string.Format("https://{0}/nexperience/perfectomobile/wd/hub", Host));
				RemoteWebDriverExtended driver = new RemoteWebDriverExtended(url, capabilities, new TimeSpan(0, 2, 0));
				driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(15));
				DriverInstance = driver;

				startedVitals.Add("outputs.monitors.cpu.total");
				startedVitals.Add("outputs.monitors.memory.used");
				StartVitals(1, startedVitals);				
				
				return driver;
			}
			catch (Exception e)
			{
				HandleGeneralException(e, DriverInstance);
			}
			return null;
		}

		private static void CheckForValidConfiguration()
		{
			if (CurrentDevice == null)
				Assert.Inconclusive("No device found in PerfectoTestingParameters.");

			if (!CurrentDevice.DeviceDetails.RunWeb)
				Assert.Inconclusive(string.Format("Web run turned off for: {0}.", CurrentDevice.DeviceDetails.Name));
		}

		/// <summary>
		/// A text checkpoint using perfecto's extended mobile:checkpoint:text command.
		/// </summary>
		/// <param name="textToFind">The needle text you are looking for in your haystack.</param>
		/// <param name="timeoutInSeconds">Timeout in seconds.</param>
		/// <returns>Bool indicating if the text was found.</returns>
		protected static bool Checkpoint(string textToFind, RemoteWebDriverExtended driver, int? timeoutInSeconds = 25)
		{
			Console.WriteLine(string.Format("Checking text {0} on {1}", textToFind, GetDeviceModel(driver)));

			long uxTimer;
			return PerfectoUtils.OCRTextCheckPoint(out uxTimer, driver, textToFind, 0, timeoutInSeconds ?? 25, true, "Checkpoint timer", "Checkpoint timer");
		}
				
		public static void PerfectoCloseConnection(RemoteWebDriverExtended driver)
		{
			string model = "unknown";
			try
			{
				//get our model before we close driver
				model = GetDeviceModel(driver);

				//get device execution identifier and stop vitals
				String executionId = GetExecutionId(driver);
				StopStartedVitals();

				driver.Close();

				Dictionary<String, Object > param = new Dictionary<String, Object>();
				driver.ExecuteScript("mobile:execution:close", param);
				
				string currentPath = TestRunLocation;
				string newPath = Path.GetFullPath(Path.Combine(currentPath, @"..\..\..\RunReports\"));
				driver.DownloadReport(DownloadReportTypes.pdf, newPath + "\\" + model + " " + TestCaseName + " report");
				//driver.DownloadAttachment(DownloadAttachmentTypes.video, newPath + "\\" + model + " " + TestCaseName + " video", "flv");
				//driver.DownloadAttachment(DownloadAttachmentTypes.image, "C:\\test\\report\\images", "jpg");
				LogDeviceExecution(executionId);
			}
			catch (Exception ex)
			{
				var message = string.Format("Failed to aqcuire new driver instance for {0} with message {1} and stacktrace {2}",
					CurrentDevice, ex.Message, ex.StackTrace);
				Console.WriteLine(message);

				HandleGeneralException(ex, DriverInstance, false);
			}
			
			driver.Quit();						
		}

		private static string GetExecutionId(RemoteWebDriverExtended driver)
		{
			string executionId = Constants.UNKNOWN;
			try
			{
				return driver.Capabilities.GetCapability("executionId").ToString();
			}
			catch (Exception)
			{
				//Couldn't get it just return unknown
			}
			return executionId;
		}

		private static void LogDeviceExecution(string executionId)
		{
			ExecutionRecorder recorder = new ExecutionRecorder();
			ExecutionRecorderParams recorderParams
				= new ExecutionRecorderParams(executionId, Host, Username, Password, TestType.Selenium,
				BaseProjectPath, TestCaseName, CurrentDevice.DeviceDetails, ExecutionErrors);

			if (executionId == Constants.UNKNOWN)
			{
				//No id to get details - need to create our own details
				ExecutionDetails details = new ExecutionDetails();
				details.executionId = executionId;
				details.status = Constants.UNKNOWN;
				details.description = "Tried to get execution details but failed.";
				details.reason = Constants.UNKNOWN;
				recorder.RecordExecutionRun(recorderParams, details);
				return;
			}

			//We have an executionId so try to get run results.
			recorder.GetAndRecordExecutionRun(recorderParams);
		}		

		private static string GetTestMethodName()
		{
			string testMethod = "Unknown";
			try
			{
				testMethod = TestContext.CurrentContext.Test.Name;
			}
			catch (Exception)
			{
				//ignore as we knew this may happen				
			}
			return testMethod;
		}


		/// <summary>
		/// The device's identifier for the test run.
		/// </summary>
		public static String GetDeviceModel(RemoteWebDriverExtended driver)
		{
			try
			{
				if (!string.IsNullOrEmpty(CurrentDevice.DeviceDetails.Name))
					return CurrentDevice.DeviceDetails.Name;

				Dictionary<String, Object> pars = new Dictionary<String, Object>();
				pars.Add("property", "model");
				String properties = (String)driver.ExecuteScript("mobile:handset:info", pars);
				return properties;
			}
			catch (Exception)
			{
				return Constants.UNKNOWN;
			}
		}

		protected static void HandleNoElementException(NoSuchElementException nsee, RemoteWebDriverExtended driver)
		{
			HandleNoElementException(nsee, driver, false);			
		}

		protected static void HandleNoElementException(NoSuchElementException nsee, RemoteWebDriverExtended driver, bool shouldThrow = true)
		{
			var model = GetDeviceModel(driver);
			var message = model + "  in Test: " + GetTestMethodName() + " had Element not found: " + nsee.Message;
			Console.WriteLine(message + " stacktrace: " + nsee.StackTrace);
			if (shouldThrow)
			{
				ExecutionErrors.Add(new ExecutionError(message, GetTestMethodName(), nsee));
				throw nsee;
			}			
		}
		

		protected static void HandleGeneralException(Exception e, RemoteWebDriverExtended driver)
		{
			HandleGeneralException(e, driver, false);
		}

		protected static void HandleGeneralException(Exception e, RemoteWebDriverExtended driver, bool shouldThrow = true)
		{
			var model = GetDeviceModel(driver);
			var message = model + " Encountered an error: " + e.Message;
			Console.WriteLine(message + " stacktrace: " + e.StackTrace);

			if (shouldThrow)
			{
				ExecutionErrors.Add(new ExecutionError(message, GetTestMethodName(), e));
				throw e; 
			}
				
		}

		protected bool IsDeskTopBrowser
		{
			get { return CurrentDevice.DeviceDetails.IsDesktopBrowser; }
		}

		protected bool IsMobileDevice
		{
			get { return !CurrentDevice.DeviceDetails.IsDesktopBrowser; }
		}

		// Wind Tunnel: Gets the user experience (UX) timer
		private static long TimerGet(String timerType, RemoteWebDriverExtended driver)
		{
			String command = "mobile:timer:info";
			Dictionary<String, Object> pars = new Dictionary<String, Object>();
			pars.Add("type", timerType);
			long result = (long)driver.ExecuteScript(command, pars);
			return result;
		}

		public static void TakeTimerIfPossible(string message, RemoteWebDriverExtended driver)
		{
			try
			{
				driver.GetScreenshot();
				long uxTimer = TimerGet("ux", driver);				
				Console.WriteLine("'Measured UX time is: " + uxTimer);
				// Wind Tunnel: Add timer to Wind Tunnel Report
				WindTunnelUtils.ReportTimer(driver, uxTimer, 8000, message, "uxTimer");
			}
			catch (NullReferenceException nex)
			{
				Console.WriteLine("Unable to take timer for " + message + " Error: " + nex.Message);
			}
		}

		protected void SetPointOfInterestIfPossible(RemoteWebDriverExtended driver, string pointOfInterestText, PointOfInterestStatus status)
		{
			//WindTunnel support coming later this year for desktop browsers - for now, ignore
			if (CurrentDevice.DeviceDetails.IsDesktopBrowser)
				return;

			WindTunnelUtils.PointOfInterest(driver, pointOfInterestText, status);
		}

		protected bool IsElementPresent(string elementToFindAsXpath, RemoteWebDriverExtended driver)
		{
			try
			{
				IWebElement field;
				field = driver.FindElementByXPath(elementToFindAsXpath);
				if (field == null)
					return false;
			}
			catch (NoSuchElementException)
			{
				return false;
			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}

		private static void StartVitals(long interval, ArrayList vitals)
		{
			String command = "mobile:monitor:start";
			Dictionary<string, object> execParams = new Dictionary<string, object>();
			List<String> vitalsList = new List<String>();
			foreach (String vital in vitals)
			{
				vitalsList.Add(vital);
			}
			execParams.Add("vitals", vitalsList);
			execParams.Add("interval", interval.ToString());
			DriverInstance.ExecuteScript(command, execParams);
		}

		private static void StopVitals(List<String> vitals)
		{
			String command = "mobile:monitor:stop";
			Dictionary<string, object> execParams = new Dictionary<string, object>();
			List<String> vitalsList = new List<String>();
			foreach (String vital in vitals)
			{
				vitalsList.Add(vital);
			}
			execParams.Add("vitals", vitalsList);
			DriverInstance.ExecuteScript(command, execParams);
			foreach (String vital in vitals)
			{
				startedVitals.Remove(vital);
			}
		}

		private static void StopStartedVitals()
		{
			if (startedVitals.Count > 0)
			{
				String command = "mobile:monitor:stop";
				Dictionary<string, object> execParams = new Dictionary<string, object>();
				execParams.Add("vitals", startedVitals);
				DriverInstance.ExecuteScript(command, execParams);
				startedVitals.Clear();
			}
		}

	}
}
