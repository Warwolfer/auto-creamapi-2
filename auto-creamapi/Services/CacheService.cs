using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public Task Initialize();

        //public Task UpdateCache();
        public IEnumerable<SteamApp> GetListOfAppsByName(string name);
        public SteamApp GetAppByName(string name);
        public SteamApp GetAppById(int appid);
        public Task<List<SteamApp>> GetListOfDlc(SteamApp steamApp, bool useSteamDb, bool ignoreUnknown);
    }

    public class CacheService : ICacheService
    {
        private const string CachePath = "steamapps.json";
        private const string SteamUri = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";

        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/87.0.4280.88 Safari/537.36";

        private HashSet<SteamApp> _cache = new HashSet<SteamApp>();

        public async Task Initialize()
        {
            MyLogger.Log.Information("Updating cache...");
            var updateNeeded = DateTime.Now.Subtract(File.GetLastWriteTimeUtc(CachePath)).TotalDays >= 1;
            string cacheString;
            if (updateNeeded)
            {
                cacheString = await UpdateCache().ConfigureAwait(false);
            }
            else
            {
                MyLogger.Log.Information("Cache already up to date!");
                // ReSharper disable once MethodHasAsyncOverload
                cacheString = File.ReadAllText(CachePath);
            }
            var steamApps = JsonSerializer.Deserialize<SteamApps>(cacheString);
            _cache = new HashSet<SteamApp>(steamApps.AppList.Apps);
            MyLogger.Log.Information("Loaded cache into memory!");
        }

        private static async Task<string> UpdateCache()
        {
            MyLogger.Log.Information("Getting content from API...");
            var client = new HttpClient();
            var httpCall = client.GetAsync(SteamUri);
            var response = await httpCall.ConfigureAwait(false);
            var readAsStringAsync = response.Content.ReadAsStringAsync();
            var responseBody = await readAsStringAsync.ConfigureAwait(false);
            MyLogger.Log.Information("Got content from API successfully. Writing to file...");

            await File.WriteAllTextAsync(CachePath, responseBody, Encoding.UTF8).ConfigureAwait(false);
            var cacheString = responseBody;
            MyLogger.Log.Information("Cache written to file successfully.");
            return cacheString;
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
            MyLogger.Log.Information("Trying to get app {Name}", name);
            var comparableName = Regex.Replace(name, Misc.SpecialCharsRegex, "").ToLower();
            var app = _cache.FirstOrDefault(x => x.CompareName(comparableName));
            if (app != null) MyLogger.Log.Information("Successfully got app {App}", app);
            return app;
        }

        public SteamApp GetAppById(int appid)
        {
            MyLogger.Log.Information("Trying to get app with ID {AppId}", appid);
            var app = _cache.FirstOrDefault(x => x.AppId.Equals(appid));
            if (app != null) MyLogger.Log.Information("Successfully got app {App}", app);
            return app;
        }

        public async Task<List<SteamApp>> GetListOfDlc(SteamApp steamApp, bool useSteamDb, bool ignoreUnknown)
        {
            MyLogger.Log.Debug("Start: GetListOfDlc");
            var dlcList = new List<SteamApp>();
            try
            {
                if (steamApp != null)
                {
                    var steamAppDetails = await AppDetails.GetAsync(steamApp.AppId).ConfigureAwait(false);
                    if (steamAppDetails != null)
                    {
                        MyLogger.Log.Debug("Type for Steam App {Name}: \"{Type}\"", steamApp.Name,
                            steamAppDetails.Type);
                        if (steamAppDetails.Type == "game" | steamAppDetails.Type == "demo")
                        {
                            steamAppDetails.DLC.ForEach(x =>
                            {
                                var result = _cache.FirstOrDefault(y => y.AppId.Equals(x));
                                if (result == null) return;
                                var dlcDetails = AppDetails.GetAsync(x).Result;
                                dlcList.Add(dlcDetails != null
                                    ? new SteamApp {AppId = dlcDetails.SteamAppId, Name = dlcDetails.Name}
                                    : new SteamApp {AppId = x, Name = $"Unknown DLC {x}"});
                            });

                            dlcList.ForEach(x => MyLogger.Log.Debug("{AppId}={Name}", x.AppId, x.Name));
                            MyLogger.Log.Information("Got DLC successfully...");

                            if (!useSteamDb) return dlcList;

                            // Get DLC from SteamDB
                            // Get Cloudflare cookie
                            // Scrape and parse HTML page
                            // Add missing to DLC list
                            var steamDbUri = new Uri($"https://steamdb.info/app/{steamApp.AppId}/dlc/");

                            var client = new HttpClient();
                            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

                            MyLogger.Log.Information("Get SteamDB App");
                            var httpCall = client.GetAsync(steamDbUri);
                            var response = await httpCall.ConfigureAwait(false);
                            MyLogger.Log.Debug("{Status}", httpCall.Status.ToString());
                            MyLogger.Log.Debug("{Boolean}", response.EnsureSuccessStatusCode().ToString());

                            var readAsStringAsync = response.Content.ReadAsStringAsync();
                            var responseBody = await readAsStringAsync.ConfigureAwait(false);
                            MyLogger.Log.Debug("{Status}", readAsStringAsync.Status.ToString());

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
                                    var query3 = element.QuerySelectorAll("td");
                                    var dlcName = query3 == null
                                        ? $"Unknown DLC {dlcId}"
                                        : query3[1].Text().Replace("\n", "").Trim();

                                    if (ignoreUnknown && dlcName.Contains("SteamDB Unknown App"))
                                    {
                                        MyLogger.Log.Information("Skipping SteamDB Unknown App {DlcId}", dlcId);
                                    }
                                    else
                                    {
                                        var dlcApp = new SteamApp {AppId = Convert.ToInt32(dlcId), Name = dlcName};
                                        var i = dlcList.FindIndex(x => x.AppId.Equals(dlcApp.AppId));
                                        if (i > -1)
                                        {
                                            if (dlcList[i].Name.Contains("Unknown DLC")) dlcList[i] = dlcApp;
                                        }
                                        else
                                        {
                                            dlcList.Add(dlcApp);
                                        }
                                    }
                                }

                                dlcList.ForEach(x => MyLogger.Log.Debug("{AppId}={Name}", x.AppId, x.Name));
                                MyLogger.Log.Information("Got DLC from SteamDB successfully...");
                            }
                            else
                            {
                                MyLogger.Log.Error("Could not get DLC from SteamDB!");
                            }
                        }
                        else
                        {
                            MyLogger.Log.Error("Could not get DLC: Steam App is not of type: \"Game\"");
                        }
                    }
                    else
                    {
                        MyLogger.Log.Error("Could not get DLC: Could not get Steam App details");
                    }
                }
                else
                {
                    MyLogger.Log.Error("Could not get DLC: Invalid Steam App");
                }

                //return dlcList;
            }
            catch (Exception e)
            {
                MyLogger.Log.Error("Could not get DLC!");
                Console.WriteLine(e);
            }

            return dlcList;
        }
    }
}