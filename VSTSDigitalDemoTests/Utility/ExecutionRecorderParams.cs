﻿using SharedComponents.Parameters;
using System.Collections.Generic;

namespace VSTSDigitalDemoTests.Utility
{
	/// <summary>
	/// Container for ExecutionRecorder parameters.
	/// </summary>
	public class ExecutionRecorderParams
	{
		public string ExecutionId { get; set; }
		public string Host { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public TestType ExecutionTestType { get; set; }
		public string BaseProjectPath { get; set; }
		public string TestCaseName { get; set; }
		public DeviceDetails CurrentDevice { get; set; }
		public List<ExecutionError> ExecutionErrors { get; set; }

		public ExecutionRecorderParams(string executionId, string host, string username, string password, 
			TestType executionTestType, string baseProjectPath, string testCaseName, DeviceDetails currentDevice, 
			List<ExecutionError> errors)
		{
			ExecutionId = executionId;
			Host = host;
			UserName = username;
			Password = password;
			ExecutionTestType = executionTestType;
			BaseProjectPath = baseProjectPath;
			TestCaseName = testCaseName;
			CurrentDevice = currentDevice;
			ExecutionErrors = errors;
		}

	}
}
