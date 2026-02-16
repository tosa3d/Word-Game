using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WordsToolkit.Scripts.Levels
{
    // This class is responsible for finding valid words from given letters
    public class WordEmbeddingModel : MonoBehaviour
    {
        // Singleton instance
        private static WordEmbeddingModel _instance;
        public static WordEmbeddingModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<WordEmbeddingModel>();
                }
                return _instance;
            }
        }

        [Tooltip("Dictionary of words for each language")]
        public List<LanguageDictionary> dictionaries = new List<LanguageDictionary>();

        [global::System.Serializable]
        public class LanguageDictionary
        {
            [Tooltip("Language code")]
            public string languageCode;

            [Tooltip("Dictionary asset with words")]
            public TextAsset dictionaryAsset;

            [HideInInspector]
            public List<string> words = new List<string>();

            [HideInInspector]
            public bool isLoaded = false;
        }

        private void Awake()
        {
            // Set up singleton instance
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Load dictionaries
            foreach (var dictionary in dictionaries)
            {
                LoadDictionary(dictionary);
            }
        }

        private void LoadDictionary(LanguageDictionary dictionary)
        {
            if (dictionary.dictionaryAsset != null && !dictionary.isLoaded)
            {
                string content = dictionary.dictionaryAsset.text;
                string[] lines = content.Split(new[] { '\n', '\r' }, global::System.StringSplitOptions.RemoveEmptyEntries);
                
                dictionary.words = new List<string>(lines);
                dictionary.isLoaded = true;
                
                Debug.Log($"Loaded {dictionary.words.Count} words for language {dictionary.languageCode}");
            }
        }
        
        // Find words that can be formed using the given letters
        public List<string> FindWordsFromSymbols(string symbols, int maxCount, string languageCode = "en")
        {
            // Get the appropriate dictionary
            var dictionary = dictionaries.Find(d => d.languageCode == languageCode);
            
            // If we don't have the requested language, try to use English as fallback
            if (dictionary == null || !dictionary.isLoaded)
            {
                dictionary = dictionaries.Find(d => d.languageCode == "en");
                
                // If still no dictionary, return empty list
                if (dictionary == null || !dictionary.isLoaded)
                {
                    Debug.LogWarning($"No dictionary available for language {languageCode}");
                    return new List<string>();
                }
            }
            
            // Get valid words that can be formed from the given letters
            List<string> validWords = new List<string>();
            char[] symbolsArray = symbols.ToLower().ToCharArray();
            
            foreach (string word in dictionary.words)
            {
                if (CanFormWord(word, symbolsArray) && word.Length >= 3)
                {
                    validWords.Add(word);
                    
                    if (validWords.Count >= maxCount)
                        break;
                }
            }
            
            // Shuffle the results to avoid always getting the same words
            validWords = validWords.OrderBy(w => UnityEngine.Random.value).ToList();
            
            // Limit to requested count
            if (validWords.Count > maxCount)
            {
                validWords = validWords.Take(maxCount).ToList();
            }
            
            return validWords;
        }
        
        // Check if a word can be formed using the given letters
        private bool CanFormWord(string word, char[] availableLetters)
        {
            // Create a copy of the available letters
            Dictionary<char, int> letterCounts = new Dictionary<char, int>();
            
            foreach (char letter in availableLetters)
            {
                if (letterCounts.ContainsKey(letter))
                    letterCounts[letter]++;
                else
                    letterCounts[letter] = 1;
            }
            
            // Check each letter in the word
            foreach (char letter in word.ToLower())
            {
                if (!letterCounts.ContainsKey(letter) || letterCounts[letter] <= 0)
                    return false;
                    
                letterCounts[letter]--;
            }
            
            return true;
        }
        
        // Helper method to load a dictionary programmatically
        public void LoadDictionary(string languageCode, TextAsset dictionaryAsset)
        {
            // Check if we already have this language
            var existing = dictionaries.Find(d => d.languageCode == languageCode);
            
            if (existing != null)
            {
                // Update existing
                existing.dictionaryAsset = dictionaryAsset;
                existing.isLoaded = false;
                LoadDictionary(existing);
            }
            else
            {
                // Create new
                var newDict = new LanguageDictionary
                {
                    languageCode = languageCode,
                    dictionaryAsset = dictionaryAsset
                };
                
                dictionaries.Add(newDict);
                LoadDictionary(newDict);
            }
        }


    }
}
