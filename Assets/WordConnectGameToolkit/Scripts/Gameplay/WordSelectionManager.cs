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
using WordsToolkit.Scripts.Utilities;

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

        private float parallelThreshold = 0.9f;
        private float minBackwardDistance = 100f;
        private float maxAngleForUnselect = .5f;
        private bool isGameWon = false;

        public bool IsSelecting => isSelecting;

        public UILineRenderer lineRenderer;
        [SerializeField]
        private Transform parentLetters;
        [SerializeField]
        private CanvasGroup panelCanvasGroup;

        [Header("UI References")]
        public Image backgroundSelectedWord;
        [SerializeField]
        private TextMeshProUGUI selectedWordText; // اگر از کامپوننت RTLTMPro استفاده می‌کنید، تایپ این را تغییر ندهید چون معمولا از TMP ارث‌بری می‌کند

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
            if (isSelecting)
            {
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

        // Persian/Arabic Normalization logic moved to PersianLanguageUtility
        // ----------------------------------------------------

        private void CheckForBackwardMovement()
        {
            if (selectedLetters.Count < 2) return;

            Vector2 lastLetterPos = selectedLetters[selectedLetters.Count - 1].GetComponent<RectTransform>().anchoredPosition;
            Vector2 secondLastLetterPos = selectedLetters[selectedLetters.Count - 2].GetComponent<RectTransform>().anchoredPosition;
            Vector2 currentMovement = currentMousePosition - lastLetterPos;

            float distanceBetweenLatest = Vector2.Distance(lastLetterPos, secondLastLetterPos);
            float dynamicMinBackwardDistance = distanceBetweenLatest * 0.5f;

            if (currentMovement.magnitude < dynamicMinBackwardDistance) return;

            if (selectedLetters.Count >= 2)
            {
                int i = selectedLetters.Count - 2;
                Vector2 letterPos = selectedLetters[i].GetComponent<RectTransform>().anchoredPosition;
                Vector2 previousSegment = selectedLetters[i + 1].GetComponent<RectTransform>().anchoredPosition - letterPos;

                if (IsMovingBackwardWithSmallAngle(currentMovement, previousSegment, letterPos))
                {
                    UnselectLettersFromIndex(i + 1);
                }
            }
        }

        private bool IsMovingBackwardWithSmallAngle(Vector2 currentMovement, Vector2 previousSegment, Vector2 segmentStart)
        {
            Vector2 currentDir = currentMovement.normalized;
            Vector2 prevDir = previousSegment.normalized;

            float dotProduct = Vector2.Dot(currentDir, prevDir);
            float angleInRadians = Mathf.Acos(Mathf.Clamp(dotProduct, -1f, 1f));
            float angleInDegrees = angleInRadians * Mathf.Rad2Deg;

            bool isOppositeDirection = dotProduct < 0;
            bool isSmallAngle = angleInDegrees > (180f - maxAngleForUnselect) && angleInDegrees < (180f + maxAngleForUnselect);

            return isOppositeDirection && isSmallAngle;
        }

        private void UnselectLettersFromIndex(int fromIndex)
        {
            for (int i = selectedLetters.Count - 1; i >= fromIndex; i--)
            {
                selectedLetters[i].SetSelected(false);
                selectedLetters.RemoveAt(i);
            }

            UpdateSelectedWordText();
            OnSelectionChanged?.Invoke(selectedLetters);
        }

        private void UpdateMousePosition()
        {
            Vector2 screenPosition = Vector2.zero;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else if (virtualMouseInput != null && virtualMouseInput.virtualMouse != null && virtualMouseInput.virtualMouse.leftButton.isPressed)
            {
                screenPosition = virtualMouseInput.virtualMouse.position.ReadValue();
            }
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
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
            if (virtualMouseInput != null && virtualMouseInput.virtualMouse != null && virtualMouseInput.virtualMouse.leftButton.isPressed) return true;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
            return false;
        }

        public void OnLevelLoaded(Level level)
        {
            if (lineRenderer != null)
            {
                lineRenderer.color = level.colorsTile.faceColor;
            }

            layout.GetComponent<Image>().color = level.colorsTile.faceColor;

            CleanupLevel();

            var letters = level.GetLetters(gameManager.language);
            int letterCount = letters.Length;
            var letterSize = 132 - Mathf.Max(0, letterCount - 6) * 10;

            for (int i = 0; i < letterCount; i++)
            {
                float angle = ((float)i / letterCount) * 2 * Mathf.PI - Mathf.PI / 2;
                float x = circleCenter.x + radius * Mathf.Cos(angle);
                float y = circleCenter.y + radius * Mathf.Sin(angle);

                var button = Instantiate(letterButtonPrefab, parentLetters);
                button.SetColor(level.colorsTile.faceColor);
                RectTransform rectTransform = button.GetComponent<RectTransform>();

                rectTransform.anchoredPosition = new Vector2(x, y);


                button.SetText(letters[i].ToString());
                button.letterText.fontSize = letterSize;
            }
        }

        public void CleanupLevel()
        {
            ClearSelection();
            ClearExistingLetters();

            if (selectedWordText != null)
            {
                selectedWordText.text = "";
            }

            SetBackgroundAlpha(0f);
            isSelecting = false;
            selectedLetters.Clear();
            isGameWon = false;
            SetPanelBlockRaycast(true);
        }

        private void ClearExistingLetters()
        {
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
            ClearSelection();

            isSelecting = true;
            selectedLetters.Add(button);
            soundBase.PlayIncremental(selectedLetters.Count);

            UpdateMousePosition();

            if (shuffleButton != null)
            {
                shuffleButton.GetComponent<CanvasGroup>().DOFade(0.0f, 0.3f);
            }

            SetBackgroundAlpha(1f);
            UpdateLineRenderer();
            UpdateSelectedWordText();

            OnSelectionChanged?.Invoke(selectedLetters);
        }

        public void AddToSelection(LetterButton button)
        {
            if (!isSelecting) return;

            if (selectedLetters.Count > 0 && selectedLetters[selectedLetters.Count - 1] == button)
                return;

            if (selectedLetters.Contains(button))
            {
                return;
            }

            soundBase.PlayIncremental(selectedLetters.Count);
            selectedLetters.Add(button);
            UpdateLineRenderer();
            UpdateSelectedWordText();
            OnSelectionChanged?.Invoke(selectedLetters);
        }

        private void UpdateLineRenderer()
        {
            if (lineRenderer == null) return;

            int pointCount = selectedLetters.Count + (isSelecting ? 1 : 0);
            Vector2[] points = new Vector2[pointCount];

            for (int i = 0; i < selectedLetters.Count; i++)
            {
                RectTransform letterRect = selectedLetters[i].GetComponent<RectTransform>();
                points[i] = letterRect.anchoredPosition;
            }

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

            if (selectedLetters.Count > 0)
            {
                string word = GetSelectedWord();
                OnSelectionCompleted?.Invoke(word);
                Invoke("ClearSelection", 0.1f);
            }
        }

        private string GetSelectedWord()
        {
            string word = "";
            foreach (var letter in selectedLetters)
            {
                // نکته: اگر LetterButton از RTLText استفاده می‌کند، مطمئن شوید GetLetter()
                // کاراکتر اصلی (بدون تغییر شکل RTL) را برمی‌گرداند.
                word += letter.GetLetter();
            }
            return word;
        }

        // --- اصلاح اصلی نمایش متن ---
        private void UpdateSelectedWordText()
        {
            if (selectedWordText != null)
            {
                selectedWordText.color = new Color(selectedWordText.color.r, selectedWordText.color.g, selectedWordText.color.b, 1f);

                string currentWord = GetSelectedWord();

                // اگر پلاگین RTL شما نیاز به Fix دارد، می‌توانید اینجا آن را صدا بزنید
                // اما اگر RTLTextMeshPro است، معمولا خودش هندل می‌کند، فقط ToUpper را حذف کردیم
                // selectedWordText.text = RTLTMPro.RTLSupport.FixRTL(currentWord); 

                selectedWordText.text = currentWord; // حذف .ToUpper() که باعث خرابی فارسی می‌شد

                UpdateHorizontalLayout(layout);
            }
        }

        public void ClearSelection()
        {
            foreach (var letter in selectedLetters)
            {
                letter.SetSelected(false);
            }

            selectedLetters.Clear();
            isSelecting = false;

            if (shuffleButton != null)
            {
                shuffleButton.GetComponent<CanvasGroup>().DOFade(1f, 0.3f);
            }

            if (lineRenderer != null)
            {
                lineRenderer.points = new Vector2[0];
            }

            if (selectedWordText != null)
            {
                selectedWordText.text = "";
            }
            SetBackgroundAlpha(0f);
        }

        // --- اصلاح متد اعتبارسنجی ---
        private void ValidateWordWithModel(string word)
        {
            if (string.IsNullOrEmpty(word)) return;

            string normalizedWord = PersianLanguageUtility.Normalize(word);


            List<Vector3> letterPositions = new List<Vector3>();

            if (selectedWordText != null && !string.IsNullOrEmpty(selectedWordText.text))
            {
                selectedWordText.ForceMeshUpdate();
                TMP_TextInfo textInfo = selectedWordText.textInfo;


                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    if (!textInfo.characterInfo[i].isVisible) continue;

                    Vector3 bottomLeft = selectedWordText.transform.TransformPoint(textInfo.characterInfo[i].bottomLeft);
                    Vector3 topRight = selectedWordText.transform.TransformPoint(textInfo.characterInfo[i].topRight);
                    Vector3 charCenter = (bottomLeft + topRight) / 2f;

                    letterPositions.Add(charCenter);
                }
            }

            if (fieldManager != null)
            {
                // For Persian RTL: check both the word and its reverse (user may select in either direction)
                string normalizedReversed = PersianLanguageUtility.Reverse(normalizedWord);
                if (fieldManager.IsWordOpen(normalizedWord) || fieldManager.IsWordOpen(normalizedReversed))
                {
                    ShakeSelectedWordBackground();
                    soundBase.PlayWrong();
                }

                if (fieldManager.ValidateWord(normalizedWord, letterPositions))
                {
                    SetPanelBlockRaycast(false);
                    AnimateAlphaDown(backgroundSelectedWord);
                    EventManager.GetEvent<string>(EGameEvent.WordOpened).Invoke(normalizedWord);
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

            Vector2 originalPosition = rectTransform.anchoredPosition;
            Sequence shakeSequence = DOTween.Sequence();

            float shakeAmount = 10f;
            float shakeDuration = 0.08f;
            int shakeCount = 3;

            for (int i = 0; i < shakeCount; i++)
            {
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
            DOVirtual.DelayedCall(1f, () =>
            {
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

            var letterButtons = parentLetters.GetComponentsInChildren<LetterButton>();
            if (letterButtons.Length < 2) return;

            var lettersToSwap = letterButtons;
            var positions = lettersToSwap.Select(btn => btn.GetComponent<RectTransform>().anchoredPosition).ToList();

            for (int i = positions.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                Vector2 temp = positions[i];
                positions[i] = positions[randomIndex];
                positions[randomIndex] = temp;
            }

            var sequence = DOTween.Sequence();
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

        public List<LetterButton> GetLetters(string wordForTutorial)
        {
            var letters = parentLetters.GetComponentsInChildren<LetterButton>();
            List<LetterButton> letterButtons = new List<LetterButton>();
            List<LetterButton> usedLetters = new List<LetterButton>();

            string normalizedTutorialWord = PersianLanguageUtility.Normalize(wordForTutorial);

            for (int i = 0; i < normalizedTutorialWord.Length; i++)
            {
                var letter = normalizedTutorialWord[i];
                var availableLetter = letters.FirstOrDefault(l =>
                {
                    // مقایسه با نرمال‌سازی
                    string btnLetter = PersianLanguageUtility.Normalize(l.GetLetter());
                    return btnLetter == letter.ToString() && !usedLetters.Contains(l);
                });

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

        public void SetVirtualMouseInput(VirtualMouseInput virtualMouse)
        {
            virtualMouseInput = virtualMouse;
        }
    }
}