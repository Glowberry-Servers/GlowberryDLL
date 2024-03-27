 using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using glowberry.common;
using glowberry.requests.abstraction;

namespace glowberry.requests.mcversions
{
    /// <summary>
    /// This class takes in a certain scope of Html Nodes and parses them down
    /// in different ways in order to extract useful information from them.
    /// </summary>
    internal class FabricRequestParser : AbstractBaseRequestParser
    {
        public override Task<string> GetServerDirectDownloadLink(string version, string url) =>
            throw new NotImplementedException();

        /// <summary>
        /// Builds a dictionary mapping the fabric version names to their respective URLs.
        /// </summary>
        /// <param name="baseUrl">The base URL to use when creating the dictionary</param>
        /// <param name="doc">The HTML doc to search for the versions on</param>
        /// <returns>A dictionary mapping the versions to their links.</returns>
        public override Dictionary<string, string> GetVersionUrlMap(string baseUrl, HtmlNode doc)
        {
            Dictionary<string, string> mappings = new Dictionary<string, string>();
            
            foreach (HtmlNode node in doc.Descendants("option"))
            {
                // Clones the base url so that we can create a new one to alter freely
                string urlClone = baseUrl.Clone().ToString();
                
                string version = node.InnerText;
                mappings.Add(version, urlClone.Replace("%VERSION%", version));
            }
            
            return mappings;
        }
    }
}