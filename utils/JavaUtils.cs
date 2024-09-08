using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using glowberry.common;
using glowberry.common.handlers;
using glowberry.requests.content;
using LaminariaCore_General.common;
using static glowberry.common.configuration.Constants;

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
                archive.Entries.LastOrDefault(e => e.FullName.EndsWith(".class", StringComparison.OrdinalIgnoreCase));

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

        /// <summary>
        /// Automatically handles the entire detection and installation of the auto detected
        /// java runtimes
        /// </summary>
        /// <param name="serverJarPath">The path to the server.jar file to check</param>
        /// <param name="outputSystem">The output system used to print any output</param>
        /// <returns>A string containing the newly installed jar runtime path</returns>
        public static async Task<string> HandleAutoJavaDetection(string serverJarPath, MessageProcessingOutputHandler outputSystem)
        {
            // Gets the major java version number and the actual java version from it
            int majorVersion = DetectMajorVersion(serverJarPath);
            int javaVersion = GetJavaVersion(majorVersion);
            javaVersion = javaVersion <= 9 ? 8 : javaVersion;

            // Downloads the detected java version
            return await DownloadJavaVersion(javaVersion, outputSystem);
        }

        /// <summary>
        /// Downloads the specified java version into the runtimes directory
        /// </summary>
        /// <param name="javaVersion">The java version to download</param>
        /// <param name="outputSystem">The output system to log information into</param>
        /// <returns>The path of the downloaded version</returns>
        public static async Task<string> DownloadJavaVersion(int javaVersion, MessageProcessingOutputHandler outputSystem)
        {
            // Gets the java runtimes folder and the final path
            Section javaRuntimes = FileSystem.AddSection("runtime");

            // Checks if this specific JRE already exists
            string runtimeFolderPath = Path.Combine(javaRuntimes.SectionFullPath, $"jdk-{javaVersion}");
            if (Directory.Exists(runtimeFolderPath)) return runtimeFolderPath;

            // Gets the closest build download link for the java version
            string link = await GetClosestBuildDownloadLink(javaVersion);
            outputSystem.Write(Logging.Logger.Info($"Obtaining the Oracle Java Runtime: {link}") + Environment.NewLine);

            // Downloads the .zip file into the runtime folder
            string downloadPath = Path.Combine(javaRuntimes.SectionFullPath, $"download.zip");
            await FileDownloader.DownloadFileAsync(downloadPath, link);

            // Unzips the file and ensures that it is stored correctly
            outputSystem.Write(Logging.Logger.Info($"Extracting Java {javaVersion} resources to {runtimeFolderPath}") +
                               Environment.NewLine);
            ZipFile.ExtractToDirectory(downloadPath, runtimeFolderPath);

            string[] directories = Directory.GetDirectories(runtimeFolderPath);

            if (directories.Length == 1)
            {
                // Ensures a one-level deep folder structure for the java files
                foreach (string file in Directory.GetFiles(directories[0]))
                {
                    outputSystem.Write(Logging.Logger.Info($"Sorting {file} to {runtimeFolderPath}") +
                                       Environment.NewLine);
                    File.Move(file, Path.Combine(runtimeFolderPath, Path.GetFileName(file)));
                }

                // Ensures a one-level deep folder structure for the java directories
                foreach (string dir in Directory.GetDirectories(directories[0]))
                {
                    outputSystem.Write(Logging.Logger.Info($"Sorting {dir} to {runtimeFolderPath}") +
                                       Environment.NewLine);
                    Directory.Move(dir, Path.Combine(runtimeFolderPath, Path.GetFileName(dir)));
                }

                Directory.Delete(directories[0]);
            }

            File.Delete(downloadPath);
            return runtimeFolderPath;
        }
    }
}