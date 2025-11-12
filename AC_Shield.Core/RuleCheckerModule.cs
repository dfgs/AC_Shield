using LogLib;
using ModuleLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AC_Shield.Core
{
	public class RuleCheckerModule : ThreadModule
	{
		private DatabaseModule databaseModule;
		private int checkIntervalSeconds;
		private int cdrRHistoryPeriodSeconds;
		private int maxCallsThreshold;
		private int blackListDurationSeconds;
		public RuleCheckerModule(ILogger Logger, DatabaseModule DatabaseModule, int CheckIntervalSeconds, int CDRHistoryPeriodSeconds,int MaxCallsThreshold,int BlackListDurationSeconds) : base(Logger, ThreadPriority.Normal, 5000)
		{
			this.databaseModule = DatabaseModule;
			this.checkIntervalSeconds = CheckIntervalSeconds;
			this.cdrRHistoryPeriodSeconds = CDRHistoryPeriodSeconds;
			this.maxCallsThreshold = MaxCallsThreshold;
			this.blackListDurationSeconds = BlackListDurationSeconds;
		}
		protected override void ThreadLoop()
		{
			CallerReport[] reports;

			Log(Message.Information("Waiting for data or quit signal"));
			while (State == ModuleStates.Started)
			{
				this.WaitHandles(checkIntervalSeconds*1000, QuitEvent);

				if (!databaseModule.GetCallerReports(DateTime.Now.AddSeconds(-cdrRHistoryPeriodSeconds)).Match(
					success => { reports = success;  },
					failure => { Log(Message.Error($"Failed to get caller reports: {failure.Message}"));  }
				).Succeeded(out reports)) continue;

				foreach (CallerReport report in reports)
				{
					Log(Message.Information($"Report: {report.SourceURI}/{report.Count} calls"));
					if (report.Count<maxCallsThreshold) continue;
					Log(Message.Information($"Caller {report.SourceURI} has reached max call threshold, adding caller to black list"));
					databaseModule.UpdateBlackList(report.IPGroup,report.SourceURI,DateTime.Now, DateTime.Now.AddSeconds(blackListDurationSeconds));
				}

			}

		}
	}

}
