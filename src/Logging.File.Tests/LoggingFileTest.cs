using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Tolitech.CodeGenerator.Logging.File.Tests
{
    public class LoggingFileTest
    {
		private readonly ILogger<LoggingFileTest> _logger;

		public LoggingFileTest()
        {
			var config = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", false, true)
				.Build();

			var logLevel = (LogLevel)config.GetSection("Logging:File:LogLevel").GetValue(typeof(LogLevel), "Default");

			var loggerFactory = LoggerFactory.Create(logger =>
			{
				logger
					.AddConfiguration(config.GetSection("Logging"))
					.AddFileLogger(x => 
                        {
							x.LogLevel = logLevel;
                        }
                    );
			});
			
			_logger = loggerFactory.CreateLogger<LoggingFileTest>();
		}

		[Fact(DisplayName = "LoggingFile - IsEnabledTrue - Valid")]
		public void LoggingFile_IsEnabledTrue_Valid()
		{
			bool b = _logger.IsEnabled(LogLevel.Trace);
			Assert.True(b == true);
		}

		[Fact(DisplayName = "LoggingFile - CreateFile - Valid")]
		public void LoggingFile_CreateFile_Valid()
		{
			_logger.LogTrace("test1");
			_logger.LogDebug("test2");
			_logger.LogInformation("test3");
			_logger.LogWarning("test4");
			_logger.LogError("test5");
			_logger.LogCritical("test6");
			_logger.Log(LogLevel.Information, new EventId(10), "test7", "param1, param2, param3");

			Thread.Sleep(5000);

			var path = Path.GetDirectoryName(this.GetType().Assembly.Location);

			if (string.IsNullOrEmpty(path))
				throw new Exception("Error");

			var fileList = new DirectoryInfo(path)
					.GetFiles("*.log", SearchOption.TopDirectoryOnly)
					.ToList();
			
			Assert.True(fileList.Count > 0);
		}
	}
}
