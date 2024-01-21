using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LaminariaCore_General.utils;
using glowberry.common;
using Open.Nat;

// ReSharper disable InconsistentNaming

namespace glowberry.utils
{
    /// <summary>
    /// This class contains a bunch of useful methods for interacting with the network
    /// </summary>
    public static class NetworkUtilExtensions
    {
        /// <summary>
        /// Recurrently tests the wifi connection every two seconds until it is established.
        /// </summary>
        /// <param name="label">The label to write status updated into</param>
        public static async Task RecurrentTestAsync(Label label = default)
        {
            while (true)
            {
                Logging.Logger.Info(@"Checking for an internet connection...");
                if (NetworkUtils.IsWifiConnected()) break;
                
                if (label != null) label.Text = @"Could not connect to the internet. Retrying...";
                await Task.Delay(2 * 1000);
            }
        }
    }
}