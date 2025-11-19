using ModuleLib;
using ResultTypeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AC_Shield.Core.Modules
{
	// This module is responsible for database operations
	public interface IDatabaseModule: IThreadModule
	{
		// Insert CDR received from SBC into database
		IResult<bool> InsertCDR(AC_CDR CDR);

		// Add or Update black listed caller in database
		IResult<bool> UpdateBlackList(BlackListItem BlackListItem);

		// Get all black listed callers from database at or after StartDate
		IResult<BlackListItem[]> GetBlackList(DateTime StartDate);

		// Get black listed caller from database at or after StartDate
		IResult<BlackListItem[]> GetBlackList(DateTime StartDate, string Caller);
		
		// Get caller statistics from database from StartDate to Now
		IResult<CallerReport[]> GetCallerReports(DateTime StartDate);
		
		// Get first Count CDRs from database
		IResult<CDR[]> GetFirstCDR(int Count);
		// Get last Count CDRs from database
		IResult<CDR[]> GetLastCDR(int Count);



	}
}
