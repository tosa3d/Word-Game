using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WordsToolkit.Scripts.Gameplay;
using Random = System.Random;

namespace WordsToolkit.Scripts.Utilities
{
    // Enum moved outside the scriptable object so it can be used by both
    public enum GenerationStrategy
    {
        Auto = -1, // Choose automatically based on seed
        Horizontal = 0, // Strategy 0: Strict horizontal-first approach
        MixedLength = 1, // Strategy 1: Mix medium and other length words
        Interleaved = 2, // Strategy 2: Interleave long and short words
        MaxIntersections = 3 // Strategy 3: Focus on maximizing intersections
    }
    
    // Simple struct for configuration, can be created from the scriptable object
    [Serializable]
    public struct CrosswordGenerationConfig
    {
        public int columns;
        public int rows;
        public int seed;
        public int maxAttempts;
        public int minHorizontalRatio;
        public int maxRows;
        public int verticalWordMaxLength;
        public int smallWordMaxLength;
        public bool forceUniqueLayout;
        public GenerationStrategy preferredStrategy;
        public int maxOverlapRetries; // New: maximum additional attempts when overlaps are detected
        
        // Constructor with default values
        public static CrosswordGenerationConfig Default => new CrosswordGenerationConfig
        {
            columns = 15,
            rows = 15,
            seed = 0,
            maxAttempts = 20,
            minHorizontalRatio = 40, // Even more balanced - allows more vertical words
            maxRows = 5,
            verticalWordMaxLength = 8, // Allow longer vertical words
            smallWordMaxLength = 6, // Allow more words to be placed vertically
            forceUniqueLayout = true,
            preferredStrategy = GenerationStrategy.Auto, // Doesn't matter anymore since we removed strategies
            maxOverlapRetries = 5
        };
    }

    [Serializable]
    public class WordPlacement
    {
        public string word;
        public Vector2Int startPosition;
        public bool isHorizontal;
        public int wordNumber;
        public List<Tile> tiles = new List<Tile>(); // Store tiles directly in word placement
    }

    public static class CrosswordGenerator
    {
        private static bool firstWordHorizontal;
        
        // Add a new overload that uses default config instead of dependency on ConfigManager
        /// <summary>
        /// Generate a crossword using the default configuration.
        /// </summary>
        /// <param name="words">Words to place in the crossword</param>
        /// <param name="grid">Output grid containing the crossword</param>
        /// <param name="placements">Output list of word placements</param>
        /// <returns>True if the crossword was generated successfully</returns>
        public static bool RegenerateCrossword(string[] words, out char[,] grid, out List<WordPlacement> placements)
        {
            return RegenerateCrossword(words, CrosswordGenerationConfig.Default, out grid, out placements);
        }
        
        // Overload that accepts a scriptable object configuration
        public static bool RegenerateCrossword(string[] words, CrosswordGenerationConfigSO configSO, out char[,] grid, out List<WordPlacement> placements)
        {
            if (configSO == null)
            {
                return RegenerateCrossword(words, CrosswordGenerationConfig.Default, out grid, out placements);
            }
            
            return RegenerateCrossword(words, configSO.ToConfig(), out grid, out placements);
        }

