// // ©2015 - 2025 Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WordsToolkit.Scripts.NLP;
using WordsToolkit.Scripts.Utilities;
using WordsToolkit.Scripts.Services.BannedWords;
using WordsToolkit.Scripts.System;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Gameplay.Managers;

namespace WordsToolkit.Scripts.Levels.Editor
{
    public static class LevelEditorServices
    {

        public static void GenerateWordsForLevel(Level level, IModelController model)
        {
            // Ensure model is properly loaded before proceeding
            if (model == null)
            {
                Debug.LogError("Model controller is null, cannot generate words for level");
                return;
            }
            
            // Make sure all language models are loaded
            model.LoadModels();
            
            // Wait a moment to ensure models are loaded
            EditorApplication.delayCall += () => {
                // Check if model is loaded properly
                bool allLanguagesLoaded = true;
                foreach (var langData in level.languages)
                {
                    if (!model.IsModelLoaded(langData.language))
                    {
                        allLanguagesLoaded = false;
                        Debug.LogWarning($"Model for language {langData.language} is not loaded properly");
                    }
                }
                
                if (allLanguagesLoaded)
                {
                    Debug.Log($"Generating words for level {level.number}");
                    GenerateAllWords(level, model);
                }
                else
                {
                    // Retry once after a short delay
                    Debug.Log("Models not fully loaded, retrying word generation shortly...");
                    EditorApplication.delayCall += () => {
                        Debug.Log($"Retrying word generation for level {level.number}");
                        GenerateAllWords(level, model);
                    };
                }
            };
        }

