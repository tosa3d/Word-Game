using System;
using System.Collections.Generic;
using System.Linq;

namespace WordsToolkit.Scripts.Utilities
{
    public static class PersianLanguageUtility
    {
        public static bool IsRTL(string langCode)
        {
            if (string.IsNullOrEmpty(langCode)) return false;
            langCode = langCode.ToLower();
            return langCode == "fa" || langCode == "ar" || langCode == "ps" || langCode == "ur";
        }

        public static string Normalize(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            return input.Replace("ي", "ی")   // Arabic Yeh to Persian Ye
                        .Replace("ك", "ک")   // Arabic Keh to Persian Keh
                        .Replace("ۀ", "ه")   // Heh with Yeh to Heh
                        .Replace("آ", "ا")   // Alef with Mad to Alef (optional but common for simplify)
                        .Replace("\u200C", "") // Remove ZWNJ
                        .Trim();
        }

        public static string Reverse(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            char[] arr = input.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        public static string PrepareForComparison(string input, string langCode)
        {
            if (string.IsNullOrEmpty(input)) return input;

            if (IsRTL(langCode))
            {
                return Normalize(input);
            }
            
            return input.ToLower();
        }
    }
}
