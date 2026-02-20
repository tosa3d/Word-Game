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
using WordsToolkit.Scripts.Settings;
using WordsToolkit.Scripts.Gameplay.Managers;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.System;
using VContainer;
using VContainer.Unity;
using WordsToolkit.Scripts.Audio;
using WordsToolkit.Scripts.System.Haptic;

namespace WordsToolkit.Scripts.Gameplay
{
    public class Tile : FillAndPreview, IPointerClickHandler
    {
        public TextMeshProUGUI character;
        public Image[] images;

        [Header("Tile Colors")]
        public Color[] closedColors;
        private Color[] openColors = new Color[3];

        private bool isSelected = false;
        private bool isOpen = false;

        [Header("Special Item")]
        [SerializeField] private GameObject specialItemPrefab; // Direct reference to the special item prefab

        [Header("Hammer Animation")]
        [SerializeField] private GameObject hammerAnimationPrefab; // Reference to hammer animation prefab

        // Added fields for special item support
        private bool hasSpecialItem = false;
        private Vector2Int specialItemPosition;
        private GameObject specialItemInstance; // Reference to the instantiated special item

        // Simple selection state

        private LevelManager levelManager;
        private FieldManager fieldManager;
        [SerializeField]
        private GameObject fx;

        private IAudioService audioService;
        private IObjectResolver objectResolver;

        [Inject]
        public void Construct(LevelManager levelManager, FieldManager fieldManager, IAudioService audioService, IObjectResolver objectResolver)
        {
            this.levelManager = levelManager;
            this.fieldManager = fieldManager;
            this.audioService = audioService;
            this.objectResolver = objectResolver;
        }

        public void SetColors(ColorsTile colorsTile)
        {
            if (colorsTile == null)
                return;

            openColors[0] = colorsTile.faceColor;
            openColors[1] = colorsTile.topColor;
            openColors[2] = colorsTile.bottomColor;
        }

        // Set the tile to closed state
        public void SetTileClosed()
        {
            isOpen = false;

            // Hide character
            if (character != null)
            {
                character.gameObject.SetActive(false);
            }

            // Apply closed color to all images
            for (var i = 0; i < images.Length; i++)
            {
                var img = images[i];
                if (img != null)
                {
                    img.color = closedColors[i];
                }
            }

            transform.SetAsFirstSibling();
        }

        // Set the tile to open state
        public void SetTileOpen()
        {
            isOpen = true;
            HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticForce.Light);

            // Show character
            if (character != null)
            {
                character.gameObject.SetActive(true);
            }

            // Apply open color to all images
            for (var i = 0; i < images.Length; i++)
            {
                var img = images[i];
                if (img != null)
                {
                    img.color = openColors[i];
                }
            }

            // Add bounce animation
            transform.DOScale(1.2f, 0.1f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    transform.DOScale(1f, 0.15f)
                        .SetEase(Ease.Linear);
                });

            transform.SetAsLastSibling();

