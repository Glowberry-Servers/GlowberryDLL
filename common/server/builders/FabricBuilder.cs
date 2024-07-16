using System.Threading.Tasks;
using glowberry.common.handlers;

namespace glowberry.common.server.builders
{
    /// <summary>
    /// This class implements the server building methods for the Fabric releases.
    /// </summary>
    public class FabricBuilder : AbstractServerBuilder
    {
        /// <summary>
        /// Main constructor for the FabricBuilder class. Defines the start-up arguments for the server.
        /// </summary>
        /// <param name="outputHandler">The output system to use while logging the messages.</param>
        public FabricBuilder(MessageProcessingOutputHandler outputHandler) : base("-jar \"%SERVER_JAR%\" nogui", outputHandler)
        {
        }

        /// <summary>
        /// Installs the server.jar file given the path to the server installer. When it finishes,
        /// return the path to the server.jar file.
        /// </summary>
        /// <param name="serverInstallerPath">The path to the installer</param>
        /// <param name="javaRuntime">The java runtime path used to run the server</param>
        /// <returns>The path to the server.jar file used to run the server.</returns>
        protected override Task<string> InstallServer(string serverInstallerPath, string javaRuntime)
        {
            return Task.Run(() => serverInstallerPath);
        }
    }
}