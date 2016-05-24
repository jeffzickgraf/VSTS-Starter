using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelDeviceExecutor
{
	/// <summary>
	/// Keeps track of launched processes so we know when our test runs are finished
	/// </summary>
	public class ProcessObserver
	{
		public List<Process> NunitProcesses;

		public ProcessObserver()
		{
			NunitProcesses = new List<Process>();
		}
		
		public void AddProcess(Process process)
		{
			NunitProcesses.Add(process);
		}

		public int GetStillRunningProcessCount()
		{
			return NunitProcesses.Where(n => !n.HasExited).Count();
		}		
	}
}
