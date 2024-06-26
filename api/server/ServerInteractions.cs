﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using LaminariaCore_General.utils;
using glowberry.common;
using glowberry.common.caches;
using LaminariaCore_General.common;
using static glowberry.common.configuration.Constants;

namespace glowberry.api.server
{
    /// <summary>
    /// This class implements a bunch of methods aimed at interacting with the server, be it
    /// receiving or sending data from/to it.
    /// </summary>
    public class ServerInteractions
    {
        /// <summary>
        /// The server editor instance to use for the server interactions.
        /// </summary>
        private string ServerName { get; }
        
        /// <summary>
        /// The server editor instance to use for the server interactions.
        /// </summary>
        private ServerEditor Editor { get; }

        /// <summary>
        /// The default buffer capacity for the server output buffer. Exists simply for optimisation
        /// purposes, since adding and removing items from a Queue within capacity is an O(1) operation, whilst
        /// doing so from a Queue without capacity is an O(n) operation.
        /// </summary>
        private const int BufferCapacity = 1000;

        /// <summary>
        /// A dictionary mapping a server to a list of messages received from the server, acting like a queue
        /// acting like a buffer for the server output.
        /// </summary>
        private static readonly Dictionary<string, List<string>> ServerOutputMappings = new ();

        /// <summary>
        /// Main constructor for the ServerInteractions class. Sets the server editor instance to use. <br/>
        /// </summary>
        /// <param name="serverName">The name of the server to interact with</param>
        public ServerInteractions(string serverName)
        {
            this.ServerName = serverName;
            
            Section serverSection = FileSystem.GetFirstSectionNamed(this.ServerName);
            this.Editor = GlobalEditorsCache.INSTANCE.GetOrCreate(serverSection);
        }
        
        /// <summary>
        /// Returns a list of all the created servers.
        /// </summary>
        public static List<string> GetServerList() => FileSystem.GetFirstSectionNamed("servers")?.GetAllTopLevelSections().Select(section => section.SimpleName).ToList() 
                                                      ?? new List<string>();

        /// <summary>
        /// Adds a message into the ServerOutputMappings buffer, to be processed later by any users of the API. <br/>
        /// When the buffer reaches the maximum capacity, the oldest  message is removed from the buffer.
        /// </summary>
        /// <param name="message">The message to add to the buffer.</param>
        public void AddToOutputBuffer(string message)
        {
            // Remove the oldest messages from the buffer until it frees up a space for the new message.
            if (this.GetOutputBuffer()?.Count > BufferCapacity)
                this.GetOutputBuffer().RemoveRange(BufferCapacity-2, this.GetOutputBuffer().Count - BufferCapacity-1);
            
            // Add the message to the buffer, creating a new buffer if it doesn't exist.
            if (!ServerOutputMappings.ContainsKey(this.ServerName)) ServerOutputMappings[this.ServerName] = new List<string>(BufferCapacity);
            ServerOutputMappings[this.ServerName].Insert(0, message);
        }

        /// <summary>
        /// Clears the output buffer for the server by removing it from the buffer dictionary.
        /// </summary>
        public void ClearOutputBuffer()
        {
            if (ServerOutputMappings.ContainsKey(this.ServerName))
                ServerOutputMappings[this.ServerName].Clear();
        }

        /// <returns>
        /// Returns a copy of the output buffer based on the server name, or an empty list if the
        /// server name is invalid.
        /// </returns>
        public List<string> GetOutputBuffer()
        {
            List<string> bufferCopy = new List<string>(BufferCapacity);

            if (!ServerOutputMappings.ContainsKey(this.ServerName) ||
                ServerOutputMappings[this.ServerName] == null)
                return bufferCopy;

            bufferCopy.AddRange(ServerOutputMappings[this.ServerName]);
            return bufferCopy;
        }

        /// <returns>
        /// Returns the latest message from the output buffer, or null if the buffer is empty.
        /// </returns>
        public string GetLatestOutput() => this.GetOutputBuffer()?.FirstOrDefault();

        /// <summary>
        /// Connects to the named pipe in the thread that is running the server and writes a message
        /// into its stdin. <br/>
        ///
        /// This message should then be transmitted through the pipe and once received, passed into
        /// the server's stdin.
        /// </summary>
        /// <param name="message">The message to send to the server thread</param>
        public async void WriteToServerStdin(string message)
        {
            // Connects to the named pipe (Format: piped<server name>) 
            using NamedPipeClientStream client = new (".","piped" + this.ServerName, PipeDirection.Out);
            await client.ConnectAsync();
            
            // Writes the message into the pipe
            using StreamWriter writer = new (client);
            await writer.WriteLineAsync(message);
            await writer.FlushAsync();
        }

        /// <returns>
        /// Returns the server process associated with the server based on the editor instance. <br/>
        /// 
        /// This method may return a completely different process if the server is not running and the latest
        /// process id has been assigned to another process.
        /// </returns>
        public Process GetServerProcess() 
            => this.Editor != null ? ProcessUtils.GetProcessById(this.Editor.GetServerInformation().CurrentServerProcessID) : null;

        /// <summary>
        /// Checks if the server is running based on its latest process id and the type of process it is. <br/>
        /// </summary>
        public bool IsRunning()
        {
            Process proc = this.GetServerProcess();
            return proc is { ProcessName: "java" or "cmd" };
        }

        /// <summary>
        /// Kills the server process associated with the server based on the editor instance.
        /// </summary>
        public async void KillServerProcess()
        {
            // Ensures that the server settings file is not being written to before killing the process.
            Section serverSection = FileSystem.GetFirstSectionNamed(this.ServerName);
            string settingsPath = serverSection.GetFirstDocumentNamed("server_settings.xml");
            using var _ = await FileUtils.WaitForFileAsync(settingsPath);
            
            Process proc = this.GetServerProcess();
            proc?.Kill();
        }

    }
}