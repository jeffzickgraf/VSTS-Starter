using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Threading;

namespace VSTSDigitalDemoTests
{
    public abstract class PerfectoUtils
    {
        public static void StartDeviceLog(RemoteWebDriver driver)
        {
            Console.WriteLine("Start Device Log");
            string command = "mobile:logs:start";
            Dictionary<string, string> Parameters = new Dictionary<string, string>();
            driver.ExecuteScript(command, Parameters);
        }

        // Start collecting device log
        public static void StopDeviceLog(RemoteWebDriver driver)
        {
            Console.WriteLine("Stop device log");
            string command = "mobile:logs:stop";
            Dictionary<string, string> Parameters = new Dictionary<string, string>();
            driver.ExecuteScript(command, Parameters);
        }

        // Gets the user experience (UX) timer
        public static long GetUXTimer(RemoteWebDriver driver)
        {
            Console.WriteLine("Get UX Timer");
            string command = "mobile:timer:info";
            Dictionary<string, string> Parameters = new Dictionary<string, string>();
            Parameters.Add("type", "UX");
            long result = (long)driver.ExecuteScript(command, Parameters);
            return result;
        }

        //Perform text check ocr function
        public static long OCRTextCheck(RemoteWebDriver driver, String text, int threshold, int timeout, bool reportTimer = false, String timerDescription = "timer description", string timerName = "timer name")
        {
            Console.WriteLine("Find: " + text);
            string command = "mobile:checkpoint:text";
            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("content", text);
            Parameters.Add("timeout", timeout.ToString());
            Parameters.Add("measurement", "accurate");
            Parameters.Add("source", "camera");
            Parameters.Add("analysis", "automatic");
            if (threshold > 0)
                Parameters.Add("threshold", threshold.ToString());
            string findstring = (string)driver.ExecuteScript(command, Parameters);
            long uxTimer = GetUXTimer(driver);
            if (reportTimer)
                WindTunnelUtils.ReportTimer(driver, uxTimer, timeout*1000, timerDescription, timerName);
                Console.WriteLine("report timer: " + timerName+ " value: " +uxTimer.ToString() + " threshold: "+(timeout*1000).ToString());
            return uxTimer;
        }

		//Perform text check ocr function with boolean return
		public static bool OCRTextCheckPoint(out long uxTime, RemoteWebDriver driver, String text, int threshold, int timeout, bool reportTimer = false, String timerDescription = "timer description", string timerName = "timer name")
		{
			bool checkpointPassed;

			Console.WriteLine("Find: " + text);
			string command = "mobile:checkpoint:text";
			Dictionary<string, object> Parameters = new Dictionary<string, object>();
			Parameters.Add("content", text);
			Parameters.Add("timeout", timeout.ToString());
			Parameters.Add("measurement", "accurate");
			Parameters.Add("source", "camera");
			Parameters.Add("analysis", "automatic");
			if (threshold > 0)
				Parameters.Add("threshold", threshold.ToString());
			string findstring = (string)driver.ExecuteScript(command, Parameters);
			bool.TryParse(findstring, out checkpointPassed);

			uxTime = GetUXTimer(driver);
			if (reportTimer)
				WindTunnelUtils.ReportTimer(driver, uxTime, timeout * 1000, timerDescription, timerName);
			Console.WriteLine("report timer: " + timerName + " value: " + uxTime.ToString() + " threshold: " + (timeout * 1000).ToString());
			return checkpointPassed;
		}

		//Performs image click ocr function
		public static long OCRImageCheck(RemoteWebDriver driver, String img, int threshold, int timeout, bool reportTimer = false, String timerDescription = "timer description", string timerName = "timer name")
        {
            Console.WriteLine("Find: " + img);
            string command = "mobile:checkpoint:image";
            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("content", img);
            Parameters.Add("timeout", timeout.ToString());
            Parameters.Add("measurement", "accurate");
            Parameters.Add("source", "camera");
            Parameters.Add("analysis", "automatic");
            if (threshold > 0)
                Parameters.Add("threshold", threshold.ToString());
            string findstring = (string)driver.ExecuteScript(command, Parameters);
            long uxTimer = GetUXTimer(driver);
            if (reportTimer)
                WindTunnelUtils.ReportTimer(driver, uxTimer, timeout * 1000, timerDescription, timerName);
            Console.WriteLine("report timer: " + timerName + " value: " + uxTimer.ToString() + " threshold: " + (timeout * 1000).ToString());
            return uxTimer;
        }

        //Performs text click ocr function
        public static void OCRTextClick(RemoteWebDriver driver, String text, int threshold, int timeout)
        {
            Console.WriteLine("Find: " + text);
            string command = "mobile:text:select";
            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("content", text);
            Parameters.Add("timeout", timeout.ToString());
            if (threshold > 0)
                Parameters.Add("threshold", threshold.ToString());
            driver.ExecuteScript(command, Parameters);
        }

