using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SharedComponents.Parameters
{
	public class ParameterRetriever
	{
		public PerfectoTestParams GetVSOExecParam(string baseProjectPath = "", bool UseBuildDropFolder = true)
		{
			List <PerfectoTestParams> parameters = new List<PerfectoTestParams>();
			List<string> devices = new List<string>();
			PerfectoTestParams testParams = null;
						
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

					finalResolutionPath = baseProjectPath +  SharedConstants.DeviceConfigFileName;

					Console.WriteLine("VSTS file not found - pulling from: " + finalResolutionPath);
				}			
				
				using (StreamReader jsonConfigFile = File.OpenText(finalResolutionPath))
				using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonConfigFile.ReadToEnd())))
				{
					DataContractJsonSerializerSettings settings =
							new DataContractJsonSerializerSettings();
					settings.UseSimpleDictionaryFormat = true;

					DataContractJsonSerializer serializer =
							new DataContractJsonSerializer(typeof(PerfectoTestParams), settings);

					testParams = (PerfectoTestParams)serializer.ReadObject(ms);					
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.StackTrace);
				throw;
			}
			return testParams;
		}
	}
}
