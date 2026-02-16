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
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.System;
using TMPro;
using UnityEngine.Serialization;
using VContainer;
using WordsToolkit.Scripts.Audio;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Infrastructure.Service;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace WordsToolkit.Scripts.Gameplay
{
    public class WordSelectionManager : MonoBehaviour, IFadeable
    {
        // Event for when selected letters change
        public event Action<List<LetterButton>> OnSelectionChanged;
        // Event for when selection is completed
        public event Action<string> OnSelectionCompleted;

        [Header("References")]
        public FieldManager fieldManager;
        public LetterButton letterButtonPrefab;

        [Header("Circle Layout Settings")]
        public float radius = 200f;
        public Vector2 circleCenter = Vector2.zero;

        private List<LetterButton> selectedLetters = new List<LetterButton>();
        private bool isSelecting = false;
        private Vector2 currentMousePosition;
        private VirtualMouseInput virtualMouseInput;
        
        private float parallelThreshold = 0.9f; // Dot product threshold for small angle detection (0.9 = ~25 degrees)
        private float minBackwardDistance = 100f; // Minimum distance to move backward before unselecting
        private float maxAngleForUnselect = .5f; // Maximum angle in degrees between lines to trigger unselect
        private bool isGameWon = false;

        public bool IsSelecting => isSelecting;

        public UILineRenderer lineRenderer;
        [SerializeField]
        private Transform parentLetters;
        [SerializeField]
        private CanvasGroup panelCanvasGroup;
        
        [Header("UI References")]
        public Image backgroundSelectedWord; // Reference to the background image
        [SerializeField]
        private TextMeshProUGUI selectedWordText;

        [SerializeField]
        private HorizontalLayoutGroup layout;

        [Header("Rearrange Settings")]
        [SerializeField] private float swapAnimationDuration = 0.5f;
        [SerializeField] private Ease swapAnimationEase = Ease.InOutQuad;

        [Header("Shuffle Button")]
        [SerializeField] private Button shuffleButton;

        private GameManager gameManager;

        private ILevelLoaderService levelLoaderService;
        private IAudioService soundBase;
        [SerializeField]
        private CanvasGroup canvasGroup;

        [Inject]
        public void Construct(ILevelLoaderService levelLoaderService, GameManager gameManager, IAudioService soundBase, ButtonViewController buttonViewController)
        {
            this.soundBase = soundBase;
            this.gameManager = gameManager;
            this.levelLoaderService = levelLoaderService;
            this.levelLoaderService.OnLevelLoaded += OnLevelLoaded;
            buttonViewController.RegisterButton(this);
        }

        private void Awake()
        {
            // Try to find VirtualMouseInput component in the scene
            virtualMouseInput = FindObjectOfType<VirtualMouseInput>();
            OnSelectionCompleted += ValidateWordWithModel;
        }

        private void Start()
        {
            if (shuffleButton != null)
            {
                shuffleButton.onClick.AddListener(RearrangeRandomLetters);
            }
            
            if (fieldManager != null)
            {
                fieldManager.OnAllTilesOpened.AddListener(OnGameWon);
            }
        }

        private void OnDestroy()
        {
            if (shuffleButton != null)
            {
                shuffleButton.onClick.RemoveListener(RearrangeRandomLetters);
            }
            if (levelLoaderService != null)
            {
                levelLoaderService.OnLevelLoaded -= OnLevelLoaded;
            }
            if (fieldManager != null)
            {
                fieldManager.OnAllTilesOpened.RemoveListener(OnGameWon);
            }
        }

        private void Update()
        {
            // Update mouse position and line renderer during selection
            if (isSelecting)
            {
                // Check if input is still active, if not, end selection
                if (!IsAnyInputActive())
                {
                    EndSelection();
                    return;
                }
                
                UpdateMousePosition();
                CheckForBackwardMovement();
                UpdateLineRenderer();
            }
        }

        private void CheckForBackwardMovement()
        {
            if (selectedLetters.Count < 2) return;

            // Get the current line segment (from last selected letter to mouse position)
            Vector2 lastLetterPos = selectedLetters[selectedLetters.Count - 1].GetComponent<RectTransform>().anchoredPosition;
            Vector2 secondLastLetterPos = selectedLetters[selectedLetters.Count - 2].GetComponent<RectTransform>().anchoredPosition;
            Vector2 currentMovement = currentMousePosition - lastLetterPos;
            
            // Calculate minBackwardDistance as half the distance between the two latest letters
            float distanceBetweenLatest = Vector2.Distance(lastLetterPos, secondLastLetterPos);
            float dynamicMinBackwardDistance = distanceBetweenLatest * 0.5f;
            
            // Check if we've moved back far enough to consider unselecting
            if (currentMovement.magnitude < dynamicMinBackwardDistance) return;
            
            // Check only the most recent line segment for backward movement
            if (selectedLetters.Count >= 2)
            {
                int i = selectedLetters.Count - 2; // Only check the last segment
                Vector2 letterPos = selectedLetters[i].GetComponent<RectTransform>().anchoredPosition;
                Vector2 previousSegment = selectedLetters[i + 1].GetComponent<RectTransform>().anchoredPosition - letterPos;
                
                // Check if current movement is backward and at small angle to previous segment
                if (IsMovingBackwardWithSmallAngle(currentMovement, previousSegment, letterPos))
                {
                    // Unselect letters from this point forward
                    UnselectLettersFromIndex(i + 1);
                }
            }
        }

        private bool IsMovingBackwardWithSmallAngle(Vector2 currentMovement, Vector2 previousSegment, Vector2 segmentStart)
        {
            // Normalize the segments
            Vector2 currentDir = currentMovement.normalized;
            Vector2 prevDir = previousSegment.normalized;
            
            // Calculate the angle between the directions
            float dotProduct = Vector2.Dot(currentDir, prevDir);
            float angleInRadians = Mathf.Acos(Mathf.Clamp(dotProduct, -1f, 1f));
            float angleInDegrees = angleInRadians * Mathf.Rad2Deg;
            
            // Check if we're moving backward (opposite direction) with small angle
            bool isOppositeDirection = dotProduct < 0; // Negative dot product means opposite directions
            bool isSmallAngle = angleInDegrees > (180f - maxAngleForUnselect) && angleInDegrees < (180f + maxAngleForUnselect);
            
            return isOppositeDirection && isSmallAngle;
        }

        private void UnselectLettersFromIndex(int fromIndex)
        {
            // Remove letters from the specified index to the end
            for (int i = selectedLetters.Count - 1; i >= fromIndex; i--)
            {
                selectedLetters[i].SetSelected(false);
                selectedLetters.RemoveAt(i);
            }
            
            // Update UI
            UpdateSelectedWordText();
            OnSelectionChanged?.Invoke(selectedLetters);
        }

        private void UpdateMousePosition()
        {
            Vector2 screenPosition = Vector2.zero;
            
            // Check for touch input first (mobile devices)
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            // Check virtual mouse input
            else if (virtualMouseInput != null && virtualMouseInput.virtualMouse != null && virtualMouseInput.virtualMouse.leftButton.isPressed)
            {
                screenPosition = virtualMouseInput.virtualMouse.position.ReadValue();
            }
            // Fallback to regular mouse input
            else if (Mouse.current != null)
            {
                screenPosition = Mouse.current.position.ReadValue();
            }
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentLetters.GetComponent<RectTransform>(), 
                screenPosition, 
                Camera.main, 
                out currentMousePosition
            );
        }

        private bool IsAnyInputActive()
        {
            // Check touch input
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return true;
                
            // Check virtual mouse
            if (virtualMouseInput != null && virtualMouseInput.virtualMouse != null && virtualMouseInput.virtualMouse.leftButton.isPressed)
                return true;
                
            // Check regular mouse
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                return true;
                
            return false;
        }

        public void OnLevelLoaded(Level level)
        {
            if (lineRenderer != null)
            {
                lineRenderer.color = level.colorsTile.faceColor;
            }

            layout.GetComponent<Image>().color = level.colorsTile.faceColor;

            // Clean up previous level
            CleanupLevel();
            
            var letters = level.GetLetters(gameManager.language);
            int letterCount = letters.Length;
            var letterSize = 132 - Mathf.Max(0, letterCount - 6) * 10;
            
            for (int i = 0; i < letterCount; i++)
            {
                // Calculate the angle for this letter (in radians)
                // Start from the top (90 degrees or π/2) and go clockwise
                float angle = ((float)i / letterCount) * 2 * Mathf.PI - Mathf.PI/2;
                
                // Calculate position on the circle
                float x = circleCenter.x + radius * Mathf.Cos(angle);
                float y = circleCenter.y + radius * Mathf.Sin(angle);
                
                // Instantiate button
                var button = Instantiate(letterButtonPrefab, parentLetters);
                button.SetColor(level.colorsTile.faceColor);
                RectTransform rectTransform = button.GetComponent<RectTransform>();
                
                // Set position
                rectTransform.anchoredPosition = new Vector2(x, y);
                
                // Set text
                button.SetText(letters[i].ToString());
                button.letterText.fontSize = letterSize;
            }
        }
        
        /// <summary>
        /// Cleans up the current level state, including selections and UI elements
        /// </summary>
        public void CleanupLevel()
        {
            // Clear any active selection
            ClearSelection();
            
            // Clear any existing letters
            ClearExistingLetters();
            
            // Reset UI elements
            if (selectedWordText != null)
            {
                selectedWordText.text = "";
            }

            // Reset background
            SetBackgroundAlpha(0f);
            
            // Reset selection state
            isSelecting = false;
            selectedLetters.Clear();
            
            // Reset game won state and re-enable panel
            isGameWon = false;
            SetPanelBlockRaycast(true);
        }

        private void ClearExistingLetters()
        {
            // Find and destroy all existing letter buttons under parentLetters
            if (parentLetters != null)
            {
                LetterButton[] existingButtons = parentLetters.GetComponentsInChildren<LetterButton>();
                foreach (LetterButton button in existingButtons)
                {
                    Destroy(button.gameObject);
                }
            }
        }

        public void StartSelection(LetterButton button)
        {
            // Clear any previous selection
            ClearSelection();
            
            // Start a new selection
            isSelecting = true;
            selectedLetters.Add(button);
            soundBase.PlayIncremental(selectedLetters.Count);

            // Initialize mouse position
            UpdateMousePosition();

            // Fade out rearrange button during selection
            if (shuffleButton != null)
            {
                shuffleButton.GetComponent<CanvasGroup>().DOFade(0.0f, 0.3f);
            }
            
            // Make background visible when selection starts
            SetBackgroundAlpha(1f);
            
            // Start drawing the line
            UpdateLineRenderer();
            
            // Update the selected word text
            UpdateSelectedWordText();
            
            // Notify listeners about the selection change
            OnSelectionChanged?.Invoke(selectedLetters);
        }

        public void AddToSelection(LetterButton button)
        {
            if (!isSelecting) return;
            
            // Don't add if it's already the last selected letter
            if (selectedLetters.Count > 0 && selectedLetters[selectedLetters.Count - 1] == button)
                return;
                
            // Check if this letter is already selected elsewhere in the chain
            if (selectedLetters.Contains(button))
            {
                return;
            }

            soundBase.PlayIncremental(selectedLetters.Count);
            // Add this letter to our selection
            selectedLetters.Add(button);
            // Update the line to include the new letter
            UpdateLineRenderer();
            // Update the selected word text
            UpdateSelectedWordText();
            // Notify listeners about the selection change
            OnSelectionChanged?.Invoke(selectedLetters);
        }

        private void UpdateLineRenderer()
        {
            if (lineRenderer == null) return;
            
            // Create array of points for the line renderer
            // Add one extra point for the current mouse position when selecting
            int pointCount = selectedLetters.Count + (isSelecting ? 1 : 0);
            Vector2[] points = new Vector2[pointCount];
            
            // Update each selected letter position
            for (int i = 0; i < selectedLetters.Count; i++)
            {
                RectTransform letterRect = selectedLetters[i].GetComponent<RectTransform>();
                // Use anchoredPosition since that's what we set in OnLevelLoaded
                points[i] = letterRect.anchoredPosition;
            }
            
            // Add current mouse position as the last point if we're actively selecting
            if (isSelecting && selectedLetters.Count > 0)
            {
                points[points.Length - 1] = currentMousePosition;
            }
            
            lineRenderer.points = points;
        }
        


        public void EndSelection()
        {
            if (!isSelecting) return;
            
            isSelecting = false;

            // Process the word that was formed
            if (selectedLetters.Count > 0)
            {
                string word = GetSelectedWord();
                // Notify listeners that a word selection is completed
                OnSelectionCompleted?.Invoke(word);
                
                // Here you would check if the word is valid and handle scoring
                // For now, just clear selection after a short delay
                Invoke("ClearSelection", 0.1f);
            }
        }

        private string GetSelectedWord()
        {
            string word = "";
            foreach (var letter in selectedLetters)
            {
                word += letter.GetLetter();
            }
            return word;
        }

        // Update the selected word display using character prefabs
        private void UpdateSelectedWordText()
        {
            if (selectedWordText != null)
            {
                selectedWordText.color = new Color(selectedWordText.color.r, selectedWordText.color.g, selectedWordText.color.b, 1f);
                selectedWordText.text = GetSelectedWord().ToUpper();
                UpdateHorizontalLayout(layout);
            }
        }

        public void ClearSelection()
        {
            // Clear all visual selection indicators
            foreach (var letter in selectedLetters)
            {
                letter.SetSelected(false);
            }
            
            selectedLetters.Clear();
            isSelecting = false;
            
            // Fade in rearrange button when selection ends
            if (shuffleButton != null)
            {
                shuffleButton.GetComponent<CanvasGroup>().DOFade(1f, 0.3f);
            }
            
            // Clear the line renderer
            if (lineRenderer != null)
            {
                lineRenderer.points = new Vector2[0];
            }

            if (selectedWordText != null)
            {
                selectedWordText.text = "";
            }
            // Make background invisible when selection is cleared
            SetBackgroundAlpha(0f);
        }
        
        // Updated method that gets character positions and delegates validation to FieldManager
        private void ValidateWordWithModel(string word)
        {
            if (string.IsNullOrEmpty(word)) return;

            // Get the positions from the actual text characters
            List<Vector3> letterPositions = new List<Vector3>();
            
            if (selectedWordText != null && !string.IsNullOrEmpty(selectedWordText.text))
            {
                // Force text to update
                selectedWordText.ForceMeshUpdate();
                
                // Get mesh info which contains character positions
                TMP_TextInfo textInfo = selectedWordText.textInfo;
                
                // Go through each character in the text
                for (int i = 0; i < word.Length && i < textInfo.characterCount; i++)
                {
                    // Make sure character is visible (not a space or control character)
                    if (!textInfo.characterInfo[i].isVisible) continue;
                    
                    // Get the center position of the character in world space
                    Vector3 bottomLeft = selectedWordText.transform.TransformPoint(textInfo.characterInfo[i].bottomLeft);
                    Vector3 topRight = selectedWordText.transform.TransformPoint(textInfo.characterInfo[i].topRight);
                    Vector3 charCenter = (bottomLeft + topRight) / 2f;
                    
                    letterPositions.Add(charCenter);
                }
            }

            // Delegate the word validation to FieldManager
            if (fieldManager != null)
            {
                // If the word is already open, show the shake animation
                if (fieldManager.IsWordOpen(word))
                {
                    ShakeSelectedWordBackground();
                    soundBase.PlayWrong();
                }
                
                // If the word is valid and was successfully opened (or was already open),
                // animate the UI elements
                if (fieldManager.ValidateWord(word, letterPositions))
                {
                    SetPanelBlockRaycast(false);
                    AnimateAlphaDown(backgroundSelectedWord);
                    EventManager.GetEvent<string>(EGameEvent.WordOpened).Invoke(word);
                }
            }
        }

        private void OnGameWon()
        {
            isGameWon = true;
            SetPanelBlockRaycast(false);
        }

        private void ShakeSelectedWordBackground()
        {
            if (backgroundSelectedWord == null) return;

            RectTransform rectTransform = backgroundSelectedWord.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // Store original position
            Vector2 originalPosition = rectTransform.anchoredPosition;
            
            // Create shake sequence
            Sequence shakeSequence = DOTween.Sequence();
            
            // Create quick, small shakes (offset from original position)
            float shakeAmount = 10f;
            float shakeDuration = 0.08f;
            int shakeCount = 3;
            
            for (int i = 0; i < shakeCount; i++)
            {
                // Alternate directions: right, left, right
                float xOffset = (i % 2 == 0) ? shakeAmount : -shakeAmount;
                
                shakeSequence.Append(rectTransform.DOAnchorPos(
                    new Vector2(originalPosition.x + xOffset, originalPosition.y), 
                    shakeDuration).SetEase(Ease.OutQuad));
            }
            
            shakeSequence.Append(rectTransform.DOAnchorPos(originalPosition, shakeDuration));
        }

        private void AnimateAlphaDown(Image image)
        {
            image.DOFade(0f, 0.3f);
            selectedWordText.DOFade(0f, 0.3f);
            DOVirtual.DelayedCall(1f, () => {
                if (!isGameWon)
                    SetPanelBlockRaycast(true);
            });
        }

        private void SetPanelBlockRaycast(bool blockRaycast)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.blocksRaycasts = blockRaycast;
            }
        }

        // Helper method to set background alpha
        private void SetBackgroundAlpha(float alpha)
        {
            if (backgroundSelectedWord != null)
            {
                Color color = backgroundSelectedWord.color;
                color.a = alpha;
                backgroundSelectedWord.color = color;
            }
        }
        
        private void ForceUpdateLayout(RectTransform layoutRectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRectTransform);
        }
        
        private void UpdateHorizontalLayout(HorizontalLayoutGroup layout)
        {
            ForceUpdateLayout(layout.GetComponent<RectTransform>());
        }

        public void RearrangeRandomLetters()
        {
            if (parentLetters == null) return;
            
            // Get all letter buttons
            var letterButtons = parentLetters.GetComponentsInChildren<LetterButton>();
            if (letterButtons.Length < 2) return;
            
            int swapCount = letterButtons.Length;
            var lettersToSwap = letterButtons;
            
            // Create a list of positions and shuffle them
            var positions = lettersToSwap.Select(btn => btn.GetComponent<RectTransform>().anchoredPosition).ToList();
            
            // Shuffle positions using Fisher-Yates algorithm
            for (int i = positions.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                Vector2 temp = positions[i];
                positions[i] = positions[randomIndex];
                positions[randomIndex] = temp;
            }
            
            // Create a single sequence for all animations
            var sequence = DOTween.Sequence();
            
            // Add all animations to the sequence simultaneously using a single Join group
            var joinGroup = sequence.Join(DOTween.Sequence());
            for (int i = 0; i < lettersToSwap.Length; i++)
            {
                joinGroup.Join(
                    lettersToSwap[i].GetComponent<RectTransform>()
                        .DOAnchorPos(positions[i], swapAnimationDuration)
                        .SetEase(swapAnimationEase)
                );
            }
            
            sequence.Play();
        }
        
        private List<LetterButton> GetRandomLetters(LetterButton[] allLetters, int count)
        {
            List<LetterButton> randomLetters = new List<LetterButton>();
            List<int> availableIndices = new List<int>();
            
            // Initialize available indices
            for (int i = 0; i < allLetters.Length; i++)
            {
                availableIndices.Add(i);
            }
            
            // Pick random letters
            for (int i = 0; i < count && availableIndices.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableIndices.Count);
                int selectedIndex = availableIndices[randomIndex];
                randomLetters.Add(allLetters[selectedIndex]);
                availableIndices.RemoveAt(randomIndex);
            }
            
            return randomLetters;
        }

        public List<LetterButton> GetLetters(string wordForTutorial)
        {
            var letters = parentLetters.GetComponentsInChildren<LetterButton>();
            List<LetterButton> letterButtons = new List<LetterButton>();
            List<LetterButton> usedLetters = new List<LetterButton>();
            
            for (int i = 0; i < wordForTutorial.Length; i++)
            {
                var letter = wordForTutorial[i];
                var availableLetter = letters.FirstOrDefault(l =>
                    l.GetLetter().ToLower() == letter.ToString().ToLower() && 
                    !usedLetters.Contains(l));
                
                if (availableLetter != null)
                {
                    letterButtons.Add(availableLetter);
                    usedLetters.Add(availableLetter);
                }
                else
                {
                    letterButtons.Add(null);
                }
            }
            return letterButtons;
        }

        public void Hide()
        {
            canvasGroup.DOFade(0f, 0.3f);
        }

        public void InstantHide()
        {
            canvasGroup.alpha = 0f;
        }

        public void HideForWin()
        {
            Hide();
        }

        public void Show()
        {
            canvasGroup.DOFade(1f, 0.3f);
        }

        /// <summary>
        /// Manually set the VirtualMouseInput reference for controller support
        /// </summary>
        /// <param name="virtualMouse">The VirtualMouseInput component to use</param>
        public void SetVirtualMouseInput(VirtualMouseInput virtualMouse)
        {
            virtualMouseInput = virtualMouse;
        }
    }
}
