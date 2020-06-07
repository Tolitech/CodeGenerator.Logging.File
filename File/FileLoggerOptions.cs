using System;
using Microsoft.Extensions.Logging;

namespace Tolitech.CodeGenerator.Logging.File
{
    public class FileLoggerOptions
    {
        string fFolder;
        int fMaxFileSizeInMB;
        int fRetainPolicyFileCount;

        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        public string Folder
        {
            get { return !string.IsNullOrWhiteSpace(fFolder) ? fFolder : System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location); }
            
            set { fFolder = value; }
        }

        public int MaxFileSizeInMB
        {
            get { return fMaxFileSizeInMB > 0 ? fMaxFileSizeInMB : 2; }

            set { fMaxFileSizeInMB = value; }
        }

        public int RetainPolicyFileCount
        {
            get { return fRetainPolicyFileCount < 5 ? 5 : fRetainPolicyFileCount; }

            set { fRetainPolicyFileCount = value; }
        }
    }
}
