using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSTSDigitalDemoTests
{
	/// <summary>
	/// Allows ParallelTestRunner to group tests to run consecutively
	/// </summary>
	public class TestGroupAttribute : Attribute
	{
		public TestGroupAttribute()
		{
		}

		public TestGroupAttribute(string name)
		{
			Name = name;
		}

		public TestGroupAttribute(string name, bool exclusive)
			: this(name)
		{
			Exclusive = exclusive;
		}

		public string Name { get; set; }

		public bool Exclusive { get; set; }
	}
}
