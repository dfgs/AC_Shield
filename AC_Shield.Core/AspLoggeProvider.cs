using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AC_Shield.Core
{
	[UnsupportedOSPlatform("browser")]
	[ProviderAlias("ColorConsole")]
	public sealed class AspLoggerProvider : ILoggerProvider
	{
		private AspLogger _logger;

		public AspLoggerProvider(LogLib.ILogger Logger)
		{
			_logger = new AspLogger(Logger);
		}

		public ILogger CreateLogger(string categoryName) => _logger;

		public void Dispose()
		{
		}


	}
}
