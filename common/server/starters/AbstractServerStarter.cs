﻿using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using LaminariaCore_General.utils;
using glowberry.common.background;
using glowberry.common.handlers;
using glowberry.common.models;
using glowberry.utils;
using LaminariaCore_General.common;

namespace glowberry.common.server.starters
{
    /// <summary>
    /// This abstract class implement the common methods and properties for starting a server across
    /// all the server types.
    /// </summary>
    public abstract class AbstractServerStarter : AbstractLoggingMessageProcessing
    {
        /// <summary>
        /// The main constructor for the AbstractServerStarter class. Sets the startup arguments for the server.
        /// Handled variables by default: (Add to startup arguments)<br></br>
        /// > %SERVER_JAR%: The path to the server.jar file.<br></br>
        /// > %RAM_ARGUMENTS%: The arguments to set the maximum and minimum RAM usage.<br></br>
        /// </summary>
        /// <param name="otherArguments">Extra arguments to be added into the run command</param>
        /// <param name="startupArguments">The startup arguments for the server</param>
        /// <param name="outputHandler">The output system to use while logging the messages.</param>
        protected AbstractServerStarter(string otherArguments, string startupArguments, MessageProcessingOutputHandler outputHandler) : base(outputHandler)
        {
            StartupArguments = otherArguments + startupArguments;
        }

        /// <summary>
        /// The startup arguments for the server.
        /// </summary>
        private string StartupArguments { get; set; }

        /// <summary>
        /// Runs the server with the given startup arguments.
        /// </summary>
        /// <param name="editor">The ServerEditor instance to use</param>
        /// <returns>The process that was just created</returns>
        public virtual Process Run(ServerEditor editor)
        {
            // Get the server.jar and server.properties paths.
            Section serverSection = editor.ServerSection;
            string serverJarPath = serverSection.GetFirstDocumentNamed("server.jar");
            ServerInformation info = editor.GetServerInformation();
            
            if (serverJarPath == null) throw new FileNotFoundException("server.jar file not found");
            
            // Builds the startup arguments for the server.
            StartupArguments = StartupArguments
                .Replace("%SERVER_JAR%", PathUtils.NormalizePath(serverJarPath))
                .Replace("%RAM_ARGUMENTS%", "-Xmx" + info.Ram + "M -Xms" + info.Ram + "M");

            string javaPath = info.JavaRuntimePath != "java" ? $"\"{info.JavaRuntimePath}/bin/java\"" : info.JavaRuntimePath;
            if (!info.UseGUI) StartupArguments += " nogui";
            
            // Creates the process and starts it.
            Process proc = ProcessUtils.CreateProcess(javaPath, StartupArguments, serverSection.SectionFullPath);

            proc.OutputDataReceived += (sender, e) => RedirectMessageProcessing(sender, e, proc, serverSection.SimpleName);
            proc.ErrorDataReceived += (sender, e) => RedirectMessageProcessing(sender, e, proc, serverSection.SimpleName);

            // Finds the port and IP to start the server with, and starts the server.
            if (!StartServer(serverSection, proc, editor))
                throw new ServerException("Could not find a port to start the server with.");
            
            return proc;
        }

        /// <summary>
        /// Find the port and IP to start the server with, and start the server.
        /// This method is subject to some higher elapsed run times due to the port mapping.
        /// </summary>
        /// <param name="serverSection">The server section to work with</param>
        /// <param name="proc">The server process to track</param>
        /// <param name="editor">The editor instance to use to interact with the files</param>
        /// <returns>Whether or not the server was successfully started</returns>
        protected bool StartServer(Section serverSection, Process proc, ServerEditor editor)
        {
            Logging.Logger.Info($"Starting the '{serverSection.SimpleName}' server...");

            // Sets up the process to be hidden and not create a window.
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.UseShellExecute = false;

            // Starts both the process, and the backup handler attached to it, and records the process ID.
            proc.Start();
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
            new Thread(new ServerBackupHandler(editor, proc.Id).RunTask) { IsBackground = false }.Start();

            return true;
        }
    }
}
