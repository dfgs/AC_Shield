using LogLib;
using ModuleLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AC_Shield.Core.Modules
{
	// This module is responsible for managing log files, including rotating them at specified intervals.
	public class LogManagerModule:ThreadModule
	{
		private int logRotationIntervalSeconds;
		

		public LogManagerModule(ILogger Logger, int LogRotationIntervalSeconds) : base(Logger, ThreadPriority.Normal, 5000)
		{
			logRotationIntervalSeconds = LogRotationIntervalSeconds;
		}
		

		protected override void ThreadLoop()
		{

			Log(Message.Information("Waiting for data or quit signal"));
			while (State == ModuleStates.Started)
			{
				WaitHandles(logRotationIntervalSeconds * 1000, QuitEvent);
				if (State != ModuleStates.Started) break;
				
				Log(Message.Information("Rotating log file"));

				Try(()=> Logger.Rotate()).Match(
					success => Log(Message.Information($"Log rotated collected succesfully")),
					failure => Log(Message.Error($"Failed to rotate log file: {failure.Message}"))
				);


			}

		}
	}
}
