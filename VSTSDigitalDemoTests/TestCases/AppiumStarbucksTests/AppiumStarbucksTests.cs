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
			DriverInstance.Context = Constants.VISUAL;
			try
			{	
				if (IsLoggedIn())
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
					if (Checkpoint("Send You Notifications", DriverInstance, 15))
					{
						PerfectoUtils.OCRTextClick(DriverInstance, "Don't Allow", 0, 15);
					}

					DriverInstance.Context = Constants.NATIVEAPP;
					DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.GoToSignIn).Click();

					DriverInstance.Context = Constants.VISUAL;
					PerfectoUtils.PutText(DriverInstance, "Username", Constants.STARBUCKSUSER, "", "");
					PerfectoUtils.PutText(DriverInstance, "Password", Constants.STARBUCKSPWD, "", "");
					
					DriverInstance.Context = Constants.NATIVEAPP;
					DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.SignInSubmit).Click();
					//Need some time for elements to render after login
					Thread.Sleep(3000);

					Assert.IsTrue(IsLoggedIn(), "Expected to see: Add a Starbucks Card to start. Login probably failed.");
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
					//Sleep to allow time for menu to fly out - getting a missed click on otherwise.
					Thread.Sleep(1000);
				}

				DriverInstance.FindElementByXPath(NativeStarbucksObjects.Nav.Settings).Click();

				DriverInstance.Context = Constants.VISUAL;
				if (Checkpoint("Want to receive", DriverInstance, 10))
				{
					PerfectoUtils.OCRTextClick(DriverInstance, "NO", 0, 15);
					Thread.Sleep(1000);
				}

				var settingsText = "SETTINGS";
				var signOutText = "SIGN OUT";

				if (IsAndroid())
				{
					settingsText = "Settings";
					signOutText = "Sign Out";
				}
					

				Assert.IsTrue(Checkpoint(settingsText, DriverInstance, 25), "Expected the Settings screen but didn't find");

				PerfectoUtils.OCRTextClick(DriverInstance, signOutText, 0, 25, true);

				DriverInstance.Context = Constants.NATIVEAPP;
				DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.VerifySignOutButton).Click();

				Assert.IsTrue(DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.SignInButton) != null);
				
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

		private bool IsLoggedIn()
		{
			try
			{
				DriverInstance.Context = Constants.NATIVEAPP;
				if (DriverInstance.FindElementByXPath(NativeStarbucksObjects.Elements.LoggedInStaticText) != null)
					return true;

			}
			catch (NoSuchElementException nsee)
			{
				HandleNoElementException(nsee, GetDeviceModel(DriverInstance), false);
			}
			catch (Exception ex)
			{
				HandleGeneralException(ex, GetDeviceModel(DriverInstance), false);
			}
			return false;
		}
	}
}
