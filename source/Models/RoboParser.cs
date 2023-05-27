using System.Text.RegularExpressions;

namespace NowPlaying.Models
{
    public static class RoboParser
    {
        /// <summary>
        /// Robocopy.exe output line Type
        /// </summary>
        public enum LineType
        {
            /// <summary>NowPlaying cache state marker file line</summary>
            MarkerFile,
            /// <summary>File cacheInstalledSize/name line</summary>
            SizeName,
            /// <summary>% progress line</summary>
            Progress,
            /// <summary>100% progress line</summary>
            Prog100,
            /// <summary>Empty line</summary>
            Empty,
            /// <summary>Ran out of disk space</summary>
            DiskFull,
            /// <summary>Unexpected line</summary>
            Other
        }

        public enum AnalysisLineType
        {
            MarkerFile,
            ExtraFile,
            NewFile,
            Newer,
            Older,
            Empty,
            Other
        }

        // Robocopy common output parsing: regular expressions
        private static string regexMarkerFile = @"\.NowPlaying\.(\w+)\s*$";

        // Robocopy standard mode output parsing: regular expressions
        private static string regexSizeName = @"^\s*(\d+)\s+(.*)$"; // e.g. ' 123 <file path> ' -> $1=123, $2=<file path>
        private static string regexPercent = @"^\s*([\d\.]+)%.*";   // e.g. ' 12.3%' -> $1=12.3
        private static string regexDiskFull = @" ERROR 112 \(0x00000070\) ";

        public static LineType GetLineType(string line)
        {
            // examine robocopy output line...
            switch (true)
            {
                case bool _ when Regex.IsMatch(line, @"^\s*$"): return LineType.Empty;
                case bool _ when Regex.IsMatch(line, regexMarkerFile): return LineType.MarkerFile;
                case bool _ when Regex.IsMatch(line, @"^\s*100%"): return LineType.Prog100;
                case bool _ when Regex.IsMatch(line, regexSizeName): return LineType.SizeName;
                case bool _ when Regex.IsMatch(line, regexPercent): return LineType.Progress;
                case bool _ when Regex.IsMatch(line, regexDiskFull): return LineType.DiskFull;
                default: return LineType.Other;
            }
        }

        public static (long fileSize, string fileName) GetFileSizeName(string line)
        {
            string[] s = Regex.Replace(line, regexSizeName, "$1,$2").Split(',');
            return (long.Parse(s[0]), s[1]);
        }

        public static long GetFileSizeOnly(string line)
        {
            return long.Parse(Regex.Replace(line, regexSizeName, "$1"));
        }

        public static double GetFilePercentDone(string line)
        {
            return double.Parse(Regex.Replace(line, regexPercent, "$1"));
        }

        // Robocopy analysis (+file class) mode output parsing: regular expressions
        private static string regexExtraFile = @"^\s*\*EXTRA File\s+\d+\s+(.*)$"; // e.g. ' *EXTRA File  12  <file path>' -> $1=<file path>
        private static string regexNewFile = @"^\s*New File\s+\d+\s+(.*)$"; // e.g. ' New File  234  <file path>' -> $1=<file path>
        private static string regexNewerFile = @"^\s*Newer\s+\d+\s+(.*)$"; // e.g. ' Newer  56  <file path>' -> $1=<file path>
        private static string regexOlderFile = @"^\s*Older\s+\d+\s+(.*)$"; // e.g. ' Older  78  <file path>' -> $1=<file path>

        public static AnalysisLineType GetAnalysisLineType(string line)
        {
            // examine robocopy analysis (+file class) mode output line...
            switch (true)
            {
                case bool _ when Regex.IsMatch(line, @"^\s*$"): return AnalysisLineType.Empty;
                case bool _ when Regex.IsMatch(line, regexMarkerFile): return AnalysisLineType.MarkerFile;
                case bool _ when Regex.IsMatch(line, @"^\s*\*EXTRA File"): return AnalysisLineType.ExtraFile;
                case bool _ when Regex.IsMatch(line, @"^\s*New File"): return AnalysisLineType.NewFile;
                case bool _ when Regex.IsMatch(line, @"^\s*Newer"): return AnalysisLineType.Newer;
                case bool _ when Regex.IsMatch(line, @"^\s*Older"): return AnalysisLineType.Older;
                default: return AnalysisLineType.Other;
            }
        }

        public static string GetMarkerFileState(string line)
        {
            return Regex.Replace(line, regexMarkerFile, "$1");
        }
        public static string GetExtraFileName(string line)
        {
            return Regex.Replace(line, regexExtraFile, "$1");
        }
        public static string GetNewFileName(string line)
        {
            return Regex.Replace(line, regexNewFile, "$1");
        }
        public static string GetNewerFileName(string line)
        {
            return Regex.Replace(line, regexNewerFile, "$1");
        }
        public static string GetOlderFileName(string line)
        {
            return Regex.Replace(line, regexOlderFile, "$1");
        }
    }
}
