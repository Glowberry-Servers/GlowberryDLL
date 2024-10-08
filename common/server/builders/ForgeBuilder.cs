﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LaminariaCore_General.utils;
using glowberry.api.server;
using glowberry.common.handlers;
using glowberry.common.models;
using glowberry.extensions;
using glowberry.requests.content;
using glowberry.utils;
using LaminariaCore_General.common;
using static glowberry.common.configuration.Constants;


// ReSharper disable AccessToDisposedClosure

namespace glowberry.common.server.builders
{
    /// <summary>
    /// This class implements the server building methods for the forge releases.
    /// </summary>
    internal class ForgeBuilder : AbstractServerBuilder
    {
        /// <summary>
        /// Main constructor for the ForgeBuilder class. Defines the start-up arguments for the server.
        /// </summary>
        /// <param name="outputHandler">The output system to use while logging the messages.</param>
        public ForgeBuilder(MessageProcessingOutputHandler outputHandler) : base(
            "-jar -Xmx1024M -Xms1024M \"%SERVER_JAR%\" nogui", outputHandler)
        {
            
        }

        /// <summary>
        /// Installs the server.jar file given the path to the server installer. When it finishes,
        /// return the path to the server.jar file.
        /// </summary>
        /// <param name="serverInstallerPath">The path to the installer</param>
        /// <returns>The path to the server.jar file used to run the server.</returns>
        /// <param name="javaRuntime">The java runtime path used to build the server</param>
        protected override async Task<string> InstallServer(string serverInstallerPath, string javaRuntime)
        {
            // Gets the server section from the path of the jar being run, and gets the name of the server from it
            List<string> directories = serverInstallerPath.Split(Path.DirectorySeparatorChar).ToList();
            string serverName = directories[directories.IndexOf("servers") + 1];
            Section serverSection = FileSystem.GetFirstSectionNamed("servers/" + serverName);

            // Creates the process that will build the server
            Process forgeBuildingProcess = ProcessUtils.CreateProcess($"\"{javaRuntime}\\bin\\java\"",
                    $" -jar {serverInstallerPath} --installServer", serverSection.SectionFullPath);

            // Set the output and error data handlers
            OutputSystem.Write("The output for the server construction is hidden in Forge for optimisation purposes. Hold tight!" + Environment.NewLine);
            forgeBuildingProcess.ErrorDataReceived += (sender, e) => RedirectMessageProcessing(sender, e, forgeBuildingProcess, serverName);
            TerminationCode = 0;

            // Start the process
            forgeBuildingProcess.Start();
            forgeBuildingProcess.BeginOutputReadLine();
            forgeBuildingProcess.BeginErrorReadLine();
            await forgeBuildingProcess.WaitForExitAsync();

            // Returns the path to the server.jar file, which in this case, is a forge file.
            serverSection.RemoveDocument("server.jar");
            
            // Checks if a run.bat file exists, and if it does, return it instead of the forge file.
            string runBatFilepath = serverSection.GetFirstDocumentNamed("run.bat");
            if (runBatFilepath != null) return runBatFilepath;
            
            // Return the forge file.
            return serverSection.GetAllDocuments().First(x => Path.GetFileName(x).Contains("forge") && Path.GetFileName(x).EndsWith("jar"));
        }

        /// <summary>
        /// Due to the stupidity of early Minecraft logging, capture the STDERR and STDOUT in this method,
        /// and separate them by WARN, ERROR, and INFO messages, calling the appropriate methods.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event arguments</param>
        /// <param name="proc">The running process of the server</param>
        protected override void ProcessMergedData(object sender, DataReceivedEventArgs e, Process proc)
        {
            if (e.Data == null || e.Data.Trim().Equals(string.Empty)) return;

            if (e.Data.Contains("ERROR") || e.Data.StartsWith("Exception"))
                ProcessErrorMessages(e.Data, proc);
            else if (e.Data.Contains("WARN"))
                ProcessWarningMessages(e.Data, proc);
            else if (e.Data.Contains("INFO") || e.Data.Contains("LOADING"))
                ProcessInfoMessages(e.Data, proc);
            else
                ProcessOtherMessages(e.Data, proc);
        }

        /// <summary>
        /// Processes any INFO messages received from the server jar.
        /// </summary>
        /// <param name="message">The logging message</param>
        /// <param name="proc">The object for the process running</param>
        /// <terminationCode>0 - The server.jar fired a normal info message</terminationCode>
        protected override void ProcessInfoMessages(string message, Process proc)
        {
            TerminationCode = TerminationCode != 1 ? 0 : 1;

            if (message.ToLower().Contains("preparing level") || message.ToLower().Contains("agree to the eula"))
                ProcessUtils.KillProcessAndChildren(proc);

            Logging.Logger.Info(message);
        }

