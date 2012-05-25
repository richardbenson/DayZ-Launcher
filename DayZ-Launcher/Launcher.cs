using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;
using SharpCompress.Common;
using SharpCompress.Reader;

namespace DayZ_Launcher
{
    public partial class Launcher : Form
    {
        string strDefaultCDN = "http://cdn.armafiles.info/";
        string strCurrentCDN;
        string strBasePath;

        Dictionary<string, string> strArmaRegLocation = new Dictionary<string, string>()
        {
            { "x86", "SOFTWARE\\Bohemia Interactive Studio\\ArmA 2 OA"},
            { "x64", "SOFTWARE\\Wow6432Node\\Bohemia Interactive Studio\\ArmA 2 OA" }
        };

        Queue<URLToDownload> downloadList = new Queue<URLToDownload>();

        public Launcher()
        {
            InitializeComponent();
        }

        public void Elevate()
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

        public void StartUp(object sender, EventArgs e)
        {

            //Set the CDN TODO: Make this changeable somehow
            strCurrentCDN = strDefaultCDN;

            //Fetch the Arma Path
            this.strBasePath = GetArmaPath();
            if (strBasePath == "")
            {
                bool bolPathOK = false;
                while (!bolPathOK)
                {
                    if (Application.UserAppDataRegistry.GetValue("Arma2Path") != null)
                    {
                        this.strBasePath = Application.UserAppDataRegistry.GetValue("Arma2Path").ToString();
                    }
                    else
                    {

                        MessageBox.Show("Unable to locate Arma2 install, please select install path (Contains Arma2oa.exe).");
                        DialogResult result = folderBrowserDialog1.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            this.strBasePath = folderBrowserDialog1.SelectedPath;
                        }
                        else
                        {
                            MessageBox.Show("DayZ requires Arma2 : Combined Operations");
                            Environment.Exit(1);
                        }
                    }

                    //Test the path is ok
                    if (File.Exists(strBasePath + @"\arma2oa.exe"))
                    {
                        Application.UserAppDataRegistry.SetValue("Arma2Path", this.strBasePath);
                        bolPathOK = true;
                    }
                }
            }
            lblStatus.Text = "Found Arma2.";

            //So we know Arma is installed, what about DayZ?
            UpdateDayZ();
        }

        private void LaunchDayZ()
        {
            lblStatus.Text = "DayZ up to date.";

            //Now Run Arma with the right switches
            lblStatus.Text = "Starting DayZ...";
            Process prcDayZ = new Process();

            prcDayZ.StartInfo.FileName = this.strBasePath + @"\arma2oa.exe";
            prcDayZ.StartInfo.Arguments = @"-mod=@dayz -nosplash";

            prcDayZ.Start();

            //Don't need us anymore, give back the precious resources
            Environment.Exit(0);
        }

        private void UpdateDayZ()
        {
            //Check or create base directories
            if (!Directory.Exists(this.strBasePath + @"\@DayZ")) Directory.CreateDirectory(this.strBasePath + @"\@DayZ");
            if (!Directory.Exists(this.strBasePath + @"\@DayZ\Addons")) Directory.CreateDirectory(this.strBasePath + @"\@DayZ\Addons");

            //Fetch the CDN page
            GetCurrentFileList();

            //Begin the checking and download
            DownloadNextFile();
        }

