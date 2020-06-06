using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tolitech.CodeGenerator.Logging.File
{
    [ProviderAlias("File")]
    public class FileLoggerProvider : LoggerProvider
    {
        bool Terminated;
        int Counter = 0;
        string FilePath;
        Dictionary<string, int> Lengths = new Dictionary<string, int>();

        ConcurrentQueue<LogEntry> InfoQueue = new ConcurrentQueue<LogEntry>();

        void ApplyRetainPolicy()
        {
            FileInfo FI;
            try
            {
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

        void WriteLine(string Text)
        {
            // check the file size after any 100 writes
            Counter++;
            if (Counter % 100 == 0)
            {
                FileInfo FI = new FileInfo(FilePath);
                if (FI.Length > (1024 * 1024 * Settings.MaxFileSizeInMB))
                {
                    BeginFile();
                }
            }

            System.IO.File.AppendAllText(FilePath, Text);
        }

        string Pad(string Text, int MaxLength)
        {
            if (string.IsNullOrWhiteSpace(Text))
                return "".PadRight(MaxLength);

            if (Text.Length > MaxLength)
                return Text.Substring(0, MaxLength);

            return Text.PadRight(MaxLength);
        }

        void PrepareLengths()
        {
            // prepare the lengs table
            Lengths["Time"] = 24;
            Lengths["Level"] = 14;
            Lengths["EventId"] = 64;
            Lengths["Category"] = 124;
            Lengths["MethodName"] = 32;
            Lengths["Scope"] = 64;
            Lengths["ActionId"] = 64;
            Lengths["ActionName"] = 184;
            Lengths["ActivityId"] = 64;
            Lengths["UserId"] = 64;
            Lengths["LoginName"] = 64;
            Lengths["RequestId"] = 64;
            Lengths["RequestPath"] = 64;
        }

        void BeginFile()
        {
            Directory.CreateDirectory(Settings.Folder);
            FilePath = Path.Combine(Settings.Folder, LogEntry.StaticHostName + "-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".log");

            StringBuilder SB = new StringBuilder();
            SB.Append(Pad("Time", Lengths["Time"]));
            SB.Append(Pad("Level", Lengths["Level"]));
            SB.Append(Pad("EventId", Lengths["EventId"]));
            SB.Append(Pad("Category", Lengths["Category"]));
            SB.Append(Pad("MethodName", Lengths["MethodName"]));
            SB.Append(Pad("Scope", Lengths["Scope"]));
            SB.Append(Pad("ActionId", Lengths["ActionId"]));
            SB.Append(Pad("ActionName", Lengths["ActionName"]));
            SB.Append(Pad("ActivityId", Lengths["ActivityId"]));
            SB.Append(Pad("UserId", Lengths["UserId"]));
            SB.Append(Pad("LoginName", Lengths["LoginName"]));
            SB.Append(Pad("RequestPath", Lengths["RequestPath"]));
            SB.AppendLine("Text");

            System.IO.File.WriteAllText(FilePath, SB.ToString());

            ApplyRetainPolicy();
        }

        void WriteLogLine()
        {
            LogEntry Info = null;
            if (InfoQueue.TryDequeue(out Info))
            {
                string S;
                string P;

                StringBuilder SB = new StringBuilder();
                SB.Append(Pad(Info.TimeStampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"), Lengths["Time"]));
                SB.Append(Pad(Info.Level.ToString(), Lengths["Level"]));
                SB.Append(Pad(Info.EventId != null ? Info.EventId.ToString() : "", Lengths["EventId"]));
                SB.Append(Pad(Info.Category, Lengths["Category"]));
                SB.Append(Pad(Info.MethodName, Lengths["MethodName"]));

                S = "";
                P = "";

                if (Info.Scopes != null && Info.Scopes.Count > 0)
                {
                    LogScopeInfo SI = Info.Scopes.Last();
                    if (!string.IsNullOrWhiteSpace(SI.Text))
                    {
                        S = SI.Text;
                    }

                    if (SI.Properties != null)
                    {
                        foreach (var properties in SI.Properties)
                        {
                            if (!string.IsNullOrEmpty(P))
                                P += " | ";

                            P += properties.Key + " = " + properties.Value;
                        }
                    }
                }
                SB.Append(Pad(S, Lengths["Scope"]));
                SB.Append(Pad(Info.ActionId, Lengths["ActionId"]));
                SB.Append(Pad(Info.ActionName, Lengths["ActionName"]));
                SB.Append(Pad(Info.ActivityId, Lengths["ActivityId"]));
                SB.Append(Pad(Info.UserId, Lengths["UserId"]));
                SB.Append(Pad(Info.LoginName, Lengths["LoginName"]));
                SB.Append(Pad(Info.RequestPath, Lengths["RequestPath"]));

                string Text = Info.Text;

                if (!string.IsNullOrWhiteSpace(Text))
                {
                    SB.Append(Text.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " "));
                }

                SB.AppendLine();
                WriteLine(SB.ToString());
            }
        }

        void ThreadProc()
        {
            Task.Run(() => {

                while (!Terminated)
                {
                    try
                    {
                        WriteLogLine();
                        System.Threading.Thread.Sleep(100);
                    }
                    catch { }
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            Terminated = true;
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
            InfoQueue.Enqueue(Info);
        }

        internal FileLoggerOptions Settings { get; private set; }
    }
}
