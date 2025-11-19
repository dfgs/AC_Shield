using AC_Shield.Core.Modules;
using AC_Shield.Service;
using LogLib;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using System.IO;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
	options.ServiceName = "AC_Shield Service";
});

LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);

string logPath = System.Configuration.ConfigurationManager.AppSettings["LogPath"] ?? @"C:\ProgramData\AC_Shield";

if (!System.IO.Path.Exists(logPath))
{
	System.IO.Directory.CreateDirectory(logPath);
}

int numberOfFilesToKeep;
try
{
	numberOfFilesToKeep = int.Parse(System.Configuration.ConfigurationManager.AppSettings["LogFileRetention"] ?? "10");
}
catch
{
	numberOfFilesToKeep = 10;
}

MainModule mainModule = new MainModule(new FileLogger(new DefaultLogFormatter(), Path.Combine(logPath, "AC_Shield.log"), numberOfFilesToKeep));

builder.Services.AddSingleton<MainModule>(mainModule);
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();

// pour créer le service sc.exe create "AC_Shield Service" binpath= "D:\Projets\AC_Shield\AC_Shield.Service\bin\Release\net8.0\win-x64\publish\win-x64\AC_Shield.Service.exe"