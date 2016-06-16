using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace VSTSDigitalDemoTests.Utility
{
	public enum TestType {
		Appium = 0,
		Selenium = 1
	}

	/// <summary>
	/// Records test run to a Comma Seperated Value file
	/// </summary>
	public class ExecutionRecorder
	{
		public void GetAndRecordExecutionRun(ExecutionRecorderParams recorderParams)
		{

			string mutexId = recorderParams.ExecutionTestType == TestType.Appium ? "PerfectoMobileAppiumTestsMutex" : "PerfectoMobileSeleniumTestsMutex";
			//Make sure its absolutely unique
			mutexId += "eb6be9caec214a38bd98eb5287bd7438";
			WriteReportDetails(GetExecutionDetails(recorderParams).Result, recorderParams, mutexId);
		}

		public async Task<ExecutionDetails> GetExecutionDetails(ExecutionRecorderParams recorderParams)
		{
			ExecutionDetails executionDetails = null; 
			using (var client = new HttpClient())
			{
				client.BaseAddress = new Uri("http://localhost:9000/");
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Add("Accept", "application/json");

				string executionUrl = string.Format("https://{0}/services/executions/{1}?operation=status&user={2}&password={3}",
													recorderParams.Host,
													recorderParams.ExecutionId,
													recorderParams.UserName,
													recorderParams.Password);

				// HTTP GET
				HttpResponseMessage response = await client.GetAsync(executionUrl);
				if (response.IsSuccessStatusCode)
				{
					var executionJson = await response.Content.ReadAsStringAsync();

					JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
					executionDetails = jsonSerializer.Deserialize<ExecutionDetails>(executionJson);

					//Add a report URL
					var reportUrlFormat = "=HYPERLINK(\"https://{0}/services/reports/{1}?operation=download&user={2}&password={3}&format=html\")";
					var reportUrl = string.Format(reportUrlFormat, recorderParams.Host, executionDetails.reportKey, recorderParams.UserName, recorderParams.Password);
					executionDetails.reportUrl = reportUrl;
					executionDetails.executionDetailsUrl = executionUrl;
				}
				else
				{
					executionDetails = new ExecutionDetails();
					executionDetails.completed = Constants.UNKNOWN;
					executionDetails.executionId = recorderParams.ExecutionId;
					executionDetails.status = Constants.UNKNOWN;
					executionDetails.description = "Call to get execution details failed. Status was " + response.StatusCode;
					executionDetails.reason = Constants.UNKNOWN;
				}

				if (executionDetails.status == Constants.COMPLETED && recorderParams.ExecutionErrors.Count > 0)
				{
					var message = string.Format("Failed with {0} unhandled errors", recorderParams.ExecutionErrors.Count);
					executionDetails.status = message;
					executionDetails.description = message;
				}
				else if (executionDetails.status == Constants.COMPLETED && recorderParams.ExecutionErrors.Count == 0)
				{
					executionDetails.status = Constants.PASS;
				}
			}
			return executionDetails;
		}

		public void RecordExecutionRun(ExecutionRecorderParams recorderParams, ExecutionDetails executionDetails)
		{

			string mutexId = GetMutexId(recorderParams.ExecutionTestType);

			//Add a report URL
			var reportUrlFormat = "=HYPERLINK(\"https://{0}/services/reports/{1}?operation=download&user={2}&password={3}&format=html\")";
			var reportUrl = string.Format(reportUrlFormat, recorderParams.Host, executionDetails.reportKey, recorderParams.UserName, recorderParams.Password);


			if (!string.IsNullOrEmpty(executionDetails.executionId))
			{
				string executionUrl = string.Format("https://{0}/services/executions/{1}?operation=status&user={2}&password={3}",
													recorderParams.Host,
													recorderParams.ExecutionId,
													recorderParams.UserName,
													recorderParams.Password);
				executionDetails.executionDetailsUrl = executionUrl;
			}

			if(!string.IsNullOrEmpty(executionDetails.reportKey))
			{
				executionDetails.reportUrl = reportUrl;
			}
									
			WriteReportDetails(executionDetails, recorderParams, mutexId);
		}

		private string GetMutexId(TestType executionTestType)
		{
			string mutexId = executionTestType == TestType.Appium ? "PerfectoMobileAppiumTestsMutex" : "PerfectoMobileSeleniumTestsMutex";
			//Make sure its absolutely unique
			mutexId += "eb6be9caec214a38bd98eb5287bd7438";
			return mutexId;
		}

		private void WriteReportDetails(ExecutionDetails details, ExecutionRecorderParams recorderParams, string mutexId)
		{
			//We must use a global mutex here so we don't get lock contentions with other runs.

			// get application GUID as defined in AssemblyInfo.cs
			string appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value.ToString();
						
			// Need a place to store a return value in Mutex() constructor call
			bool createdNew;
			var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
			var securitySettings = new MutexSecurity();
			securitySettings.AddAccessRule(allowEveryoneRule);

			using (var mutex = new Mutex(false, mutexId, out createdNew, securitySettings))
			{
				var hasHandle = false;
				try
				{
					try
					{
						hasHandle = mutex.WaitOne(12000, false);
						if (hasHandle == false)
							throw new TimeoutException("Timeout waiting for exclusive access");
					}
					catch (AbandonedMutexException)
					{
						// Log the fact that the mutex was abandoned in another process, it will still get acquired
						hasHandle = true;
					}

					// !!!!!!! Perform your work here.
					//Get the run identifier that ties parallel runs together - if none, just use now date
					string runIdentifier = recorderParams.CurrentDevice.RunIdentifier ?? string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now); 
					string logName = recorderParams.ExecutionTestType == TestType.Appium ? "AppiumTestsLog" : "SeleniumTestsLog";
					string logFileName = string.Format("{0}-{1}.csv", logName,	runIdentifier);
					
					string path = recorderParams.BaseProjectPath + @"\RunReports\" + logFileName;

					//---Note: if you receive an exception here - You may need to run Visual Studio with Administrator priveleges
					//			Just right click Visual Studio from the Start Menu and launch as Administrator
					if (!File.Exists(path))
					{
						// Create file with a header if none exists
						using (StreamWriter sw = File.CreateText(path))
						{
							sw.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
														"ExecutionId",
														"ReportKey",
														"ScriptName",
														"DeviceName",
														"DeviceId",
														"ExecutionStatus",
														"TestMethodName",
														"Description",
														"ReportDownloadLink",
														"ExecutionDetailsLink"));
						}
					}

					using (StreamWriter sw = File.AppendText(path))
					{


						foreach (ExecutionError errorItem in recorderParams.ExecutionErrors)
						{
							var errorLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
							details.executionId,
							details.reportKey,
							recorderParams.TestCaseName,
							recorderParams.CurrentDevice.Name,
							recorderParams.CurrentDevice.DeviceID,
							"Unhandled Error Occurred",
							errorItem.TestMethodName,
							//Replace double quotes with single so we can output to CSV without it bleeding into other cells
							"\"" + errorItem.Message.Replace("\"", "'") + " stacktrace: " 
								+ errorItem.ExecutionException.StackTrace.Replace("\"", "'") + "\"",
							details.reportUrl,
							details.executionDetailsUrl);
							sw.WriteLine(errorLine);
						}




						var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
													details.executionId,
													details.reportKey,
													recorderParams.TestCaseName,
													recorderParams.CurrentDevice.Name,
													recorderParams.CurrentDevice.DeviceID,
													details.status,
													details.testMethodName,
													//Replace double quotes with single so we can output to CSV without it bleeding into other cells
													"\"" + details.description.Replace("\"", "'") + "\"",
													details.reportUrl,
													details.executionDetailsUrl);
						sw.WriteLine(newLine);
					}
				}
				finally
				{
					if (hasHandle)
						mutex.ReleaseMutex();
				}
			}
		}
	}
}
