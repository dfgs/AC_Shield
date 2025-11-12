using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AC_Shield.Core
{
	public class DateTimeParser 
	{
		private static string[] validSyslogFormats = new string[] { "yyyy-MM-dd HH:mm:ss", "HH:mm:ss.fff" };
		private static Regex CDRFixRegex = new Regex(@"([^ ]+)( +[^ ]+ )(.*)");

		public DateTime? ParseCDRDate(string? Input)
		{
			string fixedDate;
			if (string.IsNullOrEmpty(Input)) return null;

			// we need to fix the date (remove timezone from text because it is not used)
			fixedDate=CDRFixRegex.Replace(Input, "$1 $3");
			return DateTime.ParseExact(fixedDate, "HH:mm:ss.fff ddd MMM dd yyyy", CultureInfo.InvariantCulture);
		}

		public DateTime? ParseSyslogDate(string? Input)
		{
			if (string.IsNullOrEmpty(Input)) return null;
			return DateTime.ParseExact(Input, validSyslogFormats, CultureInfo.InvariantCulture);
		}
	}
}
