﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Ionic.Zip;
using LaminariaCore_General.utils;
using glowberry.common.interfaces;
using glowberry.common.models;
using LaminariaCore_General.common;

namespace glowberry.common.background
{
    /// <summary>
    /// Handles the backup creation for a given running server. This thread will bind itself to a running
    /// process, and frequently check for its status, stopping only when the process does.
    /// </summary>
    internal class ServerBackupHandler : IBackgroundRunner
    {
        
        /// <summary>
        /// The server Editor to use for the backups.
        /// </summary>
        private ServerEditor Editor { get; }

        /// <summary>
        /// The server section to use for the backups. (This is a property purely for convenience and clarity)
        /// </summary>
        private Section ServerSection { get; }

        /// <summary>
        /// The Process ID of the running server, used to check if it still running.
        /// </summary>
        private int ProcessID { get; }
        
        /// <summary>
        /// Main constructor for the ServerBackupHandler, sets the ServerSection and pid properties
        /// </summary>
        /// <param name="editor">The server editor to work with</param>
        /// <param name="pid">The process ID, for status checking purposes</param>
        public ServerBackupHandler(ServerEditor editor, int pid)
        {
            Editor = editor;
            ProcessID = pid;
            ServerSection = editor.ServerSection;
        }

        /// <summary>
        /// Runs the thread until the bound process stops.
        /// </summary>
        public void RunTask()
        {
            // Logs and starts the backup thread.
            Logging.Logger.Info($"Starting backup thread for server: '{ServerSection.SimpleName}'");
            ServerInformation info = Editor.GetServerInformation();

            // Loads the settings from the server section.
            bool serverBackupsEnabled = Editor.ServerSettingsContain("serverbackupson") && Editor.GetFromBuffers<bool>("serverbackupson");
            bool playerdataBackupsEnabled = Editor.ServerSettingsContain("playerdatabackupson") && Editor.GetFromBuffers<bool>("playerdatabackupson");

            // If neither of the backups are activated, stop the thread to save resources.
            if (!playerdataBackupsEnabled && !serverBackupsEnabled) return;

            // Get the server and playerdata backups paths, and create them if they don't exist.
            string serverBackupsPath = PathUtils.NormalizePath(info.ServerBackupsPath);
            string playerdataBackupsPath = PathUtils.NormalizePath(info.PlayerdataBackupsPath);

            // Creates initial backups regardless of the current time.
            if (playerdataBackupsEnabled) CreatePlayerdataBackup(playerdataBackupsPath, ServerSection);
            if (serverBackupsEnabled) CreateServerBackup(serverBackupsPath, ServerSection);

            // Gets the delays for both backups
            int serverBackupDelay = Editor.GetFromBuffers<int>("serverbackupsdelay");
            int playerdataBackupDelay = Editor.GetFromBuffers<int>("playerdatasbackupdelay");
            
            // Calculates the time for both the next backups
            DateTime nextServerBackup = DateTime.Now.AddMinutes(serverBackupDelay);
            DateTime nextPlayerdataBackup = DateTime.Now.AddMinutes(playerdataBackupDelay);
            
            // Until the process is no longer active, keep creating backups.
            while (ProcessUtils.GetProcessById(ProcessID)?.ProcessName is "java" or "cmd")
            {
                DateTime now = DateTime.Now;

                // Creates a server backup if the current hour is divisible by 2 (every 2 hours)
                if (serverBackupsEnabled && now >= nextServerBackup)
                    CreateServerBackup(serverBackupsPath, ServerSection);

                // Creates a playerdata backup if the current min is divisible by 5 (every 5 minutes)
                if (playerdataBackupsEnabled && now >= nextPlayerdataBackup)
                    CreatePlayerdataBackup(playerdataBackupsPath, ServerSection);
                
                Thread.Sleep(1 * 1000 * 60); // Sleeps for a minute
            }
        }

        /// <summary>
        /// Creates a server backup by zipping the entirety of the server section into the
        /// specified server backups path.
        /// </summary>
        /// <param name="backupsPath">The server backups path</param>
        /// <param name="serverSection">The server section to use</param>
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private void CreateServerBackup(string backupsPath, Section serverSection)
        {
            try
            {
                // Creates the backup name and the backups folder if they don't exist.
                string backupName = DateTime.Now.ToString("yyyy-MM-dd.HH.mm.ss") + ".zip";
                if (!Directory.Exists(backupsPath)) Directory.CreateDirectory(backupsPath);
                
                // Checks the rolling backups setting and deletes the oldest backup if necessary.
                if (Editor.GetFromBuffers<int>("rollingserverbackups") > 0)
                    HandleRollingBackups(backupsPath, Editor.GetFromBuffers<int>("rollingserverbackups"));

                // Zips the server section into the backups folder.
                ZipDirectory(serverSection.SectionFullPath, Path.Combine(backupsPath, backupName));
                Logging.Logger.Info($"Backed up '{serverSection.SimpleName}' Server to {backupsPath}");
                
                CleanBackups(backupsPath);
            }
            catch (Exception e)
            {
                Logging.Logger.Info("Failed to create server backup for server: '" + serverSection.SimpleName + "'");
                Logging.Logger.Error(e);
            }
        }

