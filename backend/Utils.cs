using System.Text;
using System.Text.RegularExpressions;

namespace ChatBot;

public static class Utils
{
    public const int VECTOR_DIMENSIONS = 3072;

    private static readonly Dictionary<char, string> CyrillicToLatin = new()
    {
        // Russian Cyrillic
        {'А', "A"}, {'Б', "B"}, {'В', "V"}, {'Г', "G"}, {'Д', "D"},
        {'Е', "E"}, {'Ё', "Yo"}, {'Ж', "Zh"}, {'З', "Z"}, {'И', "I"},
        {'Й', "Y"}, {'К', "K"}, {'Л', "L"}, {'М', "M"}, {'Н', "N"},
        {'О', "O"}, {'П', "P"}, {'Р', "R"}, {'С', "S"}, {'Т', "T"},
        {'У', "U"}, {'Ф', "F"}, {'Х', "Kh"}, {'Ц', "Ts"}, {'Ч', "Ch"},
        {'Ш', "Sh"}, {'Щ', "Shch"}, {'Ъ', ""}, {'Ы', "Y"}, {'Ь', ""},
        {'Э', "E"}, {'Ю', "Yu"}, {'Я', "Ya"},
        {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"},
        {'е', "e"}, {'ё', "yo"}, {'ж', "zh"}, {'з', "z"}, {'и', "i"},
        {'й', "y"}, {'к', "k"}, {'л', "l"}, {'м', "m"}, {'н', "n"},
        {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"}, {'т', "t"},
        {'у', "u"}, {'ф', "f"}, {'х', "kh"}, {'ц', "ts"}, {'ч', "ch"},
        {'ш', "sh"}, {'щ', "shch"}, {'ъ', ""}, {'ы', "y"}, {'ь', ""},
        {'э', "e"}, {'ю', "yu"}, {'я', "ya"},
        // Kazakh specific
        {'Ә', "A"}, {'Ғ', "G"}, {'Қ', "Q"}, {'Ң', "N"}, {'Ө', "O"},
        {'Ұ', "U"}, {'Ү', "U"}, {'Һ', "H"}, {'І', "I"},
        {'ә', "a"}, {'ғ', "g"}, {'қ', "q"}, {'ң', "n"}, {'ө', "o"},
        {'ұ', "u"}, {'ү', "u"}, {'һ', "h"}, {'і', "i"}
    };

    public static string LatinizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var result = new StringBuilder(text.Length * 2);
        foreach (var ch in text)
        {
            if (CyrillicToLatin.TryGetValue(ch, out var latinChar))
                result.Append(latinChar);
            else
                result.Append(ch);
        }
        return result.ToString();
    }
    
    public static string RequireEnv(this WebApplicationBuilder builder, string key)
    {
        var v = builder.Configuration["Keys:" + key];
        if (string.IsNullOrWhiteSpace(v))
            throw new Exception($"Missing env var: {key}");
        return v!;
    }

    public static string ToUrlSafeId(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var s = title!.Trim();
        s = new string(s.Where(c => c <= 127).ToArray());
        s = Regex.Replace(s, @"[^\w\-]+", "_");
        s = Regex.Replace(s, "_{2,}", "_");
        s = s.Trim('_');

        if (string.IsNullOrEmpty(s))
            return Uri.EscapeDataString(title);

        return s;
    }
}
