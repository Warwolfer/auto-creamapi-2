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
    public sealed class CreamConfigModel
    {

        // ReSharper disable once MemberCanBePrivate.Global
        public struct CreamConfig
        {
            public int AppId;
            public string Language;
            public bool UnlockAll;
            public bool ExtraProtection;
            public bool ForceOffline;
            public Dictionary<int, string> DlcList;
        }

        private CreamConfig _config;

        public CreamConfig Config => _config;

        private static readonly Lazy<CreamConfigModel> Lazy =
            new Lazy<CreamConfigModel>(() => new CreamConfigModel());

        public static CreamConfigModel Instance => Lazy.Value;
        
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        private string _configFilePath;

        private CreamConfigModel()
        {
            _config.DlcList = new Dictionary<int, string>();
            SetConfigData();
        }

        public void ReadFile(string configFilePath)
        {
            _configFilePath = configFilePath;
            if (File.Exists(configFilePath)) {
                MyLogger.Log.Information($"Config file found @ {configFilePath}, parsing...");
                var parser = new FileIniDataParser();
                var data = parser.ReadFile(_configFilePath, Encoding.UTF8);

                SetConfigData(); // clear previous config data
                _config.AppId = Convert.ToInt32(data["steam"]["appid"]);
                _config.Language = data["steam"]["language"];
                _config.UnlockAll = Convert.ToBoolean(data["steam"]["unlockall"]);
                _config.ExtraProtection = Convert.ToBoolean(data["steam"]["extraprotection"]);
                _config.ForceOffline = Convert.ToBoolean(data["steam"]["forceoffline"]);

                var dlcCollection = data["dlc"];
                foreach (var item in dlcCollection)
                {
                    _config.DlcList.Add(int.Parse(item.KeyName), item.Value);
                }
            }
            else
            {
                MyLogger.Log.Information($"Config file does not exist @ {configFilePath}, skipping...");
                SetConfigData();
            }
        }

        public void SaveFile()
        {
            var parser = new FileIniDataParser();
            var data = new IniData();

            data["steam"]["appid"] = _config.AppId.ToString();
            data["steam"]["language"] = _config.Language;
            data["steam"]["unlockall"] = _config.UnlockAll.ToString();
            data["steam"]["extraprotection"] = _config.ExtraProtection.ToString();
            data["steam"]["forceoffline"] = _config.ForceOffline.ToString();

            data.Sections.AddSection("dlc");
            foreach (var (key, value) in _config.DlcList)
            {
                data["dlc"].AddKey(key.ToString(), value);
            }

            parser.WriteFile(_configFilePath, data, Encoding.UTF8);
        }

        public void SetConfigData()
        {
            _config.AppId = 0;
            _config.Language = "";
            _config.UnlockAll = false;
            _config.ExtraProtection = false;
            _config.ForceOffline = false;
            _config.DlcList.Clear();
        }

        public void SetConfigData(int appId,
            string language,
            bool unlockAll,
            bool extraProtection,
            bool forceOffline,
            string dlcList)
        {
            _config.AppId = appId;
            _config.Language = language;
            _config.UnlockAll = unlockAll;
            _config.ExtraProtection = extraProtection;
            _config.ForceOffline = forceOffline;

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
            _config.DlcList.Clear();
            var expression = new Regex(@"(?<id>.*) *= *(?<name>.*)");
            using var reader = new StringReader(dlcList);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var match = expression.Match(line);
                if (match.Success)
                {
                    _config.DlcList.Add(int.Parse(match.Groups["id"].Value), match.Groups["name"].Value);
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
                      $"AppID: {_config.AppId}, " +
                      $"Language: {_config.Language}, " +
                      $"UnlockAll: {_config.UnlockAll}, " +
                      $"ExtraProtection: {_config.ExtraProtection}, " +
                      $"ForceOffline: {_config.ForceOffline}, " +
                      $"DLC ({_config.DlcList.Count}):\n[\n";
            if (_config.DlcList.Count > 0)
            {
                foreach (var (key, value) in _config.DlcList)
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
