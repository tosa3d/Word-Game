using System;
using System.Collections.Generic;
using UnityEngine;

namespace WordsToolkit.Scripts.Services.BannedWords
{
    [Serializable]
    public class LanguageBannedWords
    {
        public string languageCode;
        public List<string> bannedWords = new List<string>();
    }

    public class BannedWordsConfiguration : ScriptableObject
    {
        public List<LanguageBannedWords> bannedWordsByLanguage = new List<LanguageBannedWords>();

        public List<string> GetBannedWords(string languageCode)
        {
            var languageData = bannedWordsByLanguage.Find(x => x.languageCode == languageCode);
            return languageData?.bannedWords ?? new List<string>();
        }

        public void AddBannedWord(string word, string languageCode)
        {
            var languageData = bannedWordsByLanguage.Find(x => x.languageCode == languageCode);
            if (languageData == null)
            {
                languageData = new LanguageBannedWords { languageCode = languageCode };
                bannedWordsByLanguage.Add(languageData);
            }
            if (!languageData.bannedWords.Contains(word))
            {
                languageData.bannedWords.Add(word);
            }
        }

        public void RemoveBannedWord(string word, string languageCode)
        {
            var languageData = bannedWordsByLanguage.Find(x => x.languageCode == languageCode);
            languageData?.bannedWords.Remove(word);
        }
    }
}
