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

		/// <summary>
		/// Usefule when an unhandled error occurs to know where its coming from.
		/// </summary>
		public string testMethodName { get; set; }

		/// <summary>
		/// Link for the REST call that populated the details
		/// </summary>
		public string executionDetailsUrl { get; set; }

		/// <summary>
		/// Link to the downloadable report. This will be derived and set outside of a REST call.
		/// </summary>
		public string reportUrl { get; set; }
	}
}
