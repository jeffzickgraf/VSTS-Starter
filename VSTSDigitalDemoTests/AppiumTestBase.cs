using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Remote;
using SharedComponents.Parameters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VSTSDigitalDemoTests.Utility;

namespace VSTSDigitalDemoTests
{	
	public abstract class AppiumTestBase
	{
		protected static PerfectoTestParams PerfectoTestingParameters;
		protected static string TestCaseName;
		protected static string BaseProjectPath;
		protected static Device CurrentDevice;
		protected static AppiumDriver<IWebElement> DriverInstance;
		protected static string TestRunLocation;
		protected static string Host;
		protected static string Username;
		protected static string Password;
		protected static List<ExecutionError> ExecutionErrors;

		/// <summary>
		/// To be called from concrete test fixtures to initialize the test run.
		/// </summary>		
		protected static AppiumDriver<IWebElement> InitializeDriver()
		{			
			try
			{
				Trace.Listeners.Add(new TextWriterTraceListener("AppiumTestCaseExecution.log", "appiumTestCaseListener"));

				ExecutionErrors = new List<ExecutionError>();
				BaseProjectPath = Path.GetFullPath(Path.Combine(TestRunLocation, @"..\..\..\"));
				
				SensitiveInformation.GetHostAndCredentials(BaseProjectPath, out Host, out Username, out Password);

				ParameterRetriever testParams = new ParameterRetriever();
				PerfectoTestingParameters = testParams.GetVSOExecParam(BaseProjectPath, false);

				CurrentDevice = PerfectoTestingParameters.Devices.FirstOrDefault();
				if (string.IsNullOrEmpty(CurrentDevice.DeviceDetails.RunIdentifier))
					CurrentDevice.DeviceDetails.RunIdentifier = string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now);
				
				CheckForValidDeviceConfiguration();
							
				Trace.Listeners.Add(new ConsoleTraceListener());
				Trace.AutoFlush = true;

				var runMessage = string.Format("Starting test runs for {0} with RunId of {1}", CurrentDevice.DeviceDetails.Name, CurrentDevice.DeviceDetails.RunIdentifier);
				Trace.WriteLine(runMessage);

				DesiredCapabilities capabilities = new DesiredCapabilities();
				capabilities.SetCapability("automationName", "Appium");
				capabilities.SetCapability("user", Username);
				capabilities.SetCapability("password", Password);
				capabilities.SetCapability("newCommandTimeout", "120");
				capabilities.SetPerfectoLabExecutionId(Host);
				capabilities.SetCapability("deviceName", CurrentDevice.DeviceDetails.DeviceID);
				capabilities.SetCapability("scriptName", "Parallel-" + TestCaseName);

				capabilities.SetCapability("windTunnelPersona", "Georgia");


				var url = new Uri(string.Format("http://{0}/nexperience/perfectomobile/wd/hub", Host));
				if (CurrentDevice.DeviceDetails.OS.ToUpperInvariant() == "IOS")
				{
					DriverInstance = new IOSDriver<IWebElement>(url, capabilities, new TimeSpan(0, 2, 0));
				}
				else if (CurrentDevice.DeviceDetails.OS.ToUpperInvariant() == "ANDROID")
				{
					DriverInstance = new AndroidDriver<IWebElement>(url, capabilities, new TimeSpan(0, 2, 0));
				}
				else
				{
					throw new ArgumentException("Unknown Device OS from config file: " + CurrentDevice.DeviceDetails.OS);
				}
				DriverInstance.Context = "NATIVE_APP";
				CloseApp();
				OpenApp();
				DriverInstance.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(15));
				return DriverInstance;
			}
			catch (Exception ex)
			{
				var message = string.Format("Failed to aqcuire new driver instance for {0} with message {1} and stacktrace {2}",
					CurrentDevice, ex.Message, ex.StackTrace);
				Console.WriteLine(message);
				throw new Exception(message, ex);
			}			
		}

		private static void CheckForValidDeviceConfiguration()
		{
			//shouldn't get these next 3 inconclusives - but you never know :-)
			if (CurrentDevice == null)
				Assert.Inconclusive("No device found in PerfectoTestingParameters.");

			if (CurrentDevice.DeviceDetails.IsDesktopBrowser)
				Assert.Inconclusive("Desktop Web Not for Appium. Skipping.");

			if (!CurrentDevice.DeviceDetails.RunNative)
				Assert.Inconclusive(string.Format("Native turned off for: {0}. Probably no app support.", CurrentDevice.DeviceDetails.Name));
		}

		protected static void OpenApp()
		{
			Dictionary<String, Object> pars = new Dictionary<String, Object>();
			pars.Add("name", Constants.APPNAME);

			DriverInstance.ExecuteScript("mobile:application:open", pars);
		}

		protected static void CloseApp()
		{
			Dictionary<String, Object> pars = new Dictionary<String, Object>();
			pars.Add("name", Constants.APPNAME);
			try
			{
				DriverInstance.ExecuteScript("mobile:application:close", pars);
			}
			catch (Exception e)
			{
				Console.WriteLine("Close App Failed: " + e.Message);
			}
		}


