// See https://aka.ms/new-console-template for more information
using AC_Shield.Core;
using LogLib;
using ModuleLib;
using System;
using System.Configuration;

namespace AC_Shield.Console
{
	internal class Program
	{
		private static ILogger? logger;
		private static DatabaseModule? databaseModule;
		private static CDRReceiverModule? cdrReceiverModule;
		private static RuleCheckerModule? ruleCheckerModule;

		static void Main(string[] args)
		{
			int port;
			string ipGroup;
			int checkIntervalSeconds;
			int cdrHistoryPeriodSeconds;
			int maxCallsThreshold;
			int blackListDurationSeconds;

			logger = new ConsoleLogger(new DefaultLogFormatter());
			
			logger.Log(new Log(DateTime.Now,0,"Main","Main", Message.Information("Starting AC_Shield console")));
			
			databaseModule =new DatabaseModule(logger, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AC_Shield"),"AC_Shield.db");
			databaseModule.Start();

			try
			{
				port = int.Parse(ConfigurationManager.AppSettings["Port"] ?? "514");
				ipGroup = ConfigurationManager.AppSettings["IPGroup"] ?? "LAN";
				checkIntervalSeconds = int.Parse(ConfigurationManager.AppSettings["CheckIntervalSeconds"] ?? "60");
				cdrHistoryPeriodSeconds = int.Parse(ConfigurationManager.AppSettings["CDRHistoryPeriodSeconds"] ?? "60");
				maxCallsThreshold = int.Parse(ConfigurationManager.AppSettings["MaxCallsThreshold"] ?? "10");
				blackListDurationSeconds = int.Parse(ConfigurationManager.AppSettings["BlackListDurationSeconds"] ?? "3600");
			}
			catch (Exception ex)
			{
				logger.Log(new Log(DateTime.Now, 0, "Main", "Main", Message.Fatal($"Invalid parameter find in app.config file: {ex.Message}")));
				return;
			}

			cdrReceiverModule =new CDRReceiverModule(logger,databaseModule, port, ipGroup);
			cdrReceiverModule.Start();

			ruleCheckerModule = new RuleCheckerModule(logger, databaseModule,checkIntervalSeconds,cdrHistoryPeriodSeconds,maxCallsThreshold,blackListDurationSeconds);
			ruleCheckerModule.Start();


			System.Console.ReadLine();

			ruleCheckerModule.Stop();
			cdrReceiverModule.Stop();
			databaseModule.Stop();
			logger.Log(new Log(DateTime.Now, 0, "Main", "Main", Message.Information("AC_Shield console stopped, press enter to quit")));

			System.Console.ReadLine();

		}
	}
}



