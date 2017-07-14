using System;
using System.IO;
using System.Text;

namespace WinjetApp.Droid.Net
{
    public class Log
    {
        public enum LogLevelType
        {
            None = -1,
            Error,
            Warning,
            Info,
            Debug,
            Comm,
            All
        }

        public static Log Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_SyncRoot)
                    {
                        if (_Instance == null)
                            _Instance = new Log();
                    }
                }

                return _Instance;
            }
        }

        private static volatile Log _Instance;
        private static object _SyncRoot = new Object();

        private Log()
        {
            LogFileName = "WinJet2";
            LogFileExtension = ".log";
            LogLevel = LogLevelType.Error;
        }

        private StreamWriter Writer { get; set; }
        private LogLevelType LogLevel { get; set; }
        private string LogPath { get; set; }
        private string LogFileName { get; set; }
        private string LogFileExtension { get; set; }

        private string LogFile 
        { 
            get 
            { 
                return LogFileName + "_" + DateTime.Now.ToString("yyyy_MM_dd") + LogFileExtension; 
            } 
        }

        private string LogFullPath 
        { 
            get 
            { 
                return Path.Combine(LogPath, LogFile); 
            } 
        }

        private bool LogExists 
        { 
            get 
            { 
                return File.Exists(LogFullPath); 
            } 
        }

        private void WriteLineToLog(String inLogMessage)
        {
            WriteToLog(DateTime.Now.ToString("hh:mm:ss.fff") + " - " + inLogMessage + Environment.NewLine);
        }

        private Object DataLock = new Object();

        private void WriteToLog(String inLogMessage)
        {
            if (LogPath == null)
            {
                LogPath = Environment.CurrentDirectory;
            }
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }

            if (!LogExists)
            {
                Writer = null;
            }

            lock (DataLock)
            {
                if (Writer == null)
                {
                    Writer = new StreamWriter(LogFullPath, true);
                }

                if ((Writer != null) && (Writer.BaseStream != null) && (Writer.BaseStream.CanWrite))
                {
                    Writer.Write(inLogMessage);
                    Writer.Flush();
                }
            }
        }

        private void HexDumpToLog(byte[] inMessage)
        {
            int bytesperline = 16;
            if (inMessage == null)
            {
                WriteLineToLog(string.Empty);
                return;
            }

            int byteslen = inMessage.Length;

            char[] HexChars = "0123456789ABCDEF".ToCharArray();
            int firstHexColumn = 11;

            int firstCharColumn = firstHexColumn + bytesperline * 3 + (bytesperline - 1) / 8 + 2;
            int lineLength = firstCharColumn + bytesperline + Environment.NewLine.Length;
            char[] line = (new string((char)0x20, lineLength - 2) + Environment.NewLine).ToCharArray();
            int expectedLines = (byteslen + bytesperline - 1) / bytesperline;

            StringBuilder result = new StringBuilder(expectedLines * lineLength);

            int i, j;

            for (i = 0; i < byteslen; i = i + bytesperline)
            {
                line[0] = HexChars[(i >> 28) & 0xf];
                line[1] = HexChars[(i >> 24) & 0xf];
                line[2] = HexChars[(i >> 20) & 0xf];
                line[3] = HexChars[(i >> 16) & 0xf];
                line[4] = HexChars[(i >> 12) & 0xf];
                line[5] = HexChars[(i >> 8) & 0xf];
                line[6] = HexChars[(i >> 4) & 0xf];
                line[7] = HexChars[(i >> 0) & 0xf];

                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (j = 0; j < bytesperline; j++)
                {
                    if ((j > 0) && ((j & 7) == 0))
                        hexColumn++;
                    if ((i + j) >= byteslen)
                    {
                        line[hexColumn] = (char)0x20;
                        line[hexColumn + 1] = (char)0x20;
                        line[charColumn] = (char)0x20;
                    }
                    else
                    {
                        byte b = inMessage[i + j];
                        line[hexColumn] = HexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HexChars[b & 0xF];
                        line[charColumn] = (char)b;
                        if ((b < 32) || (b >= 127))
                            line[charColumn] = '.';
                    }
                    hexColumn += 3;
                    charColumn++;
                }
                result.Append(line);
            }
            WriteToLog(result.ToString());

        }

        public static void WriteLog(LogLevelType LogLevel, String inLogMessage)
        {
            if ((LogLevel > LogLevelType.None) && (LogLevel <= Instance.LogLevel))
                Instance.WriteLineToLog("[" + LogLevel.ToString().ToUpper() + "] " + inLogMessage);
        }

        public static void HexDump(LogLevelType LogLevel, byte[] inLogMessage)
        {
            if ((LogLevel > LogLevelType.None) && (LogLevel <= Instance.LogLevel))
                Instance.HexDumpToLog(inLogMessage);
        }
    }
}
