using LogLib;
using Microsoft.Data.Sqlite;
using ModuleLib;
using ResultTypeLib;
using System.Collections.Generic;

namespace AC_Shield.Core.Modules
{
	public class SqlLiteDatabaseModule : ThreadModule,IDatabaseModule
	{
		private string databasePath;
		private string databaseName;
		private SqliteConnection? connection;
		private int dbCleanIntervalSeconds;
		private int cdrRetentionSeconds;
		private int blackListRetentionSeconds;

		public SqlLiteDatabaseModule(ILogger Logger, string DatabasePath,string DatabaseName,int DBCleanIntervalSeconds, int CDRRetentionSeconds, int BlackListRetentionSeconds) : base(Logger, ThreadPriority.Normal,5000)
		{
			databasePath = DatabasePath;
			databaseName = DatabaseName;
			dbCleanIntervalSeconds = DBCleanIntervalSeconds;
			cdrRetentionSeconds = CDRRetentionSeconds;
			blackListRetentionSeconds = BlackListRetentionSeconds;
			connection = null;
		}

		private IResult<bool> CreateDatabaseFile()
		{
			string databaseFileName;
			IResult<bool> result;

			databaseFileName = Path.Combine(databasePath, databaseName);

			/*if (System.IO.File.Exists(databaseFileName))
			{
				Log(Message.Information($"Database file found at {databaseFileName}, deleting file"));
				result=Try(() => System.IO.File.Delete(databaseFileName)).Select(success => true);
				if (!result.Succeeded()) return result;
			}//*/					

			if (!Directory.Exists(databasePath))
			{
				Log(Message.Information($"Database folder not found at {databasePath}, creating one"));
				result = Try(() => Directory.CreateDirectory(databasePath)).Select(success => true);
				if (!result.Succeeded()) return result;
			}

			if (!File.Exists(databaseFileName))
			{
				Log(Message.Information($"Create new database file at {databaseFileName}"));
				result = Try(() => File.Create(databaseFileName).Close()).Select(success => true);
				return result;
			}
			
			return Result.Success(true);
		}

		private IResult<bool> CreateTables()
		{
			string command;
			SqliteCommand createCDRTable,createBlackListTable;

			command = "CREATE TABLE IF NOT EXISTS CDR (ID NCHAR(36) PRIMARY KEY, TimeStamp DateTime NOT NULL,IPGroup NVARCHAR(256) NOT NULL,  Caller NVARCHAR(1024) NOT NULL)";
			createCDRTable = new SqliteCommand(command, connection);

			command = "CREATE TABLE IF NOT EXISTS BlackList (ID NCHAR(36) PRIMARY KEY, BlackListStartTime DateTime NOT NULL,BlackListEndTime DateTime NOT NULL,IPGroup NVARCHAR(256) NOT NULL, Caller NVARCHAR(1024) NOT NULL)";
			createBlackListTable = new SqliteCommand(command, connection);


			return Try(() => { 
				createCDRTable.ExecuteNonQuery();
				createBlackListTable.ExecuteNonQuery(); 
			})
			.Select(success => true);
		}

		public IResult<bool> InsertCDR(AC_CDR CDR)
		{
			string? caller;

			caller = CDR.GetCallerNumber();
			if (caller==null) return Result.Fail<bool>(new Exception("CDR Caller number is null"));

			string command = "INSERT INTO CDR (ID, TimeStamp , IPGroup , Caller) VALUES (@ID, @TimeStamp , @IPGroup , @Caller)";
			SqliteCommand insert = new SqliteCommand(command, connection);
			insert.Parameters.AddWithValue("@ID", Guid.NewGuid().ToString());
			insert.Parameters.AddWithValue("@TimeStamp", CDR.SetupTime);
			insert.Parameters.AddWithValue("@IPGroup", CDR.IPGroup);
			insert.Parameters.AddWithValue("@Caller", caller);
			
			return Try(()=>insert.ExecuteNonQuery()).Select(success => true);
		}

		private IResult<bool> PurgeCDR(DateTime BeforeDateTime)
		{
			string command = "DELETE FROM CDR WHERE TimeStamp<=@TimeStamp";
			SqliteCommand delete = new SqliteCommand(command, connection);
			delete.Parameters.AddWithValue("@TimeStamp", BeforeDateTime);

			return Try(() => delete.ExecuteNonQuery()).Select(success => true);
		}

