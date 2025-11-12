using LogLib;
using Microsoft.Data.Sqlite;
using ModuleLib;
using ResultTypeLib;
using System.Collections.Generic;

namespace AC_Shield.Core
{
	public class DatabaseModule : ThreadModule
	{
		private string databasePath;
		private string databaseName;
		private SqliteConnection? connection;

		public DatabaseModule(ILogger Logger, string DatabasePath,string DatabaseName) : base(Logger, ThreadPriority.Normal,5000)
		{
			this.databasePath = DatabasePath;
			this.databaseName = DatabaseName;
			this.connection = null;
		}

		private IResult<bool> CreateDatabaseFile()
		{
			string databaseFileName;
			IResult<bool> result;

			databaseFileName = System.IO.Path.Combine(databasePath, databaseName);

			if (System.IO.File.Exists(databaseFileName))
			{
				Log(Message.Information($"Database file found at {databaseFileName}, deleting file"));
				result=Try(() => System.IO.File.Delete(databaseFileName)).Select(success => true);
				if (!result.Succeeded()) return result;
			}					

			if (!System.IO.Directory.Exists(databasePath))
			{
				Log(Message.Information($"Database folder not found at {databasePath}, creating one"));
				result = Try(() => System.IO.Directory.CreateDirectory(databasePath)).Select(success => true);
				if (!result.Succeeded()) return result;
			}

			Log(Message.Information($"Create new database file at {databaseFileName}"));
			result = Try(() => System.IO.File.Create(databaseFileName).Close()).Select(success => true);

			return result;
		}

		private IResult<bool> CreateTables()
		{
			string command;
			SqliteCommand createCDRTable,createBlackListTable;

			command = "CREATE TABLE IF NOT EXISTS CDR (ID INTEGER PRIMARY KEY AUTOINCREMENT, TimeStamp DateTime NOT NULL,IPGroup NVARCHAR(256) NOT NULL,  SourceURI NVARCHAR(1024) NOT NULL)";
			createCDRTable = new SqliteCommand(command, connection);

			command = "CREATE TABLE IF NOT EXISTS BlackList (ID INTEGER PRIMARY KEY AUTOINCREMENT, BlackListStartTime DateTime NOT NULL,BlackListEndTime DateTime NOT NULL,IPGroup NVARCHAR(256) NOT NULL, SourceURI NVARCHAR(1024) NOT NULL)";
			createBlackListTable = new SqliteCommand(command, connection);


			return Try(() => { 
				createCDRTable.ExecuteNonQuery();
				createBlackListTable.ExecuteNonQuery(); 
			})
			.Select(success => true);
		}

		public IResult<bool> InsertCDR(CDR CDR)
		{
			string command = "INSERT INTO CDR (TimeStamp , IPGroup , SourceURI) VALUES (@TimeStamp , @IPGroup , @SourceURI)";
			SqliteCommand insert = new SqliteCommand(command, connection);
			insert.Parameters.AddWithValue("@TimeStamp", CDR.SetupTime);
			insert.Parameters.AddWithValue("@IPGroup", CDR.IPGroup);
			insert.Parameters.AddWithValue("@SourceURI", CDR.SrcURI);
			
			return Try(()=>insert.ExecuteNonQuery()).Select(success => true);
		}

		public IResult<bool> UpdateBlackList(string IPGroup,string SourceURI,DateTime BlackListStartTime,DateTime BlackListEndTime)
		{
			string command;
			SqliteCommand insert, select,update;
			object? id;

			command = "select ID from BlackList where IPGroup=@IPGroup and SourceURI=@SourceURI";
			select=new SqliteCommand(command, connection);
			select.Parameters.AddWithValue("@IPGroup", IPGroup);
			select.Parameters.AddWithValue("@SourceURI", SourceURI);
			if (!Try(()=>select.ExecuteScalar()).Succeeded(out id)) return Result.Fail<bool>(new Exception("Failed to query BlackList table"));

			if (id!=null)
			{
				Log(Message.Information($"Source URI {SourceURI} in IP Group {IPGroup} is already black listed, updating end time"));
				command = "update BlackList set BlackListEndTime=@BlackListEndTime where ID=@ID";
				update = new SqliteCommand(command, connection);
				update.Parameters.AddWithValue("@BlackListEndTime", BlackListEndTime);
				update.Parameters.AddWithValue("@ID", id);
				return Try(() => update.ExecuteNonQuery()).Select(success => true);
			}
			else
			{
				Log(Message.Information($"Inserting Source URI {SourceURI}/IP Group {IPGroup} in black list"));
				command = "INSERT INTO BlackList (IPGroup , SourceURI, BlackListStartTime, BlackListEndTime) VALUES (@IPGroup, @SourceURI , @BlackListStartTime, @BlackListEndTime)";
				insert = new SqliteCommand(command, connection);
				insert.Parameters.AddWithValue("@IPGroup", IPGroup);
				insert.Parameters.AddWithValue("@SourceURI", SourceURI);
				insert.Parameters.AddWithValue("@BlackListStartTime", BlackListStartTime);
				insert.Parameters.AddWithValue("@BlackListEndTime", BlackListEndTime);
				return Try(() => insert.ExecuteNonQuery()).Select(success => true);

			}


		}

		private IResult<CallerReport[]> ReadCallerReports(SqliteDataReader Reader)
		{
			List<CallerReport> reports = new List<CallerReport>();
			while (Reader.Read())
			{
				CallerReport report = new CallerReport();
				report.Count = Reader.GetInt32(0);
				report.IPGroup= Reader.GetString(1);
				report.SourceURI = Reader.GetString(2);
				reports.Add(report);
			}
			return Result.Success(reports.ToArray());
		}

		public IResult<CallerReport[]> GetCallerReports(DateTime StartDate)
		{
			string command = "SELECT COUNT(SourceURI), IPGroup, SourceURI FROM CDR where TimeStamp>=@StartDate GROUP BY SourceURI,IPGroup";
			SqliteCommand select = new SqliteCommand(command, connection);
			select.Parameters.AddWithValue("@StartDate", StartDate);

			return Try(() => select.ExecuteReader()).SelectResult(reader => ReadCallerReports(reader),failure=>failure);
		}

		protected override void ThreadLoop()
		{
			string databaseFileName;

			databaseFileName = System.IO.Path.Combine(databasePath, "AC_Shield.db");

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
				this.WaitHandles(-1, QuitEvent);
			}
		}

	}
}
