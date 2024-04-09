using System.IO;
using System.Linq;
using LaminariaCore_General.common;
using LaminariaCore_General.utils;
using Microsoft.Win32;
using static glowberry.common.configuration.Constants;


namespace glowberry.utils
{
    /// <summary>
    /// This class contains methods to interact with the Windows Registry.
    /// </summary>
    public static class RegistryUtils
    {
        /// <summary>
        /// The name of the section that contains the scripts for the server.
        /// </summary>
        private static string ScriptsSectionName = "gb-scripts";
        
        /// <summary>
        /// The name of the boot script that will be added to the Windows Registry.
        /// </summary>
        private static string BootScriptName = "boot.bat";
        
        /// <summary>
        /// Adds the server to the Windows Registry to start it on boot.
        /// </summary>
        /// <param name="serverSection">The section of the server to be added</param>
        /// <returns>Whether the addition was successful</returns>
        public static bool AddServerToStartupRegistry(Section serverSection)
        {
            // Get the key to be added to the Windows Registry
            RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string bootScriptFilepath = Path.Combine(serverSection.AddSection(ScriptsSectionName).SectionFullPath, BootScriptName);

            if (serverSection.GetFirstDocumentNamed("boot.bat") == null)
            {
                // Create the .bat file to be added to the Windows Registry
                string[] command = BuildStringForStartupRegistryCommand(serverSection.SimpleName).Split('\n');
                FileUtils.DumpToFile(bootScriptFilepath, command.ToList().Select(s => s.Trim()).ToList());
            }

            // Add the key to the Windows Registry if it doesn't already exist
            if (key != null && !IsServerInRegistry(serverSection))
            {
                key.SetValue("Glowberry-" + serverSection.SimpleName, bootScriptFilepath);
                key.Flush();
                key.Close();
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Removes the server from the Windows Registry to stop it from starting on boot.
        /// </summary>
        /// <param name="serverSection">The section of the server to be removed</param>
        /// <returns>Whether the removal was successful</returns>
        public static bool RemoveServerFromStartupRegistry(Section serverSection)
        {
            // Get the key to be removed from the Windows Registry
            RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string bootScriptFilepath = Path.Combine(serverSection.AddSection(ScriptsSectionName).SectionFullPath, BootScriptName);
            
            // Remove the key from the Windows Registry if it exists
            if (key != null && IsServerInRegistry(serverSection))
            {
                if (File.Exists(bootScriptFilepath)) File.Delete(bootScriptFilepath);
                key.DeleteValue("Glowberry-" + serverSection.SimpleName, false);
                key.Flush();
                key.Close();
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Checks if the server is in the Windows Registry to start on boot, and if the boot script exists.
        /// </summary>
        /// <param name="serverSection">The section of the server to be checked</param>
        /// <returns>Whether the server is OK in the registry</returns>
        public static bool IsServerInRegistry(Section serverSection)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string bootScriptFilepath = Path.Combine(serverSection.AddSection(ScriptsSectionName).SectionFullPath, BootScriptName);

            return key != null && File.Exists(bootScriptFilepath) && key.GetValue("Glowberry-" + serverSection.SimpleName) != null;
        }
        
        /// <summary>
        /// Builds the string to be used in the .bat file that will be added to the Windows Registry to start the server on boot.
        /// It essentially waits for an internet connection and then starts the server.
        /// </summary>
        /// <param name="serverName">The name of the server to start up</param>
        /// <returns>The command string used</returns>
        public static string BuildStringForStartupRegistryCommand(string serverName)
        {
            return $"""
                    @echo off
                    set "server=google.com"

                    :CHECK_CONNECTION
                    ping -n 1 %server% >nul
                    if errorlevel 1 (
                        echo Waiting for a Wi-Fi connection...
                        timeout /nobreak /t 5
                        goto CHECK_CONNECTION
                    )

                    echo Wi-Fi connection detected. Running Glowberry...
                    {Directory.GetCurrentDirectory()}\glowberry.exe start --server {serverName}
                    """.Trim();
        }
    }
}