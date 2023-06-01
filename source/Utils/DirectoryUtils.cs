﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Markup;

namespace NowPlaying.Utils
{
    public static class DirectoryUtils
    {
        public static string TrimEndingSlash(string path)
        {
            return path?.TrimEnd(new[] { Path.DirectorySeparatorChar });
        }

        public static bool IsEmptyDirectory(string directoryName)
        {
            DirectoryInfo di = new DirectoryInfo(directoryName);
            if (di.GetFiles().Count() > 0) return false;
            if (di.GetDirectories().Count() > 0) return false;
            return true;
        }

        public static List<string> GetSubDirectories(string directoryName)
        {
            DirectoryInfo di = new DirectoryInfo(directoryName);
            if (di.GetDirectories().Count() > 0)
            {
                return di.GetDirectories().Select(sd => sd.Name).ToList();
            }
            else
            {
                return new List<string>();
            }
        }

        public static string ToSafeFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (f, c) => f.Replace(c, '-'));
        }

        // Returns:
        // <root device name> - if path name is rooted (e.g. returns "C:\\" if path is "C:\\My\\Path\\Name"
        // null - If path name is long enough to contain a root, but it either does not or
        //        it is invalid (e.g. contains redirects or pipes)
        public static string TryGetRootDevice(string pathName)
        {
            try
            {
                string fullPath = Path.GetFullPath(pathName);  // this checks for redirects/pipes, etc.
                string device = TrimEndingSlash(Directory.GetDirectoryRoot(fullPath));
                if (device.Length > pathName.Length)
                {
                    return null;
                }
                else 
                {
                    return device.Equals(pathName.Substring(0, device.Length)) ? device : null;
                }
            }
            catch
            {
                return null;
            }
        }

        public static bool IsValidRootedDirName(string pathName)
        {
            string device = TryGetRootDevice(pathName);
            bool isValid = !string.IsNullOrEmpty(device);
            if (isValid) 
            {
                // . check each subdir, file name following the device root name for invalid chars.
                pathName = pathName.Substring(device.Length);
                foreach (string s in pathName.Split(Path.DirectorySeparatorChar))
                {
                    isValid &= s.Equals(ToSafeFileName(s));
                }
            }
            return isValid;
        }

        /// <summary>
        /// Calculates the number of directories and files found inside the named folder as well as
        /// the combined cacheInstalledSize of the files, in bytes.
        /// </summary>
        /// <param name="directoryName">RootDir directory to get stats for.</param>
        /// <returns>Increments counters via ref parameters: (# directories, # files, aggregate cacheInstalledSize in bytes)</returns>
        public static void GetDirectoryStats(string directoryName, ref long directories, ref long files, ref long size)
        {
            DirectoryInfo di = new DirectoryInfo(directoryName);

            foreach (FileInfo fi in di.GetFiles())
            {
                files++;
                size += fi.Length;
            }

            foreach (DirectoryInfo subDi in di.GetDirectories())
            {
                directories++;
                GetDirectoryStats(subDi.FullName, ref directories, ref files, ref size);
            }
            return;
        }

        public static long GetAvailableFreeSpace(string directoryName)
        {
            try
            {
                string root = Directory.GetDirectoryRoot(directoryName);
                DriveInfo dvi = new DriveInfo(root);
                return dvi.AvailableFreeSpace;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        public static long GetRootDeviceCapacity(string directoryName)
        {
            try
            {
                string root = Directory.GetDirectoryRoot(directoryName);
                DriveInfo dvi = new DriveInfo(root);
                return dvi.TotalSize;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        public static bool DeleteDirectory(string directoryName, int maxRetries = 0)
        {
            bool success = false;
            int maxTries = maxRetries >= 0 ? 1 + maxRetries : 1;
            for(int tries = 0; !success && tries < maxTries; tries++) 
            {
                try
                {
                    if (Directory.Exists(directoryName))
                    {
                        Directory.Delete(directoryName, recursive: true);
                    }
                    success = true;
                } 
                catch 
                { 
                    Thread.Sleep(1);
                }
            }
            return success;
        }

        public static bool ExistsAndIsWritable(string directoryName)
        {
            try
            {
                if (Directory.Exists(directoryName))
                {
                    string testFile = Path.Combine(directoryName, Path.GetRandomFileName() + $@".ExistsAndIsWritable");
                    File.Create(testFile, bufferSize: 1, options: FileOptions.DeleteOnClose).Dispose();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool MakeDir(string directoryName)
        {
            try
            {
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
