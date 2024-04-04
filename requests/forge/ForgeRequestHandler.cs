using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;
using glowberry.common;
using glowberry.requests.abstraction;

namespace glowberry.requests.forge
{
    /// <summary>
    /// This class handles all requests sent into the minecraftforge website, in order to retrieve all
    /// the server versions
    /// </summary>
    internal class ForgeRequestHandler : AbstractBaseRequestHandler
    {
        public ForgeRequestHandler() : base("https://files.minecraftforge.net/net/minecraftforge/forge/")
        {
        }

        /// <summary>
        /// Accesses the website and parses out all the existent version names mapped
        /// to their direct download links.
        /// </summary>
        /// <returns>A Dictionary with a VersionName:VersionSite mapping</returns>
        public override async Task<Dictionary<string, string>> GetVersions()
        {
            try
            {
                HtmlDocument document = await ScrapeHandler.LoadFromWebAsync(BaseUrl);
                return new ForgeRequestParser().GetVersionUrlMap(BaseUrl, document.DocumentNode);
            }
            catch (Exception e)
            {
                Logging.Logger.Info("An error happened whilst trying to retrieve the forge versions.");
                Logging.Logger.Error(e.Message + "\n" + e.StackTrace);
                return null;
            }
        }
    }
}