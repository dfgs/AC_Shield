using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AC_Shield.Core
{
	public class AspConsoleLifetime : IHostLifetime, IDisposable
	{
		private readonly ILogger<AspConsoleLifetime> _logger;

		public AspConsoleLifetime(ILogger<AspConsoleLifetime> logger)
		{
			_logger = logger;
			
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public Task WaitForStartAsync(CancellationToken cancellationToken)
		{
			Console.CancelKeyPress += OnCancelKeyPressed;
			return Task.CompletedTask;
		}

		private void OnCancelKeyPressed(object? sender, ConsoleCancelEventArgs e)
		{
			_logger.LogInformation("Ctrl+C has been pressed, ignoring.");
			e.Cancel = true;
		}

		public void Dispose()
		{
			Console.CancelKeyPress -= OnCancelKeyPressed;
		}
	}
}
