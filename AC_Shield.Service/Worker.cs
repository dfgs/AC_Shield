using AC_Shield.Core.Modules;
using LogLib;
using Microsoft.Extensions.Logging;

namespace AC_Shield.Service
{
	public class Worker : BackgroundService
	{
		private readonly ILogger<Worker> _logger;
		private MainModule mainModule;
		

		public Worker(MainModule MainModule,ILogger<Worker> logger)
		{
			_logger = logger;
			this.mainModule = MainModule;
		}

		public override Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Starting AC_Shield service");
			mainModule.Start();
			return base.StartAsync(cancellationToken);
		}

		public override Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Stopping AC_Shield service");
			mainModule.Stop();
			return base.StopAsync(cancellationToken);
		}


		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			try
			{
				while (!stoppingToken.IsCancellationRequested)
				{
					// nope
					await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
				}
			}
			catch (OperationCanceledException)
			{
				// When the stopping token is canceled, for example, a call made from services.msc,
				// we shouldn't exit with a non-zero exit code. In other words, this is expected...
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "{Message}", ex.Message);

				// Terminates this process and returns an exit code to the operating system.
				// This is required to avoid the 'BackgroundServiceExceptionBehavior', which
				// performs one of two scenarios:
				// 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
				// 2. When set to "StopHost": will cleanly stop the host, and log errors.
				//
				// In order for the Windows Service Management system to leverage configured
				// recovery options, we need to terminate the process with a non-zero exit code.
				Environment.Exit(1);
			}
		}

		
	}
}
