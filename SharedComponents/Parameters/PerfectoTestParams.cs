using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SharedComponents.Parameters
{
	[DataContract]
	public class PerfectoTestParams
	{		
		[DataMember(Name = "devices")]
		public List<Device> Devices { get; set; }

		public PerfectoTestParams(List<Device> devices, string repositoryKey, string activityBundle, string applicationType)
		{
			Devices = devices;
		}

		//[DataMember(Name="PerfectoRepository")]
		//public string RepositoryKey { get; set; }

		//[DataMember(Name = "ApplicationType")]
		//public string ApplicationType { get; set; }

		//[DataMember(Name = "BundleID")]
		//public string ActivityBundle { get; set; }

		//public PerfectoTestParams(List<Device> devices, string repositoryKey, string activityBundle, string applicationType)
		//{
		//	Devices = devices;
		//	RepositoryKey = repositoryKey;
		//	ActivityBundle = activityBundle;
		//	ApplicationType = applicationType;
		//}
	}
}