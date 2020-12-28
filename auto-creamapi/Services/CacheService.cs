using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using auto_creamapi.Models;
using auto_creamapi.Utils;
using NinjaNye.SearchExtensions;
using SteamStorefrontAPI;

namespace auto_creamapi.Services
{
    public interface ICacheService
    {
        public List<string> Languages { get; }

        public Task Initialize();

        //public Task UpdateCache();
        public IEnumerable<SteamApp> GetListOfAppsByName(string name);
        public SteamApp GetAppByName(string name);
        public SteamApp GetAppById(int appid);
        public Task<List<SteamApp>> GetListOfDlc(SteamApp steamApp, bool useSteamDb);
    }

    public class CacheService : ICacheService
    {
        private const string CachePath = "steamapps.json";
        private const string SteamUri = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";

        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/87.0.4280.88 Safari/537.36";

        private const string SpecialCharsRegex = "[^0-9a-zA-Z]+";

        private List<SteamApp> _cache = new List<SteamApp>();

        /*private static readonly Lazy<CacheService> Lazy =
            new Lazy<CacheService>(() => new CacheService());

        public static CacheService Instance => Lazy.Value;*/

        public CacheService()
        {
            Languages = Misc.DefaultLanguages;
        }

        /*public async void Initialize()
        {
            //Languages = _defaultLanguages;
            await UpdateCache();
        }*/

        public List<string> Languages { get; }

        public async Task Initialize()
        {
            MyLogger.Log.Information("Updating cache...");
            var updateNeeded = DateTime.Now.Subtract(File.GetLastWriteTimeUtc(CachePath)).TotalDays >= 1;
            string cacheString;
            if (updateNeeded)
            {
                MyLogger.Log.Information("Getting content from API...");
                var client = new HttpClient();
                var httpCall = client.GetAsync(SteamUri);
                var response = await httpCall;
                var readAsStringAsync = response.Content.ReadAsStringAsync();
                var responseBody = await readAsStringAsync;
                MyLogger.Log.Information("Got content from API successfully. Writing to file...");

                await File.WriteAllTextAsync(CachePath, responseBody, Encoding.UTF8);
                cacheString = responseBody;
                MyLogger.Log.Information("Cache written to file successfully.");
            }
            else
            {
                MyLogger.Log.Information("Cache already up to date!");
                // ReSharper disable once MethodHasAsyncOverload
                cacheString = File.ReadAllText(CachePath);
            }

            var steamApps = JsonSerializer.Deserialize<SteamApps>(cacheString);
            _cache = steamApps.AppList.Apps;
            MyLogger.Log.Information("Loaded cache into memory!");
        }

        public IEnumerable<SteamApp> GetListOfAppsByName(string name)
        {
            var listOfAppsByName = _cache.Search(x => x.Name)
                .SetCulture(StringComparison.OrdinalIgnoreCase)
                .ContainingAll(name.Split(' '));
            return listOfAppsByName;
        }

        public SteamApp GetAppByName(string name)
        {
            MyLogger.Log.Information($"Trying to get app {name}");
            var app = _cache.Find(x =>
                Regex.Replace(x.Name, SpecialCharsRegex, "").ToLower()
                    .Equals(Regex.Replace(name, SpecialCharsRegex, "").ToLower()));
            if (app != null) MyLogger.Log.Information($"Successfully got app {app}");
            return app;
        }

        public SteamApp GetAppById(int appid)
        {
            MyLogger.Log.Information($"Trying to get app with ID {appid}");
            var app = _cache.Find(x => x.AppId.Equals(appid));
            if (app != null) MyLogger.Log.Information($"Successfully got app {app}");
            return app;
        }

        public async Task<List<SteamApp>> GetListOfDlc(SteamApp steamApp, bool useSteamDb)
        {
            MyLogger.Log.Information("Get DLC");
            var dlcList = new List<SteamApp>();
            if (steamApp != null)
            {
                var task = AppDetails.GetAsync(steamApp.AppId);
                var steamAppDetails = await task;
                steamAppDetails?.DLC.ForEach(x =>
                {
                    var result = _cache.Find(y => y.AppId.Equals(x)) ??
                                 new SteamApp {AppId = x, Name = $"Unknown DLC {x}"};
                    dlcList.Add(result);
                });

                dlcList.ForEach(x => MyLogger.Log.Debug($"{x.AppId}={x.Name}"));
                MyLogger.Log.Information("Got DLC successfully...");

                // Get DLC from SteamDB
                // Get Cloudflare cookie
                // Scrape and parse HTML page
                // Add missing to DLC list
                if (useSteamDb)
                {
                    var steamDbUri = new Uri($"https://steamdb.info/app/{steamApp.AppId}/dlc/");

                    /* var handler = new ClearanceHandler();
                
                    var client = new HttpClient(handler);
    
                    var content = client.GetStringAsync(steamDbUri).Result;
                    MyLogger.Log.Debug(content); */

                    var client = new HttpClient();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

                    MyLogger.Log.Information("Get SteamDB App");
                    var httpCall = client.GetAsync(steamDbUri);
                    var response = await httpCall;
                    MyLogger.Log.Debug(httpCall.Status.ToString());
                    MyLogger.Log.Debug(response.EnsureSuccessStatusCode().ToString());

                    var readAsStringAsync = response.Content.ReadAsStringAsync();
                    var responseBody = await readAsStringAsync;
                    MyLogger.Log.Debug(readAsStringAsync.Status.ToString());

                    var parser = new HtmlParser();
                    var doc = parser.ParseDocument(responseBody);
                    // Console.WriteLine(doc.DocumentElement.OuterHtml);

                    var query1 = doc.QuerySelector("#dlc");
                    if (query1 != null)
                    {
                        var query2 = query1.QuerySelectorAll(".app");
                        foreach (var element in query2)
                        {
                            var dlcId = element.GetAttribute("data-appid");
                            var dlcName = $"Unknown DLC {dlcId}";
                            var query3 = element.QuerySelectorAll("td");
                            if (query3 != null) dlcName = query3[1].Text().Replace("\n", "").Trim();

                            var dlcApp = new SteamApp {AppId = Convert.ToInt32(dlcId), Name = dlcName};
                            var i = dlcList.FindIndex(x => x.CompareId(dlcApp));
                            if (i > -1)
                            {
                                if (dlcList[i].Name.Contains("Unknown DLC")) dlcList[i] = dlcApp;
                            }
                            else
                            {
                                dlcList.Add(dlcApp);
                            }
                        }

                        dlcList.ForEach(x => MyLogger.Log.Debug($"{x.AppId}={x.Name}"));
                        MyLogger.Log.Information("Got DLC from SteamDB successfully...");
                    }
                    else
                    {
                        MyLogger.Log.Error("Could not get DLC from SteamDB1");
                    }
                }
            }
            else
            {
                MyLogger.Log.Error("Could not get DLC: Invalid Steam App");
            }

            return dlcList;
        }
    }
}