		public IResult<bool> UpdateBlackList(BlackListItem BlackListItem)
		{
			string command;
			SqliteCommand insert, select,update;
			object? id;

			command = "select ID from BlackList where IPGroup=@IPGroup and Caller=@Caller";
			select=new SqliteCommand(command, connection);
			select.Parameters.AddWithValue("@IPGroup", BlackListItem.IPGroup);
			select.Parameters.AddWithValue("@Caller", BlackListItem.Caller);
			if (!Try(()=>select.ExecuteScalar()).Succeeded(out id)) return Result.Fail<bool>(new Exception("Failed to query BlackList table"));

			if (id!=null)
			{
				Log(Message.Information($"Caller {BlackListItem.Caller} in IP Group {BlackListItem.IPGroup} is already black listed, updating end time"));
				command = "update BlackList set BlackListEndTime=@BlackListEndTime where ID=@ID";
				update = new SqliteCommand(command, connection);
				update.Parameters.AddWithValue("@BlackListEndTime", BlackListItem.BlackListEndTime);
				update.Parameters.AddWithValue("@ID", id);
				return Try(() => update.ExecuteNonQuery()).Select(success => true);
			}
			else
			{
				Log(Message.Information($"Inserting caller {BlackListItem.Caller}/IP Group {BlackListItem.IPGroup} in black list"));
				command = "INSERT INTO BlackList (ID, IPGroup , Caller, BlackListStartTime, BlackListEndTime) VALUES (@ID, @IPGroup, @Caller , @BlackListStartTime, @BlackListEndTime)";
				insert = new SqliteCommand(command, connection);
				insert.Parameters.AddWithValue("@ID", BlackListItem.ID);
				insert.Parameters.AddWithValue("@IPGroup", BlackListItem.IPGroup);
				insert.Parameters.AddWithValue("@Caller", BlackListItem.Caller);
				insert.Parameters.AddWithValue("@BlackListStartTime", BlackListItem.BlackListStartTime);
				insert.Parameters.AddWithValue("@BlackListEndTime", BlackListItem.BlackListEndTime);
				return Try(() => insert.ExecuteNonQuery()).Select(success => true);

			}


		}

		


		private IResult<bool> PurgeBlackList(DateTime BeforeDateTime)
		{
			string command = "DELETE FROM BlackList WHERE BlackListEndTime<=@BlackListEndTime";
			SqliteCommand delete = new SqliteCommand(command, connection);
			delete.Parameters.AddWithValue("@BlackListEndTime", BeforeDateTime);

			return Try(() => delete.ExecuteNonQuery()).Select(success => true);
		}

	


		private BlackListItem[] ReadBlackListItems(SqliteDataReader Reader)
		{
			List<BlackListItem> items = new List<BlackListItem>();
			
			while (Reader.Read())
			{
				BlackListItem item = new BlackListItem();
				item.ID = Reader.GetGuid(0);
				item.IPGroup = Reader.GetString(1);
				item.Caller = Reader.GetString(2);
				item.BlackListStartTime = Reader.GetDateTime(3);
				item.BlackListEndTime = Reader.GetDateTime(4);
				items.Add(item);
			}
			return items.ToArray();
		}

		public IResult<BlackListItem[]> GetBlackList(DateTime StartDate)
		{
			string command = "SELECT ID,IPGroup, Caller,BlackListStartTime,BlackListEndTime FROM BlackList where BlackListEndTime>@StartDate";
			SqliteCommand select = new SqliteCommand(command, connection);
			select.Parameters.AddWithValue("@StartDate", StartDate);

			return Try(() => select.ExecuteReader()).SelectResult(reader => Try(()=>ReadBlackListItems(reader)), failure => failure);
		}

		public IResult<BlackListItem[]> GetBlackList(DateTime StartDate, string Caller)
		{
			string command = "SELECT ID,IPGroup, Caller,BlackListStartTime,BlackListEndTime FROM BlackList where BlackListEndTime>@StartDate and Caller=@Caller";
			SqliteCommand select = new SqliteCommand(command, connection);
			select.Parameters.AddWithValue("@StartDate", StartDate);
			select.Parameters.AddWithValue("@Caller", Caller);

			return Try(() => select.ExecuteReader()).SelectResult(reader => Try(() => ReadBlackListItems(reader)), failure => failure);
		}

