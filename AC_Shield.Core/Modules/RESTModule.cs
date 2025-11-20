using LogLib;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
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


namespace AC_Shield.Core.Modules
{
	// this module is responsible for providing REST API access to AC Shield data
	public class RESTModule : ThreadModule
	{
		private int cdrRHistoryPeriodSeconds;
		private IDatabaseModule databaseModule;
		private int port;
		private string[] whiteList;
		private string certificateName;

		public RESTModule(LogLib.ILogger Logger, IDatabaseModule DatabaseModule, int Port,int CDRHistoryPeriodSeconds,string CertificateName, params string[] WhiteList) : base(Logger, ThreadPriority.Normal, 5000)
		{
			databaseModule = DatabaseModule;
			port = Port;
			cdrRHistoryPeriodSeconds = CDRHistoryPeriodSeconds;
			this.certificateName = CertificateName;
			this.whiteList = WhiteList;
		}

		private string GetCallerPermission(string Caller)
		{
			BlackListItem[] items;

			if (whiteList.Contains(Caller)) return "Allow";
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

		private BlackListItem[] GetBlackList()
		{
			BlackListItem[] items;

			if (!databaseModule.GetBlackList(DateTime.Now).Succeeded(out items)) return new BlackListItem[] { };

			return items;

		}
		/*public static IHostBuilder CreateHostBuilder(string[] args)
		{
			var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
			store.Open(OpenFlags.ReadOnly);
			var certificate = store.Certificates.OfType<X509Certificate2>()
				.First(c => c.FriendlyName ==
				"Ivan Yakimov Test-only Certificate For Server Authorization");

			return Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder
						.UseKestrel(options =>
						{
							options.Listen(System.Net.IPAddress.Loopback, 44321, listenOptions =>
							{
								var connectionOptions = new HttpsConnectionAdapterOptions();
								connectionOptions.ServerCertificate = certificate;

								listenOptions.UseHttps(connectionOptions);
							});
						})
						.UseStartup<Startup>();
				});
		}*/
		private X509Certificate2 GetCertificateFromStore(string CertName)
		{
			X509Certificate2? certificate;

			X509Store store = new X509Store(StoreLocation.LocalMachine);
			
			try
			{
				store.Open(OpenFlags.ReadOnly);

				// Place all certificates in an X509Certificate2Collection object.
				X509Certificate2Collection certCollection = store.Certificates;
				// If using a certificate with a trusted root you do not need to FindByTimeValid, instead:
				// currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certName, true);
				X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
				//X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, CertName, false);
				certificate=currentCerts.FirstOrDefault(item => item.FriendlyName == CertName);
				if (certificate== null)
				{
					throw new Exception($"Certificate '{CertName}' not found in LocalMachine store");
				}
				return certificate;
			}
			finally
			{
				store.Close();
			}
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

				if (string.IsNullOrEmpty(certificateName))
				{
					builder.WebHost.ConfigureKestrel((context, serverOptions) =>
					{
						serverOptions.Listen(IPAddress.Any, port);
					});
				}
				else
				{
					builder.WebHost.ConfigureKestrel((context, serverOptions) =>
					{
						X509Certificate2? certificate;

						if (Try(() => GetCertificateFromStore(certificateName)).Match(
							success => Log(Message.Information($"Certificate with friendly name {certificateName} found")),
							failure => Log(Message.Error($"Failed to get certificate with friendly name {certificateName}: {failure.Message}"))
							).Succeeded(out certificate))
						{
							serverOptions.Listen(IPAddress.Any, port, listenOptions =>
							{
								listenOptions.UseHttps(certificate);
							});
						}
					});
				}

				

				var app = builder.Build();
				
				app.MapGet("/CallerPermission/{Caller}", (string caller) => GetCallerPermission(caller));
				app.MapGet("/CDR/First/{Count}", (int count) => GetFirstCDRs(count));
				app.MapGet("/CDR/Last/{Count}", (int count) => GetLastCDRs(count));
				app.MapGet("/CallerReport", () => GetCallerReports());
				app.MapGet("/BlackList", () => GetBlackList());

				app.RunAsync();

				WaitHandles(-1, QuitEvent);

				Log(Message.Information("Stopping web server"));
				app.StopAsync();
				//app.D();

			}
		 

		
		}


	}
}
