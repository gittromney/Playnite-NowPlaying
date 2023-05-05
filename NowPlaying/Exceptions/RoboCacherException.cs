using System;

namespace NowPlaying.Exceptions
{
    public class RoboCacherException : Exception
    {
        public bool DiskFull;

        public RoboCacherException(bool diskFull = false)
        {
            DiskFull = diskFull;
        }

        public RoboCacherException(string message, bool diskFull = false) : base(message)
        {
            DiskFull = diskFull;
        }

        public RoboCacherException(string message, Exception innerException, bool diskFull = false) : base(message, innerException)
        {
            DiskFull = diskFull;
        }

    }
}