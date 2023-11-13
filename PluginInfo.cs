using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bhaptics
{
    public static class PluginInfo
    {
        public const string
            PLUGIN_GUID = "dev.evelyn344.bhaptics", // do not change this string EVER
            PLUGIN_NAME = "Bhaptics Support",
            PLUGIN_VERSION = "0.1"; // never use spaces

        public static readonly string
            PluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), // ".../BepInEx/plugins/Bhaptics"
            FullGamePath = Process.GetCurrentProcess().MainModule.FileName, // ".../ULTRAKILL/ULTRAKILL.exe"
            GamePath = Path.GetDirectoryName(FullGamePath); // ".../ULTRAKILL"

        public const string
            GithubRepoLink = "https://api.github.com/repos/blong344/Bhaptics_Ultrakill",
            FriendlyGithubRepoLink = "https://github.com/blong344/Bhaptics_Ultrakill";
    }
}