        // Overload for RegenerateCrossword that accepts a configuration struct
        public static bool RegenerateCrossword(string[] words, CrosswordGenerationConfig config, out char[,] grid, out List<WordPlacement> placements)
        {
            // Maximum number of attempts before giving up
            int maxAttempts = config.maxAttempts;
            int currentAttempt = 0;
            int baseSeed = config.seed;
            int overlapRetryCount = 0; // Track how many times we've retried due to overlaps
            
            // Use exactly what the user specified
            int columns = config.columns;
            int rows = config.rows;
            
            // Always initialize with exact dimensions
            grid = new char[columns, rows];
            placements = new List<WordPlacement>();
            
            // Track previously used seeds to ensure variation if requested
            HashSet<int> usedSeeds = new HashSet<int>();
            
            while (currentAttempt < maxAttempts)
            {
                try
                {
                    // Use the base seed plus the attempt number to create variation between attempts
                    int attemptSeed = baseSeed + currentAttempt * 1000;
                    Random random = new Random(attemptSeed);
                    
                    // Re-initialize grid and placements each attempt
                    grid = new char[columns, rows];
                    placements = new List<WordPlacement>();
                    
                    // Create a shuffled copy of the words array - completely random order
                    List<string> sortedWords = words.OrderBy(w => random.Next()).ToList();
                    
                    // Place first word in the middle - vary direction based on strategy and seed
                    if (sortedWords.Count > 0)
                    {
                        string firstWord = sortedWords[0];
                        
                        // Check which directions are possible
                        bool canPlaceHorizontally = firstWord.Length <= columns;
                        bool canPlaceVertically = firstWord.Length <= rows;
                        
                        if (!canPlaceHorizontally && !canPlaceVertically)
                        {
                            currentAttempt++;
                            continue;
                        }

                        // Decide direction based on strategy and randomness to create more variety
                        bool placeHorizontally;
                        
                        if (!canPlaceHorizontally)
                        {
                            placeHorizontally = false;
                        }
                        else if (!canPlaceVertically)
                        {
                            placeHorizontally = true;
                        }
                        else
                        {
                            // Both directions possible - choose randomly (50/50)
                            placeHorizontally = random.Next(2) == 0;
                        }
                        
                        if (placeHorizontally)
                        {
                            int startX = columns / 2 - firstWord.Length / 2;
                            int startY = rows / 2;
                            startX = Mathf.Clamp(startX, 0, columns - firstWord.Length);
                            startY = Mathf.Clamp(startY, 0, rows - 1);
                            PlaceWord(firstWord, new Vector2Int(startX, startY), true, 1, grid, placements);
                        }
                        else
                        {
                            int startX = columns / 2;
                            int startY = rows / 2 - firstWord.Length / 2;
                            startX = Mathf.Clamp(startX, 0, columns - 1);
                            startY = Mathf.Clamp(startY, 0, rows - firstWord.Length);
                            PlaceWord(firstWord, new Vector2Int(startX, startY), false, 1, grid, placements);
                        }
                        
                        // Track the first word placement for statistics
                        firstWordHorizontal = placeHorizontally;
                        
                        sortedWords.RemoveAt(0);
                    }
                    else
                    {
                        currentAttempt++;
                        continue;
                    }

                    // Try to place remaining words
                    int wordNumber = 2;
                    bool allWordsPlaced = true;
                    
                    // Track word placement statistics
                    int horizontalPlaced = firstWordHorizontal ? 1 : 0; // Track actual first word direction
                    int totalPlaced = 1;
                    
                    // Try to place each remaining word
                    foreach (var word in sortedWords)
                    {
                        // Calculate current horizontal ratio for basic balance
                        int currentHorizontalRatio = horizontalPlaced * 100 / totalPlaced;
                        bool forceHorizontal = currentHorizontalRatio < config.minHorizontalRatio || 
                                              word.Length > config.verticalWordMaxLength;
                        
                        bool placed = false;
                        
                        // NEW APPROACH: Find all possible placements and randomly choose one
                        var allPossiblePlacements = FindAllPossiblePlacements(word, grid, placements, columns, rows, forceHorizontal);
                        
                        if (allPossiblePlacements.Count > 0)
                        {
                            // Randomly select one of the possible placements
                            var randomPlacement = allPossiblePlacements[random.Next(allPossiblePlacements.Count)];
                            PlaceWord(word, randomPlacement.position, randomPlacement.isHorizontal, wordNumber, grid, placements);
                            placed = true;
                        }
                        else
                        {
                            // Fallback: try forced placement if no intersections found
                            bool forceDirection = random.Next(2) == 0; // Random direction
                            placed = TryForcedPlacement(word, wordNumber, forceDirection, grid, placements, columns, rows);
                            
                            // If that failed, try the other direction
                            if (!placed)
                            {
                                placed = TryForcedPlacement(word, wordNumber, !forceDirection, grid, placements, columns, rows);
                            }
                        }
                        
                        if (placed)
                        {
                            wordNumber++;
                            totalPlaced++;
                            
                            // Check if last placed word was horizontal
                            if (placements[placements.Count - 1].isHorizontal)
                            {
                                horizontalPlaced++;
                            }
                        }
                        else
                        {
                            allWordsPlaced = false;
                            break;
                        }
                    }
                    
                    // If all words were placed successfully, return true - ignore constraints
                    if (allWordsPlaced && placements.Count == words.Length)
                    {
                        int finalHorizontalRatio = horizontalPlaced * 100 / totalPlaced;
                        
                        // Check for overlapping words and warn about potential issues
                        bool hasProblematicOverlaps = CheckForOverlappingWords(placements);
                        
                        // If we found problematic overlaps and still have retry attempts left, try again
                        if (hasProblematicOverlaps && overlapRetryCount < config.maxOverlapRetries && currentAttempt < maxAttempts - 1)
                        {
                            overlapRetryCount++;
                            currentAttempt++;
                            continue; // Try again with next attempt
                        }
                        
                        // Don't check any constraints - always accept (either no overlaps or out of attempts)
                        CalculateGridBounds(grid, out Vector2Int min, out Vector2Int max);
                        int width = max.x - min.x + 1;
                        int height = max.y - min.y + 1;
                        
                        return true;
                    }
                    
                    // Try again with a different seed
                    currentAttempt++;
                }
                catch (Exception ex)
                {
                    // Log any errors and continue to next attempt
                    Debug.LogError($"Error during crossword generation attempt {currentAttempt}: {ex.Message}");
                    currentAttempt++;
                    continue;
                }
            }
            
            return GenerateFallbackLayout(words, columns, rows, out grid, out placements);
        }
        