        //Launches application
        public static void LaunchApp(RemoteWebDriver driver, String app)
        {
            Console.WriteLine("Launch App: " + app);
            string command = "mobile:application:open";
            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("name", app);
            driver.ExecuteScript(command, Parameters);
            StartAppVitals(driver, app, false);
        }

        //Closes application
        public static void CloseApp(RemoteWebDriver driver, String app)
        {
            Console.WriteLine("Close App: " + app);
            StopAppVitals(driver, app, false);
            string command = "mobile:application:close";
            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("name", app);
            driver.ExecuteScript(command, Parameters);
        }

        //Add a comment
        public static void Comment(RemoteWebDriver driver, String comment)
        {
            Console.WriteLine("Comment: " + comment);
            string command = "mobile:comment";
            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("text", comment);
            driver.ExecuteScript(command, Parameters);
        }

        //sets data in a data entry field
        public static void PutText(RemoteWebDriver driver, string label, string text, string labelDirection, string labelOffset)
        {
            Console.WriteLine("Put text: " + text);
            string command = "mobile:edit-text:set";
            Dictionary<string, Object> Parameters = new Dictionary<string, Object>();
            Parameters.Add("label", label);
            Parameters.Add("text", text);
            Parameters.Add("label.direction", labelDirection);
            Parameters.Add("label.offset", labelOffset);
            driver.ExecuteScript(command, Parameters);
        }
        
        // Swipe
        public static void Swipe(RemoteWebDriver driver, string start, string end)
        {
            Console.WriteLine("swipe from: " + start + " to: " + end);
            string command = "mobile:touch:swipe";
            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("start", start);
            Parameters.Add("end", end);
            driver.ExecuteScript(command, Parameters);
        }

        // set location
        public static void SetLocationCoords(RemoteWebDriver driver, string coordinates)
        {
            Console.WriteLine("Setting location to: " + coordinates);
            string command = "mobile:location:set";
            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("coordinates", coordinates);
            driver.ExecuteScript(command, Parameters);
        }

        // take screenshot
        public static void Screenshot(RemoteWebDriver driver, string Handset)
        {
            Console.WriteLine("Taking Screenshot");
            string command = "mobile:screen:image";
            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("handsetId", Handset);
            driver.ExecuteScript(command, Parameters);
        }

        public static void StartDeviceVitals(RemoteWebDriver driver)
        {
            Console.WriteLine("Starting Vitals");
            string command = "mobile:monitor:start";
            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("monitors", "all");
            driver.ExecuteScript(command, Parameters);
        }

        public static void StopDeviceVitals(RemoteWebDriver driver)
        {
            Console.WriteLine("Stopping Vitals");
            string command = "mobile:monitor:stop";
            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("monitors", "All");
            driver.ExecuteScript(command, Parameters);
        }

        // start app vitals and optionally device vitals
        public static void StartAppVitals(RemoteWebDriver driver, String app, Boolean includeDeviceVitals)
        {
            ManageAppVitals(driver, app, includeDeviceVitals, true);
        }

        // stop app vitals and optionally device vitals
        public static void StopAppVitals(RemoteWebDriver driver, String app, Boolean includeDeviceVitals)
        {
            ManageAppVitals(driver, app, includeDeviceVitals, false);
        }

        private static void ManageAppVitals(RemoteWebDriver driver, string app, Boolean includeDeviceVitals, Boolean isStart)
        {
            string command = "";
            if (isStart)
            {
                Console.WriteLine("Starting app vitals app: " + app);
                command = "mobile:monitor:start";
            }
            else
            {
                Console.WriteLine("Stopping app vitals app: " + app);
                command = "mobile:monitor:stop";
            }

            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("monitors", "all");
            List<String> vitals = new List<String>();
            vitals.Add("all");
            Parameters.Add("vitals", vitals);
            List<String> sources = new List<String>();
            sources.Add(app);

			if (includeDeviceVitals)
                sources.Add("device");

			if(isStart)
				Parameters.Add("source", sources);

            driver.ExecuteScript(command, Parameters);
        }
		
        // block domains
        public static void BlockDomains(RemoteWebDriver driver, Boolean isStart, List<String> domains)
        {
            string command = "";
            Console.WriteLine("Blocking domains ");

			if (isStart)
                command = "mobile:vnetwork:start";
            else
                command = "mobile:vnetwork:update";

			Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("blockedDestinations", domains);
            driver.ExecuteScript(command, Parameters);
        }

        // unblock domains
        public static void UnblockDomains(RemoteWebDriver driver, List<String> domains)
        {
            Console.WriteLine("Unblocking domains ");
            string command = "mobile:vnetwork:update";

			for (int i = 0; i < domains.ToArray().Length; i++)
                domains[i] = "-" + domains[i];

			Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("blockedDestinations", domains);
            driver.ExecuteScript(command, Parameters);
        }

