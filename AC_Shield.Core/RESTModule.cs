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
		private DatabaseModule databaseModule;
		private int port;

		public RESTModule(LogLib.ILogger Logger, DatabaseModule DatabaseModule, int Port) : base(Logger, ThreadPriority.Normal, 5000)
		{
			this.databaseModule = DatabaseModule;
			this.port = Port;
		}

		private string GetStatus(string Caller)
		{
			BlackListItem[] items;

			if (!databaseModule.GetBlackList(DateTime.Now,Caller).Succeeded(out items)) return "Allow";
			if (items.Length>0) return "Block";
			else return "Allow";

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
				app.Urls.Add($"http://localhost:{port}");


				app.MapGet("/GetCallerStatus/{Caller}", (string caller) => GetStatus(caller));

				app.RunAsync();

				this.WaitHandles(-1, QuitEvent);

				Log(Message.Information("Stopping web server"));
				app.StopAsync();
				//app.D();

			}
		 

		
		}


	}
}