        public static void GenerateAllWords(Level level,IModelController model)
        {
            if (level == null) return;

            model.LoadModels();

            if (level.languages == null || level.languages.Count == 0)
            {
                Debug.LogWarning("The level has no languages defined. Add at least one language first.");
                return;
            }

            // Show progress bar
            int totalLanguages = level.languages.Count;
            int processedLanguages = 0;

            try
            {
                foreach (var languageData in level.languages)
                {
                    EditorUtility.DisplayProgressBar("Generating Words",
                        $"Processing language: {languageData.language}",
                        (float)processedLanguages / totalLanguages);

                    string letters = GenerateRandomLetters(languageData, languageData.wordsAmount, level.letters, false);
                    if (!string.IsNullOrEmpty(letters))
                    {
                        languageData.letters = letters;
                        languageData.wordsAmount = level.words;
                        GenerateWordsForLanguage(level, languageData, model, false);
                    }
                    processedLanguages++;
                }

                EditorUtility.SetDirty(level);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static void GenerateWordsForLanguage(Level level, LanguageData languageData, IModelController Controller, bool updateWordsAmount = true)
        {
            if (string.IsNullOrEmpty(languageData.letters)) return;

            // Get banned words service to filter out banned words
            var bannedWordsService = EditorScope.Resolve<IBannedWordsService>();

            // Generate words with length and probability constraints
            float probability = level.difficulty / 100f;
            var minLettersInWord = level.min;
            var maxLettersInWord = level.max;

            // Group words by length
            var wordsFromSymbols = Controller.GetWordsFromSymbols(languageData.letters, languageData.language);
            var wordsByLength = wordsFromSymbols.Where(i=> !IsWordUsed( i, languageData.language, level, out _))
                .Where(w => w.Length >= minLettersInWord && w.Length <= maxLettersInWord)
                .Where(w => bannedWordsService == null || !bannedWordsService.IsWordBanned(w, languageData.language)) // Filter out banned words
                .GroupBy(w => w.Length)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Use the new GetWordsByFactor function to select words
            var selectedWords = GetWordsByFactor(wordsByLength, probability, languageData.wordsAmount);

            if(selectedWords.Count < languageData.wordsAmount)
            {
                // If not enough words were selected, try to fill the gap with random words
                var allWords = wordsFromSymbols.Where(w => w.Length >= minLettersInWord && w.Length <= maxLettersInWord)
                    .Where(w => bannedWordsService == null || !bannedWordsService.IsWordBanned(w, languageData.language)) // Filter out banned words
                    .ToList();
                var additionalWordsNeeded = languageData.wordsAmount - selectedWords.Count;
                var remainingWords = new HashSet<string>(allWords);
                remainingWords.ExceptWith(selectedWords);

                // Break if we don't have enough unique words left
                if (remainingWords.Count < additionalWordsNeeded)
                {
                    additionalWordsNeeded = remainingWords.Count;
                }

                // Add remaining available words
                while (selectedWords.Count < languageData.wordsAmount && additionalWordsNeeded > 0 && remainingWords.Count > 0)
                {
                    string randomWord = remainingWords.ElementAt(UnityEngine.Random.Range(0, remainingWords.Count));
                    selectedWords.Add(randomWord);
                    remainingWords.Remove(randomWord);
                    additionalWordsNeeded--;
                }
            }

            if (selectedWords.Count == 0)
            {
                return;
            }

            Debug.Log($"Generated words for language {languageData.language} in level {level.number}");
            // Shuffle the final list
            var words = selectedWords.OrderBy(x => UnityEngine.Random.value).ToList();

            // Update the words array
            languageData.words = words.ToArray();
            if (updateWordsAmount)
            {
                languageData.wordsAmount = words.Count;
            }
        }

        public static List<string> GenerateAvailableWords(Level level, IModelController model, LanguageData languageData)
        {
            if (level == null || model == null || languageData == null)
            {
                Debug.LogError("Level, model or language data is null, cannot generate available words");
                return new List<string>();
            }

            model.LoadModels();
            return model.GetWordsFromSymbols(languageData.letters, languageData.language);
        }

        public static string GenerateRandomLetters(LanguageData languageData, int count, int lettersAmount, bool generateLetters)
        {
            var controller = EditorScope.Resolve<IModelController>();
            var bannedWordsService = EditorScope.Resolve<IBannedWordsService>();
            
            // Ensure model is loaded
            if (controller == null)
            {
                Debug.LogError($"Failed to resolve IModelController for generating letters");
                return "";
            }

            // 30% chance to generate actual random letters instead of finding optimal words
            if (generateLetters)
            {
                string randomLetters = GenerateActualRandomLetters(languageData.language, lettersAmount);
                
                // Check if these random letters can generate any words
                var wordsFromRandomLetters = controller.GetWordsFromSymbols(randomLetters, languageData.language);
                if (wordsFromRandomLetters != null && wordsFromRandomLetters.Count() > 0)
                {
                    return randomLetters;
                }
            }

            var usedWords = LevelEditorServices.GetUsedWords(languageData.language);
            
            // Try getting words with specified length
            var words = controller.GetWordsWithLength(lettersAmount, languageData.language).Where(w => !usedWords.Contains(w)).ToList();
            
            // If no words found with the specified length, try with other lengths
            if (words.Count == 0)
            {
                // Check if model is loaded for this language
                if (!controller.IsModelLoaded(languageData.language))
                {
                    controller.LoadModels();
                    
                    // Try again after reloading
                    words = controller.GetWordsWithLength(lettersAmount, languageData.language).Where(w => !usedWords.Contains(w)).ToList();
                }
                
                // If still no words found after reloading, try with other lengths
                if (words.Count == 0)
                {
                    // Try with smaller lengths first, then larger
                    for (int offset = 1; offset <= 3; offset++)
                    {
                        // Try smaller length
                        if (lettersAmount - offset > 2)
                        {
                            words = controller.GetWordsWithLength(lettersAmount - offset, languageData.language);
                            if (words.Count > 0) break;
                        }
                        
                        // Try larger length
                        words = controller.GetWordsWithLength(lettersAmount + offset, languageData.language);
                        if (words.Count > 0) break;
                    }
                }
            }
            
            // If still no words found, use some default letters
            if (words.Count == 0)
            {
                // Default letter sets by language
                Dictionary<string, string> defaultLetters = new Dictionary<string, string>
                {
                    { "en", "eariotnslcudpmhgbfywkvxzjq" },
                    { "es", "eaosrnidlctumpbgvyqjhfzñxw" },
                    { "ru", "оеаинтсрвлкмдпуяыьгзбчйхжшюцщэфъ" },
                };
                
                if (defaultLetters.TryGetValue(languageData.language, out string letters))
                {
                    return letters.Substring(0, Mathf.Min(lettersAmount, letters.Length));
                }
                else
                {
                    return new string('a', lettersAmount);
                }
            }
            
            // Continue with the normal process if words are found
            string bestWord = words[0];
            int maxWordsGenerated = 0;

            for (int i = 0; i < words.Count; i++)
            {
                var generatedWords = controller.GetWordsFromSymbols(words[i], languageData.language);
                
                // Filter out banned words when counting generated words
                if (bannedWordsService != null)
                {
                    generatedWords = generatedWords.Where(w => !bannedWordsService.IsWordBanned(w, languageData.language)).ToList();
                }
                
                int wordsCount = generatedWords?.Count() ?? 0;

                if (wordsCount >= count)
                {
                    return words[i];
                }

                if (wordsCount > maxWordsGenerated)
                {
                    maxWordsGenerated = wordsCount;
                    bestWord = words[i];
                }
            }

            return bestWord; // Return the word that generated the most derived words
        }

        private static string GenerateActualRandomLetters(string language, int length)
        {
            // Define Latin languages that use vowel/consonant distinction
            HashSet<string> latinLanguages = new HashSet<string> { "en", "es", "fr", "de", "it", "pt", "nl", "da", "sv", "no" };
            
            if (latinLanguages.Contains(language))
            {
                // For Latin languages, use vowel/consonant distribution
                string vowels = "aeiou";
                string consonants = "bcdfghjklmnpqrstvwxyz";
                
                var result = new List<char>();
                int vowelCount = Mathf.Max(1, length / 2); // About 1/2 vowels
                int consonantCount = length - vowelCount;

                // Add vowels
                for (int i = 0; i < vowelCount; i++)
                {
                    result.Add(vowels[UnityEngine.Random.Range(0, vowels.Length)]);
                }

                // Add consonants
                for (int i = 0; i < consonantCount; i++)
                {
                    result.Add(consonants[UnityEngine.Random.Range(0, consonants.Length)]);
                }

                // Shuffle the letters
                var chars = result.ToArray();
                for (int i = 0; i < chars.Length; i++)
                {
                    int randomIndex = UnityEngine.Random.Range(i, chars.Length);
                    (chars[i], chars[randomIndex]) = (chars[randomIndex], chars[i]);
                }

                return new string(chars);
            }
            else
            {
                // For non-Latin languages, use default letters without vowel distinction
                Dictionary<string, string> defaultLetters = new Dictionary<string, string>
                {
                    { "ru", "оеаинтсрвлкмдпуяыьгзбчйхжшюцщэфъ" },
                    { "zh", "一二三四五六七八九十百千万亿" },
                    { "ja", "あいうえおかきくけこさしすせそたちつてとなにぬねの" },
                    { "ar", "ابتثجحخدذرزسشصضطظعغفقكلمنهوي" }
                };
                
                string letters;
                if (!defaultLetters.TryGetValue(language, out letters))
                {
                    letters = "abcdefghijklmnopqrstuvwxyz"; // Fallback to Latin alphabet
                }
                
                var result = new List<char>();
                for (int i = 0; i < length; i++)
                {
                    result.Add(letters[UnityEngine.Random.Range(0, letters.Length)]);
                }
                
                return new string(result.ToArray());
            }
        }

        private static List<string> GetUsedWords(string languageDataLanguage)
        {
            // Get all levels in the project
            var allLevels = Resources.FindObjectsOfTypeAll<Level>();
            var usedWords = new List<string>();

            // Iterate through each level and collect words for the specified language
            foreach (var level in allLevels)
            {
                if (level.languages == null) continue;

                var langData = level.languages.FirstOrDefault(l => l.language == languageDataLanguage);
                if (langData != null && langData.words != null)
                {
                    usedWords.AddRange(langData.words);
                }
            }

            return usedWords.Distinct().ToList(); // Return distinct words to avoid duplicates
        }

        public static List<string> GetWordsByFactor(Dictionary<int, List<string>> wordsByLength, float factor, int count)
        {
            // Ensure factor is clamped between 0 and 1
            factor = Mathf.Clamp01(factor);

            // Create a weighted list of lengths based on the factor
            var weightedLengths = wordsByLength.Keys
                .OrderBy(length => length) // Sort lengths in ascending order
                .Select(length => new
                {
                    Length = length,
                    Weight = Mathf.Lerp(0f, 1f, factor) * (length - wordsByLength.Keys.Min()) +
                             Mathf.Lerp(1f, 0f, factor) * (wordsByLength.Keys.Max() - length)
                })
                .OrderByDescending(x => x.Weight) // Sort by weight descending
                .ToList();

            // Select words based on weighted lengths
            var selectedWords = new List<string>();
            foreach (var weightedLength in weightedLengths)
            {
                if (selectedWords.Count >= count)
                    break;

                if (wordsByLength.TryGetValue(weightedLength.Length, out var words))
                {
                    var availableWords = new List<string>(words);
                    while (selectedWords.Count < count && availableWords.Count > 0)
                    {
                        int index = UnityEngine.Random.Range(0, availableWords.Count);
                        selectedWords.Add(availableWords[index]);
                        availableWords.RemoveAt(index);
                    }
                }
            }

            return selectedWords;
        }

        // Extract and update words from crossword placement
        public static void UpdateWordsFromCrossword(Level level, string langCode, List<WordPlacement> placements)
        {
            if (level == null || string.IsNullOrEmpty(langCode) || placements == null)
            {
                Debug.LogWarning("Cannot update words: invalid parameters");
                return;
            }

            var languageData = level.GetLanguageData(langCode);
            if (languageData == null)
            {
                Debug.LogError($"Language {langCode} not found in level {level.name}");
                return;
            }

            // Extract words from placements
            var crosswordWords = placements
                .Where(p => !string.IsNullOrEmpty(p.word))
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
            bool wordsChanged = true;

            if (wordsChanged)
            {
                EditorUtility.SetDirty(level);
                AssetDatabase.SaveAssets();
            }
        }

        public static Level[] GetUsedInLevels(string elementStringValue, string langCode, Level thisLevel)
        {
            var usedInLevels = new List<Level>();
            foreach (var l in Resources.LoadAll<Level>("Levels"))
            {
                var languageData = l.GetLanguageData(langCode);
                if (l != thisLevel && languageData != null && languageData.words != null)
                {
                    if (languageData.words.Contains(elementStringValue.ToLower()))
                    {
                        usedInLevels.Add(l); // Add the level where the word is used
                    }
                }
            }
            return usedInLevels.ToArray(); // Return all levels where the word is used
        }

        public static bool IsWordUsed(string elementStringValue, string langCode, Level thisLevel, out Level usedInLevel)
        {
            usedInLevel = null;
            foreach (var l in Resources.LoadAll<Level>("Levels"))
            {
                var languageData = l.GetLanguageData(langCode);
                if (l != thisLevel && languageData != null && languageData.words != null)
                {
                    if (languageData.words.Contains(elementStringValue.ToLower()))
                    {
                        usedInLevel = l; // Set the level where the word is used
                        return true; // Word is used in this level
                    }
                }
            }
            return false; // Word is not used in any other level
        }
        
        public static void TestLevel(Level level, string languageCode)
        {
            if (level == null)
            {
                Debug.LogWarning("No level provided to test.");
                return;
            }
            
            if (string.IsNullOrEmpty(languageCode))
            {
                Debug.LogWarning("No language code provided for testing.");
                return;
            }
            
            // First, ensure we load the main scene before testing
            string mainScenePath = "Assets/WordConnectGameToolkit/Scenes/main.unity";
            if (File.Exists(mainScenePath))
            {
                // Load the main scene first
                EditorSceneManager.OpenScene(mainScenePath);
                Debug.Log($"Loaded main scene: {mainScenePath}");
            }
            else
            {
                Debug.LogError($"Main scene not found at path: {mainScenePath}");
                return;
            }
            
            // Set test play mode and level
            GameDataManager.isTestPlay = true;
            GameDataManager.SetLevel(level);
            GameDataManager.SetLevelNum(level.number);
            
            // Set the current language
            PlayerPrefs.SetString("SelectedLanguage", languageCode);
            
            // Set state manager to main menu first, then it will transition to game
            var stateManager = UnityEngine.Object.FindObjectOfType<StateManager>();
            if (stateManager != null)
            {
                stateManager.CurrentState = EScreenStates.MainMenu;
            }
            
            // Enter play mode if not already in it
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
            }

            // Give Unity a moment to enter play mode before loading the level
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlaying)
                {
                    // Wait a bit more for the scene to be fully loaded
                    EditorApplication.delayCall += () =>
                    {
                        if (EditorApplication.isPlaying)
                        {
                            // Now set the state to game to start the level
                            var playModeStateManager = UnityEngine.Object.FindObjectOfType<StateManager>();
                            if (playModeStateManager != null)
                            {
                                playModeStateManager.CurrentState = EScreenStates.Game;
                            }
                            
                            // Find and initialize the LevelManager
                            var levelManager = UnityEngine.Object.FindObjectOfType<LevelManager>();
                            if (levelManager != null)
                            {
                                levelManager.Load();
                            }
                            else
                            {
                                Debug.LogError("LevelManager not found in the scene. Make sure it exists before testing levels.");
                            }
                        }
                    };
                }
            };
        }
        
