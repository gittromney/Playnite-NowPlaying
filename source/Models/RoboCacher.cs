using NowPlaying.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Playnite.SDK;

namespace NowPlaying.Models
{
    public class RoboCacher
    {
        private readonly ILogger logger;

        // . callbacks for real-time stats updates and job done
        public event EventHandler<string> eStatsUpdated;
        public event EventHandler<GameCacheJob> eJobDone;
        public event EventHandler<GameCacheJob> eJobCancelled;
        
        private List<Process> activeBackgroundProcesses;

        public RoboCacher(ILogger logger)
        {
            this.logger = logger;
            activeBackgroundProcesses = new List<Process>();
        }

        public void Shutdown()
        {
            foreach (var process in activeBackgroundProcesses)
            {
                process.Kill();
                process.Dispose();
            }
        }

        /// <summary>
        /// Robocopy.exe process start information
        /// </summary>
        /// <param name="srcDir">Source directory path name</param>
        /// <param name="destDir">Destination directory path name</param>
        /// <param name="listOnly">If true, run in list only mode - don't do any copying</param>
        /// <param name="showClass">If true, report file classifiers: extra/new/older/newer</param>
        /// <param name="interPacketGap">Optional inter packet gap value, to limit network bandwidth used</param>
        /// <returns></returns>
        private ProcessStartInfo RoboStartInfo(string srcDir, string destDir, bool listOnly=false, bool showClass=false, int interPacketGap=0)
        {
            string roboArgs = string.Format("\"{0}\" \"{1}\" {2}{3}{4}{5}",
                DirectoryUtils.TrimEndingSlash(srcDir), 
                DirectoryUtils.TrimEndingSlash(destDir),
                "/E /NDL /BYTES /NJH /NJS",
                showClass ? "" : " /NC",
                listOnly ? " /L" : "",
                interPacketGap > 0 ? $" /IPG:{interPacketGap}" : ""
            );
            return new ProcessStartInfo
            {
                FileName = "robocopy.exe",
                Arguments = roboArgs,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false
            };
        }

        public class DiffResult
        {
            public List<string> NewFiles;
            public List<string> ExtraFiles;
            public List<string> NewerFiles;
            public List<string> OlderFiles;
            public int TotalDiffCount;
            public DiffResult()
            {
                NewFiles = new List<string>();
                ExtraFiles = new List<string>();
                NewerFiles = new List<string>();
                OlderFiles = new List<string>();
                TotalDiffCount = 0;
            }
        }

        private void RoboError(string msg)
        {
            logger.Error(msg); 
            throw new InvalidOperationException(msg);
        }

        /// <summary>
        /// Runs robocopy.exe in "analysis mode" to compare source and destination directories.
        /// This is a blocking task that waits for robocopy.exe to finish (or fail). 
        /// </summary>
        /// <param name="srcDir">Source directory</param>
        /// <param name="destDir">Destination directory</param>
        /// <returns>Returns lists of extra/new/newer/older file names, wrapped in a DiffResult class</returns>
        /// <exception cref="RoboCacherException"></exception>
        public DiffResult RoboDiff(string srcDir, string destDir)
        {
            DiffResult diff = new DiffResult();
            ProcessStartInfo rcPsi = RoboStartInfo(srcDir, destDir, listOnly: true, showClass: true);
            logger.Debug($"Starting robocopy.exe w/args '{rcPsi.Arguments}'...");
            Process foregroundProcess = Process.Start(rcPsi);

            string line;
            while ((line = foregroundProcess.StandardOutput.ReadLine()) != null)
            {
                switch (RoboParser.GetAnalysisLineType(line))
                {
                    case RoboParser.AnalysisLineType.Empty: continue;
                    case RoboParser.AnalysisLineType.MarkerFile: continue;
                    case RoboParser.AnalysisLineType.ExtraFile: diff.ExtraFiles.Add(RoboParser.GetExtraFileName(line)); break;
                    case RoboParser.AnalysisLineType.NewFile: diff.NewFiles.Add(RoboParser.GetNewFileName(line)); break;
                    case RoboParser.AnalysisLineType.Newer: diff.NewerFiles.Add(RoboParser.GetNewerFileName(line)); break;
                    case RoboParser.AnalysisLineType.Older: diff.OlderFiles.Add(RoboParser.GetOlderFileName(line)); break;
                    default:
                        // . kill robocopy.exe
                        foregroundProcess.Kill();
                        foregroundProcess.Dispose();
                        RoboError($"Unexpected robocopy.exe output line: '{line}'");
                        break;
                }
                Console.WriteLine(line);
            }

            // . After STDOUT has been emptied, check robocopy.exe exit code
            foregroundProcess.WaitForExit();
            int exitCode = foregroundProcess.ExitCode;
            foregroundProcess.Dispose();

            if (exitCode > 3)  // exit code 0-3 == successful analysis run
            {
                RoboError("Diff of cache folder contents vs install directory failed.");
            }

            diff.TotalDiffCount += diff.ExtraFiles.Count + diff.NewFiles.Count;
            diff.TotalDiffCount += diff.NewerFiles.Count + diff.OlderFiles.Count;

            return diff;
        }

