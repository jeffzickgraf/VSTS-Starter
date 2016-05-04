using NUnit.Framework;
using OpenQA.Selenium;
using System;
using System.Threading;
using VSTSDigitalDemoTests.TestResources;

namespace VSTSDigitalDemoTests.TestCases.AppiumStarbucksTests
{
	/// <summary>
	/// Tests that exercise the Starbucks native mobile app.
	/// </summary>
	[TestFixture]
	[Category("AppiumTests")]
	public class AppiumStarbucksTests : AppiumTestBase
	{
		public AppiumStarbucksTests()
		{
			TestCaseName = "AppiumStarbucks";
		}

		#region -- Initialization and Cleanup
		[OneTimeSetUp]		
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
		public void Native010_SignIn()
		{
			try
			{
				DriverInstance.Context = Constants.VISUAL;
				//Possible, we didn't get signed out last run so check for it and signout if needed
				if (Checkpoint("You're all caught up", DriverInstance))
				{
					Logout();
					CloseApp();
					OpenApp();
				}

				if (IsAndroid())
				{
					DriverInstance.Context = Constants.VISUAL;
					//Check for initial welcome screen and move through
					if (Checkpoint("Get Started", DriverInstance, 8))
					{
						PerfectoUtils.OCRTextClick(DriverInstance, "Get Started", 0, 15);
					}

					//Check for another welcome
					if (Checkpoint("Welcome to Starbucks", DriverInstance, 8))
					{
						PerfectoUtils.OCRTextClick(DriverInstance, "SIGN IN", 0, 15);
					}

					//Taking some time for login page to load completely - spinner overtop but still shows username
					//so need to wait until spinner is gone or else it eats the click.
					Thread.Sleep(15000);

					PerfectoUtils.PutText(DriverInstance, "Username", Constants.STARBUCKSUSER,"","");
					PerfectoUtils.PutText(DriverInstance, "Password", Constants.STARBUCKSPWD, "", "");
					PerfectoUtils.OCRImageClick(DriverInstance, @"PUBLIC:Jeff/Images/StarbucksSubmit.png");
					
					if (Checkpoint(NativeStarbucksObjects.Text.MakeEverySip, DriverInstance, 15))
					{
						PerfectoUtils.OCRTextClick(DriverInstance, NativeStarbucksObjects.Text.NotRightNow, 0, 15);
					}

				}
				else    //ios
				{
					DriverInstance.Context = Constants.VISUAL;
					//1st time app usage - may get a prompt
					if (Checkpoint("Send You Notifications", DriverInstance, 8))
					{
						PerfectoUtils.OCRTextClick(DriverInstance, "Don't Allow", 0, 15);
					}

					DriverInstance.Context = Constants.NATIVEAPP;
					DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.GoToSignIn).Click();

					//having issues laying down username with selenium sendkeys - try visual
					//DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.Username).SendKeys(Constants.STARBUCKSUSER);
					//DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.Password).SendKeys(Constants.STARBUCKSPWD);

					DriverInstance.Context = Constants.VISUAL;
					PerfectoUtils.PutText(DriverInstance, "Username", Constants.STARBUCKSUSER, "", "");
					PerfectoUtils.PutText(DriverInstance, "Password", Constants.STARBUCKSPWD, "", "");
					
					DriverInstance.Context = Constants.NATIVEAPP;
					DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.SignInSubmit).Click();
					Assert.IsTrue(Checkpoint("You're all caught up", DriverInstance), "Expected to see: You're all caught up. Login probably failed.");
				}
			}
			catch (NoSuchElementException nsee)
			{
				HandleNoElementException(nsee, GetDeviceModel(DriverInstance), true);
			}
			catch (Exception ex)
			{
				HandleGeneralException(ex, GetDeviceModel(DriverInstance), true);
			}
													
		}

		[Test]
		public void Native20_Logout()
		{
			Logout();
		}
		
		private void Logout()
		{
			try
			{
				DriverInstance.Context = Constants.NATIVEAPP;
				if (IsAndroid())
				{
					DriverInstance.FindElementByXPath(NativeStarbucksObjects.Nav.AndroidOnlyMenuButton).Click();
					//Sleep to allow time for menu to fly out - getting a missed click on settings otherwise.
					Thread.Sleep(1000);
				}

				DriverInstance.FindElementByXPath(NativeStarbucksObjects.Nav.Settings).Click();

				DriverInstance.Context = Constants.VISUAL;
				Assert.IsTrue(Checkpoint("Settings", DriverInstance, 15), "Expected the Settings screen but didn't find");

				DriverInstance.Context = Constants.NATIVEAPP;
				DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.SignOutButton).Click();
				DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.VerifySignOutButton).Click();

				DriverInstance.Context = Constants.VISUAL;
				Assert.IsTrue(Checkpoint("SIGN IN", DriverInstance, 15), "Expected to find a sign in button but didn't find");
			}			
			catch (NoSuchElementException nsee)
			{
				HandleNoElementException(nsee, GetDeviceModel(DriverInstance), true);
			}
			catch (Exception ex)
			{
                HandleGeneralException(ex, GetDeviceModel(DriverInstance), true);
			}
		}
	}
}
