using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AC_Shield.Core
{
	public class AspLogger : IDisposable, Microsoft.Extensions.Logging.ILogger
	{
		private LogLib.ILogger _logger;

		public AspLogger(LogLib.ILogger Logger)
		{
			_logger = Logger;
		}
		public void Dispose()
		{
		}


		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			return this;
		}


		public bool IsEnabled(LogLevel logLevel)
		{
			return logLevel>=LogLevel.Information;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			LogLib.Message message;

			switch(logLevel)
			{
				case LogLevel.Trace: message = LogLib.Message.Debug(formatter(state, exception)); break;
				case LogLevel.Debug: message = LogLib.Message.Debug(formatter(state, exception)); break;
				case LogLevel.Information: message = LogLib.Message.Information(formatter(state, exception)); break;
				case LogLevel.Warning: message = LogLib.Message.Warning(formatter(state, exception)); break;
				case LogLevel.Error: message = LogLib.Message.Error(formatter(state, exception)); break;
				case LogLevel.Critical: message = LogLib.Message.Fatal(formatter(state, exception)); break;
				default: message = LogLib.Message.Debug(formatter(state, exception)); break;
			}

			_logger.Log(new LogLib.Log(DateTime.Now,0,"AspLogger","Log",message));
		}
	}
}