        // Wrapper for backward compatibility
        public static bool RegenerateCrossword(string[] words, int seed, out char[,] grid, out List<WordPlacement> placements)
        {
            var config = Resources.Load<CrosswordGenerationConfigSO>("Settings/CrosswordConfig");
            if (config == null)
            {
                // Use default config if none is found
                var defaultConfig = CrosswordGenerationConfig.Default;
                defaultConfig.seed = seed;
                return RegenerateCrossword(words, defaultConfig, out grid, out placements);
            }
            
            // Use the loaded config but override the seed
            var configStruct = config.ToConfig();
            configStruct.seed = seed;
            return RegenerateCrossword(words, configStruct, out grid, out placements);
        }

        // Replace GenerateCrosswordWide with GenerateFallbackLayout that respects dimensions
        private static bool GenerateFallbackLayout(string[] words, int columns, int rows, out char[,] grid, out List<WordPlacement> placements)
        {
            // Initialize grid with exact dimensions
            grid = new char[columns, rows];
            placements = new List<WordPlacement>();

            // Sort words by length (longest first for better placement)
            var sortedWords = words.OrderByDescending(w => w.Length).ToList();
            
            // Place first word in the center horizontally
            if (sortedWords.Count > 0)
            {
                string firstWord = sortedWords[0];
                int startX = columns / 2 - firstWord.Length / 2;
                int startY = rows / 2;
                
                // Make sure first word fits in the grid
                if (startX < 0 || startX + firstWord.Length > columns)
                {
                    // If it doesn't fit horizontally, try vertically
                    startX = columns / 2;
                    startY = rows / 2 - firstWord.Length / 2;
                    
                    if (startY < 0 || startY + firstWord.Length > rows)
                    {
                        // If it still doesn't fit, place it at origin
                        startX = 0;
                        startY = 0;
                    }
                    
                    PlaceWord(firstWord, new Vector2Int(startX, startY), false, 1, grid, placements);
                }
                else
                {
                    PlaceWord(firstWord, new Vector2Int(startX, startY), true, 1, grid, placements);
                }
                
                sortedWords.RemoveAt(0);
            }
            else
            {
                return false;
            }

            // Try to place remaining words
            int wordNumber = 2;
            
            // Track words we couldn't place on first attempt
            List<string> unplacedWords = new List<string>();
            
            // First pass - try standard placement for each word
            foreach (var word in sortedWords)
            {
                bool placed = false;
                
                // First try horizontal placement with intersections
                placed = TryPlaceHorizontally(word, wordNumber, grid, placements, columns, rows);
                
                // If horizontal failed, try vertical
                if (!placed)
                {
                    placed = TryPlaceVertically(word, wordNumber, grid, placements, columns, rows);
                }
                
                // If both failed, try forced placement
                if (!placed)
                {
                    placed = TryForcedPlacement(word, wordNumber, true, grid, placements, columns, rows);
                }
                
                if (placed)
                {
                    wordNumber++;
                }
                else
                {
                    // Track this word to try again later
                    unplacedWords.Add(word);
                }
            }
            
            // Second pass - try aggressive placement for any words we couldn't place
            foreach (var word in unplacedWords)
            {
                bool placed = false;
                
                // Try placing horizontally anywhere, even if not intersecting
                for (int y = 0; y < rows && !placed; y++)
                {
                    for (int x = 0; x < columns - word.Length + 1 && !placed; x++)
                    {
                        Vector2Int start = new Vector2Int(x, y);
                        
                        // Only check if we don't overlap with other words, don't require intersection
                        if (IsValidPlacementIgnoringIntersection(word, start, true, grid, columns, rows))
                        {
                            PlaceWord(word, start, true, wordNumber, grid, placements);
                            placed = true;
                            wordNumber++;
                        }
                    }
                }
                
                // If horizontal placement failed everywhere, try vertical
                if (!placed)
                {
                    for (int x = 0; x < columns && !placed; x++)
                    {
                        for (int y = 0; y < rows - word.Length + 1 && !placed; y++)
                        {
                            Vector2Int start = new Vector2Int(x, y);
                            
                            // Only check if we don't overlap with other words, don't require intersection
                            if (IsValidPlacementIgnoringIntersection(word, start, false, grid, columns, rows))
                            {
                                PlaceWord(word, start, false, wordNumber, grid, placements);
                                placed = true;
                                wordNumber++;
                            }
                        }
                    }
                }
                
                // If we still couldn't place the word, it's a serious problem
                if (!placed)
                {
                    // ABSOLUTE LAST RESORT - find any legal cell where this can be placed
                    // This should always succeed unless the grid is COMPLETELY full
                    for (int attempt = 0; attempt < 4 && !placed; attempt++)
                    {
                        bool tryHorizontal = (attempt % 2 == 0);
                        bool relaxConstraints = (attempt >= 2);
                        
                        for (int y = 0; y < rows && !placed; y++)
                        {
                            for (int x = 0; x < columns && !placed; x++)
                            {
                                if (tryHorizontal && x + word.Length <= columns)
                                {
                                    Vector2Int start = new Vector2Int(x, y);
                                    if (relaxConstraints ? CanPlaceWordVeryAggressively(word, start, true, grid, columns, rows) 
                                                        : IsValidPlacementIgnoringIntersection(word, start, true, grid, columns, rows))
                                    {
                                        PlaceWord(word, start, true, wordNumber, grid, placements);
                                        placed = true;
                                        wordNumber++;
                                    }
                                }
                                else if (!tryHorizontal && y + word.Length <= rows)
                                {
                                    Vector2Int start = new Vector2Int(x, y);
                                    if (relaxConstraints ? CanPlaceWordVeryAggressively(word, start, false, grid, columns, rows)
                                                        : IsValidPlacementIgnoringIntersection(word, start, false, grid, columns, rows))
                                    {
                                        PlaceWord(word, start, false, wordNumber, grid, placements);
                                        placed = true;
                                        wordNumber++;
                                    }
                                }
                            }
                        }
                    }
                    
                    if (!placed)
                    {
                        // This should never happen unless the grid is completely full or the word is longer than grid
                        Debug.LogError($"CRITICAL: Failed to place word {word} even with emergency placement! This is likely a bug.");
                    }
                }
            }

            // Check for overlapping words and warn about potential issues
            bool hasProblematicOverlaps = CheckForOverlappingWords(placements);

            return placements.Count > 0;
        }

