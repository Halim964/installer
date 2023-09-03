
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace HDMS.Service
{
    public interface IUpdateManager
    {

    }

    public class UpdateManager
    {
        private const string LATEST_RELEASE_URL = "https://api.github.com/repos/Halim964/SIBL/releases/latest";
        private const string UNINSTALL_KEY = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{A5CADAA6-F985-43B3-9B0D-2038E0390FEC}_is1";
        private const string EXE_FILE_PATH = "G:\\Halim\\update-setup.exe";
        private static string DOWNLOAD_URL = "";

        public Action<string> OnCheckComplete;
        public Action<string> OnError;
        public Action<int> OnDownloadStatusUpdate;
        public Action OnDownloadComplete;
        public Action OnInstallationComplete;

        public UpdateManager()
        {

        }

        public async Task CheckAndUpdate()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("SIBL/2.0.8");
                    client.Timeout = TimeSpan.FromMinutes(10);
                    HttpResponseMessage response = await client.GetAsync(LATEST_RELEASE_URL);

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();

                        Root data = JsonConvert.DeserializeObject<Root>(content);

                        //Console.WriteLine(data.tag_name + " " + data.assets[0].browser_download_url);
                        //Task.Run(() => {
                        //    int number = 1000;
                        //    int counter = 1;
                        //    while (number > 0)
                        //    {
                        //        Console.WriteLine($"{counter++} second has passed.");
                        //        //Thread.Sleep(1000);
                        //        number = number--;
                        //    }
                        //});
                        await Download();
                    }
                    else
                    {
                        Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }


        public async Task GetLatestVersion()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("SIBL/2.0.8");
                    client.Timeout = TimeSpan.FromMinutes(10);
                    HttpResponseMessage response = await client.GetAsync(LATEST_RELEASE_URL);

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();

                        Root data = JsonConvert.DeserializeObject<Root>(content);
                        DOWNLOAD_URL = data.assets[0].browser_download_url;
                        OnCheckComplete(data.tag_name);

                        //Console.WriteLine(data.tag_name + " " + data.assets[0].browser_download_url);
                        //Task.Run(() => {
                        //    int number = 1000;
                        //    int counter = 1;
                        //    while (number > 0)
                        //    {
                        //        Console.WriteLine($"{counter++} second has passed.");
                        //        //Thread.Sleep(1000);
                        //        number = number--;
                        //    }
                        //});
                    }
                    else
                    {
                        OnError("Version check failed");
                    }
                }
                catch (Exception ex)
                {
                    OnError("Version check failed");
                }
            }
        }

        public async Task Download()
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(10);
                   
                    HttpResponseMessage response = await httpClient.GetAsync(DOWNLOAD_URL);

                    if (response.IsSuccessStatusCode)
                    {
                        using (FileStream fs = File.Create(EXE_FILE_PATH))
                        {
                            await response.Content.CopyToAsync(fs);
                        }
                        OnDownloadComplete();
                    }
                    else
                    {
                        OnError($"Download failed with status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                OnError($"An error occurred: {ex.Message}");
            }

        }

        private HttpClient httpClient;
        private CancellationTokenSource cancellationTokenSource;
        public async void StartDownload()
        {

            httpClient = new HttpClient();
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Send an HTTP GET request and download the file in chunks
                HttpResponseMessage response = await httpClient.GetAsync(DOWNLOAD_URL, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token);

                using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                {
                    // Total file size in bytes
                    long? totalFileSize = response.Content.Headers.ContentLength;

                    using (FileStream fileStream = new FileStream(EXE_FILE_PATH, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        long bytesReadTotal = 0;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token)) > 0)
                        {
                            fileStream.Write(buffer, 0, bytesRead);
                            bytesReadTotal += bytesRead;

                            // Calculate and update progress percentage
                            int progressPercentage = (int)((double)bytesReadTotal / totalFileSize.Value * 100);
                            OnDownloadStatusUpdate(progressPercentage);
                        }
                    }
                }
                OnDownloadComplete();

            }
            catch (HttpRequestException ex)
            {

            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                // Clean up resources
                httpClient.Dispose();
                cancellationTokenSource.Dispose();
            }
        }

        public void Install()
        {
            try
            {
                if (File.Exists(EXE_FILE_PATH))
                {
                    Process installerProcess = new Process();
                    installerProcess.StartInfo.FileName = EXE_FILE_PATH;
                    installerProcess.StartInfo.UseShellExecute = true;
                    installerProcess.StartInfo.Arguments = "/VERYSILENT";
                    installerProcess.Start();

                    installerProcess.WaitForExit();

                    OnInstallationComplete();
                }
                else
                {
                    OnError("Setup in not complete");
                }
            }
            catch (Exception ex)
            {
                OnError("Setup in not complete");
            }
        }

        public static string CheckInstalledVersion()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(UNINSTALL_KEY))
            {
                if (key != null)
                {
                    string displayName = key.GetValue("DisplayName") as string;
                    string displayVersion = key.GetValue("DisplayVersion") as string;

                    if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(displayVersion))
                    {
                        return displayVersion;
                    }
                    else
                    {
                        return "Unknown";
                    }
                }
                else
                {
                    return "Unknown";
                }
            }
        }

        public void CompareVersion(string versionStr1, string versionStr2)
        {
            Version version1 = new Version(versionStr2);
            Version version2 = new Version(versionStr2);
            int comparison = version1.CompareTo(version2);
            //if (comparison < 0)
            //{
            //    Console.WriteLine(versionString2 + " is newer than " + versionString1);
            //}
            //else if (comparison > 0)
            //{
            //    Console.WriteLine(versionString1 + " is newer than " + versionString2);
            //}
            //else
            //{
            //    Console.WriteLine(versionString1 + " and " + versionString2 + " are the same");
            //}
        }

        public static bool HasUpdate(string installedVersion, string LatestVersion)
        {
            if (installedVersion == "Unknown") return true;
            Version version1 = new Version(installedVersion);
            Version version2 = new Version(LatestVersion);
            int comparison = version1.CompareTo(version2);
            return comparison < 0;
        }

    }

}
public class Asset
{
    public string url { get; set; }
    public int id { get; set; }
    public string node_id { get; set; }
    public string name { get; set; }
    public object label { get; set; }
    public Uploader uploader { get; set; }
    public string content_type { get; set; }
    public string state { get; set; }
    public int size { get; set; }
    public int download_count { get; set; }
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
    public string browser_download_url { get; set; }
}

