using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogLib;
using ModuleLib;
using System.Configuration;
using ResultTypeLib;

namespace AC_Shield.Core.Modules
{
	// this is main module, responsible for starting/stopping all other modules
	public class MainModule : ThreadModule
	{
		private IDatabaseModule? databaseModule;
		private CDRReceiverModule? cdrReceiverModule;
		private RESTModule? restModule;
		private RuleCheckerModule? ruleCheckerModule;
		private DialPlanGeneratorModule? dialPlanGeneratorModule;
		private LogManagerModule? logManagerModule;
		private ReportGeneratorModule? reportGeneratorModule;

		public MainModule(ILogger Logger) : base(Logger,ThreadPriority.Normal,5000)
		{
			string logPath;
			string databasePath;
			int cdrPort;
			int restPort;
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
			int logRotationIntervalSeconds;
			TimeOnly reportGenerationTime;
			string smtpServer;
			string smtpLogin;
			string smtpPassword;
			string reportFrom;
			string reportTo;
			string reportSubject;
			
			try
			{
				logPath= ConfigurationManager.AppSettings["LogPath"] ?? @"C:\ProgramData\AC_Shield";
				databasePath= ConfigurationManager.AppSettings["DatabasePath"] ?? @"C:\ProgramData\AC_Shield";
				cdrPort = int.Parse(ConfigurationManager.AppSettings["CDRPort"] ?? "514");
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
				logRotationIntervalSeconds = int.Parse(ConfigurationManager.AppSettings["LogRotationIntervalSeconds"] ?? "3600");
				reportGenerationTime= TimeOnly.Parse(ConfigurationManager.AppSettings["ReportGenerationTime"] ?? "00:00");
				smtpServer= ConfigurationManager.AppSettings["SMTPServer"] ?? "smtp.gmail.com";
				smtpLogin = ConfigurationManager.AppSettings["SMTPLogin"] ?? "";
				smtpPassword=	 ConfigurationManager.AppSettings["SMTPPassword"] ?? "";
				reportFrom = ConfigurationManager.AppSettings["ReportFrom"] ?? "AC_Shield@gmail.com";
				reportTo = ConfigurationManager.AppSettings["ReportTo"] ?? "report@gmail.com";
				reportSubject= ConfigurationManager.AppSettings["ReportSubject"] ?? "AC_Shield report";
				restPort= int.Parse(ConfigurationManager.AppSettings["RESTPort"] ?? "8080");
			}
			catch (Exception ex)
			{
				Log(Message.Fatal($"Invalid parameter find in app.config file: {ex.Message}"));
				return;
			}

			

			databaseModule = new SqlLiteDatabaseModule(Logger, databasePath, "AC_Shield.db", dBCleanIntervalSeconds, cdrRetentionSeconds, blackListRetentionSeconds);
			cdrReceiverModule = new CDRReceiverModule(Logger, databaseModule, cdrPort, ipGroup);
			restModule=new RESTModule(Logger,databaseModule,restPort,cdrHistoryPeriodSeconds);
			ruleCheckerModule = new RuleCheckerModule(Logger, databaseModule, rulesCheckIntervalSeconds, cdrHistoryPeriodSeconds, maxCallsThreshold, blackListDurationSeconds);
			dialPlanGeneratorModule=new DialPlanGeneratorModule(Logger,databaseModule,dialPlanGenerateIntervalSeconds,databasePath,dialPlanName,blackListTag);
			reportGeneratorModule= new ReportGeneratorModule(Logger,databaseModule, reportGenerationTime,smtpServer,smtpLogin,smtpPassword, reportFrom,reportTo,reportSubject);
			logManagerModule = new LogManagerModule(Logger, logRotationIntervalSeconds);
		}

		protected override IResult<bool> OnStarting()
		{
			Log(Message.Information("Starting AC_Shield console"));

			if (!databaseModule?.Start().Succeeded() ?? false) return Result.Fail<bool>(new Exception("Failed to start database module"));

			Log(Message.Information("Waiting database module to start"));
			while (databaseModule?.State != ModuleStates.Started)
			{
				WaitHandles(1000, QuitEvent);
			}
			cdrReceiverModule?.Start();
			restModule?.Start();
			ruleCheckerModule?.Start();
			dialPlanGeneratorModule?.Start();
			reportGeneratorModule?.Start();
			logManagerModule?.Start();
			return base.OnStarting();
		}

		protected override IResult<bool> OnStopping()
		{
			Log(Message.Information("Stopping AC_Shield console"));

			logManagerModule?.Stop();
			reportGeneratorModule?.Stop();
			dialPlanGeneratorModule?.Stop();
			ruleCheckerModule?.Stop();
			restModule?.Stop();
			cdrReceiverModule?.Stop();
			databaseModule?.Stop();

			return base.OnStopping();
		}
		protected override void ThreadLoop()
		{
			while(State==ModuleStates.Started)
			{
				WaitHandles(-1, QuitEvent);
			}
		}


	}
}
