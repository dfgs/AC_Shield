using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AC_Shield.Core
{
	public struct CDR
	{
		public Guid ID
		{
			get;
			set;
		}
		public DateTime TimeStamp
		{
			get;
			set;
		}
		public string IPGroup
		{
			get;
			set;
		}
		public string Caller
		{
			get;
			set;
		}

		public CDR(Guid ID,DateTime TimeStmap, string IPGroup, string Caller)
		{
			this.ID= ID; this.TimeStamp = TimeStmap; this.IPGroup = IPGroup; this.Caller = Caller;

		}


	}
}
