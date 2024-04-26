using System;
using System.Security.Principal;

namespace glowberry.utils;

/// <summary>
/// This class contains methods to interact with the windows permissioning system.
/// </summary>
public static class PermissionUtils
{

    /// <summary>
    /// Checks if the current user has administrative privileges.
    /// </summary>
    public static bool IsUserAdmin()
    {
        bool isAdmin;
        
        try
        {
            WindowsIdentity user = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(user);
            isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (Exception ex) { isAdmin = false; }
        
        return isAdmin;
    }
    
}