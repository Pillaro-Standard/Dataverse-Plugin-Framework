using System;
using System.Globalization;
using System.Text;

namespace Pillaro.Dataverse.PluginFramework.Utilities;

public static class StringExtensions
{
    public static string RemoveDiacritics(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var chars = new char[normalized.Length];
        var index = 0;

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                chars[index++] = c;
        }

        return new string(chars, 0, index).Normalize(NormalizationForm.FormC);
    }

    public static string RemoveDiacriticsSafe(string text)
    {
        return text.RemoveDiacritics();
    }
}