﻿using System.Collections.Generic;
using System.Configuration;

namespace glowberry.common
{
    /// <summary>
    /// This class is responsible providing each program with its version.
    /// </summary>
    public class GlobalVersionManager
    {
        
        /// <summary>
        /// The version mappings for each program.
        /// </summary>
        private static Dictionary<string, string> VersionMappings { get; } = new ()
        {
            {"launcher", "1.4.2"},
            {"web", "1.0.0"}
        };
        
        /// <summary>
        /// Gets the version for the specified program based on the version mappings.
        /// </summary>
        /// <param name="program">The program name to get the version for</param>
        /// <returns></returns>
        public static string GetVersion(string program) => VersionMappings[program];
    }
}