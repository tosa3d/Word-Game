using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using WordsToolkit.Scripts.Settings;

namespace WordsToolkit.Scripts.Levels
{
    [Serializable]
    public class SerializableWordPlacement
    {
        public string word;
        public int wordNumber;
        public Vector2Int startPosition;
        public bool isHorizontal;

        // Add a flag to indicate if this is a special item (icon) instead of a word
        public bool isSpecialItem = false;

        // Path to the icon asset if this is a special item
        public string specialItemPath = "";
    }

    [Serializable]
    public class SerializableSpecialItem
    {
        public Vector2Int position;
        public string itemPath;
    }

    [Serializable]
    public class SerializableCrosswordData
    {
        public int columns;
        public int rows;
        [NonSerialized] public char[,] grid;
        public List<SerializableWordPlacement> placements = new List<SerializableWordPlacement>();
        public List<SerializableSpecialItem> specialItems = new List<SerializableSpecialItem>(); // Separate storage for special items
        public Vector2Int minBounds;
        public Vector2Int maxBounds;

        // Helper method for serializing grid since Unity can't serialize 2D arrays directly
        [SerializeField] private string serializedGrid;

        public void SerializeGrid()
        {
            if (grid == null) return;

            // Get actual grid dimensions
            int actualColumns = grid.GetLength(0);
            int actualRows = grid.GetLength(1);

            // Ensure we don't exceed the actual dimensions
            int safeColumns = Mathf.Min(columns, actualColumns);
            int safeRows = Mathf.Min(rows, actualRows);

            StringBuilder sb = new StringBuilder();
            for (int y = 0; y < safeRows; y++)
            {
                for (int x = 0; x < safeColumns; x++)
                {
                    char c = grid[x, y];
                    sb.Append(c == 0 ? ' ' : c);
                }
                // Add padding spaces if actual columns are less than specified
                for (int x = safeColumns; x < columns; x++)
                {
                    sb.Append(' ');
                }
                if (y < safeRows - 1)
                    sb.Append('|');
            }

            // Add padding rows if actual rows are less than specified
            for (int y = safeRows; y < rows; y++)
            {
                // Add a row of spaces
                for (int x = 0; x < columns; x++)
                {
                    sb.Append(' ');
                }
                if (y < rows - 1)
                    sb.Append('|');
            }

            serializedGrid = sb.ToString();
        }

        public void DeserializeGrid()
        {
            if (string.IsNullOrEmpty(serializedGrid) || columns <= 0 || rows <= 0) return;

            grid = new char[columns, rows];
            string[] rowStrings = serializedGrid.Split('|');
            for (int y = 0; y < Mathf.Min(rows, rowStrings.Length); y++)
            {
                string rowStr = rowStrings[y];
                for (int x = 0; x < Mathf.Min(columns, rowStr.Length); x++)
                {
                    char c = rowStr[x];
                    grid[x, y] = c == ' ' ? (char)0 : c;
                }
            }
        }
    }

    [global::System.Serializable]
    public class LanguageData
    {
        [Tooltip("Language code (e.g., 'en', 'fr', 'es')")]
        public string language;

        [Tooltip("Letters available for this level in this language")]
        public string letters;

        [Tooltip("Number of words to generate")]
        public int wordsAmount = 5;

        [Tooltip("List of words selected for this level in this language")]
        public string[] words;

        // Added field to store the crossword data
        public SerializableCrosswordData crosswordData;
    }

    [CreateAssetMenu(fileName = "Level", menuName ="WordConnectGameToolkit/Editor/Level")]
    public class Level : ScriptableObject
    {
        [Tooltip("Level number")]
        public int number = 1;

        [Tooltip("Background to use for this level")]
        public Sprite background;

        [Tooltip("Language-specific data for this level")]
        public List<LanguageData> languages = new List<LanguageData>();

        public ColorsTile colorsTile;
        [Tooltip("Generator parameters")]
        public int words = 5;
        public int letters = 5;

        [Tooltip("Minimum number of letters that each generated word should contain")]
        public int min = 3;

        [Tooltip("Maximum number of letters that each generated word should contain")]
        public int max = 6;

        [Range(0, 100)]
        [Tooltip("Probability (%) to generate longer words within the min-max range. 0% favors shorter words, 100% favors longer words")]
        public int difficulty = 0;

        [Tooltip("Enable timer for this level")]
        public bool enableTimer = false;

        [Tooltip("Time limit in seconds (0 or less means no limit)")]
        public float timerDuration = 120f;

        public bool isHardLevel = false;

        // Get language data by language code
        public LanguageData GetLanguageData(string languageCode)
        {
            if (languages == null || languages.Count == 0)
                return null;

            return languages.Find(l => l.language == languageCode);
        }

        // Add a new language to this level
        public LanguageData AddLanguage(string languageCode)
        {
            // First check if it already exists
            var existing = GetLanguageData(languageCode);
            if (existing != null)
                return existing;

            // Create a new language data entry
            var newLang = new LanguageData
            {
                language = languageCode,
                letters = "",
                wordsAmount = 5,
                words = new string[0]
            };

            // Initialize the list if needed
            if (languages == null)
                languages = new List<LanguageData>();

            // Add the new language
            languages.Add(newLang);

            return newLang;
        }

        // Remove a language from this level
        public bool RemoveLanguage(string languageCode)
        {
            if (languages == null || languages.Count == 0)
                return false;

            return languages.RemoveAll(l => l.language == languageCode) > 0;
        }

        public string[] GetWords(string languageCode)
        {
            var langData = GetLanguageData(languageCode);
            if (langData == null)
                return null;

            return langData.words;
        }

        public string GetLetters(string instanceLanguage)
        {
            return languages.Find(l => l.language == instanceLanguage)?.letters;
        }

        public LevelGroup GetGroup()
        {
            var allGroups = Resources.LoadAll<LevelGroup>("Groups");
            foreach (var group in allGroups)
            {
                if (group.levels != null && group.levels.Contains(this))
                {
                    return group;
                }
            }
            return null;
        }

        public string GetTitle(string languageCode)
        {
            return GetGroup().GetTitle(languageCode);
        }
        public string GetText(string languageCode)
        {
            return GetGroup().GetText(languageCode);
        }

        public bool GroupIsFinished()
        {
           int currentIndex = GetGroup().levels.IndexOf(this);
           return currentIndex == GetGroup().levels.Count - 1;
        }

        public void UpdateWords(LanguageData languageData)
        {
            var crosswordWords = languageData.crosswordData.placements
                .OrderBy(p => p.wordNumber)
                .Select(p => p.word.ToLower())
                .ToArray();

            // Always clear existing words first
            if (crosswordWords == null || crosswordWords.Length == 0)
            {
                // If no words in crossword, set to empty array
                languageData.words = Array.Empty<string>();
            }
            else
            {
                // Replace with crossword words
                languageData.words = crosswordWords;
            }
        }
    }
}