        public void AnalyzeCacheContents(GameCacheEntry entry)
        {
            string cacheDir = entry.CacheDir;
            string installDir = entry.InstallDir;

            try
            {
                if (!Directory.Exists(cacheDir))
                {
                    entry.State = GameCacheState.Empty;
                    entry.CacheSize = 0;
                }
                
                else if (DirectoryUtils.IsEmptyDirectory(cacheDir))
                {
                    entry.State = GameCacheState.Empty;
                    entry.CacheSize = 0;
                }

                else
                {
                    // Non-empty directory:
                    //
                    // . Use Robocopy.exe to compare install vs cache folders.
                    // . Expectations:
                    //    Populated : contents match (no new/newer/older/extra files - possible state marker file excluded)
                    //    InProgress : contents incomplete (no newer/extra files - possible state marker file excluded) 
                    //    Invalid : contents are not well correleated (1+ older/extra files)
                    //
                    DiffResult diff = RoboDiff(installDir, cacheDir);

                    if (diff.ExtraFiles.Count == 0 && diff.NewerFiles.Count == 0)
                    {
                        if (diff.NewFiles.Count == 0 && diff.OlderFiles.Count == 0)
                        {
                            // game cache is equivalent to install folder
                            entry.State = GameCacheState.Populated;
                        }
                        else
                        {
                            // game cache requires copying additional files from the install folder
                            entry.State = GameCacheState.InProgress;
                            entry.UpdateCacheDirStats();
                        }
                    }
                    else
                    {
                        // game cache is Invalid - not well correlated to the install folder 
                        entry.State = GameCacheState.Invalid;
                    }

                    // . update directory state marker + cache size
                    MarkCacheDirectoryState(cacheDir, entry.State);
                }
            }
            catch (Exception ex)
            {
                // . diff results not expected: cache state is Invalid
                entry.State = GameCacheState.Invalid;
                MarkCacheDirectoryState(cacheDir, entry.State);
                RoboError($"Analysis of cache folder contents failed: {ex.Message}");
            }
        }

        public void CheckAvailableSpaceForCache(string cacheDir, long spaceRequired)
        {
            long freeSpace = DirectoryUtils.GetAvailableFreeSpace(cacheDir);
            if (freeSpace < spaceRequired)
            {
                RoboError(string.Format("Not enough space in {0} for game cache. Needs {1}, has {2}",
                    cacheDir, SmartUnits.Bytes(spaceRequired), SmartUnits.Bytes(freeSpace)
                ));
            }
        }

        public void StartCacheResumePopulateJob(GameCacheJob job)
        {
            PrepareForResumePopulateJob(job);
            
            if (job.token.IsCancellationRequested)
            {
                job.token.ThrowIfCancellationRequested();
            }
            else
            {
                StartCachePopulateJob(job);
            }
        }

