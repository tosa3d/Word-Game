using DG.Tweening;
using TMPro;
using UnityEngine;
using VContainer;
using WordsToolkit.Scripts.Audio;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.Utils;
using WordsToolkit.Scripts.System;
using WordsToolkit.Scripts.Enums;

namespace WordsToolkit.Scripts.GUI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TimerDisplay : MonoBehaviour
    {
        private TextMeshProUGUI timerText;
        [Inject]
        private LevelManager levelManager;
        [Inject]
        private IAudioService audioService;
        private CanvasGroup canvasGroup;
        private Color originalTextColor;
        [SerializeField]
        private AudioClip alertSound;

        private Sequence bounceSequence;
        private Sequence fadeSequence;
        private bool isWarningActive;
        private bool isVisible = true;

        private void Awake()
        {
            timerText = GetComponent<TextMeshProUGUI>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            // Add canvas group if it doesn't exist
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void Start()
        {
            originalTextColor = timerText.color;
            EventManager.OnGameStateChanged += OnGameStateChanged;
            UpdateVisibility();
        }

        private void OnDestroy()
        {
            EventManager.OnGameStateChanged -= OnGameStateChanged;
            
            if (bounceSequence != null)
            {
                bounceSequence.Kill();
                bounceSequence = null;
            }
            
            if (fadeSequence != null)
            {
                fadeSequence.Kill();
                fadeSequence = null;
            }
        }

        private void Update()
        {
            if (levelManager != null && isVisible)
            {
                float timeToDisplay;
                bool isTimerActive = levelManager.HasTimer;
                
                // Update timer text - display as countdown 
                if (levelManager.HasTimer && levelManager.TimerLimit > 0)
                {
                    // Calculate remaining time
                    float remainingTime = Mathf.Max(0, levelManager.TimerLimit - levelManager.GameTime);
                    timerText.text = TimeUtils.GetTimeString(remainingTime);
                    timeToDisplay = remainingTime;
                }
                else
                {
                    // For timers without limits, just show elapsed time
                    timerText.text = TimeUtils.GetTimeString(levelManager.GameTime);
                    timeToDisplay = levelManager.GameTime;
                }
                
                if (timeToDisplay <= 10f && !isWarningActive && isTimerActive)
                {
                    StartWarningEffect();
                }
                else if (timeToDisplay > 10f && isWarningActive)
                {
                    StopWarningEffect();
                }
            }
        }
        
        private void OnGameStateChanged(EGameState newState)
        {
            UpdateVisibility();
        }
        
        private void UpdateVisibility()
        {
            bool shouldBeVisible = levelManager.HasTimer && EventManager.GameStatus <= EGameState.Playing;
            
            if (shouldBeVisible && !isVisible)
            {
                ShowTimer();
            }
            else if (!shouldBeVisible && isVisible)
            {
                HideTimer();
            }
        }

        private void ShowTimer()
        {
            if (fadeSequence != null)
            {
                fadeSequence.Kill();
                fadeSequence = null;
            }

            isVisible = true;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            fadeSequence = DOTween.Sequence();
            fadeSequence.Append(canvasGroup.DOFade(1f, 0.3f));
        }

        private void HideTimer()
        {
            if (fadeSequence != null)
            {
                fadeSequence.Kill();
                fadeSequence = null;
            }

            isVisible = false;
            
            fadeSequence = DOTween.Sequence();
            fadeSequence.Append(canvasGroup.DOFade(0f, 0.5f));
            fadeSequence.OnComplete(() =>
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            });
        }

        private void StartWarningEffect()
        {
            if (timerText == null) return;

            if (alertSound != null)
                audioService.PlaySound(alertSound);
            
            isWarningActive = true;
            originalTextColor = timerText.color;

            if (bounceSequence != null)
            {
                bounceSequence.Kill();
                bounceSequence = null;
            }

            bounceSequence = DOTween.Sequence();
            bounceSequence.Append(timerText.transform.DOScale(1.2f, 0.3f));
            bounceSequence.Append(timerText.transform.DOScale(1f, 0.3f));
            timerText.color = new Color32(0xF6, 0x42, 0x8E, 0xFF);
            bounceSequence.SetLoops(-1);
        }

        private void StopWarningEffect()
        {
            if (timerText == null) return;

            isWarningActive = false;

            if (bounceSequence != null)
            {
                bounceSequence.Kill();
                bounceSequence = null;
            }

            timerText.transform.localScale = Vector3.one;
            timerText.color = originalTextColor;
        }

    }
}