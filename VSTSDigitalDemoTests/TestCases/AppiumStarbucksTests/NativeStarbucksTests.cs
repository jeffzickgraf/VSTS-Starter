using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSTSDigitalDemoTests.TestResources;

namespace VSTSDigitalDemoTests.TestCases.AppiumStarbucksTests
{	
	[TestFixture]
	public class NativeStarbucksTests : AppiumTestBase
	{
		public NativeStarbucksTests()
		{
			TestCaseName = "AppiumStarbucks";
		}

		#region -- Initialization and Cleanup
		[OneTimeSetUp]
		//Runs at the start of class initialization.
		public static void PerfectoOpenConnection()
		{
			TestRunLocation = TestContext.CurrentContext.TestDirectory;
			DriverInstance = InitializeDriver();
		}

		/// <summary>
		/// Run automatically by framework after all test methods get run for the class.
		/// </summary>
		[OneTimeTearDown]
		public static void Cleanup()
		{
			PerfectoCloseConnection(DriverInstance);
		}
		#endregion

		[Test]
		public void NativeCase01_SignIn()
		{
			if (IsAndroid())
			{
				DriverInstance.Context = Constants.VISUAL;
				//Check for initial welcome screen and move through
				if (Checkpoint("Welcome", DriverInstance))
				{
					PerfectoUtils.OCRTextClick(DriverInstance, "Get Started", 0, 15);
				}

				//Check for another welcome
				if (Checkpoint("Welcome to Starbucks", DriverInstance))
				{
					PerfectoUtils.OCRTextClick(DriverInstance, "SIGN IN", 0, 15);
				}

				//Switch to Webview
				DriverInstance.Context = Constants.WEBVIEW;
				DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.Username).SendKeys(Constants.STARBUCKSUSER);
				DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.Password).SendKeys(Constants.STARBUCKSPWD);
				DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.SignInSubmit).Click();

				DriverInstance.Context = Constants.VISUAL;
				if (Checkpoint(NativeStarbucksObjects.Text.MakeEverySip, DriverInstance))
				{
					PerfectoUtils.OCRTextClick(DriverInstance, NativeStarbucksObjects.Text.NotRightNow, 0, 15);
				}

			}
			else
			{
				DriverInstance.Context = Constants.VISUAL;
				//1st time app usage - may get a prompt
				if (Checkpoint("Send You Notifications", DriverInstance))
				{
					PerfectoUtils.OCRTextClick(DriverInstance, "Don't Allow", 0, 15);
				}
			}


			DriverInstance.Context = Constants.NATIVEAPP;

			if (IsAndroid())
			{
				DriverInstance.FindElementByXPath(NativeStarbucksObjects.Nav.AndroidOnlyMenuButton).Click();
			}

			DriverInstance.FindElementByXPath(NativeStarbucksObjects.Nav.Settings).Click();

			DriverInstance.Context = Constants.VISUAL;
			Assert.IsTrue(Checkpoint("Settings", DriverInstance, 25), "Expected the Settings screen but didn't find");

			DriverInstance.Context = Constants.NATIVEAPP;
			DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.SignOutButton).Click();
			DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.VerifySignOutButton).Click();

			DriverInstance.Context = Constants.VISUAL;
			Assert.IsTrue(Checkpoint("SIGN IN", DriverInstance, 25), "Expected to find a sign in button but didn't find");
		}


	}
}
