using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using glowberry.common.handlers;
using LaminariaCore_General.common;
using LaminariaCore_General.utils;
using static glowberry.common.configuration.Constants;

namespace glowberry.api.server
{
    /// <summary>
    /// This class is responsible for providing an API used to start every supported server type.
    /// </summary>
    public class ServerStarting
    {
        
        /// <summary>
        /// The server editor used to manage the server.
        /// </summary>
        private ServerEditing EditingAPI { get; }
        
        /// <summary>
        /// Main constructor for the ServerStarting class. Takes in the server name and the server editor
        /// and sends the server starting instructions.
        /// </summary>
        /// <param name="serverName">The name of the server to operate on</param>
        public ServerStarting(string serverName)
        {
            Section serverSection = FileSystem.AddSection(serverName);
            this.EditingAPI = new ServerAPI().Editor(serverSection.SimpleName);
        }

        /// <summary>
        /// Sends a command to the glowberry helper to start the server. This process
        /// will have no visible window and will be hidden from the user.
        /// </summary>
        public void Run()
        {
            Process proc = new Process();
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            proc.StartInfo.FileName = "gbhelper.exe";
            proc.StartInfo.Arguments = $"run-server --name {this.EditingAPI.GetServerName()}";
            
            proc.Start();
        }
    }
}