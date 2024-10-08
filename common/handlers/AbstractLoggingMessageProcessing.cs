﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LaminariaCore_General.utils;
using glowberry.api.server;

namespace glowberry.common.handlers
{
    /// <summary>
    /// This class implements all the base methods for processing server output messages,
    /// to be worked on further or left as-is by the child classes.
    /// </summary>
    public abstract class AbstractLoggingMessageProcessing
    {
        /// <summary>
        /// The termination code for a server execution, to be used by the processing events
        /// </summary>
        protected int TerminationCode { get; set; } = -1;

        /// <summary>
        /// A collection of errors to handle differently in the processing methods
        /// </summary>
        protected ErrorCollection SpecialErrors { get; } = new ();

        /// <summary>
        /// The output system to use for the message processing system.
        /// </summary>
        protected MessageProcessingOutputHandler OutputSystem { get; }
        
        /// <summary>
        /// The server interactions API to use for the message processing system.
        /// </summary>
        protected ServerInteractions InteractionsAPI { get; set; }

        /// <summary>
        /// Main constructor for the AbstractLoggingMessageProcessing class. Sets the output system to use. <br/>
        /// This system can be STDOUT, a RichTextBox, or any other supported output system.
        /// </summary>
        /// <param name="system">The output system to use for the message processing.</param>
        protected AbstractLoggingMessageProcessing(MessageProcessingOutputHandler system) 
            => this.OutputSystem = system;

        /// <summary>
        /// Receives the data from the server output and forwards it to all the necessary processing methods.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event arguments</param>
        /// <param name="proc">The running process of the server</param>
        /// <param name="serverName">The name of the server to redirect the output from</param>
        protected void RedirectMessageProcessing(object sender, DataReceivedEventArgs e, Process proc, string serverName)
        {
            // Adds the data to the output buffer of the server
            try
            {
                this.InteractionsAPI = new ServerAPI().Interactions(serverName);
                this.InteractionsAPI.AddToOutputBuffer(e.Data);
            }
            catch
            {
                Logging.Logger.Warn("Tried to add to output buffer while directory is gone.");
            }

            this.ProcessMergedData(sender, e, proc); // Processes the data in the output system
        }
        
        /// <summary>
        /// Due to the stupidity of early Minecraft logging, capture the STDERR and STDOUT in this method,
        /// and separate them by WARN, ERROR, and INFO messages, calling the appropriate methods.
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event arguments</param>
        /// <param name="proc">The running process of the server</param>
        protected virtual void ProcessMergedData(object sender, DataReceivedEventArgs e, Process proc)
        {
            // If the lines are invalid or empty, return. 
            if (e.Data == null || e.Data.Trim().Equals(string.Empty)) return;

            // Try to match the line with the regex, expecting groups.
            Match matches = Regex.Match(e.Data.Trim(), @"^(?:\[[^\]]+\] \[[^\]]+\]: |[\d-]+ [\d:]+ \[[^\]]+\] )(.+)$",
                RegexOptions.Multiline);

            try
            {
                // Get the type section and the message from the regex groups.
                string typeSection = matches.Groups[0].Captures[0].Value;
                string message = matches.Groups[1].Captures[0].Value.Trim();

                // If the message is an error and not registered as a special error, handle it as such.
                if ((!SpecialErrors.StringMatches(typeSection) && typeSection.Contains("ERROR")) || typeSection.Contains("Exception"))
                    ProcessErrorMessages(message, proc);

                // Handle the warning messages.
                else if (typeSection.Contains("WARN"))
                    ProcessWarningMessages(message, proc);

                // Handle the info messages.
                else if (typeSection.Contains("INFO")) 
                    ProcessInfoMessages(message, proc);
            }
            catch (ArgumentOutOfRangeException) { }

            // Handle any other messages that don't fit the above criteria.
            ProcessOtherMessages(e.Data.Trim(), proc);
        }

        /// <summary>
        /// Processes any INFO messages received from the server jar.
        /// </summary>
        /// <param name="message">The logging message</param>
        /// <param name="proc">The object for the process running</param>
        /// <terminationCode>0 - The server.jar fired a normal info message</terminationCode>
        protected virtual void ProcessInfoMessages(string message, Process proc)
        {
            TerminationCode = TerminationCode != 1 ? 0 : 1;

            // In some versions the EULA message is sent as INFO, so handle it here too
            if (message.ToLower().Contains("agree to the eula")) proc.KillProcessAndChildren();
            Logging.Logger.Info(message);
        }

        /// <summary>
        /// Processes any ERROR messages received from the server jar.
        /// Since we might be updating the console from another thread, we're just going to invoke everything
        /// and that's that.
        /// </summary>
        /// <param name="message">The logging message</param>
        /// <param name="proc">The object for the process running</param>
        /// <terminationCode>1 - The server.jar fired an error. If fired last, stop the build.</terminationCode>
        protected virtual void ProcessErrorMessages(string message, Process proc)
        {
            this.OutputSystem.Write("[ERROR] " + message + Environment.NewLine, Color.Firebrick);
        }

        /// <summary>
        /// Processes any WARN messages received from the server jar.
        /// Since we might be updating the console from another thread, we're just going to invoke everything
        /// and that's that.
        /// </summary>
        /// <param name="message">The logging message</param>
        /// <param name="proc">The object for the process running</param>
        /// <terminationCode>2 - The server.jar fired a warning</terminationCode>
        protected virtual void ProcessWarningMessages(string message, Process proc)
        {
            this.OutputSystem.Write("[WARN] " + message + Environment.NewLine, Color.OrangeRed);
            TerminationCode = TerminationCode != 1 ? 2 : 1;
        }

        /// <summary>
        /// Processes any undifferentiated messages received from the server jar.
        /// Since we might be updating the console from another thread, we're just going to invoke everything
        /// and that's that.
        /// </summary>
        /// <param name="message">The logging message</param>
        /// <param name="proc">The object for the process running</param>
        /// <terminationCode>3 - The server.jar logged other messages</terminationCode>
        protected virtual void ProcessOtherMessages(string message, Process proc)
        {
            // Agreeing to the EULA is a special case, so we're going to handle it here.
            if (message.ToLower().Contains("agree to the eula")) proc.KillProcessAndChildren();

            // If the message contains the word "error" in it, we're going to assume it's an error.
            if (message.ToLower().Split(' ').Any(x => x.StartsWith("error")))
            {
                ProcessErrorMessages(message, proc);
                return;
            }

            this.OutputSystem.Write(Logging.Logger.Warn(message) + Environment.NewLine, Color.Gray);
            TerminationCode = TerminationCode != 1 ? 3 : 1;
        }
    }
}