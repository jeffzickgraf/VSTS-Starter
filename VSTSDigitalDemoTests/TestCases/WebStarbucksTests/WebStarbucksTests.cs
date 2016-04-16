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
	/// Test that exersise the starbucks responsive website.
	/// </summary>	
	[TestFixture]
	[Category("WebTests")]
	public class WebStarbucksTests : WebTestBase
	{
		public WebStarbucksTests()
		{
			TestCaseName = "WebStarbucks";
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
		public void Case01_OpenStarbucksHomepage()
		{
			try
			{				
				Trace.WriteLine("Open Starbucks Homepage - Starting Test");
				WebDriver.Navigate().GoToUrl("http://www.starbucks.com");

				//Need just a little time for the driver to be able to pull from new page instead of old one.
				Thread.Sleep(2500);
				string pageTitle = WebDriver.Title;
				Assert.IsTrue(pageTitle.Contains(WebStarbucksObjects.TextCheckPoints.StarbucksHomeTitle),
					string.Format("Expected: {0} but title was {1})", WebStarbucksObjects.TextCheckPoints.StarbucksHomeTitle, pageTitle));
				SetPointOfInterestIfPossible(WebDriver, "Starbucks site loaded", PointOfInterestStatus.Success);
			}
			catch (NoSuchElementException nsee)
			{
				HandleNoElementException(nsee, WebDriver);
			}
			catch (Exception e)
			{
				HandleGeneralException(e, WebDriver);
			}
		}
				
		[Test]
		public void Case020_NavigateToStarbucksShop()
		{
			IWebElement field;

			try
			{
				OpenMenuIfNeeded();									
				
				WebDriver.FindElementByXPath(WebStarbucksObjects.Nav.Shop).Click();
				
				if (IsMobileDevice)
				{
					//verify
					Assert.IsTrue(Checkpoint(WebStarbucksObjects.TextCheckPoints.StarbucksStore, WebDriver),
						"Expected to see " + WebStarbucksObjects.TextCheckPoints.StarbucksStore);

					TakeTimerIfPossible("Starbucks Store loaded", WebDriver);
				}
				else
				{
					//Sometimes the shop click only opens another menu so lets make sure to go to the shop if that happens
					try
					{
						field = WebDriver.FindElementByXPath(WebStarbucksObjects.Nav.ShopMegaMenu);
						//found it open so try to click the shop menu again
						WebDriver.FindElementByXPath(WebStarbucksObjects.Nav.Shop).Click();
					}
					catch (NoSuchElementException)
					{
						//expected - do nothing - menu wasn't opened so must have navigated to shop.						
					}	

					//Can't use visual analysis yet - so just verify via title we are on correct page
					var failMessage = string.Format("Expected: {0} but saw {1}", WebStarbucksObjects.TextCheckPoints.StarbucksStoreTitle, WebDriver.Title);
					Assert.IsTrue(WebDriver.Title == WebStarbucksObjects.TextCheckPoints.StarbucksStoreTitle, failMessage);
				}				
			}
			catch (NoSuchElementException nsee)
			{
				HandleNoElementException(nsee, WebDriver);
			}
			catch (Exception e)
			{
				HandleGeneralException(e, WebDriver);
			}
		}

		[Test]
		public void Case030_SearchForCoffeeAddToCart()
		{			
			try
			{
				//search for coffee
				WebDriver.FindElementByXPath(WebStarbucksObjects.Elements.SearchboxLink).Click();
				WebDriver.FindElementByXPath(WebStarbucksObjects.Elements.Searchbox).SendKeys(TestConstants.CoffeeSearch);
				WebDriver.FindElementByXPath(WebStarbucksObjects.Elements.SearchboxLink).Click();

				//select our desired result
				Thread.Sleep(2000); //need some time for elements to come down before tyring to click
				WebDriver.FindElementByXPath(WebStarbucksObjects.Elements.CoffeeSearchResult).Click();
				
				if (IsMobileDevice)
				{
					//verify
					Assert.IsTrue(Checkpoint(WebStarbucksObjects.TextCheckPoints.PikePlaceRoastText, WebDriver),
						"Expected to see " + WebStarbucksObjects.TextCheckPoints.PikePlaceRoastText);

					TakeTimerIfPossible("Starbucks Store loaded", WebDriver);
				}
				else
				{
					var title = WebDriver.Title;
					//Can't use visual analysis yet - so just verify via title we are on correct page
					var failMessage = string.Format("Expected: {0} but saw {1}", WebStarbucksObjects.TextCheckPoints.PikePlaceTitle, title);
					Assert.IsTrue(title == WebStarbucksObjects.TextCheckPoints.PikePlaceTitle, failMessage);
				}

				//Check if the newsletter div popup is open and close it
				if(IsElementPresent(WebStarbucksObjects.Elements.NewsLetterPopup, WebDriver))
				{
					WebDriver.FindElementByXPath(WebStarbucksObjects.Elements.NewsletterClose).Click();
				}

				WebDriver.FindElementByXPath(WebStarbucksObjects.Elements.AddToBagButton).Click();

				//Give an add to bag animation time to run before next click step or receive dom exception
				Thread.Sleep(2000);

				//View bag - if menu is shrunk - click takes to bag. If large menu - then extra step to go to bag
				WebDriver.FindElementByXPath(WebStarbucksObjects.Elements.MyBagTriggerButton).Click();
				if (!MenuIsShrunk())
				{
					WebDriver.FindElementByXPath(WebStarbucksObjects.Elements.ViewBagLink).Click();
				}

				//Now remove from bag
				WebDriver.FindElementByXPath(WebStarbucksObjects.Elements.RemoveLink).Click();
				WebDriver.FindElementByXPath(WebStarbucksObjects.Elements.RemoveConfirm).Click();

			}
			catch (NoSuchElementException nsee)
			{
				HandleNoElementException(nsee, WebDriver);
			}
			catch (Exception e)
			{
				HandleGeneralException(e, WebDriver);
			}
		}

		private void OpenMenuIfNeeded()
		{
			if(!MenuIsShrunk())
			{
				return;
			}

			//Its shrunk, so expand it
			WebDriver.FindElementByXPath(WebStarbucksObjects.Nav.HamburgerMenu).Click();
		}

		private bool MenuIsShrunk()
		{
			return IsElementPresent(WebStarbucksObjects.Nav.HamburgerCheck, WebDriver);
		}
	}
}
