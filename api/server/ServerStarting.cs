using System.Threading.Tasks;
using LaminariaCore_Winforms.common;
using glowberry.common.handlers;
using glowberry.common.server.starters.threads;
using static glowberry.common.Constants;

namespace glowberry.api.server
{
    /// <summary>
    /// This class is responsible for providing an API used to start every supported server type.
    /// </summary>
    public class ServerStarter
    {
        
        /// <summary>
        /// The server editor used to manage the server.
        /// </summary>
        private ServerEditing EditingAPI { get; }
        
        /// <summary>
        /// Main constructor for the ServerStarter class. Takes in the server name and the server editor
        /// and sends the server starting instructions.
        /// </summary>
        /// <param name="serverName">The name of the server to operate on</param>
        public ServerStarter(string serverName)
        {
            Section serverSection = FileSystem.GetFirstSectionNamed(serverName);
            this.EditingAPI = new ServerAPI().Editor(serverSection.SimpleName);
        }

        /// <summary>
        /// Runs the server starter based on the server type and the settings defined in the server's section.
        /// </summary>
        /// <param name="outputHandler">The output system to use while logging the messages.</param>
        /// <returns>Whether or not the run was successful</returns>
        public async Task<bool> Run(MessageProcessingOutputHandler outputHandler)
        {
            ServerStarterThreadRunner serverRunner = new ServerStarterThreadRunner(this.EditingAPI.Raw());
            serverRunner.StartThread(outputHandler);

            return await serverRunner.IsProcessRunning();
        }
 
    }
}