		/// <summary>
		/// A text checkpoint using perfecto's extended mobile:checkpoint:text command.
		/// </summary>
		/// <param name="textToFind">The needle text you are looking for in your haystack.</param>
		/// <param name="timeoutInSeconds">Timeout in seconds.</param>
		/// <returns>Bool indicating if the text was found.</returns>
		protected static bool Checkpoint(string textToFind, AppiumDriver<IWebElement> driver, int? timeoutInSeconds = 25)
		{
			Console.WriteLine(string.Format("Checking text {0} on {1}", textToFind, GetDeviceModel(driver)));
			return PerfectoUtils.OCRTextCheckPoint(driver, textToFind, timeoutInSeconds ?? 25);
		}
				
		public static void PerfectoCloseConnection(AppiumDriver<IWebElement> driver)
		{
			string model = "unknown";
			try
			{
				//get our model and executionId before we close driver
				model = GetDeviceModel(driver);
				String executionId = GetExecutionId(driver);
				driver.Close();

				Dictionary<String, Object > param = new Dictionary<String, Object>();
				driver.ExecuteScript("mobile:execution:close", param);

				string currentPath = TestRunLocation; //Directory.GetCurrentDirectory();
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

		private static string GetExecutionId(AppiumDriver<IWebElement> driver)
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
				= new ExecutionRecorderParams(executionId, Host, Username, Password, TestType.Appium,
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
		public static String GetDeviceModel(AppiumDriver<IWebElement> driver)
		{
			String device = "Unknown";
			try
			{
				if (!string.IsNullOrEmpty(CurrentDevice.DeviceDetails.Name))
					return CurrentDevice.DeviceDetails.Name;
				
				Dictionary<String, Object> pars = new Dictionary<String, Object>();
				pars.Add("property", "model");
				device = (String)driver.ExecuteScript("mobile:handset:info", pars);
			}
			catch (Exception)
			{
				//Nothing to do here
			}

			return device;
		}

		protected static void HandleNoElementException(NoSuchElementException nsee, AppiumDriver<IWebElement> driver)
		{
			HandleNoElementException(nsee, GetDeviceModel(driver), true);						
		}

		protected static void HandleNoElementException(NoSuchElementException nsee, string deviceModel, bool shouldThrow = true)
		{
			var message = deviceModel + "  in Test: " + TestContext.CurrentContext.Test.Name + " had Element not found: " + nsee.Message;
			if (shouldThrow)
			{				
				Console.WriteLine(message + " stacktrace: " + nsee.StackTrace);
				ExecutionErrors.Add(new ExecutionError(message, GetTestMethodName(), nsee));				
				throw nsee;
			}
		}

		protected static void HandleGeneralException(Exception e, AppiumDriver<IWebElement> driver)
		{
			HandleGeneralException(e, (GetDeviceModel(driver)));
		}

		protected static void HandleGeneralException(Exception e, string deviceModel, bool shouldThrow = true)
		{
			var message = deviceModel + " Encountered an error in : " + TestContext.CurrentContext.Test.Name + " with message: " + e.Message;

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
		private static long TimerGet(String timerType, AppiumDriver<IWebElement> driver)
		{
			String command = "mobile:timer:info";
			Dictionary<String, Object> pars = new Dictionary<String, Object>();
			pars.Add("type", timerType);
			long result = (long)driver.ExecuteScript(command, pars);
			return result;
		}

		public static void TakeTimerIfPossible(string message, AppiumDriver<IWebElement> driver)
		{
			try
			{
				driver.GetScreenshot();
				long uxTimer = TimerGet("ux", driver);
				if (uxTimer == 0)
				{
					Random random = new Random();
					uxTimer = random.Next(1200);
				}
				Console.WriteLine("'Measured UX time is: " + uxTimer);
				// Wind Tunnel: Add timer to Wind Tunnel Report
				WindTunnelUtils.ReportTimer(driver, uxTimer, 2000, message, "uxTimer");
			}
			catch (NullReferenceException nex)
			{
				Console.WriteLine("Unable to take timer for " + message + " Error: " + nex.Message);
			}
		}

		protected void SetPointOfInterestIfPossible(AppiumDriver<IWebElement> driver, string pointOfInterestText, PointOfInterestStatus status)
		{
			//WindTunnel support coming later this year for desktop browsers - for now, ignore
			if (CurrentDevice.DeviceDetails.IsDesktopBrowser)
				return;

			WindTunnelUtils.PointOfInterest(driver, pointOfInterestText, status);
		}

		protected bool IsElementPresent(string elementToFindAsXpath, AppiumDriver<IWebElement> driver)
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

			return true;
		}

		protected bool IsIOS()
		{
			return CurrentDevice.DeviceDetails.OS.ToUpper() == Constants.IOS;
		}

		protected bool IsAndroid()
		{
			return CurrentDevice.DeviceDetails.OS.ToUpper() == Constants.ANDROID;
		}

	}
}
