using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using glowberry.common;
using glowberry.requests.abstraction;

// ReSharper disable InconsistentNaming

namespace glowberry.requests.mcversions.full
{
    /// <summary>
    /// This class handles every request to the fabricmc.net website, and works
    /// together with FabricReleaseRequestParser in order to parse the information in a way that
    /// returns useful data.
    /// </summary>
    internal class FabricRequestHandler : AbstractBaseRequestHandler
    {
        public FabricRequestHandler() : base("https://fabricmc.net/use/server")
        {
        }

        /// <summary>
        /// Accesses the website and parses out all the existent version names mapped
        /// to their download links.
        /// </summary>
        /// <returns>A Dictionary with a VersionName:VersionSite mapping</returns>
        public override async Task<Dictionary<string, string>> GetVersions()
        {
            try
            {
                HtmlDocument document = await Handler.LoadFromWebAsync(BaseUrl);
                
                // Gets the installer versions and returns the first option, the latest
                string latestInstallerVersion = document.GetElementbyId("installer-version").Descendants("option")
                    .ElementAt(0).InnerText;

                // Does the same for the loader versions
                string latestLoaderVersion = document.GetElementbyId("loader-version").Descendants("option")
                    .ElementAt(0).InnerText;

                // Gets the fabric versions list from the page
                HtmlNode versionsList = document.GetElementbyId("minecraft-version");

                // Based on the fabric cdn pattern, constructs the url to use within the mapping function
                string url = $"https://meta.fabricmc.net/v2/versions/loader/%VERSION%/{latestLoaderVersion}/{latestInstallerVersion}/server/jar";

                return new FabricRequestParser().GetVersionUrlMap(url, versionsList);
            }
            catch (Exception e)
            {
                Logging.Logger.Info("An error happened whilst trying to retrieve the vanilla versions.");
                Logging.Logger.Error(e, LoggingType.File);
                return null;
            }
        }
    }
}