using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using auto_creamapi.Models;
using auto_creamapi.Utils;
using IniParser;
using IniParser.Model;

namespace auto_creamapi.Services
{
    public interface ICreamConfigService
    {
        public CreamConfig Config { get; }
        public void Initialize();
        public void ReadFile(string configFilePath);
        public void SaveFile();

        public void SetConfigData(int appId,
            string language,
            bool unlockAll,
            bool extraProtection,
            bool forceOffline,
            string dlcList);

        public void SetConfigData(int appId,
            string language,
            bool unlockAll,
            bool extraProtection,
            bool forceOffline,
            List<SteamApp> dlcList);

        public void SetConfigData(int appId,
            string language,
            bool unlockAll,
            bool extraProtection,
            bool forceOffline,
            IEnumerable<SteamApp> dlcList);

        public bool ConfigExists();
    }

    public class CreamConfigService : ICreamConfigService
    {
        private string _configFilePath;

        public CreamConfig Config { get; private set; }

        public void Initialize()
        {
            //await Task.Run(() =>
            //{
            //MyLogger.Log.Debug("CreamConfigService: init start");
            Config = new CreamConfig();
            ResetConfigData();
            //MyLogger.Log.Debug("CreamConfigService: init end");
            //});
        }

        public void ReadFile(string configFilePath)
        {
            _configFilePath = configFilePath;
            if (File.Exists(configFilePath))
            {
                MyLogger.Log.Information("Config file found @ {ConfigFilePath}, parsing...", configFilePath);
                var parser = new FileIniDataParser();
                var data = parser.ReadFile(_configFilePath, Encoding.UTF8);

                ResetConfigData(); // clear previous config data
                Config.AppId = Convert.ToInt32(data["steam"]["appid"]);
                Config.Language = data["steam"]["language"];
                Config.UnlockAll = Convert.ToBoolean(data["steam"]["unlockall"]);
                Config.ExtraProtection = Convert.ToBoolean(data["steam"]["extraprotection"]);
                Config.ForceOffline = Convert.ToBoolean(data["steam"]["forceoffline"]);

                var dlcCollection = data["dlc"];
                foreach (var item in dlcCollection)
                    //Config.DlcList.Add(int.Parse(item.KeyName), item.Value);
                    Config.DlcList.Add(new SteamApp {AppId = int.Parse(item.KeyName), Name = item.Value});
            }
            else
            {
                MyLogger.Log.Information("Config file does not exist @ {ConfigFilePath}, skipping...", configFilePath);
                ResetConfigData();
            }
        }

        public void SaveFile()
        {
            var parser = new FileIniDataParser();
            var data = new IniData();

            data["steam"]["appid"] = Config.AppId.ToString();
            data["steam"]["language"] = Config.Language;
            data["steam"]["unlockall"] = Config.UnlockAll.ToString();
            data["steam"]["extraprotection"] = Config.ExtraProtection.ToString();
            data["steam"]["forceoffline"] = Config.ForceOffline.ToString();

            data.Sections.AddSection("dlc");
            Config.DlcList.ForEach(x => data["dlc"].AddKey(x.AppId.ToString(), x.Name));
            /*foreach (var steamApp in Config.DlcList)
            {
                data["dlc"].AddKey(key.ToString(), value);
            }*/

            parser.WriteFile(_configFilePath, data, Encoding.UTF8);
        }

        public void SetConfigData(int appId,
            string language,
            bool unlockAll,
            bool extraProtection,
            bool forceOffline,
            string dlcList)
        {
            Config.AppId = appId;
            Config.Language = language;
            Config.UnlockAll = unlockAll;
            Config.ExtraProtection = extraProtection;
            Config.ForceOffline = forceOffline;
            SetDlcFromString(dlcList);
        }

        public void SetConfigData(int appId,
            string language,
            bool unlockAll,
            bool extraProtection,
            bool forceOffline,
            List<SteamApp> dlcList)
        {
            Config.AppId = appId;
            Config.Language = language;
            Config.UnlockAll = unlockAll;
            Config.ExtraProtection = extraProtection;
            Config.ForceOffline = forceOffline;
            Config.DlcList = dlcList;
        }

        public void SetConfigData(int appId,
            string language,
            bool unlockAll,
            bool extraProtection,
            bool forceOffline,
            IEnumerable<SteamApp> dlcList)
        {
            Config.AppId = appId;
            Config.Language = language;
            Config.UnlockAll = unlockAll;
            Config.ExtraProtection = extraProtection;
            Config.ForceOffline = forceOffline;
            Config.DlcList = new List<SteamApp>(dlcList);
        }

        public bool ConfigExists()
        {
            return File.Exists(_configFilePath);
        }

        private void ResetConfigData()
        {
            Config.AppId = -1;
            Config.Language = Misc.DefaultLanguageSelection;
            Config.UnlockAll = false;
            Config.ExtraProtection = false;
            Config.ForceOffline = false;
            Config.DlcList.Clear();
        }

        private void SetDlcFromString(string dlcList)
        {
            Config.DlcList.Clear();
            var expression = new Regex(@"(?<id>.*) *= *(?<name>.*)");
            using var reader = new StringReader(dlcList);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var match = expression.Match(line);
                if (match.Success)
                    Config.DlcList.Add(
                        new SteamApp {AppId = int.Parse(match.Groups["id"].Value), Name = match.Groups["name"].Value});
            }
        }
        /*private void SetDlcFromAppList(List<SteamApp> dlcList)
        {
            Config.DlcList.Clear();
            dlcList.ForEach(x => Config.DlcList.Add(x.AppId, x.Name));
        }*/

        public override string ToString()
        {
            var str = $"INI file: {_configFilePath}\n{Config}";

            return str;
        }
    }
}