        // Helper method to remove a word from the grid (for enforcing constraints)
        private static void RemoveWord(WordPlacement placement, char[,] grid)
        {
            for (int i = 0; i < placement.word.Length; i++)
            {
                int x = placement.isHorizontal ? placement.startPosition.x + i : placement.startPosition.x;
                int y = placement.isHorizontal ? placement.startPosition.y : placement.startPosition.y + i;
                grid[x, y] = (char)0;
            }
        }

        // Keep for backward compatibility
        public static bool GenerateCrossword(string[] words, int gridSize, out char[,] grid, out List<WordPlacement> placements)
        {
            // Call the new version with gridSize for both dimensions
            return GenerateCrossword(words, gridSize, gridSize, out grid, out placements);
        }
        
        // New version that uses columns and rows
        public static bool GenerateCrossword(string[] words, int columns, int rows, out char[,] grid, out List<WordPlacement> placements)
        {
            // Initialize grid and placements
            grid = new char[columns, rows];
            placements = new List<WordPlacement>();

            // Sort words by length (longest first for better placement)
            var sortedWords = words.OrderByDescending(w => w.Length).ToList();
            
            // Place first word in the center horizontally
            if (sortedWords.Count > 0)
            {
                string firstWord = sortedWords[0];
                int startX = columns / 2 - firstWord.Length / 2;
                int startY = rows / 2;
                
                PlaceWord(firstWord, new Vector2Int(startX, startY), true, 1, grid, placements);
                sortedWords.RemoveAt(0);
            }
            else
            {
                return false;
            }

            // Try to place remaining words
            int wordNumber = 2;
            
            // First place longer words horizontally as much as possible
            List<string> longWords = sortedWords.Where(w => w.Length >= 5).ToList();
            List<string> shortWords = sortedWords.Where(w => w.Length < 5).ToList();
            
            // Try to place longer words first with horizontal preference
            foreach (var word in longWords)
            {
                if (!TryPlaceWord(word, wordNumber, true, grid, placements, columns, rows)) // true means strong horizontal preference
                {
                }
                else
                {
                    wordNumber++;
                }
            }
            
            // Then place shorter words with normal placement rules
            foreach (var word in shortWords)
            {
                if (!TryPlaceWord(word, wordNumber, false, grid, placements, columns, rows)) // false means normal placement rules
                {
                }
                else
                {
                    wordNumber++;
                }
            }

            return placements.Count > 0;
        }

