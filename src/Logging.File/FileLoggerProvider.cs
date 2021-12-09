using System;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tolitech.CodeGenerator.Logging.File
{
    [ProviderAlias("File")]
    public class FileLoggerProvider : LoggerProvider
    {
        private bool terminated;
        private int counter = 0;
        private string? filePath;

        readonly Dictionary<string, int> lengths = new();
        readonly ConcurrentQueue<LogEntry> infoQueue = new();

        private void ApplyRetainPolicy()
        {
            FileInfo FI;
            try
            {
                if (string.IsNullOrEmpty(Settings.Folder))
                    return;

                List<FileInfo> FileList = new DirectoryInfo(Settings.Folder)
                    .GetFiles("*.log", SearchOption.TopDirectoryOnly)
                    .OrderBy(fi => fi.CreationTime)
                    .ToList();

                while (FileList.Count >= Settings.RetainPolicyFileCount)
                {
                    FI = FileList.First();
                    FI.Delete();
                    FileList.Remove(FI);
                }
            }
            catch
            {
            }
        }

        private void WriteLine(string Text)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            // check the file size after any 100 writes
            counter++;
            if (counter % 100 == 0)
            {
                FileInfo FI = new(filePath);
                if (FI.Length > (1024 * 1024 * Settings.MaxFileSizeInMB))
                {
                    BeginFile();
                }
            }

            System.IO.File.AppendAllText(filePath, Text);
        }

        private string Pad(string? Text, int MaxLength)
        {
            if (string.IsNullOrWhiteSpace(Text))
                return "".PadRight(MaxLength);

            if (Text.Length > MaxLength)
                return Text[..MaxLength];

            return Text.PadRight(MaxLength);
        }

        private void PrepareLengths()
        {
            // prepare the lengs table
            lengths["Time"] = 24;
            lengths["Level"] = 14;
            lengths["EventId"] = 64;
            lengths["Category"] = 124;
            lengths["Scope"] = 64;
            lengths["ActionId"] = 64;
            lengths["ActionName"] = 184;
            lengths["ActivityId"] = 64;
            lengths["UserId"] = 64;
            lengths["LoginName"] = 64;
            lengths["RequestId"] = 64;
            lengths["RequestPath"] = 64;
        }

        private void BeginFile()
        {
            if (string.IsNullOrEmpty(Settings.Folder))
                return;

            Directory.CreateDirectory(Settings.Folder);
            filePath = Path.Combine(Settings.Folder, LogEntry.HostName + "-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".log");

            StringBuilder SB = new();
            SB.Append(Pad("Time", lengths["Time"]));
            SB.Append(Pad("Level", lengths["Level"]));
            SB.Append(Pad("EventId", lengths["EventId"]));
            SB.Append(Pad("Category", lengths["Category"]));
            SB.Append(Pad("Scope", lengths["Scope"]));
            SB.Append(Pad("ActionId", lengths["ActionId"]));
            SB.Append(Pad("ActionName", lengths["ActionName"]));
            SB.Append(Pad("ActivityId", lengths["ActivityId"]));
            SB.Append(Pad("UserId", lengths["UserId"]));
            SB.Append(Pad("LoginName", lengths["LoginName"]));
            SB.Append(Pad("RequestPath", lengths["RequestPath"]));
            SB.AppendLine("Text");

            System.IO.File.WriteAllText(filePath, SB.ToString());

            ApplyRetainPolicy();
        }

        private void WriteLogLine()
        {
            if (infoQueue.TryDequeue(out LogEntry? info))
            {
                string scope = "";

                StringBuilder SB = new();
                SB.Append(Pad(info.TimeStampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"), lengths["Time"]));
                SB.Append(Pad(info.Level.ToString(), lengths["Level"]));
                SB.Append(Pad(info.EventId != null ? info.EventId.ToString() : "", lengths["EventId"]));
                SB.Append(Pad(info.Category, lengths["Category"]));

                if (info.Scopes != null && info.Scopes.Count > 0)
                {
                    LogScopeInfo SI = info.Scopes.Last();
                    if (!string.IsNullOrWhiteSpace(SI.Text))
                    {
                        scope = SI.Text;
                    }
                }

                SB.Append(Pad(scope, lengths["Scope"]));
                SB.Append(Pad(info.ActionId, lengths["ActionId"]));
                SB.Append(Pad(info.ActionName, lengths["ActionName"]));
                SB.Append(Pad(info.ActivityId, lengths["ActivityId"]));
                SB.Append(Pad(info.UserId, lengths["UserId"]));
                SB.Append(Pad(info.LoginName, lengths["LoginName"]));
                SB.Append(Pad(info.RequestPath, lengths["RequestPath"]));

                string? Text = info.Text;

                if (!string.IsNullOrWhiteSpace(Text))
                {
                    SB.Append(Text.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " "));
                }

                SB.AppendLine();
                WriteLine(SB.ToString());
            }
        }

        private void ThreadProc()
        {
            Task.Run(() => {

                while (!terminated)
                {
                    try
                    {
                        WriteLogLine();
                        Thread.Sleep(100);
                    }
                    catch { }
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            terminated = true;
            base.Dispose(disposing);
        }

        public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> Settings) : this(Settings.CurrentValue)
        {
            SettingsChangeToken = Settings.OnChange(settings => { this.Settings = settings; });
        }

        public FileLoggerProvider(FileLoggerOptions Settings)
        {
            PrepareLengths();
            this.Settings = Settings;

            BeginFile();

            ThreadProc();
        }

        public override bool IsEnabled(LogLevel logLevel)
        {
            bool Result = logLevel != LogLevel.None && this.Settings.LogLevel != LogLevel.None && Convert.ToInt32(logLevel) >= Convert.ToInt32(this.Settings.LogLevel);

            return Result;
        }

        public override void WriteLog(LogEntry Info)
        {
            infoQueue.Enqueue(Info);
        }

        internal FileLoggerOptions Settings { get; private set; }
    }
}