        private void PrepareForResumePopulateJob(GameCacheJob job)
        {
            GameCacheEntry entry = job.entry;
            RoboStats stats = job.stats;
            string cacheDir = entry.CacheDir;
            string installDir = entry.InstallDir;

            // The Game Cache is only partially populated.
            // 1. Run robocopy.exe in list only mode to see what's missing from the Game Cache.
            // 2. Resume Populating the Game Cache.
            //
            ProcessStartInfo rcPsi = RoboStartInfo(installDir, cacheDir, listOnly: true);
            logger.Debug($"Started robocopy.exe with arguments '{rcPsi.Arguments}'");
            List<string> stdout = new List<string> { $"robocopy.exe {rcPsi.Arguments}" };
            Process foregroundProcess = Process.Start(rcPsi);

            // Initiallize counts assuming all files have been copied to the Game Cache,
            //  then subtract away what hasn't been.
            //
            stats.BytesCopied = stats.BytesToCopy;
            stats.FilesCopied = stats.FilesToCopy;

            string line;
            while (!job.token.IsCancellationRequested && !job.cancelledOnError && (line = foregroundProcess.StandardOutput.ReadLine()) != null)
            {
                switch (RoboParser.GetLineType(line))
                {
                    case RoboParser.LineType.MarkerFile: continue;
                    case RoboParser.LineType.Empty: continue;
                    case RoboParser.LineType.SizeName:
                        stats.BytesCopied -= RoboParser.GetFileSizeOnly(line);
                        stats.FilesCopied--;
                        stdout.Add(line);
                        break;

                    default:
                        job.cancelledOnError = true;
                        job.errorLog = stdout;
                        job.errorLog.Add(line);
                        break;
                }
            }

            // cancellation requested or due to msg condition
            if (job.token.IsCancellationRequested || job.cancelledOnError)
            {
                // . kill robocopy.exe
                foregroundProcess.Kill();
                foregroundProcess.Dispose();

                eJobCancelled?.Invoke(this, job);
            }
            else
            {
                // . After STDOUT has been emptied, check robocopy.exe exit code
                foregroundProcess.WaitForExit();
                int exitCode = foregroundProcess.ExitCode;
                foregroundProcess.Dispose();

                if (exitCode > 3)  // exit code 0-3 == successful list only run
                {
                    job.cancelledOnError = true;
                    job.errorLog = stdout;
                    job.errorLog.Add($"robocopy.exe exited with exit code {exitCode}");
                    foreach (var err in job.errorLog)
                    {
                        logger.Error(err);
                    }

                    // notify of job cancelled
                    eJobCancelled?.Invoke(this, job);
                }
                else
                {
                    // . store how many bytes have already been copied (for transfer speed)
                    stats.ResumeBytes = stats.BytesCopied;

                    // . set Resume's initial progress done percentage...
                    stats.PercentDone = (100.0 * stats.BytesCopied) / stats.BytesToCopy;
                }
            }
        }

        public void StartCachePopulateJob(GameCacheJob job)
        {
            GameCacheEntry entry = job.entry;
            string cacheDir = entry.CacheDir;
            string installDir = entry.InstallDir;
            int interPacketGap = job.interPacketGap; 
        
            // . make sure there's room for the Game Cache on disk...
            CheckAvailableSpaceForCache(cacheDir, entry.InstallSize - entry.CacheSize);

            // . run robocopy.exe as a background process...
            ProcessStartInfo rcPsi = RoboStartInfo(installDir, cacheDir, interPacketGap: interPacketGap);
            logger.Debug($"Starting robocopy.exe w/args '{rcPsi.Arguments}'...");
            try
            {
                Process rcProcess = Process.Start((rcPsi));

                // . register active background process
                activeBackgroundProcesses.Add(rcProcess);

                // . (background task) monitor robocopy output/exit code and update job stats...
                Task.Run(() => MonitorCachePopulateJob(rcProcess, job));
            }
            catch (Exception ex)
            {
                RoboError(ex.Message);
            }
        }

