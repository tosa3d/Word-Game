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

using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using WordsToolkit.Scripts.Data;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.Popups
{
    public class ExtraWords : Popup
    {
        [Header("Word Display")]
        // Format for displaying each word in the list
        [Tooltip("Format for each word in the list. Use {0} for the word.")]
        public string wordFormat = "• {0}";
        
        // Separator between words
        [Tooltip("String to separate words in the list")]
        public string separator = "\n";
        
        [Header("Font Size")]
        [Tooltip("Fixed font size for all text")]
        [Range(8, 72)]
        public float fixedFontSize = 60f;
        
        [Tooltip("Enable font size adjustment to fit container")]
        public bool autoFitContainer = true;
        
        [Header("Columns")]
        [Tooltip("List of pre-existing TextMeshPro components to use as columns")]
        public TextMeshProUGUI[] columnTextObjects;
        
        [Tooltip("Maximum lines per column (0 = unlimited)")]
        [Range(0, 30)]
        public int maxLinesPerColumn = 6;

        [Tooltip("Line spacing multiplier")]
        [Range(0.5f, 2f)]
        public float lineSpacingMultiplier = 1f;

        [Header("Rewards")]
        [Tooltip("Button to claim gems for found extra words")]
        public CustomButton claimButton;
        
        [Tooltip("Text to show total gems to be claimed")]
        public TextMeshProUGUI rewardText;
        
        [Tooltip("Reference to the Gems resource")]
        public ResourceObject gemsResource;

        private bool hasClaimedRewards = false;
        [SerializeField]
        private Transform startAnimationTransform;


        protected void OnEnable()
        {
            // Configure text components on enable
            SetupTextComponents();
            UpdateExtraWordsDisplay();
            SetupClaimButton();
        }
        
        private void SetupTextComponents()
        {
            // Apply settings to existing text components
            if (columnTextObjects == null || columnTextObjects.Length == 0)
                return;
            
            foreach (var text in columnTextObjects)
            {
                if (text == null)
                    continue;
                
                // Configure text formatting
                text.enableWordWrapping = true;
                text.overflowMode = TextOverflowModes.Ellipsis;
                
                // Set font size with auto-fitting options
                if (autoFitContainer)
                {
                    text.enableAutoSizing = true;
                    text.fontSizeMax = fixedFontSize;
                    text.fontSizeMin = fixedFontSize * 0.5f;
                }
                else
                {
                    text.fontSize = fixedFontSize;
                    text.enableAutoSizing = false;
                }
                
                // Clear any existing content
                text.text = string.Empty;
                
                // Hide all initially
                text.gameObject.SetActive(false);
            }
        }

        private void SetupClaimButton()
        {
            if (claimButton != null)
            {
                // Reset claim state
                hasClaimedRewards = false;
                
                // Setup button click handler
                claimButton.onClick.RemoveAllListeners();
                claimButton.onClick.AddListener(ClaimExtraWordRewards);
                
                // Get words list
                List<string> words = GetWordsList();
                
                // Calculate total reward
                int totalGems = gameSettings.gemsForExtraWords;
                
                // Show total reward or hide button if no rewards
                int targetExtraWords = GetTargetExtraWordsFromGroup();
                if (PlayerPrefs.GetInt("ExtraWordsCollected",0) >= targetExtraWords)
                {
                    claimButton.gameObject.SetActive(true);
                    
                    // Update reward text if available
                    if (rewardText != null)
                    {
                        rewardText.text = totalGems.ToString();
                    }
                }
                else
                {
                    claimButton.gameObject.SetActive(false);
                }
            }
        }

        private void ClaimExtraWordRewards()
        {
            if (hasClaimedRewards)
                return;
                
            int totalGems = gameSettings.gemsForExtraWords;
            
            if (totalGems <= 0)
                return;
            
            // Mark as claimed
            hasClaimedRewards = true;
            PlayerPrefs.SetInt("ExtraWordsCollected", 0); // Reset count after claiming
            EventManager.GetEvent(EGameEvent.ExtraWordClaimed).Invoke();
            // Use manually assigned reference or fall back to ResourceManager if not assigned
            var gems = gemsResource != null ? gemsResource : resourceManager.GetResource("Gems");
            
            // Add reward with animation
            if (gems != null && claimButton != null)
            {
                gems.AddAnimated( totalGems, startAnimationTransform.position, animationSourceObject: null, callback: () => {
                    claimButton.gameObject.SetActive(false);
                    Close();
                });
            }

        }

#if UNITY_EDITOR
        private void Update()
        {
            // F5 to add a test word
            if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            {
                AddTestWord();
            }
        }
#endif

        // Context menu item for testing reward claim in the Unity editor
        [ContextMenu("Test Reward Claim")]
        private void TestRewardClaim()
        {
            // Reset claim state to ensure we can claim again
            hasClaimedRewards = false;
            
            // Make sure the claim button is visible
            if (claimButton != null)
                claimButton.gameObject.SetActive(true);
                
            // Set up reward text if available
            if (rewardText != null)
            {
                int testAmount = gameSettings.gemsForExtraWords;
                rewardText.text = testAmount.ToString();
            }
            
            // Call the claim method to test the full reward process
            ClaimExtraWordRewards();
        }

        // Context menu item for adding a test word
        [ContextMenu("Add Test Word")]
        private void AddTestWord()
        {
            if (customWordRepository != null)
            {
                customWordRepository.AddExtraWord("blablabla " + Random.Range(1, 1000));
                UpdateExtraWordsDisplay();
                SetupClaimButton();
            }
        }

        // Updates the text components with current extra words
        public void UpdateExtraWordsDisplay()
        {
            // Get word list from game
            List<string> words = GetWordsList();
            
            // Early exit if no columns are assigned
            if (columnTextObjects == null || columnTextObjects.Length == 0)
                return;
                
            // If no words found, display message in first column
            if (words.Count == 0)
            {
                return;
            }
            
            // Otherwise, distribute words among columns
            DistributeWordsToColumns(words);
        }

        private void DistributeWordsToColumns(List<string> words)
        {
            // Count valid columns
            int validColumnCount = 0;
            foreach (var col in columnTextObjects)
            {
                if (col != null)
                    validColumnCount++;
            }
            
            if (validColumnCount == 0)
                return;
                
            int currentColumn = 0;
            int wordIndex = 0;
            
            // First, disable all columns
            foreach (var col in columnTextObjects)
            {
                if (col != null)
                    col.gameObject.SetActive(false);
            }
            
            while (wordIndex < words.Count && currentColumn < columnTextObjects.Length)
            {
                TextMeshProUGUI col = columnTextObjects[currentColumn];
                if (col == null)
                {
                    currentColumn++;
                    continue;
                }
                
                // Show this column
                col.gameObject.SetActive(true);
                
                // Build text for this column
                StringBuilder sb = new StringBuilder();
                int wordCount = 0;
                bool columnIsFull = false;
                
                // Fill this column until max lines or out of words
                while (!columnIsFull && wordIndex < words.Count)
                {
                    // Check if we've reached the max lines for this column
                    if (maxLinesPerColumn > 0 && wordCount >= maxLinesPerColumn)
                    {
                        columnIsFull = true;
                        break;
                    }
                    
                    if (wordCount > 0)
                        sb.Append(separator);
                    sb.Append(string.Format(wordFormat, words[wordIndex]));
                    wordIndex++;
                    wordCount++;
                }
                
                // Set text content
                col.text = sb.ToString();
                
                // Move to next column if we have more words or the current column is full
                if (wordIndex < words.Count || columnIsFull)
                {
                    currentColumn++;
                }
            }
        }
        
        // Gets the list of extra words from the level manager
        private List<string> GetWordsList()
        {
            if (levelManager != null)
            {
                List<string> words = customWordRepository.GetExtraWords().Where(word => word != null).ToList();
                if (words.Count > 24)
                {
                    words = words.Skip(Mathf.Max(0, words.Count - 24)).ToList();
                }
                return words ?? new List<string>();
            }
            
            return new List<string>();
        }

        // Helper method to get target extra words from the current level's group or fallback to game settings
        private int GetTargetExtraWordsFromGroup()
        {
            var currentLevelGroup =  GameDataManager.GetLevel().GetGroup();
            return Mathf.Max(1, currentLevelGroup.targetExtraWords); // Ensure it's at least 1
        }
    }
}