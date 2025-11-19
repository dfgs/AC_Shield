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
	// This module is responsible for generating dial plan CSV files from black list
	public class DialPlanGeneratorModule:ThreadModule
	{
		private IDatabaseModule databaseModule;
		private int dialPlanGenerateIntervalSeconds;
		private string dialPlanName ;
		private string blackListTag;
		private string exportPath;

		public DialPlanGeneratorModule(ILogger Logger, IDatabaseModule DatabaseModule, int DialPlanGenerateIntervalSeconds,string ExportPath, string DialPlanName, string BlackListTag) : base(Logger, ThreadPriority.Normal, 5000)
		{
			exportPath = ExportPath;
			databaseModule = DatabaseModule;
			dialPlanGenerateIntervalSeconds = DialPlanGenerateIntervalSeconds;
			dialPlanName = DialPlanName;
			blackListTag = BlackListTag;
		}
		private void CreateCSV(BlackListItem[] Items)
		{
			string dialPlanFileName;

			dialPlanFileName = Path.Combine(exportPath, $"{dialPlanName}_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.csv");
			using(FileStream stream=new FileStream(dialPlanFileName,FileMode.Create))
			{
				StreamWriter writer=new StreamWriter(stream);
				writer.WriteLine("DialPlanName,Name,Prefix,Tag");
				foreach (BlackListItem item in Items)
				{
					
					writer.WriteLine($"\"{dialPlanName}\",\"{item.Caller}\",\"{item.Caller}#\",\"{blackListTag}\"");
				}
				writer.Flush();
			}
		}

		protected override void ThreadLoop()
		{
			int duration;

			BlackListItem[] blackList;

			Log(Message.Information("Waiting for data or quit signal"));
			while (State == ModuleStates.Started)
			{
				if (dialPlanGenerateIntervalSeconds==-1) duration=-1;
				else duration=dialPlanGenerateIntervalSeconds * 1000;
				WaitHandles(duration, QuitEvent);
				if (State!=ModuleStates.Started) break;

				if (!databaseModule.GetBlackList(DateTime.Now).Match(
					success => Log(Message.Information($"Black list collected succesfully")),
					failure => Log(Message.Error($"Failed to get black list: {failure.Message}"))
				).Succeeded(out blackList)) continue;

				Try(()=>CreateCSV(blackList)).Match(
					success => Log(Message.Information($"Dial plan generated successfully")),
					failure => Log(Message.Error($"Failed to generate dial plan: {failure.Message}"))
				);

			}

		}
	}
}
