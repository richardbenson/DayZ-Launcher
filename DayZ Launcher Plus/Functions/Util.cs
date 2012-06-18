using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Microsoft.Win32;
using SharpCompress.Common;
using SharpCompress.Reader;
using System.Text.RegularExpressions;
using System.Reflection;

namespace DZLP
{
    static class Util
    {
        private static Dictionary<string, string> strArmaOARegLocation = new Dictionary<string, string>()
        {
            { "x86", "SOFTWARE\\Bohemia Interactive Studio\\ArmA 2 OA"},
            { "x64", "SOFTWARE\\Wow6432Node\\Bohemia Interactive Studio\\ArmA 2 OA" }
        };

        private static Dictionary<string, string> strArmaRegLocation = new Dictionary<string, string>()
        {
            { "x86", "SOFTWARE\\Bohemia Interactive Studio\\ArmA 2"},
            { "x64", "SOFTWARE\\Wow6432Node\\Bohemia Interactive Studio\\ArmA 2" }
        };

        private static string strSteamRegPath = "SOFTWARE\\Valve\\Steam";

        private static Regex regServerList = new Regex(@"([\d\.]*):([\d]*) \\hostname\\(.*)\\gamever\\(.*)\\numplayers\\(\d*)\\maxplayers\\(\d*)");

        private static string strDefaultGSList = "-n arma2oapc -f \"mission LIKE '%DayZ%'\" -o 5 -X \"\\hostname\\gamever\\numplayers\\maxplayers\"";

        internal static void Elevate()
        {
            //We need Admin for almost everything in here
            // Needs UAC elevation for webmin to run
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool hasAdministrativeRight = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if (!hasAdministrativeRight)
            {
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.Verb = "runas";
                processInfo.FileName = Application.ExecutablePath;
                try
                {
                    Process.Start(processInfo);
                }
                catch
                {
                    Environment.Exit(0);
                }
                Environment.Exit(0);
            }
        }
        
        //Find where ARMA is installed
        internal static string GetArmaPath()
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey subKey = rk.OpenSubKey(strArmaRegLocation[GetBitness()]);
                if (subKey != null)
                {
                    return subKey.GetValue("MAIN").ToString();
                }
                else return "";
            }
            catch
            {
                return "";
            }
        }

        //Fine where OA is installed as could be different
        internal static string GetArmaOAPath()
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey subKey = rk.OpenSubKey(strArmaOARegLocation[GetBitness()]);
                if (subKey != null)
                {
                    return subKey.GetValue("MAIN").ToString();
                }
                else return "";
            }
            catch
            {
                return "";
            }
        }

        //What is the bitness of the system
        internal static string GetBitness()
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return "x86";
                case 8:
                    return "x64";
                default:
                    return "x86";
            }
        }

        //Fetch the modified date or etag from the registry
        internal static string GetEtag(string strURL)
        {
            if (Application.UserAppDataRegistry.GetValue(strURL) != null)
            {
                return Application.UserAppDataRegistry.GetValue(strURL).ToString();
            }
            else
            {
                return "";
            }
        }

        //Set the modified date into the registry
        internal static void SaveEtag(string strUrl, string strETag)
        {
            Application.UserAppDataRegistry.SetValue(strUrl, strETag);
        }

        //Extracts the given file to the right path
        internal static void ExtractFile(string strZip, string strDestination)
        {
            //Extract the file to the AddOns folder
            using (Stream stream = File.OpenRead(strZip))
            {
                var reader = ReaderFactory.Open(stream);
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        Console.WriteLine(reader.Entry.FilePath);
                        reader.WriteEntryToDirectory(strDestination, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                    }
                }
            }
        }

        internal static List<Objects.Server> GetServerList()
        {
            string path = Path.GetTempPath() + "gslist.exe";
            File.WriteAllBytes(path, Properties.Resources.gslist);

            Process prcGS = new Process();
            try
            {

            }
            catch (Exception e)
            {
            }

            if (File.Exists(path)) File.Delete(path);
        }
    }
}
