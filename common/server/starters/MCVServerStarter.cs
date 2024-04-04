using glowberry.common.handlers;

namespace glowberry.common.server.starters
{
    /// <summary>
    /// This class handles everything related to starting vanilla servers.
    /// </summary>
    internal class McvServerStarter : AbstractServerStarter
    {
        /// <summary>
        /// Main constructor for the MCVServerStarter class. Defines the start-up arguments for the server, as well
        /// as the "other arguments" that are passed to the server.
        /// </summary>
        /// <param name="outputHandler">The output system to use while logging the messages.</param>
        public McvServerStarter(MessageProcessingOutputHandler outputHandler) : base(" ", "-jar %RAM_ARGUMENTS% \"%SERVER_JAR%\"", outputHandler)
        {
        }
    }
}