		private IResult<CallerReport[]> ReadCallerReports(SqliteDataReader Reader)
		{
			List<CallerReport> reports = new List<CallerReport>();
			while (Reader.Read())
			{
				CallerReport report = new CallerReport();
				report.Count = Reader.GetInt32(0);
				report.IPGroup= Reader.GetString(1);
				report.Caller = Reader.GetString(2);
				reports.Add(report);
			}
			return Result.Success(reports.ToArray());
		}

		public IResult<CallerReport[]> GetCallerReports(DateTime StartDate)
		{
			string command = "SELECT COUNT(Caller), IPGroup, Caller FROM CDR where TimeStamp>=@StartDate GROUP BY Caller,IPGroup";
			SqliteCommand select = new SqliteCommand(command, connection);
			select.Parameters.AddWithValue("@StartDate", StartDate);

			return Try(() => select.ExecuteReader()).SelectResult(reader => ReadCallerReports(reader),failure=>failure);
		}

		

		private IResult<CDR[]> ReadCDRs(SqliteDataReader Reader)
		{
			List<CDR> cdrs= new List<CDR>();
			while (Reader.Read())
			{
				CDR cdr = new CDR();
				cdr.ID=Reader.GetGuid(0);
				cdr.TimeStamp= Reader.GetDateTime(1);
				cdr.IPGroup= Reader.GetString(2);
				cdr.Caller= Reader.GetString(3);
				cdrs.Add(cdr);
			}
			return Result.Success(cdrs.ToArray());
		}

		public IResult<CDR[]> GetFirstCDR(int Count)
		{
			string command;
			SqliteCommand select;

			command = "select ID, TimeStamp , IPGroup , Caller FROM CDR ORDER BY TimeStamp ASC LIMIT @Count";
			select = new SqliteCommand(command, connection);
			select.Parameters.AddWithValue("@Count", Count);
			return Try(() => select.ExecuteReader()).SelectResult(reader => ReadCDRs(reader), failure => failure);
		}
		public IResult<CDR[]> GetLastCDR(int Count)
		{
			string command;
			SqliteCommand select;

			command = "select ID, TimeStamp , IPGroup , Caller FROM CDR ORDER BY TimeStamp DESC LIMIT @Count";
			select = new SqliteCommand(command, connection);
			select.Parameters.AddWithValue("@Count", Count);
			return Try(() => select.ExecuteReader()).SelectResult(reader => ReadCDRs(reader), failure => failure);
		}


		protected override void ThreadLoop()
		{
			string databaseFileName;

			databaseFileName = Path.Combine(databasePath, "AC_Shield.db");

			if (!CreateDatabaseFile().Match(
				success => Log(Message.Information($"Database file initialized succesfully")),
				failure => Log(Message.Error($"Failed to initialise database file: {failure.Message}"))
			).Succeeded()) return;

			if (!Try(() => new SqliteConnection($"Filename={databaseFileName}"))
				.Match(
					success => connection = success,
					failure => Log(Message.Error($"Failed to initialise database connection: {failure.Message}"))
			).Succeeded(out connection)) return;

			if (!Try(() => connection.Open())
				.Match(
					success => Log(Message.Information($"Database opened successfully")),
					failure => Log(Message.Error($"Failed to open database connection: {failure.Message}"))
			).Succeeded()) return;

			if (!CreateTables().Match(
				success => Log(Message.Information($"Database tables initialized succesfully")),
				failure => Log(Message.Error($"Failed to initialise database tables: {failure.Message}"))
			).Succeeded()) return;

			while (State == ModuleStates.Started)
			{
				WaitHandles(dbCleanIntervalSeconds*1000, QuitEvent);
				if (State != ModuleStates.Started) break;
				
				Log(Message.Information("Cleaning old data from database"));
				PurgeCDR(DateTime.Now.AddSeconds(-cdrRetentionSeconds)).Match(
					success => Log(Message.Information("Old CDR data purged successfully")),
					failure => Log(Message.Error($"Failed to purge old CDR data: {failure.Message}"))
				);
				
				PurgeBlackList(DateTime.Now.AddSeconds(-blackListRetentionSeconds)).Match(
					success => Log(Message.Information("Black list purged successfully")),
					failure => Log(Message.Error($"Failed to purge black list: {failure.Message}"))
				);

			}
		}

		
	}
}
