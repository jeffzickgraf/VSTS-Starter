using Microsoft.VisualStudio.TestTools.UnitTesting;
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
using System.Configuration;

namespace VSTSDigitalDemoTests
{
	public abstract class AppiumTestBase
	{
		protected static PerfectoTestParams PerfectoTestingParameters;
		protected static string TestCaseName;
		protected static Device CurrentDevice;
		protected static AppiumDriver<IWebElement> DriverInstance;

		/// <summary>
		/// To be called from concrete test fixtures to initialize the test run.
		/// </summary>		
		protected static AppiumDriver<IWebElement> InitializeDriver()
		{			
			string model = "Unknown device model";
			try
			{
				//Pull host and credentials from app.config. See Read-Me-For-Configuration.txt
				var host = ConfigurationManager.AppSettings.Get("PerfectoCloud");
				var user = ConfigurationManager.AppSettings.Get("PerfectoUsername");
				var password = ConfigurationManager.AppSettings.Get("PerfectoPassword");

				ParameterRetriever testParams = new ParameterRetriever();
				PerfectoTestingParameters = testParams.GetVSOExecParam(false);

				CurrentDevice = PerfectoTestingParameters.Devices.FirstOrDefault();

				if (CurrentDevice == null)
					Assert.Inconclusive("No device found in PerfectoTestingParameters.");

				if (CurrentDevice.DeviceDetails.IsDesktopBrowser)
					Assert.Inconclusive("Desktop Web Not for Appium. Skipping.");
				
				model = CurrentDevice.DeviceDetails.Name ?? "Unknown device Model";

				Trace.Listeners.Add(new ConsoleTraceListener());
				Trace.AutoFlush = true;
								
				DesiredCapabilities capabilities = new DesiredCapabilities();
				capabilities.SetCapability("automationName", "Appium");
				capabilities.SetCapability("user", user);
				capabilities.SetCapability("password", password);
				capabilities.SetCapability("newCommandTimeout", "120");
				capabilities.SetPerfectoLabExecutionId(host);
				capabilities.SetCapability("deviceName", CurrentDevice.DeviceDetails.DeviceID);
				capabilities.SetCapability("scriptName", "Parallel-" + TestCaseName);
								
				capabilities.SetCapability("windTunnelPersona", "Georgia");


				var url = new Uri(string.Format("http://{0}/nexperience/perfectomobile/wd/hub", host));
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

			long uxTimer;
			return PerfectoUtils.OCRTextCheckPoint(out uxTimer, driver, textToFind, 0, timeoutInSeconds ?? 25, true, "Checkpoint timer", "Checkpoint timer");
		}
				
		public static void PerfectoCloseConnection(AppiumDriver<IWebElement> driver)
		{
			string model = "unknown";
			try
			{
				//get our model before we close driver
				model = GetDeviceModel(driver);
				driver.Close();

				Dictionary<String, Object > param = new Dictionary<String, Object>();
				driver.ExecuteScript("mobile:execution:close", param);

				string currentPath = Directory.GetCurrentDirectory();
				string newPath = Path.GetFullPath(Path.Combine(currentPath, @"..\..\..\RunReports\Native\"));
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
		public static String GetDeviceModel(AppiumDriver<IWebElement> driver)
		{
			String device = "Unknown";
			try
			{
				Dictionary<String, Object> pars = new Dictionary<String, Object>();
				pars.Add("property", "model");
				device = (String)driver.ExecuteScript("mobile:handset:info", pars);
			}
			catch (Exception)
			{
			}

			return device;
		}

		protected static void HandleNoElementException(NoSuchElementException nsee, AppiumDriver<IWebElement> driver)
		{
			HandleNoElementException(nsee, GetDeviceModel(driver));			
		}

		protected static void HandleNoElementException(NoSuchElementException nsee, string deviceModel)
		{
			Console.WriteLine(deviceModel+ " Element not found: " + nsee.Message);
			throw new NoSuchElementException(deviceModel + " Element not found" + nsee.Message, nsee);
		}

		protected static void HandleGeneralException(Exception e, AppiumDriver<IWebElement> driver)
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
