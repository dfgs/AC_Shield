using LogLib;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModuleLib;
using ResultTypeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace AC_Shield.Core
{
	public class RESTModule : ThreadModule
	{
		private int cdrRHistoryPeriodSeconds;
		private DatabaseModule databaseModule;
		private int port;

		public RESTModule(LogLib.ILogger Logger, DatabaseModule DatabaseModule, int Port,int CDRHistoryPeriodSeconds) : base(Logger, ThreadPriority.Normal, 5000)
		{
			this.databaseModule = DatabaseModule;
			this.port = Port;
			this.cdrRHistoryPeriodSeconds = CDRHistoryPeriodSeconds;
		}

		private string GetCallerPermission(string Caller)
		{
			BlackListItem[] items;

			if (!databaseModule.GetBlackList(DateTime.Now,Caller).Succeeded(out items)) return "Allow";
			if (items.Length>0) return "Block";
			else return "Allow";

		}

		private CDR[] GetFirstCDRs(int Count)
		{
			CDR[] items;

			if (!databaseModule.GetFirstCDR(Count).Succeeded(out items)) return new CDR[] { };

			return items;

		}
		private CDR[] GetLastCDRs(int Count)
		{
			CDR[] items;

			if (!databaseModule.GetLastCDR(Count).Succeeded(out items)) return new CDR[] { };

			return items;

		}
		private CallerReport[] GetCallerReports()
		{
			CallerReport[] items;

			if (!databaseModule.GetCallerReports(DateTime.Now.AddSeconds(-cdrRHistoryPeriodSeconds)).Succeeded(out items)) return new CallerReport[] { };

			return items;

		}
		protected override void ThreadLoop()
		{

			Log(Message.Information("Waiting for data or quit signal"));
			while (State == ModuleStates.Started)
			{
				Log(Message.Information($"Starting web server on port {port}"));

				var builder = WebApplication.CreateBuilder();
				builder.Services.AddSingleton<IHostLifetime, AspConsoleLifetime>();
				builder.Logging.ClearProviders();
				builder.Logging.AddProvider(new AspLoggerProvider(Logger));
				var app = builder.Build();
				app.Urls.Add($"http://0.0.0.0:{port}");


				app.MapGet("/CallerPermission/{Caller}", (string caller) => GetCallerPermission(caller));
				app.MapGet("/CDR/First/{Count}", (int count) => GetFirstCDRs(count));
				app.MapGet("/CDR/Last/{Count}", (int count) => GetLastCDRs(count));
				app.MapGet("/CallerReport", () => GetCallerReports());

				app.RunAsync();

				this.WaitHandles(-1, QuitEvent);

				Log(Message.Information("Stopping web server"));
				app.StopAsync();
				//app.D();

			}
		 

		
		}


	}
}
