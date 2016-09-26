using System;
using System.Configuration;

namespace VSTSDigitalDemoTests.Utility
{
	/// <summary>
	/// Accesses Appsettings.
	/// </summary>
	public static class AppSettingsRetriever
	{
		/// <summary>
		/// Indicates id wind tunnel is available for test runs.
		/// </summary>
		/// <returns>True or false indicating if wind tunnel is enabled. 
		/// If an error occurs while processing returns true.</returns>
		public static bool IsWindTunnelEnabled()
		{
			try
			{
				return Boolean.Parse(ConfigurationManager.AppSettings.Get("IsWindTunnelEnabled"));					
			}
			catch (Exception)
			{
				return false;
			}			
		}
	}
}
