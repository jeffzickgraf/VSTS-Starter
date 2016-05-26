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
		public async Task GetExecutionDetails(ExecutionRecorderParams recorderParams)
		{

			string mutexId = recorderParams.ExecutionTestType == TestType.Appium ? "PerfectoMobileAppiumTestsMutex" : "PerfectoMobileSeleniumTestsMutex";
			//Make sure its absolutely unique
			mutexId += "eb6be9caec214a38bd98eb5287bd7438";

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
					var executionJson = await response.Content.ReadAsStringAsync();// .ReadAsAsync<ExecutionDetails>();

					JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
					ExecutionDetails executionDetails = jsonSerializer.Deserialize<ExecutionDetails>(executionJson);

					//Add a report URL
					var reportUrlFormat = "=HYPERLINK(\"https://{0}/services/reports/{1}?operation=download&user={2}&password={3}&format=html\")";
					var reportUrl = string.Format(reportUrlFormat, recorderParams.Host, executionDetails.reportKey, recorderParams.UserName, recorderParams.Password);
					executionDetails.reportUrl = reportUrl;
					WriteReportDetails(executionDetails, recorderParams, mutexId);
				}
			}
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

					string logName = recorderParams.ExecutionTestType == TestType.Appium ? "AppiumTestsLog.csv" : "SeleniumTestsLog.csv";
					string path = recorderParams.BaseProjectPath + @"\RunReports\" + logName;

					//---Note: if you receive an exception here - You may need to run Visual Studio with Administrator priveleges
					//			Just right click Visual Studio from the Start Menu and launch as Administrator
					if (!File.Exists(path))
					{
						// Create file with a header if none exists
						using (StreamWriter sw = File.CreateText(path))
						{
							sw.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
														"ExecutionId",
														"ReportKey",
														"ScriptName",
														"DeviceName",
														"DeviceId",
														"ExecutionStatus",
														"FailedValidations",
														"FailedActions",
														"ReportDownloadLink"));
						}
					}

					using (StreamWriter sw = File.AppendText(path))
					{
						var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
													details.executionId,
													details.reportKey,
													recorderParams.TestCaseName,
													recorderParams.CurrentDevice.Name,
													recorderParams.CurrentDevice.DeviceID,
													details.status,
													details.failedValidations,
													details.failedActions,
													details.reportUrl);
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