        // Check for problematic overlapping words
        public static bool CheckForOverlappingWords(List<WordPlacement> placements, string[] originalWords = null)
        {
            // Check for words starting at the same position
            var sameStartGroups = placements
                .GroupBy(p => p.startPosition)
                .Where(g => g.Count() > 1)
                .ToList();

            bool hasPositionOverlaps = sameStartGroups.Count > 0;

            // Check for same-orientation overlaps (words that overlap along their length)
            bool hasSameOrientationOverlaps = false;
            for (int i = 0; i < placements.Count; i++)
            {
                for (int j = i + 1; j < placements.Count; j++)
                {
                    var word1 = placements[i];
                    var word2 = placements[j];
                    
                    // Only check words with the same orientation
                    if (word1.isHorizontal == word2.isHorizontal)
                    {
                        if (word1.isHorizontal)
                        {
                            // Both horizontal - check if they're on the same row and overlap
                            if (word1.startPosition.y == word2.startPosition.y)
                            {
                                int word1End = word1.startPosition.x + word1.word.Length - 1;
                                int word2End = word2.startPosition.x + word2.word.Length - 1;
                                
                                // Check for overlap
                                bool overlaps = !(word1End < word2.startPosition.x || word2End < word1.startPosition.x);
                                if (overlaps)
                                {
                                    hasSameOrientationOverlaps = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // Both vertical - check if they're on the same column and overlap
                            if (word1.startPosition.x == word2.startPosition.x)
                            {
                                int word1End = word1.startPosition.y + word1.word.Length - 1;
                                int word2End = word2.startPosition.y + word2.word.Length - 1;
                                
                                // Check for overlap
                                bool overlaps = !(word1End < word2.startPosition.y || word2End < word1.startPosition.y);
                                if (overlaps)
                                {
                                    hasSameOrientationOverlaps = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (hasSameOrientationOverlaps) break;
            }

            // Check if some words are missing from placements (indicating overlaps prevented placement)
            bool hasMissingWords = false;
            if (originalWords != null)
            {
                hasMissingWords = originalWords.Length > placements.Count;
            }

            return hasPositionOverlaps || hasSameOrientationOverlaps || hasMissingWords;
        }
    }
}