            // If this tile has a special item, animate it and ensure it stays on top
            if (hasSpecialItem && specialItemInstance != null)
            {
                specialItemInstance.transform.SetAsLastSibling();

                SpecialItem specialItem = specialItemInstance.GetComponent<SpecialItem>();
                if (specialItem != null)
                {
                    // Try to find a target position - use level manager collection point if available
                    Vector3 targetPosition = transform.position + new Vector3(0, 300, 0); // Default fallback

                    // Get special item collection point if available
                    if (levelManager != null)
                    {
                        var collectionPoint = levelManager.GetSpecialItemCollectionPoint();
                        targetPosition = collectionPoint;
                    }

                    // Start the animation
                    specialItem.FlyToPosition(targetPosition, () =>
                    {
                        // Notify level manager that item was collected
                        if (levelManager != null)
                        {
                            levelManager.CollectSpecialItem(specialItemPosition);
                        }
                        // Clear the reference since the item destroys itself
                        specialItemInstance = null;
                    });
                }
            }

        }

        // Check if tile is open
        public bool IsOpen()
        {
            return isOpen;
        }

        // Set the character for this tile
        public void SetCharacter(char c)
        {
            if (character != null)
            {
                character.text = c.ToString(); // Persian: do NOT call ToUpper() — it corrupts Persian characters
            }
        }

        public void ShakeTile()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
                return;

            Vector2 originalPosition = rectTransform.anchoredPosition;

            Sequence shakeSequence = DOTween.Sequence();

            float shakeAmount = 5f;
            float shakeDuration = 0.05f;
            int shakeCount = 4;

            for (int i = 0; i < shakeCount; i++)
            {
                float xOffset = (i % 2 == 0) ? shakeAmount : -shakeAmount;

                shakeSequence.Append(rectTransform.DOAnchorPos(
                    new Vector2(originalPosition.x + xOffset, originalPosition.y),
                    shakeDuration).SetEase(Ease.OutQuad));
            }


            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                {
                    shakeSequence.Join(images[i].DOColor(Color.white, shakeDuration)
                        .SetLoops(2, LoopType.Yoyo));
                }
            }
            shakeSequence.Append(rectTransform.DOAnchorPos(originalPosition, shakeDuration));
        }

        // Enhanced method to associate a special item with this tile
        public void AssociateSpecialItem(Vector2Int position)
        {
            hasSpecialItem = true;
            specialItemPosition = position;

            // Create the special item instance if we have a prefab
            if (specialItemPrefab != null && specialItemInstance == null)
            {
                InstantiateSpecialItem();
            }

        }

        // Associate with a specific prefab (override the default)
        public void AssociateSpecialItem(Vector2Int position, GameObject itemPrefab)
        {
            // Set the prefab
            specialItemPrefab = itemPrefab;

            // Call the regular association method
            AssociateSpecialItem(position);
        }

        // Create the special item instance
        private void InstantiateSpecialItem()
        {
            if (specialItemPrefab == null)
            {
                // Try to load a default prefab if none is assigned
                specialItemPrefab = Resources.Load<GameObject>("Prefabs/DefaultSpecialItem");

                if (specialItemPrefab == null)
                {
                    Debug.LogWarning("No special item prefab assigned to tile and no default found.");
                    return;
                }
            }

            specialItemInstance = objectResolver.Instantiate(specialItemPrefab, transform);
            specialItemInstance.transform.SetParent(transform.parent);
            specialItemInstance.transform.SetAsLastSibling();
            // fit size to tile
            RectTransform rectTransform = specialItemInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = GetComponent<RectTransform>().sizeDelta / 1.2f;
            }
            if (levelManager != null)
            {
                levelManager.RegisterSpecialItem(specialItemPosition, specialItemInstance);
            }
        }

        // Remove special item association and destroy instance
        public void RemoveSpecialItem()
        {
            hasSpecialItem = false;

            if (specialItemInstance != null)
            {
                Destroy(specialItemInstance);
                specialItemInstance = null;
            }
        }
        // Check if this tile has a special item and return its position
        public bool HasSpecialItem(out Vector2Int position)
        {
            position = specialItemPosition;
            return hasSpecialItem;
        }

        // Play hammer animation and open tile with delay
        private void PlayHammerAnimationAndOpen()
        {
            if (hammerAnimationPrefab != null)
            {
                // Instantiate hammer animation on this tile
                var offset = Vector3.right * 1.5f + Vector3.up * 0.5f;
                GameObject hammer = Instantiate(hammerAnimationPrefab, transform.position + offset, Quaternion.identity);
                DOVirtual.DelayedCall(.6f, OpenTileAfterAnimation);
            }
            else
            {
                // If no hammer animation, just open immediately
                OpenTileAfterAnimation();
            }
        }

        // Open the tile after animation completes
        private void OpenTileAfterAnimation()
        {
            SetTileOpen();
            EventManager.GetEvent<Tile>(EGameEvent.TileSelected).Invoke(this);
        }

        // Implement UI touch interface method instead of OnMouseDown
        public void OnPointerClick(PointerEventData eventData)
        {
            // Only respond if the tile is selectable and closed
            if (!isOpen && levelManager != null && levelManager.hammerMode)
            {
                // Immediately disable hammer mode to prevent multiple uses
                levelManager.hammerMode = false;
                // Instead of opening immediately, play hammer animation first
                PlayHammerAnimationAndOpen();
            }
        }

        public override void FillIcon(ScriptableData iconScriptable)
        {
            UpdateColor((ColorsTile)iconScriptable);
        }

        private void UpdateColor(ColorsTile itemTemplate)
        {
            images[0].color = itemTemplate.faceColor;
            images[1].color = itemTemplate.topColor;
            images[2].color = itemTemplate.bottomColor;
        }

        public void ShowEffect()
        {
            fx.SetActive(true);
        }

        public GameObject GetSpecialItemInstance()
        {
            return specialItemInstance;
        }
    }
}