        /// <summary>
        /// Runs the server once and closes it once it has been initialised. Deletes the world folder
        /// in the end.
        /// This method aims to initialise and build all of the server files in one go.
        /// </summary>
        /// <param name="serverJarPath">The path of the server file to run</param>
        /// <param name="editingApi">The ServerEditingAPI instance bound to the server to use with this run</param>
        /// <returns>A Task with a code letting the user know if an error happened</returns>
        protected override async Task<int> FirstSetupRun(ServerEditing editingApi, string serverJarPath)
        {
            // Due to how forge works, we need to generate a run.bat file to run the forge.
            Section serverSection = GetSectionFromFile(serverJarPath);
            serverSection.AddDocument("server.properties"); // Adds the server properties just in case
            ServerInformation info = editingApi.GetServerInformation();
            
            // Re-checks java version and tries to download the correct jdk. This is because the installer
            // Doesn't always match up version-wise with the server.
            if (info.AutoDetectHint)
            {
                string forgeFilePath = serverSection.GetAllDocuments().FirstOrDefault(x => x.EndsWith(".jar")) ??
                                       serverJarPath;
                
                info.JavaRuntimePath = await JavaUtils.HandleAutoJavaDetection(forgeFilePath, OutputSystem);
            }

            editingApi.UpdateServerSettings(info.ToDictionary());

            // Creates the run.bat file if it doesn't already exist, with simple running params
            string runCommand = $"\"{info.JavaRuntimePath}\\bin\\java\" {StartupArguments}";
            string runFilepath = Path.Combine(serverSection.SectionFullPath, "run.bat");

            if (!File.Exists(runFilepath))
                File.WriteAllText(runFilepath, runCommand.Replace("%SERVER_JAR%", serverJarPath));

            // Gets the run.bat file and removed all the comment lines
            List<string> lines = FileUtils.ReadFromFile(runFilepath);
            lines.RemoveAll(x => x.StartsWith("REM") || x.StartsWith("pause"));
            FileUtils.DumpToFile(runFilepath, lines);

            string fileText = File.ReadAllText(runFilepath);
            
            if (!fileText.Contains("nogui"))
            {
                // If the bat file was generated, there's a '%*' that needs to be replaced instead of just
                // adding nogui to the end.
                fileText = fileText.Contains("%*")
                    ? fileText.Replace("%*", "nogui %*")
                    : fileText + " nogui";

                fileText = fileText.Replace("@user_jvm_args.txt", "-Xms1024M -Xmx1024M")
                    .Replace("java", $"\"{info.JavaRuntimePath}\\bin\\java\"");
                
                File.WriteAllText(runFilepath, fileText);
            }

            // Creates the process to be run
            Process proc = ProcessUtils.CreateProcess("cmd.exe", $"/c {runFilepath}", serverSection.SectionFullPath);

            // Gets an available port starting on the one specified, and changes the properties file accordingly
            if (editingApi.Raw().HandlePortForServer() == 1)
            {
                ProcessErrorMessages("Could not find a port to start the server with! Please change the port in the server properties or free up ports to use.", proc);
                return 1;
            }

            // Handles the processing of the STDOUT and STDERR outputs, changing the termination code accordingly.
            proc.OutputDataReceived += (sender, e) => RedirectMessageProcessing(sender, e, proc, editingApi.GetServerName());
            proc.ErrorDataReceived += (sender, e) => RedirectMessageProcessing(sender, e, proc, editingApi.GetServerName());

            // Waits for the termination of the process by the OutputDataReceived event or ErrorDataReceived event.
            proc.Start();
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
            await proc.WaitForExitAsync();

            // Disposes of the process and checks if the termination code is 1 or -1. If so, return 1.
            proc.Dispose();
            serverSection.RemoveSection("world"); // Finds the world folder and deletes it if it exists
            
            // The math here is because if nothing happened, it errored with no changes, so the code is -1
            // and we can simply return 1.
            if (TerminationCode * TerminationCode == 1) return 1;

            // Completes the run, resetting the termination code
            OutputSystem.Write(Logging.Logger.Info("Silent run completed.") + Environment.NewLine);
            TerminationCode = -1;
            
            // Sneakily re-formats the -Xmx and -Xms arguments to be in a template format
            fileText = fileText.Replace($"\"{info.JavaRuntimePath}\\bin\\java\"", "%JAVA%");
            fileText = fileText.Replace("-Xms1024M", "-Xms%RAM%M");
            fileText = fileText.Replace("-Xmx1024M", "-Xmx%RAM%M");
            File.WriteAllText(runFilepath, fileText);
            return 0;
        }
    }
}