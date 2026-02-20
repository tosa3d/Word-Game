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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using WordsToolkit.Scripts.Levels;
using DG.Tweening;
using VContainer;
using VContainer.Unity;
using WordsToolkit.Scripts.Audio;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Gameplay.WordValidator;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Infrastructure.Service;
using WordsToolkit.Scripts.NLP;
using WordsToolkit.Scripts.System;
using WordsToolkit.Scripts.Utilities;

namespace WordsToolkit.Scripts.Gameplay.Managers
{
    public class FieldManager : MonoBehaviour, IHideableForWin, IShowable
    {
        public Tile tile;
        public int gridSize = 20;
        public float tileSize = 50f;
        public float spacing = 5f;
        public CrosswordGenerationConfigSO crosswordConfig;

        private char[,] grid;
        private List<WordPlacement> placedWords = new List<WordPlacement>();
        private Dictionary<Vector2Int, Tile> tileMap = new Dictionary<Vector2Int, Tile>();
        public List<Tile> allTiles = new List<Tile>();
        public TextMeshProUGUI characterPrefab;
        private Queue<TextMeshProUGUI> characterPool = new Queue<TextMeshProUGUI>();
        private HashSet<string> openedWords = new HashSet<string>(StringComparer.Ordinal);
        private Level levelData;

        [SerializeField]
        private Transform extraWordPositionTransform;
        [SerializeField]
        private Transform specialItemsContainer;
        [SerializeField]
        private GameObject defaultSpecialItemPrefab;

        public UnityEvent OnAllTilesOpened = new UnityEvent();
        public UnityEvent OnAllRequiredWordsFound = new UnityEvent();
        public UnityEvent<string> OnExtraWordFound = new UnityEvent<string>();

        private IWordValidator wordValidator;
        private IGameStateManager gameStateManager;

        private GameManager gameManager;
        private IModelController modelController;
        private ICustomWordRepository customWordRepo;
        private IObjectResolver resolver;
        private IExtraWordService extraWordService;

        [SerializeField]
        private WordBubble wordBubblePrefab;

        [SerializeField]
        private Transform extraWordAlreadyFoundPosition;
        private IAudioService audioService;
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private AudioClip extrawordSound;

        [SerializeField]
        private RectTransform fieldRect;
        [Inject]
        public void Construct(
            GameManager gameManager,
            IModelController modelController,
            ICustomWordRepository customWordRepo,
            IObjectResolver resolver,
            IExtraWordService extraWordService,
            IAudioService audioService, ButtonViewController buttonViewController)
        {
            this.gameManager = gameManager;
            this.modelController = modelController;
            this.customWordRepo = customWordRepo;
            this.resolver = resolver;
            this.extraWordService = extraWordService;
            this.audioService = audioService;
            buttonViewController.RegisterButton(this);
        }

        public TextMeshProUGUI GetPooledCharacter()
        {
            if (characterPool.Count > 0)
            {
                var character = characterPool.Dequeue();
                character.transform.SetAsLastSibling();
                character.gameObject.SetActive(true);
                return character;
            }

            var newChar = Instantiate(characterPrefab, transform);
            return newChar;
        }

        // Return character to the pool
        public void ReturnToPool(TextMeshProUGUI character)
        {
            if (character == null) return;

            character.gameObject.SetActive(false);
            characterPool.Enqueue(character);
        }

        public void Generate(Level levelData, string language)
        {
            wordValidator = new DefaultWordValidator(modelController, customWordRepo, levelData);
            gameStateManager = new DefaultGameStateManager(gameManager, levelData);
            this.levelData = levelData;
            var words = levelData.GetWords(language);
            if (words == null || words.Length == 0)
                return;
            // Add level words to custom repository so they are recognized even if not in model
            customWordRepo.InitWords(words, language);

            // Clear opened words when generating a new level
            openedWords.Clear();

            // Clear any existing tiles
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // Initialize collections
            tileMap.Clear();
            allTiles.Clear();

            // First, try to load saved crossword data from the level
            var languageData = levelData.GetLanguageData(language);
            bool useSavedData = false;

            if (languageData != null && languageData.crosswordData != null)
            {
                useSavedData = LoadSavedCrosswordData(languageData.crosswordData, words);
            }

            // If we couldn't load saved data, generate a new crossword
            if (!useSavedData)
            {
                // Load config if not set
                if (crosswordConfig == null)
                {
                    crosswordConfig = Resources.Load<CrosswordGenerationConfigSO>("Settings/CrosswordConfig");
                }

                // Use the CrosswordGenerator with configuration
                bool success;
                if (crosswordConfig != null)
                {
                    // Use configuration-based generation
                    success = CrosswordGenerator.RegenerateCrossword(words, crosswordConfig, out grid, out placedWords);
                }
                else
                {
                    // Fallback to legacy method for backward compatibility
                    success = CrosswordGenerator.GenerateCrossword(words, gridSize, out grid, out placedWords);
                }

                if (!success)
                {
                    Debug.LogError("Could not place any words. Check word list.");
                    return;
                }
            }

            // Calculate center offset for the grid
            CrosswordGenerator.CalculateGridBounds(grid, out Vector2Int min, out Vector2Int max);
            Vector2Int gridCenter = new Vector2Int(
                (min.x + max.x) / 2,
                (min.y + max.y) / 2
            );

            // Start coroutine to wait for layout calculation before creating visual grid
            StartCoroutine(CreateVisualGridWhenReady(gridCenter, levelData));
        }

