using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AC_Shield.Core
{
	public class DateTimeParser 
	{
		private static string[] validSyslogFormats = new string[] { "yyyy-MM-dd HH:mm:ss", "HH:mm:ss.fff" };
		public DateTime? ParseCDRDate(string? Input)
		{
			if (string.IsNullOrEmpty(Input)) return null;
			return DateTime.ParseExact(Input, "HH:mm:ss.fff  'UTC' ddd MMM dd yyyy", CultureInfo.InvariantCulture);
		}

		public DateTime? ParseSyslogDate(string? Input)
		{
			if (string.IsNullOrEmpty(Input)) return null;
			return DateTime.ParseExact(Input, validSyslogFormats, CultureInfo.InvariantCulture);
		}
	}
}