        /// <summary>
        /// Creates a playerdata backup by zipping the world/playerdata files into the specified
        /// playerdata backups path.
        /// </summary>
        /// <param name="backupsPath">The playerdata backups path</param>
        /// <param name="serverSection">The server section to use</param>
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private void CreatePlayerdataBackup(string backupsPath, Section serverSection)
        {
            try
            {
                string backupName = DateTime.Now.ToString("yyyy-MM-dd.HH.mm.ss") + ".zip";
                if (!Directory.Exists(backupsPath)) Directory.CreateDirectory(backupsPath);

                // Gets all the 'playerdata' folders in the server, excluding the backups folder.
                List<Section> filteredSections = serverSection.GetSectionsNamed("playerdata")
                    .Where(section => section.SectionFullPath != backupsPath)
                    .ToList();

                // Creates a playerdata backup for every world in the server.
                foreach (Section section in filteredSections)
                {
                    string backupFileName = $"{Path.GetFileName(Path.GetDirectoryName(section.SectionFullPath))}-{backupName}";
                    string backupFilePath = Path.Combine(backupsPath, backupFileName);
                    
                    // Checks the rolling backups setting and deletes the oldest backup if necessary.
                    if (Editor.GetFromBuffers<int>("rollingplayerdatabackups") > 0)
                        HandleRollingBackups(backupsPath, Editor.GetFromBuffers<int>("rollingplayerdatabackups"));

                    ZipDirectory(section.SectionFullPath, backupFilePath);
                    Logging.Logger.Info($"Backed up '{serverSection.SimpleName}.{section.SimpleName}' Playerdata to {backupsPath}");
                }
                
                CleanBackups(backupsPath);
            }
            catch (Exception e)
            {
                Logging.Logger.Error($"Failed to create playerdata backup for server: {serverSection.Name}");
                Logging.Logger.Error(e);
            }
        }

        /// <summary>
        /// Handles the rolling backups setting, deleting the oldest backups if the current amount
        /// of backups is greater than the rolling backups value, making space for the new backups.
        /// </summary>
        /// <param name="backupsPath">Either the playerdata or server backups path</param>
        /// <param name="rollingBackupsValue">The amount of backups to keep</param>
        private void HandleRollingBackups(string backupsPath, int rollingBackupsValue)
        {
            // Gets the current amount of backups in the backups folder.
            int currentBackupCount = Directory.GetFiles(backupsPath, "*.*", SearchOption.AllDirectories)
                .Count(file => file.EndsWith(".zip"));
                    
            // Iterates x times starting on the amount of backups existent (+1 to account for the one that will be created)
            // deleting any excess backups, one at a time from the oldest to newest.
            // Decrements i each iteration to reflect the changes.
            for (int i = currentBackupCount + 1; i > rollingBackupsValue; i--)
                DeleteOldestBackup(backupsPath);
        }
        
        /// <summary>
        /// Deletes the oldest backup in the backups folder.
        /// </summary>
        /// <param name="backupsPath">Either the playerdata or server backups path</param>
        private void DeleteOldestBackup(string backupsPath)
        {
            // Gets all the .zip files in the backups folder
            string[] files = Directory.GetFiles(backupsPath, "*.*", SearchOption.AllDirectories)
                .Where(file => file.EndsWith(".zip")).ToArray();
            
            if (files.Length == 0) return;
            
            // Deletes the oldest file in the backups folder
            try
            {
                string oldestFile = files.OrderBy(file => new FileInfo(file).CreationTime).First();
                File.Delete(oldestFile);
                Logging.Logger.Info($"Deleted {oldestFile} from the {backupsPath}.");
            }
            catch (IOException e)
            {
                Logging.Logger.Error("Failed to delete the oldest backup in the backups folder.");
                Logging.Logger.Error(e);
            }
        }

        /// <summary>
        /// Zips an entire directory into another location.
        /// </summary>
        /// <param name="directory">The directory to use for zipping</param>
        /// <param name="destination">The destination, including the final filename for the zip.</param>
        private void ZipDirectory(string directory, string destination)
        {
            // Creates and opens the zipping file, using a resource manager
            using ZipFile zipper = new(destination);
            zipper.ZipErrorAction = ZipErrorAction.Skip; // Skips any errors that may occur during the zipping process.

            // Adds every file into the zip file, parsing their path to exclude the root directory path.
            foreach (string file in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(directory.Length + 1);
                if (relativePath.Contains("backups") || relativePath.Contains(".lock")) continue;
                zipper.AddFile(file, Path.GetDirectoryName(relativePath));
            }

            zipper.Save();
        }

        /// <summary>
        /// Cleans every file that isn't .zip in the backups folder.
        /// </summary>
        /// <param name="backupsPath">The backups file path to use</param>
        private void CleanBackups(string backupsPath)
        {
            try
            {
                // Gets every file in the backups folder, excluding .zip files.
                string[] files = Directory.GetFiles(backupsPath, "*.*", SearchOption.AllDirectories)
                    .Where(file => file.EndsWith(".tmp")).ToArray();

                // Deletes every file in the array.
                foreach (string file in files) File.Delete(file);
            }
            
            // If the cleanup fails, log the error.
            catch (Exception e)
            {
                Logging.Logger.Error("Failed to clean the backups folder.");
                Logging.Logger.Error(e);
            }
        }
    }
}