        private IEnumerator CreateVisualGridWhenReady(Vector2Int gridCenter, Level levelData)
        {
            // Wait for the end of frame to ensure layout calculations are completed
            yield return new WaitForEndOfFrame();

            // Wait one more frame to be extra sure
            yield return null;

            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

            // Wait one more frame after forcing rebuild
            yield return null;

            // Now create the visual grid
            CreateVisualGrid(gridCenter, levelData);
            foreach (Tile tile in allTiles)
            {
                tile.SetTileClosed();
            }
        }

        private void CreateVisualGrid(Vector2Int gridCenter, Level levelData)
        {
            // Calculate the used grid dimensions
            CrosswordGenerator.CalculateGridBounds(grid, out Vector2Int minBounds, out Vector2Int maxBounds);
            int gridWidth = maxBounds.x - minBounds.x + 1;
            int gridHeight = maxBounds.y - minBounds.y + 1;

            // Get the field's RectTransform to determine available space
            RectTransform fieldRect = GetComponent<RectTransform>();

            // Get the parent canvas scale
            float canvasScale = 1f;
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvasScale = parentCanvas.scaleFactor;
            }

            // Calculate available space with margins
            float totalMargin = 20f * 2; // 20 pixels margin on each side
            float availableWidth = fieldRect.rect.width - totalMargin;
            float availableHeight = fieldRect.rect.height - totalMargin;

            // Ensure we have valid dimensions, otherwise use fallback values
            if (availableWidth <= 0 || availableHeight <= 0)
            {
                availableWidth = 1000f;
                availableHeight = 1000f;
            }

            // Calculate cell size based on available space and grid dimensions
            float cellSize = Mathf.Min(
                availableWidth / gridWidth,
                availableHeight / gridHeight
            );

            // Apply reasonable limits
            cellSize = Mathf.Clamp(cellSize, 40f, 300);

            // Calculate spacing as a percentage of cell size
            spacing = cellSize * -0.01f; // 10% of cell size

            // Get original tile size from prefab for positioning calculations
            RectTransform tilePrefabRect = tile.GetComponent<RectTransform>();
            float originalTileSize = tilePrefabRect != null ? tilePrefabRect.sizeDelta.x : 50f; // fallback to 50f

            // Calculate grid positioning offsets using original tile size
            float totalWidth = gridWidth * originalTileSize + (gridWidth - 1) * spacing;
            float totalHeight = gridHeight * originalTileSize + (gridHeight - 1) * spacing;
            float startX = -totalWidth / 2f;
            float startY = totalHeight / 2f;

            // Fill the grid with tiles or placeholders
            int gridX = 0, gridY = 0;
            for (int y = minBounds.y; y <= maxBounds.y; y++)
            {
                gridX = 0;
                for (int x = minBounds.x; x <= maxBounds.x; x++)
                {
                    if (grid[x, y] != 0)
                    {
                        // Create a tile with letter using VContainer
                        Tile newTile = resolver.Instantiate(tile, transform);
                        newTile.SetColors(levelData.colorsTile);
                        newTile.SetCharacter(grid[x, y]);

                        // Position tile manually without changing its size
                        RectTransform tileRect = newTile.GetComponent<RectTransform>();
                        // Keep original tile size, just position it

                        float posX = startX + gridX * (originalTileSize + spacing);
                        float posY = startY - gridY * (originalTileSize + spacing);
                        tileRect.anchoredPosition = new Vector2(posX, posY);

                        // Store tile in the map
                        tileMap[new Vector2Int(x, y)] = newTile;
                        allTiles.Add(newTile);

                    }
                    gridX++;
                }
                gridY++;
            }
            // Force canvas update

