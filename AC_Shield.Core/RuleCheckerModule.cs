using LogLib;
using ModuleLib;
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
	public class RuleCheckerModule:ThreadModule
	{
		private DatabaseModule databaseModule;

		public RuleCheckerModule(ILogger Logger, DatabaseModule DatabaseModule) : base(Logger, ThreadPriority.Normal, 5000)
		{
			this.databaseModule = DatabaseModule;
		}
		protected override void ThreadLoop()
		{
			bool result;
			

			Log(Message.Information("Waiting for data or quit signal"));
			while (State == ModuleStates.Started)
			{
				this.WaitHandles(-1, QuitEvent);
			}

		}
	}
