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

using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using WordsToolkit.Scripts.Data;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Popups;
using WordsToolkit.Scripts.Settings;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.GUI.Buttons
{
    public class GiftButton : BaseGUIButton
    {
        private const string SAVE_KEY = "GiftButton_CollectedItems";
        
        [Header("Counter Settings")]
        [SerializeField] private TextMeshProUGUI counterText;
        [SerializeField] private GameObject counterContainer;
        [SerializeField] private int collectedItems = 0;
        
        [Header("Visual Feedback")]
        [SerializeField] private float pulseScale = 1.2f;
        [SerializeField] private float pulseDuration = 0.3f;

        [Header("Resources")]
        [SerializeField] private ResourceObject resource;
        [Tooltip("Optional override for gems awarded per gift (if not set, uses value from GameSettings)")]
        [SerializeField] private int gemsPerGift = 0;
        [SerializeField] private TextMeshProUGUI gemsValueText;
        [SerializeField] private GameObject gemsLabelObject;

        [Header("Visual Appearance")]
        [SerializeField] private Image mainBoxSprite;
        [SerializeField] private Sprite lightModeSprite;
        [SerializeField] private Sprite darkModeSprite;

        [Inject]
        private MenuManager menuManager;

        [SerializeField]
        private Popup giftPopup;

        [Inject]
        private GameManager gameManager;



        private bool isActive;

        protected override void OnEnable()
        {
            base.OnEnable();
            EventManager.GetEvent(EGameEvent.SpecialItemCollected).Subscribe(OnSpecialItemCollected);
            onClick.AddListener(ConsumeResource);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventManager.GetEvent(EGameEvent.SpecialItemCollected).Unsubscribe(OnSpecialItemCollected);
            onClick.RemoveListener(ConsumeResource);
        }

        protected override void Start()
        {
            base.Start();
            if(!Application.isPlaying)
            {
                return;
            }
            
            // Load saved collected items
            collectedItems = PlayerPrefs.GetInt(SAVE_KEY, 0);
            
            UpdateCounterDisplay();
            UpdateGemsValueDisplay();
            UpdateState();
        }
        
        /// <summary>
        /// Updates the displayed gems value
        /// </summary>
        private void UpdateGemsValueDisplay()
        {
            if (gemsValueText != null)
            {
                int gemsAmount = gemsPerGift > 0 ? 
                    gemsPerGift : 
                    gameSettings.gemsForGift;

                gemsValueText.text = gemsAmount.ToString();
            }
            
            // Update visibility of gems label
            if (gemsLabelObject != null)
            {
                gemsLabelObject.SetActive(collectedItems > 0);
            }
            else if (gemsValueText != null)
            {
                // If no specific label object is assigned, control the text object directly
                gemsValueText.gameObject.SetActive(collectedItems > 0);
            }
        }
        
        /// <summary>
        /// Updates the visual state of the button based on collection status
        /// </summary>
        private void UpdateState()
        {
            isActive = collectedItems > 0;
            interactable = isActive;
        }
        
        /// <summary>
        /// Increments the counter when a special item is collected
        /// </summary>
        public void CollectItem()
        {
            collectedItems++;
            // Save the updated count
            PlayerPrefs.SetInt(SAVE_KEY, collectedItems);
            PlayerPrefs.Save();
            
            UpdateCounterDisplay();
            UpdateGemsValueDisplay();
            UpdateState();
            PlayCollectionFeedback();
        }
        
        /// <summary>
        /// Updates the counter UI text
        /// </summary>
        private void UpdateCounterDisplay()
        {
            if (counterText != null)
            {
                counterText.text = collectedItems.ToString();
            }
            
            // Show counter only if we've collected items
            if (counterContainer != null)
            {
                counterContainer.SetActive(collectedItems > 0);
            }
        }

        private void ConsumeResource()
        {
            // Get the gems amount from GameSettings or use the override if set
            int gemsAmount = gemsPerGift > 0 ? 
                gemsPerGift : 
                gameSettings.gemsForGift;
            
            // Attempt to consume the resource and add gems
            if (resource != null && resourceManager.ConsumeWithEffects(resource,gemsAmount))
            {
                // Decrease the counter
                collectedItems--;
                // Save the updated count
                PlayerPrefs.SetInt(SAVE_KEY, collectedItems);
                PlayerPrefs.Save();
                
                // Update the display
                UpdateCounterDisplay();
                UpdateGemsValueDisplay();
                UpdateState();
                
                menuManager.ShowPopup(giftPopup);

            }
            
        }
        
        /// <summary>
        /// Play visual feedback when item is collected
        /// </summary>
        private void PlayCollectionFeedback()
        {
            // Simple pulse animation
            mainBoxSprite.transform.DOScale(pulseScale, pulseDuration / 2)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => {
                    mainBoxSprite.transform.DOScale(1f, pulseDuration / 2).SetEase(Ease.InQuad);
                });
        }

        // Event handler for when a special item is collected
        private void OnSpecialItemCollected()
        {
            CollectItem();
        }
    }
}