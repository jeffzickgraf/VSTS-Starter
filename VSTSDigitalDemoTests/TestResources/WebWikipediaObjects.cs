using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSTSDigitalDemoTests.TestResources
{
	public static class WebWikipediaObjects
	{
		public static class Element
		{
			public static string SearchTextBox = "//input[@id='searchInput']";
			public static string SearchSubmitButton = "//button[1]";
			public static string SearchResultElement = "//h1[text()='American Staffordshire Terrier']";							
		}

		public static class Text
		{
			public const string HomeTitle = "Wikipedia";
		}

		public static class InputValues
		{
			public const string SearchText = "American Staffordshire Terrier";
		}

		public static class TextCheckPoints
		{
			public const string Wikipedia = "Wikipedia";
			public const string SearchPageResult = "American Staffordshire Terrier";
		}		
	}	
}
