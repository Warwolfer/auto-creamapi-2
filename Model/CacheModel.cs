using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using auto_creamapi.POCOs;
using auto_creamapi.Utils;
using NinjaNye.SearchExtensions;
using SteamStorefrontAPI;

namespace auto_creamapi.Model
{
    public class CacheModel
    {
        private const string CachePath = "steamapps.json";
        private const string SteamUri = "https://api.steampowered.com/ISteamApps/GetAppList/v2/";
        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/87.0.4280.88 Safari/537.36";
        private static List<SteamApp> _cache = new List<SteamApp>();

        public readonly List<string> Languages = new List<string>(new[]
        {
            "arabic",
            "bulgarian",
            "schinese",
            "tchinese",
            "czech",
            "danish",
            "dutch",
            "english",
            "finnish",
            "french",
            "german",
            "greek",
            "hungarian",
            "italian",
            "japanese",
            "koreana",
            "norwegian",
            "polish",
            "portuguese",
            "brazilian",
            "romanian",
            "russian",
            "spanish",
            "latam",
            "swedish",
            "thai",
            "turkish",
            "ukrainian",
            "vietnamese"
        });

        private static readonly Lazy<CacheModel> Lazy =
            new Lazy<CacheModel>(() => new CacheModel());

        public static CacheModel Instance => Lazy.Value;

        private CacheModel()
        {
            UpdateCache();
        }

        private static void UpdateCache()
        {
            var updateNeeded = DateTime.Now.Subtract(File.GetCreationTimeUtc(CachePath)).TotalDays >= 1;
            string cacheString;
            if (updateNeeded)
            {
                MyLogger.Log.Information("Updating cache...");
                var client = new HttpClient();
                var httpCall = client.GetAsync(SteamUri);
                var response = httpCall.Result;
                var readAsStringAsync = response.Content.ReadAsStringAsync();
                var responseBody = readAsStringAsync.Result;

                /*var writeAllTextAsync = File.WriteAllTextAsync(CachePath, responseBody, Encoding.UTF8);
                writeAllTextAsync.RunSynchronously();*/
                File.WriteAllText(CachePath, responseBody, Encoding.UTF8);
                cacheString = responseBody;
            }
            else
            {
                MyLogger.Log.Information("Cache already up to date!");
                cacheString = File.ReadAllText(CachePath);
            }
            
            var steamApps = JsonSerializer.Deserialize<SteamApps>(cacheString);
            _cache = steamApps.AppList.Apps;
            MyLogger.Log.Information("Loaded cache into memory!");
        }

        public EnumerableStringSearch<SteamApp> GetListOfAppsByName(string name)
        {
            var listOfAppsByName = _cache.Search(x => x.Name)
                .SetCulture(StringComparison.OrdinalIgnoreCase)
                .ContainingAll(name.Split(' '));
            return listOfAppsByName;
        }

        public SteamApp GetAppByName(string name)
        {
            MyLogger.Log.Information($"Trying to get app {name}");
            var app = _cache.Find(x => x.Name.ToLower().Equals(name.ToLower()));
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
                            if (query3 != null)
                            {
                                dlcName = query3[1].Text().Replace("\n", "").Trim();
                            }

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
                MyLogger.Log.Error($"Could not find game: {steamApp}");
            }

            return dlcList;
        }
    }
}