using System;
using System.Linq;
using glowberry.common;
using glowberry.common.models;
using WindowsFirewallHelper;

namespace glowberry.utils;

/// <summary>
/// This class is used to manage the Windows firewall programatically.
/// </summary>
public class FirewallUtils
{

    /// <summary>
    /// Configures a port in the Windows firewall in any specified way.
    /// </summary>
    /// <param name="name">The name of the new rule to be created</param>
    /// <param name="port">The target port being configured</param>
    /// <param name="action">The action to perform, be it block or allow</param>
    /// <param name="protocol">The protocol to use to configure the port</param>
    /// <param name="direction">The direction of the packets, either in or outbound</param>
    /// <returns>Whether the configuration was successful or not</returns>
    private static bool ConfigurePort(string name, ushort port, FirewallAction action, FirewallProtocol protocol, FirewallDirection direction)
    {

        try
        {
            var rule = FirewallManager.Instance.CreatePortRule(name, action, port, protocol);
            rule.Direction = direction;
            
            Logging.Logger.Info($"Adding firewall rule with PORT: {port} with NAME: {name}; ACTION: {action}; PROTOCOL: {protocol}; DIRECTION: {direction}");
            FirewallManager.Instance.Rules.Add(rule);
        }
        catch (Exception e)
        {
            Logging.Logger.Warn($"Failed to configure port {port} in firewall: " + e.Message);
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// Removes a port rule from the firewall.
    /// </summary>
    /// <param name="name">The name of the port rule</param>
    /// <param name="exact">Whether the name used has to be matched exactly in order for the rule to be removed or not</param>
    /// <returns>Whether the removal was successful or not</returns>
    public static bool RemovePortRuleByName(string name, bool exact = false)
    {
        try
        {
            // Iterates over all the rules and checks if there's one matching the name
            // Depending on the exact parameter, the rule is found by name matching or by name containing
            IFirewallRule[] rules = exact ? 
                    FirewallManager.Instance.Rules.ToList().FindAll(r => r.Name == name).ToArray() : 
                    FirewallManager.Instance.Rules.ToList().FindAll(r => r.Name.Contains(name)).ToArray();

            foreach (IFirewallRule rule in rules)
            {
                Logging.Logger.Info($"Removing firewall rule '{rule.Name}'");
                FirewallManager.Instance.Rules.Remove(rule);
            }
        }
        
        catch (Exception e)
        {
            Logging.Logger.Warn($"Failed to remove port rule {name} from firewall: " + e.Message);
            return false;
        }

        return true;
    }


    /// <summary>
    /// Creates a port rule for a server in the firewall.
    /// </summary>
    /// <param name="editor">The server editor used to gather information about the server</param>
    /// <returns>Whether the addition was successful or not</returns>
    public static bool CreatePortRuleForServer(ServerEditor editor)
    {
        // Gets the server name and information
        string serverName = editor.GetServerName();
        ServerInformation info = editor.GetServerInformation();
        FirewallDirection[] directions = { FirewallDirection.Inbound, FirewallDirection.Outbound };
        
        // Iterates over all the directions and configures the ports for them
        foreach (FirewallDirection direction in directions)
        {
            if (!ConfigurePort($"Glowberry-{serverName}@{info.Port}#TCP", (ushort) info.Port, FirewallAction.Allow, FirewallProtocol.TCP, direction))
                return false;
        }

        return true;
    }
    
    /// <summary>
    /// Checks if a port rule is present in the firewall. Matches the name exactly if 'exact'
    /// is set to true, otherwise it checks if the name contains the specified string.
    /// </summary>
    /// <param name="name">The name of the rule to check for</param>
    /// <param name="port">The port to check for, if set to -1, ignores.</param>
    /// <param name="exact">Whether to exactly match the string or not</param>
    /// <returns>Whether the port rule exists</returns>
    public static bool IsPortRulePresent(string name, int port = -1, bool exact = false)
    {
        // If the port is set, it checks if the rule contains the port and the name
        if (port != -1)
        {
            return exact ? FirewallManager.Instance.Rules.Any(r => r.Name == name && r.LocalPorts.Contains((ushort) port)) 
                : FirewallManager.Instance.Rules.Any(r => r.Name.Contains(name) && r.LocalPorts.Contains((ushort) port));
        }
        
        // Otherwise, it just checks if the rule contains the name
        return exact ? FirewallManager.Instance.Rules.Any(r => r.Name == name) 
            : FirewallManager.Instance.Rules.Any(r => r.Name.Contains(name));
    }

}