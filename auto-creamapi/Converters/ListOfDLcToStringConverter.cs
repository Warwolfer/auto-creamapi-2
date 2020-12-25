using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using auto_creamapi.Models;
using MvvmCross.Converters;

namespace auto_creamapi.Converters
{
    public class ListOfDLcToStringConverter : MvxValueConverter<List<SteamApp>, string>
    {
        protected override string Convert(List<SteamApp> value, Type targetType, object parameter, CultureInfo culture)
        {
            var dlcListToString = DlcListToString(value);
            return dlcListToString.GetType() == targetType ? dlcListToString : "";
        }

        protected override List<SteamApp> ConvertBack(string value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringToDlcList = StringToDlcList(value);
            return stringToDlcList.GetType() == targetType ? stringToDlcList : new List<SteamApp>();
        }

        private static List<SteamApp> StringToDlcList(string value)
        {
            var result = new List<SteamApp>();
            var expression = new Regex(@"(?<id>.*) *= *(?<name>.*)");
            using var reader = new StringReader(value);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var match = expression.Match(line);
                if (match.Success)
                {
                    result.Add(new SteamApp
                    {
                        AppId = int.Parse(match.Groups["id"].Value),
                        Name = match.Groups["name"].Value
                    });
                }
            }

            return result;
        }

        private static string DlcListToString(List<SteamApp> value)
        {
            var result = "";
            value.ForEach(x => result += $"{x}\n");
            return result;
        }
    }
}