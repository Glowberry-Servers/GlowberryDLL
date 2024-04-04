using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using glowberry.common;
using glowberry.requests.abstraction;

namespace glowberry.requests.fabric
{
    /// <summary>
    /// This class takes in a certain scope of Html Nodes and parses them down
    /// in different ways in order to extract useful information from them.
    /// </summary>
    internal class FabricRequestParser : AbstractBaseRequestParser
    {
        /// <summary>
        /// Since fabric is an amazing modloader, simply return the url because the cached URL is already
        /// the direct download link. We're just keeping this method here for consistency and interface
        /// contract.
        /// </summary>
        /// <param name="version">The version to get the direct link for. Does not matter.</param>
        /// <param name="url">The direct download link</param>
        /// <returns>The URL parameter</returns>
        public override Task<string> GetServerDirectDownloadLink(string version, string url)
        {
            return Task.FromResult(url);
        }
        

        /// <summary>
        /// Builds a dictionary mapping the fabric version names to their respective URLs.
        /// </summary>
        /// <param name="baseUrl">The base URL to use when creating the dictionary</param>
        /// <param name="json">The json file data associated with the version</param>
        /// <returns>A dictionary mapping the versions to their links.</returns>
        public Dictionary<string, string> GetVersionUrlMap(string baseUrl, List<Dictionary<string, string>> json)
        {
            Dictionary<string, string> mappings = new Dictionary<string, string>();
            
            // Iterate through the json object and add the stable versions to the dictionary.
            foreach (var entry in json)
            {
                if (entry["stable"].Equals("False")) continue;
                
                // Get the version and url from the json object.
                string url = baseUrl.Clone().ToString();
                string version = entry["version"];
                
                mappings.Add(version, url.Replace("%VERSION%", version));
            }

            return mappings;
        }
    }
}