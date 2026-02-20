using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.System;
using WordsToolkit.Scripts.Utilities;

namespace WordsToolkit.Scripts.NLP
{
    public interface ICustomWordRepository
    {
        void AddWord(string word, string language = null);
        void InitWords(IEnumerable<string> words, string language = null);
        bool ContainsWord(string word, string language = null);
        void RemoveWord(string word, string language = null);
        float[] GetWordVector(string word, string language = null);
        bool AddExtraWord(string word);
        int GetExtraWordsCount();
        HashSet<string> GetExtraWords();
        void ClearExtraWords();
    }

    public class CustomWordRepository : ICustomWordRepository
    {
        private readonly string m_DefaultLanguage = "en";
        private readonly Dictionary<string, HashSet<string>> customWordsByLanguage = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, Dictionary<string, float[]>> customWordVectorsByLanguage = new Dictionary<string, Dictionary<string, float[]>>();
        private HashSet<string> extraWords = new HashSet<string>(StringComparer.Ordinal);

        public void AddWord(string word, string language = null)
        {
            language = language ?? m_DefaultLanguage;

            if (string.IsNullOrEmpty(word))
                return;

            // Persian: do NOT call ToLower() — it corrupts Persian characters
            // Use neutralization for consistent comparison
            word = PersianLanguageUtility.PrepareForComparison(word, language);

            if (!customWordsByLanguage.ContainsKey(language))
            {
                customWordsByLanguage[language] = new HashSet<string>();
            }

            customWordsByLanguage[language].Add(word);

            if (!customWordVectorsByLanguage.ContainsKey(language))
            {
                customWordVectorsByLanguage[language] = new Dictionary<string, float[]>();
            }
        }

        public void InitWords(IEnumerable<string> words, string language = null)
        {
            extraWords = LoadExtraWords();
            foreach (var word in words)
            {
                AddWord(word, language);
            }
        }

        public bool AddExtraWord(string word)
        {
            if (string.IsNullOrEmpty(word))
                return false;
            var addExtraWord = extraWords.Add(word);
            if (addExtraWord)
            {
                SaveExtraWords();
                PlayerPrefs.SetInt("ExtraWordsCollected", PlayerPrefs.GetInt("ExtraWordsCollected") + 1);
            }
            return addExtraWord;
        }

        private void SaveExtraWords()
        {
            PlayerPrefs.SetString("ExtraWords", string.Join(",", extraWords));
            PlayerPrefs.Save();
        }

        private HashSet<string> LoadExtraWords()
        {
            var extraWordsString = PlayerPrefs.GetString("ExtraWords", string.Empty);
            if (string.IsNullOrEmpty(extraWordsString))
                return new HashSet<string>(StringComparer.Ordinal);

            var wordsArray = extraWordsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return new HashSet<string>(wordsArray, StringComparer.Ordinal);
        }

        public int GetExtraWordsCount()
        {
            return extraWords.Count;
        }

        public HashSet<string> GetExtraWords()
        {
            return extraWords;
        }

        public void ClearExtraWords()
        {
            PlayerPrefs.DeleteKey("ExtraWords");
            PlayerPrefs.Save();
            extraWords.Clear();
            EventManager.GetEvent<string>(EGameEvent.ExtraWordClaimed).Invoke(null);
        }

        public bool ContainsWord(string word, string language = null)
        {
            language = language ?? m_DefaultLanguage;

            if (string.IsNullOrEmpty(word))
                return false;

            // Persian: do NOT call ToLower() — it corrupts Persian characters
            word = PersianLanguageUtility.PrepareForComparison(word, language);

            return customWordsByLanguage.ContainsKey(language) &&
                   customWordsByLanguage[language].Contains(word);
        }

        public void RemoveWord(string word, string language = null)
        {
            language = language ?? m_DefaultLanguage;

            if (string.IsNullOrEmpty(word))
                return;

            // Persian: do NOT call ToLower() — it corrupts Persian characters
            word = PersianLanguageUtility.PrepareForComparison(word, language);

            if (customWordsByLanguage.ContainsKey(language))
            {
                customWordsByLanguage[language].Remove(word);
            }

            if (customWordVectorsByLanguage.ContainsKey(language))
            {
                customWordVectorsByLanguage[language].Remove(word);
            }
        }

        public float[] GetWordVector(string word, string language = null)
        {
            language = language ?? m_DefaultLanguage;

            if (string.IsNullOrEmpty(word))
                return null;

            // Persian: do NOT call ToLower() — it corrupts Persian characters

            if (customWordVectorsByLanguage.ContainsKey(language) &&
                customWordVectorsByLanguage[language].ContainsKey(word))
            {
                return customWordVectorsByLanguage[language][word];
            }

            return null;
        }

    }
}
