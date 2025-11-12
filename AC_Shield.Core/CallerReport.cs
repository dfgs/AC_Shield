using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AC_Shield.Core
{
	public struct CallerReport
	{
		public int Count
		{
			get;
			set;
		}
		public string IPGroup
		{
			get;
			set;
		}
		public string SourceURI
		{
			get;
			set;
		}

		public CallerReport(int Count,string IPGroup, string SourceURI)
		{
			this.Count= Count;this.IPGroup = IPGroup; this.SourceURI = SourceURI;

		}


	}
}
