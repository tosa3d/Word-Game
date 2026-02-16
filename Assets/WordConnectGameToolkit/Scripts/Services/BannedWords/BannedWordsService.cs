using System.Collections.Generic;
using UnityEngine;
using VContainer;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WordsToolkit.Scripts.Services.BannedWords
{
    public class BannedWordsService : IBannedWordsService
    {
        private readonly BannedWordsConfiguration configuration;

        [Inject]
        public BannedWordsService(BannedWordsConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public List<string> GetBannedWords(string languageCode)
        {
            return configuration.GetBannedWords(languageCode);
        }

        public bool IsWordBanned(string word, string languageCode)
        {
            var bannedWords = GetBannedWords(languageCode);
            return bannedWords.Contains(word.ToLower());
        }

        public void AddBannedWord(string word, string languageCode)
        {
            configuration.AddBannedWord(word.ToLower(), languageCode);
            SaveConfiguration();
        }

        public void RemoveBannedWord(string word, string languageCode)
        {
            configuration.RemoveBannedWord(word.ToLower(), languageCode);
            SaveConfiguration();
        }

        private void SaveConfiguration()
        {
#if UNITY_EDITOR
            if (configuration != null)
            {
                EditorUtility.SetDirty(configuration);
                AssetDatabase.SaveAssets();
                Debug.Log($"Saved BannedWordsConfiguration");
            }
#endif
        }
    }
}
