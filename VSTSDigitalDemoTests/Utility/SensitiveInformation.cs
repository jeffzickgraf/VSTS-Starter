using System;
using System.Linq;
using System.Xml.Linq;

namespace VSTSDigitalDemoTests.Utility
{
	public static class SensitiveInformation
	{
		public static void GetHostAndCredentials(string baseProjectPath, out string host, out string user, out string password)
		{
			//Pull host and credentials from appsettings.config. See Read-Me-For-Configuration.txt
			//Note - couldn't get ConfigurationManager.AppSettings.Get("key") to work when run from Nunitconsole for multitestexecutor
			//so having to do this myself.
			XDocument doc = XDocument.Load(baseProjectPath + @"\VSTSDigitalDemoTests\appsettings.config");
			var addElements = doc.Descendants("add");

			host = addElements.First(a => a.Attribute("key").Value == "PerfectoCloud").Attribute("value").Value;
			user = addElements.First(a => a.Attribute("key").Value == "PerfectoUsername").Attribute("value").Value;
			password = addElements.First(a => a.Attribute("key").Value == "PerfectoPassword").Attribute("value").Value;

			//Using for debugging purposes since wasn't getting host from ConfigManager
			Console.WriteLine("----->>>>--->>!!!! host is:" + host);
		}

	}
}
