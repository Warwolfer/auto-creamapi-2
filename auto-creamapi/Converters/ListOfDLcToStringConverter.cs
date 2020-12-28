using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using auto_creamapi.Models;
using auto_creamapi.Utils;
using MvvmCross.Converters;
using MvvmCross.Platforms.Wpf.Converters;

namespace auto_creamapi.Converters
{
    public class ListOfDLcToStringNativeConverter : MvxNativeValueConverter<ListOfDLcToStringConverter>
    {
    }

    public class ListOfDLcToStringConverter : MvxValueConverter<ObservableCollection<SteamApp>, string>
    {
        protected override string Convert(ObservableCollection<SteamApp> value, Type targetType, object parameter,
            CultureInfo culture)
        {
            MyLogger.Log.Debug("ListOfDLcToStringConverter: Convert");
            var dlcListToString = DlcListToString(value);
            return dlcListToString.GetType() == targetType ? dlcListToString : "";
        }

        protected override ObservableCollection<SteamApp> ConvertBack(string value, Type targetType, object parameter,
            CultureInfo culture)
        {
            MyLogger.Log.Debug("ListOfDLcToStringConverter: ConvertBack");
            var stringToDlcList = StringToDlcList(value);
            return stringToDlcList.GetType() == targetType ? stringToDlcList : new ObservableCollection<SteamApp>();
        }

        private static ObservableCollection<SteamApp> StringToDlcList(string value)
        {
            var result = new ObservableCollection<SteamApp>();
            var expression = new Regex(@"(?<id>.*) *= *(?<name>.*)");
            using var reader = new StringReader(value);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var match = expression.Match(line);
                if (match.Success)
                    result.Add(new SteamApp
                    {
                        AppId = int.Parse(match.Groups["id"].Value),
                        Name = match.Groups["name"].Value
                    });
            }

            return result;
        }

        private static string DlcListToString(ObservableCollection<SteamApp> value)
        {
            var result = "";
            //value.ForEach(x => result += $"{x}\n");
            foreach (var steamApp in value) result += $"{steamApp}\n";
            return result;
        }
    }
}