        private void GetCurrentFileList()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strCurrentCDN);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string html = reader.ReadToEnd();
                    Regex regex = new Regex("<a href=\".*\\.rar\">(?<name>.*)</a>");
                    MatchCollection matches = regex.Matches(html);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            if (match.Success)
                            {
                                URLToDownload thisFile = new URLToDownload
                                {
                                    URL = strCurrentCDN + match.Groups["name"].ToString(),
                                    File = this.strBasePath + @"\@DayZ\" + match.Groups["name"].ToString()
                                };
                                downloadList.Enqueue(thisFile);
                            }
                        }
                    }
                }
            }
        }

        //Find where ARMA is installed
        private string GetArmaPath()
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

        //What is the bitness of the system
        private string GetBitness()
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

        private string GetEtag(string strURL)
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

        private void SaveEtag(string strUrl, string strETag)
        {
            Application.UserAppDataRegistry.SetValue(strUrl, strETag);
        }

        private void UpdateStatus(string strMessage)
        {
            lblStatus.Invoke((Action)(() => lblStatus.Text = strMessage)); 
        }

        // Worker thread to grab the file
        private void DownloadFile(object sender, DoWorkEventArgs e)
        {
            URLToDownload args = e.Argument as URLToDownload;
            
            // the URL to download the file from
            string sUrlToReadFileFrom = args.URL;
            // the path to write the file to
            string sFilePathToWriteFileTo = args.File;

            // first, we need to get the exact size (in bytes) of the file we are downloading
            Uri url = new Uri(sUrlToReadFileFrom);
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            response.Close();
            // gets the size of the file in bytes
            Int64 iSize = response.ContentLength;

            // keeps track of the total bytes downloaded so we can update the progress bar
            Int64 iRunningByteTotal = 0;

            // use the webclient object to download the file
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                UpdateStatus("Downloading " + sUrlToReadFileFrom);
                // open the file at the remote URL for reading
                using (System.IO.Stream streamRemote = client.OpenRead(new Uri(sUrlToReadFileFrom)))
                {
                    // using the FileStream object, we can write the downloaded bytes to the file system
                    using (Stream streamLocal = new FileStream(sFilePathToWriteFileTo, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        // loop the stream and get the file into the byte buffer
                        int iByteSize = 0;
                        byte[] byteBuffer = new byte[iSize];
                        while ((iByteSize = streamRemote.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                        {
                            // write the bytes to the file system at the file path specified
                            streamLocal.Write(byteBuffer, 0, iByteSize);
                            iRunningByteTotal += iByteSize;

                            // calculate the progress out of a base "100"
                            double dIndex = (double)(iRunningByteTotal);
                            double dTotal = (double)byteBuffer.Length;
                            double dProgressPercentage = (dIndex / dTotal);
                            int iProgressPercentage = (int)(dProgressPercentage * 100);

                            // update the progress bar
                            bgDownloadWorker.ReportProgress(iProgressPercentage);
                        }

                        // clean up the file stream
                        streamLocal.Close();
                    }

                    // close the connection to the remote server
                    streamRemote.Close();
                }

                //Extract and delete the file
                UnRarFile(args);
            }
        }

        private void UnRarFile(URLToDownload args)
        {
            //Extract the file to the AddOns folder
            using (Stream stream = File.OpenRead(args.File))
            {
                UpdateStatus("Extracting " + args.File);
                var reader = ReaderFactory.Open(stream);
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        Console.WriteLine(reader.Entry.FilePath);
                        reader.WriteEntryToDirectory(this.strBasePath + @"\@DayZ\Addons", ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                    }
                }
            }
            //Delete the rar
            File.Delete(args.File);
        }

        private void UpdateProgress(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void DownloadComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            //Move on to the next file
            DownloadNextFile();
        }

        private void DownloadNextFile()
        {
            if (downloadList.Count > 0)
            {
                URLToDownload nextFile = downloadList.Dequeue();
                UpdateIfNeeded(nextFile);
            }
            else
            {
                LaunchDayZ();
            }
        }

        //Download update file only if needed
        private bool UpdateIfNeeded(URLToDownload getFile, string strType = "modified")
        {
            //Get our stored eTag for this URL
            string strETag = "";
            strETag = GetEtag(getFile.URL);

            UpdateStatus("Checking " + getFile.URL);

            try
            {
                //Set up a request and include our eTag
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(getFile.URL);
                request.Method = "GET";
                if (strETag != "")
                {
                    if (strType == "etag")
                    {
                        request.Headers[HttpRequestHeader.IfNoneMatch] = strETag;
                    }
                    else
                    {
                        try
                        {
                            strETag = strETag.Replace("UTC", "GMT"); //Fix for weird servers not sending correct formate datetime
                            request.IfModifiedSince = Convert.ToDateTime(strETag);
                        }
                        catch (Exception e) { MessageBox.Show("Unable to set modified date for URL: " + getFile.URL + "; " + e.Message); }
                    }
                }
                //Grab the response, will throw an exception if it's a 304 (not modified)
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();


                //We need to check if an elevation is required
                if (this.strBasePath.IndexOf("Program Files") > 0) Elevate();

                //Download the file
                bgDownloadWorker.RunWorkerAsync(getFile);
                response.Close();

                //Save the etag
                if (strType == "etag")
                {
                    if (response.Headers[HttpResponseHeader.ETag] != null) SaveEtag(getFile.URL, response.Headers[HttpResponseHeader.ETag]);
                }
                else
                {
                    if (response.Headers[HttpResponseHeader.LastModified] != null) SaveEtag(getFile.URL, response.Headers[HttpResponseHeader.LastModified]);
                }

                return true;
            }
            catch (System.Net.WebException ex)
            {
                if (ex.Response != null)
                {
                    using (HttpWebResponse response = ex.Response as HttpWebResponse)
                    {
                        if (response.StatusCode == HttpStatusCode.NotModified)
                        {
                            //304 means there is no update available
                            UpdateStatus(getFile.URL + " is up to date");
                            DownloadNextFile();
                            return false;
                        }
                        else
                        {
                            // Wasn't a 200, and wasn't a 304 so let the log know
                            MessageBox.Show(string.Format("Failed to check " + getFile.URL + ". Error Code: {0}"));
                            return false;
                        }
                    }
                }
                else return false;
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Failed to update " + getFile.URL + ". Error: {0}", e.Message));
                return false;
            }
        }

        class URLToDownload
        {
            public string URL { get; set; }
            public string File { get; set; }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.StartUp();

        }

        private void StartUp()
        {
        
        }
    }
}
