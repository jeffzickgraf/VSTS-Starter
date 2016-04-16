using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSTSDigitalDemoTests.TestResources
{
	public static class WebStarbucksObjects
	{	
		public static class Nav
		{
			public const string HamburgerCheck = "//div[@id='nav' and contains(@class,'small')][1]";
			public const string HamburgerMenu = "//div[@class='nav_control']";
			public const string Coffee = "//li[@id='nav_coffee]";
			public const string Shop = "//li[@id='nav_shop']//a";
			public const string ShopMegaMenu = "//li[@id='menu_shop' and @class='fields open' ]";
		}

		public static class Elements
		{
			public const string CoffeeShopLink = "//a[text()='Coffee']";
			public const string CoffeeSearchResult = "//a[text()='Starbucks® Pike Place® Roast, Whole Bean']";
			public const string AddToBagButton = "//button[@value='Add to Bag']";
			public const string MyBagTriggerButton = "//div[@class='mybag-trigger']";
			public const string ViewBagLink = "//a[text()='VIEW BAG']";
			public const string RemoveLink = "//a[@title='Remove'][1]";
			public const string RemoveConfirm = "//button[text()='Yes']";
			public const string SearchboxLink = "//a[@id='mobile-search-icon'] | //a[@href='#searchbox']";
			public const string Searchbox = "//input[@id='searchinput'] | //input[@id='searchbox']";
			public const string SearchSubmitButton = "//button[@id='btnSubmit'] | //button[@id='submit_search_util']";

			//newsletter popup
			public const string NewsLetterPopup = "//div[@id='signupnewsletter' and @class='popup1 show']";
			public const string NewsletterClose = "//div[@class='snlcloseicon']";
		}	

		public static class TextCheckPoints
		{
			public const string StarbucksHomeTitle = "Starbucks";
			public const string StarbucksStore = "Starbucks Store";
			public const string StarbucksStoreTitle = "Starbucks® Store Coffee, Tea, Products and Gifts";
			public const string PikePlaceTitle = "Starbucks Pike Place Roast Whole Bean Coffee | Starbucks Store";
			public const string PikePlaceText = "Pike Place";
		}
	}
}
