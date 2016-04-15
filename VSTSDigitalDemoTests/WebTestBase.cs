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

namespace VSTSDigitalDemoTests
{	
	public abstract class WebTestBase
	{
		protected static PerfectoTestParams PerfectoTestingParameters;
		protected static string TestCaseName;
		protected static Device CurrentDevice;
		protected static string TestRunLocation;

		/// <summary>
		/// To be called from concrete test fixtures to initialize the test run.
		/// </summary>		
		protected static RemoteWebDriverExtended InitializeDriver()
		{
			string model = "Unknown device model";
			try
			{
				string baseProjectPath = Path.GetFullPath(Path.Combine(TestRunLocation, @"..\..\..\"));
				string host, user, password;
				SensitiveInformation.GetHostAndCredentials(baseProjectPath, out host, out user, out password);

				ParameterRetriever testParams = new ParameterRetriever();
				PerfectoTestingParameters = testParams.GetVSOExecParam(baseProjectPath, false);

				CurrentDevice = PerfectoTestingParameters.Devices.FirstOrDefault();

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
				capabilities.SetCapability("user", user);
				capabilities.SetCapability("password", password);
				capabilities.SetCapability("newCommandTimeout", "120");
				capabilities.SetPerfectoLabExecutionId(host);

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

				var url = new Uri(string.Format("https://{0}/nexperience/perfectomobile/wd/hub", host));
				RemoteWebDriverExtended driver = new RemoteWebDriverExtended(url, capabilities, new TimeSpan(0, 2, 0));
				driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(15));
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
				driver.Close();

				Dictionary<String, Object > param = new Dictionary<String, Object>();
				driver.ExecuteScript("mobile:execution:close", param);
				
				string currentPath = TestRunLocation;
				string newPath = Path.GetFullPath(Path.Combine(currentPath, @"..\..\..\RunReports\"));
				driver.DownloadReport(DownloadReportTypes.pdf, newPath + "\\" + model + " " + TestCaseName + " report");
				//driver.DownloadAttachment(DownloadAttachmentTypes.video, newPath + "\\" + model + " " + TestCaseName + " video", "flv");
				//driver.DownloadAttachment(DownloadAttachmentTypes.image, "C:\\test\\report\\images", "jpg");
			}
			catch (Exception ex)
			{
				HandleGeneralException(ex, model, false);
			}
			
			driver.Quit();						
		}

		/// <summary>
		/// The device's identifier for the test run.
		/// </summary>
		public static String GetDeviceModel(RemoteWebDriverExtended driver)
		{
			Dictionary<String, Object> pars = new Dictionary<String, Object>();
			pars.Add("property", "model");
			String properties = (String)driver.ExecuteScript("mobile:handset:info", pars);
			return properties;			
		}

		protected static void HandleNoElementException(NoSuchElementException nsee, RemoteWebDriverExtended driver)
		{
			HandleNoElementException(nsee, GetDeviceModel(driver));			
		}

		protected static void HandleNoElementException(NoSuchElementException nsee, string deviceModel)
		{
			Console.WriteLine(deviceModel+ " Element not found: " + nsee.Message);
			throw new NoSuchElementException(deviceModel + " Element not found" + nsee.Message, nsee);
		}

		protected static void HandleGeneralException(Exception e, RemoteWebDriverExtended driver)
		{
			HandleGeneralException(e, (GetDeviceModel(driver)));
		}

		protected static void HandleGeneralException(Exception e, string deviceModel, bool shouldThrow = true)
		{
			Console.WriteLine(deviceModel + " Encountered an error: " + e.Message + "stacktrace: " + e.StackTrace);

			if (shouldThrow)
				throw new Exception(deviceModel + " Encountered an error: " + e.Message, e);
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

			return true;
		}

	}
}
