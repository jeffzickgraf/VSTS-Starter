using System;

namespace VSTSDigitalDemoTests.Utility
{
	/// <summary>
	/// Container for an execution error that gets encountered during a run
	/// </summary>
	public class ExecutionError
	{
		public string TestMethodName { get; set; }
		public Exception ExecutionException { get; set; }
		public string Message { get; set; }

		public ExecutionError(string message, string testName, Exception executionException)
		{
			TestMethodName = TestMethodName;
			ExecutionException = executionException;
			Message = message;
		}
	}
}