        public static void SetElement(RemoteWebDriver driver, string elementID, string elementData)
        {
            Console.WriteLine("Application.element set " + elementID + " to " + elementData);
            string command = "mobile:application.element:set";
            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("value", elementID);
            Parameters.Add("text", elementData);
            driver.ExecuteScript(command, Parameters);
        }

        public static void NetworkVirtualizationStartUpdate(RemoteWebDriver driver, Boolean isStart, string profile)
        {
            string command = "";
            Console.WriteLine("NV setup with profile: "+ profile);

			if (isStart)
                command = "mobile:vnetwork:start";
            else
                command = "mobile:vnetwork:update";

            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("Profile", profile);
            driver.ExecuteScript(command, Parameters);
        }

        public static void NetworkVirtualizationStartUpdate(RemoteWebDriver driver, Boolean isStart, long latency, long packetLoss, long bandwidthIn, long bandwidthOut,
            long packetCorruption, long packetReordering, long packetDuplication, long delayJitter, long correlation)
        {
            string command = "";
            Console.WriteLine("NV setup");
            if (isStart)
                command = "mobile:vnetwork:start";
            else
                command = "mobile:vnetwork:update";

            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            if (latency > -1)
                Parameters.Add("Latency", latency);

            if (packetLoss > -1)
                Parameters.Add("PacketLoss", packetLoss);

            if (bandwidthIn > -1)
                Parameters.Add("BandwidthIn", bandwidthIn);

            if (bandwidthOut > -1)
                Parameters.Add("BandwidthOut", bandwidthOut);

			if (packetCorruption > -1)
                Parameters.Add("PacketCorruption", packetCorruption);

			if (packetReordering > -1)
                Parameters.Add("packetReordering", packetReordering);

			if (packetDuplication > -1)
                Parameters.Add("PacketDuplication", packetDuplication);

			if (delayJitter > -1)
                Parameters.Add("DelayJitter", delayJitter);

			if (correlation > -1)
                Parameters.Add("Correlation", correlation);

			driver.ExecuteScript(command, Parameters);
        }


        public static void CloudCall(RemoteWebDriver driver, string to)
        {
            string command = "mobile:gateway:call";
            string whoToCall = "";

			if (null != to)
            {
                whoToCall = to;
                Console.WriteLine("cloud call: " + whoToCall);
            }
            else
            {
                whoToCall = GetPhoneNumber(driver);
                Console.WriteLine("cloud call self: " + whoToCall);
            }

			Dictionary<string, object> Parameters = new Dictionary<string, object>();
                Parameters.Add("to.handset", whoToCall);
            driver.ExecuteScript(command, Parameters);
        }

        public static void CloudSMS(RemoteWebDriver driver, string to, string content)
        {
            string command = "mobile:gateway:sms";
            string whoToSMS = "";

			if (null != to)
            {
                whoToSMS = to; 
                Console.WriteLine("cloud sms content: "+ content+" to: " + whoToSMS);
            }
            else
            {
                whoToSMS = GetPhoneNumber(driver);
                Console.WriteLine("cloud sms content: " + content + " to: " + whoToSMS);
            }

			Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("to.handset", whoToSMS);
            Parameters.Add("body", content);

			driver.ExecuteScript(command, Parameters);            
         }

        public static string GetPhoneNumber(RemoteWebDriver driver)
        {
            string command = "mobile:handset:info";
            Console.WriteLine("get phone number");
            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("property", "phoneNumber");
            String result = (String)(driver.ExecuteScript(command, Parameters));
            return result;
        }

        public static void SetLocation(RemoteWebDriver driver, string location, bool isAddress)
        {
            string command = "mobile:location:set";
            Console.WriteLine("set location: " + location);
            Dictionary<string, object> Parameters = new Dictionary<string, object>();

			if (isAddress)
                Parameters.Add("address", location);
            else
                Parameters.Add("coordinates", location);

			driver.ExecuteScript(command, Parameters);
        }

        public static string GetLocation(RemoteWebDriver driver)
        {
            string command = "mobile:location:get";
            Console.WriteLine("get location");
            Dictionary<string, object> Parameters = new Dictionary<string, object>();
            String result = (String)(driver.ExecuteScript(command, Parameters));

			return result;
        }

        public static string GetOS(RemoteWebDriver driver)
        {
            string command = "mobile:handset:info";
            Console.WriteLine("get OS");

			Dictionary<string, object> Parameters = new Dictionary<string, object>();
            Parameters.Add("property", "os");
            String result = (String)(driver.ExecuteScript(command, Parameters));

			return result;
        } 
    }
}
