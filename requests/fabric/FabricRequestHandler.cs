using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;
using glowberry.common;
using glowberry.requests.abstraction;
using glowberry.requests.mcversions;

// ReSharper disable InconsistentNaming

namespace glowberry.requests.fabric
{
    /// <summary>
    /// This class handles every request to the fabricmc.net website, and works
    /// together with FabricReleaseRequestParser in order to parse the information in a way that
    /// returns useful data.
    /// </summary>
    internal class FabricRequestHandler : AbstractBaseRequestHandler
    {
        public FabricRequestHandler() : base("https://meta.fabricmc.net/v2/versions/")
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
                string installerApiUrl = this.BaseUrl + "installer";
                string loaderApiUrl = this.BaseUrl + "loader";
                string gameApiUrl = this.BaseUrl + "game";
                
                // Based on the strings above, get the json data for the installer, loader and game versions
                List<Dictionary<string, string>> installerVersions = await GetJsonResponse(installerApiUrl);
                List<Dictionary<string, string>> loaderVersions = await GetJsonResponse(loaderApiUrl);
                List<Dictionary<string, string>> gameVersions = await GetJsonResponse(gameApiUrl);
                
                // Get the latest version of the installer and loader

                string latestInstallerVersion = installerVersions.First(x => x["stable"].Equals("True"))["version"];
                string latestLoaderVersion = loaderVersions.First(x => x["stable"].Equals("False"))["version"];
                
                // Based on the fabric cdn pattern, constructs the url to use within the mapping function
                string url = $"https://meta.fabricmc.net/v2/versions/loader/%VERSION%/{latestLoaderVersion}/{latestInstallerVersion}/server/jar";

                return new FabricRequestParser().GetVersionUrlMap(url, gameVersions);
            }
            catch (Exception e)
            {
                Logging.Logger.Info("An error happened whilst trying to retrieve the fabric versions.");
                Logging.Logger.Error(e, LoggingType.File);
                return null;
            }
        }
        
        /// <summary>
        /// Sends a request to the fabricmc website and retrieves the json response from the server as
        /// a dictionary.
        /// </summary>
        /// <param name="url">The URL to send a request to</param>
        /// <returns>A dictionary containing the values in the API</returns>
        private async Task<List<Dictionary<string, string>>> GetJsonResponse(string url)
        {
            HttpResponseMessage response = await RequestHandler.GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            
            // Deserializes the json response into a dictionary using System.Text.Json
            var versionDictionaries = JsonSerializer.Deserialize<List<Dictionary<string, dynamic>>>(json);
            
            // Converts the dynamic dictionaries into a string dictionary, by creating a new dictionary and adding the values as strings
            List<Dictionary<string, string>> stringVersionDictionaries = new List<Dictionary<string, string>>();
            
            foreach (var dict in versionDictionaries)
            {
                // Creates a new dictionary to add the string values to
                Dictionary<string, string> dictionaryEntry = new Dictionary<string, string>();
                
                foreach (var entry in dict)
                    dictionaryEntry.Add(entry.Key, entry.Value.ToString());
                
                stringVersionDictionaries.Add(dictionaryEntry);
            }

            return stringVersionDictionaries;
        }
    }
}