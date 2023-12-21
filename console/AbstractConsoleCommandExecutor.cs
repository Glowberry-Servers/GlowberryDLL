using System;
using System.Linq;
using System.Reflection;
using glowberry.api.server;
using glowberry.common.handlers;

namespace glowberry.console
{
    /// <summary>
    /// This class is responsible for taking commands through its methods and executing API calls
    /// to the backend.
    ///
    /// The methods created in here must use the provided API to interact with the backend, and their signature
    /// must be «public void Command_(Command Name) (ConsoleCommand command)».
    /// </summary>
    public class AbstractConsoleCommandExecutor
    {
        
        /// <summary>
        /// The API used to interact with the backend.
        /// </summary>
        protected ServerAPI API { get; } = new ServerAPI();
        
        /// <summary>
        /// The output handler used to write messages to the console.
        /// </summary>
        protected MessageProcessingOutputHandler OutputHandler { get; } = new MessageProcessingOutputHandler(Console.Out);
        
        /// <summary>
        /// Using reflection, accesses all the methods within this class and tries to run the one matching
        /// the command name.
        /// If not found, write the help message.
        /// </summary>
        public void ExecuteCommand(ConsoleCommand command)
        {
            try
            {
                MethodInfo method = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name.ToLower() == "command_" + command.Command.Replace("-", "_").ToLower());
                
                // If the method exists, run it and return.
                if (method != null)
                {
                    method.Invoke(this, new object[] {command});
                    return;
                }
                
                // If the method does not exist, write the help message.
                OutputHandler.Write( $@"Command '{command.Command}' not found. Use 'help' for a list of possible commands.");
            }
            
            // If the method throws an exception, try to expose the inner exception.
            catch (TargetInvocationException e)
            {
                if (e.InnerException != null) throw e.InnerException;
                throw;
            }
        }
    }
}