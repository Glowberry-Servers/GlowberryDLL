using System.IO;
using System.Linq;
using LaminariaCore_General.common;
using LaminariaCore_General.utils;
using Microsoft.Win32.TaskScheduler;

namespace glowberry.utils;

/// <summary>
/// This class contains methods to interact with the Windows Task Scheduler, specifically ones to add,
/// delete and check for tasks.
/// This utils class is very closely related to doing this for servers.
/// </summary>
public class WindowsSchedulerUtils
{
    
    /// <summary>
    /// The name of the section that contains the scripts for the server.
    /// </summary>
    private static string ScriptsSectionName = "gb-scripts";
        
    /// <summary>
    /// The name of the boot script that will be added to the Windows Registry.
    /// </summary>
    private static string BootScriptName = "boot.bat";

    /// <summary>
    /// Registers a server to the Windows Task Scheduler to start it on boot.
    /// </summary>
    /// <param name="serverSection">The section of the server to register into the scheduler</param>
    /// <returns>Whether the registration was succesful or not</returns>
    public static bool AddServerToTaskScheduler(Section serverSection)
    {
        // Create the task service and boot filepath to interact with the Windows Task Scheduler
        using TaskService taskService = new TaskService();
        string taskName = $"Glowberry-{serverSection.SimpleName}-bootstart";
        string bootScriptFilepath = Path.Combine(serverSection.AddSection(ScriptsSectionName).SectionFullPath, BootScriptName);

        if (serverSection.GetFirstDocumentNamed("boot.bat") == null)
        {
            // Create the .bat file to be added to the Windows Registry
            string[] command = BuildStringForStartupCommand(serverSection.SimpleName).Split('\n');
            FileUtils.DumpToFile(bootScriptFilepath, command.ToList().Select(s => s.Trim()).ToList());
        }

        try
        {
            // Creates the task definition and registers it into the Windows Task Scheduler
            TaskDefinition definition = taskService.NewTask();
            definition.RegistrationInfo.Description = $"Glowberry server {serverSection.SimpleName} startup task";
            
            definition.Triggers.Add(new BootTrigger());
            definition.Actions.Add(new ExecAction(bootScriptFilepath));
            
            taskService.RootFolder.RegisterTaskDefinition(taskName, definition);
        }
        catch { return false;}

        return true;
    }
    
    /// <summary>
    /// Builds the startup command for the server to be run on system boot.
    /// It essentially waits for an internet connection and then starts the server.
    /// </summary>
    /// <param name="serverName">The name of the server to start up</param>
    /// <returns>The command string to be added into a .bat file</returns>
    public static string BuildStringForStartupCommand(string serverName)
    {
        return $"""
                @echo off
                set "server=google.com"

                :CHECK_CONNECTION
                ping -n 1 %server% >nul
                if errorlevel 1 (
                    echo Waiting for a Wi-Fi connection...
                    timeout /nobreak /t 5
                    goto CHECK_CONNECTION
                )

                echo Wi-Fi connection detected. Running Glowberry...
                {Directory.GetCurrentDirectory()}\glowberry.exe start --server {serverName}
                """.Trim();
    }
}