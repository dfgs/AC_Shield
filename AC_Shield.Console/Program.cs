// See https://aka.ms/new-console-template for more information
using AC_Shield.Core.Modules;
using LogLib;
using ModuleLib;
using System;
using System.Configuration;
using System.IO;

namespace AC_Shield.Console
{
	internal class Program
	{
		private static ILogger? logger;
		private static MainModule? main;

		static void Main(string[] args)
		{

			logger = new ConsoleLogger(new DefaultLogFormatter());
			//logger = new FileLogger(new DefaultLogFormatter(), Path.Combine(@"C:\ProgramData\AC_Shield", "AC_Shield.log"), 10);

			main = new MainModule(logger);

			main.Start();
			System.Console.ReadLine();
			main.Stop();
			logger.Log(new Log(DateTime.Now, 0, "Main", "Main", Message.Information("AC_Shield console stopped, press enter to quit")));

			System.Console.ReadLine();

		}
	}
}



