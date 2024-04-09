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
    public static bool IsUserAdmin() =>
        new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    
}