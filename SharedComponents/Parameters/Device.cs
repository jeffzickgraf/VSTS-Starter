using System.Runtime.Serialization;

namespace SharedComponents.Parameters
{
	[DataContract]
	public class Device
	{
		[DataMember(Name ="device")]
		public DeviceDetails DeviceDetails { get; set; }
	}

	[DataContract]
	public class DeviceDetails
	{
		/// <summary>
		/// Device identifier - i.e. 32838717166
		/// </summary>
		[DataMember(Name = "deviceID")]
		public string DeviceID { get; set; }

		[DataMember(Name = "os")]
		public string OS { get; set; }

		[DataMember(Name = "osVersion")]
		public string OSVersion { get; set; }
				
		/// <summary>
		///General description for device or browser. i.e. Jeff iPhone 6s
		/// </summary>
		[DataMember(Name = "name")]
		public string Name { get; set; }

		[DataMember(Name = "browserName")]
		public string BrowserName { get; set; }

		[DataMember(Name = "browserVersion")]
		public string BrowserVersion { get; set; }

		[DataMember(Name="isDesktopBrowser")]
		public bool IsDesktopBrowser { get; set; }

		public DeviceDetails(string deviceID, string os, string osVersion, string name, string browserName, 
			string browserVersion, bool isDesktopBrowser = false)
		{
			DeviceID = deviceID;
			OS = os;
			OSVersion = osVersion;
			Name = name;
			BrowserName = browserName;
			BrowserVersion = browserVersion;
			IsDesktopBrowser = isDesktopBrowser;
		}
	}
}