            // Calculate center of the created tiles grid
            if (allTiles.Count > 0)
            {
                // Find the actual bounds of placed tiles
                float minX = float.MaxValue, maxX = float.MinValue;
                float minY = float.MaxValue, maxY = float.MinValue;

                foreach (var tile in allTiles)
                {
                    Vector2 tilePos = tile.GetComponent<RectTransform>().anchoredPosition;
                    minX = Mathf.Min(minX, tilePos.x);
                    maxX = Mathf.Max(maxX, tilePos.x);
                    minY = Mathf.Min(minY, tilePos.y);
                    maxY = Mathf.Max(maxY, tilePos.y);
                }

                // Calculate the actual center of the tiles grid
                Vector2 tilesGridCenter = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);

                // Get the field's rect center (use actual rect center, not zero)
                Vector2 fieldCenter = fieldRect.rect.center; // Use actual field rect center

                // Calculate the offset needed to center the grid precisely
                Vector2 centerOffset = fieldCenter - tilesGridCenter;

                // Apply the centering offset to each individual tile instead of moving the entire transform
                foreach (var tile in allTiles)
                {
                    RectTransform tileRect = tile.GetComponent<RectTransform>();
                    tileRect.anchoredPosition += centerOffset;
                }

                // Calculate the scale needed to fit the grid into the field rect
                float actualGridWidth = maxX - minX + originalTileSize; // Add tile size to account for tile dimensions
                float actualGridHeight = maxY - minY + originalTileSize;

                // Calculate available space with margins
                float marginPercent = 0.001f; // 10% margin
                float usableWidth = fieldRect.rect.width * (1f - marginPercent);
                float usableHeight = fieldRect.rect.height * (1f - marginPercent);

                // Calculate scale factors for both dimensions
                float scaleX = usableWidth / actualGridWidth;
                float scaleY = usableHeight / actualGridHeight;

                // Use the smaller scale to ensure the grid fits in both dimensions
                float finalScale = Mathf.Min(scaleX, scaleY);

                // Apply reasonable limits to prevent extreme scaling
                finalScale = Mathf.Clamp(finalScale, 0.1f, 3.0f);

                // Apply the scale to the transform
                transform.localScale = Vector3.one * finalScale;

                // After scaling, apply a final centering correction for maximum precision
                RectTransform thisRect = GetComponent<RectTransform>();

                // Recalculate actual centers after scaling
                float finalMinX = float.MaxValue, finalMaxX = float.MinValue;
                float finalMinY = float.MaxValue, finalMaxY = float.MinValue;

                foreach (var tile in allTiles)
                {
                    Vector3 globalTilePos = tile.GetComponent<RectTransform>().TransformPoint(Vector3.zero);
                    finalMinX = Mathf.Min(finalMinX, globalTilePos.x);
                    finalMaxX = Mathf.Max(finalMaxX, globalTilePos.x);
                    finalMinY = Mathf.Min(finalMinY, globalTilePos.y);
                    finalMaxY = Mathf.Max(finalMaxY, globalTilePos.y);
                }

                Vector3 finalTilesGlobalCenter = new Vector3((finalMinX + finalMaxX) / 2f, (finalMinY + finalMaxY) / 2f, 0);
                Vector3 finalFieldGlobalCenter = thisRect.TransformPoint(fieldCenter);

                // Apply micro-adjustment to the entire transform if needed
                Vector3 finalCenteringOffset = finalFieldGlobalCenter - finalTilesGlobalCenter;
                if (finalCenteringOffset.magnitude > 0.01f) // Only adjust if difference is significant
                {
                    // Convert global offset back to local space and apply
                    Vector3 localOffset = thisRect.InverseTransformDirection(finalCenteringOffset);
                    thisRect.anchoredPosition += new Vector2(localOffset.x, localOffset.y);
                }
            }

            // Associate tiles with their words
            AssociateTilesWithWords();

