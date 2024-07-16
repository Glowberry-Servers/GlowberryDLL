using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace glowberry.utils
{

    /// <summary>
    /// This class contains methods to interact Java runtimes.
    /// </summary>
    public class JavaUtils
    {
        
        /// <summary>
        /// Gets the major version of the Java runtime that is installed on the system.
        /// </summary>
        /// <param name="jarPath">The path to the jar file to check the Java version for</param>
        /// <returns>The major version of the Java runtime</returns>
        public static int DetectMajorVersion(string jarPath)
        {
            using ZipArchive archive = ZipFile.OpenRead(jarPath);

            // Locate the first .class file in the JAR
            var classEntry =
                archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".class", StringComparison.OrdinalIgnoreCase));

            if (classEntry == null)
                throw new FileNotFoundException("No class files found in the JAR file.");

            using Stream classStream = classEntry.Open();

            // Read the first 8 bytes of the class file
            byte[] header = new byte[8];
            classStream.Read(header, 0, 8);

            // The major version is stored in bytes 6 and 7 (0-based index)
            int majorVersion = header[6] << 8 | header[7];
            return majorVersion;
        }
        
        /// <summary>
        /// Based on an arithmetic progression of u5 + (n - 5)*1, pattern of the
        /// java versions mapped to the major version, we return the Java version
        /// for the given major version.
        /// </summary>
        /// <param name="majorVersion">The major version of the Java runtime</param>
        /// <returns>The java version associated with the major version</returns>
        public static int GetJavaVersion(int majorVersion) => majorVersion - 44;

        /// <summary>
        /// Gets the latest build download link for the given Java version, and if it doesn't exist,
        /// get the latest build download link for the next Java version.
        /// </summary>
        /// <param name="version">The java version to look up the download link for</param>
        /// <param name="tries">A counter to limit the amount of tries this method can perform toget the download links</param>
        /// <returns>The download link for the latest build of the specified java version</returns>
        public static async Task<string> GetClosestBuildDownloadLink(int version, int tries = 0)
        {
            // Set the API URL to get the latest build of the Java runtime
            string apiUrl = $"https://api.adoptium.net/v3/assets/latest/{version}/hotspot";
            using HttpClient client = new HttpClient();
            
            // Create a GET request to the API URL
            var response = await client.GetStringAsync(apiUrl);
            dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(response);
            
            if (tries >= 10) return null;
            if (jsonResponse is null || jsonResponse.Count < 0) return await GetClosestBuildDownloadLink(version + 1);
            
            // Iterate through the assets to find the correct download link
            foreach (var asset in jsonResponse)
            {
                string link = asset.binary.package.link;
                string os = asset.binary.os;
                string arch = asset.binary.architecture;
                string imageType = asset.binary.image_type;
                
                // If the asset is a Windows x64 zip, return the download link
                if (os.Equals("windows") && arch.Equals("x64") && imageType.Equals("jdk") && link.EndsWith(".zip"))
                    return link;
            }

            return null;
        }
    }
}