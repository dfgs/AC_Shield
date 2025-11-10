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
			string command = "CREATE TABLE IF NOT EXISTS CDR (CallID INTEGER PRIMARY KEY AUTOINCREMENT, TimeStamp DateTime NOT NULL,IPGroup NVARCHAR(256) NOT NULL,  SourceURI NVARCHAR(1024) NOT NULL)";
			SqliteCommand createTable = new SqliteCommand(command, connection);
			return Try(() => createTable.ExecuteNonQuery()).Select(success => true);
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

		protected override void ThreadLoop()
		{
			string databaseFileName;
			bool result;

			databaseFileName = System.IO.Path.Combine(databasePath, "AC_Shield.db");

			result=CreateDatabaseFile().Match(
				success => Log(Message.Information($"Database file initialized succesfully")),
				failure => Log(Message.Error($"Failed to initialise database file: {failure.Message}"))
			);
			if (!result) return;

			result=Try(() => new SqliteConnection($"Filename={databaseFileName}"))
				.Match(
					success => connection = success,
					failure => Log(Message.Error($"Failed to initialise database connection: {failure.Message}"))
			);
			if (connection == null) return;

			result = Try(() => connection.Open())
				.Match(
					success => Log(Message.Information($"Database opened successfully")),
					failure => Log(Message.Error($"Failed to open database connection: {failure.Message}"))
			);
			if (!result) return;

			//SQLitePCL.Batteries.Init();

			result =  CreateTables().Match(
				success => Log(Message.Information($"Database tables initialized succesfully")),
				failure => Log(Message.Error($"Failed to initialise database tables: {failure.Message}"))
			);
			if (!result) return;

			

			while (State == ModuleStates.Started)
			{
				this.WaitHandles(-1, QuitEvent);
			}
		}

	}
}
