using System.Collections.Generic;
using glowberry.common.server.builders;
using glowberry.common.server.starters;
using glowberry.requests.fabric;
using glowberry.requests.forge;
using glowberry.requests.mcversions;
using glowberry.requests.mcversions.full;
using glowberry.requests.mcversions.snapshots;
using glowberry.requests.spigot;
using static glowberry.common.Constants;


namespace glowberry.common.factories
{
    /// <summary>
    /// This is a partial class of ServerTypeMappingsFactory, containing the actual mappings to be
    /// interacted with.
    /// TODO: Turn this into an XML file-based mapping system... somehow.
    /// </summary>
    public partial class ServerTypeMappingsFactory
    {
        /// <summary>
        /// The dictionary containing the values for every server type supported.
        /// </summary>
        private Dictionary<string, Dictionary<string, object>> Mappings { get; } =
            new ()
            {
                {
                    "vanilla", new Dictionary<string, object>
                    {
                        { "handler", new MCVRequestHandler() },
                        { "parser", new McvRequestParser() },
                        { "builder", typeof(McvBuilder) },
                        { "starter", typeof(McvServerStarter) },
                        { "cache_file", FileSystem.AddSection("versioncache").AddDocument("vanilla_releases.cache") }
                    }
                },
                {
                    "vanilla snapshots", new Dictionary<string, object>
                    {
                        { "handler", new MCVSnapshotsRequestHandler() },
                        { "parser", new McvRequestParser() },
                        { "builder", typeof(McvBuilder) },
                        { "starter", typeof(McvServerStarter) },
                        { "cache_file", FileSystem.AddSection("versioncache").AddDocument("vanilla_snapshots.cache") }
                    }
                },
                {
                    "spigot", new Dictionary<string, object>
                    {
                        { "handler", new SpigotRequestHandler() },
                        { "parser", new SpigotRequestParser() },
                        { "builder",  typeof(SpigotBuilder) },
                        { "starter", typeof(SpigotServerStarter) },
                        { "cache_file", FileSystem.AddSection("versioncache").AddDocument("spigot_releases.cache") }
                    }
                },
                {
                    "forge", new Dictionary<string, object>
                    {
                        { "handler", new ForgeRequestHandler() },
                        { "parser", new ForgeRequestParser() },
                        { "builder", typeof(ForgeBuilder) },
                        { "starter", typeof(ForgeServerStarter) },
                        { "cache_file", FileSystem.AddSection("versioncache").AddDocument("forge_releases.cache") }
                    }
                },
                {
                    "fabric", new Dictionary<string, object>
                    {
                        { "handler", new FabricRequestHandler {GetUnstablesOnly = false} },
                        { "parser", new FabricRequestParser() },
                        { "builder", typeof(FabricBuilder) },
                        { "starter", typeof(FabricServerStarter) },
                        { "cache_file", FileSystem.AddSection("versioncache").AddDocument("fabric_releases.cache") }
                    }
                },
                {
                    "fabric (unstable)", new Dictionary<string, object>
                    {
                        { "handler", new FabricRequestHandler {GetUnstablesOnly = true} },
                        { "parser", new FabricRequestParser() },
                        { "builder", typeof(FabricBuilder) },
                        { "starter", typeof(FabricServerStarter) },
                        { "cache_file", FileSystem.AddSection("versioncache").AddDocument("fabric_releases.unstable.cache") }
                    }
                },
                {
                    "unknown", new Dictionary<string, object>
                    {
                        { "handler", null },
                        { "parser", null },
                        { "builder", null },
                        { "starter", typeof(McvServerStarter) },
                        { "cache_file", null }
                    }
                }
            };
    }
}