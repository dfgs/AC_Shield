using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AC_Shield.Core
{
	public struct BlackListItem
	{
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
		
		public DateTime BlackListStartTime
		{
			get;
			set;
		}
		
		public DateTime BlackListEndTime
		{
			get;
			set;
		}

		public BlackListItem(string IPGroup, string SourceURI, DateTime BlackListStartTime, DateTime BlackListEndTime)
		{
			this.IPGroup = IPGroup;
			this.SourceURI = SourceURI;
			this.BlackListStartTime = BlackListStartTime;
			this.BlackListEndTime = BlackListEndTime;
		}
	}
}
