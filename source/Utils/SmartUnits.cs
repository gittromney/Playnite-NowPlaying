using System;

namespace NowPlaying.Utils
{
    public static class SmartUnits
    {
        public static string Duration(TimeSpan? ts)
        {
            string duration;
            if      (ts == null)              { duration = "0s"; }
            else if (ts?.TotalDays < 0)       { duration = "∞"; }  // overload: negative => infinite duration
            else if (ts?.TotalDays > 1.0)     { duration = string.Format("{0}d:{1}h",    ts?.Days, ts?.Hours); }
            else if (ts?.TotalHours > 1.0)    { duration = string.Format("{0}h:{1:d2}m", ts?.Hours, ts?.Minutes); }
            else if (ts?.TotalMinutes > 1.0)  { duration = string.Format("{0}m:{1:d2}s", ts?.Minutes, ts?.Seconds); }
            else if (ts?.TotalSeconds > 10.0) { duration = string.Format("{0:d2}s",      ts?.Seconds); }
            else if (ts?.TotalSeconds > 1.0)  { duration = string.Format("{0:d1}s",      ts?.Seconds); }
            else                              { duration = string.Format("{0:n1}s",      ts?.TotalSeconds); }
            return duration;
        }

        // . determine smart unit scaling
        public static int GetBytesAutoScale(long? bytes)
        {
            int unitScale = 0;
            while ((bytes >= 512 || bytes <= -512) && (unitScale < 5))
            {
                bytes >>= 10; // bytes /= 1024
                unitScale++;
            }
            return unitScale;
        }

        /// <summary>
        /// Converts a value in bytes into a 'smart scaled' string in units of PB/TB/GB/MB/KB/B.
        /// The units are chosen by finding a scaling value (0-5) such that 
        /// '<value in bytes>/1024^(<scaling value>)' is between 0.5 and 512.0, if possible. 
        /// The user can optionally override the scaling value with the 'userScale' parameter 
        /// to select specific unit to convert into, and can also override defaults for
        /// decimal places and a switch for showing/hiding units in the result.
        /// 
        /// Defaults:
        ///    decimals = 3
        ///    showUnits = true
        ///    userScale = -1   [ (<0) autoscale, (0-5) apply user unit scaling ]
        ///    
        /// </summary>
        public static string Bytes(long? bytes, int decimals=2, bool showUnits=true, int userScale=-1)
        {
            int scale = userScale < 0 ? GetBytesAutoScale(bytes) : userScale;
            string digits = "";
            string units = "";

            // . apply scaling to our value-in-bytes
            if (scale > 0)
            {
                double smartValue = (double) bytes;

                for (int i = 0; i < scale; i++)
                {
                    smartValue /= 1024.0;
                }

                digits = smartValue.ToString($"n{decimals}");
            }
            else
            {
                digits = bytes?.ToString();
            }

            // . fill units string, if applicable
            if (showUnits)
            {
                switch (scale)
                {
                    case 0: units = " B"; break;   // in Bytes
                    case 1: units = " KB"; break;  // in Kilobytes
                    case 2: units = " MB"; break;  // in Megabytes
                    case 3: units = " GB"; break;  // in Gigabytes
                    case 4: units = " TB"; break;  // in Terabytes
                    case 5: units = " PB"; break;  // in Petabytes
                }
            }
            return digits + units;
        }

        /// <summary>
        /// Returns formatted string of the form: "[bytes] of [ofBytes] [units]", e,g, "3.14 of 42.42 GB"
        /// Note, bytes value is displayed as "0 of ..." if bytes is exactly 0.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="ofBytes"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        public static string BytesOfBytes(long bytes, long ofBytes, int decimals=2)
        {
            int scale = GetBytesAutoScale(ofBytes);
            string bytesStr = bytes == 0 ? "0" : Bytes(bytes, userScale: scale, decimals: decimals, showUnits: false);
            string ofBytesStr = Bytes(ofBytes, userScale: scale, decimals: decimals);
            return $"{bytesStr} of {ofBytesStr}";
        }
    }
}
