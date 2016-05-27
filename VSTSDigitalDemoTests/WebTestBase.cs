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
using System.Threading;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Reflection;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;

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
		protected static int ErrorCount;

		/// <summary>
		/// To be called from concrete test fixtures to initialize the test run.
		/// </summary>		
		protected static RemoteWebDriverExtended InitializeDriver()
		{
			string model = "Unknown device model";
			try
			{
				Trace.Listeners.Add(new TextWriterTraceListener("webTestCaseExecution.log", "webTestCaseListener"));

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

					//WindTunnel only for devices
					capabilities.SetCapability("windTunnelPersona", "Georgia");
				}

				var url = new Uri(string.Format("https://{0}/nexperience/perfectomobile/wd/hub", Host));
				RemoteWebDriverExtended driver = new RemoteWebDriverExtended(url, capabilities, new TimeSpan(0, 2, 0));
				driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(15));
				DriverInstance = driver;
				return driver;
			}
			catch (Exception e)
			{
				HandleGeneralException(e, model);
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

				//get device execution identifier
				String executionId = GetExecutionId(driver);
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
				HandleGeneralException(ex, model, false);
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
				BaseProjectPath, TestCaseName, CurrentDevice.DeviceDetails, ErrorCount);

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

		private static void LogFailedDeviceExecution(string executionId, string status, string description)
		{
			ExecutionRecorder recorder = new ExecutionRecorder();
			ExecutionRecorderParams recorderParams
				= new ExecutionRecorderParams(executionId, Host, Username, Password, TestType.Selenium,
				BaseProjectPath, TestCaseName, CurrentDevice.DeviceDetails, ErrorCount);
			ExecutionDetails details = new ExecutionDetails();

			if (executionId == Constants.UNKNOWN)
			{
				details.executionId = executionId;
			}
			else
			{
				//although we will be overwriting some values - try to get other details like reportKey
				details = recorder.GetExecutionDetails(recorderParams).Result;
			}

			details.status = status;
			details.description = description;
			details.reason = status;
			details.testMethodName = GetTestMethodName();

			recorder.RecordExecutionRun(recorderParams, details);
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
			if (!string.IsNullOrEmpty(CurrentDevice.DeviceDetails.Name))
				return CurrentDevice.DeviceDetails.Name;

			Dictionary<String, Object> pars = new Dictionary<String, Object>();
			pars.Add("property", "model");
			String properties = (String)driver.ExecuteScript("mobile:handset:info", pars);
			return properties;			
		}

		protected static void HandleNoElementException(NoSuchElementException nsee, RemoteWebDriverExtended driver)
		{
			HandleNoElementException(nsee, GetDeviceModel(driver));			
		}

		protected static void HandleNoElementException(NoSuchElementException nsee, string deviceModel, bool shouldThrow = true)
		{
			var message = deviceModel + "  in Test: " + TestContext.CurrentContext.Test.Name + " had Element not found: " + nsee.Message;
			Console.WriteLine(message);
			if (shouldThrow)
			{
				LogFailedDeviceExecution(GetExecutionId(DriverInstance), Constants.UNHANDLEDEX, message);
				ErrorCount++;
				throw new NoSuchElementException(message, nsee);
			}
			
		}

		protected static void HandleGeneralException(Exception e, RemoteWebDriverExtended driver)
		{
			HandleGeneralException(e, (GetDeviceModel(driver)));
		}

		protected static void HandleGeneralException(Exception e, string deviceModel, bool shouldThrow = true)
		{
			var message = deviceModel + " Encountered an error: " + e.Message + "stacktrace: " + e.StackTrace;
			Console.WriteLine(message);

			if (shouldThrow)
			{
				LogFailedDeviceExecution(GetExecutionId(DriverInstance), Constants.UNHANDLEDEX, message);
				ErrorCount++;
				throw new Exception(message, e); 
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

	}
}
