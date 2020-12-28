using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using auto_creamapi.Messenger;
using auto_creamapi.Utils;
using HttpProgress;
using MvvmCross.Plugin.Messenger;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace auto_creamapi.Services
{
    public interface IDownloadCreamApiService
    {
        /*public void Initialize();
        public Task InitializeAsync();*/
        public Task<string> Download(string username, string password);
        public void Extract(string filename);
    }

    public class DownloadCreamApiService : IDownloadCreamApiService
    {
        private const string ArchivePassword = "cs.rin.ru";

        //private string _filename;
        private readonly IMvxMessenger _messenger;
        //private DownloadWindow _wnd;

        public DownloadCreamApiService(IMvxMessenger messenger)
        {
            _messenger = messenger;
        }

        public async Task<string> Download(string username, string password)
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

            MyLogger.Log.Debug("Choosing first element from list...");
            var (filename, url) = archiveFileList.FirstOrDefault();
            //filename = filename;
            if (File.Exists(filename))
            {
                MyLogger.Log.Information($"{filename} already exists, skipping download...");
                return filename;
            }

            MyLogger.Log.Information("Start download...");
            var progress = new Progress<ICopyProgress>(
                x => _messenger.Publish(new ProgressMessage(this, "Downloading...", filename, x)));
            await using var fileStream = File.OpenWrite(filename);
            var task = client.GetAsync(url, fileStream, progress);
            var response = await task;
            if (task.IsCompletedSuccessfully)
                _messenger.Publish(new ProgressMessage(this, "Downloading...", filename, 1.0));
            MyLogger.Log.Information("Download done.");
            return filename;
        }

        public void Extract(string filename)
        {
            MyLogger.Log.Debug("Extract");
            MyLogger.Log.Information($@"Start extraction of ""{filename}""...");
            var options = new ReaderOptions {Password = ArchivePassword};
            var archive = ArchiveFactory.Open(filename, options);
            var expression1 = new Regex(@"nonlog_build\\steam_api(?:64)?\.dll");
            _messenger.Publish(new ProgressMessage(this, "Extracting...", filename, 1.0));
            foreach (var entry in archive.Entries)
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

            MyLogger.Log.Information("Extraction done!");
        }
    }
}