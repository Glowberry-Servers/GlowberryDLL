﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using LaminariaCore_General.utils;
using glowberry.common.handlers;
using glowberry.common.models;
using LaminariaCore_General.common;

namespace glowberry.common.server.starters
{
    /// <summary>
    /// This class handles everything related to starting Forge servers.
    /// </summary>
    internal class ForgeServerStarter : AbstractServerStarter
    {
        /// <summary>
        /// Main constructor for the ForgeServerStarter class. Defines the start-up arguments for the server, as well
        /// as the "other arguments" that are passed to the server.
        /// </summary>
        /// <param name="outputHandler">The output system to use while logging the messages.</param>
        public ForgeServerStarter(MessageProcessingOutputHandler outputHandler) : base(" ", "-jar %RAM_ARGUMENTS% \"%SERVER_JAR%\"", outputHandler)
        {
        }

        /// <summary>
        /// Overriden method from the AbstractServerStarter class. Runs the server with the given startup arguments, under
        /// the "run.bat" file.
        /// </summary>
        /// <param name="editor">The ServerEditor instance to use</param>
        public override Process Run(ServerEditor editor)
        {
            Section serverSection = editor.ServerSection;
            string runBatFilepath = PathUtils.NormalizePath(serverSection.GetFirstDocumentNamed("run.bat"));
            ServerInformation info = editor.GetServerInformation();
            
            // Makes sure the run.bat file exists, and gets the command from it.
            if (runBatFilepath == null) throw new FileNotFoundException("run.bat file not found");
            string startupArguments = GetRunCommandArguments(runBatFilepath, editor);
            
            // Creates the process through the extracted arguments
            Process proc = ProcessUtils.CreateProcess($"\"{info.JavaRuntimePath}\\bin\\java\"", startupArguments, serverSection.SectionFullPath);
            proc.OutputDataReceived += (sender, e) => RedirectMessageProcessing(sender, e, proc, serverSection.SimpleName);
            proc.ErrorDataReceived += (sender, e) => RedirectMessageProcessing(sender, e, proc, serverSection.SimpleName);

            // Finds the port and IP to start the server with, and starts the server.
            StartServer(serverSection, proc, editor);
            return proc;
        }

        /// <summary>
        /// Gets the run command template from the run.bat file and creates the run command string
        /// from it. 
        /// </summary>
        /// <param name="path">The path to the run.bat filepath</param>
        /// <param name="editor">The editor to use alongside the </param>
        private string GetRunCommandArguments(string path, ServerEditor editor)
        {
            // Reads the run.bat file and gets the server info
            List<string> lines = FileUtils.ReadFromFile(path);
            ServerInformation info = editor.GetServerInformation();
            
            // Gets the template line from the run.bat file.
            string templateLine = lines.FindLast(line => line.StartsWith("%JAVA%"));
            if (templateLine == null) throw new InvalidDataException("run.bat file is invalid");

            // Decides whether to use the GUI or not based on the server information.
            // and if so, moves the nogui argument to the end of the line.
            string noguiValue = info.UseGUI ? "" : "nogui";
            
            // returns the template line key arguments with the actual arguments.
            return templateLine.Replace("%JAVA%", "")
                .Replace("%RAM%", info.Ram.ToString())
                .Replace("nogui", "")
                .Trim() + " " + noguiValue;
        }
    }
}