        // Fixed TryPlaceWord to correctly use columns and rows
        public static bool TryPlaceWord(string word, int wordNumber, bool forceHorizontalPreference, char[,] grid, List<WordPlacement> placements, int columns, int rows)
        {
            // Track horizontal and vertical word counts
            int horizontalWords = placements.Count(w => w.isHorizontal);
            int verticalWords = placements.Count - horizontalWords;
            
            // Try to intersect with already placed words
            foreach (var placedWord in placements)
            {
                string placed = placedWord.word;
                Vector2Int startPos = placedWord.startPosition;
                bool isHorizontal = placedWord.isHorizontal;

                // Try to place the word intersecting with each letter of the placed word
                for (int i = 0; i < placed.Length; i++)
                {
                    char intersectChar = placed[i];
                    
                    // Check if this character exists in the new word
                    int indexInNewWord = word.IndexOf(intersectChar);
                    while (indexInNewWord >= 0)
                    {
                        Vector2Int intersectionPoint;
                        if (isHorizontal)
                        {
                            intersectionPoint = new Vector2Int(startPos.x + i, startPos.y);
                        }
                        else
                        {
                            intersectionPoint = new Vector2Int(startPos.x, startPos.y + i);
                        }

                        // Try horizontal placement first (regardless of the placed word orientation)
                        Vector2Int horizontalStart = new Vector2Int(
                            intersectionPoint.x - indexInNewWord, 
                            intersectionPoint.y
                        );

                        if (CanPlaceWord(word, horizontalStart, true, grid, columns, rows, placements))
                        {
                            // Place the word horizontally
                            PlaceWord(word, horizontalStart, true, wordNumber, grid, placements);
                            return true;
                        }

                        // Only try vertical placement if we're maintaining the desired ratio
                        // and not forcing horizontal preference for longer words
                        if (!forceHorizontalPreference && CanAddVerticalWord(horizontalWords, verticalWords, word.Length))
                        {
                            Vector2Int verticalStart = new Vector2Int(
                                intersectionPoint.x, 
                                intersectionPoint.y - indexInNewWord
                            );

                            if (CanPlaceWord(word, verticalStart, false, grid, columns, rows, placements))
                            {
                                PlaceWord(word, verticalStart, false, wordNumber, grid, placements);
                                return true;
                            }
                        }

                        // Look for next occurrence of this character
                        indexInNewWord = word.IndexOf(intersectChar, indexInNewWord + 1);
                    }
                }
            }

            // If we couldn't place the word with intersections but we're not forcing horizontal,
            // try placing it horizontally anywhere valid as a last resort
            if (!forceHorizontalPreference)
            {
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns - word.Length + 1; x++)
                    {
                        Vector2Int start = new Vector2Int(x, y);
                        if (CanPlaceWord(word, start, true, grid, columns, rows))
                        {
                            PlaceWord(word, start, true, wordNumber, grid, placements);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        // New helper methods to support horizontal-focused placement
        
        private static bool TryPlaceHorizontally(string word, int wordNumber, char[,] grid, List<WordPlacement> placements, int columns, int rows)
        {
            // Simple horizontal placement - just find first valid placement
            foreach (var placedWord in placements)
            {
                string placed = placedWord.word;
                Vector2Int startPos = placedWord.startPosition;
                bool isHorizontal = placedWord.isHorizontal;
                
                for (int i = 0; i < placed.Length; i++)
                {
                    char intersectChar = placed[i];
                    int indexInNewWord = word.IndexOf(intersectChar);
                    
                    while (indexInNewWord >= 0)
                    {
                        Vector2Int intersectionPoint;
                        if (isHorizontal)
                        {
                            intersectionPoint = new Vector2Int(startPos.x + i, startPos.y);
                        }
                        else
                        {
                            intersectionPoint = new Vector2Int(startPos.x, startPos.y + i);
                        }
                        
                        // Try horizontal placement
                        Vector2Int horizontalStart = new Vector2Int(
                            intersectionPoint.x - indexInNewWord, 
                            intersectionPoint.y
                        );
                        
                        if (CanPlaceWord(word, horizontalStart, true, grid, columns, rows))
                        {
                            PlaceWord(word, horizontalStart, true, wordNumber, grid, placements);
                            return true;
                        }
                        
                        // Look for next occurrence of this character
                        indexInNewWord = word.IndexOf(intersectChar, indexInNewWord + 1);
                    }
                }
            }
            
            return false;
        }
        
        private static bool TryPlaceVertically(string word, int wordNumber, char[,] grid, List<WordPlacement> placements, int columns, int rows)
        {
            // Simple vertical placement - just find first valid placement
            foreach (var placedWord in placements)
            {
                string placed = placedWord.word;
                Vector2Int startPos = placedWord.startPosition;
                bool isHorizontal = placedWord.isHorizontal;
                
                for (int i = 0; i < placed.Length; i++)
                {
                    char intersectChar = placed[i];
                    int indexInNewWord = word.IndexOf(intersectChar);
                    
                    while (indexInNewWord >= 0)
                    {
                        Vector2Int intersectionPoint;
                        if (isHorizontal)
                        {
                            intersectionPoint = new Vector2Int(startPos.x + i, startPos.y);
                        }
                        else
                        {
                            intersectionPoint = new Vector2Int(startPos.x, startPos.y + i);
                        }
                        
                        // Try vertical placement
                        Vector2Int verticalStart = new Vector2Int(
                            intersectionPoint.x, 
                            intersectionPoint.y - indexInNewWord
                        );
                        
                        if (CanPlaceWord(word, verticalStart, false, grid, columns, rows))
                        {
                            PlaceWord(word, verticalStart, false, wordNumber, grid, placements);
                            return true;
                        }
                        
                        // Look for next occurrence of this character
                        indexInNewWord = word.IndexOf(intersectChar, indexInNewWord + 1);
                    }
                }
            }
            
            return false;
        }
        
        private static bool TryForcedPlacement(string word, int wordNumber, bool preferHorizontal, char[,] grid, List<WordPlacement> placements, int columns, int rows)
        {
            // Try placing the word anywhere valid as a last resort
            // First try horizontal (if preferred)
            if (preferHorizontal)
            {
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns - word.Length + 1; x++)
                    {
                        Vector2Int start = new Vector2Int(x, y);
                        if (CanPlaceWord(word, start, true, grid, columns, rows))
                        {
                            PlaceWord(word, start, true, wordNumber, grid, placements);
                            return true;
                        }
                    }
                }
            }
            
            // If horizontal failed (or not preferred), try vertical
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows - word.Length + 1; y++)
                {
                    Vector2Int start = new Vector2Int(x, y);
                    if (CanPlaceWord(word, start, false, grid, columns, rows))
                    {
                        PlaceWord(word, start, false, wordNumber, grid, placements);
                        return true;
                    }
                }
            }
            
