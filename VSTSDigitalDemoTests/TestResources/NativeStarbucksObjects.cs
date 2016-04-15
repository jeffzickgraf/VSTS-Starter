﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSTSDigitalDemoTests.TestResources
{
	public static class NativeStarbucksObjects
	{
		public static class Elements
		{
			public static string GoToSignIn = "//android.widget.Button[@resource-id='com.starbucks.mobilecard:id/'][1] | //UIAButton[@label='SIGN IN']";
			public static string Username = "//textfield[@resourceid='username'] | //UIATextField[@path='/0/0/9/0/0']";
			public static string Password = "//secure[@resourceid='password'] | //UIASecureTextField[@path='/0/0/9/0/1']";
			public static string SignInSubmit = "//text[@contentDesc='Submit'] | //UIAButton[@name='SIGN IN']";
			public static string SignOutButton = "//android.widget.TextView[@text='Sign Out'] | //UIAButton[@label='SIGN OUT']";
			public static string VerifySignOutButton = "//android.widget.Button[@text='SIGN OUT'] | //UIAButton[@label='Sign Out']";
		}

		public static class Nav
		{
			public static string AndroidOnlyMenuButton = "//android.widget.ImageButton[@content-desc='Starbucks, main navigation menu']";
			public static string Settings = "//android.support.v7.widget.RecyclerView | //UIAButton[@label='SETTINGS']";			
			public static string Stores = "//android.widget.TextView[@text='Stores'] | //UIAButton[@label='STORES']";
		}

		public static class Text
		{
			public static string MakeEverySip = "Make ever sip";
			public static string NotRightNow = "Not right now, thanks";
			public static string Home = "Home";

		}
	}
}