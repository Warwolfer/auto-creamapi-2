using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using auto_creamapi.Utils;
using IniParser;
using IniParser.Model;

namespace auto_creamapi.Model
{
    public class CreamConfig
    {
        public int AppId { get; set; }
        public string Language { get; set; }
        public bool UnlockAll { get; set; }
        public bool ExtraProtection { get; set; }
        public bool ForceOffline { get; set; }
        public Dictionary<int, string> DlcList { get; }

        public CreamConfig()
        {
            DlcList = new Dictionary<int, string>();
        }
    }
    public sealed class CreamConfigModel
    {
        public CreamConfig Config { get; }

        private static readonly Lazy<CreamConfigModel> Lazy =
            new Lazy<CreamConfigModel>(() => new CreamConfigModel());

        public static CreamConfigModel Instance => Lazy.Value;
        
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        private string _configFilePath;

        private CreamConfigModel()
        {
            Config = new CreamConfig();
            ResetConfigData();
        }

        public void ReadFile(string configFilePath)
        {
            _configFilePath = configFilePath;
            if (File.Exists(configFilePath)) {
                MyLogger.Log.Information($"Config file found @ {configFilePath}, parsing...");
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
                {
                    Config.DlcList.Add(int.Parse(item.KeyName), item.Value);
                }
            }
            else
            {
                MyLogger.Log.Information($"Config file does not exist @ {configFilePath}, skipping...");
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
            foreach (var (key, value) in Config.DlcList)
            {
                data["dlc"].AddKey(key.ToString(), value);
            }

            parser.WriteFile(_configFilePath, data, Encoding.UTF8);
        }

        private void ResetConfigData()
        {
            Config.AppId = 0;
            Config.Language = "";
            Config.UnlockAll = false;
            Config.ExtraProtection = false;
            Config.ForceOffline = false;
            Config.DlcList.Clear();
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

        /*private void SetConfigData(int appId,
            string language,
            bool unlockAll,
            bool extraProtection,
            bool forceOffline,
            List<POCOs.App> dlcList)
        {
            _config.AppId = appId;
            _config.Language = language;
            _config.UnlockAll = unlockAll;
            _config.ExtraProtection = extraProtection;
            _config.ForceOffline = forceOffline;

            SetDlcFromAppList(dlcList);
        }*/

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
                {
                    Config.DlcList.Add(int.Parse(match.Groups["id"].Value), match.Groups["name"].Value);
                }
            }
        }

        /*private void SetDlcFromAppList(List<POCOs.App> dlcList)
        {
            _config.DlcList.Clear();
            dlcList.ForEach(x => _config.DlcList.Add(x.AppId, x.Name));
        }*/

        public override string ToString()
        {
            var str = $"INI file: {_configFilePath}, " +
                      $"AppID: {Config.AppId}, " +
                      $"Language: {Config.Language}, " +
                      $"UnlockAll: {Config.UnlockAll}, " +
                      $"ExtraProtection: {Config.ExtraProtection}, " +
                      $"ForceOffline: {Config.ForceOffline}, " +
                      $"DLC ({Config.DlcList.Count}):\n[\n";
            if (Config.DlcList.Count > 0)
            {
                foreach (var (key, value) in Config.DlcList)
                {
                    str += $"  {key}={value},\n";
                }

            }
            str += "]";

            return str;
        }

        public bool ConfigExists()
        {
            return File.Exists(_configFilePath);
        }
    }
}
