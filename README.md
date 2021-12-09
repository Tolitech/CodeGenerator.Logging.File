# Logging.File
Logging File library used in projects created by the Code Generator tool.

This project contains the implementation for recording logs in physical text files. 

Tolitech Code Generator Tool: [http://www.tolitech.com.br](https://www.tolitech.com.br/)

Examples:

```
// Constructor
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
            });
    });

    _logger = loggerFactory.CreateLogger<LoggingFileTest>();
}
```

```
_logger.LogTrace("test1");
_logger.LogDebug("test2");
_logger.LogInformation("test3");
_logger.LogWarning("test4");
_logger.LogError("test5");
_logger.LogCritical("test6");
```