            // If we still failed and tried horizontal first, now try vertical places
            if (preferHorizontal)
            {
                // Already tried vertical above
                return false;
            }
            else
            {
                // Try horizontal as last resort
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns - word.Length + 1; x++)
                    {
                        Vector2Int start = new Vector2Int(x, y);
                        if (CanPlaceWord(word, start, true, grid, columns, rows))
                        {
                            PlaceWord(word, start, true, wordNumber, grid, placements);
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        // Helper method to check if we can add another vertical word while maintaining the desired ratio
        private static bool CanAddVerticalWord(int horizontalWords, int verticalWords, int wordLength)
        {
            // Allow vertical placement if horizontal words are at least 1-2 more than vertical words
            // For longer words, require an even greater horizontal-to-vertical ratio
            if (wordLength >= 7)
            {
                // Very long words require even more horizontal words
                return (horizontalWords >= verticalWords + 3);
            }
            else if (wordLength >= 5)
            {
                // Medium-long words require slightly more horizontal words
                return (horizontalWords >= verticalWords + 2);
            }
            else
            {
                // Shorter words use the standard ratio
                return (horizontalWords >= verticalWords + 1);
            }
        }

        public static bool CanPlaceWord(string word, Vector2Int start, bool isHorizontal, char[,] grid, int columns, int rows, List<WordPlacement> existingPlacements = null)
        {
            // Check if the word would fit within the grid boundaries
            if (isHorizontal)
            {
                if (start.x < 0 || start.x + word.Length > columns || start.y < 0 || start.y >= rows)
                    return false;
            }
            else
            {
                if (start.x < 0 || start.x >= columns || start.y < 0 || start.y + word.Length > rows)
                    return false;
            }

            // CRITICAL FIX: Check for problematic overlaps with existing words
            if (existingPlacements != null && WouldCreateProblematicOverlap(word, start, isHorizontal, existingPlacements))
            {
                return false;
            }

            // Check if the word can be placed (no conflicts with existing words)
            bool hasIntersection = false;
            int intersectionCount = 0;

            for (int i = 0; i < word.Length; i++)
            {
                int x = isHorizontal ? start.x + i : start.x;
                int y = isHorizontal ? start.y : start.y + i;
                
                char existing = grid[x, y];
                
                // If this cell already has a character, it must match
                if (existing != 0)
                {
                    if (existing != word[i])
                        return false;
                    
                    hasIntersection = true;
                    intersectionCount++;
                }
                else
                {
                    // Check adjacent cells perpendicular to word direction
                    if (isHorizontal)
                    {
                        // Check above and below for horizontal words
                        if ((y > 0 && grid[x, y - 1] != 0) || 
                            (y < rows - 1 && grid[x, y + 1] != 0))
                            return false;
                    }
                    else
                    {
                        // Check left and right for vertical words
                        if ((x > 0 && grid[x - 1, y] != 0) || 
                            (x < columns - 1 && grid[x + 1, y] != 0))
                            return false;
                    }
                }
                
                // Check if the cell before or after the word is empty (to avoid words running into each other)
                if (i == 0)
                {
                    int prevX = isHorizontal ? x - 1 : x;
                    int prevY = isHorizontal ? y : y - 1;
                    
                    if (prevX >= 0 && prevY >= 0 && grid[prevX, prevY] != 0)
                        return false;
                }
                
                if (i == word.Length - 1)
                {
                    int nextX = isHorizontal ? x + 1 : x;
                    int nextY = isHorizontal ? y : y + 1;
                    
                    if (nextX < columns && nextY < rows && grid[nextX, nextY] != 0)
                        return false;
                }
            }

            // CRITICAL FIX: Prevent words that overlap too much with existing content
            // If more than 50% of the word already exists in the grid, this is likely
            // a problematic overlap (like "swipe" vs "wipe")
            if (hasIntersection && intersectionCount > word.Length / 2)
            {
                return false;
            }

            // At least one intersection is required (except for the first word)
            return grid.GetLength(0) == 0 || hasIntersection;
        }

        public static void PlaceWord(string word, Vector2Int start, bool isHorizontal, int wordNumber, char[,] grid, List<WordPlacement> placements)
        {
            // Place the word on the grid
            for (int i = 0; i < word.Length; i++)
            {
                int x = isHorizontal ? start.x + i : start.x;
                int y = isHorizontal ? start.y : start.y + i;
                
                grid[x, y] = word[i];
            }
            
            // Add to placed words list
            placements.Add(new WordPlacement
            {
                word = word,
                startPosition = start,
                isHorizontal = isHorizontal,
                wordNumber = wordNumber
            });
        }

        // Also update CalculateGridBounds to better handle larger dimensions
        public static void CalculateGridBounds(char[,] grid, out Vector2Int min, out Vector2Int max)
        {
            int columns = grid.GetLength(0);
            int rows = grid.GetLength(1);
            
            // Find the minimum and maximum coordinates used in the grid
            min = new Vector2Int(columns, rows);
            max = new Vector2Int(0, 0);
            
            bool foundAnyCell = false;
            
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (grid[x, y] != 0)
                    {
                        min.x = Mathf.Min(min.x, x);
                        min.y = Mathf.Min(min.y, y);
                        max.x = Mathf.Max(max.x, x);
                        max.y = Mathf.Max(max.y, y);
                        foundAnyCell = true;
                    }
                }
            }
            
            // If no cells are used, default to center
            if (!foundAnyCell)
            {
                min = new Vector2Int(columns/2, rows/2);
                max = new Vector2Int(columns/2, rows/2);
            }
            
        }

