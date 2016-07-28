using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharedComponents.Parameters
{
	/// <summary>
	/// Retrieves device parameters from a JSON config file.
	/// </summary>
	public class ParameterRetriever
	{
		public PerfectoTestParams GetVSOExecParam(string baseProjectPath = "", bool UseBuildDropFolder = true)
		{
			PerfectoTestParams testParams = null;
			List<Device> devices = new List<Device>();

			try
			{
				//We either get the device list via VSTS passing in a selected json
				//	configuration file or we look in our code folder (which is also the location for a copied and
				//	reduced file for individual parallel run if the MultiTestExecutor is calling)								
				string currentPath = Directory.GetCurrentDirectory();
				Console.WriteLine("Parameter Retriever Current path : " + currentPath);
				string vSTSConfigJSONPath = Path.GetFullPath(Path.Combine(currentPath, @"..\..\..\..\")) + SharedConstants.DeviceConfigFileName;

				string finalResolutionPath = vSTSConfigJSONPath;
				if (!UseBuildDropFolder || !File.Exists(vSTSConfigJSONPath))
				{
					finalResolutionPath = GetFinalResolutionOfPath(ref baseProjectPath, currentPath);

					Console.WriteLine("VSTS file not found - pulling from: " + finalResolutionPath);
				}
				
				InflateDeviceList(devices, finalResolutionPath);

				testParams = new PerfectoTestParams(devices, string.Empty, string.Empty, string.Empty);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.StackTrace);
				throw;
			}
			return testParams;
		}

		private static void InflateDeviceList(List<Device> devices, string finalResolutionPath)
		{
			/*
			* Jeff Zickgraf's HACK Note:
			*
			* Previous code was nice and simple:
			*	using (StreamReader jsonConfigFile = File.OpenText(finalResolutionPath))				
			{
				JsonSerializer serializer = new JsonSerializer();
				testParams = (PerfectoTestParams)serializer.Deserialize(jsonConfigFile, typeof(PerfectoTestParams));
			}
			*
			* However, deserialization on DoS's TFS instance isn't working as expected for some reason
			* On VSTS we never have seen any issue.
			* I tried using both .NET serialization/deserialization as well as JSON.NET
			* and get intermitent deserialization errors. Don't have access to their TFS box to troubleshoot
			* So going to inflate to our DeviceList object with Linq to JSON with JSON.NET: http://www.newtonsoft.com/json/help/html/LINQtoJSON.htm
			*/

			string deviceJSONString = File.ReadAllText(finalResolutionPath);
			JObject devicesObject = JObject.Parse(deviceJSONString);
			var devicesList = devicesObject.Children().Children().Children()["device"].ToList();

			//For each content object create a new device instance.			
			foreach (JObject content in devicesList)
			{
				string deviceID = string.Empty;
				string os = string.Empty;
				string osVersion = string.Empty;
				string browserName = string.Empty;
				string browserVersion = string.Empty;
				string name = string.Empty;
				string runIdentifier = string.Empty;
				bool isDesktopBrowser = false;
				bool runNative = false;
				bool runWeb = false;

				Console.WriteLine(">>>>>>>>>>>>>>>>>Create Device Object ");

				foreach (JProperty prop in content.Properties())
				{
					switch (prop.Name)
					{
						case "deviceID":
							deviceID = prop.Value.ToString();
							break;
						case "os":
							os = prop.Value.ToString();
							break;
						case "osVersion":
							osVersion = prop.Value.ToString();
							break;
						case "browserName":
							browserName = prop.Value.ToString();
							break;
						case "browserVersion":
							browserVersion = prop.Value.ToString();
							break;
						case "name":
							name = prop.Value.ToString();
							break;
						case "runIdentifier":
							runIdentifier = prop.Value.ToString();
							break;
						case "isDesktopBrowser":
							isDesktopBrowser = (bool)prop.Value;
							break;
						case "runNative":
							runNative = (bool)prop.Value;
							break;
						case "runWeb":
							runWeb = (bool)prop.Value;
							break;
						default:
							Console.WriteLine("Unknown JSON Property while deserializing Device List of: " + prop.Name);
							break;
					}
					Console.WriteLine(prop.Name + " " + prop.Value);
				}
				Device device = new Device();
				device.DeviceDetails = new DeviceDetails(deviceID, os, osVersion, name, browserName,
						browserVersion, isDesktopBrowser, runNative, runWeb);
				device.DeviceDetails.RunIdentifier = runIdentifier;
				devices.Add(device);
			}
		}

		private static string GetFinalResolutionOfPath(ref string baseProjectPath, string currentPath)
		{
			string finalResolutionPath;
			//Get file from local machine build

			//When the MultiTestExecutor is used, need to take from test run folder otherwise - need to grab from sharedcomponents
			if (currentPath.Contains("TestRuns"))
			{
				baseProjectPath = currentPath + @"\TestResources\DevicesGroup\";
			}
			else
			{
				baseProjectPath += @"\SharedComponents\TestResources\DevicesGroup\";
			}

			finalResolutionPath = baseProjectPath + SharedConstants.DeviceConfigFileName;
			return finalResolutionPath;
		}
	}
}
