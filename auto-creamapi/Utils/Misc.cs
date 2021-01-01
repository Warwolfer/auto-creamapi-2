using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace auto_creamapi.Utils
{
    public class Misc
    {
        public const string SpecialCharsRegex = "[^0-9a-zA-Z]+";
        public const string DefaultLanguageSelection = "english";
        public static readonly ObservableCollection<string> DefaultLanguages = new ObservableCollection<string>(new[]
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
    }
}