            // Store the current tile size for later reference
            tileSize = cellSize;
        }

        private void AssociateTilesWithWords()
        {
            // For each word placement, find and store its tiles
            foreach (var wordPlacement in placedWords)
            {
                var tilesList = wordPlacement.tiles as List<Tile>;
                tilesList?.Clear(); // Clear without casting to avoid type errors

                // Create a new typed list and copy to the object list
                List<Tile> typedTiles = new List<Tile>();

                for (int i = 0; i < wordPlacement.word.Length; i++)
                {
                    int x = wordPlacement.isHorizontal ? wordPlacement.startPosition.x + i : wordPlacement.startPosition.x;
                    int y = wordPlacement.isHorizontal ? wordPlacement.startPosition.y : wordPlacement.startPosition.y + i;
                    Vector2Int pos = new Vector2Int(x, y);

                    if (tileMap.TryGetValue(pos, out Tile tile))
                    {
                        typedTiles.Add(tile);
                        tilesList?.Add(tile);
                    }
                }
            }
        }

        private void ShakeOpenedWordTiles(WordPlacement wordPlacement)
        {
            foreach (var tileObj in wordPlacement.tiles)
            {
                Tile tile = tileObj as Tile;
                if (tile == null)
                    continue;

                tile.ShakeTile();
            }
        }

        public bool IsWordOpen(string word)
        {
            return openedWords.Contains(word);
        }

        private void AnimateCharacters(WordPlacement wordPlacement, List<Vector3> letterPositions)
        {
            // Convert the generic object list to typed Tile list
            var tiles = wordPlacement.tiles
     .Cast<Tile>()
     .Reverse()
     .ToList();
            int minCount = Mathf.Min(tiles.Count, letterPositions.Count);

            for (int i = 0; i < minCount; i++)
            {
                var fromPos = letterPositions[i];
                Tile tile = tiles[i];
                if (tile == null) continue;

                // Get character from pool
                TextMeshProUGUI animChar = GetPooledCharacter();
                animChar.text = tile.character.text;

                var duration = 0.3f;

                // Set initial position and make visible
                RectTransform rectTransform = animChar.GetComponent<RectTransform>();
                rectTransform.position = fromPos;
                // Store original scale
                Vector3 originalScale = rectTransform.localScale;

                // Create animation sequence
                Sequence animSequence = DOTween.Sequence();

                // Initial slight scale up with bounce effect
                animSequence.Append(rectTransform.DOScale(originalScale * 1.2f, 0.2f)
                    .SetEase(Ease.OutBack));

                // Movement animation
                Tween moveTween = rectTransform.DOMove(tile.transform.position, duration)
                    .SetDelay(0.1f * i)
                    .SetEase(Ease.InOutQuad);
                animSequence.Append(moveTween);

                // Create custom scale animation that peaks at the middle of the movement
                float scaleUpTime = duration / 2;
                float scaleDownTime = duration / 2;

                // Scale up during first half of movement
                animSequence.Join(rectTransform.DOScale(originalScale * 4.5f, scaleUpTime)
                    .SetEase(Ease.OutQuad));

                // Scale down during second half of movement while also fading out (happening simultaneously)
                animSequence.Append(rectTransform.DOScale(originalScale, scaleDownTime)
                    .SetEase(Ease.InQuad));
                animSequence.Join(animChar.DOFade(0, scaleDownTime).SetEase(Ease.InQuad));

                // Capture the current tile and animChar in a local closure to ensure correct references
                TextMeshProUGUI currentAnimChar = animChar;
                Tile currentTile = tile;
                var isLastCharacter = (i == minCount - 1);
                // When this specific character's animation completes
                animSequence.OnComplete(() =>
                {
                    currentTile.SetTileOpen();
                    Color textColor = currentAnimChar.color;
                    textColor.a = 1f;
                    currentAnimChar.color = textColor;
                    audioService.PlayOpenWord();
                    ReturnToPool(currentAnimChar);
                    // Check if all words are open after the last character animation
                    if (isLastCharacter)
                    {
                        EventManager.GetEvent(EGameEvent.WordAnimated).Invoke();
                        CheckAllTilesOpened();
                    }
                });
            }

            // Make sure any remaining tiles are opened if we don't have positions for them
            for (int i = minCount; i < tiles.Count; i++)
            {
                tiles[i]?.SetTileOpen();
            }
        }

        // New method that validates a word with the ModelController and opens it if valid
        public bool ValidateWord(string word, List<Vector3> letterPositions = null)
        {
            if (string.IsNullOrEmpty(word))
                return false;

            // For Persian RTL: check both the word as typed and its reverse
            string wordReversed = PersianLanguageUtility.Reverse(word);
            if (!wordValidator.IsWordKnown(word, gameStateManager.CurrentLanguage) &&
                !wordValidator.IsWordKnown(wordReversed, gameStateManager.CurrentLanguage))
                return false;

            bool wasOpened = false;

            // For Persian RTL: the user may select letters right-to-left OR left-to-right.
            // PlaceWord stores the word based on language direction in the grid.
            // So we must check BOTH the word and its reverse.
            string reversedWord = PersianLanguageUtility.Reverse(word);

            // Find the word in the placed words list (try both directions)
            WordPlacement wordPlacement = placedWords?.FirstOrDefault(w =>
                w.word.Equals(word, StringComparison.Ordinal) ||
                w.word.Equals(reversedWord, StringComparison.Ordinal));

            // Use the canonical (stored) form for openedWords tracking
            string canonicalWord = wordPlacement?.word ?? word;

            // Check if the word is already open
            bool alreadyOpen = IsWordOpen(canonicalWord);

            // Case 1: Word exists on the field and is already open - shake it
            if (alreadyOpen && wordPlacement != null)
            {
                ShakeOpenedWordTiles(wordPlacement);
                return true;
            }

            // Case 2: Word exists on the field but is not open - animate to tiles
            if (wordPlacement != null)
            {
                openedWords.Add(canonicalWord);

                if (letterPositions != null && letterPositions.Count > 0)
                {
                    AnimateCharacters(wordPlacement, letterPositions);
                }
                else
                {
                    foreach (var tileObj in wordPlacement.tiles)
                    {
                        if (tileObj is Tile tile)
                        {
                            tile.SetTileOpen();
                        }
                    }
                }
                wasOpened = true;

                var levelWords = gameStateManager.GetLevelWords();
                if (levelWords != null)
                {
                    var allRequiredWords = new HashSet<string>(levelWords, StringComparer.Ordinal);
                }
            }
            // Case 3: Word is known but not on the field or is slang - it's an extra word
            else
            {
                var levelWords = gameStateManager.GetLevelWords();
                bool isExtraWord = (levelWords == null ||
                    (!levelWords.Contains(word, StringComparer.Ordinal) &&
                     !levelWords.Contains(reversedWord, StringComparer.Ordinal)));

                if (isExtraWord && letterPositions != null && letterPositions.Count > 0)
                {
                    OnExtraWordFound?.Invoke(word);
                    if (customWordRepo.AddExtraWord(word))
                    {
                        AnimateExtraWord(word, letterPositions);
                        wasOpened = true;
                    }
                    else
                    {
                        ShakeWord(word);
                        audioService.PlayWrong();
                    }
                }
            }

            if (wasOpened && wordPlacement != null)
            {
                foreach (var tileObj in wordPlacement.tiles)
                {
                    if (tileObj is Tile tile && tile.HasSpecialItem(out Vector2Int itemPosition))
                    {
                        StartCoroutine(CollectSpecialItemDelayed(itemPosition, 0.5f));
                    }
                }
            }

            return wasOpened || alreadyOpen;
        }

        private void ShakeWord(string word)
        {
            // Implement the shake animation or effect here
            var wordBubbleObject = Instantiate(wordBubblePrefab, transform);
            wordBubbleObject.transform.position = extraWordAlreadyFoundPosition.position;
            wordBubbleObject.SetWord(word);

        }

        // New method to animate extra words (words not on the field)
        private void AnimateExtraWord(string word, List<Vector3> letterPositions)
        {
            if (letterPositions == null || letterPositions.Count == 0)
                return;

            // Use the extraWordPositionTransform if available, otherwise fall back to hardcoded position
            Vector3 targetPosition;
            if (extraWordPositionTransform != null)
            {
                targetPosition = extraWordPositionTransform.position;
            }
            else
            {
                // Fallback to hardcoded position
                targetPosition = new Vector3(Screen.width * 0.5f, Screen.height * 0.85f, 0);
                Debug.LogWarning("extraWordPositionTransform not assigned! Using default screen position.");
            }

            audioService.PlayDelayed(extrawordSound, 0.5f);

            for (int i = 0; i < word.Length && i < letterPositions.Count; i++)
            {
                var fromPos = letterPositions[i];
                char letter = word[i];

                // Get character from pool
                TextMeshProUGUI animChar = GetPooledCharacter();
                animChar.text = letter.ToString();

                var duration = 0.7f;

                // Set initial position and make visible
                RectTransform rectTransform = animChar.GetComponent<RectTransform>();
                rectTransform.position = fromPos;

                // Store original scale
                Vector3 originalScale = rectTransform.localScale;

                // Create animation sequence
                Sequence animSequence = DOTween.Sequence();

                // Initial slight scale up with bounce effect
                animSequence.Append(rectTransform.DOScale(originalScale * 1.2f, 0.2f)
                    .SetEase(Ease.OutBack));

                // Calculate a mid-point for the arc
                Vector3 midPoint = (fromPos + targetPosition) / 2f;

                // Determine arc height based on the distance between points
                float arcHeight = Vector3.Distance(fromPos, targetPosition) * 0.3f; // 30% of the distance

                midPoint.y += arcHeight; // In screen space, higher Y is lower on screen

                // Create a path array with control points for the arc
                Vector3[] arcPath = new Vector3[] {
                    fromPos,
                    midPoint,
                    targetPosition
                };

                // Calculate half duration for scaling
                float halfDuration = duration / 2f;

                // Create path animation
                var pathTween = rectTransform.DOPath(
                    arcPath,
                    duration,
                    PathType.CatmullRom)
                    .SetDelay(0.1f * i)  // Stagger the animations
                    .SetEase(Ease.OutQuad);

                // Add the path animation to the sequence
                animSequence.Append(pathTween);

                // Scale down during the second half
                animSequence.Join(rectTransform.DOScale(originalScale * 0.1f, halfDuration).SetDelay(.2f)
                    .SetEase(Ease.InQuad));

                // Quick fade out at the end
                animSequence.Join(animChar.DOFade(0, 0.3f).SetEase(Ease.InQuad));

                // Capture the current animChar in a closure
                TextMeshProUGUI currentAnimChar = animChar;

                // When animation completes
                animSequence.OnComplete(() =>
                {
                    // Reset alpha
                    Color textColor = currentAnimChar.color;
                    textColor.a = 1f;
                    currentAnimChar.color = textColor;
                    currentAnimChar.transform.localScale = originalScale;
                    // Return to pool
                    ReturnToPool(currentAnimChar);
                    // the last character will trigger the extra word found event
                    if (currentAnimChar.text == word.First().ToString())
                    {
                        EventManager.GetEvent<string>(EGameEvent.ExtraWordFound).Invoke(word);
                    }
                });
            }
        }

        // Enhanced generation method that accepts special item placements
        public void GenerateWithSpecialItems(Level levelData, string language, List<SerializableWordPlacement> specialItemPlacements)
        {
            // First attempt to load or generate the basic crossword
            Generate(levelData, language);

            // Early exit if generation failed (no grid)
            if (grid == null)
            {
                Debug.LogError("Failed to generate or load crossword grid");
                return;
            }

            // Check if we're using a loaded crossword or a newly generated one
            var languageData = levelData.GetLanguageData(language);
            bool isLoadedCrossword = (languageData != null &&
                                     languageData.crosswordData != null &&
                                     languageData.crosswordData.grid != null);

            // Then add special items
            if (specialItemPlacements != null && specialItemPlacements.Count > 0)
            {
                // Filter out non-special items for clarity
                var onlySpecialItems = specialItemPlacements.Where(p => p.isSpecialItem).ToList();

                if (onlySpecialItems.Count > 0)
                {
                    StartCoroutine(AddSpecialItemsWhenReady(onlySpecialItems));
                }
            }
        }

        // Overloaded method to work with the new SerializableSpecialItem format
        public void GenerateWithSpecialItems(Level levelData, string language, List<SerializableSpecialItem> specialItems)
        {
            // First attempt to load or generate the basic crossword
            Generate(levelData, language);

            // Early exit if generation failed (no grid)
            if (grid == null)
            {
                Debug.LogError("Failed to generate or load crossword grid");
                return;
            }

            // Then add special items from the new format
            if (specialItems != null && specialItems.Count > 0)
            {
                StartCoroutine(AddSpecialItemsWhenReady(specialItems));
            }
        }

        // Coroutine to add special items after the grid is fully generated
        private IEnumerator AddSpecialItemsWhenReady(List<SerializableWordPlacement> specialItemPlacements)
        {
            // Wait until the main grid is fully built
            yield return new WaitUntil(() => allTiles.Count > 0 && tileMap.Count > 0);

            // Add each special item
            foreach (var placement in specialItemPlacements)
            {
                if (!placement.isSpecialItem) continue;

                // Only place special items above letters
                Vector2Int position = placement.startPosition;

                // Check if we have a tile at this position
                if (!tileMap.TryGetValue(position, out Tile tile))
                {
                    Debug.LogWarning($"Cannot place special item at {position}: No letter tile found.");
                    continue;
                }

                // Just tell the tile to create a special item - it has its own prefab
                tile.AssociateSpecialItem(position);
            }
        }

        // Coroutine to add special items from the new SerializableSpecialItem format
        private IEnumerator AddSpecialItemsWhenReady(List<SerializableSpecialItem> specialItems)
        {
            // Wait until the main grid is fully built
            yield return new WaitUntil(() => allTiles.Count > 0 && tileMap.Count > 0);

            // Add each special item
            foreach (var specialItem in specialItems)
            {
                // Only place special items above letters
                Vector2Int position = specialItem.position;

                // Check if we have a tile at this position
                if (!tileMap.TryGetValue(position, out Tile tile))
                {
                    Debug.LogWarning($"Cannot place special item at {position}: No letter tile found.");
                    continue;
                }

                // Tell the tile to create a special item - it has its own prefab
                tile.AssociateSpecialItem(position);
            }
        }

        // Updated method to load saved crossword data with better logging
        private bool LoadSavedCrosswordData(SerializableCrosswordData savedData, string[] words)
        {
            try
            {
                // Deserialize grid from saved data
                savedData.DeserializeGrid();

                if (savedData.grid == null)
                {
                    Debug.LogWarning("Saved grid data couldn't be loaded (grid is null).");
                    return false;
                }

                // Additional validation - check grid dimensions
                if (savedData.grid.GetLength(0) <= 0 || savedData.grid.GetLength(1) <= 0)
                {
                    Debug.LogWarning($"Invalid grid dimensions: {savedData.grid.GetLength(0)}x{savedData.grid.GetLength(1)}");
                    return false;
                }

                // Set the grid
                grid = savedData.grid;

                // Filter out special items from placements to get just words
                var wordPlacements = savedData.placements;

                // Convert saved placements to runtime placements
                placedWords = new List<WordPlacement>();
                foreach (var savedPlacement in wordPlacements)
                {
                    WordPlacement placement = new WordPlacement
                    {
                        word = savedPlacement.word,
                        wordNumber = savedPlacement.wordNumber,
                        startPosition = savedPlacement.startPosition,
                        isHorizontal = savedPlacement.isHorizontal,
                        tiles = new List<Tile>()
                    };

                    placedWords.Add(placement);
                }

                // Store special items for later processing (after visual grid is created)
                if (savedData.specialItems != null && savedData.specialItems.Count > 0)
                {
                    // We'll add these special items after the visual grid is created
                    StartCoroutine(AddSpecialItemsFromSavedData(savedData.specialItems));
                }

                // Verify the words match what we expect
                var savedWords = placedWords.Select(p => p.word).ToArray();
                var levelWords = words; // Persian: do NOT call ToLower() — it corrupts Persian characters

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load saved crossword data: {ex.Message}");
                return false;
            }
        }

        // Coroutine to add special items from saved crossword data
        private IEnumerator AddSpecialItemsFromSavedData(List<SerializableSpecialItem> specialItems)
        {
            // Wait until the visual grid is fully created
            yield return new WaitUntil(() => allTiles.Count > 0 && tileMap.Count > 0);

            // Add each special item
            foreach (var specialItem in specialItems)
            {
                Vector2Int position = specialItem.position;

                // Check if we have a tile at this position
                if (!tileMap.TryGetValue(position, out Tile tile))
                {
                    Debug.LogWarning($"Cannot place special item at {position}: No letter tile found.");
                    continue;
                }

                // Tell the tile to create a special item
                tile.AssociateSpecialItem(position);
            }
        }

        // Coroutine to collect special item with a delay
        private IEnumerator CollectSpecialItemDelayed(Vector2Int position, float delay)
        {
            yield return new WaitForSeconds(delay);

            // Check if the tile still has a special item
            if (tileMap.TryGetValue(position, out Tile tile) && tile.HasSpecialItem(out _))
            {
                // The tile handles the animation and collection itself now via the SpecialItem component
                // We don't need to do anything here as it will be handled automatically
                // when the tile is opened and the special item is animated.
            }
        }

        /// <summary>
        /// Opens a random closed tile on the field
        /// </summary>
        /// <returns>True if a tile was opened, false if no closed tiles remain</returns>
        public bool OpenRandomTile()
        {
            // Get list of all tiles that are not yet open
            var closedTiles = allTiles.Where(t => t != null && !t.IsOpen()).ToList();

            if (closedTiles.Count > 0)
            {
                // Select random tile from closed tiles
                int randomIndex = UnityEngine.Random.Range(0, closedTiles.Count);
                var selectedTile = closedTiles[randomIndex];
                selectedTile.SetTileOpen();
                selectedTile.ShowEffect();
                audioService.PlayBonus();
                CheckAllTilesOpened();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Opens the first closed tile found on the field
        /// </summary>
        /// <returns>True if a tile was opened, false if no closed tiles remain</returns>
        public bool OpenFirstClosedTile()
        {
            var firstClosedTile = allTiles.FirstOrDefault(t => t != null && !t.IsOpen());

            if (firstClosedTile != null)
            {
                firstClosedTile.SetTileOpen();
                CheckAllTilesOpened();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the number of remaining closed tiles
        /// </summary>
        public int GetClosedTilesCount()
        {
            return allTiles.Count(t => t != null && !t.IsOpen());
        }

        public void Clear()
        {
            // Stop all running coroutines
            StopAllCoroutines();

            // Kill any active DOTween animations
            DOTween.Kill(transform);

            // Clear and destroy all tiles
            foreach (Transform child in transform)
            {
                if (child != null)
                {
                    // Kill any DOTween animations on the child
                    DOTween.Kill(child);
                    Destroy(child.gameObject);
                }
            }

            // Clear collections
            tileMap?.Clear();
            allTiles?.Clear();
            placedWords?.Clear();
            openedWords?.Clear();

            // Clear character pool
            while (characterPool != null && characterPool.Count > 0)
            {
                var character = characterPool.Dequeue();
                if (character != null)
                {
                    DOTween.Kill(character.transform);
                    Destroy(character.gameObject);
                }
            }
            characterPool = new Queue<TextMeshProUGUI>();

            // Reset other variables
            grid = null;
            levelData = null;
            tileSize = 50f; // Reset to default tile size

            // Force immediate layout update
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }

        public bool AreAllTilesOpen()
        {
            if (allTiles == null) return false;

            foreach (var tile in allTiles)
            {
                if (tile != null && !tile.IsOpen())
                {
                    return false;
                }
            }
            return true;
        }

        public void CheckAllTilesOpened()
        {
            if (allTiles == null) return;

            bool allOpened = true;
            foreach (var tile in allTiles)
            {
                if (tile != null && !tile.IsOpen())
                {
                    allOpened = false;
                    break;
                }
            }

            if (allOpened)
            {
                Debug.Log("All tiles have been opened!");
                OnAllTilesOpened?.Invoke();
            }
        }

        private void OnEnable()
        {
            EventManager.GetEvent<Tile>(EGameEvent.TileSelected).Subscribe(OnTileSelected);
        }

        private void OnDisable()
        {
            EventManager.GetEvent<Tile>(EGameEvent.TileSelected).Unsubscribe(OnTileSelected);
        }

        private void OnTileSelected(Tile tile)
        {
            audioService.PlayBonus();
            CheckAllTilesOpened();
        }

        public bool HasSpecialItems()
        {
            if (allTiles == null || allTiles.Count == 0)
                return false;

            // Check if any tile has a special item
            foreach (var tile in allTiles)
            {
                if (tile.HasSpecialItem(out _))
                    return true;
            }
            return false;
        }

        public List<Tile> GetTilesWordWithSpecialItems(out string wordWithSpecialItems)
        {
            List<Tile> tilesWithSpecialItems = new List<Tile>();
            wordWithSpecialItems = string.Empty;
            bool gemFound = false;
            foreach (var words in placedWords)
            {
                tilesWithSpecialItems.Clear(); // Clear previous results
                foreach (var til in words.tiles)
                {
                    tilesWithSpecialItems.Add(til);
                    if (til != null && til.HasSpecialItem(out _))
                    {
                        gemFound = true;
                        wordWithSpecialItems = words.word;
                    }
                }
                if (gemFound)
                {
                    break; // Stop after finding the first special item
                }
            }

            return tilesWithSpecialItems;
        }

        public List<Tile> GetTilesWord(string word)
        {
            var wordPlacement = placedWords.FirstOrDefault(w => w.word.Equals(word, StringComparison.OrdinalIgnoreCase));
            if (wordPlacement != null)
            {
                return wordPlacement.tiles.ToList();
            }
            return new List<Tile>();
        }

        public void HideForWin()
        {
            canvasGroup.DOFade(0, .5f);
        }

        public void Show()
        {
            canvasGroup.DOFade(1, .5f);
        }
    }

}