public class Author
{
    public string login { get; set; }
    public int id { get; set; }
    public string node_id { get; set; }
    public string avatar_url { get; set; }
    public string gravatar_id { get; set; }
    public string url { get; set; }
    public string html_url { get; set; }
    public string followers_url { get; set; }
    public string following_url { get; set; }
    public string gists_url { get; set; }
    public string starred_url { get; set; }
    public string subscriptions_url { get; set; }
    public string organizations_url { get; set; }
    public string repos_url { get; set; }
    public string events_url { get; set; }
    public string received_events_url { get; set; }
    public string type { get; set; }
    public bool site_admin { get; set; }
}

public class Root
{
    public string url { get; set; }
    public string assets_url { get; set; }
    public string upload_url { get; set; }
    public string html_url { get; set; }
    public int id { get; set; }
    public Author author { get; set; }
    public string node_id { get; set; }
    public string tag_name { get; set; }
    public string target_commitish { get; set; }
    public string name { get; set; }
    public bool draft { get; set; }
    public bool prerelease { get; set; }
    public DateTime created_at { get; set; }
    public DateTime published_at { get; set; }
    public List<Asset> assets { get; set; }
    public string tarball_url { get; set; }
    public string zipball_url { get; set; }
    public string body { get; set; }
}

public class Uploader
{
    public string login { get; set; }
    public int id { get; set; }
    public string node_id { get; set; }
    public string avatar_url { get; set; }
    public string gravatar_id { get; set; }
    public string url { get; set; }
    public string html_url { get; set; }
    public string followers_url { get; set; }
    public string following_url { get; set; }
    public string gists_url { get; set; }
    public string starred_url { get; set; }
    public string subscriptions_url { get; set; }
    public string organizations_url { get; set; }
    public string repos_url { get; set; }
    public string events_url { get; set; }
    public string received_events_url { get; set; }
    public string type { get; set; }
    public bool site_admin { get; set; }
}