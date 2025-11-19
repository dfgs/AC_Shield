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

namespace AC_Shield.Core.Modules
{
	// this module is responsible for checking caller reports against defined rules and updating black list accordingly
	public class RuleCheckerModule : ThreadModule
	{
		private IDatabaseModule databaseModule;
		private int rulesCheckIntervalSeconds;
		private int cdrRHistoryPeriodSeconds;
		private int maxCallsThreshold;
		private int blackListDurationSeconds;
		public RuleCheckerModule(ILogger Logger, IDatabaseModule DatabaseModule, int RulesCheckIntervalSeconds, int CDRHistoryPeriodSeconds,int MaxCallsThreshold,int BlackListDurationSeconds) : base(Logger, ThreadPriority.Normal, 5000)
		{
			databaseModule = DatabaseModule;
			rulesCheckIntervalSeconds = RulesCheckIntervalSeconds;
			cdrRHistoryPeriodSeconds = CDRHistoryPeriodSeconds;
			maxCallsThreshold = MaxCallsThreshold;
			blackListDurationSeconds = BlackListDurationSeconds;
		}
		protected override void ThreadLoop()
		{
			CallerReport[] reports;
			BlackListItem blackList;

			Log(Message.Information("Waiting for data or quit signal"));
			while (State == ModuleStates.Started)
			{
				WaitHandles(rulesCheckIntervalSeconds*1000, QuitEvent);
				if (State != ModuleStates.Started) break;

				if (!databaseModule.GetCallerReports(DateTime.Now.AddSeconds(-cdrRHistoryPeriodSeconds)).Match(
					success => Log(Message.Information($"Caller reports collected succesfully")),
					failure => Log(Message.Error($"Failed to get caller reports: {failure.Message}"))
				).Succeeded(out reports)) continue;

				foreach (CallerReport report in reports)
				{
					Log(Message.Information($"Report: {report.Caller} has made {report.Count} calls during last {cdrRHistoryPeriodSeconds} seconds"));
					if (report.Count<maxCallsThreshold) continue;
					Log(Message.Information($"Caller {report.Caller} has reached max call threshold ({maxCallsThreshold}), adding caller to black list"));
					blackList = new BlackListItem(Guid.NewGuid(), report.IPGroup, report.Caller, DateTime.Now, DateTime.Now.AddSeconds(blackListDurationSeconds));
					databaseModule.UpdateBlackList(blackList);
				}

			}

		}
	}

}
