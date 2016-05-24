using System.Runtime.Serialization;

namespace VSTSDigitalDemoTests.Utility
{
	[DataContract]
	public class ExecutionDetails
	{
		[DataMember]
		public string failedValidations { get; set; }
		[DataMember]
		public string flowEndCode { get; set; }
		[DataMember]
		public string reason { get; set; }
		[DataMember]
		public string status { get; set; }
		[DataMember]
		public string description { get; set; }
		[DataMember]
		public string failedActions { get; set; }
		[DataMember]
		public string executionId { get; set; }
		[DataMember]
		public string reportKey { get; set; }
		[DataMember]
		public string user { get; set; }
		[DataMember]
		public string completed { get; set; }
	}
}
