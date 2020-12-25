using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;
using auto_creamapi.Utils;
using HttpProgress;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace auto_creamapi.Services
{
    public interface IDownloadCreamApiService
    {
        public void Initialize();
        // public Task InitializeAsync();
        public Task DownloadAndExtract(string username, string password);
    }
    public class DownloadCreamApiService : IDownloadCreamApiService
    {
        private const string ArchivePassword = "cs.rin.ru";
        private static string _filename;
        //private static DownloadWindow _wnd;

        public DownloadCreamApiService()
        {
            
        }

        public void Initialize()
        {
            MyLogger.Log.Debug("DownloadCreamApiService: Initialize begin");
            if (File.Exists("steam_api.dll") && File.Exists("steam_api64.dll"))
            {
                MyLogger.Log.Information("Skipping download...");
            }
            else
            {
                MyLogger.Log.Information("Missing files, trying to download...");
                DownloadAndExtract(Secrets.ForumUsername, Secrets.ForumPassword).Start();
            }
            //await creamDllService.InitializeAsync();
            MyLogger.Log.Debug("DownloadCreamApiService: Initialize end");
        }

        public async Task InitializeAsync()
        {
            MyLogger.Log.Debug("DownloadCreamApiService: Initialize begin");
            if (File.Exists("steam_api.dll") && File.Exists("steam_api64.dll"))
            {
                MyLogger.Log.Information("Skipping download...");
            }
            else
            {
                MyLogger.Log.Information("Missing files, trying to download...");
                var downloadAndExtract = DownloadAndExtract(Secrets.ForumUsername, Secrets.ForumPassword);
                await downloadAndExtract;
                downloadAndExtract.Wait();
            }
            //await creamDllService.InitializeAsync();
            MyLogger.Log.Debug("DownloadCreamApiService: Initialize end");
        }

        public async Task DownloadAndExtract(string username, string password)
        {
            MyLogger.Log.Debug("DownloadAndExtract");
            //_wnd = new DownloadWindow();
            //_wnd.Show();
            var download = Download(username, password);
            await download;
            download.Wait();
            /*var extract = Extract();
            await extract;
            extract.Wait();*/
            var extract = Task.Run(Extract);
            await extract;
            extract.Wait();
            //_wnd.Close();
        }

        private static async Task Download(string username, string password)
        {
            MyLogger.Log.Debug("Download");
            var container = new CookieContainer();
            var handler = new HttpClientHandler {CookieContainer = container};
            var client = new HttpClient(handler);
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("redirect", "./ucp.php?mode=login"),
                new KeyValuePair<string, string>("login", "login")
            });
            MyLogger.Log.Debug("Download: post login");
            var response1 = await client.PostAsync("https://cs.rin.ru/forum/ucp.php?mode=login", formContent);
            MyLogger.Log.Debug($"Login Status Code: {response1.EnsureSuccessStatusCode().StatusCode.ToString()}");
            var cookie = container.GetCookies(new Uri("https://cs.rin.ru/forum/ucp.php?mode=login"))
                .FirstOrDefault(c => c.Name.Contains("_sid"));
            MyLogger.Log.Debug($"Login Cookie: {cookie}");
            var response2 = await client.GetAsync("https://cs.rin.ru/forum/viewtopic.php?t=70576");
            MyLogger.Log.Debug(
                $"Download Page Status Code: {response2.EnsureSuccessStatusCode().StatusCode.ToString()}");
            var content = response2.Content.ReadAsStringAsync();
            var contentResult = await content;

            var expression =
                new Regex(".*<a href=\"\\.(?<url>\\/download\\/file\\.php\\?id=.*)\">(?<filename>.*)<\\/a>.*");
            using var reader = new StringReader(contentResult);
            string line;
            var archiveFileList = new Dictionary<string, string>();
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var match = expression.Match(line);
                // ReSharper disable once InvertIf
                if (match.Success)
                {
                    archiveFileList.Add(match.Groups["filename"].Value,
                        $"https://cs.rin.ru/forum{match.Groups["url"].Value}");
                    MyLogger.Log.Debug(archiveFileList.LastOrDefault().Key);
                }
            }

            /*foreach (var (filename, url) in archiveFileList)
            {
                MyLogger.Log.Information($"Downloading file: {filename}");
                var fileResponse = await client.GetAsync(url);
                var download = fileResponse.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(filename, await download);
            }*/
            MyLogger.Log.Debug("Choosing first element from list...");
            var (filename, url) = archiveFileList.FirstOrDefault();
            _filename = filename;
            if (File.Exists(_filename))
            {
                MyLogger.Log.Information($"{_filename} already exists, skipping download...");
                return;
            }

            MyLogger.Log.Information("Start download...");
            /*await _wnd.FilenameLabel.Dispatcher.InvokeAsync(
                () => _wnd.FilenameLabel.Content = _filename, DispatcherPriority.Background);
            await _wnd.InfoLabel.Dispatcher.InvokeAsync(
                () => _wnd.InfoLabel.Content = "Downloading...", DispatcherPriority.Background);*/
            /*var fileResponse = await client.GetAsync(url);
            var download = fileResponse.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(filename, await download);
            MyLogger.Log.Information($"Download success? {download.IsCompletedSuccessfully}");*/
            var progress = new Progress<ICopyProgress>(
                x =>
                {
                    /*_wnd.PercentLabel.Dispatcher.Invoke(
                        () => _wnd.PercentLabel.Content = x.PercentComplete.ToString("P"),
                        DispatcherPriority.Background);
                    _wnd.ProgressBar.Dispatcher.Invoke(
                        () => _wnd.ProgressBar.Value = x.PercentComplete, DispatcherPriority.Background);*/
                });
            await using var fileStream = File.OpenWrite(_filename);
            var task = client.GetAsync(url, fileStream, progress);
            var response = await task;
            if (task.IsCompletedSuccessfully)
            {
                /*_wnd.PercentLabel.Dispatcher.Invoke(
                    () => _wnd.PercentLabel.Content = "100,00%", DispatcherPriority.Background);
                _wnd.ProgressBar.Dispatcher.Invoke(
                    () => _wnd.ProgressBar.Value = 1, DispatcherPriority.Background);*/
            }
        }

        private static void Extract()
        {
            MyLogger.Log.Debug("Extract");
            MyLogger.Log.Information("Start extraction...");
            var options = new ReaderOptions {Password = ArchivePassword};
            var archive = ArchiveFactory.Open(_filename, options);
            var expression1 = new Regex(@"nonlog_build\\steam_api(?:64)?\.dll");
            /*await _wnd.ProgressBar.Dispatcher.InvokeAsync(
                () => _wnd.ProgressBar.IsIndeterminate = true, DispatcherPriority.ContextIdle);
            await _wnd.FilenameLabel.Dispatcher.InvokeAsync(
                () => _wnd.FilenameLabel.Content = _filename, DispatcherPriority.ContextIdle);
            await _wnd.InfoLabel.Dispatcher.InvokeAsync(
                () => _wnd.InfoLabel.Content = "Extracting...", DispatcherPriority.ContextIdle);
            await _wnd.PercentLabel.Dispatcher.InvokeAsync(
                () => _wnd.PercentLabel.Content = "100%", DispatcherPriority.ContextIdle);*/
            foreach (var entry in archive.Entries)
            {
                // ReSharper disable once InvertIf
                if (!entry.IsDirectory && expression1.IsMatch(entry.Key))
                {
                    MyLogger.Log.Debug(entry.Key);
                    entry.WriteToDirectory(Directory.GetCurrentDirectory(), new ExtractionOptions
                    {
                        ExtractFullPath = false,
                        Overwrite = true
                    });
                }
            }

            MyLogger.Log.Information("Extraction done!");
        }
    }
}