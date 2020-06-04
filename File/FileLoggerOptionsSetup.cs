using System;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Tolitech.CodeGenerator.Logging.File
{
    internal class FileLoggerOptionsSetup : ConfigureFromConfigurationOptions<FileLoggerOptions>
    {
        public FileLoggerOptionsSetup(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration) : base(providerConfiguration.Configuration)
        {

        }
    }
}
