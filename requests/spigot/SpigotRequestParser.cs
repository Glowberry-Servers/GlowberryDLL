﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using glowberry.common;
using glowberry.requests.abstraction;

namespace glowberry.requests.spigot
{
    /// <summary>
    /// This class takes in a certain scope of html nodes and parses out the information
    /// contained in them in such a way that it will be useful for the program
    /// </summary>
    internal class SpigotRequestParser : AbstractBaseRequestParser
    {
        /// <summary>
        /// Returns the direct download link for a server given its version page
        /// </summary>
        /// <param name="version">The server version</param>
        /// <param name="url">The url of the version page to get the download link from</param>
        /// <returns>The direct download link for the server</returns>
        public override async Task<string> GetServerDirectDownloadLink(string version, string url)
        {
            try
            {
                using CancellationTokenSource ct = new(new TimeSpan(0, 0, 0, 10));

                HtmlDocument doc = await AbstractBaseRequestHandler.ScrapeHandler.LoadFromWebAsync(url, ct.Token)
                    .ConfigureAwait(false);

                IEnumerable<HtmlNode> wellDiv = from div in doc.DocumentNode.Descendants("div")
                    where div.HasClass("well")
                    select div;

                return wellDiv.ElementAt(0).SelectSingleNode("//*[@id=\"get-download\"]/div/div/div[2]/div/h2/a")
                    .GetAttributeValue("href", null);
            }

            // If the task ended up being cancelled due to a time out, throw an exception.
            catch (TaskCanceledException)
            {
                throw new TimeoutException("Request timed out");
            }
        }

        /// <summary>
        /// Parses out the version names and server download URLs from the node and
        /// returns them in the form of a dictionary mapping name:link
        /// </summary>
        /// <param name="baseUrl">The current url of the node</param>
        /// <param name="doc">The HtmlNode to parse</param>
        /// <returns>A Dictionary(string,string) containing the mappings</returns>
        public Dictionary<string, string> GetVersionUrlMap(string baseUrl, HtmlNode doc)
        {
            Dictionary<string, string> mappings = new ();

            IEnumerable<HtmlNode> downloadPanels = from div in doc.Descendants("div")
                where div.HasClass("download-pane")
                select div;

            // Navigates to the version name and download links inside each of the downloadPanels,
            // retrieves the text inside it, and adds it to the mappings.
            foreach (HtmlNode downloadPanel in downloadPanels)
            {
                string key = downloadPanel.SelectSingleNode(downloadPanel.XPath + "/div/div[1]/h2").InnerText;
                string value = downloadPanel.SelectSingleNode(downloadPanel.XPath + "/div/div[4]/div[2]/a[1]")
                    .GetAttributeValue("href", null);

                if (value == null) continue;
                mappings.Add(new MinecraftVersion(key).Version, value);

                if (key == "1.7.10") break;
            }

            return mappings;
        }
    }
}