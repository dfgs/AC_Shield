using AC_Shield.Core.Modules;
using AC_Shield.Daemon;
using LogLib;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;



string logPath = System.Configuration.ConfigurationManager.AppSettings["LogPath"] ?? @"/var/log/AC_Shield";


if (!System.IO.Path.Exists(logPath))
{
	Console.WriteLine($"Log path '{logPath}' does not exist. Creating it...");
	System.IO.Directory.CreateDirectory(logPath);
}
else
{
	Console.WriteLine($"Log path '{logPath}' exist. skipping...");
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



var builder =  Host.CreateDefaultBuilder(args)
	.UseSystemd()
	.ConfigureServices((hostContext, services) =>
	{
		services.AddHostedService<Worker>();
		services.AddSingleton<MainModule>(mainModule);
		services.AddHostedService<Worker>();
	});


var host = builder.Build();
host.Run();
