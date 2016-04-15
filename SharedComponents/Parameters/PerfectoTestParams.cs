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
	}
}