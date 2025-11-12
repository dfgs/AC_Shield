using AC_Shield.Core;
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

string path = Path.Combine(@"C:\ProgramData", "AC_Shield");
if (!System.IO.Path.Exists(path))
{
	System.IO.Directory.CreateDirectory(path);
}

MainModule mainModule = new MainModule(new FileLogger(new DefaultLogFormatter(), Path.Combine(path, "AC_Shield.log")));

builder.Services.AddSingleton<MainModule>(mainModule);
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();

// pour créer le service sc.exe create ".AC_Shield Service" binpath= "D:\Projets\AC_Shield\AC_Shield.Service\bin\Release\net8.0\win-x64\publish\win-x64\AC_Shield.Service.exe"