        private void MonitorCachePopulateJob(Process rcProcess, GameCacheJob job)
        {
            List<string> stdout = new List<string> { $"robocopy.exe {rcProcess.StartInfo.Arguments}" };
            GameCacheEntry entry = job.entry;
            RoboStats stats = job.stats;
            string cacheDir = entry.CacheDir;
            string installDir = entry.InstallDir;
            string fileSizeLine = string.Empty;
            bool firstLine = true;

            // while job is in progress: grab robocopy.exe output lines, one at a time...
            string line;
            while (!job.token.IsCancellationRequested && !job.cancelledOnDiskFull && !job.cancelledOnError && (line = rcProcess.StandardOutput.ReadLine()) != null)
            {
                // On 1st output line, mark cache folder state as InProgress state
                if (firstLine)
                {
                    MarkCacheDirectoryState(cacheDir, GameCacheState.InProgress);
                    entry.State = GameCacheState.InProgress;
                    firstLine = false;
                }
                switch (RoboParser.GetLineType(line))
                {
                    case RoboParser.LineType.Empty: continue;
                    case RoboParser.LineType.MarkerFile: continue;

                    // We get a new cacheInstalledSize/path line...
                    // 1. If we never saw 100% progress on the previous file, assume it completed.
                    //    -> Update file/byte counters, accordingly.
                    // 2. Grab the new file's cacheInstalledSize and path name
                    //
                    case RoboParser.LineType.SizeName:
                        if (stats.CurrFilePct > 0.0)
                        {
                            stats.FilesCopied++;
                            stats.BytesCopied += stats.CurrFileSize;
                            stats.CurrFileSize = 0;
                            stats.CurrFilePct = 0.0;
                            stdout.Add(fileSizeLine);
                        }
                        fileSizeLine = line;
                        (stats.CurrFileSize, stats.CurrFileName) = RoboParser.GetFileSizeName(line);
                        break;

                    // We got 100% file progress... 
                    // 1. Update file/byte counters.
                    // 2. Clear current file cacheInstalledSize in case duplicate 100% line(s) follow it
                    //
                    case RoboParser.LineType.Prog100:
                        if (stats.CurrFileSize > 0)
                        {
                            stats.FilesCopied++;
                            stats.BytesCopied += stats.CurrFileSize;
                            stats.CurrFileSize = 0;
                            stats.CurrFilePct = 0.0;
                            stdout.Add(fileSizeLine);
                        }
                        break;

                    // We got a (non-100)% progress line... 
                    // . update current file percentage
                    //
                    case RoboParser.LineType.Progress:
                        stats.CurrFilePct = RoboParser.GetFilePercentDone(line);
                        break;

                    case RoboParser.LineType.DiskFull:
                        job.cancelledOnDiskFull = true;
                        stdout.Add(line);
                        break;
                        
                    // We got an unexpected line... return msg log
                    default:
                        job.cancelledOnError = true;
                        job.errorLog = stdout;
                        job.errorLog.Add(line); 
                        
                        entry.State = GameCacheState.Unknown;
                        MarkCacheDirectoryState(cacheDir, entry.State);
                        break;
                }

                // . update cache cacheInstalledSize for game cache view model, if applicable
                entry.CacheSize = stats.GetTotalBytesCopied();

                // . notify interested parties (e.g. UI) that real-time stats have been updated...
                eStatsUpdated?.Invoke(this, entry.Id);
            }

            // cancellation requested or due to disk full/msg condition
            if (job.token.IsCancellationRequested || job.cancelledOnDiskFull || job.cancelledOnError)
            {
                // . kill robocopy.exe
                rcProcess.Kill();
                rcProcess.Dispose();
                activeBackgroundProcesses.Remove(rcProcess);

                // . update InProgress cache stats
                entry.UpdateCacheDirStats();

                // notify of job cancelled
                eJobCancelled?.Invoke(this, job);
            }
            else
            {
                // Grab robocopy.exe exit code, dispose of process, etc
                rcProcess.WaitForExit();
                int exitCode = rcProcess.ExitCode;
                rcProcess.Dispose();
                activeBackgroundProcesses.Remove(rcProcess);

                // robocopy.exe exit codes 0-3 are successful in copy mode 
                if (exitCode > 3)
                {
                    job.cancelledOnError = true;
                    job.errorLog = stdout;
                    job.errorLog.Add($"robocopy.exe exited with exit code {exitCode}");
                    foreach (var err in job.errorLog)
                    {
                        logger.Error(err);
                    }

                    entry.State = GameCacheState.Unknown;
                    MarkCacheDirectoryState(cacheDir, entry.State);

                    // notify of job cancelled
                    eJobCancelled?.Invoke(this, job);
                }
                else
                {
                    // update real-time stats to reflect 100% completion...
                    stats.PercentDone = 100.0;
                    stats.BytesCopied = stats.BytesToCopy;
                    stats.FilesCopied = stats.FilesToCopy;
                    stats.CurrFileName = string.Empty;
                    stats.CurrFileSize = 0;
                    stats.CurrFilePct = 0.0;

                    // update cache entry/directory state + cache directory stats
                    entry.State = GameCacheState.Populated;
                    entry.CacheSize = stats.BytesToCopy;
                    MarkCacheDirectoryState(cacheDir, entry.State);
                    entry.UpdateCacheDirStats();

                    // notify of job completion
                    eJobDone?.Invoke(this, job);
                }
            }
        }

