using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogLib;
using ModuleLib;
using System.Configuration;
using ResultTypeLib;

namespace AC_Shield.Core
{
	public class MainModule : ThreadModule
	{
		private DatabaseModule? databaseModule;
		private CDRReceiverModule? cdrReceiverModule;
		private RuleCheckerModule? ruleCheckerModule;
		private DialPlanGeneratorModule? dialPlanGeneratorModule;

		public MainModule(ILogger Logger) : base(Logger,ThreadPriority.Normal,5000)
		{
			int port;
			string ipGroup;
			int rulesCheckIntervalSeconds;
			int cdrHistoryPeriodSeconds;
			int maxCallsThreshold;
			int blackListDurationSeconds;
			int dBCleanIntervalSeconds;
			int cdrRetentionSeconds;
			int blackListRetentionSeconds;
			int dialPlanGenerateIntervalSeconds;
			string dialPlanName;
			string blackListTag;

			try
			{
				port = int.Parse(ConfigurationManager.AppSettings["Port"] ?? "514");
				ipGroup = ConfigurationManager.AppSettings["IPGroup"] ?? "LAN";
				rulesCheckIntervalSeconds = int.Parse(ConfigurationManager.AppSettings["RulesCheckIntervalSeconds"] ?? "60");
				dBCleanIntervalSeconds = int.Parse(ConfigurationManager.AppSettings["DBCleanIntervalSeconds"] ?? "1800");
				cdrHistoryPeriodSeconds = int.Parse(ConfigurationManager.AppSettings["CDRHistoryPeriodSeconds"] ?? "60");
				cdrRetentionSeconds = int.Parse(ConfigurationManager.AppSettings["CDRRetentionSeconds"] ?? "3600");
				maxCallsThreshold = int.Parse(ConfigurationManager.AppSettings["MaxCallsThreshold"] ?? "10");
				blackListDurationSeconds = int.Parse(ConfigurationManager.AppSettings["BlackListDurationSeconds"] ?? "3600");
				blackListRetentionSeconds = int.Parse(ConfigurationManager.AppSettings["BlackListRetentionSeconds"] ?? "3600");
				dialPlanGenerateIntervalSeconds = int.Parse(ConfigurationManager.AppSettings["DialPlanGenerateIntervalSeconds"] ?? "7200");
				dialPlanName= ConfigurationManager.AppSettings["DialPlanName"] ?? "BlackList";
				blackListTag= ConfigurationManager.AppSettings["BlackListTag"] ?? "BlackList";
			}
			catch (Exception ex)
			{
				Log(Message.Fatal($"Invalid parameter find in app.config file: {ex.Message}"));
				return;
			}

			string path = Path.Combine(@"C:\ProgramData", "AC_Shield");

			databaseModule = new DatabaseModule(Logger, path, "AC_Shield.db", dBCleanIntervalSeconds, cdrRetentionSeconds, blackListRetentionSeconds);
			cdrReceiverModule = new CDRReceiverModule(Logger, databaseModule, port, ipGroup);
			ruleCheckerModule = new RuleCheckerModule(Logger, databaseModule, rulesCheckIntervalSeconds, cdrHistoryPeriodSeconds, maxCallsThreshold, blackListDurationSeconds);
			dialPlanGeneratorModule=new DialPlanGeneratorModule(Logger,databaseModule,dialPlanGenerateIntervalSeconds,path,dialPlanName,blackListTag);
		}

		protected override IResult<bool> OnStarting()
		{
			Log(Message.Information("Starting AC_Shield console"));

			if (!databaseModule?.Start().Succeeded() ?? false) return Result.Fail<bool>(new Exception("Failed to start database module"));

			Log(Message.Information("Waiting database module to start"));
			while (databaseModule?.State != ModuleStates.Started)
			{
				this.WaitHandles(1000, QuitEvent);
			}
			cdrReceiverModule?.Start();
			ruleCheckerModule?.Start();
			dialPlanGeneratorModule?.Start();
			return base.OnStarting();
		}

		protected override IResult<bool> OnStopping()
		{
			Log(Message.Information("Stopping AC_Shield console"));

			dialPlanGeneratorModule?.Stop();
			ruleCheckerModule?.Stop();
			cdrReceiverModule?.Stop();
			databaseModule?.Stop();

			return base.OnStopping();
		}
		protected override void ThreadLoop()
		{
			while(State==ModuleStates.Started)
			{
				this.WaitHandles(-1, QuitEvent);
			}
		}


	}
}
