using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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
        // null - If path name is too short or it contains invalid characters.
        //
        // Notes:
        // 1. Assumes Windows root device naming convention, "<Letter>:\\"
        // 2. GetFullPath will throw an exception if pathName contains illegal stuff like redirects/pipes/extra colons, etc.
        //
        public static string TryGetRootDevice(string pathName)
        {
            if (pathName.Length > 2 && Regex.IsMatch(pathName.Substring(0, 3), @"[a-zA-Z]:\\")) // Note 1.
            {
                string device = pathName.Substring(0, 3).ToUpper();

                if (DriveInfo.GetDrives().Any(d => d.Name == device))
                {
                    try
                    {
                        Path.GetFullPath(pathName); // Note 2.
                        return device;
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static async Task<string> TryGetRootDeviceAsync(string pathName)
        {
            return await Task.Run(() => TryGetRootDevice(pathName));
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

        public static bool DeleteFile(string fileName, int maxRetries = 0)
        {
            if (File.Exists(fileName))
            {
                bool success = false;
                int maxTries = maxRetries >= 0 ? 1 + maxRetries : 1;
                for (int tries = 0; !success && tries < maxTries; tries++)
                {
                    try
                    {
                        File.SetAttributes(fileName, FileAttributes.Normal);
                        File.Delete(fileName);
                        success = true;
                    }
                    catch
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            return true;
        }

        // Recursive delete which can handle read only files, unlike Directory.Delete
        private static void SetAttributesAndDeleteDirectory(string directoryName)
        {
            File.SetAttributes(directoryName, FileAttributes.Normal);
            foreach (var file in Directory.GetFiles(directoryName))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            } 
            foreach (var dir in Directory.GetDirectories(directoryName))
            {
                SetAttributesAndDeleteDirectory(dir);
            }
            Directory.Delete(directoryName, recursive: false);
        }

        public static bool DeleteDirectory(string directoryName, int maxRetries = 0)
        {
            if (Directory.Exists(directoryName))
            {
                bool success = false;
                int maxTries = maxRetries >= 0 ? 1 + maxRetries : 1;
                for(int tries = 0; !success && tries < maxTries; tries++) 
                {
                    try
                    {
                        Directory.Delete(directoryName, recursive: true);
                        success = true;
                    }
                    catch
                    {
                        try
                        {
                            SetAttributesAndDeleteDirectory(directoryName);
                            success = true;
                        }
                        catch 
                        { 
                            Thread.Sleep(5);
                        }
                    }
                } 
                return success;
            }
            return true;
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

        public static bool IsOnNetworkDrive(string directoryName)
        {
            var rootDev = TryGetRootDevice(directoryName);
            if (rootDev != null)
            {
                try
                {
                    var driveInfo = new DriveInfo(rootDev);
                    return driveInfo != null && driveInfo.DriveType == DriveType.Network;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }
}
