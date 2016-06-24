using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using VSTSDigitalDemoTests;
using NUnit.Framework;
using System.Diagnostics;
using VSTSDigitalDemoTests.TestResources;
using System.Threading;

namespace VSTSDigitalDemotests
{
	/// <summary>
	/// Test that exersise the Wikipedia website.
	/// </summary>	
	[TestFixture]
	[Category("WebTests")]
	[Category("WebWikipedia")]
	[Category("WebSmoketests")]
	public class WebWikipediaTests : WebTestBase
	{
		public WebWikipediaTests()
		{
			TestCaseName = "WebWikipedia";
		}

		protected static RemoteWebDriverExtended WebDriver { get; set; }

		//Might be able to put initialize and cleanup back in testbase
		[OneTimeSetUp]
		public static void PerfectoOpenConnection()
		{
			TestRunLocation = TestContext.CurrentContext.TestDirectory;
			WebDriver = InitializeDriver();
		}

		/// <summary>
		/// Run automatically by framework after all test methods get run for the class.
		/// </summary>
		[OneTimeTearDown]
		public static void Cleanup()
		{
			PerfectoCloseConnection(WebDriver);
		}

		[Test]
		public void Case01_OpenWikipediaHomepage()
		{
			try
			{				
				Trace.WriteLine("Open Wikipedia Homepage - Starting Test");
				WebDriver.Navigate().GoToUrl("https://www.wikipedia.org");

				//Need just a little time for the driver to be able to pull from new page instead of old one.
				Thread.Sleep(2500);
				string pageTitle = WebDriver.Title;
				Assert.IsTrue(pageTitle.Contains(WebWikipediaObjects.Text.HomeTitle),
					string.Format("Expected: {0} but title was {1})", WebWikipediaObjects.Text.HomeTitle, pageTitle));
				SetPointOfInterestIfPossible(WebDriver, "Wikipedia site loaded", PointOfInterestStatus.Success);
			}
			catch (NoSuchElementException nsee)
			{
				HandleNoElementException(nsee, WebDriver, true);
			}
			catch (Exception e)
			{
				HandleGeneralException(e, WebDriver, true);
			}
		}
				
		[Test]
		public void Case020_SearchVerifyResults()
		{		
			try
			{
				IWebElement field;
				field = WebDriver.FindElementByXPath(WebWikipediaObjects.Element.SearchTextBox);
				field.Click();
				field.SendKeys(WebWikipediaObjects.InputValues.SearchText);

				field = WebDriver.FindElementByXPath(WebWikipediaObjects.Element.SearchSubmitButton);
				field.Click();

				//verify through checkpoint for supported devices
				if (IsDeskTopBrowser)
				{
					Assert.IsTrue(Checkpoint(WebWikipediaObjects.TextCheckPoints.SearchPageResult, WebDriver));
				}
				else
				{
					WebDriver.FindElementByXPath(WebWikipediaObjects.Element.SearchResultElement);
				}
			}
			catch (NoSuchElementException nsee)
			{
				HandleNoElementException(nsee, WebDriver, true);
			}
			catch (Exception e)
			{
				HandleGeneralException(e, WebDriver, true);
			}
		}

	}
}