        // Helper method to check if a word can be placed without requiring intersections
        private static bool IsValidPlacementIgnoringIntersection(string word, Vector2Int start, bool isHorizontal, char[,] grid, int columns, int rows)
        {
            // Check if the word would fit within the grid boundaries
            if (isHorizontal)
            {
                if (start.x < 0 || start.x + word.Length > columns || start.y < 0 || start.y >= rows)
                    return false;
            }
            else
            {
                if (start.x < 0 || start.x >= columns || start.y < 0 || start.y + word.Length > rows)
                    return false;
            }

            // Check if the placement doesn't conflict with existing words
            for (int i = 0; i < word.Length; i++)
            {
                int x = isHorizontal ? start.x + i : start.x;
                int y = isHorizontal ? start.y : start.y + i;
                
                char existing = grid[x, y];
                
                // If this cell already has a character, it must match
                if (existing != 0 && existing != word[i])
                    return false;
                
                // Check adjacent cells perpendicular to word direction
                if (isHorizontal)
                {
                    // Check above and below for horizontal words
                    if ((y > 0 && grid[x, y - 1] != 0) || 
                        (y < rows - 1 && grid[x, y + 1] != 0))
                        return false;
                }
                else
                {
                    // Check left and right for vertical words
                    if ((x > 0 && grid[x - 1, y] != 0) || 
                        (x < columns - 1 && grid[x + 1, y] != 0))
                        return false;
                }
            }

            // Additional check for words not running into each other
            if (isHorizontal)
            {
                // Check if there's a character before the word
                if (start.x > 0 && grid[start.x - 1, start.y] != 0)
                    return false;
                
                // Check if there's a character after the word
                if (start.x + word.Length < columns && grid[start.x + word.Length, start.y] != 0)
                    return false;
            }
            else
            {
                // Check if there's a character before the word
                if (start.y > 0 && grid[start.x, start.y - 1] != 0)
                    return false;
                
                // Check if there's a character after the word
                if (start.y + word.Length < rows && grid[start.x, start.y + word.Length] != 0)
                    return false;
            }
            
            return true;
        }

        // Emergency placement - only checks that we don't directly clash with an existing letter
        private static bool CanPlaceWordVeryAggressively(string word, Vector2Int start, bool isHorizontal, char[,] grid, int columns, int rows)
        {
            // Check if the word would fit within the grid boundaries
            if (isHorizontal)
            {
                if (start.x < 0 || start.x + word.Length > columns || start.y < 0 || start.y >= rows)
                    return false;
            }
            else
            {
                if (start.x < 0 || start.x >= columns || start.y < 0 || start.y + word.Length > rows)
                    return false;
            }

            // Only check for direct conflicts, ignore adjacency rules
            for (int i = 0; i < word.Length; i++)
            {
                int x = isHorizontal ? start.x + i : start.x;
                int y = isHorizontal ? start.y : start.y + i;
                
                char existing = grid[x, y];
                
                // If this cell already has a different character, we can't place the word
                if (existing != 0 && existing != word[i])
                    return false;
            }
            
            return true;
        }

        /// <summary>
        /// Check for overlapping words that start at the same position with different orientations.
        /// This can cause issues when grid is manually edited and one of the overlapping words gets broken.
        /// </summary>
        /// <param name="placements">List of word placements to check</param>
        /// <returns>True if problematic overlaps were found that should trigger regeneration</returns>
        private static bool CheckForOverlappingWords(List<WordPlacement> placements)
        {
            // Check for words starting at the same position
            var sameStartGroups = placements
                .GroupBy(p => p.startPosition)
                .Where(g => g.Count() > 1)
                .ToList();

            return sameStartGroups.Count > 0;
        }