        public void EvictGameCacheJob(GameCacheJob job)
        {
            GameCacheEntry entry = job.entry;
            string cacheDir = entry.CacheDir;
            string installDir = entry.InstallDir;
            bool cacheIsDirty = false;

            // . check for dirty game cache
            if (entry.State == GameCacheState.Played)
            {
                try
                {
                    DiffResult diff = RoboDiff(cacheDir, installDir);
                    cacheIsDirty = diff.NewFiles.Count > 0 || diff.NewerFiles.Count > 0;
                }
                catch (Exception ex)
                {
                    job.cancelledOnError = true;
                    job.errorLog = new List<string> { $"Error diffing cache vs install dir for '{job.entry.Title}': {ex.Message}" };
                    foreach (var err in job.errorLog)
                    {
                        logger.Error(err);
                    }
                    eJobCancelled?.Invoke(this, job);
                    return;
                }
            }

            if (cacheIsDirty)
            {
                // . write-back any differences to the install dir
                //   -> Remove cache state marker file, so it won't get written back
                //   -> Rely on robocopy to report an msg ("Other" LineType) if there are any issues,
                //      such as insufficient disk space, etc.
                //
                ClearCacheDirectoryStateMarker(cacheDir);
                ProcessStartInfo rcPsi = RoboStartInfo(cacheDir, installDir);
                List<string> stdout = new List<string> { $"robocopy.exe {rcPsi.Arguments}" };
                Process foregroundProcess = Process.Start(rcPsi);

                string line;
                while (!job.cancelledOnError && (line = foregroundProcess.StandardOutput.ReadLine()) != null)
                {
                    switch (RoboParser.GetLineType(line))
                    {
                        case RoboParser.LineType.SizeName: stdout.Add(line); break;
                        case RoboParser.LineType.DiskFull:
                            job.cancelledOnError = true;
                            job.errorLog = stdout;
                            job.errorLog.Add($"robocopy.exe terminated because disk was full");
                            foreach (var err in job.errorLog)
                            {
                                logger.Error(err);
                            }
                            break;

                        case RoboParser.LineType.Other:
                            job.cancelledOnError = true;
                            job.errorLog = stdout;
                            job.errorLog.Add($"Unexpected robocopy.exe output line: '{line}'");
                            foreach (var err in job.errorLog)
                            {
                                logger.Error(err);
                            }
                            break;

                        default: continue;
                    }
                }

                if (job.cancelledOnError)
                {
                    // . kill robocopy.exe
                    foregroundProcess.Kill();
                    foregroundProcess.Dispose();

                    // . not sure how to recover... just restore the state marker
                    MarkCacheDirectoryState(cacheDir, entry.State);

                    eJobCancelled?.Invoke(this, job);
                    return;
                }
                else
                {
                    // Grab robocopy.exe exit code, dispose of process, etc
                    foregroundProcess.WaitForExit();
                    int exitCode = foregroundProcess.ExitCode;
                    foregroundProcess.Dispose();

                    // robocopy.exe exit codes 0-3 are successful in copy mode 
                    if (exitCode > 3)
                    {
                        // . not sure how to recover... just restore the state marker
                        MarkCacheDirectoryState(cacheDir, entry.State);

                        job.cancelledOnError = true;
                        job.errorLog = new List<string> { $"Unexpected robocopy.exe exit code: {exitCode}" };
                        foreach (var err in job.errorLog)
                        {
                            logger.Error(err);
                        }
                        eJobCancelled?.Invoke(this, job);
                        return;
                    }
                    else
                    {
                        // . Installation directory status have potentially changed
                        entry.UpdateInstallDirStats();
                    }
                }
            }

            // Delete the game cache
            try
            {
                // . delete the Game Cache folder
                if (Directory.Exists(cacheDir))
                {
                    DirectoryUtils.DeleteDirectory(entry.CacheDir);
                }
                entry.State = GameCacheState.Empty;
                entry.CacheSize = 0;
            }
            catch (Exception ex)
            {
                // . Game cache folder may be partially deleted ?
                entry.State = GameCacheState.Unknown;
                entry.UpdateCacheDirStats();

                job.cancelledOnError = true;
                job.errorLog = new List<string> { $"Deleting cache directory failed: {ex.Message}" };
                foreach (var err in job.errorLog)
                {
                    logger.Error(err);
                }
                eJobCancelled?.Invoke(this, job);
                return;
            }

            eJobDone?.Invoke(this, job);
        }

        public void ClearCacheDirectoryStateMarker(string cacheDir)
        {
            GameCacheState[] states = {
                GameCacheState.Played,
                GameCacheState.Populated,
                GameCacheState.InProgress,
                GameCacheState.Invalid
            };
            foreach (GameCacheState s in states)
            {
                string markerFile = Path.Combine(cacheDir, $@".NowPlaying.{s}");
                if (File.Exists(markerFile))
                {
                    File.Delete(markerFile);
                }
            }
        }

        // Note: Empty|Unknown states do not leave a marker file
        public void MarkCacheDirectoryState(string cacheDir, GameCacheState cacheState)
        {
            ClearCacheDirectoryStateMarker(cacheDir);
            switch (cacheState)
            {
                case GameCacheState.Played:
                case GameCacheState.Populated:
                case GameCacheState.InProgress:
                case GameCacheState.Invalid:
                    File.Create(Path.Combine(cacheDir, $@".NowPlaying.{cacheState}")).Dispose();
                    break;
            }
        }
    }

}