        // Add method to check for problematic same-orientation overlaps
        public static bool WouldCreateProblematicOverlap(string word, Vector2Int start, bool isHorizontal, List<WordPlacement> existingPlacements)
        {
            foreach (var existing in existingPlacements)
            {
                // Only check words with the same orientation
                if (existing.isHorizontal == isHorizontal)
                {
                    // Check if the words would overlap in a problematic way
                    if (isHorizontal)
                    {
                        // Both horizontal - check if they're on the same row
                        if (existing.startPosition.y == start.y)
                        {
                            int existingEnd = existing.startPosition.x + existing.word.Length - 1;
                            int newEnd = start.x + word.Length - 1;
                            
                            // Check for overlap
                            bool overlaps = !(existingEnd < start.x || newEnd < existing.startPosition.x);
                            
                            if (overlaps)
                            {
                                // Calculate overlap amount
                                int overlapStart = Math.Max(existing.startPosition.x, start.x);
                                int overlapEnd = Math.Min(existingEnd, newEnd);
                                int overlapLength = overlapEnd - overlapStart + 1;
                                
                                // If overlap is more than 1 character, it's problematic
                                if (overlapLength > 1)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Both vertical - check if they're on the same column
                        if (existing.startPosition.x == start.x)
                        {
                            int existingEnd = existing.startPosition.y + existing.word.Length - 1;
                            int newEnd = start.y + word.Length - 1;
                            
                            // Check for overlap
                            bool overlaps = !(existingEnd < start.y || newEnd < existing.startPosition.y);
                            
                            if (overlaps)
                            {
                                // Calculate overlap amount
                                int overlapStart = Math.Max(existing.startPosition.y, start.y);
                                int overlapEnd = Math.Min(existingEnd, newEnd);
                                int overlapLength = overlapEnd - overlapStart + 1;
                                
                                // If overlap is more than 1 character, it's problematic
                                if (overlapLength > 1)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Find all possible placements for a word (both horizontal and vertical intersections)
        /// </summary>
        private static List<(Vector2Int position, bool isHorizontal)> FindAllPossiblePlacements(string word, char[,] grid, List<WordPlacement> placements, int columns, int rows, bool forceHorizontal)
        {
            var possiblePlacements = new List<(Vector2Int position, bool isHorizontal)>();
            
            // Find all horizontal placements
            foreach (var placedWord in placements)
            {
                string placed = placedWord.word;
                Vector2Int startPos = placedWord.startPosition;
                bool isHorizontal = placedWord.isHorizontal;
                
                for (int i = 0; i < placed.Length; i++)
                {
                    char intersectChar = placed[i];
                    int indexInNewWord = word.IndexOf(intersectChar);
                    
                    while (indexInNewWord >= 0)
                    {
                        Vector2Int intersectionPoint;
                        if (isHorizontal)
                        {
                            intersectionPoint = new Vector2Int(startPos.x + i, startPos.y);
                        }
                        else
                        {
                            intersectionPoint = new Vector2Int(startPos.x, startPos.y + i);
                        }
                        
                        // Try horizontal placement
                        Vector2Int horizontalStart = new Vector2Int(
                            intersectionPoint.x - indexInNewWord, 
                            intersectionPoint.y
                        );
                        
                        if (CanPlaceWord(word, horizontalStart, true, grid, columns, rows))
                        {
                            possiblePlacements.Add((horizontalStart, true));
                        }
                        
                        // Try vertical placement (if not forcing horizontal)
                        if (!forceHorizontal)
                        {
                            Vector2Int verticalStart = new Vector2Int(
                                intersectionPoint.x, 
                                intersectionPoint.y - indexInNewWord
                            );
                            
                            if (CanPlaceWord(word, verticalStart, false, grid, columns, rows))
                            {
                                possiblePlacements.Add((verticalStart, false));
                            }
                        }
                        
                        // Look for next occurrence of this character
                        indexInNewWord = word.IndexOf(intersectChar, indexInNewWord + 1);
                    }
                }
            }
            
            return possiblePlacements;
        }
        
        /// <summary>
        /// Count the actual number of intersections a word would create if placed at a specific position
        /// </summary>
        private static int CountActualIntersections(string word, Vector2Int startPos, bool isHorizontal, List<WordPlacement> placements)
        {
            int intersectionCount = 0;
            
            // Check each character position of the word
            for (int i = 0; i < word.Length; i++)
            {
                Vector2Int charPos;
                if (isHorizontal)
                {
                    charPos = new Vector2Int(startPos.x + i, startPos.y);
                }
                else
                {
                    charPos = new Vector2Int(startPos.x, startPos.y + i);
                }
                
                // Check if this position intersects with any existing word
                foreach (var placement in placements)
                {
                    // Only count intersections with perpendicular words
                    if (placement.isHorizontal == isHorizontal)
                        continue;
                        
                    Vector2Int placementStart = placement.startPosition;
                    
                    // Check if this character position intersects with the existing word
                    bool intersects = false;
                    if (placement.isHorizontal)
                    {
                        // Existing word is horizontal, check if our vertical word crosses it
                        if (charPos.y == placementStart.y && 
                            charPos.x >= placementStart.x && 
                            charPos.x < placementStart.x + placement.word.Length)
                        {
                            intersects = true;
                        }
                    }
                    else
                    {
                        // Existing word is vertical, check if our horizontal word crosses it
                        if (charPos.x == placementStart.x && 
                            charPos.y >= placementStart.y && 
                            charPos.y < placementStart.y + placement.word.Length)
                        {
                            intersects = true;
                        }
                    }
                    
                    if (intersects)
                    {
                        intersectionCount++;
                        break; // Only count one intersection per character position
                    }
                }
            }
            
            return intersectionCount;
        }
    }
}
